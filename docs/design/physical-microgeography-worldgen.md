# Physical Microgeography and World Generation

Status: implemented for deferred items 1–3 and 6–7; items 4–5 and 8–11 remain
follow-ups to the completed
[growing-map terrain substrate](../analysis/growing-map-terrain-review-proposal-20260718.md).

The implementation now uses the ordered whole-field pipeline, independent
physical material vocabulary, connected geography-shaped subdivisions, and
authored aggregate terrain synthesis described here. A later presentation
tranche added immutable projection, chunked inspection, and zoom-dependent
microtriangle rendering. Runtime utilization and triangle-scale gameplay remain
outside the completed work.

This document records the deferred physical-microgeography work and designs the
first implementation tranche. The tranche ends before simulation behavior: it
builds a good latent physical world, represents that world faithfully, and
derives the compatibility terrain of each hex before the kernel starts ticking.

The configured frontier radius and entity density are deliberately outside this
work. Another effort owns them. This design consumes the configured generation
radius without changing its value, growth pacing, or materialization rules.

## Decision

The first three deferred items are one pre-simulation feature and should be
implemented together:

1. Replace the prototype physical generator with an ordered whole-field world
   generator.
2. Replace terrain-enum microtriangles with a physical geography vocabulary.
3. Replace the triangle-count cascade with an authored, deterministic hex
   terrain synthesizer.

Splitting these would create another migration. A drainage generator needs a
flow-capable triangle shape, and the terrain synthesizer needs the fields that
the new generator produces. The generated field remains latent through the
configured radius; only the existing heart is materialized at startup.

## Current substrate and observed failure

The existing substrate establishes useful structural contracts:

- Every physical hex has 24 canonically indexed microtriangles.
- Triangle topology is reciprocal within a hex and across neighboring hexes.
- A full latent physical disk is generated deterministically before startup.
- Each hex stores a derived base terrain and geographic subdivision IDs.
- The kernel installs and snapshots the triangle arrays and hex summaries.
- Frontier materialization reveals the precomputed base terrain instead of a
  uniform fallback.

It is not yet a satisfactory geography generator:

- Microtriangle material indexes resolve directly to `IceRemembersTerrain`.
  Bedrock, surface material, landform, water, and gameplay terrain therefore
  remain one overloaded vocabulary.
- Elevation is a few layers of value noise plus an outer falloff. It has no
  explicit continental structure, tectonic structure, or drainage basins.
- Rivers are two sinusoidal channel tests. They do not flow from elevation,
  accumulate runoff, form tributaries, fill lakes, or terminate at real mouths.
- Hydrology stores only water, river, and coast flags. It cannot say where water
  flows or whether a river network is acyclic and connected.
- Continent and superregion anchors are calculated at the radius-18 heart scale
  even though the latent field is much larger.
- Crossing a subdivision boundary forcibly repaints the first sixteen triangle
  indexes as ocean, reef, or shelf. Administrative geography is therefore
  creating physical water rather than following physical geography.
- Coast is an elevation band rather than an edge between connected land and
  water.
- Hex terrain is selected by a fixed cascade of material-count thresholds.
  Important minority features and the arrangement of triangles are mostly
  discarded.
- Of the stored microgeography, only the resulting base terrain currently
  affects the world. Elevation, relief, water coverage, river/coast edges, and
  individual triangle fields have no other consumer.

The causal problem is not insufficient tuning of the existing thresholds. The
generator samples several unrelated fields independently and then attempts to
infer geography from those samples. Coasts, rivers, regions, and gameplay
terrain need to be derived in dependency order from one shared physical model.

## Complete deferred inventory

The microhex follow-up remains divided into the following chunks:

1. **Physical world generation.** Landmasses, elevation, mountain systems,
   climate, drainage, water bodies, materials, and geography-shaped
   subdivisions. Designed in this document.
2. **Physical microgeography vocabulary.** Separate material, landform,
   hydrology, climate, and feature data. Designed in this document.
