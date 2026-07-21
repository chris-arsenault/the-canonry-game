# Elevation causality plan

Making interior relief a consequence of geological process rather than a
field painted from a continental mask. Evidence for every claim here is in
`docs/analysis/river-relief-findings-20260721.md`; diagnostics are in
`tools/WorldGenerator/WorldDiagnosticsCli.Drainage.cs`.

River work is blocked behind this. Channel initiation, the floodplain
gradient gate, and the basin size gate all calibrate against gradient, and
today there is none inland to calibrate against.

## The deficiencies this plan must close

1. **`ContinentalPotential` saturates and drives elevation.** Pinned at
   its 1000 ceiling beyond ~25 triangles inland, so
   `baseElevation = (potential - 0.5) * 1.72 - 0.04` is a hard constant
   0.82 over 83% of land. The dominant elevation term contributes zero
   gradient across the interior.
2. **One field serves two incompatible roles.** A land/ocean mask must
   saturate or coastlines go mushy; an elevation driver must keep varying
   or interiors go flat. The mask requirement wins.
3. **Epoch 0 paints, later epochs add.** `TectonicUpliftLayer` branches
   `IsEvolutionEpoch ? ApplyIncrementalUplift : Elevation = FromScaled(sum)`,
   so the saturated table is laid down as the initial condition and
   accumulation only decorates it.
4. **Everything accumulated is long-wavelength.** `delta` comes from
   `boundaryRelief` and `belt`, both smooth continuous fields. An interior
   belt triangle gains roughly 32 units per epoch, ~97 over three, spread
   across a belt many hexes wide. Perfect accumulation of a smooth field
   yields bigger smooth swells, never ranges or divides.
5. **No crustal thickness, so no isostasy.** Elevation is painted rather
   than derived from a quantity that accumulates. Thickness has no reason
   to saturate — two collisions stack where one collided once.
6. **Tectonics reach the interior but elevation ignores them.**
   Convergent boundaries hold near 25% of triangles from the shore out to
   fifty triangles inland, yet median slope falls 204,217 to 15,886 over
   that range. Identical input, thirteenfold different output.
7. **Coastal relief is geometric, not causal.** The only convincing
   gradient in the world comes from `MarginProfile`, a distance-driven
   shaping function, not from the convergence underneath it.
8. **Erosion is inert.** Median cumulative fluvial erosion is 0.5% of the
   p10–p90 elevation spread, so nothing dissects relief into the valley
   and divide texture drainage needs.

## Non-negotiable acceptance metric

**Local gradient bucketed by distance from coast.** Global relief bands,
hypsometric spread, and whole-map slope distributions cannot distinguish
real terrain from a plateau with swells, and have now allowed this defect
to pass twice — once in `02e7fdc`, once in this session's own analysis.

No phase is accepted on a global statistic.

## Baseline (seed 4200, radius 128)

| measure | today |
|---|---|
| slope p50 at d1–3 | 204,217 |
| slope p50 at d12–25 | 23,467 |
| slope p50 at d50–100 | 13,813 |
| slope p50 at d100–200 | 9,747 |
| height p50 at d1–3 / d25–50 / d200+ | 550 / 885 / 813 |
| elevation p10–p90 spread | 695,108 |
| median cumulative fluvial erosion | 3,375 (0.5% of spread) |
| drainage basins | 16,172, median size 3 |
| basins fitting in one hex | 89.6% |
| Strahler max | 4 |

Capture the same table before and after every phase.

## Phase 0 — Guardrails before any change

Nothing here alters generation.

- Promote the coast-distance gradient table to a first-class diagnostic
  with a stable, parseable output.
- Add a **divide density** measure: local maxima on the conditioned
  surface per unit area. Relief that produces no divides has not produced
  terrain, and gradient alone can be raised by a single tilted plane.
- Record the baseline table above for at least three seeds, not one.
  Every conclusion this session rests on seed 4200.
- Add a coastline-integrity measure (land/ocean triangle counts, coastal
  crenulation) so later phases can prove they did not soften coastlines.

**Exit:** baselines captured across three seeds; diagnostics stable.

**Done.** Baselines below. The pattern holds on every seed: potential
saturated at 1000 by d50, erosion 0.4–0.5%, slope collapsing inland,
Strahler capped at 4–5.

| measure | 4200 | 42 | 9173 |
|---|---|---|---|
| slope p50 d1–3 | 204,217 | 131,788 | 115,373 |
| slope p50 d12–25 | 23,467 | 18,839 | 16,895 |
| slope p50 d50–100 | 13,813 | 16,959 | 14,890 |
| slope p50 d100–200 | 9,747 | 18,129 | 15,285 |
| potential p50 at d50–100 | 1000 | 1000 | 1000 |
| elevation p10–p90 spread | 695,108 | 788,445 | 740,618 |
| erosion as % of spread | 0.5 | 0.5 | 0.4 |
| basins | 16,172 | 12,520 | 17,115 |
| coast/land | 2.36% | 2.01% | 2.63% |
| divides per 10k land | 1,336 | 873 | 1,250 |
| pits per 10k land | 1,210 | 749 | 1,105 |
| Strahler max | 4 | 4 | 5 |

**Use the divide/pit ratio, not divide count.** Divides and pits are at
near-parity on all three seeds (1.10, 1.17, 1.13). A landscape shaped by
drainage has many more divides than pits, because pits get filled and
breached; parity is the signature of an undissected noisy plane. Divide
count alone is easy to satisfy with noise, so the ratio is the honest
gate — target a clear majority of divides.

## Phase 1 — Crustal thickness becomes the elevation source

The structural change. Everything else depends on it.

