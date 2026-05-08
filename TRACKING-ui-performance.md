# UI Performance Tracking

Status: active investigation; first quick wins and component render tree landed.

Observed symptom: by about tick 5,000, interactive UI advancement can become slower than 1 second per tick. The simulation performance budget remains 10 ticks/second, so UI work must not scale with total historical state on the default display path.

This document records visual-inspection findings only. Numbers below are code-path counts, fanout, or known runtime targets, not profiler timings.

## Findings

1. `Main._Process` fans one tick pulse into 6 full UI refreshes.
   - Source: `src/TheCanonryGame.App/Main.cs:70`.
   - On any `pulse.TicksProcessed > 0`, the app refreshes map renderer, entity layer, entity browser, card layer, attribute graph, and tableau.
   - Chronicle, top bar, HUD positioning, camera movement, and tooltip update run every frame regardless of tick pulse.
   - Risk: a single processed tick causes multiple full-world rebuilds, and a stalled frame can accumulate more event/projection work before the next presentation.

2. The map renderer rebuilds and redraws all visible cells every tick.
   - Sources: `src/TheCanonryGame.UI/HexMapRenderer.cs:56`, `src/TheCanonryGame.UI/HexMapRenderer.cs:87`, `src/TheCanonryGame.UI/HexCell.cs:29`.
   - Each refresh sorts all hexes for the current plane and materializes an array.
   - Every cell bind calls `QueueRedraw()`.
   - Owner color assignment scans physical and semantic planes together, so the default physical view still pays for 2 planes of owner/influence data.
   - Per-cell drawing allocates point arrays and closed polylines during redraw.

3. The static map frame is redrawn every tick.
   - Source: `src/TheCanonryGame.UI/HexMapFrameLayer.cs:17`.
   - `Bind` always calls `QueueRedraw()`.
   - `_Draw` materializes all cells for the plane, then performs 3 border passes: continent, superregion, and owner/region borders.
   - Each border pass checks up to 6 neighbors per cell.
   - Subdivision labels group all cells twice, once for continent and once for superregion.
   - Risk: terrain/subdivision borders are mostly static but are currently tick-driven.

4. The entity browser is a full unvirtualized history list.
   - Source: `src/TheCanonryGame.UI/EntityBrowser.cs:90`.
   - Each refresh sorts every projected entity by kind and display name, groups by kind, materializes group arrays, and updates every row.
   - Historical entities are included by default.
   - Tracking item: default UI lists should display active entities only. Historical entities need an explicit filter/view/query path for memorialization and audit features.

5. The map entity layer refreshes all entities, including historical entities.
   - Source: `src/TheCanonryGame.UI/EntityLayer.cs:51`.
   - Each refresh sorts all projected entities by entity id, builds a new hex offset dictionary, and updates every entity node.
   - `Entity.BindTo` resolves display state, resets label/texture/state, and calls `QueueRedraw()`.
   - `Entity.MoveTo` kills and recreates a tween when position changes.
   - Risk: map entity rendering scales with total entity history rather than active visible actors.

6. Chronicle refresh scans accumulated provenance every frame.
   - Sources: `src/TheCanonryGame.App/Main.cs:344`, `src/TheCanonryGame.Game/Chronicle/GameRuntimeChronicle.cs:40`, `src/TheCanonryGame.Game/Chronicle/GameRuntimeChronicle.cs:49`.
   - `RefreshChronicleLabel` calls `Lines` every frame.
   - `Lines` sorts all provenance invocations by sequence before skipping already emitted chains.
   - The base chronicle is capped at 160 lines, but the provenance search space is not capped.
   - Risk: the visible chronicle is small while the scan cost grows with total generator/gambit history.

7. UI runtime always performs run logging.
   - Sources: `src/Engine.Host/KernelRuntimeFactory.cs:48`, `src/Engine.Host/KernelRuntimeFactory.cs:77`, `src/Engine.Observability/RunLogger.cs:33`, `src/Engine.Observability/RunLogger.cs:54`.
   - UI startup registers 4 projections: map, ui, provenance, and chronicle.
   - A separate `RunLogger` also drains the event log.
   - `KernelUiRuntime.Pump` drains registered consumers, drains the logger, then calls `RunLogger.OnTickEnded` for every tick advanced.
   - `RunLogger` writes with `AutoFlush = true`.
   - Risk: interactive play pays disk logging and an extra projection pass even when the user only needs visual state.

