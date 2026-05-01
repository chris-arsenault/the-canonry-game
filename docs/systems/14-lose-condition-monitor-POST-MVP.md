# System 14: Lose Condition Monitor - POST-MVP

Lose conditions are deferred. The current need is measurement, not failure
states.

## Preserved Quality Axes

The monitor should eventually reason about the same generalized quality goals
used for simulation tuning:

- entity diversity and relationship structure
- map utilization and spatial spread
- per-tick visible life

## Candidate Failure Modes

- **Extinction:** too few active entities remain.
- **Stagnation:** entities exist but movement, influence, relationships, and
  gambits stop changing.
- **Monoculture:** one culture, faction, settlement cluster, or relationship
  component dominates everything.
- **Spatial collapse:** active entities cluster in a narrow corridor or small
  pocket while most of the map remains unused.

## MVP Substitute

Expose the metrics without attaching game-over conditions. Thresholds should be
chosen from Monte Carlo evidence after the simulation has stable behavior.
