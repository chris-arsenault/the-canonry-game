# Exploration, Ice, and Culture — Implementation Plan

Replace the conflated thaw/culture-expansion mechanism with three decoupled
layers: entity-driven one-way reveal (fog of war at entity speed), a
taste-shaped culture field that acts as home base and soft leash, and a
separate dynamic ice layer that modulates travel and range with the ages but
never un-reveals ground. Out of scope: player agency, procedural ice events,
semantic-plane growth and the knowledge system (both stay as-is), sea-ice
authoring changes beyond absorption into the ice layer.

## Confirmed decisions

- Reveal is untied from ice and culture: cells materialize where entities go,
  never from culture mass thresholds, and reveal is one-way.
- Reveal driver is presence + explorer verbs: every mobile entity passively
  reveals a small radius as it moves; dedicated exploration verbs push
  deliberately into fog; water-capable kinds range across oceans.
- The supported range (culture field + static entities) is a soft leash:
  verb targeting degrades and attrition rises beyond support; no hard walls.
- Culture identity shapes expansion: influence targeting multiplies by the
  culture's placement taste over target-hex facts; the per-terrain
  InfluenceMultiplier table retires in favor of a capacity-derived term.
- The ice is a dynamic hostility layer: cold pressure derived from fact
  columns scaled by the open World-track age, expressed as covers on known
  cells (deep ice on land, sea ice on water), resisted locally by roads,
  culture mass, and warm refuges. It closes passages and shrinks range; it
  never reclaims fog of war.
- Ages remain the macro pacing clock. Semantic growth and knowledge are
  untouched.

## Context / reuse map

Verified against code at b289a67 (+uncommitted concurrent worldgen work):

- `MapFrontierProgramRuntime` (+ `.Reclaim`, `.SeaIce`, `.SemanticGrowth`):
  the current thaw/reclaim/sea-ice/semantic program. Semantic growth and the
  sea-ice pulse mechanics are **reused**; mass-threshold thaw of unknown
  cells and the reclaim rule are **replaced**. `State.RecordThawed` ring
  (65536 cap) tracks frontier-grown cells — reusable as the revealed-cell
  ring.
- Reveal = `Simulation.CreateCell` from `KernelPhysicalGeographyData`
  latent cells + `ApplyInitialTerrainCover` — exactly what `ThawCell` does
  today; the mechanism is **reused**, only the driver changes.
- Capacity: `MapCellCapacity` 131072 covers full dual-plane radius-128
  reveal (~99k cells). Verified; no resize.
- Taste: `KernelPlacementTasteData` + `Simulation.PlacementTasteScore`
  (kind+culture over 12 axes) — **reused** for expansion targeting and
  exploration scoring; culture-only weights via `CombinedWeight(0, cultureId,
  axis)`.
- Facts: `PhysicalHexFactCodes` columns + `DomainPhysicalGeographyFacts`
  — the only geography source for cold pressure and range formulas
  (one-habitability-primitive rule).
- Influence targeting: `CultureInfluenceTargetScorer` (topology: components,
  headroom, bridging) — **reused-but-extended** with a geography multiplier.
- Terrain `InfluenceMultiplier` (`KernelTerrainData`, authored in
  `IceRemembersTerrain`) — **retired** for the physical plane (semantic
  plane keeps its authored values; semantic has no fact columns).
- Movement: entity physical position updates (verb movement, minor actors,
  transports) — the passive-reveal hook point. `MovementProgramRuntime`
  directed costs and covers already express passage closing.
- Verbs: `LowAltitudeVerbs` config chain (Gameplay → Domain → kernel
  install) with geography filters (MinTemperature, site flags) — the surface
  for new exploration verbs.
- Ages: `WorldBeats.OpenAgeId(WorldBeatTrackCodes.World)` +
  `IceRemembersAges` (Thaw/Freeze/Long Dark/Great Thaw/Great Freeze) — the
  age scalar source for cold pressure.
- Knowledge grant domains `FrontierThawThreshold` / `FrontierReclaimThreshold`
  — re-pointed at the new mechanisms (range bonus / ice resistance) rather
  than deleted.
- Instruments: `--hex-contract` diagnostics, entity-overlay captures, MC
  registry workflow, seed validator — the evidence harness for calibration.

## Cross-cutting constraints

- Kernel discipline: no strings, fixed-capacity state, primitive codes,
  analyzers as errors; authored config flows Gameplay → Domain compile →
  Host install.
- All geographic terms derive from fact columns via authored formulas —
  no new per-terrain tables, no invented scalars.
- Design invariants: strongholds not frontier (the leash preserves this);
  static entities stay static; no stock/flow reservoirs (attrition is
  health-effect pressure, not a supply resource); the world must still grow.
