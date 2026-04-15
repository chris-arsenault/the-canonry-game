# Codex Feedback

## Independent Audit: Goal / Process Architecture

Date: 2026-04-14

This note summarizes an independent audit of the current goal system, the mapping from goals to processes, and process execution. The main conclusion is that the architecture is currently inverted in a way that makes the system look data-driven and goal-driven on paper while behaving more like an attribute-matching utility loop at runtime.

## Core Finding

The system wants processes to be defined by who they act on, but the current implementation mostly discovers and selects them by what attribute name they eventually change.

That inversion shows up in several places:

- The planner infers providers from process writes instead of from an explicit target model.
- The wiring layer cannot formally express `Target` scope, even though the docs and runtime behavior depend on it.
- Goal generation and execution do not share a single source of truth.
- Domain definitions compensate by declaring misleading wiring so the planner can find candidates.
- Sub-goals are generated from lossy blocker data, so causal chains are weak and noisy.

## Structural Findings

### 1. Provider discovery is backwards

`GoalGenerator` claims providers come from process target refs, but it actually scans `procDef.Writes` and uses those writes to discover candidate providers.

Impact:

- The engine has no first-class way to say "this process acts on target entities of this shape."
- Provider discovery is based on outputs, not on the interaction the process requires.
- Processes that mutate targets are only discoverable if their write contracts are shaped to satisfy the planner.

## 2. The wiring model is missing the core concept

`AttributeRef` has `Self`, `Location`, `Related`, `MapCell`, `Resource`, `Pressure`, and `Structural`, but no `Target`.

Impact:

- The most important distinction in the goal/process system is not representable in wiring contracts.
- Planning, validation, and execution cannot agree on what a process consumes from self vs target.
- The docs describe a scoped contract model that the code cannot currently encode.

## 3. Planning and execution use different authority layers

Goal generation reads raw `ProcessDefinition` values from the domain config. Process execution goes through `ActivityRegistry`, `IProcess`, and `DataDrivenProcess`.

Impact:

- There are effectively two architectures in play: a data-definition architecture and an older runtime registry/interface architecture.
- It is possible for planning assumptions and execution behavior to drift apart.
- The DSL is not truly the single runtime source of truth.

## 4. Domain wiring is compensating for engine shape

In `TestDomain`, processes like `Propaganda` and `Attract Population` mutate target state, but their wiring is declared as `Self`.

Impact:

- The contracts are not reliable architectural definitions.
- Validation results are less meaningful because the declared reads/writes are not the real interaction pattern.
- Future content authors will be forced to learn engine quirks instead of writing truthful definitions.

## 5. Sub-goals are too lossy to be trustworthy

`PreconditionResult` reports blocking attributes as plain strings. The evaluator serves goals by matching only attribute names.

Impact:

- `self.ideology` and `target.ideology` collapse into the same blocker.
- A parent goal can generate sub-goals from every process that writes the parent attribute, not from the actual blocked process chain.
- The "enabler process" story is conceptually strong in the docs but weak in the runtime data.

## 6. Provider eligibility is state-driven instead of structurally driven

`GraphAttributeQuery` only returns candidates whose relevant attribute or resource is currently `> 0`, and `self` is then always added manually.

Impact:

- Candidate discovery is tied to transient numeric state rather than role/topology.
- Valid providers can disappear because a current value is zero.
- Invalid providers are still surfaced and filtered later by preconditions.

## 7. Card-gated decisions are not actually card-gated

The docs describe action cards enabling decisions at runtime, but current decisions are loaded into `ActivityRegistry` at startup. `TableauManager` is not wired to decision registration.

Impact:

- The runtime does not match the documented three-layer behavioral model.
- Action availability is effectively static.
- A major gameplay boundary is currently only conceptual.

## What The System Actually Is

At runtime, the system currently behaves like this:

1. Attribute deficits generate `(attribute, provider, urgency)` tuples.
2. A process is considered relevant if one of its declared writes matches the goal attribute name.
3. The evaluator picks a goal by urgency weight.
4. It evaluates registered runtime processes against that goal.
5. The chosen goal's provider is injected as the resolved target during execution.

