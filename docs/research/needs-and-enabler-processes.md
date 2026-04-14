# Needs, Enablers, and Process Selection Research

Research into how simulation games handle need-driven behavior, indirect goal satisfaction, and enabling actions — specifically the problem of "move serves all needs as an intermediary step."

---

## The Core Problem

An NPC has hunger=80. Two processes compete: `eat` (directly reduces hunger, but requires food at current location) and `move` (doesn't reduce hunger at all, but gets the NPC to where food is). How should move score? It doesn't satisfy the need — it enables another process to satisfy it.

This is the **enabler problem**: some actions don't directly satisfy needs but create conditions for other actions that do. The scoring system must account for indirect value without hardcoding which actions enable which other actions.

---

## The Sims: Motive Advertising (Foundational Model)

The Sims (Maxis, 2000) pioneered the approach most relevant to our architecture.

### How It Works

1. **Needs decay over time.** Hunger, bladder, energy, fun, social, comfort, hygiene, room — each decays at its own rate toward zero. Low need levels produce mood penalties.

2. **Objects advertise what they satisfy.** Every interactable object contains metadata: a fridge advertises "+40 hunger if you eat from me", a bed advertises "+60 energy if you sleep in me", a toilet advertises "+20 bladder if you use me, +5 room if you clean me." An object can advertise multiple interactions with different motive contributions.

3. **Sims weight advertisements by current need level.** A starving Sim multiplies the fridge's hunger advertisement dramatically upward. A well-fed Sim multiplies it toward zero. The weighting uses **non-linear curves** — physiological needs (hunger, bladder, energy) use steep curves where severe deficits produce extremely high multiplied scores. Leisure needs (fun, social) use shallower curves.

4. **Distance attenuates the score.** Nearby objects are more tempting than distant ones. The Sim multiplies the weighted score by an inverse-distance factor, making closer satisfiers preferable.

5. **Sims pick from the top-scoring interactions with randomization.** Not the absolute best — one of the top few, chosen randomly. Prevents robotic optimization behavior.

### Key Insight: Objects Are the Intelligence

The Sim itself has no "seek food" behavior. It doesn't know what a fridge is or that food reduces hunger. The **fridge** knows what it offers. The Sim just evaluates all advertisements, weights them by current needs, and picks the best nearby option. Adding a new need-satisfying object requires no AI changes — just adding advertisement data to the object.

**This is exactly our provider/consumer model.** Entities on the graph advertise what they provide (via their attributes and resources). Consumers query for what they need. No coupling.

### The Distance Factor IS Movement

In The Sims, movement isn't a separate decision. When a Sim picks "eat from fridge," it automatically pathfinds to the fridge and then eats. Movement is implicit in choosing a distant object — the distance penalty makes nearby options score higher, but a sufficiently hungry Sim will walk across the house for the fridge because the need multiplier overwhelms the distance penalty.

**This is the key architectural insight for our problem.** The Sims doesn't have a "move" action that competes with "eat." It has "eat from fridge A" and "eat from fridge B" where the scores include distance. The Sim walks to the chosen fridge as a side effect.

---

## RimWorld: Priority Tiers with Need Thresholds

RimWorld uses a fundamentally different model: strict priority ordering, not utility scoring.

### How It Works

1. **Needs have thresholds.** Hunger below 30% triggers "must eat" override. Rest below 30% triggers "must sleep." Recreation below 35% triggers "must recreate."

2. **ThinkTree evaluation is priority-ordered.** The pawn evaluates a tree of ThinkNodes top-down. Higher nodes (survival needs) preempt lower nodes (work). Within a tier, left-to-right ordering breaks ties.

3. **Job selection within a tier.** When "must eat" triggers, the pawn looks for the food that grants the most mood, then picks the closest among equal-mood options.

4. **Work is separate from needs.** Work priorities (1-4 scale, manual assignment) only matter when no need threshold is breached. When a need is urgent, work priorities are ignored.

5. **Movement is implicit.** Like The Sims, RimWorld doesn't have a "move" job that competes with "eat." When the pawn decides to eat, it pathfinds to the food and eats. Movement is part of the job, not a separate decision.

### Key Insight: Threshold Override, Not Utility Competition

RimWorld doesn't ask "how much does this pawn want to eat vs. move vs. work?" It asks "is this pawn below the eat threshold?" If yes, eat overrides everything. If no, do work. There's no scoring competition between needs and enabling actions.

**Not directly applicable to our utility AI model**, but the threshold concept is useful: below a certain need level, the entity's behavior should be dominated by that need. Above it, other considerations matter more.

---

## Dwarf Fortress: Need Satisfaction with Labor Filters

Dwarf Fortress uses a hybrid: needs as periodically evaluated priorities, filtered through labor assignments.

### How It Works

1. **Needs accumulate over time.** Pray, socialize, admire art, train martial skills, etc. Two priority levels: low (interruptible by work) and high (overrides work).

2. **Job selection filters through labor assignments.** A dwarf only performs jobs matching their enabled labors. If cooking labor is disabled, the dwarf never cooks regardless of fort needs.

3. **Focus attribute tracks overall need satisfaction.** Multiple unmet needs compound into low Focus, which affects work quality. The system doesn't optimize individual needs — it tracks aggregate satisfaction.

### Key Insight: Multiple Needs Don't Compete, They Compound

A dwarf doesn't choose between praying and socializing by scoring them. Instead, unmet needs stack penalties. The dwarf satisfies whichever need is available when it becomes high priority. The system avoids the "which need to satisfy" competition by making all unmet needs bad in aggregate.

---

## Utility AI Literature: Considerations and Distance

From Game AI Pro (Mark Lewis, "Choosing Effective Utility-Based Considerations"):

### How Distance Works in Utility Scoring

Distance is treated as a **consideration** — one factor among many in a multiplicative scoring formula. Each action's final score is the product of all its considerations:

```
score = consideration_1 * consideration_2 * ... * consideration_n
```

A distance consideration uses a response curve (often inverse linear or inverse squared) that produces a value between 0 and 1. Close targets → high value. Far targets → low value. But not zero — a very motivated actor will still choose a distant target because the need consideration overwhelms the distance penalty.

### Zero-Scoring as Disqualification

Any consideration that scores 0 kills the entire action (multiplicative identity). This is how preconditions work: "food available at location" is a consideration that scores 0 when false, making "eat" impossible. No special precondition system needed — it's just a consideration with a binary response curve.

### Key Insight: Enabling Actions as Composite Scores

The literature doesn't directly address enabling actions. The standard utility AI approach is: if you can't eat here, "eat" scores 0. If you need food, you need a separate "go find food" action. But that "go find food" action needs to know about food, which couples movement to specific needs.

The Sims' approach avoids this by making distance part of the eat score. You don't need a separate "go find food" — you just need "eat from [distant fridge]" to score high enough despite the distance penalty.

---

## Synthesis: What This Means For Our Architecture

### The Sims Model Is Closest to Our Design

Our provider/consumer model is architecturally identical to The Sims' advertisement system:
- Entities broadcast what they provide (attributes/resources = advertisements)
- Consumers weight providers by need urgency and distance
- Selection is weighted random from top candidates

### The Problem With Separate "Move" and "Eat"

The Sims and RimWorld both avoid having "move" as a separate action competing with need satisfaction. Movement is implicit — choosing a distant satisfier includes the walk as part of the action.

Our architecture separates them: `eat` (requires food at current hex) and `move` (steps one hex toward a target). This separation exists because our entities move **one hex per tick**, not instantly. A Sim teleports to the fridge in a few seconds. Our NPCs might take 10 ticks to walk to a settlement. So we can't bundle movement into eat — the walk IS the multi-tick gameplay.

### Three Possible Models for Our System

**Model A: The Sims Inline (score includes distance, no separate move)**

Each need-satisfying process scores against ALL potential targets, including distant ones. "Eat at settlement A" scores differently from "eat at settlement B" based on distance and food amount. The highest-scoring target is chosen, and the entity moves one hex per tick toward it, remembering the target across ticks.

Problem: requires process persistence across ticks (the entity "commits" to walking to settlement A for N ticks). Our current model is stateless — one process selected per tick.

**Model B: Enabler Scoring (move scores based on unmet needs)**

Move's score is derived from need urgency: if the entity has unmet needs that can't be satisfied at the current location, move scores high. If all needs can be satisfied here, move scores low. Move doesn't know WHAT the need is — it just knows the entity needs to be somewhere else.

Concrete formula: `move_score = max_urgency * (1 - local_satisfaction)`, where `max_urgency` is the dominant need's urgency and `local_satisfaction` is whether any process can satisfy it here (binary: 0 or 1).

Problem: move still doesn't know WHERE to go. Target resolution is separate.

**Model C: Composite — Processes Score Against Providers (Our Current Direction)**

Each process conceptually scores against ALL reachable providers. "Eat" scores `hunger_urgency * food_amount * inverse_distance` for each settlement with food. The winning process-provider pair determines both what to do and where to go. If the winning pair is eat-at-distant-settlement, the entity moves toward it this tick and will eat when it arrives.

This is closest to The Sims model but adapted for hex-by-hex movement. It requires:
1. All need-satisfying processes evaluate against providers (not just eat, all of them)
2. The winning process-provider pair persists as a "current goal" until satisfied or preempted
3. Each tick, the entity either executes the process (if at the provider) or steps toward the provider
4. A new evaluation each tick can preempt the current goal if urgency changes

This subsumes both eat and move into a single evaluation. There is no separate "move" process — movement is what happens when the winning process's target is not at the current location.

---

## Recommendation

**Model C is the most architecturally clean** and aligns with both The Sims' proven design and our provider/consumer model. However, it requires a concept our system doesn't have: **process-provider pair persistence across ticks** (a "current goal" that the entity works toward until it arrives).

This is a meaningful architectural addition. The current system is stateless per tick — each tick evaluates fresh. Adding goal persistence means entities remember what they're doing and why, which enables:
- Multi-tick journeys toward a target
- Preemption (a more urgent need overrides the current goal)
- Commitment (slight hysteresis — don't abandon a goal for marginal improvements)
- Visibility ("this NPC is heading to Frosthaven to eat" — inspectable state)

The alternative (Model B) is simpler — keep move as a separate process, score it based on unmet need urgency — but less correct. It separates "what" from "where" in a way that creates the scoring problems we've already identified.

---

## IMPORTANT: Where This Research Misleads

The Sims model is spatial-first: objects advertise at locations, Sims walk to them, distance attenuates scores. This is appropriate for The Sims because Sims physically move through a house to interact with objects.

**Our game is NOT primarily spatial for most interactions.** Settlements interact through attribute comparisons (fame, ideology, population), not physical proximity. A settlement's propaganda affects another settlement based on ideology distance on a semantic plane, not hex distance on the map. Distance MAY be a factor in some process expressions but it is NOT a structural assumption of the engine.

The provider/consumer model from this research is correct: entities broadcast attributes, processes query for matching providers. But the SCORING of providers must be process-defined (via expressions), not engine-hardcoded (via distance). Each process declares how to evaluate candidate providers based on whatever attributes matter for that process.

Do NOT use this research to justify distance-based scoring defaults.

---

## Sources

- [The Genius AI Behind The Sims - Mark Brown (GMTK)](https://gmtk.substack.com/p/the-genius-ai-behind-the-sims)
- [Needs-Based AI - Robert Zubek (Game Programming Gems 8)](https://zubek.net/robert/publications/Needs-based-AI-draft.pdf)
- [How Pawns Think - RimWorld Mod Guide](https://github.com/roxxploxx/RimWorldModGuide/wiki/SHORTTUTORIAL:-How-Pawns-Think)
- [RimWorld AI Tutorial - CBornholdt](https://github.com/CBornholdt/RimWorld-AI-Tutorial/wiki/Part-1---Introduction)
- [RimWorld Menus Wiki (need thresholds)](https://rimworldwiki.com/wiki/Menus)
- [Choosing Effective Utility-Based Considerations - Mike Lewis (Game AI Pro 3)](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter13_Choosing_Effective_Utility-Based_Considerations.pdf)
- [Utility System - Wikipedia](https://en.wikipedia.org/wiki/Utility_system)
