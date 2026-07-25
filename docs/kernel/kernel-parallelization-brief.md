# Kernel Parallelization Brief

Status: historical pre-Phase-4 briefing for external architecture discussion,
not current architecture or an approved implementation plan. The current
direction and completed groundwork are recorded in
`pre-parallelization-simulation-redesign-plan.md`.

Code basis: `main` at `c81d507` on 2026-07-24. The earlier parallel experiment
is preserved separately at `origin/kernel-parallel-execution` commit
`f556332`. That branch forked from `main` at `876b525` and is 203 mainline
commits behind this briefing's code basis.

## Executive Summary

The current kernel is a synchronous, deterministic, single-writer simulation.
One driver thread calls `Simulation.AdvanceTicks`; each tick drains commands,
runs every due program in registration order, and emits a terminal
`TickCompleted` event. Programs mutate live state immediately. A mutation
usually changes one or more fixed arrays and indexes, increments one global
world version, appends provenance, and writes a primitive event before the
calling program continues.

There are two relevant clock scales:

- a raw simulation tick, on which minor actors run;
- a main graph pulse every five raw ticks by default, on which most named
  entity systems run.

The main graph pulse is not a set of independent systems. It is an ordered
pipeline with both same-pulse and previous-pulse dependencies. It also contains
order-sensitive global random draws and ascending-ID loops whose earlier
mutations are visible to later iterations.

The July 13 experiment parallelized evaluation and path planning while replaying
mutations serially. Its best registered 10k result was 308.60 ticks/s with 16
workers versus 254.26 with one worker: `1.214x`. Eight workers were already at
`1.204x`; two workers were slower than one. The branch concluded that another
round of worker-local scratch and parallel decision loops would not plausibly
reach `2x`. The remaining boundary was mutation, index, provenance, version,
and event application, plus map activity/signals and repeated barriers.

The old throughput numbers are not a current baseline. The latest durable
mainline evidence found for the newer, denser simulation is the July 24
`stages-3x100k-20260724b` batch at 15.8/20.1/39.4 ticks/s. A separate
`stages-perf-probe-1x100k-20260724` simulation-only probe ran at 17.1 ticks/s
for seed 9100. That finding attributed much of the point-in-time slowdown to
excess entity density, not to a fresh CPU profile. A new parallelization design
should begin with a current fixed-seed simulation-only trace and worker-free
baseline.

## Runtime Boundary

The runtime stack is:

```text
host / tool
  -> stepped, realtime, or unpaced driver
    -> IKernel.AdvanceTicks
      -> Simulation.AdvanceTicks
        -> WorldState + program-owned state
          -> KernelEventLog
            -> independent projection / telemetry consumers
```

`Engine.Host` compiles and installs domain configuration before the run.
`Engine.Kernel.Driver` owns the thread that advances the kernel.
`Engine.Kernel` owns all simulation state and mutation. Consumers read primitive
events outside the kernel and do not define simulation behavior.

The kernel project deliberately forbids runtime strings, LINQ, common heap
collections, tasks, thread creation, logging, and I/O. State is primarily
preallocated arrays and fixed-capacity primitive tables. If workers are
introduced without changing that boundary, worker lifetime and OS threading
belong in `Engine.Kernel.Driver`; the kernel can expose synchronous job and
phase contracts without owning threads. This is the arrangement the July
experiment used.

The repository currently codifies one simulation writer as an architecture
rule. The July experiment proceeded under an explicit exception: it allowed a
new deterministic multi-writer model and did not require exact equivalence with
the old sequential history. A future multi-writer proposal needs to make that
policy decision explicit again; it should not silently treat the experimental
exception as current mainline architecture.

## Exact Tick Entry Sequence

`Simulation.AdvanceTicks(count)` establishes the writer thread and repeats:

