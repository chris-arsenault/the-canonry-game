# Game Systems

The vertical slices that make up the game. Each system is a full-stack unit: data model, logic, and display. Systems are listed in dependency order -- earlier systems are foundations for later ones.

MVP scope is noted per system. "MVP" means required for first playable. "Post-MVP" means architecturally planned but not implemented initially.

> Implementation note: this document describes systems conceptually. In the
> current codebase the runtime is split into a microkernel (`Engine.Kernel`),
> domain compilation (`Engine.Domain`), an event log (`Engine.Eventing`),
> projections (`Engine.Projection`), observability (`Engine.Observability`),
> and a host that wires them together (`Engine.Host`). The kernel runs the
> per-tick programs listed under "Tick Clock" below. The data model lives in
> `WorldState` and `KernelDataSet` inside the kernel; the "display" half of
> each system is a projection that consumes primitive events off the sim
> thread. See `docs/architecture.md` for the code-level picture and
> `docs/kernel/simulation-kernel-discipline.md` for the rules that govern
> what may live in the kernel versus the consumers.

---

## Core Data Systems

These are the foundational data structures. They don't DO anything on their own -- they are read and written by the simulation and player systems.

### 1. Entity Graph

The single source of truth for world state. All other systems read from and write to the graph.

**What it is:**
- A directed graph of entities connected by typed relationships
- CRUD operations: create, read, update, delete entities and relationships
- Query interface: find entities by kind, by attribute range, by relationship, by spatial proximity
- Change notification: other systems subscribe to graph changes they care about

**Data model:**
- Entity: id, kind, subtype, name, status, coordinates, createdAt, updatedAt
- Relationship: source, target, kind, strength, metadata

**MVP scope:** Full implementation. This is the foundation.

### 2. Entity Kinds

The hard-coded schema. Defines the vocabulary of the simulation.

**What it is:**
- The enumeration of all entity kinds. See `docs/systems/entity-kind-registry.md` for the formalized list (11 kinds, 4 MVP).
- Per-kind: valid subtypes, valid relationship types, valid statuses, valid attributes
- Kind-specific behavior rules (what this kind can do, what systems affect it)

**Data model:**
- Kind definitions with subtype enumerations
- Relationship kind definitions with valid source/target kind constraints
- Status enumerations per entity kind

**MVP scope:** Full implementation. Hard-coded, not configurable.

### 3. Attributes

Per-entity continuous variables. The numeric state that game systems modify and cards respond to.

**What it is:**
- Named numeric values attached to entities
- Kind-specific: regions track slow world-state, settlements track integrity and projection, factions track cohesion/reach/provisions, NPCs track vitality and drives, artifacts track condition/power/containment
- Bounded: each attribute has a min/max range
- Change tracking: current value, rate of change, recent delta history (for trend display)
- Attribute selection follows a shared design model. See `docs/systems/attribute-model.md`.

**Data model:**
- Attribute definitions per entity kind (name, min, max, default)
- Attribute values per entity instance
- Rate-of-change tracking for display/analysis

**MVP scope:** Full implementation. The set of attributes per kind will expand over time, but the system itself is MVP.

### 3a. Domain Definition Graph

The engine also maintains a typed definition graph for the authored domain itself. This is separate from the runtime entity graph and is used for validation and semantic reasoning.

**What it is:**
- Typed domain schema graph built from `DomainConfig`
- Semantic stock/flow layer built from typed semantic config
- Validators and analyzers for coverage, loop detection, and semantic drift
- The programmatic replacement for worksheet-only reasoning

See `docs/systems/15-domain-definition-graph.md`.

### 4. Map / Spatial

The 2D physical world. Geography that matters to the simulation.

**What it is:**
- Grid-based map (hex or square -- to be determined) divided into regions
- Per-region: terrain type, biome, resources, habitability, fertility
- Adjacency graph: which regions border which
- Connectivity graph: trade routes, paths, waterways between settlements
- Spatial queries: distance between entities, entities within radius, path between points