3. **Hex terrain synthesis.** Derive the current hex-scale terrain and physical
   summary from the 24 triangles. Designed in this document.
4. **Runtime consistency.** Define how permanent hex transformations and
   transient cover relate to generated microgeography.
5. **Hex-scale simulation utilization.** Apply slope, water, rivers, coasts,
   and other summaries to traversal, placement, culture, roads, capacity, and
   map signals without moving entities onto triangles.
6. **Projection (implemented).** Expose generated microgeography through an
   immutable startup projection, while normal event-backed map projection
   continues to define revealed cells and current terrain.
7. **Rendering (implemented).** Draw terrain, material, elevation, climate,
   hydrology, and landform views through culled chunks. Close zoom draws the 24
   triangles plus rivers, coasts, ridges, cliffs, peaks, and passes; normal play
   respects materialization and an explicit inspection toggle reveals the
   complete latent field.
8. **Geography-specific gameplay.** Fords, bridges, mountain passes, peak
   occupation, ports, reefs, resources, and structures.
9. **Runtime microtriangle editing.** Terraforming, excavation, flooding,
   erosion, triangle feature changes, and summary recomputation.
10. **Operational observability.** Inspect geography distributions and runtime
    utilization after the simulation and projection consume the new fields.
    Generation-time correctness validation is part of items 1–3.
11. **Semantic world generation.** Improve conceptual regions and transitions
    separately. The semantic plane remains per-hex and is not part of physical
    microgeography.

## Scope and ownership

### Owning layers

- `Engine.Constants` owns the shared primitive triangle topology and generic
  numeric codes that both worldgen and the kernel must interpret identically.
- `Engine.WorldGen` owns domain-neutral physical field generation: indexed
  triangle storage, deterministic field construction, landform analysis,
  hydrology, and structural validation.
- `IceRemembers` owns the authored physical material catalog, world-generation
  profile, current terrain vocabulary, and terrain-synthesis rules.
- `IceRemembers.Seed` selects the authored profile, runs worldgen, assigns the
  Ice Remembers subdivisions, synthesizes base terrain, validates the startup
  heart, and creates the domain startup blueprint.
- `Engine.Domain.Startup` owns the stable-name transport shape.
- `Engine.Host` resolves stable authored identifiers and installs primitive
  arrays.
- `Engine.Kernel` stores bounded primitive geography and snapshots it. This
  tranche adds no tick behavior and no Ice Remembers logic to the kernel.

### What will not change

- Frontier radius, frontier pressure, thaw/reclaim pacing, or density.
- The radius of the initially materialized physical heart.
- Entity, culture, knowledge, or semantic seed placement rules.
- Semantic terrain generation.
- Movement, pathfinding, culture influence, map signals, placement, generator,
  gambit, or minor-actor behavior.
- Runtime terrain transformation or Deep Ice semantics.
- Projection payloads, UI inspection, map rendering, or art assets.
- Triangle-scale entity positions, structures, resources, or combat.
- Monte Carlo schemas or analysis. Pure worldgen correctness does not require a
  new simulation run.

## Design constraints

The implementation must preserve these constraints:

- The same complete generation seed and generation profile produce the same
  blueprint regardless of collection enumeration order.
- Geography is generated for the entire configured latent field before any hex
  is materialized.
- Rivers, coastlines, ridges, and other connected features are generated on the
  canonical triangle graph, not rolled independently per hex.
- Geographic subdivision assignment never modifies elevation, water, material,
  or landform.
- The terrain ID is a compatibility projection of geography, not the source of
  geographic truth.
- Generation may allocate freely within the application memory envelope. The
  resulting kernel data remains fixed-capacity, primitive, allocation-free on
  hot paths, and snapshot-safe.
- Prefer clear arrays and precomputed summaries over dense bit packing. The
  target working set is approximately 4 GB and up to 8 GB is acceptable.
- The generator accepts a configured radius. Feature scales are authored in
  world units and do not silently reuse the heart radius or stretch with the
  frontier limit.

