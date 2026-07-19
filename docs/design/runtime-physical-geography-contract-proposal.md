# Runtime Physical Geography Contract — Proposal (v2)

Date: 2026-07-18

Status: proposed for review. No implementation until approved. Supersedes the
v1 draft in this file. Derived from the consumer analysis required by
[the handoff](runtime-physical-geography-contract-handoff.md) **and** from the
registered Monte Carlo diagnosis of what the simulation is actually failing to
do (`world-kinetics-20260715`, `ordered-emergence-population-plan-20260718`,
`growing-map-terrain-review-proposal-20260718`).

## The design principle: projection, not mirroring, not minimalism

The two rejected extremes failed the same test from opposite sides. Mirroring
(15 terrains, 64 flags, magnitude bands for everything) put facts in the kernel
because worldgen produced them. Minimalism (edge word plus two site bits) kept
facts out because no consumer existed yet — which is circular, because the
whole point of this tranche is to *build* the consumers that give worldgen
gameplay weight.

The correct question is: **which projection of the worldgen superset gives the
simulation the structure it is measurably missing, at an authoring cost that
stays linear?**

The measured disorder is isotropy. Every diagnosis reduces to "the world is
the same in every direction, so nothing has a reason to be anywhere or go
anywhere":

- movement is jitter, not journeys — 86% of verb walking cancels out; median
  penguin nets 11 hexes per 80 hops (`world-kinetics-20260715`);
- culture churn is arithmetically alive but spatially uniform (gains:losses
  ~1.02 everywhere) — no fronts, no visible ebb, a frozen map at zoom-out;
- clusters exist without a reason: 1.45 entities per occupied physical
  coordinate, stacking on old ground, while capacity is near-flat across the
  map (`ordered-emergence-population-plan-20260718` K3);
- physical expansion stalls near ring 21–24 of an available 128, and only 7%
  of physical movement ever enters grown cells — frontier and purpose are not
  one loop;
- meanwhile worldgen produces warmth pockets, river networks, ridge/pass
  systems, fire/aurora/pressure fields, and carved coasts that no gameplay
  system can see.

The anti-explosion rule: **categorical facts stay few (terrain IDs, edge
classes, site flags); everything gradient-like enters as small numeric bands
consumed by formulas, never by per-value authored rules.** A band multiplies a
score or a cost through one authored curve per consumer system. Nobody ever
authors "wastes × aurora-2 × cliff" — that combination space exists in the
world but not in the authoring surface.

## Contract overview

Per latent physical hex, installed once at startup, immutable at runtime:

| Field | Size | Content |
|---|---|---|
| baseTerrainId | int (existing) | dominant gameplay affordance, catalog grows 8 → 11 |
| superregionId, continentId | int (existing) | unchanged |
| crossingWord | uint16 | 6 directions × 2-bit class: Open / River / Ridge / Cliff (passes folded to Open at install) |
| fieldWord | uint16 | warmth band (2b), fire band (2b), aurora band (2b), pressure band (2b), site flags: Summit, Thermal, Anomaly, Polynya (4b) |

Plus one new **transient cover terrain**, `sea_ice`, using the existing Deep
Ice cover mechanism (cover applied over water, removed by thaw), driven by the
Ice-track world-beat ages.

Approximately 4 bytes of new immutable data per hex (~200 KB at radius 128),
and no new mutable kernel state outside the existing cover layer.

Everything else — microtriangles, materials, exact scalars, bathymetry zones,
water-body IDs, fracture conductance, the full magic surface taxonomy, lore
names — stays external, inspectable through the blueprint as today.

## 1. Terrain catalog: 8 → 11 primary IDs plus one cover

Primary terrain keeps its definition: a deliberately authored synthesis into
the **dominant gameplay affordance** (what the synthesizer already computes).
The additions are the three latent affordances the current catalog cannot
express, each with a named gameplay role:

| New ID | Source facts | Why it earns a catalog slot |
|---|---|---|
| `verdant` | warm open land (worldgen natural surface `open_field`; climate or thaw-halo warmth, high moisture) | The good land. Highest land capacity and culture influence; low travel cost. This is the terrain fronts should fight over and hubs should sit on — the substrate for localized contest that `world-kinetics` finding 2 asks for. Scarce by worldgen construction (warm-steppe is deliberately rare). |
| `fjord` | glacially carved coastal water (worldgen `fjord`; cliff-lined inlet) | The natural harbor. Wake- and shore-accessible sheltered water with dramatic approach; `harbor`/`reefhold` placement gets a real site preference instead of generic shelf. Distinct from `shelf` by shelter affordance, not by rendering. |
| `lake` | inland water bodies (hydrology-classified lakes) | Inland water distinct from ocean: freshwater site, fishing/bloom affordance, no sea connectivity for Wake movers. Makes river networks terminate somewhere visible, which corridor legibility needs. |
| `sea_ice` (cover) | frozen sea surface (worldgen `pack_ice`/`sea_ice`/`ice_shelf`) | Not a base terrain — a transient cover like Deep Ice, with the inverse gameplay: it makes water Trail-passable (slow) while blocking Wake. See mechanism M4. |

