# Causal Physical World Generation

Status: implemented; the current map-generation scope is closed.

The physical world is generated as a pass-ordered dependency chain over the
canonical microtriangle graph. It is not a stack of independent noise layers.
Elevation causes drainage; drainage is eroded before glaciation; glaciation
overprints the ancestral river landscape; fire changes temperature before
aurora settles; pressure propagates through the resulting ocean geometry.

The prototype generator was removed rather than retained behind a compatibility
layer. Generation now starts and ends on one global microtriangle lattice. The
current cell-scale simulation boundary remains a useful runtime contract, but
its 24-triangle grouping is a projection concern and is not generation
topology.

## Canonical climate history

Ice Remembers is set during an artificial ice age, not on a naturally frozen
planet. Before the Collapse, the world had a climate roughly comparable to
Earth's. Its continents, ocean basins, mature river systems, eroded mountain
ranges, and temperate landscapes formed under ordinary long-term climate.

The apocalyptic pre-game war killed the Dead God and broke the balance among
the world's magical forces. That failure caused the **Deep Freeze**, a terminal
glacial overprint of unknown duration that destroyed pre-Collapse civilization.
Game year 0 is roughly twenty years after the Deep Freeze ended. The acute
freeze has retreated, but the artificial ice age continues.

This history constrains generation:

- early geological and erosional epochs must be capable of widespread liquid
  precipitation and temperate drainage rather than treating every epoch as an
  ice-planet variant;
- the Deep Freeze is a late, magic-caused glacial catastrophe over an already
  mature landscape;
- the present pass represents partial retreat, renewed runoff, exposed coasts,
  and isolated warm refuges while retaining extensive land and sea ice;
- present barrens and glaciers must preserve readable underlying relief,
  ancestral valleys, basins, continental margins, and depositional history;
- current warmth pockets are refuges within the artificial ice age, not the
  only places where geological complexity was ever allowed to form.

The four configured epochs must express that history as measurable outcomes,
not merely as names and relative strength values. `crust-assembly` assembles a
mature physical world under ordinary long-term climate;
`orogeny-and-wet-erosion` performs major uplift followed by genuinely warm and
wet erosion with widespread liquid drainage; `glacial-maximum` applies the
magic-caused Deep Freeze as a catastrophic overprint on an already mature
landscape; `retreat-and-present` retreats toward the still-icebound present,
reopening some water and warmth without erasing inherited temperate geology.
Temperature offsets alone do not establish a temperate or wet epoch — verify
actual thawed land, liquid runoff, erosion distribution, glacial advance, and
partial retreat. Tectonics must likewise leave successive active and inherited
relief rather than repeatedly uplifting one fixed boundary arrangement.

The Before Times are the same immediate pre-Collapse period. Their
serial-numbered artifacts are manufactured high-technology weapons used in the
war that killed the Dead God. Once ordinary physical generation is sound, a
later domain generation stage may seed pre-Collapse roads, settlements,
megaliths, and weapon sites into the mature temperate landscape before applying
the Deep Freeze and retreat passes. Glaciation, erosion, sediment, fracture,
and magic processes can then turn those works into buried, broken, flooded, or
newly exposed relic sites. This archaeology stage is intentionally deferred; it
must not be used to disguise defective natural geography.

## Ownership and runtime boundary

- `Engine.WorldGen` owns the domain-neutral global microtriangle lattice, graph
  algorithms, uplift and erosion, connected land and ocean classification,
  bathymetry, hydrology, climate, glaciation, fractures, and structural
  validation. It has no knowledge of runtime cells or their coordinates.
- `IceRemembers` owns the Ice Remembers generation profile, physical surface
  taxonomy, material rules, tunable generation controls, and diagnostic goals.
  `IceRemembers.Seed` executes the domain generation pipeline: natural
  surfaces, fire, aurora, pressure, magic interactions, administrative
  subdivisions, projection into runtime cells, aggregate synthesis, and
  compatibility terrain synthesis.
