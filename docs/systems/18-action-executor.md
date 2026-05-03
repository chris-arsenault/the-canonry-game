# System 18: Action Executor

The current action layer is the gambit system. Future player cards may enable,
disable, weight, or modify gambits, but the runtime mechanism is already
entity-authored high-stakes actions.

## Purpose

Gambits let entities do meaningful things that are too large for low-altitude
physics and too small for generators.

Low altitude creates the live substrate: movement, influence, support, threat,
health loss/recovery, and relationship drift. Gambits read that world state and
attempt discrete actions. Generators later read the resulting graph and create
new named entities when major event conditions are met.

## Runtime Shape

A gambit declares:

- actor kind;
- target mode and target kind where applicable;
- cooldowns;
- preconditions;
- scoring/objective data when planning is used;
- success chance;
- ordered effects.

Preconditions use shared kernel query predicates. Trait-authored score and
success modifiers use the shared decision-modifier path. Score expressions,
reward expressions, target scheduling, and planning remain gambit policy.

Effects are usually graph and health mutations:

- create a relationship;
- change relationship strength;
- reverse or cull a relationship;
- modify actor or target health;
- mark historical outcomes.

Entity creation belongs to generators and should be rare in gambits.

Gambit mutations must execute through the shared kernel effect path. If a
gambit needs a new world mutation, add the `Simulation` mutation and shared
effect wrapper before exposing it to authored gambit data.

## Planning Boundary

The engine still contains behavior bytecode and planner support for gambits.
That infrastructure is retained because gambits need target selection,
precondition evaluation, scoring, and ordered effects.

This is distinct from the removed process deck. Low-altitude behavior no longer
uses standing drives or generated process definitions.

## Card Boundary

Cards are expected to affect which gambits are available and how they are
weighted. Cards should not own the per-tick execution loop. The simulation
should remain entity-authored: the player changes the possibility space, then
entities decide whether and when to act.

## Design Bias

Good gambits produce graph consequences that other systems understand. Prefer
relationship strength, relationship facts, health effects, traits, and locality
queries over one-off tags or isolated bookkeeping. If a gambit needs a numeric
gate, relationship strength should be the first option unless health is the
actual subject of the action.
