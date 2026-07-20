# Map Generation Review — Rivers and General Findings

Date: 2026-07-19. Point-in-time review of the map-generation tranche handed off
in `docs/design/map-generation-work-handoff.md` (commit `b0ea8d3`). Evidence:
the ten seed-42 captures in `.artifacts/captures/seed42-shared-renderer-final/`
plus a quantitative diagnostic run (`tools/WorldGenerator` `diagnose` mode,
radius 128, seed 42, production profile and epochs — the same configuration as
the captures).

## Headline numbers (radius 128, seed 42)

- 29,617 river-channel triangles, but only **669 confluences** across **713
  basins** — the average river has slightly under one confluence. The network
  is parallel threads, not trees.
- Maximum Strahler order is **3** (21,462 order-1, 6,465 order-2, 1,690
  order-3). A drainage network on ~505k land triangles should reach order 6–8.
- 12-step net river direction: **57% of all windows head due east or due
  west** (0°: 26.2%, 180°: 30.5%). Order-3 windows instead concentrate
  north/south (270°: 29.8%). Minor rivers run E/W in parallel rows; the few
  trunks run N/S. Mean straightness (net/path) is 0.61.
- Land elevation above sea level: **p25 = 923, p50 = 959, p75 = 1000** units.
  Half of all land sits in a 77-unit band. p5 is 372, so even "low" land is
  high; land at all elevations below ~370 units barely exists.
- Median land slope is **2.5 units**; p99 is 845. Relief is a knife-edge
  coastal cliff around a nearly flat interior plateau.
- Per-step river drops: p50 = 2.1 units, but 616 spine steps drop ≥100 units
  (giant waterfalls, in the data) — and nothing in the landform, surface, or
  render vocabulary expresses them.
- Volcanism: 18,161 triangles carry volcanic construction (max cone 1,911
  units) from only 4 mantle plumes. It exists but reads poorly against the
  plateau (see below).
- Stripe index (variance of column means / total variance): elevation
  X = 0.27 vs Y = 0.15. The vertical banding visible in the captures is in the
  generated data, not only in the renderer.

## Root cause 1 — continents are mesas (drives almost everything else)

`TectonicProvinceLayer.AssignTriangle` computes continental potential as a
sigmoid of *coastal distance*: `0.5 + (oceanicDist − continentalDist) /
(2 × transitionWidth)`, clamped to [0,1], floored at
`ContinentalCrustPotentialFloor` (0.62). With `ContinentalTransitionWidth = 14`
triangles, everywhere more than ~14 triangles inland saturates to potential
1.0. `TectonicUpliftLayer` then maps potential to base elevation, so the entire
continental interior starts at one uniform height (~820 units) plus ±120 units
of noise (`ElevationVariationStrength` 0.12).

Consequences:

- The elevation view renders as flat tan with a cliff to ocean — exactly the
  measured p25–p75 band.
- Interior gradients (~2 units/step) are smaller than the relief the drainage
  code needs to make topography-driven choices, so flow direction is decided
  by conditioning artifacts (root cause 2).
- Fluvial erosion cannot fix it: stream power scales with accumulation^0.42,
  and because flow never converges (root cause 3), accumulation stays low and
  erosion stays uniform — the flatness is self-preserving.
- Mountains exist only as tectonic-boundary ridges; there are no interior
  highlands, basins, or lowland plains. 914 peaks vs 42,244 passes in the
  landform histogram is the same symptom: elevation thresholds assume land
  spans a range it doesn't.
- Volcanic cones (up to 1,911 units) sit on a 960-unit plateau under an
  absolute-range elevation ramp (`ElevationColor` normalizes by world max with
  a 0.55 exponent), so a cone reads as a small bright dot rather than a
  mountain rising from varied terrain.

The thought-process error: continental potential is a *land mask* being reused
as a *height field*. A mask saturates by design; a height field must vary in
the interior (cratons, platforms, rift basins, interior seas).

## Root cause 2 — priority-flood tie order manufactures the straight parallel rivers

The chain:

