# System 18: Action Executor

The current action layer is the gambit system. Player-facing cards can expose,
enable, disable, weight, or modify gambits, but the runtime mechanism is
entity-authored high-stakes actions.

## Purpose

Gambits let entities do meaningful things that are too large for low-altitude
physics and too small for generators.

Low altitude creates the live substrate: movement, culture influence, support,
threat, health loss/recovery, minor labor, and relationship drift. Gambits read
that world state and attempt discrete actions. Generators later read the
resulting graph and create new named entities when major event conditions are
met.

## Runtime Shape

A gambit declares:

- actor kind;
- target mode and target kind where applicable;
- cooldowns;
- preconditions;
- provider selectors;
- score expression;
- success chance;
- ordered effects.

Preconditions use shared kernel query predicates. Trait-authored score and
success modifiers use the shared decision-modifier path. Score expressions,
target selection, and cooldown policy remain gambit policy. Cooldowns are
current expiry ticks indexed by actor occurrence and gambit slot. They are
cleared on retirement; there is no lifetime gambit-action history or implicit
recency-score decay.

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

## Opportunistic Execution

Gambits are opportunistic. On its tick, the gambit runtime evaluates eligible
gambits for each active entity, filters real targets through provider
selectors and preconditions, weights candidates by score, selects
one by weighted random choice, rolls success, and applies ordered effects.

This is distinct from the removed process deck. Low-altitude behavior no longer
uses standing drives or generated process definitions, and gambits no longer
ask a separate planner to satisfy blocked preconditions.

## Card Boundary

Cards are expected to affect which gambits are available and how they are
weighted. Cards should not own the per-tick execution loop. The simulation
should remain entity-authored: the player changes the possibility space, then
entities decide whether and when to act.

## Design Bias

Good gambits produce graph consequences that other systems understand. Prefer
relationship strength, health effects, traits, locality queries, and ordinary
relationship kinds over one-off tags or isolated bookkeeping. If a gambit needs
a numeric gate, relationship strength should be the first option unless health
is the actual subject of the action.
