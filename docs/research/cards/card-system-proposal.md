# Card System Design Proposal

Synthesis of two rounds of research into a concrete design direction for combining roguelite card mechanics with the civilization simulation. This is a set of ideas to explore, not a final design.

---

## The Core Idea

Each declarative simulation component -- every generator template, system definition, and action definition -- is a card. The player's active tableau determines which components are running in the simulation. The card IS the simulation component. Playing "Cultural Diffusion" into your tableau activates the cultural diffusion system. Removing it deactivates it. The player shapes civilizations not by commanding individual actions but by configuring the persistent systems that generate those actions.

This creates a player who is a **legislator of reality**: not a ruler executing orders, but a god deciding which rules of the world are in effect.

---

## Card Types

### Generator Cards (Creation Valves)

Generator cards activate creation templates. While in the tableau, new entities can be created when preconditions are met.

**How they work:**
- Each generator has preconditions checked each generation tick (e.g., "Found Settlement" requires a region with sufficient population and no existing settlement within N distance)
- When preconditions are met, the generator creates a batch of pre-connected entities (settlement + founding population + initial relationships)
- Generation rate is influenced by the card's weight and world state
- The card is NOT consumed -- it remains in the tableau, continuing to generate as preconditions are met

**What makes them interesting:**
- Early game, generators are essential (the world needs entities to simulate)
- Mid/late game, they become double-edged: "Heroic Emergence" keeps spawning heroes, which is exciting but creates more actors competing for prominence and potentially destabilizing established power structures
- Generators interact with other cards: "Found Settlement" + "Migration Waves" (system) creates rapid frontier expansion because migration moves population into empty regions, satisfying founding preconditions

**Examples:**
- "The Crucible of Nations" -- generates new nation entities in regions with sufficient population density and cultural distinctiveness. More nations = more geopolitical complexity = more instability.
- "Heroic Emergence" -- generates hero entities in settlements with high prominence or during era transitions. Heroes drive narrative but create unpredictable actors.
- "Artifact Forge" -- generates artifacts in settlements with high development and skilled heroes. Artifacts amplify their holders but create targets for theft and war.
- "Schismatic Thought" -- generates new culture/religion variants from existing ones. Creates diversity but fragments cultural unity.

### System Cards (World Rules)

System cards activate ongoing world processes. While in the tableau, a force of nature operates on the simulation.

**How they work:**
- Each system runs on system ticks (frequency varies by system)
- Systems read world state, evaluate conditions, and modify continuous variables or create/destroy relationships
- Systems interact with each other: multiple active systems can compound, counterbalance, or produce emergent behaviors
- Systems are always double-edged: they affect ALL entities, not just ones the player favors

**What makes them interesting:**
- Every system card cuts both ways. "Disease Spread" weakens all dense populations, including your favored civilization. "Trade Routes" spreads prosperity but also spreads disease and cultural influence. The decision is never "is this good?" but "given the current world state, does this help or hurt what I care about?"
- System interactions create emergent complexity: "Cultural Diffusion" + "Trade Routes" = cultures spread along trade corridors. "Disease Spread" + "Dense Urbanization" = devastating plagues in cities. The tableau is combinatorial.
- Systems can become irrelevant or dangerous as world state shifts, forcing tableau adaptation

**Examples:**
- "Cultural Diffusion" -- cultures spread to adjacent regions and along trade connections. Dominant cultures spread fastest. Good for a player nurturing a large culture; dangerous if their favored culture is small and surrounded.
- "Plague Winds" -- diseases emerge in dense populations, spread along trade routes. Counterbalanced by "Medical Tradition" (another system card). Devastating when combined with urbanization.
- "Prominence Decay" -- all entities slowly lose prominence unless actively maintained through actions and events. Keeps the world dynamic; prevents permanent heroes. Always active might be desirable, but it occupies a slot.
- "Economic Pressure" -- supply and demand dynamics between connected settlements. Creates trade prosperity but also creates dependency and vulnerability to disruption.
- "Technological Innovation" -- cultures develop innovations that spread through contact networks. Accelerates the pace of change; can make one culture dominant if it innovates faster.

### Action Cards (Possibility Space Controllers)

Action cards determine which behaviors are available to simulated actors. While in the tableau, actors CAN perform the corresponding action. Without it, the action doesn't exist in the simulation.

**How they work:**
- Each action defines: which actor kinds can perform it, preconditions, success/failure probabilities, and effects on continuous variables and relationships
- Actors evaluate available actions each actor tick and select based on their state, personality, and situation
- The player doesn't perform the action -- they make it possible. AI actors decide when and how to use it.
- Actions are the primary source of prominence change

