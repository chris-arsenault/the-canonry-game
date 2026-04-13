# Design Review: The Canonry Game

A thorough critique of the MVP system designs from a game design perspective. Covers inconsistencies, player agency, complexity, design gaps, balance, and production readiness.

---

## 1. Inconsistencies Requiring Resolution

### 1.1 System Numbering is Broken

Every system doc from Tick Clock onward has a title number that disagrees with `game-systems.md` and the file names. The files were renumbered (file `05-tick-clock.md`) but the internal headings still say the old number ("System 6: Tick Clock"). This cascades through every cross-reference table in every doc.

| File | Title in Doc | Number in game-systems.md |
|------|-------------|---------------------------|
| `05-tick-clock.md` | System 6 | 5 |
| `06-entity-behaviors.md` | System 7 | 6 |
| `07-relationships.md` | System 8 | 7 |
| `08-resources-economy-POST-MVP.md` | System 9 | 8 |
| `09-lifecycle.md` | System 10 | 9 |
| `10-map-visualization.md` | System 11 | 10 |
| `11-pressure.md` | System 5 | 11 |
| `12-deck.md` | System 13 | 12 |
| `13-tableau-mana.md` | System 14 | 13 |
| `14-lose-condition-monitor-POST-MVP.md` | System 15 | 14 |
| `16-generator-executor.md` | System 17 | 16 |
| `17-system-executor.md` | System 18 | 17 |
| `18-action-executor.md` | System 19 | 18 |
| `19-narrative-domain.md` | System 20 | 19 |

Pressure is the worst: "System 5" in its own doc, "System 11" in `game-systems.md`, file `11-pressure.md`. The interaction tables within docs use yet another numbering convention (they seem to reference `game-systems.md` numbers, e.g. "Entity Behaviors (#6)"). This needs a single pass to unify.

**Action:** Pick one numbering scheme and update all doc titles, file names, and cross-reference tables to match.

### 1.2 Pressure's Altitude Classification is Contradictory

`game-systems.md` lists Pressure under "Low-Altitude Systems (Always-On Physics)" (section header, right alongside entity behaviors and relationships). But `11-pressure.md` explicitly states: "Pressure is a MID-ALTITUDE concept" and "Pressures are read-only to low-altitude systems. Low-altitude entity behaviors DO NOT read pressure values."

The design intent is clearly that pressure is a *bridge* between altitudes -- computed from low-altitude state, consumed by mid-altitude cards. But its categorization in `game-systems.md` puts it in the low-altitude section, which contradicts its own spec.

**Action:** Create a distinct "Bridge Layer" section in `game-systems.md` between Low-Altitude and Player Systems, or relabel Pressure's placement. The altitude model is one of the game's best architectural ideas -- it should not be muddled by sloppy categorization.

### 1.3 Resources: Deferred but Load-Bearing

Resources/Economy is POST-MVP, but entity behaviors for settlements list "resource production," "resource consumption," and "resource deficit triggers decline" as MVP behaviors. Nations list "resource flow between settlements" as a core behavior. `game-systems.md` says "For MVP, resource-like effects can be approximated through entity attributes (settlement prosperity, regional fertility)" -- but the entity behaviors doc doesn't describe this approximation. It describes a full resource system.

This creates ambiguity: do settlements produce and consume at MVP, or don't they? If they do, what are they producing and consuming? "Prosperity" as a single attribute that goes up and down? That's a very different simulation than one with food and materials.

**Action:** Write a short "MVP Resource Approximation" section that specifies exactly which entity attributes substitute for resources, how settlement "production" and "consumption" work without a resource system, and what triggers population decline without food deficit tracking. This is a critical gap because settlement behaviors are the economic backbone of the simulation.

### 1.4 Entity Kind List: "Not Finalized" but Designed Around

`02-entity-kinds.md` explicitly states: "The specific set of entity kinds requires its own dedicated design review. The kinds referenced elsewhere in these docs are working placeholders, not a reviewed design." It flags "culture as kind vs. attribute" as an open question.

