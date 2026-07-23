# Terrain-Aware Content: Audit and Plan

Audit date: 2026-07-23. Scope: all authored IceRemembers content (67 generators,
58 gambits, 51 movement verbs, 18 ages, 8 chronicle archetypes) against the
terrain interface the engine actually exposes. Companion goal: identify where
net-new generators/gambits are needed so every entity kind has a workable
palette, not just the four mobile kinds.

## 1. What the audit found

### 1.1 The supply/demand picture

The engine exposes terrain to content through exactly two rich surfaces and
one scoring system:

- **Generator placement** — preferred/avoided/allowed terrains, five site
  flags (summit, thermal, anomaly, polynya, crater), four band affinities
  (warmth, fire, aurora, pressure), FeatureAnchor+FeatureKind, Frontier,
  Across, Wilderness/CultureFrontier/CultureRoute, coverage
  PreferredTerrains/TransformTerrain, and one target-side filter
  (TerrainCountRequirements: "fewer than N cells of terrain X within R").
- **Movement verbs** — terrains set, minTemperature, fogAdjacent, sites
  (declared but never used by any verb), distanceScale, travelInfluence.
- **PlacementTaste** — 12 fact-derived axes (capacity, temperature, moisture,
  elevation, relief, ice, river, fire, aurora, pressure, water, coast) scored
  for kinds and cultures.

Everything else is terrain-blind at the interface level:

- **Gambit preconditions cannot read terrain.** No precondition reads terrain
  type, hex facts, or site flags at self/target location. The only indirect
  couplings are WithinTravelReach (terrain-derived movement cost) and the
  culture-field preconditions.
- **Trait effect conditions** read culture/signal fields and distance, never
  terrain.
- **Chronicle conditions** have pair-distance only (used well by Flight/The
  Closing and the Far Wanderer), no terrain reads.
- **World-beat facts** (age open/close) are population/culture facts only.

Demand matches this shape: 11 of 67 generators are terrain/frontier-aware;
roughly 12 of 58 gambits touch the map at all; terrain-aware movement is
concentrated in penguin/orca/transport/monster. The terrain system built this
cycle (16 fact columns per hex, features, sites, taste) is consumed almost
entirely by placement and four kinds' movement.

### 1.2 Flavor-mechanics mismatches (retrofit list)

Twelve generators promise geography in their fiction that their mechanics
ignore. Sibling generators already demonstrate the correct pattern in every
case, so these are cheap, high-confidence retrofits:

| generator | fiction says | mechanics do | fix with existing vocabulary |
|---|---|---|---|
| what_the_spring_remembers | cleansed hot spring, "where the warmth holds" | Near, default profiles | sanctuary/rite outputs prefer HotSprings + Thermal site, warmth affinity |
| cleansed_springs_found_sanctuaries | "settle around the warmed scar" | Near | same as above |
| controlled_depths_wake_leviathans | "the depth answers" at bloom/shoal | Near | leviathan output: water-preferring profile + pressure affinity |
| overclaimed_blooms_draw_beasts | bloom/shoal harvest edge | Near | beast output: coast/water preference |
| conduits_map_hidden_peaks | "a peak enters the record as a navigational fact" | Near + semantic frontier | FeatureAnchor peak — **blocked on worldgen identifying peaks** (see 1.3); interim: Summit site preference |
| mapped_peaks_birth_techniques | mapped peak, crossing the ice | Near | technique/tome placement near summit sites |
| pod_currents_launch_scouts | hunting ground, claimed waters, named shoal | Near + semantic frontier | shoal output: FeatureAnchor lake or water profile |
| the_current_provides | "warm water widens... along the corridor" | Near + CultureRoute | calves prefer water + warmth affinity |
| the_thaw_calls_the_bold | young colony on new ground, "the unread white" | Near | witness/relic outputs Frontier-adjacent (physicalFrontier on the wayfinder) |
| the_stone_remembers_the_walk | far walk, frost, far trails | Near | waystone: ice/tundra preference, aurora affinity (already lands beside the walker — mild) |
| corruption_wakes_monsters | "wakes a monster under the ice", lair | Near | wraith: DeepIce-adjacent/ice preference + anomaly site |
| claimed_waters_draw_bulls | claimed waters, living presence at the site | Near | bull: water terrain preference |

