# Monte Carlo Analysis Areas

This is a handoff list for the next Monte Carlo analysis pass. It should stay
focused on what to investigate, not how to run the tooling.

## Roads And Movement

- Are roads forming naturally from repeated travel?
- Do created roads keep attracting later movement?
- Do roads become useful infrastructure, inert decoration, or runaway corridors?
- Does road growth differ between physical and semantic planes?
- Does road usage correlate with broader roaming and map activity?

Do not treat road usage as proof of road prioritization. Prioritization needs
choice telemetry that compares selected and rejected movement options.

## Terrain Meaning

- Does terrain materially change movement, influence, claim, and placement
  outcomes?
- Which terrains are overused, ignored, or acting as blockers?
- Do terrain costs create interesting spatial patterns, or do entities behave
  as if terrain is cosmetic?
- Are terrain effects legible across both physical and semantic map behavior?

## Semantic Plane Behavior

- Are entities actually moving in semantic space?
- Does semantic movement produce different patterns from physical movement?
- Does semantic influence spread, cluster, or fragment differently from physical
  influence?
- Do semantic terrain and semantic regions feel like meaningful structure, not
  just copied physical map concepts?

## Influence And Claims

- Are influence fields expanding, consolidating, and contesting in coherent
  ways?
- Are claims stable enough to matter without becoming static?
- Are influence and claim outcomes sensitive to terrain, roads, and region
  structure?
- Are semantic influence and physical influence telling different stories?

## Regions, Superregions, And Continents

- Is activity distributed across higher-order map regions, or collapsing into a
  few cells?
- Do regions create useful analysis boundaries for movement, influence, claims,
  and generation?
- Are superregions and continents currently meaningful, or just labels?

## Entity Roaming And Activity

- Which entity kinds move, cluster, stall, or roam too widely?
- Are entities traversing meaningful space, or bouncing inside tiny local loops?
- Does movement volume translate into spatial variance?
- Are physical and semantic roaming patterns diverging in useful ways?

## Generators And Map Mutation

- Do generator map-cell operations create areas that entities later use?
- Are generated terrain, regions, claims, and roads integrated into later
  behavior?
- Are generators creating coherent spatial opportunities or isolated artifacts?

## Relationships, Gambits, And Spatial Effects

- Do spatial conditions create meaningful relationship and gambit opportunities?
- Do gambits alter movement, influence, claims, or territorial pressure?
- Are relationship changes spatially concentrated, diffuse, or disconnected from
  the map?

## Lifecycle And Stocks

- Do spatial constraints affect survival, attrition, or lifecycle transitions?
- Are stocks moving in response to terrain, influence, claims, or relationships?
- Are lifecycle outcomes varied enough to tune, or collapsing into one dominant
  path?

## Degeneracy Watchlist

- roads never form
- roads form but nobody reuses them
- roads dominate all movement
- terrain has no measurable effect
- semantic movement is inactive
- semantic influence mirrors physical influence too closely
- influence fields fragment into isolated noise
- claims never change hands
- generators create unused map artifacts
- entities overcluster despite available space

## Available From This Session

- Road creation, road usage, and road reinforcement are now analysis areas.
- Road prioritization is explicitly not covered yet.
- Influence analysis is plane-aware.
- Movement and roaming analysis are plane-aware.
- The output contracts now include the new road analysis surfaces.