- Determinism and snapshot round-trip for all new program state.
- Seed-affecting changes bump `IceRemembersWorldGenerationVersion` and
  regenerate the four standard worlds; runtime-only changes must not.
- A concurrent workstream is active in worldgen/visualization; this work
  stays in kernel/gameplay/domain/verb layers and coordinates only at the
  version bump.

## Milestones

### M1 — Reveal layer: presence-driven materialization
Replace mass-threshold thaw of unknown cells with entity-presence reveal.
Mobile entities reveal a small per-kind radius around their physical
position (hooked at position change, budgeted per pulse); revealed cells
materialize from latent geography exactly as `ThawCell` does today. The
old culture-mass thaw path and its config retire; deep-ice cover clearing
moves to M4 (ice layer). Fog-of-war reveal becomes one-way by
construction.
**Exit gate:** solution builds; kernel fast suite green including new
reveal tests (entity movement reveals; reveal never reverses; determinism
snapshot round-trip); MC smoke run shows revealed-cell count growing with
entity travel, not culture mass.

### M2 — Exploration verbs and ranging kinds [depends on M1]
Author the deliberate push into fog: exploration verb family
(scout/survey for land kinds, voyage for transports, deep-ranging for
orcas) targeting fog-adjacent known cells, scored by taste over the
frontier hex facts. Water-capable movement profiles cross oceans reaching
coasts and islands. Passive + active reveal rates become the exploration
pacing dials.
**Exit gate:** fast suites green; MC evidence that explorer kinds extend
reveal notably beyond the passive envelope (revealed-radius trajectory per
kind), with ships opening non-contiguous landmasses.

### M3 — Supported range: the soft leash [depends on M1]
Define per-hex support (culture field mass + static-entity anchors,
distance-discounted) and thread it as a soft leash: verb targeting and
exploration scoring degrade beyond support, and an attrition health effect
applies to entities operating far outside it, scaled by kind (ranging
kinds tolerate more). Knowledge `FrontierThawThreshold` grants re-point to
range bonus. [DECISION] attrition severity band: gentle pressure vs real
danger — tuned in M6 but the band choice shapes authoring.
**Exit gate:** fast suites green; MC shows operations clustered near
support with ranging-kind exceptions (roaming-radius by kind vs support
distance), no hard boundary artifacts.

### M4 — Ice layer: cold pressure and breathing covers [depends on M1]
One ice dynamics rule for both land and sea: per-hex cold pressure derived
from facts (temperature, ice thickness, geothermal refuge) scaled by the
open age; covers advance onto known cells where pressure beats local
resistance (roads, culture mass, warm refuges — thresholds from facts, not
constants) and retreat where it collapses, under existing budgets. Sea-ice
pulse is absorbed as the water case; the old reclaim rule and
`ReclaimMassCeiling` retire; knowledge `FrontierReclaimThreshold` grants
re-point to ice resistance. Covers close passages and raise travel cost —
range pressure, never un-reveal.
**Exit gate:** fast suites green; MC/capture evidence across a
Thaw→Long Dark age sequence: passages close and reopen, no revealed cell
ever becomes unknown, culture-held ground resists cover.

### M5 — Taste-shaped culture growth [depends on M3]
Culture expansion reads geography and identity: influence targeting
multiplies its topology score by culture taste over target-hex facts plus
a capacity-derived land-quality term; the physical-plane terrain
InfluenceMultiplier is removed from authoring and compile. Contest
sharpening and remittance stay untouched.
**Exit gate:** fast suites green; MC per-culture terrain-occupancy shapes
diverge (Nightshelf toward ice, Wake-Singers toward coast/water) while
front stability metrics stay within current bands.

### M6 — Integration, calibration, and evidence [depends on M2, M4, M5]
Worldgen version bump; regenerate the four worlds; validator gains reveal
and ice sanity teeth (reveal pace floor, no-unreveal invariant, ice never
covers the heart under normal ages); captures with reveal/ice overlays;
long-horizon MC batch (bootstrap-scale ticks) for exploration pace, range
behavior, ice breathing, and per-culture territory; calibration report in
docs/analysis; docs/systems pages for the three layers updated.
[DECISION] accept/adjust the exploration pacing targets (how much of the
world a 25k-tick bootstrap should reveal).
**Exit gate:** full `make ci` green; MC report reviewed; worlds load in
the game shell.

### Decisions needing your input
| Where | Decision you own |
| ----- | ---------------- |
| M3 | Attrition severity band beyond supported range: gentle pressure vs real danger |
| M6 | Exploration pacing target: share of world revealed by bootstrap horizon (25k ticks) |
