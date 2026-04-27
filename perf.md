# Performance Profiling Notes

This document records the performance methodology and results recovered from
the Sulion conversation history for PTY `c7eac4fe`, session
`019dc27c-d1b0-7711-9e50-8ab7b0edde0b`, on 2026-04-25.

The measurements below were taken against the legacy `Engine` /
`SimulationRunner` runtime before the current kernel migration. Treat them as
historical evidence about the simulation hot path, not as proof of current
kernel performance.

## Methodology

Two complementary approaches were used.

### Monte Carlo Performance Telemetry

The Monte Carlo tool was run in perf mode to capture broad system-level timing
and allocation behavior across multiple full simulation runs.

Representative command:

```bash
OUT=.artifacts/perf/hotspots-$(date -u +%Y%m%dT%H%M%SZ)
dotnet run -c Release --project tools/MonteCarlo/MonteCarlo.csproj -- \
  --runs 30 \
  --ticks 1500 \
  --seed 8000 \
  --perf \
  --perf-sample-cadence 1 \
  --system-perf-sample-cadence 1 \
  --out "$OUT"
```

The exported performance data was queried with DuckDB. This data was useful
for identifying tick windows, system-level hotspots, worst ticks, memory
pressure, and GC behavior. It was not sufficient to identify method-level CPU
hotspots.

The MC perf run tracked:

- total run time and per-tick timing
- early, mid, and late tick windows
- active entity and relationship counts
- process candidate counts
- message counts
- system phase totals
- managed memory and GC counts

### Managed CPU Profiling

After the MC data showed that the process/planning system dominated runtime,
the pass moved to actual .NET CPU profiling.

`dotnet-trace` was installed into `/tmp/canonry-dotnet-tools` and run with
`Microsoft-DotNETCore-SampleProfiler` outputting both `.nettrace` and
Speedscope JSON files.

The profile workload used a temporary Release console harness outside the repo
that referenced the legacy `src/Engine/Engine.csproj`. It ticked the simulation
directly and avoided the Monte Carlo parquet/state exporter so exporter cost
would not be mixed into engine cost.

Representative workload:

```bash
/tmp/canonry-dotnet-tools/dotnet-trace collect \
  --providers Microsoft-DotNETCore-SampleProfiler \
  --format Speedscope \
  --output .artifacts/profiles/sim-cpu-<stamp>.nettrace \
  --show-child-io \
  -- dotnet /tmp/canonry-sim-profile-harness/bin/Release/net8.0/canonry-sim-profile-harness.dll \
    --runs 10 \
    --ticks 1500 \
    --seed 8000 \
    --warmup-runs 1 \
    --warmup-ticks 250
```

Reports were generated with:

```bash
/tmp/canonry-dotnet-tools/dotnet-trace report "$TRACE" topN -n 40 --inclusive
/tmp/canonry-dotnet-tools/dotnet-trace report "$TRACE" topN -n 50 --exclusive
```

Do not compare Monte Carlo elapsed times directly with profile harness elapsed
times. They used different harnesses and instrumentation. Compare each run only
against runs from the same methodology.

## Monte Carlo Results

### Baseline Perf Run

Artifact:

```text
.artifacts/perf/hotspots-20260425T025957Z
```

Summary:

| Metric | Value |
| --- | ---: |
| runs | 30 |
| ticks | 45,000 |
| avg run ms | 9,671.87 |
| total sim ms | 290,156 |
| avg tick ms mean | 6.438229 |
| p95 tick ms mean | 12.043943 |
| max tick ms | 91.9096 |
| peak managed memory bytes | 37,519,704 |
| gen0 collections | 10,102 |
| gen1 collections | 4,592 |
| gen2 collections | 100 |

Window timing:

| Window | Samples | Avg Tick Ms | P95 Tick Ms | Max Tick Ms | Avg Entities | Avg Relationships | Avg Process Candidates | Avg Messages |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| early 1-100 | 3,000 | 3.8756 | 6.21938 | 91.9096 | 24.88 | 19.19 | 32.08 | 92.15 |
| mid 700-900 | 6,030 | 7.93351 | 15.569385 | 31.7421 | 31.41 | 43.65 | 45.96 | 108.03 |
| late 1300-1500 | 6,030 | 1.495087 | 4.110215 | 7.0273 | 14.73 | 42.17 | 13.67 | 44.69 |

Late-window system hotspots:

