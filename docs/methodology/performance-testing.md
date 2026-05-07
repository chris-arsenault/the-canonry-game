# Performance Testing

Use CPU sampling profiles for hotspot work. Monte Carlo runs are still useful for behavioral evidence and balance checks, but the old `--perf` mode and its custom telemetry tables were removed because they did not provide actionable hotspots.

## Tooling

Install or update `dotnet-trace`:

```bash
# If dotnet-trace is missing:
dotnet tool install --global dotnet-trace

# If dotnet-trace is already installed:
dotnet tool update --global dotnet-trace
```

Build the profiling target in Release mode:

```bash
dotnet build -c Release tools/KernelProfile/KernelProfile.csproj
```

`tools/KernelProfile` starts the Ice Remembers kernel, advances deterministic batch ticks, and writes only console progress. It intentionally does not export Monte Carlo telemetry, parquet files, or analysis reports.

## Collect A CPU Trace

Keep traces under `.artifacts/profiles/`:

```bash
mkdir -p .artifacts/profiles
PROFILE=.artifacts/profiles/kernel-profile-$(date -u +%Y%m%dT%H%M%SZ)

~/.dotnet/tools/dotnet-trace collect \
  --profile dotnet-sampled-thread-time \
  --output "${PROFILE}.nettrace" \
  --show-child-io \
  -- dotnet .artifacts/bin/KernelProfile/Release/net8.0/KernelProfile.dll \
    --runs 50 \
    --ticks 500 \
    --seed 8000
```

Use the same `--runs`, `--ticks`, `--seed`, and `--config-file` values before and after a change. The default harness values are `--runs 50 --ticks 500 --seed 1000`.

Pass `--trace` only when measuring the engine trace-event path itself. Leave it off for ordinary kernel hotspot analysis.

## Generate Reports

Create exclusive, inclusive, and Speedscope views from the trace:

```bash
~/.dotnet/tools/dotnet-trace report "${PROFILE}.nettrace" topN -n 120 -v > "${PROFILE}.exclusive120.txt"
~/.dotnet/tools/dotnet-trace report "${PROFILE}.nettrace" topN --inclusive -n 180 -v > "${PROFILE}.inclusive180.txt"
~/.dotnet/tools/dotnet-trace convert "${PROFILE}.nettrace" --format Speedscope -o "${PROFILE}"
```

Open the generated `.speedscope.json` in <https://www.speedscope.app/> for call-stack exploration.

## Reading Results

Use exclusive time to find methods doing direct work. Use inclusive time to find callers responsible for expensive subtrees.

Ignore JIT, assembly load, and startup frames unless the work item is specifically about startup. Prefer a long enough run for steady-state tick work to dominate the profile.

Do not use Monte Carlo telemetry exports for hotspot analysis. They are observational artifacts for simulation behavior, not a CPU profiler.

## Profiling The Running App

To attach to an already-running .NET process:

```bash
~/.dotnet/tools/dotnet-trace ps
~/.dotnet/tools/dotnet-trace collect \
  --process-id <pid> \
  --profile dotnet-sampled-thread-time \
  --output ".artifacts/profiles/app-$(date -u +%Y%m%dT%H%M%SZ).nettrace"
```

Attach only when the app scenario matters. For repeatable kernel work, prefer the `KernelProfile` harness.
