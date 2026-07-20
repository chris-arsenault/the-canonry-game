# Runtime Physical Geography Contract — Proposal (v4)

Date: 2026-07-20

Status: proposed for review. No implementation until approved. Supersedes v3.

v4 incorporates the latest map round and two new forward requirements:

- **The structural-fidelity round landed**
  (`map-generation-fidelity-and-coastline-plan.md`): the microtriangle lattice
  was untwisted and locked behind conformance tests; tectonic margin types
  (passive / convergent / rifted / transform) and crust age now reach the
  coasts, giving coasts that differ in kind; plates drift across epochs and
  crust carries history; magic features are anchored to discrete causes
  rather than field thresholds; unread percentage quality gates were deleted
  in favor of structural assertions plus human review of captures.
- **Two future features must be supported by this contract without redesign:**
  *old-world megalith structures* (the deferred pre-Collapse archaeology
  stage) and *named geologic features* (peaks, waterfalls, and similar
  landmarks that can carry names and be referenced by gameplay and
  chronicles). v4 adds the geographic feature registry and the latent-site
  channel for them.

Carried forward unchanged: the canon (temperate world under a retreating
artificial ice age), the MC-diagnosed target (break isotropy: jitter not
journeys, uniform culture churn, flat capacity, frontier stall), and the
anti-explosion rule — **categorical facts stay few; gradients enter as small
numeric bands consumed by one authored curve per consumer system, never by
per-value rules.**

The proposal has four parts: the **hex projection scheme** (map → hex), the
**feature registry and latent sites** (map → named places), the **kernel
contract** (hex → simulation), and the **mechanisms** (contract → gameplay
order).

---

# Part I — The hex projection scheme (map → hex)

## What is wrong today

`IceRemembersPhysicalTerrainSynthesizer` is a competitive scorer over **eight
terrains** using **material counts** plus a few aggregates. It predates the
worldgen rebuilds and was explicitly labeled a temporary compatibility
projection:

- It never reads the surface classification. Worldgen spends its causal
  layers deciding a triangle is `fjord`, `delta`, `cold_steppe`,
  `basalt_flat`, or `polar_barrens` — and the synthesizer re-derives terrain
  from substrate materials, discarding that judgment. A fjord becomes
  "shelf," a delta becomes "tundra," a lava plain becomes "wastes."
- Its 8-slot catalog is the old "eternal ice with warm pockets" world. The
  canon's landscape — warm steppes, river lands, carved inlets, inland lakes,
  volcanic fields, frozen seas, margin-typed coasts — has no identities to
  land in.
- It produces one ID and drops everything else: no flags, no bands, no
  edges, no sites reach gameplay.
- Scoring-melee weights are untestable; nobody can say *why* a hex is Wastes.

## Design: a two-stage projection with an explicit precedence ladder

**Stage 1 (exists, keep): triangles → hex facts.** `PhysicalHexAggregator`
already reduces 24 triangles to coverages, means/maxima, peak counts, and
reciprocal edge masks — and is now covered by the lattice conformance suite.

**Stage 2 (redesign): hex facts → gameplay projection.** Replace the material
scorer with a documented, ordered classification consuming what worldgen
concluded — **surfaces, landforms, water kinds, coverages** — emitting the
full contract: terrain ID, initial cover, field word, crossing word, and
latent site reference.

### 2a. Terrain classification ladder

Deterministic, top-down; each rule a named, individually testable predicate.
Magic surfaces never decide terrain — each maps to its underlying natural
surface (magic enters through bands and flags), preserving worldgen's rule
that magic overlays never erase natural fields.

