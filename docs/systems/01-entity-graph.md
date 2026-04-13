# System 1: Entity Graph

The single source of truth for world state. All other systems read from and write to the graph.

---

## Purpose

The entity graph represents everything that exists or has existed in the simulation. It is both the living state of the world and a record of what came before. Entities persist through time -- a destroyed nation doesn't vanish, it transitions to a historical status while remaining queryable.

---

## Design Decisions

### The graph is both living and historical

The graph has a time component. Entities exist across time -- they are created, they change, and they may cease to be active, but they remain in the graph. A current nation can have a "successor_of" relationship to a historical one. A living hero can reference the nation they once served that no longer exists.

Whether this is represented as one combined graph with status flags or separate living/historical stores is an implementation decision. The design contract is: historical entities are always queryable and can be referenced by current entities.

### Relationships are created only by cards

Game systems (low-altitude physics) shift relationship STRENGTH continuously -- proximity, shared culture, competing claims all modify existing relationship values. But game systems do NOT create new relationship links. New relationships come into existence only through card execution: generators create batches of pre-connected entities, action cards create new links (alliances, rivalries), and system cards may create links as part of their mid-altitude effects.

This means the graph's topology (which entities are connected) is card-controlled, while the graph's weights (how strong those connections are) are physics-controlled. The card system defines the structure. The game systems modulate intensity.

**Implication:** Two nations that share a border but have no relationship link have no formal diplomatic connection. Their proximity may create pressure (via low-altitude systems), but until a card creates a relationship, that pressure has no relationship to modulate. Cards like "Establish Diplomacy" or "First Contact" would create the initial link that game systems then evolve.

### Entities exist independent of their relationships

An entity's identity is intrinsic, not relational. A nation with no settlements, no heroes, and no allies is still a nation (a government-in-exile, a diaspora polity, a name on a map with no substance). It may be weak, purposeless, and trending toward historical status, but it exists.

Destruction is a status change, not a deletion. The conditions for transitioning to "historical" or "destroyed" are kind-specific (a settlement with zero population, a hero whose health reaches zero, a nation that loses all territory AND all members), but the entity remains in the graph.

### Hierarchy is encoded through relationships, not structure

The graph is flat -- all entities are peers. There is no structural nesting (a nation does not "contain" settlements as sub-objects). Instead, hierarchy is expressed through relationship semantics:

- "controls" relationships from nation to settlement encode political hierarchy
- "located_in" relationships from settlement to region encode spatial hierarchy
- "member_of" relationships from hero to faction encode organizational hierarchy
- "practiced_in" relationships from culture to settlement encode cultural presence

Some relationships carry semantic meaning beyond connection:
- **Containment semantics:** "controls" implies the nation governs the settlement. Queries like "what settlements does this nation have" traverse "controls" relationships.
- **Exclusivity rules:** some relationship kinds encode limits. A settlement can only be "controlled_by" ONE nation at a time. But a settlement can be "influenced_by" MANY cultures simultaneously.
- **Cardinality constraints:** some relationships encode "no more than N" or "at least one" rules. These are properties of the relationship kind definition, not of individual relationship instances.

This approach allows semantic querying ("give me everything this nation controls, and everything those settlements contain") without hard-coding hierarchy into the graph structure. New relationship kinds with new semantic meanings can be added through entity kind definitions without changing the graph system.

### Multiple simultaneous relationships are the norm

An entity can have many relationships of many types simultaneously:
- A hero can be loyal_to a nation, member_of a faction, practitioner_of a culture, holder_of an artifact, located_at a settlement
- A settlement can be controlled_by a nation, influenced_by multiple cultures, home_to multiple factions, located_in a region
- A nation can control many settlements, be allied_with some nations, rival_of others, and led_by a hero

The relationship kind definition specifies constraints:
- **Exclusive:** only one of this relationship kind per entity (a settlement has one controller)
- **Non-exclusive:** many allowed (a settlement has many cultural influences)
- **Symmetric:** if A is allied_with B, then B is allied_with A
- **Asymmetric:** A can be rival_of B without B being rival_of A
- **Required:** an entity of this kind must have at least one of this relationship (a settlement must be located_in a region)

---

## What Lives in the Graph

- **Entity identity:** id, kind, subtype, name, status
- **Entity position:** coordinates (links entity to map/spatial system)
- **Entity timestamps:** createdAt, updatedAt (time component)
- **Relationships:** typed, directed links between entities with strength and metadata
- **Relationship constraints:** defined per relationship kind (exclusivity, symmetry, cardinality)

## What Does NOT Live in the Graph

- **Attribute values** (prominence, stability, population): owned by the Attributes system, keyed by entity ID. The graph knows entities exist; the Attributes system knows their numeric state.
- **Pressure values:** derived by the Pressure system from attributes and relationships. Not stored in the graph.
- **Bulk flow data:** regional phenomena computed by the Bulk Flow system. Not entity-level data.
- **Map terrain/biome data:** owned by the Map/Spatial system. Regions may be entities in the graph, but their terrain data lives in the spatial system.

---

## Interactions with Other Systems

| System | Reads | Writes |
|--------|-------|--------|
| Entity Kinds (#2) | Defines valid kinds, subtypes, relationship constraints | -- |
| Attributes (#3) | Entity IDs to attach attributes to | -- |
| Map/Spatial (#4) | Entity coordinates for spatial queries | Entity position updates |
| Entity Behaviors (#6) | Entity state, relationships for behavior evaluation | -- (behaviors modify attributes, not graph topology) |
| Relationships (#7) | Relationship links and strength values | Relationship strength changes |
| Lifecycle (#9) | Entity status | Entity status changes (active -> historical) |
| Generator Executor (#16) | Current graph state for precondition checks | New entities and relationships |
| System Executor (#17) | Current graph state for system logic | Relationship modifications (via cards) |
| Action Executor (#18) | Current graph state for action evaluation | New relationships, entity status changes |

---

## Open Questions (Deferred)

- **Time component representation:** how the historical dimension is stored and queried. Design contract is clear (historical entities are queryable); implementation is deferred.
- **Query optimization:** which queries are hot paths that need optimization. Deferred to implementation.
- **Change notification model:** how other systems learn about graph changes. Deferred to implementation.
- **Graph size bounds:** how large the graph gets in a typical 4+ hour session. Unknown until we have entity generation rates.
