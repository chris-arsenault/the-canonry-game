# UI Scene Refactor — Plan

## Context

The Godot-side code today is "kernel-viewer" shaped: `Main.cs` (1057 lines)
builds the entire UI procedurally in `_Ready`, `HexMapRenderer` paints the map
via `_Draw()`, and every panel rebuilds its child tree from the projection on
every refresh. There are no `.tscn` files except a one-node `Main.tscn`.

This works because all game state lives in the kernel and the view is a
read-model viewer. It strains in three ways:

1. `Main.cs` is a god object doing layout, input, camera, hover, card mgmt,
   sim controls, and projection wiring.
2. There is no surface for animation — cards "fire" by a label changing, etc.
3. Map and entity picking are hand-rolled distance loops; `EntityCard`
   re-implements drag/resize/scroll-anchor from scratch.

The goal of this refactor is idiomatic Godot 2D **patterns** (scene-per-object,
persistent view nodes, `Area2D` picking, `Tween` animation, signal-driven
communication) without committing to `AnimationPlayer` or visual editor
authoring of complex scenes. Scene files are kept minimal — root + script +
maybe one or two child stubs — so the script (where iteration is cheapest)
does the heavy lifting in `_Ready`.

## End-state target

- `Main.tscn` is a real layout: chrome (top bar, bottom bar, registry rail,
  chronicle rail, map area) authored as a tree, not built imperatively.
  `Main.cs` shrinks to ~200 lines doing only runtime wiring and input.
- One `.tscn` per game-object kind: `EntityCard.tscn`, `TableauPanel.tscn`,
  `Card.tscn`, `Entity.tscn`. Each is `root + script + minimal child stubs`.
- Per-object identity in the view: each `Card` and `Entity` is a persistent
  node, addressed by id, that updates in place from the projection. Replaces
  the current "rebuild from scratch every refresh" model.
- `HexMapRenderer` keeps `_Draw()` for terrain, regions, territory overlay,
  rose window — those are batched cartography and there's no win in changing
  them. It loses the entity-marker drawing (moves to `Entity` scenes).
- Picking on the map via `Area2D` signals on each `Entity`. The hand-rolled
  `HandleMapClick` distance loop dies. Hex hover stays as it is for now.
- Animation is `Tween` only, in code, on persistent nodes. No
  `AnimationPlayer`.

## The linchpin: `ViewSync<TKey, TNode>`

This must come before any per-object scene work, because every kind of
persistent view object needs the same diff-from-projection logic. Without it,
each migration reinvents the wheel and they diverge.

Each tick, `Sync()` does a three-way diff between the keys in its internal
dict and the keys in the projection snapshot:

- **Add** — key in snapshot, not in dict → instantiate scene, parent, call
  updater so the new node has correct content from frame zero, store in dict.
- **Update** — key in both → call `update(existingNode, newData)`. Same C#
  object, same node id, in-flight Tween keeps running, signal connections
  stay live.
- **Remove** — key in dict, not in snapshot → `QueueFree()` and drop from
  the dict.

Sketch (~30 lines):

```csharp
public sealed class ViewSync<TKey, TNode> where TNode : Node
{
    private readonly Dictionary<TKey, TNode> _byKey = [];

    public IReadOnlyDictionary<TKey, TNode> Nodes => _byKey;

    public void Sync<TData>(
        Node parent,
        IEnumerable<KeyValuePair<TKey, TData>> snapshot,
        Func<TData, TNode> create,
        Action<TNode, TData> update)
    {
        var seen = new HashSet<TKey>();
        foreach (var (key, data) in snapshot)
        {
            seen.Add(key);
            if (!_byKey.TryGetValue(key, out var node))
            {
                node = create(data);
                parent.AddChild(node);
                _byKey[key] = node;
            }
            update(node, data);
        }
        foreach (var key in _byKey.Keys.Where(k => !seen.Contains(k)).ToList())
        {
            _byKey[key].QueueFree();
            _byKey.Remove(key);
        }
    }
}
```

Each migration plugs into it:

```csharp
_entitySync.Sync(
    parent: this,
    snapshot: _projection.Entities,
    create: _ => _entityScene.Instantiate<Entity>(),
    update: (entity, data) => entity.BindTo(data));
```

"Get this right once" because four migrations call into it. If the helper has
a bug (orphaned nodes, missed removes, double-creates), every migrated
component inherits it. Dedicated commit + a few unit tests with a fake `Node`,
then nothing else touches it.

## Phased plan

Each phase is shippable on its own, leaves `make ci` clean, and is
independently revertible.

A "migration" in this plan = converting one existing UI element from its
current pattern (procedurally built in code, fully rebuilt on every
`Refresh()`) to the new pattern (instanced from a `.tscn`, persistent node,
updated in place via `ViewSync`).

### Phase 1 — Decompose `Main.cs` chrome (no scenes for game objects yet)

