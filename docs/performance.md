# Performance State and Experiment Ledger

This document is the durable record of runtime-performance decisions. It
records the workload contract, comparable measurements, accepted and rejected
experiments, and the strategy implied by those results. Raw Monte Carlo facts
remain in the run registry and traces remain under `.artifacts/profiles/`;
this document does not replace either evidence source.

Use [performance-testing.md](./methodology/performance-testing.md) for the
measurement procedure. Use this document to decide which result is current,
which experiments changed the architecture, and which approaches should not be
repeated without new evidence.

## Current Reference State

The production reference is the deterministic, one-clock, one-worker path on
`parallelization-batch-c`. The fixed worker set and typed destination-owner
reducers are implemented, but worker count one remains the default because
fine-grained multi-worker execution made the measured workloads slower.
Execution topology is not snapshot state.

The most recent clean short comparison uses:

- Release mode;
- seed `4200`;
- the matching pre-rendered Release world;
- one simulation and one kernel worker;
- `10,000` ticks;
- simulation-only execution; and
- no profiler attached.

The accepted current-source run is
`parallel-batch-c-tuning-locality-cache-1x10000-20260729b`:

| Measure | Result |
| --- | ---: |
| Elapsed time | `159.333 s` |
| Aggregate throughput | `62.762 ticks/s` |
| Final 1,000-tick window | `41.3 ticks/s` |

The same-source baseline before the accepted lookup and locality-cache changes,
`parallel-batch-c-tuning-baseline-1x10000-20260729a`, measured `44.208
ticks/s` overall and `25.9 ticks/s` in its final window. The accepted changes
therefore improved aggregate throughput by `42.0%` and the final window by
`59.5%` on this exact comparison.

This is the current short baseline, not a long-horizon claim. No 20,000-tick
run has yet been made after both accepted tuning changes. The preceding
movement-correctness run,
`batch-e-movement-coordinate-repair-20k-20260729a`, measured `42.529 ticks/s`
overall and `25.5 ticks/s` in its final window, but it predates those changes.
Do not combine the short current result and the older 20,000-tick result to
infer a new midgame rate.

## Current Profile

Registered trace `parallel-batch-c-post-movement-10k-20260729a` covered the
same seed and 10,000-tick simulation shape before the latest accepted lookup
and locality-cache tuning. It completed at `43.502 ticks/s` under profiler
overhead. The throughput is not a baseline; normalized sampled time inside
`Simulation.AdvanceTicks` is the useful evidence:

| Area | Inclusive share |
| --- | ---: |
| Low-altitude verbs | `54.449%` |
| Effects | `16.985%` |
| Movement reach/path work | `12.599%` |
| Minor actors | `6.944%` |
| Primitive event publication | `4.595%` |
| Local-delta resolution | `3.929%` |
| Deterministic sorting | `3.825%` |

Overlapping callees included `CultureLocality.CellAt` at `15.322%`,
hierarchical route planning at `12.994%`, and `CellIsKnown` at `4.636%`.
The accepted lookup and locality-cache changes directly reduce several of
those overlapping costs, so this trace must be refreshed before using its
percentages to rank another implementation.

## Accepted Work

### Simulation-shape and storage groundwork

The completed groundwork is retained because it reduced work, made hot state
bounded by live data, or established explicit mutation ownership:

- one tick clock replaced the former raw/main subdivision;
- persistent minor actors replaced emitted short-lived workers while retaining
  discrete cultural fronts, road crews, campaign columns, and encounters;
- retired entities, ended relationships, expired cooldowns, completed
  provenance, and zero-mass culture rows leave active hot storage;
- runtime numeric state is fixed point;
- dense map-cell IDs, active vectors, relationship adjacency, due indexes, and
  reusable occurrence-safe slots replace broad high-water scans;
- local evaluators publish typed deltas to entity, relationship, or cell
  owners; and
- gambits and generators use frozen strategic intent epochs with deterministic
  structural reservation.

These changes define the current kernel shape. Historical “old-tick
equivalent” measurements are useful only for judging the clock/minor-actor
redesign that produced them; they are not current throughput baselines.

### One-worker scheduler groundwork

