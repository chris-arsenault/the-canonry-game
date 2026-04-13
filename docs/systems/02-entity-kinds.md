# System 2: Entity Kinds

The hard-coded schema. Defines the vocabulary of the simulation -- what classes of things exist and what rules govern them.

---

## Purpose

Entity kinds define the classes of objects that can exist in the simulation. Kinds are the primary organizing principle: attributes, relationships, low-altitude behaviors, and card targeting are all kind-locked. A kind is not a type -- it is a class of object. Types (subtypes) and statuses exist within a kind.

---

## Design Decisions

### Kinds are classes, not types

A kind represents a fundamental class of object in the simulation. Within a kind, entities are further differentiated by:
- **Subtype:** a specialization within the kind. A location might be subtyped as settlement, city, fortress, ruin. A mortal entity might be subtyped as warrior, merchant, scholar.
- **Status:** lifecycle state. Active, historical, dormant, destroyed, etc. Status values are kind-specific.

An entity may change subtype (a settlement grows into a city) or status (active becomes historical). An entity NEVER changes kind. Kind is permanent identity.

### Kinds lock everything

Kind is where behavioral rules are tied. Specifically:
- **Attributes:** each kind defines which attributes its entities have. A location kind has population and development. A political entity kind has stability and military strength. Attribute sets do not cross kinds.
- **Relationships:** each kind defines which relationship types it can participate in, and in which role (source or target). A political entity can have a "controls" relationship to a location. A mortal entity cannot have a "controls" relationship. Invalid relationships cannot be created.
- **Low-altitude behaviors:** each kind has its own behavioral simulation. Political entities experience border tension. Locations attract/repel population. These behaviors are kind-specific code.
- **Card targeting:** cards target kinds. A generator card creates entities of a specific kind. A system card affects entities of specific kinds. An action card enables actions for actors of specific kinds.

### If you strip kind-specific rules, all entities are the same

At the graph level, all entities are identical: an ID, a kind label, a subtype, a status, coordinates, timestamps. The differences are entirely in what rules, attributes, relationships, and behaviors the kind label activates. Kind is the dispatch key for everything.

### All kinds are content

There is no divide between "structural" and "content" kinds. All kinds represent things in the simulation world. Some kinds are physical (locations, objects). Some are metaphysical (political entities, organizational entities). Some are temporal (time periods, events). But they are all content that the player observes and that cards interact with.

### Entity kind list is NOT finalized

The specific set of entity kinds requires its own dedicated design review. The kinds referenced elsewhere in these docs (region, settlement, nation, culture, hero, artifact, faction, occurrence, era) are working placeholders, not a reviewed design. Key open questions include:

- **Culture as kind vs. attribute:** culture may be better represented as an attribute of other entities (settlements have a culture value, nations have a dominant culture) rather than a standalone entity kind. This needs exploration.
- **Kind granularity:** the right level of abstraction. "Location" as a kind with subtypes (settlement, city, fortress, ruin, landmark) vs. separate kinds for each. "Mortal entity" as a kind with subtypes (hero, monster, creature) vs. separate kinds.
- **Physical vs. metaphysical vs. temporal:** these are categories of kinds, not kinds themselves. The specific kinds within each category are TBD.

---

## Kind Definition Structure

Each kind definition specifies:

- **Name:** the kind identifier
- **Subtypes:** enumeration of valid subtypes within this kind
- **Statuses:** enumeration of valid lifecycle states for this kind
- **Attribute schema:** which attributes entities of this kind have, with min/max/default
- **Valid relationships:** which relationship kinds this entity can participate in, with role (source/target), exclusivity rules, cardinality constraints
- **Low-altitude behavior set:** which behavioral simulation runs for entities of this kind
- **Card targetability:** which card types can target this kind and how

---

## Subtype and Status

### Subtypes

Subtypes specialize an entity within its kind. They may:
- Modify attribute defaults or ranges (a city subtype has higher population capacity than a village subtype)
- Enable or disable specific relationship types (a fortress subtype can have "garrisons" relationships that a village cannot)
- Weight behavioral priorities differently (a merchant subtype hero prioritizes trade-related movement)
- Affect card targeting (some cards may target specific subtypes, not just kinds)

An entity CAN change subtype. This represents evolution within the same class: a settlement becoming a city, a warrior becoming a commander, a faction becoming a state religion. Subtype change may be triggered by attribute thresholds (population exceeds X -> settlement becomes city) or by card effects.

### Statuses

Statuses represent lifecycle state. They are kind-specific (different kinds have different valid statuses) but share common patterns:
- **Active:** the entity is currently participating in the simulation
- **Historical:** the entity existed but is no longer active. Remains in the graph for reference.
- Additional kind-specific statuses as needed (dormant, exiled, ruined, etc.)

Status changes may be triggered by low-altitude systems (health reaches zero -> status becomes historical), by card effects, or by other entity state changes (a nation whose last settlement is destroyed may transition to exiled or historical).

---

## Parallel Existence

Entities of different kinds can represent aspects of the same real-world concept and exist in parallel:
- A city (location kind) and a nation (political entity kind) are separate entities connected by a "controls" relationship. The city is the physical place. The nation is the political entity that governs it.
- A faction (organizational kind) that seizes territory does not become a nation. A new nation entity is generated by a card, connected to the faction by a "founded_by" or "political_arm_of" relationship. Both continue to exist.
- A person (mortal kind) who founds a religion does not become the religion. The religion entity is generated, connected to the founder.

This avoids entity kind changes and keeps the graph composable: complex real-world concepts are modeled by multiple simple entities connected by relationships.

---

## Interactions with Other Systems

| System | How Entity Kinds Interacts |
|--------|---------------------------|
| Entity Graph (#1) | Kind definitions constrain what entities and relationships can exist in the graph |
| Attributes (#3) | Kind defines which attributes an entity has |
| Entity Behaviors (#6) | Kind determines which low-altitude behavioral simulation runs |
| Relationships (#7) | Kind constrains which relationship types are valid |
| Card Executors (#16-18) | Cards target kinds and subtypes for preconditions and effects |
| Lifecycle (#9) | Kind defines valid status transitions |

---

## Open Questions (Deferred to Entity Kind Review)

- **The specific kind list.** Current working set is placeholder. Needs dedicated design session.
- **Culture: kind or attribute?** Strong arguments both ways. As a kind, culture is a first-class entity with its own behaviors, relationships, and prominence. As an attribute, culture is a property of locations and entities, spreading through low-altitude dynamics. Needs exploration.
- **Kind granularity level.** Broad kinds with many subtypes (fewer kinds, more subtypes) vs. narrow kinds (more kinds, fewer subtypes). Affects card targeting granularity and behavioral code organization.
- **Kind categories.** Physical, metaphysical, temporal -- these are useful organizational labels but may or may not affect game mechanics. TBD whether category matters to the simulation or is purely taxonomic.