1. `DrainageConditioningLayer` runs `PriorityFlood.FillFromOcean`. The
   `MonotoneRadixPriorityQueue` dequeues equal priorities in **insertion
   order** (FIFO buckets), and ocean seeds are enqueued in triangle-index
   order, which follows `HexCoord.CoordsInRadius` — a deterministic west-to-east
   column sweep. On the plateau (root cause 1) nearly all interior triangles
   tie at the same filled elevation, so the flood becomes a lattice BFS whose
   parent forest is laid down in sweep order: long, straight, axis-aligned
   parent chains. First-visitor-claims (`DrainageRanks[neighbor] >= 0` guard)
   locks them in.
2. `ConditionedFlowResolver.SelectTarget` aims each triangle at its
   `ParentLookahead` ancestor **12 steps** down that chain
   (`DrainageGuidanceLookaheadSteps`), and on flat ground (conditioned drop
   ≤ 0) the score is dominated by the alignment term
   (`DrainageOutletAlignmentWeight/10 = 32` effective vs a rank-progress term
   capped at 3). The hashed `CoordinateTie` only fires on near-exact score
   ties, which the alignment term prevents. So guidance reproduces the
   BFS chains almost verbatim.
3. `FlowRoutingLayer` copies guidance targets as final routing; rivers are the
   high-accumulation subset of that field.

Result: dense parallel drainage aligned to the lattice/sweep axes, breaking
into right-angle jogs where wavefronts from two coasts meet — the
circuit-board look in the whole-map hydrology view, the stacked E/W river rows
two hexes apart in the cold-surface view (uniform runoff on parallel chains
crosses the channel threshold at the same distance from the coast, so rivers
all start on the same contour), and the dead-straight N/S trunks.

Note the same tie-break idiom appears elsewhere: `AuroraFieldLayer`
`ColdestNeighbor` breaks temperature ties by *lower triangle index*, so aurora
charge also flows in lattice order across flat temperature fields. Any
"follow the best neighbor" pass with a deterministic index tie-break will
axis-align on flats; this is a generator-wide idiom to fix once.

## Root cause 3 — nothing makes flow converge

Real dendritic networks come from convergent topography. Here valleys barely
exist (root cause 1), guidance chains are parallel (root cause 2), and the
inherited-valley term (`DrainageInheritedValleyWeight` 180/1000, capped at 100
units of cumulative erosion) is too weak relative to alignment to make one
channel capture its neighbor. 669 confluences over 713 basins and Strahler max
3 quantify it. Tributaries, wide trunks, and meaningful deltas cannot emerge
until parallel channels are made to merge — either by real interior relief, by
strengthening erosion inheritance across epochs, or by an explicit capture
step (e.g., join adjacent same-target chains into the deeper channel).

## Quality-gate note

`DominantRiverDirectionShare` target is 0.25 ± 0.25 — a river field where 50%
of flow shares one of six lattice directions passes. The measured 57% E/W
share (26% + 31% across the two opposite bins) would pass a naive per-bin
check. Consistent with the handoff's quality-gate lesson: the percentage gate
was tuned until green while the maps stayed wrong.

## Secondary findings

- **Waterfalls and gorges exist in data, not in vocabulary.** 616 spine steps
  drop ≥100 units and 1,740 drop ≥50, mostly on the coastal cliff descent, but
  `PhysicalLandformCodes`, `IceRemembersPhysicalSurface`, and the painter have
  no waterfall/gorge/canyon concept, so the most dramatic hydrology renders as
  an ordinary river line. Cheap win: classify high-drop channel steps and
  incised narrow valleys, and render them.
- **13% of land is lake** (65,666 basin triangles). Combined with
  `LakeDepthThreshold` 18 on a flat interior, depressions pond into large
  inland seas and speckled small lakes rather than draining. Visible as the
  regular pale-blue triangle dots in the cold-surface capture.
- **Elevation render compresses what relief there is.** Absolute-range
  normalization with a 0.55 exponent puts all land in a narrow tan band of the
  ramp. A hypsometric (percentile-based) ramp over land would make existing
  variation visible immediately — worth doing even though the deeper fix is
  root cause 1.
