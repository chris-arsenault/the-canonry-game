# System 3: Attributes

Per-entity numeric state. The values that make entities distinct from each other and that change over the course of the simulation.

---

## Purpose

Attributes are the measurable properties of entities. They are the substrate that low-altitude systems modify, that pressure aggregates, and that cards evaluate preconditions against. Every meaningful difference between two entities of the same kind is expressed through attributes.

The current active domain also uses a 5-bucket selection model to keep attribute lists disciplined: `Integrity`, `Need/Drive`, `Capacity`, `Projection`, and `Alignment`. See [attribute-model.md](./attribute-model.md).

---

## Design Decisions

### Attributes are static unless acted upon

Attributes do not change on their own. They have no inherent drift, decay, or growth. An attribute stays at its current value until a system explicitly changes it. That system may be a low-altitude game system (resource production increases a settlement's stockpile), a card-driven system (Cultural Diffusion shifts cultural influence), or a lifecycle system (prominence decays over time -- but this is an explicit low-altitude system acting on the attribute, not the attribute decaying by itself).

This means: if no system touches an attribute, it sits still. There are no hidden dynamics. An attribute's value at any point in time is the result of every system that has acted on it, and nothing else.

### Prefer continuous over discrete

Most entity state should be modeled as continuous values, even when the concept feels categorical. The reason: entities migrate between states over time, and we want to model that migration, not sudden jumps.

A nation doesn't suddenly become theocratic. It migrates there through a gradual shift in its governance attributes. A hero doesn't instantly become corrupt -- their integrity attribute drifts downward over time as systems act on it.

Discrete state (subtype, status, tags) exists but is the exception, reserved for things that genuinely are categorical and don't migrate (an entity's kind, whether it's alive or dead).

### Attributes are scalar state

Entity attributes are scalar values. Multi-axis concepts belong either in domain
modeling as multiple explicit scalar attributes, or in the engine's separate
physical/semantic placement coordinates when the concept is location-like. The
attribute system should not carry hidden reducer grids or per-component state
that only some systems understand.

This keeps process preconditions, wiring validation, tableau output, and planner
blockers reading the same graph language. If a generator or process cares about
two qualities, it should name both qualities directly rather than target an
implicit label reduced from a coordinate pair.

### Everything is visible

All attributes are accessible to the player. The "everything visible is everything there is" principle applies fully. Not every attribute needs to be prominently displayed in the UI (UX considerations may hide rarely-needed values behind a detail panel), but every attribute must be inspectable. No hidden modifiers, no invisible state.

---

## Attribute Categories

### Scalar Attributes

Single numeric values with a defined range.
- Population (0-N, settlements)
- Condition (0-100, artifacts)
- Health (0-100, mortal entities)
- Development (0-100, settlements)
- Prominence (0-5, all entities) -- POST-MVP, listed for illustration

Scalars are the simplest and most common attribute type.

### Tags

Key-value pairs for flexible metadata that doesn't fit scalar or plane models.
- Free-form, not pre-defined per kind (though kinds may define expected tags)
- Used for properties that are genuinely categorical and don't migrate: a hero's origin settlement, an artifact's material, a faction's founding ideology
- Tags are static once set (they don't change through simulation dynamics, though cards could modify them)

---

## Attribute Interactions

### Attributes feed into pressure

Attribute values across entities in a region are inputs to the pressure system. The pressure system aggregates these into smoothed regional values. This is one-way: attributes feed pressure, pressure does not directly write back to attributes. (Pressure may influence card preconditions, which fire card effects, which modify attributes -- but that's an indirect loop through cards, not a direct pressure-to-attribute write.)

### Low-altitude systems modify attributes

Entity behavior systems are the primary writers of attribute values. Each low-altitude system reads relevant attributes and relationships, computes changes, and writes updated values. Examples:
- Resource production system reads settlement population and regional fertility, writes to settlement stockpile
- Border tension system reads nation proximity and military strength, writes to relationship strength
- Lifecycle system reads entity age, writes to health (or triggers status change)

### Cards modify attributes

Card effects can directly modify attribute values (a system card that "increases prosperity in trade-connected settlements" writes to settlement prosperity). Cards can also modify attributes indirectly by creating relationships or entities that cause low-altitude systems to produce different results.

## Interactions with Other Systems

| System | How Attributes Interacts |
|--------|--------------------------|
| Entity Graph (#1) | Attributes are keyed by entity ID. Graph knows entities exist; Attributes knows their values. |
| Entity Kinds (#2) | Kind defines which attributes an entity has, with ranges and defaults. |
| Pressure (#11) | Attribute values are inputs to pressure computation. One-way: attributes -> pressure. |
| Entity Behaviors (#6) | Low-altitude systems are the primary readers and writers of attribute values. |
| Card Executors (#16-18) | Cards read scalar attributes for preconditions. Cards write scalar attributes as effects. |
| Lifecycle (#9) | Lifecycle reads attributes (age, health) and may trigger status changes when thresholds are crossed. |
| Lose Condition Monitor (#14) | Reads aggregate attribute values (total population, diversity metrics) for threshold evaluation. |

---

## Open Questions (Deferred)

- **Specific attribute lists per kind.** Which attributes each kind has is TBD pending the entity kind review.
- **Rate-of-change tracking.** Should the attribute system track deltas over time (for trend display and system evaluation), or should this be computed on demand? Design preference is to track it, but the cadence of tracking is TBD.
- **Attribute interactions within an entity.** Can one attribute directly affect another on the same entity (high population reduces per-capita prosperity)? Or must all inter-attribute effects flow through systems? Preference is through systems (keeps attributes as pure state, systems as pure logic), but may be impractical for some basic relationships.
