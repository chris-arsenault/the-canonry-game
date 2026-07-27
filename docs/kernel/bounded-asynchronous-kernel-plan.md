# Bounded Asynchronous Typed-Owner Kernel Plan

Status: active implementation plan.

Rollback checkpoint:
`pre-async-deterministic-kernel-20260727` at commit `fc02ebd7`.

This plan supersedes the scheduler choice made by Phase 9 of
`pre-parallelization-simulation-redesign-plan.md`. It retains the completed
storage, fixed-point, frozen-view, typed-delta, occurrence-identity, and
strategic-intent groundwork. It does not retain the assumption that every
owner must publish the same tick together or that worker counts must produce
identical histories.

## Decision

The kernel will be:

- exactly safe within each authoritative owner;
- eventually consistent across owners;
- statistically consistent across schedules and physical worker counts; and
- globally convergent only when a consumer explicitly requires a cut.

Ticks remain the authored unit of time. There are no pulses, subticks, or
worker-specific gameplay rates. An owner may advance ahead of the global low
watermark only within a bounded lead window.

## Hard Contracts

- Each active entity slot, relationship slot, cell, mutable index partition,
  and program-state partition has one logical owner.
- Logical ownership is fixed independently of physical worker count.
- Reassigning logical partitions to physical workers does not change whether an
  effect is local or cross-owner.
- Cross-owner mutation uses bounded primitive messages. Mutating messages are
  never silently dropped.
- Generation-safe occurrence handles reject messages for retired or reused
  slots.
- Fixed capacities, fixed-point field contracts, no hot-path allocation,
  primitive events, and explicit overflow behavior remain kernel requirements.
- Saving, final Monte Carlo capture, deterministic debugging, and topology
  reassignment may request an explicit convergence cut.

## Relaxed Contracts

- Owners do not have to enter or finish a tick simultaneously.
- Remote reads may observe a boundedly stale published view.
- Cross-owner effects become visible on a receiver-owned tick, not necessarily
  the source tick.
- Worker scheduling, message arrival order, and worker count may change an
  individual history.
- There is no globally meaningful mutation sequence or `WorldVersion`.
- Events require owner tick, owner identity, and owner-local sequence; they do
  not require a hot-path global sort.
- Validation compares exact structural invariants and statistical simulation
  shape rather than byte-identical worlds across worker counts.

## Runtime Shape

The runtime uses more logical owner partitions than physical workers. A
physical worker owns a set of entity, relationship, cell, and strategic
partitions and continuously repeats:

1. drain its owned inboxes;
2. apply authoritative local mutations;
3. advance runnable partitions through their next local tick;
4. evaluate due local work;
5. coalesce and send cross-owner messages;
6. publish owner-local views and watermarks; and
7. continue without returning to a per-tick coordinator.

Logical partitions use the same mailbox protocol even when their current
physical assignment is the same worker. This keeps cross-owner timing semantics
independent of worker count.

Workers may run ahead of the low watermark only within the configured lead
window. A worker that reaches the limit drains messages and yields until lagging
owners advance. Threads park only when no assigned partition is runnable; they
are not awakened and joined for every tick or phase.

## Gameplay Semantics

- The canonical calibration remains 50 ticks per in-world year.
- Lifecycle targets, cooldowns, commitments, tenure, and authored cadences stay
  in ticks and are not retuned merely because execution becomes asynchronous.
- Entity position is authoritative at its entity owner. Cell occupancy is an
  eventually consistent cell-owner projection.
- Relationship state is authoritative at its relationship owner. Endpoint
  adjacency visible to entity behavior is a published relationship projection.
- Culture, roads, coverage, terrain mutations, and map activity are
  authoritative at cell owners.
- Gambits remain entity-authored. Generators, chronicles, world beats, and other
  genuinely global low-cadence work use strategic owners reading published
  aggregates and emitting ordinary typed messages.
- Remote targeting may use boundedly stale position, culture, or relationship
  views. Destination owners may reject stale occurrence handles, but the kernel
  does not roll back a history because a fresher choice might have been made.
- UI frames compose published owner views at display cadence. Displayed world
  time is the global low watermark; visible slices may be ahead within the
  declared lead bound.

## Ownership Layout

Logical owner count is execution topology, not persisted gameplay state.
Owner identity is derived from stable destination identity:

- entity owner from entity slot;
- relationship owner from relationship slot or canonical relationship
  allocation partition;
- cell owner from stable cell identity; and
- strategic owner from program/state identity.

Logical owner count must be independent of physical worker count and large
enough to rebalance the measured hot/cold workload. Physical assignments may
change only at a convergence cut. Slot free lists and structural allocation
will become owner-local so the coordinator does not reappear as an allocation
lane.

## Implementation Phases

### Phase 1: Logical Ownership

Status: complete.

