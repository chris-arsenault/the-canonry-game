# River and relief findings — 2026-07-21

Point-in-time findings from investigating why landmasses read as marsh and
why rivers appear as parallel unconnected rills. Measured on seed 4200 at
radius 128 unless stated. Diagnostics live in
`tools/WorldGenerator/WorldDiagnosticsCli.Drainage.cs`.

## The headline finding: continents are mesas

Slope split by height above sea level:

| height | triangles | median slope | % channel |
|---|---|---|---|
| 0–25 | 3,236 | 219,280 | 23.0% |
| 25–60 | 2,844 | 128,860 | 23.1% |
| 60–120 | 4,664 | 117,504 | 25.4% |
| 120–250 | 11,534 | 99,279 | 23.5% |
| 250–500 | 44,444 | 43,711 | 15.0% |
| **500–1000** | **430,208** | **12,528** | 4.3% |
| 1000+ | 189,132 | 23,538 | 1.4% |

83% of land sits in a single band at 500–1000m with a median slope of
12,528 — seventeen times flatter than the coast, and far below the
scree-field threshold of 73,728. The 1000m+ band is flatter still than
250–500m, so the high ground is tableland rather than mountains.

The continents therefore have elevation but almost no interior relief: a
high flat plateau ringed by a narrow steep coast of roughly 22,000
triangles, 4% of land.

**This is the root cause.** A flat plateau has no drainage divides, so
there is nothing for a drainage network to organise around and the
priority flood's epsilon fill ends up choosing flow direction instead of
the terrain. Everything below follows from it: 69% of all channel
triangles sit on that plateau, basins fragment into thousands of
three-triangle units, parallel strands never converge, flat ground pools
into lakes, and interiors read as marsh.

### Relief lives only at the coast

Walking outward from the ocean separates "far inland" from "high":

| distance from ocean | triangles | height p50 | slope p50 |
|---|---|---|---|
| 1–3 | 31,406 | 550 | 204,217 |
| 3–6 | 41,103 | 596 | 41,970 |
| 6–12 | 67,760 | 667 | 36,295 |
| 12–25 | 94,000 | 812 | 23,467 |
| 25–50 | 113,728 | 885 | 15,886 |
| 50–100 | 168,131 | 822 | 13,813 |
| 100–200 | 134,208 | 749 | 9,747 |
| 200+ | 35,726 | 813 | 14,698 |

Slope falls 21x from shore to deep interior and is effectively gone by 12
triangles inland, while height barely moves (550 to 885 to 813). The
continent climbs to roughly 800 units within three triangles of the sea
and is then a table at that height for hundreds of triangles.

Erosion is not responsible: median cumulative fluvial erosion is 3,375
subunits against a p10–p90 elevation spread of 695,108, or 0.5%.

Suspected mechanism, not yet confirmed: in `TectonicUpliftLayer`,
`baseElevation = (potential - 0.5) * 1.72 - 0.04`. If
`ContinentalPotential` saturates near its maximum across continental
interiors, `baseElevation` is constant inland and the coast is merely
where potential sweeps from 0.5 to 1.0. Interior texture would then have
to come entirely from `InteriorReliefStrength` (0.45) and
`ElevationVariationStrength` (0.12), which are long-wavelength and weak
against a saturated base — meaning interior relief cannot be fixed by
tuning the relief terms while the base term saturates.

### Methodological warning

A whole-map slope distribution spans three orders of magnitude
(p10 3,498 → p99 1,989,827) and looks perfectly healthy. That entire
spread comes from the 4% coastal rim. Measuring the map globally and
concluding the elevation model could differentiate headlands was wrong,
and it pointed the investigation away from the real defect for a round.
Split by locality before concluding anything about relief.

## Secondary findings

### Floodplain is a category error a third of the time

A hex reads `floodplain` once 8 of its 24 triangles carry a channel, with
no gradient condition, and `RiverValleyMinimumOrder = 1` means a single
order-1 rill qualifies a triangle. Of 1,309 hexes reading floodplain, 427
(32.6%) have a median slope at or above the scree threshold. A floodplain
is a low-gradient depositional feature by definition; that ground is a
ravine wearing a floodplain's name.

### Basin definition creates the fragmentation count

`FlowRoutingLayer.AssignBasins` starts a new basin for every land
triangle that drains directly to ocean, so a long coastline yields
thousands of basins. Seed 4200: 16,172 basins over 778,066 non-ocean
triangles, median size 3, 89.6% fitting inside one hex. This is the
definition behaving as written, not a failure to connect — `PriorityFlood`
provably reaches every triangle from the sea. But it makes per-basin
maxima meaningless at the low end.

### Basin-relative thresholds put a river in every puddle

`RiverNetworkLayer.BuildChannels` sets the channel threshold as a
fraction of the basin's own maximum discharge, so every basin gets
channels regardless of size. A three-triangle coastal basin has a
threshold of roughly a third of one triangle's runoff.

The rule exists for a good reason. It was introduced by `8aea817` to
satisfy the plan item *"calibrate tributary retention by basin and
discharge rather than relying on one global accumulation cutoff alone"* —
a single global cutoff kept tributaries only in the largest basins and
left the rest riverless. **Do not replace it with a global absolute
cutoff**; that reintroduces the bug it was written to fix.

### Channel initiation ignores slope

Real channel initiation follows contributing area and slope together
(steep ground channelises with a small catchment, flat ground needs a
large one). We use area alone, so flat ground channelises at the same
catchment as steep. Note this cannot help while the interior is a
plateau — with no gradient there is no slope signal to weight by.

### Absolute thresholds have drifted out of scale

A single triangle's runoff is roughly 7,000 while
`RiverAccumulationThreshold` is 7,200, so channel initiation is decided
by about one triangle's rainfall. Thresholds should be expressed in
contributing-triangle equivalents so they cannot drift when world wetness
changes.

## Intended work, in order

Relief first. Every constant below was calibrated against plateau data
and will need re-measuring afterwards.

1. **Interior relief.** Confirm whether `ContinentalPotential` saturates
   inland, and if so stop `baseElevation` depending on it in a way that
   flattens interiors. Erosion is already ruled out as the cause. Target:
   interior median slope rises well above 12,528 while the p10–p90
   elevation spread stays near 695,108 — texture, not more height.
2. **Gradient gate on floodplain.** `RiverLand → Floodplain` requires low
   median gradient; steep and channelled routes to crags or a gorge-like
   terrain.
3. **Slope term in channel initiation.** `A·Sⁿ` rather than area alone.
   Redistributes rather than adds: flat ground loses channels, steep
   ground keeps what it earns.
4. **Basin size gate.** A minimum absolute basin discharge before any
   channel initiates, keeping the basin-relative fraction for tributary
   retention within qualifying basins.
5. **Re-measure the compensating constants.**
   `RiverBasinHeadwaterFraction` (55 → 250 this session) and
   `CragSurfaceCut` (8 → 14 this session) were both fitted against the
   defects above and are suspect.

Gate each on Strahler distribution, trunk run lengths, and confluence
counts bucketed by basin size — not on total channel coverage, which
hides suppression in one size class behind improvement in another.

## Outstanding

- `make ci-full` has never had a valid run; an earlier "pass" was `tail`'s
  exit code through a pipe.
- `CompiledDomain_ExposesGeneratorCapabilityStructureOutsideSymbols` fails
  in placement-taste code outside this workstream.