- `Engine.Domain.Startup` transports the immutable generated blueprint with
  stable palettes. The developer world cache persists this blueprint.
- `Engine.Projection` and the Godot UI inspect and render the complete external
  blueprint.
- `Engine.Kernel` receives only latent per-hex data needed by gameplay and
  frontier reveal. It does not store generation-time microtriangles or fields.

The current kernel needs only base gameplay terrain, superregion, and
continent for each latent physical hex. A later gameplay-mirroring tranche may
compile selected primary and significant hex features into primitive runtime
data. That later decision must not drag the complete triangle field into the
kernel.

## Geometry and retained model

Generation is defined over algebraic global microtriangle coordinates
`(u, v, inverted)`. Each triangle has exactly three edge neighbors within the
padded work graph, and adjacent centroids are exactly one generation-distance
unit apart. All connected passes use this graph. The requested runtime radius
is converted once, before generation, into a content triangle count; the
generator grows a centered content domain of that size, adds padding measured
in microtriangle steps, and crops to the content domain only after generation
and validation.

After generation, `IceRemembers.Seed` constructs
`PhysicalKernelMapGeometry`. That projection partitions the content lattice
into the kernel's cells and 24 local slots, translates triangle feature edges
into runtime edge masks, and supplies aggregate summaries. No generator layer
may use projected cell identity, local triangle identity, cell distance, or a
cell-shaped boundary as physical input.

Each generation process lives in its own layer file and implements the common
`IMapGenerationLayer<PhysicalGeographyField, PhysicalGeographyGenerationContext>`
boundary. A layer declares the facts it requires, produces, and invalidates;
the pipeline refuses to run it when its inputs are missing and rebuilds facts
invalidated by a later causal edit. Algorithms shared by multiple layers live
separately from the layers themselves. Layer tests exercise both the intended
effect and rejection of malformed inputs or outputs.

The domain pipeline uses the same dependency-inversion boundary over an
`IceRemembersMicrogeographyMap`. Natural classification, each magic field,
fire's climate feedback, each feature-classification pass, interactions, final
surface precedence, and validation are independent files with declared fact
dependencies. These generation-time details remain outside the simulation
kernel.

Generation-time arrays may allocate freely within the application target of
about 4 GB and acceptable ceiling of 8 GB. Clear field-major arrays and reusable
scratch buffers are preferred over compact encodings. Exported startup data is
immutable and deterministic.

The retained triangle model separates:

- physical material, continental assembly provenance, and transported suture
  identity and strength;
- primary surface type before and after magic overrides;
- landform;
- landmass and continental shelf grouping;
- bathymetry and connected water;
- elevation, slope, temperature, moisture, and runoff;
- flow routing, accumulation, rivers, lakes, and watersheds;
- ice thickness and ice flux;
- fracture conductance and fire-core density;
- fire, aurora, and pressure intensity;
- non-exclusive anomaly and interaction flags.

An exclusive final surface type exists for rendering and feature extraction,
but it never erases the underlying fields or overlapping flags.

## Numeric model and lattice invariants

These are the failure modes that cost the most to find. They are cheap to
respect and expensive to rediscover.

**Topology and geometry are one global algebra.** Neighbor coordinates are
derived directly from `(u, v, inverted)`, and centroid positions use the same
triangular-lattice basis. The durable invariant is exhaustive reciprocity plus
an exact unit centroid step for every interior edge. A previous parent-cell
implementation could be internally reciprocal while connecting geometrically
distant triangles across selected cell boundaries; the symptoms included
disjoint rivers, cell-boundary artifacts in every diffused field, phantom
cliffs, and directional flow bias. Those cell seams no longer exist in the
generation graph.

**Projection does not alter physical adjacency.** Runtime cell directions,
local slot numbers, and edge masks are conventions of
`PhysicalKernelMapGeometry` and the presentation geometry. They may summarize
or render generated facts, but they must not feed back into drainage, erosion,
ice flow, slope, or any other physical pass. Projection tests verify that every
content triangle is assigned exactly once and that translated feature edges
remain reciprocal.

