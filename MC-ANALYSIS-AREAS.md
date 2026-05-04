# Monte Carlo Analysis Areas

This is a handoff list for the next Monte Carlo analysis pass. It should stay
focused on what to investigate, not how to run the tooling.

## Current System Facts To Keep Straight

- Low-altitude verbs are ambient multi-tick intents. Each entity kind scans its
  authored verbs, builds candidates for eligible verb/target pairs, weights
  them, selects stochastically, then keeps the intent until it expires, dwells,
  reaches its target, or becomes invalid.
- Movement is not its own decision-maker. The movement program owns traversal,
  terrain costs, physical and semantic movement mutations, and road state. It
  executes movement requested by low-altitude verbs, gambits, relationship
  attachment behavior, and other callers.
- Gambits are opportunistic decisive actions. The old generic planning program
  and the separate reward expression lane have been removed. Gambits now
  evaluate eligible targets directly from provider selectors, preconditions,
  score expressions, cooldowns, success chance, and effects.
- Generator and gambit affordances are not plans. They are source-aware,
  shape-aware pressure rows compiled into low-altitude weighting so ordinary
  verb/target choices become more likely to set up future generator or gambit
  opportunities.
- Low-altitude verbs and gambits still do not share an activity lane or action
  budget. They can both mutate or move the same entity on the same tick. This is
  a design gap to measure, not behavior the affordance rewrite solved.
- Relationships can drift passively through relationship maintenance, change
  through explicit low-altitude relationship-strength verbs, and change through
  gambit/generator effects. Relationship topology is still created primarily by
  generators and gambits, not ordinary low-altitude maintenance.
- `holds` has special attachment behavior: its target follows the source
  physically. That is correct behavior for held or possessed things, but it
  should be analyzed as attachment or possession resolution, not as independent
  agency by the held entity.

## Roads And Movement

- Are roads forming naturally from repeated travel?
- Do created roads keep attracting later movement?
- Do roads become useful infrastructure, inert decoration, or runaway corridors?
- Does road growth differ between physical and semantic planes?
- Does road usage correlate with broader roaming and map activity?
- Is movement from low-altitude verbs, gambits, and `holds` follow behavior
  distinguishable by movement cause?
- Are same-entity, same-tick movements happening from multiple sources?
- When multiple movement sources happen in a tick, do they cancel, override, or
  create coherent travel?
- Do dwell phases create local life around targets, or just repeated no-op
  ticks?
- Do entities still explore enough after affordance weighting, or do they
  collapse into generator/gambit setup loops?

Do not treat road usage as proof of road prioritization. Prioritization needs
choice telemetry that compares selected and rejected movement options.

## Terrain Meaning

- Does terrain materially change movement, influence, claim, and placement
  outcomes?
- Which terrains are overused, ignored, or acting as blockers?
- Do terrain costs create interesting spatial patterns, or do entities behave
  as if terrain is cosmetic?
- Are terrain effects legible across both physical and semantic map behavior?

## Semantic Plane Behavior

- Are entities actually moving in semantic space?
- Does semantic movement produce different patterns from physical movement?
- Does semantic influence spread, cluster, or fragment differently from physical
  influence?
- Do semantic terrain and semantic regions feel like meaningful structure, not
  just copied physical map concepts?

## Influence And Claims

- Are influence fields expanding, consolidating, and contesting in coherent
  ways?
- Are claims stable enough to matter without becoming static?
- Are influence and claim outcomes sensitive to terrain, roads, and region
  structure?
- Are semantic influence and physical influence telling different stories?

## Regions, Superregions, And Continents

- Is activity distributed across higher-order map regions, or collapsing into a
  few cells?
- Do regions create useful analysis boundaries for movement, influence, claims,
  and generation?
- Are superregions and continents currently meaningful, or just labels?

## Entity Roaming And Activity

- Which entity kinds move, cluster, stall, or roam too widely?
- Are entities traversing meaningful space, or bouncing inside tiny local loops?
- Does movement volume translate into spatial variance?
- Are physical and semantic roaming patterns diverging in useful ways?
- Which low-altitude verbs are offered, selected, and unable to find targets by
  kind, plane, operation, target mode, target kind, and verb?
