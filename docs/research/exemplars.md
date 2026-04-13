# Exemplar Analysis: Deep Research

Games and systems that inform the design of this simulation. Each entry provides detailed mechanical analysis of how the game's systems actually work under the hood, not just what they accomplish thematically.

---

## 1. RimWorld

### The Storyteller AI System

RimWorld's core innovation is that difficulty and pacing are not hardcoded but delegated to an AI Storyteller that selects events based on colony state. Three storytellers ship with the game, each implementing fundamentally different algorithms for the same event pool.

**Cassandra Classic** operates on a cooldown-based escalation curve. She aims to send 1-2 major threats per quadrum (roughly every 7-10 in-game days) with at least two days of cooldown between major events. Her pattern is tension-release-tension: push the colony with a dangerous event, allow breathing room for recovery, then escalate again. The key mechanic is that her threat intensity scales monotonically with time and colony wealth, so the difficulty curve is predictable and rising. She never sends events that are wildly disproportionate to what the colony can handle, nor does she let the player coast indefinitely.

**Randy Random** abandons curve-based pacing entirely. Randy applies a random multiplier between 0.5x and 1.5x to raid points, meaning raids can be half as challenging or 50% more intense than the calculated baseline. More importantly, Randy does not follow cooldown rules for event spacing. He can send three raids in a row, or nothing for an entire season. The result is that Randy creates a fundamentally different emotional arc: rather than steady escalation, the player experiences unpredictable spikes and lulls. This makes Randy stories feel chaotic and memorable, but can produce unwinnable scenarios or boring stretches.

**The mechanical difference** between storytellers is not the event pool (they share it) but the scheduling algorithm: when to fire events, how much time between them, and how to weight severity.

**The Threat Point / Raid Point System** is the engine that sizes all threats. Raid Points are a budget the storyteller spends to purchase raiders, where each raider has a Combat Power cost and every 1 raid point roughly equates to 1 combat power. The formula for calculating raid points is:

`RaidPoints = (WealthPoints + PawnPoints) * ThreatScale * AdaptationFactor * StorytellerMultiplier`

**Colony Wealth calculation** creates a feedback loop that is central to RimWorld's design. The game computes "Storyteller Wealth" which counts building wealth at only 50% (to avoid punishing defensive infrastructure too harshly). This Storyteller Wealth is then linearly interpolated on a graph: at 14,000 wealth or below, there are 0 Wealth Points. At 400,000 wealth, there are 2,400 Wealth Points. The curve continues linearly up to 1,000,000 wealth = 4,200 Wealth Points, at which point it caps. This means wealth accumulation directly increases raid difficulty, creating a natural pressure against hoarding and encouraging the player to spend wealth on defenses or consumption rather than stockpiling.

**Pawn Points** add 15 points per full colonist (0-10,000 wealth range), with modifiers: slaves at 75%, cryptosleep at 30%, babies at 0%, children scaling linearly from 0% at age 3 to 100% at age 18. This ensures that growing your population also increases threat pressure.

**The Adaptation Factor** is a hidden variable that tracks how well the player has been performing. If the colony has been consistently winning fights, adaptation increases, making future threats harder. If the colony has suffered losses, adaptation decreases, providing relief. This is shared across multiple settlements to prevent cheese strategies.

### The Needs/Mood System and Cascade Mechanics

Every colonist has a Mood value (0-100%) driven by a stack of positive and negative Thoughts. Mood is not instantaneous; there is a Mood Target (the sum of all active thoughts) and the actual Mood bar that drifts toward it over time. Three mental break thresholds divide the mood bar: 35% for minor breaks, 20% for major breaks, and 5% for extreme breaks. When mood drops below a threshold, there is a probability per tick that the colonist will snap.

Mental breaks include: sad wandering (unproductive but harmless), food binges (consumes resources), insulting sprees (damages relationships, potentially cascading), berserk (attacks other colonists, causing injuries). The cascade potential is enormous: one colonist's berserk attack injures another, whose medical treatment pulls a doctor off other duties, whose untended patient develops an infection, whose death triggers "colonist died" debuffs on the entire colony, pushing more colonists toward break thresholds.

**The Expectations system** is RimWorld's circuit breaker against unrecoverable cascades. After a disaster (multiple deaths, major raid), colonist expectations dynamically adjust downward. This means that the mood penalty from "ate without table" or "slept on ground" matters less during a crisis period, preventing trivial irritants from compounding with genuine trauma into a death spiral. This is a deliberate design choice by Tynan Sylvester: the death spiral should be dramatic and drawn out, not instant. As Sylvester has stated: "The goal is to not just cut off the story, but let the spiral down into death play out over a long dramatic arc, or even be reversed."

**The Man in Black** is an ultimate failsafe: if all colonists are incapacitated simultaneously, a wanderer arrives to provide a last chance at recovery. This ensures that total-party-kill scenarios are rare, preserving player investment.

### Modding Architecture: Defs and XML

RimWorld's modding system is architecturally elegant and directly relevant to data-driven simulation design. The game separates data from logic using XML "Defs" (Definitions). A Def is a data template, not an instance. A `ThingDef` defines what a "thing" is (its stats, textures, behaviors); the game engine then instantiates actual Things from ThingDefs at runtime.

Key Def types include: `ThingDef` (items, buildings, pawns, plants, weapons, apparel, nearly everything visible), `PawnKindDef` (specific pawn configurations: race, faction, backstory category, gear loadout), `HediffDef` (health differences: injuries, diseases, prosthetics, drug effects, temporary buffs), `ResearchProjectDef`, `TraitDef`, `ThoughtDef`, `BiomeDef`, and dozens more.

Defs support inheritance via Abstract parents. An abstract Def is never instantiated but serves as a template that concrete Defs inherit from, avoiding copy-paste duplication across hundreds of entries. A modder can define `<ThingDef Abstract="True" Name="BaseMeleeWeapon">` with shared properties, then create specific weapons that inherit from it using `ParentName="BaseMeleeWeapon"`.