Owning layer: `Engine.Kernel` execution and state identity, with host execution
configuration.

Observed failure: current owner ranges are equal slices of the current work
array sized from active physical workers. `LogicalShardCount` is a no-op
topology knob. Neither gives a destination a stable logical owner.

Causal hypothesis: fixed logical identity must precede mailboxes and local
watermarks; otherwise changing worker count changes message semantics and
mutable ownership.

Work:

- replace `LogicalShardCount` with a real `OwnerPartitionCount`;
- add one capacity-aware logical ownership layout;
- publish frozen entity and relationship ranges grouped by logical owner;
- assign logical owner ranges to physical workers without changing logical
  identity;
- route existing entity, relationship, and cell destination work using the
  same logical owner calculation; and
- keep the one-worker runtime on the same logical ranges.

Primary files:

- `src/Engine.Kernel/SimulationOptions.cs`
- `src/Engine.Kernel/Execution/KernelOwnerTopology.cs`
- `src/Engine.Kernel/Execution/FrozenTickView.cs`
- `src/Engine.Kernel/Execution/KernelWorkerSet.cs`
- `src/Engine.Kernel/Execution/Simulation.Execution.cs`
- `src/Engine.Kernel/Execution/TickDeltaBuffer*.cs`
- `src/Engine.Kernel/Persistence/KernelSnapshot.cs`
- `src/Engine.Host/KernelRuntimeFactory.cs`
- `tests/Engine.Kernel.Tests/SerialEpochInvarianceTests.cs`
- `tests/Engine.Kernel.Tests/KernelSnapshotTests.cs`

Will not change: authored gameplay, tick calibration, lifecycle formulas,
physical worker lifecycle, event schema, or persistence state layout.

Exit:

- every routed destination reports one stable logical owner across worker
  counts;
- one worker executes the same logical topology rather than an old serial
  gameplay path;
- snapshot load takes owner topology from the host rather than saved state; and
- `make test` passes.

Progress on 2026-07-27:

- `LogicalShardCount` and its unused reduction argument are removed rather than
  retained as a compatibility setting.
- `OwnerPartitionCount` defaults to 64 logical partitions and is supplied by
  the host on snapshot load.
- entity, relationship, and cell owner identity now derives from stable slot or
  cell identity independently of physical worker count;
- frozen entity and relationship publication is grouped into owner-contiguous
  ranges, and physical workers receive complete logical-owner ranges rather
  than arbitrary equal slices; and
- the full repository `make test` pass is green, including the durable
  logical-owner/physical-worker assignment invariant and the existing
  1/2/4/8/16-worker behavior comparison.

Typed destination work now executes the fixed logical topology as well. A
physical worker receives a range of logical owners and iterates those owners;
one physical worker iterates the same 64-owner topology rather than entering a
separate serial resolution path.

### Phase 2: Bounded Owner Mailboxes

Status: complete.

Owning layer: `Engine.Kernel` execution and bounded storage.

Work:

- replace tick-wide append arenas with fixed-capacity logical-owner inboxes;
- keep the existing primitive typed records, removing deterministic fields that
  exist only for global merge order;
- define local delivery, worker-pair delivery, backpressure, overflow, and
  stale-occurrence behavior;
- coalesce additive cell and attribute traffic before enqueue; and
- execute the complete mailbox semantics with one physical worker before
  concurrency is enabled.

Will not change: program selection rules, authored rates, public commands,
projection, or persistence.

Exit:

- every cross-owner mutation traverses the same inbox protocol even when source
  and destination share a physical worker;
- no coordinator apply queue or direct cross-owner mutation remains inside the
  typed mutation boundary; and
- exact structural and bounded-capacity tests pass.

Protocol completed on 2026-07-27:

- `ContinuousOwnerInbox<T>` owns a fixed-capacity block pool plus one
  multi-producer channel per logical destination owner. Local and remote
  delivery use the same append/drain path, and capacity exhaustion fails
  explicitly rather than dropping work.
- Entity and relationship messages use generation-bearing occurrence handles.
  The destination owner rejects an occurrence that retired or whose slot was
  reused before consumption.
- Attribute deltas, per-source culture contributions, culture reductions, and
  competing cell writes are coalesced in owner-contiguous storage before owner
  consumption. Culture coalescing preserves source occurrence identity so
  influence-production credit is unchanged.
- Relationship-creation requests first route to a canonical topology owner for
  deduplication, then accepted records route through a second inbox to the
  reserved relationship-slot owner.
- Movement, cell state, culture, relationships, tags, entity state,
  relationship endings, lifecycle retirement, influence credit, and existing
  strategic typed mutations all execute at fixed logical destinations.
- The old single-worker mutation shortcuts, tick-wide delta buffers, merge
  sorts, deterministic source keys, and worker append mutation arenas are
  deleted. `OwnerInboxArena<T>` remains only as bounded event-segment staging;
  it is not a second mutation protocol.