## 1. Ordered physical world-generation pipeline

Worldgen operates on a padded canonical triangle graph and crops the result to
the configured latent disk. Padding is derived from the largest authored
feature scale, not from a second gameplay radius. It prevents the edge of the
playable disk from becoming an artificial coastline, watershed, or plate
boundary.

Every stage uses its own seed derived from `WorldGenerationSeed.TerrainSeed`.
Coordinate hashes and sorted candidate lists make random decisions independent
of traversal order. Later stages consume earlier fields; they do not sample a
new unrelated noise field to imitate the result of an earlier physical process.

### Stage A: canonical geometry

Create one indexed triangle graph for the padded field:

- Stable `(q, r, triangleIndex)` to dense-index conversion.
- Stable triangle centroid and corner coordinates.
- Three reciprocal neighbors per triangle.
- Explicit identification of shared hex edges and the two outer triangles that
  touch each edge.
- Stable deterministic ordering for ties.

The topology currently embedded in `Engine.Kernel` moves to the shared
primitive contract so generation, validation, installation, and runtime storage
cannot disagree about triangle adjacency.

### Stage B: macrostructure and elevation

Generate a deterministic plate-shaped macrostructure rather than simulating
full physical tectonics:

1. Place well-spaced macro sites across the padded field using an authored
   physical spacing. Site count therefore follows generated area rather than
   stretching a fixed set of sites when the configured radius changes. Each
   site receives a stable base elevation, continental/oceanic tendency, age,
   and motion vector.
2. At every triangle, measure the closest and second-closest sites. Their
   distance field defines plate interiors and boundary influence.
3. Relative motion classifies boundaries as convergent, divergent, or transform
   and contributes broad uplift, trench, rift, or roughness fields.
4. Add coordinate-warped medium- and small-scale variation. These fields add
   irregularity but do not decide land, mountain, or river identity by
   themselves.
5. Apply a broad authored heart bias that ensures the startup heart intersects
   connected, placeable land and maritime terrain. It must not paint individual
   startup locations or force uniform heart terrain.

The output is raw ground elevation and retained generation-only fields such as
plate identity, boundary influence, and uplift. The system is intentionally a
world-shape generator, not an expensive geological simulation.

### Stage C: ocean and shelf structure

Apply the authored sea level to raw elevation, then flood from the padded field
boundary to identify the connected world ocean. This distinction matters:

- Below-sea-level triangles connected to the exterior are ocean.
- Enclosed depressions are not automatically ocean; hydrology decides whether
  they become lakes or internally drained basins.
- Water depth comes from sea level minus ground elevation.
- Shelf and abyssal zones derive from depth, slope, and distance to coastline.
- A coastline is the shared edge between ocean water and non-ocean ground. It
  is not an elevation tolerance band.

The generator may guarantee adequate exterior ocean by its macrostructure
profile, but it must not apply a radial ocean ring to the cropped playable disk.

### Stage D: stable climate and geothermal fields

Generate the long-term climate fields required for material selection and
future geography-aware gameplay:

- Temperature combines the authored polar baseline, elevation lapse, broad
  climate variation, and geothermal contribution.
- Moisture starts with ocean and open-water exposure, follows an authored
  prevailing-wind direction, and loses moisture across elevated terrain to
  create rain shadows.
- Runoff is derived from moisture, temperature, permanent ice, and slope.
- Geothermal strength follows plate boundary types plus a small number of
  deterministic hotspots.

These are stable geographic normals, not tick-by-tick weather. Temperature,
moisture, and geothermal strength are retained because they explain the chosen
surface and are useful to later simulation utilization.

### Stage E: conditioned drainage and water bodies

Hydrology is solved across the entire triangle graph:

1. Treat connected ocean triangles as drainage outlets.
2. Run a deterministic priority-flood over ground elevation. Retain raw ground
   elevation and calculate a separate spill/drainage elevation; do not flatten
   visual terrain merely to make routing easy.