- **Vertical banding is partly data.** Elevation X-stripe 0.27 vs Y-stripe
  0.15; moisture and temperature show the same X > Y asymmetry. Continent
  placement explains some, but the faint periodic stripes in the ocean render
  suggest an additional renderer or noise-lattice artifact worth a focused
  look (value-noise integer lattice in `PhysicalNoise` is world-axis-aligned).
- **Wind is a single global direction per seed** (`AtmosphericFlow
  .WindDirection`: one hashed base angle plus a gentle turn field). Coherent
  but monotone: one prevailing wind for the whole planet, so rain shadows and
  moisture gradients all tilt the same way. Fine for now; worth latitude bands
  or per-assembly variation later.
- **Magic legibility split.** The good regions (volcanic island halo) come
  from causal, spatially anchored sources. The noisy ones are per-triangle
  threshold speckle over accumulated fields (fire/aurora feature layers in
  warm zones); the bland ones are broad fields proportional to ice thickness
  or coastal adjacency, which paint the same ring on every coast and the same
  wash on every ice sheet. Pattern to apply: every feature should trace to a
  discrete anchored cause (vent, locus, breach) with local falloff, not to a
  field threshold crossed over a wide area.
- **Only 4 mantle plumes** (`MantlePlumeCount`) for a 128-radius world; with
  drift they produce good local features (the liked craters and cones) but
  most of the map has no volcanic story. More plumes with a strength
  distribution (few large, several small) would spread character without
  rewriting anything.

## What is sound and should be kept

The layer/contract architecture, fixed-point elevation, priority-flood +
guidance + routing separation, spine/coverage split, Strahler ordering,
pruning, lake routing, the shared painter, and the validation style are all
solid engineering. Every defect above is a data-shape or tie-breaking problem
inside a sound pipeline — none requires a rewrite of the pipeline itself.

## Suggested fix order (diagnosis-first, smallest structural set)

1. Give continental interiors a real height field (multi-scale potential:
   cratons/basins/platforms), replacing the saturating coastal sigmoid as the
   sole base-elevation source. This unlocks everything downstream.
2. Break flat-ground determinism at the source: randomize equal-priority
   dequeue order in `PriorityFlood` (seeded shuffle within FIFO buckets, or a
   small hashed jitter added to filled elevations on ties) and audit other
   best-neighbor passes for index tie-breaks.
3. Make convergence win: raise inherited-valley weight / lower alignment
   weight on flats, and/or merge adjacent parallel chains sharing a lookahead
   ancestor. Verify by confluence count and max Strahler order, not coverage
   percentages.
4. Add waterfall/gorge classification + rendering, and a hypsometric
   elevation ramp (visual wins, small diffs).
5. Re-tune lake thresholds and landform elevation thresholds against the new
   hypsometry; revisit magic feature anchoring afterwards.

Acceptance evidence stays human review of captures, per the standing quality
gate, but the diagnostic's confluence count, Strahler max, direction shares,
and elevation percentiles are the right regression numbers to watch.

## Addendum (2026-07-20): fixes applied

All five fix stages above were implemented. Measured on the same radius-128
seed-42 configuration, before → after:

- Land elevation p25/p50/p75 above sea level: 923/959/1000 → 196/386/609 with
  max 1221 — a full hypsometric pyramid (74% of land ≥200u, 8% ≥800u) instead
  of a mesa. Median land slope 2.5 → 18.9 units.
- Confluences: 669 → ~1,800; Strahler orders now reach 4; delta triangles
  908 → ~2,400.
- Guidance tie decisions (the tie-break-randomization usage metric): 0.01% of
  ~21.6M decisions — effectively zero, as required.
- Elevation column-stripe index: 0.27 → 0.16; rainfall 0.17 → 0.10.
- New landforms live at sensible densities: ~1,800 waterfalls, ~1,000 gorges,
  rendered as surfaces, landform colors, and spine markers.
- Seed 42 now accepts on attempt 0 (~45 s); the previous 3-attempt rejection
  loop was a mid-pass coastline-emergence throw in coastal forcing, now
  handled as coastline movement.

