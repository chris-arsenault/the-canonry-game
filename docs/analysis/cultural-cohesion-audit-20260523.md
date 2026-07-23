# Cultural Cohesion Audit Tracking

Date: 2026-05-23

Baseline commit before this tracker: `9cac2d5 Document culture boundary and minor actor systems`.

This file is intentionally diagnostic. It records basic cause/effect analysis, open investigation items, and requested candidate fixes.

## 1. Cultural Targeting In Gambits And Generators - Complete

Status: complete as of 2026-05-23. The apparent "foreign deepening" concern was explained by plane separation and by legitimate cross-culture/cross-kind generator inheritance cases, not by `Deepen the Holding` directly mutating another culture.

Concern: some actions appear to let entities deepen or reinforce the holding of another culture, or appear to operate without enough physical locality.

### Current Code Facts

- Work tier gambits are generated for every entity kind as `Deepen the Holding`.
- `Deepen the Holding` uses `SelfCultureMassPrecondition(NonMobilePrimaryPlane(entity.PrimaryPlane), raw >= 12, connected >= 90)`.
- The kernel implementation of `SelfCultureMassPrecondition` reads the actor's own culture at the actor's coordinate on the configured plane. It does not read target culture because the target is self.
- For physical-primary entities such as penguins, orcas, artifacts, transports, and monsters, the checked plane is semantic.
- For semantic-primary entities such as settlements, factions, regions, POIs, abilities, and occurrences, the checked plane is physical.
- The work-tier effect increases minor actor tier, which increases worker operating radius and worker cap.
- Claim gambits for faction/POI, orca/POI, and transport/region require the target to be `The Ice` and the actor to be non-Ice.
- Several relationship gambits intentionally allow cross-culture relationships, sometimes with only a same-culture score bonus rather than a same-culture hard gate.
- Several generators inherit produced entity culture from a target or secondary binding. A produced penguin can therefore inherit orca or monster culture when the generator's narrative says it is a defender/hunter of that context.

### Basic Analysis

The `Deepen the Holding` precondition is not directly deepening another culture. It is keyed to the actor's own culture. The more plausible cause of confusing UI behavior is plane separation: a physical actor can deepen based only on semantic culture support, with no direct requirement that the actor's physical cell be inside the same culture. This can look like a penguin standing in another culture while increasing its own culture's worker throughput.

There is also a separate generator-targeting risk. `UnclaimedPlacesDrawWardens` targets any POI without outgoing `ClaimedBy` and binds an unconstrained faction as `$patron`. It does not require the POI to be `The Ice` or require `$patron` to match the target culture. If a non-Ice POI lacks the relationship lock, this generator can attach an arbitrary factional culture to it through generated warden/scout topology.

The generator system's placement culture score only affects output placement after a target has already been selected. It does not make culturally supported target entities more likely to be selected by the generator.

### Tracking Questions

- [x] Confirm whether any live MC examples of "foreign deepening" are `Deepen the Holding` on a non-mobile plane rather than a wrong-culture mutation.
- [x] Audit all generators with unconstrained secondary bindings, especially those that produce physical actors from a culture ref different from the target's current culture.
- [x] Confirm whether `UnclaimedPlacesDrawWardens` can target non-Ice POIs after recent claim/placement changes.
- [x] Confirm whether generator-produced penguins of orca/monster culture are intended narrative exceptions or are contributing to misleading culture visuals.

## 2. Culture Reaches 700-1000 Too Frequently - Complete

Status: complete as of 2026-05-23. The immediate tuning pass added `KernelTuning.Programs.CultureInfluenceActionScale`, applied it to low-altitude culture actions and minor actor culture expansion/cleanup, and set the default to `0.15`.

Concern: the cap was raised to 1000, but the UI still frequently shows cells above 700 and many cells reach full culture.

### Current Code Facts

- Raw culture bands are:
  - trace: 5
  - claimed: 30
  - controlled: 100
  - entrenched: 300
  - nexus: 700
  - full: 1000
