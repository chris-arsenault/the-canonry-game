# Runtime Physical Geography Contract — Proposal (v3)

Date: 2026-07-20

Status: proposed for review. No implementation until approved. Supersedes v2.

v3 exists because the ground changed under v2:

- **Canon changed.** This is not an intrinsically frozen planet. The world was
  temperate before the Collapse; the Deep Freeze was an artificial glacial
  catastrophe that ended ~20 years before game year 0. The present map is a
  temperate world's geography under a *retreating* artificial ice age
  (`docs/lore/encyclopedia/deep-freeze.md`).
- **Worldgen was rebuilt** into multi-epoch evolution: tectonic provinces,
  wet-era erosion, glacial maximum and retreat, coastal evolution, impact
  craters, mantle-plume volcanism, high-fidelity hydrology with river spines,
  floodplains and deltas, and a vocabulary of 44 surfaces, 17 landforms, and
  12 materials (`physical-microgeography-worldgen.md`).
- **The user named a missing deliverable**: the map→hex mapping has never been
  designed intentionally, and the mapping scheme — terrain, flags, and all —
  must be part of this proposal.

The MC diagnoses that drove v2 are unchanged and still govern (isotropy:
jitter not journeys, uniform culture churn, flat capacity, frontier stall).
So does the anti-explosion rule: **categorical facts stay few (terrain IDs,
edge classes, site flags); gradients enter as small numeric bands consumed by
one authored curve per consumer system, never by per-value rules.** The
combination space exists in the world, never in the authoring surface.

The proposal now has three parts: the **hex projection scheme** (map → hex),
the **kernel contract** (hex → simulation), and the **mechanisms** (contract →
gameplay order).

---

# Part I — The hex projection scheme (map → hex)

## What is wrong today

`IceRemembersPhysicalTerrainSynthesizer` is a competitive scorer over **eight
terrains** using **material counts** (TundraSoil, Permafrost, ReefStone…) plus
a few aggregates. It predates the worldgen rebuild and was explicitly labeled
"a temporary compatibility projection." Concretely:

- It never reads the surface classification. The generator spends twelve
  causal layers deciding a triangle is `fjord`, `delta`, `cold_steppe`,
  `basalt_flat`, or `polar_barrens` — and the synthesizer then re-derives a
  terrain from substrate materials, discarding that judgment. A fjord becomes
  "shelf," a delta becomes "tundra," a lava plain becomes "wastes."
- Its 8-slot catalog is the old "eternal ice with warm pockets" world. The
  new canon's landscape — warm steppes, river lands, carved inlets, inland
  lakes, volcanic fields, frozen seas — has no identities to land in.
- It produces one ID and drops everything else: no flags, no bands, no edges
  reach gameplay.
- Scoring-melee weights are untestable and unexplainable; nobody can say *why*
  a hex is Wastes.

## Design: a two-stage projection with an explicit precedence ladder

**Stage 1 (exists, keep): triangles → hex facts.** `PhysicalHexAggregator`
already reduces 24 triangles to coverages, means/maxima, peak counts, edge
masks, dominant material, and primary surface. Stage 1 is not the problem.

**Stage 2 (redesign): hex facts → gameplay projection.** Replace the material
scorer with a documented, ordered classification that consumes what worldgen
actually concluded — **surfaces, landforms, water kinds, and coverages** —
and emits the full contract: terrain ID, initial cover, field word (bands +
site flags), and crossing word.

### 2a. Terrain classification ladder

The ladder is deterministic and reads top-down; each rule is a named,
individually testable predicate. Magic surfaces never decide terrain — each
maps to its underlying natural surface for classification (magic enters
through bands and flags), preserving worldgen's own rule that magic overlays
never erase the natural fields.

```
1. Water partition: hex is water if WaterCoverage ≥ T_water
   (single threshold, startup-only, no hysteresis needed).

2. Water hexes, in order:
   lake      — LakeCoverage dominates water coverage (inland WaterKind)
   fjord     — Fjord landform/surface present + coastal + high relief
   reef      — reef surface coverage ≥ T_reef
   shelf     — shelf/exposed-shelf bathymetry dominates (coastal shallow)
   ocean     — otherwise

3. Land hexes: dominant natural-surface group by triangle vote through the
   surface→terrain affinity table (§2b), with three ordered tie-breaks:
   geothermal dominance beats ice; relief/peak evidence beats flat groups;
   river-lands beat tundra when floodplain+delta+wide-river coverage ≥ T_river.

4. Initial covers (recorded per latent hex, applied at reveal):
   sea_ice   — water hex whose surfaces are pack_ice / sea_ice / ice_shelf
               (the frozen seas of the continuing ice age)
   deep_ice  — unrevealed frontier, as today (the unretreated Freeze)
```

