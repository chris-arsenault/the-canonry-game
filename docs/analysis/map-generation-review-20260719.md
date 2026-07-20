# Map Generation Review — Rivers and General Findings

Date: 2026-07-19. Point-in-time review of the map-generation tranche as of
commit `b0ea8d3`. Follow-on work is planned in
`docs/design/map-generation-fidelity-and-coastline-plan.md`. Evidence:
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

## Addendum (2026-07-20, round 3): spine continuity and stream order

Follow-up screenshots showed rivers still reading as disjoint segments. A
spine-endpoint census settled the split between data and rendering: the flow
data has only 6 interior stops in the whole seed-42 world (all
below-channel-threshold lake exits), with every other endpoint a headwater or
an ocean/lake entry. The disjoint reading was rendering:

- The spine line was drawn in flat river-blue and disappeared against
  channel-fill, floodplain, waterfall, and lake surfaces. It now draws with a
  dark casing under a bright core and stays legible on any background.
- Channel widening spreads fill upstream past the channel head, so rivers
  "lost" their spine inside their own apron. The dominant filled inflow chain
  above each channel head now carries the spine to the top of the fill.
- Rivers crossing lakes draw their flow path when they carry channel-scale
  discharge, so a river entering and leaving a lake reads as one system.

Strahler order also stopped resetting at lakes: lake cells act as order
conduits, so chained-lake systems accumulate order (order-3 river length grew
~2x, order-4 ~2.2x). On the "shouldn't max order be 6–8" question: channels
begin only after ~1,800 triangles of drainage area, which is already a natural
order-3/4 stream, so the observed max of 4 corresponds to a fully resolved
natural order of roughly 7–8. Remaining known curiosity: two rivers rising on
opposite sides of a divide can share one fill apron, which reads as facing
line ends inside one blue patch.

## Addendum (2026-07-20, round 4): in-game river holes and hex-lattice leakage

In-game screenshots still showed hex-shaped river gaps that the capture
harness never reproduced. A serialized-world audit (`diagnose
--audit-world=...`) proved the `.world` flow data gapless (zero flow breaks
across ~40k river triangles), which isolated the cause to the game's render
path: `PhysicalMicrogeographyLayer` hid a hex's entire microgeography whenever
the live kernel terrain no longer matched the generated base terrain — so
every sim-changed hex punched a hole through any river crossing it. Diverged
hexes now render their microgeography with a translucent live-terrain wash on
top; only unmaterialized (unrevealed) hexes stay hidden.

The separate hex-boundary quantization (river-valley crescents hugging hex
edges map-wide, rivers running along hex rows) came from comparing raw
elevation drops between neighbor centroids whose distances differ by edge
type: longer steps always looked steeper, so drainage, erosion history, ice
flow, and the slope field all guttered onto the hex lattice. The grid now
carries per-edge slope scales (mean edge length over actual length), and every
drop comparison — guidance scoring, descent-gradient estimation, stream-power
erosion, ice-flow steepest descent, and `MeasureSlopes` — multiplies by them,
comparing true slope instead of drop.

Observation noted for a future tranche: several climate/magic overlays read as
palette swaps of one another because they share the same drivers
(ocean-distance, elevation, one global wind); differentiating their dynamics
is design work, not a bug fix.

## M2 baseline (2026-07-20): deferred items re-measured after the lattice fix

Measured on seed 42, radius 128, with the corrected lattice. Which deferred
items survived:

**Item 17 (stripe asymmetry) — survived, but probably not its own defect.**
Moisture X-stripe 0.27 against Y-stripe 0.11; temperature 0.16/0.08;
elevation 0.16/0.09. The column-vs-row asymmetry is intact. The most likely
cause is item 11: a single global wind direction tilts every moisture and
rain-shadow gradient the same way, which is exactly what a directional stripe
measures. Do not treat this as separate work — re-measure after M5 and expect
it to fall with the circulation change.

**Item 10 (overlay similarity) — confirmed, now quantified.** Pairwise Pearson
correlation over land triangles:

| pair | r |
| --- | --- |
| moisture / snowfall | 0.77 |
| temperature / rainfall | 0.65 |
| geothermal / fire | 0.63 |
| temperature / moisture | 0.62 |
| temperature / elevation | −0.59 |
| rainfall / fire | 0.50 |

This is the baseline M5 must improve. The geothermal/fire pair is the sharpest
single instance of the complaint: fire intensity is close to a recolouring of
geothermal, so it needs structure of its own rather than a shared driver.

**Pressure is ocean-only by design** — the Dead God locus sits in the ocean and
wake scars propagate through ocean triangles, so pressure is constant on land
and correctly absent from land overlays. Not a defect; noted so the zero row in
the correlation matrix is not mistaken for one.

The correlation matrix is now part of `diagnose`, so M5 has a measurable gate
rather than an aesthetic one.

## M5 result (2026-07-20): the overlay complaint is mostly not a data defect

Two of the three things M5 set out to fix turned out to be misdiagnosed, and
the measurements say so plainly.