- `make test` passes with bounded-capacity, owner-contiguity, occurrence, and
  1/2/4/8/16-worker behavioral coverage.

The synchronous mailbox epoch was the Phase 2 stopping point. Phase 3 replaced
it with continuous owner inboxes and bounded local-tick windows, and Phase 4
removed the remaining strategic dispatch and structural mutation shortcuts.
There is now one typed mutation protocol.

### Phase 3: Continuous Local Workers

Status: complete.

Owning layers: `Engine.Kernel` owner state machines and
`Engine.Kernel.Driver` physical thread lifecycle.

Work:

- replace `KernelWorkerSet` job dispatch with persistent worker run loops;
- add owner-local ticks, published watermarks, and bounded lead;
- run entity, minor-cohort, relationship, and cell-local programs continuously;
- publish fixed-capacity remote read views;
- park only on lack of runnable work or lead-window backpressure; and
- retain temporary convergence only around unsupported strategic work.

Exit:

- ordinary local ticks contain no global barrier, coordinator dispatch, or
  worker join;
- one- and multi-worker runs use the same owner state machines; and
- an 8-worker 1,000-tick screen is faster than one worker before proceeding.

Completed on 2026-07-27:

- `KernelWorkerSet` now keeps fixed background threads alive and assigns each a
  stable range of the 64 logical owners. Once a local window begins, each
  worker repeatedly drains and advances those owners without coordinator
  dispatch, phase barriers, or joins between ordinary ticks.
- `ContinuousOwnerRuntime` gives every owner a local tick, published tick,
  published revision, fixed-capacity typed inboxes, and an eight-tick default
  lead bound. One physical worker executes the same owner state machines and
  inbox paths as many workers.
- entity and relationship programs execute on frozen owner-contiguous
  membership; cell culture decay executes on frozen owner-contiguous culture
  rows; minor cohorts execute every local tick on their program-state owner
  until their structural state is partitioned in Phase 4.
- destination owners apply entity coordinates, attributes, relationship
  strength, tags, cell values, culture mass, and influence credit immediately.
  Structural membership changes travel as typed projections to the structural
  owner.
- remote primitive values may be observed after their destination owner
  publishes them, while active membership and structural indexes stay fixed
  for the window. A new culture row therefore becomes targetable after the
  next cut rather than retroactively inside the window that created it.
- Phase 4 subsequently moved gambits, generators, chronicles, world beats,
  culture slots, knowledge, and map-frontier work onto stable program owners
  and removed the window-edge replay path.
- snapshot load takes `OwnerLeadTicks` from the host topology just like worker
  and owner-partition counts. The lead setting is not persisted gameplay
  state.
- the repository-level `make test` pass is green. The registered untraced
  pre-rendered screens `async-item3-screen-w1-20260727` and
  `async-item3-screen-w8-20260727` completed 1,000 ticks at 120.0 and 154.3
  ticks per second respectively, so eight workers are 1.29x faster than one.

The corresponding trace runs
`async-item3-after-w1-20260727` and
`async-item3-after-w8-20260727` measured 98.3 and 94.5 ticks per second. The
sampling collector charged thread parking and collection overhead to the
multi-worker process strongly enough to reverse the wall result, so the Phase
3 exit uses the registered uninstrumented screen while retaining both traces
for hotspot analysis.

### Phase 4: Strategic And Structural Owners

Status: complete.

Owning layer: `Engine.Kernel` strategic programs and owner-local allocation.

Work:

- move gambits, generators, chronicles, world beats, culture slots, and map
  frontier onto published views and typed messages;
- partition entity and relationship free lists by logical owner;
- make structural indexes owner-authoritative or published projections;
- remove coordinator slot reservation and structural reconciliation; and
- replace global event ordering with owner-local primitive event segments.

Exit:

- no simulation program requires per-tick global convergence;
- structural creation and retirement remain generation-safe; and
- broad gameplay smoke metrics remain healthy.

Completed on 2026-07-27:

- all registered programs now execute from stable logical owners inside the
  continuous window. Gambits and culture slots capture explicit published
  views; chronicles and world beats consume typed program signals; generators,
  map frontier, knowledge, and minor actors own their program state and emit
  ordinary destination messages.
- generator evaluation no longer re-enters `KernelWorkerSet` or joins a nested
  worker phase. Generator slots evaluate serially inside their one authoritative
  program owner, and the obsolete owner-work interface, worker configuration
  path, and per-worker generator scratch replicas are deleted.
- entity and relationship slot allocation uses partitioned free stacks keyed
  by logical owner. Program owners may reserve outputs without routing through
  a coordinator allocation lane; accepted records apply at the reserved slot
  owner.
