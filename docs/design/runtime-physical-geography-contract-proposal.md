# Runtime Physical Geography Contract — Proposal (v4)

Date: 2026-07-20

Status: proposed for review. No implementation until approved. Supersedes v3.

v4 incorporates the latest map round:

- **The current physical worldgen contract landed**
  ([physical-microgeography-worldgen.md](./physical-microgeography-worldgen.md)):
  the microtriangle
  lattice was untwisted and locked behind conformance tests; tectonic margin
  types and crust age now reach the coasts, giving coasts that differ in
  kind; plates drift across epochs and crust carries history; magic features
  are anchored to discrete causes rather than field thresholds; unread
  percentage quality gates were deleted in favor of structural assertions
  plus human review of captures.
- **The contract must be extensible** to named future feature classes —
  old-world megalith structures and named geologic features (peaks,
  waterfalls, and similar) are the stated examples. These are future ideas
  the map generator does not yet support. **No work on them is part of this
  plan.** They are used only to verify the contract has clean extension
  seams (§ Extension seams).

Carried forward unchanged: the canon (temperate world under a retreating
artificial ice age), the MC-diagnosed target (break isotropy: jitter not
journeys, uniform culture churn, flat capacity, frontier stall), and the
anti-explosion rule — **categorical facts stay few; gradients enter as small
numeric bands consumed by one authored curve per consumer system, never by
per-value rules.**

The proposal has three parts: the **hex projection scheme** (map → hex), the
**kernel contract** (hex → simulation), and the **mechanisms** (contract →
gameplay order).

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
- It produces one ID and drops everything else: no flags, bands, or edges
  reach gameplay.
- Scoring-melee weights are untestable; nobody can say *why* a hex is Wastes.

## Design: a two-stage projection with an explicit precedence ladder

**Stage 1 (exists, keep): triangles → hex facts.** `PhysicalHexAggregator`
already reduces 24 triangles to coverages, means/maxima, peak counts, and
reciprocal edge masks — and is now covered by the lattice conformance suite.

**Stage 2 (redesign): hex facts → gameplay projection.** Replace the material
scorer with a documented, ordered classification consuming what worldgen
concluded — **surfaces, landforms, water kinds, coverages** — emitting the
full contract: terrain ID, initial cover, field word, and crossing word.

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
| `aurora_field` | aurora_field, light_well, frost_prism dominance over ice/glacier base |
| `basalt` *(decision)* | basalt_flat, firecore_vent (volcanic fields) |

**Grounding against rendered worlds:** generated worlds mix archipelagos,
mountain corridors, and plains in roughly equal presence across seeds — the
projection must be generic across that variety, never tuned to one look.
Facts confirmed on captures (`seed42-plate-drift`) that hold regardless of
mix: aurora fields occur at *province scale* (a band cannot carry them);
warm land is scarce; rivers form dense lowland webs (so only trunks can be
crossings); frozen sea is region-scale structure; fire is compact; pressure
is oceanic. The ladder is fact-driven, so an archipelago seed yields
strait-and-sea-ice structure, a cordillera seed yields ridge/pass corridors,
a plains seed yields river-and-verdant structure — same rules, no per-kind
authoring.

The six additions, each earning its slot by dominant gameplay affordance:

- **`verdant`** — the good land (warm steppe): highest land capacity and
  culture influence; the substrate fronts fight over. Scarce by construction.
- **`floodplain`** — river lands (valleys, floodplains, deltas): fertile
  corridor terrain — high capacity, low travel cost, road-friendly.
- **`fjord`** — the natural harbor: sheltered carved water on rifted
  glaciated margins.
- **`lake`** — inland freshwater; no sea connectivity for Wake movers.
- **`aurora_field`** — aurora-charged ice provinces. These are region-scale
  on the actual maps and are the signature land identity of the setting (a
  culture is named for them); collapsing them to a 2-bit band would erase
  the world's most visible feature from gameplay. Affordances: slow, hard
  land (ice-like movement), low capacity, but a placement magnet for
  aurora/memory-flavored content and a distinct culture substrate via the
  existing terrain kind-profile mechanism. The aurora *band* remains for
  gradients at and beyond field margins.
