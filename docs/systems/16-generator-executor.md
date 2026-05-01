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
Saturation should be expressed through relationships or graph facts the rest of
the simulation can understand, not hidden one-off bookkeeping.

## Boundary

Low altitude creates conditions. Gambits create high-stakes entity actions and
relationship changes. Generators create new named entities and major event
bundles.