3. Use the priority-flood parent and stable tie ordering to assign exactly one
   downstream edge to every draining non-ocean triangle. The resulting graph is
   acyclic and terminates at an outlet or a classified inland basin.
4. Group significant filled depressions into lakes, assign stable water-body
   IDs, calculate water depth, and route their outlets. Tiny depressions may be
   drained through the priority-flood route without becoming named lake
   features.
5. Accumulate runoff downstream in topological order.
6. Classify river channels from accumulated runoff using thresholds authored in
   physical world units. Derive river order from the actual tributary graph.
7. Record the outgoing river edge, river order, drainage-basin ID, water-body
   ID, water kind, and water depth.

Rivers are therefore paths on triangle edges with sources, tributaries,
confluences, lakes, and outlets. Hex river masks are derived from those paths.
Neighboring masks cannot disagree because both read the same shared edge.

### Stage F: landforms

Classify landforms from elevation topology rather than material names:

- Slope comes from elevation differences across triangle neighbors.
- Plains are connected low-relief areas, independent of whether their surface
  is tundra, snow, or sediment.
- Valleys follow drainage and local concavity.
- Ridges follow connected uplift and local convexity.
- Peaks are ridge maxima that satisfy an authored prominence requirement, not
  every noisy local maximum.
- Passes are low saddles that cross a ridge system between navigable sides.
- Basins are enclosed concave systems; seabeds are underwater ground forms.

Every triangle has one primary landform code. Sparse flags may record concrete
secondary facts such as a geothermal vent. Coast, lake, and river identity stay
in hydrology rather than becoming mutually exclusive landforms.

### Stage G: Ice Remembers surface materials

The Ice Remembers material classifier consumes ground elevation, slope,
landform, temperature, moisture, geothermal strength, water depth, and nearby
hydrology. It assigns a physical surface material, not a gameplay terrain.

The initial stable material vocabulary should cover at least:

- `exposed_bedrock`
- `scree`
- `permafrost`
- `tundra_soil`
- `snowpack`
- `glacial_ice`
- `shelf_sediment`
- `deep_marine_sediment`
- `reef_stone`
- `geothermal_crust`

Water is represented by water kind and depth over a ground material. A reef can
therefore be shallow water over reef stone; ocean is not treated as the seabed
material. Permanent glacial or sea ice remains generated physical surface.
Transient Deep Ice remains a later runtime cover layer.

Material rules are ordered only where a physical prerequisite exists. For
ordinary classification they use scored affinities so a single threshold does
not turn all cold or dry land into one surface.

### Stage H: geography-shaped subdivisions

Subdivisions are assigned after elevation, hydrology, landforms, and material:

- Select authored macroregion anchors by geographic archetype. The Near Shelf
  anchor must intersect the connected startup-heart shelf/land system; Farwake
  prefers a major marine system; the White Empty prefers cold elevated or
  glaciated geography.
- Grow macroregions over the hex graph using deterministic weighted distance.
  Coastlines, major ridges, and drainage divides are expensive to cross, so
  boundaries tend to follow visible geography.
- Select superregion anchors within their macroregion using the authored
  Northlands, Heartlands, Coastlands, Farwake, Blackwater, White Empty, and Deep
  Empty geographic preferences.
- Repair disconnected fragments by assigning them to the adjacent region with
  the strongest connected boundary.
- Assign every latent hex exactly one existing continent and superregion ID.

This preserves stable authored subdivision identities and existing seed-region
relationships. It changes only their generated coverage. Subdivision assignment
must never repaint triangles or introduce water.

### Stage I: crop, synthesize, and export

Crop the padded generation field to the configured latent disk, aggregate the
24 triangles of each hex, run the terrain synthesizer described below, and
produce `DomainPhysicalGeographyBlueprint` in stable coordinate order.

Any cropped boundary triangle whose downstream target lies only in padding is
recorded as an explicit external drainage outlet. Cropping never converts it to
ocean or invents a coastline, and no persisted flow edge points to a triangle
outside the installed field.

