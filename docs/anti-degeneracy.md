# Anti-Degeneracy Mechanisms

Degenerate states are derived from entity categories. Anti-degeneracy responses come from three layers: low-altitude shock absorbers (automatic, slow the decline), card-level responses (player-chosen, the interesting decision), and UI warnings (explicit alerts when patterns are detected).

Thresholds and rates are tuning targets determined through playtesting. The architectural commitment is: the mechanisms EXIST, the patterns are DETECTABLE, and the card pool CONTAINS responses.

The deck's cycling model ensures the player will eventually draw relevant cards, but not necessarily in time to prevent a specific crisis. This is part of the roguelite aspect: learning which disasters to plan for and which card combinations address them is a core skill that develops across runs. Reaching a degenerate state and trying again with better deck construction is an expected part of the learning loop, not a failure of the system. A future tutorial will guide players through this, but for MVP the discovery is through play.

Note: the game will have an autosave/resume system -- players are not forced to restart from scratch when things go wrong. They can reload and try a different approach. **Open question for pre-player-facing phases:** is reload a convenience/testing feature, or a real player-facing pillar? If reload is a pillar, it changes the learning loop (the player experiments by reloading rather than by restarting). This does not need to be resolved for MVP but MUST be resolved before Tech Demo. How reload interacts with meta-progression is also TBD. See also the post-MVP backlog item for branched history ("what-if" divergent timelines from save points).

---

## The Three-Layer Model

For each degeneracy pattern:

1. **Low-altitude shock absorber (automatic).** Built into entity physics. Slows the decline. Buys the player time to notice and respond. Does NOT prevent the degeneracy -- just makes it gradual, not instant.

2. **Card-level responses (player choice).** The card pool contains generator, system, and action cards that could address each pattern. The player's deck (built from archetype + modifications) determines which responses are available in a given run. The cycling deck ensures all cards in the deck will be drawn eventually, but the player may not have the right card in hand at the exact moment a warning fires. This is by design -- degeneracy response is a strategic concern (build a deck that covers your risks), not a tactical emergency.

3. **UI warning (explicit alert).** Known degeneracy patterns are codified in the engine and surfaced as warnings. The player sees the trend forming and can decide whether and how to respond.

Without player intervention, the shock absorbers buy time but don't prevent collapse. The game is about guided evolution, not degeneracy prevention -- but the player must be aware when the world is trending badly so they can steer.

---

## Spatial Degeneracy

### Settlement Depopulation (MVP)

Settlements lose population and trend toward abandonment.

**Low-altitude shock absorber:**
- Soup deficit causes SLOW population decline, not instant death. The drain rate is proportional to the deficit, not binary.
- NPC hunger process drives migration TOWARD settlements with surplus, concentrating surviving population rather than letting it scatter and die.
- Lifecycle drain on settlements starts near zero and increases with age -- young settlements are resilient, only old neglected settlements decline quickly.

**Card-level responses:**
- **Generator:** "Found Settlement" creates new settlements in viable locations. As long as habitable land exists, the player can inject new spatial anchors.
- **System:** cards that modify soup production, population growth rates, or settlement resilience. "Agricultural Reform" might boost soup production in deficit settlements.
- **Action:** cards that enable migration policy, resettlement, or emergency supply. "Forced Relocation" might move population from a dying settlement to a viable one.

**UI warning:**
- **"Settlement in decline: [name] has been in soup deficit for N ticks."** Triggered when a settlement's population has been decreasing continuously for a configured duration.
- Shows on the event log and potentially as a map marker (dimming, red indicator) on the affected settlement.

**How fast without intervention:** tuning target. Structural commitment: slow enough that the player can observe 2-3 ticks of decline before it becomes critical. A settlement should survive at least N ticks of pure deficit before reaching zero population.

---

## Agency Degeneracy

### Political Monoculture (MVP)

One nation absorbs or destroys all others. The world has only one political entity.

**Low-altitude shock absorber:**
- Consolidation/fragmentation pressure: large nations accumulate internal strain (administrative distance from capital, cultural diversity of member settlements). The bigger a nation gets, the more strain it experiences. This is a natural ceiling on expansion. (Post-MVP: faction pressure adds an additional internal strain source when factions are implemented.)
- Lifecycle aging: old nations have increasing cohesion drain. A nation that has existed long enough to dominate the world is also old enough to be fragile.
- Cultural friction within large polities: a nation controlling settlements of different cultures experiences internal friction that undermines stability.

**Card-level responses:**
- **Generator:** "Splinter Nation" creates a new nation from a fragment of the dominant one when internal strain is high. "Frontier Independence" creates a new nation in distant, weakly-controlled territory.
- **System:** cards that increase internal strain on large entities. "Administrative Entropy" accelerates cohesion drain proportional to nation size. "Cultural Awakening" increases friction within culturally diverse nations.
- **Action:** cards that enable rebellion, secession, or independence movements. "Declare Independence" allows a high-strain nation's settlements to break away.

