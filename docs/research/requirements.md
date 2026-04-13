# Requirements Summary

## Vision

A high-altitude civilization simulation game. The player observes and influences the rise and fall of societies, nations, and cultures on a 2D physical map. The simulation is more than the sum of its parts: individual heroes, artifacts, and factions drive micro-events, but civilizational dynamics emerge from their aggregate behavior.

The game sits in a design space between Crusader Kings (character-driven political simulation), Victoria 3 (economic/demographic emergence), and Dwarf Fortress legends mode (procedural history). The player is a god-like observer with the ability to intervene, not a ruler of a specific nation.

---

## Inherited from The Canonry

We take four conceptual systems from the-canonry, adapted for a game context. We take the *ideas*, not the implementation.

### Generator Templates

Declarative templates that create batches of pre-connected entities. In the-canonry, these populate a world graph during "growth phases" using JSON configuration. Templates define entity kinds, subtypes, names, attributes, relationships, and initial prominence.

In this game, generator templates serve the same role: procedurally creating entities that make up the simulation's initial and evolving state. They fire during generation ticks to create new nations, found settlements, spawn heroes, forge artifacts, and establish the relationships between them. Templates are data-driven: the rules for "what gets created and how it connects to existing things" are defined declaratively, not in imperative code.

Key properties from the-canonry template system:
- Templates create **batches of pre-connected entities** (not isolated entities)
- Templates are **weighted** and selected based on current world state
- Templates have **preconditions** (this template can only fire if certain entities/relationships exist)
- Templates interact with **distribution targets** (homeostatic pressure toward desired entity counts)
- Template output is influenced by **era** (what gets generated changes over time)

### Actor/Actions