```
1. Water partition: hex is water if WaterCoverage ≥ T_water.

2. Water hexes, in order:
   lake      — LakeCoverage dominates water coverage (inland WaterKind)
   fjord     — Fjord classification present + coastal + high relief
               (fjord keys on cumulative glacial erosion, which survives
               drowning — now a first-class worldgen fact)
   reef      — reef surface coverage ≥ T_reef
   shelf     — shelf/exposed-shelf bathymetry dominates
   ocean     — otherwise

3. Land hexes: dominant natural-surface group by triangle vote through the
   surface→terrain affinity table (§2b), with ordered tie-breaks:
   geothermal dominance beats ice; relief/peak evidence beats flat groups;
   river-lands beat tundra when floodplain+delta+wide-river ≥ T_river.

4. Initial covers (recorded per latent hex, applied at reveal):
   sea_ice   — water hex whose surfaces are pack_ice / sea_ice / ice_shelf
   deep_ice  — unrevealed frontier, as today
```

### 2b. The surface→terrain affinity table

One authored row per natural surface — linear in surfaces — assigning each to
a terrain group. Sketch:

| Terrain (13) | Natural surfaces and landforms that vote for it |
|---|---|
| `ocean` | ocean, trench, current_channel |
| `shelf` | shelf, exposed_shelf |
| `reef` | reef |
| `fjord` | fjord (surface + cumulative-glacial-erosion classification) |
| `lake` | lake |
| `verdant` | open_field, cold_steppe |
| `floodplain` | river_valley, river_channel, floodplain, delta |
| `tundra` | tundra, wet_tundra |
| `wastes` | polar_barrens, snowfield(dry), permafrost-dominant tundra |
| `crags` | ridge, scree_field, gorge, waterfall + Peak/Ridge/ImpactRim landforms |
| `ice` | glacier, moraine, snowfield(deep), land ice_shelf |
| `hot_springs` | hot_spring, thawed_halo + vent features |
| `basalt` *(decision)* | basalt_flat, firecore_vent (volcanic fields) |

The five additions, each earning its slot by dominant gameplay affordance:

- **`verdant`** — the good land (warm steppe): highest land capacity and
  culture influence; the substrate fronts fight over. Scarce by construction.
- **`floodplain`** — river lands (valleys, floodplains, deltas): fertile
  corridor terrain — high capacity, low travel cost, road-friendly.
- **`fjord`** — the natural harbor: sheltered carved water on rifted
  glaciated margins.
- **`lake`** — inland freshwater; no sea connectivity for Wake movers.
- **`basalt`** *(optional)* — volcanic fields: hazard-and-power land,
  forge/monster/anomaly affinity. If trimmed, folds into wastes +
  Thermal/Anomaly flags.

Plus **`sea_ice` as a cover** (not base terrain): Trail-passable (slow),
Wake-blocking, over water whose base identity is preserved for thaw — the
Deep Ice mechanism with inverse polarity. Much of the ocean *starts* covered:
the ice age continues though the Freeze ended.

Margin type itself stays external: passive/rifted/convergent coasts already
express themselves through the terrain their shelves, fjords, arcs, and
platforms classify into. No consumer needs the margin fact at runtime.

### 2c. Field word and crossing word extraction

- **Bands (2 bits each)**: warmth (temperature + geothermal + thaw halos),
  fire, aurora, pressure — from mean/max intensity with authored cuts. The
  anchored-feature rework makes these honest: intensity now traces to
  discrete causes, not threshold speckle.
- **Site flags**: Summit (PeakCount > 0), Thermal (significant but not
  dominant), Anomaly (interaction surface or anchored magic locus present),
  Polynya, Crater (ImpactBasin/ImpactRim).
- **Initial cover class (2 bits)**: none / sea_ice.
- **Crossing word**: six directions × 2-bit class Open/River/Ridge/Cliff from
  the reciprocal edge masks; Pass edges clear Ridge/Cliff to Open at install;
  fracture edges install Open; reciprocity validated.

Flags are *ambient* placement facts ("any peak-ish hex"). Discrete notable
features — *the* mountain, *the* falls — are Part II's registry, not flags.
The two serve different consumers and both are needed.

### 2d. Where it lives, how it is judged

A documented mapping module in `IceRemembers.Seed` replaces the synthesizer.
Acceptance honors the hardened verification stance (unread percentage gates
are deleted project-wide; structural assertions and human review decide):

