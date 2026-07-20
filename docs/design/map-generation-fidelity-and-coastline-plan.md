# Map Generation: Structural Fidelity, Coastlines, and Deferred Character

Date: 2026-07-20

Status: proposed. No implementation started.

Supersedes the completed map-generation plans (multi-epoch worldgen,
high-fidelity hydrology, geography correction, growing-map terrain review) and
the map-generation work handoff. Current generator behavior stays documented in
`physical-microgeography-worldgen.md`; point-in-time diagnosis for this round
lives in `docs/analysis/map-generation-review-20260719.md`.

## Why this plan exists

The microtriangle neighbor table was geometrically twisted for the four
diagonal sectors — an internally consistent graph that did not match the world.
It survived every existing validation because every existing validation checked
*internal consistency* (reciprocity, acyclicity, spine continuity, mask
agreement) and nothing checked *cross-representation agreement* between the
topology table and the geometry it claims to describe.

Two consequences shape this plan:

1. Test fidelity has to rise before more generator character is added, or the
   next constant-table defect will cost another multi-day hunt.
2. Every quality judgment made before the fix — including the deferred items
   below — was formed on a twisted lattice and needs re-measuring before anyone
   spends effort on it.

## Scope

In scope: generator structural fidelity, coastline realism, and the deferred
character items (overlay differentiation, wind, volcanism, magic anchoring,
slope-normalization completeness, dead quality targets).

Explicitly out of scope: frontier radius and entity density, microtriangles in
the simulation kernel, triangle-scale entity movement, gameplay use of
geography beyond the aggregate terrain contract, the expanded hex taxonomy,
vegetation and biomes, pre-Collapse megaliths, full tectonic/fluid simulation,
and the runtime terrain-mutation contract. Those are separate tranches.

## Coastline diagnosis (new work — grounded in code)

Every coastline in the generated worlds reads as a smooth, rounded passive
margin. There is no Florida peninsula, no Norwegian fjord coast, no
southeast-Asian archipelago, no Central American isthmus. Three mechanisms
explain it, and they are independent:

**Fractal deficit.** The coast is the sea-level contour of a smooth field. The
only coastal roughness is in `ContinentalDomainLayer`: four octaves of
`FractalNoise` at base scale `ContinentalWarpScale * 0.44` (≈ 23.8 world units
in the production profile). Its finest octave lands near 3 world units — about
ten triangles — at one-eighth amplitude. **The generator is structurally
incapable of producing a two-triangle peninsula or a one-triangle strait**,
because no coastal signal exists at that scale.

**Gradient insensitivity.** `ContinentalDomainLayer` applies `SmoothStep` to
continental potential, which *sharpens* the gradient exactly at the coast, and
`TectonicUpliftLayer` maps potential to elevation linearly. A steep near-shore
elevation gradient means a given elevation wiggle moves the shoreline a short
horizontal distance. Real fractal coasts come from the opposite: near-flat
coastal plains and shelves, where small vertical noise produces enormous
horizontal excursions. Florida exists because the Florida platform is nearly
level, not because the noise there is special.

**No differential erosion.** `CoastalErosionLayer` erodes purely as a function
of wave exposure, with no rock-resistance term. Wave exposure is highest on
protruding headlands, so the layer is a coastline *straightener*: it erodes
headlands and fills bays with no mechanism for hard rock to survive as a
peninsula, stack, or cape. Real coasts stay complex because lithology varies.

Type-specific coast forms then follow from the above plus existing processes:
fjords need grounded glacial valleys carved below sea level and drowned (the
`Fjord` landform code and glacial erosion already exist, but the carving depth
at coasts is unverified); archipelagos need volcanic chains plus shelf highs
isolated by the flattened-shelf change; isthmuses need interior belts crossing
shallow water at the same low gradient.

## Milestones

### M1 — Lattice conformance and symmetry tests

Lock in the corrected topology and close the hole that let it through.

- Exhaustive conformance over all 24 local triangles × 3 edges: reciprocity,
  geometric adjacency (centroid step bounded, currently ≤ 0.29 world units),
  and shared-edge midpoint coincidence computed independently from each side.
- Cross-hex neighbors land in the hex the sector faces, checked against the
  `HexCoord` direction table.
- The three sector↔direction mappings (`PhysicalMicrotriangleTopology`,
  `PhysicalHexAggregation`, `PhysicalMicrogeographyPainter`) agree with each
  other by assertion, not convention.
- Isotropy: the triangle graph is six-fold symmetric by construction, so
  purely topological statistics (edge-direction histogram, per-direction
  cross-hex link counts, degree distribution) must be uniform across the six
  sector rotations.

[DECISION] Split across two projects: the core conformance suite lives in
`Engine.WorldGen.Tests` using `PhysicalGeographyGrid` centroids as the oracle
(no Godot dependency); a second suite in `TheCanonryGame.UI.Tests` asserts
`PhysicalMicrogeographyGeometry` agrees with the same topology, covering the
render-side edge-midpoint tables.

