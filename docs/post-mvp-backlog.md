# Post-MVP Backlog

Ideas discussed and deferred during design. Organized by system area. None of these are committed -- they are captured so they aren't lost.

---

## Card System Extensions

### Low-Altitude Override Cards

Cards that temporarily modify low-altitude (always-on) entity behaviors. These break the clean altitude separation deliberately -- divine intervention that changes the physics themselves.

**Examples (across entity kinds):**
- "Fortify Borders" -- nation border tension is frozen or reduced for N ticks. Prevents escalation but also prevents natural de-escalation. Pressure builds underneath without an outlet.
- "Cultural Anchor" -- a culture's cohesion is locked high for N ticks. Prevents fragmentation but also prevents natural adaptation.
- "Accelerate Growth" -- settlement population attraction multiplied for N ticks. Rapid urbanization, but may overshoot carrying capacity.
- "Freeze Trade" -- goods flow between settlements halted for N ticks. Settlements rely on stockpiles. Tests economic resilience.
- "Preserve Artifact" -- artifact degradation paused for N ticks. Keeps a powerful artifact in play longer but costs a card slot.
- "Suppress Hunger" -- entities don't consume food for N ticks. Removes hunger-driven movement, letting mobile entities stay where they are. Trade-off: entities stop seeking food, so they're suddenly starving when the effect expires.
- "Extended Lifespan" -- mortal entities age slower for N ticks. Keeps valuable leaders alive longer but blocks succession.
- "Faction Truce" -- internal faction politics within a nation are suppressed for N ticks. Stability is artificially maintained. When the truce expires, pent-up political pressure releases at once.

**Design considerations:**
- These should be costly (high mana, or intervention cards that are one-shot)
- Duration should be visible and tracked -- the player knows when the override expires
- Expiration should have consequences -- suppressed dynamics snap back, often harder than if left alone
- These create interesting "what happens when the miracle ends" dynamics
- Override cards should exist across all entity kinds, not just for heroes/NPCs

### Hidden Event Cards

Cards that are placed on the tableau with hidden preconditions. The player knows the card exists and that it has a trigger, but not exactly when it will fire.

**Examples:**
- "The Great Plague" -- fires when population density and trade connectivity cross an unknown threshold. The player knows plague is in their tableau but not when it will strike.
- "Prophetic Birth" -- fires when cultural and mystical conditions align. A legendary hero emerges.
- "Volcanic Eruption" -- fires based on geological pressure (a new pressure type). Destroys settlements in a region but creates fertile land.

**Design considerations:**
- The player CHOSE to put the card in their tableau, so they accepted the risk
- The hidden preconditions should be discoverable through play over multiple runs (meta-knowledge)
- These add controlled unpredictability without violating the "everything visible" principle -- the card itself is visible, only the timing is hidden

### Weather Cards (System Cards)

Weather events as simulation system cards. These are mid-altitude: they modify low-altitude resource production, movement, and comfort.

- "Monsoon Season" -- periodic heavy rain increases fertility in affected regions but reduces movement speed and increases flood risk
- "Drought" -- reduces regional fertility and water availability. Triggers population migration pressure.
- "Harsh Winter" -- reduces food production, increases resource consumption, slows movement. Tests settlement stockpiles.
- "Trade Winds" -- increases maritime trade flow speed and volume. Benefits coastal settlements.

### Intervention Cards (Hand Cards)

One-shot cards kept in a hand, played for immediate divine effect.

- "Smite" -- damage an entity directly
- "Divine Inspiration" -- boost an entity's prominence and capabilities
- "Earthquake" -- reshape geography, destroy settlements, create refugees
- "Miraculous Recovery" -- cure plague, restore an entity, repair an artifact
- "Prophetic Vision" -- create an occurrence that drives entity behavior across kinds
- "Sever Alliance" -- dissolve a relationship
- "Bounty of the Harvest" -- regional prosperity boost

**Open questions:** draw mechanics, hand size limits, how intervention cards are earned/drawn, mana cost vs separate resource.

---

## Favored Nation Mode

### Full Patron God Implementation

- Mode selection at run start (neutral vs patron)
- Card pool modification (neutral-only cards removed, patron-only cards added)
- Patron-only cards: Divine Champion, Covenant, Sacred Ground, Prophecy, Chosen People, etc.
- Neutral-only cards: Cosmic Equilibrium, Primordial Chaos, Impartial Observer, World Seed
- Simulation detail increase for favored nation (higher tick frequency for its entities, more detailed events)
- Increased event frequency and detail around favored nation (more camera focus, more metrics -- NOT a hidden storyteller/director)
- UI telescoping (persistent sidebar, map centering, nation detail view)

### Win Conditions (Patron Mode)

- Run-specific goals generated from favored nation state
- Examples: Ascendant Empire, Golden Age, Cultural Hegemon, Legendary Dynasty, David's Triumph, Survivor
- Pick 1-2 from 3-4 generated options at declaration time

### Favored Nation Switching

- Mid-run switching with "Forsaken" debuff on abandoned nation
- Cooldown before new declaration
- Card loss and re-earning through new relationship

### Alternative Patronage Targets

- Favored culture (more resilient than nation, survives political upheaval)
- Favored hero (most personal, most fragile, ends when hero dies)

---

## Entity Kind Extensions

### Occurrence Depth

- More detailed occurrence entities with relational links
- Occurrences as graph entities that all entity kinds can reference and respond to
- "Legacy" occurrences that persist across eras and influence future events

---

## Simulation Depth

### Multi-Resource Economy