Yet every other system doc treats the kind list as settled fact. Entity behaviors has detailed behavioral descriptions for nations, cultures, settlements, factions, artifacts, and heroes. Pressure references culture-specific pressures. Cards target specific kinds. The entire altitude model depends on these kinds existing with these behaviors.

If culture stops being a kind and becomes an attribute, the ripple effects are massive: cultural pressure computation changes, the pairwise cultural friction system card example breaks, influence radiation as a low-altitude behavior disappears, and cultural cohesion as a metric needs rethinking.

**Action:** Resolve the entity kind list before production begins. Specifically: (a) commit to culture as kind or attribute, (b) decide on kind granularity (broad kinds + subtypes vs. narrow kinds), (c) confirm the working set or change it. Every system doc downstream depends on this decision.

### 1.5 "Era" Entity Kind Exists but is Never Described

`game-systems.md` lists the entity kinds as: "region, settlement, nation, culture, faction, hero, artifact, occurrence, era." The entity kinds doc also references "era." But no system doc describes what an era is, how eras transition, what triggers them, or what role they play in the simulation. Autosave "at era transitions" is mentioned. The post-MVP backlog references "era-locked" card swapping. But the era concept itself is undefined.

**Action:** Either define eras (what triggers a transition, what changes, how the player perceives them) or explicitly defer them to post-MVP and remove era references from MVP docs.

### 1.6 "Occurrence" Entity Kind: Thin Definition

Similarly, "occurrence" is listed as an entity kind but only appears in the post-MVP backlog under "Occurrence Depth." There's no system doc describing what occurrences are, how they're created, or what role they serve at MVP.

**Action:** Same as eras -- define or defer.

---

## 2. Player Agency Analysis

### 2.1 The Core Verb is Thin

The player's verb set across a 4+ hour session:

| Verb | Frequency | Description |
|------|-----------|-------------|
| Observe the map | Continuous | Look at overlays, entity markers, spatial patterns |
| Inspect entities | Frequent | Read attribute panels, relationship details |
| Adjust speed | Frequent | Pause, play, fast-forward |
| Play card to tableau | Occasional | Move card from hand to active slots |
| Remove card from tableau | Occasional | Free mana, deactivate a simulation component |
| Draw cards | Automatic | Happens on timer/milestone, not a player choice |
| Choose deck modification | Rare | Select from options during modification event |

The player's **only active lever** is tableau composition: which cards are in and which are out. Drawing is automatic. Deck modification is rare. Everything else is observation.

Compare to the cited inspirations:
- **SimCity:** Zone, build roads, set budgets, manage utilities, respond to disasters, set tax rates, pass ordinances -- dozens of active verbs, constant small decisions
- **Stellaris:** Research, diplomacy, fleet management, construction, colonization, policies, edicts, espionage, war management -- every game system has direct player interaction

This game has one verb: "swap cards." The design is philosophically coherent (the player defines nature, not commands), but the question is whether one lever is enough to sustain contemplative engagement for 4+ hours.

**The risk:** The game feels like watching a screensaver with occasional card changes. The contemplative tempo requires that *looking* is productive, but productive looking without the ability to *act* on what you see leads to frustration, not contemplation. SimCity works because when you notice a traffic problem, you can build a road *right now*. In this game, when you notice cultural tension, you can... swap a card and wait 50 ticks to see if it helps.

### 2.2 Intervention Cards Would Fix This -- But They're Post-MVP

The post-MVP backlog describes intervention cards: one-shot hand cards for direct divine action (smite, earthquake, bless, sever alliance). These would give the player granular, immediate, targeted agency -- the exact thing the core loop is missing. They'd transform the game from "swap cards and watch" to "swap cards, watch, and occasionally reach into the world."

**Question:** Should intervention cards be promoted to MVP? Even a minimal version (3-4 basic interventions with simple effects) would dramatically improve player agency without undermining the "define nature, don't command" thesis. An earthquake that reshapes the map, a blessing that boosts an entity, a smite that damages one -- these are god-game staples for a reason.

### 2.3 Free Swapping Undermines Commitment