Note for whoever implements this: the `diagnose` tool has been *printing* the
lattice anisotropy for days (`lattice edge availability` spread across eight
bins with two at 5.6%) and it read as terrain character every time. A printed
number nobody asserts on is not a test. These become assertions.

### M2 — Re-measure the deferred items on the corrected lattice

Pure measurement, no behavior change. Every judgment below was made while
graph-diffused fields smeared across geometrically wrong links.

- Re-run stripe indices (item 17). The X > Y asymmetry may already be gone.
- Re-review magic legibility (item 13): which regions are still threshold
  speckle and which are still broad washes.
- Re-review overlay similarity (item 10) and quantify it — a correlation
  matrix across the climate and magic scalar fields, so "they look like palette
  swaps" becomes a number that can be driven down.

Deliverable: a short addendum to the analysis doc recording which deferred
items survived the fix. Items that did not survive get struck, not worked.

### M3 — Representation-crossing tests and honest quality gates

- Round-trip assertions across field → blueprint → `.world` → projection →
  painter: adjacency, `MicrotriangleOffset`, and mask-bit conventions survive
  every hop. The sector-versus-direction mask bug lived exactly in such a hop.
- Dead quality targets (item 14): of roughly twenty declared `QualityTargets`,
  only four are ever read (`OceanCoverage`, `LandIceCoverage`, `SeaIceCoverage`,
  `InlandLakeBodyCount`), and those act as generation controls inside layers
  rather than acceptance gates. `DominantRiverDirectionShare`,
  `RiverChannelCoverage`, `TributarySpineCoverage`, `MajorLandmassCount`,
  `ColdLandformTypeCount` and the rest are unread configuration.

[DECISION] Delete unread targets rather than wiring them in. A percentage gate
that gets tuned until green while the map stays wrong is the failure mode this
project already learned; structural assertions and human review of captures are
the gates that work. Keep only targets a layer actually consumes.

### M4 — Coastal realism

The largest visual item.