This is a reactive utility loop with a goal-shaped filter. It is not yet a fully coherent goal-to-process planner.

## Why This Feels Weird In Practice

The blockers likely feel strange because the architecture is split at the exact seams that should be authoritative:

- process targeting
- provider discovery
- blocker propagation
- action availability
- contract validation

When those seams are implicit instead of explicit, the system still passes tests but becomes hard to reason about, hard to extend, and easy to accidentally define "inside out."

## Proposed Fixes

### Phase 1: Restore a single source of truth

- Make `ProcessDefinition` the authoritative runtime unit for planning and execution.
- Collapse or heavily reduce the `IProcess` / `IDecision` abstraction layer so it no longer owns behavior semantics.
- Treat registries as indexes over definitions, not as alternate behavior models.

Expected result:

- Planning and execution evaluate the same object graph.
- Runtime behavior becomes inspectable from definitions alone.

## Phase 2: Add an explicit target/provider contract model

- Extend `AttributeRef` or add a sibling contract type that can express `Self` and `Target` explicitly.
- Distinguish "selection refs" from "read/write refs" if needed.
- Add a first-class provider/target selector to `ProcessDefinition` instead of inferring targets from writes.

Expected result:

- The engine can formally represent who a process acts on.
- Provider search becomes intentional instead of guessed.

## Phase 3: Make goal generation target-driven, not output-driven

- Generate candidate providers from explicit target selectors, relationships, spatial scope, or kind constraints.
- Use write contracts only to determine what goals a process can satisfy, not who the provider is.
- Remove the fallback pattern where domain authors must fake `Self` writes to make target processes discoverable.

Expected result:

- Goal creation aligns with process intent.
- Provider enumeration becomes stable and explainable.

## Phase 4: Preserve blocker scope and causality

- Change blocker reporting from plain strings to scoped refs or a richer blocker object.
- Generate sub-goals from the actual blocked process candidate, not from every process that writes the parent attribute.
- Preserve whether the blocker belongs to self, target, location, relationship, resource, or pressure.

Expected result:

- Sub-goals become meaningful enabling steps instead of attribute-name echoes.
- The causal chain from failure to enabler becomes inspectable.

## Phase 5: Make validation match the real architecture

- Validate process definitions against truthful scoped contracts.
- Add checks for target-provider coherence.
- Add checks that blocker outputs can actually be served by some process.
- Add checks that action/decision definitions are only available through their enabling cards.

Expected result:

- Wiring validation becomes architectural validation instead of loose attribute accounting.

## Phase 6: Wire tableau to action availability

- Register and unregister decisions when action cards enter and leave the tableau.
- Stop loading card-gated decisions as globally available startup content.
- Make the evaluator read active decision availability from tableau-driven state.

Expected result:

- The runtime matches the documented design.
- Action cards genuinely define possibility space.

## Recommended Invariants

After refactoring, these should be true:

- A process definition fully explains how it discovers targets, what it reads, what it writes, and what goals it can satisfy.
- Goal generation and execution operate on the same definitions.
- A blocker preserves scope and can be traced to a specific failed process candidate.
- Domain contracts describe the truth, not engine workarounds.
- Decisions are unavailable unless enabled by active cards.

## Suggested Refactor Order

1. Unify planning and execution around `ProcessDefinition`.
2. Introduce explicit target/provider contracts.
3. Convert provider discovery to use those contracts.
4. Upgrade blocker data and sub-goal derivation.
5. Rework wiring validation around the new contracts.
6. Finish tableau-driven decision registration.

## Verification Notes

No code changes were made as part of the audit itself.

Behavior and wiring tests were run with:

`dotnet test tests/Engine.Tests/Engine.Tests.csproj --filter "FullyQualifiedName~Behaviors|FullyQualifiedName~Wiring"`

Result:

- Passed: 52
- Failed: 0

This is important because it suggests the current tests mostly validate the simplified architecture as implemented, not the stronger architecture described in the docs.
