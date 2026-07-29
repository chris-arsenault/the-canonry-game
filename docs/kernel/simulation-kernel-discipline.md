# Simulation Kernel Discipline

This document is a critical architecture reference for future engine work.

The simulation kernel is the code executed by `Simulation.AdvanceTicks()` (in
`Engine.Kernel/Simulation.cs`) and every program/system it invokes on the sim
coordinator or its fixed evaluation workers. It should be treated like an audio
callback: deterministic, bounded, allocation-aware, and isolated from
presentation work.

The kernel split is in place — `Engine.Kernel`, `Engine.Domain`,
`Engine.Eventing`, `Engine.Eventing.Schema`, `Engine.Host`,
`Engine.Kernel.Driver`, `Engine.Kernel.Schema`, `Engine.Projection`,
`Engine.Observability`, `Engine.Observability.Export`, and
`Engine.WorldGen` — and `Engine.Kernel/BannedSymbols.txt` enforces the
no-string, no-LINQ, no-allocation, no-async, no-I/O rules at build time.
Existing violations are migration debt, not precedent. New work must move the
engine toward these rules and must not introduce avoidable new violations.

## Core Rule

Every mutable destination has one exclusive owner for a published phase.

The coordinator owns admission, owner preparation, strategic selection,
cross-owner structural reconciliation, and publication. Fixed workers may
mutate only the disjoint program-local or world-state destinations assigned to
their evaluation range or owner range, and may append only through their
bounded worker arena. Effects that cross those owners remain primitive deltas
until their typed owner reduces them at a barrier. Uncoordinated threads may
consume primitive snapshots or event records, but may not read or mutate live
kernel state.

## Architectural Enforcement Direction

These invariants cannot be enforced reliably while all runtime, projection,
telemetry, logging, UI-facing messages, and support code live in one engine
project. The engine should move toward a microkernel-style project split where
the simulation kernel has strict references and stricter analyzers than the
surrounding services.

Target project responsibilities:

- `Engine.Kernel`: owns deterministic simulation state, tick mutation, primitive
  IDs, process execution, fixed buffers, and primitive event emission.
- `Engine.Domain`: owns domain registration and compilation from strings into
  stable runtime IDs. Strings are allowed here as input/metadata.
- `Engine.Eventing`: owns the primitive event stream, consumer registration,
  per-consumer cursors, overflow accounting, and async consumer lifecycle.
- `Engine.Projection.*`: owns UI projection, telemetry rows, run logs, perf
  summaries, debug traces, Monte Carlo exports, and human-readable formatting.
- `Engine.Host`: wires kernel, domain, eventing, projections, tools, UI, and
  plugin loading.

`Engine.Kernel` should not reference projection, telemetry, logging, UI,
debugging, export, or async infrastructure. It should have the narrowest possible
event-writer dependency and should be the only project with strict no-string and
no-allocation kernel analyzers enabled.

This split is an enforcement mechanism, not just organization. If code formats
text, blocks on I/O, depends on consumer pacing, allocates projection objects, or
needs live UI/log/telemetry concepts, it does not belong in `Engine.Kernel`.

## Invariant 1: No Runtime String Creation In The Kernel

Engine tick code must not create user-facing text.

Forbidden in the simulation kernel:

- string interpolation for traces, logs, diagnostics, descriptions, or messages
- `ToString()` for diagnostics or event payloads
- `string.Join`, string concatenation, or formatted exception messages in hot paths
- debug trace construction inside evaluator/process execution
- message-bus payloads that carry formatted text instead of primitive facts

Allowed outside the simulation kernel:

- UI labels
- debug traces
- logs
- Monte Carlo exports
- profiler analysis
- narrative text
- human-readable analysis files

The engine should emit primitive facts. Consumers resolve and format those facts
off the sim thread.

## Invariant 2: No Runtime String Lookup After Domain Registration

Domain registration may ingest strings. Runtime simulation work should not.

At startup, domain content should compile string names into stable IDs:

- `KindId`
- `AttributeId`
- `ProcessId`
- `RelationshipKindId`
- `CardId`
- reason/event codes

After registration, engine logic should use stable IDs and compact primitive
records. Name strings remain registry metadata for tooling and presentation.

Runtime string hashing, string equality, and name-based dispatch are engine debt.

## Invariant 3: No Hot-Path Allocations

The sim tick should avoid heap allocation in steady state.

Forbidden or suspect in hot paths:

