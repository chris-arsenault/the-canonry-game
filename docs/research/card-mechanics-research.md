# Card Mechanics Research

Research into roguelite deckbuilding games, card-strategy hybrids, and the "cards as simulation interface" design pattern. This document collects findings across two rounds of research. See `card-system-proposal.md` for the design synthesis.

---

## Part 1: Slay the Spire Deep Dive

### The Deckbuilding Loop

Slay the Spire begins every run with a starter deck of ~10 mediocre cards (basic Strikes and Defends). The arc of a run is transforming this weak, generic deck into a specialized engine.

**Card acquisition** happens after combat encounters: choose one of three randomly selected cards, or skip. This "draft one of three" mechanic is effective for interlocking reasons:

1. **Meaningful scarcity.** You cannot build toward a predetermined strategy because needed cards may never appear. A player planning a Strength-scaling deck may need to pivot to poison if the right cards show up. Forced adaptation separates good players from great ones.

2. **Contextual value.** A card's value depends entirely on what's in your deck, what relics you hold, what enemies lie ahead, and your current HP. Catalyst (triple your poison) is meaningless without poison-applying cards. The same card goes from "skip" to "run-defining" based on context.

3. **Simple without being shallow.** Three choices avoid analysis paralysis while still presenting genuine dilemmas.

**Card removal** is arguably more important than acquisition. Removing a basic Strike from a 15-card deck makes every remaining card appear more frequently. This is "deck thinning" -- a 10-card deck where every card is strong outperforms a 15-card deck where 5 are dead weight.

The **thin vs. fat deck** debate encapsulates a core strategic axis. Thin decks (8-15 cards) cycle quickly, finding key cards every fight, but are fragile to status cards (Burn, Wound, Daze) added by enemies. Fat decks (25+ cards) are resilient but inconsistent. Relics like Snecko Eye reward fat decks; Runic Pyramid rewards thin ones.

**Card upgrades** at rest sites create a heal-vs-upgrade dilemma: improving a card's stats versus restoring HP. Short-term survival against long-term deck quality.

### The Relic System

Relics are passive items persisting for an entire run. ~180 relics range from straightforward stat boosts (Vajra: +1 Strength) to game-warping rule changes (Dead Branch: exhausting a card generates a random replacement).

**Build-defining relics** fundamentally reshape strategy. Dead Branch transforms Exhaust-heavy decks from careful resource management into chaotic card generation engines. Snecko Eye (draw 2 extra cards, all costs randomized 0-3) destroys low-cost decks but makes expensive cards dramatically better.

**Relic synergies** create emergent "a-ha" moments. Necronomicon (first Attack costing 2+ is played twice) plus Heavy Blade (damage multiplied by Strength) creates multiplicative interactions the game never explicitly teaches. Discovery through play.

The boss relic choice after Act 1 is pivotal: powerful relics carry severe downsides (Coffee Dripper: +1 energy but no resting; Runic Dome: +1 energy but enemy intents hidden). Evaluating whether your deck can handle the downside requires deep self-knowledge.

### The Energy System

Each turn: 3 energy, 5 cards drawn. **You cannot play everything in your hand.** This constraint is where all tactical depth lives. Every turn asks: prioritize damage (ending the fight faster) or defense (surviving this turn)?

**Powers** (persistent effect cards) create a temporal tradeoff: investing energy in setup early pays dividends over a long fight but may kill you in a short one. "Can I afford to set up rather than block right now?" is a constant evaluation.

### Card Types: Attacks, Skills, Powers

- **Attacks**: deal damage, scale with Strength. Reactive -- played because enemies need to die.
- **Skills**: defense, utility, card manipulation. Block prevents damage 1:1 and decays next turn.
- **Powers**: played once, provide permanent effects. Demon Form (+2 Strength per turn) starts as a 3-energy investment doing nothing, but compounds into devastation.

The interplay creates archetypes: "front-loaded" decks win quickly with cheap attacks; "scaling" decks invest in Powers and plan for extended fights. Enemy design tests both approaches.

### Map and Pathing