### 2b. The surface→terrain affinity table

One authored row per natural surface — **linear in surfaces, the whole
point** — assigning each to a terrain group (weights only where a surface is
ambiguous). Sketch:

| Terrain (13) | Natural surfaces and landforms that vote for it |
|---|---|
| `ocean` | ocean, trench, current_channel |
| `shelf` | shelf, exposed_shelf |
| `reef` | reef |
| `fjord` | fjord (surface + landform) |
| `lake` | lake |
| `verdant` | open_field, cold_steppe |
| `floodplain` | river_valley, river_channel, floodplain, delta |
| `tundra` | tundra, wet_tundra |
| `wastes` | polar_barrens, snowfield(dry), permafrost-dominant tundra |
| `crags` | ridge, scree_field, gorge, waterfall + Peak/Ridge/ImpactRim landforms |
| `ice` | glacier, moraine, snowfield(deep), land ice_shelf |
| `hot_springs` | hot_spring, thawed_halo + vent features |
| `basalt` *(decision)* | basalt_flat, firecore_vent (volcanic fields) |

The five additions relative to today, each earning its slot by dominant
gameplay affordance in the new canon:

- **`verdant`** — the good land (warm steppe). Highest land capacity and
  culture influence; the substrate fronts fight over. Scarce by construction.
- **`floodplain`** — river lands (valleys, floodplains, deltas). Fertile
  corridor terrain: high capacity, low travel cost, road-friendly; where the
  rebuilt hydrology becomes somewhere to live, not just lines on the map.
- **`fjord`** — the natural harbor: sheltered carved water, cliff-lined.
- **`lake`** — inland freshwater, no sea connectivity for Wake movers.
- **`basalt`** *(optional)* — volcanic fields from the plume/caldera work:
  hazard-and-power land; forge/monster/anomaly placement affinity. If
  trimmed, its surfaces fold into wastes + Thermal/Anomaly flags.

Plus **`sea_ice` as a cover** (not base terrain): Trail-passable (slow),
Wake-blocking, over water whose base identity is preserved for thaw — the
exact Deep Ice mechanism with inverse polarity. Under the new canon much of
the ocean *starts* covered: the ice age continues even though the Freeze
ended.

`hot_springs` and `reef` remain primary terrains; `crags` absorbs impact rims
and gorges; `wastes` becomes specifically the cold dead land
(polar barrens), no longer the default bucket for everything unclassified.

### 2c. Field word and crossing word extraction

Extracted in the same stage-2 pass, rules documented beside the ladder:

- **Bands (2 bits each)**: warmth (temperature + geothermal + thaw halos),
  fire, aurora, pressure (each from mean/max intensity with authored cuts).
- **Site flags**: Summit (PeakCount > 0), Thermal (vents/geothermal
  significant but not dominant), Anomaly (any interaction surface or magic
  locus present), Polynya, Crater (ImpactBasin/ImpactRim present — the
  future archaeology/relic-site hook, named but not yet consumed by
  mechanics).
- **Initial cover class (2 bits)**: none / sea_ice (deep ice remains
  frontier-driven).
- **Crossing word**: six directions × 2-bit class Open/River/Ridge/Cliff from
  the reciprocal edge masks; Pass edges clear Ridge/Cliff to Open at install;
  fracture edges install Open. Reciprocity validated.

### 2d. Where it lives, how it is judged

The scheme replaces `IceRemembersPhysicalTerrainSynthesizer` with a
documented mapping module in `IceRemembers.Seed` (surfaces and thresholds are
domain vocabulary; the engine sees only resulting IDs and words). Two
acceptance surfaces, honoring the quality-gate lesson from the map work
(percentage gates repeatedly accepted bad maps):

1. **Durable tests**: one exemplar fixture per terrain (a synthetic hex whose
   facts must classify to it), ladder-order contracts (fjord beats shelf,
   floodplain beats tundra at threshold, magic never decides terrain), band
   and flag extraction, pass folding, reciprocity.
