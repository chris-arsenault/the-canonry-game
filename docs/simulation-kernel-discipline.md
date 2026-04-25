# Simulation Kernel Discipline

This document is a critical architecture reference for future engine work.

The simulation kernel is the code executed by `SimulationRunner.Tick()` and every
system it invokes on the sim thread. It should be treated like an audio callback:
deterministic, bounded, allocation-aware, and isolated from presentation work.

The current codebase is not fully compliant with this document. Existing
violations are migration debt, not precedent. New work must move the engine
toward these rules and must not introduce avoidable new violations.

## Core Rule

The sim thread owns simulation mutation.

No other thread may mutate engine state. Off-thread systems may consume primitive
snapshots or primitive event records, compute suggestions, and return primitive
intent records for the sim thread to validate and apply.

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
- `PressureId`
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

## Review Checklist

For any engine change, ask:

- Does this run during `SimulationRunner.Tick()`?
- Does it allocate in steady state?
- Does it create or compare strings after domain registration?
- Does it emit presentation text instead of primitive facts?
- Does it pass live engine objects across threads?
- Does it make the sim thread anything other than the only state mutator?
- Does it preserve deterministic tick ordering?

If the answer exposes a violation, either redesign the change or document the
violation as explicit migration debt with a contained scope.
