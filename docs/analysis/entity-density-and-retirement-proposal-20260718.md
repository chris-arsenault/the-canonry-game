# Entity Density and Retirement: Findings and Scoped Proposal

Point-in-time analysis, 2026-07-18. This continues
`entity-population-growth-20260718.md` with the reframed problem statement:
unbounded growth is not the defect by itself. The defect is that entity growth
does not track map growth, so the world densifies — entities stack many to a
hex and the UI reads as clutter. This document reports the investigation and
proposes knobs. No code was changed.

## Reframed Problem Statement

- Absolute population growth is acceptable when the map grows with it.
- The failure is density: active entities per usable cell rises continuously,
  and per-hex stacking gets worse, not better, with horizon.
- Primary consumer of the fix is legibility (UI clutter); the simulation-side
  levers are creation placement, retirement, and carrying capacity. A separate
  agent owns UI rendering performance; nothing here touches UI code.

## Evidence

All figures from registered runs `relationship-history-lifetime-100k-20260713`
(seed 7900, 100k ticks; structural evidence from an earlier worktree) and
`knowledge-real-grants-postrepair` (seed 4200, 15k ticks; current balance).
Queries were run read-only against the runs' `telemetry.duckdb`.

### Correction: rollback retirements polluted earlier counts

Generator fires that fail after creating entities mark those entities
historical in the same tick
(`GeneratorProgramRuntime.Fire.cs` → `MarkCreatedEntitiesHistorical`). These
rollbacks are indistinguishable from real retirements in
`entity_events`/`lifecycle_events` unless filtered by `retire_tick >
create_tick`. The prior analysis doc's by-kind retirement counts include them.
Filtered, the 100k picture is:

| Kind | Created (real) | Real retirements | Net | Retiring source |
|---|---|---|---|---|
| artifact | 389 | **0** | +389 | none |
| settlement | 110 | **0** | +110 | none |
| region | 78 | **0** | +78 | none |
| point_of_interest | 29 | **0** | +29 | none |
| canon | 21 | 1 | +20 | upkeep |
| faction | 70 | 21 | +49 | upkeep |
| monster | 68 | 29 | +39 | gambits 15, upkeep 14 |
| ability | 63 | 30 | +33 | upkeep 30 |
| penguin | 344 | 296 | +48 | upkeep 173, generators 123 |
| occurrence | 136 | 113 | +23 | upkeep |
| orca | 57 | 46 | +11 | upkeep, generators, gambits 7 |
| transport | 90 | 79 | +11 | upkeep |

Two structural facts follow:

1. **No artifact, settlement, region, or POI has ever been genuinely retired.**
   Every accumulating kind has literally zero working retirement paths in
   practice.
2. **Gambits retired 22 entities in 100,000 ticks** (15 monsters, 7 orcas).
   The gambit layer is currently a negligible sink.

The mortal kinds (penguin, occurrence, orca, transport) are in genuine
equilibrium — churn works where drain works.

### Why the accumulating kinds never retire

- `artifact`, `region`, `point_of_interest`: `WorldAnchor` band — no lifecycle
  drain by design (`IceRemembersEntityDefinition.cs:344-360`).
- `settlement`, `canon`: `Monumental` band (20k-tick nominal lifespan), but
  positive health effects (`MapSupportAtLocation` and friends) outweigh the
  drain, so settlements run 5x past nominal lifespan with no deaths. In
  practice they are anchors too.
- The authored retirement vocabulary exists but is narrow: 5
  `SetBindingHistoricalEffect` uses and 3 `SetTargetHistoricalMutation` uses in
  the whole domain, all targeting penguins, monsters, orcas, or wardens. No
  authored act retires an artifact, settlement, POI, or canon.

### Density vs map growth (100k run)

