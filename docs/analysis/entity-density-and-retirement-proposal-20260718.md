# Entity Demography: Territory-Proportional Population, Density, and Retirement

Point-in-time analysis, 2026-07-18, revised same day after design review.
This supersedes the first draft's causal framing and continues
`entity-population-growth-20260718.md`. No fix is implemented here except the
generator atomic-fire bug fix, which landed separately (commit `ec4d9e5`).

## Problem Statement

The problem is not that population grows — a growing world should grow its
population. The problem is that population growth is disconnected from map
growth, in both directions and on both planes:

- Anchor kinds (artifact, settlement, region, POI, canon) grow linearly
  forever, producing physical-plane pile-ups and a semantic-plane dot-field.
- Mortal kinds (penguin, orca, transport, occurrence) sit at fixed
  equilibria while territory expands, so the live world *thins*.
- The population mix therefore inverts over horizon: the live, moving share
  shrinks while the static-anchor share climbs (artifacts alone reach 46% of
  the active population by 100k ticks).

The visible symptom is UI clutter, but the cause is demographic: no term in
the population dynamics reads territory.

## The Population Model

Active population per kind follows `N* ≈ creation rate × mean lifespan`.
Both factors are currently world-size-independent constants:

**Creation is a fixed global pipe.** Pulse-driven generators share a budget of
`DefaultFiresPerPulse = 1` per `DefaultPulseWidthTicks = 25`
(`KernelCodes.cs:589`), and cadence generators run on fixed timers. The
budget binds hard: in the current 15k run, generator evaluations show 4,098
`EligibleNotSelected` against 256 `Fired` — the world offers roughly 16×
more eligible creation than the pipe admits. Growing the map grows the
target pool and the eligible set, not the throughput.

**Lifespans are fixed per band, and two bands never end.**

- Mortal kinds leak in proportion to their population, so a fixed inflow
  pins them at a fixed `N*` regardless of territory. Named penguins held at
  ~48 for 100k ticks while covered territory grew 2.3× — live density per
  covered cell fell by more than half.
- `WorldAnchor` kinds (artifact, region, POI) have no drain by design, and
  `Monumental` settlements never die in practice because map-support health
  effects fully offset their drain. Zero genuine retirements of any
  artifact, settlement, region, or POI occurred in 100k ticks. Gambits
  retired 22 entities total (15 monsters, 7 orcas) — a negligible sink.

Fixed faucet into a leaking bucket = pinned equilibrium. Fixed faucet into a
sealed bucket = linear accumulation. Neither sees the map. That single
structure explains every observed curve.

## Two Planes, Two Clutter Geometries

The density complaint applies to both planes, and they fail differently.

**Physical plane: piles.** Stacking worsens monotonically — avg entities per
occupied hex 1.43 → 1.75 over the 100k run, hexes with 6+ entities 1 → 23,
max stack 14. The worst hexes are artifact hoards (9–11 artifacts plus a
penguin or monster). Four authored/mechanical loops produce this:

