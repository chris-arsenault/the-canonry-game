# System 6: Entity Behaviors

Per-kind low-altitude simulation. The always-on physics of how entities behave.

---

## Purpose

Every entity kind has inherent behaviors that run each tick. These produce the CONDITIONS that mid-altitude cards respond to. Entity behaviors are the low-altitude simulation -- they modify attributes, shift relationships, create pressure, and drive bulk flow. They do not produce outcomes (that's card territory).

---

## Design Intent (Captured, Not Finalized)

The detailed decision-making model for this system is not yet determined. What follows captures the current design thinking and open directions.

### Entities as Character Sheets

Each entity is essentially a character sheet / nation sheet -- a collection of attributes, relationships, tags, and semantic plane positions that define what the entity is and what it can do. The behavioral system reads this sheet and determines what the entity does each tick.

This framing applies across ALL entity kinds, not just heroes:
- A nation has a "sheet" with stability, military strength, border tension, ideology position, etc.
- A settlement has a "sheet" with population, prosperity, development, resource stockpiles, etc.
- An artifact has a "sheet" with condition, mystical resonance, influence radius, etc.
- A hero has a "sheet" with health, hunger, ambition, skill, etc.

The behavioral system is kind-agnostic at the base: "read the sheet, evaluate the situation, produce attribute changes." Kind-specific logic determines WHAT gets evaluated and HOW.

### Decision Model: Utility AI with GOAP Extension Path

**MVP: Utility AI with weighted random selection.** Each entity evaluates a set of available impulses, scores them using kind-specific utility curves weighted by current attributes, and selects via weighted random from the top candidates. This produces varied but personality-consistent behavior. See `docs/research/entity-behavior-models.md` for detailed pattern analysis.

**Post-MVP: GOAP layer for multi-step planning.** The utility AI handles moment-to-moment decisions well but cannot plan multi-step sequences (e.g., "accumulate resources, then build an army, then invade the frontier"). GOAP can be layered on top: entities set high-level goals based on utility evaluation, then GOAP plans action sequences to achieve them. The architecture must be extensible to support this without rewriting the base system.

### Two Levels of Entity Action (Critical Distinction)

There are two distinct kinds of things entities "do," and they must not be conflated:

**Impulses (low-altitude, this system):** The small, frequent, always-available behaviors that entity behaviors evaluates each tick. Move one cell toward food. Consume a resource. Radiate cultural influence. Shift a relationship value. Flow population toward a prosperous settlement. These are the granular physics of entity life. They do NOT require cards to be available. They rarely produce new relationships or entities. They are the "move one square, eat some soup" level of action.

Impulses are:
- Always available (not card-gated)
- Kind-specific (nations have national impulses, settlements have settlement impulses)
- High frequency (evaluated every tick or every few ticks)
- Small effect (incremental attribute changes, gradual movement)
- Selected by the Utility AI from the entity's impulse list

**Actions (mid-altitude, card-gated, System #19):** The significant, deliberate, card-enabled behaviors that produce meaningful world-state changes. Declare war. Forge an alliance. Invade a territory. Found a settlement. Wield an artifact. These nearly always produce a new relationship, a new entity, or a major attribute shift. They only exist if an Action card is in the player's tableau.

Actions are:
- Card-gated (only available if the corresponding Action card is active)
- Cross-kind (an "Invade Territory" action involves nations, settlements, and possibly heroes)
- Low frequency (evaluated less often, triggered by sustained conditions)
- Large effect (new relationships, status changes, entity creation/destruction)
- Resolved by the Action Executor (System #19), not by this system

**How they interact:** The entity behavior system (this system) evaluates impulses and produces the low-altitude conditions that build toward Action preconditions. A nation's impulses gradually increase border tension. When border tension is high enough AND the "Declare War" Action card is active, the Action Executor fires. This system does not fire Actions -- it creates the conditions for them.

The entity behavior system also responds to Action results: after a war is declared (by the Action Executor), national impulses shift -- military mobilization increases, trade impulses redirect, population flow changes direction.

### Cross-Kind Interaction Through Effects and Tags

Entity behaviors interact across kinds through two mechanisms:

**Attribute effects (ongoing modifiers):**
Relationships between entities create ongoing attribute modifiers on the related entities. These effects are local to the receiving entity and recomputed each tick (or at appropriate cadence) by summing all active effects.

Examples:
- Members of Nation X receive -3 to hunger (the nation's infrastructure feeds its people)
- Members of Nation X receive +3 to aggression (the nation's ideology promotes militarism)
- Holders of a powerful artifact receive +6 to might
- Residents of a prosperous settlement receive +2 to satisfaction
- Members of a militant faction receive +4 to aggression

The source of the effect can change (a nation's hunger modifier might worsen during famine), and the sum of all effects on an entity is recomputed per tick. This means an entity's effective attributes are: base attributes + sum of all relationship-derived effects.

**Goal tags (message-passing between entities):**
Entities can set goal tags on other entities or locations. These are not direct commands -- they are signals that other entities can perceive and respond to (or ignore).

Examples:
- A nation sets a goal tag on a frontier landmark: `{type: acquire, source: nation_x, importance: high}`
- Heroes aligned with Nation X perceive this tag and may prioritize moving toward the landmark
- A faction sets a goal tag on a rival faction: `{type: undermine, source: guild_y, importance: medium}`
- Member heroes may undertake espionage-related behaviors against the target

Goal tags are:
- Set by entity behavior logic (nations set strategic goals, factions set political goals)
- Readable by other entities during their own behavior evaluation
- Weighted by the receiving entity's own attributes and relationships (a loyal hero weighs their nation's goals heavily; a disloyal hero may ignore them)
- Ephemeral or persistent (some goals expire, some persist until achieved or cancelled)

This system avoids tight coupling between entity kinds. A nation doesn't directly command its heroes -- it broadcasts goals. Heroes respond based on their own sheets. The gap between intent and execution is where interesting emergent behavior lives.

### Entity Behavior vs. Bulk Flow

The boundary between these two systems is not yet clearly defined. They may be the same system, or they may be separate systems that read each other's outputs. Current thinking:

- Entity Behaviors handles per-entity attribute changes (a specific settlement's prosperity increases, a specific hero moves)
- Bulk Flow handles aggregate phenomena across the map (population flowing between settlements, goods moving along routes, influence gradients)
- Entity Behaviors may FEED Bulk Flow (settlement prosperity differentials drive population flow direction)
- Bulk Flow may FEED Entity Behaviors (population inflow increases a settlement's population attribute)

Whether these are one system or two with defined interfaces is an implementation question. The design contract is: both exist, both are low-altitude, and they interact.

---

## Per-Kind Behavioral Descriptions

These are the low-altitude behaviors per entity kind. They describe WHAT the entity does, not HOW the decision is made (that's the unresolved decision model above).

### Nations
- Border tension computed from proximity to neighbors, relative military strength, relationship values
- Internal resource flow between controlled settlements
- Population pressure driving frontier expansion tendency
- Consolidation/fragmentation pressure from size, cultural diversity, administrative distance
- Ideological drift based on internal faction composition and external cultural influence
- Strategic goal-setting via goal tags on locations and rivals

### Cultural Dynamics (attribute-driven, not entity-driven)

Culture is an ATTRIBUTE, not an entity kind. Cultural dynamics are computed from the cultural attributes of locations, nations, and other entities, not from standalone culture entities. These dynamics are still low-altitude and always-on:

- Influence radiation from settlements based on population and cultural attribute strength
- Cultural pressure buildup at borders where different cultural attributes meet
- Cohesion computed from geographic concentration of shared cultural attributes
- Innovation accumulation from settlement development and inter-cultural contact
- Cultural drift: entities' cultural attributes shift over time based on neighboring influences

These dynamics apply to entities that HAVE cultural attributes (locations, nations, factions, heroes), not to culture entities. Cards that target "culture" target the cultural attributes of real entities, not standalone culture entities.

### Settlements
- Population attraction/loss based on prosperity, safety, opportunity
- Resource production from regional terrain, population, development
- Resource consumption by population
- Development growth/decline from sustained prosperity/hardship
- Strategic significance from geographic position

### Factions
- Goal pursuit influencing member behavior via goal tags and attribute effects
- Influence accumulation from member prominence and actions
- Internal politics creating pressure on host nation attributes
- Cross-national reach for factions spanning multiple nations

### Artifacts
- Condition degradation over time
- Mystical resonance accumulation from involvement in significant events
- Influence radius affecting nearby entities and locations

### Heroes
- Need evaluation (hunger, safety, ambition) driving movement and goal selection
- Movement via pathfinding on the map
- Aging toward eventual death
- Response to goal tags from nations, factions, and other entities
- Attribute modification from relationship-derived effects

---

## Interactions with Other Systems

| System | How Entity Behaviors Interacts |
|--------|-------------------------------|
| Entity Graph (#1) | Reads entity state and relationships. Does NOT modify graph topology (no creating/destroying relationships -- that's card territory). |
| Entity Kinds (#2) | Kind determines which behavioral model runs for each entity. |
| Attributes (#3) | Primary reader and writer of attribute values. Reads base attributes + computes effective attributes from relationship effects. |
| Map/Spatial (#4) | Reads terrain, distance, pathfinding for movement and spatial evaluation. |
| Pressure (#11) | Entity behavior outputs feed INTO pressure computation. Behaviors do NOT read pressure. |
| Relationships (#7) | Reads relationship values for cross-kind effect computation and goal tag evaluation. |
| Map Visualization (#10) | Entity behavior outputs (positions, attribute changes) are rendered by the visualization system. |
| Action Executor (#18) | Action cards register available Actions (distinct from impulses). Action Executor fires Actions when conditions are met. This system creates those conditions through impulses. |

---

## Open Questions (Deferred)

- **Impulse lists per kind.** What specific impulses does each entity kind have? Needs to be defined per-kind during entity kind review. Nations will have different impulses than settlements, artifacts, or heroes.
- **GOAP extension architecture.** How the utility AI base should be structured to allow GOAP layering post-MVP. The base must be extensible without rewriting.
- **Entity Behavior vs. Bulk Flow boundary.** One system or two. Interface between them. May be that impulses for aggregate entities (nations, cultures) ARE bulk flow.
- **Effect computation performance.** Summing all relationship-derived effects per entity per tick could be expensive. May need caching, dirty-flagging, or cadence optimization.
- **Goal tag design.** Schema for goal tags, how they're set and consumed, how they expire, how conflicts between multiple goal sources are resolved.
- **Per-kind behavioral complexity.** How complex each kind's impulse model needs to be at MVP. Nations likely need more sophisticated impulse evaluation than artifacts.
- **Impulse naming.** "Impulse" is a working term to distinguish from card-gated "Actions." May need a better name. Candidates: impulse, behavior, instinct, drive, tendency.
