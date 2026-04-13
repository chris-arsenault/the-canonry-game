# Lose Conditions, Metrics, and the Chronicle

There are no win conditions. The player is a cosmic force shaping a world -- "winning" is producing an interesting, complex, long-lived civilization. But the world can fail, and the player needs ways to track what's happening and set their own goals.

---

## Lose Conditions

Three parallel threat dimensions, following the Cultist Simulator model of simultaneous ticking clocks. Each represents a different kind of failure. Each requires different tableau strategies to counter. The player must manage all three, and over-focusing on one leaves the others unattended.

### 1. Extinction (Total World Death)

Every civilization has collapsed. The map is empty. No settlements remain, no heroes live, no cultures persist. The simulation has nothing left to simulate.

**How it happens:** Cascading collapse. A major war destroys several nations. The displaced populations create instability elsewhere. Plague spreads through refugee movements. Remaining nations fragment. Without intervention, the collapse accelerates as each failure feeds the next.

**How the player prevents it:** Generator cards create new entities. System cards like "Migration Waves" redistribute populations. Intervention cards can stabilize crises. The player's role is not to prevent any single civilization from dying (that's natural and creates history) but to prevent ALL civilizations from dying simultaneously.

**Design parallel:** Frostpunk's generator failure. The ultimate catastrophe. The simulation stops being a simulation.

**Circuit breaker:** If the world approaches extinction (fewer than 2 nations, fewer than 3 settlements), the pacing system should slow the rate of destruction and increase the probability of recovery events. The player should have a narrow but real window to intervene. Complete extinction should feel like a failure the player watched happen, not a sudden surprise.

### 2. Stagnation (Heat Death)

The world is alive but nothing is happening. No prominence is changing. No events are firing. No relationships forming or breaking. The world is in equilibrium -- stable, peaceful, and boring.

**How it happens:** The player's tableau configuration produces a world that reaches a stable state and stays there. All nations have settled boundaries. All cultures have stabilized. All heroes are doing the same things every tick. The simulation is running but producing no narrative.

**How the player prevents it:** System cards that inject dynamism (Cultural Diffusion creates change, Disease Spread creates crises, Economic Pressure creates competition). Generator cards that create new actors who disrupt equilibrium. Action cards that enable conflict, diplomacy, and ambition.

**Design parallel:** SimCity without challenges. Cities: Skylines with perfect traffic. Technically functional, but the player has nothing to watch or respond to.

**Measurement:** A "dynamism" metric tracking events-per-era, prominence changes per tick, relationship formations/dissolutions. When dynamism drops below a threshold for a sustained period, a stagnation warning appears. If it stays below the threshold, the run ends.

**Nuance:** Brief periods of peace should be fine (and narratively interesting -- a golden age). The lose condition triggers only from sustained, unbroken stagnation. The threshold should be generous enough that the player doesn't feel punished for achieving temporary stability.

### 3. Monoculture (Convergence)

A single nation or culture has consumed everything. The world is alive and active but homogeneous -- one culture, one nation, one political system, no diversity, no tension, no alternative. The simulation has converged to a single point.

**How it happens:** One civilization becomes dominant (through military conquest, cultural absorption, or economic hegemony) and absorbs or destroys all alternatives. The simulation continues but with only one actor, which means no interaction, no conflict, no emergence.

**How the player prevents it:** Cards that promote diversity (Schismatic Thought creates cultural variants, Crucible of Nations spawns new polities). Cards that limit dominance (Economic Pressure creates internal tensions in large empires, Prominence Decay ensures no entity stays dominant forever). Removing "Cultural Diffusion" when one culture threatens to absorb the rest.

**Design parallel:** Stellaris endgame where one federation controls 90% of the galaxy. Technically the game continues, but strategically it's over.

**Measurement:** A "diversity index" tracking the number of distinct nations, cultures, and factions relative to the world's capacity. Also tracking the power ratio between the strongest and second-strongest entity. When the dominant entity controls >80% of the world with no viable rival, convergence warning appears.