Branching maps with node types: normal enemies, elites (harder, guaranteed relic), rest sites, shops, events, treasure. The player sees 3-5 nodes ahead and plans routes. Strong deck + low gold -> path toward elites. Low HP -> route through rest sites. This blend of planned strategy and tactical adaptation makes each run feel like a coherent narrative.

Three-act structure provides macro-pacing: Act 1 establishes a viable deck, Act 2 dramatically increases difficulty, Act 3 demands a fully realized strategy.

### Synergy Discovery

Slay the Spire avoids "one viable strategy" through: character-specific card pools supporting multiple archetypes; relic-driven pivots that make otherwise suboptimal strategies dominant; enemy composition that rewards different configurations; and emergent combos from simple rules (Corruption + Dead Branch + Dark Embrace = play entire deck for free while generating replacements).

### Tension Sources

**Turn-by-turn:** energy/hand constraint creates a miniature optimization puzzle each turn. Enemy intent (showing planned attack/block/buff) provides enough information to plan but not enough for certainty.

**Run-level:** three currencies (HP, gold, deck composition) create cross-cutting pressures. Playing conservatively preserves HP but produces weak decks. Playing aggressively builds strength but leaves you at 10 HP going into elites.

### Ascension System

20-level difficulty ladder. Each level adds permanent handicaps (more enemy HP, starting curse, less healing, boss buffs). Ascension does NOT unlock new content -- it changes the value calculus of existing content. Cards that are mediocre at Ascension 0 become essential at Ascension 15. Replayability through shifted strategic constraints.

---

## Part 2: Non-Combat Roguelite Card Games

### Against the Storm

City-building roguelite where card-like mechanics govern strategic decisions. Building blueprints are drafted (pick 1 of 3), cornerstones are permanent buffs selected at reputation milestones, and orders are objective cards from the Queen.

**Tension without combat:** The dual Reputation/Impatience track. Reputation rises from fulfilling orders and keeping villagers happy. Impatience rises over time and from villager deaths. Rushing orders may neglect villager needs (deaths raise Impatience); focusing on comfort may mean orders expire (insufficient Reputation). Storm cycles add temporal pressure: periodic storms debuff production and morale, requiring stockpiling during clear weather.

**Opposition:** The environment and time. The Blightstorm disrupts production and morale. Hostile glades contain dangers. The Queen's Impatience is the ever-present clock.

**Meta-progression:** Smoldering City upgrades unlock new building blueprints and perks that enter the randomized pool for future runs. More unlocks = more variety per run, not more things in every run. Expands possibility space without expanding per-run complexity.

### Stacklands

Everything -- villagers, resources, buildings, food, enemies -- is a card on a board. Actions performed by stacking: drag Villager onto Berry Bush to harvest. Stack 2 Villagers to create a child. Stack Villager + Crafting Table + Wood + Stone to produce tools.

**Tension:** The food clock is relentless. Each villager consumes 1 food per moon. Insufficient food = death = reduced workforce = less food production (death spiral). Coin scarcity limits card pack purchases, forcing balance between food/resource/exploration spending. The board has finite physical space.

**Design pattern:** Pure tableau building. Cards stay on the table, they don't cycle through a draw pile. The tactile act of arranging cards into production chains IS the gameplay.

### Signs of the Sojourner

Deck represents communication skills. Conversations resolved through card-matching: your card's right edge symbol must match the NPC's left edge. Enough matches = successful conversation.

**Tension:** After each conversation, you must swap one of the NPC's cards into your deck, replacing one of yours. Adapting to one town's symbols means losing connection to another. Your 10-card deck drifts from your hometown's symbols, making returning home feel mechanically and emotionally distant. **You literally cannot be fluent in everyone's language.**

**Design insight:** The deck IS your personality. A deck full of empathy symbols connects with emotional NPCs but fails with brusque traders. Deckbuilding as identity formation.

### Griftlands (Negotiation Deck)

Dual-deck roguelite: battle deck for combat, negotiation deck for social conflicts. The negotiation deck resolves persuasion, intimidation, and manipulation as a card game. Both parties have an "argument core" with HP. Diplomatic (green) cards use reason; Hostile (red) cards use intimidation.

**Tension:** Using Hostile cards wins arguments but NPCs remember -- they become future enemies in combat. The two decks interact: social choices affect who fights you and who helps you. Meta-tension is relational, not just mechanical.