- **`basalt`** *(optional)* — volcanic fields: hazard-and-power land,
  forge/monster/anomaly affinity. If trimmed, folds into wastes +
  Thermal/Anomaly flags. Fire needs no terrain beyond this plus
  `hot_springs`: on the maps fire is compact, already dominantly expressed
  by those two identities plus the Thermal flag and fire band.

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
- **Crossing word**: six directions × 2-bit class Open/River/Ridge/Cliff.
  **River bits mark trunk rivers only** — edges whose crossing river meets a
  Strahler-order/discharge threshold (worldgen carries both). The rendered
  maps show rivers as dense webs across lowlands and the warm heart; marking
  every river edge would surcharge the best-settled land uniformly and
  recreate isotropy. The web expresses through `floodplain`/wet terrain;
  only the great rivers are crossings. Ridge/Cliff from the reciprocal edge
  masks as before (sparse at hex scale — banded ridge systems, strait
  walls); Pass edges clear Ridge/Cliff to Open at install; fracture edges
  install Open; reciprocity validated.

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
   glyphs, crossing classes on edges) so fixed-seed captures show what the
   simulation will see beside what worldgen drew. The mapping is accepted
   when the hex map reads as the same world.

---

# Part II — The kernel contract (hex → simulation)

Per latent physical hex, installed once, immutable at runtime:

| Field | Size | Content |
|---|---|---|
| baseTerrainId | int (existing) | 8 → 13 catalog per Part I |
| superregionId, continentId | int (existing) | unchanged |
| crossingWord | uint16 | 6 × 2-bit Open/River/Ridge/Cliff |
| fieldWord | uint16 | warmth/fire/aurora/pressure bands (8b), flags Summit/Thermal/Anomaly/Polynya/Crater (5b), initial cover (2b) |

~4 bytes per hex beyond today (~200 KB at radius 128), two primitive arrays
in `KernelPhysicalGeographyData`, serialized via the existing snapshot path.
Frontier reveal reads terrain + cover; everything else reads by (q, r) on
demand.

Staying external: microtriangles (permanently), materials, exact scalars,
margin types and crust age, water-body/watershed IDs, fracture conductance,
the named magic taxonomy, and every Ice Remembers name.

**Mutation model** (unchanged): immutable skeleton (crossing word, field
word, superregion, continent) vs mutable surface (base terrain via rare
authored repaints; transient covers Deep Ice and sea ice). Compile-time
constraint: authored terrain mutations may not cross the land/maritime
boundary-group divide — both current uses pass.

## Extension seams

The contract must absorb future feature classes without redesign. The stated
examples — old-world megalith structures and named geologic features (peaks,
waterfalls, …) — are **not supported by the generator yet and are not part
of this plan**; they are used here only to verify the seams hold. Three
seams, all additive:

1. **Categorical growth is linear.** A new terrain ID is one affinity-table
   row plus one set of terrain-data rows; a new POI subtype is an ordinary
   catalog addition; a new site flag is a spare `fieldWord` bit (one is
   free) plus its extraction rule. No existing consumer changes.
2. **Discrete identities have a reserved shape, not a reserved
   implementation.** A future "notable place" fact (a named peak, a buried
   megalith) would arrive as: blueprint-side records (stable ID, class,
   anchor hex) + one per-hex reference array in
   `KernelPhysicalGeographyData` + that reference included in the existing
   cell-created reveal event. Each step is additive — a new array in an
   install-once store, a versioned snapshot addition, an event field —
   and nothing in this plan's fields, ladder, or mechanisms would be
   touched. Nothing in this plan is allowed to preclude that shape (no
   consumer may assume the reveal event's payload is closed, and the
   geography store keeps its install-once array pattern).
3. **Worldgen stages compose.** A future archaeology stage would run inside
   the existing epoch pipeline (seeding works into the temperate landscape
   before the Freeze epochs rework them) and surface through the same
   Stage-2 extraction. The ladder's inputs are surfaces/landforms/coverages,
   so new upstream stages change *facts*, not the projection design.

The governing rule stays the handoff's: a fact enters the kernel when a
consumer exists. These examples have no consumer yet; the seams guarantee
that when they do, entry is additive.