MVP has no cooldown or cost for swapping tableau cards. The design acknowledges this might be "too flexible" but defers friction to later tuning.

From a game design perspective, free swapping fundamentally undermines strategic identity. If swapping is free:
- The player has no reason to commit to a tableau configuration
- The optimal strategy is reactive: swap cards in response to immediate conditions, never building a coherent worldview
- The "deck as theory" concept collapses -- a theory you can revise every tick is not a theory, it's reflex
- There's no cost to experimentation, which means no weight to decisions

**Action:** Even for MVP, add *some* friction. A 10-tick cooldown after any swap is the minimal version. This forces the player to live with their choices for a meaningful period, creating the "will this work?" tension that drives engagement.

### 2.4 No Targeting Means No Favorites

In neutral mode, cards affect the entire world uniformly. The player cannot direct "Cultural Diffusion" at one region. They cannot boost one nation's stability. They cannot target any card effect at any specific entity or place.

This means the player's strategic space is one-dimensional: they choose WHICH forces exist in the world, but not WHERE those forces apply. A richer strategic space would let the player do both. Even without violating the "define nature, don't command" principle, scoped cards could exist: "Cultural Diffusion (Northern Highlands)" that only applies to one region.

**Question:** Should some cards have player-selectable scope? This adds targeting as a second strategic axis without giving the player direct control over entities. The player still defines nature -- but they could define *regional* nature, not just global nature.

### 2.5 Speed Control as the Real Primary Verb

Given the thin action space, I predict speed control becomes the player's actual primary activity:
1. Fast-forward through quiet periods
2. Notice something interesting on the map
3. Pause
4. Study what's happening
5. Maybe swap a card
6. Unpause
7. Fast-forward again

This is fine as a gameplay loop -- it's essentially Stellaris on autopilot. But it means the game lives or dies on the quality of "notice something interesting on the map." If the map visualization doesn't surface meaningful, actionable information at a glance, the fast-forward periods become "skip everything" periods, and the game loses its contemplative quality.

**Action:** The map visualization system needs to be treated as a **first-class game design system**, not just a rendering layer. It should surface alerts, trends, and anomalies -- not just current state. "Military tension increasing in the Northern Highlands" should be visually obvious without the player having to inspect each region's pressure panel.

---

## 3. Complexity Assessment

### 3.1 Engine Complexity: High but Justified

The simulation architecture is sophisticated:
- 6 entity kinds with distinct behavioral models
- Utility AI with weighted random selection for all entity impulse evaluation
- Semantic planes with reducer functions for multi-dimensional attributes
- Full DSL for card definitions (preconditions, targeting, mutations)
- Pressure system with homeostasis and smoothing
- Relationship system with dispositions derived from semantic plane distance
- Effect transfer system for cross-entity attribute modification via relationships

Each of these is individually well-designed and serves a clear purpose. The altitude model (low-altitude physics -> pressure bridge -> mid-altitude cards) is architecturally elegant. The question is not whether any individual piece is too complex, but whether the **aggregate** is buildable, testable, and tunable as an MVP.

### 3.2 Six Entity Kinds at MVP is Ambitious

Each entity kind needs:
- A defined attribute set (including semantic planes and reducers)
- An impulse list with utility scoring curves
- Relationship types it can participate in
- Lifecycle drain function
- Behavioral simulation code that runs every tick
- Card targetability definitions

That's six behavioral models, each with unique impulse evaluation logic, running simultaneously from day one. The risk isn't that any one is too complex -- it's that debugging emergent behavior across six interacting kinds is extremely hard without good tooling.

**Question:** Could MVP ship with 3-4 entity kinds (nations, settlements, cultures, heroes) and add factions and artifacts in a fast-follow? Factions and artifacts are interesting but not structurally necessary for the core simulation loop. Reducing the kind count at MVP reduces the testing surface area significantly.

### 3.3 The DSL is a Hidden Major Workstream

