# Monte Carlo Follow-up Tracking

This doc tracks the current Monte Carlo follow-up gaps as independent work
items. These came from the 10x500 preliminary and 10x2500 long MC runs after
the degeneracy-system removal.

Lifecycle collapse is intentionally not tracked here as a standalone issue.
The likely tuning path is to make generators and gambits more active rather
than treating lifecycle in isolation. Monster absence is also intentional for
now because monsters are not currently spawned.

## 1. Semantic Road Usage And Telemetry

Status: closed. Generated semantic terrain fixed the observed semantic road
failure enough for this item; semantic roads now mature and are used over the
long run.

Semantic movement is building road quality, but the analyzer reports zero
semantic road moves because `road_move_count` only counts destination road
quality above the light-road threshold of `25.0`.

Observed in the long run:

- Semantic moves: `48,429`
- Semantic moves landing on any prior road quality above `0`: `38,497`
- Semantic moves landing on quality above `5`: `6,464`
- Semantic moves landing on quality above `10`: `1,533`
- Semantic moves landing on quality above `20`: `67`
- Semantic moves landing on quality above `25`: `0`
- Maximum semantic road quality: `24.964`

Original gap behind the blocked status:

The earlier telemetry interpretation is incomplete. Semantic roads are used
below the current light-road threshold, but the stronger finding is that
semantic terrain is not meaningfully generated. Physical cells are generated,
painted, serialized, and seeded as physical terrain. Semantic cells are treated
as known by radius, but missing semantic terrain falls back to terrain id `1`,
which is `wastes` in Ice Remembers.

That means semantic movement is probably happening over an all-wastes semantic
plane: movement cost `1.6`, road build multiplier `0.6`, and road speed
multiplier `1.8`. Road building reads terrain through the traveled plane, so
semantic road weakness may be caused by accidental harsh terrain rather than by
the road thresholds themselves.

Road telemetry work was blocked until semantic terrain became explicit. The
next step is to measure whether roads still fail to mature over the generated
semantic terrain map.

Resolution from `.artifacts/monte-carlo/followups-10x2500-20260505`:

- Semantic moves: `127,138`.
- Semantic road moves: `9,896`.
- Semantic road move share: `7.78%`.
- Semantic road cells: `11,669`.
- Light semantic road cells: `801`.
- Heavy semantic road cells: `18`.
- Maximum semantic road quality: `86.262`.
- Semantic road reinforcement reused `10,703` road cells with `91.4%` reused
  road-cell share.

The original gap was that semantic roads never crossed the light-road
threshold. That is no longer true after first-class semantic terrain generation.

## 2. Fix Semantic Map Generation

Status: closed. Semantic map generation now has first-class semantic terrain
vocabulary, materialized semantic cells, and seed entity placement against
semantic terrain pockets. It does not mirror physical terrain, physical
coordinates, entities, or relationships. Roads remain movement-derived.

Semantic map generation should be a first-class system, not a physical terrain
fallback and not a mirror of the physical map.

Original finding:

- Physical terrain is generated through `HexGrid`, terrain painting, startup
  cell serialization, and physical cell seeding.
- Semantic cells are considered known inside semantic bounds even when no
  semantic terrain cell has been materialized.
- Missing semantic terrain resolves to terrain id `1`.
- In Ice Remembers, terrain id `1` is `wastes`, so semantic movement and road
  building inherit physical harsh-land costs.

Original gap:

The semantic plane needs its own terrain vocabulary and generation rules. The
implemented MVP uses `lineage`, `yoke`, `witness`, `silence`, `severance`,
`breakaway`, `beacon_spire`, `taboo_ridge`, `cave_pocket`, and `open_field` to
describe conceptual structure rather than ground. These terrain types shape
movement, influence, claims, and semantic road formation through the shared
terrain hooks.

Implemented scope:

- Added explicit semantic terrain definitions instead of reusing the physical
  terrain catalog by fallback.
- Materialized semantic cells with semantic terrain, continent, and superregion
  metadata.
- Generated semantic terrain from static conceptual worldgen, not from physical
  map coordinates, seed entities, regions, or relationships.
- Placed seed entities into semantic terrain pockets using kind/subtype/culture
  preferences.

Follow-up questions moved elsewhere:

- Semantic road behavior returns to item 1.
- Entity placement debt is tracked in item 8.

## 3. Affordance Coverage And Attribution

Status: open. Runtime trace telemetry now records aggregate affordance
source/shape contribution facts before top-source collapse, and MC confirms
that hidden gambit pressure exists. The remaining gap is coverage and dominance:
top attribution is still generator presence, while relationship-target,
influence, and terrain contribution coverage is still effectively absent.

