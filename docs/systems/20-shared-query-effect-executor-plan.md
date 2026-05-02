# System 20: Shared Query And Effect Executor Plan

Status: implemented foundation; remaining work is decision-modifier cleanup.

This plan records the architecture direction for generators, gambits, traits,
and future card effects. It exists as a guardrail: do not add new parallel query
or mutation systems when extending these features.

## Architectural Principle

Generators, gambits, traits, cards, and low-altitude verbs may differ in when
they run, what actor or target they bind, how they score choices, and how often
they are allowed to act. Those differences belong at the highest caller level.

Once a system needs to query world state or mutate world state, it should use the
same kernel query and effect path as every other system.

The intended layering is:

- high-level caller selects timing, actor, target, cadence, score, and success;
- shared kernel query evaluator resolves predicates, bindings, evidence, and
  locality;
- shared kernel effect executor applies mutations through `Simulation`;
- `Simulation` and `WorldState` remain the only layer that writes world state
  events.

Do not add new trait-specific, gambit-specific, generator-specific, or
card-specific interpreters for relationship checks, health gates, locality
queries, or world mutations.

## Implemented Baseline

The codebase centralizes low-level mutations through `Simulation`/`WorldState`,
which keeps event emission localized. The first shared layer now exists:

- `KernelQuery` owns common relationship, trait, health, distance, count,
  overlap, and evidence predicates;
- `KernelEffectExecutor` owns the common high-level mutation caller surface and
  calls `Simulation` for event-emitting writes;
- gambits, generators, and trait-backed health effects use the shared effect
  path for world mutations.

Remaining cleanup:

- generators still have target, binding, relationship, saturation, production,
  and generator-effect logic;
- gambits still have target selectors, precondition evaluator, scoring
  expressions, success modifiers, and mutation bytecode;
- trait operations are authored in one place but currently compile into several
  consumers: effects, gambits, and low-altitude weighting.

This is a transitional state. New predicates and mutations should extend the
shared primitives rather than deepening caller-local implementations.

## Pressure Removal

All pressure-related concepts should be removed before or during this refactor.
Pressure was intended to be removed earlier, and the cleanup is incomplete.

Remove pressure from:

- constants and runtime ids;
- `KernelProgramCodes` and program registration;
- `Simulation`, `SimulationOptions`, kernel tuning, and snapshot persistence;
- `PressureProgramRuntime`, data, and state;
- pressure events, schema layout, projection, UI, and observability;
- gambit preconditions and operations such as pressure range gates and pressure
  deltas;
- domain config, gameplay config, Ice Remembers definitions, and seed data;
- Monte Carlo metrics, tests, docs, and any remaining references.

Replacement guidance:

- use relationship strength as the default numeric gate for generators and
  gambits;
- use health only when the subject is survival, damage, decay, repair, or
  recovery;
- use map signals, influence, coverage, resource values, and locality queries
  when the gate is spatial or environmental;
- do not introduce a new pressure-like aggregate under another name as part of
  this cleanup.

Useful cleanup search terms:

- `Pressure`
- `pressure`
- `Pressures`
- `PressureRange`
- `AddPressure`
- `PressureChangedKernelEvent`
- `KernelProgramCodes.Pressures`
- `PressureSourceCodeId`

The pressure docs should either be removed or replaced with a short tombstone
that explains the removal.

## Target Kernel Primitives

Add a shared query primitive. It should support:

- actor, target, related entity, and named binding aliases;
- entity kind, subtype, status, culture, and trait predicates;
- health range predicates;
- relationship kind, direction, strength, polarity, and fact predicates;
- relationship endpoint count predicates;
- physical and semantic distance predicates;
- colocated and nearby entity predicates;
- coverage, influence, map signal, terrain, and resource predicates;
- deterministic count, exists, any, all, and evidence-style queries.

Add a shared effect primitive. It should support:

- health set and delta;
- resource set and delta;
- relationship create, strength set, strength delta, and cull;
- entity create, status set, physical move, and semantic move;
- terrain, coverage, and influence mutations;
- trait assignment and removal if traits ever become mutable after creation.

Every effect operation must execute through `Simulation` methods. If an effect
needs a mutation that only `WorldState` or a subsystem can perform today, add a
`Simulation` method first so source ids and event emission remain centralized.

Do not include pressure operations in the shared effect primitive.

## Decision Modifiers

Not every trait operation is a world mutation. Low-altitude weights, gambit
scores, and gambit success chances are decision policy modifiers.

If those remain trait-authored, they should compile to a shared
`DecisionModifier` primitive, not to ad hoc code inside each caller. A decision
modifier may affect scoring or selection but should not be treated as an effect
that mutates world state.

Decision modifiers should share query predicates with the kernel query
evaluator. Their caller-specific difference is the output slot they modify, such
as `LowAltitudeLocationWeight`, `GambitScore`, or `GambitSuccessChance`.

## Refactor Phases

1. Freeze expansion of parallel paths. Completed.
   Do not add new trait operation kinds, gambit precondition kinds, generator
   query filters, or card effect paths unless they are going through the shared
   query/effect design.

2. Remove pressure. Completed.
   Delete pressure runtime, config, events, persistence, projection, docs,
   metrics, and tests. Replace current pressure gates or deltas with
   relationship strength, health, map signal, influence, resource, or locality
   predicates.

3. Extract shared query evaluation. Foundation completed.
   Move duplicated relationship, trait, health, distance, count, influence,
   and locality predicates out of generator, gambit, effects, and low-altitude
   runtimes into one kernel query evaluator.

4. Extract shared effect execution. Foundation completed.
   Move mutation bytecode and generator effect execution behind one effect
   executor. The executor should call only `Simulation` mutation methods.

5. Recompile existing systems to shared primitives.
   Generators should compile their target gates, bindings, relationship
   creation, entity creation, and cleanup effects to shared query/effect
   instructions. Gambits should compile preconditions and ordered effects to
   the same instruction families. Trait health effects should remain data
   authored but use the shared query/effect executor.

6. Normalize decision modifiers.
   Move low-altitude trait weight modifiers and gambit trait score/success
   modifiers into one shared decision modifier mechanism, or deliberately keep
   them as caller-local policy with no claim that they are world effects.

7. Add architecture tests. Foundation completed.
   Add tests or analyzers that catch direct world mutation bypasses, pressure
   concept reintroduction, and duplicate query implementations in new systems.

## Invariants

- A new world mutation must have one canonical `Simulation` entry point.
- A new query predicate must be implemented once in the shared kernel query
  evaluator.
- A new authored effect must compile to the shared effect executor.
- A new authored scoring modifier must compile to the shared decision modifier
  path, if that path exists.
- Relationship strength is the default numeric gate for generator and gambit
  conditions unless the subject is health or a spatial/environmental value.
- Locality-based queries are acceptable when proximity, containment, influence,
  or regional map state is the actual condition.
- Pressure must not be used as a gameplay, simulation, telemetry, or
  persistence concept.

## Definition Of Done

This refactor is complete when:

- pressure references are gone except in historical migration notes;
- generators, gambits, traits, and cards share query predicates;
- generators, gambits, traits, and cards share world mutation execution;
- decision modifiers are either centralized or explicitly limited to
  caller-local policy;
- event emission for all world mutations remains localized in
  `Simulation`/`WorldState`;
- tests prevent accidental reintroduction of parallel effect paths.