**Data model:**
- Region grid with terrain/biome data
- Adjacency and connectivity graphs (may be derived from grid or manually defined)
- Entity position tracking (which region, which settlement)

**MVP scope:** Core implementation including procedural map generation. Hex vs square TBD. Hand-authored maps may also be supported but procedural generation is MVP.

---

## Simulation Altitude Model

The simulation operates at three layers within each entity's tick evaluation, plus the card execution layer above. This is a critical architectural distinction.

### Layer 1: Physics (Automatic, Unconditional)

Runs for every active entity every tick. Not selected, not scored, not skipped. Lifecycle drain, ambient location-resource drift, maintenance, relationship drift, and ambient resource preservation all live here. Implemented by dedicated engine systems. Physics sets conditions.

### Layer 2: Processes (Planner + Utility AI)

The mundane, granular, always-on activities. Eat, move, rest, harvest, repair, degrade, accumulate. ALL active entity kinds participate. The planner first builds bounded goal trees from deficits, standing drives, blockers, and support rules; then the evaluator selects one actionable process leaf. One process fires per entity per tick. Processes are multi-effect and may be cross-entity. They produce local changes and conditions, not global outcomes.

### Layer 3: Decisions (Utility AI, Card-Gated)

The significant, world-changing actions. Declare war, forge alliance, expand borders, wield artifact. They share the same definition model as processes. The intended runtime model is still card-gated decision availability, though the full tableau-driven availability wiring remains deferred.

### Card Execution (Mid-Altitude)

Generator and System cards operate on the AGGREGATE EFFECTS of low-level simulation. They read pressure and entity state, then write to the entity graph. Cards define WHAT CONDITIONS MEAN -- what happens when pressure releases, what emerges from accumulated state.

### Why This Ordering Matters

The canonry declared high-level outcomes and hoped coherence emerged downward. This game simulates low-level behaviors and lets cards define what the aggregate means. Coherence comes from the bottom up, not the top down.

The hierarchy:
1. Physics produces raw state changes (drift, decay, ambient conditions)
2. Processes produce conditions through planner-driven low-altitude activity
3. Decisions define what those conditions mean for entities that act
4. Card execution (generators, systems) defines WHAT CONDITIONS MEAN at the world level
5. Outcomes EMERGE from the interaction of all layers

---

## Low-Altitude Systems (Always-On Physics)

These run every tick (at varying cadences) regardless of the player's card configuration. They are NOT card-controlled. They simulate the inherent behaviors of each entity kind and produce the conditions that cards respond to.

### 5. Tick Clock

Time management. Drives everything.

**What it is:**
- Tick counter that advances the simulation
- Speed control: pause, play, 2x, 4x (possibly more)
- Cadence management: different systems register for different tick frequencies (every tick, every 5 ticks, every 20 ticks, etc.)
- Strict scheduled order: within a single tick, systems execute in registration order

**Current built-in order (within a single tick):**
1. Upkeep
2. Relationships
3. Processes
4. Gambits
5. Pressures
6. Generators
7. Degeneracy

**MVP scope:** Full implementation. Speed control and cadence management are essential from day one.

### 6. Entity Behaviors (Three-Layer Behavioral Model)

Each entity kind participates in a three-layer behavioral model that runs per entity per tick:

1. **Physics** (automatic, unconditional): lifecycle drain, relationship drift, ambient resource drift, and maintenance. Dedicated engine systems. Not scored, not selected.
2. **Processes** (planner + utility AI): mundane activities. The planner builds bounded goal trees from deficits and standing drives; the evaluator executes one actionable process leaf per entity per tick.
3. **Decisions** (same definition model, card-gated): significant actions. Runtime tableau-driven decision availability is still a deferred integration step.

**Design principle:** Processes produce tendencies, conditions, and local changes. They are low-altitude behavior, not global outcomes.