The card executor base requires a DSL that supports:
- Composable AND/OR precondition trees
- Variable binding ($actor, $target, $site)
- Graph path traversal (2-hop relationship queries)
- Spatial queries (find cells matching constraints)
- Semantic plane evaluation (reducer labels)
- Runtime modification (Balatro-style card alterations)
- All of this must be serializable (for save/load) and inspectable (for player UI)

This is essentially a query language + scripting language + data format. It needs to be designed, implemented, tested, documented, and debugged. The docs treat it as a configuration format, but it's really a programming language that happens to be embedded in data.

**Action:** Identify DSL design as a standalone workstream with its own prototype phase. Don't assume it will "fall out of" the card executor implementation. Budget time for DSL iteration, debugging tools, and editor support.

### 3.4 The Learning Curve Problem

The player needs to understand:
1. What low-altitude physics exist (per-kind behaviors producing conditions)
2. What pressure is and how it aggregates from conditions
3. What each card in their hand/deck does
4. How cards interact with each other (tag synergies)
5. How card preconditions relate to pressure and world state
6. What the three lose conditions are and what drives them toward danger
7. How deck modification works
8. How the mana economy works

This is a lot. The docs acknowledge no tutorial system at MVP. A game with this much systemic complexity needs *some* onboarding strategy, even if it's just "start with 2 cards and add more as you play."

**Action:** Define a minimal onboarding flow as part of the MVP narrative domain. Even if it's just a pre-configured starting state with a small world and 2-3 cards, with UI annotations explaining what's happening. The Ice Remembers domain definition should include a "first run" configuration.

---

## 4. Critical Design Gaps

### 4.1 Initial World State is Undefined

The map is procedurally generated. But what about the entities ON the map? How many nations exist at game start? How many settlements? How many cultures? Who controls what? What relationships exist?

The generator executor creates "batches of pre-connected entities" -- but generators are mid-altitude cards. They need to be in the tableau. At game start, does the player have generators in their starting deck? Do they fire immediately? Or is the initial world state pre-seeded by the domain?

This is a chicken-and-egg problem:
- Entities create the conditions that cards respond to
- But entities are created by cards (generators)
- At game start, there are presumably no entities and no active cards
- So nothing happens

**Action:** Define the initial world seeding strategy. Options: (a) the domain provides a pre-seeded entity graph with initial nations, settlements, cultures, and relationships, or (b) the first few ticks run a "world generation" phase where initial generators fire rapidly to populate the world. Option (a) is simpler and more controllable. Either way, document it.

### 4.2 No Event/Notification System

When significant things happen (a nation declares war, a settlement is founded, a hero dies, a card fires), how does the player know?

The docs mention "event descriptions" and "event logging" in passing, but no system describes:
- What qualifies as an event worth surfacing
- How events are presented (log, popup, map animation, sound)
- Whether events pause the simulation (some games pause on significant events)
- How event priority is determined (which events are important enough to interrupt fast-forward)
- How event history is browsable

For a game where the player watches the simulation, the notification system is the **primary communication channel** between the game and the player. This isn't a QoL feature -- it's a core system.

**Action:** Define a minimal event system for MVP. At minimum: a scrolling event log with categories (creation, conflict, diplomacy, lifecycle), map ping animations for spatial events, and an option to pause on high-priority events. This system should be in the MVP priority list.

### 4.3 No UI Architecture

The docs describe what the player sees (map, overlays, entity panels, tableau, hand, mana) but not how these fit together on screen. For a game where the player spends 4+ hours staring at the screen, layout is a critical design question:

- How much screen space does the map get?
- Where is the tableau displayed? Always visible? Toggleable sidebar?
- Where are entity detail panels? Do they overlay the map? Slide in from the side?
- Where is the hand displayed?
- Where is the event log?
- Where are pressure readouts?
- How does the player toggle between overlay modes?

**Action:** Create a wireframe or layout specification before production. Even a rough sketch showing the spatial relationship between map, tableau, hand, entity detail, and event log would prevent architectural mistakes in the UI layer.

### 4.4 Relationship Bootstrapping is Unclear

The docs state: "Relationships are created only by cards" and "Two nations that share a border but have no relationship link have no formal diplomatic connection."