For logic modification, the community uses **Harmony**, a runtime method-patching library for .NET/Mono. Harmony provides three patch types: Prefixes (run before the original method, can skip it), Postfixes (run after, can modify return values), and Transpilers (rewrite IL bytecode of the original method). This layered architecture means mods can: add pure data via XML, extend data classes via C# Def subclasses, and modify game behavior via Harmony patches, all without touching the game's source code.

### Tick System

RimWorld runs on a fixed-timestep tick system decoupled from rendering framerate. At 1x speed, the game targets 60 ticks per second. At 2x, 180 tps. At 3x, 360 tps. At dev-mode 4x, 900 tps. One in-game hour always equals 2,500 ticks regardless of speed setting. Multiple ticks can execute within a single visual frame, which is how time acceleration works: the rendering loop runs at monitor refresh rate while the simulation loop runs N ticks per frame based on speed setting.

When individual tick computation takes too long (large colonies, many pawns), the game automatically throttles TPS to maintain stable FPS, which is why late-game colonies feel slower. The tick rate doubles when all player-controlled pawns are sleeping, as an optimization. Not all systems run every tick: pawns check needs on a staggered schedule, and modders can use HugsLib's custom tick scheduling to register callbacks at intervals (every 250 ticks, every 2000 ticks, etc.), which is essential for performance with complex mods.

---

## 2. Crusader Kings 3

### Character Trait System and Event Interaction

CK3's fundamental design thesis is that civilization-level events emerge from character-level simulation. Every character has a set of traits that fall into categories: Personality traits (e.g., Ambitious, Craven, Honest, Deceitful), Congenital traits (inherited genetically: Beautiful, Genius, Albino, Dwarf), Lifestyle traits (earned through perk trees: Strategist, Scholar, Seducer), Health traits (Wounded, Ill, Pregnant), and Fame/Infamy traits.

Personality traits are the key drivers of emergent behavior because they interact with the Stress system. Every character action is evaluated against their personality traits. A Just ruler who imprisons someone without cause gains Stress. A Sadistic character who passes up an opportunity for cruelty gains Stress. When Stress accumulates past thresholds (at levels 1, 2, and 3), the character must choose a coping mechanism, which adds a new trait: turning to drink (Drunkard), becoming reclusive, flagellating, or visiting brothels. These coping traits further modify behavior and create cascading personality changes.

Traits also drive the event system through weighted random selection. CK3 uses "on_actions" as hooks: when specific game events occur (character born, war declared, season change) or on regular pulses (every 5 years), the engine evaluates a weighted list of potential events. Weights are modified by the character's traits, situation, and relationships. A Paranoid ruler is more likely to trigger "suspect assassination plot" events. A Lustful character sees more romance events. The weight system means trait combinations create unique narrative trajectories without hard-scripted paths.

### Dynasty and Succession: Personal Becomes Political

CK3's succession system is where personal simulation creates political simulation most directly. When a ruler dies, their realm is divided among heirs according to succession law. Under Partition (the most common early law), all eligible children receive titles. This mechanically means that a realm the player spent a lifetime building gets split among children, creating immediate brother-vs-brother wars, border conflicts, and vassals switching allegiance.

The player can mitigate this through personal actions that are also political: disinheriting children (costs Prestige and creates enemies), assassinating inconvenient heirs (risks exposure and tyranny), changing succession laws (requires specific innovations and vassal approval), or strategically marrying to produce fewer children. Each of these personal decisions has political ramifications: disinheriting a popular heir makes that heir's supporters your enemies; changing succession laws may trigger a civil war if powerful vassals object.

**Dynasty Legacies** are permanent bonuses purchased with Renown (a currency that accrues based on how many independent rulers share your dynasty). This creates a meta-incentive: you want your dynasty's cadet branches to hold independent thrones, even if that means your own realm faces external competitors from your own bloodline. The personal and the political are inseparable.

### Culture and Religion Innovation

Each culture in CK3 has 5 Cultural Pillars: Heritage (origin group, prevents hybrid cultures between shared heritages), Language (affects naming, title terminology), Aesthetics (visual appearance, architecture), Martial Custom (who can be knights/commanders), and Ethos (core values, one of 7 options like Bellicose, Bureaucratic, Communal, Courtly, Diplomatic, Spiritual, or Stoic).

Cultures also carry Traditions (up to 5 in tribal era, +1 per era advancement), which grant specific mechanical bonuses. Adding a tradition costs 2,000 Prestige as a base, with +2,000 if the culture lacks the tradition's preferred ethos and +3,000 if optional requirements are unmet. This creates path dependency: a culture's ethos makes certain traditions cheap and others expensive, so cultures naturally specialize.

**Innovation** represents technological and ideological advancement. Innovations are grouped into 4 eras, with 4 categories each (Military, Civic, Cultural, Regional). A culture progresses to the next era once 8 innovations from the current era are unlocked and a minimum year is reached. Progress speed depends on average Development across the culture's counties. Critically, cultures can gain exposure to innovations known by adjacent or religiously-connected cultures, creating a diffusion mechanic where technological advancement spreads geographically and along cultural/religious networks.

**Culture Spread** on the map operates through county-level conversion. A ruler can order their Steward to "Promote Culture" in a county, which gradually converts the county's culture to the ruler's. The rate depends on Cultural Acceptance (base threshold of 40%), which is influenced by shared traditions, diplomatic relations, and various modifiers. Hybridization allows two cultures with sufficient acceptance to merge, inheriting pillars and traditions from both parents and gaining all innovations known by either. Divergence allows a culture to split off, changing at least one pillar.

### Map Interaction: De Jure vs De Facto

CK3 maintains two parallel territorial maps. The **de jure** map shows legal claims: which counties belong to which duchies, which duchies to which kingdoms, which kingdoms to which empires. The **de facto** map shows actual control. These two maps constantly diverge as rulers conquer, inherit, and lose territory. A county can be de jure part of the Kingdom of France but de facto held by the King of England.