**All physical edges have one length.** A raw elevation difference across an
adjacent pair is therefore already proportional to slope. No edge-type scale,
parent-cell correction, or slope divisor exists. Drainage guidance,
descent-gradient estimation, stream-power erosion, ice-flow descent, and the
slope field all operate on the same unit-step graph. Reintroducing a numerical
correction for projected cell boundaries would hide a topology error rather
than model a physical distance.

**Elevation is deterministic fixed point.** Generation elevation carries 1,024
subunits per authored unit (`PhysicalElevation`). Rounded whole-unit elevation
exists only as compatibility output and must never be the basis of a routing or
comparison decision; the fractional part is what keeps uplift, erosion,
deposition, and conditioned drainage from collapsing into ties. Five facts stay
distinct and must not be conflated: physical floor (bedrock plus sediment),
water-surface elevation, water depth, conditioned drainage surface, and
outletward drainage rank.

**Flat conditioned surfaces are a lattice trap.** Where the conditioned surface
is flat, routing decisions fall to tie-breaking, and any deterministic
tie-break (enqueue order, triangle index) becomes a visible axis-aligned comb.
The priority flood therefore carries a seeded per-triangle jitter in its
priority key and fills depressions along a strictly decreasing surface using a
hashed, heavy-tailed epsilon, so flats drain through a few dendritic corridors
instead of evenly spaced parallel chains. Guidance then runs a second
discharge-capture pass so parallel streams coalesce into trunks. The share of
decisions actually settled by tie-break is a reported metric and should stay
near zero.

## Margins, crust age, and coastal character

Plate motion and boundary type were once computed and then discarded before
the coastline, so every coast came out the same smooth passive shape. They now
reach the coast, because **coasts differ in kind, not merely in raggedness**,
and the kind is set by what the nearest boundary is doing.

`TectonicMarginLayer` classifies crust as passive, convergent, rifted, or
transform by propagating boundary kind outward to a bounded reach; crust beyond
every boundary's reach is plate interior, which on continental crust is a
passive margin. The same layer ages oceanic crust away from spreading centres,
which drives thermal subsidence in the crustal support model. Oceanic age does
not stand in for a continental shelf.

The provisional connected ocean first solves one stable `EffectiveSeaLevel`.
`ContinentalMarginEvolutionLayer` floods signed distance landward and oceanward
from that actual shoreline, propagating the adjacent coastal margin kind. It
then evolves crustal thickness toward broad passive shelves and coastal plains,
somewhat deeper rifted shelves, narrow transform margins, or convergent
trenches. The next support/flexure pass converts the subsurface shape to
regional bathymetry. `MarineSedimentationLayer` fills available passive and
rifted shelf accommodation only from `SedimentExportedTotal`; it cannot invent
material to hit a target profile.

`CoastalDetailLayer` displaces the near-shore band with warped octaves reaching
triangle scale, with amplitude, wavelength, warp, and ridging chosen by margin
type; it runs on the landscape surface rather than replacing the causal margin
shape. Coastal erosion applies rock resistance derived from volcanic
construction, orogenic history, and sediment thickness, because erosion driven
by wave energy alone is a coastline *straightener*: wave exposure peaks on
headlands, so without differential resistance every promontory is planed off
and every bay filled.

The standing check is that coasts remain tellable apart by margin type.
Crenulation — coastline triangles over the square root of enclosed land area —
should separate by kind, and shelf width per unit coastline should separate
with it. If they converge, a margin fact is being persisted but not consumed.

A fjord is a glacial valley the sea has drowned. Glacial landform
classification skips water, so a valley that ended below sea level never
carried a `UValley` code; fjord classification therefore keys on cumulative
glacial erosion, which is persistent history and survives drowning.

## Persistent and derived state