## 2. Physical microgeography vocabulary

### Separation of concepts

The durable model separates four concepts:

| Concept | Meaning | Example |
| --- | --- | --- |
| Surface material | What physically covers the ground | scree, permafrost, reef stone |
| Landform | The local shape of the ground | plain, valley, ridge, peak, pass |
| Hydrology | Water state and connected flow | ocean, lake, river edge, basin |
| Base terrain | Current hex-scale gameplay compatibility label | crags, tundra, reef |

No one field attempts to encode all four. Base terrain is derived after the
other three exist.

### Persisted triangle fields

Each triangle persists direct integer fields rather than a prematurely packed
bit layout:

- `MaterialIndex`: index into the domain startup material palette.
- `LandformCode`: plain, slope, valley, basin, ridge, peak, pass, or seabed.
- `Elevation`: raw ground elevation in the shared fixed scale.
- `Temperature`: stable mean temperature in an authored fixed scale.
- `Moisture`: stable moisture availability in an authored fixed scale.
- `Geothermal`: stable geothermal strength.
- `WaterKindCode`: none, ocean, or lake.
- `WaterDepth`: zero on dry ground, positive under standing water.
- `WaterBodyId`: zero for none; stable positive ID for a connected ocean or
  lake body.
- `DrainageBasinId`: stable positive ID for the downstream basin.
- `FlowEdge`: one of the triangle's three canonical edges, or none for an
  outlet.
- `FlowAccumulation`: upstream runoff in a fixed integer scale.
- `RiverOrder`: zero for no classified river, otherwise the derived stream
  order.
- `FeatureFlags`: only sparse facts with a concrete use, initially geothermal
  vent, permanent sea ice, and permanent ice margin. General landform and water
  facts do not repeat here.

Generation-only fields such as plate ID, raw noise components, priority-flood
queue state, and distance transforms are discarded after validation and export.

The domain blueprint carries stable material names. The host installs material
indexes as their own primitive geography IDs; it no longer resolves them through
the gameplay terrain registry. Future gameplay rules may compile material IDs
explicitly, but this tranche does not add those consumers.

### Persisted hex summary

The authoritative summary derived from the 24 triangles contains:

- Base gameplay terrain ID.
- Dominant physical material ID.
- Mean, minimum, and maximum ground elevation.
- Relief: maximum minus minimum elevation.
- Roughness: mean absolute elevation change across internal triangle edges.
- Mean temperature, moisture, and geothermal strength.
- Land, ocean, lake, permanent-ice, and river triangle coverage counts.
- Peak count.
- Six-bit river, coast, ridge, cliff, and pass edge masks.
- Superregion and continent IDs.

Coverage stays as integer triangle counts in `[0, 24]`. Edge masks use the
existing stable hex direction order. Summary construction reads shared triangle
edges, ensuring reciprocal river/coast/ridge/pass facts between neighboring
hexes.

### Identity and mutation boundary

This tranche defines only generated, immutable startup geography. It does not
decide how later permanent transformations alter triangles. Until the runtime
consistency tranche is designed:

- The generated triangle field is the original physical geography.
- Base terrain remains the existing mutable hex-scale gameplay layer.
- Transient cover remains separate.
- No new runtime operation mutates a triangle or recomputes a summary.

This explicit limitation prevents the pre-simulation design from silently
choosing transformation semantics for later gameplay.

## 3. Deterministic hex terrain synthesis

The synthesizer is an Ice Remembers content component. `Engine.WorldGen`
produces a domain-neutral aggregate, while Ice Remembers rules decide which
existing gameplay terrain best represents that aggregate at hex scale.

### Aggregate facts

For each hex, the generic aggregator calculates normalized facts from all 24
triangles and their arrangement:

- Coverage by material, landform, and water kind.
- Mean and extrema for elevation, slope, temperature, moisture, geothermal
  strength, water depth, and flow accumulation.