- Introduce a persistent `CrustThickness` field, advected with the crust
  by `PlateAdvectionLayer` alongside the existing histories.
- Seed initial thickness from province assembly: continental crust thick,
  oceanic thin, with genuine variation across continental interiors rather
  than a saturating ramp.
- Derive elevation by Airy isostasy — height as a function of thickness
  above a compensation depth — replacing the `ContinentalPotential` term
  in `baseElevation`.
- Reduce `ContinentalPotential` to what it can do without contradiction:
  the land/ocean mask for water classification. It no longer sets height.
- Retain `MarginProfile` for now, so this phase changes one thing.

**Exit gates**
- Interior slope (d50–200) rises materially above ~13,000; target the
  d6–12 range of ~36,000 as a first milestone.
- Elevation p10–p90 spread stays within roughly ±30% of 695,108. This is
  texture, not more height — a large rise means we tilted the world.
- Coastline integrity unchanged within noise.
- Height profile by coast distance stops being flat: interiors should show
  both high and low ground, not a uniform ~800.

**Risk:** coastlines soften if thickness and mask disagree near shore.
Mitigation is that the mask still classifies water; only height derives
from thickness. If they fight, that is a finding, not something to tune
away.

## Phase 2 — Processes write to thickness

Makes tectonics causal instead of decorative.

- Convergence and collision thicken crust; rifting thins it; accretion
  adds thickness rather than adding elevation directly; volcanism adds
  material.
- **Epoch 0 stops painting.** Crust assembly becomes a process that writes
  thickness like every other epoch, so there is no painted initial
  condition for later epochs to decorate.
- `TectonicActivityHistory` and `InheritedRelief` are retained — worn old
  orogens reading as subdued highlands is exactly right and already works.

**Exit gates**
- Convergence share and interior relief correlate, closing deficiency 6:
  interior triangles on convergent boundaries should be measurably higher
  and steeper than interior triangles that are not.
- Thickness varies across continental interiors with a spread comparable
  to the height variation it produces.

## Phase 3 — Short-wavelength structure

Accumulating a smooth field yields smooth swells. This phase supplies
structure worth accumulating.

- Convergent boundaries produce discrete ranges with steep flanks rather
  than gaussian belts — fault-bounded blocks, crests with defined edges.
- Use strain orientation so ranges have grain rather than being isotropic
  noise.
- Only now revisit `InteriorReliefStrength` and `InteriorBeltStrength`;
  they are the noise term and should be the *smaller* contributor once
  process-driven relief exists.

**Exit gates**
- Divide density rises to the point where a drainage network has structure
  to organise around.
- Gradient distribution within the interior becomes bimodal — range flanks
  distinct from intervening lowland — rather than uniformly low.

## Phase 4 — Erosion becomes consequential

Closes the loop: tectonics builds, water carves.

- Raise fluvial erosion from its current 0.5% of elevation spread to
  something that dissects. Calibrate against valley texture, not a
  percentage target picked in advance.
- Add isostatic rebound: erosion removes mass, crust rises. This is the
  natural coupling and it falls out of Phase 1 for free.
- Differential erosion by rock resistance, using the existing
  `VolcanicRockResistance` and material palette.

**Exit gates**
- Drainage basin count collapses from 16,172 toward the hundreds; median
  basin size rises from 3 into the hundreds.
- Strahler max rises from 4 toward 6–8.
- Relief survives: erosion must not plane the continents back into the
  peneplain this plan exists to remove. Re-check the Phase 1 gates.

## Phase 5 — Re-derive everything downstream

Every river and terrain constant currently in the tree was fitted against
a plateau and is suspect.

- Floodplain gradient gate: `RiverLand → Floodplain` requires low median
  gradient; steep and channelled routes to crags or a gorge terrain.
- Slope term in channel initiation, `A·Sⁿ` — meaningful only now that
  slope carries signal inland.
- Basin size gate: a minimum absolute basin discharge before any channel
  initiates, **keeping** the basin-relative fraction for tributary
  retention. Do not replace it with a global cutoff; see the analysis for
  why `8aea817` introduced it.
- Re-measure `RiverBasinHeadwaterFraction` (55 → 250 this session) and
  `CragSurfaceCut` (8 → 14 this session). Both were compensating for
  defects this plan removes.
- Re-measure `RiverAccumulationThreshold` against single-triangle runoff;
  they are currently the same magnitude (~7,000 vs 7,200), so channel
  initiation is decided by about one triangle's rainfall.

**Exit gates:** Strahler distribution, trunk run lengths, and confluence
counts bucketed **by basin size**, so suppression in one size class cannot
hide behind improvement in another.

## Sequencing notes

Phases 1 and 2 are one conceptual change split for measurability; do not
merge them, because Phase 1's gates are what prove thickness works before
processes start writing to it.

Phase 3 can be deferred if Phase 2 already produces adequate divide
density — measure before building it.

Phases 4 and 5 are independent of each other and can be reordered.

## Traps recorded

- **Do not raise `EpochTectonicElevationStep` to make relief.** It would
  lift the global relief band, which is the metric that has already let
  this defect pass twice, while local gradient stays flat.
- **Do not add relief terms beside the saturating one.** That was
  `02e7fdc`'s approach and it is why the mesa survived.
- **Do not measure globally.** Coastal rim triangles carry the entire
  whole-map slope spread and make a plateau look healthy.

## Outstanding, unrelated to this plan

- `make ci-full` has never had a valid run; an earlier "pass" was `tail`'s
  exit code through a pipe.
- `CompiledDomain_ExposesGeneratorCapabilityStructureOutsideSymbols` fails
  in placement-taste code outside this workstream.
