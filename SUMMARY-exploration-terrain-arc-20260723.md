# Summary: Exploration and Terrain Arc (2026-07-22..23)

Temporary orientation doc for the completed exploration/terrain work. The
durable state lives in `docs/systems/` (26-growing-map, 16-generator-executor,
06-entity-behaviors) and `docs/analysis/terrain-content-audit-20260723.md`;
delete this file once it has served its purpose.

## What changed, in order

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