Epochs are cumulative, so a small set of fields is history and everything else
is recomputed. Retain and conservatively transport: crust assembly,
composition, thickness, age, support datum, surface load, categorical
provenance, and material suture pair and strength; bedrock elevation and
sediment thickness; cumulative fluvial, glacial, and coastal erosion and
deposition; geothermal history; maximum ice thickness and flux; wave-exposure
history; and domain fire, aurora, and pressure history. Current boundary mode,
plate-pair component, support target, convergent surplus, and orogenic
construction are re-derived for the new epoch. A material suture is not a
current province boundary: it moves with transported continental crust and
survives after the active boundary migrates.

Ocean connectivity, bathymetry, active temperature, precipitation, present
flow, active rivers, current land and sea ice, and every classification are
derived: they may be invalidated and rebuilt at any point in the schedule. Do
not retain complete field snapshots per epoch.

## Ordered pipeline

### 1. Uplift and ancestral erosion

Generate reduced moving crust provinces across the padded field. Deterministic
frontier growth joins distributed nuclei into continental assemblies and
oceanic crust; relative motion types convergent, divergent, and transform
boundaries. Epoch activity varies which inherited boundaries remain active,
while cumulative strain and uplift preserve old orogenies and rifts. When
unlike transported continental assembly IDs converge, a material suture records
their pair identity and shortening strength. Connected suture systems can keep
building or preserve ranges in the interior after assembly, instead of relying
on a synthetic interior-elevation belt or requiring every major range to remain
on a current coast. Orogenic construction gives each range a broad flexed root
and a narrower resistant summit-rock load so erosion can carve valleys without
reducing every crest to gorge-scale relief.

Warped multi-scale variation roughens only continental margins rather than
deciding land from a noise threshold. Sparse domain-neutral impacts then
excavate basins, raise fractured rims, distribute conserved ejecta, and retain
decaying heat for sufficiently large strikes. Warm/wet epoch hydrology carves
ancestral V-valleys through this mature landscape before the Deep Freeze.

### 2. Landmasses and continental groups

Apply the sea-level parameter and connected-component label every above-water
triangle. Large components are landmasses; small components are islands.

Shelf-or-shallower connected components form continental groups. Multiple
islands on the same submerged shelf therefore read as one archipelago. Domain
administrative continents remain separate authored subdivisions and never
replace these physical IDs.

### 3. Bathymetry

Ocean, shelf, exposed shelf, shelf break, and reef eligibility derive from
elevation below sea level, slope, connected ocean, distance to shore, and
temperature. Sea level is an explicit profile parameter. It is not assumed to
be zero and it controls exposed shelf and island fragmentation.

### 4. Hydrology

Use one deterministic priority-flood implementation to condition drainage and
establish a routing sill and outletward rank for every depression. The filled
surface is routing geometry, not a command to fill every hollow with water.
Flat routes follow propagated outlet geometry rather than a tangent around the
world origin, and lake roots do not depend on storage IDs.

A separate lake stage discovers topographic accommodation and solves the
supported water level from accumulated catchment runoff plus precipitation and
melt falling directly on the candidate pool, against open-water evaporation
and infiltration. A basin may therefore remain dry, hold a partial endorheic
lake below its sill, or reach the spill stage and become exorheic. If a
partially supported surface leaves disconnected pools, each pool is a separate
water body. Only a spilling lake records overflow and a physical outlet;
fluvial erosion spends that overflow on the controlling outlet sill before the
next hydrology reconciliation. Priority fill never becomes lake depth or
water-surface elevation.

Accumulate the reconciled runoff and overflow, derive basin-relative headwater
thresholds and Strahler order, and assign watersheds. Every classified channel
remains continuous through exorheic lakes to terminal water. The one-triangle
flow spine stays authoritative while a separate layer widens major rivers
perpendicular to local flow tangents across low-relief banks; cross-valley
floodplains and seaward delta fans remain derived coverage.