**Design principle:** This system is not NPC-centric. All active entity kinds participate in the same runtime model. The engine is kind-agnostic.

**Design principle:** Processes and decisions are data, not code. They are defined in the domain config and interpreted by the engine. See `docs/systems/06-entity-behaviors.md` for the behavior stack and `docs/systems/planner.md` for the detailed planning model.

**Design principle:** Processes are multi-effect and can be cross-entity. Planner contracts (`GoalEffects`, `ProviderSelectors`, blockers, support rules) and execution contracts (`Reads`, `Writes`, `Mutations`) are all declared on `ProcessDefinition`.

**Design principle:** Provider scoring is process-defined. Target candidates are weighted by the process's reward expression; the engine does not hardcode settlement, NPC, faction, or artifact logic.

#### Process Examples (Domain-Defined, Not Engine-Level)

These are examples of how the IceRemembers defines processes. They are data, not engine code:
- **Grow** (Settlement): increases `residents` using ambient local `food` and `population`
- **Irrigate** (Settlement): increases ambient local `food`, often as a support action for later growth
- **Propaganda** (Settlement): shifts a target settlement's ideology
- **Attract Population** (Settlement): steals `residents` from another settlement when prestige and ideology line up
- **Eat / Move To Target / Wander** (NPC): a simple substitute loop showing multi-step movement without hardcoded chains

Entity kinds differ only in what processes the domain registers for them, not in engine behavior.

**Regions:** Seasonal fertility shifts, slow feature development. Permanent entities with no lifecycle drain.

#### Future Entity Kinds

Architecture supports additional kinds without structural changes: Monsters/Creatures, Legendary Vehicles/Ships, Religious Orders/Institutions. Not MVP.

**MVP scope (5 entity kinds):** Region, Settlement, Faction, NPC, Artifact. All participate in the shared behavior stack.

**Post-MVP kinds:** Point of Interest, Monster, Transportation, Ability, Occurrence. See `docs/systems/entity-kind-registry.md`.

### 7. Relationships

Fine-grained, always-on relationship dynamics between entities.

**What it is:**
- Relationship values shift every tick based on low-altitude factors:
  - Proximity: nearby entities develop stronger relationships (positive or negative depending on other factors)
  - Shared culture: same-culture entities trend positive
  - Competing claims: entities with overlapping territorial interests trend negative
  - Resource competition: entities competing for the same scarce resources trend negative
  - Trade connections: entities connected by active trade routes trend positive
- Relationship strength is 0 to 1 with polarity defined by relationship kind (positive kinds like allied_with, negative kinds like rival_of, neutral kinds like controls)
- Relationship TYPES are fixed but relationship STRENGTH changes continuously through low-altitude dynamics
- Low-strength relationships are automatically culled (set end date, transition to historical -- topology change as lifecycle, not as simulation)

**What this does NOT do:**
- Does not determine what happens when relationships reach extremes. That's card territory.
- Does not create or destroy relationship links. That's generator/action territory.
- Does not produce outcomes. Only shifts values that feed into pressure.

**MVP scope:** Core implementation with basic modifiers (proximity, shared culture, competing claims, resource competition).

### 8. Resources (MVP: Ambient Hex Resources)

The MVP resource model uses three ambient per-hex fields:

- `food`
- `wealth`
- `population`

These are properties of the map graph, not entity attributes.

They represent what a place can support, not a concrete actor-owned inventory layer. Processes read and modify them as thresholds, utility inputs, support targets, and background mass.

Examples:

- ambient `food` slows NPC hunger growth
- `Eat` still requires a named settlement for strong negative hunger
- `Grow` depends on ambient local `food` and `population`
- `Irrigate` increases ambient local `food` to improve later growth

See `docs/systems/resources-mvp.md` for the current implementation and design intent. See `docs/systems/08-resources-economy-POST-MVP.md` for the future expansion principles.

### 9. Lifecycle

