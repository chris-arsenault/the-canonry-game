# Geological features and river presentation plan

Standing plan for the work discussed after the elevation causality plan
completed. Items are added here as they come up in design; **build order is
what this file says**, not the order things were raised in.

Status legend: `todo`, `doing`, `done`.

## Context

Everything worldgen produces today is a per-triangle fact. Nothing identifies
discrete features as objects — a mountain, a lake, a river system, a plain —
which is what naming, chronicle reference, and gameplay hooks need.

`MapSubdivisionMap` is the precedent: built in worldgen from triangle data,
handed onward as a `Dictionary<HexCoord, …>` of pure hex facts. Continents and
superregions already reach gameplay that way.

## Architectural constraints

- **The kernel must never see a triangle.** Features are identified during
  worldgen, where triangle data lives, and **persisted** in hex terms. They are
  not derived on demand: that would require triangle data at call time, which
  is exactly the contamination `domain-check` prevents.
- **Display-level facts are a separate thing.** The thalweg and area outlines
  are derived when the projection is built, because the projection legitimately
  holds triangles to feed the renderer. Do not generalise that to features.
- **IDs deterministic from position**, not iteration order, so regenerating a
  seed is stable. Surviving code changes is explicitly *not* a priority.

## Items

### 1. Overlay contiguity to ocean or terminal lake — `done` (2026-07-21)

The spine network is continuous by construction
(`ValidateContinuousOutletPath`), but the overlay draws one triangle per
cross-section, so where the deepest thread changes sides the drawn line can
step to a triangle that is not edge-adjacent. Seed 4200 draws 20,918 triangles
with **174 breaks** (0.8%).

Fixed by walking each drawn chain downstream and drawing in whatever it hands
off to. A flow target is always edge-adjacent, so following it closes the line
by construction rather than by heuristic.

**Verification note:** the `overlay: breaks=` diagnostic replicates the thalweg
rule against the field and cannot see the projection's closing walk, so it
still reports the pre-fix count. It measures how much closing there is to do,
not what remains. Confirmed instead by the rendered capture, where trunk rivers
run unbroken to the sea.

### 2. Thalweg through lakes — `done` (2026-07-21)

Lake triangles have no flow target - water does not run downhill inside a lake
- so the downstream walk from item 1 stops dead at the shore. The route across
is traced instead: from the inflow carrying the most water, breadth-first
through the body, to the nearest point where the river leaves again.

Seed 4200 has 80 lake bodies, 59 with an inflow and 61 with an outflow, of
which **58 need a crossing**. Not yet confirmed visually: the river captures
centre on (-8,12), which is `reef` - open sea, not a lake - so no crossing
appears in them. A capture centred on a lake with a through-river would settle
it.

### 3. Geological feature pass — `todo`

Worldgen identifies features; each gets an internal ID, a type, a primary
`HexCoord`, a magnitude, and the set of hexes it covers. Persisted into the
blueprint alongside subdivisions. Kernel sees a hex-keyed interface: what
features are here, of what type and size.

Start with **river systems**, because it subsumes items 1 and 2 — the same
network walk yields the hex-level system identity and the triangle-level line.

Then: peaks and ranges (topographic isolation already computed), lakes and
inland seas (`WaterBodyId` exists), plains and basins (connected low relief),
notable waterfalls (drop and discharge, to separate the few from the
thousands), craters (the durable rings are already legible landmarks).

### 4. Trunk rivers at hex zoom — `done` (2026-07-21)

Built out of order; recorded here for completeness. `TrunkRiverEdgeMask`
already existed in hex aggregation, filtered by Strahler order, and the painter
read neither it nor `RiverEdgeMask`; the projection was also dropping it. Now
threaded and drawn, trunks only. Edge midpoints derive from half the offset to
the neighbouring hex centre so they stay anchored to the `HexCoord` direction
convention the masks are built from.

## Carried over, unresolved

- `make ci-full` has never had a valid run. An early attempt piped through
  `tail`, so the exit code was `tail`'s and proved nothing.
- `CompiledDomain_ExposesGeneratorCapabilityStructureOutsideSymbols` fails in
  placement-taste code belonging to another workstream.
- Channel-widening banks carry `surface=river_channel` and channel strength
  with `RiverOrders == 0` and accumulation in the hundreds — seed 4200
  (-4,8) t6/t7/t10. A triangle with 430 accumulation calling itself a river
  channel is a data inconsistency.
- The diagnostic line "hexes reading floodplain" counts its own `>=8/24`
  channel proxy rather than the classifier's output, so it cannot see the
  gradient gate. Misleading; read heart terrain shares instead.
