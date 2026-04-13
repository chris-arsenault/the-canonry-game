# Design Constraints

Things this game is NOT, decisions we've explicitly rejected, and boundaries we're maintaining. This is the companion to `design-principles.md` -- the principles say what to build, this says what to avoid.

---

## Rejected Architectural Patterns

### No Hidden Storyteller / Director AI

No RimWorld-style storyteller AI curating events behind the scenes. No canonry illuminator concept. No hidden meta-layer guiding drama, pacing, or difficulty. The simulation's behavior must be fully explainable by the visible game systems + the player's active cards + world state. If the player can't understand why something happened by examining what's on screen, the design is broken.

**Why rejected:** The core thesis is "the player defines the world's nature." A hidden director contradicts this -- it means the world has a nature the player didn't author and can't see. Transparency is non-negotiable.

**What we might do instead:** A visible "locked tableau" of baseline system behaviors. These are always-on, always-inspectable, and the player knows they exist. No hidden cards, hidden preconditions, or hidden event generation (though this may be revisited as a future feature -- see backlog).

### No Entity-Agnostic Rulesets

The canonry's approach of entity-agnostic systems (where any rule could apply to any entity kind) was too mentally complex for both designer and AI collaborator. Entity kinds, subtypes, relationship kinds, and status values are all hard-coded. A "nation" is a nation with nation-specific behaviors, not a generic entity that happens to have nation-like properties.

**Why rejected:** Validated through canonry experience. The cognitive overhead of entity-agnostic design exceeded the flexibility benefit. Hard-coded entity kinds allow kind-specific continuous variables, valid relationship types, and behavioral rules that are clearer to reason about.

**What remains declarative:** The generators, systems, and actions themselves. The schema (entity kinds, relationships, statuses) is code. The simulation components (cards) are data.

### No Generative AI

The simulation is entirely procedural/mechanical. No LLM calls, no AI-generated text, no neural network inference. Entity names are procedurally generated from rules. Event descriptions are template-based. All output is deterministic given the same state.

**Why rejected:** The game should work offline, be reproducible, and not depend on external services. The simulation's behavior should be fully explainable by its rules, not by an opaque model.

---

## Rejected Game Feel

### Not a Crisis Management Game

The player is not a firefighter. The game should not feel like a series of emergencies requiring urgent response. Crises happen, but they emerge from the nature the player defined, and the player's response is deliberate (study, plan, adjust the tableau) not reactive (panic, scramble, patch).

**Why rejected:** The tempo is contemplative. Urgency undermines study. If the player is always reacting to the latest crisis, they never get to observe and understand the deeper dynamics of their world.

### Not an Optimization Puzzle

There should be no single correct tableau, no solved meta, no "best strategy." Different deck compositions produce different worlds. "Interesting" is subjective. The simulation's natural resistance to degenerate states (extinction from unchecked decay, stagnation from unresolved pressure, monoculture from unchecked dominance) combined with the emergent complexity of card interactions should resist reduction to a formula.

**Why rejected:** If the game is solvable, the contemplative tempo becomes boring -- there's nothing to study because the answer is known. The game's replayability depends on genuine uncertainty about what card combinations will produce.

### Not a Narrative Game

No scripted story. No dialogue trees. No authored narrative arcs. No quest lines. Entities are stat blocks with names and relationships. Events are brief, system-generated descriptions. The player's experience is observing and interpreting emergent patterns, not following a story.

**Why rejected:** Authored narrative fights emergence. If the game scripts "the Kingdom of Kesh will fall in era 3," then the player's cards don't really define the world's nature -- the script does. Emergence means the designers don't know what will happen either.

**What "narrative" means here:** The player constructs their own narrative from observation. "The Keshians expanded too fast, their stability collapsed, and the mountain cultures absorbed their border settlements" is a narrative the player reads from the simulation's output. The game provides the data. The player provides the story.

### Not a Real-Time Strategy Game

The player does not command units, manage economies directly, or fight battles. They do not click on a nation and tell it where to expand. They do not set production queues for settlements. They define the rules by which the simulation's entities -- nations, factions, NPCs, artifacts, and all others -- behave autonomously.

**Why rejected:** Direct control contradicts "the deck defines the world's nature." If the player can bypass the card system by micromanaging entities, the card system is decorative, not structural.

**Exception: Intervention cards.** One-shot hand cards that let the player act directly (smite an enemy, destabilize a nation, cause an earthquake, bless an artifact). These are scarce, costly, and represent divine intervention -- the exception that proves the rule of indirect control.

---

## Rejected Mechanics

### No Per-Entity Tableau / Multiple Tableaux

The player manages ONE global tableau, not a separate card set for each nation/culture/entity. Managing 5+ tableaux simultaneously is cognitive overload and fights the contemplative tempo.