This divergence creates permanent strategic pressure: rulers with de jure claims on held territory get a free casus belli (reason for war). If a de facto holder controls enough of a de jure duchy for long enough, the de jure borders can drift to match reality. This mechanic models the historical phenomenon of political legitimacy following military reality.

### The Event/Decision System

CK3's scripting system uses a custom language (Paradox Script / PDXScript) where events have: trigger blocks (conditions that must be true for the event to fire), weight blocks (probability modifiers based on character state), option blocks (player choices), and effect blocks (consequences). Decisions are player-initiated actions with `is_shown` (visibility conditions), `is_valid` (availability conditions), and `effect` (outcomes) blocks.

The on_action system fires events either on specific game occurrences (character birth, war declaration, season change) or on regular pulses (e.g., every 5 years). Events can be placed in weighted random lists where trait-based modifiers determine which events fire, creating trait-dependent narrative paths without hard scripting.

---

## 3. Victoria 3

### Pop System in Detail

Victoria 3's fundamental simulation unit is the "pop" (population group). A pop is defined by the unique combination of: profession (e.g., farmer, laborer, machinist, engineer, aristocrat), culture, religion, and workplace, within each state. The game tracks over 700 geographic regions containing up to 100,000 individual pop groups.

**Stratification** works through social strata tied to profession. Lower strata (laborers, peasants) have basic needs and low political power. Middle strata (machinists, shopkeepers, clerks) have more complex consumption needs and moderate political power. Upper strata (aristocrats, capitalists, officers) have expensive tastes but disproportionate political influence. A pop's political strength is based primarily on wealth level, modified by discrimination status, whether they live in an incorporated vs unincorporated state, and laws regarding enfranchisement.

**Pop Needs** drive the entire economic simulation. Each pop has consumption needs determined by their social stratum and wealth level. Basic needs include food, clothing, and housing. As wealth increases, pops demand luxury goods, services, and cultural products. When needs go unmet, pops become Radicals. When needs are exceeded, they become Loyalists. This creates a direct link: economic performance drives political stability.

**Pop stances** are loyalist, neutral, or radical. The ratio of loyalists to radicals in each pop determines its political behavior. Radicals support political movements, reduce stability, and can ultimately trigger revolutions.

### Economic Simulation

Victoria 3's economy was designed as an explicitly open market system, abandoning the closed-loop model of its predecessor. Lead designer Mikael Andersson has described the philosophy: the game is in many ways a "Historical Materialism Simulator" where political affiliation emerges from economic status.

**Goods and Production**: Buildings are the basic production unit, but they don't generate output alone. They are vessels where workforce and production methods combine to generate output. Resource Industries (agriculture, mining) generate goods from labor alone. Manufacturing Industries convert input goods plus labor into finished products. Production methods are configurable, determining required inputs, outputs, and employee role compositions. For example, a textile mill might use either hand-loom methods (more laborers, fewer engineers) or power-loom methods (fewer laborers, more engineers, requires access to iron and coal as inputs).

**Market Dynamics**: Prices emerge from genuine supply-demand interactions rather than being predetermined. When supplies are scarce, prices rise; abundance drives prices down. Consumer access depends on purchasing capacity, not administrative rank. This differs fundamentally from Victoria 2, where national prestige determined market access priority.

**Trade Routes**: Countries establish trade routes to move goods between markets. Overland trade is relatively free, but oceanic trade requires convoys produced by ports. Market prices determine how many units of goods flow along routes. Trade policies carry real consequences: tariffs and restrictions create domestic winners (protected industries) and losers (consumers paying higher prices).

### Interest Group System

Interest Groups represent organized political factions within society. Individual pops support various interest groups based primarily on profession (laborers lean toward Trade Unions, farmers toward Rural Folk, aristocrats toward Landowners), but also on broader ideological alignment.

**Clout** measures an interest group's political power. Above 20% clout, a group is Powerful (traits twice as effective). Between 5-19%, they are Influential. Below 5%, they are Marginalized (traits inactive). Clout is derived from the total political strength of supporting pops, which itself is driven by wealth. This creates a feedback loop: economic policies that enrich certain classes increase the clout of their associated interest groups, which in turn push for policies that further enrich them.

**Approval** measures how happy an interest group is with current laws and government composition. Interest group ideologies determine which laws they support or oppose on a 5-point scale. Groups that are in government and see their preferred laws enacted have high approval. Groups excluded from government or whose ideology clashes with current laws have low approval. Angry groups (low approval) provide 50% more resistance to law changes, while neutral-or-happy groups provide only half as much resistance.

**Legitimacy** derives from the clout of interest groups that participate in government. If your government excludes high-clout groups, legitimacy drops, increasing radicalism nationwide.

### Political Movements

Political Movements form when sufficient pops and interest groups want a specific law changed. A movement has a singular goal and exists only until that goal is met or abandoned. Movements draw support from two sources: interest groups (representing establishment backing) and individual pops (representing grassroots activism).

**Radicalism** of a movement comes from two sources: the proportion of Radicals among supporting pops, and the clout of supporting interest groups that are angry enough to be classified as "Angry." While demands remain unmet, supporting pops gradually gain more Radicals, creating escalating pressure.

When radicalism exceeds thresholds, movements can trigger armed uprisings. Revolutionary countries form from states proportional to movement strength, localized where supporters are concentrated. The revolutionary country has the same technology as the parent but different laws reflecting the movement's demands.

### Diplomatic Plays

Victoria 3 replaces direct war declarations with structured diplomatic plays that model 19th-century brinksmanship. A play passes through three phases:

1. **Opening Moves**: The initiator makes formal demands. The world is notified. Interested parties assess the situation.
2. **Diplomatic Maneuvering**: Both sides court allies, make concessions, add or modify war goals. Mobilizing generals and sending them to fronts escalates the countdown. This phase is a game of chicken: military mobilization demonstrates resolve but accelerates the timeline toward war.
3. **Countdown to War**: All parties are locked in. Only backing down (accepting enemy demands) or capitulation prevents conflict.

Primary Demands (the core war goals) are enforced if the opponent backs down. Secondary Demands only come into play if war actually erupts and is won. This creates strategic depth: overloading demands risks making backing down unacceptable for the opponent, guaranteeing war.

### What Makes Victoria's Simulation "Emerge"

Victoria 3 achieves emergence through layered feedback loops. Economic production determines pop wealth, which determines consumption demand, which drives market prices, which affects production profitability, which determines which buildings get built, which determines available jobs, which determines pop profession distribution, which determines interest group clout, which determines what laws can be changed, which determines economic policies, which affects production. This is a genuine circular dependency where intervention at any point ripples through the entire system. No single outcome is scripted; they all emerge from the interaction of these subsystems.

---

## 4. Dwarf Fortress (Legends Mode / World Generation)

### How World Generation Works

Dwarf Fortress world generation proceeds in two major phases. The first phase generates physical geography: memory is allocated, base field values (elevation, rainfall, temperature, drainage, volcanism, wildness) are seeded along a variable-size grid, then filled in fractally. The process includes pole selection affecting temperature distribution, peak placement and altitude adjustment, vegetation setting based on elevation/rainfall/temperature, biome validation checks, erosion and river carving, lake generation, elevation smoothing, rain shadow and temperature recalculation, geological layer creation, and wildlife population initialization. Worlds that fail validation criteria (insufficient mountains, bad biome distribution) are rejected and regenerated.

The second phase is history generation, which creator Tarn Adams ("Toady One") describes as "a giant zero-player strategy game going on with somewhat loose turn rules and bad AI (but thousands of agents), and history is just a record of that." This is a crucial design philosophy: the game does not script historical narratives. It simulates thousands of agents making decisions simultaneously, and the resulting record is the world's history.

### Entity Creation

The system places civilizations (entities) of multiple species: dwarves, humans, goblins, kobolds, and elves. The number scales with world size, with configurable maximums up to 300 civilizations. Each civilization is characterized by: recognizable symbols and art forms, language, ethics and values, site types (towns, fortresses, hamlets), resource usage patterns, governing systems, and complex diplomatic relationships including trade networks, knowledge sharing, and conflicts.

Additionally, megabeasts (dragons, giants, hydras), semi-megabeasts, and titans are placed as independent agents that interact with civilizations -- attacking settlements, being hunted by heroes, and accumulating their own histories.

### Historical Event Generation and Linking

The history simulator runs for a player-specified number of years (typically 250-1,000, up to 2,000). During each simulated year, the system resolves: population growth and resource competition, migration (populations moving between sites), founding of new settlements, wars between rival civilizations (battles resolved abstractly, with outcomes affecting borders and refugees), technological progression, artifact creation (legendary items produced by skilled individuals, recorded with full provenance), rise and fall of leaders, diplomatic shifts, and natural events (plagues, natural disasters).

All events are linked to specific historical figures, sites, and civilizations. A war is not just "Civilization A fought Civilization B"; it records which leaders initiated it, which heroes fought in which battles, which sites were besieged or destroyed, which artifacts were lost or captured, and what the territorial consequences were. This creates a dense graph of interconnected historical events.

### Civilization Simulation During World Gen

Civilizations compete for territory and resources in an abstract strategy layer. Wealthier and more populated civilizations expand, founding new sites and attacking weaker neighbors. Wars are resolved abstractly but track individual combatant fates. Migration occurs when populations are displaced by war or resource pressure. Artifact creation is tied to individual craftspeople entering "strange moods" -- states of creative obsession that produce unique named items of extraordinary quality. These artifacts have full provenance: who made them, where, with what materials, what images they bear, and how they changed hands over the centuries (through trade, theft, or conquest).

### Individuals and Civilizations

The relationship between individuals and civilizations is bidirectional. Civilizations are governed by individuals (kings, mayors, generals) whose personal traits, ambitions, and relationships influence civilizational decisions. A warmongering king makes their civilization more aggressive. An artistically inclined leader might focus on cultural development. When leaders die, succession can cause instability.

Conversely, civilizational membership shapes individuals. A dwarf born in a fortress with a rich metalworking tradition is more likely to become a legendary smith. A human from a trading city might become a renowned merchant. Historical figures accumulate accomplishments, relationships, grudges, and trauma that are all recorded in Legends Mode.

History length directly impacts complexity: 250-year worlds are simple (fewer abandoned sites, less complex alliances), while 1,000-year worlds have dense histories with extinct civilizations, multi-generational grudges, and artifacts that have passed through dozens of hands. Very long histories (2,000 years) risk generation instability but create extraordinarily rich settings.

---

## 5. Plague Inc / Pandemic

### Plague Inc's Spreading Mechanics

Plague Inc models disease spread across a real-world-inspired map where every country has distinct attributes: climate (temperature and humidity), wealth level (correlating to healthcare infrastructure), population density, urbanization level, and connectivity (number of airports and seaports, and where they connect).

**Transmission vectors** determine how the disease spreads between and within countries. The player evolves transmission types: Air transmission (effective in dense, enclosed environments), Water transmission (effective in countries with poor sanitation), Blood transmission via insects (effective in hot, humid climates), and direct contact. Each transmission type has differential effectiveness based on the target country's attributes. Evolving Air 2 makes the disease spread fast in dense urban populations but does little for isolated rural nations.

**Within-country spread** is a function of population density, climate match to evolved transmission types, and healthcare quality. Dense countries with matching climate profiles can be fully infected within days. Sparse, wealthy countries with mismatched climates may take months to penetrate.