But "Declare War" as an action card has a precondition of `relationship_exists($actor, "rival_of", min_strength=0.5)` in the action executor doc's discussion. So war requires an existing rivalry. But rivalry is created by... a card. Which card creates the initial rival_of relationship? Is there a "First Contact" card in every starting deck?

Meanwhile, the relationship strength modifiers (proximity, competing claims) modify *existing* relationship strength. They can't create relationships. So if no card creates the initial diplomatic links, nations can share borders forever with no formal connection, accumulating pressure that has no outlet because no relationship exists to modulate.

The "dispositions" concept (derived from semantic plane distance) partially addresses this -- dispositions exist without relationships and serve as preconditions for relationship-creating cards. But the causal chain needs to be explicit:

1. Nations exist near each other (spatial)
2. They have computed dispositions (semantic plane distance)
3. A "First Contact" or "Establish Diplomacy" action card fires when disposition/proximity conditions are met
4. This creates the initial relationship
5. Low-altitude systems now modulate the relationship's strength
6. Other action cards (war, alliance) can now fire based on the relationship

If step 3's card isn't in the starting deck, steps 4-6 never happen. The starting archetype deck composition is load-bearing for this flow.

**Action:** Ensure that every starting archetype includes at least one relationship-creating action card, and document the intended bootstrapping chain explicitly.

### 4.5 Causal Transparency vs. Emergent Opacity

The design principle "Everything Visible Is Everything There Is" promises that the player can understand why anything happened. But the simulation involves:
- 6 entity kinds running impulses simultaneously
- Cross-kind effects via relationship-derived attribute modifiers
- Goal tags passed between entities
- Pressure aggregated from dozens of contributing factors
- Cards with compound preconditions evaluating against pressure + attributes + relationships + spatial state
- Success/failure probability on actions modified by multiple factors

In practice, "why did Nation A declare war on Nation B?" requires tracing:
1. The action card's preconditions (pressure thresholds, disposition, relationship state)
2. The pressure values and their contributing factors
3. The low-altitude behaviors that built those factors
4. The actor's utility AI scoring that selected this action over alternatives
5. The success probability modifiers
6. Any semantic plane positions and reducer labels involved

Every piece is visible in theory, but the causal chain is deep. The player *can* understand -- but will they? Without causal explanation tools ("why did this happen?" drilldown), transparency is theoretical.

**Question:** Should MVP include a basic "why did this happen?" drilldown for card firings and action executions? Even showing "this card fired because: military tension = 72 (threshold: 60), target found: Nation B (rival_of, strength 0.7)" would make the transparency principle real rather than aspirational.

---

## 5. Balance and Feel Concerns

### 5.1 The Lose Conditions ARE the Game -- and They're Post-MVP

The design principles identify the three-way tension (extinction vs. stagnation vs. monoculture) as the strategic core. The player's tableau must navigate all three. This is what makes card selection meaningful: every card choice affects all three dimensions.

But the lose condition monitor is POST-MVP. Without it, the MVP has no stakes. The player is watching a simulation with no threat of failure, no feedback on whether their tableau is balanced, and no reason to care about card selection beyond curiosity.

**This is the most significant design risk in the current plan.** A simulation sandbox without stakes is an interesting toy but not a game. The contemplative tempo works when observation has consequences. Without lose conditions, observation is leisure, not strategy.

**Action:** Strongly consider promoting the lose condition monitor to MVP, even in a simplified form. At minimum, surface the three threat metrics (extinction risk, stagnation risk, monoculture risk) as visible dashboard values with color-coded warnings (green/yellow/red). This doesn't require calibrated thresholds -- just showing the player "you're trending toward monoculture" gives them something to respond to. Actual game-ending thresholds can be tuned later.

### 5.2 The Cold-Start Problem

At game start:
- The player has ~3-4 mana (enough for 1-2 mid-cost cards, or 3-4 cheap ones)
- They have a hand drawn from their archetype deck
- The world has some initial entities (assuming the bootstrapping problem is solved)
- All 6 entity kinds are running low-altitude behaviors, building pressure in multiple dimensions simultaneously