Latest selection-model finding:

The current low-altitude weighting model should not be tuned further by adding
more constants. Explore and terrain verbs sample a small random subset of the
map (`CandidateSampleCount`, or `CandidateSampleCount * 3` for terrain) and
only rank targets within that sample. With thousands of cells, the map signal
weights mostly choose the best of a tiny random draw, not a meaningfully good
world target. This makes the map terms weak for actual destination selection
and makes affordance tuning hard to reason about.

The nested selection shape is also suspect: each verb first chooses one target,
then that selected target's weight becomes the verb's offer in the entity-level
lottery. That means verb selection can be overly sensitive to sampled target
luck. Any redesign should start from candidate generation and per-verb target
normalization, not from another additive tweak to the current weight formula.

No implementation decision has been made. The next pass should simplify the
selection model while preserving map dynamism, movement, influence, relationship
maintenance, and affordance pressure.

Definition compilation creates broader affordance coverage than telemetry is
showing. Generator and gambit affordances are both compiled, and the compiler
has paths for presence, relationship target, relationship strength, influence,
and terrain affordance shapes.

Observed in the long run:

- Top-attributed affordance rows were generator-sourced only.
- Top-attributed shapes were `near_entity` only.
- No top-attributed gambit rows appeared.
- No top-attributed `relationship_strength`, `near_relationship_target`,
  `influence`, or `terrain` rows appeared.
- `what_the_spring_remembers` produced selected affordance pressure despite
  never having an eligible generator pulse.
- Second-wave kinds had low-altitude activity, but no selected-with-affordance
  rows.

Gap:

Telemetry records only the top affordance source/shape for each candidate.
Because generator affordances are registered before gambit affordances, equal
or lower gambit contributions can be hidden by generator presence pressure.
This means current MC output cannot distinguish "missing gambit/non-presence
pressure" from "present but not top-attributed pressure."

There is also a behavioral gap: definition-derived affordance pressure can
remain active for generators that are not currently reachable, as seen with
`what_the_spring_remembers`.

Implemented scope:

- Kept low-altitude scoring behavior unchanged.
- Kept existing top-attributed source kind, source id, and shape fields.
- Added trace fields for source kind mask, shape mask, total contribution
  count, generator contribution count, gambit contribution count, and hidden
  contribution count.
- Added MC affordance-utilization columns that expose generator/gambit and
  shape contribution counts before top-attribution collapse.
- Did not gate runtime affordance pressure on live generator/gambit eligibility.
- Did not change Ice Remembers generator or gambit definitions.

Observed in `.artifacts/monte-carlo/followups-10x2500-20260505`:

- Selected affordance rows: `5,188`.
- Selected contribution count: `10,203`.
- Selected hidden contribution count: `5,015`.
- Selected gambit contribution count: `2,606`.
- Rows with selected gambit contribution: `2,606`.
- Top-attributed selected rows remained entirely generator
  `PresenceNearEntity`.
- Selected relationship-strength contribution rows: `453`.
- Selected relationship-target contribution rows: `0`.
- Selected influence contribution rows: `0`.
- Selected terrain contribution rows: `0`.

Follow-up questions:

- Why do generator presence rows dominate top attribution even when hidden
  gambit and relationship-strength pressure exists?
- If unreachable generator pressure is still material, should source
  reachability scale affordance pressure somewhere above low-altitude runtime?
- Why are relationship-target, influence, and terrain affordance shapes absent
  from selected rows?
- Should second-wave kinds get explicit generator/gambit affordance coverage,
  or should their current zero selected-with-affordance rows remain expected
  until the second-wave expansion?

## 4. `what_the_spring_remembers` Is Effectively Unreachable

Status: closed. The generator now targets regions with incoming `cleansed_by`
and without incoming `memorializes`, instead of excluding incoming `anchored`.
`artifact_cleanses_scar` now depends on physical locality instead of `anchored`,
so the spring chain is driven by cleanse provenance rather than a generic
location relationship. `cleansed_by` remains durable provenance; the spring
creates `memorializes` as the one-per-region repeat guard.

Original finding:

`what_the_spring_remembers` targeted a `region` with incoming `cleansed_by`
and without incoming `anchored`. The only observed upstream path to
`cleansed_by`, `artifact_cleanses_scar`, requires the artifact to already have
`anchored` to the region before creating `cleansed_by`.

Observed in the long run:

- `what_the_spring_remembers`: `1000` blocked pulses, `0` candidates, `0`
  fires.
