# Kernel Architecture Migration Plan

This is the ordered path for moving from the current legacy `Engine` project to
the invariant-enforced microkernel architecture.

The rule for this migration is hard failure over compatibility masking. Do not
add shims, compatibility fallbacks, or silent dual behavior that hides an
architectural deficiency. Temporary dual-write validation is acceptable only when
both paths are checked against each other and mismatches fail tests or telemetry.

## 1. Lock Project Boundaries

Keep active simulation in legacy `Engine` briefly, but require new kernel code to
land in `Engine.Kernel`. Add kernel-specific analyzer/build rules that fail on
forbidden references, runtime strings, LINQ/materialization, async primitives,
logging/I/O, and common allocating collections.

## 2. Define Stable ID Registries In `Engine.Domain`

Compile domain strings into stable runtime IDs for attributes, relationships,
pressures, resources, subtypes, cultures, generators, event codes, reason codes,
and source codes.

This addresses tracker items 1 and 2.

## 3. Expand `Engine.Eventing` Schema

Replace generic event fields with explicit primitive event layouts and explicit
overflow policies. Keep the kernel write API tiny and non-blocking, and keep
consumer cursors independent.

This advances tracker item 3.

## 4. Dual-Write Primitive Events From The Current Simulation

Keep current output stable while primitive events are validated against current
`SimulationMessage` and telemetry outputs. This is not a shim: mismatches should
fail loudly.

This reduces Monte Carlo breakage risk while advancing tracker items 3 and 7.

## 5. Move Telemetry, Logging, UI Projection, And Monte Carlo Projection Out Of The Hot Path

Move projection code into `Engine.Projection.*` consumers that read primitive
events and immutable snapshots. Logs, UI state, telemetry rows, debug traces, and
Monte Carlo exports should be downstream products.

This addresses tracker item 7.

## 6. Replace Runtime String APIs

Migrate `AttributeRef`, `AttributeSet`, `Relationship`, `PressureSystem`, and
`IMutationContext` from names to stable IDs.

This is the main work for tracker items 1 and 2.

## 7. Replace Per-Query Materialization

Turn `WorldStateQueryCache` into mutation-maintained versioned indexes with
primitive keys. Cached results should be stable ranges, spans, or fixed buffers,
not newly allocated arrays/lists.

This addresses tracker item 5.

## 8. Move Tick-Owned Runtime State Into `Engine.Kernel`

After strings, events, and projections are separated, move actual tick systems
into the kernel project. The legacy `Engine` project should become host-facing
compatibility only until deleted or replaced.

## 9. Remove Evaluator And Planner Allocations

Replace planner/evaluator lists, hash sets, LINQ, snapshots, and report objects
with fixed buffers and explicit truncation/overflow behavior.

This addresses tracker item 4.

## 10. Replace Graph Storage

Move away from QuikGraph/enumerable traversal toward stable-handle adjacency and
index storage with primitive IDs and no hot-path allocation.

This addresses tracker item 6.

