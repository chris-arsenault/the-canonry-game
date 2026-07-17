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

## Open design questions (resolve before building more)

1. **What founds a new anchor?** Ambient generator, entity gambit, or minor-actor
   labor crossing a threshold? Likely generator (heroic, named) + supporting
   ambient labor.
2. **Do mobile entities settle, or do static anchors spawn daughters?** Civ says
   a mobile settler founds a new static city. Here: do orcas/factions/abilities
   carry out and found, or does an existing anchor bud a daughter anchor?
3. **What is an anchor's footprint?** A bounded emission radius (like a city
   border) so anchors don't over/under-emit. Do we need a new emission term for
   this, distinct from the current reinforce/extend labor?
4. **New cultural-mass-emission mechanics?** Probably yes: a first-class
   "found new anchor at the frontier" behavior (the settler), general across
   kinds; and possibly a per-anchor bounded footprint. The current
   reinforce/extend minor-actor labor covers "hold + creep," not "settle a new
   anchor."
5. **Keep or replace the mass-echo?** Either bound it (model a city's finite
   border) and keep founding for reach beyond, or remove it and let founding do
   all the work.
6. **The generator placement gap.** To found anchors at the true frontier we
   likely need a new placement mode: "place at the culture's semantic *mass*
   frontier" (search the mass-field border), since coverage/radius placement
   can't hit it. This is the one genuinely new kernel piece.

## Next steps

- Resolve the open questions above with the user (reanalysis, not code).
- Then: implement general anchor-founding as the real expansion engine; revisit
  the mass-echo; generalize/replace the scholar generator; only then layer the
  authored tech set, claim, and telemetry (tasks 39–41 in the tracker).

## MC evidence log

- `semantic-expand-20k` — frontier plateaus at ring 26, new cells die to 4/bucket
  (expand weight alone insufficient; sources are heart-bound).
- `knowledge-seed-1` — with mass-echo, frontier climbs to ring 33 and keeps
  climbing (13→92→151→119→118→87 new cells/4k). Growth works; mechanism is the
  shortcut above.