**What makes them interesting:**
- Enabling an action enables it for ALL actors, including rivals. "Forge Alliance" lets your favored faction gain allies, but also lets enemies form coalitions.
- Disabling an action constrains the entire simulation. Removing "Declare War" creates a peaceful world -- but also prevents your favored nation from conquering a weakened rival.
- The player controls the possibility space, not the outcomes. This is the god-perspective made mechanical.

**Examples:**
- "Forge Alliance" -- factions and nations can form alliances based on shared enemies, cultural compatibility, and power balance. Creates power blocs that stabilize or destabilize.
- "Espionage Networks" -- heroes and factions can conduct intelligence operations, discovering hidden relationships and states. Enables intrigue but also enables betrayal.
- "Diplomatic Summit" -- nations can resolve disputes through negotiation rather than war. Creates peaceful resolutions but also enables manipulation.
- "Crusade / Holy War" -- religious cultures can wage wars of conversion. Enables dramatic expansion but at enormous cost in stability and prosperity.
- "Found Dynasty" -- heroes can establish hereditary rule, creating succession mechanics. Enables political continuity but also succession crises.

### Intervention Cards (Hand Cards / God Powers)

Not tableau cards. Drawn or earned during play, used once for immediate effect.

**How they work:**
- Kept in a hand (limited hand size)
- Played for an immediate, powerful, one-time effect on the simulation
- Can target specific entities, regions, or the whole world
- Some are responses to crises; others are proactive shaping

**What makes them interesting:**
- The only cards where the player acts directly rather than configuring rules
- Powerful but scarce -- force triage decisions
- Can compensate for tableau gaps (no "Disease Quarantine" system card? An intervention card might cure a specific plague)

**Examples:**
- "Divine Inspiration" -- instantly boost a hero's prominence and skill. Create a great leader.
- "Earthquake" -- destroy settlements in a region, reshape geography, create refugees.
- "Prophetic Vision" -- create an occurrence entity (prophecy) that draws heroes and factions toward a goal.
- "Sever Alliance" -- immediately dissolve a relationship between entities.
- "Bounty of the Harvest" -- instantly boost prosperity of a region for several ticks.

---

## The Tableau

### Structure

The player has a single global tableau with a limited number of slots (initial thinking: 7-9 slots). Each slot holds one card of any type (generator, system, or action). The mix of card types in the tableau IS the player's strategy.

A tableau heavy on generators = a world that keeps creating new things (chaotic, expansive, unpredictable).
A tableau heavy on systems = a world governed by strong forces (structured, pressured, emergent dynamics).
A tableau heavy on actions = a world where actors have many options (complex politics, diverse behaviors).

### Slot Limits as Core Tension

The slot limit is the game's primary constraint. With 7 slots and potentially 30+ available cards, every inclusion means multiple exclusions. The question is never "is this card good?" but "is this card better than what I'd have to remove?"

This prevents "everything on" dominance. You CANNOT have all generators, all systems, and all actions active simultaneously. You must prioritize.

### Card Swapping

Swapping a card in or out of the tableau should feel weighty -- it is a major civilizational transition. Possibilities:
- **Cooldown**: swapped-out cards can't be re-activated for several ticks (prevents rapid cycling)
- **Transition cost**: swapping causes temporary disruption (removing "Trade Routes" doesn't instantly stop all trade; there's a decay period)
- **Momentum**: cards that have been active longer produce stronger effects (incentivizes commitment)
- **Swap opportunities**: the player can only swap during specific moments (era transitions, after major events, at fixed intervals) rather than freely at any time

The exact mechanism needs playtesting. The goal: make swaps feel like significant decisions with lasting consequences, not minute-to-minute optimization.

### Card Interactions and Synergies

Cards should interact through a tag/keyword system:

- Cards tagged "connectivity" (Trade Routes, Cultural Diffusion, Disease Spread, Migration Waves) amplify each other -- anything that spreads along connections spreads faster when more connection-related systems are active
- Cards tagged "conflict" (Declare War, Forge Alliance, Espionage Networks) create complex political dynamics together
- Cards tagged "creation" (generators) compete for the same world resources -- too many active generators may exhaust population and territory

Some cards should actively conflict:
- "Isolationist Policy" (reduces inter-entity interaction) contradicts "Trade Routes" (increases it)
- "Cultural Purity" (cultures resist change) contradicts "Cultural Diffusion" (cultures spread and merge)
- Conflicting cards in the same tableau might cancel each other's effects or produce chaotic results

---

## Where Tension Comes From

### The Autonomous Simulation Creates Visible Problems

Following the Spirit Island model: the simulation runs whether the player does anything or not. Nations expand, heroes act, resources deplete, cultures shift. The player's tableau choices determine HOW these things happen, but they happen regardless.