```text
1. Check cancellation.
2. Evaluate the optional stop predicate against current tick/version.
3. Increment CurrentTick.
4. World.BeginTick(CurrentTick).
5. Grow eligible provenance storage if its periodic threshold check is due.
6. Drain and apply every queued command.
7. Walk registered systems in ascending registration slot:
     skip disabled systems;
     skip systems whose cadence does not divide CurrentTick;
     enter the program's execution/provenance context;
     call system.Tick(simulation, CurrentTick).
8. Append TickCompleted(CurrentTick, WorldVersion).
```

There is no general begin-phase snapshot, intent queue, reduction stage, commit
stage, or rollback. A program reads the world left by all earlier work and
writes directly into the world observed by all later work.

Commands precede every program. Current commands can enable or disable gambits
and generators, add culture influence around a cell, or favor an entity.
Command effects are therefore visible to all due programs in that tick.

## Clock And Program Order

`MinorActorMainTickRatio` defaults to `5`. In the table below, `M` means that
ratio, so the default main cadence is raw ticks `5, 10, 15, ...`.

| Order | Program | Default cadence | What its `Tick` does | Important visibility |
| ---: | --- | ---: | --- | --- |
| 1 | Movement | `M` | Reconciles local road marks at raw ticks divisible by 10 and regional marks at ticks divisible by 25. Actor movement itself is invoked by other programs. | Runs before entity travel. |
| 2 | LowAltitudeVerbs | `M` | Ascending active-entity loop: initialize influence, validate/choose one committed intent, then apply movement, culture, claim, relocation, or relationship-strength steps. | Intentionally reads the previous main pulse's map activity and signals. Its writes are visible to every later program and later entity in its own loop. |
| 3 | MapActivity | `M` | Clears current occupancy, scans active entities, and records physical and semantic presence, visit counts, last-visit ticks, and decayed heat. | Sees low-altitude movement from this pulse. |
| 4 | MapSignals | `M` | Clears signal tables; emits kind fields; emits relationship fields; computes per-entity capacity, support, threat, crowding, comfort, and attractor values. | Reads same-pulse MapActivity and current relationships/positions. Effects later read these values. |
| 5 | CultureFieldDecay | `5M` | For each culture and plane, snapshots indexed culture cells into scratch, then decays/culls them. | Runs after signals, so same-pulse map signals do not include this decay. Boundary queries after it see the changed raw field. |
| 6 | CultureBoundaries | `M` | Its registered `Tick` is currently a no-op. Actual boundary/component work happens lazily in `CellAt` and related queries, with invalidation on culture writes. | This is a cache service in the program list, not a materialization barrier. |
| 7 | Relationships | `M` | Ascending relationship loop: update contact memory, sum maintenance deltas, clamp/cull, then move follow-governed targets to their sources. | Reads positions and culture after low-altitude/decay. Follow movement occurs after MapActivity/Signals, so those derived fields retain the target's earlier position until the next main pulse. |
| 8 | Effects | `M` | Ascending active-entity loop: apply attribute rules, relationship-derived tags, stage ambient culture emission; then apply district health/culture bonuses. | Reads current relationships/culture and same-pulse map signals. Writes attributes, tags, and culture before upkeep. |
| 9 | Upkeep | `M` | Ascending active-entity loop: apply authored attribute drain, including randomized lifecycle drain, and mark zero-health entities historical. | Effects can heal or harm before death is decided. Later strategic systems do not act with entities retired here. |
| 10 | Gambits | `M` | Ascending active-entity loop: evaluate weighted gambit/target candidates, draw from the global RNG, roll success, apply ordered effects, set cooldowns, append provenance/events, and queue a chronicle execution notice. | Earlier actors can change eligibility for later actors in the same loop. |
| 11 | ChronicleDirector | `M` | Consume pending gambit/generator notices, decay and step active chronicles, then try to cast new ones. | Consumes this pulse's gambit notices. Generator notices are produced later and wait until the next main pulse. |
| 12 | Generators | `M` | Tick independent cadence generators; refresh province bands when due; evaluate provincial pulse pools; select targets; create entities, relationships, coverage, map effects, and provenance bundles. | Sees all earlier low-altitude, lifecycle, gambit, and chronicle state. Performs rare but broad structural mutation. |
| 13 | WorldBeats | `25M` | Scan population/relationship/culture/chronicle facts, close/open ages, and push age charge multipliers into other program runtimes. | Runs after the main actions at that tick, so new charges affect later pulses rather than earlier work in the same pulse. |
| 14 | MapFrontier | `25M` | Read the newly recognized world age; thaw/refreeze physical terrain cover and grow the semantic frontier. Physical arrival reveal is also called synchronously from movement outside this `Tick`. | Can perform structural map changes after generators and world-beat recognition. |
| 15 | CultureSlots | `10M` | For each authored culture slot, scan eligible entities, score the best candidate, apply hysteresis/tenure, and change occupant benefits. | New benefits are visible to Knowledge and MinorActors later in the raw tick. |
| 16 | Knowledge | `10M` | Clear and recompute culture claims and multiplicative modifiers from semantic knowledge sites. | Main programs on this tick used the previous knowledge table; MinorActors use the newly rebuilt table. |
| 17 | MinorActors | `1` | Increment the minor pulse; emit cohorts from eligible named sources; ascending actor loop selects/applies work, moves legacy actors, decrements TTL, and expires cohorts. | Always last. On non-main raw ticks it is the only default simulation program. It accumulates culture, road, and lifecycle work for the next main pulse. |