- Are all target modes actually contributing: explore, entity kind,
  relationship target, terrain, own influence, and foreign influence?
- Are selection rates explainable from authored base weights plus exploration,
  staleness, heat, crowding, relationship affinity, and affordance pressure?
- Are relationship-target and terrain-target verbs starved compared with broad
  exploration verbs?
- Are second-batch entity kinds actually active in low altitude: orca, monster,
  ability, point of interest, transport, and occurrence?
- Are non-movement low-altitude operations producing repeated work during their
  commitment windows, or are they mostly selecting and then doing nothing?
- Does stochastic exploration remain broad enough to prevent a stale map?

## Generators And Map Mutation

- Do generator map-cell operations create areas that entities later use?
- Are generated terrain, regions, claims, and roads integrated into later
  behavior?
- Are generators creating coherent spatial opportunities or isolated artifacts?
- For each generator, are pulses, eligible pulses, blocked pulses, eligible but
  unselected pulses, fires, failures, target pools, and produced outputs in a
  tunable range?
- Do generator relationship gates have real upstream sources in normal play?
- Do generator relationship spends have visible downstream consequences?
- Does affordance pressure improve the conditions required by generators, or is
  it noise compared with ordinary roaming?
- Are generated entities and relationships later used by low-altitude verbs,
  gambits, maintenance, and additional generators?
- Current generator-specific targets:
  - `ice_settles_accounts`: war relationship exists, offered penguin exists,
    war relationship is removed, memorial/witness/exile outputs matter later.
  - `what_the_spring_remembers`: cleansed regions become anchored, witnesses,
    sanctuaries, and choirs rather than isolated outputs.
  - `war_breaks_the_land`: war produces scars and waste pressure without
    saturating the map.
  - `routes_draw_readers`: strong trade routes cross the threshold, are spent,
    and produce brokers, harbors, orders, and instruments that re-enter play.
  - `what_the_artifact_knows`: held artifacts and missing home bases produce
    rookeries, seals, and possible cells.

## Relationships, Gambits, And Spatial Effects

- Do spatial conditions create meaningful relationship and gambit opportunities?
- Do gambits alter movement, influence, claims, or territorial pressure?
- Are relationship changes spatially concentrated, diffuse, or disconnected from
  the map?
- Are relationship-strength changes materially large enough to matter over a
  Monte Carlo run?
- Are low-strength positive relationships preferentially selected for repair
  when affordances make that useful?
- Are negative relationship strengths being interpreted correctly: strengthening
  a rivalry, war, ward, or corruption means intensifying it; weakening means
  cooling, spending, or resolving it.
- Are physical and semantic distance gates blocking relationship maintenance
  too often, or creating useful locality?
- Do relationship maintenance rules and explicit relationship-strength verbs
  reinforce each other or fight each other?
- Are relationship events attributable through `action_id` to low-altitude
  verbs, gambits, generators, relationship maintenance, and effects?
- Relationship kinds whose strength should be checked as meaningful because
  they have maintenance, numeric gates, spends, or explicit mutation pressure:
  `controlled_by`, `allied_with`, `rival_of`, `led_by`, `member_of`,
  `memorializes`, `claimed_by`, `trade_route`, `anchored`, `conduit_of`,
  `ward_against`, `corrupted_by`, `cleansed_by`, `at_war_with`,
  `betrayed_by`, `practiced_by`, and `taught_by`.
- Relationship kinds whose strength is probably topology, provenance, or
  attachment unless a later system gives strength a job: `home_base`, `holds`,
  `created_by`, `founded_from`, `native_to`, and `part_of`.
- `holds` should be analyzed twice: as graph state for requirements, and as
  physical attachment through follow movement. Do not count follow movement as
  voluntary travel by the held entity.
- `home_base` should be watched specifically. It is meaningful topology and is
  used by generators and affinity, but strengthening or weakening the numeric
  strength is still not clearly meaningful.
