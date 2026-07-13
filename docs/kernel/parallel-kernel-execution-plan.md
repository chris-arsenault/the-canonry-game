# Parallel Kernel Execution Plan

Status: approved for implementation on `kernel-parallel-execution`.

This plan replaces the single-writer simulation model with bounded,
bulk-synchronous parallel execution. Before/after output equivalence with the
legacy sequential kernel is not a requirement. Memory safety, explicit
conflict semantics, fixed/bounded hot-path storage, structurally valid world
state, and repeatability of the new execution model remain requirements.

## Decisions

- The simulation worker count is capped at 16, even on larger machines.
- `Engine.Kernel.Driver` owns worker threads and their lifetime.
- `Engine.Kernel` owns phases, shard ownership, parallel-safe random streams,
  mutation records, conflict reduction, and state application.
- Workers read a stable phase epoch. Mutations become visible at a barrier,
  deliberately replacing actor-order-dependent same-loop visibility.
- Parallel decisions use counter-based random cursors keyed by seed, tick,
  phase, program, actor, purpose, and draw index.
- State writes are partitioned by logical owner. Shared destination mutations
  are routed to the destination shard instead of protected by fine-grained
  locks.
- Events are accumulated in per-worker primitive buffers and merged at a
  barrier.
- Rare structural work may remain coordinator-driven until it is measured as a
  bottleneck. This does not prevent entity, cell, actor, and relationship state
  from being applied concurrently by their owning shards.

## Target Tick Shape

1. Drain commands and publish a readable world epoch.
2. Evaluate actor decisions and movement plans in parallel.
3. Route, reduce, and apply entity/cell mutations by shard.
4. Rebuild activity, signals, spatial indexes, and culture-derived fields in
   parallel.
5. Evaluate effects, relationships, and gambits in parallel.
6. Route, reduce, and apply graph/entity mutations by shard.
7. Run rare structural generators and culture slots.
8. Merge worker events and complete the tick.

## Conflict Rules

- Additive attribute, influence, road, and relationship mutations are summed
  and clamped once.
- Competing absolute writes resolve by phase priority, action priority, and a
  counter-random tie break.
- Entity death wins over other mutations in the same phase.
- One accepted movement intent is applied per actor per phase.
- Competing coverage claims resolve by claim score and counter-random tie break.
- Duplicate relationship creates coalesce by `(source, target, kind)`.
- A cull of an existing relationship wins over updates to that relationship in
  the same phase.
- New entity and relationship IDs are assigned from barrier-time prefix-sum
  reservations so workers populate disjoint ranges.
- Event merge order is phase, shard, then worker-local sequence.

## Implementation Phases

### Phase 1: execution foundation

- Add the kernel scheduler/job contracts and inline scheduler.
- Add the fixed reusable worker pool to `Engine.Kernel.Driver`.
- Add worker-count, chunk-size, and shard-count options with a hard maximum of
  16 workers.
- Add counter-based random cursors for parallel decisions.
- Add reusable per-worker scratch, event, and mutation buffers.
- Wire scheduler ownership and disposal through `Engine.Host`.

### Phase 2: parallel derived culture fields

- Replace lazy shared culture cache rebuilds with barrier-published derived
  epochs.
- Drain dirty culture cells and materialize neighborhood values in parallel.
- Build independent culture/plane component caches concurrently.
- Make hot `CellAt` reads side-effect-free against the published epoch.

### Phase 3: parallel actor decisions

- Split minor-actor evaluation from action application.
- Split low-altitude verb selection and movement planning from application.
- Give each worker independent verb, minor-actor, pathfinding, gambit, and
  expression scratch.
- Convert effects, gambit evaluation, and relationship maintenance to
  read-phase jobs producing primitive mutations.

### Phase 4: sharded multi-writer application

- Partition entity and minor-actor state by contiguous ID ranges.
- Partition primitive tables and map layers by destination owner.
- Batch and concurrently apply cell, attribute, road, and influence mutations.
- Batch relationship creation, strength updates, culls, and endpoint-index
  maintenance.
- Reserve structural IDs before concurrent population.

### Phase 5: phase graph and integration

- Replace sequential `TickSystems` execution with the approved phase graph.
- Preserve the external `IKernel.AdvanceTicks` contract.
- Wire worker configuration into Monte Carlo and hosted runtime startup.
- Bump snapshot schema where the stored logical state changes; do not add an
  obsolete-snapshot compatibility layer.
- Update stable architecture and performance documentation.

## Intended Files

New kernel infrastructure lives under `src/Engine.Kernel/Parallel/`, including
the scheduler/job contracts, phase model, random cursor, shard layout, mutation
records, buffers, reducers, and inline scheduler. Partitioned primitive tables
live in `src/Engine.Kernel/`.

The reusable worker implementation lives in
`src/Engine.Kernel.Driver/FixedKernelWorkerPool.cs` and
`src/Engine.Kernel.Driver/KernelWorker.cs`.

The primary modified runtime files are:

- `src/Engine.Kernel/Simulation.cs`
- `src/Engine.Kernel/SimulationOptions.cs`
- `src/Engine.Kernel/WorldState.cs`
- `src/Engine.Kernel/WorldState.MapLayers.cs`
- `src/Engine.Kernel/WorldState.RelationshipIndexes.cs`
- `src/Engine.Kernel/MapLayerSet.cs`
- `src/Engine.Kernel/SpatialLayerIndex.cs`
- the culture-boundary, low-altitude verb, minor-actor, traversal, effects,
  gambit, and relationship program runtimes
- `src/Engine.Host/KernelHostBuilder.cs`
- `src/Engine.Host/KernelHost.cs`
- `src/Engine.Host/KernelRuntimeFactory.cs`
- `tools/MonteCarlo/MonteCarloCli.cs`

Tests will cover the worker pool, phase barriers, counter-based random streams,
mutation conflict rules, sharded tables, relationship indexes, snapshot state,
deadlock/exception propagation, and same-seed repeatability of the new model.

## Verification Gates

- Use repository-level `make test` during implementation and `make ci` for the
  final verification pass.
- Run one 10k worker sweep at 1, 2, 4, 8, and 16 workers.
- Profile the best useful worker count with `dotnet-trace` and OS thread CPU.
- Run one registered 25k full Monte Carlo evidence batch.
- Run one registered 100k simulation-only confirmation after the architecture
  stabilizes.
- Do not repeat 100k solely for confidence.

Performance targets are at least eight concurrently hot simulation workers in
actor-heavy phases, at least 1.75x throughput improvement at 25k, and at least
2x throughput improvement at 100k. A missed target requires trace-backed
attribution; it does not justify unsafe races or unbounded queues.

## Out Of Scope

- Preserving exact output from the sequential kernel.
- Changing Ice Remembers authored rules or domain content.
- Introducing strings, LINQ, I/O, logging, or avoidable allocations into the
  kernel hot path.
- Running multiple Monte Carlo simulations concurrently during this tuning
  pass.
- Saturating all machine cores when the active world does not contain enough
  independent work.
