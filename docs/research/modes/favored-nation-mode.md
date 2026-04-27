# Favored Nation Mode

The game supports two modes: **Neutral God** (default) and **Patron God** (favored nation). Both use the same simulation engine, the same map, the same entity graph. The difference is the player's relationship to the world and the card tools available.

This is not a difficulty setting. It is a fundamentally different play experience using the same systems.

---

## The Two Modes

### Neutral God (Default)

The player is a cosmic force with no faction alignment. They shape reality through a global card tableau that affects all civilizations equally (though with asymmetric effects -- Cultural Diffusion benefits dominant cultures, etc.).

**Emotional arc:** Authorship and discovery. The satisfaction of building a system, watching emergence, and iterating. The player is a game designer testing hypotheses about what rules produce interesting worlds. Investment comes from creation: "I built this world."

**Strategic focus:** Global patterns. Cultural balance, economic flows, systemic health, diversity maintenance. The player cares about the whole map equally.

**Lose conditions:** Extinction, stagnation, monoculture (all three threat tracks).

**No win conditions.** The game continues until a lose condition triggers or the player chooses to end the run. Success is measured through the chronicle, personal bests, and milestones.

### Patron God (Favored Nation)

The player declares alignment with a specific nation (or culture, or hero -- see below). This opens asymmetric cards targeting the favored entity, increases simulation detail around it, and adds win conditions.

**Emotional arc:** Investment and triumph (or tragedy). The satisfaction of coaching a team, guiding a civilization through crises, watching your chosen people rise (or fall heroically). Investment comes from identification: "This is MY civilization."

**Strategic focus:** The favored entity's trajectory. Its borders, heroes, rivals, internal politics. The rest of the world matters insofar as it affects the favored nation.

**Lose conditions:** The same three global threats (extinction, stagnation, monoculture) PLUS: the favored nation is destroyed or falls to "forgotten" prominence. The patron god has a personal stake that neutral god lacks.

**Win conditions:** Run-specific goals based on the favored entity's current state (see Win Conditions below).

---

## How Patron Mode Works

### Declaration

Patron mode is selected at run start. The player begins a run in neutral mode while the simulation generates initial entities (genesis phase). Once the world has populated sufficiently, the player can declare a favored nation from the existing entities. This means the player chooses their favorite based on what the simulation actually produced, not from a menu of pre-defined options.

Declaration can happen during any era, not just the first. A player might observe the world for a while, develop an organic preference, then formalize it.

### What Changes on Declaration

**Card pool shifts:**
- 3-5 neutral-only cards are removed from eligibility (cards that require cosmic detachment, like "Cosmic Equilibrium" or "Primordial Chaos")
- 5-8 patron-only cards are added to eligibility (targeted blessings, divine champions, prophecies -- see Patron Cards below)
- All shared cards (most generators and systems) remain unchanged

**Simulation detail increases** for the favored nation:
- Actor ticks run more frequently for its heroes (more actions, more events, more narrative)
- More detailed Occurrence entities are generated for its events
- Internal faction dynamics, character relationships, and economic details are surfaced at higher resolution
- Other nations stay at default simulation altitude

This creates a telescoping effect: the favored nation is simulated at RimWorld-like resolution (individual character detail) while the rest of the world stays at CK3-like resolution (high-altitude political/cultural dynamics).

**The storyteller refocuses:**
- More events centered on the favored nation
- Dramatic arc shaping: when stable, inject challenges; when in crisis, provide recovery windows
- The favored nation experiences a narrative (setup, rising tension, crisis, climax, resolution)
- Other nations experience the simulation normally

**UI telescopes:**
- The favored nation gets a persistent sidebar panel showing its focus hero, key metrics, and recent events
- The map defaults to centering on the favored nation's territory
- A dedicated "nation view" shows internal details: settlements, heroes, factions, cultural composition, economic state
- The event feed prioritizes events involving the favored nation

### The Protagonist Hero

When the player declares a favored nation, the simulation surfaces the nation's highest-prominence hero as a **focus character**. This is not a formal assignment -- it emerges from the prominence system. The focus hero is displayed in a persistent UI panel with expanded information.

