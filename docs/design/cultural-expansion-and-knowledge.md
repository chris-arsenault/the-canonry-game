# Cultural Expansion & Knowledge — Working Design Doc

**Status:** in progress (design + first implementation passes). This is a living
working document. Re-read it at the start of every session on this system; keep
the "Current state" and "Open questions" sections current. It exists because the
work spans many sessions and compaction keeps dropping the thread.

## Goal

A world-progression / "research" / knowledge system that (1) makes the world
feel alive and visibly progressing, (2) gives actions meaning, and eventually
(3) gives the player agency (deferred — first passes are fully emergent). Not a
Civ-style tech tree in feel; ES2 tech-circle is the reference for shape (radial,
directional, tiered outward). But the *mechanical substrate* for expansion is
modeled on a **Civ game** (see below).

## The core model: Civ, on both planes

The system runs on two competing priorities, in **both physical and semantic
space**:

1. **Stable home base (fortress).** Existing anchors hold and reinforce a
   bounded cultural footprint around themselves. Culture mass concentrates and
   persists at the anchor. (Civ: a city radiates culture, works its tiles,
   defends a bounded border.)
2. **Expansion.** Cultures found **new anchors** further out, each a new
   emission point with its own footprint, so the culture's reach marches
   outward. (Civ: a settler leaves a city and founds a new city; the empire is a
   network of city-anchors covering expanding territory.)

The frontier advances because cultures **found new anchors outward**, not
because mass magically spreads. Reaching a higher tier (further ring, see below)
means the anchor network has been pushed out to that ring.

### The mechanism (decided — keep it this simple)

