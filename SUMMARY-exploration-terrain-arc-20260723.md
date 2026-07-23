# Summary: Exploration and Terrain Arc (2026-07-22..23)

Temporary orientation doc for the completed exploration/terrain work. The
durable state lives in `docs/systems/` (26-growing-map, 16-generator-executor,
06-entity-behaviors, 04-map-spatial) and
`docs/analysis/terrain-content-audit-20260723.md`; delete this file once it
has served its purpose.

## The terrain foundation this arc consumed (parallel worldgen workstream)

The gameplay work below sits on a physical-generation rework that was built
in parallel and is documented in `docs/game-systems.md` and
`docs/systems/04-map-spatial.md`. In brief, so this summary reads standalone:

- **Global microtriangle lattice, not a hex generator.** Worldgen runs on one
  padded equilateral microtriangle lattice with uniform adjacency and no
  parent-cell knowledge. It partitions moving continental and oceanic crust,
  derives typed plate boundaries and strain, and carries crust composition,
  age, thickness, load, and material history through conservative plate
  transport. Plate-pair systems spend finite deformation budgets on
  collision roots, subduction belts, rifts, and transform relief;
  crustal-support and lithospheric-flexure passes convert those loads into
  the bedrock datum; sutures between transported assemblies preserve
  interior ranges after active boundaries move; margin evolution grades
  shelves and rises while retaining rare trenches.
- **Four geological epochs of surface history.** Impacts and mantle-root
  volcanism, then hydrology and climate: connected land/ocean labeling,
  bathymetry, basin currents and wave exposure, one shared atmospheric
  field for moisture/rain/snow, and persistent bedrock+sediment evolution
  through fluvial, coastal, and glacial erosion/deposition with
  mass-conserving mass wasting and rebound. Outlet-rooted drainage keeps
  rivers continuous through lakes; a separate water-balance stage sets real
  lake stages and spill outlets; deltas and floodplains expand from the
  drainage spine. Ice Remembers interleaves fire, aurora, and pressure
  passes with the epochs and keeps cumulative magic exposure.
- **Radius-128 latent field, 24 microtriangles per runtime hex.** The
  seed-to-kernel projection folds the lattice into hex-local slots. Scored
  authored rules turn each 24-triangle arrangement into one durable base
  terrain; only base terrain plus superregion/continent ids enter the
  kernel. Everything the gameplay layer reads as "the ground" — the 16
  quantized per-hex fact columns (temperature, moisture, geothermal,
  elevation, relief, roughness, land fraction, river/floodplain coverage,
  ice thickness, fire/aurora/pressure mean+max), the discrete site flags
  (summit, thermal, anomaly, polynya, crater), crossing edge masks
  (river/ridge/cliff), and the hard land/water partition — is written here.
- **Acceptance and inspection.** Structural invariants and a seed validator
  decide world acceptance (distributions are diagnostics, not gates);
  worlds are versioned and must be generated from Release builds. The map
  renders external facts at aggregate and triangle zoom, and a read-only 3D
  QC mode displays the full latent field as displaced meshes.

This arc's only additions inside that pipeline were the Peak and Crater
feature-identification passes (T3) — everything else below consumes the
foundation through facts, sites, features, groups, and taste.

## What changed, in order (this arc)

**1. Culture/thaw redesign (three decoupled layers).** Exploration untied
from culture and ice. Reveal is entity-driven and one-way (per-kind radii on
arrival and materialization; fog is simply the unknown set — no pulse, no
cursor). The taste-shaped culture field is home-base support: a soft leash
via BeyondCultureSupport costs, never a wall. Ice is a separate cover layer
over frontier-grown ground: ColdPressure vs IceResistance (mass, roads,
knowledge), age-scaled, advance and retreat.

**2. Exploration content (O1–O7, EXPLORATION-CONTENT-PLAN, deleted).**
Founding ratchet via physicalFrontier placements (colonies, reefholds,
harbors, vents, orca bulls); expedition gambits (Raise the Waymark, Sound
the Far Passage) that pack travel structures and cost flesh; discovery
generators on FeatureAnchor (river namings, lake soundings, remembrance
stones); ages of wandering (The Far Wandering, The Great Crossing);
fog-adjacent seeker verbs with waymark travel influence; the Far Wanderer
chronicle archetype (cast on the waymark deed, distance-milestone beats,
world-produced endings). MC instruments: exploration section from
map_snapshots (the CellCreated stream undercounts ~20x — never use it for
growth). Result: every kind places beyond the startup rim; bootstrap growth
+1600..+2500 cells; 50k runs reach 3x start extent.

**3. Terrain audit and retrofit (T1–T6, audit preserved in
docs/analysis/terrain-content-audit-20260723.md).**
- T1: gambit preconditions can finally read the ground —
  Self/TargetHexFactThreshold (banded latent facts, quantized per fact
  scale) and SelfOnTerrainGroup (boundary groups, no hand lists).
- T2: monster subtype placement (leviathan deep water, beast reef edge,
  wraith cold ground); wayfinder lands at the colony's frontier.
- T3: worldgen identifies Peak and Crater features (worldgen version 16,
  four standard worlds regenerated); peaks anchor the navigational-fact
  discoveries; The Crater Keeps Its Count raises Before-Times ruin POIs
  with rare serial-numbered weapon finds.
- T4: terrain-gated gambits — Shelter at the Vent, Find the Pass (travel
  reach annulus, 27% success), Breathe the Thaw (calibrated to measured
  geothermal p90), Claim the Warm Ground (Nightshelf).
- T5: palette equity — wars scar their ground, pods name polynyas, canons
  get carried, settlements hire hulls, factions parley with pods; the
  springs-wintering verb seeks thermal sites.
- T6: two chronicle archetypes — The Scar That Would Not Close (region's
  first chronicle presence; runs on the land's slow clock) and The Burden
  (held Before-Times weapon, bearer/burden stances).

## Watch items

- `faction_claims_the_warm_ground`: zero attempts in the 25k gate — pool is
  unclaimed TheIce vents on geothermal 400+; check again at 50k+ scale.
- `penguin_shelters_at_the_vent`: rare by design (4 attempts/25k); confirm
  it matters at longer horizons before tuning.
- The scar chronicle runs quiet (cleanse needs the artifact within 2
  hexes); most tellings fade half-healed by design.
- `the_thaw_calls_the_bold` / `conduits_map_hidden_peaks` /
  `sanctuaries_anchor_colonies` are late-window rare (first fires observed
  at ticks 32k–40k) — a 25k run with zero fires is normal variance.
- Pre-existing failure not from this arc: slow-category
  `IceRemembers_EntityCatalogCoversCanonicalKinds` fails on clean HEAD
  (belongs to the in-flight worldgen rework).

## Cleanup done alongside

Completed working plans deleted (CULTURE-THAW-PLAN, EXPLORATION-CONTENT-PLAN;
TERRAIN-CONTENT-PLAN moved to docs/analysis as the audit record). Superseded
HANDOFF-knowledge-system removed. Point-in-time notes moved to docs/analysis
with dates: road-problem findings, gambit-frequency tuning notes, cultural
cohesion audit, content-design thesis. Left in place as live/other-stream:
HANDOFF-lifetime-capacity, TRACKING-authored-content-prep-cleanup (fog UI
presentation still open), TRACKING-culture-identity, TRACKING-hpa-architecture,
TRACKING-name-forge-subkind-coverage, TRACKING-narrative-quality,
TRACKING-nexus-and-agency.
