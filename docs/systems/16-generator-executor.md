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

For physical regions and coverage, placement must respect terrain and named
coverage rules. For semantic placement, entities are placed within the bounded
semantic plane.

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

Generator target gates, relationship gates, terrain gates, causal-limit checks,
and produced entity binding queries use the shared kernel query path.
Generator-specific code still owns cadence, target choice, production
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
