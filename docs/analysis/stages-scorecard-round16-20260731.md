# Round 16 — the 60/20/20 counterweight arc (2026-07-31)

Owner target: at most 60/20/20 culture imbalance. This round chased it
through a smoke chain on seed 9101 (r16–r16e) and two 3-seed fans
(r16f, r16g on 9100/9102/9103). Runs under
`.artifacts/monte-carlo/stages-r16*`.

## What landed (the keep-package)

- **Desperation gate 4x -> 3x.** A 3:1 leader:minority count ratio is the
  60/20/20 boundary; the valve now opens at the stated tolerance.
- **Per-culture devotion rites.** Devotion was multi-culture at the
  worship end (offering gambits) and 91% Wake-Singer at the blessing
  end — every rite that consumed offerings minted Wake-Singer entities.
  `the_sky_renders_judgment` (Aurora) and `the_dead_keep_their_secrets`
  (Nightshelf) flipped devotion births to 107/82/52 on the first smoke.
- **Culture placement plumbing.** `RequiredAurora`/`RequiredNightshelf`
  hardcoded HostileIntrusion — culture-placement scoring was disabled
  for every product of both cultures. Now a parameter.
- **Nightshelf physical viability.** At 0.75x physical production, a
  base-stage holding emitted 0.19/run against the 0.2 decay floor: a
  lone Nightshelf cell was net negative *everywhere*. Not a tuning
  shortfall — an arithmetic exclusion from the physical plane. Raised
  to 1.0; semantic identity retained (see temper below).
- **Desperation valve defect fixed.** The surges had never fired in any
  round (gates never opened before), and when round 16f finally opened
  them the fires failed 100% on placement (rejection code 8): Near
  placement around arbitrary targets found no water for orcas, no valid
  cell for courts. The backstop did not work the first time it was
  needed. Fix: surge anchors place at the culture's own **frontier**
  (map-wide search, near-origin fallback), and the people place at the
  anchor. Verified in r16g: Nightshelf surge 25–35 fires, ~0 failures.
  Aurora's beacon still fails on worlds where Aurora is already
  collapsed (113/48 failures on 9102/9103) — open defect.
- **Nightshelf semantic temper 2.5 -> 1.8.** The 2.5 multiplier was
  balanced for a ~60-entity culture; once the count engines were fixed
  it pinned the semantic plane at 0.84–0.92 (r16f).
- **Scorecard**: 17b per-plane mass, 17d living entities by culture,
  22c desperation valve status. Telemetry note:
  `generator_target_evaluations.generator_id` joins symbols at
  symbol_kind=12 (generator_events uses 4); rejection 7=binding,
  8=placement.

## Reverted levers (adverse single draws — cautionary, not refuted)

Methodology note (owner correction, 2026-08-01): the simulation is
chaotic — any code change perturbs comparison order once and the whole
trajectory diverges, so same-seed reruns under changed configs are
independent draws, not controlled comparisons. The single-smoke
"refutations" originally recorded here are downgraded accordingly.

- **Goldilocks placement scores** (Present 780 > Dominant 560): the one
  draw came in at 0.91 top-share with the leader's mass ballooning.
  Reverted on that adverse draw plus the mechanism argument (expansion
  preference multiplies with placement-event count, which the leader
  has the most of). Not distribution-verified.
- **Doubled proportional decay** (0.004 -> 0.008): the one draw pinned
  0.96 with world population halved. Reverted; the mechanism argument
  (the floor governs small holdings, proportional decay only bites
  large ones, so global pressure starves minorities first) stands on
  the arithmetic, but the outcome claim is one sample.

## Where this leaves the target

Mechanical facts that survive the methodology correction: physical mass
is an integral of emission history (0.4%/run proportional decay), so
early advantage compounds; entity-count and semantic-mass scoreboards
moved to ~target under the keep-package while physical top-shares
across all round-16 draws ranged 0.45–0.98. That spread is over MIXED
configurations and says nothing about the final config's distribution —
which has only 3 draws (r16g: 0.90/0.94/0.95 physical horizon).

Open question requiring distribution data (6+ draws of the fixed final
config): does the current package's physical top-share distribution
have meaningful weight at or below ~0.6, or is it concentrated in
hegemonic outcomes? If hegemonic, the standing hypothesis is that
threshold valves rescue counts but exert no pressure on mass, and a
*continuous restoring force* scaling with dominance is needed —
in-cadence and causal, e.g. **density-dependent wilds pressure**
(predation targeting where mass is thickest; the wilds hunt where prey
is densest). `overclaimed_blooms_draw_beasts` already points partway
there.

## Watches carried forward

- Aurora beacon placement failures on collapsed-Aurora worlds.
- 9102 population thins at horizon under some configurations
  (r16f: 227 actives, density 0.37).
- The offering-driven rites compete for the same charged sites; weight
  interplay between the three cultures' rites is untuned.
