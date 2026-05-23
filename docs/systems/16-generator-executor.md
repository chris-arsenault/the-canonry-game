# System 16: Generator Executor

Generators are high-altitude event bundles. They create new named entities and
relationships when the world state satisfies their preconditions.

## Purpose

The world is richer than the named entities the sim tracks. A generator fires
when background world activity becomes important enough to name. "Create" means
the entity enters the named simulation, not necessarily that it was born from
nothing.

Without generators, lifecycle and relationship drift eventually wind the world
down. With generators, the player-selected generator set determines what kinds
of history the world can produce.

## Bundle Shape

A generator can:

- select one or more existing targets;
- resolve or create target-like produced entities when configured to do so;
- create named entities;
- create relationships among targets and produced entities;
- apply additional effects.

Generated entities should usually form a connected bundle. Every generator
should introduce relationships that the rest of the graph understands.

## Placement

Generators place entities on the physical and semantic planes. Placement may be
near a target, across the map, inside a region, or based on other authored
placement strategies.

Placement uses the same plane-aware kernel placement model for physical and
semantic coordinates. Area placement searches an authored radius band around a
point; locality placement searches inside the target locality. The old
semantic/physical shortcuts are not separate placement systems.

Produced entities can author preferred and allowed terrain per plane. Allowed
terrain is a hard filter; preferred terrain biases scored candidates after
locality, radius, occupancy, semantic bounds, and coverage constraints pass.
For physical regions and coverage, placement must respect terrain and named
coverage rules. For semantic placement, entities are placed within the bounded
semantic plane against semantic terrain.

If no legal placement candidate exists, the generator fire fails cleanly and
does not consume pulse budget. This keeps invalid placement from suppressing
later valid fires inside the same pulse window.

Generator culture placement uses the kernel culture-boundary model. Normal
placement prefers same-culture support and avoids another culture's
entrenched/nexus fields. Hostile intrusion is an explicit placement mode for
content that should appear in a foreign cultural field; it is not the default
for wilderness or ordinary across-map placement.

## Pulse Budget

Generators run on the main graph cadence, not every minor-actor tick. Fire
budget is reset by pulse windows. The default tuning is `PulseWidthTicks = 25`
main ticks and `FiresPerPulse = 1`. With the current
`MinorActorMainTickRatio = 5`, the default pulse width spans `125` simulation
ticks. A generator can fire on any eligible generator tick inside the current
pulse window while budget remains, instead of only on an exact modulo boundary.

When multiple generators are eligible, selection is weighted by authored pulse
weight. Fire budget limits how many successful fires can occur in one window;
it does not increase the low-altitude tick frequency.

## Preconditions

Generator preconditions should be satisfiable from the seed or from other
generators. If a precondition requires a relationship or kind that no seed or
generator can create, the generator is dead content.

Generators should avoid self-propagating infinitely through the same target.
Repeat control should be expressed as a causal limit: the generator must spend,
relieve, transform, or lock ordinary world state that made the fire eligible.
See [System 20: Generator Causal Limits](./20-generator-causal-limits.md).

If a precondition needs a numeric threshold, use relationship strength as the
default. For example, require an artifact to have a strong `anchored` or
`holds` relationship before it can cleanse a scar, rather than inventing an
artifact `power` attribute. Health thresholds remain valid for survival,
damage, decay, and repair. Locality-based queries are acceptable when nearby
map state, semantic position, or regional containment is the real gate.

Generator target gates, relationship gates, terrain gates, and produced entity
binding queries use the shared kernel query path.
Generator-specific code still owns pulse budgeting, target choice, production
orchestration, placement, and reference binding order.

## Effects

Generator-created entities, relationships, status changes, coverage changes,
and cleanup effects must execute through the shared kernel effect path. If a
generator needs a new world mutation, add the `Simulation` mutation and shared
effect wrapper before exposing it to authored generator data.

## Boundary

Low altitude creates conditions. Gambits create high-stakes entity actions and
relationship changes. Generators create new named entities and major event
bundles.