1. **Durable tests**: one exemplar fixture per terrain; ladder-order
   contracts (fjord beats shelf; floodplain beats tundra at threshold; magic
   never decides terrain); band/flag/cover extraction; pass folding;
   reciprocity. A committed structural digest of the classification for fixed
   seeds acts as a change detector CI diffs and a human acknowledges — never
   a threshold gate.
2. **Rendered review as the primary gate**: a **gameplay-terrain view** in
   the shared painter/capture harness (hex fill by terrain + cover, flags as
   glyphs, crossing classes on edges, registry sites labeled) so fixed-seed
   captures show what the simulation will see beside what worldgen drew. The
   mapping is accepted when the hex map reads as the same world.

---

# Part II — Geographic feature registry and latent sites (map → named places)

Two future features must not require a contract redesign: **named geologic
features** (peaks, waterfalls, gorges, craters, great rivers…) and
**old-world megalith structures** (the deferred pre-Collapse archaeology
stage). Both reduce to the same need: *discrete, stable, nameable identities
anchored to geography, which the simulation can surface when the world
reaches them.* One design serves both.

## The feature registry (blueprint-side, external)

At extraction time, worldgen emits a **geographic feature registry** into the
blueprint beside the hex summaries:

| Registry field | Content |
|---|---|
| featureId | stable int, deterministic per seed |
| featureClass | generic: summit, falls, gorge, crater, vent-field, locus, ruin… |
| anchorHex (q, r) | the hex a runtime site would occupy |
| memberHexes | optional extent (a range, a river trunk, a crater rim) |
| magnitude | prominence / discharge / size — one int, class-relative |
| sourceFacts | class-specific detail for UI/inspection (external only) |

Candidates come from facts worldgen already computes discretely: peaks by
prominence, waterfalls and gorges from landform runs, craters from impact
history, anchored magic loci, major river trunks by discharge. Selection is
top-N by magnitude with spacing, per class — a bounded, legible set of
landmarks, not every bump.

**Names are domain content, not worldgen output.** The registry carries
identity and class; the Ice Remembers naming pass (seed-side for startup-
visible features, or at discovery time for revealed ones) assigns names. The
registry is also exactly where the future archaeology stage deposits its
work: megalith sites, drowned roads, and weapon sites become registry entries
of ruin-classes with `sourceFacts` recording what the Freeze buried. The
archaeology stage stays deferred; the shelf it will stock exists now.

## The latent-site channel (kernel-side, minimal)

The kernel gets one new fact per latent hex: **siteId** (int, 0 for none —
the anchor hex of at most one registry feature; ties broken by magnitude at
install). The kernel never interprets it. Exactly two behaviors:

1. Frontier reveal includes siteId in the cell-created event.
2. A query exposes siteId at (q, r) for placement and gameplay hooks.

Domain gameplay decides what a site becomes: startup founds POI entities for
registry features inside the materialized heart (existing kinds — `peak`
subtype for summits; new POI subtypes like `falls`, `crater`, `ruin` are
ordinary catalog additions when wanted); revealed sites found their POI when
the ice retreats past them (mechanism M7). An un-consumed siteId is inert —
no cost, no rules.

This is deliberately *not* flags-in-the-word (identity, not a predicate),
*not* entities-in-the-kernel (entities stay a domain concern), and *not* a
feature-effect system (a site does nothing until domain content founds
something on it).

---

# Part III — The kernel contract (hex → simulation)

Per latent physical hex, installed once, immutable at runtime:

| Field | Size | Content |
|---|---|---|
| baseTerrainId | int (existing) | 8 → 13 catalog per Part I |
| superregionId, continentId | int (existing) | unchanged |
| crossingWord | uint16 | 6 × 2-bit Open/River/Ridge/Cliff |
| fieldWord | uint16 | warmth/fire/aurora/pressure bands (8b), flags Summit/Thermal/Anomaly/Polynya/Crater (5b), initial cover (2b) |
| siteId | int32 | 0, or the anchored registry feature |