| System | Total Ms | Share | Avg Ms | P95 Ms |
| --- | ---: | ---: | ---: | ---: |
| processes | 8,776.101 | 97.64% | 1.455406 | 4.04631 |
| relationships | 72.313 | 0.80% | 0.011992 | 0.02001 |
| tableau | 25.700 | 0.29% | 0.004262 | 0.01441 |

The baseline showed that the process system dominated late-window runtime. The
mid window was the worst aggregate tick window because it had the highest
process candidate pressure.

### Perf Harness / Diagnostic Improvements

Artifact:

```text
.artifacts/perf/hotspots-optimized-20260425T031920Z
```

Summary:

| Metric | Baseline | Optimized |
| --- | ---: | ---: |
| avg run ms | 9,671.87 | 7,334.64 |
| avg tick ms mean | 6.438229 | 4.879568 |
| p95 tick ms mean | 12.043943 | 9.363327 |
| max tick ms | 91.9096 | 92.5065 |
| peak managed memory bytes | 37,519,704 | 30,342,672 |
| gen0 collections | 10,102 | 5,582 |
| gen1 collections | 4,592 | 48 |
| gen2 collections | 100 | 24 |

Late-window `processes` remained dominant:

| System | Total Ms | Share | Avg Ms | P95 Ms |
| --- | ---: | ---: | ---: | ---: |
| processes | 5,765.234 | 96.80% | 0.956092 | 3.19145 |
| relationships | 67.067 | 1.13% | 0.011122 | 0.017855 |

This pass improved measured MC perf, especially GC behavior, but it did not
fully answer the simulation hot-path question. The changes were too mixed with
diagnostic/harness cost to be sufficient evidence for engine optimization.

### Actual Simulation Cache/Evaluator Attempts

Two subsequent MC runs attempted actual simulation-side improvements.

Artifacts:

```text
.artifacts/perf/cache-sim-20260425T034246Z
.artifacts/perf/evaluator-sim-20260425T035025Z
```

Summary:

| Run | Avg Run Ms | Avg Tick Ms Mean | P95 Tick Ms Mean | Max Tick Ms | Peak Managed Memory Bytes | Gen0 | Gen1 | Gen2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| cache-sim | 7,541.75 | 5.016929 | 9.514133 | 91.6673 | 31,483,128 | 6,003 | 99 | 26 |
| evaluator-sim | 7,556.65 | 5.026921 | 9.705047 | 100.2465 | 31,470,696 | 5,789 | 121 | 26 |

Late-window process share remained effectively unchanged:

| Run | Process Total Ms | Process Share | Process Avg Ms | Process P95 Ms |
| --- | ---: | ---: | ---: | ---: |
| cache-sim | 6,461.112 | 96.81% | 1.071495 | 3.409485 |
| evaluator-sim | 6,782.217 | 97.14% | 1.124746 | 3.1385 |

These runs did not beat the earlier optimized MC run. The evidence at this
point was that system-level MC telemetry could identify the process system as
the target, but method-level profiling was required before making further
optimization claims.

## CPU Profile Results

Profile artifacts:

```text
.artifacts/profiles/sim-cpu-20260425T040241Z.nettrace
.artifacts/profiles/sim-cpu-20260425T040241Z.speedscope.json
.artifacts/profiles/sim-cpu-post-tracefix-20260425T040648Z.nettrace
.artifacts/profiles/sim-cpu-post-tracefix-20260425T040648Z.speedscope.json
.artifacts/profiles/sim-cpu-post-blockers-20260425T041056Z.nettrace
.artifacts/profiles/sim-cpu-post-blockers-20260425T041056Z.speedscope.json
```

### Initial Managed Profile

Workload:

```text
10 runs x 1500 ticks, seed 8000, 1 warmup run x 250 ticks
elapsed_ms=94459.71
checksum=15811
```

Top inclusive functions:

| Function | Inclusive | Exclusive |
| --- | ---: | ---: |
| TickClock.ExecutePhases | 97.35% | 0.02% |
| EntityEvaluator.EvaluateAll | 96.18% | 0.02% |
| EntityEvaluator.RegenerateGoals | 89.59% | 0.00% |
| GoalGenerator.Generate | 89.59% | 0.01% |
| GoalGenerator.GeneratePrimaryPlans | 86.79% | 0.06% |
| AttributeRef.ToString | 59.57% | 0.00% |
| Char[].Rent | 52.52% | 52.52% |
| DefaultInterpolatedStringHandler.ToStringAndClear | 19.41% | 0.00% |
| String.Join | 13.02% | 0.00% |
| List<T>.AddRange | 7.18% | 6.92% |
| SpatialIndex.FindPath | 4.34% | 1.05% |