---

# Part III — Mechanisms (contract → gameplay order)

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

**M4 Sea ice breathes.** One of the two corridor systems (with M1); which
dominates depends on the seed's mix — straits on archipelago worlds, passes
on cordillera worlds, both mediated by the same contract. (a) Startup state
from worldgen: frozen seas install as sea_ice cover, so the world begins
mid-ice-age. (b) Age-driven modulation: Ice-track world-beat ages extend
cover from coasts and existing ice or retract it, reusing the MapFrontier
cover machinery and age-set wiring. A freeze closes harbors and sea lanes
and opens strait land bridges; a thaw reverses it. Connectivity itself ebbs
with the fiction.

**M5 Frontier reveal follows the land.** Thaw candidates weighted by warmth
band and crossing hostility: the retreating ice exposes valleys, coasts, and
warm refuges first — expansion lobes with shape, which is what the Deep
Freeze retreating from a temperate world should look like. (Pacing/budgets
remain the separate identified proposal.)

**M6 Verbs can want warm and significant places.** Terrain-mode targeting
gains optional minimum-warmth-band and site-flag filters. Rides the
ordered-emergence K2 work; geography becomes one of the things a purpose can
point at.

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
| Generator site masks + affinities | ~10 profiles × 1–4 values |
| Verb band/site filters | 2 verbs initially |
| Placement/filter list touch-ups | ~10 lists |

Order of 200 authored numbers; every value owned by a named rule or
mechanism; no combinatorial rules anywhere.

# Performance

+1 uint16 fetch per directed step with a predictable zero-word branch;
capacity/influence hooks are one fetch + one multiply at existing per-cell
read points; stage-2 classification is seed/startup-side; sea-ice cover uses
the existing effective-terrain read; immutable facts add no cache epochs.
Validated by first-tick and steady-state profiles before acceptance.

# Sequencing (each slice reviewed, evidence-gated)

1. **Hex projection scheme + catalog + contract install + gameplay-terrain
   capture view.** Startup sea-ice state lands here. Rendered human review
   is the gate.
2. M1 traversal + M5 frontier shaping — corridors before things want them.
3. M4 age-driven sea-ice modulation — paired with M1 as the two corridor
   systems; landed early so both terrain kinds of world get their structure.
4. M2 capacity/influence — hubs and fronts.
5. M3 placement — content lands where geography says.
6. M6 verb filters — with or after ordered-emergence K2.

# Tests and evidence

Durable: ladder exemplars and order contracts; word pack/install validation,
reciprocity, pass folding; edge-factor compile and river road-negation;
`DirectedStepCost` agreement across the three traversal paths; curve
application; placement mask/affinity scoring; sea-ice apply/thaw restore;
compiler rejection of maritime-crossing repaints; snapshot round-trips;
classification structural digest as change detector.

Registered MC, paired seeds, judged on the diagnosed metrics: displacement
efficiency and reversal rates (M1/M6 — move the 86% number); spatial
variance of culture gain/loss and front formation at verdant/floodplain
boundaries (M2); cluster-reason correlation (M2/M3); placement distribution
vs bands and flags (M3); Trail/Wake reachable components across freeze/thaw
ages (M4); frontier reach and lobe shape vs the ring-21 plateau (M5);
profiles.

Plus the non-MC gate: fixed-seed gameplay-terrain captures reviewed by a
human against the worldgen views.

# Decision points for approval

1. **Catalog**: the six additions (verdant, floodplain, fjord, lake,
   aurora_field, basalt) — include/trim, and the `verdant`/`basalt` names.
   Also: trunk-river thresholds (which Strahler order/discharge makes a
   crossing).
2. **Startup sea-ice cover** from worldgen in slice 1 (proposed) vs deferred
   to the M4 age driver.
3. **Crater flag** now (ambient placement fact from existing impact
   landforms) vs dropping it until a consumer is authored.
4. **Warmth into culture influence** in M2 now, or capacity-only first.
5. **Band affinity encoding**: four optional signed values (proposed) vs
   single dominant-band preference.
6. Surcharge-only edges; all factor/curve/threshold numbers are calibration
   values, not contract.
