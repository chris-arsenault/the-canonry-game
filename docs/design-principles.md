# Design Principles

The central thesis and core principles of the game. Everything else flows from these.

---

## Central Thesis

The world has physics that creates conditions. Your deck defines the world's **nature**: what it becomes, how its forces behave, and what its actors can do.

The player is not commanding the world. They are defining its character.

---

## The Three Facets of Nature

A world's nature is expressed through three card types, each defining a different dimension:

### Generators: What the World Becomes

Generators define the world's evolutionary potential. When simulation conditions are right -- a frontier fills with people, a nation's ideology fractures, a faction gains enough reach to break away -- generators determine what emerges. A new nation. A splinter faction. A legendary artifact. A monstrous threat.

Generators are not pressure release. They are creation. They respond to simulation conditions (population density, political instability, faction reach) and produce new entities that reshape the graph. The player's choice of generators defines the trajectory of the world: a deck heavy on faction generators produces a world of competing organizations vying for power within nations. A deck heavy on political generators produces a world of rising and falling empires.

### Systems: How Forces Behave

Systems define how the world's existing tensions and dynamics operate. When relationships deteriorate, what happens? When resources are scarce, how does scarcity propagate? When nations of different cultures share a border, does friction escalate or does trade flourish?

Systems are the pressure-release valves. The world's physics (game systems) build pressure -- relationships shift, resources accumulate and deplete, populations grow. Systems determine how that pressure resolves. Without system cards, pressure builds with no outlet. The player's choice of systems defines the world's temperament: volatile or stable, interconnected or isolated, competitive or cooperative.

### Actions: What Actors Can Do

Actions define the possibility space for the world's agents. Nations, factions, and NPCs evaluate available actions each tick and choose based on their state and situation. The player doesn't perform actions -- they make actions possible.

Actions are about agency. "Forge Alliance" doesn't forge an alliance. It makes alliance-forging a thing that actors in this world can do. The player's choice of actions defines what moves are on the board: a world with "Espionage Networks" and "Diplomatic Summit" plays differently than one with "Holy Crusade" and "Forced Conversion," even if the underlying conditions are identical.

---

## Core Principles

### 1. The World Is Always Alive

Game systems always run. Relationships form and shift. Resources accumulate and deplete. Populations grow and shrink. Nations experience border tension. Cultural differences between neighboring entities create friction. Artifacts degrade and accumulate resonance. Entities age and die. Pressure builds. These are the physics of the world -- they create conditions, they are not configurable, and they are always on.

The world doesn't need the player to exist. It needs the player to have a nature.

### 2. The Deck Defines Nature, Not State

The player's deck does not control what happens. It defines what CAN happen. The simulation determines the specific outcomes based on world state, entity attributes, and the rules the deck provides.

A world with "Declare Military War" in the tableau doesn't mean war happens. It means war is possible when conditions are met. The player shapes the rules of reality. The simulation plays by them.

### 3. Unresolved Pressure Is the Default

The world is full of tensions with no outlet. Two nations with deteriorating relationships have no resolution mechanism unless the player provides one. This is the default state -- and it's fine, up to a point.

What the player DOESN'T put in their deck matters as much as what they do. A world without war cards is a world where military conflict doesn't exist as a concept. A world without cultural exchange cards is a world where cultural friction between nations has no outlet. Omission is a choice.

At extremes, unresolved pressure triggers cascading failures or lose conditions. The player cannot ignore all tensions forever. But they choose which ones to give outlets and which ones to let build.

### 4. Mana Gates Complexity

The mana pool determines how many simultaneous cards can be active -- how many facets of the world's nature the player can define at once. Early game: few active cards, each choice weighty, the world has limited outlets and limited evolutionary paths. Late game: more cards, the world becomes richer but less predictable as more systems interact.

Mana is expandable (e.g., through in-simulation buildings or milestones) but not a forced economy. The player CAN invest in expanding their capacity but doesn't HAVE to. Mana gates complexity, not power.

### 5. Everything Visible Is Everything There Is

No hidden storyteller. No invisible director. No behind-the-scenes event curation. The game systems are the game systems. The cards are the cards. The world state is the world state.

If the player understands their active cards and the current world state, they can understand why anything happened. Transparency is non-negotiable. The player is the author of the world's nature -- they cannot author what they cannot see.

A "locked tableau" of non-player-modifiable baseline behaviors may exist, but it is visible and inspectable. There are no hidden cards, hidden preconditions, or hidden interventions (this may change in future iterations, but not in the initial design).

### 6. The Tempo Is Contemplative

Long observation. Study the state. Make a deliberate move. Watch the consequences.

The player watches pressure build, studies where conditions are concentrating, examines entity details to understand the dynamics, and then makes a move -- swapping a card, playing an intervention, adjusting the tableau. The move is deliberate because it was preceded by understanding.

