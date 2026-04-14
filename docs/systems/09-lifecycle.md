# System 9: Lifecycle

Aging, decay, and the passage of time. All entities trend toward non-existence.

---

## Purpose

The lifecycle system models the universal truth that everything ends. Every entity kind experiences a time-since-creation dependent effect that nudges it toward non-existence. This is always-on, low-altitude physics -- not card-controlled. It ensures the world is dynamic: entities don't persist forever by default, creating space for new things to emerge.

---

## Design Decisions

### All entities have a health metric and time-dependent decay

Every entity kind has a health-like attribute that represents its vitality. When this attribute reaches zero, the entity transitions to historical status. The specific attribute name is kind-specific, but the pattern is universal:

- **Mortal entities (heroes, etc.):** HP. Reaches 0 = death.
- **Locations (settlements, cities):** Population. Reaches 0 = abandoned/ruined.
- **Objects (artifacts, etc.):** Condition/Integrity. Reaches 0 = broken/destroyed.
- **Political entities (nations, factions):** Loyalty/Cohesion. Reaches 0 = dissolved/collapsed.
- **Cultures:** Vitality/Cohesion. Reaches 0 = extinct/absorbed.

The lifecycle system applies a time-since-creation dependent drain on this health metric. The drain function:
- Starts near zero for newly created entities (young things are resilient)
- Increases over time (older things become more fragile)
- Is NOT a fixed lifespan -- it is a growing downward pressure
- Is kind-specific in shape and rate

An old entity CAN survive indefinitely if other systems counteract the drain. A prosperous ancient city's population is sustained by attraction processes that outpace the age drain. A well-maintained artifact's condition is restored by its holder. Cards can also artificially extend lifespan by boosting the health metric directly -- effectively competing with entropy.

This creates a natural dynamic: young entities are self-sustaining (the drain is negligible). Mature entities are stable if conditions are favorable. Ancient entities require active investment to survive -- they become expensive to maintain, creating interesting decisions about whether to sustain the old or let it die to make room for the new.

The exact functions per kind are TBD. The design commitment is: there EXISTS a health metric per kind, and there EXISTS a drain function that increases with time since creation.

### Two statuses: active and historical

For MVP, entities have two lifecycle states:
- **Active:** participating in the simulation. Visible, interactable, affects and is affected by other entities.
- **Historical:** no longer participating. Remains in the graph for reference and relationship queries. Does not consume simulation resources (no process/decision evaluation, no attribute modification). Is not displayed on the map by default (may be togglable).

Transition from active to historical is triggered by kind-specific conditions:
- A mortal entity whose health reaches zero
- A location whose population reaches zero
- A political entity that meets kind-specific dissolution conditions
- An object whose condition reaches zero

These triggers are low-altitude (always-on rules), not card-gated. Cards may accelerate or prevent the conditions that lead to the trigger (a healing card keeps health above zero, a prosperity card keeps population up), but the transition rule itself is physics.

**Extensible:** the status system is designed to support additional statuses in the future (dormant, exiled, ruined, cursed, etc.) without architectural changes. Whether these are distinct statuses or better encoded as attributes/tags on an active entity is TBD.

### Prominence is post-MVP

The prominence system (0-5 scale, decay sink, sources from actions, reflected glory) is tabled to post-MVP. It is primarily interesting for narrative depth and event prioritization, which are not critical for the core simulation loop.

For MVP, entities do not have prominence. When prominence is added post-MVP, it will be an attribute with its own decay function (likely relationship-count-dependent, per the canonry model) layered into this lifecycle system.

---

## The Drain Function

The core mechanic is a function that takes time-since-creation as input and produces a per-tick drain on the entity's health metric. The function:
- Starts near zero for newly created entities
- Increases over time (not necessarily linearly -- could be exponential, logistic, stepped)
- Is kind-specific in shape and rate (mortal entities age faster than political entities)
- Applies a negative delta to the entity's health metric each tick

The drain function does NOT directly trigger status transitions. It modifies the health metric. When the health metric reaches zero, the status transition rule fires (active -> historical). Other systems can counteract the drain by adding positive deltas to the same health metric. Cards can boost it directly.

This keeps the lifecycle system's role clean: it drains health, other systems sustain health, and the transition fires when the balance tips.

---

## Interactions with Other Systems

| System | How Lifecycle Interacts |
|--------|------------------------|
| Entity Graph (#1) | Reads entity creation timestamps. Writes status changes (active -> historical). |
| Entity Kinds (#2) | Kind determines which aging function applies and which attributes it modifies. |
| Attributes (#3) | Writes to kind-specific attributes (health, population, condition, stability) based on age function. |
| Entity Behaviors (#6) | Aging modifies attributes that entity behavior reads. An aging hero's reduced proficiency changes their process scores. |
| Tick Clock (#5) | Time-since-creation is computed from current tick minus entity's createdAt. |
| Card Executors (#16-18) | Cards may counteract aging effects (healing, maintenance, renewal) by modifying the same attributes the aging function pushes down. Cards compete with entropy. |

---

## Open Questions (Deferred)

- **Aging function shape per kind.** Linear, exponential, logistic, stepped? Different shapes produce different feels: linear = steady decline, exponential = long health then rapid collapse, logistic = plateau then cliff. Needs playtesting.
- **Age-related subtype transitions.** Does aging trigger subtype changes? A young warrior becomes an elder statesman? Or is subtype change only card-driven?
- **Additional statuses.** When and whether to add dormant, exiled, ruined, cursed. Whether these are statuses or attribute/tag encodings.
- **Prominence system design.** Deferred to post-MVP. When added, needs: decay function, source mechanisms (what increases prominence), reflected glory mechanic, and integration with the attention/display systems.
- **Entity "renewal."** Can entities reset or reduce their age-based decay? A refounded nation, a rebuilt settlement, a reforged artifact? Or does age always accumulate monotonically?