**Mortality and succession:** The focus hero is mortal. When they die, the focus shifts to the next highest-prominence hero in the nation. The player must adapt to the successor, who may have different traits, skills, and relationships. This prevents the protagonist from becoming a static advantage and creates CK3-style succession drama.

**The player doesn't control the hero.** The hero acts according to the simulation's actor tick rules, taking available actions based on their state and personality. The player influences the hero's world through tableau cards and intervention cards, but the hero makes their own decisions. This creates the Crest dynamic: the god issues guidance (through tableau configuration), the follower interprets it.

### The Constraint Principle

Drawing from every god game researched (Black & White, Populous, Dominions), the patron relationship should constrain the player as much as it empowers them:

- **Empowerment:** Targeted cards that specifically benefit the favored nation. Increased narrative detail. Win conditions that provide direction.
- **Constraint:** Loss of neutral-only cards (cosmic detachment tools). The favored nation attracts more challenges (the storyteller tests them harder). The player's emotional investment creates vulnerability (watching your favored nation suffer feels worse than watching any nation suffer in neutral mode).

The choice between modes should feel like a genuine trade-off: patron mode gives focus and power at the cost of cosmic flexibility. Neutral mode gives breadth and detachment at the cost of personal investment.

---

## Patron-Only Cards

Cards available only when the player has declared a favored nation. These are targeted rather than global.

### Targeted Generators

- **"Chosen Hero"** -- When in tableau, the favored nation is more likely to produce exceptional heroes (higher initial stats, rarer subtypes). Other nations are unaffected.
- **"Sacred Architecture"** -- When in tableau, the favored nation's settlements develop faster (higher prosperity growth, more cultural output). Represents divine favor blessing their works.
- **"Prophetic Lineage"** -- When in tableau, the favored nation's heroes produce more capable successors. Dynasties become more prominent over generations.

### Targeted Systems

- **"Divine Covenant"** -- The favored nation gains a permanent stability bonus, but this stability bonus decreases if the nation acts against the god's values (defined by the player's tableau composition -- a tableau heavy on "connectivity" cards means the god values openness, and isolationist national actions violate the covenant).
- **"Sacred Ground"** -- A specific region is designated as holy to the favored culture. The region gains defense bonuses and cultural cohesion. But the holy land becomes a target for rivals and a source of religious conflict if contested.
- **"Providence"** -- The favored nation's heroes have a small success bonus on all actions. But the bonus creates resentment from other nations ("why do THEY always succeed?"), increasing diplomatic hostility.

### Targeted Actions

- **"Divine Champion"** -- The focus hero can be "anointed," gaining a temporary but significant boost to skill and prominence. Available once per era. The anointed hero attracts both followers and enemies.
- **"Revelation"** -- The player can create a specific Occurrence (a vision, a prophecy, a divine sign) that drives the favored nation's heroes toward a goal. The heroes interpret the revelation through their own traits and may act on it in unexpected ways (Crest's interpretation mechanic).
- **"Sanctuary"** -- The player can designate a settlement as divinely protected, preventing its destruction for a limited duration. The protection is visible to all nations, which may provoke jealousy or awe.

### Targeted Interventions (Hand Cards)

- **"Smite"** -- Directly damage an enemy of the favored nation. Powerful but visible -- other nations may fear or resent the divine intervention.
- **"Miraculous Recovery"** -- Restore a favored nation hero from near-death or cure a plague in favored territory.
- **"Divine Inspiration"** -- Grant a specific technology or innovation to the favored nation, bypassing normal diffusion mechanics.

---

## Neutral-Only Cards

Cards available only in neutral mode. These require cosmic detachment that is incompatible with faction alignment.

