# Entity-stages scorecard, round 4 (stages-r4-20k-20260729)

The post-parallelization verification redo owed by `HANDOFF.md` and
`docs/backlog.md` (items 1–21 + 12a). First formal round on the unified
tick clock: commit `18f7bfe4` on `main`, seeds 9100–9102 fanned one
process per seed, horizon 20,000 ticks (equivalent to the old 100k-raw
horizon; all pre-round-4 record figures divide by 5 when compared).
All three seeds ran to horizon; zero drops; wall time ~25 minutes.

Judged by the round-2 standing criteria: trend shape over point
targets; plateau, not level; seed variance is signal.

## Scoring-infrastructure changes this round

- Scorecard windows rescaled to the unified clock: fine window 2500
  ticks, coarse 5000 (was 12500/25000) — same 8/4 windows per run.
- The parallelization replaced per-event culture-influence telemetry
  with periodic field snapshots. `cell_influence_events` and
  `culture_influence_action_field_events` exist but hold zero rows;
  the per-source-lane income statement (map_control vs gameplay vs
  emission) is **no longer recorded**. Section 17 now reads the mass
  trajectory from `culture_field_cell_snapshots` (every 5 ticks).
  If lane attribution is wanted again it needs a kernel telemetry
  decision, not a scorecard fix.

## What held (the perf changes did not shift these)

- **Density (1): PASS.** Every seed shows bootstrap climb → mid-run
  hump → plateau/recession as the growing map opens room. Horizon:
  9100 0.614, 9101 0.624, 9102 0.512, all flat-or-falling with
  coverage still growing (966–1352 cells). 9102 lands on the r3b
  pre-perf reference plateau (~0.51) almost exactly. Two seeds
  plateau above the provisional 0.3–0.5 band — this is the
  re-ratification case the backlog reserved: propose 0.3–0.65
  against these healthy curves rather than tuning further.
- **Artifact net convergence (3): PASS.** Created:retired reaches
  parity by mid-run (9100 win 6: 110:115, win 7: 132:132) on all
  seeds. POI creation tapers toward zero late with occasional
  retirements — net near zero.
- **the_offering_is_spent (5): FIXED.** 1–3 fires per seed (5 total
  across the trio). The cadence-800 fix (`15aa1f8`) closed it; the
  radius-variant escalation is not needed.
- **Mass headline (17): trend-PASS.** Total physical raw mass at
  horizon far above bootstrap on all seeds (9100 215k, 9101 723k,
  9102 280k vs ~75–82k at tick 2500). Negative windows (all seeds dip
  in the 15k–17.5k stretch) recover by horizon on 9100/9101; 9102
  drifts −24% off its 12.5k peak but holds 3.4× bootstrap. Volatility
  is coverage-coupled (cells-with-mass swings with mass), consistent
  with world-beat freeze/thaw cycles.
- **Wild artifacts (6): IMPROVED.** Wild share 10–27% by seed (round
  2: ~6%). Direction the owner wanted.
- **Deepen share (19): PASS.** 3.1–3.5% of executions (<10% gate;
  round 2 was 4.2%).
- **Semantic stacking (9): PASS.** 1.04–1.08 per cell, worst stack 3.
- **Headroom (12): PASS.** Relationship lifetime totals 9.6k–16.9k,
  cooldown pairs 5.3k–8.5k vs 65,536 caps.
- **Provincial tempo (10): PASS.** Fires scale with settledness
  (64/window bootstrap → ~220–300 late) on all seeds.

## What moved (drift to diagnose or judge)

- **Stage pyramid (18): FAIL for settlements, degraded for factions.**
  Settlements inverted: 51 base / 84 mid / 178 apex across the trio
  (57% apex; round 2: ~13%). Factions flat: 65/75/63. Artifacts
  mid-heavy but acceptable: 107/151/77 (23% apex; round 2: 11%).
  Canons healthy: 101/10/8. Crown pacing constants are unchanged and
  unit-consistent (gambits ran on the 5-tick main cadence before
  unification, so attempts-per-run are the same); the suspect is the
  mass gate — `CulturalWorkTierMinimumRawMass × 8` — becoming easy to
  clear as interior holding strengthened. Settlement crowns succeeded
  178× vs 273 deepens (1.5:1; artifacts 5.3:1). Demotion pressure is
  near-absent: `settlement_the_hold_withers` fired 3× total. Next
  lever: raise the crown mass multiplier or give withers real
  frequency; decide after checking round-2 settlement mass-at-crown
  levels against these runs.
- **Mortal share (2): BELOW TARGET.** Penguins+orcas are 10–13% of
  active population (target ≥20%); adding monsters reaches only
  17–20%. Penguin median lifespan 1863 ticks (~37y) sits slightly
  under the 40–60y intent.
- **Culture concentration (new watch).** Two of three seeds end as
  near-monocultures (9101: culture 3 holds 720k of 723k mass; 9102:
  258k of 280k). 9100 stays pluralist (91k/79k/46k). Whether this is
  pre-existing or perf-change drift is unknown — the round-2 lane
  aggregates cannot answer it; needs a culture-share trajectory from
  the r2b snapshot tables if pursued.

## Standing watch items

- **Orcas: still near-extinct** at horizon (2–3 active per seed, 121
  deaths). Predates the stack (round 1, 2, now 4); needs its own
  diagnosis.
- **Transports thin on one seed**: 9102 ends with 3 active (441
  deaths across trio); 9100/9101 hold 24/18. Seed variance for now.
- Faction accumulation: 18 faction deaths across the trio (round-2
  candidate for base-band treatment) — unchanged, monotonic.

## Run/record notes

- Fan throughput ~10–137 t/s (fast at bootstrap, ~10–20 t/s at high
  population); full 3-seed fan ≈ 25 minutes wall. The old 1–2 h
  verification loop is now a coffee break — mid-run kill decisions
  are barely needed at this horizon.
- `symbols` dedup and stream-dir scoring behavior unchanged from
  round 2. Stage-ledger reconstruction unchanged (crowns count as
  promotions, seal-shatter as faction demotions).
