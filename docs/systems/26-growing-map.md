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

## MapFrontier: reveal, culture, and ice

`Engine.Kernel/Programs/MapFrontier/` runs three decoupled physical layers.
Exploration is untied from both culture and ice:

- **Reveal is entity-driven and one-way.** Fog of war is simply the set of
  unknown cells; there is no scheduled scan or cursor to miss. Runtime
  movement and runtime entity creation call `MapFrontier.RevealOnArrival`,
  which opens the latent cells in the arriving kind's reveal radius through
  `Simulation.CreateCell`. Startup seed placement does not reveal additional
  cells: the seed's explicit cell set defines the initial live map. Terrain,
  superregion, and continent come from the coordinate's installed per-hex
  physical geography. Growth is bounded by `MaxRadius` (128). The world
  opens where bodies actually go: fog-adjacent exploration verbs, frontier
  placements, and journey content are what push it.
- **Culture is the home base, not the reveal mechanism.** The taste-shaped
  culture field (deposits scaled by `CultureGrowthMultiplier`, derived from
  kind and culture placement taste over the latent facts) supports
  operations as a soft leash: acting beyond the field's support raises
  costs through the `BeyondCultureSupport` effect condition rather than
  forbidding anything. Knowledge lowers the support threshold.
- **Ice is a separate dynamic cover layer.** Over the frontier-grown ring
  (never the startup heart), per-cell `ColdPressure` — cold from the latent
  temperature fact plus ice fact minus geothermal, scaled by the open age —
  contests `IceResistance` from local culture mass, roads, and knowledge.
  Freeze ages advance impassable `deep_ice` cover where pressure wins;
  thaw ages retreat it where resistance wins. Ice modulates travel and
  range; it does not own exploration.

New cells are ordinary: culture spreads onto them, entities move onto them, and
generators place into them. The world tracks physical and semantic live extents
independently, and every map consumer and snapshot uses the appropriate extent.
Candidate scratch capacity is an explicit kernel option, authored budgets are
validated against it, and the culture scan rotates so low culture ids do not
monopolize a shared per-run budget.

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

Physical worldgen converts the radius-128 request once into a global
microtriangle content domain and generates it with microtriangle-step padding
to avoid an artificial coastline at the latent frontier. It has no runtime-cell
topology. After generation, `IceRemembers.Seed` partitions the content lattice
into the canonical 24 local slots of each runtime cell. Uplift and ridge axes
cause the initial topography; erosion creates ancestral valleys; connected
land/ocean and bathymetry feed conditioned drainage; a separate water-balance
stage establishes actual lakes and spilling outlets; derived climate and
separated warmth pockets feed ice cover and glacial carving; fractures finish
the physical substrate. Ice Remembers then generates fire networks, aurora reservoirs,
pressure propagation from an unnamed deep-ocean locus, and rare interaction
surfaces. Frozen-ocean surfaces make the world broadly icebound while warm
ocean, shore, inland, and peak pockets remain viable away from the startup
heart.

Physical materials, natural surfaces, anomaly surfaces, and gameplay terrain
are distinct vocabularies. Scored authored rules derive the hex's durable base
terrain from its triangle arrangement and retained minority features. The
complete external summary retains relief, climate, water and ice coverage,
peaks, anomaly flags and magic intensities, plus reciprocal
river/coast/ridge/cliff/pass/fracture edge masks. Continents and superregions
follow this geography without creating water bands. Only base terrain,
superregion, and continent are installed in kernel latent-geography state; the
triangle fields live in the immutable startup blueprint, cache, and projection.
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
`SemanticMaxRadius` (128) and its explicit per-run budget bound growth;
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