The owner-ready storage pass removed an empty locked command read and prepared
explicit destination ownership. Its paired seed-4200, 5,000-tick results moved
from a `15.620 ms/tick` mean to `15.349 ms/tick`. The improvement was inside
host spread, so the accepted conclusion was no regression rather than material
speedup.

The persistent-worker implementation initially regressed to `17.225 ms/tick`.
A direct worker-zero path through the same evaluators and append structures
removed unnecessary execution-context and arena bookkeeping. Runs
`parallel-batch-b-after-fastpath-1x5000-20260726a` and
`parallel-batch-b-after-fastpath-repeat-1x5000-20260726a` averaged `14.999
ms/tick`, `2.28%` faster than the prior paired mean.

Typed owner reduction, strategic intents, deterministic worker-count
invariance, and host/tool worker-count wiring remain implemented. Their value
is a clear ownership boundary and a safe experimentation surface; they are not
represented as a parallel speedup.

### Movement-coordinate correctness

Deferred movement admission and application now share one contract. A
destination carries explicit `(q, r, z)`, must already be a known cell, and is
reported as moved only when its typed movement delta is actually admitted.
Follow and gambit placement copy the intended vertical coordinate; semantic
movement remains on `z = 0`.

The fixed-seed repair runs
`batch-e-movement-coordinate-repair-9k-20260729a` and
`batch-e-movement-coordinate-repair-20k-20260729a` completed without an
invalid destination. This was a correctness repair, not a claimed speedup.

### Direct cell lookup and entity-locality reuse

Run `parallel-batch-c-tuning-lookups-1x10000-20260729a` measured `59.768
ticks/s`, a `35.2%` improvement over its exact baseline. The retained
mechanical changes resolve a known cell once and reuse its dense ID for terrain,
travel structure, road quality, and culture mass. They also remove a redundant
known-cell check from exploration.

Run `parallel-batch-c-tuning-locality-cache-1x10000-20260729b` measured
`62.762 ticks/s`, a further `5.0%` improvement. The retained cache is one
last-entity entry per worker, keyed by phase revision, world version, entity
slot, and occurrence handle. It reuses physical and semantic pressure and
crowding only inside an unchanged published world view. It does not add a
materialized locality field or relax mutation visibility.

## Rejected or Inconclusive Experiments

| Experiment | Evidence | Outcome |
| --- | --- | --- |
| Initial typed-owner application | Two 5,000-tick runs averaged `18.892 ms/tick`, `25.96%` slower than the prior reference. | Rejected orchestration shape; direct one-worker resolution retained. |
| Four-worker typed-owner run | `parallel-batch-c-owner-reduction-4w-1x5000-20260726a` measured `23.908 ms/tick`. | Rejected as a performance configuration. |
| Multi-worker end-to-end screen | Initial 1,000-tick results were `7.849`, `11.555`, `13.105`, and `14.744 ms/tick` at 1, 2, 4, and 8 workers. | Every measured multi-worker count was slower. |
| Coalesced/atomic-free 8-worker path | `parallel-batch-e-atomic-free-screen-8w-1x1000-20260726a` reached `13.071 ms/tick`, only `0.60x` the initial worker-one throughput. | Rejected; barriers and wake/join costs still dominated. |
| Worker-arena padding | `parallel-batch-e-padded-arena-screen-8w-1x1000-20260726a` did not improve the result. | Reverted. |
| Cross-program local coalescing | A 5,000-tick pair averaged `15.260 ms/tick` versus the preceding `13.409 ms/tick` mean. | Reverted for worker one; direct registration-order dispatch restored. |
| Event order fast path | `parallel-batch-c-tuning-event-order-1x10000-20260729a` improved the clean comparison by only `0.53%`. | Reverted as insufficient signal and unnecessary branch complexity. |
| Locality-cache first run | `parallel-batch-c-tuning-locality-cache-1x10000-20260729a` ran during heavy competing compiler load. | Rejected as host-contaminated; replaced by the clean `...20260729b` run. |
| Remove culture-locality dirty tracking | Inspection found `Engine.Host` drains these cells for projection refresh. | Not dead state; removal was immediately reverted. Preserve until that projection contract changes. |
| Eventually consistent owner execution | Explored on `non-deterministic-owner-kernel-wip`; integration did not produce an acceptable stable implementation. | Abandoned on the current line. The deterministic rollback point is tagged `pre-async-deterministic-kernel-20260727`. |