- LINQ materialization (`ToList`, `ToArray`, `GroupBy`, `OrderBy`, etc.)
- `List<T>.AddRange(IEnumerable<T>)`
- closures and iterator blocks
- heap-allocated messages per event
- per-tick dictionaries unless preallocated and reused
- exceptions for control flow
- string diagnostics
- ad hoc snapshots that allocate collections

Preferred patterns:

- pre-sized arrays
- ring buffers
- stable handles into registries
- pooled scratch buffers with explicit lifetime
- struct event records
- versioned primitive snapshots
- fixed-capacity collections with clear overflow behavior

Overflow must be explicit. Dropping an event, truncating a candidate list,
or expanding a buffer are design choices and should be visible in tests/telemetry.

## Invariant 4: Active State And Occurrence Identity

Fixed capacity bounds concurrent simulation state, not the number of
occurrences that have ever existed. Entities, relationships, districts,
cohorts, cooldown rows, and other lifetime-shaped records must release their
active slots when they end.

Reusable entity and relationship slots are internal addresses. A reference
that can survive the current mutation boundary must carry a generation-safe
occurrence handle, including primitive events and commands, frozen adjacency,
provenance, chronicles, gambits, hosted state, and snapshots. Resolving a stale
handle must fail; it must never name the later occupant of the same slot.

Hot iteration uses dense active, due, dirty, or owner-local indexes. High-water
scans are acceptable only for cold validation or reconstruction where the cost
is explicitly outside the tick path. Snapshot persistence contains active state
and the minimum generation metadata required for identity safety, then rebuilds
free lists and derived indexes on load. It does not preserve retired rows or
ended edges for compatibility.

## Invariant 5: Deterministic Durable Numeric State

Durable additive simulation fields must use a field-specific integer
representation before their values can be reduced across work items or shards.
The contract for each field includes its range, scale, rounding rule,
intermediate width, overflow behavior, clamp point, persistence layout, and
consumer conversion.

Required patterns:

- quantize authored values and API inputs at a declared boundary;
- quantize each contribution before entering a shared reduction;
- use an intermediate wide enough for addition and multiplication;
- reject invalid inputs and make saturation or checked overflow explicit;
- apply semantic clamps in a deterministic place after reduction;
- serialize and emit the integer units, with floats derived only for consumers.

`Engine.Kernel` has a complete floating-point ban: no `System.Single`,
`System.Double`, `MathF`, floating-point tables, or floating-point member
signatures. Candidate scoring, probabilities, path costs, and other local
runtime values use `FixedScalar` even when they are not reduced. Floating point
remains valid only outside the kernel for authored configuration before
compilation and for diagnostics or presentation after explicit unit conversion.
Primitive event payloads and snapshots carry integer units and provide no
floating-point compatibility views.

## Primitive Event Boundary

The message bus should become a primitive event stream.

The kernel-facing event API should remain tiny and non-blocking, for example:

```csharp
bool TryWrite(in KernelEvent evt);
```

The kernel must never await event publication, invoke consumer callbacks, format
messages, or depend on consumer progress. Event writes are fire-and-forget from
the kernel's perspective, with explicit overflow counters and policies.

Events should carry:

- tick
- generation-safe entity and relationship occurrence handles
- stable kind/process/attribute/reason IDs
- numeric values
- enum codes
- compact flags
- source world version where relevant

Events should not carry:

- formatted descriptions
- entity names
- attribute names
- diagnostic prose
- lists built only for presentation

Consumers may build rich objects or text after reading the primitive stream.

The event stream should support multiple independent async consumers with
separate read heads/cursors. UI projection, telemetry export, run logging,
profiling, debug traces, Monte Carlo analysis, and future plugins should not
share a synchronized read head and should not block one another. A slow consumer
may lag, drop, or compact according to its own policy; it must not stall the
kernel or another consumer.

The bus is not a callback list and not a single dispatcher as the architectural
source of truth. It is an append-only primitive stream with sequence numbers,
consumer cursors, bounded retention, and explicit overflow behavior. Host-side
dispatchers may exist as implementation details for individual consumers, but
the bus model must allow consumers to be built, tested, paced, and replaced
independently.

## Thread Discipline

The current scheduler is bulk-synchronous. A coordinator admits commands,
prepares local inputs, freezes the active entity and relationship views,
dispatches eligible local programs over fixed worker ranges, joins all workers,
resolves typed deltas by destination, runs strategic programs, and publishes
the tick. Gambits and generators use the same frozen evaluate, typed route,
owner apply, and coordinator reconciliation contract inside their strategic
registration positions. Semantic events are phase-buffered and
deterministically sorted before they enter the event log.

