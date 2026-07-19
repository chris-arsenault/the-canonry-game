# Evolutionary Multi-Epoch Physical Worldgen Plan

Date: 2026-07-18

Status: approved; implementation in progress

This document is the authoritative implementation tracker for the physical
map-generation rework. It supersedes both the earlier rewrite-heavy epoch plan
and the later overly conservative layer-tuning plan.

## Governing design

Build an evolutionary multi-epoch generator by retaining the existing layer
framework and sound algorithms, replacing only incompatible internals, adding
genuinely missing physical stages, and running the resulting processes through
four configurable geological epochs.

Design feedback is cumulative. The implementation must preserve both sides of
the accepted direction:

- reuse the triangle grid, dependency contracts, fact invalidation, ocean and
  landmass algorithms, priority flood, lake model, flow graph, river ordering,
  climate machinery, ice flow, fracture graph, magic propagation, extraction,
  caching, projection, and UI framework;
- replace radial continent/ridge placement, forced geothermal peaks,
  index-biased flat routing and ice selection, standing-water runoff, and the
  one-shot glacial stamp where their causal premises are incompatible;
- add reduced tectonic provinces, precipitation, ocean currents, persistent
  bedrock and sediment history, glacial and coastal evolution, river extent,
  deltas, and configurable epochs because those processes are genuinely absent.

This is not full plate tectonics, an ocean-fluid solver, or an ice-sheet PDE.
It is a reduced deterministic process model over the canonical triangle graph.

## Canonical epoch interpretation

The world was roughly Earth-temperate before the Collapse. The apocalyptic war
killed the Dead God, broke the magical balance, and caused the Deep Freeze.
Game year 0 begins roughly twenty years after that acute freeze ended, while the
world remains inside the resulting artificial ice age.

The four configured epochs must express that history as outcomes, not merely as
names or relative strength values:

1. assemble a mature physical world under ordinary long-term climate;
2. perform major uplift followed by genuinely warm/wet erosion with widespread
   liquid drainage;
3. apply the magic-caused Deep Freeze as a catastrophic glacial overprint;
4. retreat toward the still-icebound present, reopening some water and warmth
   without erasing inherited temperate geology.

The current schedule's temperature offsets do not by themselves establish a
temperate or wet epoch. Follow-on correction work must validate actual thawed
land, liquid runoff, erosion distribution, glacial advance, and partial retreat.
Tectonic activity must likewise leave successive active and inherited relief
rather than repeatedly uplifting one fixed boundary arrangement.

The Before Times are the pre-Collapse civilization. Their serial-numbered
artifacts are high-technology weapons used in the war that killed the Dead God.
After natural geography is correct, a later domain-owned archaeology stage may
place pre-Collapse megaliths and infrastructure before the Deep Freeze so later
physical and magic passes transform them into relic sites. That stage is not a
substitute for continent, relief, climate, or hydrology quality.

## Ownership and boundaries

- `Engine.WorldGen` owns domain-neutral triangle-scale geology, climate,
  hydrology, erosion, glaciation, currents, coasts, and river geometry.
- `IceRemembers.Seed` owns the authored epoch schedule, fire/aurora/pressure
  history, final domain surfaces, and orchestration for now.
- Projection and UI inspect and render generated facts; they do not generate
  them.
- `Engine.Kernel` remains unaware of epochs and microtriangles and receives only
  compiled hex-scale startup/runtime facts.

Moving generation out of `IceRemembers.Seed` is a later mechanical ownership
cleanup and must not be mixed into algorithm work.

The implementation does not change frontier radius, entity density, semantic
generation, runtime simulation behavior, gameplay terrain mirroring, entity
placement, generators, gambits, or movement.

## Persistent and derived state

Retain only the persistent history needed to make epochs cumulative:

- crust province and type, boundary type, and tectonic strain;
- bedrock elevation and sediment thickness;
- cumulative fluvial, glacial, and coastal erosion;
- geothermal history and maximum ice thickness/flux;
- current/wave exposure and physical deposition history;
- domain fire, aurora, and pressure history.

Ocean connectivity, bathymetry, active temperature, precipitation, present
flow, active rivers, current land/sea ice, and classifications are derived and
may be invalidated and recomputed. Do not retain complete maps for every epoch.

Generation-time arrays should favor clarity and reusable scratch space within
the 4 GB target and acceptable 8 GB envelope.

## Milestone tracker

### 0. Persist approved plan

- [x] Commit and push this document before implementation.

### 1. Tectonically grounded macro geography

Observed failure: continents form a centered radial arrangement, coasts are
artificial, and geothermal activity clusters around the center.

