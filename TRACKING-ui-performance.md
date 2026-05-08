# UI Performance Tracking

Status: active investigation; component render tree, dirty map/entity
rendering, and default hot-path indexes have landed.

Observed symptom: by about tick 5,000, interactive UI advancement can become
slower than 1 second per tick. The simulation performance budget remains 10
ticks/second, so default UI work must not scale with total historical state.

This document records visual-inspection findings only. Numbers below are
code-path counts, fanout, or known runtime targets, not profiler timings.

## Current State

1. Whole-UI tick fanout is removed.
   - Current path: `Main._Process` pumps the runtime, then calls
     `GameRenderTree.Present`.
   - `GameRenderTree` gates top bar, map cells, map frame, entity map,
     browser, cards, chronicle, tableau, and attribute graph with component
     versions.
   - A kernel tick no longer directly means map + entities + browser + cards +
     chronicle + tableau all redraw.

2. Map rendering is component-version gated and cell-delta gated.
   - `HexMapProjection.CellVersion` and `FrameVersion` split dynamic cell
     content from static-ish frame/backdrop work.
   - `HexMapProjection.DirtyCellIds` tracks changed cells per plane, and
     `HexMapRenderer.RefreshDirtyCells` updates only those cell nodes after the
     initial/full refresh path.
   - Owner/influence color inputs are tracked by `OwnerColorEntityIds` and
     `OwnerColorVersion`; the renderer no longer scans both map planes to build
     colors on each cell refresh.
   - Remaining cost: dirty cell updates still rebuild the changed cell's render
     item and queue redraw for that cell.

3. Static map frame redraw is no longer tick-driven.
   - `MapRender` calls `RefreshFrame` only when `FrameVersion`, plane, region
     visibility, or hex size changes.
   - Remaining cost: a frame refresh still recomputes full backdrop/frame
     geometry and labels.

4. Historical entities are removed from the default hot display path.
   - Entity browser defaults to active entities only.
   - Entity map layer defaults to active entities only.
   - Entity card relationship display ignores inactive relationships and
     relationships with historical endpoints.
   - Historical entities and relationships remain queryable through the
     projection state for future history/memorialization views.

5. Entity browser invalidation is isolated from movement and relationships.
   - `UiProjectionState.EntityBrowserVersion` advances for active-list changes
     and row-summary changes.
   - Movement-only and relationship-only events do not rebuild the registry.
   - Remaining cost: when the browser is dirty, it still sorts/groups all active
     entities and uses `ViewSync` over the full active registry.

6. Entity map rendering is version gated and dirty-hex delta updated.
   - `EntityMapRender` checks `UiProjectionState.EntityMapVersion`, plane, and
     name visibility before refreshing.
   - `UiProjectionState` tracks active occupants and dirty entity hexes per
     plane; movement updates source/destination hexes, and `EntityLayer`
     refreshes only entities in dirty hexes.
   - Remaining cost: full refresh still runs on runtime bind, plane changes, and
     name visibility changes.

7. Chronicle rendering is incremental enough to avoid full provenance sorting.
   - `GameRuntimeChronicle` caches visible lines, tracks provenance sequence,
     scans only new invocations into pending chain candidates, and only updates
     the panel when `Version` changes.
   - Remaining cost: `GameRenderTree.Present` still calls
     `GameRuntimeChronicle.Refresh` every frame, and pending chain candidates
     can be revisited until their parent/trigger data appears.
   - Remaining cost: provenance projection still retains invocation history for
     later chain queries.

8. Interactive run logging is opt-in.
   - UI runtime creates `RunLogger` only when a run log path is provided.
   - Normal interactive play no longer pays disk logging or the extra logger
     drain.

9. Entity cards use active relationship indexes.
   - `UiProjectionState` maintains outgoing and incoming active relationship
     indexes and excludes inactive relationships or historical endpoints when
     indexing.
   - Entity cards query only the selected entity's active edges.
   - Remaining cost grows with the selected entity's visible degree and number
     of open cards, not total relationship graph size.

10. Tooltip hex picking and occupant lookup are indexed.
    - `HexMapRenderer.GetHexAtWorldPosition` converts world position directly
      to axial coordinates and validates with `TryGetHex`.
    - Semantic tooltip occupant text reads `UiProjectionState.ActiveOccupants`
      for the hovered semantic coordinate.

11. `ViewSync` is no longer called by every surface every tick, but it remains
    too blunt for dirty large collections.
    - `ViewSyncCore.Sync` allocates a removal set, calls update for every
      snapshot item, and reorders nodes.
    - Remaining hot use: entity browser rows whenever the browser is dirty.

12. Tableau and attribute graph are version gated.
    - Tableau refreshes only when visible and `UiProjectionState.TableauVersion`
      changes.
    - Attribute graph refreshes only when visible and entity detail version
      changes.
    - Remaining cost: when dirty, both still rebuild/sort their visible data
      from projection-wide state.

13. Per-frame layout and tooltip work remain.
    - `GameRenderTree.PositionFloatingHud` runs every frame to size/place the
      floating tableau panel.
    - `HexTooltipPanel.UpdateTooltip` runs every frame while the UI is active.
    - These are smaller than full redraw fanout, but should become
      layout/hover-state driven if profiling shows frame overhead while paused.

## Remaining Improvement Targets

1. Replace `ViewSync` on the remaining large collection.
   - Keep `ViewSync` for small panels.
   - Entity browser still uses `ViewSync` and rebuilds/sorts/group all active
     rows whenever `EntityBrowserVersion` changes.
   - Consider browser virtualization or incremental row updates if active entity
     count becomes large enough.

2. Bound chronicle/provenance UI work.
   - Keep chain matching incremental, but cap or age out unresolved pending
     chain candidates if they can accumulate.
   - Consider disabling provenance projection for UI modes that do not display
     history chains.

3. Reduce per-frame non-simulation UI work.
   - Make floating tableau placement respond to layout/visibility changes
     instead of every frame.
   - Make tooltip formatting update only when hovered hex, plane, or relevant
     projection version changes.

4. Lower-priority panel rebuilds.
   - Attribute graph can use per-entity detail versions if it becomes visible
     during long runs.
   - Tableau can maintain pre-sorted generator/gambit summaries if tableau
     interaction becomes a measured bottleneck.