- Raw culture is clamped at 1000.
- Dominance share is recalculated from raw culture totals at the cell.
- Culture worker deltas are currently roughly 1.5-2.8 raw per action by source kind.
- Road workers can run `Extend Route Culture` at `1.6 * 4.5 = 7.2` raw before slot/source multipliers.
- Culture workers have `ActionCadencePulses = 1`, so an active worker acts every minor actor tick.
- Worker TTL is commonly 75-90 ticks.
- Emit cadence is commonly 65-100 ticks, with work tier and slot status increasing active worker capacity and/or effective throughput.
- Low-altitude culture verbs still add raw culture in the 2.5-5.5 range on their own cadence.

### Basic Analysis

The cap changed, but the production scale still appears close to the old 0-100 world. A single worker with delta around 2 acting every tick for 80 ticks can contribute around 160 raw culture over its life. A route worker can contribute substantially more. With multiple sources, slot multipliers, and work tiers, 700 raw is not rare; it is a normal result of sustained worker action.

This is probably not a clamp bug. The numbers imply that the current source rates are large enough to make "nexus" and "full" cells common.

The `culture-scale-015-trace-1x5000-20260523` run showed the new default materially reduces cap pressure:

- physical p90 raw: 379.33
- physical p99 raw: 938.07
- physical nexus/full entries: 46 / 7
- semantic p90 raw: 307.45
- semantic p99 raw: 644.29
- semantic nexus/full entries: 1 / 0

For comparison, the previous scale 1.0 baseline had p90 raw at 1000 on both planes. Scale 0.25 still produced 130 physical nexus cells per run and 30 physical full cells per run; scale 0.1 made nexus cells very rare. Scale 0.15 is the current compromise point.

### Tracking Questions

- [x] Measure culture raw additions by source type: minor actor, low-altitude verb, generator effect, seed/startup.
- [x] Measure how the action scale changes final 700/full cell counts across 0.25, 0.1, and 0.15.
- [x] Compare high raw cells by source so the cause is not inferred from aggregate culture maps.

## 3. Culture Boundaries Disappear In The UI - Complete

Status: complete as of 2026-05-23. The incremental boundary refresh now uses kernel-owned boundary dirty cells instead of projection dirty cells.

Concern: Nightshelf and Aurora main masses sometimes disappear from the UI. It is unclear whether this is UI rendering, projection refresh, or kernel boundary calculation.

### Current Code Facts

- Kernel boundary cells are derived from raw culture plus local neighborhood and component data.
- Components include cells with raw mass >= 30.
- A cell is contested when own raw >= 5, foreign raw >= 30, and own share is less than 35 points above the strongest foreign share.
- The UI map projection first receives `CellCultureInfluenceChanged` events that update culture share only.
- `KernelUiRuntime.RefreshCultureBoundariesIfChanged()` then refreshes derived boundary projections if the kernel boundary revision changed.
- Incremental boundary projection refresh uses dirty map cells and a radius-2 neighborhood.
- The frame layer draws culture areas from `cell.CultureInfluences[0]`, using raw mass when boundary projection has filled it in; otherwise it falls back to share.
- The frame layer cache is keyed by `CultureOverlayVersion`.
- `HexMapProjection.OnCellCultureInfluenceChanged()` updates only the exact changed cell with share-only culture data, then marks that cell dirty.
- `KernelHost.RefreshCultureBoundaries(... incremental: true)` uses the map's dirty cell set as its input, expands each dirty cell by radius 2, and writes boundary-enriched culture data only for that local area.
- The map renderer clears dirty cells after refreshing the visible plane.
- `HexMapFrameLayer.BuildCultureAreaGeometry()` draws only the dominant culture per cell. Non-dominant culture mass can exist in the projection and tooltip data but not receive a filled area.

### Basic Analysis