The player has very few outlets (1-2 active cards) for pressure building across many dimensions. Either:
- The initial world has so few entities that pressure is negligible (boring opening)
- Pressure builds fast and the player is overwhelmed with one card in the tableau (frustrating opening)

Neither is great. The sweet spot requires careful calibration of initial entity count, pressure smoothing rates, and starting mana -- none of which are defined.

**Action:** Plan for significant tuning of the first 50 ticks. The opening feel will make or break first impressions. Consider whether the game should start in a "gentle" mode where pressure build rates are lower for the first N ticks, giving the player time to understand the systems before they become urgent.

### 5.3 Systems Cards Dominate the Mana Economy

System cards run continuously, accumulating incremental effects every N ticks. Over a 200-tick period, a system card at cadence 5 fires 40 times, applying its delta each time.

An action card with a 30-tick per-actor cooldown fires at most 6-7 times per actor over the same period. With maybe 3-5 valid actors, that's 18-35 firings total -- but each firing has a success probability, so effective firings are fewer.

A generator card with a 20-tick cooldown fires at most 10 times, creating 10 entity batches.

For the same mana cost, a system card delivers the most consistent, predictable, accumulated value. This suggests rational players will build system-heavy tableaux, especially without minimum type requirements.

**Question:** Is this intended? If systems are the "backbone of most tableaux" (as the cost design section suggests), then generators and actions need a different value proposition. Generators create the entities that make systems meaningful. Actions create the relationships that systems modulate. Perhaps the balance is self-correcting: a system-only tableau stagnates because no new entities or relationships appear. But this relies on the stagnation lose condition, which is post-MVP.

### 5.4 Deck Cycling May Create Feast-or-Famine Draws

The deck cycles: draw pile exhausted -> reshuffle discard -> new draw pile. With a persistent hand (cards stay until played), the player accumulates cards they want and discards ones they don't. This means each cycle through the deck, the hand fills with increasingly "good" cards and the discards are increasingly "bad" cards.

But the reshuffle puts everything back in. So the player might go through a long stretch of drawing unwanted cards early in a cycle, then a burst of wanted cards late in a cycle. This feast-or-famine pattern could create frustration (early-cycle dry spells) without the player understanding why.

**Question:** Is the persistent hand + cycling deck the right model? Alternatives worth considering: (a) no reshuffle -- each card is drawn once per cycle, period; (b) partial reshuffle -- only discarded cards reshuffle, played cards stay out until removed from tableau; (c) market model -- always-available pool the player picks from, no randomness.

### 5.5 The Hoarding Problem

Cards persist in hand until played or discarded. Hand limit is ~3-7. If the player has a powerful card they're "saving for the right moment," it occupies a hand slot indefinitely, reducing effective draw rate.

Classic TCG hoarding: the player holds a "Declare War" card for 200 ticks waiting for the perfect conditions, while their hand is full and they can't draw anything new. This is mechanically punishing but psychologically hard to resist.

**Question:** What's the pressure to play or discard? Without end-of-turn discard (no turns), the only pressure is hand size limit (can't draw if full). Is that enough? Should cards in hand degrade, or should there be a benefit to playing cards quickly? "Fresh play" bonuses?

---

## 6. Production Readiness Assessment

### 6.1 Load-Bearing Open Questions

These open questions block production or create major rework risk if resolved differently than assumed:

| Question | Impact if Unresolved |
|----------|---------------------|
| Entity kind list (especially culture) | Ripples through every system doc |
| Hex vs. square grid | Affects map generation, pathfinding, adjacency, movement, visualization |
| Tick-to-time mapping | Affects lifecycle tuning, seasonal cycles, cadence numbers, session pacing |
| Initial world seeding | Can't test anything without entities |
| MVP resource approximation | Settlement behaviors don't work without resource definitions |
| Card DSL syntax | Card executors can't be built without a DSL spec |
| Starting archetype definitions | Can't test player experience without decks |

### 6.2 What IS Ready for Production