Pure mechanical refactor. The chrome-building methods (`BuildTopBar`,
`BuildBottomBar`, `BuildChroniclePanel`, `BuildLegendPanel`) move out of
`Main.cs` into their own scripts:

- `TopBar.cs` + `TopBar.tscn` (root: `PanelContainer`, script attached)
- `BottomBar.cs` + `BottomBar.tscn`
- `ChroniclePanel.cs` + `ChroniclePanel.tscn`
- `LegendPanel.cs` + `LegendPanel.tscn`
- `MapArea.cs` + `MapArea.tscn` (host for plane toggle, legend, future
  overlays)

`Main.tscn` becomes a real layout tree:
`Main → CanvasLayer/UiLayer → Control(UiRoot) → VBoxContainer → [TopBar,
MiddleRow → [EntityBrowser, MapArea, ChroniclePanel], BottomBar]`.
`Main.cs`'s `_Ready` shrinks to: load assets, init kernel, get child
references via `GetNode<>`, wire signals.

This phase is not yet a "migration" by the definition above — it preserves
the build-procedurally-rebuild-on-refresh pattern. It just moves where the
procedural construction lives.

**Validation:** identical visual output, identical behavior, `Main.cs` under
~400 lines.

This is also the right phase to land the **screenshot harness** (Playwright-
style: launch the game, click through scripted interactions, capture screen-
shots into `.artifacts/`). Phase 2 onward is when "did the visual change?"
becomes the dominant question, and without a screenshot loop, iteration on
visual bugs degrades to multi-round guessing.

### Phase 2 — `ViewSync<TKey, TNode>` + retrofit `EntityBrowser`

Build `ViewSync`. Migrate `EntityBrowser.Refresh()` to use it instead of
`QueueFree`-and-rebuild. Each entity row becomes an `EntityRow.tscn` with the
glyph + button + subtitle. Selection signals stay the same.

Lowest-stakes test of the pattern — it's a list, not a game world — and proves
the diff/update contract before applying it to the map.

**Validation:** registry behaves identically. No more flicker on refresh
(quiet bonus). Scrolling preserves position across refresh (also free).

### Phase 3 — `EntityCard` as a scene

`EntityCard.tscn` + `EntityCard.cs` (script trims down — no more procedural
body construction).

- Replaces hand-rolled `_GuiInput` drag with
  `_GetDragData`/`_CanDropData`/`_DropData`. Smaller, harder to get wrong.
- `Main` keeps the `_openCards` dict but uses `ViewSync` to manage it.
- Add: a 120ms `Tween` modulate fade-in on open, fade-out on close. Single
  line each.
- Defer: resize. Either keep the hand-rolled grip or drop it; recommend
  dropping pending a real need.

**Validation:** clicking an entity opens a card that looks the same and drags
the same.

### Phase 4 — `Card.tscn` + `TableauPanel.tscn`

`Card.tscn`: root `PanelContainer`, child `TextureRect` (face), `Label`
(name), `Label` (status). Script exposes `BindTo(CardInstance,
GameTableauCardState)` and a tap-flash Tween (`modulate` to white briefly
when the card fires).

`TableauPanel.tscn`: root `PanelContainer` with the title, scroll, etc.
Script uses `ViewSync<CardRuntimeId, Card>` to manage children from
`GameRuntimeTableau.Cards`.

Drag-and-drop wiring scaffolded but inert (no gameplay action yet) —
`Card._GetDragData` returns the card id, no drop targets defined. Plumbing in
place for "drag a card to play it" without committing to the gameplay shape.

Tap-flash trigger: each tick, check `projection.GeneratorFires` for new
entries; for each, look up the corresponding card node via `ViewSync.Nodes`;
call `card.Flash()`. **The model:** simulation events from the projection
drive view animations on persistent nodes.

**Validation:** tableau looks the same; uniform card heights are now
structural (set once on `Card.tscn`); cards visibly pulse when their
generator fires.

### Phase 5 — `Entity.tscn` on the map

`Entity.tscn`: root `Node2D`, child `Sprite2D`, `Label` for name, `Area2D` +
`CollisionShape2D` (circle, radius ~glyph size) for picking. Script exposes
`BindTo(UiEntityProjection)`, a `Selected` signal, and a
`MoveTo(Vector2 worldPos)` that tweens position over ~250ms.

A new `EntityLayer.tscn` (root `Node2D`) sits in `Main.tscn` between the
`HexMapRenderer` and the camera/UI, owning the `Entity` children. Script
uses `ViewSync<int, Entity>` from `projection.Entities`.

`HexMapRenderer.DrawEntityMarkers` is deleted. The fan-out-on-shared-hex
logic moves into `EntityLayer.PositionEntity` so multiple entities on one
hex still don't overlap — same algorithm, different home.

Plane toggle animation: when `CurrentPlane` changes, every `Entity`'s
`MoveTo(targetCoordInNewPlane)` fires. They glide instead of jumping. Most
visible single payoff in the whole refactor.

