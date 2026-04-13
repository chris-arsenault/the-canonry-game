# System 15: Lose Condition Monitor — POST-MVP

Tracks the three threat dimensions. Deferred to post-MVP because meaningful thresholds require a playable simulation to tune against.

---

## Design Intent (Preserved from Earlier Design Work)

Three parallel lose conditions that tension against each other:

- **Extinction:** all civilizations collapse. Monitored through entity counts (nations, settlements, active entities). Triggers when the simulation has nothing left.
- **Stagnation:** the world is alive but nothing is changing. Monitored through dynamism metrics (attribute changes per era, relationship changes, entity creation/destruction rate). Triggers when sustained low dynamism.
- **Monoculture:** one entity has consumed everything. Monitored through diversity metrics (distinct nations, cultures, factions) and power concentration (ratio of strongest to second-strongest). Triggers when a single entity dominates.

Preventing one can trigger another. This three-way tension is the strategic core that the tableau must navigate.

## Why Post-MVP

- Thresholds cannot be set without observing real simulation behavior
- The three dimensions need actual metric baselines to calibrate against
- Lose conditions that fire too early or too late ruin the experience
- The core simulation loop (entity behaviors, cards, tableau) must work first before adding stakes on top
- During development, the game is more useful without lose conditions (allows observing degenerate states to understand what to prevent)

## MVP Substitute

For MVP, surface the underlying metrics (entity counts, dynamism measures, diversity indices) as dashboard values without attaching lose conditions. This gives the player visibility into world health without the pressure of loss. The metrics become the basis for calibrating thresholds when the system is implemented.

See `docs/research/lose-conditions-and-metrics.md` for full design exploration including visual indicators, the satisfaction loop, and making loss productive through meta-progression.