Also unused vocabulary worth exercising: the **Crater site flag** (no content
uses it — natural fit for Before-Times anomaly content), the **`sites`
parameter on movement verbs** (never passed — e.g. monsters lairing at
anomalies, pilgrims seeking thermal sites), and band affinities beyond the
few kinds that set taste.

### 1.3 Supply gaps that block content

1. **Worldgen identifies only RiverSystem and Lake.** Range, Peak, Waterfall,
   Crater, Plain exist in the kernel vocabulary and the FeatureAnchor
   machinery handles them already (kind filter is data-driven), but no
   identification pass produces them. Peaks are the highest-value unlock
   (two authored generators want them); Crater ties into Before-Times lore.
   Coordinate with the in-flight microtriangle worldgen rework before adding
   identification passes.
2. **No terrain-reading gambit precondition.** This is the single biggest
   interface gap: an entire content family (58 gambits) cannot ask "am I
   standing on warm ground?" or "is my target's hex geothermal?". The O2
   plan's "fact-reading preconditions (geothermal refuges, passes)" was
   deferred for exactly this reason. Proposal (fits the projection
   principle — numeric bands, not categorical ID lists):
   - `SelfHexFactThreshold(factCode, min?, max?)` and
     `TargetHexFactThreshold(...)` — read one of the 16 fact columns at the
     entity's physical hex.
   - `SelfOnTerrainGroup(boundaryGroup)` — terrain *group* (harsh_land,
     frozen_land, warm_land, maritime, forged), not per-terrain hand lists.
   Two precondition shapes cover every terrain-gated gambit idea found in
   this audit.
