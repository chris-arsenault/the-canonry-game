# Culture Identity, Asymmetric Balance, And The Z Axis

Date: 2026-07-11 (branch `culture-identity-round`, baseline `4cf1596`)

Status: ACTIVE. Approved scope for this round: **Z-B (true sparse z-cells)
now** — skipping Z-A — plus the locked culture power metrics below. B3/B1/C
seed items remain scoped but queued behind Z-B and the ledgers.

## Redefining Balance

Balance is not equal landmass. Each culture should dominate its own axis of
power, contest the others', and never go extinct:

- **Aurora Stack — the Witnessed Life.** Power is visible presence on the
  ice: physical-plane culture mass, public settlements, records, height.
  Land (and height) dominance is their theology, not a bug.
- **Nightshelf — the economy of secrets.** Power without exposure: semantic
  mass, shadow-debt relationship leverage, artifact/ability custody, captured
  fire, depth. Smallest surface footprint by design; it should not matter.
- **Wake-Singers — you are your kills.** Power is predation: wars won,
  claims held over waters and points of interest, threat projection, ebbing
  and flowing claim-corridors rather than accumulated blobs.

Collapse is redefined as a culture losing its own scoreboard or going
demographically extinct — not landmass inequality.

## The Diagnosed Failure (evidence)