~8 bytes per hex beyond today (~400 KB at radius 128), three primitive arrays
in `KernelPhysicalGeographyData`, serialized via the existing snapshot path.
Frontier reveal reads terrain + cover + siteId; everything else reads by
(q, r) on demand.

Staying external: microtriangles (permanently), materials, exact scalars,
margin types and crust age, water-body/watershed IDs, fracture conductance,
the named magic taxonomy, registry `sourceFacts` and extents, and every
Ice Remembers name.

**Mutation model** (unchanged): immutable skeleton (crossing word, field
word, siteId, superregion, continent) vs mutable surface (base terrain via
rare authored repaints; transient covers Deep Ice and sea ice). Compile-time
constraint: authored terrain mutations may not cross the land/maritime
boundary-group divide — both current uses pass.

---

# Part IV — Mechanisms (contract → gameplay order)

**M1 Corridors and crossings.** Directed step cost =
`SurfaceTravelCost(to) × EdgeFactor(class, profile, roadQuality)` across
local movement, hierarchical portals, and travel reach. River: Trail
surcharge, road-negated (bridges emerge). Ridge/Cliff: road-immune
surcharges; passes were folded to Open so crossings concentrate there.
Surcharges never block — `CanEnter` stays cell-based; no topology or cache
semantic changes. Floodplain's low cost makes river valleys the natural
roads.

**M2 Warmth shapes capacity and culture.** Base capacity ×
warmthCapacityCurve[band] in `MapSignalsProgramRuntime.BaseCapacityAt`;
influence delta × warmthInfluenceCurve[band] in
`Simulation.TerrainInfluenceMultiplier`. Verdant valleys and floodplains
carry cities; contest concentrates on land worth holding — the localization
the uniform 1.02 churn ratio lacks.

**M3 Band- and site-aware generator placement.** Per produced entity:
preferred-site mask (flat bonus) and four signed band affinities
(score += affinity × band × constant). Authored opt-in (~10 profiles):
monsters → fire + Anomaly; artifacts → aurora + Anomaly; orca content →
pressure; `peak` → Summit (retiring the maritime workaround); `vent`,
`sanctuary` → Thermal; `bloom`, `shoal` → Polynya. Magic-density-aware
placement with zero magic concepts in the engine.

**M4 Sea ice breathes.** (a) Startup state from worldgen: frozen seas
install as sea_ice cover, so the world begins mid-ice-age. (b) Age-driven
modulation: Ice-track world-beat ages extend cover from coasts and existing
ice or retract it, reusing the MapFrontier cover machinery and age-set
wiring. A freeze closes harbors and opens land bridges; connectivity itself
ebbs with the fiction.

**M5 Frontier reveal follows the land.** Thaw candidates weighted by warmth
band and crossing hostility: the retreating ice exposes valleys, coasts, and
warm refuges first — expansion lobes with shape, which is what the Deep
Freeze retreating from a temperate world should look like. (Pacing/budgets
remain the separate identified proposal.)

**M6 Verbs can want warm and significant places.** Terrain-mode targeting
gains optional minimum-warmth-band and site-flag filters. Rides the
ordered-emergence K2 work; geography becomes one of the things a purpose can
point at.

**M7 Landmarks are discovered.** When frontier reveal materializes a hex
with a siteId, domain gameplay founds the corresponding named POI (subtype by
feature class) — a summit, a falls, a crater; later, a megalith. Discoveries
give explorers real destinations and the chronicle layer real places —
directly serving the journeys axis (K2) and the "clusters need a reason"
axis (K3), and giving the archaeology stage its delivery mechanism when it
lands. Startup founds registry POIs inside the heart the same way, so heart
and frontier use one path.

**Named future consumer:** the archaeology stage seeds ruin-class registry
entries into the pre-Freeze landscape; the contract (registry + siteId + M7)
carries them without change.

---

# Authoring surface accounting