Death animation: when an entity transitions to historical status, fade
modulate to 30% over 400ms, leave the node in place (dead entities are still
listed in the registry). When the projection drops the entity entirely,
`QueueFree` it.

`Main.HandleMapClick` is deleted — replaced by `Entity.input_event` signals
on each `Area2D`, forwarded to `OpenEntityCard`.

**Validation:** entities visibly move when the plane toggles; clicking an
entity opens its card; same z-order behavior on shared hexes.

### Phase 6 — Deferred (separate decisions, not in this refactor)

Hex picking via `Area2D`, hex map via `TileMapLayer`, EventBus autoload.
None are required by Phases 1–5. Each has its own tradeoffs and should be
evaluated standalone after Phase 5 is in.

## Explicitly out of scope

- `AnimationPlayer` (per direction).
- `AttributeGraphPanel` migration. Debug panel, low traffic, skip.
- Theme rewriting. `UiTheme.cs` and `UiTokens.cs` stay as-is — they work and
  the code form is more legible than `Theme.tres` would be for blind editing.
- Per-hex scenes / `TileMapLayer`. Separate decision; the rose window in
  particular wants to keep its `_Draw()` overlay.

## Risks and mitigations

1. **View-state leakage.** A `Card` or `Entity` script that starts holding
   "my future state" instead of reflecting projection is a regression of the
   kernel/view boundary. Mitigation: every script's update path is
   `BindTo(snapshotData)` only — no methods that mutate state without a
   fresh snapshot. Code review rule, easy to enforce by reading.

2. **Visual regressions during migration.** The class of error currently hit
   (off-screen, wrong layer, click-through, non-uniform heights, label on
   wrong layer) won't go away with scenes alone. Mitigation: screenshot
   harness lands in Phase 1, before Phase 3. Without it, iteration on visual
   bugs is multi-round guessing.

3. **Performance ceiling for per-entity nodes.** Fine for MVP-sized worlds
   (≤ a few hundred entities). If late-game numbers grow into the thousands,
   "interesting" entities stay as scenes and "swarm" entities go back to
   `MultiMeshInstance2D` or `_Draw()`. Flag, don't pre-optimize.

4. **Z-order on shared hexes.** Today the renderer fans them in one pass;
   with persistent nodes, fan-position must be recomputed whenever the
   shared-hex set changes. Mitigation: `EntityLayer` recomputes per-hex
   offsets in its sync pass. ~20 lines.

5. **`Main.tscn` and per-object `.tscn` files authored blind in text.** The
   risk applies to the *initial* writes of these scene files. Mitigation:
   keep scenes minimal (root + script + one or two child stubs); the script
   does the heavy lifting in `_Ready` where iteration is cheapest. Same
   playbook the existing `Main.tscn` already follows.

## Validation gate between phases

Each phase has a short, concrete check. If the check fails, that phase
reverts cleanly because each phase is an isolated commit set.

- **Phase 1:** `Main.cs` ≤ 400 lines; all keystrokes/clicks behave
  identically; visual diff = none.
- **Phase 2:** registry no longer flickers; scroll position survives a tick.
- **Phase 3:** card open/close drag identical; cards fade in/out.
- **Phase 4:** cards uniform height; firing card visibly pulses.
- **Phase 5:** plane toggle glides entities; map clicks land on `Area2D`;
  dead entities fade.

## Open questions before starting

1. Confirm the screenshot harness lands in Phase 1 (or accept slower visual
   iteration through Phases 3–5).
2. Should the deferred drag-and-drop scaffolding in Phase 4 actually wire to
   a real "play card" intent, or stay inert? Affects whether
   `Card._DropData` is just plumbing or has gameplay meaning.
3. Phase 5's death-fade timing (400ms) and plane-toggle glide (250ms) are
   guesses. Worth tuning once visible, not now.

## What I (Claude) can and cannot do in this space

Honest self-assessment from the conversation that produced this plan:

- **Can do at parity or better than current code-only approach:**
  scene-instancing in C#, `Area2D` picking, `Tween` animations, signals,
  `_GetDragData`/`_DropData`, `ViewSync`-style diff helpers, splitting
  `Main.cs` into smaller scripts. The same bugs I make in code-built UIs
  (off-screen, wrong layer, non-uniform heights) I would also make in
  `.tscn` files, but `.tscn`'s explicit tree structure helps with at least
  the "label parented under wrong node" and "card heights not uniform"
  classes.

- **Cannot do well:** `AnimationPlayer` keyframes (visual artifact, blind
  authoring is bad), `Theme.tres` authored visually (UiTheme.cs in code is
  more readable for me).

- **The actual ceiling is feedback, not API.** I cannot see the running
  game. The screenshot harness is the single biggest capability multiplier
  for this refactor; without it, iteration on visual regressions stays slow
  regardless of code-vs-scene format.