If tooltip/projection data still has culture while the drawn boundary disappears, the most likely area is projection or rendering rather than the raw kernel culture field. The projection path has two different representations in sequence: event-level share-only culture, then boundary-enriched culture. If an incremental refresh misses cells whose derived component state changed, the UI can draw stale or incomplete area geometry even though the kernel field is still present.

The radius-2 incremental refresh is also smaller than the potential impact of component reclassification. A raw change in one cell can alter component membership, largest-component flags, attached component mass, and cell kind beyond the changed cell's immediate radius.

The `culture-scale-015-trace-1x5000-20260523` MC run supports this. Kernel boundary telemetry still shows Aurora and Nightshelf masses at tick 5000:

- physical Aurora: 140 cells, 2 components, largest 77.9%
- physical Nightshelf: 244 cells, 1 component, largest 46.7%
- semantic Aurora: 147 cells, 3 components, largest 30.6%
- semantic Nightshelf: 223 cells, 8 components, largest 35.4%

That makes raw kernel disappearance unlikely. The more likely bug is that the UI projection is stale or incomplete after incremental refresh. The current invalidation source is map dirty cells, but boundary-derived facts are component-wide. When a cell change joins, splits, contests, or reclassifies a component, cells outside radius 2 can need refreshed boundary fields even though their own raw culture did not change.

The renderer can also make a valid culture look absent: culture areas are filled only for `CultureInfluences[0]`. A culture with substantial non-dominant raw mass becomes invisible as an area. In the latest trace this is not the main cause for physical Aurora/Nightshelf, because most of their raw cells are still dominant, but it is still a real UI semantics problem for contested fronts.

### Implemented Fix

Culture mutations now mark the changed culture/plane/cell directly in the kernel boundary program. The dirty entry records whether the cell may have crossed the claimed support threshold. Incremental UI refresh drains this kernel-owned dirty buffer:

- normal raw/share changes refresh the changed cell's radius-2 dependency area
- support-threshold changes refresh the local dependency area plus the affected current component(s) and their radius-1 attachment neighborhood
- the host no longer derives boundary refresh input from generic projection dirty cells
- regression tests cover component split, component merge, and non-topology local refresh behavior

This fixes the actual incremental algorithm without restoring the previous full-refresh workaround.

### Remaining Optional Follow-Ups

1. Add a debug consistency check that compares incremental projection to a full boundary projection.
   This can run on demand or every N UI pulses in debug mode. It should report cells where `CultureInfluences`, cell kind, strength band, component id, or component mass differ after an incremental refresh.

2. Change the culture overlay semantics so non-dominant but meaningful culture does not vanish visually.
   Options include drawing runner-up contested hatching, outlining non-dominant controlled/entrenched raw mass, or adding a selected-culture overlay mode that draws that culture wherever it has raw mass above a threshold regardless of dominance.

3. Add UI diagnostics for a clicked cell: dominant culture, all raw culture masses, boundary cell kind, component id, and whether the current displayed culture data came from boundary refresh or share-only event projection.
   This would make the next disappearance report immediately classifiable as kernel, projection, or renderer behavior.

### Tracking Questions

- [x] Compare the projection refresh algorithm against raw kernel boundary dependencies.
- [x] Determine whether disappearing masses still exist in `CultureFieldComponentSnapshots`.
- [x] Determine whether the previous generic projection dirty-cell algorithm could miss component-wide boundary changes.
- [x] Check whether disappearing cultures could be present in `CultureInfluences` but sorted behind a different dominant culture.

## 4. Generator Placement And Cultural Coalescence

Concern: cultural masses may not encourage generators to spawn near them. Each generator, especially those with physical/static outputs, needs specific review.

### Current Code Facts

- `Required(...)` and `Optional(...)` produced entities default to physical `Near` and semantic `Near` placement around `PlacementRef`.
- `Near` means radius 0-5.
- `Across` means physical radius 20+ with unbounded max.
- `HarborAcross(...)` is the main custom physical `Across` placement helper.
- `ScarRegion(...)` and `DepthsRegion(...)` create physical coverage patches near the target.
- Produced entities default to `PreferInheritedCulture`.
- Non-hostile placement rejects cells where foreign raw culture is >= 300.
- Culture placement score gives:
  - dominant own culture: 980 plus connected bonus up to 240
  - present own culture: 620 plus connected bonus up to 240
  - supported adjacency: 260 plus connected bonus up to 240
  - foreign dominant penalty: -350
