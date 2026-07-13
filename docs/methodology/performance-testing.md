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

Build the workload in Release mode before profiling:

```bash
dotnet build -c Release
```

Keep traces under `.artifacts/profiles/`.

## Collect A CPU Trace

Wrap the real command with `dotnet-trace collect`. Keep the run parameters fixed
before and after an optimization pass.

For batch simulation plus Monte Carlo telemetry/export:

```bash
mkdir -p .artifacts/profiles
PROFILE=.artifacts/profiles/mc-trace-$(date -u +%Y%m%dT%H%M%SZ)

~/.dotnet/tools/dotnet-trace collect \
  --profile dotnet-sampled-thread-time \
  --output "${PROFILE}.nettrace" \
  --show-child-io \
  -- dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
    run \
    --run-id perf-trace-1x10000 \
    --runs 1 \
    --ticks 10000 \
    --seed 9000 \
    --trace
```

For UI/app work, start the app scenario and attach to the running .NET process:

```bash
~/.dotnet/tools/dotnet-trace ps
~/.dotnet/tools/dotnet-trace collect \
  --process-id <pid> \
  --profile dotnet-sampled-thread-time \
  --output ".artifacts/profiles/app-$(date -u +%Y%m%dT%H%M%SZ).nettrace"
```

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

The current kernel performance budget is at least 10 ticks/sec for deterministic
batch simulation. When evaluating the tick budget, profile the same workload the
user cares about: simulation-only, simulation with trace events, simulation with
Monte Carlo export, or the interactive app. Do not mix those numbers.