**Nuance:** A large empire that still contains internal cultural diversity and factional competition should NOT trigger this. The lose condition is about homogeneity, not size. A continent-spanning empire with three competing factions and five regional cultures is interesting. A world where everyone is the same is not.

### How Lose Conditions Interact

The three lose conditions create tensions with each other:

- **Preventing extinction** (keeping entities alive) can lead to **stagnation** (too many stable entities with no reason to change)
- **Preventing stagnation** (injecting dynamism) can lead to **extinction** (too much disruption destroys everything) or **monoculture** (disruption benefits the strongest)
- **Preventing monoculture** (promoting diversity) can lead to **stagnation** (many small entities in stable equilibrium) or **extinction** (fragmentation weakens everyone)

The player cannot optimize against all three simultaneously. This is the core strategic tension that the card tableau must navigate. The "right" tableau depends on which threat dimension is most acute.

### Making Loss Productive

Following the roguelite principle (Slay the Spire, Against the Storm): loss must leave the player with something.

- **Milestone unlocks persist.** Any milestones achieved during a failed run still unlock new cards for future runs.
- **The chronicle persists.** The run's history is recorded. Failed civilizations become part of the player's cross-run narrative. "In my third run, every civilization was destroyed by plague -- that's when I unlocked Quarantine Protocols."
- **Failure unlocks specific cards.** An extinction run might unlock "Last Bastion" (generator that creates hardy survivor settlements). A stagnation run might unlock "Cosmic Catalyst" (system that periodically injects chaos). A monoculture run might unlock "Seeds of Rebellion" (generator that creates resistance factions within dominant empires).
- **Thematic framing:** The player is a god learning about what civilizations can do. Every failed world teaches something. The meta-progression IS the god's growing understanding.

### Lose Condition Display

A persistent but unobtrusive indicator showing the three threat levels. Not a bar that fills -- that creates anxiety-driven play. Instead, ambient indicators:

- **Extinction threat:** the map edges darken, empty regions grow visually desolate
- **Stagnation threat:** the color palette mutes, the event feed slows, the simulation feels "quiet"
- **Monoculture threat:** the map becomes monochromatic as one culture's color dominates

These visual cues communicate threat level without numbers, letting the player feel the world's state rather than reading it. Exact metrics are available in the dashboard for players who want them.

---

## Player-Trackable Metrics

The game defines no goals. But the player needs tools to set and track their own goals. The principle from Factorio: provide measurement tools, never tell the player what to measure.

### World-State Dashboard

A persistent panel showing the current state of the simulation. Available at a glance alongside the map.

**Headline metrics:**
- Total world population
- Number of active nations
- Number of distinct cultures
- Number of active heroes
- Number of artifacts in existence
- Current era
- Elapsed time (ticks/years)

**Secondary metrics (one click deeper):**
- Average prosperity across settlements
- Average stability across nations
- Prominence distribution (histogram: how many entities at each tier)
- Power distribution (relative strength of top nations)
- Cultural diversity index (a single number representing how diverse the world is)
- War count (active conflicts)

### Dynamism Metrics (The Pulse)

Time-series data showing how active the world is. Displayed as line graphs over the run's duration.

- Events per era (total occurrences generated)
- Prominence changes per era (sum of all prominence gains and losses)
- Nations founded vs. nations destroyed per era
- Cultural mergers and schisms per era
- Heroes born vs. heroes died per era
- Relationships formed vs. relationships dissolved per era

These graphs tell the player whether the world is alive or settling. A flat line means stagnation. Wild oscillations mean chaos. Gradual trends reveal long-term trajectories. The player decides which patterns are desirable.

### Comparative Metrics (How Am I Doing?)

Cross-run comparison without prescribing what's good:

- "Your world prosperity is 3.7 -- higher than 60% of your previous runs at this era"
- "You've reached era 5 -- your previous average is era 3.8"
- "Peak number of simultaneous nations: 12 (your record is 15)"
- "This is your longest-surviving nation ever (47 years and counting)"

Comparative framing gives context without judgment. A player pursuing "harsh world of constant warfare" has low prosperity and is satisfied. A player pursuing "peaceful multi-civ utopia" has high prosperity and is also satisfied. Both can see how they compare to their own history.