**Why rejected:** Research showed this creates management burden, not strategic depth. The god-perspective means the player's cards affect the whole world, with asymmetric effects on different entities.

**Exception: Patron mode.** Favored nation mode adds targeted cards to the global tableau but does not create a second tableau. The structure remains one tableau with mode-specific card pools.

### No "Everything On" Default

The player cannot have all simulation systems active simultaneously. Slot limits (gated by mana) prevent this. This is both a mechanical constraint and a cognitive one -- 30 active systems is too many for a designer to cognate, validated by canonry experience.

**Why rejected:** Without scarcity, there are no interesting decisions. If every system can be active, the game reduces to "unlock everything as fast as possible." The slot limit forces prioritization, which is the strategic core.

### No Default Pressure Outlets

When game-system pressure builds (relationships deteriorate, resources become scarce, populations outgrow their home), there is NO default resolution mechanism. The pressure sits until the player provides an outlet via cards, or until it reaches extremes that trigger cascading failure or lose conditions.

**Why rejected:** Default outlets would make cards optional rather than structural. If negative relationships automatically cause minor skirmishes, then "Declare War" is an upgrade, not a defining choice. The absence of outlets is what makes each card meaningful -- you are deciding what the world CAN do with its tensions.

### No Storyteller Event Timer / Random Catastrophes (For Now)

No hidden timer that generates random crises. No "every N ticks, something bad happens" mechanic. All events emerge from the simulation's game systems + the player's active cards.

**Why rejected:** Hidden event generation is a form of hidden storyteller, which violates transparency. Random catastrophes also fight the contemplative tempo by injecting unearned urgency.

**Backlogged:** The concept of "hidden cards" -- cards that are unlocked, placed on the tableau, and activate when hidden preconditions are met -- is interesting but deferred. This would be a transparent mechanic (the player knows the card exists and that it has a trigger) with hidden timing (they don't know exactly when). This is a future exploration, not an initial design feature.

---

## Rejected Scope

### No Chronicle as Primary Feature

Entities are stat blocks with names and relationships. Events are brief, system-generated descriptions. There is no Dwarf Fortress Legends Mode-style deep historical record as a primary game feature. The game is about the living state of the world, not its historical record.

**Why rejected:** Building a rich chronicle system is a large investment that doesn't serve the core loop (observe, study, intervene). The player's attention is on the map and entity details, not on reading history. Basic event logging exists for tracking, but it's not a feature the game is designed around.

**May revisit:** A post-run summary or lightweight timeline could be valuable later. But it's not core.

### No Multiplayer, No Sound, No Mobile, No Modding (Initial Design)

These are explicitly out of scope for the initial design. They may come later but should not influence architectural decisions now.

### Map Generation Scope for First Playable

The first playable may use a fixed hand-authored map or a minimal fixed-seed generator. Full procedural generation is MVP but not required for the initial prototype. The simulation's complexity is in the entity graph and card interactions -- map generation quality is important but doesn't need to be solved first.

---

## Inherited Constraints from The Canonry

### Engine vs. Domain Schema Boundary

**Engine-level (hard-coded):** Entity kinds are defined in engine code. The engine knows what a "nation" is, what a "location" is. Adding a new entity kind requires code changes.

**Domain-level (defined per domain, can be programmatic):** Subtypes, relationship kinds, attribute definitions, pressure definitions, and status values are defined by the narrative domain module. The domain module CAN be programmatic (it doesn't have to be purely JSON), but it is a separate module from the engine. Domains define subtypes ("ice-warden" is a hero subtype in The Ice Remembers), relationship kinds ("allied_with" is defined by the domain), and attribute schemas.

**Declarative:** Generators, systems, and actions (cards) are data-driven, defined in DSL. This is the expandability boundary: new cards require only data changes.

This means a second domain can define entirely new subtypes, new relationship kinds, and new attribute configurations without engine changes, but it cannot define new entity kinds.

### 30 Systems Is Too Many

Validated through canonry experience: a designer cannot cognate 30 simultaneous systems. The card scarcity mechanic serves as a complexity budget. The mana/slot system formalizes this -- the player can only have N active cards, where N is small enough to reason about.

---

## Process Constraints

### All Mechanics Must Be Isolatable

Every mechanical subsystem (deck building, mana model, card costs, draw mechanics, swap rules) must be separable and independently tweakable. No mechanical decision is final until tested through play. Tight coupling between systems makes iteration impossible.

### Defer What Needs Hands-On Testing

Several mechanics are explicitly deferred until we have a playable prototype:
- Deck construction timing and method
- Mana model specifics
- Card draw/availability mechanics
- Card swap rules
- Intervention card design
- Favored nation integration details

These will be designed through iteration, not upfront specification.