Aging, death, growth, decay. The passage of time affects entities.

**What it is:**
- Heroes age. They have a lifespan. They die of old age if nothing else kills them.
- Settlements grow when prosperous (population increases via attraction) and shrink when not.
- Artifacts degrade over time (condition decreases) unless maintained.
- All entity kinds have a time-dependent lifecycle drain function. Factions lose cohesion over time. See System 9 (Lifecycle).
- Prominence decays for all entities unless actively maintained (this is the always-on sink). **POST-MVP: prominence system is deferred.**

**MVP scope:** Time-since-creation aging function per entity kind. Settlement population decline from age, entity aging/death, artifact degradation. Prominence decay is post-MVP. All entity kinds have lifecycle behaviors from day one.

### 10. Map Visualization

Dynamic visual representation of simulation state on the map. This is NOT a simulation system -- it reads state from low-altitude systems and renders it. It writes nothing back.

**What it renders:**
- Entity positions and movement (heroes traveling, settlements growing/shrinking, national territory shifting)
- Relationship state spatially (alliance/rivalry lines, control territories, factional presence)
- Attribute gradients as overlays (settlement prosperity, cultural reach, pressure heatmaps)
- Seasonal terrain variation (cyclic appearance changes)
- Status transitions (ruins where settlements once stood, faded borders of former nations)

**Overlay modes** (toggle between, Cities Skylines pattern):
- Political (nation territory), Cultural (reach gradients), Pressure (heatmap per type), Entity density, Relationship network, Terrain

**Design principle:** The map must be dynamic and correlate to game state. The player learns by looking. Spatial patterns that aren't obvious from entity panels become visible on the map.

**MVP scope:** Entity markers, national territory fill, attribute-driven overlays (at least political and one pressure type), seasonal terrain variation. Animated bulk flow (population/goods movement along routes) is post-MVP.

**Bulk flow (post-MVP):** When the full resource system is expanded, animated population and goods flow along routes becomes a map visualization feature.

### Event Log

Scrolling event log. The primary communication channel between the simulation and the player. Every significant state change (entity creation, war declaration, alliance formation, settlement founding, NPC death, card firing) generates a log entry. Entries are brief, link to involved entities, and are filterable by type and entity.

Also serves as the debugging backbone during development. Same infrastructure supports player awareness and developer causality tracing.

**MVP scope:** Scrolling text log, filterable, with clickable entity references. No popups, no map pings, no pause-on-event. See `docs/systems/event-log.md`.

### 11. Pressure

Smoothed, homeostatic-bounded aggregate values that evolve over time. The BRIDGE between low-altitude simulation and mid-altitude card evaluation.

**What it is:**
- Global or regional pressures computed from low-altitude system outputs: military tension, economic stress, cultural friction, demographic pressure
- Smoothed: pressure values don't spike instantly. They rise and fall gradually, reflecting sustained conditions rather than momentary fluctuations.
- Homeostatic: pressures drift toward a baseline unless actively pushed by sustained low-altitude conditions. Brief disruptions dampen out. Sustained forces push pressure past thresholds.
- Cards read pressure values to evaluate preconditions. "When military tension in a region exceeds X" is a card precondition that triggers based on pressure state.

**Data model:**
- Pressure definitions (name, scope [global/regional], baseline, bounds, smoothing rate)
- Pressure values per scope (global value, or per-region values)
- Input mappings: which low-altitude system outputs feed into which pressures, and how

**MVP scope:** Core implementation with 3-4 pressures: military tension, economic stress, cultural friction, demographic pressure. Specific pressure definitions will be tuned through iteration.

---

## Player Systems

Systems the player directly interacts with.

### 12. Deck

The player's card collection and composition.

**What it is:**
- The player's full collection of unlocked cards
- Deck composition: which cards from the collection are in the active deck for this run
- Draw mechanics: how cards move from the deck to the player's hand/available pool (specifics TBD -- keep isolatable)
- Card definitions: each card has a type (generator/system/action), preconditions, effects, mana cost, tags

