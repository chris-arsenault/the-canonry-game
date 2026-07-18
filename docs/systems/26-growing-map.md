# The Growing Map and Travel-Cost Reach

The live world is not materialized up front. Seed/worldgen deterministically
prepares latent physical and semantic geography through radius 128, but each
plane is born as a small heart and earns its live extent through play. Reach
across it is priced by travel cost, so a larger world and its infrastructure
change what can interact.

## The thawed heart

Seed/worldgen materializes the physical grid at `PhysicalHeartRadius` (18).
Idea-space is born the same way: the semantic plane is seeded only within
`SemanticHeartRadius` (18), and the rest of the radius-128 field starts as fog.
The heart is dense by construction: all six cultures fit and press each other,
which is the co-location the early simulation needs.

A semantic cell is *known* only once it has been materialized — the seeded heart
or ground the frontier later opens — exactly like the physical plane. Un-opened
idea-space is genuine fog: movement, generator placement, and verb targeting all
refuse it.

## MapFrontier: thaw and reclaim

`Engine.Kernel/Programs/MapFrontier/` grows and covers the physical world. It
reads which age holds the World track from `WorldBeats.OpenAgeId`:

- **Thaw** (Age of the Thaw): each pulse, culture mass at a frontier cell
  (raw ≥ `FrontierMassThreshold`) opens its unknown neighbors through
  `Simulation.CreateCell`. Terrain, superregion, and continent come from the
  coordinate's installed physical geography rather than a uniform fallback or
  inheritance from the pressing cell. Growth is bounded by `MaxRadius` (128)
  and an explicit per-pulse budget. The world opens where history presses, not
  on a timer.
- **Reclaim** (the Long Dark): the program rotates a persistent cursor over
  cells previously created by physical frontier growth. A known, weakly held
  edge cell with no road and an unknown or already covered neighbor receives
  impassable `deep_ice` cover. Startup-heart cells are absent from this eligible
  set. The Long Dark's decay tide can cool abandoned rim ground, and repeated
  pulses can propagate cover inward through earned territory without
  destructively rewriting its terrain.

New cells are ordinary: culture spreads onto them, entities move onto them, and
generators place into them. The world tracks physical and semantic live extents
independently, and every map consumer and snapshot uses the appropriate extent.
Candidate scratch capacity is an explicit kernel option, authored budgets are
validated against it, and the culture scan rotates so low culture ids do not
monopolize a shared pulse budget.

Re-freezing does not delete a cell or replace its terrain. Physical cells hold
durable base terrain separately from transient cover. Deep Ice is the effective
terrain while present; thaw removes it and exposes the current base. Permanent
terrain changes made under ice remain hidden until thaw, then become visible.
Occupancy indexes, culture rows, geography identity, and terrain history
therefore remain valid through ebb and flow.

If the Long Dark later needs to invade the startup heart, that should be a
separate reversible invasion rule with explicit coverage and pacing. Ordinary
frontier reclaim does not do it implicitly.

## Latent geography

Physical worldgen generates the whole radius-128 field as 24 canonical
microtriangles per hex, using generation padding outside that field to avoid an
artificial coastline at the latent frontier. Macrostructure and elevation feed
connected-ocean classification, climate and geothermal normals, priority-flood
drainage, lakes, accumulated rivers, and topology-derived landforms. Physical
materials are distinct from gameplay terrain. Scored authored rules derive the
hex's durable base terrain from its triangle arrangement and retained minority
features, while the complete summary retains relief, climate, water and ice
coverage, peaks, and reciprocal river/coast/ridge/cliff/pass edge masks.
Continents and superregions follow this geography without creating water bands.
The simulation still acts at hex scale; richer summary utilization and
triangle-scale gameplay are follow-ups. Presentation can render the 24
triangles and connected physical features for materialized cells, while an
explicit inspection mode reveals the complete latent field without
materializing it or bypassing gameplay fog.

Semantic worldgen installs a varied per-hex terrain and geography blueprint
through the same generation radius. It does not use microtriangles.

## Semantic growth: idea-space has no ice

The semantic plane grows through the same `MapFrontier` machinery, but its
driver is different. Ideas have no seasonal ice, so semantic growth is
**continuous and ungated by any age** and never re-freezes. Each tick, every
semantic cell a culture holds with raw idea-mass at or above
`SemanticFrontierMassThreshold` opens unknown neighbors from the installed
semantic blueprint. Each new cell reveals its coordinate's terrain,
superregion, and continent rather than becoming `open_field`.
`SemanticMaxRadius` (128) and its explicit per-pulse budget bound growth;
`SemanticGrowthEnabled` disables it when false.

## Travel-cost reach

Interaction range is priced by geography, not hex count. The
`WithinTravelReach(maxCost, plane)` precondition line-samples the hex line
between actor and target, summing each cell's road-discounted terrain cost
(`Movement.TravelCost`). Impassable or un-thawed ground on the line makes the
route infinite, so the frozen frontier gates reach the way real terrain does,
and roads (lower per-cell cost) extend it. The reader is passed into gambit
evaluation through `QueryBinding`, keeping the kernel query layer
simulation-agnostic; absent a reader, a reach gate conservatively fails.

Domain gates that cross real ground use it: the hunt's `Corner the Beast`,
`Slay a Monster`/`Slay an Orca`, `Claim a Point of Interest`, and
`Raise a Causeway` are travel-reach gated. Short-range and semantic gates stay
on hex distance.

## Why this shape

The geometry analysis (docs/analysis/world-kinetics-20260715.md) showed the old
physics survived only because of co-location; a fixed-radius gate on a very
large map would produce disconnected halo-villages. A heart that grows, plus
reach that follows roads and stops at frozen ground, keeps interaction
connected at any map size — and building the connective tissue (roads,
thawed corridors) is itself visible flow.