Causal hypothesis: `ContinentalDomainLayer` explicitly constructs a central
site, surrounding rings, and straight radial ocean cuts;
`TectonicUpliftLayer` independently constructs another radial arrangement; and
`GeothermalLayer` forces five peaks.

Implementation:

- [x] Add a reduced `TectonicProvinceLayer` producing irregular provinces
  beyond the cropped map, continental/oceanic crust, motion, and boundary type.
- [x] Make `ContinentalDomainLayer` derive continuous continental potential and
  basin structure from provinces; remove center/ring and radial-channel code.
- [x] Replace the incompatible radial-axis placement in `TectonicUpliftLayer`
  while retaining continuous shaped relief for ridges, rifts, arcs, and
  trenches sourced from typed province boundaries.
- [x] Extend fractures from tectonic strain while retaining independent
  branching.
- [x] Ground geothermal systems in boundary activity, fracture intersections,
  and rare configurable mantle plumes; delete the forced five peaks.
- [x] Preserve ocean connectivity, target-calibrated sea level, ancestral
  erosion, bathymetry, and landmass connected components.
- [x] Add configurable landmass-size, spacing, island-share, coast-complexity,
  hotspot-spacing, and hotspot-distribution quality targets.
- [x] Add inspector views required to evaluate provinces and boundaries.
- [x] Add positive and negative layer tests for causal boundary behavior and
  configurable target validation.
- [x] Inspect fixed-seed output, run final milestone verification, commit, and
  push.

Primary files:

- `src/Engine.WorldGen/PhysicalGeographyGrid.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Pipeline/PhysicalGeographyFacts.cs`
- `src/Engine.WorldGen/PhysicalGeneration/PhysicalGeographyGenerationProfile.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Pipeline/PhysicalGeographyPipeline.cs`
- new and existing files under
  `src/Engine.WorldGen/PhysicalGeneration/Layers/`
- `tests/Engine.WorldGen.Tests/Map/PhysicalMacroLayerTests.cs`
- physical-geography projection/UI inspector files

### 2. Climate, sea ice, and drainage inputs

Observed failure: sea ice forms artificial slabs, the cold world lacks varied
climate structure, warmth is insufficiently distributed, and rivers inherit a
storage-order direction bias.

Implementation:

- [x] Add a reduced `OceanCurrentLayer` producing current direction, strength,
  and wave exposure from basin geometry and broad forcing.
- [x] Retain moisture transport and rain-shadow work; add a discrete
  `PrecipitationLayer` for rainfall and snowfall.
- [x] Feed currents into temperature, warmth, and sea-ice morphology.
- [x] Feed precipitation and melt into runoff; standing water contributes no
  local runoff.
- [x] Replace absolute-index flat routing with a geometric outlet gradient.
- [x] Retain priority flood, lake routing, flow targets, accumulation, pruning,
  and Strahler ordering.
- [x] Replace exact ranked sea-ice selection with a continuous suitability
  field and a target-calibrated threshold.
- [x] Ground warm habitats in geothermal systems, ocean moderation, currents,
  and sheltered basins while preserving the heart guarantee.
- [x] Add rotation and graph-renumbering tests proving storage order cannot
  select physical river or ice direction.
- [x] Inspect climate, current, ice, lake, and river-spine views; verify,
  commit, and push.

Primary files:

- new `OceanCurrentLayer.cs` and `PrecipitationLayer.cs`
- `MoistureLayer.cs`, `TemperatureLayer.cs`, `WarmthSourceLayer.cs`,
  `SeaIceLayer.cs`, `RunoffLayer.cs`, `FlowRoutingLayer.cs`, and
  `RiverNetworkLayer.cs`
- `PhysicalGeneration/Algorithms/ConditionedFlowResolver.cs`
- physical field, facts, profile, tests, projection, and inspector files

### 3. Persistent landscape evolution

Observed failure: erosion and glaciation are one-shot edits with no sediment or
geological memory, while coasts are mostly sea-level contours.

Implementation:

- [x] Add bedrock, sediment, cumulative erosion, maximum ice, tectonic history,
  and wave/coast history fields.
- [x] Retain the stream-power core of `FluvialErosionLayer`, make it
  incremental against bedrock, and produce eroded sediment.
- [x] Add `SedimentTransportLayer` for downstream transport and deposition.
- [x] Make land-ice mass balance incremental; retain `IceFlowLayer`.
- [x] Add `GlacialErosionLayer` and `GlacialDepositionLayer` using flux,
  substrate, and sediment.
- [x] Retain `GlacialLandformLayer` as classification over the evolved result.
- [x] Add `CoastalErosionLayer` using fetch, current, wave exposure, substrate,
  shelf slope, sea-level change, and available sediment.
