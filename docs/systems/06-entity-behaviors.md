# System 6: Entity Behaviors

Per-kind low-altitude simulation. The always-on physics of how entities behave.

---

## Purpose

Every entity kind has inherent behaviors that run each tick. These produce the CONDITIONS that mid-altitude cards respond to. Entity behaviors are the low-altitude simulation -- they modify attributes, shift relationships, create pressure, and drive bulk flow. They do not produce outcomes (that's card territory).

---

## Three-Layer Behavioral Model

Entity evaluation runs three layers per entity per tick, in strict order. Each layer has a distinct role and different rules about availability, selection, and effect magnitude.

### Layer 1: Physics (Automatic, Unconditional)

Physics runs for every active entity every tick. It is not selected, not scored, not skipped. These are the systems that run because they must: lifecycle drain, relationship drift, resource production and consumption, population flow.

Physics is implemented by dedicated engine systems (LifecycleSystem, RelationshipSystem, ResourceSystem), not by the process/decision evaluation pipeline. Physics sets conditions that processes and decisions respond to.

### Layer 2: Processes (Utility AI, Always Available)

Processes are the mundane, granular, always-on activities that entities perform. They are NOT card-gated. They run because that's what these things DO.

- Selected via utility AI: all viable processes are scored, one is picked via weighted random selection from ALL viable candidates (no top-N cap).
- One process fires per entity per tick.
- ALL entity kinds participate. Not just "agency" kinds -- settlements harvest, artifacts degrade, regions shift. Every entity kind has a process menu.
- Processes are **multi-effect and cross-entity**. An NPC eating reduces its own hunger AND consumes sustenance from the settlement it is located at. A settlement harvesting reads regional fertility AND writes to its own sustenance stockpile. Cross-kind interaction through the entity graph is fundamental, not optional.
- Processes create variance: the same entity in the same conditions might eat OR move OR rest depending on the weighted random roll and the score jitter.

Processes produce tendencies and incremental changes. They do NOT produce outcomes. Border tension goes up through processes -- what that tension MEANS (war, diplomacy, isolation) is decided by decisions, which are card territory.

### Layer 3: Decisions (Utility AI, Card-Gated)

Decisions are the significant, world-changing actions that only exist when the player has placed the enabling card in the tableau. They are the pressure release valves the player chooses to install.

- Selected via utility AI: same weighted random as processes, evaluated separately.
- One decision fires per entity per tick (independent of the process that fired).
- ALL entity kinds participate. A settlement can "expand borders" if the card exists. An artifact can "bond with wielder" if the card exists. Non-sentient objects take actions in 4X games.
- Card-gated: if the enabling Action card is not in the tableau, the decision does not exist. Removing the card removes the possibility.
- Decisions nearly always produce new relationships, new entities, or major attribute shifts. They are discrete, significant events.
- Decisions consume population (the universal action currency). Population is finite, forcing the player to triage what the world spends its people on.
- Decision execution emits typed messages to the pub/sub bus. These become event log entries visible to the player.

A world without the "Declare War" card is a world where war doesn't exist as a concept, and military tension has no outlet. A world without "Cultural Exchange" is a world where cultural friction between nations sits unresolved. Omission is a choice.

---

## Data-Driven Definition

Processes and decisions are defined as DATA, not code. They use the same DSL as cards:

- **Preconditions**: composable AND/OR tree of checks (attribute thresholds, pressure thresholds, relationship existence, cooldowns, entity counts)
- **Scoring expression**: data-defined formula for utility AI (`attribute("hunger") / 100`, `constant(0.5)`, weighted sums). Scores include variance (jitter) to prevent deterministic selection.
- **Mutations**: ordered list of effects (modify self attribute, modify related entity attribute, create relationship, modify pressure, etc.). Mutations include variance (multiplicative jitter on deltas).
- **Wiring contracts**: declared Reads and Writes as structured AttributeRef values. The WiringValidator checks at startup that all processes form coherent feedback loops with no dead-end or orphan attributes.

A ProcessDefinition with `Role = Process` is always available. A ProcessDefinition with `Role = Decision` requires an `EnablingCardId` referencing an active card in the tableau. The engine interprets both identically -- the only difference is availability gating.

This means:
- No C# process classes. Processes are data in the DomainConfig.
- Changing a process = changing data, not recompiling.
- The engine tests verify interpretation, not specific content.
- The domain defines all processes. A new domain can define entirely new processes without engine changes.

---

## Cross-Kind Interaction

Cross-kind interaction is what makes the simulation feel like a world instead of five independent counters. Processes interact across entity kinds through the entity graph:

- **Relationship-resolved reads and writes**: A mutation can follow a named relationship to find a target entity and read or modify its attributes. An NPC's "eat" process follows its `located_at` relationship to the settlement, reads the settlement's sustenance, and modifies both the settlement's sustenance (down) and its own hunger (down).
- **Attribute effects (PF2e typed bonus system)**: Relationships create ongoing attribute modifiers on related entities. Effects are typed (political, cultural, artifact, environmental). Same-type effects don't stack (take best bonus / worst penalty). Cross-type effects always stack. Every effect carries source metadata. Effective attribute = base + sum(filtered effects).
- **Goal tags (message-passing)**: Entities can set goal tags on other entities or locations. These are signals that other entities perceive and respond to during their own process evaluation. A nation sets a strategic goal on a frontier region; NPCs aligned with that nation weigh it during movement scoring.

The engine provides generic cross-kind mutation primitives. The domain composes them into specific behaviors like "eat" or "harvest."

---

## Per-Kind Behavioral Descriptions

These describe WHAT each kind does, not HOW the decision is made.

### Nations

The primary macro-level entities. Aggregate behavioral tendencies modeled through bulk flow and attribute dynamics -- NOT anthropomorphized.
- Border tension computed from proximity to neighbors, relative military strength, relationship values
- Internal resource flow between controlled settlements
- Population pressure driving frontier expansion tendency
- Consolidation/fragmentation pressure from size, cultural diversity, administrative distance
- Ideological drift based on internal faction composition and external cultural influence
- Strategic goal-setting via goal tags on locations and rivals

### Cultural Dynamics (attribute-driven)

Culture is an ATTRIBUTE, not an entity kind. Cultural dynamics are computed from the cultural attributes of settlements, nations, and NPCs:
- Cultural friction: entities of different cultures in proximity create friction pressure, feeding into the pressure system
- Cultural influence: settlements with large populations project their culture's influence onto neighboring settlements
- Culture itself is static (doesn't spread, merge, or fragment). Nations and NPCs of different cultures interact, creating dynamics enacted by nations and NPCs, not by culture itself.

### Settlements

The foundational economic unit. Growth and decline dynamics.
- Resource production from regional terrain, population, and development level
- Resource consumption by population (sustenance consumed, gold consumed)
- Population attraction when prosperous, population loss when impoverished
- Development growth from sustained prosperity, decline from sustained hardship
- Strategic significance from geographic position

### Artifacts

Objects of significance with their own low-altitude dynamics.
- Condition degradation over time (wear and entropy)
- Mystical resonance accumulation from involvement in significant events
- Influence radius affecting nearby entities and locations
- What mystical resonance and artifact influence DO in aggregate is card territory. The accumulation itself is inherent.

### NPCs (Heroes)

Individual named entities. One entity kind among many -- NOT the primary focus. Nations and cultures are the more interesting macro-level piece.
- Needs: hunger, safety. Hunger increases each tick. A hungry NPC moves toward food. A threatened NPC moves toward safety.
- Movement: pathfinding on the map, driven by need evaluation. Movement is just a process in the menu, not a separate system.
- Aging: NPCs age and eventually die of old age if nothing else kills them.
- Ambition: NPCs seek opportunity based on their subtype.
- Response to goal tags from nations, factions, and other entities.
- NPCs are the visible face of deeper systemic forces. Cultural dynamics operate through entity attributes, not through NPC actions directly.

### Regions

Passive stage. Scope unit for pressure computation. Provide terrain that determines what settlements can produce.
- Seasonal fertility shifts (cyclical modification around baseline)
- Slow feature development over long time scales (resource discovery from sustained habitation)
- Regions are permanent entities -- no health metric, no lifecycle drain.

---

## Entity Behaviors vs. Bulk Flow

The boundary between per-entity behaviors and aggregate map phenomena is not yet clearly defined:
- Entity Behaviors handles per-entity attribute changes (a specific settlement's prosperity increases, a specific NPC moves)
- Bulk Flow handles aggregate phenomena (population flowing between settlements, goods moving along routes, influence gradients)
- Entity Behaviors feeds Bulk Flow (settlement prosperity differentials drive population flow direction)
- Bulk Flow feeds Entity Behaviors (population inflow increases a settlement's population attribute)

Whether these are one system or two with defined interfaces is an implementation question.

---

## Wiring Validation

All processes and decisions declare wiring contracts: which attributes they read and which they write, as structured AttributeRef values. The WiringValidator runs at startup after all domain content is loaded and checks:

1. No dead-end attributes (written but never read by anything)
2. No orphan attributes (read but never written by anything)
3. Every entity kind participates in at least one feedback cycle
4. Every entity kind with a health attribute has a sustaining path (something increases health)
5. Every entity kind with a health attribute has a threatening path (something decreases health -- guaranteed by lifecycle drain)

The validator is pure instrumentation. It validates that the domain's data produces a coherent world. It doesn't alter behavior. If validation fails, the simulation refuses to start with specific error messages identifying exactly what's missing.

Key feedback loops that must exist:
- Sustenance surplus -> population growth -> more labor -> more sustenance production (reinforcing)
- Population consumed by decisions -> fewer laborers -> less production (balancing)
- Sustenance deficit -> population decline -> fewer laborers -> less production (death spiral with recovery path)
- NPC presence at settlement -> settlement effects via PF2e effect system
- Nation controls settlement -> settlement effects via effect system
- Relationship strength -> bilateral attribute effects
- Terrain at coordinates -> attribute modifiers
- Settlement prosperity -> NPC attraction -> more NPC presence (reinforcing)
- Lifecycle drain offset by resource availability (well-fed entities live longer)

---

## Interactions with Other Systems

| System | How Entity Behaviors Interacts |
|--------|-------------------------------|
| Entity Graph (#1) | Reads entity state and relationships. Processes do NOT modify graph topology (no creating/destroying relationships -- that's decision/card territory). Decisions CAN create relationships. |
| Entity Kinds (#2) | Kind determines which processes and decisions are available for each entity. |
| Attributes (#3) | Primary reader and writer of attribute values. Reads base attributes + computes effective attributes from relationship effects. |
| Map/Spatial (#4) | Reads terrain, distance, pathfinding for movement and spatial evaluation. |
| Resources (#8) | Processes read and write resource layers (sustenance, gold, population). Cross-kind interaction happens partly through shared resource state. |
| Pressure (#11) | Entity behavior outputs feed INTO pressure computation. Behaviors do NOT read pressure. |
| Relationships (#7) | Reads relationship values for cross-kind effect computation and goal tag evaluation. Processes modify relationship STRENGTH but not topology. |
| Action Executor (#18) | Decision cards register available decisions. Entities evaluate them alongside processes during their tick. |

---

## Open Questions (Deferred)

- **GOAP extension architecture.** How the utility AI base should be structured to allow GOAP layering post-MVP. The base must be extensible without rewriting.
- **Entity Behavior vs. Bulk Flow boundary.** One system or two. Interface between them.
- **Effect computation performance.** Summing all relationship-derived effects per entity per tick could be expensive. May need caching, dirty-flagging, or cadence optimization.
- **Goal tag design.** Schema for goal tags, how they're set and consumed, how they expire, how conflicts between multiple goal sources are resolved.
