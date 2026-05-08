# UI Component Render Tree

Status: implementation design.

The UI must stop treating a kernel tick as a global redraw signal. Each event consumer/read model should own the invalidation state for the screen section it feeds, and each render component should update only the Godot subtree it owns.

## Goal

Replace this pattern:

```text
tick processed -> Main refreshes map + entities + browser + cards + chronicle + tableau
```

with this pattern:

```text
event consumer -> section read model version/deltas -> render component presents if dirty
```

`Main` remains the composition root and input scheduler. It does not decide that a tick means every panel changed.

## Tree

```text
GameRootRender
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
```

## Rules

1. A render component owns exactly one UI subtree.
2. A component presents only when its own version, input state, visibility, or layout state changes.
3. Consumer cursors may remain independent. Cursor fanout is acceptable until event-drain cost proves otherwise.
4. Godot nodes are mutated only by render components on the main thread.
5. Projections/read models expose versions or deltas; they do not call Godot APIs.
6. Durable UI state belongs in read models. Presentation state, such as last presented version, belongs in render components.
7. Historical entities stay queryable, but default hot-path render components present active entities only.

## Version Ownership

- `HexMapProjection.CellVersion`: terrain, influence, owner, road, or cell content changed.
- `HexMapProjection.FrameVersion`: map frame/subdivision/region boundary inputs changed.
- `UiProjectionState.EntityMapVersion`: active map entity set or positions changed.
- `UiProjectionState.EntityBrowserVersion`: active entity list/grouping or row summary changed.
- `UiProjectionState.EntityDetailVersion`: selected-card/readings details changed.
- `UiProjectionState.RelationshipVersion`: relationship details changed.
- `UiProjectionState.TableauVersion`: generator/gambit/tableau-visible counters changed.
- `UiChronicleProjection.Version` and `GameRuntimeChronicle.Version`: chronicle text changed.

## Implementation Shape

`GameRenderTree.Present(...)` receives the latest runtime/read models and asks each component to present itself. Components compare read-model versions against their `LastPresented*` fields and return immediately when clean.

Examples:

- `ChronicleRender` checks `GameRuntimeChronicle.Version`; no other screen change can update chronicle UI.
- `EntityBrowserRender` checks `EntityBrowserVersion`; movement-only and relationship-only events do not rebuild the registry.
- `EntityMapRender` checks `EntityMapVersion`; relationship-only events do not move or redraw map entities.
- `MapRender` checks `CellVersion` for cells and `FrameVersion` for frame/backdrop.
- `TableauRender` owns tableau visibility and data versions; `Main` does not tick-refresh it.

## Non-goals

- Do not implement full map cell deltas until the component tree is in place.
- Do not make projections mutate Godot nodes directly.
- Do not collapse all consumers behind one cursor unless projection-drain cost becomes the next measured bottleneck.