`hot_springs` and `reef` remain primary terrains (their movement-profile rows
are load-bearing affordance encoding). No other worldgen surface earns a slot:
`glacier`/`moraine`/`snowfield` are magnitude variants of `ice`/`crags` with
no distinct affordance; `polynya` is a minority feature (site flag);
`basalt_flat`/`thawed_halo` fold into the warmth band; all magic surfaces fold
into bands and the Anomaly flag.

Naming note: the physical warm-steppe cannot reuse `open_field` (taken by the
semantic catalog); `verdant` is the placeholder name pending your choice.

Catalog cost is linear and bounded: 3 terrain rows (movement profiles,
influence/claim/road multipliers, boundary group), 3 capacity rows, membership
in a few placement lists and the `MaritimeTerrains()` filter set, plus the
sea-ice cover row. Boundary groups: `verdant` → `frozen_land`'s neighbor
family (decide: own `warm_land` group shared with `hot_springs`' `sanctuary`?
— flagged as a decision), `fjord`/`lake` → `maritime`.

## 2. The field word: four bands and four site flags

### Bands (2 bits each, 0–3)

| Band | Derived from | Consumers (all formula-based) |
|---|---|---|
| Warmth | mean temperature, geothermal, thaw halos | capacity multiplier (M2), culture influence multiplier (M2), frontier thaw weighting (M5), verb target weighting (M6), generator placement affinity (M3) |
| Fire | mean/max fire intensity, vent density | generator placement affinity (M3): monsters, vents, forge-flavored content |
| Aurora | mean/max aurora intensity | generator placement affinity (M3): artifacts, memory/witness-flavored content |
| Pressure | mean/max pressure intensity (ocean) | generator placement affinity (M3): orca, deep-sea occurrence content |

Band thresholds are authored once in the seed extraction (Ice Remembers owns
what "warmth 2" means); the kernel sees only 0–3. Bands are *gradients for
formulas*: no consumer ever branches per band value through authored rules.

### Site flags

| Flag | Derived from | Consumer |
|---|---|---|
| Summit | PeakCount > 0 | `peak` PoI placement (fixes the authored `Maritime(Reef, Shelf, …)` workaround) |
| Thermal | vents/geothermal significant but not dominant | `vent` PoI, `sanctuary` settlement placement |
| Anomaly | any worldgen interaction surface (thermal war rim, steam fracture, pressure-aurora breach) or magic locus in the hex | rare-site placement for artifacts, occurrences, monsters — one bit covers the whole taxonomy; the *which* stays external for inspection/narrative |
| Polynya | open-water-in-ice feature | `bloom`/`shoal` PoI and maritime wildlife placement; stays meaningful when sea-ice cover closes the surrounding water |

Flags are consumed only by placement (a preferred-site mask scored beside
preferred terrain). Dominant-affordance hexes also carry their flag (a Hot
Springs hex is Thermal), so consumers test one fact.

### Crossing word

Six directions × 2-bit class — Open, River, Ridge, Cliff — with worldgen
passes folded to Open at install and fractures installed as Open (no
mechanic). Reciprocity validated at install. Rationale and traversal semantics
in M1.

## 3. The six mechanisms

Each mechanism names its observed failure, its consumer code, and its new
authored surface. These are the gameplay weight; the contract exists for them.

### M1 — Corridors and crossings (traversal)

*Failure:* cost fields are isotropic; routes, travel reach, and road
condensation cannot express the carved skeleton; campaign columns stall on
featureless corridors.

Directed step cost for all three traversal paths (local movement, hierarchical
portal evaluation, `TravelReachCost`):

```
DirectedStepCost = SurfaceTravelCost(to) × EdgeFactor(class(from→to), profile, roadQuality)
```

