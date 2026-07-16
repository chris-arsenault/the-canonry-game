# The Growing Map and Travel-Cost Reach

The physical world is not allocated up front. It is born as a small thawed
heart and earns its extent through play; reach across it is priced by travel
cost, so a larger world and its infrastructure change what can interact.

## The thawed heart

Seed/worldgen builds the physical grid at `PhysicalHeartRadius` (18) — about
a fifth of the eventual r40 extent — while the semantic plane is born full
(ideas have no ice). The heart is dense by construction: all six cultures fit
and press each other, which is the co-location the early simulation needs.

## MapFrontier: thaw and reclaim

`Engine.Kernel/Programs/MapFrontier/` grows and shrinks the physical world. It
reads which age holds the Ice track from `WorldBeats.OpenAgeId` and acts only
then:

- **Thaw** (Age of the Thaw): each pulse, culture mass at a frontier cell
  (raw ≥ `FrontierMassThreshold`) opens its unknown neighbours as new cells,
  materialized at runtime via `Simulation.CreateCell` with region/superregion
  inherited from the pressing cell. Growth is bounded by `MaxRadius` (40) and a
  per-pulse budget. The world opens where history presses, not on a timer.
- **Reclaim** (the Long Dark): rim ground the program once thawed — no culture
  mass, no road, still touching unknown or frozen neighbours — is taken back as
  impassable `deep_ice`. The map's extent itself can ebb. The program keeps a
  ring of thawed coordinates so reclaim only revisits ground it opened.

New cells are ordinary: culture spreads onto them, entities move onto them,
generators place into them. Re-freezing transforms terrain rather than deleting
cells, so occupancy indexes and culture rows stay valid; passable, culture-
bearing extent is what grows and shrinks.

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