- [x] Reconcile ocean, bathymetry, and drainage after elevation changes.
- [x] Add conservation, provenance, and malformed-input tests.
- [x] Inspect history and resulting landforms; verify, commit, and push.

### 4. River channels, floodplains, and deltas

Observed failure: a river is only a one-triangle flow spine, so major channels,
floodplains, distributaries, and deltas cannot exist.

Implementation:

- [x] Keep the existing flow spine authoritative.
- [x] Add `RiverChannelGeometryLayer` to widen channels from discharge, order,
  slope, and valley confinement.
- [x] Add `FloodplainLayer` for low-gradient terrain repeatedly affected by
  large channels.
- [x] Add `DeltaFormationLayer` using discharge, sediment, mouth geometry, and
  coastal-current energy.
- [x] Add `river_channel`, `floodplain`, and `delta` domain surfaces.
- [x] Extend aggregation, blueprint, projection, and UI to render filled
  triangle coverage instead of universal border lines.
- [x] Add positive/negative topology and rendering tests.
- [x] Inspect tributaries, wide channels, lake outlets, distributaries, and
  deltas; verify, commit, and push.

### 5. Configurable epochs and magic history

The corrected layers are made epoch-capable as they are introduced; this
milestone connects them rather than creating disposable single-pass versions.

Initial authored epochs, all configurable:

1. crust assembly;
2. orogeny and wet erosion;
3. glacial maximum;
4. retreat and the present world.

Per-epoch process order:

1. incremental uplift, rifting, fractures, and geothermal change;
2. elevation, ocean, bathymetry, and current reconciliation;
3. temperature, moisture, precipitation, and fire heat;
4. runoff, lakes, routing, rivers, fluvial erosion, and sediment;
5. land-ice balance, ice flow, glacial erosion, and deposition;
6. post-glacial ocean and drainage reconciliation;
7. aurora accumulation in qualifying cold/icy epochs;
8. pressure propagation, coastal amplification, erosion, and deposition;
9. persist history and invalidate derived fields for the next epoch.

Implementation:

- [x] Add configurable epoch definitions and epoch-aware generation context.
- [x] Add a physical evolution session that reuses the existing layer executor,
  contracts, facts, and invalidation.
- [x] Add an Ice Remembers evolution orchestrator without moving projects.
- [x] Reuse fire, aurora, and pressure propagation during epochs; accumulate
  history separately from current intensity and final feature classification.
- [x] Add a discrete domain pressure-to-coast feedback stage.
- [x] Run magic feature, interaction, and final-surface classification only
  after the final epoch.
- [x] Prove epoch count and strengths are configured rather than hard-coded.
- [x] Verify deterministic cumulative history, commit, and push.

### 6. Final classification, UI, quality, and performance

- [x] Run final derived-field reconciliation and existing natural/magic surface
  classification.
- [x] Complete inspector views for tectonics, currents, precipitation,
  sediment, erosion, maximum glaciation, river geometry, and magic age/history.
- [x] Preserve developer-world caching and responsive startup behavior.
- [x] Profile radius-128 generation and UI startup with actual hotspot data.
- [x] Validate a fixed multi-seed corpus.
- [x] Keep distribution goals configurable and hard invariants structural.
- [x] Update stable architecture/system documentation if final ownership or
  contracts changed.
- [x] Run `make ci`, commit all approved implementation changes, and push.

Performance evidence for the final implementation pass:

- startup traces identified repeated atmospheric path tracing, ocean-current
  neighbor selection, redundant epoch reconciliation, and per-frame loading
  label mutation as the actionable hotspots;
- moisture and precipitation now consume one discrete atmospheric-flow layer,
  ocean-current transport reuses one upstream graph, epoch work reconciles at
  its defined operator-split boundaries, and the loading overlay updates only
  on visible status/second changes while completion is still polled every frame;
- the exact radius-128 Release generation case completed in 3m42s with a
  4.20 GB peak working set, within the accepted 8 GB ceiling but slightly above
  the 4 GB target; and
- a cold-cache Debug launch remained CPU-bound and responsive but exceeded ten
  minutes, confirming that fixed-seed developer caching remains necessary.

## Validation policy

Configurable distribution goals include ocean and ice coverage, landmass count
and size distribution, island share, coast complexity, hotspot spacing,
represented warm habitats, inland lakes, channel coverage, tributary density,
direction balance, higher-order rivers, wide-river prevalence, and delta
prevalence.

Hard invariants are limited to deterministic output, reciprocal topology,
valid field provenance, nonnegative conserved quantities, connected water
bodies, acyclic routing, valid source-to-outlet behavior, and internally
consistent triangle-to-hex aggregation.

Each discrete layer gets positive behavior coverage and a meaningful negative
case. Tests should encode those durable contracts rather than mirror tuned
content values.
