# Kernel Context Findings

## Current Status

There appear to be two overlapping attribution/trace systems:

1. `KernelExecutionContext` on `Simulation`
   - In-memory context with `ProgramId` and `SourceCodeId`.
   - Intended to stop manual source/context threading through kernel runtime.
   - Currently used by mutation paths such as entity, attribute, resource, relationship, cell creation, and by central emit methods for process/gambit/generator/degeneracy events.

2. Explicit planner trace events
   - `PlanStarted`, `GoalConsidered`, `GoalScored`, `GoalSelected`, `PlanStepEmitted`, `PlanCommitted`, `PlanAbandoned`.
   - Reconstructed by `Engine.Observability.PlannerTraceProjection`.
   - Uses event payload fields such as `tick`, `entity_id`, and `plan_id`.
   - Does not read or depend on `KernelExecutionContext`.

These systems are not currently integrated. The execution context is not serialized into `KernelEvent`, so observability consumers cannot reconstruct `ProgramId` from the event stream.

## What Was Fixed Recently

- `KernelExecutionContext` was narrowed to `ProgramId` and `SourceCodeId`; vestigial scheduler subcodes were removed.
- `Simulation.EnterExecutionContext(...)` now restores both previous context and previous nesting depth.
- Explicit source overloads such as `SetAttribute(..., sourceCodeId, ...)` no longer override an already-active execution context.
- Kernel program runtimes no longer write directly to `simulation.Events.TryWrite`; direct writes are centralized through `Simulation.WriteKernelEvent`.
- Registered kernel program starts now require a nonzero mapped source code.
- Runtime source codes now exist for pressure, degeneracy, and planning in addition to process, gambit, upkeep, startup, relationship, and generator.
- Anonymous `RegisterSystem` callbacks no longer create a zero-source execution context.

Verified tests:

- `KernelExecutionContextTests.ExecutionScopeRestoresPreviousContextAfterDispose`
- `KernelExecutionContextTests.RegisteredProgramStartsEmitEventsWithTheirExecutionContextAndRestoreCallerContext`
- `KernelExecutionContextTests.AnonymousSystemDoesNotMaskExplicitSourceContext`
- `KernelExecutionContextTests.ProgramExecutionRestoresPreviousContextWhenSystemThrows`
- `Engine.Kernel.Tests` passed after the changes.
- `Engine.Kernel.Driver.Tests`, `Engine.Domain.Tests`, and `Engine.Host.Tests` also passed.

## Scheduler Metadata Finding

The scheduler now runs systems in registration order with cadence checks. Scheduler order and cadence are not part of `KernelExecutionContext`.

## Trace Reconstruction Findings

`PlannerTraceProjection` reconstructs traces from explicit planner trace events, keyed by:

- start tick
- entity id
- plan id

It does not consume:

- `KernelExecutionContext.ProgramId`
- `KernelExecutionContext.SourceCodeId`

Because `ProgramId` is not present in `KernelEvent`, event-log consumers cannot reconstruct execution context from the stream. They can only use explicit fields carried by each event type.

## Main Risk

The kernel currently has an in-memory execution context model, but the event stream does not carry that context as event metadata. That means the stated architecture, where non-kernel subsystems consume the kernel event stream as the single source of truth, is not satisfied for execution context.

The planner trace system works as a separate explicit-event reconstruction path, but it is not proof that general kernel execution context is available to observability. It also risks duplicating the purpose of context if both systems try to describe "what was executing and why."

## Open Design Question

The next decision should be whether trace reconstruction is supposed to be:

1. Event-specific, using explicit domain trace events like planner trace events, or
2. Context-driven, using generic execution context metadata attached to every kernel event, or
3. A strict split where context answers "where/under what execution scope did this event occur" and explicit trace events answer "what domain trace facts happened."

The current code is closest to option 3 in intent, but the implementation is incomplete because context is not serialized into kernel events.
