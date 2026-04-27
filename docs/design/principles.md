# Design Principles

The central thesis, the principles that flow from it, and the boundaries we
explicitly hold. Everything else in `docs/design/` is downstream of this file.

## Central Thesis

The world has physics that creates conditions. Your deck defines the world's
**nature**: what it becomes, how its forces behave, and what its actors can do.

The player is not commanding the world. They are defining its character.

## The Three Facets of Nature

A world's nature is expressed through three card types, each defining a
different dimension:

**Generators — what the world becomes.** Generators define the world's
evolutionary potential. When simulation conditions are right — a frontier
fills with people, a faction's ideology fractures, a settlement gains enough
reach to break away — generators determine what emerges. New entities. New
relationships. The player's choice of generators defines the trajectory of
the world: a deck heavy on faction generators produces a world of competing
organizations vying for power; a deck heavy on political generators produces
a world of rising and falling polities.

**Systems — how forces behave.** Systems define how the world's existing
tensions and dynamics operate. When relationships deteriorate, what happens?
When resources are scarce, how does scarcity propagate? When entities of
different cultures share a border, does friction escalate or trade flourish?
Systems are the pressure-release valves. Without system cards, pressure
builds with no outlet. The player's choice of systems defines the world's
temperament: volatile or stable, interconnected or isolated, competitive or
cooperative.

**Actions — what actors can do.** Actions define the possibility space for
the world's agents. Factions, settlements, NPCs, regions, and artifacts
evaluate available actions each tick and choose based on their state and
situation. The player doesn't perform actions — they make actions possible.
"Forge Alliance" doesn't forge an alliance; it makes alliance-forging a
thing actors in this world can do.

## Core Principles

### 1. The world is always alive

Game systems always run. Relationships form and shift. Resources accumulate
and deplete. Populations grow and shrink. Cultural differences create
friction. Artifacts degrade and accumulate resonance. Entities age and die.
Pressure builds. These are the physics of the world — not configurable, not
optional, always on. The world doesn't need the player to exist. It needs
the player to have a nature.

### 2. The deck defines nature, not state

The player's deck does not control what happens. It defines what CAN happen.
The simulation determines outcomes based on world state, entity attributes,
and the rules the deck provides. A world with "Declare Military War" in the
tableau doesn't mean war happens; it means war is possible when conditions
are met.

### 3. Unresolved pressure is the default

The world is full of tensions with no outlet. What the player DOESN'T put in
their deck matters as much as what they do. A world without war cards is a
world where military conflict doesn't exist as a concept. Omission is a
choice. At extremes, unresolved pressure triggers cascading failures or
degenerate states. The player cannot ignore all tensions forever, but they
choose which ones to give outlets and which ones to let build.

### 4. Mana gates complexity

The mana pool determines how many simultaneous cards can be active — how
many facets of the world's nature the player can define at once. Mana is
expandable but not a forced economy. Mana gates complexity, not power.

### 5. Everything visible is everything there is

No hidden storyteller. No invisible director. No behind-the-scenes event
curation. If the player understands their active cards and the current world
state, they can understand why anything happened. Transparency is
non-negotiable. A "locked tableau" of non-player-modifiable baseline
behaviors may exist, but it is visible and inspectable.

### 6. The tempo is contemplative

Long observation. Study the state. Make a deliberate move. Watch the
consequences. SimCity/Stellaris pacing — speed controls let the player
fast-forward through quiet periods and pause to plan during complex
moments. The map and entity details must always have something to study.
The difference between "contemplative" and "nothing to do" is whether
looking is productive.

### 7. Degenerate states, not lose conditions

The simulation can reach degenerate states that are no longer interesting:
extinction, stagnation, monoculture. These are design concerns, not formal
lose conditions. The game resists them through three layers — automatic
shock absorbers in low-altitude physics, card-level responses chosen by the
player, and explicit UI warnings when patterns are detected. There are no
win conditions in neutral mode; success is measured by the player's own
goals. (Patron mode adds optional win conditions post-MVP.) See
[anti-degeneracy.md](anti-degeneracy.md) for the full mechanism.

### 8. The deck is a theory

Each run, the player assembles a deck — a theory about what kind of world
they want. The simulation tests this theory against emergent reality. The
theory may prove wrong. The player adapts: swapping cards, adjusting their
theory, or starting a new run with a revised worldview. Over many runs the
player develops deeper intuition about how card combinations interact with
world conditions. This is the meta-progression's real reward: not more
powerful cards, but deeper understanding.

### 9. All mechanics are isolatable

Every mechanical subsystem (deck building, mana model, draw, swap rules)
must be separable and independently tweakable. No mechanical decision is
final until tested through play. Tight coupling between systems makes
iteration impossible.

## What this game is not

These boundaries are as load-bearing as the principles. They are decisions
already made.

