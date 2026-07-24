# Entity-stages scorecard, round 2 (stages-r2b-100k-20260724)

Point-in-time findings from the crown-gate round (commit `7d361eb`,
seeds 9100–9102 fanned one process per seed with mid-run streaming).
Seed 9100 was killed at tick 50k by design — density 0.993 and falling
throughput with nothing left to learn from a third saturated horizon.
Seeds 9101/9102 ran to 100k with full telemetry.

## How rounds are judged (standing criteria)

Trend shape over point targets. The knobs exist to remove divergences,
not to hit numbers:

- **Plateau, not level.** A density curve that flattens near the band
  passes even slightly above 0.5; the 0.3–0.5 band is provisional per
  the backlog and gets re-ratified against a healthy world, not vice
  versa.
- **Shape, not counts.** Stage pyramid = apex rare and earned; exact
  bucket ratios are seed noise.
- **Headline stays headline**: cultural mass trajectory non-regressing
  with the tier grind gone; mortals dying on their clocks; durable
  kinds converging; endings firing with attribution.
- **Seed variance is signal.** One hot seed above a plateauing family
  is not a to-do item.

## What the crown gates fixed

- **Pyramid shape (18): PASS.** Apex share of stage-tracked artifacts
  fell from 52% (round 1) to ~11% (35 legends, seed 9102); settlements
  ~13%. Promotion flow: 363 deepens, 35 crowns for artifacts — the last
  rung is genuinely steep. Deepen family at 4.2% of executions (19).
- **Mass headline (17): trend-PASS.** Cumulative physical raw mass
  +262k over 100k (seed 9102) with two negative windows both recovered.
  The ambient-emission lane ramps to ~1M/window, roughly half the
  gameplay lane — the mesh compensation package is carrying interior
  holding as designed. Confirm at healthy density in round 3.
- Endings, districts (146 installs, seed 9102), stacking (1.10/cell),
  relationship (14k) and cooldown (7.1k) headroom: all fine.

## What they could not fix

- **Density (1): FAIL, as predicted.** 9102 climbed all run
  (0.479→0.755 at horizon); 9101 plateaued high (0.851→0.831). Artifact
  net flow stayed positive every window (seed 9102 win 7: 122 created,
  63 retired). Promotion gates bound who reaches the apex, not how many
  artifacts exist: the base lifecycle band was Monumental (100k raw), a
  relic from before stages, so even trinkets barely died inside a run.
  Fixed post-run in `549fabe`: base band → Enduring, making the base
  band the trinket clock (≈6.75k raw) with stages stretching it —
  famed ≈22.5k, legend ≈225k — the settlement pattern.
- **the_offering_is_spent: still zero fires** even at devotion ≥0.4.
  Diagnosis: cadence generators log evaluations only when they fire;
  the rite attempted ~10×/run (cadence 2000) and the required
  instrument-beside-the-chosen-devoted-site alignment missed nearly
  all attempts (0.33 fires/run round 1). Devotion is plentiful (29k
  strength events ≥0.4, p90 0.69) and some instrument sits beside some
  POI on ~70% of ticks — the intersection with the selected site is
  what's rare. Fixed in `15aa1f8`: cadence 800 (~25 attempts/run). A
  trigger-kind swap was tried first and correctly rejected by the
  definition-graph validator (trigger kind must match target kind). If
  round 3 still shows zero, the next option is a radius variant of
  AdjacentToTarget — new query vocabulary, owner decision required.

## Scoring-infrastructure notes

- The parquet `symbols` table repeats rows per kernel run and has no
  run_id: all symbol joins must SELECT DISTINCT. Round-1 record was
  corrected for this.
- The stage-ledger reconstruction must count crown successes as
  promotions and seal-shatter fires as faction demotions; early round-2
  scoring missed both (fixed in the scorecard script).
- Fanned per-seed runs + 12.5k-tick core-table streaming worked: the
  round-2 pyramid shape was visible at tick 25k, and the kill decision
  for seed 9100 was made at 50k instead of after four hours.

## Watch items going into round 3 (stages-r3b-100k-20260724)

- Density plateau level with the Enduring artifact band; artifact
  created:retired converging toward parity post-bootstrap.
- Wild artifact share (still ~94% held at horizon; owner wants
  artifacts expressing their own will more often).
- Orcas near-extinct at horizon (4 active, seed 9102; same pattern in
  round 1) — predates this stack, needs its own diagnosis.
- Faction accumulation slow but monotonic (Generational band, 4 deaths
  per run) — candidate for the same base-band treatment if the trend
  holds.
- the_offering_is_spent nonzero.
