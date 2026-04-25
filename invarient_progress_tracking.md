# Invariant Progress Tracking

This tracks remaining work against the simulation kernel invariants:

- no runtime string creation in the kernel
- no runtime string lookup after domain registration
- no hot-path allocations in steady-state tick work

## Architecture Migration Status

Status: started.

Project boundaries now exist for `Engine.Kernel`, `Engine.Domain`, `Engine.Eventing`, `Engine.Projection`, and `Engine.Host`. The current legacy `Engine` project still owns the active simulation implementation; code has not yet been moved into `Engine.Kernel`.

`Engine.Eventing` now contains the first primitive bounded event log with independent consumer cursors. It is not yet wired into the legacy simulation message path. The old object-message bus remains in `src/Engine/Messages` until producers and consumers are migrated.

`Engine.Kernel` now has hard build guardrails: it may only reference `Engine.Eventing`, it may not add runtime package dependencies, implicit usings are disabled, runtime string syntax fails the build, and banned-symbol analyzers reject LINQ, common allocating collections, async primitives, logging/timing, and file I/O.

`Engine.Domain` now contains the first stable ID registry for attributes, relationship kinds/facts, pressures, resources, subtypes, cultures, generators, event codes, reason codes, and source codes. The active simulation has not yet been migrated to these IDs.

## 1. Runtime Strings Still Live In Core State

Status: started.

`Engine.Domain` now has stable runtime ID types and a registry that compiles strings into IDs with hard failures for duplicate registrations and unknown lookups. `Entity` still stores `Subtype`, `Name`, and `Culture` as live strings. `AttributeRef` still stores `Name` and `RelationshipKind` strings. `IMutationContext` still exposes string-based reads and writes for attributes, pressures, relationships, variables, tags, and entity creation.

Required direction: compile subtype, culture, attribute, relationship, pressure, variable, tag, and source names into stable primitive IDs during domain registration. Live engine state should hold IDs only; display strings stay in registries for UI/export.

## 2. Graph, Attributes, Relationships, And Pressure Still Use String Lookup

Status: started.

Stable ID types now exist for the relevant categories in `Engine.Domain`, but active graph/runtime systems are not migrated. `AttributeSet` is still keyed by `string`. `WorldGraph.CreateRelationship` still accepts string relationship kinds and string facts. `ProcessMutationContext` still uses strings for variables, relationship kinds, attributes, pressure names, and source labels.

Required direction: replace name-keyed runtime dictionaries and APIs with ID-keyed storage and lookup. Relationship facts should become primitive fact IDs or compact bitsets.

## 3. The Message Bus Is Not Primitive Or Allocation-Free Yet

Status: started.

The new `Engine.Eventing` project introduces a primitive bounded event log with independent read heads. The active simulation still publishes heap-allocated `SimulationMessage` objects. Many current message payloads still carry strings: attribute names, entity names, resource names, relationship kinds, diagnostics, generator IDs, degeneracy descriptions, and status text. The graph-to-message bridge still formats enum strings and allocates arrays on the sim thread.

Required direction: replace object messages with primitive event records in a preallocated/ring-buffer stream. Formatting, diagnostics, and human-readable projection should happen off the engine thread.

## 4. The Evaluator And Planner Still Allocate Heavily

Status: remaining.

`EntityEvaluator` still creates lists, hash sets, locality reports, snapshots, and uses LINQ/grouping/sorting during tick work. `GoalGenerator` still allocates dictionaries/lists and uses string source labels such as `deficit` and `standing`.

Required direction: move evaluator/planner working sets to reusable fixed-capacity buffers or pooled scratch buffers with explicit overflow behavior. Replace string source labels with enum/source IDs.

## 5. The Query Cache Is Partial, Not The Target Architecture

Status: partial.

The current query cache gives same-tick quick returns, but it still clears per tick, rebuilds arrays with LINQ, uses string relationship keys, and stores materialized lists behind `IReadOnlyList`.

Required direction: move from per-query materialization to versioned indexes maintained on mutation. Cache keys and relationship filters should be primitive IDs. Cached results should be stable spans/ranges or fixed buffers, not newly allocated arrays.

## 6. Core Graph Storage Is Not Allocation-Disciplined

Status: remaining.

`WorldGraph` still uses QuikGraph, dictionaries, hash sets, LINQ, `IEnumerable`, and iterator traversal. This is not compatible with a strict audio-thread-style kernel.

Required direction: replace or wrap graph storage with custom stable-handle adjacency/index storage. Relationship traversal should avoid iterator allocation, closures, and materialized enumerable chains.

## 7. Observability Is Still Partly In-Engine

Status: remaining.

Run logging, telemetry recording, performance recording, and message projection still live under `src/Engine` and consume live engine objects/messages.

Required direction: make observability a consumer of primitive event streams and immutable primitive snapshots. UI, logs, Monte Carlo export, profiler summaries, and narrative/debug text should resolve IDs and format text off the sim thread.
