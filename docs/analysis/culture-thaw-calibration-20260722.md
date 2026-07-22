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
- **Correction (measured twice, second time on the honest channel):**
  the first draft of this report said 36–65 cells revealed per run.
  That number came from the `CellCreated` event stream, which undercounts
  actual materialization by roughly 20x (see watch items). The
  authoritative `map_snapshots` cell counts show the pulse-based system
  grew the world 1,027 → ~1,900 cells per bootstrap, and two real
  improvements followed: reveal-on-arrival (movement and materialization
  hooks replacing the pulse entirely) doubled growth to ~+1,700, and
  widening the exploration verbs' search reach (distanceScale 14, search
  radius 28) added another third, landing at 1,027 → ~3,300 per
  bootstrap - about 4.7% of the latent world per 100 in-world years,
  which extrapolates to a substantially opened map over a full game
  before any support ratchet exists. Raising exploration verb weights
  2.5x had no measurable additional effect and was reverted.
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

Growth pace is an emergent product of three levers:

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
- **Telemetry:** `map_cell_events` rows with `event_type_code=20`
  (CellCreated) capture only a small fraction of actual cell
  materializations (~50 rows against ~2,000 real creations per run).
  Judge world growth from `map_snapshots` cell counts, never from the
  event stream, until the exporter is fixed.
- Entities routinely travel and dwell on unrevealed coordinates (movement
  events out to distance 45 with far fewer known cells) — arrival reveal
  now materializes ground under and around them, but the movement
  targeting rules' treatment of unknown cells deserves its own review.
