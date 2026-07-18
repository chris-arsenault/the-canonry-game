# Causal Physical World Generation

Status: approved replacement design, implementation in progress.

The physical world is generated as a pass-ordered dependency chain over the
canonical microtriangle graph. It is not a stack of independent noise layers.
Elevation causes drainage; drainage is eroded before glaciation; glaciation
overprints the ancestral river landscape; fire changes temperature before
aurora settles; pressure propagates through the resulting ocean geometry.

The existing prototype generator is replaced rather than retained behind a
compatibility layer. The canonical 24-triangle topology and the current
hex-scale simulation boundary remain useful contracts.

## Ownership and runtime boundary

- `Engine.WorldGen` owns domain-neutral generation grids, graph algorithms,
  uplift and erosion, connected land and ocean classification, bathymetry,
  hydrology, climate, glaciation, fractures, aggregation, and validation.
- `IceRemembers.Seed` owns the Ice Remembers generation profile, fire, aurora,
  pressure, magic interactions, physical surface taxonomy, material rules,
  administrative subdivisions, and compatibility hex-terrain synthesis.
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

## Ordered pipeline

### 1. Uplift and ancestral erosion

Generate an uplift field from a small authored set of warped continental masks
and one or two ridge axes per major land system. Ridge axes are geological
causes, not a classification applied after noise.

Low-amplitude coordinate-warped variation roughens the field without deciding
the large-scale geography. A deterministic fluvial erosion pass then carves an
ancestral V-valley drainage network before ice exists.

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
classify lakes, and establish stable outlets. Route each dry triangle to its
lowest conditioned neighbor, accumulate runoff, derive river order, and assign
watersheds. Rivers are accumulated paths and tributary networks rather than
drawn channels.

### 5. Icebound climate with warmth pockets

The world has a cold global baseline. Temperature is derived from elevation
lapse, ocean moderation, water proximity, and several deterministic warmth
systems rather than latitude or an independent noise channel.

Warmth pockets may occur offshore, on coasts, inland, or around geothermal
systems. The startup heart is guaranteed to overlap one viable warmth system,
but it is not the only warm place. High geothermal peaks may support verdant
hot springs even while surrounding alpine terrain remains frozen.

Moisture derives from open-water exposure, prevailing transport, rain shadow,
and local thaw. Tundra remains the dominant ordinary biome; open field is a
scarcer warm-steppe outcome.

### 6. Glaciation

Derive ice thickness from cold, elevation above snowline, moisture, and
concavity. Route ice flux downhill with the shared flow machinery. Stamp the
recognizable consequences of ice flow:

- widened and flattened U-valleys;
- cirques at glaciated valley heads;
- flooded fjords where carved valleys meet the sea;
- moraine ridges at glacier termini.

The landform pass runs after these elevation edits and retains glacial
landform codes.

### 7. Natural surface classification

Classify a cold-shifted climate base, then apply physical overrides in causal
order. The initial natural surface vocabulary is:

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
- glacial landforms require actual ice flux;
- fracture paths are connected and not identical to ridge classification;
- magic fields satisfy their source and propagation constraints;
- every surface and anomaly palette index resolves;
- every hex summary matches its 24 triangles;
- administrative subdivision assignment changes no physical field;
- the startup heart has viable placement geography across a fixed seed corpus.

Quality is calibrated across several deterministic seeds rather than tuned to
one screenshot. Cold generation at radius 128 targets roughly 30 seconds on the
development machine; fixed-seed cached startup should remain near five seconds.

## Explicit exclusions

This replacement does not change frontier radius or pacing, entity density,
semantic world generation, simulation behavior, movement, placement, map
signals, runtime terrain mutation, or the gameplay hex-terrain catalog.