- `artifact_cleanses_scar`: `11` attempts, `1` success.
- The successful cleanse created `cleansed_by` at run `4008`, tick `776`.
- The same artifact-region pair already had `anchored` from tick `775`.
- `anchored` was not culled until tick `2389`.
- The region was historical by tick `2000`.

Original gap:

The target requirement `without incoming anchored` conflicts with the current
upstream chain that creates `cleansed_by`. In practice there was no useful
window where a region was active, had incoming `cleansed_by`, and lacked
incoming `anchored`.

Decision taken:

- Keep `anchored` as a generator-created/general location relationship for now,
  but do not make gambits depend on it.
- Change `artifact_cleanses_scar` to require physical locality instead of
  `anchored`.
- Let `what_the_spring_remembers` accept the anchored cleansed region while
  preserving `cleansed_by`.
- Use `memorializes` from the generated relic to the region as the repeat guard.

Verification:

- A `1x1000` MC smoke run with seed `4008` using the `memorializes` guard
  produced `what_the_spring_remembers` candidates and `3` fires.
- At tick `1000`, both `memorializes` and `cleansed_by` remained live in the
  relationship snapshots.
- `.artifacts/monte-carlo/followups-10x500-20260505` produced `6`
  `what_the_spring_remembers` fires.
- `.artifacts/monte-carlo/followups-10x2500-20260505` produced `40`
  `what_the_spring_remembers` fires.
- The generator still varies by run, but the original contradiction made it
  effectively unreachable; that specific bug is fixed.

## 5. Relationship Strength Maintenance Is Mistuned

Status: closed. Explicit low-altitude relationship-strength deltas were retuned
and a 10x500 MC showed strengths remained bounded and dynamic. Remaining
`trade_route` absence is not a relationship-strength maintenance failure; it is
tracked separately in item 9.

Relationship maintenance dominates relationship-strength traffic and tends to
overpower explicit low-altitude relationship-strength verbs.

Observed in the long run:

- Relationship-source maintenance generated very large numbers of strength
  changes.
- Explicit gameplay relationship-strength verbs did fire, but many average
  absolute deltas were around `0.002`.
- `settlement_trade` added net `+15.325` to `trade_route`.
- Generator spend removed net `-22.2` from `trade_route`.
- Total `trade_route` strength movement ended net negative despite ordinary
  trade maintenance.
- Positive relationship maintenance often decayed faster than explicit repair
  verbs could rebuild.

Gap:

Runtime strength flows are not balanced in practice. The definition invariant
checks source/sink existence, but MC output shows maintenance magnitude and
generator spends can swamp ordinary repair verbs.

Follow-up questions:

- Should passive maintenance decay be reduced, made less frequent, or made more
  conditional?
- Should explicit relationship-strength verbs apply larger deltas?
- Should generator spends such as `routes_draw_readers` spend less, require
  higher surplus, or create stronger downstream recovery paths?
- Should topology/provenance relationships stay out of numeric maintenance
  unless a system explicitly needs their strength?

Resolution:

- Retuned explicit low-altitude relationship-strength verb deltas in
  `IceRemembersLowAltitudeVerbs.cs` so ordinary committed relationship verbs
  can make visible movement against passive maintenance.
- Verified with
  `.artifacts/monte-carlo/relationship-strength-10x500`.
- Relationship event strengths stayed bounded: min `0.0`, max `1.0`.
- Relationship snapshot strengths stayed bounded: min `0.050000086`, max
  `1.0`.
- Gameplay-source relationship-strength absolute movement increased from the
  previous 10x500 comparison baseline `9.626` to `16.453`.
- Maintenance remains a passive background force, but this item's diagnosed
  balance gap is closed unless a longer MC run reopens it with new evidence.

## 6. Rename Historical Seed Entity File

Status: closed.

The seed builder extension file name `IceRemembersSeedBuilder.NewKinds.cs`
named the file after when content was added instead of what the code owns.
Historical names such as "new kinds" or "second wave" should not be used for
current code structure; git records history.

Resolution:

- Renamed the file to
  `src/IceRemembers.Seed/IceRemembersSeedBuilder.Entities.cs`.
- Moved all startup seed entity creation into that file, including region,
  settlement, faction, penguin, artifact, orca, point-of-interest, ability,
  transport, and occurrence creation plus initial entity relationships.
- Left terrain generation, subdivisions, semantic terrain generation, and
  generic placement helpers in their existing files.

Decision for now:

- Do not make gambits depend on `anchored`.
- Generators may continue to create `anchored`.

## 7. Increase Generator Frequency

Status: closed.