2. **Rendered human review as the primary gate**: add a **gameplay-terrain
   view** to the shared painter/capture harness (hex fill by classified
   terrain + cover, flags as glyphs, crossing classes on edges) so seed-42
   captures show exactly what the simulation will see next to what worldgen
   drew. Distribution counts become diagnostics, not acceptance.

---

# Part II — The kernel contract (hex → simulation)

Per latent physical hex, installed once, immutable at runtime:

| Field | Size | Content |
|---|---|---|
| baseTerrainId | int (existing) | 8 → 13 catalog per Part I |
| superregionId, continentId | int (existing) | unchanged |
| crossingWord | uint16 | 6 × 2-bit Open/River/Ridge/Cliff |
| fieldWord | uint16 | warmth/fire/aurora/pressure bands (8b), flags Summit/Thermal/Anomaly/Polynya/Crater (5b), initial cover class (2b) |

~4 bytes per hex beyond today (~200 KB at radius 128), two primitive arrays
in `KernelPhysicalGeographyData`, serialized via the existing snapshot object
path. Frontier reveal reads terrain + cover class and materializes
accordingly; everything else reads by (q, r) on demand.

Everything else stays external: microtriangles (permanently), materials,
exact scalars, water-body/watershed IDs, fracture conductance, the named
magic taxonomy (Anomaly says *that*, the blueprint says *what*), landform
detail beyond what the ladder consumed, and every Ice Remembers name.

**Mutation model** (unchanged from v2): immutable skeleton (crossing word,
field word, superregion, continent) vs mutable surface (base terrain via rare
authored repaints; transient covers Deep Ice and sea ice). New compile-time
constraint: authored terrain mutations may not cross the land/maritime
boundary-group divide — both current uses (scar → wastes, cleanse →
hot_springs) pass.

---

# Part III — Mechanisms (contract → gameplay order)

Unchanged in intent from v2 — each targets a measured failure — with
canon-driven updates:

**M1 Corridors and crossings.** Directed step cost =
`SurfaceTravelCost(to) × EdgeFactor(class, profile, roadQuality)` across
local movement, hierarchical portals, and travel reach. River: Trail
surcharge, road-negated (bridges emerge). Ridge/Cliff: road-immune
surcharges (passes were folded to Open, so crossings concentrate there).
Surcharges never block: `CanEnter` stays cell-based, no topology or cache
semantics change. Floodplain terrain's low cost makes river valleys the
natural roads they historically are.

**M2 Warmth shapes capacity and culture.** Two formula hooks at existing
per-cell read points: base capacity × warmthCapacityCurve[band] in
`MapSignalsProgramRuntime.BaseCapacityAt`; influence delta ×
warmthInfluenceCurve[band] in `Simulation.TerrainInfluenceMultiplier`.
Verdant valleys and floodplains carry cities; contest concentrates on land
worth holding — the localization the uniform 1.02 churn ratio lacks.

**M3 Band- and site-aware generator placement.** Two generic placement
inputs per produced entity: preferred-site mask (flat bonus) and four signed
band affinities (score += affinity × band × constant). Authored opt-in:
monsters → fire + Anomaly; artifacts → aurora + Anomaly; orca content →
pressure; `peak` → Summit (retiring the authored maritime workaround);
`vent`/`sanctuary` → Thermal; `bloom`/`shoal` → Polynya. Roughly ten touched
profiles. This is magic-density-aware placement with zero magic concepts in
the engine.

**M4 Sea ice breathes.** Now two parts: (a) *startup state from worldgen* —
frozen seas install as sea_ice cover per the mapping scheme, so the world
begins mid-ice-age as canon requires; (b) *age-driven modulation* — Ice-track
world-beat ages extend cover from coasts and existing ice (Long Dark, Great
Freeze) or retract it (Thaw ages), reusing the MapFrontier cover machinery
and existing age-set wiring. A freeze closes harbors and opens land bridges;
connectivity itself ebbs with the fiction — the most legible zoom-out signal
this setting can produce.

**M5 Frontier reveal follows the land.** Thaw candidate selection weighted by
warmth band and crossing hostility: the retreating ice exposes valleys,
coasts, and warm refuges first — expansion lobes with shape, which is also
exactly what "the Deep Freeze retreating from a temperate world" should look
like. (Thaw pacing/budgets remain the separate identified proposal.)