Entities take actions that modify the world. In the-canonry, actions have success/failure outcomes that affect prominence and continuous variables, with frequency-aware scaling (high-volume actions don't inflate everyone).

In this game, actions are the primary mechanism by which heroes, factions, and nations affect the world during actor and system ticks. An action is:
- **Declarative**: defined as data, not code (actor kind, preconditions, effects, probabilities)
- **Frequency-aware**: common actions have smaller effects than rare ones
- **Dual-outcome**: success and failure both have consequences
- **Targeted**: actions often have a target entity, which also experiences effects
- **Prominence-linked**: actions are the primary source of prominence gain/loss

Examples: a hero "leads army" action (increases hero prominence on success, decreases nation stability on failure), a faction "forge alliance" action (creates relationship on success, damages trust on failure), a settlement "trade route" action (increases prosperity of connected settlements).

### Entity/Relationship Graph

The world is a graph. The graph is the single source of truth for world state. All simulation ticks read from and write to the graph. The renderer and UI observe the graph.

Core entity structure (adapted from the-canonry's HardState):
- **id**: stable identifier
- **kind**: hard-coded entity kind (see Entity Kinds below)
- **subtype**: specialization within kind (e.g., "merchant", "warrior", "theocracy")
- **name**: display name (procedurally generated)
- **status**: lifecycle state (active, historical, dormant, destroyed)
- **prominence**: 0-5 numeric scale
- **culture**: cultural affiliation
- **tags**: key-value semantic tags for flexible metadata
- **relationships**: typed, directed links to other entities
- **coordinates**: physical position on the map
- **continuous variables**: kind-specific numeric state (stability, prosperity, etc.)
- **createdAt / updatedAt**: tick timestamps

Relationships have:
- **kind**: typed (controls, allied_with, member_of, part_of, founded_by, etc.)
- **source / target**: directed
- **strength**: optional numeric weight (e.g., alliance strength 0-1)
- **metadata**: optional additional data

### Prominence

A numeric scale (0-5: forgotten, marginal, recognized, renowned, mythic) representing how historically significant an entity is. Prominence is the simulation's editorial voice -- it determines what matters.

**Sources** (ways prominence increases):
- Actions (primary source): successful actions boost actor and sometimes target prominence
- System events: significant occurrences grant prominence (founding a city, winning a war, creating an artifact)
- Reflected glory: proximity to high-prominence entities provides a small boost

**Sinks** (ways prominence decreases):
- Decay: entities with few connections lose prominence over time
- Failure: failed actions reduce prominence
- Obsolescence: entities that haven't been involved in events gradually fade

**What prominence does:**
- Drives what the player notices (high-prominence entities surface in the UI, news feed, map display)
- Influences simulation priority (the storyteller/pacing system gives more attention to high-prominence entities)
- Determines historical weight (when reviewing a simulation run's history, prominence determines what events are narrated)
- Affects actor behavior (heroes gravitate toward high-prominence entities; factions compete for prominence)

Target distribution: roughly normal centered around 2.5-3.0, with only 10-15% of entities reaching mythic. Most entities should be marginal or recognized. Forgotten entities are effectively archived.

---

## Divergences from The Canonry

- **Hard-coded entity schema.** Entity kinds, subtypes, relationship kinds, and status values are all defined in code (the equivalent of the-canonry's schema editor/enumerist and cosmographer). No entity-agnostic rulesets. This was a deliberate decision: the-canonry's entity-agnostic approach was too mentally complex for both designer and AI collaborator. The schema is the fixed vocabulary of the simulation.
- **Declarative systems/templates/actions.** While the entity schema is hard-coded, the generators (templates), actions, and systems themselves remain **declarative and data-driven** -- defined in configuration, not imperative code. This is the expandability of the system: new templates can be added without code changes, new actions defined as data, new systems composed from existing primitives. This is also the foundation for the card/meta-progression system (see separate card mechanics doc).
- **Physical map.** Entities have spatial positions that matter. Geography drives simulation: trade routes follow terrain, cultural influence spreads by adjacency, wars require physical proximity.
- **GUI.** This is a game with a visual interface, not a procedural generation tool. The map is the primary representation, with drill-down panels for entity details.
- **No generative AI.** The simulation is entirely procedural/mechanical.

---

## Simulation Layers

The simulation operates across multiple tick types. These are **NOT strictly tiered**; any tick type can fire on any cadence. The pacing layer (see below) determines when each tick type fires.

### A) Actor Ticks

Low-altitude simulation. Individual agents (heroes, items, named characters) acting in the world. They move around the map, respond to dynamics, interact with other entities. They primarily affect **continuous variables** (health, wealth, loyalty, influence) without restructuring the graph.

Examples:
- A hero traveling from one settlement to another
- A merchant completing a trade route (increasing prosperity of connected settlements)
- An artifact changing hands (its "holder" relationship changes, but no new entities created)
- A faction leader making a speech (increasing faction influence, possibly swaying hero loyalty)
- A spy gathering intelligence (discovering hidden relationships or states)

Actor ticks are frequent and cheap. They are the heartbeat of the micro-simulation.

### B) System Ticks

The world's systems taking action, distinct from individual agents. Systems model forces that no single actor controls: disease, weather, economics, cultural drift, technological diffusion.

Examples:
- Disease spreading between connected settlements (based on proximity, trade routes, population density)
- Cultural influence diffusing across borders (adjacent regions slowly shift cultural affiliation)
- Economic pressure adjusting trade values (supply/demand across connected settlements)
- Weather/seasonal effects (harvests, storms, droughts affecting regional prosperity)
- Technological innovation spreading (cultures with contact to innovating cultures gain exposure)
- Prominence decay (all entities tick toward lower prominence unless actively maintained)

System ticks can be frequent (weather) or infrequent (cultural drift). Different systems update at different frequencies, following the Paradox pattern: economic recalculation might happen "monthly," cultural drift "yearly," while disease spread is "daily."

### C) Generation Ticks

High-altitude simulation. The graph structure changes substantially: new entities created, old ones destroyed or transformed, relationships form or break. This is the generator template system.

Examples:
- A nation splitting into two (civil war creates a new nation entity, redistributes settlements)
- A new religion emerging (new culture entity with connections to founding hero and origin settlement)
- A dynasty founding a city (new settlement entity connected to founding hero and controlling nation)
- An era transition (current era becomes historical, new era begins, templates shift)
- An artifact being forged (new artifact entity connected to creator hero, creation event, materials from region)
- A faction being disbanded or absorbed (entity status changes, relationships redistributed)

Generation ticks are infrequent and expensive. They are the punctuation marks of the macro-simulation.

### D) User Interaction

The player interacts through:

**God powers**: Direct interventions that override simulation internals.
- Cause a famine (set region prosperity to zero, trigger cascading effects)
- Inspire a hero (boost a hero's prominence and influence)
- Trigger an earthquake (destroy settlements in a region, displace populations)
- Forge a prophecy (create a new occurrence entity that drives hero behavior)
- Sever an alliance (remove relationship between nations)

**Entity attribute modification**: Directly editing entity state through inspector panels.
- Set a nation's stability value
- Change a hero's loyalty target
- Modify a settlement's population
- Override a faction's goals

The player is an outside force. Their interventions bypass normal simulation rules but feed back into the simulation through normal channels (a divinely-caused famine cascades through the economic system just like a natural one).

### E) Simulation Start States / Meta-Progression

The player can build simulation starting conditions, choosing what entities, relationships, and initial state the simulation begins with.

**Meta-progression**: Across simulation runs, the player unlocks new tools for shaping worlds.
- Completing milestones (e.g., "witness a nation reach mythic prominence," "see three cultures merge") unlocks new simulation start cards/attributes
- Start cards correspond roughly to the-canonry's generators, conditions, and targets:
  - **Generator cards**: unlock new template types (e.g., "Dragon Forges" enables artifact-creation events)
  - **Condition cards**: modify simulation rules (e.g., "Age of Sail" enables long-distance trade routes)
  - **Modifier cards**: adjust continuous variable dynamics (e.g., "Fertile Lands" increases base prosperity)
- This gives the player a reason to replay: each run unlocks new tools for shaping future worlds

---

## Pacing / Storyteller System

Drawing from RimWorld's storyteller AI and Paradox's event systems, the simulation needs a pacing layer that curates what happens and when.

**What the pacing system does:**
- Controls the cadence of generation ticks (don't create new nations every tick; space them for drama)
- Selects which events surface to the player (not every actor tick is interesting)
- Manages difficulty/drama curves (if everything is stable, inject pressure; if everything is collapsing, provide recovery windows)
- Prevents death spirals (circuit breakers when cascading failure threatens to make the simulation unrecoverable)

**What the pacing system does NOT do:**
- Script outcomes (it selects from weighted event pools, it doesn't determine results)
- Override simulation rules (it adjusts probabilities and timing, not mechanics)
- Remove player agency (the player's god powers always work regardless of pacing state)

This is a later-stage concern but architecturally important to plan for. The simulation must expose enough state for a pacing layer to make informed decisions.

---

## Entity Kinds (Initial Set)

Hard-coded, not configurable. Each kind has specific continuous variables, valid relationship types, and behavioral rules.

### Region
Geographic area on the map. Defines terrain, biome, resources.
- **Continuous variables**: fertility, resource richness, habitability
- **Key relationships**: contains (settlements), borders (other regions)
- **Behavior**: mostly passive; affected by weather/seasonal system ticks

### Settlement
City, town, or village within a region. The primary unit of population and economy.
- **Continuous variables**: population, prosperity, defense, development
- **Key relationships**: located_in (region), controlled_by (nation), trade_route (other settlements)
- **Behavior**: grows/shrinks based on prosperity and regional fertility; can be founded, conquered, destroyed

### Nation
Political entity controlling settlements and regions.
- **Continuous variables**: stability, military_strength, treasury, legitimacy
- **Key relationships**: controls (settlements/regions), allied_with/rival_of (nations), led_by (hero)
- **Behavior**: expands/contracts based on military and diplomatic actions; can split, merge, be conquered

### Culture
Shared identity, values, practices. Can span nations and exist independently of political borders.
- **Continuous variables**: influence, innovation, cohesion
- **Key relationships**: practiced_in (settlements/regions), originated_from (settlement), derived_from (parent culture)
- **Behavior**: spreads by adjacency and trade; can hybridize with neighboring cultures; innovations diffuse through cultural networks

### Faction
Organized group with goals. Guilds, cults, armies, trade leagues, religious orders.
- **Continuous variables**: influence, resources, cohesion, secrecy
- **Key relationships**: based_in (settlement), member_of (nation), led_by (hero), allied_with (factions)
- **Behavior**: pursues goals through member actions; competes with other factions for influence; can be created, disbanded, absorbed

### Hero
Named individual with agency. Leaders, warriors, scholars, prophets, merchants.
- **Continuous variables**: health, skill, loyalty, ambition
- **Key relationships**: leads (nation/faction), located_at (settlement), loyal_to (nation/faction), rival_of (hero)
- **Behavior**: takes actor-tick actions; moves around map; accumulates prominence through deeds; ages and eventually dies

### Artifact
Significant object. Weapons, relics, texts, technologies, treasures.
- **Continuous variables**: power, condition, mystique
- **Key relationships**: held_by (hero/settlement/faction), created_by (hero), created_at (settlement)
- **Behavior**: mostly passive but amplifies holder's actions; can be lost, stolen, destroyed, discovered

### Occurrence
Event that happened. Battles, plagues, discoveries, treaties, natural disasters.
- **Continuous variables**: impact (how much it changed the world), duration
- **Key relationships**: involved (heroes, nations, factions), happened_at (settlement/region), caused_by (hero/system)
- **Behavior**: created by generation ticks or significant actor/system events; purely historical after creation

### Era
Time period structuring the simulation.
- **Continuous variables**: none (eras are structural, not stateful)
- **Key relationships**: supersedes (previous era), contains (occurrences)
- **Behavior**: transitions when conditions are met (number of generation ticks, milestone events, pacing decisions)

---

## Continuous Variables

Entities track numeric state that changes through actor and system ticks. These form the substrate that generation ticks evaluate when deciding what to create or destroy.

**Universal:**
- **Prominence** (all entities): 0-5 scale. The simulation's measure of historical significance.

**Per-kind (see Entity Kinds above for assignment):**
- **Stability**: resistance to internal disruption. Low stability -> civil war, faction revolt, succession crisis.
- **Prosperity**: economic health. Low prosperity -> famine, migration, unrest.
- **Influence**: ability to shape neighbors. High influence -> cultural spread, diplomatic leverage.
- **Population**: demographic pressure. Drives economic output, military capacity, resource demand.
- **Loyalty**: allegiance strength. Low loyalty -> betrayal, defection, faction switching.
- **Military strength**: capacity for armed conflict. Drives territorial expansion/defense.
- **Cohesion**: internal unity. Low cohesion -> faction splits, cultural divergence.
- **Legitimacy**: recognized right to rule. Low legitimacy -> instability, rivals, coups.

These are not the full list. Additional variables will emerge during design. The key principle: continuous variables change gradually through actor/system ticks, while the **thresholds** on those variables trigger generation ticks (a nation whose stability drops below 0.2 triggers a "civil war" generation event).

---

## Cascading Effects

Drawing from Pandemic's outbreak chains and RimWorld's mood cascades, the simulation must support cascading failure (and cascading success):

- A famine in one region (low prosperity) reduces food supply to connected settlements
- Settlements with unmet needs generate unrest (low stability in controlling nation)
- National instability emboldens rival factions (faction influence increases)
- Faction actions may trigger a succession crisis (generation tick: nation splits)
- The split nation is weaker, inviting aggression from neighbors
- War destroys settlements, reducing regional prosperity, potentially starting the cycle elsewhere

Circuit breakers prevent infinite death spirals:
- Recovery mechanisms (a charismatic hero can stabilize a failing nation)
- Natural dampening (extreme states are harder to push further)
- Pacing system intervention (if too many cascades are happening simultaneously, the storyteller slows them down)

---

## Map and Geography

The 2D physical map is the primary representation. Map properties that matter to the simulation:

- **Terrain types**: plains, mountains, forests, deserts, coastal, water. Affect movement, settlement viability, resource availability.
- **Adjacency**: which regions border each other. Drives cultural spread, military access, trade routes.
- **Connectivity**: trade routes and paths between settlements. Can be created/destroyed by events.
- **Strategic positions**: chokepoints, river crossings, harbors. Drive political and military behavior naturally through geography.
- **Resources**: per-region resources that drive economic specialization and trade.

The map should create strategic pressure that drives political behavior naturally, following the Diplomacy principle: geography shapes politics without scripted political mechanics.

**Map visualization** (informed by exemplar analysis):
- **Color-gradient overlays** (Plague Inc pattern): paint the map with cultural influence, prosperity, stability, etc. Toggle between overlays.
- **Mapmodes** (Paradox pattern): political, cultural, religious, economic, terrain views that recolor the entire map.
- **Drill-down on demand** (RimWorld pattern): click any entity on the map to inspect it. The default view shows macro state; details appear when requested.
- **News ticker / event feed**: running text feed translating simulation events into readable narrative.

---

## Non-Requirements (For Now)

- Multiplayer
- 3D rendering
- Sound/music
- Save/load (will come later but not in initial design)
- Modding support
- Mobile deployment
- Web deployment (desktop-first; web possible later depending on engine choice)
- Procedural map generation (maps can be hand-authored initially)
- Real-time simulation (tick-based with time controls: pause, play, fast-forward)