8. Open entity cards scan all relationships twice per refresh.
   - Source: `src/TheCanonryGame.UI/EntityCard.cs:223`.
   - Each open card scans all UI relationships once for outgoing and once for incoming, then sorts both result sets.
   - Risk: acceptable for 1 card and small graphs, but costly once relationships grow and multiple cards are open.

9. Tooltip hex picking is O(map) every frame while the map can be hovered.
   - Sources: `src/TheCanonryGame.UI/HexTooltipPanel.cs:63`, `src/TheCanonryGame.UI/HexMapRenderer.cs:65`.
   - `GetHexAtWorldPosition` loops over all current-plane coordinates and computes distance to each center.
   - It also materializes current-plane coordinate keys into an array.
   - Semantic tooltip formatting scans all active entities for occupants.
   - Risk: this will worsen with the intended larger map size even when simulation ticks are paused.

10. Shared `ViewSync` updates every node in every snapshot.
    - Sources: `src/TheCanonryGame.UI/ViewSync.cs:21`, `src/TheCanonryGame.UI.Core/ViewSyncCore.cs:28`.
    - `ViewSyncCore.Sync` allocates a `HashSet`, calls `update` for every snapshot item, and removes missing keys via a keys filter plus `ToArray`.
    - Godot `ViewSync` then checks and may reorder every ordered node.
    - Risk: the helper is useful for small dynamic lists, but it is too blunt for map cells, entity rows, and entity nodes on every tick.

11. Tableau and attribute graph are secondary but still tick-driven.
    - Sources: `src/TheCanonryGame.UI/TableauPanel.cs:65`, `src/TheCanonryGame.UI/AttributeGraphPanel.cs:91`.
    - Tableau sorts all cards and recomputes recent fire text each refresh.
    - Attribute graph sorts active entities and attributes when present.
    - Risk: these are not the largest offenders, but should become dirty/version driven after the main surfaces are fixed.

## Work Queue

1. Replace whole-UI tick refresh with projection/version invalidation.
   - Status: implemented through `GameRenderTree`; `Main._Process` no longer tick-refreshes every surface.
   - Entity browser invalidation is isolated behind `EntityBrowserVersion`; movement-only and relationship-only events do not rebuild the registry.
   - Map, entity layer, browser, cards, tableau, chronicle, and attribute graph should each expose or consume a relevant version/dirty set.
   - A tick pulse should not imply every surface changed.

2. Make map rendering delta-aware.
   - Status: partially implemented at component level; map cells and frame now have separate versions, but per-cell dirty deltas remain future work.
   - Separate static terrain/subdivision frame rendering from dynamic owner/influence/road rendering.
   - Redraw only dirty cells for cell changes, influence changes, and road-quality changes.
   - Recompute owner color tables only when owner/influence membership changes.

3. Do not display historical entities by default.
   - Status: implemented for entity browser and map entity layer.
   - Entity browser default: active entities only.
   - Entity map layer default: active entities only.
   - Entity cards and explicit history/memorialization features must still be able to query historical entities.
   - Relationship display should ignore inactive relationships and relationships with historical endpoints by default.

4. Remove or gate interactive run logging.
   - Status: implemented as opt-in run logging when a run log path is provided.
   - UI runtime should not create `RunLogger` unless explicitly requested.
   - If kept for debugging, buffer writes and avoid `AutoFlush` per tick.

5. Make chronicle incremental.
   - Do not scan and sort all provenance invocations on every frame.
   - Maintain a last-seen provenance sequence or invocation cursor.
   - Only update the `RichTextLabel` when the rendered chronicle text actually changes.

6. Index UI relationships for entity cards.
   - Maintain outgoing and incoming active relationship indexes in `UiProjectionState`.
   - Default card relationship sections should query active edges only.

7. Replace O(map) tooltip picking with hex-coordinate inversion.
   - Status: implemented for map hex picking.
   - Convert world position to axial/cube hex coordinate and validate with `TryGetHex`.
   - Avoid materializing all coordinate keys every frame.

8. Limit `ViewSync` use on hot paths.
   - Keep it for small panels.
   - Avoid it for map cells and large entity collections unless update can be skipped for unchanged data.

## First Implementation Candidates

1. Stop refreshing the static map frame every tick.
2. Make chronicle chain generation incremental.
3. Add dirty-cell refresh for map cells.
4. Design map render deltas/component render trees before implementing broader map invalidation.
