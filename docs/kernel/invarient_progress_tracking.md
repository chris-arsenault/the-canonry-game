# Invariant Progress Tracking

This tracks remaining work against the simulation kernel invariants:

- no runtime string creation in the kernel
- no runtime string lookup after domain registration
- no hot-path allocations in steady-state tick work

## Architecture Migration Status

Status: project split complete; hot-path discipline ongoing.

The legacy `src/Engine/` project no longer exists. Active simulation code now
lives in the microkernel split:

- `Engine.Kernel` — deterministic tick (`Simulation`, `WorldState`,
  `KernelDataSet`, the eight program runtimes under `Programs/`,
  `Persistence/`).
- `Engine.Constants` — primitive ID code catalogs.
- `Engine.Domain` — stable-ID registry and `DomainCompiler` producing
  `CompiledDomain`.
- `Engine.Eventing`, `Engine.Eventing.Schema`,
  `Engine.Eventing.Generators` — bounded primitive event log, schema
  registry, source generators.
- `Engine.Kernel.Driver`, `Engine.Kernel.Schema` — async edge and event
  semantic helpers.
- `Engine.Projection`, `Engine.Observability`,
  `Engine.Observability.Export` — independent async consumers (UI read
  models, run logs, telemetry, planner traces, perf, exports).
- `Engine.Host` — `KernelHostBuilder`, `KernelRuntimeFactory`,
  `KernelHost`, consumer registration, startup seeding.
- `Engine.WorldGen` — hex grid, terrain catalog, semantic plane.

`Engine.Kernel/BannedSymbols.txt` rejects strings, LINQ, common allocating
collections, async primitives, logging/timing, and file I/O at build time.
Implicit usings are disabled. The kernel references only `Engine.Constants`
and `Engine.Eventing`.

The kernel's public mutation surface (`Simulation.CreateEntity`,
`SetAttribute`, `SetResource`, `CreateRelationship`, `MoveEntityPhysical`,
etc.) is primitive-ID only. Domain content compiles strings to stable IDs
once at startup; runtime work uses IDs. Display names are resolved by
projections through `ProjectionSymbolCatalog` off the sim thread.

`KernelEventLog` is the primary message path: programs emit primitive
events; projections and observability read independently via
`KernelEventCursor`s. The legacy object-message bus has been retired.

The remaining work below concentrates inside the kernel programs
themselves — planner/evaluator allocation cleanup, graph storage handles,
versioned indexes — and on tightening observability away from object
allocation. Each item lists the migration step it advances against
`kernel-architecture-migration-plan.md`.

## 1. Runtime Strings In Core State (migration plan items 2 + 6)

Status: done at the kernel surface; verify no string fields slipped into
program scratch buffers.

`Simulation`'s public mutation API uses primitive IDs only. Entity state in
`KernelEntityData` is stored in `PrimitiveIntTable` / `PrimitiveFloatTable`.
Subtype, culture, attribute, relationship, pressure, variable, tag, and
source code names are compiled by `DomainCompiler` into stable IDs and are
not read at runtime. Display names live in `ProjectionSymbolCatalog`.

Required direction: keep new program runtimes off strings. Audit every
`Programs/*Runtime` for string fields in `Data` / `State` / `Scratch` and
replace any that crept in with primitive IDs.

## 2. Graph, Attributes, Relationships, And Pressure ID-Keying (migration plan items 2 + 6 + 10)

Status: kernel surface migrated; relationship traversal still uses
`RelationshipGraphCursor` over compact adjacency, but the underlying
storage discipline (stable-handle adjacency with no iterator allocation)
is the open follow-up to migration plan item 10.

`Simulation.CreateRelationship`/`SetRelationshipStrength`/`CullRelationship`
take primitive IDs. Relationship facts are stored as a compact 4-slot
primitive bitset on each relationship. Pressure values are keyed by
`PressureId` in `PressureProgramRuntime`.

Required direction: continue the move to stable-handle adjacency storage
and confirm all relationship traversal paths avoid allocation in steady
state.

## 3. Primitive Event Boundary (migration plan items 3 + 4 + 7)

Status: done.

`KernelEventLog` is the primary write path. The kernel's
`WriteKernelEvent(in KernelEvent)` is non-blocking and stamps the current
execution context (program ID + source code ID) onto every event.
Projections and observability read via independent
`KernelEventCursor`s; slow consumers cannot stall the kernel or another
consumer.

Required direction: keep new event types primitive (no strings, no formatted
prose, no presentation-only payloads); validate via
`KernelEventSchemaRegistry`.

## 4. The Evaluator And Planner Still Allocate Heavily (migration plan item 9)

Status: remaining.

The current planner/evaluator implementations inside
`ProcessProgramRuntime`, `GambitProgramRuntime`, and
`PlanningProgramRuntime` still rely on per-tick working sets that allocate
in steady state — candidate goal lists, locality reports, scoring buffers,
and provider snapshots. Source labels are typed as IDs but some scratch
shapes remain object-y.

Required direction: move evaluator/planner working sets to reusable
fixed-capacity buffers or pooled scratch with explicit overflow behavior.
The runtimes already carry `Scratch` types — push more of the working set
into them and shrink per-tick allocation toward zero.

## 5. The Query Cache Is Partial, Not The Target Architecture (migration plan item 7)

Status: partial.

The current query path gives same-tick quick returns but still rebuilds
arrays per tick. The target shape is mutation-maintained versioned indexes
keyed by primitive IDs.

Required direction: move from per-query materialization to versioned
indexes maintained on mutation. Cache keys and relationship filters should
be primitive IDs. Cached results should be stable spans/ranges or fixed
buffers, not newly allocated arrays.

## 6. Core Graph Storage Is Not Allocation-Disciplined (migration plan item 10)

Status: remaining.

Relationship adjacency in `WorldState` uses primitive IDs at the surface
but parts of the underlying traversal still rely on iterator-style flow
that can allocate. Replace remaining iterator/enumerable patterns with
stable-handle adjacency walks over fixed buffers.

Required direction: stable-handle adjacency/index storage with span-based
traversal. Avoid iterator allocation, closures, and materialized
enumerable chains in any relationship walk used during the tick.

## 7. Observability Now Lives Outside The Kernel (migration plan item 5)

Status: project split done; allocation cleanup ongoing.

Run logging, telemetry, planner trace reconstruction, performance
recording, and Monte Carlo export are all in `Engine.Observability` and
`Engine.Observability.Export`, registered as independent consumers via the
host. They consume primitive events through `KernelEventCursor`s.

Required direction: tighten the consumers themselves — telemetry batches
and run-log lines still allocate per event. The kernel side is clean; the
remaining work is on the consumer side, where allocation is allowed but
should still be bounded and bursty rather than per-tick steady-state.