- **"Cosmic Equilibrium"** -- When in tableau, automatically dampens the power differential between the strongest and weakest nations. The strong lose a small amount of stability; the weak gain it. A patron god cannot use this because it would weaken their own nation if it's the strongest.
- **"Primordial Chaos"** -- When in tableau, periodically injects random large-scale disruptions (volcanic eruptions, great migrations, mysterious plagues) that prevent any single nation from dominating. A patron god wouldn't risk this hitting their nation.
- **"Impartial Observer"** -- When in tableau, the player gains detailed information about ALL nations simultaneously (normally, detailed info requires clicking into each nation individually). A patron god's attention is focused, not omniscient.
- **"World Seed"** -- When in tableau, new geographic features (resource deposits, fertile valleys, mountain passes) appear in unclaimed territory, creating opportunities for any nation. A patron god would want to direct these specifically.

---

## Win Conditions (Patron Mode Only)

Patron mode adds 2-3 win conditions per run, generated based on the favored nation's starting state and the world context. These are ambitious goals that require sustained effort across multiple eras.

### Examples

- **"Ascendant Empire"** -- The favored nation controls the majority of the map's settlements and has mythic prominence. The classic power fantasy.
- **"Golden Age"** -- The favored nation achieves maximum prosperity across all settlements simultaneously, with zero active wars, for a sustained duration. The peaceful utopia.
- **"Cultural Hegemon"** -- The favored nation's culture spreads to become the dominant culture across the majority of regions, without military conquest. The soft-power victory.
- **"Legendary Dynasty"** -- The favored nation's ruling dynasty survives for N generations, with at least one hero per generation reaching "renowned" or higher prominence. The CK3 fantasy.
- **"David's Triumph"** -- Starting as the smallest nation on the map, the favored nation achieves "renowned" prominence while remaining smaller than the largest nation. The underdog story.
- **"Survivor"** -- The favored nation survives to the final era while at least two other nations are destroyed. The endurance test.