With defaults:

- ticks not divisible by 5 run commands and MinorActors;
- ticks divisible by 5 run the main pipeline and then MinorActors;
- ticks divisible by 25 also run culture decay;
- ticks divisible by 50 also run CultureSlots and Knowledge;
- ticks divisible by 125 also run WorldBeats and MapFrontier.

The stable document `docs/systems/05-tick-clock.md` predates several additions
and omits CultureFieldDecay, ChronicleDirector, WorldBeats, MapFrontier, and
Knowledge. `Simulation.RegisterDefaultPrograms` is the source of truth for the
current list.

## The Main High-Frequency Processes

### Low-altitude named-entity behavior

Each active entity has one committed verb intent in fixed per-entity arrays.
An entity with no usable intent enumerates authored rules for its kind, searches
eligible entity/cell/relationship targets, computes routine and opportunity
weights, and consumes global random draws to choose a verb and target. It then
commits duration, dwell, and step timing.

The same ascending entity loop immediately applies the intent. Movement can run
hierarchical pathfinding and updates position plus spatial occupancy indexes.
Other steps can add or remove culture mass over several cells, change coverage,
relocate an entity, or change an existing relationship. Those changes affect
target scoring for later entities unless a specific read is declared
previous-pulse.

The runtime has shared candidate arrays and movement scratch. Any concurrent
evaluation needs worker-owned scratch or a read-only precomputed structure.

### Map activity and signals

MapActivity is a full active-entity aggregation into open-addressed primitive
tables. Current occupancy is cleared each main pulse; visit counts and heat are
persistent.

MapSignals is another aggregation:

1. clear all current signal tables;
2. scatter kind capacity/support/threat fields around active entities;
3. scatter support/threat from both endpoints of active relationships;
4. gather cell fields and activity around every active entity;
5. store per-entity signals and scatter attractor mass back over cells.

This is naturally bulk work, but the current destination tables are not
multi-writer containers. Entity partitions alone do not prevent different
workers from adding to the same cell. A parallel version must choose an
ownership or reduction model for destination cells rather than add locks around
the existing hash tables.

### Culture field and boundary cache

Raw culture influence is durable world/map state. Each write updates source
layers and aggregate influence/share, increments the world version, emits an
event, and calls `CultureBoundaries.MarkDirty`.

Boundary data is not rebuilt by the registered CultureBoundaries tick.
`CellAt` lazily calls `EnsureCache`, which may rebuild a culture/plane component
cache. The cache also computes neighborhood mass and scans all cultures at the
queried cell for the strongest rival. The runtime has 16 direct-mapped cache
slots.

