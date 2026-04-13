# System 17: Generator Executor

Creates new entities when simulation conditions are met. Defines what the world BECOMES.

---

## Purpose

Generator cards are creation valves. While in the tableau, they watch the simulation for conditions that warrant new entities -- a frontier ripe for settlement, a culture reaching critical mass, a power vacuum ready for a new nation. When conditions are met and a valid target location exists, the generator fires and creates a batch of pre-connected entities.

Generators are how the world evolves. Without active generators, no new entities appear and the world slowly winds down through lifecycle decay.

---

## How It Differs from Base

Generators extend the card executor base with:

### Batch entity creation

Generators don't create single entities -- they create batches of pre-connected entities in a single firing. A "Found Settlement" generator creates: the settlement entity + initial population attributes + a "controlled_by" relationship to the nearest nation + a "located_in" relationship to the region + possibly a founding hero or cultural affiliation.

This follows the canonry pattern: templates create batches of pre-connected entities. The batch definition is part of the card's DSL definition. Each entity in the batch can reference others via variable binding ($settlement, $founder, $culture) and establish relationships between them.

### Spatial placement

Most generators need to place entities on the map. Placement rules are part of the card definition:
- **Anchor:** where to start looking (near the target entity, within the target region, near high-population areas, near the actor's territory)
- **Constraints:** what the cell must be (habitable, unoccupied, specific terrain type, within N distance of something)
- **Strategy:** how to pick from valid candidates (random, most fertile, most defensible, most connected)

The canonry's placement system used anchor + steps (anchor_region, seed_region, sparse, random). A similar approach works here but with spatial/map integration.

### Creation context

When a generator fires, the created entities inherit context from the world state at creation time:
- Cultural affiliation may inherit from the nearest dominant culture or the controlling nation
- Names are generated based on cultural naming rules (from the narrative domain system)
- Initial attribute values may be influenced by regional conditions (a settlement founded in fertile land starts with higher prosperity potential)
- The founding event is recorded (which generator card, which tick, which conditions)

### Cadence

Generators check preconditions less frequently than systems or actions. A generator firing every tick would flood the world with entities. Typical cadence: every 10-50 ticks (TBD through playtesting). The card's cooldown further throttles: even if preconditions are met again immediately, the generator won't fire until the cooldown expires.

---

## Generator-Specific Preconditions

In addition to the base precondition types, generators commonly use:

- **Population threshold:** "a settlement in this region has population above X" (something needs to grow large enough to spawn something new)
- **Vacancy:** "there exists a valid placement location" (the world must have room)
- **Absence:** "there is no entity of kind K with subtype S in region R" (don't create duplicates)
- **Pressure-driven:** "demographic pressure in region R exceeds X" (population pressure drives expansion)
- **Relationship-driven:** "nation A has no allied_with relationships" (isolation drives faction creation)

### The saturation concept

From the canonry: saturation limits prevent generators from over-populating. A generator for heroes might have a saturation limit: "no more than 5 heroes per settlement." This is a precondition that checks current entity density against a threshold, preventing runoff creation.

---

## Achievement Conditions (Deck Modification Triggers)

Generator cards track their creation count and creation context toward deck modification events:

- **Count-based:** "this generator has fired N times this run"
- **Variety-based:** "this generator has created entities in N distinct regions"
- **Quality-based:** "this generator has created an entity that subsequently reached [subtype/attribute threshold]"
- **Combination-based:** "this generator has created entities of N distinct subtypes"

These conditions are visible to the player as progress bars or checklists on the card's detail view, giving them something to work toward.

---

## Example Generator Card (Illustrative)

```
generator: "Frontier Settlement"
mana_cost: 3
cooldown: 20 ticks
cadence: 10 ticks
tags: [creation, expansion, settlement]

preconditions:
  AND(
    entity_count(kind="location", subtype="settlement", region=$region, max=3),
    pressure("demographic", region=$region, min=40),
    target_available(
      type: cell,
      constraints: [habitable, unoccupied, within_distance(nearest_settlement, 10)],
      pick: most_fertile,
      bind: $site
    )
  )

creation:
  - entity: $settlement
    kind: location
    subtype: settlement
    attributes: { population: 100, development: 10 }
    placement: $site
    culture: inherit_from(nearest_culture, $site)
    name: generate(culture=$settlement.culture, type=settlement)

  - relationship:
    kind: controlled_by
    source: $settlement
    target: nearest_entity(kind=nation, from=$site)
    strength: 0.8

  - relationship:
    kind: located_in
    source: $settlement
    target: region_of($site)
    strength: 1.0

achievement_conditions:
  - { type: fire_count, target: 5, reward: deck_modification }
  - { type: distinct_regions, target: 3, reward: deck_modification }
```

This is illustrative, not final syntax. The DSL design is an open question.

---

## Interactions with Other Systems

| System | Generator-Specific Interaction |
|--------|-------------------------------|
| Entity Graph (#1) | Creates new entities and relationships (primary writer). |
| Map/Spatial (#4) | Spatial placement queries. Finds valid cells for new entities. |
| Entity Kinds (#2) | Validates created entities against kind definitions. |
| Lifecycle (#9) | Newly created entities begin with full health metric and zero age. |
| Entity Behaviors (#6) | Newly created entities immediately begin low-altitude behavioral simulation on the next tick. |
| Narrative Domain (XX) | Names, descriptions, cultural context for created entities draw from the narrative domain. |

---

## Open Questions (Deferred)

- **Creation rate balancing.** How to prevent generator-heavy tableaux from flooding the world vs generator-light tableaux from starving it. Cadence and cooldown are the levers, but specific values need playtesting.
- **Batch size variability.** Can a generator's batch size vary based on conditions (a larger settlement spawns more initial entities than a smaller one)?
- **Generator interaction.** Can one generator's output trigger another generator's preconditions in the same tick? (Current design says no -- next tick.)
- **Player feedback on creation.** How the player is notified when a generator fires. Map animation? Log entry? Sound? Should it pause for the player's attention or happen silently?
