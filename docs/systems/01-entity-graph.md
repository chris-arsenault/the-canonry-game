# System 1: Entity Graph

The entity graph is the named-world state: entities, relationships, identity,
status, and history.

## Purpose

The graph tracks the named things the simulation cares about. The world is
richer than the named graph, but only named entities are simulated directly.
When an entity dies, collapses, or becomes irrelevant, it should become
historical rather than being erased.

## Current Contract

- Entities have a domain-declared kind, subtype, culture, status, coordinates,
  health, and traits.
- Relationships are typed edges with polarity, strength, and domain validation.
  Runtime event history and provenance describe how those edges came to exist
  or change.
- The graph topology is sparse and meaningful. Generators and gambits create or
  change relationships. Low-altitude physics mainly moves entities, changes
  culture influence, changes health, and adjusts relationship strengths.
- Relationship absence is meaningful. A generator or gambit can require that an
  edge is absent.
- Historical entities remain queryable so later generators, gambits, and UI
  views can refer to memory and lineage.

## Boundaries

- The graph owns identity and edges.
- Health values and traits are keyed by entity but are not a separate graph
  topology.
- Physical and semantic placement are entity state used by map and relationship
  programs.
- Map coverage and culture influence are map-owned spatial state, even when
  attached to named regions, settlements, or factions.
- Kernel programs must see compiled IDs, not domain strings.

## Current Writers

| Writer | Graph Mutation |
| --- | --- |
| Generators | Create new named entities and relationship bundles. |
| Gambits | Create or invert significant relationships, modify status, or apply major effects. |
| Lifecycle | Transitions entities out of active simulation when health reaches terminal states. |
| Relationship maintenance | Changes relationship strength, not topology. |
| Low-altitude verbs | Move entities and alter culture influence/coverage, not graph topology. |

## Quality Rules

- Prefer relationship strength changes over creating disposable relationship
  kinds.
- Do not create graph-only markers that no other system understands.
- If a relationship exists only to gate a generator, reject it and use ordinary
  world state: relationship strength, causal spend, durable output state, map
  terrain, or provenance for narrative history.
- Keep topology connected enough for targeting, but not so connected that target
  selection becomes random.