### The Chronicle (Legends Mode)

The single most valuable tracking feature. A structured record of every significant event in the simulation, browsable during and after play.

**What gets recorded:**
- Nation founded / destroyed / split / merged
- Hero born / reached new prominence tier / died / accomplished feat
- Artifact forged / changed hands / destroyed / discovered
- War declared / battle fought / peace treaty signed
- Culture emerged / spread to new region / merged / went extinct
- Settlement founded / grew to city / was destroyed / changed hands
- Era transition
- Player interventions (which cards were played, which tableau changes were made)

**How it's structured:**
- Chronological timeline (scrollable, zoomable)
- Filterable by entity (show all events involving "the Kingdom of Kesh")
- Filterable by type (show all wars, all artifacts, all cultural events)
- Relational links (click on a war to see the nations, heroes, and artifacts involved; click on a hero to see all events they participated in)

**Why this matters:**
The chronicle IS the implicit goal-tracking system. A rich, complex, interconnected chronicle means the player built an interesting world. A sparse chronicle means the world was boring. The player doesn't need the game to tell them "you did well" -- they can read the history they co-authored.

The chronicle also highlights **firsts and records** across runs:
- "First time a hero survived three era transitions"
- "Largest war ever recorded (7 nations involved)"
- "Most artifacts created in a single era"
- "Longest dynasty (12 generations)"
- "Most cultures coexisting simultaneously"

These cross-run records serve as the personal-best tracking that drives roguelite replay, translated into the language of historical simulation.

### Run Summary (Post-Run Report)

After a run ends (through lose condition or the player choosing to end), a summary screen shows:

- **Duration:** eras reached, total ticks elapsed
- **Peak metrics:** peak population, peak nation count, peak cultural diversity
- **Records:** most prominent entity ever, longest-surviving nation, most devastating war
- **Milestones:** which milestones were achieved this run (new unlocks highlighted)
- **Cause of ending:** which lose condition triggered (or player-initiated end)
- **Tableau history:** what cards were active at each era transition (showing how the player's strategy evolved)
- **Notable events:** the 5-10 most significant chronicle entries, automatically selected by prominence impact

This is the Slay the Spire victory/defeat screen applied to civilization simulation. It gives the player a digestible summary of what happened and why it mattered.

### Self-Set Goal Ideas

The game surfaces metrics. The player decides what to pursue. Some patterns that naturally emerge from the metric system:

- **The Empire Builder:** maximize the peak size of a single nation (population, territory, hero count)
- **David vs. Goliath:** guide a small nation to prominence while a larger one dominates
- **Peaceful Utopia:** reach the latest possible era with zero wars and high prosperity
- **Cultural Mosaic:** maximize simultaneous distinct cultures at peak
- **Legendary Hero:** guide a single hero to mythic prominence and record the most chronicle entries
- **Artifact Collector:** maximize artifacts in existence, track their provenance chains
- **Phoenix Run:** recover from near-extinction to peak civilization
- **Speed Run:** reach era N in the fewest ticks
- **Minimalist:** reach era N with the fewest tableau cards active

None of these are formally defined by the game. They emerge from the metrics, the chronicle, and the comparative data. The player who wants to pursue "most cultures" knows their personal record and can track current count against it.

---

## The Satisfaction Loop Revisited

With lose conditions and metrics, the satisfaction loop becomes:

1. **Configure** the tableau (hypothesis: "these cards will produce an interesting world")
2. **Observe** the simulation (watch the world develop, track metrics, read the chronicle)
3. **Diagnose** problems (stagnation threatening? monoculture emerging? extinction looming?)
4. **Adapt** the tableau (swap cards to address the most pressing threat)
5. **Track** progress (am I beating my personal bests? achieving milestones? producing interesting history?)
6. **Lose** (inevitably, eventually -- entropy wins)
7. **Unlock** (new cards from milestones, new knowledge from the chronicle, new strategies from the failure)
8. **Restart** with more tools and deeper understanding

The lose conditions create urgency. The metrics create goals. The chronicle creates investment. The meta-progression creates replay motivation. Together they transform "watch a simulation" into "play a game."
