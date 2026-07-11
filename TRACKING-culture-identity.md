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

## Round 1 Scope (proposed)

- [ ] **A1. Dominance ledgers — LOCKED v1 definitions.** Each ledger is the
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
- [ ] **B3. Demographic floor.** Generator target scoring gains a weight
  inversely proportional to the target culture's living-population share,
  plus one authored revival loop per culture (e.g. `The Unspoken Convene`
  emerges from the Deep Warrens when Nightshelf withers). Owning layers:
  `Engine.Kernel` generator target scoring (small), `IceRemembers` authored
  generators.
- [ ] **B1. Per-culture plane bias.** `CultureConfig` gains
  physical/semantic influence-production multipliers compiled into the
  existing per-culture multiplier path. Aurora: physical up. Nightshelf:
  semantic up, physical down. Wake-Singers: neutral. Owning layers:
  `Engine.Domain` config/compiler, `Engine.Kernel` multiplier lookup.
- [ ] **C1/C3. Seed rebalance.** Nightshelf gains entity parity through its
  own kinds (brokers/factions/artifacts/abilities + semantic initial
  influence, not more settlements); Aurora keeps its settlement/physical
  start; Wake-Singers seed with claims and one live grievance so the raid
  economy boots. Owning layer: `IceRemembers.Seed`.
- [ ] **Z-B. True sparse z-cells (see below).** Approved to land now,
  skipping Z-A. Physical plane only this round; semantic z remains Z-C.

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
