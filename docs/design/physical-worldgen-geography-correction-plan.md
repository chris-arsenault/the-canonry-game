# Physical Worldgen Geography Correction Plan

Date: 2026-07-19

Status: approved; implementation in progress

This is the authoritative tracker for the correction pass prompted by review of
`a.png` through `d.png`. It evolves the existing multi-epoch, layered generator;
it does not replace the triangle graph, dependency contracts, or established
physical processes.

## Canonical premise

The world was roughly Earth-temperate before the Collapse. The apocalyptic war
killed the Dead God, broke the magical balance, and caused the Deep Freeze.
Game year 0 is roughly twenty years after the acute freeze ended, while the
world remains in the resulting artificial ice age.

Generation must therefore build mature temperate geology first, then overprint
it with a late magic-driven glaciation and partial retreat. Present ice may hide
older geography; it must not prevent that geography from having formed.

## Ownership

- `Engine.WorldGen` owns domain-neutral tectonics, impacts, climate, hydrology,
  erosion, glaciation, landforms, and river geometry.
- `IceRemembers` owns the authored epoch schedule and domain geography profile.
- `IceRemembers.Seed` owns natural-surface and magic classification.
- Projection and UI own compositing and relief presentation.
- `Engine.Kernel` remains unaware of epochs and microtriangles.

## Observed failures and causal hypotheses

1. Continents remain a few centered blobs because continental anchors are
   restricted to the configured center and each assembly repeatedly claims the
   nearest unselected province to its original anchor.
2. Epochs repeat uplift over one fixed boundary map. They accumulate erosion
   and ice history but do not distinguish active from inherited tectonic relief
   or allow selected rifts and collisions to change through time.
3. The nominal warm and wet epochs remain below widespread snowmelt conditions,
   so liquid runoff and landscape incision concentrate around exceptional warm
   pockets.
4. Conditioned-flow tie resolution contains a tangent around the world origin,
   producing parallel directional river bias. Other equal-rank choices retain
   storage-index tie breaks.
5. Cold precipitation creates little runoff, short headwaters are aggressively
   pruned, and no explicit contract proves that every classified channel
   remains connected through lakes to an ocean, inland basin, or crop outlet.
6. River widening is graph-neighbor dilation rather than cross-valley channel
   geometry, producing blocky lattice-shaped channels and poor confluences.
7. Final magic surfaces and the default UI surface view can hide continuous
   river geometry. Surface views also discard elevation shading, making real
   relief less legible.
8. Sparse anomalies cannot provide global geographic variety. The physical
   landscape needs inherited temperate valleys, rifts, glacial forms, and rare
   impact basins before any vegetation or archaeology layer is added.

## Milestones

### 0. Persist this plan

- [x] Commit and push this tracker before implementation.

### 1. Continental assemblies and evolving tectonics

- [x] Retain the existing province sites, Voronoi assignment, motion, boundary
  typing, padded work graph, and continental-potential pipeline.
- [x] Replace centered nearest-anchor acquisition with deterministic frontier
  growth over province adjacency, including nuclei outside the configured crop.
- [x] Bias assembly growth into coherent elongated chains and fragments rather
  than compact disks.
- [x] Apply bounded multi-scale margin warping only near the continental
  transition; do not classify continents from raw fractal thresholds.
- [x] Add a discrete epoch tectonic stage that varies active strain and rift
  separation while retaining cumulative tectonic history and inherited relief.
- [x] Let strong early uplift decay into lower later activity instead of
  repeatedly renewing the same ridge everywhere.

Primary files:

- `src/Engine.WorldGen/PhysicalGeneration/Layers/TectonicProvinceLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/ContinentalDomainLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/TectonicUpliftLayer.cs`
- new `src/Engine.WorldGen/PhysicalGeneration/Layers/TectonicEpochLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/PhysicalGeographyEvolutionSession.cs`
- physical field, facts, profile, and macro/evolution tests

### 2. Climate history and cold drainage

- [x] Retune the authored schedule so the wet epoch produces widespread liquid
  precipitation and runoff as an outcome, not just a positive offset.
- [x] Keep the Deep Freeze as the glacial maximum and the final epoch as partial
  retreat into the still-icebound present.
- [x] Add seasonal/glacial ablation, basal/geothermal melt, and proglacial
  discharge to runoff without treating standing water as local rainfall.
- [x] Preserve cumulative fluvial erosion as ancestral or seasonally dry valley
  evidence when present flow is weak.

Primary files:

- `src/IceRemembers/IceRemembersPhysicalGeography.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/RunoffLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/TemperatureLayer.cs`
- `src/IceRemembers.Seed/PhysicalGeneration/Layers/NaturalSurfaceLayer.cs`
- profile, climate, evolution, and classification tests