- Connected land and water components within the hex.
- Presence and prominence of peaks, ridges, valleys, passes, vents, and rivers.
- Which hex edges each connected feature reaches.
- Whether a river enters, exits, originates, terminates, or crosses the hex.
- Whether the hex is open water, mixed coast, inland water, or land dominated.

These facts are generation-time values. The kernel receives the selected
terrain and compact summary, not an object graph or rule engine.

### Authored rule model

Each physical gameplay terrain has an authored synthesis rule containing:

- Hard eligibility predicates for physical impossibilities.
- A base score.
- Weighted affinities for normalized aggregate facts.
- A stable priority used only to break exact ties.

Selection is deterministic:

1. Aggregate the triangles.
2. Exclude physically ineligible terrain candidates.
3. Score every eligible candidate.
4. Select the highest score, breaking exact ties by authored priority and then
   stable terrain name.

Hard predicates remain narrow. For example, Ocean requires dominant connected
ocean water, while Hot Springs requires meaningful geothermal activity. Cold is
an affinity for Ice, not a rule that forces every cold hex to Ice.

The initial candidate meanings are:

- **Ocean:** dominant connected deep ocean with low land coverage.
- **Shelf:** dominant shallow water or a strongly mixed coast over shelf
  sediment.
- **Reef:** shallow connected water with substantial reef stone and suitable
  temperature/geothermal conditions.
- **Hot Springs:** land or mixed coast containing a meaningful vent system,
  geothermal strength, and surface water.
- **Crags:** high relief with connected ridge, peak, cliff, exposed-bedrock, or
  scree evidence.
- **Ice:** dominant permanent glacial/sea ice or strongly cold snow-and-ice
  surface.
- **Wastes:** dry, exposed, low-productivity land without enough relief to be
  Crags.
- **Tundra:** connected habitable land with permafrost or tundra soil and no
  stronger specialist classification.

Terrain does not erase minority geography. A Tundra hex may still contain a
river, a Crags hex may contain a pass, and a Shelf hex may contain dry islands.
Those facts remain in the summary and triangles even though the compatibility
terrain has one value.

### Rule configuration and calibration

Weights, gates, feature scales, and density targets live in one immutable Ice
Remembers physical-generation profile. They must not be scattered as numeric
literals among noise, hydrology, and synthesis functions.

Initial values should be calibrated against a fixed corpus of generation seeds,
not one attractive seed. Calibration reports at least:

- Material and base-terrain distributions for the heart and complete field.
- Connected land and water component sizes.
- Elevation, relief, temperature, and moisture distributions.
- River counts, lengths, orders, tributaries, lake connections, and outlets.
- Coast, ridge, peak, and pass density.
- Subdivision size and connectivity.
- Number of valid startup placement candidates by existing terrain profile.

Distribution ranges are authored quality targets, not permanent simulation
balance invariants. Structural facts such as acyclic drainage and reciprocal
edge masks are hard invariants.

## Validation contracts

The pre-simulation implementation is complete only when these contracts hold:

- A fixed seed produces an identical physical blueprint on repeated builds.
- Generation order does not change output.
- Every configured hex has exactly 24 triangles and all three triangle
  neighbors resolve reciprocally.
- Shared feature edges agree across neighboring hexes.
- Every non-outlet flow edge resolves to a valid neighboring triangle.
- The drainage graph has no cycles.
- Every classified river reaches an ocean, lake, explicit inland terminal
  basin, or cropped external outlet.
- Every lake has a coherent surface and outlet/terminal classification.
- Coast edges separate connected ocean water from non-ocean geography.
- Peaks have required prominence and passes connect opposite navigable sides of
  a ridge system.
- Material IDs resolve through the physical material palette, never the
  gameplay terrain registry.
- Every hex produces exactly one valid base terrain and a summary matching its
  triangles.
- Every continent and superregion is connected after repair and subdivision
  assignment changes no physical field.
- The startup heart remains valid for all existing seed entities and contains
  adequate candidates for their existing physical placement profiles.