Edge factors from one compiled 4-class × 3-profile table. Proposed starting
values: River — Trail 1.6, road-negated (bridges emerge from travel); Ridge —
Trail/Shore 2.0, road-immune (ridges are crossed at passes); Cliff — Trail/
Shore 4.0, road-immune; Wake ignores all land edges. **Edges are surcharges,
never blockers**: `CanEnter` stays cell-based, so reachability semantics,
wander, and route-cache meaning are untouched, and immutable edges add zero
new invalidation.

Expected order: travel and therefore roads concentrate into passes and river
crossings; ridge lines become real barriers that shape culture and campaign
geometry without any system knowing what a "mountain" is.

### M2 — Warmth shapes capacity and culture (hubs and fronts)

*Failure:* capacity is near-flat, so clusters have no reason to be anywhere;
culture churn never localizes, so no fronts or visible ebb.

Two one-line formula hooks, both at existing per-cell read points:

- `MapSignalsProgramRuntime.BaseCapacityAt`: base capacity = terrain base ×
  warmthCapacityCurve[band]. Proposed starting curve 0.5 / 1.0 / 1.5 / 2.2.
  A warm verdant valley carries a city; frozen wastes carry nearly nothing.
  This gives the ordered-emergence plan's K3 ("strong hubs support more than
  weak sites; a cluster of six may be correct for a major city and wrong for
  an empty ice hex") its missing geographic input.
- `Simulation.TerrainInfluenceMultiplier`: influence delta = terrain
  multiplier × warmthInfluenceCurve[band] (proposed 0.7 / 1.0 / 1.2 / 1.4).
  Culture accumulates fastest on good land, so contest concentrates where the
  land is worth holding — the localization the uniform 1.02 churn ratio
  lacks. No reservoir, no decay change; this only tilts the existing
  deposition, consistent with the conservative contest-decay stance.

Claims/claim-cost stay terrain-only for now (deliberate exclusion; one curve
away if evidence wants it).

### M3 — Band- and site-aware generator placement (including magic density)

*Failure:* generators place by terrain lists alone; worldgen's magic geography
is invisible; POI subtypes are authored as terrain workarounds.

`GeneratorProgramRuntime` placement scoring gains two generic inputs per
produced entity per plane, compiled like the existing terrain filters:

- **preferred site mask** (byte): flat score bonus when `siteFlags & mask != 0`;
- **band affinities** (four signed 4-bit or float values: warmth, fire,
  aurora, pressure): score += affinity × band × constant.

Authoring is opt-in and tiny: a monster generator sets fire affinity; an
artifact generator sets aurora affinity; a deep occurrence sets pressure
affinity; most generators set nothing. Initial authored set: `peak` → Summit;
`vent`, `sanctuary` → Thermal; `bloom`, `shoal` → Polynya; monsters → fire +
anomaly; artifacts → aurora + anomaly; orca content → pressure. Roughly ten
touched profiles, each a line or two.

This is "magic density aware generator placement": the magic fields finally
pull the content that should haunt them, without a single named magic concept
entering the engine.

### M4 — Sea ice breathes: connectivity follows the ages

*Failure:* the world-beat ages change rates but the *map* never visibly
changes; sea lanes and land bridges are static; zoom-out shows no ebb and
flow.

`sea_ice` is a transient cover (existing Deep Ice mechanism, inverse
polarity): applied over eligible water — `shelf`, `fjord`, `lake`, and ocean
hexes with a coast-classified crossing... (eligibility: shelf/fjord/lake
always; ocean only when adjacent to land or existing ice) — it makes the cell
Trail-passable (slow, e.g. 5.0) and Wake-impassable, while `reef`/`ocean`
under cover keep their identity for thaw restoration, exactly like Deep Ice
restores base terrain.

Driver: the Ice-track world-beat ages. Freeze-leaning ages (Long Dark, Great
Freeze) extend cover outward from coasts and existing ice by a paced budget;
thaw-leaning ages retract it. Reuses the MapFrontier cover machinery and the
existing age-set wiring (MapFrontier already takes thaw/reclaim age sets).

Order coaxed: a Long Dark closes harbors and sea lanes but opens walking
routes between shores — migrations, stranded transports, cut-off reefholds; a
Thaw reverses it. Connectivity itself ebbs and flows with the fiction, which
is the most legible zoom-out signal this setting can produce, and it is
lore-exact for Ice Remembers.

### M5 — Frontier reveal follows the land

*Failure:* physical growth stalls near ring 21–24 and expands as an
undifferentiated rim; expansion cells receive 4.9% of influence change; thaw
has no relationship to where life could actually go.

