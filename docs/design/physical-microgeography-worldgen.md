# Causal Physical World Generation

Status: implemented.

The physical world is generated as a pass-ordered dependency chain over the
canonical microtriangle graph. It is not a stack of independent noise layers.
Elevation causes drainage; drainage is eroded before glaciation; glaciation
overprints the ancestral river landscape; fire changes temperature before
aurora settles; pressure propagates through the resulting ocean geometry.

The prototype generator was removed rather than retained behind a compatibility
layer. The canonical 24-triangle topology and the current hex-scale simulation
boundary remain the useful contracts.

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

- `Engine.WorldGen` owns domain-neutral generation grids, graph algorithms,
  uplift and erosion, connected land and ocean classification, bathymetry,
  hydrology, climate, glaciation, fractures, aggregation, and structural
  validation.
- `IceRemembers` owns the Ice Remembers generation profile, physical surface
  taxonomy, material rules, tunable generation controls, and diagnostic goals.
  `IceRemembers.Seed` executes the domain generation pipeline: natural
  surfaces, fire, aurora, pressure, magic interactions, administrative
  subdivisions, and compatibility hex-terrain synthesis.
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

Every physical hex has 24 canonical triangles. Each triangle has exactly three
edge neighbors within the padded work graph. All connected passes use this
graph, and the configured radius-128 field is cropped only after generation and
validation.

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

- physical material;
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

**The topology table must agree with the geometry it describes.** Triangle
adjacency is declared in `PhysicalMicrotriangleTopology`, and triangle
positions are computed independently in `PhysicalGeographyGrid` (and again in
the renderer's `PhysicalMicrogeographyGeometry`). These are two descriptions of
one structure, and *internal* consistency of either proves nothing. A neighbor
table can be perfectly reciprocal, pass every acyclicity and continuity check,
and still be geometrically twisted — the cross-hex table was once the y-mirror
of the geometry for the four diagonal sectors, linking triangles a full hex row
apart. Symptoms were rivers that were connected in graph space but rendered as
disjoint offset segments, hex-boundary-aligned artifacts in every
graph-diffused field, phantom cliffs where twisted links joined uncorrelated
elevations, and a stubborn east/west flow bias. Any change to adjacency,
sector indexing, or hex geometry must be validated by exhaustive comparison
against world-space positions: all 24 local triangles × 3 edges, checking
reciprocity, bounded centroid step, and shared-edge midpoint coincidence
computed independently from each side.

**Sector index and hex direction are different conventions.** Geometric sector
`s` faces the hex at `HexCoord` direction `[0,5,4,3,2,1][s]`. Hex geometry and
triangle indexing use sector order; edge-mask bits use direction order. The
mapping appears in `PhysicalHexAggregation` and
`PhysicalMicrogeographyPainter`, and the two must agree by assertion rather
than by comment.

**Compare slopes, not drops.** Centroid-to-centroid steps differ in world
length by edge type, so a raw elevation difference is not a gradient: longer
steps look systematically steeper, and every process that picks a neighbor by
"biggest drop" then gutters onto the hex lattice. `PhysicalGeographyGrid`
exposes a per-edge slope scale (mean edge length over actual length); any
comparison that treats an elevation difference as a gradient multiplies by it.
Drainage guidance, descent-gradient estimation, stream-power erosion, ice-flow
descent, and the slope field all do. Ordinal comparisons — counting neighbors
higher or lower — do not need it.

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

## Persistent and derived state

Epochs are cumulative, so a small set of fields is history and everything else
is recomputed. Retain: crust province and type, boundary type and tectonic
strain; bedrock elevation and sediment thickness; cumulative fluvial, glacial,
and coastal erosion and deposition; geothermal history; maximum ice thickness
and flux; wave-exposure history; and domain fire, aurora, and pressure history.

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
while cumulative strain and uplift preserve old orogenies and rifts.

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

Use one deterministic priority-flood implementation to condition drainage,
classify lakes, and establish stable outlets. Flat routes follow propagated
outlet geometry rather than a tangent around the world origin, and lake roots
do not depend on storage IDs. Accumulate rainfall and glacial melt, derive
basin-relative headwater thresholds and Strahler order, and assign watersheds.
Every classified channel remains continuous through exorheic lakes to terminal
water. The one-triangle flow spine stays authoritative while a separate layer
widens major rivers perpendicular to local flow tangents across low-relief
banks; cross-valley floodplains and seaward delta fans remain derived coverage.

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

Lake bodies are compact depressions, not every filled hollow. Elongated
one-or-two-triangle chains along epsilon-filled valleys are river corridors and
are rejected by a compactness gate; after the submergence filter a depression
may split into disconnected lobes, and each lobe becomes its own body.

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

### 12. Hex extraction

Feature extraction reduces 24 triangles to an external hex summary containing:

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
- classified rivers terminate at a lake, ocean, inland basin, or cropped
  external outlet;
- glacial landforms require qualifying historical maximum ice flux;
- fracture paths are connected and not identical to ridge classification;
- magic fields satisfy their source and propagation constraints;
- every surface and anomaly palette index resolves;
- every hex summary matches its 24 triangles;
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
artifacts.

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

These numbers are regression evidence and diagnosis aids, never acceptance
criteria — with one class of exception. Assertions that check facts which are
simply true or false, such as lattice conformance and six-fold topological
isotropy, are hard gates precisely because they cannot be tuned until green.
A metric that is only ever printed is not a test: the lattice anisotropy that
revealed the twisted neighbor table sat in diagnostic output for days, read
each time as terrain character.

## Explicit exclusions

This replacement does not change frontier radius or pacing, entity density,
semantic world generation, simulation behavior, movement, placement, map
signals, runtime terrain mutation, or the gameplay hex-terrain catalog.

Deferred scope, set outside this workstream:

- **Vegetation and cover classification** (forest, marsh, ecological scrub,
  climate-derived aridity) is a later domain layer, and when it lands it must
  not replace or hide landform, substrate, rivers, or magic features.
- **Pre-Collapse archaeology** (roads, settlements, megaliths, weapon sites) is
  a later domain stage inserted before the Deep Freeze overprint, never a
  substitute for sound natural geography.

Standing directive:

- **No percentage quality gates.** Fixed-proportion gates repeatedly accepted
  visibly bad maps and encouraged tuning the proxy instead of fixing geography.

## Current implementation choices

These are engineering choices made inside this workstream, not directives.
They are recorded with their reasoning so that revisiting one is an informed
decision rather than a rediscovery. None of them is settled.

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