What changed, briefly: interior relief (macro highlands/basins plus
epoch-stable ridged mountain belts) is added at base-elevation construction in
`TectonicUpliftLayer` and persists through all epochs via bedrock and a small
per-epoch belt uplift share; `PhysicalNoise` got a proper avalanche hash and
golden-angle octave rotation (this removed the vertical striping and diamond
coastlines the old weak hash caused, which the old low amplitudes had hidden);
`PriorityFlood` fills with seeded tie jitter and a hashed 1–8-subunit epsilon
gradient so no flat conditioned surfaces survive; `LakeBasinLayer` regroups
depressions by adjacency against the spill surface; drainage weights favor
inherited valleys over outlet alignment; lake thresholds (45u depth, 24
triangles) turn noise-ponds into through-flowing collector valleys; and
gorge/waterfall classification plus a hypsometric land ramp landed in the
shared painter.

Remaining known cosmetic bias: 12-step river windows still trend E/W (~65% in
the two 0°/180° bins) because steepest-descent choice is quantized to the
triangle lattice's edge availability (E/W edges are 3× more available than
vertical). Visually this now reads as regional flow direction rather than
ruler-straight parallels; if it needs further softening, the next lever is a
world-space smoothed-gradient desired direction replacing parent-chain
lookahead in `ConditionedFlowResolver`.

## Addendum (2026-07-20, round 2): review feedback fixes

In-game screenshot review surfaced four issues; root causes and fixes:

1. **"Broken river bits" at micro zoom were mostly not rivers.** The pale
   dashed E/W rows were frost-prism surfaces: `AuroraFeatureLayer` retained
   them where the smooth aurora field crossed its threshold on Ridge/Slope
   landforms — a contour band, the field-threshold anti-pattern. Prisms now
   retain only on Peak/Ridge with a hashed scatter, so they cluster on ridge
   systems. The second contributor was narrow "worm lakes": adjacency-grouped
   depressions one or two triangles wide along epsilon-filled valleys rendered
   as spine-less blue chains. A compactness gate (`LakeCompactnessShare`, ≥56%
   of members need two lake neighbors) reclassifies them as river corridors.
2. **Water dominance.** Interior basins now form at ~52% of highland amplitude
   (`InteriorBasinDepthShare`), and with the compactness gate, seed-42 water
   fell from ~70% to 58.3% total (ocean 52.3%, lake 6.0% in 72 bodies, median
   356 triangles). Lakes read as features again rather than the dominant land
   surface.
3. **Uniform oceans.** Reef gates were unreachable in a cold world (temp 300
   with geothermal 150); reefs now accept warm-current heat as an alternative
   gate. Two new ocean surfaces derive from existing facts: `current_channel`
   (strong ocean currents) and `trench` (deep-ocean plate boundaries under
   high strain), with palette colors. Oceans now carry strategic texture:
   shelf, reef arcs, current lanes, trench seams, ice states.
4. **Parallel-comb streams / few trunks.** Two-pass drainage guidance adds a
   channel-capture bonus (log2 of first-pass discharge,
   `DrainageCaptureWeight`), and the priority-flood epsilon is heavy-tailed
   (quarter of cells cheap, rest ~12×), so the flood's shortest-path tree
   concentrates into dendritic corridor valleys instead of evenly spaced
   parallel chains. Trunk runs of order ≥2 now reach 200–350 triangles
   (90+ runs over 40). Lake-body splitting after the submergence filter fixed
   a routing crash the new epsilon exposed (disconnected lobes shared one
   body id).

The aggregate direction metric is essentially unmoved (~65% E/W over 12-step
windows) and proved insensitive to alignment weight ×20, epsilon jitter, and
the capture pass — the residue is lattice legality itself (conditioned descent
along fill chains whose E/W neighbors are spatially closest and therefore
elevation-correlated). The visual, however, no longer reads as combs; the
remaining trend resembles regional drainage. Treat further de-biasing as its
own tranche if it still bothers review: the lever would be conditioning, not
scoring.