3. **No terrain chronicle condition / world-beat fact.** Lower priority: pair
   distance already carries the journey tales, and ages modulate movement
   globally. Revisit only if a concrete tale needs it (e.g. "the wanderer
   stands on the far shore" would need a continent-id or terrain condition).

## 2. Palette equity by kind

Current palette (actor gambits / target gambits / movement verbs / chronicle
roles):

| kind | actor | target | verbs | chronicle | assessment |
|---|---|---|---|---|---|
| penguin | 18 | 8 | 11 | 5 | full palette; the protagonist kind |
| faction | 10 | 8 | 2 sem | 1 | strong graph agency; zero terrain engagement |
| orca | 6 | 2 | 8 | 2 | good actor; thin as target |
| transport | 6 | 2 | 6 | 1 | good mover; thin as target |
| settlement | 5 | 8 | 2 sem | 2 | placement-rich; no terrain-gated gambits |
| monster | 4 | 8 | 5 | 2 | healthy; unique heat-seeking |
| artifact | 3 | 7 | 5 | 2 (open only) | never chronicle primary |
| ability | 2 | 1 | 5 sem | 1 | lowest target count |
| region | 1 (work) | 4 | 2 sem | 0 | pure object |
| occurrence | 1 (work) | 3 | 2 sem | 0 | pure object |
| point_of_interest | 1 (work) | 5 | 3 sem | 0 | pure object |
| canon | 1 (work) | 2 | 0 | 0 | fully static |

Principles applied to the proposals below (from standing design constraints):
place-kinds and canon stay static — their agency is *drawing the mobile
kinds* and *changing what happens around them*, not walking; named-entity
involvement stays rare escalation; no reservoir attributes; prefer numeric
fact bands over hand lists.

### Net-new content proposals

**Region** (the land itself should act through weather/thaw, not walk):
- Gambit `region_breathes_the_thaw` — region self-gambit, gated on
  SelfHexFactThreshold(Geothermal, min) [needs precondition from 1.3.2];
  strengthens cleansed_by/anchored edges, small coverage terrain warm-up.
  The scar/spring cycle gets a middle verb it currently lacks.
- Chronicle archetype `the_scar_that_would_not_close` — region:scar primary
  (corrupted_by ≥ threshold), cleanser penguin supporting, artifact open
  role. Beats already expressible: cleanse gambit strikes, corruption
  attribute bands, Historical endings. Gives region its first chronicle
  presence and reuses the existing cleanse/corrupt content loop.

**Occurrence** (wars and festivals should mark the land):
- Generator `the_war_leaves_its_mark` — targets occurrence:war with high
  momentum; produces region:scar via ScarRegion pattern (coverage
  TransformTerrain=wastes) at the war's location. Closes the loop
  war_breaks_the_land opened (which targets factions, not the war itself)
  and makes occurrences terrain-consequential.
- Chronicle: no new archetype — occurrences already thread through rivalry
  beats (wars_get_names). Their gap closes via the generator above.

**Point of interest** (places act by calling):
- Generator `the_polynya_never_closes` — FeatureAnchor-style POI creation on
  Polynya sites (site flag exists, unused): open-water refuges that draw
  fishing verbs and orca. First consumer of the polynya flag.
- Movement verb retrofit: pass `sites:` on existing seeker verbs
  (monster_surface_at_the_fissure → anomaly sites,
  penguin_winter_at_the_springs → thermal sites) so POI-adjacent geography
  actually pulls movers.

**Canon** (static by design — agency through observance, not motion):
- Generator `the_canon_is_carried` — targets canon with strong observance;
  produces a transport:trade or penguin:witness that carries it (relationship
  ConduitOf), placed toward CultureRoute. Canons stay put; the world moves
  because of them. No movement verbs for canon — that stays intentional.

**Orca/transport as targets** (thin target columns):
- Gambit `settlement_hires_the_hull` — settlement→transport, travel-reach
  gated; charters idle transports toward harbors (Holds/ControlledBy edges).
- Gambit `faction_parleys_with_the_pod` — faction→orca, culture-gated
  (Flipper Accord fiction); rare de-escalation edge for the raid loop.

**Faction terrain engagement** (10 gambits, zero terrain reads):
- Retrofit, not new content: gate `faction_claims_point_of_interest` variants
  on target hex facts once 1.3.2 lands (geothermal claims for Nightshelf,
  aurora ground for the Stack — via fact thresholds, not culture hand lists).

**Artifact as chronicle primary**:
- Archetype `the_burden` — artifact:weapon/seal primary with strong Holds
  chain, bearer supporting role. The Before-Times fear made personal.
  Existing beat vocabulary suffices (Holds strength bands, bearer Historical,
  PairRelAbsent = the artifact is laid down).

## 3. Proposed milestones

- **T1 — Terrain preconditions (kernel+domain+DSL).** SelfHexFactThreshold,
  TargetHexFactThreshold, SelfOnTerrainGroup. Small kernel surface, unblocks
  every terrain-gated gambit. Includes MC instrument: precondition rejection
  telemetry by code (already exists for preconditions generally — verify
  fact-threshold rejections surface).
- **T2 — Generator retrofits (no new interface).** The 12-row table from 1.2
  minus the peak-blocked rows: springs, blooms, depths, currents, bulls,
  wraith lairs, thaw witnesses, waystone profile. Pure content edits through
  existing placement vocabulary. Gate: placements land on fitting terrain in
  MC (placement-by-terrain histogram per retrofitted generator).
- **T3 — Feature identification expansion (worldgen).** Peak and Crater
  identification passes (Range/Waterfall/Plain deferred until content wants
  them). Then FeatureAnchor retrofit of conduits_map_hidden_peaks +
  mapped_peaks_birth_techniques, and a Crater/Before-Times discovery
  generator. **Coordinate with the in-flight microtriangle rework first.**
- **T4 — Terrain-gated gambits.** The O2 leftovers (geothermal refuges,
  pass-finding) plus region_breathes_the_thaw and the faction claim
  retrofits. Depends on T1.
- **T5 — Palette equity content.** the_war_leaves_its_mark,
  the_polynya_never_closes, the_canon_is_carried, settlement_hires_the_hull,
  faction_parleys_with_the_pod, verb `sites:` retrofits.
- **T6 — Chronicle additions.** the_scar_that_would_not_close, the_burden.
  Lore re-read before authoring (standing rule).

Sequencing: T2 is independent and can start immediately; T1 before T4; T3
gated on worldgen coordination; T5/T6 last (content on top of everything).

## 4. Status

- [ ] T1 terrain preconditions
- [ ] T2 generator retrofits
- [ ] T3 feature identification (peaks, craters)
- [ ] T4 terrain-gated gambits
- [ ] T5 palette equity content
- [ ] T6 chronicle additions
