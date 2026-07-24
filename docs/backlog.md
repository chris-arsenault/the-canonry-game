# Backlog

Deferred work with its origin. Point-in-time findings stay in
`docs/analysis/`; this file is the durable list of decided-but-not-scheduled
items and the verification debts owed before the next formal evidence run.

## Deferred by explicit decision

- **Per-kind creation shares as per-band pulse weights** (owner decision,
  2026-07-24). When provincial throughput grows, which kinds fill the new
  slots. Natural home: per-band generator pulse weights (frontier skews
  discovery/founding, heartland skews wars/politics). The owner has ideas on
  the broader budget design; do not implement without them.
- **Typed installed-legacy component** (owner decision, 2026-07-24; full
  design: `ordered-emergence-population-plan-20260718.md` §Legacy Retirement
  Gambits). Chosen representation for now is fungible legacy (option a):
  explicit durable host-owned state with the artifact-host relationship
  retained for history. The typed alternative is a fixed-capacity
  installed-legacy attachment recording historical source entity id, host
  entity id or landscape coordinate/coverage owner, registered legacy kind
  id, magnitude/authored parameters, installation tick, and active/removed
  state — the future shape for map-placed objects in the Civ-6-districts
  direction. Binding constraint EITHER WAY: the installed benefit must be
  attributable and reversible so a history-facing reactivation mechanic
  stays possible. Do not add the typed component until districts need it.
- **Artifact resurrection / return from the zeitgeist** (owner note,
  2026-07-18: durable things "come back" as a follow-up mechanic; design
  sketch in the superseded proposal + plan). A history-facing mechanic
  reactivates a historical artifact: rediscovery by expedition/generator
  (e.g. `records_cast_shadows` targeting historical relics *instead of
  minting new ones* — creation-pressure relief), or un-installation of an
  enshrined legacy (reverse the durable benefit, restore active status).
  Requires the legacy data shape above; not separately approved yet. POIs
  already have their loop (faded POI frees its FeatureAnchor for
  re-naming) — verify it closes.
- **Dynamic meta-entities** (explicitly not a requirement). Settlement +
  faction + leader + held artifacts as one game/UI entity. Superseded by
  legacy retirement gambits + carry-to-holds unless clutter persists after
  the current stack is judged.

## Held in reserve (fire only on evidence)

- **Arrival-time repulsion**: resting entities refusing already-occupied
  hexes — only if the 100k stacking tail stays fat after the beacon
  removal, saturation 0.6, and cadence endings.
- **Density band re-ratification**: proposed bands (active/covered 0.3–0.5,
  semantic occupancy ≤15%, mortal share ≥20%) are provisional; re-judge
  against the next formal 3×100k.

## Standing engine items

- **Relationship history recycler** (`HANDOFF-lifetime-capacity.md`).
  Relationship slots are lifetime totals; culled edges never recycle. R5
  (2026-07-13) established eviction is safe. Now urgent-adjacent: provincial
  pulses raised creation ~2.6×, so 100k+ runs may approach the 65,536 cap.
  Recycler deferred by owner decision (2026-07-24); lifetime totals are
  measured as part of each formal run's scorecard, observation only.
- **Archival policy for the historical graph** (plan): active-population
  work never deletes history; a separate design owns eventual archival at
  the 2.5M-tick horizon.

## Verification debts (bundle into the next formal 3×100k, do not run it early)

From the demography stack (slices 1–3, uncommitted as of 2026-07-24):

1. Density plateau 0.3–0.5 active/covered at horizon.
2. Mortal share ≥20% of active population (the provincial-pulse headline).
3. Artifact/POI net convergence toward zero post-bootstrap.
4. Settlement demography: bimodal survival, count tracking covered cells,
   razing fires attributed (not just solitude).
5. Every new ending firing with attribution — seal-shatter, one-word-yields,
   memorial wall, tome-read, offering-spent, slips-the-grasp, enshrinement
   (durable host stat verified applied) — without gutting war-saga or
   world-beat cadence.
6. Wild-vs-held artifact split improving; no carried-forever deadlock from
   the movement-suspension rule.
7. Movement at horizon: artifact/transport reversal and volume; artifact
   rest-at-destination holding; penguin health unchanged.
8. Departure loop vs settlement lifeline: crowded hubs shed excess without
   starving the population that keeps settlements alive.
9. Semantic plane: occupancy ≤15%, stacking low, travel-influence verbs not
   distorting culture shape.
10. Provincial pulses: fires scale with settledness across the run, band
    promotions occur, dormant provinces wake on reveal, world tempo lands
    ~2–3× the pre-provincial rate at bootstrap settledness.
11. POI rediscovery loop actually closes at least once.
12. Performance ≥ ~50 ticks/s at 100k; relationship lifetime totals leave
    headroom (see recycler above).

From artifact districts (`docs/design/artifact-districts.md`):

12a. Installs and reawakenings both attributed at horizon; tile bonuses
    verifiably applying (host health deltas, district-tile influence);
    settlement footprint growth from installs visible in coverage; no
    install-rate runaway (installed share of artifact retirement sane).

From entity stages (`docs/design/entity-stages.md`) — NOTE: this change
reshapes the culture economy; expect rebalancing iterations at smoke scale
before this run is worth spending, and treat these lines as the acceptance
gates for that rebalancing:

17. **Cultural mass over time** — the headline: per-lane income statement
    (map_control vs gameplay vs effects/emission gains and losses) at
    25k/50k/100k; total physical raw mass trajectory; cells above
    trace/claimed/controlled thresholds; covered-cell growth — all
    non-regressing vs the pre-stage baseline despite grind removal.
18. Stage pyramid: many base-stage, few apex entities per catalog kind;
    promotion/demotion rates; deaths only at base stage; demotion events
    attributed to their causes (generator side-effects vs hard times); no
    demotion spirals (repeat-demotion frequency bounded).
19. Deepen family (promotion gambits) < 10% of gambit executions; freed
    pool share flowing to context gambits, not evaporating.
20. Mortal-kind lifespans within their healthy bands despite the
    maintenance-labor shift (78% of maintenance was tier-carried).
21. Per-stage ambient emission + district tiles + decay relief carrying
    interior holding: interior (non-frontier) mass share stable; thaw and
    reclaim cadence unaffected.

From the exploration/terrain arc (`SUMMARY-exploration-terrain-arc-20260723.md`
watch items, carried until judged at 50k+):

13. `faction_claims_the_warm_ground` — zero attempts at 25k; re-check at
    50k+ scale.
14. `penguin_shelters_at_the_vent` — rare by design; confirm it matters at
    longer horizons before tuning.
15. Late-window generators (`the_thaw_calls_the_bold`,
    `conduits_map_hidden_peaks`, `sanctuaries_anchor_colonies`) — first
    fires 32k–40k; zero at 25k is normal variance.
16. Pre-existing slow-suite failure:
    `IceRemembers_EntityCatalogCoversCanonicalKinds` fails on clean HEAD
    (belongs to the in-flight worldgen rework, not this stack).