Consequently, a world epoch being otherwise read-only is not sufficient for
safe parallel queries: boundary caches must be published before workers or made
worker/shard-owned. The July branch added a `PrepareParallelReads` gate for this
reason.

### Relationship maintenance and graph indexes

Relationship scalar fields are structure-of-arrays indexed by relationship ID.
Maintenance evaluation is mostly relationship-local, but commit is not:

- strength changes advance the global world version and emit provenance/event
  records;
- culls update active/end state and may remove an edge from the delta graph;
- follow relationships can compete to move the same target entity;
- creates allocate the first free global relationship ID and insert outgoing
  and incoming graph entries.

Graph queries merge a compacted frozen CSR with a live delta structure. Delta
insertion/removal shifts relationship entries and updates all later row
offsets; a threshold can trigger a full graph compaction. Those operations are
global structural writes, not independent per-relationship array stores.

### Effects and upkeep

Effects and upkeep scan active entities in ascending ID order. Most resulting
attribute/tag writes are entity-owned and could be evaluated by entity
partition, but every applied mutation still updates global version,
provenance, and the event stream. Effects can also write shared culture cells,
and district effects target host entities independently of the normal entity
loop.

Upkeep consumes the global RNG for per-entity lifecycle variance. Parallel
evaluation cannot retain the current order-dependent xorshift stream without
either serial draw assignment or a defined counter/keyed random model.

### Gambits, chronicles, and generators

Gambit evaluation has substantial entity/target scanning work, but successful
effects immediately change the graph and attributes seen by later entities.
Gambit cooldown tables, the global RNG, provenance scopes, chronicle pending
queue, and structural effects are shared.

Chronicles are small, globally coupled narrative state. Generators are less
frequent and perform the broadest structural transactions: target selection,
ID allocation, entity and relationship creation, placement, coverage, cleanup,
provenance, and event bundles. They are reasonable serial candidates until a
profile says otherwise, but their barriers and output rules still need to fit
the tick's chosen concurrency semantics.

### Minor actors

Minor actors run every raw tick and have two passes:

1. scan named sources and emitter rules, possibly allocate new actor slots;
2. scan active actors, select a bounded local verb target, write culture/road/
   lifecycle consequences, update position/cadence/TTL, and expire actors.

Actor state stores integer fields and `FixedScalar` values in bounded primitive
tables. Source active counts and emission clocks are also shared tables.
Different actors frequently target the same culture cell or named source, so
actor ownership alone does not make action application independent.

The July branch converted actor evaluation to worker jobs and experimented with
direct per-actor arrays, but that storage rewrite never reached main.

## Mutable State And Coupled Commit Work

### Entity state

World entities use direct arrays for allocated/status/kind/subtype/culture,
positions on both planes, physical elevation, home anchors, influence
production, favor, timestamps, tags, and linked kind/culture indexes.
Attributes and traits use fixed open-addressed tables.

Physical and semantic position changes also edit per-cell linked occupancy
indexes. Moving distinct entities can still conflict when their source or
destination cell head/link structures overlap.

### Map state

Terrain, terrain cover, superregion, continent, coverage, influence, culture
source layers, and derived aggregate layers use `PrimitiveIntTable`,
`PrimitiveFloatTable`, `MapLayerSet`, and `SpatialLayerIndex`.

The primitive tables use linear probing plus a global count and occupied-slot
array. Concurrent inserts are unsafe even for logically different keys.
Spatial indexes maintain global entry/page allocation and per-value linked
pages. These require explicit partitioning, per-worker accumulation, or a
redesigned merge.

### Program state and scratch

Some program state is cleanly keyed by entity or relationship ID, but many
runtimes also own:

- global high-water marks and ID counters;
- fixed hash tables with shared insertion metadata;
- global reusable candidate/path/query scratch;
- direct-mapped lazy caches;
- pending queues and global pulse counters;
- derived tables cleared and rebuilt as one unit.