- entity culture changes, entity retirement, relationship creation, and
  relationship retirement are two-stage owner transitions. The slot owner
  mutates the authoritative record; a structural owner maintains active lists,
  kind/culture membership, spatial membership, endpoint adjacency, coverage,
  districts, roads, and follow projections from typed messages.
- entity retirement cleanup routes to the culture-slot, minor-actor, and
  chronicle program owners. Entity-keyed low-altitude, gambit cooldown, and
  relationship-follow rows clear at the entity owner. Retirement no longer
  scans and mutates remote program state.
- retired entity and relationship records remain allocated through window
  quiescence. Only after every owner reaches the window target and all typed
  messages drain are slots cleared, generations advanced, and the partitioned
  free stacks replenished. Frozen raw membership therefore cannot alias a new
  occurrence inside the same window.
- relationship-follow state is an entity-owner projection computed from the
  structural endpoint index after creation or retirement. Programs no longer
  mutate relationship records and endpoint/follow indexes from one thread.
- event publication uses bounded owner-local segments. The old global
  deterministic sort, tick delta types, strategic intent worker interface, and
  registration-order tests are removed rather than retained as compatibility
  paths.
- the obsolete assertions that registered systems run in global order or see
  each other's writes in the same tick were replaced with the durable contract
  that every due registered system executes once. Cross-owner visibility is
  bounded and eventual by design.

### Phase 5: Convergence And Consumers

Status: complete.

Owning layers: kernel convergence contract, drivers, host, eventing, projection,
persistence, and Monte Carlo tooling.

Work:

- implement `ConvergeAt(tick)` and low-watermark queries;
- route commands to owner inboxes with a bounded admission delay;
- pause and capture owner state plus pending future messages for persistence;
- compose UI frames from owner-published views;
- stop Monte Carlo runs by low watermark and capture at convergence; and
- replace `TickCompleted` consumption with watermark progress where required.

Exit:

- save/load, pause, manual stepping, unpaced execution, projection, telemetry,
  and registered Monte Carlo runs operate without restoring per-tick barriers.

Completed on 2026-07-27:

- `Simulation` now distinguishes owner-local progress from the consumer-facing
  published low watermark. `ConvergeAt(tick)` advances bounded owner windows
  until a structurally reconciled and strategically published cut reaches the
  requested tick; owner high-watermark progress never masquerades as a
  consumer-ready frame.
- realtime, unpaced, and stepped drivers advance or stop by published
  watermarks. Pausing joins the active driver window and converges any published
  owner lead before returning. Manual stepping converges once at the requested
  horizon rather than installing a barrier on every authored tick.
- external commands wait in the bounded command ring until the next local
  window, route to a stable entity, cell, or program owner, and expand into the
  same typed owner inboxes as simulation work. Command completion is published
  only after all destination traffic and deferred structural consequences
  converge at the closing cut.
- snapshot format `56` requires a published convergence cut. Owner ticks and
  inboxes are empty and reconstructible at that cut; commands not yet admitted
  are serialized in FIFO order with their assigned ids, so pause/save does not
  silently lose future work. Physical workers and owner topology remain
  host-supplied execution state.
- `WatermarkAdvanced` is the frame/progress record. UI, map, chronicle,
  minor-actor, entity-trail, and telemetry projections no longer infer
  published time from arbitrary future-tick mutations or `TickCompleted`.
  `TickCompleted` remains temporarily available for non-progress consumers
  until the Phase 6 cutover.
- full Monte Carlo execution advances to the next actual consumer boundary:
  snapshot cadence, progress, stream, checkpoint, or final horizon. State
  snapshots are captured only after the telemetry projection reaches the same
  low watermark; a disabled snapshot cadence captures the initial and final
  cuts rather than forcing one convergence per tick.
- Phase 4 subsequently removed the structural/strategic replay cut. Phase 5
  consumers still converge only at requested window horizons.

### Phase 6: Cutover And Evidence

Status: pending.

Work:

- delete the bulk-synchronous coordinator, deterministic per-phase event sort,
  global mutation-version contract, and obsolete invariance knobs;
- update stable architecture and systems documentation;
- run schedule-fuzzed structural invariants;
- establish multi-seed distribution envelopes for map utilization, culture
  shape, minor labor, lifecycle churn, relationships, gambits, and generators;
- measure identical pre-rendered workloads at 1, 8, and 16 workers; and
- run 25,000- and 100,000-tick evidence only after short gates pass.

Acceptance:

- one worker does not regress against the tagged checkpoint;
- eight workers reach at least `4.5x` and sixteen reach at least `7x`
  end-to-end;
- peak working set stays below 8 GB;
- no mutation messages are dropped and no capacity invariant fails; and
- multi-seed outcomes remain inside accepted broad-shape envelopes.

If the performance gates fail after barrier removal and message/coalescing
profiling, revert to `pre-async-deterministic-kernel-20260727` rather than
retaining two runtime models.
