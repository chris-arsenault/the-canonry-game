# System 11: Pressure

Smoothed, homeostatic-bounded aggregate values that represent macro-level conditions. The bridge between low-altitude entity state and mid-altitude card preconditions.

---

## Purpose

Pressure values represent the macro-level forces at work in the simulation: military tension, economic stress, cultural friction, demographic pressure. They are computed from the aggregate state of entities, relationships, and map conditions within a scope (region or entity). They smooth out tick-to-tick noise into meaningful signals that cards can evaluate preconditions against.

Pressures are a common card precondition, but not the only one. Cards can also check entity attributes, relationship values, and map state directly. Pressure's unique role is providing a smoothed, human-readable aggregate that captures "the overall state of things in this area" in a single value -- something that raw entity-by-entity data does not.

---

## Design Decisions

### Pressures are scoped to geographic regions or entities

Each pressure value is computed within a scope:

**Regional pressures:** computed across all entities and relationships within a geographic region. The region concept is now a first-class spatial grouping: a connected subset of map cells defined by geographic boundaries (mountain ranges, rivers, coastlines, or arbitrary designer-defined borders). Every map cell belongs to exactly one region.

Regional pressure examples:
- Military tension in the Northern Highlands (high if multiple nations have border claims, low if one nation controls it unchallenged)
- Economic stress in the River Valley (high if settlements are in deficit, low if trade is flowing)
- Cultural friction along the Eastern Border (high if two strong cultures meet, low if one culture dominates)

**Entity pressures:** computed for a specific entity (typically a nation or faction). These aggregate the state of the entity's internal components.

Entity pressure examples:
- Internal stability of the Kingdom of Kesh (computed from its settlements' prosperity, faction politics, cultural diversity)
- Military readiness of the Trade League (computed from member strength, resource stockpiles, border threats)

### Pressure computation model

Following the canonry pattern, each pressure has:

**Contributing factors:** inputs that push the pressure value up or down. Each factor has:
- A source type: entity count, relationship count, tag count, attribute aggregate, ratio, or other computable value within the pressure's scope
- A coefficient: how strongly this factor affects the pressure
- A direction: positive feedback (pushes pressure up) or negative feedback (pushes pressure down)

**Homeostasis:** a rate at which the pressure drifts back toward its baseline (typically 0). This means temporary spikes dampen out over time unless sustained by contributing factors. Homeostasis rate is per-pressure-type.

**Smoothing:** pressure values don't jump instantly. Changes are applied gradually, preventing single-tick events from causing wild pressure swings. The smoothing factor determines how quickly the pressure responds to changes in contributing factors.

**Range:** -100 to +100 for all pressures. The sign carries meaning specific to each pressure type (e.g., positive resource availability = plenty, negative = crisis).

**Computation per tick (or per appropriate cadence):**
1. Evaluate all contributing factors within the pressure's scope
2. Sum positive and negative contributions
3. Apply homeostasis (drift toward baseline)
4. Apply smoothing (blend new value with previous value)
5. Clamp to range

### Pressure types are extensible

The initial set is small (4-8 pressure types) but the system supports adding new types. Each pressure type is a data definition: name, scope type, baseline, homeostasis rate, smoothing factor, and a list of contributing factor definitions. Adding a new pressure type is a data change, not a code change.

This means cards can be designed around pressure types that don't exist yet. The pressure system is a framework; individual pressures are content.

### Pressures are read-only to low-altitude systems

Low-altitude entity behaviors DO NOT read pressure values. They read entity attributes and relationships directly. Pressure is a MID-ALTITUDE concept -- it exists as an aggregation layer for cards and for the player's understanding.

The information flow is strictly:
1. Low-altitude systems modify entity attributes and relationships
2. Pressure system aggregates those into pressure values
3. Cards read pressure values for preconditions
4. Cards fire effects that modify entity attributes and relationships
5. (Loop back to 1)

Pressure is never an input to low-altitude simulation. It is a derived signal, not a causal force.

### Cards can modify pressure directly

In addition to pressure being computed from contributing factors, card effects can directly modify pressure values (as seen in the canonry's action mutations: `modify_pressure` with a delta). This allows cards to have immediate macro-level effects: a successful "seize control" action might directly boost regional security pressure, in addition to the indirect effect of the new control relationship eventually being picked up by the pressure computation.

Direct modifications are also subject to homeostasis and smoothing -- a one-time pressure spike from a card effect will dampen out unless sustained by underlying contributing factors.

---

## Relationship to the Canonry

The canonry's pressure system provides the mechanical template. Key patterns carried forward:
- Pressures have positive and negative feedback computed from entity counts, tag counts, relationship counts
- Pressures have homeostasis values that pull them toward baseline
- Generators check pressure thresholds as applicability conditions
- Actions can directly modify pressure values as mutation effects
- Systems reference pressure state for their logic

Key differences from the canonry:
- Pressures are scoped to geographic regions or entities (the canonry had global-only pressures)
- The contributing factor types will include spatial/map-based factors (the canonry had no spatial dimension)
- More pressure types may exist simultaneously (the canonry had 4)

---

## Interactions with Other Systems

| System | How Pressure Interacts |
|--------|------------------------|
| Entity Graph (#1) | Reads entity counts, relationship counts, tag counts within scope |
| Attributes (#3) | Reads attribute aggregates within scope (average prosperity, total population) |
| Map/Spatial (#4) | Defines regional scopes. Map cell attributes may contribute to pressure. |
| Entity Behaviors (#6) | Does NOT interact. Low-altitude systems do not read pressure. |
| Relationships (#7) | Reads relationship counts and types within scope |
| Card Executors (#16-18) | Cards read pressure for preconditions. Cards can write pressure directly via effects. |
| Lose Condition Monitor (#14) | May read pressure values as inputs to threat assessment |
| Map Visualization (#10) | Does NOT interact directly. Pressure values are rendered as map overlays by the visualization system. |

---

## Open Questions (Deferred)

- **Specific pressure types.** The initial 4-8 pressures and their contributing factors are TBD. Likely candidates: military tension, economic stress, cultural friction, demographic pressure, political stability, resource availability. Exact definitions need iteration with the entity kind review.
- **Region definition.** How geographic regions are defined (procedurally generated, hand-authored, derived from natural boundaries). Whether regions are static or can change during a run. Whether region boundaries affect pressure computation (e.g., mountain ranges dampen pressure propagation between adjacent regions).
- **Pressure propagation between regions.** Does high military tension in one region affect adjacent regions? If so, at what rate and with what dampening? This would model cascading regional instability.
- **Entity-scoped pressure details.** How entity pressures (e.g., national stability) aggregate from regional pressures and internal entity state. Whether entity pressures are a separate computation or derived from the regions the entity controls.
- **Pressure visualization.** How pressures are displayed to the player. Likely as map overlays (color-coded heatmaps per pressure type) and as numeric values in entity/region detail panels.