**What is deferred:**
- When the deck is built (before run, during run, hybrid)
- How cards enter the hand (draw from deck, world-state availability, market, draft)
- Whether/how the deck is modified during a run

**MVP scope:** Core card data model and a basic deck/draw mechanism. The specifics of deck construction and draw mechanics are designed to be swappable.

### 13. Tableau / Mana

The player's active card configuration and resource management.

**What it is:**
- The active tableau: which cards are currently placed and running in the simulation
- Mana pool: total mana available, mana cost per card, mana cap
- Activation/deactivation: placing a card into the tableau activates its simulation component. Removing it deactivates.
- Mana cap expansion: mechanism TBD (buildings, milestones, era progression), but mana cap grows over the course of a run, allowing more simultaneous cards

**Card removal:** Costly, not free. Cards have lifespans and expire naturally. Early removal requires sacrificing a hand card and incurs an early-removal penalty. Some cards can remove other cards as an effect. Free swapping is explicitly rejected -- see `docs/systems/13-tableau-mana.md` for full model. (During testing/MVP, free removal may be enabled for iteration.)

**MVP scope:** Tableau with slots, mana pool with cap, activation/deactivation. Card removal with hand-card sacrifice. Mana cap expansion via deck modification events.

### 14. Lose Condition Monitor — POST-MVP

Tracks three threat dimensions (extinction, stagnation, monoculture). Deferred to post-MVP because thresholds require a playable simulation to calibrate against.

**MVP substitute:** Surface the underlying metrics (entity counts, dynamism measures, diversity indices) as dashboard values without attaching lose conditions. These metrics become the basis for threshold calibration later.

See `docs/systems/14-lose-condition-monitor-POST-MVP.md` for design intent.

### 15. Save / Load

Serialize and restore full simulation state.

**What it is:**
- Serialize the complete simulation state: entity graph, all attributes, all relationship values, map state, pressure values, bulk flow state, tick counter, deck state, tableau state, mana pool, lose condition monitor state
- Deserialize to restore a simulation to an exact prior state
- Multiple save slots
- Autosave at era transitions and on a timer

**Why MVP:**
- 4+ hour sessions require the ability to stop and resume
- Essential for testing and iteration during development
- Architecturally, save/load forces clean serialization boundaries between systems, which is good hygiene

**Design considerations:**
- Every system must be serializable. This is an architectural constraint that should be enforced from the start, not retrofitted.
- Save format should be versioned for forward compatibility as systems are added/modified.

**MVP scope:** Full implementation. Every system must support serialization from day one.

---

## Card Execution Systems

Where the player's cards meet the simulation. These share a common base (precondition evaluation, effect application) but differ in what they produce and when they run.

### Shared Base

All three executors share:
- **Precondition evaluation**: given current world state (entity attributes, relationships, pressures, map state), are this card's preconditions met?
- **Effect application**: apply the card's defined effects to the entity graph (create entities, modify attributes, create/destroy relationships)
- **Frequency/cadence**: how often this card's preconditions are checked and effects applied
- **Scope**: what entities/regions this card's evaluation considers

### 16. Generator Executor

Creates new entities when conditions are met.

**What it does:**
- Checks generator card preconditions each generation tick
- When preconditions are met, creates a batch of pre-connected entities using the generator's template
- Entities are created with initial attributes, relationships, and spatial positions defined by the template
- Creation is influenced by world state (available regions, existing entities, cultural context)

**Cadence:** Generation ticks (infrequent -- every N ticks, configurable per card)

**MVP scope:** Full implementation. This is how the world evolves.

### 17. System Executor

Runs card-defined world processes.

**What it does:**
- For each active system card, runs its logic on the appropriate tick cadence
- System logic reads world state and modifies entity attributes, relationships, or pressures
- Systems can have card-to-card interactions (tags/keywords that amplify or dampen when co-active)

