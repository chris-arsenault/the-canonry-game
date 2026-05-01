# System 7: Relationships

Relationships are the graph edges between named entities. They encode actual
world history: who is tied to whom, by what relationship kind, with what
strength.

## Core Rules

Relationships exist only between specific entity instances. There is no default
neutral relationship between strangers.

Each relationship kind defines polarity, symmetry, exclusivity, valid endpoint
categories, and a cull threshold. Strength is always non-negative. A weak
alliance does not become a rivalry by crossing zero; it is culled, and a
separate event or gambit may create a rivalry.

Low altitude may modify relationship strength. It should not create new
relationship kinds or create routine new relationship topology.

## Maintenance

Relationship maintenance rules are authored per relationship kind and compiled
into the relationship kernel program. Rules can read:

- configured plane distance;
- configured plane co-presence;
- configured plane contact memory;
- same or different culture;
- source influence at target;
- target influence at source.

The rule plane is semantic meaning, not relationship type. A semantic
relationship may strengthen through physical closeness if encounters matter. A
spatial relationship may strengthen through semantic closeness if alignment or
ritual resonance matters.

## Topology Changes

New relationships are primarily created by generators and gambits. Existing
relationships can be strengthened, weakened, or culled by low-altitude drift,
gambits, generators, and future card effects.

Culling moves a relationship out of active use while preserving history. The
historical graph remains useful for narrative queries and future generator
preconditions.

## Interactions

Low-altitude verbs use relationships for targeting and affinity.

Map signals use relationships to emit support or threat around related
entities.

Effects use relationships, influence, and distance to modify attributes such as
health.

Gambits and generators use relationships as preconditions and outputs.