Two river facts are easy to break. Lakes are *conduits* for stream order, not
terminators: a system flowing through a chain of lakes keeps accumulating
Strahler order instead of restarting below every pond, and a river crossing a
lake draws its flow path when it carries channel-scale discharge, so a
lake-fed system reads as one river. And the visible spine must survive its own
channel fill: widening spreads fill upstream past the channel head, so the
dominant filled inflow chain above each head carries the spine to the top of
the apron rather than stopping inside it. Because channels begin only after
roughly 1,800 triangles of drainage area — already a natural order-3 or -4
stream — an observed maximum Strahler order near 5 corresponds to a fully
resolved natural order around 7 or 8.

Lake bodies are compact, water-supported portions of depressions, not every
filled hollow. Elongated one-or-two-triangle chains along epsilon-conditioned
valleys are river corridors and are rejected by a compactness gate.

### 5. Present icebound climate with warmth pockets

The present world has a cold global baseline. Temperature is derived from
elevation lapse, ocean moderation, water proximity, and several deterministic
warmth systems rather than latitude or an independent noise channel.

Warmth pockets may occur offshore, on coasts, inland, or around geothermal
systems. The startup heart is guaranteed to overlap one viable warmth system,
but it is not the only warm place. High geothermal peaks may support verdant
hot springs even while surrounding alpine terrain remains frozen.

Moisture derives from open-water exposure, prevailing transport, rain shadow,
and local thaw. Tundra remains the dominant ordinary biome; open field is a
scarcer warm-steppe outcome.

### 6. Glaciation

Derive ice thickness from cold, elevation above snowline, moisture, and
concavity. Route ice flux downhill with the shared flow machinery. Surface,
basal, geothermal, and retreat melt feed proglacial runoff. Incremental erosion
and deposition retain maximum thickness/flux and produce the recognizable
consequences of ice flow:

- widened and flattened U-valleys;
- cirques at glaciated valley heads;
- flooded fjords where carved valleys meet the sea;
- moraine ridges at glacier termini.

The landform pass runs after these elevation edits and retains glacial
landform codes from historical maximum ice rather than requiring present ice.
It also classifies outwash, ancestral river valleys, inherited ridges, and
impact basins/rims from persistent history.

### 7. Natural surface classification

Classify a cold-shifted climate base, then apply physical overrides in causal
order. The natural surface vocabulary is:

- `tundra`
- `open_field`
- `ridge`
- `glacier`
- `moraine`
- `lake`
- `fjord`
- `exposed_shelf`
- `shelf`
- `ocean`
- `reef`
- `pack_ice`
- `sea_ice`
- `ice_shelf`
- `polynya`
- `snowfield`
- `polar_barrens`
- `wet_tundra`
- `cold_steppe`
- `scree_field`
- `river_valley`
- `river_channel`
- `floodplain`
- `delta`

Physical material remains independent of this surface classification.

### 8. Fracture network and fire

Generate a branching fracture-conductance graph seeded by uplift boundaries
but carrying its own connectivity. Generate a spatially varying core-density
field and sample many fire cores with density-dependent separation. A few
density maxima become magma chambers.

Multi-source cost-distance propagation follows inverse fracture conductance.
Fire writes heat back into temperature before aurora runs. Fire surface types
are:

- `firecore_vent`
- `thermal_vein`
- `hot_spring`
- `basalt_flat`
- `thawed_halo`

### 9. Aurora

Aurora rains over the cold world, routes toward colder neighbors, and stores in
thick ice. Warm cells have little or no storage. Accumulation therefore depends
on flow history and reservoir capacity rather than being a monotonic color map
of temperature or ice.

Aurora surface types are:

- `aurora_field`
- `light_well`
- `frost_prism`
- `melt_line`

### 10. Pressure

Choose one deterministic deep-basin dead-god locus as a generation landmark,
not a graph entity. Generate additional ancient rogue-wave impulses and wake
paths as world history, also without entities.

Propagate bounded rays through connected ocean, attenuate them, reflect at
bathymetric boundaries, and sum deposits. Focusing and constructive overlap
produce structure that depth alone cannot predict.

Pressure surface types are:

- `pressure_god_locus`
- `crush_zone`
- `wake_scar`
- `interference_lattice`
- `rogue_impact`

### 11. Interactions

