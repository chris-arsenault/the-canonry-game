# Monte Carlo Tooling

Monte Carlo batches are registered evidence. Do not create, rename, edit, or
delete Monte Carlo registry or manifest files by hand.

Use Monte Carlo output for simulation behavior and balance evidence. For CPU
hotspot analysis, use [performance testing](../methodology/performance-testing.md)
instead of adding profiling telemetry to the Monte Carlo batch.

## Registered Runs

Every new batch requires a unique human-readable run id:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  run \
  --run-id semantic-road-test-3 \
  --runs 10 \
  --ticks 500 \
  --seed 1000 \
  --trace
```

The tool writes the batch under:

```text
.artifacts/monte-carlo/semantic-road-test-3/
```

The top-level registry is:

```text
.artifacts/monte-carlo/registry.duckdb
```

Each run directory also receives a generated `manifest.json`. The registry and
manifest are owned by the Monte Carlo CLI; they are not source files.

## Commands

Create and run a batch:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  run --run-id placement-kernel-area-locality-10x500 \
  --runs 10 --ticks 500 --seed 1000 --trace
```

Register a run shell without executing simulation:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  register --run-id semantic-road-followup-notes --runs 10 --ticks 500 --seed 1000
```

Append arbitrary stats:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  append-stats --run-id placement-kernel-area-locality-10x500 \
  semantic_road_moves=28 placement_coord_diffs=6818
```

Rerun analysis for a registered batch:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  analyze --run-id placement-kernel-area-locality-10x500
```

Inspect registered evidence:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- list
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  show --run-id placement-kernel-area-locality-10x500
```

Delete a run through the registry:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  delete-run --run-id placement-kernel-area-locality-10x500
```

Deletion moves artifacts to `.artifacts/monte-carlo/.deleted/` and marks the run
deleted in the registry.

## Provenance

The CLI rejects duplicate run ids by querying `registry.duckdb` first. It also
rejects existing artifact directories for new run ids.

Evidence runs should be launched without `--no-build`. The CLI captures source
and assembly timestamps and rejects stale local builds before simulation runs.

Use `--artifact-root` only when a test or unusual local workflow needs a
different root:

```bash
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  run --run-id smoke-test --artifact-root /tmp/canonry-mc --runs 1 --ticks 5
```