Classifying a loop as read-only requires checking these hidden writes as well
as `WorldState`.

### Mutation side effects

A typical public simulation mutation performs:

```text
validate
  -> update primary scalar/array
  -> update secondary indexes or aggregate layers
  -> advance global WorldVersion
  -> append provenance output when in a provenance scope
  -> append a contextual primitive KernelEvent
  -> invalidate/update derived caches where relevant
```

These are synchronous parts of the current observable contract. The event log
supports concurrent readers but `TryWrite` currently reserves its sequence with
a non-atomic single-producer update. It must not be treated as a ready-made
multi-producer queue.

WorldVersion is a mutation sequence, not merely a tick number. Projection and
command staleness behavior consume it. A parallel design must define whether
versions remain a total order, become barrier/epoch versions, or gain a
composite phase/shard identity. The same choice affects event ordering and
provenance linkage.

## Current Determinism And Ordering Sources

Repeatability currently comes from:

- strict program registration order;
- ascending entity, relationship, slot, generator, and actor loops;
- first-free entity/relationship/minor-actor allocation;
- one global xorshift random stream consumed in call order;
- immediate same-loop mutation visibility;
- one global mutation/world-version sequence;
- one event sequence in mutation order;
- snapshot persistence of world, program state, and random state.

These are coupled. Changing only the RNG does not define conflicts between
simultaneous movement, culture deposits, relationship creates, death, or
coverage claims. Conversely, defining reductions without defining event and ID
order leaves projections and replay nondeterministic.

Exact legacy history and same-seed repeatability are separate requirements:

- exact legacy equivalence requires reproducing the old call-order random draws
  and same-loop visibility;
- repeatability of a new parallel model only requires the new phase, conflict,
  allocation, and random rules to produce the same result for the same inputs
  regardless of worker scheduling.

The July experiment explicitly dropped exact legacy equivalence while retaining
repeatability as a goal.

## July 13 Parallel Experiment

### Branch and implementation

The retained branch contains two commits:

- `40c4c24` — documented the bulk-synchronous, sharded direction;
- `f556332` — checkpointed the experimental implementation.

It added:

- a driver-owned fixed worker pool, capped at 16 workers;
- a synchronous `IKernelWorkScheduler.Run(job, range, minimumChunkSize)` API;
- contiguous static range partitioning;
- one semaphore wake and completion barrier per scheduler run;
- counter-keyed random cursors using seed/tick/phase/program/subject/purpose;
- worker-local movement/query scratch;
- per-entity/per-relationship decision arrays;
- parallel evaluation in low-altitude verbs, movement planning, minor actors,
  gambits, effects, relationship maintenance, and culture-cache preparation;
- primitive mutation buffers for part of the effects path;
- Monte Carlo worker-count configuration and provenance.

Low-altitude work illustrates the shape:

```text
serial prepare/scan
  -> parallel intent evaluation
  -> serial intent commit
  -> serial prepare/scan
  -> parallel movement planning
  -> serial movement and other action application
```

Comparable evaluate-then-commit splits were used elsewhere. The coordinator
continued to own world mutation, indexes, global IDs, provenance, versioning,
and event publication.

### Registered scaling result

The completed fixed-seed 10k sweep recorded:

| Run ID | Workers | Ticks/s | Speedup |
| --- | ---: | ---: | ---: |
| `parallel-final-w1-10k-20260713` | 1 | 254.26 | `1.000x` |
| `parallel-final-w2-10k-20260713` | 2 | 248.22 | `0.976x` |
| `parallel-final-w4-10k-20260713` | 4 | 268.26 | `1.055x` |
| `parallel-final-w8-10k-20260713` | 8 | 306.20 | `1.204x` |
| `parallel-final-w16-10k-20260713` | 16 | 308.60 | `1.214x` |