- Culture scoring happens inside the candidate area around `PlacementRef`. It does not select the generator target.
- Generator target eligibility is primarily graph/relationship/attribute/absence topology.

### Basic Analysis

Cultural mass currently influences placement, but only after the generator and target have already been selected. If a generator target is outside a main cultural component, its outputs are still searched near that target. Culture scoring can choose the best nearby supported cell, but it cannot pull the whole event toward a distant cultural center.

This means generator placement can reinforce existing hubs only when eligible target entities already exist in or near those hubs. It does not by itself make hubs more eligible for future generator activity.

### Generator-Specific Notes

| Generator | Produced entities | Placement/culture notes | Basic cause/effect concern |
| --- | --- | --- | --- |
| `ice_settles_accounts` | seal, witness, optional exile | Near target faction; culture inherited from target. Offered penguin is same culture as target. | Likely culturally coherent; output locality follows the faction, not a field center. |
| `what_the_spring_remembers` | rite, witness, optional sanctuary, optional choir | Target region; patron faction is same culture as target; outputs inherit target/patron. | Coherent when the region is already culturally anchored; can create static settlement/faction near a region target rather than the strongest mass. |
| `war_breaks_the_land` | scar region, optional weapon, optional compact | Target faction; scar coverage is created near target and inherits target culture. | Static region output follows war faction location; can create new regional mass away from existing culture if the faction is away from it. |
| `routes_draw_readers` | broker, harbor, optional order, optional instrument | Harbor uses `Across` from settlement and inherits target settlement culture; order/instrument are near harbor. | High satellite risk: one far harbor can start a same-culture static cluster away from the source mass. |
| `what_the_artifact_knows` | rookery, seal, optional cell | Target penguin with held artifact; rookery/cell inherit penguin culture. | Mobile target can found a static cluster wherever the penguin happens to be. |
| `unclaimed_places_draw_wardens` | warden, scout | Target POI without `ClaimedBy`; patron faction is unconstrained; outputs inherit patron culture. | Strong targeting risk: arbitrary faction culture can attach to any unclaimed POI if target culture is not gated elsewhere. |
| `practiced_forms_find_tomes` | student, tome | Target ability; outputs inherit ability culture. | Mostly semantic-culture coherent; physical locality depends on ability placement. |
| `charters_find_harbors` | harbor, broker, optional instrument | Target transport; outputs inherit transport culture and place near transport/harbor. | Mobile target can seed static harbor away from an existing mass. |
| `vigils_leave_relics` | relic, witness | Target occurrence with home base; outputs inherit occurrence culture. | Occurrence locality can drift; generator follows occurrence rather than culture field. |
| `corruption_wakes_monsters` | monster, scout, optional witness | Target scar region; outputs inherit scar culture. | Coherent around scar; static/mobile mix can amplify a scar satellite. |
| `wars_get_names` | war occurrence, witness, optional weapon | Target faction; outputs inherit faction culture. | Coherent around faction location; not field-driven. |
| `trade_routes_convene_negotiation` | trade conference, broker, accord, optional charter | Target settlement; patron faction same culture as target; produced culture from target. | Coherent around settlement if settlement is in the hub; otherwise reinforces a route endpoint. |
| `claimed_waters_draw_bulls` | bull | Target POI with strong `ClaimedBy`; caller orca same culture as target. | Coherent if claim topology is coherent; output follows claimed POI. |
| `pod_currents_launch_scouts` | orca scout, skiff, shoal | Target orca with home base; outputs inherit orca culture and place near orca. | Mobile orca can generate POI/transport satellites away from durable mass. |
| `orca_route_wars_get_names` | war, defender penguin, convoy, optional escort | Target orca at war; all outputs inherit orca culture. | Produces penguin/transport of orca culture; can look like penguins reinforcing another culture. |
| `conduits_map_hidden_peaks` | peak POI | Target transport; peak inherits transport culture. | Mobile transport can create static POI away from center. |
| `mapped_peaks_birth_techniques` | technique, manual | Target peak; outputs inherit peak/technique culture. | Coherent if peak placement was coherent; otherwise preserves the satellite. |
| `controlled_depths_wake_leviathans` | leviathan, plague | Target bloom/shoal controlled by someone; outputs inherit target culture. | Coherent with the controlled point; can intensify off-center controlled POIs. |
| `overclaimed_blooms_draw_beasts` | beast | Target bloom/shoal with strong claim; beast inherits target culture. | Coherent with claimed point; amplifies off-center POI if claim is off-center. |
| `corrupted_monsters_raise_revenants` | revenant | Target monster; revenant inherits monster culture. | Monster-centered; likely outside main culture model and can preserve monster-culture artifacts. |
| `stolen_artifacts_draw_hunters` | hunter penguin | Target monster with held artifact; hunter inherits monster culture. | Produces penguin of monster culture; can be visually confusing and may create non-cultural penguin labor if monster culture is active. |
| `monster_slayings_become_festivals` | festival, rite, convoy | Target penguin; outputs inherit penguin culture. | Mobile penguin can create semantic/transport outputs away from a durable mass. |
| `haunted_transports_spread_plague` | wraith, plague | Target transport; outputs inherit transport culture. | Mobile transport can seed off-center hostile/event topology. |
| `war_occurrences_launch_scouts` | scout transport | Target war occurrence; scout inherits occurrence culture. | Occurrence-centered; can extend an off-center war locus. |
| `trade_conferences_raise_citadels` | citadel transport | Target trade conference; citadel inherits conference culture. | Conference-centered; follows event location rather than culture field. |
| `conferences_become_festivals` | festival | Target conference, host settlement same culture as target; placement near host, culture from target. | Coherent if host selection is coherent; placement follows host settlement rather than strongest mass. |
| `cleansed_springs_found_sanctuaries` | sanctuary, choir, rite | Target region; patron faction same culture as target; sanctuary/choir/rite inherit patron/target. | Coherent around cleansed region; can create a static civic cluster wherever the region is. |
| `sanctuaries_anchor_colonies` | colony, order, charter | Target sanctuary; outputs inherit target culture. | Strong hub-growth path when sanctuary is in mass; also preserves any off-center sanctuary. |
| `conduit_points_found_reefholds` | reefhold, instrument, broker | Target conduit POI; port settlement same culture as target; outputs inherit port culture. | Coherent by culture, but POI target can be physically away from mass; creates static reefhold cluster there. |
| `deep_conduits_name_depths` | depths region, seal | Target conduit POI with founded settlement; patron faction same culture as target. | Static region coverage follows conduit POI, not necessarily main mass. |
| `warded_monsters_found_orders` | order faction, battleship | Target monster with ward; outputs inherit monster culture. | Produces faction/transport of monster culture; likely confusing if monster culture is active or inherited from a major culture. |

