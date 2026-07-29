# Entity-stages scorecard, rounds 5–6 (stages-r5/r6-20k-20260729)

The culture-pluralism round. Round 4 flagged near-monoculture on two of
three seeds as its top finding; the owner set it as the priority. This
round diagnoses the causes, lands a three-layer fix in the kernel, and
verifies with a full 3×20k fan. Runs kept at
`.artifacts/monte-carlo/stages-r5-20k-20260729-s9*`; smokes under
`*-smoke-*`. Scorecard gained section 17c (top-culture mass share per
plane per window) — culture concentration is now a standing criterion.

## Diagnosis: why the world went monoculture

Same-seed comparison against the pre-perf r2b runs showed the
concentration *tendency* pre-existed (Wake-Singers led both rounds) but
the tick-unification's minor-actor redesign (`be5c4d47`) amplified it
decisively: at equivalent world-time, seed 9101 went 0.60→0.89 top
share, 9102 0.51→0.72, both converging to 0.92–1.00 at horizon where
r2b held 0.54–0.76. Three stacked causes, all traced to the retirement
of the worker treadmill:

1. **Erosion without projection.** The new cultural fronts and campaign
   columns scrub foreign mass via `ReduceOtherCultureSourcesAtCell`
   with no requirement of local backing — scrub volume scales with
   attacker entity count (60:1 at peak), violating the ratified contest
   model (moving fronts need attacker mass projection, not erosion).
2. **Stage-gated survival.** Interior holding moved to per-stage
   ambient emission, but the `MinimumDecayPerRun` floor (0.6/run) sat
   above a base-stage town's emission (0.05/tick × 5 = 0.25/run). A
   lone town could not hold its own tile; cities and capitals could.
   Minority cultures are mostly base-stage, so their interiors
   evaporated while the leader's self-sustained.
3. **Age tides multiplied the floor.** World-beat global decay tides
   reach 4×/8× in harsh ages and multiplied the whole decay term
   including the floor — effective floor 1.6/run, above city emission.
   Each age extincted every sub-capital holding and ratcheted
   concentration toward whoever had the most entities to recover with.

## The fix stack (kernel, all landed)

- `ContestProjectionShare` (new tuning, 0.01): every foreign-mass
  reduction is capped at `share × reducer's connected mass at the
  cell` — applied at the single choke point all reducers flow through
  (verbs, fronts, campaigns). Real fronts are unaffected; deep
  unsupported raids damp toward zero.
- `MinimumDecayPerRun` 0.6 → 0.2: below town emission, so survival is
  no longer stage-gated; unsustained trails still cull in bounded time.
- `DecayUnits` restructured: the global tide scales only the
  proportional decay term; the contest multiplier still amplifies the
  floor (front sharpening preserved). Ages now pressure held mass
  without extincting every sub-apex tile.

Kernel tests pass (329/329, including serial-epoch invariance).
Throughput unchanged (~10–45 t/s across a run).

## Round-5 verdict

- **Birth pluralism restored.** Seed 9101 windows 1–3 births:
  c1 32/46/20, c2 55/61/52 (round-4 baseline had collapsed to
  11/24/3) — closely matching the r2b shape.
- **Seed 9102: cured.** Physical top share wanders 0.51→0.37→0.60 with
  the lead changing hands twice (Wake-Singers → Aurora Stack →
  Nightshelf). Semantic settles Nightshelf-led at 0.86 — its design
  plane (authored physical 0.75× / semantic 2.5×). Judge pluralism per
  plane; Nightshelf's physical marginality is intended.
- **Seed 9101: still concentrates** (1.00 physical at horizon) — and is
  already at 0.82 by tick 2500 in every post-fix run. Its die is cast
  in bootstrap: near-balanced starts (9/18/14 entities) amplify to
  3.6:1 in births by tick 1000 through the open generator race — fires
  are mass-score-weighted, so a modest early edge compounds
  superlinearly. Seed 9100 traded places (balanced in round 4,
  concentrated to 0.86 late this round), confirming concentration
  remains an attractor rather than a determinism.