**UI warning:**
- **"[Nation name] controls N% of all settlements."** Triggered when a single nation's settlement count exceeds a configured percentage of total settlements.
- Shows on the event log. Map overlay makes the dominant nation's territorial extent visually obvious.

**How fast without intervention:** tuning target. Structural commitment: consolidation/fragmentation pressure creates a natural expansion ceiling that most card configurations can't easily overcome. A runaway monoculture should require ACTIVE player support (aggressive military cards, suppression of internal factions) -- it shouldn't happen passively.

---

## Actor Degeneracy

### NPC Extinction (MVP)

All NPCs die and none are generated. The world has no individual actors.

**Low-altitude shock absorber:**
- NPC lifecycle drain starts near zero. Young NPCs are highly resilient. Only old NPCs face significant mortality pressure.
- Hunger process drives NPCs toward food (soup surplus settlements), naturally keeping them alive as long as food exists.
- NPCs don't die from a single bad tick -- HP drain from hunger is gradual, and moving toward food reverses it.

**Card-level responses:**
- **Generator:** "NPC Emergence" creates new NPCs at settlements. The preconditions should be easy to meet: any settlement with population above a minimum can spawn an NPC. This is the primary recovery valve.
- **System:** cards that improve NPC survival conditions. "Medical Tradition" reduces natural death rates. "Martial Training" improves NPC resilience.
- **Action:** cards that enable NPC recruitment, training, or protection. "Recruit Champion" actively creates a powerful NPC.

**UI warning:**
- **"NPC population low: only N NPCs remain."** Triggered when the total NPC count drops below a configured threshold.
- Shows on the event log.

**How fast without intervention:** tuning target. Structural commitment: NPC generation from the starting deck should outpace natural attrition under normal conditions. NPC extinction should only be reachable through sustained adverse conditions (no soup anywhere, extreme conflict) combined with no active NPC generators.

---

## Degeneracy Patterns: Post-MVP

These patterns are identified but deferred. The architecture must support adding them, but they are not implemented for the first playable.

### Spatial Concentration
All settlements cluster in one area, leaving the map mostly empty.
- Shock absorber: settlement founding preconditions should include spatial spread requirements.
- Card response: generators with minimum-distance preconditions.
- UI warning: "All settlements are within N cells of each other."

### Agency Stagnation
All nations reach equilibrium. No one does anything. Relationships are stable. Pressure is resolved.
- Shock absorber: lifecycle decay makes equilibrium expensive to maintain. Relationship drift creates instability over time.
- Card response: action cards that enable disruptive behaviors. System cards that increase pressure.
- UI warning: "No significant events for N ticks."

### Resource Collapse
Both soup and gold reach zero everywhere simultaneously.
- Shock absorber: regional fertility ensures some base soup production always exists. Scarcity drives migration toward productive areas.
- Card response: system cards that boost production. Generator cards that create settlements on fertile land.
- UI warning: "Global soup/gold deficit for N ticks."

### NPC Congregation
All NPCs cluster in one settlement, leaving the rest of the world uninhabited by actors.
- Shock absorber: ambition process should drive NPCs toward opportunity, not just safety. Competition for limited resources at crowded settlements should push NPCs outward.
- Card response: action cards that enable exploration, pioneering, or assignment.
- UI warning: "N% of NPCs are at a single settlement."

---

## Implementation Requirements

### Degeneracy detectors

Each MVP degeneracy pattern has a codified detector: a function that evaluates world state each tick (or at low cadence) and returns a severity level. Detectors are simple threshold checks on aggregate metrics:

- Settlement depopulation: `count(settlements where population is declining) / count(settlements)`
- Political monoculture: `max(nation.settlement_count) / total(settlements)`
- NPC extinction: `count(active NPCs)`

Severity levels: normal, warning, critical. Thresholds are configurable (tuning targets).

### UI warnings

When a detector reaches "warning" severity:
- An entry appears in the event log with a distinct warning style
- The affected entities are flagged in the UI (map marker, detail panel indicator)
- The warning persists until severity drops back to normal

Warnings are informational, not prescriptive. They tell the player WHAT is happening, not WHAT TO DO. The player decides the response based on their theory and their available cards.

### Extensibility

The detector system is a registry: new detectors can be added with a metric function, threshold configuration, and warning text. Post-MVP patterns are added by registering new detectors, not by modifying the core system.

---

## What This Is NOT

- **Not a game-over system.** Warnings inform; they don't end the game.
- **Not a hidden director.** Detectors and warnings are fully visible. The player knows exactly what the game is worried about and why.
- **Not a balancing force.** The detectors don't modify the simulation. They observe and report. The shock absorbers are part of the low-altitude physics, not part of the detection system.
- **Not a prescription.** The warnings say "settlements are declining." They don't say "play a generator card." The player chooses the response.