`MapFrontierProgramRuntime` candidate selection currently takes the first
eligible neighbors in scan order. Weight candidate selection by the geography
it already reads: prefer higher warmth band, then lower crossing hostility
(fewer Ridge/Cliff edges toward known land). Thaw then advances along
valleys, coasts, and warm pockets — expansion lobes with shape, which
downstream gives explorers and cultures differentiated new territory worth
entering. (Thaw *pacing* — budgets, age availability — remains the separate
proposal identified in the growing-map review; this mechanism only shapes
*which* eligible cells thaw first.)

### M6 — Verb targeting can want warm and significant places

*Failure:* verb movement is centripetal jitter; `Press toward Heat` can only
name the Hot Springs terrain; nothing pulls journeys toward geography.

Extend low-altitude terrain-mode targeting (`Mode.Terrain`) with an optional
minimum warmth band and site-flag filter, so authored verbs can say "any
warmth ≥ 2 cell" or "any Thermal/Polynya site" instead of exact terrain sets.
Initial authored uses: `Press toward Heat` becomes band-aware; a maritime
forage verb targets Polynya. This rides the ordered-emergence K2 work
(purposeful movement) rather than replacing it: K2 owns commitment and
frontier-sector selection; this contract just makes geography one of the
things a purpose can point at.

## 4. Authoring surface accounting

The explosion the user fears is `64 flags × 15 terrains × 18 consumers` of
categorical rules. This proposal's complete new authored surface:

| Surface | Count |
|---|---|
| New terrain rows (verdant, fjord, lake, sea_ice cover) | 4 × ~10 values |
| Capacity rows for new terrains | 4 values |
| Edge factor table | 4 classes × 3 profiles = 12 values |
| Band curves (capacity, influence, thaw weight) | 3 curves × 4 values |
| Band extraction thresholds (seed-side) | 4 bands × 3 cuts |
| Generator site masks + affinities | ~10 profiles × 1–4 values |
| Verb band/site filters | 2 verbs initially |
| Placement list touch-ups for new terrains | ~8 lists |

On the order of 120 authored numbers, every one attached to a named mechanism,
zero combinatorial rules. Bands compose with terrain multiplicatively inside
engine formulas; the authored space never sees the cross-product.

## 5. What stays external

Microtriangles (permanently), materials and surface taxonomies, exact scalar
fields, bathymetry zones and water-body/watershed IDs, fracture conductance
and fracture edges, ice thickness/flux, the named magic surfaces and loci
(Anomaly flag says *that*, the blueprint says *what*), coast edges as runtime
facts (terrain + profiles already encode the land/water seam), pass identity
(folded to Open), lake/ocean distinction beyond the terrain ID, and every
Ice Remembers name. Startup placement keeps its richer blueprint access.

Deliberately excluded pending evidence: warmth effects on claims, threat
signals from fire/pressure bands, band effects on road build, pressure
effects on Wake travel cost.

## 6. Mutation model

Unchanged from v1 and compatible with everything above:

- **Immutable skeleton**: crossing word, field word, superregion, continent.
- **Mutable surface**: base terrain (rare authored repaints: scar → wastes,
  cleanse → hot springs) and transient covers (Deep Ice, now also sea ice).
- New compile-time constraint in `DomainCompiler`: authored terrain mutations
  may not cross the land/maritime boundary-group divide (both current uses
  pass). Repaints change what ground *is*; the skeleton of rivers, ridges,
  and warmth survives, which is physically coherent for both scarring and
  cleansing.

## 7. Performance

- +4 bytes per latent hex ≈ 200 KB at radius 128; two primitive arrays in
  `KernelPhysicalGeographyData`, serialized by the existing snapshot object
  path.
- Hot paths: traversal adds one uint16 fetch and a predictable branch (most
  words are zero-class); capacity and influence hooks add one fetch and one
  table multiply at existing per-cell read points; placement scoring is cold
  path; sea-ice cover uses the existing cover read that traversal already
  performs (effective terrain).
- No new epochs: immutable facts don't invalidate; sea-ice cover changes flow
  through the existing terrain-cover → `RecordTopologyChanged` path exactly
  as Deep Ice does today.
- Claims to be validated by first-tick and steady-state profiles per the
  handoff's evidence requirement.

## 8. Intended files

Contract and install: `KernelPhysicalGeographyData.cs`,
`KernelDataSnapshot.cs`, `KernelConfigurationContext.cs`,
`DomainStartupSeedInstaller.cs`, `DomainPhysicalGeographyBlueprint.cs`,
`IceRemembersSeedBuilder.Microgeography.cs` (band/flag/crossing packing),
`IceRemembersPhysicalTerrainSynthesizer.cs` (verdant/fjord/lake synthesis).

