# Simulation Kernel Discipline

This document is a critical architecture reference for future engine work.

The simulation kernel is the code executed by `Simulation.AdvanceTicks()` (in
`Engine.Kernel/Simulation.cs`) and every program/system it invokes on the sim
caller or its fixed owner workers. It should be treated like an audio callback:
bounded, allocation-aware, and isolated from presentation work. Mutation is
exact within an authoritative owner; cross-owner histories may differ with
bounded message timing.

The kernel split is in place — `Engine.Kernel`, `Engine.Domain`,
`Engine.Eventing`, `Engine.Eventing.Schema`, `Engine.Host`,
`Engine.Kernel.Driver`, `Engine.Kernel.Schema`, `Engine.Projection`,
`Engine.Observability`, `Engine.Observability.Export`, and
`Engine.WorldGen` — and `Engine.Kernel/BannedSymbols.txt` enforces the
no-string, no-LINQ, no-allocation, no-async, no-I/O rules at build time.
Existing violations are migration debt, not precedent. New work must move the
engine toward these rules and must not introduce avoidable new violations.

## Core Rule

Every mutable destination has one exclusive logical owner.

The caller opens bounded windows and admits commands. Entity, relationship,
cell, structural-index, and program owners mutate only their assigned state.
Effects that cross owners remain primitive typed messages until their
destination owner consumes them. A window waits for quiescence only to publish
a consumer cut, replace frozen membership, or reclaim retired slots; it does
not replay a global mutation queue or establish per-tick registration order.
Uncoordinated threads may consume primitive snapshots or event records, but may
not read or mutate live kernel state.

## Architectural Enforcement Direction

These invariants cannot be enforced reliably while all runtime, projection,
telemetry, logging, UI-facing messages, and support code live in one engine
project. The engine should move toward a microkernel-style project split where
the simulation kernel has strict references and stricter analyzers than the
surrounding services.

Target project responsibilities:

- `Engine.Kernel`: owns bounded authoritative simulation state, tick mutation, primitive
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

The low-altitude scheduler uses fixed logical owners on long-lived physical
workers. The caller admits commands and opens a bounded local-tick window;
workers then repeat owner drain, local evaluation, destination application, and
watermark publication without returning to the caller between ordinary ticks.
The default lead window is eight ticks. A worker that cannot advance
because of the lead bound drains its owners and yields or parks until progress
is possible.

Logical ownership is independent of physical worker count:

- entity slots own coordinates, attributes, tags, lifecycle state, and
  entity-program rows;
- relationship slots own edge facts and relationship strength;
- coordinate-derived cell owners own terrain, coverage values, culture mass,
  and other cell values;
- bounded indexes own spatial membership, endpoint adjacency, coverage reverse
  rows, and culture row membership; and
- program owners hold low-altitude state that is not yet partitioned, including
  the current minor-cohort active/index structures.

Every primitive mutation uses a fixed-capacity destination inbox even when
source and destination are assigned to the same physical worker. Entity and
relationship messages carry generation-safe occurrence handles. Destination
owners reject stale handles, apply accepted messages exactly once, and never
silently drop overflow. Additive fields retain their field-specific fixed-point
range and clamp contracts; schedule order may change an individual history.

Entity, relationship, and culture-row membership is captured in
owner-contiguous frozen ranges at the opening cut. Those ranges stay stable
through the window. Destination-owned primitive values publish as their owner
advances and may be read with bounded lag or bounded lead; readers may not
mutate through those views. A structural row created during a window therefore
becomes iterable only after the closing cut even though its owning primitive
value may already exist. Quiescence makes the next opening view safe.

Structural indexes and program-scoped state have stable owners inside the same
continuous message graph. Movement membership, coverage reverse indexes,
culture row membership and release, locality invalidation, road work,
relationship retirement, and lifecycle retirement are typed projections rather
than closing-cut replay. Gambits, generators, chronicles, world beats, culture
slots, knowledge, and map frontier run on program owners. Window quiescence
only publishes a complete consumer view and permits generation-safe slot
reclamation.

The owner low watermark and the consumer-published low watermark are distinct.
An owner may publish its local tick before all structural and program-owner
messages are consumed. `ConvergeAt(tick)` returns the consumer watermark only
after the whole cut is available, and `WatermarkAdvanced` is the primitive
record that lets projections publish that frame. `TickCompleted` is not a
consumer clock.

External commands are bounded future messages. At the opening cut they route
to a stable destination owner and may produce ordinary typed cross-owner
traffic. Applied or rejected completion is published after the complete window, not
when the command first enters an owner inbox. Pausing must finish the active
window and converge owner lead. Persistence may then reconstruct equal owner
ticks and empty inboxes from the cut while serializing commands still waiting
for admission.

Runtime randomness remains stateless and keyed by seed, owner-local tick,
program, occurrence or logical key, rule, purpose, and attempt. Unrelated work
does not consume a shared stream, but worker schedules are allowed to change
which boundedly published remote values a decision observes.

`WorkerCount`, `OwnerPartitionCount`, and `OwnerLeadTicks` are host execution
topology and are not persisted in kernel snapshots. Worker count one executes
the same logical-owner ranges, continuous state machines, and inbox protocol as
larger worker counts; there is no separate serial mutation model.

Allowed inside a continuous owner loop:

- reading immutable program data and the frozen membership ranges for the
  current window;
- reading destination-published primitive values within the declared lead
  bound;
- mutating only slots and values authoritative to the current logical owner;
- consuming and producing bounded primitive owner messages;
- using worker-local fixed-capacity scratch and deferred structural records;
  and
- publishing the owner's tick and revision only after its accepted local
  mutations have completed.

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
- mutating a remote owner's authoritative value directly
- iterating a structural index created after the current opening cut
- retaining live engine references outside the owning worker or convergence
  operation
- adding a global apply queue, per-tick join, or compatibility index beside the
  typed owner protocol
- doing async I/O, allocation, or consumer work in an owner loop

## Async Activity Direction

Derived work outside the kernel remains separate from authoritative owner
workers.

Any future derived-work worker should use this shape:

1. Owners publish an immutable primitive world/activity view and watermark.
2. Worker reads only that snapshot and immutable domain registries.
3. Worker emits primitive activity intents.
4. The destination owner validates occurrence identity and source watermark,
   then applies or discards the intent.

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
- Are live references confined to their logical owner or an explicit
  convergence cut?
- Does it belong in `Engine.Kernel`, or should it be a domain, eventing,
  projection, host, or plugin concern?
- Does it require one consumer to keep pace with another consumer?
- Does it preserve authored tick units and declared owner-lag contracts?
- Is experimental or tunable kernel behavior controlled by host-loaded JSON/TOML
  runtime config rather than command-line-specific switches or code edits?

If the answer exposes a violation, either redesign the change or document the
violation as explicit migration debt with a contained scope.