Configured multi-worker evaluation coalesces the due local range programs into
one dispatch. Destination application coalesces only mutually compatible owner
families. Entity creation, spatial state, primary state, dependent secondary
state, and final relationship/tag/entity state remain separate batches because
coordinator reconciliation or ordered writes connect them. This is deliberate
dependency structure, not permission to restore one barrier per small family.
Worker-local revision and aggregate counters publish after an owner batch; hot
owner loops must not contend on shared change-notification atomics.

Additive deltas sum in a wide intermediate before the destination clamp.
Competing absolute writes use an explicit priority and stable logical source
key. Relationship end and lifecycle retirement resolve after their accepted
strength and attribute changes. Logical shard count changes traversal only; it
does not change destination ownership or convergence state.

Runtime randomness is stateless and keyed by stable logical inputs: seed,
tick, phase, program, occurrence/logical key, rule, purpose, and attempt.
Evaluation order, rejected unrelated work, and logical shard count therefore do
not consume or shift a shared random stream.

Worker count one is the reference contract. It uses the same range evaluator as
larger worker counts while taking direct worker-zero context, scratch, and
append paths so scheduler plumbing does not make the reference runtime slower.

Execution topology is host state. `WorkerCount` and `LogicalShardCount` are
selected when a simulation is constructed or loaded and are not persisted in
kernel snapshots. Changing topology may alter work partitioning only; it may
not add another simulation clock or change published state.

The implemented typed-owner bulk-synchronous scheduler has this ownership
topology:

- stable-ID ranges or bounded dynamic chunks for read-only evaluation;
- entity owners for entity slots and entity-program state;
- relationship owners for relationship slots and relationship state;
- cell owners for terrain, culture, and other map-cell destinations; and
- bounded typed messages whenever one operation affects more than one owner.

Two concrete storage mutation boundaries are:

- entity slots own coordinates; the bounded spatial-membership index owns
  cell-to-entity chains; and
- relationship slots own edge facts; the bounded relationship endpoint index
  owns incident adjacency, CSR rows, delta chains, and compaction.

The fixed-lifetime worker set uses immutable entity/relationship ranges,
worker-local scratch and execution contexts, and bounded append arenas. Effects
and relationship maintenance can evaluate ranges concurrently. Low-altitude
verbs can do so when trace capture is disabled. Minor actors use the same range
contract but remain one range while actor indexes and encounters have shared
ownership.

Typed owners route the resulting deltas without a coordinator apply queue:

- entity ranges own attributes, tags, movement coordinates, and influence
  production credits;
- relationship ranges own relationship strength;
- cell ranges own direct terrain, coverage, culture influence, and culture
  reduction values; and
- worker event arenas publish only after deterministic compaction and sort.

Strategic intent epochs extend that topology without creating a second mutation
model:

- gambit actors evaluate over frozen entity ranges with worker-local scratch;
- generator definitions score over disjoint worker ranges;
- entity and relationship slots are reserved in deterministic coordinator
  blocks before structural intents are published;
- absolute entity and relationship writes, cell terrain/coverage, travel
  structures, structural slot records, and their primitive events route to
  typed owners; and
- active vectors, graph endpoints, spatial membership, districts, and other
  cross-owner indexes reconcile on the coordinator after the owner barrier.

Coordinator barriers prepare storage that cannot be allocated concurrently and
reconcile cross-owner structures afterward. Movement detaches and reattaches
spatial membership around entity-owned coordinate writes. Coverage reverse
indexes, map extents, path topology, culture row release, locality invalidation,
relationship culling, and lifecycle retirement remain explicit ordered
reconciliation stages.

Hot queries may directly read arrays published by an owner while the phase
barrier guarantees they are immutable; they must not mutate through those read
views. New cross-owner work must use the existing bounded typed routing and
barriers, not locks, a global apply queue, or duplicate compatibility indexes.

The ownership model does not authorize preserving accidental work merely to
parallelize it. Remove redundant scans and queries before widening execution.
Persistent geographic actors remain rejected as the universal scheduler;
evaluation workers do not reopen that strategy.

Geography is not the universal ownership key. It applies to cell/map storage
only. Relationship and gambit interactions are too nonlocal, and geographic
entity load is too skewed, for persistent spatial actors to be the primary
scheduler. The implementation must use the same range/owner paths at one worker;
the current implementation does so and may not grow separate serial and
parallel simulation models. Worker one may bypass thread dispatch and preserve
registration-order `CompleteRanges` timing as a fast lane around those same
program and reduction implementations.