Terrain catalog: `IceRemembersTerrain.cs`, `IceRemembersDefinition.cs`
(capacity rows), `DomainConfig.cs`, `CompiledTerrainData.cs`,
`DomainCompiler.Mapping.cs`.

M1: `KernelTerrainData.cs` or sibling `KernelCrossingData`,
`MovementProgramRuntime.cs`, `.HierarchicalPathfinding.cs`,
`Simulation.MapTerrain.cs`.

M2: `MapSignalsProgramData.cs`/`Runtime.cs`, `Simulation.MapTerrain.cs`
(influence hook), `MapSignalsConfig.cs`, `GameplayCompiler.cs`.

M3: `SemanticGeneratorConfig.cs`, `GeneratorProgram.cs`,
`GeneratorProgramData.cs`, `GeneratorProgramRuntime.PlacementTerrain.cs`,
`IceRemembersEntityDefinition.cs`, `PointOfInterestEntity.cs`,
`SettlementEntity.cs`, `IceRemembersGeneratorDefinition.cs`.

M4: `MapFrontierProgramRuntime.cs` (+ a cover-extension partial),
`MapFrontierProgramData.cs`, `IceRemembersAges`/world-beat wiring,
`WorldState.MapLayers.cs` (cover eligibility only if needed).

M5: `MapFrontierProgramRuntime.cs` candidate scoring.

M6: `LowAltitudeVerbConfig.cs`, `LowAltitudeVerbProgramData.cs`,
`LowAltitudeVerbProgramRuntime.Candidates.cs`,
`IceRemembersLowAltitudeVerbs.cs`.

Mutation constraint: `DomainCompiler.cs`.

## 9. Sequencing

The mechanisms are separable and should land as reviewed slices in this
order, each behind its own registered evidence:

1. Contract install + new terrains + synthesizer update (no behavior change
   beyond terrain variety) — the substrate.
2. M1 traversal + M5 frontier shaping — corridors before anything wants to
   use them.
3. M2 capacity/influence — hubs and fronts.
4. M3 placement — content lands where geography says.
5. M4 sea ice — connectivity breathing (largest new moving part, lands last
   on a stable substrate).
6. M6 verb filters — alongside or after the ordered-emergence K2 work.

## 10. Tests and evidence

Durable contracts: word pack/unpack and install validation (reciprocity, pass
folding, band ranges, flag validity); edge-factor compile and river
road-negation; `DirectedStepCost` agreement across the three traversal paths;
capacity/influence curve application; placement mask and affinity scoring;
sea-ice cover apply/thaw restore (reusing the Deep Ice contract tests);
`DomainCompiler` rejection of maritime-crossing repaints; snapshot round-trip
of the two new arrays.

Registered Monte Carlo, paired seeds old-vs-new, judged on the metrics the
diagnoses named:

- displacement efficiency and immediate-reversal rates (M1, M6 — the
  86%-cancellation number is the one to move);
- spatial variance of culture gain/loss (M2 — churn ratio must stop being
  1.02 *everywhere*; fronts should appear at verdant/warm boundaries);
- cluster-reason correlation: stacked coordinates vs warmth band, capacity,
  and settlements (M2/M3);
- generator placement distribution vs bands and flags (M3);
- connectivity deltas across freeze/thaw ages: Trail/Wake reachable
  component sizes over time (M4);
- frontier reach and lobe shape vs the ring-21 plateau baseline (M5);
- first-tick and steady-state profile comparison (§7 claims).

## 11. Decision points for approval

1. **Terrain additions**: `verdant`, `fjord`, `lake` (and the `verdant`
   name); or trim to a subset.
2. **Boundary group for warm land**: own `warm_land` group (shared with
   `hot_springs`) vs joining `frozen_land` — affects coverage growth shape.
3. **Warmth into culture influence** (M2's second hook) now, or capacity-only
   first with influence behind its own evidence gate.
4. **Sea ice scope**: full age-driven breathing (M4) in this tranche, or
   contract-eligibility only now with the driver as a follow-up.
5. **Band affinity encoding** for generators: four optional floats per
   produced entity (proposed) vs a single dominant-band preference.
6. **Crossing semantics**: surcharge-only edges (proposed) — accepts cliffs
   slow rather than forbid.
7. Starting factor/curve numbers are calibration values, not contract.