**Cadence:** Variable per system card (some run every tick, some every N ticks)

**MVP scope:** Full implementation. These are the pressure release valves.

### 18. Action Executor

Makes actions available to actors and resolves them.

**What it does:**
- For each active action card, registers available actions with the entity behavior system
- Actors (nations, factions, NPCs, and any entity kind with agency) evaluate available actions based on their state, needs, and situation
- When an actor selects an action, the executor resolves it: checks success/failure probability, applies effects (attribute changes, relationship changes, prominence changes)
- Actions have dual outcomes: success effects and failure effects

**Cadence:** Actor ticks (frequent -- every tick or every few ticks)

**MVP scope:** Full implementation. These define the possibility space.

---

## System Dependency Graph

```
Entity Graph ← everything reads/writes this
    ↑
Entity Kinds → defines valid structure for the graph
    ↑
Attributes → per-entity numeric state
    ↑
Map / Spatial → positions, adjacency, terrain (includes procedural generation)
    ↑
Tick Clock → drives all updates
    ↑
LAYER 1: PHYSICS (automatic, unconditional) — Upkeep + Relationships kernel programs
├── Lifecycle → aging drain, death transitions, ambient resource drift
├── Relationships → strength drift from proximity, culture, competing claims
├── Ambient resources → per-hex food/wealth/population fields
└── Map Visualization → projection consumer of primitive events; not a sim program
    ↑
LAYER 2: PROCESSES (planner + utility AI, one per entity per tick) — Processes kernel program
├── Per-kind process menus (data-defined in domain config, compiled to IDs)
├── Cross-entity mutations (NPC eats → settlement loses food)
├── Multi-step behavior emerges from sub-goal generation (see MULTI_STEP.md)
└── Wiring contracts validated at domain compile time
    ↑
LAYER 3: DECISIONS / GAMBITS (utility AI, card-gated, one per entity per tick) — Gambits kernel program
├── Currently always available; full tableau-driven gating is deferred
├── Consume population (universal action currency)
└── Produce significant world-state changes (new relationships, entities)
    ↑
Pressure → BRIDGE: smoothed aggregates of layers 1-3 — Pressures kernel program
    ↑
CARD EXECUTION (mid-altitude) — Generators + Degeneracy kernel programs
├── Deck → player's card collection
├── Tableau / Mana → active cards + resource pool (TheCanonryGame.Game.Tableau)
├── Save / Load → KernelSnapshot binary round-trip
├── Card Precondition Evaluation (reads pressure, attributes, relationships, map)
├── Generators → create entities (what the world BECOMES)
├── Systems → modify world processes (how forces BEHAVE)
├── Actions → register decisions/gambits into Layer 3 above
└── Degeneracy → detect spatial / agency / actor degenerate trends
    ↑
Effects applied back to WorldState; events emitted to KernelEventLog;
projection consumers update UI read models (loop)
```

Code-level mapping of these layers to the kernel programs registered in
`Engine.Kernel/Simulation.cs` is in `docs/architecture.md`.

---

## MVP Priority Order

For a first playable vertical slice, build in this order:

1. **Entity Graph + Entity Kinds + Attributes** -- the data foundation
2. **Map / Spatial** -- regions, positions, procedural map generation
3. **Tick Clock** -- time advancement with speed control
4. **Entity Behaviors (all kinds)** -- nation border tension/resource flow, cultural pressure, settlement growth/decline, faction politics, artifact degradation/resonance, hero needs/movement
5. **Relationships + Lifecycle** -- basic game physics
6. **Map Visualization** -- render low-altitude state on the map (overlays, entity markers, territory)
7. **Pressure** -- a few computed aggregate pressures bridging low to mid altitude
8. **Save / Load** -- full state serialization (architectural constraint: all systems must be serializable)
9. **Deck + Tableau + Mana** -- player card system
10. **Generator + System + Action Executors** -- card execution
Steps 1-7 produce a living world with low-altitude physics and visible pressure. Step 8 enables persistence. Steps 9-10 add the player's ability to define the world's nature.