### Ratropolis

Real-time deckbuilder combining tower defense with city management. Cards represent buildings, military units, economic actions, and advisors. Economic infrastructure cards compete with military cards for deck slots.

**Tension:** Military units cost gold; gold comes from economic buildings; economic buildings require population; population requires housing and food. Over-investing in military = economic collapse. Over-investing in economy = military vulnerability. Real-time play means you use the best card available NOW, not the optimal card.

### Cultist Simulator

Card-based narrative simulation. Cards placed into timed "verb slots" (Work, Study, Dream, Talk, Explore). Everything is a card: Health, Reason, Passion, books, followers, lore fragments. No recipes are explicitly taught -- discovery through experimentation.

**Tension:** Multiple simultaneous ticking clocks. Fascination (too high = insanity), Dread (too high = despair), Notoriety (too high = arrest), Health decay (zero = death). Each is generated as byproduct of useful actions: studying forbidden lore generates Fascination, dangerous expeditions generate Dread, public rituals generate Notoriety. Must manage all simultaneously while making progress.

**Design pattern:** Tableau grows organically. No formal "draft" mechanic -- new cards emerge from verb interactions. Management challenge equivalent to deckbuilding: limited verb slots create action economy, deciding which cards to commit to which actions is the strategic core.

### Board Game Exemplars

**Dominion** (2008): Invented deckbuilding. Non-combat economic/political power engine. Victory Point cards are dead weight in deck but necessary to win. Foundational tension: the thing you need to win makes your deck worse.

**Terraforming Mars**: Three card types as temporal patterns. Green cards = one-shot with permanent tag effects. Blue cards = persistent engines with ongoing/activatable actions. Red cards = pure one-shot events. Private engine (tableau) produces effects on shared simulation (the planet).

**Wingspan**: 3-row habitat grid tableau. Birds activate sequentially (right-to-left) creating chain reactions. Each bird represents a real species with ecological behaviors. Engine-building as ecosystem simulation.

**Dune: Imperium**: Cards serve triple purpose -- send agent to board location (political action), provide reveal effects (planning culmination), or serve as currency (spent to buy better cards). Deckbuilding as political network building.

**Spirit Island**: Invader deck runs autonomously via deterministic rules. Player cards are interventions disrupting the simulation. Fast powers (before Invaders) prevent damage but are weaker. Slow powers (after Invaders) are stronger but require absorbing damage first. The simulation runs; you interfere with it.

---

## Part 3: Card Mechanic Theory

### Why Deckbuilding Works Psychologically

- **Ownership and expression.** Your deck reflects your strategic preferences and adaptation. Two players facing identical offerings build different decks. Authorship creates investment.
- **Visible progression within a session.** Each new card or removal is tangible improvement. Your deck IS your character sheet, visible at a glance.
- **Engine-building dopamine.** Adding a synergy card triggers anticipation of future turns. Anticipation is often more pleasurable than execution.
- **Meaningful irreversibility.** Every card added or removed changes every future draw. Permanence makes choices feel consequential.

### Card Advantage Beyond Combat

- **Action economy advantage**: more meaningful actions per unit of time (Cultist Simulator verb slots, Terraforming Mars actions per round).
- **Information advantage**: knowing opponent's style or future threats enables informed decisions.
- **Option advantage**: versatile deck with answers to multiple situations > narrow specialization.

### Tableau Building vs. Deckbuilding

Distinct mechanics, often conflated:

**Deckbuilding**: cards cycle through draw pile -> hand -> discard -> reshuffle. Key property is **probability** -- adding/removing cards changes draw frequency. Strategic question: probability manipulation.

**Tableau building**: cards played onto table permanently, providing ongoing effects. Key property is **permanence** -- each card is a permanent engine addition. Strategic question: spatial and combinatorial optimization.

For a civilization simulation, both have merit:
- Tableau naturally represents persistent infrastructure (a "Granary" card sitting on the table producing food each turn)
- Deckbuilding naturally represents available actions (drawing "Diplomatic Mission" this turn = diplomatic opportunity available right now)
- **Hybrid approach** (some cards permanent installations, others cycled actions) may best serve a simulation context

### Emergence from Simple Card Interactions

