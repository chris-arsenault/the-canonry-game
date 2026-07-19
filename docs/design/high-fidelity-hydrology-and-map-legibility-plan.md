# High-Fidelity Hydrology and Map Legibility Plan

Date: 2026-07-19

Status: approved; implementation in progress

This is the authoritative tracker for the next physical-map tranche. It evolves
the existing layered, multi-epoch generator. It does not replace the canonical
microtriangle graph, epoch orchestration, priority-flood principle, flow graph,
river accumulation/order, domain magic pipeline, startup blueprint, or UI
projection boundary.

## Ownership

- `Engine.WorldGen` owns high-resolution physical elevation, explicit water and
  drainage surfaces, basin/glacier interaction, routing, erosion, rivers, and
  volcanism.
- `IceRemembers.Seed` owns domain surface classification and coherent magic
  feature regions.
- Projection and UI own relief, river presentation, and inspection views.
- The app host owns unattended rendered-map capture.
- `Engine.Kernel` remains unaware of microtriangles and generation fields.

## Diagnosed failures

1. Smooth geological elevation is rounded to whole integer units before
   hydrology, and later uplift, erosion, and deposition also lose fractional
   changes. Many neighboring triangles therefore become spuriously coequal.
2. Priority flooding correctly derives flat spill surfaces, but routing then
   resolves too many choices with crust motion and a consistent turn
   preference, producing lattice-aligned rivers.
3. Terrain floor and water depth are separate, but lake level is implicit and
   classified lakes prevent later land ice from shaping their floors.
4. The authoritative flow spine can be continuous while filled river coverage
   renders it as broken, blocky, or disconnected through lakes.
5. Cold relief is visually suppressed and broad magic surfaces hide natural
   structure; independent feature thresholds make warm regions noisy.
6. Geothermal sources do not construct persistent volcanic geography.

## Milestones

### 0. Persist the approved plan

- [x] Record ownership, failures, intended stages, verification policy, and
  explicit non-goals before implementation.

### 1. Unattended rendered evidence

- [x] Add an application capture mode that loads a normal `.world`, selects
  deterministic cameras/views, captures the actual Godot viewport, and writes
  a manifest beside the PNGs.
- [x] Capture whole-world surface/elevation/hydrology views and deterministic cold,
  warm, and major-river crops.
- [x] Keep captures as human-review evidence, not pixel-golden tests.

### 2. High-resolution physical elevation

- [x] Use deterministic signed fixed-point generation elevation with 1,024
  subunits per existing authored elevation unit.
- [x] Keep profile values in existing authored units and convert them through
  one explicit unit boundary.
- [x] Preserve fractional uplift, erosion, deposition, floor, water surface,
  and conditioned drainage values throughout every epoch.
- [x] Replace the range-sized monotone bucket queue with a deterministic
  monotone radix queue suitable for fine priorities while retaining the
  existing priority-flood algorithm.
- [x] Export rounded compatibility elevation plus diagnostic fine elevation;
  do not add high-resolution state to the kernel.

### 3. Explicit floor, water surface, and drainage surface

- [x] Keep physical floor authoritative as bedrock plus sediment.
- [x] Store actual ocean/lake surface separately and derive water depth from
  surface minus floor.
- [x] Store conditioned drainage surface and outletward routing rank separately
  from both terrain and actual water.
- [x] Split drainage conditioning into a dependency-declared layer; priority
  flooding must never mutate the physical floor.
- [x] Preserve one flat lake surface over irregular basin floors.

### 4. Glacial basin evolution

- [x] Permit glaciers to advance over frozen inland water during qualifying
  epochs using the supported ice/water surface.
- [x] Erode submerged floors only where the glacier is grounded; floating ice
  must not carve the bed.
- [x] Allow glacial deposition to construct sills and moraine dams.
- [x] Invalidate and recompute lakes after each glacial landscape edit.

### 5. Conditioned routing and river topology

- [x] Add a discrete drainage-guidance layer using high-resolution local
  downhill gradient, conditioned outlet direction, and physically compatible
  inherited valleys.
- [x] Restrict guidance to legal descending or outletward-flat candidates.
- [x] Remove crust motion and global turn handedness from flow selection.
- [x] Use coordinate-stable deterministic selection only for exact final ties.
- [x] Preserve sustained headwaters and tributaries through confluences while
  pruning only unsupported reaches.
- [x] Require every classified channel to reach ocean, lake, endorheic water,
  or the configured crop outlet.

### 6. River rendering and terrain legibility

- [x] Render narrow rivers as connected segments over the authoritative spine,
  including confluences, lake entry, and lake outflow.
- [x] Retain filled geometry for genuinely wide channels, floodplains, and
  deltas.
- [ ] Strengthen scale-aware relief from elevation, slope, and roughness.
- [ ] Composite broad magic fields over natural geography instead of hiding
  relief and structural water.
- [ ] Classify fire, aurora, and pressure regions with causal per-feature
  hysteresis rather than independent noisy thresholds.

### 7. Persistent physical volcanism

- [ ] Add stable mantle-plume roots and epoch volcanic construction before
  climate/hydrology.
- [ ] Use crust motion over persistent roots to produce age-progressive
  seamount/island chains and occasional continental cones, calderas, and lava
  plains.
- [ ] Let later erosion, glaciation, sea-level change, and rivers reshape the
  constructed relief.
- [ ] Keep fire magic reactive to volcanic provenance rather than causal.

### 8. Verification and handoff

- [ ] Use durable unit/contract tests and only targeted small-world integration
  tests during milestones; do not run the heavy full suite after each stage.
- [ ] Trace one complete radius-128 generation after core hydrology and optimize
  demonstrated hotspots only.
- [ ] Treat approximately 4 GB as the target and 8 GB as acceptable.
- [ ] Generate the four fixed worlds once after implementation, capture matching
  rendered evidence, and review geography directly rather than accepting it
  through distribution percentages.
- [ ] Run `make ci` once as final verification, update stable documentation,
  commit, and push.

## Explicitly unchanged

- Continental and coastal construction, apart from local volcanic construction
  and ordinary later erosion.
- Frontier radius and entity density.
- Kernel/runtime microtriangle storage and gameplay-terrain mirroring.
- Vegetation, forests, marshes, and pre-Collapse archaeology.
- Full plate tectonics, fluid simulation, and ice-sheet PDEs.
- Random per-triangle elevation jitter and percentage quality gates.