Non-exclusive field predicates produce rare boundary terrain:

- `thermal_war_rim` where aurora piles against fire-thawed ground;
- `steam_fracture` where pressure concentration intersects a fire vein;
- `pressure_aurora_breach` where pressure reaches aurora-charged coastal ice.

The interaction may become the triangle's primary render surface, but the
underlying natural surface, all three intensity values, and all anomaly flags
remain available.

### 12. Runtime-cell projection

After all physical and domain generation is complete, projection reduces each
runtime cell's 24 assigned triangles to an external summary containing:

- primary surface type;
- significant natural, fire, aurora, pressure, and interaction feature masks;
- dominant material and surface coverage;
- elevation, relief, roughness, climate, water, river, and ice facts;
- mean and maximum fire, aurora, and pressure intensity;
- reciprocal coast, river, ridge, cliff, pass, and fracture edge masks.

The existing gameplay terrain is synthesized from these facts as a temporary
compatibility projection. Its vocabulary is not expanded in this tranche.

## Validation and quality

Hard contracts:

- fixed-seed determinism;
- reciprocal triangle topology and shared feature edges;
- coherent physical landmass and shelf components;
- acyclic water and ice routing;
- every lake has one coherent supported water surface, and only a spilling lake
  has positive overflow and a physical outlet;
- classified rivers terminate at a lake, ocean, inland basin, or cropped
  external outlet;
- glacial landforms require qualifying historical maximum ice flux;
- fracture paths are connected and not identical to ridge classification;
- magic fields satisfy their source and propagation constraints;
- every surface and anomaly palette index resolves;
- every runtime-cell summary matches its 24 projected triangles;
- administrative subdivision assignment changes no physical field;
- the startup heart overlaps warmth while viable systems also exist far away.

Distribution values are tuning inputs and diagnostics, not acceptance
invariants. Ocean, land-ice, sea-ice, and lake controls still shape their causal
layers, while broader proportions and counts may guide later comparison. They
do not make generation fail: previous fixed-percentage gates repeatedly
accepted visibly bad maps and encouraged tuning to the proxy instead of fixing
geography. Final validation is structural: legal codes, coherent water bodies,
acyclic and continuous flow, conserved landscape state, valid magic sources,
and consistent aggregate output. Geographic quality is judged from rendered
fixed-seed worlds, with failures traced back to the owning causal layer.

Full radius-128 generation remains an offline minutes-scale operation;
fixed-seed interactive development should use explicit versioned `.world`
artifacts. Canonical local worlds live under the repository `worlds/` directory
and use the `ice-remembers-seed-<seed>.world` naming convention; do not generate
review worlds into ad hoc directories. Review, diagnostic, screenshot, and
gameplay seed worlds are always generated with the Release `WorldGenerator`
artifact; Debug builds are rejected by the CLI and never constitute evidence.

A seed is built through an acceptance loop that retries with successive
attempt numbers until structural validation passes. Repeated rejection is a
signal worth investigating rather than absorbing: a mid-pass throw inside a
causal layer once burned attempts silently, and a single seed accepting on
attempt 3 instead of 0 was the only outward sign.

## Development evidence tooling

Geographic quality is judged from rendered fixed-seed worlds, so the tooling
that produces evidence is part of the contract.

One presentation painter owns map colors, triangle and hex geometry, feature
overlays, river spines, and level-of-detail decisions. The live Godot node and
the headless capture utility are both thin canvas adapters over that painter,
so a defect visible in the game reproduces in a capture and vice versa. The
capture utility loads an ordinary `.world` and writes PNGs without a display
server. Divergence between the two adapters means a rendering bug, and a
defect visible in the game but not in captures points at the game's own gating
rather than at generation — materialized hexes whose live terrain has diverged
from the generated base still render their microgeography, because rivers and
relief are physical and must stay continuous.