### 19. Narrative Domain

The content layer that defines what world the player is playing in. NOT cosmetic -- mechanical. Defines subtypes, cards, domain-specific low-altitude physics, ground truths, naming rules, map generation parameters, and strategy archetypes. The engine is domain-agnostic. The game IS a domain.

The first domain is **The Ice Remembers** -- penguin civilizations on floating icebergs, orca threats, the Long Dark, ice magic, soup culture.

The domain is a single swappable data package. Different domains produce different games on the same engine.

**MVP scope:** The Ice Remembers as the first and only domain. Domain package format defined. Engine/domain boundary enforced from day one.

See `docs/systems/19-narrative-domain.md` for full design.

---

## MVP Priority Order (Revised)

**MVP entity kinds: Region, Settlement, Faction, NPC, Artifact.** Additional kinds such as Point of Interest, Monster, Transportation, Ability, and Occurrence remain deferred. See `docs/systems/entity-kind-registry.md`.

**Phase 1: Engine + card executor stubs with generic test content.** Build and validate the domain-agnostic engine using the 5 MVP entity kinds (Region, Settlement, Faction, NPC, Artifact) with placeholder subtypes and cards. Includes card executor and tableau stubs so we can programmatically place test cards and observe their effects -- no deck/hand/draw mechanics yet, just "put this card in the tableau and see what happens."

1. **Entity Graph + Entity Kinds + Attributes** -- the data foundation with 5 MVP kinds (Region, Settlement, Faction, NPC, Artifact) and generic subtypes
2. **Map / Spatial** -- hex grid, fixed test map (procedural generation post-Phase 1)
3. **Tick Clock** -- time advancement with speed control
4. **Resources (ambient hex fields)** -- `food`, `wealth`, and `population` as map-state possibility space
5. **Entity Behaviors (MVP kinds)** -- settlement growth/support actions, faction cohesion/reach, NPC needs/movement, artifact degradation/compulsion
6. **Relationships + Lifecycle** -- basic game physics with generic relationship kinds
7. **Pressure** -- a few generic pressure types bridging low to mid altitude
8. **Tableau + Mana (stub)** -- active card slots and mana pool. Cards placed programmatically for testing.
9. **Generator + System + Action Executors** -- card execution with generic test cards
10. **Event Log** -- scrolling event log for debugging and player awareness
11. **Map Visualization** -- render low-altitude state + card effects on the map
12. **Save / Load** -- full state serialization

**Initial world state:** A single fixed configuration defined programmatically. One map, one set of starting entities and relationships, always the same. Modifiable through code for different test scenarios.

Phase 1 produces: a living world with low-altitude physics, ambient map resources, visible pressure, an event log, AND the ability to place test cards and observe their mid-altitude effects. Validates the entire altitude model end-to-end.

**Phase 2: Narrative Domain definition.** Define The Ice Remembers domain package: specific subtypes, specific cards, specific pressures, specific ground truths, naming rules. Content creation informed by the validated engine.

13. **Narrative Domain (The Ice Remembers)** -- domain package populating the engine with real content

**Phase 3: Full player systems.** Wire up the deck/hand/draw mechanics, archetype selection, deck modification events, and card enhancement/modification using domain content.

14. **Deck + Hand + Draw mechanics** -- cycling deck, timed + milestone draws, hand management
15. **Deck Modification Events** -- achievement conditions, add/remove/modify/expand-mana choices
16. **Card Enhancement / Modification** -- Balatro-inspired per-card modification layers

**Post-MVP kinds:** Region, Point of Interest, Faction, Monster, Transportation, Ability, Occurrence.

**Post-MVP systems:** Degenerate state metrics, expanded resource types (3-6), bulk flow visualization, prominence system, additional narrative domains, procedural map generation.