The following are well-specified and ready to build:
- **Entity Graph** (System 1): clean spec, well-defined API contract, clear what lives in the graph and what doesn't
- **Tick Clock** (System 5): single clock, strict phase ordering, cadence registration -- clean and buildable
- **Relationship model** (System 7): strength 0-1, polarity, culling, history -- well-designed and implementable
- **Pressure framework** (System 11): contributing factors, homeostasis, smoothing -- clear computation model
- **Card executor base**: precondition composition, mutation types, lifecycle states -- solid architecture
- **Altitude model** (conceptual): low-altitude physics -> pressure bridge -> mid-altitude cards is the game's strongest design decision
- **Narrative domain separation**: engine/domain boundary is well-defined and should be enforced from day one
- **Save/Load requirement**: forcing serialization from day one is architecturally wise

### 6.3 What Needs Work Before Production

| Item | Work Needed | Why It Blocks |
|------|------------|---------------|
| Entity kind review | Dedicated design session to finalize kinds, resolve culture question | Everything downstream depends on this |
| MVP resource approximation | Short spec defining attribute-based substitutes | Settlement and nation behaviors need this |
| Initial world seeding strategy | Define how entities exist at game start | Can't run the simulation without entities |
| Event/notification system spec | Define what events exist, how surfaced | Player can't understand the simulation without this |
| UI layout wireframe | Rough spatial layout of map/tableau/hand/panels | Prevents rework in the rendering layer |
| Card DSL prototype | Build a minimal DSL and test 5-10 cards | Executors depend on this |
| Starting archetype (1 minimum) | Define one complete deck of cards | Can't test the player loop without it |
| Lose condition metrics (dashboard only) | Surface extinction/stagnation/monoculture metrics | Gives the player stakes without calibrated thresholds |

### 6.4 The Phase Plan is Sound but Underestimates Phase 2

The three-phase plan (engine with test content -> narrative domain -> player systems) is well-structured. Phase 1 validates architecture. Phase 3 builds on a working simulation.

But Phase 2 ("Define The Ice Remembers domain package") is where the entire game's content lives: every card, every subtype, every pressure definition, every ground truth, every archetype, every naming rule, every map parameter. This is a massive content-creation effort, not a single system implementation. The docs treat it as one step ("System 11 in Phase 2"). In practice, it requires:
- Card design (dozens of generators, systems, and actions)
- Card balancing (mana costs, cooldowns, precondition thresholds)
- Subtype definition per kind (impulse weights, attribute ranges)
- Pressure tuning (contributing factors, coefficients, homeostasis rates)
- Map generation parameter tuning
- Archetype deck construction
- Naming rule authoring
- Ground truth definition

Each of these is iterative and depends on playtesting. Phase 2 may be longer than Phase 1.

---

## 7. Specific Questions Requiring Answers Before Production

### Architecture

1. **What happens to ongoing system card effects when the card is removed?** The docs say "effects already applied persist." But what about ongoing per-tick attribute modifications? If "Cultural Diffusion" was pushing cultural influence by +0.5/tick and is removed, the influence stops growing. But does the accumulated shift remain? (Docs say yes.) Does this mean removing a system card is always "safe" because you keep what you've earned? Is there ever a downside to playing a system temporarily?

2. **How do cards evaluate preconditions against entities they can't "see"?** A generator card checks "entity_count(kind=settlement, region=$region, max=3)." But $region isn't bound until the card evaluates. Does the card check ALL regions? Does it iterate? This has major performance implications for cards with spatial scopes.

3. **What prevents feedback loops between systems?** System A increases attribute X. System B's precondition checks attribute X. System B fires and increases attribute Y. System A's precondition checks attribute Y. Both systems fire every tick, amplifying each other. The docs say "all cards evaluate against the same state snapshot per tick" -- which prevents same-tick feedback. But cross-tick feedback loops with 1-tick delay are still possible and could cause divergence.

### Player Experience

4. **What does the player do while paused?** Pause is described as "the simulation is frozen, the player can inspect, plan, and configure the tableau." But can they swap cards while paused? Draw cards? If so, pausing becomes the dominant strategy: pause, study everything, make all changes, unpause. This is fine (Stellaris does this) but should be explicit.