The player watches the simulation and sees problems forming: a dominant culture is homogenizing the continent (maybe they should remove "Cultural Diffusion"). A power vacuum in the south is breeding instability (maybe they need "Crucible of Nations" to spawn a stabilizing polity). A plague is spreading along trade routes (do they remove "Trade Routes" to slow it, sacrificing economic growth?).

The simulation surfaces dilemmas. The cards are the player's response. But every response has side effects.

### Triage Under Slot Constraints

The player can see multiple problems but cannot address all of them. With 7 tableau slots, fixing the plague means giving up something else. This is the Pandemic principle: forced triage creates drama. The player chooses what to save and what to let burn.

### Phase Transitions Force Adaptation

The world state changes, making the current tableau suboptimal:

- **Genesis Phase** (early): generators are critical. The world needs entities. Systems and actions have nothing to work with.
- **Interaction Phase** (mid): systems become critical. Entities need to interact. Generator cards can be swapped out for systems.
- **Competition Phase** (late): actions matter most. Established entities compete. Fine-tuning the possibility space steers outcomes.
- **Crisis Phase** (disruption): specific response cards needed. "Disease Quarantine" is wasteful in peacetime, essential in pandemic. The insurance problem: dedicate a slot to crisis response or run lean?

These aren't scripted phases -- they emerge from the simulation. The player must read the world state and adapt.

### Asymmetric Card Effects

Each card affects different entities differently. "Cultural Diffusion" benefits the dominant culture and threatens small ones. "Disease Spread" devastates dense urban civilizations and barely touches nomadic ones. "Forge Alliance" empowers diplomatically skilled nations and isolates aggressive ones.

The player has preferences (even without controlling a specific nation, they develop favorites and goals) and must weigh which entities benefit from each card.

### The Simulation Breaks Your Cards

The simulation can invalidate tableau cards by changing the world state they depend on:
- "Fertile River Valley" becomes irrelevant if the river region is conquered and its population displaced
- "Religious Unity" loses effect if a schism splits the dominant religion
- "Trade Routes" produces different results when a plague shuts down ports

This forces reactive adaptation -- the player's carefully constructed tableau is disrupted by the very simulation it was shaping.

---

## Card Availability: World-State-Driven

Cards don't come from a random deck. They become available because the simulation reached a state that makes them relevant.

**How it works:**
- Each card has availability preconditions tied to world state
- "Naval Supremacy" (action) only appears when coastal settlements and navies exist
- "Monotheistic Reform" (generator) only appears when a polytheistic religion achieves sufficient influence
- "Plague Winds" (system) only appears when population density crosses a threshold
- When preconditions are met, the card enters a "draft pool" -- the player is offered 2-3 eligible cards and picks one

**Why this works:**
- Discovery comes from the simulation, not from random draws
- Available cards feel responsive to the world the player is building
- The feedback loop: simulation state generates card options -> player's card choices reshape simulation -> new world state generates new options
- Avoids both "random slot machine" (pure draw) and "solved puzzle" (always available)

**The draft mechanism:**
When the simulation surfaces new eligible cards (periodically, or at milestone moments), the player chooses from a small set. The unchosen cards go back to the eligibility pool (they may be offered again later). This preserves the agonizing "I want both" feeling of Terraforming Mars's card draft.

---

## Meta-Progression

Across simulation runs, the player unlocks new cards into the eligibility pool.

### Unlock Triggers

Unlocks are tied to simulation milestones the player witnesses or causes:
- "You witnessed a civilization develop monotheism" -> "Prophetic Tradition" generator unlocked
- "A hero reached mythic prominence and died" -> "Legendary Apotheosis" system unlocked (mythic heroes become cultural touchstones)
- "Three distinct cultures survived to the final era" -> "Cultural Mosaic" system unlocked (diversity bonuses)
- "A nation collapsed from internal instability" -> "Constitutional Reform" action unlocked

### Pool Expansion, Not Power Escalation

New cards expand the possibility space of future runs without making them easier. More cards means more variety in what's offered (different world states surface different subsets), not more cards active at once (slot limit stays the same).

To prevent pool dilution:
- Weight recently unlocked cards higher for the first few runs
- Allow the player to "pin" a few cards that are always eligible regardless of world state
- Group cards into thematic sets that are unlocked together (unlocking a "Maritime" milestone unlocks 3-4 naval-themed cards)

### The Milestone System

Milestones are specific simulation outcomes the player is tracking. Each run has a set of available milestones. Completing a milestone:
1. Unlocks new cards for future runs
2. May provide an immediate in-run reward (a powerful intervention card, or a free tableau swap)
3. Advances a meta-narrative (the player is a god learning about what civilizations can do)

Milestones create goals for each run beyond "watch the simulation." They give the player something to steer toward, creating investment without faction alignment.

---

## The Player's Role: God as Game Designer

