# Anti-Degeneracy

Anti-degeneracy is now framed as simulation quality, not a list of fixed crisis
patches. The stable metric contract lives in
[`simulation-principles.md`](../systems/simulation-principles.md).

## Quality Axes

The simulation should resist three broad failures:

1. Entity diversity collapses.
2. The map is underused or degenerately clustered.
3. The map looks static from tick to tick.

These are not currently lose conditions. They are tuning targets and warning
surfaces.

## Response Layers

1. Low-altitude physics should create movement, influence, relationship drift,
   aging, and death without needing player cards.
2. Generators should periodically introduce new named entities and keep the
   graph from only shrinking.
3. Gambits and cards should provide outlets for war, betrayal, wielding,
   sealing, cleansing, and other major relationship changes.
4. UI and telemetry should expose bad patterns before they become mysterious.

## Detector Direction

Detectors use generalized metrics:

- active entity counts by kind/subtype/culture
- largest connected relationship components and isolated components
- map activity entropy, largest strict empty hex radius, and top-cell share
- influence component count and discontinuity
- per-entity roam radius and unique cells visited
- relationship creation/cull/strength-change rates
- death and historical transition rates by kind

Thresholds should come from Monte Carlo evidence, not from paper design.