- Expand beyond food/materials to include: trade goods, luxury goods, strategic resources, knowledge
- Resource-specific production chains (iron ore -> weapons, grain -> bread)
- Regional resource specialization driving trade necessity

### Historical Event Modifiers on Relationships

- Past conflicts create lasting negative relationship modifiers
- Past cooperation creates lasting positive modifiers
- Shared history as a relationship factor

### Detailed Entity Needs and Personality

- Expand entity needs beyond basics: ideology, loyalty, social needs, spiritual needs (applicable to heroes, factions, and potentially nations)
- Personality traits per entity kind that weight behavioral priorities differently
- Inter-entity relationships beyond diplomatic: rivalry, mentorship, dependency (across all kinds, not just heroes)

### Culture Innovation System

- Cultures develop innovations over time
- Innovations spread through contact networks (cultural proximity, trade)
- Innovation as a culture attribute that affects settlement development and military capability

### Religion as Entity Kind

- Separate from culture (cultures can share a religion, religions can span cultures)
- Religious spread mechanics (low-altitude: influence radiation. Mid-altitude cards: conversion, reformation, schism)
- Religious sites, pilgrimages, holy wars

---

## Map and Visual

### Full Visual Bulk Flow

- Animated population movement along roads between settlements
- Animated trade goods movement along trade routes
- Visual weather effects on the map
- Animated cultural influence spread (color gradients shifting over time)

### Map Overlays (Extended)

- Military tension overlay
- Economic prosperity overlay
- Cultural influence overlay
- Population density overlay
- Resource availability overlay
- Relationship network overlay (lines between allied/rival entities)
- Pressure heatmaps

---

## Meta-Progression

### Milestone System

- Specific simulation outcomes that unlock new cards
- Cross-run tracking of milestones achieved
- Thematic unlock triggers ("witnessed monotheism" -> unlocks religious cards)
- Failure-specific unlocks (extinction run unlocks survival cards)

### Run Statistics

- Persistent cross-run statistics (total civilizations witnessed, entities created, cultures merged, artifacts forged, etc.)
- Personal bests per metric
- Run comparison data

### Deck Building Refinements

- Pre-run deck construction with unlocked card pool
- Mid-run deck modification mechanics (add/remove cards at era transitions, or through in-game events)
- Deck size limits and composition rules
- Card upgrade/evolution system (cards that improve through use)

---

## Quality of Life

### Branched History / What-If Timelines

The ability to create a save point at a pivotal moment and branch the simulation into two divergent timelines. The player can swap back and forth between branches to explore "what if I had played a different card here?" or "what if this nation had survived?"

- Create branch points manually or at significant events
- Multiple active branches from the same save
- Compare branch outcomes side by side
- Way post-MVP -- requires save/load maturity and UI for timeline management

### Speed Options

- More speed levels (1x, 2x, 4x, 8x, max)
- "Skip to next event" option
- "Run until condition" automation (run until a pressure threshold is reached)

### Entity Search and Filter

- Search entities by name, kind, attribute range
- Filter map display by entity kind
- Bookmark entities for quick access

### Tutorial / Onboarding

- Guided first run introducing low-altitude systems
- Incremental card introduction (start with 1-2 cards, unlock more as the player demonstrates understanding)
- Tooltip system explaining what each card does and what conditions it responds to

---

## Planner Extensions

### Support-Goal Generalization

Today `GoalGenerator.BuildBestPlanForBlocker` only creates sub-goals when a precondition is **hard-blocked**. Once every precondition passes, the planner commits to the immediate action even if the process would be substantially more productive in a better context.

The existing `SupportRules` concept is attached to specific processes (settlement_grow's "food ≥ 4 helps") but is narrow and hand-authored. Generalize it so any process can declare **"would be better if X"** conditions (target stock ≥ threshold, pressure ≤ threshold, relationship strength ≥ threshold, etc.) and the planner automatically surfaces processes that can improve that state as *support sub-goals* with their own urgency scaled by how much they'd lift the parent's reward.

**What this unlocks:**
- Factions naturally interleave building-up NPCs (distribute food, protect) with extracting from them (starve, extort) — pastoral / parasitic cycles rather than pure extraction.
- Regions pre-seed pristineness before settlement growth hits the threshold where it becomes relevant.
- Settlements invest in sanitation now because sanctify scales better with it later.
- Reads as long-form strategy rather than hammering.

**Design questions:**
- How does support urgency get scored against direct-action urgency without the planner vanishing into unbounded support chains?
- Depth limits (current `MaxPlanningDepth` may need revision).
- How to distinguish "support would help" from "support is actually useful given current world state" — prevent pathological cases where every process projects support sub-goals on every tick.
- Should support sub-goals use the same expansion rules as blocker sub-goals, or a different scoring regime?

**Prerequisites:**
- Transfer conservation (done — #26) so support state measurements are trustworthy.
- Recency decay (done — #27) so hammering-plus-support doesn't become a super-hammer.

**Why deferred:** biggest structural change to the planner yet. Lands after we have more behavioral data from the current generator + decay combination to know what scoring shapes are needed.

---

## Architectural Extensions

### Mod Support

- Expose card definitions as data files (already declarative by design)
- Allow custom entity kind extensions
- Map editor for custom maps

### Web Deployment

- If using Godot: GDScript variant for web export (C# doesn't export to web)
- If pursuing web: consider a lite/demo version

### Performance Optimization

- Entity graph query caching
- Bulk flow computation optimization (spatial hashing, level-of-detail)
- Large map support (chunking, LOD rendering)

### Testing Infrastructure

- Headless simulation mode (run without rendering for automated testing)
- Simulation replay from saved state
- Card balance testing tools (run N simulations with different tableaux, compare outcomes)
