# Exploration, ice, and culture — first bootstrap-horizon evidence (2026-07-22)

Point-in-time findings for the culture/thaw redesign (CULTURE-THAW-PLAN.md,
M1–M6). Evidence: MC batch `culture-thaw-m6` — 6 runs × 25,000 ticks
(the bootstrap horizon), seeds 1000–1005, ~215 generator fires per run,
no degeneration.

## What the redesign shipped

- **Reveal** (91fcea3): entity presence materializes latent ground,
  one-way, un-gated by age or culture mass; per-kind radii; exploration
  verbs (Scout the White, Sound the Far Water, Voyage Beyond the Charts)
  hunt fog-adjacent cells and ignore the leash.
- **Leash**: unsupported physical targets keep 35% weight;
  `BeyondCultureSupport` drains mobile mortals on unheld ground (middle
  band: penguins 1.0×, factions 0.6×, ships 0.35×, orcas 0.25× of
  −0.12/tick before lifecycle time-scaling). Knowledge range grants lower
  the support threshold.
- **Ice**: one cold-pressure formula (facts × open age) against mortal
  resistance (culture mass, roads, occupancy); deep-ice cover advances
  only over frontier-grown rim in freeze ages, melts in thaw ages; sea
  ice is the water case. Never un-reveals, never touches the heart.
- **Taste-shaped growth** (M5): positive physical culture production and
  expansion targeting scale by `CultureGrowthMultiplier` = 0.25 + derived
  capacity + 0.75 × culture taste (clamped 0.1–2.5, neutral where no
  facts exist). The physical-plane authored terrain `InfluenceMultiplier`
  is no longer consumed (the config field remains for the semantic rows;
  physical values are dead — full authoring removal deferred to avoid
  churning the shared catalog).

## Measured behavior at 25k ticks

- **The whole chain works end to end**: entities range to distance 30–34
  (heart rim is 18), revealing as they go, and movement follows into the
  newly opened ground. Reveals arrive continuously across the entire
  horizon, not just at startup.
- **But the pace is a trickle**: 36–65 cells revealed per run against
  ~48,500 latent cells. Exploration reaches a stable equilibrium: scouts
  push past the rim, attrition (working as designed) turns them home,
  the culture field never extends to hold the new ground, so the range
  never ratchets. The revealed world is the heart plus a thin fringe.
- **Ice breathes correctly scoped**: 3,777 sea-ice cover changes (hard
  seasonal breathing) vs 155 deep-ice changes (conservative, frontier-rim
  only — matching how little frontier exists to contest).
- **Territory shape is now identity** (M5): Wake-Singers' final presence
  concentrates on shelf/ocean (~65% water-adjacent), Nightshelf on
  shelf + aurora ground + ice, Aurora Stack across tundra, hot springs,
  and aurora fields. Same drive, different lands.
- **Leash respects kind**: settlements/POIs/canons static; penguin mean
  max-range 22.9, transports 20.8 ranging to 32, orcas to 31.

## The pacing lever (decision owed)

The equilibrium range is an emergent product of three levers:

1. **Support extension** — the range ratchets only when something static
   holds the new ground. Frontier-founded settlements (generators can
   already found place-anchors; the semantic plane uses exactly this
   mechanism per the knowledge doc) would extend the field, quiet the
   attrition, and let the next generation push farther. **Recommended
   lever**: honors "strongholds, not frontier" and "the world must grow".
2. **Attrition band** — gentler drain deepens corridors without support.
   Cheap to tune, but weakens the leash's meaning.
3. **Reveal radii / verb weights** — widens the fringe, does not move the
   equilibrium.

Recommendation: keep the middle attrition band; add a physical
frontier-founding generator (or extend an existing settlement generator's
placement gates) as the pacing mechanism, then re-measure. Both calls —
the band and the target share of world revealed per bootstrap — are
design decisions, not defaults.

## Watch items

- The growth multiplier shifts total field production (median heart
  ground ≈ 0.65× the old flat rate, rich ground up to 2.5×). Front
  stability at 25k ticks showed no degeneration, but semantic/physical
  mass ratios shifted; knowledge claim pacing should be re-baselined
  before the next knowledge tuning pass.
- Physical `influenceMultiplier` values in the terrain catalog are dead;
  remove them in the next catalog-touching change.
- `map_degenerate=1` flagged once (run 6, seeds 1005) — same rate as
  pre-redesign baselines; not attributed to the new layers.
