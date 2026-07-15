# World Kinetics — why the map reads as chaos and stagnation

Point-in-time diagnosis, 2026-07-15. Evidence run: `c2d-quiet-trio-3x25k-20260715`
(run 4700 unless noted). Prompted by the observed play experience: entities not
moving, entities clustering around a settlement for decades and dying, and no
ebb and flow of cultures at zoom-out — despite culture, verbs, gambits,
generators, and chronicles all functioning by their own metrics.

The intended model: **low-altitude verbs drive the ebb and flow** (with gambits
as punctuation on top). The verb layer is not idle — the failure is in the
*shape* of what it produces.

## 1. Verbs are the movers, and they generate jitter, not journeys

- Movement attribution (physical plane): penguins 43,697 verb-sourced hops vs
  130 gambit-sourced; orcas and monsters ~100% verb-sourced; transports split
  verb/relationship. Every hop is distance 1.
- Tempo: median penguin steps once per 21 ticks, orca 27, monster 121,
  ability 538. One monster never moved in its life.
- **Displacement efficiency: a median penguin makes 80 hops per 2,500-tick
  epoch but nets only 11 hexes of displacement — ~86% of verb walking cancels
  itself out.** Verb targeting is centripetal: intents alternate among
  near-home influence/reinforcement targets around the same centroid, so the
  layer produces a random walk, not travel.
- Home orbit: the median penguin spends 54% of its time within 2 hexes of its
  epoch home (p75 78%). Monsters' median lifetime range is 11 hexes;
  transports 11.5 (the route-runners jitter around a terminus).

## 2. Culture flux exists arithmetically but never spatially concentrates

- Per epoch, cell-influence gains vs losses run ~2.5M : 2.47M — a churn ratio
  of ~1.02, uniform everywhere, always net-positive. Witness mass grows
  monotonically for every culture, every epoch, the whole run (Culture 1:
  31k → 260k, no reversal anywhere).
- Ebb exists as micro-noise; it never concentrates at a place strongly enough
  to flip ownership or move a boundary visibly. There are no fronts, no
  collapse, no reconquest. Wars damp relationship edges but nothing couples
  conflict (wars, raids, hot chronicles) to concentrated territorial erosion.
- Zoom-out therefore reads: fill in for ~15k ticks, then a frozen map with
  imperceptible flicker.

## 3. Deeds are punctuation on a static page

- Worldwide deed density: 0.27 gambit attempts/tick, 0.13 successes/tick —
  one visible deed somewhere on the entire map every ~8 ticks; in any local
  region, one every few sim-months.
- Chronicles concentrate meaning (heat, auras, journal) but not motion: beat
  charges bias which gambit an actor picks and how often it succeeds, but
  nothing makes a hunter walk toward the quarry or a second journey to join a
  hunt. Generators teleport consequences into existence.
- Kind activity: gambit attempts per 1,000 ticks of life — faction 3.5,
  penguin 2.4, monster 1.8, orca 0.68. Orcas are structurally inert as actors
  (a full-run orca made 1 gambit attempt in 23,535 ticks); their lives flow
  through generator loops.

## Biography spot-checks (same run)

- Veilmonger Tutris (seed penguin, 77y): 41 gambit attempts, 779 hops in a
  29x18 box, 2 chronicle appearances - the system's best case, still visually
  a slow orbit.
- Glyphmaster Tivesa (penguin, 78y): became a teacher in old age (6x
  passes_the_teaching after t19,530) - an emergent arc invisible on the map.
- Grandmother Deep (orca, 94y): 1 gambit attempt, zero wars, zero stories.
- Hroorsoom (monster, 40y): hunt-qualified (hoards + wars), never cast - the
  1-per-archetype cap left the hunt untold.

## Implications (design axes for a kinetics round; not yet proposed)

1. **Journeys**: verbs (and chronicle charges) need long-range purposeful
   movement - pursuit, pilgrimage, migration, caravans that traverse routes -
   so gross motion becomes net motion. The displacement-efficiency number
   (80 hops -> 11 hexes) is the metric to move.
2. **Fronts**: culture needs spatially concentrated contest - conflict
   coupling to erosion at places - so ownership can visibly recede. The
   gains:losses ratio (1.02 uniform) is the metric: ebb must localize.
3. **Tempo/legibility**: action density and step cadence tuned for watching,
   not only for statistics.

The narrative layer (beats, heat, entrances, remembrance) is sound on its own
terms but sits on top of this kinetics gap: it makes stories followable
without making the world visibly alive.