Design principle: make cards that modify game rules or create triggers, then let players discover interactions. Cards saying "when X happens, do Y" combined with cards that "make X happen more often" always create emergent combos. Slay the Spire's Exhaust synergies, Dominion's Village-Smithy chains, Terraforming Mars's tag cascades all demonstrate this.

### Representing Systemic vs. One-Shot Effects

Four proven temporal patterns:
- **Powers / Permanents**: played once, effects forever. Institutions, cultural practices, technologies.
- **Events**: played once for immediate effect, then gone. Discrete historical moments.
- **Durations**: effects over current AND next turn. Multi-turn campaigns, seasonal phenomena.
- **Exhaust / One-use**: powerful but single-use per run. Unique historical opportunities.

---

## Part 4: Card-Strategy Hybrid Game Analysis

### Against the Storm (Detailed)

Blueprint selection must respond to map reality: if map has clay but no copper, you need blueprints leveraging clay chains. Cornerstones that boost specific processing are valuable only if the map supports it. Three-way tension: what cards are offered, what the map provides, what species demand.

All randomness is concentrated in the *selection* layer. The simulation (resource production, population needs) is entirely deterministic. The player never feels the simulation is cheating. Challenge comes from adapting deterministic systems to randomized constraints.

### Stacklands (Detailed)

Cards are the simulation, not an interface to it. No separate simulation layer. Card arrangement on the board is spatial management. The tactile act of stacking cards to form production chains IS the core interaction. Directed randomness through themed card packs (player chooses domain of randomness but not specific outcome).

### Griftlands (Detailed)

Negotiation cards map rhetorical strategies onto mechanical effects. "Veiled Threat" deals damage. "Common Ground" builds composure. Hostile cards win arguments but create long-term enemies. The dual-deck system forces resource division between combat and social preparation.

### Dorfromantik

Hexagonal tile cards placed to build landscapes. Score from matching edges. Quests demand specific terrain configurations but draws are random. Tension between ideal placement (maximizing matches) and quest completion (specific terrain groups). Zero-conflict, purely constructive, yet deeply tense. Proves card-based randomness creates compelling tension even without opposition.

### Stellaris Tech System

Technology "cards" drawn from weighted pools. Weights depend on empire traits, existing research, resources, and discoveries. "Guided randomness" -- draws feel responsive to empire identity while producing surprises. Prevents optimal-path beelining that static tech trees suffer from.

### Frostpunk Board Game

Law cards come in contradictory pairs (Child Labor vs Child Education). Passing a law shuffles consequence cards into the event deck. Consequences surface later unpredictably. Models delayed, uncertain social effects of governance decisions. The mechanical elegance: you know a consequence is coming but not when.

### Oath: Chronicles of Empire and Exile

Denizen cards represent political infrastructure, cultural institutions, military capabilities. Card composition of the world carries over between sessions. The World Deck evolves: cards never drawn get removed (forgotten possibilities); heavily used cards persist. Civilization's "character" emerges from play history.

---

## Part 5: The Active Tableau Pattern

### How Existing Games Handle Tableaux

**Terraforming Mars**: blue cards form persistent tableau. No upper limit. Engine power accelerates through tag synergies.

**Wingspan**: 3x5 grid with positional strategy. 15-slot maximum forces evaluation of each bird against future potential. Sequential activation (right-to-left) creates chain reactions.

**Race for the Galaxy**: tableau provides ongoing production, consumption, and cost reductions. Multi-use cards (hand cards also serve as currency) create constant tension between building tableau and maintaining hand flexibility.

**Everdell**: five card categories -- one-time-use, location (worker-placement spot), production (activates during transitions), ongoing (passive modifier), endgame-scoring. Maps directly to simulation design.

**Deus**: playing a card of a given color activates ALL cards of that color. Exponential scaling rewards specialization but diversification gives flexibility.

### Adding and Removing Cards

**Addition** gated by: resources (Terraforming Mars: megacredits), action economy (one per turn), prerequisite tags. Creates pacing: sparse early, dense late.

**Removal** is rare and dramatic. Wingspan birds cannot be removed (ecological permanence). Oath denizens can be replaced (institutional overthrow). The inability to remove is itself a design choice: forces living with past decisions, creating path dependency.