### Tracking Questions

- [ ] For each generator fire, compare target cell culture band/component to produced entity placement cell culture band/component.
- [ ] Count produced static entities born inside the largest same-culture component versus outside it.
- [ ] Count produced mobile entities born outside same-culture support and later emitting workers there.
- [ ] Identify generators whose secondary culture ref differs from target culture or has no target-culture match.
- [ ] Identify generator outputs that produce cross-kind/cross-culture visuals, especially penguins or factions with orca/monster culture.

## 5. Occurrences, POIs, And Regions Only Select Deepen The Holding

Concern: in `gambit-deepen-selection-5x5000-20260523`, occurrences, points of interest, and regions selected `Deepen the Holding` for 100% of their gambit attempts. This may mean those entity kinds lack enough direct gambit verbs, or their other available roles only appear as targets of other actors and generators.

### Current Code Facts

- `CreateWorkTierGambits()` creates `Deepen the Holding` for every entity kind, including occurrence, point of interest, and region.
- The current gambit catalog uses occurrence, POI, and region frequently as targets, but much less often as actors.
- In the sampled MC:
  - occurrence: 116 selected gambits, 116 were `occurrence_deepens_the_holding`
  - point_of_interest: 83 selected gambits, 83 were `point_of_interest_deepens_the_holding`
  - region: 53 selected gambits, 53 were `region_deepens_the_holding`