**M6 Verbs can want warm and significant places.** Terrain-mode targeting
gains optional minimum-warmth-band and site-flag filters ("warmth ≥ 2", "any
Polynya"). Rides the ordered-emergence K2 work (commitment, frontier
sectors); geography becomes one of the things a purpose can point at.

**Named future consumer (not built now):** the deferred pre-Collapse
archaeology stage will want Crater and Anomaly flags and the floodplain/
valley skeleton for seeding megalith/relic sites. The contract carries the
flags; no mechanic is authored until that stage is designed.

---

# Authoring surface accounting

| Surface | Count |
|---|---|
| Surface→terrain affinity table | ~30 natural-surface rows (one-time, seed-side) |
| Ladder thresholds | ~8 values |
| New terrain rows (verdant, floodplain, fjord, lake, basalt?, sea_ice cover) | 6 × ~10 values |
| Capacity rows for new terrains | 6 values |
| Edge factor table | 4 classes × 3 profiles = 12 |
| Band curves (capacity, influence, thaw weight) | 3 × 4 values |
| Band extraction cuts | 4 × 3 values |
| Generator site masks + affinities | ~10 profiles × 1–4 values |
| Verb band/site filters | 2 verbs initially |
| Placement/filter list touch-ups for new terrains | ~10 lists |

Order of 180 authored numbers, every one owned by a named rule or mechanism.
No combinatorial rules anywhere: the ladder is ordered predicates, bands
compose in engine formulas, and the affinity table is linear in surfaces.

# Performance

Unchanged claims from v2, to be validated by profile: +1 uint16 fetch per
directed step with a predictable zero-word branch; capacity/influence hooks
are one fetch + one table multiply at existing read points; placement is cold
path; sea-ice cover uses the existing effective-terrain read; immutable facts
add no cache epochs. Stage-2 classification is startup/seed-side only.

# Sequencing (each slice reviewed, evidence-gated)

1. **Hex projection scheme + catalog + contract install** — the map→hex fix,
   including the gameplay-terrain capture view and rendered review. No
   behavior change yet beyond terrain variety and startup sea-ice state.
2. M1 traversal + M5 frontier shaping — corridors before things want them.
3. M2 capacity/influence — hubs and fronts.
4. M3 placement — content lands where geography says.
5. M4 age-driven sea-ice modulation (startup state already landed in slice 1).
6. M6 verb filters — with or after ordered-emergence K2.

# Tests and evidence

Durable: ladder exemplars and order contracts; word pack/unpack, reciprocity,
pass folding, band/flag/cover extraction; edge-factor compile and river
road-negation; `DirectedStepCost` agreement across the three traversal paths;
curve application; placement mask/affinity scoring; sea-ice cover apply/thaw
restore (Deep Ice contract reuse); compiler rejection of maritime-crossing
repaints; snapshot round-trips.

Registered MC, paired seeds, judged on the metrics the diagnoses named:
displacement efficiency and reversal rates (M1/M6 — move the 86% number);
spatial variance of culture gain/loss and front formation at verdant/
floodplain boundaries (M2); cluster-reason correlation with warmth/capacity
(M2/M3); placement distribution vs bands and flags (M3); Trail/Wake reachable
component sizes across freeze/thaw ages (M4); frontier reach and lobe shape
vs the ring-21 plateau (M5); first-tick and steady-state profiles.

Plus the non-MC gate this tranche adds: seed-42 (and siblings) gameplay-
terrain captures reviewed by a human against the worldgen surface views —
the mapping is accepted when the hex map *reads as the same world*.

# Decision points for approval

1. **Catalog**: the five additions (verdant, floodplain, fjord, lake, basalt)
   — include/trim, and the `verdant`/`basalt` names.
2. **Ladder ownership**: mapping module in `IceRemembers.Seed` consuming
   surfaces/landforms (proposed) vs keeping any material-based scoring.
3. **Startup sea-ice cover** from worldgen in slice 1 (proposed) vs deferring
   all cover to the M4 age driver.
4. **Crater flag now** (carried for the deferred archaeology stage) vs
   folding craters into Anomaly.
5. **Warmth into culture influence** in M2 now, or capacity-only first.
6. **Band affinity encoding** for generators: four optional signed values
   (proposed) vs single dominant-band preference.
7. Surcharge-only edges; all factor/curve/threshold numbers are calibration
   values, not contract.