**Transformation**: Ark Nova allows upgrading action cards (flipping to stronger side), representing development rather than replacement.

### What Creates Tableau Composition Tension

- **Opportunity cost with limited slots** (Wingspan's 15-slot limit): "is this card better than anything I might draw later for this slot?"
- **Synergy vs. coverage** (Terraforming Mars tags): specialization creates power but leaves gaps
- **Tempo vs. engine power**: early engine cards compound; late engine cards can't pay off. When is it too late to invest?
- **Contradictions and anti-synergies** (Frostpunk law pairs): individually powerful but collectively incompatible cards create the most interesting puzzles

### How Tableau Cards Interact

- **Tag/type triggering**: playing cards with specific tags triggers effects on existing cards. Discoverable combos reward mastery.
- **Sequential activation**: order matters; placement sequences maximize chain output.
- **Resource sharing**: some cards produce resources consumed by others, creating internal dependencies.
- **Global condition modification**: meta-cards change rules for all other cards ("all Plant cards cost 2 less"). Multiplicative effects reward specialization.

### What Limits Tableaux

- **Slot limits** (Wingspan: 15; Ark Nova: board space): hard caps
- **Resource costs** (Terraforming Mars: escalating costs): soft caps
- **Maintenance costs** (ongoing investment to keep cards active): attrition pressure
- **Contradiction rules** (Frostpunk: paired laws): logical incompatibility

---

## Part 6: Tension in Non-Combat Tableau Games

### How Existing Games Create Tension

**Terraforming Mars** creates agonizing card selection through sunk-cost drafting (pay 3 MC per card kept) and a shared game clock (global parameters advance regardless of your actions). "Doing nothing" is visibly costly because the game approaches its end whether you like it or not.

**Wingspan** creates tension through shrinking action budgets (8, 7, then 6 turns across three rounds) and mutual exclusion (every action precludes three others). Investment-vs-harvest timing is the core dilemma.

**Spirit Island** creates tension through predictable autonomous threat. The Invader pipeline (explore -> build -> ravage) is visible two turns ahead. You are never reacting to surprises; you are triaging known future damage. **Predictability amplifies tension** because the decision of what to prevent feels like genuine responsibility, not luck.

**Common pattern**: tension emerges when the player wants to do more than the system allows, and the cost of inaction is visible and escalating.
- Shrinking opportunity windows (cards disappear, parameters advance, turns decrease)
- Visible future consequences (you can see what happens if you don't act)
- Mutual exclusion (genuine either/or, not "do everything eventually")
- Asymmetric pacing (simulation moves faster than you can match, forcing triage)

### The Draw vs. Always-Available Question

**Randomized draw** (Slay the Spire): creates adaptation and discovery. Works because discrete encounters make hand composition tactical.

**Always available** (Grand Strategy): pure optimization. Tension from resource constraints and opportunity cost. Risk: experienced players solve it.

**World-state-driven availability** (Stellaris tech, Against the Storm blueprints): cards become available because simulation reached a state making them relevant. "Monotheistic Reform" appears only after polytheistic religion exists. Discovery comes from the simulation surfacing possibilities, not from random draws.

**Recommendation**: world-state-driven availability with a mild draft (pick 1 of 2-3 eligible cards). Preserves agonizing choice without pure randomness.

### Multi-Entity Tableau Management

**One tableau for whole world (god model)**: cards represent universal laws and meta-forces. "Cultural Diffusion" applies to ALL cultures. Thematically clear (player is a god), but global effects feel blunt. Mitigate with conditionally asymmetric effects.

**One per entity (ruler model)**: granular control, asymmetric strategy, but cognitive overload managing 5+ tableaux.

**Recommendation**: god model (one global tableau) with cards that have conditionally asymmetric effects on different civilizations. A small "focus" mechanism can target certain cards at specific entities without full multi-tableau management.

### Card Interaction with Autonomous Simulation

**Spirit Island model**: simulation runs regardless; cards modify how it runs, not whether it runs. Cards are interventions, not controls.

**Simulation can invalidate cards**: "Fertile River Valley" becomes invalid if the river is dammed. "Religious Unity" breaks if a schism occurs. Forced adaptation from the simulation itself is the richest tension source.

**The feedback loop**: card effects are observable (cultures spread faster after playing Cultural Diffusion) -> simulation changes are legible (this nation expanded because of resource advantage) -> new card relevance is discoverable ("now that there are coastal cities, Naval Trade would be powerful"). The simulation teaches the player what cards to want.

### Meta-Progression and Unlocking

**Slay the Spire**: character-level XP unlocks new cards into the random offering pool. Unlock is a promise of future discovery, not immediate power.

**Against the Storm**: Smoldering City progression unlocks new blueprints into the draft pool. More unlocks = more per-run variety, not more per-run complexity.

**Pool dilution mitigation**: guarantee new card appears in next run, weight recently unlocked cards higher, let player pin/favorite cards.

**Recommendation**: unlock cards into eligibility pool; world state determines which eligible cards are offered per run. Thematic triggers: "You witnessed monotheism develop -- 'Prophetic Tradition' is now unlocked."

---

## Part 7: Card = Simulation Component Analysis

### Does "Which Components Are Active" Create Interesting Decisions?

**The baseline problem**: in naive implementation, every system is purely beneficial. Rational player wants everything on. This is "strictly dominant strategy" -- no real decision.

**Four structural solutions**:

1. **Asymmetric trade-offs (every card cuts both ways)**: Cultural Diffusion spreads ALL culture, not just yours. Disease Spread weakens all dense populations, including yours. The decision is contextual: given current world state, does this help or hurt?

2. **Slot limits as opportunity cost**: only 7 active cards means activating one means NOT activating another. Transforms "is this card good?" into "is this card better than what I'd replace?"

3. **Environmental response**: the simulation changes card value. Early game (few entities): generators critical. Mid game (many entities): systems critical. Late game (competing entities): actions critical.

4. **Interaction effects**: Cultural Diffusion + Trade Routes = faster spread along trade corridors. Disease Spread + Dense Urbanization = devastating plagues. Value of any card depends on rest of tableau.

### How Card Types Map to Simulation Components

**Generator cards as "creation valves"**: continuous model is more interesting than exhaustion. While "Found Settlement" is in tableau, new settlements may appear each tick. The card is a genuine simulation component -- an ongoing world rule ("settlements can be founded"). Give generator cards visible, immediate impact on activation (place the card, see a hero emerge next tick).

**System cards as "world rules"**: most novel card type. When in tableau, a world process is active. Key design space: inter-system interaction. Cultural Diffusion + Trade Routes = cultures spread along trade corridors. Communicate interactions through tags/keywords or through narrative surfacing ("Trade routes between Kesh and Aldara carry cultural influence").

**Action cards as "possibility space controllers"**: player determines which actions are POSSIBLE for simulated actors. "Forge Alliance" in tableau = factions can form alliances. Not in tableau = alliance concept doesn't exist. Player is a **legislator of reality**, not an executive. Tension: enabling powerful actions that AI actors may use against your interests.

### Phase Transitions Force Tableau Adaptation

- **Genesis Phase** (early): generators paramount ("Forge Civilization," "Settle Frontier"). Systems have nothing to work with.
- **Interaction Phase** (mid): systems critical (trade, diffusion, diplomacy). Generators less important.
- **Competition Phase** (late): actions matter most (alliances, wars, wonders). Fine-tuning possibility space.
- **Crisis Phase** (disruption): specific response cards needed. "Disease Quarantine" wasteful in peacetime, essential in pandemic. The insurance problem: dedicate a slot to crisis response or run lean?

### The God Perspective

Most card games put you in the role of one competitor. This game makes you a force shaping reality itself.

**Existing precedents**: Cultist Simulator (you ARE the organizing principle), Black & White (literal god), Reus (elemental giants shaping a planet), Crest (god issuing commandments followers may misinterpret).

**Creating investment without faction alignment**: investment through authorship. The player cares because they shaped the rules, and outcomes are products of their decisions. Same investment a DM feels in a D&D campaign.

**Mitigation for detachment risk**: personal objectives creating emotional stakes. "Guide a hero to found a lasting dynasty." "Prevent collapse of the oldest civilization." "Ensure three distinct cultures survive."