### 3. Drainage direction and continuity

- [x] Retain priority flood, basin classification, flow accumulation, pruning,
  and Strahler ordering.
- [x] Replace the world-origin tangent with outlet-rooted flat routing.
- [x] Remove integer triangle IDs as physical tie breakers; use deterministic
  geometry or coordinate hashing where a final tie remains.
- [x] Prove rotational behavior over the complete triangle graph and prove that
  the final geometric selector ignores renumbered candidate storage IDs.
- [x] Enforce classified source-to-outlet continuity through lakes and external
  crop outlets, including lake outflow reaches.
- [x] Calibrate tributary retention by basin and discharge rather than relying
  on one global accumulation cutoff alone.

Primary files:

- `src/Engine.WorldGen/PhysicalGeneration/Algorithms/ConditionedFlowResolver.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/LakeBasinLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/FlowRoutingLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/RiverNetworkLayer.cs`
- hydrology algorithm and layer tests

### 4. River geometry and rendering

- [x] Keep the one-triangle flow spine authoritative.
- [x] Derive a local channel tangent from upstream and downstream geometry.
- [x] Widen toward perpendicular low-relief banks, with width driven by
  discharge/order and constrained by valley confinement.
- [x] Merge tributaries cleanly and derive floodplains and delta distributaries
  from valley and mouth geometry rather than generic neighbor dilation.
- [x] Composite rivers, lakes, and deltas in ordinary surface views so magic
  classification cannot visually sever them.

Primary files:

- `src/Engine.WorldGen/PhysicalGeneration/Layers/RiverChannelGeometryLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/FloodplainLayer.cs`
- `src/Engine.WorldGen/PhysicalGeneration/Layers/DeltaFormationLayer.cs`
- `src/TheCanonryGame.UI/PhysicalMicrogeographyChunk.cs`
- river geometry and UI render-decision tests

### 5. Sparse physical impacts

- [x] Add an early domain-neutral impact layer with configurable sparse crater
  count and size distribution.
- [x] Excavate basins, raise fractured rims, produce ejecta/sediment, and expose
  heat only where impact scale warrants it.
- [x] Run later hydrology, erosion, and glaciation over impacts so they become
  lakes, breached basins, buried rims, or ice-filled scars rather than decals.
- [x] Keep Ice Remembers magic free to react to impact facts without defining
  the physical crater.

Primary files:

- new `src/Engine.WorldGen/PhysicalGeneration/Layers/ImpactCrateringLayer.cs`
- physical field/facts/profile and evolution session
- focused landscape-evolution tests

### 6. Inherited landforms, UI relief, and quality

- [x] Classify old river valleys, outwash, impact basins/rims, and inherited
  orogenies from persistent physical history.
- [x] Add restrained elevation shading to normal macro and micro surface views
  without obscuring surface identity or structural water overlays.
- [x] Tighten configurable goals for distributed coast complexity, per-landmass
  variety, cold-region variety, basin branching, river continuity, and flow
  direction balance.
- [ ] Evaluate the existing pregenerated worlds before generating replacements;
  regenerate the corpus only after the corrected implementation is complete.

Primary files:

- `src/Engine.WorldGen/PhysicalGeneration/Layers/LandformClassificationLayer.cs`
- `src/IceRemembers.Seed/PhysicalGeneration/Layers/NaturalSurfaceLayer.cs`
- physical and domain generation profiles and validation layers
- macro and micro geography UI renderers

### 7. Verification and handoff

- [ ] Exercise durable layer contracts with positive and negative cases.
- [ ] Inspect macro continents, relief, climate history, complete drainage,
  tributaries, channel geometry, impacts, and default surface rendering.
- [ ] Run final repository verification after all milestones are complete.
- [ ] Update stable generation documentation to the final implemented behavior.
- [ ] Commit and push each completed milestone and the final verified result.

## Explicit deferrals

- Vegetation/cover classification (`forest`, `marsh`, ecological scrub, and
  climate-derived aridity) is a later domain layer. It must not replace or hide
  landform, substrate, rivers, or magic features.
- Pre-Collapse roads, settlements, megaliths, and weapon sites are a later
  domain archaeology stage inserted before the Deep Freeze overprint.
- No full plate-tectonics solver, generic cellular automaton continent builder,
  fluid PDE, or direct fractal-threshold continent mask is being added.
- This pass does not change frontier radius, entity density, semantic worldgen,
  runtime simulation behavior, gameplay terrain mirroring, generators, gambits,
  or the kernel's hex-scale boundary.
