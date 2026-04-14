# System 19: Narrative Domain

The content layer that defines what world the player is playing in. Mechanical, not cosmetic.

---

## Purpose

The narrative domain defines the specific world the simulation takes place in. It is NOT a presentation skin over generic mechanics. It is a content package that determines: which entity subtypes exist, which cards are available, which low-altitude physics apply, what naming conventions are used, what ground truths constrain the simulation, and what the world feels like.

The game engine is domain-agnostic. The game itself is a domain. "The Ice Remembers" is a domain. "The Glass Frontier" could be another. Same engine, same core systems, different worlds with different content, different cards, and different mechanical rules.

---

## Design Decisions

### The domain is mechanical, not cosmetic

A narrative domain doesn't just change names and flavor text. It changes what EXISTS in the simulation:

- **Subtypes:** the domain defines which subtypes exist for each entity kind. In The Ice Remembers, hero subtypes might be: ice-warden, lore-keeper, wave-rider, tusk-hunter. In The Glass Frontier, they might be: sand-singer, dune-merchant, glass-forger, mirage-reader. Different subtypes mean different process scoring weights, different card targeting, different behavioral priorities.
- **Cards:** the domain defines the card pool. Each domain has its own generators, systems, and actions reflecting its world. The Ice Remembers has cards for orca raids, ice magic, the Long Dark. The Glass Frontier has cards for sandstorms, glass trade, oasis politics. Some cards may be shared across domains (basic settlement founding, basic diplomacy), but many are domain-specific.
- **Low-altitude physics:** some low-altitude behaviors are domain-specific. In The Ice Remembers, there might be an "ice stability" low-altitude system that doesn't exist in other domains. In The Glass Frontier, "sand migration" might affect settlement placement. These are not card-gated -- they are always-on physics specific to the domain.
- **Ground truths:** facts that constrain the simulation. "All food is soup" means the food resource (post-MVP) has specific production/consumption characteristics. "The Long Dark is coming" means there is a cyclical environmental pressure unique to this domain. "Orcas are a persistent external threat" means there is an always-on hostile entity behavior.
- **Naming and presentation:** culture-specific naming rules, entity descriptions, event text templates, map visual style, UI flavor.

### The engine has a domain-agnostic base layer

The core systems (entity graph, entity kinds, attributes, map, pressure, tick clock, relationships, lifecycle, deck, tableau, card executors) are domain-agnostic. They don't know about penguins or orcas or ice magic. They know about entities, attributes, relationships, and cards.

The domain plugs into the engine through defined extension points:

- **Entity kind subtypes:** the engine defines entity kinds (hard-coded). The domain defines subtypes within those kinds (data-driven).
- **Attribute definitions:** the engine defines the attribute system (scalars, semantic planes, reducers). The domain defines which specific attributes each kind+subtype has, their ranges, and their semantic plane configurations.
- **Relationship kind definitions:** the engine defines the relationship system (strength, polarity, culling, exclusivity). The domain defines which specific relationship kinds exist and their properties.
- **Card definitions:** the engine defines the card executor system (preconditions, mutations, DSL). The domain defines the specific cards (generators, systems, actions) using the DSL.
- **Process and decision definitions:** the engine defines the behavioral framework (utility AI, three-layer evaluation, data-driven DSL). The domain defines all processes and decisions as data in the DomainConfig.
- **Pressure definitions:** the engine defines the pressure system (contributing factors, homeostasis, smoothing). The domain defines the specific pressure types and their contributing factor configurations.
- **Strategy archetypes:** the engine defines the deck system (deck > hand > tableau, archetype selection). The domain defines which archetypes are available and what cards they contain.
- **Naming rules:** the engine defines name generation as an interface. The domain provides culture-specific naming data (from the canonry's name forge or equivalent).
- **Map generation parameters:** the engine defines procedural map generation. The domain provides terrain type definitions, biome configurations, and generation constraints specific to the world.

### Single package per domain

A narrative domain is a single data package. Switching the domain swaps the entire content layer. You don't mix and match domains -- you're either playing in The Ice Remembers or in The Glass Frontier. The package contains everything domain-specific in one place.

**Domain package contents:**
- Subtype definitions per entity kind
- Attribute definitions per kind+subtype (including semantic plane configurations)
- Relationship kind definitions
- Pressure definitions
- Card definitions (generators, systems, actions)
- Strategy archetype definitions (starting decks)
- Low-altitude behavior extensions (domain-specific impulses)
- Naming rules per culture
- Ground truth declarations
- Map generation parameters and terrain definitions
- Presentation assets (flavor text templates, event description templates, UI labels)
- Achievement/milestone definitions
- Meta-progression unlock trees

### The first domain is The Ice Remembers

The initial domain draws from the canonry's default project universe:
- Penguin civilizations on floating icebergs
- Orca as persistent external threat
- The Long Dark as cyclical environmental pressure
- Ice magic (though toned down from the canonry's more metaphysical constructs)
- Soup-based food culture
- Clan/faction dynamics from the canonry's existing content

Specific canonry constructs we're dropping:
- Heavily metaphysical elements (anomalies, dimensional rifts)
- The illuminator / generative AI narrative layer
- Entity-agnostic rulesets (replaced by hard-coded entity kinds)

Specific canonry constructs we're keeping:
- Cultural naming rules (name forge integration or equivalent)
- The penguin/orca conflict as a core domain-specific dynamic
- The Long Dark as a domain-specific cyclical pressure
- Clan structures and faction dynamics
- The Ice Remembers as narrative identity

---

## Domain-Agnostic vs Domain-Specific Boundary

This boundary must be maintained rigorously. Every system must be auditable: "does this code/data reference anything domain-specific?" If yes, it belongs in the domain package, not in the engine.

**Domain-agnostic (engine):**
- Entity graph structure and operations
- Attribute system (scalars, semantic planes, reducers, effect stacking)
- Relationship system (strength, polarity, culling, history)
- Map system (hex grid, regions, adjacency, pathfinding)
- Pressure computation framework
- Tick clock and phase ordering
- Entity behavior framework (three-layer model, utility AI, process/decision evaluation, goal tags)
- Card executor framework (precondition evaluation, mutation application, DSL interpreter)
- Deck / hand / tableau mechanics
- Mana system
- Save / load serialization
- Map visualization framework (overlay system, entity markers, relationship lines)

**Domain-specific (The Ice Remembers package):**
- "Ice-warden" as a hero subtype with specific process scoring weights
- "Orca raid" as a generator card with specific preconditions
- "The Long Dark" as a cyclical pressure with specific contributing factors
- "Iceberg stability" as a domain-specific low-altitude physics
- "Soup surplus" as a ground truth affecting resource descriptions
- "Clan of the Northern Current" as a culture with specific naming rules
- "Drift Navigator" archetype as a starting deck with specific cards

---

## Ground Truths

Ground truths are declarative facts about the domain that constrain the simulation. They are referenced by cards, naming templates, and potentially low-altitude systems.

**Categories:**
- **Physical:** "the world is an archipelago of icebergs," "the sky is perpetually overcast," "the ocean is hostile"
- **Cultural:** "all food is soup," "names follow clan-patronymic conventions," "oral tradition is valued over written"
- **Existential threats:** "orcas are an ever-present danger," "the Long Dark reduces all production cyclically"
- **Absent concepts:** "there are no horses," "there is no metalworking (bone and ice tools only)," "there is no organized religion (spiritual practices are cultural, not institutional)"

Ground truths that declare absence ("there is no X") constrain the subtype and card pools: no cavalry subtype, no metalworking cards, no religion entity kind (or no religion subtypes in this domain).

Ground truths are declared in the domain package data and queryable by systems that need to check "does this concept exist in this domain?"

---

## Interactions with Other Systems

| System | How Narrative Domain Interacts |
|--------|-------------------------------|
| Entity Kinds (#2) | Domain defines subtypes, valid statuses, and attribute schemas per kind. |
| Attributes (#3) | Domain defines specific attributes, semantic plane configurations, and reducer labels per kind+subtype. |
| Entity Behaviors (#6) | Domain adds domain-specific impulses and scoring curves per subtype. |
| Relationships (#7) | Domain defines relationship kind catalog (which relationship types exist). |
| Pressure (#11) | Domain defines pressure types and contributing factor configurations. |
| Deck (#12) | Domain defines strategy archetypes (starting deck compositions). |
| Card Executors (#16-18) | Domain defines all card content (generators, systems, actions) in the DSL. |
| Map/Spatial (#4) | Domain defines terrain types, biome configurations, and map generation parameters. |
| Lifecycle (#9) | Domain may define kind+subtype-specific aging functions and health metric ranges. |
| Map Visualization (#10) | Domain provides presentation assets: visual styles, color palettes, flavor text. |

---

## Open Questions (Deferred)

- **Domain package format.** Single large file vs directory of files. Whether the DSL used for cards also encodes ground truths and naming rules. Whether there's a domain schema that validates packages.
- **Cross-domain shared content.** How much card/subtype content is shared across domains vs domain-specific. Whether there's a "core" package of universal content (basic settlement founding, basic diplomacy) that all domains extend.
- **Domain creation tooling.** Whether domain packages are hand-authored, editor-assisted, or (eventually) player-created. The canonry had a visual editor (the Canonry app) -- do we need equivalent tooling?
- **The Ice Remembers specifics.** The detailed content of the first domain: specific subtypes, specific cards, specific pressures, specific ground truths. This is the largest content-creation task ahead and deserves its own design document.
- **Name Forge integration.** Whether to reuse the canonry's name forge system directly, adapt it, or build a simpler replacement. The canonry's name forge is sophisticated but may be more than we need.
- **Domain-specific low-altitude physics.** How domain-specific always-on systems are registered and run alongside the engine's core low-altitude systems. Whether they use the same utility AI framework or are separate code.
- **Meta-progression across domains.** Whether unlocks in one domain affect another. Whether the player's "god" identity persists across domains or resets.