- This is different from factions, transports, monsters, or settlements, where `Deepen the Holding` competes with several other selected actor gambits.

### Basic Analysis

This result does not by itself prove `Deepen the Holding` is crowding out richer behavior for these kinds. It may instead show that occurrences, POIs, and regions currently have few or no authored actor-side gambits that can become eligible. If these entity kinds are intended to be mostly passive loci, then the result is expected. If they are supposed to act as cultural/narrative engines, then the current selected-gambit distribution is a gap.

The relevant distinction is actor-side affordance, not target-side importance. POIs and regions can still matter through generators and through other entities acting on them, but their own gambit agency appears effectively limited to work-tier progression in this run.

### Tracking Questions

- [ ] Enumerate occurrence, POI, and region gambits where they are the actor, not the target.
- [ ] For each such gambit, check whether it was absent from the catalog, never eligible, or eligible but not selected.
- [ ] Compare selected actor-side gambits against generator usage for these same entity kinds to separate "passive locus" behavior from missing agency.
- [ ] Decide whether occurrences, POIs, and regions are intended to have direct cultural actions or only be acted upon by mobile/social entities.

## 6. UI Entity Browser And Chronicle Refactor

Concern: the entity browser tree has become difficult to scan. It should return to the pre-tree browsing model with filtering and sorting. The right-hand chronicle panel also needs filter and sort controls so it can be used diagnostically during long runs.

### Current Code Facts

- The entity browser UI lives in `src/TheCanonryGame.UI/EntityBrowser.cs`.
- The current browser organizes entities through a hierarchy of realms, places, lives, orphan places, orphan lives, and untiered entities.
- The chronicle UI lives in `src/TheCanonryGame.UI/ChroniclePanel.cs`.
- Chronicle data is exposed through `IGameChronicleFeed` and `GameRuntimeChronicle`, then rendered by `ChronicleRender` in `src/TheCanonryGame.App/GameRenderTree.cs`.
- The projection already tracks entity browser invalidation through `UiProjectionState.EntityBrowserVersion`.

### Basic Analysis

The tree shape is useful for hierarchy review, but it is a poor default for debugging large simulations because it makes broad questions harder: what changed recently, which culture/kind dominates, which entities are alive, which entities are slotted, which entities are unhealthy, and which entities are currently producing activity. Filtering and sorting are better primitives for this workflow.

The chronicle has a similar problem. It is currently a chronological stream, but long runs need narrowing by entity, culture, kind, producer type, event type, success state, and time window. Without those controls, the right panel is noisy exactly when the simulation becomes interesting.

### Tracking Questions

- [ ] Identify the pre-tree entity browser behavior and recover its useful filter/sort affordances.
- [ ] Decide whether hierarchy should remain as an optional view or be removed from the default browser path.
- [ ] Add entity browser filters for culture, kind, active/dead state, slot occupancy, worker tier, and text search.
- [ ] Add entity browser sorts for name, kind, culture, health, age, worker tier, and recent activity.
- [ ] Add chronicle filters for culture, entity, kind, producer type, event type, success state, and tick range.
- [ ] Add chronicle sorts for newest/oldest first and optionally grouped-by-entity or grouped-by-producer.
- [ ] Make sure browser and chronicle controls stay usable during high-speed UI runs without forcing full rebuilds every frame.