- Current explicit relationship-strength low-altitude verbs to confirm in MC:
  `faction_tend_alliance`, `faction_fray_alliance`,
  `faction_inflame_rivalry`, `faction_cool_rivalry`,
  `settlement_trade`, `ability_reinforce_practice`,
  `ability_let_practice_fade`, `orca_reinforce_pod_tie`,
  `point_of_interest_loosen_claim`, `ability_charge_conduit`,
  `monster_deepen_corruption`, `transport_overstrain_conduit`, and
  `occurrence_exhaust_war`.
- `settlement_trade` / "Tend Trade Route" is the current ordinary strengthener
  for `trade_route`, the relationship we have been discussing as "trades with."
- Every relationship kind should have both a source and sink for strength, or
  neither, unless it is intentionally one-way because a generator/gambit spends
  a stored condition. The invariant test guards the definition side; MC should
  check whether runtime flows are actually balanced.

## Gambit Behavior

- Current gambits to analyze:
  - `penguin_wields_artifact`: nearby penguin picks up an unheld artifact,
    creates `holds`, and moves the artifact to the penguin.
  - `faction_declares_territorial_war`: faction with contested influence and
    enough cohesion creates reciprocal `at_war_with`.
  - `settlement_opens_trade_route`: nearby, semantically close, viable
    settlements create reciprocal `trade_route`.
  - `artifact_cleanses_scar`: intact anchored artifact near a region creates
    `cleansed_by` and changes the target terrain to hot springs.
  - `faction_sues_for_peace`: low-cohesion faction at war removes reciprocal
    war and creates `betrayed_by`.
- Are gambit preconditions too strict, too loose, or roughly right?
- Are gambit score expressions non-constant and actually changing attempt
  ordering?
- Physical proximity is an eligibility gate, not a score replacement. Do not
  read higher score as "closer" unless the score expression explicitly says so.
- Are gambits firing opportunistically from world state, or are they still
  effectively waiting on missing support behavior?
- Are cooldowns suppressing otherwise healthy opportunities?
- Do successful gambits create downstream generator and low-altitude activity?
- Do failed gambits still leave enough world activity, or do they create
  dead-end selection churn?

## Unified Affordances

- Are affordance rows compiled for both source kinds: generator and gambit?
- Are source ids and shape codes populated enough to attribute pressure to
  specific generators and gambits?
- Current affordance shapes to analyze: presence near entity, presence near
  relationship target, relationship strength change, influence change, and
  terrain presence.
- Are affordance-weighted candidates being offered?
- Are candidates with higher affordance score and bonus selected more often
  than comparable candidates without affordance pressure?
- Does selected affordance pressure lead to material downstream effects:
  relationship strength deltas, influence changes, terrain use, later gambit
  attempts, or later generator fires?
- Does relationship-strength affordance scoring actually prioritize useful
  deficits or surplus against thresholds, especially weak positive
  relationships that need maintenance?
- Does influence affordance scoring direct grow/consolidate/reduce verbs toward
  cells that matter for later gambits and generators?
- Does terrain/presence affordance pressure move entities into places where
  opportunities become possible, without eliminating ordinary exploration?
- Are any affordance source ids dominating world behavior?
- Is definition-derived pressure enough, or do active generator/gambit runtime
  states need to emit live affordance rows in a later refactor?
- Traits should not be the primary explanation in this pass. We deliberately
  dropped trait-driven "care about the world" from the affordance design for now.

## Activity Lane And Contradictions

- How often does the same entity perform both low-altitude work and gambit work
  on the same tick?
- How often does the same entity receive multiple movement mutations in a tick?
- Are there cases where low-altitude moves an entity one way and a gambit moves
  it elsewhere in the same tick?
- Are relationship-strength mutations and gambit topology changes happening to
  the same relationship in the same tick?
- Which effects should eventually consume a unified activity lane, and which
  should remain passive consequences: relationship maintenance, `holds` follow,
  lifecycle, generator effects, gambit effects, and ordinary low-altitude work?