- The blueprint, expanded summary, and triangle arrays survive kernel snapshot
  round trips.

Tests should encode structural contracts and deterministic behavior. Exact
terrain percentages and individual authored rule weights belong in diagnostic
quality reports rather than brittle content-mirroring tests.

## Proposed implementation milestones

### Milestone A: shared topology and geography data shape

- Move canonical triangle topology to the shared primitive layer.
- Add domain-neutral landform and water codes.
- Expand the domain and kernel triangle/summary fields.
- Separate physical material IDs from gameplay terrain IDs.
- Preserve installation and snapshot round trips.

This milestone may temporarily populate the new fields from the old generator,
but it must not add a compatibility layer that survives Milestone C.

### Milestone B: whole-field physical generation

- Implement padded macrostructure and elevation.
- Generate connected oceans, climate, geothermal fields, conditioned drainage,
  lakes, rivers, and landforms.
- Classify Ice Remembers physical materials.
- Replace forced subdivision-boundary water with geography-shaped assignment.
- Add hard worldgen validators and fixed-seed quality diagnostics.

### Milestone C: terrain synthesis and replacement

- Implement generic hex aggregate facts.
- Add the Ice Remembers terrain synthesis rules and profile.
- Generate the startup heart and latent blueprint exclusively from the new
  pipeline.
- Delete the prototype elevation, sine-channel hydrology, forced boundary water,
  and triangle-count cascade.
- Verify with the repository-level final pass.

## Exact intended files

The implementation proposal changes only the following surfaces, subject to
minor test fixture placement discovered during implementation.

Shared primitive contracts:

- `src/Engine.Constants/KernelCodes.cs`
- New `src/Engine.Constants/PhysicalMicrotriangleTopology.cs`
- `src/Engine.WorldGen/Engine.WorldGen.csproj`
- New `src/Engine.WorldGen/PhysicalGeographyGrid.cs`
- New `src/Engine.WorldGen/PhysicalGeographyGeneration.cs`
- New `src/Engine.WorldGen/PhysicalHydrology.cs`
- New `src/Engine.WorldGen/PhysicalLandforms.cs`
- New `src/Engine.WorldGen/PhysicalHexAggregation.cs`
- New `src/Engine.WorldGen/PhysicalGeographyValidation.cs`

Domain transport and primitive storage:

- `src/Engine.Domain/Startup/DomainPhysicalGeographyBlueprint.cs`
- `src/Engine.Host/DomainStartupSeedInstaller.cs`
- `src/Engine.Host/KernelConfigurationContext.cs`
- `src/Engine.Kernel/KernelData/KernelPhysicalGeographyData.cs`

Ice Remembers content and seed orchestration:

- New `src/IceRemembers/IceRemembersPhysicalGeography.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.Microgeography.cs`
- `src/IceRemembers.Seed/IceRemembersSeedBuilder.Subdivisions.cs`
- New `src/IceRemembers.Seed/IceRemembersPhysicalTerrainSynthesizer.cs`
- `src/IceRemembers.Seed/IceRemembersSeedQualityValidator.cs`

Durable contract tests:

- `tests/Engine.WorldGen.Tests/Engine.WorldGen.Tests.csproj`
- New `tests/Engine.WorldGen.Tests/PhysicalGeographyGenerationTests.cs`
- `tests/Engine.Kernel.Tests/TerrainAwareMapTests.cs`
- `tests/Engine.Kernel.Tests/KernelSnapshotTests.cs`
- `tests/IceRemembers.Seed.Tests/IceRemembersSeedBuilderTests.cs`

Stable documentation updated when the implementation lands:

- `docs/architecture.md`
- `docs/game-systems.md`
- `docs/systems/04-map-spatial.md`
- `docs/systems/26-growing-map.md`
- `docs/systems/simulation-principles.md`

Final implementation verification is `make ci`. No Monte Carlo run is required
until a later tranche makes simulation behavior consume the generated physical
fields.