**Between-country spread** occurs via airports and seaports. Saudi Arabia's airport is the game's busiest hub, connecting to every other airport. Countries with both airports and seaports (India, China, Egypt) are ideal starting points because they provide multiple outbound vectors. Island nations with single ports (Greenland, Madagascar, Iceland) are notoriously difficult to infect because they can close their single entry point before infection arrives.

### The Reactive AI System

Plague Inc's "opponent" is not a single AI but a distributed response system modeling global public health infrastructure. The world's response has several components:

**Awareness**: As the disease becomes visible (symptoms evolve, deaths accumulate), global awareness increases. Countries individually track awareness, which drives local responses. A key strategic tension exists: symptoms increase disease severity and lethality but also increase visibility, triggering faster responses.

**Country-level responses**: Individual governments close airports, seaports, and land borders based on their awareness level, wealth, and current infection status. Wealthy countries respond faster and more effectively. Countries already heavily infected may prioritize internal containment over border closure.

**Cure research**: Once global awareness crosses a threshold, nations begin collaborative cure research. The cure progress bar advances based on total research funding, which correlates with the GDP of aware, uninfected nations. If the player kills off rich nations early, cure research slows -- but killing rich nations requires visible symptoms that accelerate awareness.

**The core tension** is stealth vs. lethality. The optimal strategy involves spreading the disease silently (few or no symptoms) until every country is infected, then rapidly evolving lethal symptoms to kill faster than the cure can be developed. This creates a natural two-act narrative structure without any scripting.

### Pandemic's Outbreak Chain and Infection Deck Reshuffle

The board game Pandemic uses two interlocking deck mechanics to create escalating tension:

**The Infection Deck** is a stack of city cards. Each turn, cards are drawn and cubes placed on those cities. Once drawn, cards go to the discard pile. This creates a known-but-uncertain threat profile: you know which cities are in the deck, but not when they will come up.

**The Epidemic mechanic** is where the design genius lies. When an Epidemic card is drawn from the player deck: (1) the infection rate marker advances, (2) a card is drawn from the bottom of the infection deck and 3 cubes placed on that city (introducing a new hot zone), (3) the entire infection discard pile is shuffled and placed back on top of the infection deck. This means cities that have already been drawn will come up again soon. Cities that were recently infected become hot spots guaranteed to be re-infected in the near future.

**Outbreaks** occur when a city already has 3 disease cubes and would receive a 4th. Instead of placing a cube, an outbreak triggers: each adjacent city receives one cube of that disease color. If any adjacent city also has 3 cubes, a chain reaction triggers additional outbreaks (though each city can only outbreak once per turn, preventing infinite loops). If the outbreak marker reaches 8, the game is lost.

### What Makes Cascading Failure Work

Cascading failure works as a game mechanic because it creates emergent drama from simple rules. The infection deck reshuffle guarantees that known-danger cities will be re-threatened. The 3-cube outbreak threshold creates predictable-but-hard-to-prevent tipping points. The chain reaction from outbreaks creates geographic spread that follows the map topology (cities connected by lines). The outbreak counter creates global stakes from local failures.

The result is that players must triage: they cannot prevent all infections, so they must choose which cities to protect and which to let burn, knowing that unattended hot spots will cascade into regional crises. This triage decision under resource constraints (limited actions per turn) is the core of the game's drama.

---

## 6. Civilization Series (Brief)

The Civilization series layers simulation systems beneath its 4X (explore, expand, exploit, exterminate) framework. The simulation layer includes: per-tile yield calculations (food, production, gold, science, culture, faith) modified by terrain, improvements, and buildings; population growth driven by food surplus with diminishing returns; amenity/happiness systems that penalize rapid expansion; and tech/civic trees that gate unlockable capabilities.

**Cultural influence** in Civ 5 (Brave New World) operates as a dual system. Culture is the defensive stat, accumulating to protect your civilization's identity. Tourism is the offensive stat, spreading your civilization's influence to others. When your cumulative Tourism output toward a rival exceeds their total Culture, you achieve "Influential" status. The system creates pressure/counter-pressure dynamics: a culturally strong civilization can resist influence from tourism-heavy rivals. Civ 6 expanded this with a separate Civics tree (previously part of the Tech tree) and district-based cultural production, making cultural output more spatially grounded in city planning. Great Works (art, writing, music) placed in specific building slots generate Tourism, creating a museum-curation minigame within the larger strategy layer.

---

## 7. Europa Universalis 4 / Hearts of Iron 4 (Brief)

Paradox games structure their simulation around a fixed daily tick (one tick = one day in EU4 and CK3, one hour in HOI4, six hours in Victoria 3). Within this framework, different systems update at different frequencies: yearly ticks handle tax/income recalculation, monthly ticks handle manpower recovery and attrition, and daily ticks handle unit movement and combat resolution. Victoria 3 further subdivides into yearly, monthly, weekly, daily, and "regular" tick categories, with the modifier system using a dependency graph (modifier nodes) that flags values as dirty and only recalculates them and their dependents when changed.

The **event/decision/modifier system** across Paradox titles uses a consistent architecture in PDXScript: events have triggers (boolean conditions), weights (probability modifiers), options (player choices), and effects (state changes). Decisions use `allowed` (checked once at startup for performance), `visible` (checked periodically), and `available` (checked when the player tries to take the action). Modifiers are numerical adjustments to game values (tax income +10%, army morale +5%) that can be applied by events, decisions, buildings, advisors, or national ideas, with a standardized stacking system. The `allowed` optimization is notable: by evaluating expensive conditions only at game start rather than every tick, Paradox avoids massive performance overhead from thousands of decisions being continuously evaluated.

---

## 8. Stellaris (Brief)