The multi-worker invariance test publishes byte-identical snapshots at 1, 2,
4, 8, and 16 workers. That is a correctness result only. No 16-worker
throughput result exists, and the rejected short screen was deliberately not
escalated to 10,000, 25,000, or 100,000 ticks.

## Historical Results That Are Not Current Baselines

The persistent-minor-actor/tick-unification pass measured `49.629` current
ticks/s over 5,000 ticks in
`phase10-persistent-actors-midgame-1x5000-20260726a`. Under the removed
five-old-ticks-per-current-tick model, that represented `248.144` old-tick
equivalents/s overall and a `2.009x` improvement over the corrected prior
whole-run measurement. The final window was `31.7` current ticks/s and still
declined sharply across the run. This evidence accepted the redesign while
also showing that later-world costs lay outside minor actors.

An older Godot 4.6.2 Mono headless UI comparison at tick 10,000, seed 4200,
and 180 projected entities measured:

| Metric | Old UI | Optimized UI | Change |
| --- | ---: | ---: | ---: |
| Mean reported FPS | `36.2` | `53.1` | `+46.5%` |
| Process CPU, one-core scale | `172.2%` | `134.8%` | `-21.7%` |
| Resident memory | `858,424 KiB` | `845,176 KiB` | `-1.5%` |

That comparison established the value of batched entity drawing, incremental
projection invalidation, draw gating, and overlay culling. It used Debug and
the dummy renderer, so it is historical UI evidence, not a current GPU or
kernel budget.

## Strategy Decisions

The performance goal remains a material `5x` to `10x` improvement against an
explicitly named equivalent workload. No current result is represented as
having reached that goal.

Current strategy, in priority order:

1. Reduce executed work and repeated queries on the one-worker reference path.
   Prefer direct indexes, dense IDs, due/active/dirty sets, bounded
   memoization, and simpler gameplay rules when an accepted design can remove
   whole classes of work.
2. Reprofile after each material accepted reduction. Percentages from an older
   trace are not a queue of implementation tasks after their callees change.
3. Keep the typed-owner and persistent-worker infrastructure only where it
   preserves the one-worker path and explicit mutation ownership. Do not expose
   multi-worker execution as a production speed feature.
4. Reconsider parallel execution only around a much larger independent unit of
   work. A viable proposal must amortize dispatch and publication across more
   than one fine-grained tick phase and must measure end-to-end process
   throughput, not worker-loop utilization.
5. Treat eventual or statistical consistency as an available gameplay design
   choice, not a requirement. If revisited, design the observation and conflict
   model first; do not incrementally weaken the deterministic tick contract.

Persistent geographic actors remain rejected as the universal scheduler:
relationship and gambit traffic is nonlocal and entity density is spatially
skewed. Cell ownership remains appropriate for cell state. A hybrid may still
combine stable-ID evaluation with entity, relationship, and cell destination
owners, but the existing fine-grained barriers have already failed their
performance gate.

## Next Evidence

Before another optimization:

1. rerun the 10,000-tick CPU trace on the accepted current source;
2. run a clean 20,000- or 25,000-tick current-source comparison and report
   fixed windows, especially the final window;
3. rank remaining work from the refreshed trace; and
4. require a causal work-removal hypothesis before changing gameplay or adding
   scheduler complexity.

The earlier trace suggests movement reach/path planning is the largest
remaining discrete algorithm candidate, while low-altitude and effects work
still dominate in aggregate. That ranking is provisional until the refreshed
trace accounts for the accepted direct-cell and locality-cache changes.

If movement remains material, the next routing design is portal-aware
hierarchical planning rather than another local cache around segment pairs.
The current 5/10-hex abstraction can select a plausible segment and portal that
is incoherent with the actor's actual entry coordinate and final target,
including routing an actor away from a target inside the same coarse region.
The design must cover concrete boundary-portal generation and pruning, cache
keys, road-mark influence, invalidation cadence, unreachable routes, and the
cost of reachability queries. This is a gameplay-visible routing correction and
algorithm replacement, so it requires design approval and a short movement
usability run in addition to throughput evidence.

Performance acceptance always includes `make ci`. Behavioral or statistical
Monte Carlo evidence is additionally required when an optimization changes the
simulation rather than merely its implementation.