**[DECISION — open, user's call] Does coastal character come from tectonic
margin type, or from added detail on the current smooth margins?**

Every coast form named in the request is a tectonic outcome rather than a noise
outcome. A Florida peninsula is a carbonate platform on a passive margin. The
Norwegian fjord coast is a glaciated rifted margin with a deep shelf. The
southeast-Asian archipelago is a convergent margin with island arcs, back-arc
basins, and accreted microplates. The Central American isthmus is a volcanic
arc land bridge. Real coastlines do not differ merely in how ragged they are;
they differ in *kind*, and the kind is set by what the margin is doing.

The generator has no concept of margin type. Crust provinces carry motion
vectors and boundary classification, but that information is consumed only as
strain-driven uplift and never reaches the coast. M4.1–M4.4 below add generic
raggedness — real, needed, and insufficient on their own to produce any of the
four named forms.

Two routes:

- **Detail-only.** Implement M4.1–M4.4 as written. Cheaper, lower risk, and
  produces coastlines that are irregular at every scale but still all of one
  kind. Recommended if coastline variety is a visual concern.
- **Raise tectonic fidelity first.** Make margin type a first-class generated
  fact and let the coastal layers consume it. This is the larger change and
  is described in M4.0. Recommended if coastlines are meant to carry
  *strategic* meaning — different margins afford different play.

M4.1–M4.4 are needed under either route; the question is whether M4.0 precedes
them.

### M4.0 — Tectonic margin fidelity (conditional on the decision above)

Not a full plate-motion solver. A kinematic extension of the model that already
exists, in increasing cost:

1. **Margin type as a fact.** Classify every coastal crust province as passive,
   active, transform, or collisional from the existing motion vectors and
   boundary types, and persist it. Cheap — the inputs are already computed and
   thrown away.
2. **Crust age and seafloor depth.** Track age from spreading and make ocean
   depth follow it, so basins deepen away from ridges instead of by noise. This
   is what gives passive margins their broad shallow shelves, and broad shallow
   shelves are the precondition for platform peninsulas and archipelagos.
3. **Plate drift across epochs.** Let provinces translate so boundaries migrate
   and collide, leaving inherited sutures and orogens away from present
   boundaries rather than repeatedly uplifting one fixed arrangement — which
   the multi-epoch plan already identified as a defect and never fixed.
4. **Terrane accretion.** Fragments detach and weld onto continental margins.
   This is the specific mechanism behind irregular continental outlines and
   offshore island groups, and it is the single highest-value item for the
   stated complaint.

A full mantle-convection or plate-motion solver remains a further step beyond
this and is not proposed: at hex scale most of its extra fidelity would be
invisible, generation time is already about a minute, and steering a solver
toward a playable map (warm heart, legible regions) is materially harder than
steering a kinematic model. That is an assessment, not a rule — if the
kinematic route proves insufficient, the solver is on the table.

Then three independent detail changes, in dependency order.

**M4.1 Near-shore gradient shaping.** Introduce a coastal-platform elevation
profile so the land-to-shelf transition flattens near sea level instead of
passing through it at full continental gradient. This is what converts existing
noise into large horizontal shoreline excursions, and it is a precondition for
the other two changes to be visible.

**M4.2 Multi-scale coastal displacement.** Extend coastal roughness with
octaves down to triangle scale under a power-law amplitude, and domain-warp the
coastal band so the shoreline folds rather than merely wobbling.

[DECISION] Implement as a new dedicated layer operating on the near-sea-level
band, not by adding octaves to `ContinentalDomainLayer`. Continent-scale
structure is currently good and should not be destabilized; a separate layer
follows the existing map-in/map-out contract style and is independently
testable and tunable.

**M4.3 Differential coastal erosion.** Give `CoastalErosionLayer` a rock
resistance term so headlands of hard rock survive while soft bays retreat.

[DECISION] Derive resistance from facts the generator already computes —
crust type, volcanic construction history, sediment thickness, cumulative
tectonic activity — rather than introducing an independent noise field. This
keeps the generator causal (per the design principle in
`physical-microgeography-worldgen.md`): basalt headlands and volcanic necks
survive because they are volcanic, not because a noise function said so.

**M4.4 Coast-type verification.** Confirm the characteristic forms appear:
fjords where grounded glacial valleys are carved below sea level and drowned
(verify carving depth at coasts, since the `Fjord` code exists but its
preconditions are unverified), archipelagos from volcanic chains and isolated
shelf highs, isthmuses from belts crossing the flattened shelf.

Risk: coastline complexity changes ocean coverage and shelf area, which feeds
seed acceptance and the four live quality targets. Expect to retune ocean
coverage and expect a few seeds to shift acceptance attempt. Watch generation
time — the new layer runs per-epoch over the near-shore band, not the whole
field, and should stay well inside the current ~60 s budget.

### M5 — Climate and overlay differentiation (items 10, 11)

Drive down the measured correlation from M2.

- Replace the single global wind direction with structured circulation
  (latitude bands, or per-assembly cells), so rain shadows and moisture
  gradients stop all tilting the same way.
- Decorrelate the remaining climate and magic drivers so the overlays stop
  reading as palette swaps of ocean-distance-plus-elevation.

Gate: the correlation matrix from M2 measurably improves. This is the item
most at risk of being design work disguised as a bug fix — if decorrelation
requires inventing new physical drivers, that is a design conversation, not a
tuning pass.

### M6 — Volcanic and magic character (items 12, 13)

- More mantle plumes with a strength distribution (few large, several small)
  instead of four uniform plumes for a radius-128 world, so volcanic story
  reaches more of the map.
- Apply the anchoring pattern to the fire and pressure feature layers, as was
  done for frost prisms: every feature traces to a discrete anchored cause with
  local falloff, never to a field threshold crossed over a wide area.

### M7 — Slope-normalization completeness and residual river artifacts

- Item 16: `FloodplainLayer` bank relief still compares raw elevation
  differences between neighbors; the glacial and coastal layers are unaudited
  for the same pattern. Audit every neighbor comparison that treats an
  elevation difference as a gradient and normalize by edge length.
- Item 15: two rivers rising on opposite sides of a divide can share one
  channel-fill apron, rendering as facing line-ends inside one patch. Prevent
  fill expansion from crossing a drainage divide.

### M8 — Mutation testing and change detection (test items 4, 6)

- Stryker.NET scoped to `Engine.Constants` and
  `Engine.WorldGen/PhysicalGeneration/Algorithms`, run nightly or on demand.
  Whole-solution mutation against a sixty-second generator is not viable, but
  constant lookup tables are exactly where mutation testing pays: it would have
  reported the twisted neighbor table as a surviving mutant.
- Structural digests as *change detectors*, not gates: a committed digest of
  structural facts for fixed seeds that CI diffs and a human acknowledges.
  Deliberately not thresholds.

### M9 — Elevation unit types (test item 5) — conditional

Distinct types for elevation subunits, authored units, and slope, so the
slope-versus-drop class of bug becomes a compile error rather than a silent
wrong answer. Invasive; only worth doing if M7 finds that units confusion is
recurring rather than a one-off.

## Sequencing

M1 and M2 first — both are cheap, and M2 prevents spending effort on items the
lattice fix already resolved. M3 next, since deleting dead gates changes what
"passing" means for everything after it. M4 is the main visual payload. M5–M7
follow in any order. M8 is infrastructure that can proceed in parallel. M9 is
conditional on M7's findings.

## Verification stance

Unchanged from the standing gate: structural assertions and human review of
rendered captures decide correctness. The `diagnose` tool's numbers
(confluences, Strahler distribution, direction histograms, water balance, trunk
lengths, tie-break usage, spine endpoints) are regression evidence and
diagnosis aids, never acceptance criteria. New for this plan: the M1 conformance
and isotropy assertions *are* hard gates, because they check facts that are true
or false rather than aesthetic qualities that get tuned until green.