5. **How does the player discover what cards DO before playing them?** The card is in their hand. It has a DSL definition with preconditions, effects, targeting. How is this presented? Can the player see "this card will fire when military tension exceeds 60 in a region with at least 2 nations"? The "everything visible" principle demands this, but the UI for presenting complex precondition trees to players is non-trivial.

6. **What is the run length target?** The docs mention 4+ hour sessions. But is a single "run" intended to be 4 hours? 40 hours? The roguelite framing suggests shorter runs (1-3 hours, like Slay the Spire). The SimCity/Stellaris framing suggests longer sessions. These are very different games. The run length determines: how fast mana expands, how many deck modifications happen, how many entities are created, how much the world evolves. This needs a target.

### Simulation

7. **How do nations expand territory without direct command?** Nations don't receive commands. Their "population pressure driving frontier expansion tendency" is a low-altitude impulse. But "expansion" implies settling new territory, which requires a "Found Settlement" generator card in the tableau. If that card isn't active, nations can't expand. Is national expansion entirely card-gated? Or is there a low-altitude impulse for gradual territorial drift (claiming adjacent empty cells)?

8. **What do factions actually DO at the impulse level?** Nations have clear impulses (border tension, resource flow, population pressure). Cultures have clear impulses (influence radiation, cohesion). But factions are described as "goal pursuit, influence accumulation, internal politics" without specifics. What are a faction's tick-by-tick impulses? "Accumulate influence" -- how? "Pursue goals" -- what specific goals? This is the vaguest of the entity kind behaviors.

9. **How do heroes interact with the macro simulation?** Heroes have needs, movement, aging. But what do they *contribute*? The docs say they're "one entity kind among many -- NOT the primary focus." But they still need to matter mechanically. A hero in a settlement -- does that affect the settlement's attributes? (The effect transfer system suggests yes -- a hero's prominence might boost the settlement's cultural significance.) But the specific effects aren't defined.

10. **What creates the initial relationships between starting entities?** If the world is seeded with 3 nations and 10 settlements, the settlements need "controlled_by" relationships to nations, and they need "located_in" relationships to regions. These are relationships. Relationships are created by cards. Are these "starter relationships" created outside the card system? Or is the seeding process a special initial generator run?

---

## 8. Summary Assessment

### Strengths
- The altitude model (low/bridge/mid) is the game's best idea. It creates a clear separation of concerns and a genuine design innovation for the god-game genre.
- The "deck as theory" concept is compelling. Each run IS a hypothesis about what makes a good world.
- The engine/domain separation is forward-thinking and well-specified.
- The three-way lose condition tension (if implemented) is a strong strategic core.
- The relationship system (strength modulation + topology from cards + dispositions from semantic planes) is sophisticated and well-designed.
- The docs are unusually thorough and self-aware about what's decided vs. what's open.

### Weaknesses
- Player agency is too thin for 4+ hour sessions. One lever (tableau composition) is not enough.
- The lose conditions being post-MVP removes the game's strategic core from the MVP. This is backwards.
- The resource system being post-MVP but settlement behaviors depending on resources creates a major specification gap.
- Too many open questions are load-bearing for production to begin without addressing them.
- The learning curve has no mitigation strategy.
- There is no event/notification system, which means the player has no way to know what's happening in the simulation beyond staring at the map.

### Verdict: Close to Production-Ready, Not There Yet

The architectural design is strong. The system interactions are well-thought-out. The altitude model is genuinely innovative. But several critical gaps need filling before production:

1. Resolve the entity kind list (especially culture)
2. Define initial world seeding
3. Specify the MVP resource approximation
4. Design a minimal event system
5. Promote lose condition metrics to MVP (dashboard only, no game-ending thresholds)
6. Create a UI layout wireframe
7. Address player agency (consider promoting intervention cards, or adding card scoping)
8. Fix the numbering inconsistencies (minor but will cause confusion)

Once these are addressed, the design is ready for Phase 1 production.