The current runs still show too little high-altitude generator activity relative
to the amount of low-altitude world churn. Some lifecycle symptoms are likely a
side effect of generators and gambits not firing often enough to replenish
meaningful named entities and relationship bundles.

Gap:

Generator cadence appears too conservative for the current simulation scale.
This makes downstream analysis noisy because missing generator output can look
like lifecycle collapse, affordance failure, or content absence when the primary
issue may simply be insufficient generator opportunity.

Follow-up questions:

- Should generator pulse cadence increase globally, or should specific
  generators get higher chance/priority?
- Should generator eligibility be measured against world population/relationship
  pressure so sparse runs get more high-altitude intervention?
- Should MC reporting include a headline for generator opportunity versus actual
  fires by generator, not only raw fire counts?

Resolution:

- Replaced exact-tick generator cadence with a pulse-window budget. Generators
  can now fire on any tick inside the pulse window while the pulse has remaining
  fire budget.
- Defaults are `PulseWidthTicks = 25` and `FiresPerPulse = 1`.
- The scheduler still runs generators every tick; this did not increase the
  global program pulse frequency.
- Verified with
  `.artifacts/monte-carlo/10x1000-generator-frequency`.
- The new 10x1000 average was `14.2` fires per run.
- The first 500 ticks averaged `7.1` fires per run, compared to the prior
  comparable 10x500 average of `5.6`.
- `140/142` new fires happened off the old `tick % 25 == 0` boundary, so the
  timing relaxation is working.
- The impact was positive but modest. Early-game instability remains and is
  tracked separately in item 10.

## 8. Unify Entity Placement Terrain Scoring

Status: needs MC.

Current finding:

Generator-produced entity placement does not have terrain scoring. Physical
placement chooses `on`, `near`, `across`, or `in coverage locality` by occupancy
and radius. Semantic placement is similarly coordinate-local. Terrain filters
and scoring exist for produced coverage placement, not for general produced
entity placement.

The semantic map implementation added a seed-only semantic terrain preference
scorer for startup entity placement. That makes seed placement more natural, but
it is a divergent system rather than a shared entity-placement policy.

Implementation note:

Generator-produced entity placement now has plane-aware preferred and allowed
terrain filters. Physical and semantic placement both keep radius, occupancy,
semantic bounds, and locality coverage as hard constraints, then score eligible
cells by terrain travel cost for the produced kind. Authored allowed terrain is
a hard filter; authored preferred terrain biases the score. Startup seed
semantic placement remains its existing content-specific scorer until MC shows
whether that remaining divergence is causing a real placement gap.

Gap:

Entity placement needs one coherent placement model across startup seed
placement and generator-produced entities. The model should decide whether
terrain preference belongs to entity placement, coverage placement, or both. If
entity terrain preference is real, it should be plane-aware and reusable for
physical and semantic placement instead of living as a semantic seed-only helper.

Follow-up questions:

- Should entity placement have terrain preferences at all, or should only
  coverage placement score terrain?
- If entity placement should score terrain, should preferences come from
  kind/subtype/culture, generator definition, terrain kind profiles, or a small
  authored placement profile table?
- Should generated entities be allowed to move their physical and semantic
  placement anchors independently based on each plane's terrain?
- Should the current seed-only semantic placement scorer be replaced by the
  shared model once generator entity placement is fixed?

## 9. Investigate Trade Route Bootstrap

Status: closed. Trade-route bootstrap is no longer blocked. The route topology
still needs future tuning, but that is no longer the original "no routes enter
the world" failure.

Current finding:

The relationship-strength 10x500 run did not exercise `trade_route` at all.
This was not caused by relationship-strength maintenance or trade-route spend,
because no trade-route relationship ever entered the world state.

Observed in `.artifacts/monte-carlo/relationship-strength-10x500`:

- `relationship_events` for `trade_route`: `0`.
- `relationship_snapshots` for `trade_route`: `0`.
- `settlement_opens_trade_route`: `0` executed events, `0` successes.
- `routes_draw_readers`: `200` pulses, `0` eligible, `0` fires.
- `settlement_trade` and `transport_run_the_trade`: no utilization rows.

Gap:

`settlement_opens_trade_route` is the current bootstrap path for reciprocal
`trade_route`, but its candidate chain appears too hard to satisfy in practice.
The gambit requires nearby settlements on both physical and semantic planes,
sufficient settlement HP, and shared physical influence. Snapshot
reconstruction found `8` directed settlement pairs that passed physical,
semantic, and HP gates, but `0` of those also had shared physical influence at
the required threshold.

Follow-up questions:

- Are settlement physical placement and movement creating too little settlement
  influence overlap?
