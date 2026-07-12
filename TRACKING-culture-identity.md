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

## Wake Round (Wake-Singers devotion economy) — IMPLEMENTED, UNSTABLE

Implemented per approved design (commit `45953f0`): Wake ledger v2 (claims +
war + offering flow attributed to the receiving site's culture), `offered_to`/
`consigned_to` relationship kinds with maintenance drift, the Corpse Current
seeded as a living sacred site with a live grievance, four fill channels
(kills sung, elder descents, relic consignment, vigils), two spend loops
(the_current_provides calving pods, pilgrims_found_new_currents), and
low-altitude offering-renewal verbs on both sides.

**Evidence (`wake-devotion-2x25k-20260712` seeds 25000/25001,
`wake-devotion-2x25k-b-20260712` seeds 26000/26001): NOT STABLE.** Held for
consultation per instruction — no tuning applied.

- The devotion loop underfires. Offering edges equilibrate at ~0.55-0.7
  under maintenance decay + verb renewal, below the 0.8 Provides gate:
  the_current_provides fired 0-4 times per run; elder descents fired zero
  times across all four runs; vigils 1-3 (repeat offerings from the same
  singer appear blocked by duplicate-edge creation).
- Wake-Singer survival, where it happened, came from the *war* loops ignited
  by the seeded grievance, not from devotion (boom-ebb 10-46-19).
- The seeded grievance destabilizes the whole bootstrap: wars metastasize
  (1 seeded war grows to 13 active by 25k), Aurora's growth loops never
  start (17 births in run 26000 vs ~320-380 in healthy prior runs), and
  Aurora collapses to 1-4 living in 3 of 4 runs.
- With Aurora gone, Nightshelf's undermarkets chain off their *own* trade
  hubs (undermarket becomes a hub, hosts the next undermarket): 395 NS
  births, 117-138 living, Shadow 94-97% — a latent self-parasitism runaway
  from the Nightshelf round that Aurora's collapse exposed.

Candidate fixes identified but NOT applied (user decision pending): remove
or weaken the seeded grievance; lower the Provides gate toward the observed
edge equilibrium or raise renewal; allow offering edges to re-strengthen on
repeat fires (ReplaceExistingOutgoing); one-generation undermarket chains
(target WithoutOutgoing founded_from).

## Predation Round (tags + predation + escalation) — IMPLEMENTED

Approved redesign after the Wake round instability (commits `6608bc4`,
`57b6624`, `e61410d` + threshold tuning). Engine: tag/status system v1
(catalog, per-entity bits, threshold effect rules with hysteresis, generator
tag targeting/effects, entity_tag_events telemetry). Content: `preys_on`
(agency->spatial) as the ambient conflict substrate acting on the unnamed
masses; named victims only via the rare a_hero_falls branch; escalation
generators (glory_to_the_dead_god, counter_offensive) and the Accord
Convenes cadence; devotion tuning (Provides 0.5, pilgrims 0.75, offering
edges re-strengthen, second elder); undermarkets can no longer parent
undermarkets.

**Evidence (`predation-escalation-2x25k-r2/b-r2/r3-20260712`, 4 seeds +
tuned rerun):**

- The world is multi-polar for the first time: final populations across
  seeds — Aurora {40, 13, 66, 22}, Nightshelf {24, 32, 38, 29}, Wake-Singers
  {54, 77, 22, 86}. Different cultures lead different runs; the NS runaway
  is fixed (undermarket lock); the war-contagion Aurora collapse is fixed
  (no seeded war).
- Devotion economy functions: provides/relics/vigils/descents/pods all fire;
  Wake led by Wake-Singers in 3 of 4 seeds; each culture leads its own axis
  in the healthiest world (26000: Aurora witness 55.6, NS shadow 64.1).
- Tags function mechanically: fervent cycles on the WS faction, bereaved
  sets on hero-fall, terrified sets/clears after tuning (0.6/0.3).
- Remaining gaps, deliberately left for design review rather than more
  tuning: (1) escalation generators never fired — their coincidence
  requirements (terrified+bereaved on one settlement; fervent + predation
  >= 0.7 concurrently) exceed what the bootstrap horizon produces, while
  wars (2-11 active) still arise from the pre-existing rivalry/monster
  paths; (2) Aurora remains fragile on some seeds (13-22); (3) Wake-Singer
  amplitude runs high (54-107) — the devotion+predation engine may need a
  damper or the amplitude may be acceptable ebb-and-flow at this horizon.

## Escalation Round (terror ladder + prey response + kind participation)

Correction round after user review: state machines are discrete (relationship
strength remains the only numeric). Raids set `hunted`; the_hunt_settles_in
steps hunted->terrified while predation persists; the_fear_fades steps back
down. Prey-side response is culture-generic: bereaved shores raise memorials
(martyrs_are_witnessed), terrified shores raise wardens that damp the hunt.
Kind participation widened: monsters stalk settlements, pods hunt convoys
(`preys_on` targets spatial+asset), all through the same ladder.

**Evidence (`terror-ladder-2x25k[-b][-r2]-20260712`, 6 runs):** the whole
chain functions — hunted/terrified set AND clear on every seed, wardens and
martyrs rise, the Accord convenes, and counter_offensive produced its first
named escalation war. After one predator-prey rebalance (warden damp -0.06,
monster weight 3, provides restored to 10/-0.30), seed 25001 settles the
first true tri-polar world: populations 32/62/39 with Aurora leading Witness
44, Nightshelf Shadow 60, Wake-Singers Wake 35.5.

**Open dynamic, needs design direction:** the predator-prey system is
seed-bistable. Roughly half the seeds settle multi-polar; the other half tip
into an Aurora-dominant basin where the monster-driven warden response
drains Wake-Singer predation faster than devotion can renew it, and
Wake-Singers slide to 1-8 living (Aurora 76-88). glory_to_the_dead_god has
still never fired (fervent + hot predation rarely coincide even at 0.6).
Candidate directions, not taken unilaterally: separate the warden response
so it damps only the edges of the culture that caused the terror; give
Wake-Singers the fervor-field bandwidth (deferred design); or accept
bistability at bootstrap horizon and map the basins with a wider seed sweep.

## Ability Participation (dread-songs and warding forms)

Abilities are strategic actors in the conflict loop (user direction:
abilities are a core concept and should curb the wardens).
the_dread_song_breaks_the_watch: a Wake-Singer rite turned against a named
warden — hero-versus-hero, the warden falls, the kill is sung to the deep.
wardens_take_up_the_old_forms: surviving wardens practice same-culture
abilities (custody feed, craft lineage). Evidence
(`dread-song-2x25k-20260712`): both fire (2 dread-songs, 5 warding forms);
mechanism verified. Regions deliberately left thin (user: not concerned).

**Balance state after this round:** every authored loop demonstrably fires,
but the ecosystem equilibrium remains trajectory-sensitive with a strong
Aurora-dominant attractor: WS finals across recent runs {2, 10, 4, 39, 1,
21} — the dread-song at weight 3 does not flip basins, and 2-seed batches
are too noisy to tune against. Next decision is structural, not numeric:
wide seed sweep (10+ seeds) to map attractor basins before further tuning,
and/or the deferred fervor-field round for Wake-Singer loop bandwidth.

## Basin Sweep (10 seeds, one build)

`basin-sweep-{a..e}-2x25k-20260712`, seeds 25000-25009, current build.
Final populations (Aurora/NS/WS): 88/31/2, 65/30/10, 7/34/69, 67/31/5,
78/31/5, 6/89/6, 73/22/5, 87/20/2, 2/27/79, 3/91/6.

**Finding: winner-take-all tri-stability with survival floors.** Three
attractor basins — Aurora (6/10 seeds), Wake-Singers (2/10), Nightshelf
(2/10) — and in every basin exactly one culture dominates while the other
two are suppressed to a floor (2-10 living; no culture ever reaches zero —
the cadence timelines and parasitic/devotion floors hold). The earlier
"tri-polar" observation was a transient, not an attractor. Nightshelf never
drops below 20 (parasitism feeds at any host scale). In WS basins the
corridor becomes landmass (WS leads Witness 63-67), so axis identity
partially collapses under dominance.

Diagnosis: each culture's self-reinforcing growth loop is stronger than all
cross-culture negative feedback, so early advantage compounds to saturation.
Coexistence requires a leader-scaling brake — this is precisely the evidence
the deferred local carrying capacity (Option D) was reserved for — or an
explicit design acceptance that one culture rises per world and the player
is the balancing force. Decision pending.

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
