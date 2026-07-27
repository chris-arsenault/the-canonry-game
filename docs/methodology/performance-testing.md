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

## Tick Budget

For short pre-parallelization regression checks, the current kernel budget is at
least 312.5 ticks/sec on the seed-9100, 5,000-tick, Release, simulation-only
workload loaded from its pre-rendered world. Five current ticks perform the
work formerly bundled into one measured advance: five minor-actor runs and one
main-program run. Keep the separate accepted long-horizon target and memory
gate from the master plan; this short gate prevents groundwork from quietly
making the one-writer kernel slower.

When evaluating the budget, profile the same workload the user cares about:
simulation-only, simulation with trace events, simulation with Monte Carlo
export, or the interactive app. Do not mix those numbers.

## Batch A Owner-Ready Storage Baseline

The Batch A regression screen uses seed 4200, the versioned pre-rendered world,
5,000 ticks, Release mode, and `--simulation-only`. World loading occurs before
the runner starts its elapsed-time stopwatch.

Two registered before runs,
`parallel-batch-a-before-1x5000-20260726a` and
`parallel-batch-a-before-repeat-1x5000-20260726a`, averaged
`15.620 ms/tick`. Two exact-final after runs,
`parallel-batch-a-after-final-1x5000-20260726c` and
`parallel-batch-a-after-final-repeat-1x5000-20260726a`, measured
`15.742` and `14.956 ms/tick`, averaging `15.349 ms/tick`. This is a `1.73%`
improvement (`1.018x`) across paired means.

The after-run spread is larger than the mean improvement, so this evidence
supports a no-regression conclusion, not a material speedup claim. Report the
individual range with the paired mean when using these runs as the Batch B
one-worker baseline. Command traces
`parallel-batch-a-command-before-1x1000-20260726a` and
`parallel-batch-a-command-after-1x1000-20260726a` show the empty locked command
read moving from `1.68%` of sampled CPU to no named `TryRead` samples.

## Batch B Worker-One Regression Gate

Batch B uses the same seed-4200, pre-rendered-world, 5,000-tick, Release,
simulation-only contract as Batch A. Compare worker-one paired means before
measuring any multi-worker speedup: persistent-worker infrastructure is not
acceptable if its disabled/default topology slows the reference simulation.

The first implementation failed that gate. Registered runs
`parallel-batch-b-after-1x5000-20260726a` and
`parallel-batch-b-after-repeat-1x5000-20260726a` measured `15.909` and
`18.541 ms/tick`, averaging `17.225 ms/tick`. That was `12.2%` slower than
Batch A's exact-final `15.349 ms/tick` mean. Investigation found worker one
paying thread-local execution-context and scratch lookup plus block-arena
bookkeeping on hot operations.

Those runs are rejected implementation evidence, not the Batch B result. The
worker-zero fast lane stays inside the common range and append infrastructure;
it does not restore separate serial gameplay code. Exact-final registered runs
`parallel-batch-b-after-fastpath-1x5000-20260726a` and
`parallel-batch-b-after-fastpath-repeat-1x5000-20260726a` measured `14.887`
and `15.110 ms/tick`, averaging `14.999 ms/tick`. That is `2.28%` faster
(`1.023x`) than the Batch A mean. Use the exact-final pair as the Batch C
worker-one baseline.

## Batch C Typed-Owner Gate

Batch C uses the same seed-4200, pre-rendered-world, 5,000-tick, Release,
simulation-only contract. Preserve failed measurements in the registry; do not
rename a slower run into a successful result after implementation changes.

The first owner-reduction pair,
`parallel-batch-c-after-owner-reduction-1x5000-20260726a` and
`parallel-batch-c-after-owner-reduction-repeat-1x5000-20260726a`, measured
`18.851` and `18.934 ms/tick`. Their `18.892 ms/tick` mean was `25.96%` slower
than the Batch B mean and is rejected. A trace showed all delta resolution at
only `2.41%` inclusive, which redirected the investigation toward unnecessary
multi-pass orchestration and concurrency guards in read-heavy world queries.