The deepest framing of this system: the player is not playing the simulation. The player is **designing** the simulation, then watching it play out, then redesigning it in response.

Each tableau configuration is a hypothesis: "I think these 7 cards will produce an interesting world." The simulation is the experiment. The results may confirm or deny the hypothesis. The player adjusts and tries again.

This is the same satisfaction loop as Dwarf Fortress world generation, Factorio factory design, or Minecraft redstone engineering: build a system, observe its behavior, iterate. The card mechanic gives this loop structure (limited slots, forced choices, availability constraints) that pure sandbox lacks.

Investment comes from authorship. The player cares about the simulation because it is an expression of their choices. "I chose Cultural Diffusion and Trade Routes, and THAT is why Keshian culture dominates the continent." The outcomes feel owned.

The meta-progression deepens this: over many runs, the player becomes a more experienced god with more tools and deeper understanding. The milestone system records what they've witnessed. The expanding card pool reflects their accumulated knowledge of what civilizations can do. Each new run starts with more sophisticated questions: not "what happens if I turn on Cultural Diffusion?" but "what happens if I combine Cultural Diffusion with Isolationist Policy and Plague Winds in a desert-dominated map?"

---

## Open Design Questions

These need playtesting and iteration, not more research:

1. **How many tableau slots?** 7 feels right as a starting point. Too few (4-5) and the player can't express strategy. Too many (12+) and the constraint is too loose. The number might also vary by difficulty or meta-progression.

2. **When can the player swap cards?** Options: freely any time (most agency, risk of min-maxing); only at era transitions (dramatic, weighty, but constraining); at fixed intervals (pacing control); any time but with a cooldown or cost (middle ground).

3. **How many cards in the full pool?** Enough for variety without overwhelming. Starting pool of ~20-25 cards across all three types feels manageable. Meta-progression could expand to 50-60 over many runs.

4. **How do intervention (hand) cards work?** Separate draw pile? Earned from milestones or events? Limited hand size? Energy/resource cost to play? This is a parallel design track.

5. **How fast does the simulation run relative to card decisions?** If the simulation runs too fast, card changes feel disconnected. If too slow, the game drags. The time-control model (pause, play, fast-forward) combined with "pause at significant events" is likely right.

6. **Balancing card power.** Every card must be double-edged and contextually valuable. If any card is always good or always bad regardless of world state, the system fails. This is the hardest design challenge and requires extensive playtesting.

7. **Visual representation of the tableau.** How does the player see their active cards alongside the map? A sidebar? A bottom panel? Cards overlaid on the map? The UI needs to communicate both "what's active" and "what each card is currently doing to the simulation."

8. **How granular are card effects?** "Cultural Diffusion" is a broad world rule. "Keshian Cultural Expansion" is targeted. The right granularity affects strategic depth: too broad feels impersonal; too narrow creates optimization overload.

9. **Can the simulation exist without any cards?** If the player removes all tableau cards, does the world freeze? Or does a baseline simulation continue? A baseline (entities exist, time passes, basic survival) with cards as enhancements/modifications feels more interesting than a world that literally stops.

10. **How does the card system interact with the storyteller/pacing layer?** Does the pacing layer influence which cards are offered? Does it adjust card effects during crisis moments? Or does the pacing layer operate independently?

---

## Exemplar Mapping

| Game Mechanic | Source Exemplar | How We Adapt It |
|---|---|---|
| Tableau with limited slots | Wingspan, Terraforming Mars | 7-9 slots for active generators/systems/actions |
| Every card cuts both ways | Spirit Island (powers help and hurt), Twilight Imperium (laws) | All cards have asymmetric effects on different entities |
| World-state-driven availability | Stellaris tech draws, Against the Storm biome-dependent blueprints | Cards appear when simulation preconditions are met |
| Draft from eligible set | Slay the Spire (pick 1 of 3), Against the Storm (pick 1 of 3) | When eligible cards appear, player picks from 2-3 |
| Autonomous simulation as tension | Spirit Island Invader deck, Against the Storm Impatience | Simulation runs regardless; cards shape how, not whether |
| Phase transitions force adaptation | Slay the Spire act structure, Against the Storm storm cycles | Genesis -> Interaction -> Competition -> Crisis |
| Cards as possibility-space control | Stellaris policies, Dwarf Fortress labor assignments | Action cards determine what actors CAN do |
| Meta-progression as pool expansion | Against the Storm Smoldering City, Slay the Spire unlocks | Milestone-driven card unlocks across runs |
| God perspective with objectives | Cultist Simulator, Reus, Crest | Player is cosmic force with milestone goals, not faction ruler |
| Card interactions through tags | Terraforming Mars tag synergies, Wingspan habitat chains | Tags like "connectivity," "conflict," "creation" drive interactions |