Win conditions are selected at declaration time from a generated set (pick 1-2 from 3-4 options). This prevents the player from always pursuing the same strategy and ensures win conditions are contextually appropriate (you can't pick "David's Triumph" if your favored nation is already the largest).

Achieving a win condition ends the run with a victory summary, but the player can choose to continue playing in neutral mode (the patron relationship dissolves but the simulation continues). This lets the player experience the aftermath of their favored nation's triumph.

---

## Changing the Favored Nation

The player can change their favored nation mid-run. This is allowed but devastating -- a god abandoning their people.

### What Happens

**To the abandoned nation:**
- A "Forsaken" debuff: stability collapse, hero loyalty crisis, cultural despair
- The nation's heroes experience a loss of divine connection, potentially triggering a faith crisis or religious schism
- An Occurrence is generated: "The God of [Nation] turned away" -- this becomes part of the chronicle and affects the nation's history
- The debuffs decay over time (the nation recovers from divine abandonment, or it doesn't)

**To the player:**
- All patron-specific cards are immediately removed from the tableau (slots become empty)
- Patron-specific cards already earned are lost; new patron cards must be earned through the new relationship
- There is a cooldown before the player can declare a new favored nation (a few eras of mandatory neutral play)

**To the newly favored nation (when the cooldown ends):**
- "Divine Attention" buff: the nation's heroes sense a new divine presence, creating wonder and opportunity
- The storyteller begins ramping up narrative detail for the new nation
- New patron-specific cards begin appearing in the eligibility pool

### When Would a Player Do This?

- Their favored nation was destroyed despite their best efforts (the patron relationship survives destruction briefly, creating a "lost god" period before the player must find new followers)
- Their favored nation achieved its win condition and the player wants a new challenge in the same world
- A different nation has become more interesting through emergent events
- Strategic pivot: the favored nation's situation is hopeless, and the player decides to invest elsewhere

The cost should be high enough that it's a desperate pivot or a deliberate narrative choice, not routine optimization.

---

## Favoring Other Entity Types

The core design assumes favoring a nation, but the system extends to other entity types:

### Favored Culture (instead of nation)

The player aligns with a culture rather than a political entity. This is more abstract: cultures span nations, persist through political upheaval, and can split or merge.

- Win conditions shift to cultural goals: "Your culture becomes the foundation of a new religion," "Your culture's innovations spread to every nation on the map"
- The focus character is the most prominent hero of the culture, regardless of which nation they belong to
- The player's investment survives political destruction -- if the nation falls but the culture persists, the patron relationship continues
- More resilient but less focused than nation patronage

### Favored Hero (instead of nation)

The player aligns with a specific hero. This is the most personal and most fragile mode.

- Win conditions are character-scale: "Your hero reaches mythic prominence," "Your hero founds a dynasty that survives three generations," "Your hero creates an artifact of legendary power"
- Simulation detail focuses entirely on this one individual and their immediate relationships
- The patron relationship ends when the hero dies -- the player enters mandatory neutral mode and must choose whether to continue or start a new run
- The most dramatic mode: highest investment, highest stakes, shortest potential duration

---

## How Patron Mode Interacts with the Card System

The existing card-as-simulation-component architecture handles patron mode naturally:

1. **Card pool is a superset.** The full card pool contains neutral-only, patron-only, and shared cards. Mode selection filters which cards are eligible.

2. **Tableau slots are the same.** The global tableau has the same number of slots in both modes. Patron cards compete with shared cards for slots, creating the same opportunity-cost tension.

3. **World-state-driven availability still applies.** Patron cards have their own preconditions: "Divine Champion" requires the focus hero to exist. "Sacred Ground" requires the favored nation to control at least one region. Cards become available when the simulation state warrants them.

4. **The storyteller is mode-aware.** In neutral mode, the storyteller balances drama across all entities. In patron mode, it prioritizes the favored nation's narrative arc while keeping the rest of the world active enough to provide context and challenge.

5. **Meta-progression works across modes.** Milestones can be earned in either mode. Some milestones are mode-specific ("Win a patron-mode run" unlocks a new patron card; "Achieve maximum cultural diversity in neutral mode" unlocks a new neutral card). The player is encouraged to play both modes.

---

## Open Questions

1. **When exactly can the player declare?** After a fixed number of ticks? After a specific number of nations exist? Any time they want? Too early means the favored nation hasn't had time to develop character. Too late means the player has been playing neutral for a long time and might not want to switch.

2. **How much stronger should patron cards be than shared cards?** If patron cards are too strong, patron mode becomes the "correct" choice. If too weak, it's not worth the constraint. The constraint (losing neutral cards, increased storyteller pressure) should roughly offset the power of patron cards.

3. **Should other nations know the player is a patron god?** If other nations have a "divine awareness" system, they might react to the favored nation's divine backing -- with fear, resentment, or attempts to court their own divine patron. This adds depth but also complexity.

4. **Can the player favor multiple entities?** One nation AND one hero from a different nation? One culture AND one faction? The research suggests keeping it to one favored entity to maintain focus. Multiple patronages would dilute the constraint principle.

5. **How does patron mode interact with monoculture lose condition?** If the player's favored nation IS the dominant culture threatening monoculture, do they lose? This creates a fascinating dilemma: the player's success (their nation dominates) triggers their failure (the world becomes homogeneous). Resolution: patron mode's monoculture threshold could be slightly higher (the god-backed nation is allowed more dominance), but not eliminated.

---

## Exemplar Mapping

| Design Element | Source Exemplar | How We Adapt It |
|---|---|---|
| Localized divine power | Black & White influence sphere | Patron cards are more effective near favored territory |
| Worship = power | Populous mana system | Favored nation's prosperity/faith fuels patron card strength |
| Imperfect control | Crest commandment interpretation | Player configures rules, heroes interpret them autonomously |
| Scales / world modification | Dominions pretender design | Patron declaration modifies simulation parameters in favored territory |
| Asymmetric mode selection | Stellaris ethics, Slay the Spire characters | Different card pools, same simulation engine |
| Telescoping detail | Root faction perspectives, Spirit Island spirits | Same board, different strategic focus and information depth |
| Protagonist through mortality | CK3 succession | Focus hero dies, next hero inherits focus with different traits |
| Emergent protagonist | RimWorld pawn attachment | Increased info density around favored nation creates organic attachment |
| Constraint = empowerment | All patron deity games | Patron mode gains targeted power, loses cosmic flexibility |