1. **Memorials drop where the fallen stood.** `war_takes_its_toll` places
   each relic at the casualty's hex (`Required("$memorial", K.Artifact,
   "relic", "$casualty", …)`). Long-lived wars re-fire on the same fronts,
   so the same conflict zones accrete relics indefinitely.
2. **Artifacts advertise their own location.** Every artifact emits a map
   signal (`baseMass 10, support 0.4, capacity 0.3, attractorRadius 2` in
   `ArtifactEntity.cs`). Piles project support and culture mass and attract
   activity, reinforcing the local field that permits further placement —
   densification is self-reinforcing through the signal layer.
3. **Claims never resolve into carrying.** The memorial is authored
   `ClaimedBy` its faction at 0.6, but a claim is only an edge; no authored
   content moves a claimed artifact to its claimant's hold, though the
   engine op (`MoveTargetToSelfPhysical`) exists. 334 of 389 active
   artifacts at 100k sit in the wild where a generator dropped them.
4. **Founding is gated into the interior.** Interior/route outputs require
   own-culture-supported cells (`PlacementCellHasOwnCultureSupport`), and
   all authored physical placement is `On`/`Near` an existing entity.
   Freshly thawed corridor cells have no culture mass, so the gate forbids
   founding exactly where the map just grew. The kernel's `Frontier`
   placement mode works on both planes, but the authoring DSL only wires
   `frontier: true` to **semantic** placement
   (`IceRemembersGeneratorDefinition.cs:924`) — physical-plane frontier
   placement is currently unreachable from authored content.

**Semantic plane: spray.** Stacking is minimal (max 2–4 per hex — semantic
placement requires free cells and drift spreads entities). The clutter is
raw count: 841 active entities occupied 761 hexes of a 4,921-cell idea
plane (~15%) in the 100k run, in which the semantic plane never grew. The
current build grows the semantic plane via idea-mass thaw (1,027 → 1,401
cells over 15k), but linear population growth outruns it. Physical-only
remedies (carrying, physical frontier) do not touch this plane; only
population-level remedies do.

## Registered Evidence

Runs: `relationship-history-lifetime-100k-20260713` (seed 7900, structural
long-horizon evidence from an earlier worktree),
`knowledge-real-grants-baseline`/`-postrepair` (seed 4200, 15k, matched
pair), `atomic-fire-fix-15k-20260718` (seed 4200, 15k, current build).

Retirement counts from pre-2026-07-18 runs must be filtered
`retire_tick > create_tick`: failed generator fires used to leave phantom
entities marked historical in the same tick. That defect is fixed (atomic
fires, commit `ec4d9e5`, verified zero rollbacks in
`atomic-fire-fix-15k-20260718`), but it pollutes all older telemetry.
Filtered 100k picture:

| Kind | Created (real) | Real retirements | Retiring source |
|---|---|---|---|
| artifact | 389 | **0** | none |
| settlement | 110 | **0** | none |
| region | 78 | **0** | none |
| point_of_interest | 29 | **0** | none |
| canon | 21 | 1 | upkeep |
| faction | 70 | 21 | upkeep |
| monster | 68 | 29 | gambits 15, upkeep 14 |
| ability | 63 | 30 | upkeep |
| penguin | 344 | 296 | upkeep 173, generators 123 |
| occurrence | 136 | 113 | upkeep |
| orca | 57 | 46 | upkeep, generators, gambits 7 |
| transport | 90 | 79 | upkeep |

Map vs population, 100k run: cells +8.6%, covered cells +132%, active
entities +1,809%. Creation flat at ~165–195 per 10k window throughout; no
runaway generator loop — the dynamics are steady faucet, missing leak,
territory-blind pipe.

## Target Shape (proposed, to ratify)

**Territory-proportional demography:** every kind's population plateaus in
proportion to a territory measure, with authored per-kind densities.
Judged at long horizon (the game is ~2.5M ticks; bootstrap may run hot):

- **Physical density:** active entities per covered cell holds in a band
  (~0.3–0.5) instead of climbing (0.10 → 0.82 observed). Hexes with 6+
  entities are rare and story-justified (a reliquary hoard is a named
  place, not an accident).
- **Semantic occupancy:** occupied fraction of the idea plane holds in a
  band as the plane grows, instead of monotone thickening.
- **Mix:** the live share (mortal kinds) grows with territory rather than
  being diluted — named-penguin density per covered cell should hold
  roughly constant, not halve.
- **Anchor stock:** artifact/POI counts plateau (retirement ≈ creation);
  settlements track covered cells (~1 per 10 covered cells matches today).

## Proposed Knobs

Ordered by what each controls: count (both planes), then location.

### K1. Territory-scaled creation — the pipe reads the map

Scale generator throughput with a territory measure (covered cells,
settlement count, or culture mass) so a bigger world creates
proportionally more of everything, with authored per-kind shares. The
budget is demonstrably the binding constraint (16× eligible surplus), so
scaling it is the direct lever. This is a kernel-tuning change
(`KernelProgramTuningOptions`), arguably tuning rather than semantics, but
it still needs the authored-layer-first check: verify whether per-settlement
cadence generators could achieve the same scaling through the existing
budget before touching it. Without K1, retirement alone produces a
plateaued-but-thinning world.

### K2. Universal leak — zeitgeist lifecycle plus authored endings

The only lever that reduces count on **both planes at once**, since
active→historical removes an entity from the physical map, the idea plane,
targeting pools, and signal emission in one transition, while the graph
keeps it (which is exactly the "passed out of the zeitgeist" fiction, and
what a later comeback mechanic needs).

- Reclassify `Artifact` and `PointOfInterest` out of `WorldAnchor` into
  mortal bands, case by case per subtype; a small authored subset
  (canon-grade relics, seals while their accord holds) may remain anchors.
  **Regions stay `WorldAnchor`** (design invariant).
- Fix the settlement accounting so Monumental drain is not fully cancelled
  by support effects — thriving settlements should persist, immortality
  should not be the default outcome.
- Author endings with fiction: relics spent in Wake-Singer rites (the
  offering-spend economy already exists), shattered in raids, consolidated
  (several battlefield relics transform into one memorial canon); weakened
  settlements razed by war as a gambit outcome; canons superseded within a
  jurisdiction. The vocabulary (`SetTargetHistoricalMutation`,
  `SetBindingHistoricalEffect`) exists and is currently pointed almost
  exclusively at penguins, monsters, and orcas.

### K3. Resolve claims into carrying — the physical-pile lever

Claimed/consigned artifacts get brought to the claimant's hold: a gambit
("the faction enshrines its memorial") or generator consequence using the
existing carry op. This de-litters battle sites without retiring anything,
concentrates artifacts into settlement hoards whose co-location the UI
already collapses, gives named bearers a rare heroic retrieval beat, and
makes an eventual reliquary/meta-entity presentation natural. Physical
plane only — it does not reduce count.

### K4. Spread creation onto the growing map

- **Physical:** expose frontier placement to authored physical placement —
  the DSL's `frontier:` flag currently reaches only the semantic plane.
  The produceds already marked `frontier: true` (colonies, reefholds,
  rookeries, harbors, peaks, shoals, vents) are the natural candidates:
  their fiction is already "reaching new ground." Pair with a
  corridor-aware exception to the culture-support gate (or trace-mass
  seeding along thaw corridors) so founding can follow the growing map.
- **Semantic:** the mechanism exists (idea-mass frontier, semantic
  `frontier: true`); the question is pacing — whether plane growth plus K2
  retirement holds occupancy in band. Measure before changing.

### K5. Crowding as the closing loop

The per-cell crowding signal exists on both planes
(`CrowdingLoadAt(planeCode, …)`) and is wired into exactly one settlement
trait today. Extend it as a relevance drain so any residual stacking is
self-limiting. Backstop, not primary.

## What Stays Untouched

- Regions remain `WorldAnchor`; no region lifecycle drain.
- Historical retention: nothing is deleted from the graph.
- Causal limits stay causal limits, not population caps.
- No global active-entity cap, no hidden ledgers or reservoirs, no
  UI-only visibility cap presented as a simulation fix.
- Mortal-kind lifespans and churn mechanics are correct as mechanisms;
  what changes is that their creation inflow scales (K1), not how they die.

## Matched Monte Carlo Comparison Plan

Control and candidate from fresh builds, unique registered run ids, 3
seeds, 100k ticks (plus a 15k pair for bootstrap shape). All runs on the
atomic-fire build, so no rollback filtering. Metrics per 10k window and at
25k/50k/100k:

- creation, retirement, net by kind — anchors converge to net ≈ 0 after
  bootstrap; mortal net tracks territory growth instead of zero;
- physical: active per covered cell (flat after bootstrap), stacking
  distribution (avg, ≥6 share, max);
- semantic: plane cell count, occupied fraction (flat), stacking max;
- mix: mortal share of active population (non-decreasing as map grows);
- wild vs held artifacts (K3), founding events on corridor/frontier cells
  (K4);
- unchanged-behavior guards: war/beat cadence and narrative-quality
  metrics — retirement and scaling must not gut the war-saga loops;
  `narrative_population_equilibrium` flag.

Record material DuckDB findings via `append-stats` on the registered runs.

## Follow-ups Noted, Not Required Now

- **Return from the zeitgeist:** generators/gambits that target historical
  artifacts or POIs and resurface them (a relic rediscovered, a shrine
  reconsecrated) — doubles as creation-pressure relief when resurfacing
  substitutes for minting.
- **Dynamic meta-entities** (explicitly not a requirement): settlement +
  resident faction + leader + held artifacts presenting as one game/UI
  entity. K3 makes the grouping physically real first; evaluate K1–K5
  before paying for this.

## Open Questions

1. Which territory measure drives K1 (covered cells, settlements, culture
   mass), and can authored per-settlement cadences achieve it without a
   kernel budget change?
2. Subtype-level anchor classification for K2: which artifacts/POIs are
   true anchors, and what band lengths fit the fiction?
3. Settlement drain rebalance (K2): target a survival curve, not
   immortality — what fraction of settlements should outlive their
   founding culture's era?
4. Semantic-plane growth pacing (K4): is idea-mass thaw + retirement
   sufficient, or does plane growth also need a territory link?