| Tick | Map cells | Covered cells | Active entities | Active/covered | Occupied hexes | Avg/occupied hex | Max hex | Hexes ≥6 |
|---|---|---|---|---|---|---|---|---|
| 0 | 4,921 | 440 | 44 | 0.10 | — | — | — | — |
| 15,000 | ~4,940 | ~480 | 193 | 0.40 | 135 | 1.43 | 7 | 1 |
| 50,000 | 5,099 | 693 | 509 | 0.73 | 315 | 1.62 | 11 | 6 |
| 100,000 | 5,342 | 1,021 | 841 | 0.82 | 481 | 1.75 | 14 | 23 |

Map cells grew 8.6% and covered cells 132% while active entities grew 19x.
Density rises monotonically on every measure; there is no equilibrium in sight.
The current-balance 15k run shows the same shape early (202 active on 139
occupied hexes of 525 covered, max stack 6, avg 1.45).

### The stacks are artifact hoards

The worst hexes at 100k are piles of 6–11 artifacts plus one or two live
entities (e.g. `artifact:11, penguin:2, monster:1`). 389 active artifacts sit
on 170 hexes; only 55 of 389 are co-located with a settlement or POI. The
rest lie where a generator dropped them — battlefield memorials, relics,
records — and never move again. Artifacts alone are 46% of the active
population at 100k.

### Creation side is steady, not exploding

Total creation is flat (~165–195 per 10k window across the whole run);
retirement rises slowly but never converges, leaving net +46 to +105 per 10k
window. Per-generator fire rates are mostly flat; a few grow mildly via
reopen loops (`the_accord_holds` 0→12 per 25k quarter, `the_current_provides`
4→11, `conduit_points_found_reefholds` 2→8). Artifact permanence is
catalog-wide, not one bad generator: `war_takes_its_toll` 62/62 surviving,
`wars_get_names` 51/51, `the_accord_holds` 28/28, `rich_hubs_grow_undermarkets`
24/24, and so on. The dominant math is simply steady creation times zero
retirement.

## Proposed Target Shape (to ratify, not yet an invariant)

Express targets as density, not absolute counts, so a growing map buys a
growing population:

- **Active entities per covered cell:** plateau in a 0.3–0.5 band at long
  horizon (currently 0.82 at 100k and still rising).
- **Stacking:** average ≤ ~1.5 per occupied hex; hexes with 6+ entities rare
  and story-justified (a hoard site should be a named place, not an accident).
