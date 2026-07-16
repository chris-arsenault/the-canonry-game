# The Growing Map and Travel-Cost Reach

The physical world is not allocated up front. It is born as a small thawed
heart and earns its extent through play; reach across it is priced by travel
cost, so a larger world and its infrastructure change what can interact.

## The thawed heart

Seed/worldgen builds the physical grid at `PhysicalHeartRadius` (18) — about
a fifth of the eventual r40 extent. Idea-space is born the same way: the
semantic plane is seeded only within `SemanticHeartRadius` (18), and the rest of
its disk starts as fog. The heart is dense by construction: all six cultures fit
and press each other, which is the co-location the early simulation needs.

A semantic cell is *known* only once it has been materialized — the seeded heart
or ground the frontier later opens — exactly like the physical plane. Un-opened
idea-space is genuine fog: movement, generator placement, and verb targeting all
refuse it.

## MapFrontier: thaw and reclaim

`Engine.Kernel/Programs/MapFrontier/` grows and shrinks the physical world. It
reads which age holds the Ice track from `WorldBeats.OpenAgeId` and acts only
then:

- **Thaw** (Age of the Thaw): each pulse, culture mass at a frontier cell
  (raw ≥ `FrontierMassThreshold`) opens its unknown neighbours as new cells,
  materialized at runtime via `Simulation.CreateCell` with region/superregion
  inherited from the pressing cell. Growth is bounded by `MaxRadius` (40) and a
  per-pulse budget. The world opens where history presses, not on a timer.
- **Reclaim** (the Long Dark): while the reclaim age holds the Ice track, the
  program scans the frontier box for cold edge cells — known, weakly held
  (culture mass at or below the reclaim ceiling), no road, touching unknown or
  frozen ground — and takes them back as impassable `deep_ice`, propagating
  inward as the interior cools behind them. The Long Dark charges a global
  decay tide so untended frontier actually cools; a heart-radius floor keeps a
  minimal core, so the map recedes but never fully dissolves. The scan runs
  only while the reclaim age holds (deep stagnation), never on the hot path.
  Evidence: the saturation scenario (`tools/MonteCarlo/scenarios/`) drives the
  full Ice-age arc (Thaw → Still Ice → Long Dark → Thaw) and `deep_ice` appears
  exactly when the Long Dark opens.

New cells are ordinary: culture spreads onto them, entities move onto them,
generators place into them. Re-freezing transforms terrain rather than deleting
cells, so occupancy indexes and culture rows stay valid; passable, culture-
bearing extent is what grows and shrinks.

## Semantic growth: idea-space has no ice

The semantic plane grows through the same `MapFrontier` machinery, but its driver
is different. Ideas have no seasonal ice, so semantic growth is **continuous and
ungated by any age** and never re-freezes: each tick, every semantic cell a
culture holds with raw idea-mass at or above `SemanticFrontierMassThreshold`
opens its unknown neighbours as new `open_field` cells (`SemanticMaxRadius` and
the shared per-pulse budget bound it). New idea-ground is neutral open field and
earns its meaning as culture and entities reach it — the specialty rim terrains
(beacon spires, taboo ridges, cave pockets) of the full-scale field are not
pre-materialized. Configured on `MapFrontierConfig` via `SemanticOpenTerrain`
(null disables semantic growth).

Evidence (`semantic-growing-map-1`, 2×5000 ticks): idea-space grew from its
1027-cell heart to 1182 known cells, opening 155 new cells beyond the heart and
reaching ring 25 (vs. ring 19 for the age-gated physical plane), progressively
across the whole run (max ring 19 → 21 → 23 → 24 → 25) with no stranded entities
and no degenerate maps.

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