- Is `SharedInfluencePrecondition(Physical, 20, 20)` the right gate for opening
  a trade route, or should route opening depend on proximity plus lower-grade
  sustained contact?
- Are settlement low-altitude influence verbs strong and frequent enough to
  create route-opening opportunities?
- Should seed placement intentionally create a small number of natural
  trade-route opportunities, or should all routes emerge from movement and
  influence only?

Resolution:

- Removed hard physical/semantic distance and shared physical influence gates
  from `settlement_opens_trade_route`.
- Replaced them with nonlinear physical-distance scoring and a same-culture
  score bonus, leaving route caps, duplicate-route guards, and HP floors as
  hard gates.
- `.artifacts/monte-carlo/trade-route-weighted-10x500-20260505` produced
  `299` `settlement_opens_trade_route` attempts, `61` successes, and `129`
  `routes_draw_readers` fires.
- The bootstrap gap is closed. The run also showed the new scoring permits too
  many long-distance routes, so route-network topology should be handled as a
  separate tuning/design follow-up rather than keeping this item open.

## 10. Early Game Generator Stability

Status: open.

The pulse-window change makes generator timing less brittle, but early generator
activity is still unstable. The first 10 pulse windows should nearly always
produce generator fires so the high-altitude layer starts contributing named
entities and relationship bundles before low-altitude churn dominates the run.

Current finding:

- With the default `25`-tick pulse width, the first 10 pulses are ticks
  `1..250`.
- In `.artifacts/monte-carlo/10x1000-generator-frequency`, fired pulse counts
  across the first 10 pulses were `0, 1, 0, 4, 4, 4, 5, 1, 4, 0`.
- Average fired pulses in the first 10 pulse windows: `2.3/10`.
- `3/10` runs had no generator fire at all in the first 10 pulse windows.
- In `.artifacts/monte-carlo/followups-10x500-20260505`, first-250-tick
  generator fires by run were `0, 1, 0, 4, 4, 4, 5, 1, 4, 0`.
- In `.artifacts/monte-carlo/followups-10x2500-20260505`, the first-250-tick
  numbers were the same: `0, 1, 0, 4, 4, 4, 5, 1, 4, 0`.
- `3/10` runs still had no generator fire in the first 10 pulse windows.
- Long-run total fires were highly uneven: `4, 91, 3, 14, 50, 6, 90, 70, 94,
  3`.

Gap:

The runtime pulse model now lets a generator fire whenever it becomes valid
inside the window, but the authored generator catalog still does not provide
enough early, reliable, low-impact opportunities. Existing generators are often
major world developments with relationship, locality, or terrain requirements
that can remain absent for many early pulses.

Likely direction:

- Add more early-game generators rather than further relaxing the pulse runtime.
- Prefer minor/bootstrap generators that can almost always find a valid target
  and produce small but useful named entities, relationships, terrain changes,
  or provenance hooks.
- Keep these generators conceptually smaller than the major event generators so
  early stability does not flood the world with high-impact entities.
- Use MC to judge first-10-pulse fire coverage directly, not only aggregate
  generator fires over 500 or 1000 ticks.

Follow-up questions:

- What early minor generator categories fit Ice Remembers without feeling like
  filler?
- Should early generators target broad seed kinds such as regions, settlements,
  factions, scars, or artifacts?
- Should early generators have `MaxRunsPerSim` or causal spend guards so they
  stabilize startup without dominating mid-run behavior?
- Should analyzer output include first-10-pulse generator coverage as a headline
  metric?

## 11. Revisit Low-Altitude Verb Mix

Status: open.

Current finding:

The influence-frontier mechanism is directionally correct: existing influence
cells can splash influence into neighboring cells, creating a local frontier.
The issue is the authored verb mix. Settlements and other entities have enough
influence verbs that influence work can crowd out movement, relationship, and
other low-altitude behaviors that are supposed to make higher-altitude
generators and gambits more likely.

Gap:

Low-altitude influence verbs should remain as a way to create territorial
pressure, but the catalog should not contain multiple near-duplicate influence
verbs per kind unless they serve distinct gameplay purposes. Revisit the verb
set, weights, commitment timing, and affordance pressure so influence behavior
does not drown out route-building, relationship maintenance, and other
actionable setup verbs.

Follow-up questions:

- Which influence verbs are actually distinct in simulation outcomes?
- Which influence verbs can be removed, merged, or made much rarer?
- Should influence verbs be limited by phase, current influence saturation, or
  explicit high-altitude demand?
- Does reducing influence-verb pressure make settlement route bootstrap and
  other gambits more observable without weakening territorial behavior too far?