Stellaris handles procedural civilizations by combining authored components in random configurations. Each AI empire is generated from: species (randomized traits like Industrious, Intelligent, Repugnant), ethics (chosen from 8 options along 4 mutually exclusive axes, with moderate and fanatical variants), civics (2 at start, +1 from tech, constrained by authority and ethics), and authority type (Democracy, Oligarchy, Dictatorship, Imperial). The combinatorial explosion of these parameters produces diverse AI civilizations without hand-crafting each one. Emergent stories arise from scripted event chains triggered by procedural conditions (discovering a precursor civilization's ruins, encountering a space anomaly) combined with AI empire interactions driven by their ethics and traits.

Stellaris's pop system differs from Victoria 3's in granularity and focus. Stellaris pops represent individual population units assigned to jobs on planets, with species traits affecting productivity (a Rapid Breeder species grows faster, an Intelligent species produces more research). Victoria 3's pops are demographic groups defined by profession/culture/religion combinations within states, focused on economic stratification and political behavior. Stellaris pops are more about resource optimization; Victoria 3 pops are about social dynamics. Stellaris also features end-game crises (extragalactic invasions, AI uprisings, extradimensional incursions) that function as shared existential threats forcing diplomatic realignment, and the player can become a crisis faction themselves through dangerous technologies.

---

## 9. Old World (Brief)

Old World, designed by Soren Johnson (lead designer of Civilization IV), blends character-driven simulation with 4X mechanics by making rulers mortal and dynastically linked. Each turn represents one year. As the empire advances, its leader ages, gains experience, develops new personality traits, falls ill, and eventually dies. The player must produce an heir who inherits the throne with their own ambitions and traits. Characters (generals, governors, spies) are recruited and can tutor royal children, lead armies, or pursue personal agendas that may conflict with the player's plans.

The **Orders system** is Old World's signature constraint mechanism. Instead of each unit acting once per turn, all actions (moving units, building improvements, issuing city construction commands) draw from a shared pool of Orders per turn. A single unit can be moved multiple times until exhausted, or Orders can be spread across many units. Order generation scales with Legitimacy (gained through accomplishing Ambitions, finishing Wonders, and earning renown). This means political success (legitimacy) directly translates to military and economic capability (more orders), creating a unified resource that bridges the character and civilization layers. The constraint forces strategic triage: you cannot do everything each turn, so prioritization becomes the core skill.

---

## 10. Songs of Syx (Brief)

Songs of Syx demonstrates that individual-level simulation can scale to tens of thousands of agents. Written in Java by a solo developer, the game simulates each citizen with unique stats, personality, likes, dislikes, strengths, weaknesses, and racial baselines. Each person has needs for specific foods, housing quality, furniture, and roads. Employed individuals continuously collect, use, and produce resources, or stand idle when resources are unavailable.

The key UI challenge Songs of Syx faces is transitioning from micro to macro oversight. At small populations, the player manages individuals. As population swells into the thousands, the focus necessarily shifts to group management: combinations of race, social standing, religion, and trade become the unit of management rather than individuals. The game provides fulfillment and expectation metrics at individual, species, and settlement-wide levels. However, community feedback indicates ongoing UI challenges: the game has struggled with clear categorization, and updates have focused on adding tabbed interfaces for Industry, Food Production, Civics, and World systems. The lesson for any simulation designer: the simulation can scale, but the UI must provide appropriate abstraction layers or the player drowns in data.

---

## Synthesis: Cross-Game Design Patterns

### Common Simulation Patterns

Several structural patterns recur across all successful simulation games studied:

**Layered feedback loops**: Every game creates chains of cause-and-effect that cycle back on themselves. RimWorld: wealth drives raid difficulty, raids destroy wealth. Victoria 3: economic production drives pop wealth, pop wealth drives political clout, political clout drives law changes, laws drive economic production. CK3: personal traits drive political decisions, political outcomes create stress, stress generates new traits. These loops are the engine of emergence: when multiple loops intersect, behavior becomes unpredictable without being random.

**Threshold-triggered state changes**: Rather than smooth gradients, successful simulations use discrete thresholds that trigger qualitatively different behaviors. RimWorld mental breaks fire at 35%/20%/5% mood. Victoria 3 interest groups change category at 20%/5% clout. Pandemic outbreaks trigger at exactly 3 cubes. CK3 stress breaks at levels 1/2/3. These thresholds create drama because approaching them generates tension (the player sees crisis coming) and crossing them creates irreversible consequences.

**Dual-track legitimacy**: Multiple games maintain parallel systems of legal/formal state versus actual/practical state. CK3's de jure vs de facto territory. Victoria 3's legal government vs actual power distribution (legitimacy from clout). The Plague Inc tension between a disease's "legal" status (unknown to authorities) versus its actual spread. This duality creates strategic depth because players must manage both the nominal and real situation.

**Wealth/power as a double-edged sword**: Accumulation of strength consistently triggers countervailing pressures. RimWorld's wealth-to-raid-point pipeline punishes stockpiling. Victoria 3's wealthy pops gain disproportionate political power, potentially undermining the player's preferred policies. CK3's powerful vassals are harder to control. This pattern prevents runaway success and creates natural tension in every growth decision.

### The Micro-to-Macro Problem

The central design challenge for civilization simulation is bridging individual-level simulation with civilization-level behavior. The games studied use three distinct approaches:

**Aggregation with representative agents** (Victoria 3, Stellaris): Instead of simulating every individual, group people into demographic clusters (pops) that share profession, culture, religion, and location. Each pop behaves according to aggregate rules. The macro behavior (political movements, economic trends) emerges from the interaction of these groups. This scales well computationally and produces convincing civilization-level dynamics, but loses individual stories.

**Full individual simulation with narrative surfacing** (RimWorld, Dwarf Fortress, Songs of Syx): Simulate every individual with unique attributes, then use narrative systems to surface interesting individual stories to the player. The macro state (colony mood, fortress stability) is a genuine aggregate of micro states. This produces rich emergent stories but faces computational limits and UI challenges at scale.

**Individual simulation of elites, abstraction of masses** (CK3, Old World): Simulate rulers, generals, and other important characters individually, while abstracting common populations into statistical aggregates (development levels, levies, tax income). Political and military events emerge from elite character interactions, while economic and demographic trends come from aggregate systems. This compromise allows character-driven stories at civilization scale.

For a high-altitude civilization simulation, the CK3/Old World approach may be most applicable: simulate heroes and leaders as full characters whose traits and decisions drive national behavior, while modeling population and economics through aggregate demographic systems like Victoria 3's pops.

### UI/UX Patterns for Presenting Simulation State on Maps

Successful simulation games converge on several map presentation patterns:

**Color-gradient overlays** (Plague Inc, RimWorld, Victoria 3): The map surface is painted with color gradients representing continuous variables (infection level, beauty, cultural influence, economic prosperity). This communicates spatial distribution instantly without requiring the player to inspect individual locations. Multiple overlays are toggled between, not stacked.

**Drill-down on demand** (RimWorld, CK3, Victoria 3): The default map view shows the macro state. Clicking a location reveals micro-level detail (colony, character sheet, state economy). Alert banners surface critical state changes that require attention. The player sees the forest until a tree catches fire.

**Mapmodes** (all Paradox titles): Dedicated map rendering modes that recolor the entire map surface to show different data layers: political ownership, culture, religion, trade goods, terrain type. The player cycles between these views to understand different aspects of the same geography.

**News ticker / event log** (Plague Inc, CK3, Victoria 3, Dwarf Fortress Legends): A running text feed that translates simulation state changes into human-readable narrative. "France has closed its borders" (Plague Inc), "Duke Robert has declared war on his brother" (CK3), "The Silk Weavers' Guild demands free trade" (Victoria 3). This textual layer makes abstract numerical changes feel like a living world.

### Event/Narrative Systems

Games make simulation output readable and interesting through several techniques:

**Weighted random event selection** (CK3, RimWorld, Stellaris): Events are not triggered deterministically but selected from weighted pools where the weights depend on current game state. This means the same simulation state can produce different narrative outcomes across playthroughs, while still feeling contextually appropriate. A paranoid ruler gets paranoia-themed events more often, but not exclusively.

**The storyteller/director pattern** (RimWorld, Left 4 Dead): A meta-system monitors overall pacing and adjusts event timing and severity to maintain dramatic tension. This addresses the fundamental problem that pure simulation produces uneven pacing: sometimes nothing happens for long stretches, other times everything happens at once. The director smooths the experience without scripting outcomes.

**Event chains** (Stellaris, CK3): Multi-step narrative sequences where each event's outcome determines which subsequent event fires. This creates mini-stories within the simulation: discovering ruins leads to archaeological investigation, which reveals an ancient threat, which triggers a crisis. Each step involves player choice, and the chain can branch based on those choices.

**Historical record / legends** (Dwarf Fortress, CK3): The simulation maintains a permanent record of all events, creating a browsable history. This transforms ephemeral simulation ticks into persistent narrative. Players can review why nations fell, how artifacts changed hands, and which heroes shaped history. The record itself becomes content.

### Time/Tick Systems

Simulation pacing is handled through several patterns:

**Fixed-timestep with variable display speed** (RimWorld, all Paradox titles): The simulation runs on a fixed tick (1 day, 1 hour, 6 hours) regardless of visual speed. Time acceleration works by running more ticks per rendered frame. This ensures simulation determinism while allowing the player to control pacing. RimWorld runs 60 tps at 1x, 360 tps at 3x. Paradox games typically offer 5 speed levels.

**Staggered update frequencies** (Victoria 3, RimWorld): Not all systems need to update every tick. Economic recalculation might happen monthly, population growth yearly, combat resolution daily. This allows computationally expensive systems to run at lower frequencies without losing important temporal resolution for fast-moving systems. Victoria 3's modifier node system adds dependency tracking so that only dirty values are recalculated.

**World-gen compression** (Dwarf Fortress): During world generation, hundreds of years of history are simulated in minutes by running a much more abstract simulation than gameplay mode. Wars are resolved in single calculations rather than turn-by-turn combat. This allows deep historical context without requiring real-time simulation of centuries.

**Turn-as-abstraction** (Civilization, Old World): Each turn represents a variable time period (years in early game, months in late game for Civ; always one year in Old World). This gives the designer control over pacing and allows systems operating at different time scales to coexist within a single turn resolution.

### Biggest Design Pitfalls

**The death spiral without recovery**: When cascading failure mechanisms have no circuit breakers, a single bad event can make the game unwinnable. RimWorld addresses this with the Expectations system and Man in Black. Songs of Syx has been criticized for not providing enough recovery mechanisms after drought-triggered cascades. The design lesson: failure cascades create drama, but the player must have a plausible path to recovery, or the cascade becomes frustrating rather than dramatic.

**Simulation opacity**: When the player cannot understand why things are happening, the simulation feels arbitrary rather than emergent. Victoria 3's economic system has been criticized for being hard to diagnose: players can see that their economy is failing but struggle to identify which specific goods shortage or policy decision caused the problem. Every simulation output should be traceable to identifiable causes, either through tooltips, logs, or diagnostic overlays.

**Performance cliffs**: Individual-level simulation hits hard computational limits. RimWorld becomes sluggish above ~30 colonists. Dwarf Fortress infamously slows with large fortresses. Songs of Syx pushes the boundary but still faces Java GC pauses. The design choice between individual simulation (rich emergent stories, poor scalability) and aggregate simulation (good scalability, less emergent individuality) is one of the most consequential architectural decisions in simulation game design.

**The UI scaling problem**: A simulation that works beautifully at 10 agents becomes unmanageable at 1,000 because the player cannot inspect, monitor, or direct that many entities. The solution is not more UI elements but better abstraction layers: showing group-level metrics by default, surfacing individual-level detail only for exceptional cases, and using alert systems to direct attention to problems rather than requiring the player to constantly scan everything.

**Deterministic-feeling randomness**: If events are purely random, the simulation feels like a slot machine. If events are purely deterministic, the simulation feels scripted. The sweet spot, demonstrated by CK3's weighted event system and RimWorld's storyteller AI, is contextually-influenced randomness: what happens feels unpredictable but, in retrospect, makes sense given the state of the world.

**Simulation breadth vs depth trade-off**: Dwarf Fortress simulates individual dwarf psychology, fluid dynamics, geology, ecology, and civilization history, but each system has rough edges and the game resists accessibility. Victoria 3 focuses tightly on economics and politics, achieving depth in those domains at the cost of abstracted military and cultural systems. For a new simulation game, choosing which systems to simulate deeply and which to abstract is more important than trying to simulate everything.

---

## Applicability Matrix

| Design Concern | Primary Exemplar | Mechanism | Applicability to Canonry |
|---------------|-----------------|-----------|-------------------------|
| Pacing / drama | RimWorld | Storyteller AI curating event selection and severity via wealth-based threat scaling | High: curate which simulation events the player sees |
| Hero-to-nation coupling | CK3 | Individual traits drive political events via stress/personality system | Critical: heroes as the mechanism for national change |
| Population dynamics | Victoria 3 | Stratified pops with needs driving economy and politics via interest groups | High: demographic composition as continuous variables |
| Cascading failure | RimWorld + Pandemic | Threshold-triggered state changes with circuit breakers | High: instability should chain geographically |
| Map visualization | Plague Inc + Paradox | Color-gradient overlays with mapmodes and drill-down-on-demand | Critical: map is the primary interface |
| Historical depth | Dwarf Fortress | Zero-player-game history simulation with linked event graphs | Medium: world-gen should produce legible history |
| Economic emergence | Victoria 3 | Open market with supply/demand price discovery and production methods | Medium: economic pressure should be emergent |
| Culture spread | CK3 | Innovation diffusion via adjacency/acceptance with hybridization mechanics | High: cultural influence as geographic phenomenon |
| Time/tick architecture | RimWorld + Paradox | Fixed timestep with staggered update frequencies and speed control | Critical: foundational architecture decision |
| Diplomatic tension | Victoria 3 | Structured diplomatic plays with escalation/backing-down phases | Medium: soft diplomacy with brinkmanship |
| Constraint-as-gameplay | Old World | Orders system limiting actions per turn, scaled by legitimacy | Medium: resource constraints force interesting triage |
| Scale management | Songs of Syx + Victoria 3 | Individual sim at small scale, group abstraction at large scale | Critical: must solve the micro-to-macro UI problem |

Sources:
- [RimWorld Wiki - AI Storytellers](https://rimworldwiki.com/wiki/AI_Storytellers)
- [RimWorld Wiki - Raid Points](https://rimworldwiki.com/wiki/Raid_points)
- [RimWorld Wiki - Mood](https://rimworldwiki.com/wiki/Mood)
- [RimWorld Wiki - Time](https://rimworldwiki.com/wiki/Time)
- [RimWorld Wiki - XML Defs](https://rimworldwiki.com/wiki/Modding_Tutorials/XML_Defs)
- [RimWorld Modding Wiki - Def Types](https://rimworldmodding.wiki.gg/wiki/Def_Types)
- [RimWorld Wiki - Harmony Modding](https://rimworldwiki.com/wiki/Modding_Tutorials/Harmony)
- [CK3 Wiki - Culture](https://ck3.paradoxwikis.com/Culture)
- [CK3 Wiki - Traits](https://ck3.paradoxwikis.com/Traits)
- [CK3 Wiki - Innovation](https://ck3.paradoxwikis.com/Innovation)
- [CK3 Wiki - Scripting](https://ck3.paradoxwikis.com/Scripting)
- [CK3 Dev Diary #30 - Event Scripting](https://forum.paradoxplaza.com/forum/threads/crusader-kings-3-dev-diary-30-event-scripting.1397140/)
- [Victoria 3 Wiki - Pops](https://vic3.paradoxwikis.com/Pops)
- [Victoria 3 Wiki - Interest Groups](https://vic3.paradoxwikis.com/Interest_group)
- [Victoria 3 Wiki - Political Movements](https://vic3.paradoxwikis.com/Political_movement)
- [Victoria 3 Wiki - Diplomatic Plays](https://vic3.paradoxwikis.com/Diplomatic_play)
- [Game Developer - Deep Dive: Modeling the Global Economy in Victoria 3](https://www.gamedeveloper.com/design/deep-dive-modeling-the-global-economy-in-victoria-3)
- [Game Developer - The Art of the Spiral: Failure Cascades in Simulation Games](https://www.gamedeveloper.com/design/the-art-of-the-spiral-failure-cascades-in-simulation-games)
- [Dwarf Fortress Wiki - World Generation](https://dwarffortresswiki.org/index.php/World_generation)
- [Plague Inc Wiki - Strategy Guides](https://plagueinc.wiki.gg/wiki/Strategy_Guides/Starting_Country)
- [Plague Inc Wiki - Cure Mode](https://plagueinc.wiki.gg/wiki/Cure_Mode)
- [Wikipedia - Pandemic Board Game](https://en.wikipedia.org/wiki/Pandemic_(board_game))
- [Stellaris Wiki - Population](https://stellaris.paradoxwikis.com/Population)
- [Stellaris Wiki - Civics](https://stellaris.paradoxwikis.com/Civics)
- [Old World on Steam](https://store.steampowered.com/app/597180/Old_World/)
- [Songs of Syx on Steam](https://store.steampowered.com/app/1162750/Songs_of_Syx/)
- [Songs of Syx Wiki - Citizens](https://songsofsyx.com/wiki/index.php/Citizens)
- [Paradox Forums - Victoria 3 Dev Diary #76 Performance](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-76-performance)
- [Game Developer - Using Emergent Systems to Improve Interactive Storytelling](https://www.gamedeveloper.com/design/using-emergent-systems-to-improve-interactive-storytelling)
