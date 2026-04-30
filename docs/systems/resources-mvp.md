# Resources (MVP)

The current resource system is an ambient hex-state model.

The three MVP resource types are:

- `food`
- `wealth`
- `population`

They live on the map, not on entities.

How the typed domain-definition graph represents these ambient resources is documented in [15-domain-definition-graph.md](./15-domain-definition-graph.md).

---

## Core Intent

Resources are **possibility space**, not a standalone economy sim.

They answer questions like:

- what can this place support?
- how favorable is this place for a process?
- what background mass or capacity exists here?
- what slow world-state can support actions modify?

They are meant to shape action choice, not become a giant autonomous subsystem of production chains and actor inventories.

---

## Principles

### 1. Ambient, not actor-owned

Hex resources are ambient map-state values. They are not settlement attributes, NPC inventory, or faction wallets.

### 2. Named entities convert or exploit ambient state

Processes turn ambient conditions into concrete outcomes.

Examples:

- a settlement grows because local `food` and `population` make growth possible
- `Irrigate` raises ambient `food` so later growth becomes better
- an NPC can remain in rich terrain longer because warmth drains more slowly there
- an artifact may eventually move only through hexes with enough ambient `population`

### 3. Resources should bias and gate, not dominate

The resource layer exists to:

- bias utility
- gate actions
- provide support targets
- represent unnamed background forces

It is not meant to become the main game loop by itself.

### 4. Keep ambient and concrete semantics distinct

The design must preserve the difference between:

- `possible here`
- `available now`

Right now MVP mostly uses the first. If the game later truly needs the second, it should be added deliberately rather than smuggled in as ad hoc settlement attributes.

---

## Current Implementation

### Resource fields

The engine currently exposes three per-hex resource layers:

- `food`
- `wealth`
- `population`

These are stored in `ResourceLayer` and managed by `ResourceSystem`.

### Map generation

Map generation seeds resource values directly from terrain-specific variance bands.

Examples:

- plains have higher `food` and `population`
- tundra has lower `food`
- mountains tend to be low `food` but better `wealth`

This is done at hex generation time, not derived from settlement presence.

### Runtime behavior

The MVP runtime currently preserves those seeded ambient fields rather than running a settlement-centric production/consumption economy every tick.

That means:

- no automatic intra-faction flow economy
- no settlement-driven resource derivation pass
- no actor-owned stockpile layer in the core engine

Processes and systems can still read and mutate ambient resource values.

---

## How Resources Affect Behavior Today

### Settlements

Settlements currently use ambient resources directly.

- `Grow` consumes ambient local `population`
- `Grow` depends on ambient local `food`
- `Irrigate` increases ambient local `food`

This gives the first support-planning chain:

```text
Want settlement temporary health
  -> Grow is weak
  -> Irrigate improves local food
  -> better food makes Grow better later
```

### NPCs

NPC warmth now uses ambient food in two different ways:

- ambient `food` at the NPC's location slows warmth loss
- `Eat` must still happen at a named settlement and provides the strong warmth-recovery correction

So a rich grassland lets an NPC stay out longer, but it does not replace the need to return to a named place eventually.

### Future examples already supported by the model

The intended use of resources includes cases like:

- artifacts moving only through hexes with enough ambient `population`
- factions preferring or taxing high-`wealth` territory
- support actions improving ambient fields because they make later actions better

Those are aligned with the model even if they are not all implemented yet.

---

## Resource Roles by Type

### `food`

Current role:

- sustainment quality of place
- growth support input
- planner threshold and support target

### `wealth`

Current role:

- desirability / future extraction potential
- future faction and settlement projection input

### `population`

Current role:

- ambient background populace on a hex
- unnamed labor / carry capacity / support substrate

This is intentionally different from settlement `temp_hp`, which is a temporary lifecycle anchor rather than literal population.

---

## What The Resource System Is Not

The current MVP resource system is **not**:

- a trade simulator
- a settlement stockpile economy
- a faction redistribution system
- a general-purpose inventory model

Those may become useful later, but they should only be added if they serve actual gameplay needs and preserve the ambient-resource distinction.

---

## Interactions with Other Systems

| System | Current interaction |
|--------|---------------------|
| Map / Spatial | Terrain seeds ambient resource values per hex. |
| Entity Behaviors | Processes read and mutate ambient resources as thresholds, utility inputs, and support targets. |
| Planner | Resource refs can appear in goal effects, preconditions, and support rules. |
| Lifecycle | Ambient food currently influences warmth drift through a dedicated location-resource drift system. |
| UI | Hover tooltip shows live per-hex ambient resource values. |

---

## Near-Term Direction

The current model is intentionally light.

Near-term expansion should preserve the same principles:

- slow environmental change is good
- support actions that improve ambient fields are good
- named entities converting ambient state into concrete outcomes is good
- introducing a full autonomous economy just because the engine can do it is not

If the game later needs explicit named-location stores or services, that should be treated as a deliberate new layer, not a silent return to settlement-owned pseudo-resource attributes.