Expansion is just this: **generators — of all types — place their entities in
frontier space, on both planes.** Generators are the world's entity-creation
engine and fire constantly; if the entities they create land at the *edge* of
the culture's reach instead of clustering in the heart, the culture's anchor
network (and the frontier) marches outward on its own. No carrier units, no
special "founding" trigger, no kernel mass-echo. The only piece that has to
work is a **frontier-space placement** that reliably drops a produced entity at
the edge of the culture's reach — which today it does not, because generator
placement searches coverage cells or fixed radius bands, and the culture
frontier is a mass field (the culture's raw-influence border). Making that
placement real, and having entity-creating generators use it, is the whole job.

### Mapping to this world

- **Anchors = static entities**, per plane. On the semantic plane the static
  kinds (penguins, artifacts, …) are the "cities" of idea-space: fixed cultural
  emission points. On the physical plane, settlements and static physical
  entities are the anchors. **Every static entity is a potential anchor** — the
  mechanism must be general across kinds and cultures (Wake-Singers expand via
  orcas, Nightshelf via its kinds, etc.), NOT a single special kind. ("How do
  orcas get cultural expansion?" — the mechanism cannot be penguin/scholar-
  specific.)
- **Mobile entities = settlers / carriers.** Mobile kinds (orcas, factions,
  abilities on the semantic plane) carry culture toward new ground and are the
  plausible agents that found (or trigger the founding of) new static anchors at
  the frontier.
- **Emission.** An anchor emits cultural mass into a bounded footprint around
  it. Expansion = founding a new anchor whose footprint sits further out.

### Tiers = radius

Tech/idea tier ≈ rough radius from center: the r18 heart holds foundational
ideas; outer rings hold advanced ones. Climbing tiers = pushing the anchor
network (and thus the frontier) outward. A tech is drawn from a fixed authored
set; copies appear on the map through play, terrain-flavored (angle = discipline,
radius = tier), claimed when a culture's presence reaches them.

## Hard invariants (see memory `project_tech_design_invariants`)

Check EVERY proposal against all of these; each has been forgotten at least once:

1. Culture consolidates at strongholds; it does NOT march outward on its own.
2. Many entities are static on the semantic plane (penguins, artifacts); only
   institutional/carrier kinds self-move there.
3. Distance is proximity only; no absolute "similarity" metric.
4. Culture is contested on both planes (mass + cleanup + dominance).
5. Canons are neutral, world-owned coverage regions, NOT culture fortresses.
6. No stock/flow reservoirs; carry accumulation as mass, coverage, or
   relationship strength.
7. Named entities are the heroics; ambient churn acts on unnamed masses.
8. Knowledge can be lost, but forgetting is the exception (dark age), not the
   default.
9. Grants are an authored layer over existing primitives (gambits, generators,
   world-beat charges, effects), not a new parallel engine.
10. The system must produce visible GROWTH over a run, not a static 1–3 holdings.
11. Refinement = culture-mass band tiers; no semantic z-axis (none exists).
12. Player agency is deferred; first passes are fully emergent.
13. Tech tier = rough radius from center.
14. Two drives on BOTH planes — expand + fortress — weighted differently per
    plane.
15. Techs are drawn from a fixed authored set; copies appear through play.
16. A breakthrough is a static entity placed at the tech site; that placement is
    the culture's claim (different cultures → different embodiments; one site).

## Current state (keep this current)

**Frontier placement works, and is applied as a deliberate balance — not a
blanket.** A new `SemanticPlacement.Frontier` (kernel
`GeneratorPlacementCodes.Frontier`) searches the produced entity's culture
raw-influence border and places at the outermost free, known, terrain-allowed
cell just beyond it, falling back to a near-origin search. `Required`/
`RequiredWakeSingers` take a `frontier: true` flag; it is set ONLY on **founded
static place-anchors that reach new ground** — colony, newsite (new current),
reefhold, harbor, peak, shoal, rookery. Everything else stays home: retinue
(orders, choirs, witnesses, rites, seals…) near its parent, target-anchored and
restoration entities in place, conflict generators (monsters, wars, intrusions)
on their own placement. Mobile productions (e.g. the towed citadel) are not
frontier — a mover would just roam back.

Result (MC `frontier-balanced-2`, 20k ticks): the semantic frontier climbs from
the r18 heart to **ring 29 and is still climbing** (steady, not the ring-26
plateau, not the rush-to-rim of the earlier blanket version), with **~79% of
living entities in the heart and ~21% at the frontier** — a real home/expansion
balance. `make ci` green.

Lesson locked in memory (`feedback_no_opposite_extreme`): the fix for "not all X
Near" was NOT "all X Frontier" — it was the *distribution* above. Don't swing
to the opposite pole.

Still TODO: physical-plane frontier placement (only semantic is tuned so far;
physical stays Near to avoid scattering physical bundles — do it carefully),
pace/mix tuning (which anchors and how many is a knob), then the authored tech
set / claim / telemetry (tasks 39–41).

### History

Branch `progression-lattice`. Semantic-plane reframe (neutral axes + growing
map) is merged and done. On the expansion/knowledge system so far:

- **Per-plane expand/fortress posture** (`IceRemembersMinorActors.cs`):
  semantic leans expand, physical leans fortress. Effect on frontier reach alone
  was small — not the lever.
- **Semantic frontier mass-echo** (`MapFrontierProgramRuntime.SemanticGrowth.cs`):
  each opened cell inherits 72% of the threshold from the pressing culture. This
  made the frontier climb r18→ring 33 over 20k ticks and keep climbing (MC
  `knowledge-seed-1`), vs a plateau at ring 26 that died to 4 new cells/bucket.
  **This is a shortcut, not the real mechanism** — it makes the frontier number
  climb without founding anchors, and is probably too strong (unbounded creep
  from one source, unlike a bounded Civ border). To be reconsidered/bounded or
  replaced by proper anchor founding.
- **`scholar` penguin subtype + `frontier_scholars_settle_the_reaches`
  generator** (`IceRemembersGeneratorDefinition.Knowledge.cs`, `PenguinEntity.cs`):
  a first stab at "found a static anchor at the frontier." **Wrong as built:**
  penguin/scholar-specific (not general across kinds), and generator placement
  can only target coverage cells or fixed radius bands — the semantic frontier
  is a mass field with neither — so scholars land near the heart, not the edge.

### Why the current impl is off (the recurring failure)

The correct end is a general Civ-style expansion system (found new anchors of a
culture's characteristic kinds at the frontier). Instead I fixated on the
symptom ("make the frontier ring number go up"), fought the generator placement
system, and hacked a kernel mass-echo. Means got confused for the end.

## Decided (no more debate)

- Expansion = generators of all types place entities in frontier space, both
  planes. No carriers, no founding-trigger machinery, no mass-echo.
- Build a real **frontier-space placement** that searches the culture's
  raw-influence border (the mass field), on both planes, and place there.
- Have entity-creating generators use it (broadly — via the per-kind culture-
  placement default and/or generator configs), so all types spread outward.
- Rip out the semantic frontier mass-echo. Drop the special scholar generator
  (the frontier placement is general; no penguin-specific mechanism).

## Next steps

1. Remove the mass-echo (`MapFrontierProgramRuntime.SemanticGrowth.cs`).
2. Make `CultureFrontier` placement search the culture's raw-influence border
   (both planes); today it uses coverage cells and finds nothing for culture
   mass.
3. Apply frontier placement broadly (per-kind default in `CulturePlacementFor`
   and/or generator configs); drop `frontier_scholars_settle_the_reaches`.
4. MC: frontier climbs because generator-placed entities land at the edge.
5. Then layer the authored tech set, claim, and telemetry (tasks 39–41).

## MC evidence log

- `semantic-expand-20k` — frontier plateaus at ring 26, new cells die to 4/bucket
  (expand weight alone insufficient; sources are heart-bound).
- `knowledge-seed-1` — with the (since-removed) mass-echo, frontier climbed to
  ring 33. Growth worked but via the shortcut, not the real mechanism.
- `frontier-place-1` — blanket frontier placement (all produced entities): rim
  (ring 40) by ~tick 8000. Too aggressive — replaced by the balanced version.
- `frontier-balanced-2` — balanced (7 founded place-anchors frontier, rest home):
  ring 29 and still climbing; ~79% of entities in the heart, ~21% at the
  frontier. The keeper for expansion.
- `frontier-balanced-2` (tech analysis, DuckDB only) — tested tech = (tier band ×
  terrain), claimed at mass ≥ 30: the claim LOOP works (cultures accumulate
  techs, diverge 30/24/24/8, reach tiers 4–6), but the catalog caps at ~24–30
  (bounded by tiers×terrains) and is ~80% front-loaded by tick 5000. Confirms the
  freedom cap → moving to discrete named sites.

## Tech representation: DECISION (named hexes)

`(tier × terrain)` is dropped as the catalog (too small, front-loaded). A **tech
is a named hex** seeded across the semantic disk at authored (tier, terrain)
spots, most in fog beyond the frontier. Authored set = a plain growable list of
named techs (adding a tech = adding a site → unbounded). A culture **claims** a
site when its semantic mass over that hex crosses the band (pure coverage,
co-holdable, no stockpile). Tier = the site's ring, discipline = its terrain.
Claiming stays sustained because outer sites are reached across the whole run.
Telemetry = per-culture claimed-site count + max tier. Grants come after.

**Validated (DuckDB preview on frontier-balanced-2, synthetic 196-site set at 4%
density):** claiming is sustained over the run (culture 3: 4→4→5→10; culture 2:
10→16), not front-loaded like (tier×terrain); claim count scales with site
density (tunable) and max tier scales with frontier reach; a culture claims the
sites its footprint covers. The model works and fixes both the cap and the
front-loading. Sites MUST be data (named coords) — outer sites live at fog
coordinates that don't exist as cells until the frontier opens them.

**Build plan (lightest real version):** seed generates a knowledge-site catalog
(named coords across the disk, density a knob), held in a small kernel structure,
with per-culture claimed-site count + max tier exported as telemetry. Claim =
semantic mass ≥ band at the site's cell. No grants yet.

**Authoring: DECIDED — hand-authored, always was.** The catalog is a discrete
hand-authored list of named techs. "Unbounded" means we keep ADDING authored
techs to the list; it is NOT procedural generation. Each authored tech has a
tier (radius band) and a direction/sector (to spread them and drive divergence);
the seed places each at a hex at that ring+direction. Outer rings open as
open_field, so discipline is the angular sector, not the terrain (terrain variety
only exists in the heart). Add a tech = add a line to the list; the seed places
it. Claim = a culture's semantic mass reaches the tech's hex.

**Authored list exists:** `src/IceRemembers/IceRemembersKnowledge.cs` — 27 named
techs across 5 tiers × 6 disciplines (Remembrance/Deep/Craft/Hunt/Song/Cold),
growable by adding lines. Tier band width = 8 rings; discipline = one of six
spokes.

**Validated (DuckDB, real authored hexes on frontier-balanced-2):** the discrete-
hex claim works and cultures diverge (claimed 4 / 12 / 4, different subsets). But
placing each tech on an EXACT axis spoke is too brittle — a culture must hold
that precise cell, so counts are sparse and max tier stalls at 1–2 even though
the frontier reached ring 29. **Real-build fixes:** (1) claim = mass ≥ band
within a small radius of the tech hex (control the hex and its immediate ring),
not the exact cell only; (2) place techs slightly off the pure spoke / at several
offsets per (tier, discipline) so they land where footprints actually spread; (3)
higher-tier claims also need faster expansion or longer runs.

**Remaining build:** seed places the authored techs (tier→ring band, discipline→
spoke, with offsets) into `DomainStartupSeed` as knowledge-site coords; a small
kernel structure holds them; per-culture claimed-tech count + max tier as
telemetry; then the in-sim claim curve. Grants after.