**Not a hidden-storyteller game.** No RimWorld-style storyteller AI curating
events. No canonry illuminator concept. No hidden meta-layer guiding drama,
pacing, or difficulty. The simulation's behavior must be fully explainable
by visible game systems + active cards + world state. Transparency violates
the central thesis if traded away.

**Not entity-agnostic.** Entity kinds, subtypes, relationship kinds, and
status values are hard-coded. A nation is a nation with nation-specific
behaviors, not a generic entity that happens to have nation-like properties.
Validated through canonry experience: cognitive overhead of entity-agnostic
design exceeded the flexibility benefit. What remains declarative is the
simulation content (cards), not the schema.

**Not generative-AI-driven.** Entirely procedural and mechanical. No LLM
calls, no AI-generated text, no neural network inference at runtime. Names
are procedurally generated from rules. Event descriptions are
template-based. All output is deterministic given the same state and seed.

**Not a crisis-management game.** The player is not a firefighter. The game
should not feel like a series of emergencies requiring urgent response.
Crises happen, but they emerge from the nature the player defined, and the
player's response is deliberate, not reactive.

**Not an optimization puzzle.** No single correct tableau. No solved meta.
Different deck compositions produce different worlds. "Interesting" is
subjective. If the game is solvable, the contemplative tempo becomes boring.

**Not a narrative game.** No scripted story, no dialogue trees, no authored
arcs, no quest lines. Entities are stat blocks with names and relationships.
Events are brief, system-generated descriptions. The player constructs their
own narrative from observation. The game provides the data; the player
provides the story.

**Not a real-time strategy game.** The player does not command units, manage
economies directly, or fight battles. They define the rules by which the
simulation's entities behave autonomously. Direct control contradicts "the
deck defines the world's nature." (Exception: intervention cards are
one-shot hand cards representing divine intervention — the exception that
proves the rule of indirect control.)

**Not a multi-tableau game.** One global tableau, not a separate card set
per nation/culture. Five-plus tableaux is cognitive overload and fights the
contemplative tempo. (Patron mode adds targeted cards to the global tableau,
not a second tableau.)

**No "everything on" default.** Slot limits gated by mana prevent every
system from being active simultaneously. Without scarcity, there are no
interesting decisions. Validated by canonry experience: 30 active systems
is too many to cognate.

**No default pressure outlets.** When pressure builds, there is no default
resolution. Pressure sits until the player provides an outlet via cards, or
until it reaches extremes that trigger cascading failure or warnings.
Default outlets would make cards optional rather than structural.

**No hidden timer / random catastrophes (for now).** All events emerge from
game systems + active cards. Hidden event generation is a form of hidden
storyteller. The "hidden cards" concept (cards visible on the tableau with
hidden trigger timing) is a possible future feature, not initial design.

**No deep chronicle as primary feature.** Entities are stat blocks; events
are brief, system-generated descriptions. No Dwarf Fortress Legends Mode-style
historical record as a primary feature. Basic event logging exists for
tracking, but the player's attention is on the living state of the world,
not its archived history. A post-run summary may be added later.

**Out of initial scope:** multiplayer, sound, mobile, modding. Map
generation may use a fixed hand-authored map or minimal fixed-seed generator
for the first playable; full procedural generation is MVP but not required
for the first prototype.

## Engine vs. domain boundary

**Engine-level (hard-coded):** Entity kinds are defined in engine code. The
engine knows what a "settlement" is, what a "region" is. Adding a new
entity kind requires engine changes.

**Domain-level (defined per domain):** Subtypes, relationship kinds,
attribute schemas, pressure definitions, status values, and processes are
defined by the narrative domain module. The domain CAN be programmatic; it
does not have to be JSON. Domains define subtypes (e.g. "ice-warden" is an
NPC subtype in The Ice Remembers), relationship kinds, and attribute
configurations.

**Declarative content:** Generators, systems, and actions (cards) are
data-driven, defined in DSL. New cards require only data changes.

A second domain can define entirely new subtypes, new relationship kinds,
new attribute configurations, new processes, and new cards without engine
changes. It cannot define new entity kinds.

## Open design areas

Acknowledged unknowns that need hands-on iteration. None will be resolved
on paper.

- Deck construction timing and method (before-run / during-run / blind /
  informed / mid-run modification).
- Mana model specifics (pool size, expansion mechanism, per-card costs).
- Card draw / availability mechanics (deck draw, world-state unlocks,
  drafting, market).
- Card swap rules (free / cooldown / cost / era-locked).
- Exact game-systems-vs-simulation-systems boundary: which behaviors are
  always-on physics vs card-defined.
- Favored nation mode integration with these principles.
- Intervention card design (hand cards, draw, costs, frequency).
- Hidden event cards (backlogged: cards visible on the tableau with hidden
  trigger preconditions).