An Amdahl-style fit to the 8/16-worker endpoints implies an effective
non-scaling share around 81%. This is not a measured source-code serial
fraction: it combines true serial work, worker launch/barrier cost, load
imbalance, cache effects, and any work added by the parallel representation.
It does accurately summarize the flat part of the observed curve.

The branch's own pause finding was that reaching `2x` required moving map
activity/signals and mutation/event application away from coordinator replay
and into shard-owned phase commits.

### What remained incomplete

The checkpoint was deliberately not merged:

- it was below the target speedup;
- final repository CI was not run;
- all 298 kernel tests had passed before the last minor-actor storage rewrite,
  but an aggregate test run failed in another suite after that rewrite and was
  not diagnosed;
- the stabilized post-rewrite 10k sweep and planned 25k/100k evidence runs were
  not completed;
- mainline has since changed by more than 200 commits.

Treat the branch as evidence and a code sketch, not as a patch to revive or
cherry-pick wholesale.

## Why The First Attempt Flattened

The observed result is consistent with these concrete boundaries:

1. **Evaluation was not the whole tick.** Candidate scoring and path planning
   ran in workers, but coordinator scans, state preparation, action application,
   index updates, versioning, provenance, and events remained serial.
2. **Several parallel regions were short.** Each scheduler call woke workers,
   partitioned a range, and waited at a barrier. The low-altitude pass alone had
   separate intent and movement jobs plus serial passes between them.
3. **Destination conflicts were deferred, not removed.** Cell culture, roads,
   map aggregates, attributes, relationship topology, positions, and events
   still converged through serial replay.
4. **MapActivity and MapSignals remained serial aggregation/scatter passes.**
   These are recurring whole-population work and need destination ownership or
   reduction to scale.
5. **The active world was not huge at 10k.** The historical evidence had only
   hundreds of active entities/actors, so fixed barriers and uneven expensive
   path/culture queries were material.
6. **Lazy/shared structures narrowed safe reads.** Culture components,
   movement scratch/cache, primitive hash tables, graph traversal scratch, and
   the global RNG all required preparation, duplication, or serialization.
7. **Rare structural systems were not the only serial work.** Leaving
   generators serial was reasonable; leaving the common commit path serial was
   the limiting choice.

## Decisions An Outside Design Should Make Explicitly

Before evaluating code, the discussion should settle:

1. **Semantic compatibility**
   - Is exact output equivalence with the sequential kernel required?
   - Is same-seed repeatability under the new model required?
   - May within-phase actor order change world outcomes?

2. **Writer model**
   - Keep one state writer and seek coarse snapshot/evaluate/reduce batches?
   - Permit shard-owned multi-writer state at barriers?
   - Which current repository rule/document changes would that require?

3. **Phase graph**
   - Which current same-pulse dependencies are essential?
   - Which can deliberately become previous-phase or previous-pulse reads?
   - How many barriers per main pulse are affordable?

4. **Ownership**
   - Are entities, relationships, cells, or destination keys the primary
     shards?
   - How are cross-shard movement, relationship endpoints, culture deposits,
     and coverage claims routed?

5. **Conflict rules**
   - additive cell/attribute/strength deltas;
   - competing absolute writes and movement;
   - death versus healing/action;
   - duplicate relationship creation and cull/update races;
   - structural entity/relationship/cell allocation;
   - event and provenance ordering.

6. **Randomness**
   - serial preassigned draws;
   - counter-based streams keyed by stable logical action;
   - shard streams with deterministic merge;
   - whether a rejected/invalid action consumes random positions.

7. **Derived state**
   - publish immutable activity/signal/culture epochs;
   - reduce worker-local fields into destination shards;
   - replace lazy cache mutation during queries;
   - decide when spatial and relationship indexes become visible.

8. **Observable ordering**
   - total or epoch-based WorldVersion;
   - one merged event order or per-shard sequences;
   - how projections know a phase/tick is complete;
   - how snapshots capture a barrier-consistent state.