This is SimCity/Stellaris pacing: speed controls let the player fast-forward through quiet periods and pause to plan during complex moments. The game must reward looking -- information density must be high enough that every additional second of observation teaches something about the system.

The difference between "contemplative" and "nothing to do" is whether looking is productive. The map and entity details must always have something to study.

### 7. Degenerate States, Not Lose Conditions

The simulation can reach degenerate states that are no longer interesting to play:

- **Extinction**: all civilizations collapse. The simulation has nothing left.
- **Stagnation**: the world is alive but nothing is changing. Equilibrium without dynamism.
- **Monoculture**: one entity has consumed everything. Alive but homogeneous.

These are design concerns, not formal lose conditions. The game resists degenerate states through a three-layer model:

1. **Low-altitude shock absorbers (automatic):** built into entity physics. Collapse creates fragments not voids. Scarcity drives migration not death. Lifecycle drain is gradual not cliff-edge. These SLOW the decline, buying the player time.
2. **Card-level responses (player choice):** each degeneracy pattern has generator, system, and action cards that can address it. The player chooses HOW to respond based on their worldview. This is the interesting decision.
3. **UI warnings (explicit):** known degeneracy patterns are codified as detectors. When thresholds are crossed, the player sees warnings. Informational, not prescriptive.

The game is about guided evolution, not degeneracy prevention. But the player must be aware when the world is trending badly so they can steer. Without intervention, shock absorbers buy time but don't prevent collapse indefinitely.

Degenerate patterns are derived from entity categories. Anti-degeneracy responses come from card types. MVP covers three detectors with UI warnings: settlement depopulation (spatial degeneracy), political monoculture (agency degeneracy), and NPC extinction (actor degeneracy). Stagnation detection is post-MVP, but the low-altitude physics that resist stagnation (lifecycle decay, relationship drift, pressure accumulation) are active from day one -- we just don't alert on it yet. See `docs/anti-degeneracy.md` for the full mechanism mapping, MVP patterns, and implementation requirements.

There are no win conditions in neutral mode. Success is measured through the player's own goals. Patron mode (favored nation) adds optional win conditions post-MVP.

### 8. The Deck Is a Theory

Each run, the player assembles a deck -- a theory about what kind of world they want to create. "I believe conflict should resolve through cultural isolation, not war. I believe population pressure should resolve through migration, not famine. I believe the world should evolve toward cultural diversity, not imperial consolidation."

The simulation tests this theory against emergent reality. The theory may prove wrong -- the combination of cards may produce unexpected dynamics, cascading failures, or boring equilibrium. The player adapts: swapping cards, adjusting their theory, or starting a new run with a revised worldview.

Over many runs, the player develops deeper intuition about how card combinations interact with world conditions. This is the meta-progression's real reward: not more powerful cards, but deeper understanding.

### 9. All Mechanics Are Isolatable

Deck building (before run, during run, blind, informed), mana model (pool size, expansion mechanism, costs), draw mechanics (deck draw, world-state unlocks, drafting), card swapping rules (free, cooldown, cost) -- every mechanical subsystem must be separable and independently tweakable.

The design must support iteration through play. No mechanical decision is final until it's been tested. Systems that are tightly coupled to other systems become impossible to adjust. Keep them modular.

---

## What This Is Not

- **Not a reactive crisis game.** The player is not a firefighter responding to emergencies. They are an author defining rules. Crises happen, but they happen because of the nature the player defined, not because a director threw them.
- **Not an optimization puzzle.** There is no single correct tableau. Different deck compositions produce different worlds, and "interesting" is subjective. The game should resist solved strategies through emergent complexity and the three-way lose condition tension.
- **Not a narrative game.** The simulation produces events and entities have names and relationships, but there is no scripted story, no dialogue trees, no authored narrative. Events are brief, system-generated descriptions. The narrative is emergent and lives in the player's interpretation of what happened.
- **Not a real-time strategy game.** The player does not command units, manage economies directly, or fight battles. They define the rules by which the simulation's actors do those things.

---

## Open Design Areas

These are acknowledged unknowns that need hands-on iteration:

- **Deck construction timing and method** (before run, during run, blind, informed, modification during play)
- **Mana model specifics** (pool size, expansion mechanisms, per-card costs)
- **Card draw/availability mechanics** (deck draw, world-state unlocks, drafting, market)
- **Card swap rules** (free, cooldown, cost, era-locked)
- **Exact game systems vs simulation systems boundary** (which specific behaviors are always-on physics vs card-defined)
- **Favored nation mode integration** (how patron mode interacts with these principles)
- **Intervention cards** (hand cards, draw mechanics, costs, frequency)
- **Future: hidden event cards** (backlogged concept: hidden cards with hidden preconditions that activate when met)