From `nexus-physics-2x25k-20260711` (run 25000, tick 25k): living entities
Aurora 76, Wake-Singers 37, **Nightshelf 5**; generator births Aurora 286,
Wake-Singers 113, Nightshelf 26; Nightshelf deaths (31) exceed its births.
Generators select targets in proportion to existing population and outputs
inherit the target's culture, so the initial demographic lead compounds. The
landmass runaway is downstream of this loop. Aurora also dominates the
semantic plane (712-924 trace cells vs Nightshelf's 12-65), so Nightshelf
currently has no axis at all.

Map degenerates (`map_degenerate=1` in run 25001) are accepted as
cull-and-retry cost, not a work item.

Horizon framing: these 25k-tick runs test the bootstrap-into-steady-state
phase only. Real play (SimCity mental model) extends 4x-50x, so low absolute
map coverage is expected; judge shape, trend, and steady-state stability, not
end-state totals. See "Game Horizon" in
`docs/systems/simulation-principles.md`. This framing is established before
the Wake-Singers round begins.

## Round 1 Scope (proposed)

- [x] **A1. Dominance ledgers — LOCKED v1 definitions.** Implemented:
  `CulturePowerConfig` on `DomainConfig` (custody kinds/relationships, claim
  and war relationships) declared by `IceRemembersDefinition`;
  `KernelHost.CulturePower.cs` computes the three axes host-side;
  `culture_power_snapshots` exported per snapshot cadence from the MC CLI;
  nations rows in `LegendPanel` show `witness/shadow/blood` per culture
  (refreshed every 100 ticks through `KernelUiRuntime.CulturePowers()`).
  Pre-rebalance ledger baseline (`zb-forging-ledgers-2x25k-r2-20260711`,
  avg over ticks >20k, run 25000): Witness Aurora 72.5 / Nightshelf 22.1 /
  Wake-Singers 5.4; Shadow Aurora 61.6 / Nightshelf 36.7; Blood Aurora 75.5 /
  Wake-Singers 0.0. The ledgers now *expose* the diagnosed imbalance —
  Aurora leads every axis and Wake-Singers have no Blood at all — which is
  exactly what the queued B3/B1/C1/C3 items must move.
  Each ledger is the
  culture's share (0-100%) of a world total; the balance target is that each
  culture leads its own axis in a healthy run. Components whose world total
  is zero are dropped from their mean. Computed host-side per snapshot
  cadence; exported as `culture_power_snapshots`; all three axes shown per
  culture in the nations rows.

  **Witness (Aurora's axis)** — share of world physical-plane raw culture
  mass at `z >= 0`, where each forged spire cell's mass is weighted
  `1 + 0.25 * z`. Tunnel mass (`z < 0`) is excluded: unwitnessed presence
  does not count.

  **Shadow (Nightshelf's axis)** — mean of three shares: (1) world
  semantic-plane raw culture mass; (2) world *hidden* physical mass at
  `z < 0`; (3) custody — living artifacts plus abilities belonging to the
  culture, each weighted by the summed strength of its `Holds`/`Anchored`/
  `PracticedBy`/`ConduitOf` edges.

  **Blood (Wake-Singers' axis)** — mean of two shares: (1) claims — summed
  strength of `ClaimedBy` relationships whose claimant (target endpoint)
  belongs to the culture; (2) war presence — summed strength of `AtWarWith`
  relationships with at least one endpoint in the culture. (Kill/predation
  attribution deferred until provenance-based cause-of-death is cheap.)

  Owning layers: `Engine.Host` (computation), `Engine.Observability`
  (export), `Engine.Projection` + `TheCanonryGame.UI` (nations rows).
- [ ] ~~B3. Demographic floor (inverse-population weighting).~~ REJECTED as
  a homeostat on the outcome variable — it would compensate away player
  disruption and make the cultures mechanically identical. Superseded by
  the Nightshelf package v2 below; rationale and deferred alternatives in
  `docs/design/culture-balance-options.md`.
- [x] **NS-A. Parasitic cross-cultural generators.** Aurora activity spawns
  Nightshelf entities: smuggling cells under thriving trade hubs, war
  profiteers, counter-archives, secrets from Aurora's dead. Aurora's growth
  becomes Nightshelf's food supply — theme-emergent negative feedback, no
  scoreboard controller. Requires produced entities to take an authored
  culture instead of inheriting the target's (small `Engine.Domain`
  config/compiler + kernel fire-path addition). Owning layers:
  `Engine.Domain`, `Engine.Kernel` (vocabulary), `IceRemembers` (content).
- [x] **NS-B. Deep Conclave timeline generator.** One cadence-based
  generator (every ~N ticks, jittered, world-state preconditions) outside
  the population-proportional pulse pool. Cadence, not scarcity-gating.
  Owning layers: `Engine.Domain`/`Engine.Kernel` (cadence vocabulary),
  `IceRemembers` (the Conclave).
- [x] **B1/N2. Per-culture plane bias.** `CultureConfig` gains
  physical/semantic influence-production multipliers compiled into the
  existing per-culture multiplier path. Aurora: physical up mildly.
  Nightshelf: semantic up, physical down. Wake-Singers: neutral. Owning
  layers: `Engine.Domain` config/compiler, `Engine.Kernel` multiplier
  lookup.
- [x] **C1/N3. Nightshelf seed custody parity.** Nightshelf gains entity
  parity through its own kinds (brokers/factions/artifacts/abilities with
  strong custody edges + semantic initial influence, not more settlements);
  Aurora keeps its settlement/physical start. Wake-Singers seed changes are
  out of scope for this round. Owning layer: `IceRemembers.Seed`.

  **Nightshelf round evidence (`ns-shadow-economy-2x25k-20260711`, seeds
  25000/25001, vs `zb-forging-ledgers-2x25k-r2-20260711`):** Nightshelf now
  leads Shadow 63.7/66.8% vs Aurora 35.2/30.0% (was 36.7 vs 61.6); Aurora
  still leads Witness 69.6/64.2%; Nightshelf living population 38 at tick 25k
  (was 12), Aurora 81 — asymmetric ~2:1, not forced equality. NS births 144
  (was 61) with the shadow economy as the top source (undermarkets 60
  entities, war profiteers 16, counter-archives 8, conclave 6 in run 25000;
  fires: 20/8/5/3). NS semantic mass now exceeds Aurora's (141k vs 108k at
  25k; was 24k vs 187k) via the 2.5x semantic bias; custody roughly at parity
  (12 vs 15). Nexus shape stays healthy for every culture (largest component
  63-79%). Note: `CadenceTicks` is in main ticks and scales with
  `MinorActorMainTickRatio` like pulse width, so the Conclave's 1200 spans
  ~6000 simulation ticks (3-10 convenings per run). Wake-Singers (3 living,
  Blood 0) remain the next round's subject, pending the Blood-metric rethink
  recorded in `docs/design/culture-balance-options.md`.
- [x] **Z-B. True sparse z-cells (see below).** Landed. Coordinate keys hash
  `(q, r, z)` with `z = 0` identical to the historical 2-arg hash; physical
  cells span `z in [-3, +3]`; non-surface cells exist only when forged by
  `ForgeCellVertical` (column extension, then creep to a known neighbor
  column). Authored gambits: `settlement_raises_the_spire` (Aurora,
  `spire_tier` up), `settlement_digs_the_gallery` / `faction_digs_the_gallery`
  (Nightshelf, `tunnel_gallery` down). Movement gains vertical wander;
  culture influence deposits at the actor's z; decay covers all z. Documented
  simplifications: boundary components, roads, and route planning stay
  surface-only; generators/seeds place at `z = 0`; forged mass feeds the
  ledgers directly. Evidence (`zb-forging-ledgers-2x25k-r2-20260711`, seeds
  25000/25001): 34/25 spire cells reaching `z = +3`, 14/12 tunnel cells
  reaching `z = -3`, forged-cell culture mass present at every occupied z,
  Nightshelf hidden mass feeding Shadow (avg 17 late-run), and final field
  shape comparable to the `nexus-physics-2x25k-20260711` baseline
  (components 5-24 vs 3-36, contested 3-32 vs 1-29 — cross-seed variance,
  no systematic regression). A telemetry defect found during evidence review
  (map cell projection deduplicated forged cells by `(plane, q, r)` and
  dropped z) was fixed in the same round.

Explicitly out of scope for round 1: generator pulse-budget stratification,
the war axis (B5), true z-cells (Z-B), semantic z (Z-C).

## The Z Axis

Goal (user direction): integer z in `[-3, +3]` on the map, where z locations
are **forged by actions** — Aurora raises spire tiers upward, Nightshelf digs
tunnels downward. Not an arbitrary 3D landscape. Also the honest form of
"three truly independent axes": keep hex `(q, r)` (with derived `s = -q-r`)
and add an independent forged `z`, on both planes eventually.

Lore fit is exceptional and pre-authored: Aurora Stack is canonically
vertical (Apex, Trade Terraces, Huddle Warrens, Foundation Depths — status is
literally elevation), Nightshelf is canonically subterranean (Shroud Depths,
Whisper Galleries, Void Markets, Glow-Fissure), and pressure-depth magic is
literally diving (orca depth is a natural later extension at sea cells).

Do **not** unshackle `s` from `q + r + s = 0`. That would turn the hex plane
into a cubic lattice and break hex adjacency, distance math, rendering, and
road assumptions everywhere. Independent-axis semantics come from `z`.

### Z-A: Forged tiers as cell state (round 1)

Each physical cell gains two small forged counters: `spireHeight (0..+3)`
and `tunnelDepth (0..-3)`, mutated only by new authored actions (gambit or
work-tier-style progression + minor actor labor): Aurora `Raise the Spire`,
Nightshelf `Dig the Gallery`. Tier effects:

- each spire tier raises influence capacity/production and slows decay for
  the tier owner's culture at that cell (height = witnessed permanence);
- each tunnel tier adds **hidden** culture mass (not shown on the surface
  overlay; counted in Nightshelf's ledger) and slows Nightshelf decay;
- tiers contribute to the dominance ledgers (Aurora counts height, Nightshelf
  counts depth);
- tier state lives in a plane-aware kernel layer keyed like cells and is
  deliberately shaped so Z-B can migrate `tierCount at (q, r)` into real
  cells at `(q, r, 1..n)` later.

No coordinate change, no movement change, no cell-event schema change (one
new primitive event for tier changes). Owning layers: `Engine.Kernel` (tier
layer + event), `Engine.Gameplay` (verb/gambit vocabulary), `IceRemembers`
(authored actions), UI badges/shading.

### Z-B: Sparse true z-cells (round 2, its own feature plan)

Coordinates become `(plane, q, r, z)` with `z = 0` preloaded and `z != 0`
cells existing only when forged. Vertical adjacency between existing cells at
the same `(q, r)`; tunnels connect horizontally at `z < 0` where dug —
Nightshelf gains genuinely hidden road networks that pass under contested
surface. Touches coordinate keys (easy — hashed), 8 coordinate-bearing kernel
event schemas + projection + exporter + snapshots (wide but mechanical),
movement/pathfinding, boundary components (3D connectivity), and placement.
Largest single feature since the low-altitude rewrite; requires its own
feature plan and migration of Z-A tier data.

### Z-C: Semantic z (round 3)

The same forged-z mechanism on the semantic plane: depth as esoteric strata
(deep secrets vs public record). Nightshelf semantic power concentrates at
negative z, Aurora public record at positive z. Together with Z-B this
delivers the three-independent-axes goal on both planes. Orca sea-depth
(forged nothing; dived) also lands here.

## Later Rounds

- **B5. The war axis** — raid gambits that transfer culture mass and claims,
  war outcomes that move `ClaimedBy`, threat auras that suppress enemy
  influence production near raids. Makes Wake-Singers matter to the other
  two cultures' stories. Own authored round with its own MC evaluation.
- **B2/B4 residue** — per-culture decay profiles and terrain affinity are
  largely subsumed by Z-A tier modifiers; revisit only if the ledgers show a
  gap after round 1.

## Verification Plan

`make ci` plus an MC evidence run per landed scope item, compared against
`nexus-physics-2x25k-20260711` on the A1 ledgers: expect Nightshelf living
entities and births to stabilize, Nightshelf semantic share to lead its
ledger, Aurora physical share to lead its ledger without extinguishing the
others, and forged tier counts to appear concentrated at each culture's
nexus.
