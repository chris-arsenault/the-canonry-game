# Shared Query And Effect Executor

This note describes the kernel execution boundary for shared queries, effects,
and decision modifiers. It is not a game-system design document; it is a
guardrail for extending kernel execution without creating parallel query or
mutation paths.

## Principle

Generators, gambits, traits, cards, and low-altitude verbs may differ in when
they run, how they choose actors and targets, how they score choices, and how
often they are allowed to act. Those differences belong at the caller level.

Once a system needs to query world state or mutate world state, it should use
the shared kernel path:

- caller-level code selects timing, actor, target, cadence, score, and success;
- `SharedQuery`, `QueryPredicate`, `QueryBinding`, and `QueryEvaluation`
  describe current shared query instructions;
- `KernelQuery` owns reusable relationship, trait, health, distance, count,
  overlap, evidence, and binding predicates;
- `KernelEffectExecutor` applies high-level mutations through `Simulation`;
- `Simulation` and `WorldState` remain the event-emitting mutation boundary;
- `DecisionModifier` owns trait-authored decision modifiers for gambit score,
  gambit success chance, and low-altitude weighting.

Do not add trait-specific, gambit-specific, generator-specific, or card-specific
interpreters for relationship checks, health gates, locality queries, decision
modifiers, or world mutations.

## Current Split

Shared primitives own:

- generator target gates, relationship gates, and produced entity binding
  queries;
- gambit precondition predicates;
- trait-backed health effect gates;
- trait-authored gambit score/success and low-altitude weight modifiers;
- common world mutations used by effects, gambits, and generators.

Caller-local policy still owns:

- generator cadence, target choice, production orchestration, and reference
  binding order;
- gambit actor/target scheduling, score expressions, direct target selection,
  and ordered operation sequencing;
- low-altitude movement, influence policy, and target selection.

That split is intentional. The shared layer answers "what does this predicate
mean?" and "how is this mutation applied?" The caller answers "when do I ask?"
and "what do I do with the score?"

## Extension Rules

- A new query predicate belongs in `KernelQuery` and should be exposed through
  the shared query instruction shape when it is authored data.
- A new world mutation must have one canonical `Simulation` entry point and a
  `KernelEffectExecutor` wrapper before higher-level programs use it.
- A new authored effect should compile to the shared effect path.
- A new authored scoring or weighting modifier should use `DecisionModifier`.
- Relationship strength is the default numeric gate for generator and gambit
  conditions unless the subject is health, a map signal, influence, coverage,
  or locality.
- Health is reserved for survival, damage, decay, repair, recovery, and
  lifecycle.
- Locality-based queries are valid when proximity, containment, influence, or
  regional map state is the actual condition.
- Do not introduce pressure or another macro aggregate as a shortcut around
  graph, health, map-signal, or locality state.

## Why Not

### Why Not Macro Aggregates

A broad aggregate such as pressure makes generator and gambit gates easy to
write but hard to inspect. It hides the actual source of tension behind a
single number, and future systems can begin depending on that number instead of
the entity graph. The current architecture uses relationship strength, health,
traits, map signals, influence, coverage, and locality directly so
events can be explained from visible world state.

### Why Not Caller-Local Query Interpreters

If generators, gambits, traits, and cards each interpret relationship gates,
health ranges, locality, and trait checks themselves, future fixes will update
one path and miss another. Shared query predicates keep those meanings in one
place while still letting callers own timing, targeting, and scoring policy.

### Why Not Trait-Name Special Cases

Traits are content. Runtime systems should not check for specific trait names
or ids. A trait should carry operation data that compiles to a shared health
effect, query gate, or decision modifier. Adding or removing a normal trait
should stay a content-authoring action.

### Why Not Direct World Mutation From Programs

Direct writes from high-level programs spread event emission and provenance
rules across the runtime. Mutations should go through `Simulation` and the
shared effect wrapper so event source, provenance, and validation stay
localized.

## Guardrails

Architecture tests should continue to catch:

- direct world mutation bypasses from higher-level effect callers;
- direct raw traversal from effects, gambits, and generators;
- caller-local generator query interpreters;
- caller-local gambit precondition query interpreters;
- trait decision modifiers outside `DecisionModifier`;
- removed attribute buckets or pressure-like concepts returning to runtime
  code.

Behavior tests should cover the actual shared paths, not just source-shape
guards. Current coverage includes shared query evaluation, shared decision
modifier evaluation, target/related trait gates for health effects, and trait
gambit score/success modifiers.
