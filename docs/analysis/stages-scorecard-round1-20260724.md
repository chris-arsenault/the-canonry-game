# Entity-stages scorecard, round 1 (stages-3x100k-20260724b)

Point-in-time findings from the first bundled 3×100k after the stage
system landed (seeds 9100–9102, commit `d2073ab` + cooldown-capacity fix
`01aa70e`). Gates are docs/backlog.md items 1–21 + 12a. Round-2 changes
shipped as `7d361eb`.

## Run integrity

- First attempt crashed at tick 80,883: gambit cooldown table is lifetime
  (entityId, gambitId) pairs with no eviction; 8,183 distinct pairs
  against a capacity of 8,192. Fixed by raising
  `GambitCooldownCapacity` to 65,536 (`01aa70e`); the lifetime-recycler
  backlog item now covers both stores. Full run peaked at 10,903 pairs.
- Post-run MonteCarlo analyzer OOM-killed (exit 137) after writing all
  parquet tables; only the built-in convenience analysis outputs were
  lost. 1.5B cell-culture-influence rows total: 948M map_control, 500M
  gameplay, 47M gameplay_effects.
- `map_degenerate=1` on seed 9101 is a worldgen retry counter, harmless.

## Failed gates and the root chain

One root cause: the flat promotion gate (design said per-stage scaling
gates; round 1 shipped raw 36 / connected 180 for every rung).

- **18 Stage pyramid: APEX-HEAVY.** Corrected counts (the first scoring
  pass tripled every symbols-join — the parquet symbols table repeats
  each row once per kernel run and has no run_id; queries must SELECT
  DISTINCT it): artifacts 316 base / 336 mid / 711 legend (52% of
  stage-tracked artifacts at the apex, which should be rare); settlements
  66/46/234; factions 95/75/95; canons 76/6/20. The apex is the mode for
  artifacts and settlements — wrong shape, though not the zero-middle
  inversion first reported. Numbers from tick_snapshots (density, total
  actives) were never affected.
- **3 Artifact convergence: FAIL.** Legend = 10× band, so promoted
  artifacts nearly stopped dying; creation outran retirement ~2:1 all
  run. 643 active artifacts in run 9100 (39% of population), ~99% held
  (wild: 2).
- **1 Density: FAIL.** Active/covered hit 1.16 (band 0.3–0.5), driven by
  the artifact pool plus monsters (489).
- **12 Performance: FAIL.** 15.8/20.1/39.4 ticks/s vs ≥50. Kernel-bound,
  not telemetry-bound: a `--simulation-only` probe on seed 9100 (round-2
  binary) still ran 17.1 t/s. Entity count is the dominant cost; density
  fix is the perf lever before any kernel profiling.
- **5 the_offering_is_spent: nearly dead.** 1 fire across 3×100k;
  devotion ≥0.6 rarely accrues before the instrument wanders.

## Passed gates (round-1 evidence)

- **19 Deepen share: 5.7–6.0%** of executions (was 50.1%), success 25%
  baseline, freed pool visibly spread across context gambits.
- **18 (partial) Deaths only at base stage: PASS.** All 118 settlement
  deaths (per-run true count) had zero promotions. Corollary: `settlement_the_hold_withers`
  had zero attempts — promoted holds never sink to TempHp ≤25 because
  stage bands stretch their drain. Not a bug; it is a dormant safety
  valve. Re-judge after the pyramid fix.
- **5 Endings attributed**: seal-shatters 8–10, one-word-yields 6–7,
  memorial-wall 11–14, tome-read 12 per run.
- **12a Districts**: 316 installs, 78 reawakenings, both attributed.
- **9 Semantic stacking**: 1.08 entities/cell, worst stack 3–5.
- **10 Provincial tempo**: fires/12.5k window grow 84→363 with
  settledness.
- **12 Lifetime totals**: relationships peak 24.8k of 65,536.
- **2 Mortal share**: ~27% ≥ 20% (inflated denominator noted).

## Watch items for round 2 (stages-3x100k-20260724c)

- Pyramid shape with two rungs (Deepen 0→1 old gates; Crown 1→2 at
  8×/4× mass, health 70, 800/1600 cooldowns): expect wide base, thin
  middle, rare apex. If artifacts still race the crown, the semantic
  connected-mass gate is the next knob (their gate reads the semantic
  plane, where mass is easy to stand on).
- Artifact net creation-retirement convergence once trinket/famed bands
  (0.3×/1×) dominate lifespans.
- Density trajectory vs 0.3–0.5 band; perf follows density.
- map_control lane deeply negative (−38M net in win 3) with ambient
  emission (+12.7M) and gameplay (+26M) carrying it; run 9100 total mass
  went negative in the last two windows. Judge trend at matched density
  in round 2 before touching decay or emission.
- the_offering_is_spent at devotion ≥0.4.