- **Density improved as a side effect**: horizon 0.450/0.500/0.584
  (round 4: 0.512/0.614/0.624) — the family now brackets the 0.3–0.5
  band's top edge. Coverage grew ~25% (gentler floors hold more
  ground). Mass headline non-regressing on 9100/9101; 9102 ends a weak
  window (68k, watch). Offering-spent: 1 fire on 9100, zero on
  9101/9102 — rare-event variance after round 4's 5 fires, watch only.

**The remaining amplifier is the generator/creation-budget layer** —
fires won by mass-weighted scoring with no diminishing returns. That
layer is explicitly reserved in `docs/backlog.md` ("per-kind creation
shares as per-band fire weights… the owner has ideas on the broader
budget design; do not implement without them"), so this loop stops at
the boundary: field mechanics fixed, budget design left to its owner.
A per-culture diminishing-returns term on generator target scoring is
the natural shape if/when that design opens.

## Settlement crown inversion: independent, and retuned

Round 4's inverted settlement pyramid (57% apex) did NOT resolve with
pluralism — the cured seed 9102 was also inverted (59% apex). Measured
cause: the crown local-mass gate (12 × 8 = 96) sits 5× below the p25
of settlement local mass (542/1553/5317 at p25/p50/p90); the gate did
no work, leaving apex share to cooldown pacing × 25% success ≈ the
observed 55%. Canon is the exception that proves it: its low-mass
semantic niche keeps the base gate binding, and its pyramid was always
healthy.

Fix: per-kind local-mass gate scale in `WorkTierGateScale`
(IceRemembersGambits.Stages.cs), anchored to measured distributions.
Two calibration lessons from the smokes: a p85 anchor (×50/×80/×30)
was a wall — zero crowns and 3 deepens by tick 10k, because promotion
happens on the way up, not at late-run mass; and anchors measured on
round-5's *concentrated* worlds overshoot in a pluralist world, which
runs leaner per-culture mass (×25/×30 still gave zero capitals and
dynasties at 20k, while the artifact ×15 pyramid came out healthy at
59/20/2). Settled at ×15 for settlement, faction, and artifact
(crown ≈ 1440 local mass), canon ×1. Connected-mass gate stays an
unscaled network floor. Single-seed smokes are at their variance floor
for this judgment — the round-6 fan is the instrument.

## Round-6 verdict (stages-r6-20k-20260729, full stack + ×15 gates)

- **Culture concentration: no monoculture pins.** Physical horizon top
  shares 0.73 / 0.94 / 0.82 (rounds 4–5 had 1.00s). 9100 is fully
  healthy (0.73, r2b territory) with semantic lead changes; 9102
  improved vs round 4 (0.92→0.82) with Nightshelf holding the semantic
  early game; 9101 remains the stubborn seed (0.94) — its
  concentration flows through the mass-weighted generator race, which
  lives in the owner-reserved budget layer.
- **Stage pyramid: fixed.** Settlement 83/100/48 (apex 55%→21%, no
  longer the mode), artifact 213/98/10 (25%→3%), canon 124/8/7
  unchanged-healthy, faction 155/91/2 — dynasties now genuinely rare,
  possibly a touch harsh; watch rather than retune.
- **Density:** 0.417 / 0.450 / 0.527 — tightest family yet, straddling
  the provisional band's top edge. The eased decay floors shifted the
  family down ~0.1 from round 4; the 0.3–0.5 band now looks
  ratifiable as-is rather than needing widening.
- **Mass headline:** non-regressing on all seeds (2–8× bootstrap at
  horizon).
- **Offering-spent: zero fires this round** (round 4: 5, round 5: 1).
  The cadence fix demonstrably works, but the rite's geometry is
  rare-event-variance-dominated at 3 seeds; if round 7 is also dry,
  reach for the AdjacentToTarget radius variant.
- Standing watches: orcas near-extinct (1–3), transports thin on
  9102 (2), mortal share still below the ≥20% target — untouched by
  this arc, next in the queue.