`tools/WorldGenerator` provides generation (`--seed`, `--output`), capture
(`capture`), and diagnosis (`diagnose`). Diagnostic modes report tie-break
usage, direction attribution and river-direction histograms, spine endpoints,
water balance and lake shape, trunk-run lengths, network structure
(confluences, Strahler distribution, basins), hypsometry, landform counts,
volcanism, and stripe indices; `--audit-world` re-reads a serialized world and
proves flow continuity end to end; `--inspect=q,r[,range]` dumps per-triangle
river and water facts for a named hex neighborhood. In-game tooltips report
`(q, r, tN)` so a visual defect can be traced to exact triangles.

Capture output is likewise fixed: `capture` accepts a world from `worlds/` and
writes `.artifacts/captures/seed<seed>/`. It has no output-directory option, so
all review tooling and people inspect the same artifacts.

These numbers are regression evidence and diagnosis aids, never acceptance
criteria — with one class of exception. Assertions that check facts which are
simply true or false, such as lattice conformance and six-fold topological
isotropy, are hard gates precisely because they cannot be tuned until green.
A metric that is only ever printed is not a test: the lattice anisotropy that
revealed the twisted neighbor table sat in diagnostic output for days, read
each time as terrain character.

Three instruments sit alongside the structural contracts:

- **Lattice conformance** compares algebraic adjacency against global centroid
  geometry over the generated domain. Reciprocal neighbors and exact unit edge
  length are gates. Projection coverage and edge-mask translation are tested as
  separate contracts so a runtime grouping cannot redefine generation
  topology.
- **A structural digest** of a fixed small world is held in a committed
  baseline and diffed by the test suite. It is a change *detector*, not a gate:
  when it moves, read the difference and either accept it in the same commit
  with a reason or investigate. It never asserts that a map is good.
- **Scoped mutation testing** (`make mutation`) runs on demand over the
  algorithms and grid, where lookup tables and index arithmetic live. It
  addresses the wrong-constant class directly by reporting code that no test
  distinguishes from its mutation.

The test suite is split by wall time. `make ci` skips tests tagged
`Category=Slow` — the ones that build full worlds, kernels, domain
configurations, or Monte Carlo batches.
`make ci-full` runs everything and should be run before pushing anything that
touches worldgen, startup, wiring, or the generator catalog.

## Explicit exclusions

This replacement does not change frontier radius or pacing, entity density,
semantic world generation, simulation behavior, movement, placement, map
signals, runtime terrain mutation, or the gameplay hex-terrain catalog.

Deferred map work is recorded only in the
[post-MVP backlog](./post-mvp-backlog.md). It is not part of the closed current
scope.

Standing directive:

- **No percentage quality gates.** Fixed-proportion gates repeatedly accepted
  visibly bad maps and encouraged tuning the proxy instead of fixing geography.

## Implementation rationale

These are the current engineering choices and their reasoning. Revisit them
only from new evidence and a fresh approved proposal; closing this workstream
does not make them immutable, but it does make this document the durable
baseline rather than an active migration plan.

- **Geology is produced by ordered causal layers over persistent history
  rather than by numerical simulation.** There is no plate-motion solver,
  fluid PDE, or ice-sheet PDE. The kinematic model that exists — crust
  provinces with motion vectors, derived boundary types, and strain-driven
  uplift — was chosen for controllability and generation time, not because
  simulation was judged unable to do better. Raising fidelity here is an open
  question, and plate motion is the most likely candidate to repay it: the
  characteristic coastline forms (passive-margin platforms, rifted fjord
  coasts, arc archipelagos, volcanic isthmuses) are consequences of margin
  type, which a kinematic model can supply and noise cannot.
- **Continents come from crust provinces, assemblies, and boundary types**
  rather than a cellular-automaton builder or a thresholded fractal mask, so
  that later processes inherit causes. If coastline work needs finer structure,
  the preferred direction is more causal structure (margin type, crust age,
  accreted fragments), not replacing the causal mask with noise.
- **Roughness comes from processes, not from per-triangle elevation jitter.**
  This is distinct from the seeded jitter in the priority-flood *priority key*,
  which breaks tie ordering without perturbing any stored elevation.