After direct one-worker resolution and direct culture-revision reads, registered
run `parallel-batch-c-revision-read-fastpath-1x5000-20260726a` measured
`15.531 ms/tick` (`64.388 ticks/sec`). This is inside Batch A's previously
observed `14.956`-to-`15.742 ms/tick` host spread, but remains `3.55%` above the
Batch B paired mean. Do not claim the worker-one gate has passed from this single
clean run. Registered repeats
`parallel-batch-c-revision-read-fastpath-repeat-1x5000-20260726a` and
`parallel-batch-c-revision-read-fastpath-clean-repeat-1x5000-20260726a` are
rejected because competing Roslyn/MSBuild processes appeared after launch.

The internal four-worker run
`parallel-batch-c-owner-reduction-4w-1x5000-20260726a` measured
`23.908 ms/tick`, slower than worker one. It is negative architecture evidence:
public worker wiring must remain closed until dispatch regions are coalesced and
the complete worker-count matrix passes. Compare process-wide throughput, not
worker-loop time, and record host-load rejection causes with `append-stats`.

## Batch E Worker-Count Gate

Batch E wired `MC_WORKERS` through every canonical Monte Carlo and kernel-profile
target and used the seed-4200 pre-rendered world. Full kernel snapshots are
byte-identical at 1, 2, 4, 8, and 16 workers in the durable epoch invariance
test. This establishes determinism, not speed.

The first 1,000-tick screen measured `7.849`, `11.555`, `13.105`, and
`14.744 ms/tick` at 1, 2, 4, and 8 workers respectively. The corresponding
registered run ids are:

- `parallel-batch-e-screen-1w-1x1000-20260726a`;
- `parallel-batch-e-screen-2w-1x1000-20260726a`;
- `parallel-batch-e-screen-4w-1x1000-20260726a`; and
- `parallel-batch-e-screen-8w-1x1000-20260726a`.

Coalescing owner families into dependency-preserving batches and replacing
shared culture revision/mass atomics with worker-local reduction improved the
eight-worker short result to `13.071 ms/tick` in
`parallel-batch-e-atomic-free-screen-8w-1x1000-20260726a`. It still delivered
only `0.60x` the initial worker-one throughput instead of the required `4.5x`.
An arena-padding experiment did not help and was reverted; its negative run is
`parallel-batch-e-padded-arena-screen-8w-1x1000-20260726a`.

The post-coalescing trace
`parallel-batch-e-coalesced-profile-8w-1x1000-20260726a` puts owner batches at
`0.70%` inclusive sampled thread time, delta resolution at `0.81%`,
deterministic sorting at `0.39%`, and event publication at `1.05%`. Worker
sleep/wake and joins dominate: `KernelWorker.Run` is `41.66%` exclusive, the
two `Monitor.Wait` forms are `10.14%` and `5.28%` exclusive,
`WaitForWorkers` is `3.95%` inclusive, and `KernelWorker.Start` is `1.17%`
exclusive. Observed RSS was about `460 MB`.

Cross-program local coalescing also failed the worker-one gate. Its registered
5,000-tick pair averaged `15.260 ms/tick`, versus Batch D's
`13.409 ms/tick` mean. Direct registration-order worker-one dispatch was
restored through the same evaluators and reducers; the next registered run,
`parallel-batch-e-worker1-restored-1x5000-20260726a`, measured
`14.770 ms/tick` on a busier host and still did not establish a passing paired
gate.

The Batch E candidate is therefore rejected. Do not quote a 16-worker result:
none was run. The 10,000-, 25,000-, and 100,000-tick escalations were also
stopped as required. Any next parallel design must first reduce dispatch/join
frequency or move a materially larger independent unit of work behind each
boundary; adding workers to the current synchronous tick is not evidence-backed.