The first real profile showed that goal generation was dominated by string
creation, especially `AttributeRef.ToString`, interpolation, and `String.Join`.

### After Trace/String Fix

Workload:

```text
10 runs x 1500 ticks, seed 8000, 1 warmup run x 250 ticks
elapsed_ms=38261.12
checksum=15720
```

Top inclusive functions:

| Function | Inclusive | Exclusive |
| --- | ---: | ---: |
| TickClock.ExecutePhases | 75.14% | 0.02% |
| EntityEvaluator.EvaluateAll | 73.41% | 0.02% |
| EntityEvaluator.RegenerateGoals | 62.90% | 0.01% |
| GoalGenerator.Generate | 62.89% | 0.03% |
| GoalGenerator.GeneratePrimaryPlans | 58.62% | 0.10% |
| List<T>.AddRange | 41.03% | 40.65% |
| Utilities.GetMemoryPressure | 19.92% | 19.92% |
| SpatialIndex.FindPath | 6.81% | 1.50% |
| EnumerableHelpers.ToArray | 5.61% | 5.54% |
| DefaultInterpolatedStringHandler.AppendFormatted | 4.64% | 4.63% |
| AttributeRef.ToString | 1.92% | 0.00% |

The string fix was strongly validated by the profile. The profile workload fell
from `94.5s` to `38.3s`, and `AttributeRef.ToString` dropped from `59.57%`
inclusive to `1.92%` inclusive.

The new dominant cost became allocation/copying in planner goal generation,
especially `List<T>.AddRange`.

### After Blocker/Diagnostic Split

Workload:

```text
10 runs x 1500 ticks, seed 8000, 1 warmup run x 250 ticks
elapsed_ms=50297.84
checksum=15791
```

Top inclusive functions:

| Function | Inclusive | Exclusive |
| --- | ---: | ---: |
| TickClock.ExecutePhases | 95.06% | 0.02% |
| TickClock.Tick | 94.18% | 0.00% |
| EntityEvaluator.EvaluateAll | 93.02% | 0.02% |
| EntityEvaluator.RegenerateGoals | 80.16% | 0.00% |
| GoalGenerator.Generate | 80.15% | 0.03% |
| GoalGenerator.GeneratePrimaryPlans | 75.07% | 0.18% |
| List<T>.AddRange | 52.17% | 51.64% |
| SpatialIndex.FindPath | 8.43% | 2.02% |
| List<T>.set_Capacity | 7.78% | 7.78% |
| EnumerableHelpers.ToArray | 6.55% | 6.47% |
| DefaultInterpolatedStringHandler.AppendFormatted | 6.32% | 6.27% |
| AttributeRef.ToString | 2.49% | 0.00% |

The blocker/diagnostic split did not validate as a performance improvement in
the profile run. It may have changed behavior or measurement shape, but the
measured workload regressed from `38.3s` to `50.3s`. It should not be counted as
a confirmed optimization.

## Conclusions

The MC telemetry was useful for locating the broad system hotspot:
`processes` dominated late-window cost at roughly `97%`, and mid-game process
candidate pressure produced the worst aggregate tick timings.

The managed CPU profiles were necessary to identify the real method-level hot
spots:

- before the string fix, goal generation was dominated by string creation
- after the string fix, planner allocation/copying became dominant
- `GoalGenerator.GeneratePrimaryPlans` remained the central hot path
- `List<T>.AddRange`, list resizing, `ToArray`, and buffer allocation were the
  clearest allocation/copying costs
- `SpatialIndex.FindPath` was the secondary simulation hotspot

The most important validated optimization was removing trace/string creation
from the hot planner path. The remaining unclosed performance work from this
pass was to redesign planner goal generation so it does not allocate and copy
large temporary goal/blocker lists per entity evaluation.

For the current architecture, these results support the kernel invariants:

- hot-path kernel work should use primitive IDs, not strings
- diagnostic formatting should happen outside the simulation path
- planner structures should be preallocated or arena/ring-buffer backed
- analysis/export layers must not reintroduce string interpolation into MC hot
  loops
- future claims about kernel performance should use both MC-level telemetry and
  direct managed profiling
