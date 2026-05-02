# Simulation Kernel Discipline

This document is a critical architecture reference for future engine work.

The simulation kernel is the code executed by `Simulation.AdvanceTicks()` (in
`Engine.Kernel/Simulation.cs`) and every program/system it invokes on the sim
thread. It should be treated like an audio callback: deterministic, bounded,
allocation-aware, and isolated from presentation work.

The kernel split is in place — `Engine.Kernel`, `Engine.Domain`,
`Engine.Eventing`, `Engine.Eventing.Schema`, `Engine.Host`,
`Engine.Kernel.Driver`, `Engine.Kernel.Schema`, `Engine.Projection`,
`Engine.Observability`, `Engine.Observability.Export`, and
`Engine.WorldGen` — and `Engine.Kernel/BannedSymbols.txt` enforces the
no-string, no-LINQ, no-allocation, no-async, no-I/O rules at build time.
Existing violations are migration debt, not precedent. New work must move the
engine toward these rules and must not introduce avoidable new violations.

## Core Rule

The sim thread owns simulation mutation.

No other thread may mutate engine state. Off-thread systems may consume primitive
snapshots or primitive event records, compute suggestions, and return primitive
intent records for the sim thread to validate and apply.

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
- debug trace construction inside planner/evaluator/process execution
- message-bus payloads that carry formatted text instead of primitive facts

Allowed outside the simulation kernel:

- UI labels
- debug traces
- logs
- Monte Carlo exports
- profiler summaries
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
- `ResourceId`
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

Overflow must be explicit. Dropping an event, truncating a planner candidate list,
or expanding a buffer are design choices and should be visible in tests/telemetry.

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
- entity IDs
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

The default model is one sim writer thread.

Allowed off-thread work:

- UI formatting and debug trace building
- log writing
- Monte Carlo parquet/CSV export
- batch analysis
- profiler/perf aggregation
- planner computation from immutable snapshots
- pathfinding/scoring batches that return primitive deltas or intents

Disallowed off-thread work:

- mutating `WorldGraph`, entities, attributes, goals, resources, relationships, or
  cooldown state
- retaining live engine object references from the sim thread
- applying planner results without sim-thread validation

Cross-thread data must be primitive, immutable, and versioned. A worker may
return `PlanIntent` records, but the sim thread decides whether the source world
version is still current and whether the intent remains legal.

Async work belongs on the consumer side of the primitive event boundary. UI,
logs, telemetry, profiler aggregation, Monte Carlo export, debug trace building,
and plugin services should run as independent consumers. The kernel remains
synchronous and deterministic; consumers are asynchronous and independently
paced.

## Planner Thread Direction

A planner worker is desirable, but only after the boundary is clean.

Planner threading should use this shape:

1. Sim thread publishes an immutable primitive world/planner snapshot.
2. Planner worker reads only that snapshot and immutable domain registries.
3. Planner worker emits primitive plan intents.
4. Sim thread validates source tick/version and applies or discards intents.

The planner worker must not read live entities or write `Entity.Goals` directly.

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

- `programs.planning.default_planner`: `goap`, `htn`, or `opportunity`.
- `programs.planning.goap_target_reservations_enabled`: `true` or `false`.
- `programs.planning.opportunity_target_capacity`: integer target capacity.
- `programs.upkeep.aging_model`: `linear` or `late_life`.
- `programs.generators.retry_fire_targets`: `true` or `false`.

## Review Checklist

For any engine change, ask:

- Does this run during `Simulation.AdvanceTicks()` (i.e. inside a kernel
  program registered via `Simulation.RegisterProgram` or as a per-tick
  system)?
- Does it allocate in steady state?
- Does it create or compare strings after domain registration?
- Does it emit presentation text instead of primitive facts?
- Does it pass live engine objects across threads?
- Does it make the sim thread anything other than the only state mutator?
- Does it belong in `Engine.Kernel`, or should it be a domain, eventing,
  projection, host, or plugin concern?
- Does it require one consumer to keep pace with another consumer?
- Does it preserve deterministic tick ordering?
- Is experimental or tunable kernel behavior controlled by host-loaded JSON/TOML
  runtime config rather than command-line-specific switches or code edits?

If the answer exposes a violation, either redesign the change or document the
violation as explicit migration debt with a contained scope.