**Item 17 (stripe asymmetry) is continent placement, not wind.** The M2 note
guessed the column-versus-row asymmetry tracked the single global wind.
Replacing that wind with banded circulation moved moisture X-stripe only from
0.270 to 0.261. The reason is in `MoistureLayer`: moisture is
`720 − oceanDistance·k − rainShadow`, so it is dominated by distance to ocean,
and wind enters only through the rain-shadow term. Distance to ocean varies
strongly by column because this world's continents sit in a north-south band.
The asymmetry is a true description of the geography, not an artefact. **Item
17 is closed as not-a-defect.**

**Item 10 (overlay similarity) is mostly causal physics.** Ranked by
correlation, the strongest pairs are moisture/snowfall 0.79,
temperature/rainfall 0.63, temperature/elevation −0.59 — snow is cold rain,
warm air carries more water, and air cools with height. Decorrelating those
would make the world *less* physical, not more. The one pair worth acting on
was geothermal/fire, which shared a driver without fire having structure of its
own; making fire propagation conduit-limited along fractures rather than
heat-limited moved it 0.63 → 0.60 and, more importantly, made firecore reach
anisotropic so it reads as flow along lineations instead of a halo.

The residual complaint that overlays look like palette swaps is therefore
largely a **rendering** matter — similar colour ramps over fields that are
legitimately correlated — and belongs with the presentation layer rather than
with the generator. Distinct visual encodings per overlay would address it
without falsifying the physics.

**Item 11 (single global wind) is fixed on its own terms.** Prevailing wind is
now banded, with alternating belts, cross-belt turning, and terrain-scale
wander, so rain shadows are no longer globally parallel even though the metric
that prompted the change turned out to measure something else.

## M4 result (2026-07-20): coasts now differ in kind

Seed 42, radius 128, after margin classification, crust age, margin-dependent
near-shore flattening, margin-dependent coastal displacement, and differential
coastal erosion:

| margin | land | shelf per coast | crenulation |
| --- | --- | --- | --- |
| passive | 238,806 | 19.15 | 3.54 |
| transform | 64,960 | 13.36 | 5.46 |
| rifted | 82,149 | 10.17 | 6.80 |
| convergent | 99,773 | 8.04 | 6.20 |

Crenulation is coastline triangles over the square root of enclosed land area:
a smooth blob sits near 4 and climbs as the shoreline becomes ragged. Passive
margins are broad-shelved and comparatively smooth, rifted and convergent
margins are narrow-shelved and deeply indented, transforms sit between. That
separation is the M4.0 acceptance criterion and it holds. The world also
carries 32 islands against 6 major landmasses, and 1,441 neck triangles
(land with ocean on two or more sides) — peninsulas, isthmuses, and strands.

**Residual: fjords remain rare (7 triangles).** The `Fjord` branch was
previously unreachable — glacial landform classification skips water, so a
valley drowned by later sea-level rise never carried the `UValley` code that
the branch tested. It now keys on cumulative glacial erosion, which is
persistent history and survives drowning, so the path works. The reason the
count stays low is upstream: little glacial carving happens on the shelf that
the glacial-maximum sea-level drop exposes, because grounded ice is thin at
those margins. Making fjord coasts common needs the glaciation model to route
ice onto exposed shelves, which is its own piece of work. Lowering the
threshold further would relabel ordinary seabed as fjord, which is worse than
reporting few.

## Addendum (2026-07-20, round 5): the twisted lattice

Persisting disjoint rivers — visible even in capture-harness output while
every data audit reported zero flow breaks — finally reduced to one root
cause. An exhaustive check of all 72 (sector, triangle, edge) neighbor
correspondences against world-space geometry found 8 broken pairs: the
`PhysicalMicrotriangleTopology` cross-hex neighbor table was the y-mirror of
the render geometry for the four diagonal sectors, so those links connected
triangles a full hex row apart. The generation graph was internally consistent
and reciprocal — every structural validation passed — but geometrically
twisted: flow "teleported" one hex row wherever it crossed a diagonal-sector
hex edge.

That one table explains the persistent symptom cluster: rivers rendering as
offset disjoint segments (connected in graph space, not in world space),
hex-boundary-aligned artifacts (the twisted links sit exactly on diagonal hex
edges), the stubborn E/W direction bias (twisted vertical links joined
spatially uncorrelated elevations and read as cliffs, so coherent descent only
formed along the geometrically true east/west edges), and correlated smearing
in every graph-diffused field.

After correcting the table (verified 72/72 midpoint coincidence, all centroid
steps ≤ 0.29 world units): 12-step river direction share in the two E/W bins
fell from ~66% to 11% with all twelve bins populated, Strahler order reaches
5, land slope p90 fell from ~108 to ~32 units (the phantom cliffs were
twisted-edge jumps), and rivers render as connected dendritic trees in both
the capture harness and the game. The radius-10 test harness needed a colder
temperature baseline once ocean moderation flowed across the corrected graph.
Tooltips now show (q, r, tN) microtriangle coordinates for precise defect
reports.
