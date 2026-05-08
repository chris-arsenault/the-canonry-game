# UI Principles

The UI is an event-stream consumer and presentation layer. It makes the
simulation legible, but it does not compute simulation truth or write runtime
state.

## Core Principles

1. The UI must not treat a kernel tick as a global redraw signal.
   - Kernel advancement only means new events may be available.
   - Each projection/read model owns the version or delta state for the screen
     section it feeds.
   - Each render component presents only when its own data, visibility, input,
     or layout state changes.

2. Render components own Godot subtrees.
   - `Main` remains the composition root and input scheduler.
   - `GameRenderTree` coordinates presentation, but each child renderer mutates
     only the subtree it owns.
   - Projections/read models expose data and versions; they never call Godot
     APIs.

3. Default hot paths show the living world.
   - Historical entities and relationships remain queryable for future history
     and memorialization views.
   - Default map, browser, card, and relationship surfaces ignore historical
     entities and relationships with historical endpoints.
   - Player attention belongs on current world state unless a view explicitly
     asks for history.

4. UI work must not scale with total historical state.
   - Hot panels should read active indexes or dirty sets.
   - Large collections should use keyed direct updates, pooling, or
     virtualization.
   - `ViewSync` is acceptable only for small, bounded panels where full
     reordering is cheap and clear.

5. Layout and tooltip work should be state driven.
   - Floating panels are positioned on resize, visibility, or layout changes,
     not every frame.
   - Tooltip text is keyed by hovered item, view mode, and relevant projection
     versions.
   - Per-frame hover probing is acceptable only until input-driven invalidation
     becomes the measured bottleneck.

6. Independent consumer cursors are acceptable.
   - A read model may keep its own event cursor if that keeps ownership clear.
   - Cursor fanout should be revisited only if event-drain cost becomes a
     measured bottleneck.

## Component Render Tree

Current presentation shape:

```text
GameRenderTree
  TopBarRender
  MapRender
    HexCellRender
    MapFrameRender
    EntityMapRender
    TooltipRender
  EntityBrowserRender
  ChronicleRender
  CardLayerRender
  AttributeGraphRender
  TableauRender
  FloatingHudLayoutRender
```

Each render component stores the last state it presented and returns
immediately when clean. Common state inputs are projection versions, dirty
sets, layout state, visibility, and active UI mode.

## Version And Delta Ownership

- `HexMapProjection.CellVersion`: terrain, influence, owner, road, or other
  cell content changed.
- `HexMapProjection.DirtyCellIds`: changed map cells for visible-plane delta
  rendering.
- `HexMapProjection.OwnerColorEntityIds` and `OwnerColorVersion`: owner or
  influence color membership changed.
- `HexMapProjection.FrameVersion`: map frame, subdivision, or region-boundary
  inputs changed.
- `UiProjectionState.EntityMapVersion`: active map entity set or positions
  changed.
- `UiProjectionState.DirtyEntityHexes`: changed occupied hexes for entity-map
  delta rendering.
- `UiProjectionState.ActiveOccupants`: active entity ids indexed by plane and
  hex for entity-map and tooltip lookup.
- `UiProjectionState.EntityBrowserVersion`: active entity list, grouping, or
  row summary changed.
- `UiProjectionState.EntityDetailVersion`: selected-card/readings details
  changed.
- `UiProjectionState.RelationshipVersion`: relationship details changed; active
  relationship indexes feed entity cards.
- `UiProjectionState.TableauVersion`: generator/gambit/tableau-visible counters
  changed.
- `UiChronicleProjection.Version` and `GameRuntimeChronicle.Version`: chronicle
  text changed.

## Current Implementation Notes

- Map cells are component-version gated and dirty-cell delta updated.
- Entity map presentation uses active-occupant indexes and dirty hexes.
- Entity browser presentation is active-only, version gated, and virtualized
  with spacer controls plus row/header pools.
- Entity cards read selected-entity active relationship indexes instead of
  scanning the full relationship graph.
- Chronicle presentation is version gated and incrementally builds visible text.
- Tableau and attribute graph presentation are visible/version gated.
- Floating tableau layout is dirty-state driven.
- Tooltip text and position updates are cached by hover/projection/layout
  state.

## Future Improvement Triggers

Do not optimize these areas preemptively. Revisit them when profiling or
long-run play exposes a concrete bottleneck:

- bound unresolved chronicle/provenance chain candidates if they accumulate;
- maintain grouped active-entity browser lists or row-level deltas if active
  entity counts make browser snapshot rebuilds expensive;
- move tooltip hover probing to input/camera invalidation if paused-frame
  profiling calls it out;
- add per-entity detail versions for attribute graph or pre-sorted tableau
  summaries if those visible panels become hot.
