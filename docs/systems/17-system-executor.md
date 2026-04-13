# System 17: System Executor

Runs card-defined world processes. Defines how the world's forces BEHAVE.

---

## Purpose

System cards are the pressure release valves and force multipliers. While in the tableau, they continuously modify entity attributes, relationships, and pressures based on world state. They define HOW the world's tensions resolve: does cultural pressure produce diffusion or isolation? Does economic scarcity produce trade or conflict? Does demographic pressure produce migration or urbanization?

Systems are the mid-altitude layer that transforms low-altitude conditions into macro-level dynamics. Without active systems, pressure builds but nothing happens with it.

---

## How It Differs from Base

Systems extend the card executor base with:

### Continuous operation (not one-shot)

Unlike generators (which fire discrete creation events), systems run their effects CONTINUOUSLY. Each tick (at their configured cadence), they evaluate their preconditions and, if met, apply their effects. A system card that's active for 100 ticks applies its effects up to 100 times (minus cooldown gaps).

This means system effects are typically small per-tick but accumulate over time. "Cultural Diffusion" doesn't instantly merge two cultures -- it nudges cultural influence values by a small delta each tick, and over many ticks the accumulated change produces visible cultural shift.

### Scope-based processing

Systems typically operate across a scope rather than on a single target:
- **Regional systems:** process all entities in a region. "Disease Spread" evaluates every settlement in a region each tick.
- **Kind-scoped systems:** process all entities of a kind. "Prominence Decay" (post-MVP) would evaluate every entity.
- **Relationship-scoped systems:** process all relationships of a kind. "Alliance Maintenance" evaluates every active alliance.
- **Pairwise systems:** evaluate pairs of entities meeting criteria. "Cultural Friction" evaluates every pair of bordering cultures.

This is different from generators and actions, which typically target a single entity or location. Systems are broad-effect, sweeping through large portions of the entity graph.

### Effect accumulation and thresholds

System effects are typically incremental:
- "Cultural Diffusion" shifts cultural influence by ±0.5 per tick at borders
- "Border Tension" increases relationship negativity by 0.1 per tick for competing claims
- "Trade Prosperity" increases settlement prosperity by 0.3 per tick along active trade connections

These small deltas accumulate into significant changes over time. The player watches the gradual shift on map overlays and in attribute panels. This is the contemplative tempo in action: the effects are visible but slow, rewarding patient observation.

When accumulated effects cross thresholds (an attribute hits a critical value, a pressure reaches a level), OTHER cards' preconditions may be triggered. This is the indirect chain: system A gradually pushes a value, which eventually triggers generator B or action C.

### Inter-system interaction through tags

System cards interact with other active system cards through the tag system:
- Two active cards sharing a "connectivity" tag might amplify each other's effects (Cultural Diffusion spreads faster when Trade Routes is also active)
- Two active cards with conflicting tags might dampen each other (Isolationist Policy reduces the effect of Cultural Diffusion)

The interaction is defined in the DSL: "if another card with tag X is active, multiply this card's effect by Y." This creates the combinatorial tableau depth without hard-coding specific card-to-card interactions.

---

## System-Specific Preconditions

Systems commonly use preconditions that define WHEN to run, not just WHETHER to run:

- **Pressure band:** "run only when military tension is between 30 and 70" (the system operates in a specific pressure range -- too low means nothing to act on, too high means a different system should be handling it)
- **Entity minimum:** "run only if there are at least N entities of kind K" (don't process cultural diffusion if only one culture exists)
- **Seasonal:** "run only during certain seasons" (growth effects during growing season, hardship effects during winter)
- **Relationship density:** "run only if there are at least N relationships of kind K in the scope" (alliance maintenance only matters if alliances exist)

### System cards can have "always met" preconditions

Some systems should run unconditionally whenever they're in the tableau. "Prominence Decay" (post-MVP) should run every tick regardless of world state. For these, the precondition is trivially true and the card's behavior is purely defined by its effects and cadence.

---

## Achievement Conditions (Deck Modification Triggers)

System cards track their impact toward deck modification events:

- **Cumulative effect:** "this system has shifted a total of N attribute points across all entities" (measuring total impact)
- **Threshold crossings caused:** "entities affected by this system have crossed N attribute thresholds" (measuring consequential impact)
- **Duration-based:** "this system has been active for N ticks" (rewarding commitment to a system)
- **Interaction-based:** "this system has co-fired with another system sharing tag X at least N times" (rewarding synergistic tableau building)

---

## Example System Card (Illustrative)

```
system: "Cultural Friction"
mana_cost: 2
cooldown: 0 (runs every cadence tick)
cadence: 5 ticks
tags: [culture, tension, regional]

preconditions:
  AND(
    entity_count(kind=culture, min=2),
    scope_has_entities(scope=$region, kind=culture, min=2)
  )

scope: per_region
iteration: pairwise(kind=culture, in=$region)

effects:
  for_each_pair($culture_a, $culture_b):
    - condition: disposition($culture_a, $culture_b) < 0.4
      mutations:
        - modify_pressure(
            pressure="cultural_friction",
            region=$region,
            delta=+3,
            scaled_by: semantic_distance($culture_a, $culture_b)
          )
        - modify_attribute(
            entity=$culture_a,
            attribute="cohesion",
            delta=+0.2  # friction strengthens internal cohesion
          )

tag_interactions:
  - if_active_tag: "connectivity"
    effect_multiplier: 1.3  # connected cultures experience more friction
  - if_active_tag: "isolation"
    effect_multiplier: 0.5  # isolated cultures experience less friction

achievement_conditions:
  - { type: cumulative_pressure_change, target: 500, reward: deck_modification }
  - { type: active_duration, target: 100, reward: deck_modification }
```

---

## Interactions with Other Systems

| System | System Executor-Specific Interaction |
|--------|--------------------------------------|
| Attributes (#3) | Primary writer of incremental attribute changes across many entities per tick. |
| Pressure (#11) | Reads pressure for preconditions. Writes pressure modifications directly. System effects also indirectly affect pressure through attribute changes picked up next recomputation. |
| Relationships (#7) | May modify relationship strength. May create/cull relationships (though this is more typically generator/action territory). |
| Entity Behaviors (#6) | System effects modify the attributes that entity behaviors read, indirectly influencing low-altitude impulse selection. |
| Other active cards | Tag-based interaction with co-active cards amplifies or dampens effects. |

---

## Open Questions (Deferred)

- **Scope definition.** How scopes (per-region, per-kind, pairwise) are defined in the DSL. Whether custom scopes are possible.
- **Effect scaling.** How system effects scale with entity count, region size, or other factors. A system that applies +0.5 per entity per tick could be overwhelming in a region with 100 entities.
- **System ordering within a tick.** When multiple system cards fire in the same tick, does order matter? If system A modifies an attribute that system B reads, the order affects the result. Current design says all cards evaluate against the same state snapshot per tick.
- **Continuous vs. event-driven systems.** Some systems conceptually "always run" (Cultural Diffusion). Others conceptually fire on events (a system that responds to war declarations). Whether event-driven systems are a distinct pattern or just systems with specific preconditions.