- Until the unified activity lane exists, treat same-tick contradictions as
  analysis targets, not regression failures by themselves.

## Lifecycle And Stocks

- Do spatial constraints affect survival, attrition, or lifecycle transitions?
- Are stocks moving in response to terrain, influence, claims, or relationships?
- Are lifecycle outcomes varied enough to tune, or collapsing into one dominant
  path?
- Do relationship and influence changes materially affect health effects and
  lifecycle outcomes?
- Are second-batch entity kinds living long enough to participate in the
  systems they are supposed to exercise?

## Telemetry And Parquet Coverage

- Prefer parquet/raw telemetry exploration over bespoke post-hoc analyzer
  scripts. Add reusable analyzer tables only when they expose a stable repeated
  question.
- Raw trace events now carry affordance source kind, affordance source id,
  affordance shape, affordance score, and affordance bonus.
- `low_altitude_verb_utilization` includes selected affordance score/bonus and
  selected-with-affordance counts.
- `low_altitude_affordance_utilization` should be the first reusable table for
  checking whether generator and gambit affordances are offered and selected.
- Relationship events carry `action_id`; use that to attribute
  relationship-strength changes to the concrete low-altitude verb, gambit,
  generator, or passive system that caused them.
- Core tables/surfaces for this pass: raw trace events, raw relationship
  events, raw cell influence events, raw movement events, raw gambit events, raw
  generator events, generator balance, gambit success rate, low-altitude verb
  utilization, low-altitude affordance utilization, entity roaming, and roaming
  summary.
- Do not analyze removed compatibility surfaces: old planning traces, old
  gambit reward expressions, old support objectives, or old generator-only
  affordance rows.

## Degeneracy Watchlist

- roads never form
- roads form but nobody reuses them
- roads dominate all movement
- terrain has no measurable effect
- semantic movement is inactive
- semantic influence mirrors physical influence too closely
- influence fields fragment into isolated noise
- claims never change hands
- generators create unused map artifacts
- entities overcluster despite available space
- affordance pressure starves ordinary exploration
- affordance pressure is present but has no measurable downstream impact
- one affordance source dominates most low-altitude selection
- relationship-strength verbs are offered but rarely selected
- relationship-strength verbs are selected but deltas are too small to matter
- positive relationships decay faster than entities can repair them
- negative relationships never cool, or cool too quickly to drive events
- `trade_route` never crosses generator thresholds, or is immediately spent
  into irrelevance
- `home_base` and `holds` strengths drift or mutate without a clear gameplay
  reason
- gambits rarely find eligible targets
- gambits fire but do not affect later movement, influence, relationships, or
  generators
- generators depend on relationship strengths with no runtime source
- generators spend relationship strengths with no runtime recovery path
- same-tick low-altitude and gambit effects frequently contradict each other
- follow movement from `holds` is mistaken for independent exploration
- second-batch entity kinds exist but do not participate in movement,
  influence, relationships, gambits, or generators

## Available From This Session

- Road creation, road usage, and road reinforcement are now analysis areas.
- Road prioritization is explicitly not covered yet.
- Influence analysis is plane-aware.
- Movement and roaming analysis are plane-aware.
- The output contracts now include the new road analysis surfaces.
- Gambit planning was removed. Gambits are now opportunistic direct evaluators,
  with score as the only authored ranking expression.
- The old reward expression lane and old planning/support compatibility surfaces
  should not appear in analysis.
- Generator and gambit affordances now feed low-altitude selection through one
  unified compiled affordance model.
- Affordance telemetry is now present in raw trace output and reusable parquet
  analysis tables.
- Relationship events now include `action_id`, making material relationship
  strength impact directly attributable.
- Explicit relationship-strength verbs now cover alliance, rivalry, trade
  route, practiced-by, member-of, claimed-by, conduit-of, corrupted-by, and
  war-pressure cases, including second-batch entity kinds.
- `holds` follow behavior is intentionally preserved and should be analyzed as
  attachment, not agency.