Every parallel phase must give each mutable slot, relationship, or cell one
exclusive owner for that phase. Cross-owner effects travel as bounded primitive
deltas and become visible only after deterministic reduction at a published
phase boundary. A phase may not read a field while another owner is mutating
the version it sees.

Allowed inside the synchronous kernel worker set:

- reading the frozen active-ID view and world version published for the phase;
- reading immutable world/program data while the coordinator holds the phase
  barrier;
- mutating only program-local slots exclusively assigned by the worker range;
- reducing only entity, relationship, or cell destinations exclusively assigned
  to the worker owner range;
- using worker-local fixed-capacity scratch; and
- appending primitive deltas and events through bounded worker segments.

Allowed off-thread work:

- UI formatting and debug trace building
- log writing
- Monte Carlo parquet export
- batch analysis
- profiler analysis
- activity computation from immutable snapshots
- pathfinding/scoring batches that return primitive deltas or intents

Disallowed off-thread work:

- concurrent mutation without explicit phase ownership
- reading unpublished or concurrently mutating kernel state
- retaining live engine object references beyond a synchronous worker phase
- applying worker results without deterministic validation and reduction

The fixed kernel workers may receive live kernel references only while the
coordinator has frozen the phase and will join those workers before mutation
advances. All data that escapes that synchronous phase must be primitive,
immutable, and versioned. Typed world deltas and strategic intents route to an
exclusive owner inside that synchronous phase; cross-owner structural
reconciliation occurs only after the owner barrier while preserving the same
version, legality, and reduction contract.

Async work belongs on the consumer side of the primitive event boundary. UI,
logs, telemetry, profiler analysis, Monte Carlo export, debug trace building,
and plugin services should run as independent consumers. The kernel remains
synchronous and deterministic; consumers are asynchronous and independently
paced.

## Async Activity Direction

Async activity workers are only desirable after the primitive event boundary is
clean.

Any future derived-work worker should use this shape:

1. Sim thread publishes an immutable primitive world/activity snapshot.
2. Worker reads only that snapshot and immutable domain registries.
3. Worker emits primitive activity intents.
4. The current sim thread, or the future explicit owner, validates source
   tick/version and applies or discards intents.

The worker must not read live entities or write activity state directly.

## Monte Carlo Direction

Monte Carlo has two separate threading opportunities:

- parallel independent runs
- off-thread event export/analysis

Both depend on the same primitive event boundary. Runs can execute independently,
but aggregation and export should consume primitive run outputs without requiring
the sim thread to allocate analysis objects or strings.

## Runtime Tuning Config

Kernel behavior changes must be compiled with explicit runtime tuning config when
the behavior is experimental, tunable, or likely to need A/B comparison. The
config file is a host startup input, loaded from JSON or TOML, never YAML. Host
startup maps that file into `SimulationOptions.KernelTuning`; the kernel itself
does not read files, parse strings, or depend on command-line flags.

The tuning object is part of the kernel input contract. It should be primitive,
deterministic, organized by program family, passed into program runtimes at
construction, persisted with kernel snapshots when it affects future ticks, and
covered by tests for configured and default behavior. The default value should
represent the current intended game behavior, not a temporary test convenience.

Current tuning keys:

- `programs.lifecycle.aging_model`: `linear` or `late_life`.
- `programs.generators.retry_fire_targets`: `true` or `false`.
- `programs.generators.fire_window_ticks`: positive integer.
- `programs.generators.fires_per_window`: non-negative integer.

## Review Checklist

For any engine change, ask:

- Does this run during `Simulation.AdvanceTicks()` (i.e. inside a kernel
  program registered via `Simulation.RegisterProgram` or as a per-tick
  system)?
- Does it allocate in steady state?
- Does it create or compare strings after domain registration?
- Does it emit presentation text instead of primitive facts?
- Does it pass live engine objects across threads?
- Does every live mutation have one exclusive phase owner?
- Are live references confined to a joined synchronous worker phase?
- Does it belong in `Engine.Kernel`, or should it be a domain, eventing,
  projection, host, or plugin concern?
- Does it require one consumer to keep pace with another consumer?
- Does it preserve deterministic tick ordering?
- Is experimental or tunable kernel behavior controlled by host-loaded JSON/TOML
  runtime config rather than command-line-specific switches or code edits?

If the answer exposes a violation, either redesign the change or document the
violation as explicit migration debt with a contained scope.
