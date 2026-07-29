# Performance Testing

Use `dotnet-trace` CPU sampling against the actual workload being optimized.
Do not add custom perf telemetry tables, and do not use synthetic kernel
harnesses as the source of truth for hotspots.

Monte Carlo output is still useful for behavior and balance evidence. For CPU
work, profile the process with `dotnet-trace` and read sampled call stacks.

## Tooling

Install or update `dotnet-trace`:

```bash
# If dotnet-trace is missing:
dotnet tool install --global dotnet-trace

# If dotnet-trace is already installed:
dotnet tool update --global dotnet-trace
```

Use the repository targets so Release mode, registry provenance, and the direct
workload process stay consistent:

```bash
make mc-smoke RUN_ID=kernel-smoke-20260725
make profile-kernel PROFILE_RUN_ID=kernel-profile-20260725
```

Both targets load the versioned pregenerated world selected by
`MC_SMOKE_WORLD` or `PROFILE_WORLD`. They fail when that artifact is absent or
stale instead of silently spending the smoke/profile run generating another
world. Generate or replace it explicitly with `make world SEED=<seed>`.
Calibration batches that need distinct starting worlds continue to use
`make mc`; pass a pregenerated world through `MC_EXTRA` only for a single-run
batch whose seed matches that world.

The simulation-only runner advances the kernel in progress-window batches.
This preserves exact tick totals and progress boundaries without stopping the
driver and constructing a stepped driver once per tick. Its reported throughput
therefore measures the requested kernel work rather than a tooling loop whose
overhead changes when the tick unit changes.

Traces stay under `.artifacts/profiles/`.

## Collect A CPU Trace

Keep the registered run parameters and pregenerated world fixed before and
after an optimization pass:

```bash
make profile-kernel \
  PROFILE_RUN_ID=perf-trace-1x10000 \
  PROFILE_TICKS=10000 \
  PROFILE_SEED=4200 \
  PROFILE_WORLD=worlds/ice-remembers-seed-4200.world
```

The target builds the Release Monte Carlo binary, traces that binary directly,
and writes exclusive and inclusive reports beside the trace. Do not wrap
`dotnet run`; that profiles the SDK/build host and can miss the simulation
process.

For UI/app work, start the app scenario and attach to the running .NET process:

```bash
godot --path . -- --prewarm-ticks=10000 --prewarm-tick-rate=10 --seed=4200

~/.dotnet/tools/dotnet-trace ps
~/.dotnet/tools/dotnet-trace collect \
  --process-id <pid> \
  --profile dotnet-sampled-thread-time \
  --output ".artifacts/profiles/app-$(date -u +%Y%m%dT%H%M%SZ).nettrace"
```

`--prewarm-ticks` advances the real interactive runtime in bounded batches,
drains its projections, binds the UI once at the requested tick, and leaves the
game paused unless `--prewarm-tick-rate` resumes it at an explicit rate. Pair
it with `--seed` so before/after traces open the same world.
Use 10,000 ticks for the reported population boundary and 25,000 ticks when the
UI workload needs a larger active census. Resume or step the paused game only
after attaching the profiler, so trace time measures steady presentation and
incremental updates rather than startup world generation.

## Generate Reports

Create exclusive, inclusive, and Speedscope views from the trace:

```bash
~/.dotnet/tools/dotnet-trace report "${PROFILE}.nettrace" topN -n 120 -v > "${PROFILE}.exclusive120.txt"
~/.dotnet/tools/dotnet-trace report "${PROFILE}.nettrace" topN --inclusive -n 180 -v > "${PROFILE}.inclusive180.txt"
~/.dotnet/tools/dotnet-trace convert "${PROFILE}.nettrace" --format Speedscope -o "${PROFILE}"
```

Open the generated `.speedscope.json` in <https://www.speedscope.app/> for
call-stack exploration.

## Reading Results

Use exclusive time to find methods doing direct work. Use inclusive time to find
callers responsible for expensive subtrees.

Ignore JIT, assembly load, and startup frames unless the work item is
specifically about startup. Prefer a long enough run for steady-state tick work
to dominate the profile.

Do not infer throughput from sampled stack reports. Use the profiled workload's
own completed-run output for elapsed time, processed ticks, row counts, and
artifact paths.

When throughput changes with world growth, report fixed tick-window rates as
well as the whole-run average. A 25,000-tick average can remain above a target
after the final 5,000-tick window has fallen below it. Parallel before/after
evidence must compare the same windows and must include the final window; an
early fast world or whole-run average may not hide midgame degradation.

Inspect operating-system thread CPU alongside the managed trace. A large thread
count does not imply useful parallelism: identify which threads consume CPU and
whether they overlap. In the Monte Carlo pipeline, a simulation-only run should
normally have one hot simulation writer, while a full run may also have one hot
DuckDB export worker during flushes.

Escalate trace length deliberately. Start with a fixed-seed 10k trace, use 25k
when throughput changes with world growth or a capacity boundary is still
unclear, and use 100k when long-horizon steady state or lifetime-key capacity is
the question. Once a representative 100k trace has completed, do not repeat it
solely as a confidence ritual: use shorter exact-output comparisons and rerun
100k only when a subsequent change affects the hotspot, memory-growth, or
capacity conclusion that the long trace established.

## Recording Results

Every Monte Carlo evidence run must have a unique human-readable run id and be
created through the registry workflow. Record important ad hoc findings with
`append-stats`. Preserve rejected and host-contaminated runs with their
rejection reason; never rename or silently substitute them.

Before comparing two results, confirm that seed, world artifact, source state,
tick count, worker count, simulation count, telemetry/export mode, and profiler
attachment match. Report individual measurements as well as paired means when
host spread is material.

The durable result and strategy ledger is
[performance.md](../performance.md). Update that document when an experiment
changes the accepted baseline, rejects an architecture, or changes the next
strategy. Do not accumulate dated batch narratives in this methodology.
