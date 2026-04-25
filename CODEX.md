# CODEX.md

## Purpose

This file is the Codex operating ruleset for this repository.

Codex should treat this as the repo-specific implementation contract for future work. If older agent guidance conflicts with this file, follow this file.

## Project Overview

The Canonry Game is a high-altitude civilization simulation with card-driven world-shaping.

The player does not directly command actors. The player defines the world's nature through cards:

- generators define what the world becomes
- systems define how forces behave
- actions define what actors can do

Current engine work is focused on a data-driven simulation architecture in Godot 4 + C#.

## Non-Negotiable Architecture Rules

### Critical Reference: Simulation Kernel Discipline

Before changing engine runtime code, read `docs/simulation-kernel-discipline.md`.

That document is the controlling architecture direction for simulation-thread
work: no runtime string creation in the kernel, no runtime string lookup after
domain registration, no steady-state hot-path allocations, primitive event
boundaries, and one sim writer thread.

Existing engine violations are migration debt, not precedent. New work should
move the engine toward that model and should not add avoidable new violations.

### 1. No Domain Mixing

Engine code must remain domain-agnostic.

- No narrative-domain terms in `src/Engine/`
- No special-case content logic in engine systems
- Domain-specific truths belong in domain config/data, not engine code

If a domain term appears in engine code, treat it as a bug.

### 2. No Legacy Preservation

There is no working legacy behavior model worth preserving through compatibility layers.

Do not add:

- backward-compatibility shims
- adapter layers whose only purpose is to preserve obsolete abstractions
- dual runtime paths
- parallel planner interpretations
- fallback code kept only to avoid deleting old structure

Prefer direct replacement over transitional cruft.

### 3. ProcessDefinition Is the Authority

The simulation must converge on a single definition-backed runtime model.

- Planning and execution must use the same semantic source of truth
- Wrapper/index structures may exist only for lookup/performance
- No interface or registry should define behavior semantics independently

### 4. Multi-Step Planning Must Be Emergent

The engine must support multi-step behavior without hand-authored process chains.

The intended model is:

1. an entity has a desired state change
2. a terminal process is blocked
3. the blocker becomes a sub-goal
4. another process satisfies that sub-goal
5. the terminal process eventually becomes executable

Do not solve this by hardcoding explicit process-to-process chains.

### 5. Planning Facts Must Be Explicit

The planner must preserve enough structure to be truthful and inspectable.

That means:

- explicit provider/target semantics
- explicit goal effects
- explicit blocker scope
- explicit plan identity across parent and sub-goals
- explicit runtime decision availability

If a planning behavior depends on an implicit convention, the representation is probably too weak.

### 6. Uniform Evaluation

No per-kind behavior code in the evaluator.

Entity kinds differ by data:

- kinds
- attributes
- processes
- decisions
- cards

The engine should not branch on settlement vs NPC vs nation for planner semantics.

### 7. Systems Must Stay Separable

Each system should remain independently testable and replaceable.

Systems communicate through:

- world state
- defined contracts
- messages/events

Avoid hidden coupling between planner, execution, cards, pressure, relationships, and UI.

### 8. Everything Important Must Be Visible

The planner will become player-facing.

Do not add hidden AI state that cannot be explained through runtime reports or UI-facing inspection data. The UI should be able to answer:

- what this entity wants
- what process path it is attempting
- what blocked it
- what sub-goal it is pursuing now

### 9. Everything Must Be Serializable

New planner state must be serializable from day one.

If a new planning structure cannot be saved and restored cleanly, it is incomplete.

### 10. Strict Tick Semantics

Tick ordering must remain deterministic and inspectable.

No planner or execution code should reach backward in the tick or rely on ambiguous within-tick ordering.

## Current Refactor Direction

The original planner refactor body is complete. New work should preserve that architecture rather than reopening compatibility-oriented changes.

Current behavior/resource work should continue to align with these completed principles:

- unify planning/execution around `ProcessDefinition`
- add explicit planner contracts
- rebuild provider search
- preserve blocker and plan identity
- add bounded recursive planning/replanning
- align validation/observability/tests
- make card-gated decisions runtime-driven

Deferred follow-up work is now tracked as standalone backlog items in GitHub project `#2` rather than under the old umbrella.

## Workflow Rules

### Git Workflow

This repository currently uses a direct-to-`main` workflow.

- Commit directly to `main`
- Push directly to `origin/main`
- Do not create feature branches, draft PRs, or branch-based handoff flows unless explicitly requested
- Commit regularly during substantial work instead of batching everything into one late publish step

For substantial work:

1. Confirm the issue(s) being executed
2. Move the relevant issue to `In Progress` on project `#2`
3. Implement the smallest coherent slice that satisfies the issue
4. Add or update tests with the change
5. Run the relevant validation commands
6. Update the issue with progress and notes

If a task is not yet ticketed and is large enough to matter architecturally, ticket it first.

## Testing Rules

### Tests Are Diagnostics Unless Marked As Invariants

Do not optimize game design or simulation tuning merely to make a broad test suite green.

There are two distinct classes of tests:

- Invariant tests encode hard design constraints. If an invariant test fails, fix the implementation or the domain data so the invariant is true, unless the user explicitly changes the invariant.
- Ordinary tests are diagnostic probes. If an ordinary test fails, first determine whether it exposes a real intended-behavior bug. If it does not, update, narrow, rename, or remove the test rather than changing game behavior to satisfy it.

When a failure concerns generator frequency, simulation balance, map utilization, death rates, process weighting, or other game-design tuning, do not tune the domain just to satisfy the test. Surface the failure as design data unless the user has already accepted the test as an invariant.

Before declaring a planner change complete:

- default to `make ci` as the single verification pass
- do not run separate `dotnet build` / `dotnet test` commands and then run `make ci` again in the same normal flow
- use narrower commands only when debugging a failure, shortening a tight edit loop, or isolating a specific broken area before the final `make ci`
- prefer updating tests that currently encode the wrong architecture rather than preserving them

If a current test suite validates obsolete behavior, revise the tests to validate the intended architecture.

## Documentation Rules

When architecture changes materially:

- update the relevant docs
- prefer correcting the architecture description over preserving stale wording
- keep docs aligned with the implemented system, especially for planner semantics

## Implementation Biases

When choosing between two approaches, prefer the one that is:

- more explicit
- easier to inspect
- easier to validate
- easier to serialize
- easier to reason about as a single runtime model

Avoid approaches that are:

- clever but implicit
- temporarily compatible with obsolete structure
- dependent on naming conventions instead of typed contracts
- hard to surface in the UI

## Repository Commands

Use the existing Make targets where appropriate:

```bash
make ci
make lint
make build
make test
make clean
```

For GitHub tracking, use `gh` and keep project `#2` updated when executing ticketed work.