- **By kind:** artifact and POI active stock plateaus (retirement ≈ creation
  beyond bootstrap); settlement count tracks covered-cell growth (roughly one
  settlement per 8–12 covered cells matches today's 110 per 1,021); mortal
  kinds keep their current equilibria untouched.
- Bootstrap (first ~25k ticks) may legitimately run above band while
  Generational/Monumental stock fills in; judge the trend, not the total.

## Proposed Knobs (3–4, complementary, each proportionate)

### K1. Zeitgeist lifecycle for artifacts and POIs (authored bands)

Reclassify `Artifact` and `PointOfInterest` from `WorldAnchor` to mortal bands
(candidates: Generational for ordinary relics, Monumental for named-war
memorials). Fiction: health (`condition`, `presence`) is relevance, not
existence — reaching zero means passing out of the zeitgeist, and the object
persists as history in the graph exactly as historical entities already do.
Case-by-case subtype classification; a small authored subset (canon-grade
relics, e.g. accord seals while their accord holds) may stay `WorldAnchor`.
**Regions stay WorldAnchor** (tech invariants: strongholds, static entities).
Owner: `src/IceRemembers/Entities/ArtifactEntity.cs`,
`PointOfInterestEntity.cs`, band constants in
`IceRemembersEntityDefinition.cs`.

### K2. Authored retirement acts (gambits + generator consequences)

Give the world ordinary, narratively meaningful acts that consume, destroy, or
transform the accumulating kinds, and tune the existing hunt-gambit pattern up
from its current 22-per-100k contribution:

- artifacts: spent in rites (Wake-Singers offerings), shattered in raids,
  claimed and carried off by named actors, consolidated — several battlefield
  relics transform into one memorial canon;
- settlements: fall to war/predation when health is already low (razing as a
  gambit outcome, not a new global rule); the Monumental drain already exists —
  the fix is partly that support effects should not fully cancel it;
- canons: superseded by newer canons of the same jurisdiction.

Owner: `IceRemembersGambits*.cs`, `IceRemembersGeneratorDefinition*.cs`,
`IceRemembersRelationshipStrengths.cs`. The vocabulary
(`SetTargetHistoricalMutation`, `SetBindingHistoricalEffect`) already exists;
this is authoring, not engine work.

### K3. Carrying capacity via the existing crowding signal

`MapSignals` already computes per-cell crowding load (occupancy vs capacity,
`MapSignalsProgramRuntime.cs:244`) and the authored effect condition
`MapCrowdingLoad` already exists — it is currently wired into exactly one
settlement negative trait. Extend it as a health drain for more kinds so
overcrowded hexes shed relevance faster, making density self-limiting where
K1 gives kinds a drain to modulate. Owner: authored effect/trait definitions
in `src/IceRemembers/Entities/*.cs`; `Engine.Gameplay` only if the condition
vocabulary needs a new shape.

### K4. Occupancy-aware creation placement (spread instead of stack)

Bias produced-entity placement and generator target scoring away from crowded
cells and toward covered-but-empty and newly reclaimed cells, tying creation
to the growing map. This is the knob most likely to need engine evidence
first: placement scoring lives in
`Engine.Kernel/Programs/Generators/GeneratorProgramRuntime.TargetScoring.cs`
and placement resolution in the same runtime — kernel changes require
demonstrating the authored layer cannot express the rule. Start by checking
whether placement rules can already prefer low-occupancy cells; only escalate
with that evidence in hand.

## What Stays Untouched

- Regions remain `WorldAnchor`; no region lifecycle drain.
- Historical retention: nothing is deleted from the graph.
- Causal limits stay causal limits, not population caps.
- No global active-entity cap, no blanket generator slowdown, no UI-only
  visibility cap presented as a simulation fix.
- Mortal-kind equilibria (penguin, orca, transport, occurrence) are working;
  leave their tuning alone.

## Matched Monte Carlo Comparison Plan

- Control and candidate from fresh builds, unique registered run ids, 3 seeds
  each, 100k ticks (plus one 15k pair for bootstrap-shape regression).
- Metrics, all rollback-filtered (`retire_tick > create_tick`):
  - real creation/retirement/net per kind per 10k window — convergence of net
    toward 0 for artifact/POI after bootstrap;
  - active per covered cell at 25k/50k/100k — flat or falling after 25k;
  - stacking: avg per occupied hex, share of hexes ≥6, max stack;
  - unchanged-behavior guards: mortal-kind equilibria, generator fire rates,
    war/beat cadence (`narrative_population_equilibrium` flag plus existing
    narrative-quality metrics — retirement must not gut the war-saga loops).
- Record material DuckDB findings via `append-stats` on the registered runs.

## Follow-ups Noted, Not Required Now

- **Return from the zeitgeist:** a generator/gambit path that targets
  *historical* artifacts or POIs and resurfaces them as active again (a relic
  rediscovered, a shrine reconsecrated). Doubles as a creation-pressure
  reducer: resurfacing can substitute for creating a new entity in loops like
  `records_cast_shadows`.
- **Dynamic meta-entities** (idea only, explicitly not a requirement): a
  settlement + resident faction + leader + held artifacts presenting as one
  game/UI entity. Held artifacts already co-locate crudely. This is a large,
  cross-cutting feature; K1–K4 should be evaluated first since they may
  remove most of the clutter that motivates it.
- **Observability:** ~~emit a distinct reason/source for rollback retirements~~
  Resolved 2026-07-18 on this branch: the generator executor now plans the
  whole produced bundle before committing any creation, so failed fires write
  nothing to the graph and rollback retirements no longer exist (see
  `docs/systems/16-generator-executor.md`, atomic-fire section). The
  `retire_tick > create_tick` filter is still required when querying runs
  recorded before the fix, including every run cited in this document.