9. **Granularity and evidence**
   - current population/work-item counts at 10k, 25k, and 100k;
   - current simulation-only CPU trace;
   - phase wall time and worker utilization without adding permanent hot-path
     telemetry;
   - performance targets at a named horizon and worker count.

## Useful Design Seams, Not Yet A Proposal

The present code exposes several plausible seams for discussion:

- entity-owned evaluation for low-altitude selection, effects, upkeep, gambits,
  and per-entity signal gather;
- relationship-owned maintenance evaluation;
- actor-owned minor-actor evaluation;
- destination-cell aggregation for map activity, signals, culture, and roads;
- a rare serial structural lane for generators, culture slots, world beats, and
  map growth;
- barrier-published culture components and map-signal epochs;
- barrier-time deterministic allocation and event/provenance merge.

Whether these should become one phase graph, several coarse jobs, or a sharded
world is the design question. The previous branch already demonstrated that
parallelizing only the first three evaluation categories while leaving the
destination and commit path serial is insufficient.

## Verification Expectations

Any approved implementation should preserve the repository's normal gates:

- use `make test` during development and `make ci` for final verification;
- do not run test projects in parallel;
- benchmark Release builds through registered Monte Carlo runs with unique
  human-readable run IDs;
- do not use `--no-build` for Monte Carlo evidence;
- profile the real fixed-seed simulation-only workload with `dotnet-trace`;
- record important ad hoc findings through the Monte Carlo CLI;
- compare 1/2/4/8/16 workers only after a one-worker path represents the same
  new execution model;
- test repeatability across worker counts and repeated executions if that is an
  accepted invariant;
- separately validate behavior, graph/index integrity, snapshot consistency,
  event/provenance completeness, deadlock/exception propagation, and
  throughput.

Independent Monte Carlo runs remain the easiest coarse parallelism because they
share no state. That can improve analysis throughput, but it does not solve
single-run tick latency and should not be reported as kernel scaling.

## Primary Source Map

Current main:

- `src/Engine.Kernel/Simulation.cs` — construction, registration order,
  cadences, `AdvanceTicks`, writer assertion, system loop.
- `src/Engine.Kernel/Simulation.Commands.cs` — pre-program command drain.
- `src/Engine.Kernel/WorldState.cs` — entity/relationship state and mutations.
- `src/Engine.Kernel/WorldState.EntityPosition.cs` — position plus occupancy
  index mutation.
- `src/Engine.Kernel/WorldState.RelationshipIndexes.cs` — frozen/delta graph
  and version/event plumbing.
- `src/Engine.Kernel/WorldState.MapLayers.cs` and
  `Simulation.CultureInfluence.cs` — map/culture mutation and invalidation.
- `src/Engine.Kernel/MapLayerSet.cs`, `SpatialLayerIndex.cs`,
  `PrimitiveIntTable.cs`, `PrimitiveFloatTable.cs` — shared storage shapes.
- `src/Engine.Kernel/RandomSource.cs` — global order-dependent RNG.
- `src/Engine.Eventing/KernelEventLog.cs` — single-producer event append and
  concurrent consumer cursors.
- `src/Engine.Kernel/Programs/*/*ProgramRuntime*.cs` — program internals.
- `docs/kernel/simulation-kernel-discipline.md` — current kernel constraints.
- `docs/methodology/performance-testing.md` — profiling protocol.
- `docs/analysis/stages-scorecard-round1-20260724.md` — latest durable
  point-in-time 100k performance finding.

Historical experiment:

- `origin/kernel-parallel-execution:docs/kernel/parallel-kernel-execution-plan.md`
- `origin/kernel-parallel-execution:src/Engine.Kernel/Simulation.Parallel.cs`
- `origin/kernel-parallel-execution:src/Engine.Kernel/Parallel/`
- `origin/kernel-parallel-execution:src/Engine.Kernel.Driver/FixedKernelWorkerPool.cs`
- commit `f556332`
