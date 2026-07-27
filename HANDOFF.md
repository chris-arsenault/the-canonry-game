# Handoff — 2026-07-27

State at machine move. Branch: `parallelization-batch-c` (kernel
parallelization work in progress, through the deterministic typed-owner
scheduler). Everything below is pushed; working tree clean.

## Where things stand

- **Entity stages + demography stack**: implemented and tuned through
  three 3×100k rounds on `main` (merged into this branch's history).
  Round records: `docs/analysis/stages-scorecard-round1-20260724.md`
  and `-round2-…md` (round 2 opens with the standing judgment criteria:
  trend shape over point targets). Round 3b was deliberately stopped
  mid-run for the perf work; its partial streams (ticks 0–50/75k) are
  the pre-perf-change reference under
  `.artifacts/monte-carlo/.deleted/stages-r3b-*`.
- **Verification is owed**: the perf/parallelization changes subtly
  alter the simulation, so the full bundled 3×100k scorecard
  (`docs/backlog.md`, items 1–21 + 12a) must be redone once they land.
  Round-3b partials showed the fixes working pre-change: artifacts
  bounded on all seeds (created:retired near parity), seed 9102 density
  plateaued ~0.51, remaining growth proportional across kinds.

## How to rerun verification

```
make mc-fan MC_TICKS=100000 MC_SEEDS="9100 9101 9102" RUN_ID=<id> \
    MC_EXTRA="--save-state-every 25000"
python3 tools/analysis/mc_scorecard.py .artifacts/monte-carlo/<id>-s*/
```

One MC process per seed; wall time is the slowest seed (~1–2 h).
Progress lines print every 5k ticks (density is the early kill signal);
core scorecard tables stream to `<run>/stream/` every 12.5k ticks and
the scorecard script accepts those dirs mid-run. Registry supports
concurrent per-seed runs (short-lived connections).

## Open items for the next verification pass

- Density plateau levels with the Enduring artifact band (`549fabe`);
  if curves flatten above 0.5, propose re-ratifying the provisional
  0.3–0.5 band (backlog anticipates this) rather than tuning further.
- `the_offering_is_spent` > 0 fires after the cadence-800 fix
  (`15aa1f8`). If still zero: next lever is a radius variant of
  `AdjacentToTarget` — new query vocabulary, owner decision.
- Watch items (round-2 record): wild-artifact share (~94% held), orcas
  near-extinct at horizon (predates the stack), slow faction
  accumulation (candidate for the same base-band treatment).
- Perf gate (≥50 ticks/s at 100k) re-measured solo post-parallelization;
  `MC_EXTRA=--simulation-only` gives kernel-only throughput.

## Gotchas

- Parquet `symbols` repeats rows per kernel run with no run_id —
  always SELECT DISTINCT before joining (the scorecard script does).
- Cadence generators write evaluation rows only when they fire; silence
  in `generator_events` means "never won a slot", not "never tried".
- Cooldown and relationship stores are lifetime-append (no eviction);
  occupancy is measured in the scorecard, recycler design is backlogged.