| Surface | Count |
|---|---|
| Surface→terrain affinity table | ~30 natural-surface rows (seed-side, one-time) |
| Ladder thresholds | ~8 values |
| New terrain rows (verdant, floodplain, fjord, lake, basalt?, sea_ice cover) | 6 × ~10 values |
| Capacity rows for new terrains | 6 values |
| Edge factor table | 4 × 3 = 12 values |
| Band curves (capacity, influence, thaw weight) | 3 × 4 values |
| Band extraction cuts | 4 × 3 values |
| Registry selection (per class: top-N, spacing) | ~6 classes × 2 values |
| Feature-class → POI subtype mapping + naming lists | ~6 rows + name content |
| Generator site masks + affinities | ~10 profiles × 1–4 values |
| Verb band/site filters | 2 verbs initially |
| Placement/filter list touch-ups | ~10 lists |

Order of 200 authored numbers plus naming content; every value owned by a
named rule or mechanism; no combinatorial rules anywhere.

# Performance

+1 uint16 fetch per directed step with a predictable zero-word branch;
capacity/influence hooks are one fetch + one multiply at existing per-cell
read points; siteId is read only at reveal and in cold placement paths;
stage-2 classification and registry extraction are seed/startup-side; sea-ice
cover uses the existing effective-terrain read; immutable facts add no cache
epochs. Validated by first-tick and steady-state profiles before acceptance.

# Sequencing (each slice reviewed, evidence-gated)

1. **Hex projection scheme + catalog + contract install + registry
   extraction + gameplay-terrain capture view.** Startup sea-ice state and
   heart-POI founding land here. Rendered human review is the gate.
2. M1 traversal + M5 frontier shaping.
3. M2 capacity/influence.
4. M3 placement + M7 discovery founding.
5. M4 age-driven sea-ice modulation.
6. M6 verb filters — with or after ordered-emergence K2.

# Tests and evidence

Durable: ladder exemplars and order contracts; word and siteId pack/install
validation, reciprocity, pass folding; registry determinism and spacing;
edge-factor compile and river road-negation; `DirectedStepCost` agreement
across the three traversal paths; curve application; placement mask/affinity
scoring; sea-ice apply/thaw restore; M7 reveal→found contract; compiler
rejection of maritime-crossing repaints; snapshot round-trips; classification
structural digest as change detector.

Registered MC, paired seeds, judged on the diagnosed metrics: displacement
efficiency and reversal rates (M1/M6/M7 — move the 86% number); spatial
variance of culture gain/loss and front formation at verdant/floodplain
boundaries (M2); cluster-reason correlation (M2/M3); placement distribution
vs bands and flags (M3); Trail/Wake reachable components across freeze/thaw
ages (M4); frontier reach and lobe shape vs the ring-21 plateau (M5);
discovery events and post-discovery visitation (M7); profiles.

Plus the non-MC gate: fixed-seed gameplay-terrain captures (with registry
labels) reviewed by a human against the worldgen views.

# Decision points for approval

1. **Catalog**: the five additions (verdant, floodplain, fjord, lake,
   basalt) — include/trim, and the `verdant`/`basalt` names.
2. **Registry scope in slice 1**: geologic classes only (summit, falls,
   gorge, crater, locus, river-trunk), with ruin classes reserved for the
   archaeology stage (proposed) — vs deferring the registry entirely.
3. **siteId in the kernel now** (proposed; inert until consumed) vs
   registry-external until M7.
4. **Startup sea-ice cover** from worldgen in slice 1 (proposed) vs deferred
   to the M4 age driver.
5. **Crater flag** kept alongside crater registry entries (proposed: flag =
   ambient placement fact, registry = the named landmark) vs registry-only.
6. **Warmth into culture influence** in M2 now, or capacity-only first.
7. **Band affinity encoding**: four optional signed values (proposed) vs
   single dominant-band preference.
8. Surcharge-only edges; all factor/curve/threshold numbers are calibration
   values, not contract.
