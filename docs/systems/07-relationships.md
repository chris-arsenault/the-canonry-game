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

Relationship-strength low-altitude verbs are explicit relationship-target
commitments. They require an existing relationship of the authored kind and
direction, then apply a small signed strength delta through the relationship
kernel effect path.

## Strength Meaning

Relationship strength is meaningful only when "more" or "less" of that
relationship changes play. Topology-only relationships still store a strength
because the graph edge type is uniform, but authored systems should not spend
low-altitude verbs on making those edges stronger or weaker.

| Relationship | Strength? | Source / sink posture |
| --- | --- | --- |
| `controlled_by` | Yes | Culture influence and contact can strengthen control; decay weakens it. |
| `home_base` | No | Home is a binary attachment; move or replace the edge instead. |
| `allied_with` | Yes | Factions can tend or fray alliances through low altitude. |
| `rival_of` | Yes | Factions can inflame or cool rivalries through low altitude. |
| `led_by` | Yes | Culture/contact maintenance can strengthen leadership; decay weakens it. |
| `member_of` | Yes | Culture/contact maintenance and orca pod activity can strengthen membership; decay weakens it. |
| `holds` | No | Possession is a binary attachment; create, move, or remove the edge instead. |
| `created_by` | No | Provenance is historical, not a live intensity. |
| `memorializes` | Yes | Semantic proximity/contact can strengthen memory; decay weakens it. |
| `claimed_by` | Yes | Claimant culture influence/contact can strengthen a claim; points of interest can loosen claims; decay weakens it. |
| `trade_route` | Yes | Settlements can tend routes; route-drawing generators can spend route strength. |
| `founded_from` | No | Provenance is historical, not a live intensity. |
| `native_to` | No | Origin identity is binary. |
| `anchored` | Yes | Contact/proximity can strengthen an anchor; decay weakens it. |
| `conduit_of` | Yes | Semantic proximity/contact and abilities can strengthen conduits; transports can overstrain them; decay weakens them. |
| `part_of` | No | Structural membership is binary. |
| `ward_against` | Yes | Semantic proximity/contact can strengthen the ward; decay weakens it. |
| `corrupted_by` | Yes | Semantic proximity/contact and monsters can deepen corruption; decay weakens it. |
| `cleansed_by` | Yes | Semantic proximity/contact can sustain cleansing; decay weakens it. |
| `at_war_with` | Yes | Gambits can declare or end war; conflict maintenance changes war intensity; occurrences can exhaust wars. |
| `betrayed_by` | Yes | Conflict maintenance can deepen betrayal; decay weakens it. |
| `practiced_by` | Yes | Abilities can reinforce or let practice fade through low altitude. |
| `taught_by` | Yes | Culture/contact maintenance can strengthen lineage; decay weakens it. |

## Maintenance

Relationship maintenance rules are authored per relationship kind and compiled
into the relationship kernel program. Rules can read:

- configured plane distance;
- configured plane co-presence;
- configured plane contact memory;
- same or different culture;
- source culture influence at target;
- target culture influence at source.

The rule plane is semantic meaning, not relationship type. A semantic
relationship may strengthen through physical closeness if encounters matter. A
spatial relationship may strengthen through semantic closeness if alignment or
ritual resonance matters.

## Topology Changes

New relationships are primarily created by generators and gambits. Existing
relationships can be strengthened, weakened, or culled by low-altitude drift,
gambits, generators, and card effects.

Culling moves a relationship out of active use while preserving history. The
historical graph remains useful for narrative queries and generator
preconditions.

Default relationship queries operate on hot-active edges only: the relationship
itself must be active, and both endpoints must be active entities. Relationships
whose source or target has moved to historical status are preserved for explicit
history-facing queries, but they are excluded from ordinary low-altitude,
gambit, generator, map-signal, and relationship-maintenance hot paths.

## Interactions

Low-altitude verbs use relationships for targeting, signed fitness scoring, and
explicit relationship-strength mutation.

Map signals use relationships to emit support or threat around related
entities.

Effects use relationships, culture influence, culture slot occupancy, and
distance to modify health.

Gambits and generators use relationships as preconditions and outputs.

When a generator or gambit needs a numeric gate, relationship strength should
be the default source. Example: artifact cleansing should require a strong
`holds`, `anchored`, or other relevant relationship above the authored
threshold instead of a generic artifact `power` attribute. Locality-based
queries are also acceptable when physical or semantic context is the meaningful
condition.
