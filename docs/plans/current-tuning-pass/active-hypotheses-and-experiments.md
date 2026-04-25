# Active Hypotheses And Experiments

Working document for the current tuning pass. This is not a design manifesto. It is a live list of hypotheses, experiments, and stop conditions so the work stays methodical.

## Scope

Current priority order:

1. process deck and planner physics
2. sustainment / death pressure
3. frontier expansion as a byproduct of the above
4. generator tuning after the low-altitude systems behave sanely

Primary baseline used so far:

- `.artifacts/monte-carlo/baseline-long-batch-0`
- 200 runs, 1000 ticks

## Current Evidence Snapshot

The current working interpretation is not that movement itself is the problem.

Current data-backed issues:

- non-movement process use is badly collapsed even after factoring movement out
- the world is physically sparse and does not grow organically
- ambient resources appear inert
- individual entity stocks appear too binary instead of fluctuating over time

Grounding numbers from the latest locality batch:

- movement-heavy executed share still dominates, but this is not itself a failure condition
- dead processes offered zero times: 12
- average physical occupied share: `1.7%`
- average physical anisotropy: `9.60`
- total generator fires improved, but frontier spread did not materially improve
- locality-planning produced only thin bundles; this is useful evidence, but locality is no longer the main framing for the next move

Important data caveats:

- settlement `residents` band reporting is not reliable enough yet for fine-grained tuning because its current stock range is very wide and the analyzer output is misleading there
- the locality experiment provided evidence, but should not dominate the next hypothesis ladder

## Experiment Ladder

### H1. The main deck problem is poor comparative use of non-movement processes

Why this is active:

- movement can remain common and still be healthy
- the bad signal is that once movement is factored out, the remaining process deck is still highly collapsed
- many verbs are never offered, and the live non-movement verbs still monopolize by kind

Experiment:

- inspect utilities, mutation magnitudes, and precondition/cooldown interactions with movement removed as the primary diagnosis
- measure process concentration within the non-movement slice
- identify whether dead verbs are structurally unavailable or just numerically losing

What to measure:

- non-movement process concentration per kind
- zero-offered and near-zero-selected verbs
- attribute delta mix by process source
- comparative score distributions among same-kind non-movement verbs

Success looks like:

- the non-movement deck becomes materially more diverse even if movement remains common

Status:

- active

### H2. Individual stock dynamics are too binary

Why this is active:

- the sim should have fluctuating entity-level stocks, not mostly pinned high or low values
- current runs suggest many attributes behave more like latched states than living stocks

Experiment:

- inspect per-entity time series rather than only aggregate bands
- test whether utility curves, mutation sizes, decay, and cooldowns are creating overshoot or permanent collapse
- evaluate whether some stock ranges are simply too wide to show meaningful dynamics

What to measure:

- per-entity variance over time
- reversal frequency
- time spent in middle bands
- repeated bounce patterns versus permanent pinning

Success looks like:

- individual entities move between low, mid, and high over time instead of staying latched

Status:

- active

### H3. Multi-turn work may create healthier action economics than single-tick action selection

Why this is active:

- colony sims often tolerate many travel turns because the arrival produces several turns of work
- current sim actions are highly per-tick and may be creating too much planner churn once an entity reaches a worthwhile site

Experiment:

- model selected processes as short work commitments rather than single-tick impulses
- compare a few representative work types first, not the whole deck

What to measure:

- consecutive same-site productive turns
- reduction in planner churn
- non-movement process share
- local stock fluctuation near work sites
- whether repeated work simply deepens the existing core or actually produces organic growth

Success looks like:

- arrivals lead to visible multi-turn productive phases without removing movement from the sim

Status:

- active design branch

### H4. Spatial growth likely needs lightweight development heuristics, not just emergent free play

Why this is active:

- late-state target is closer to a Civ or SimCity map than to sparse wandering entities
- those games are emergent, but they also rely on hard procedural preferences
- current sim may need equivalent structure for placement and growth patterns

Experiment:

- explore lightweight development rules such as:
  - terrain/resource desirability
  - settlement spacing constraints
  - local growth adjacency preferences
  - infrastructure-like anchors or corridors later, if needed
- test these as placement/growth heuristics rather than player-authored zoning systems

What to measure:

- occupied map share
- anisotropy
- clustering shape
- frontier settlement survival
- whether late-state maps read as developed rather than merely populated

Success looks like:

- broader and more legible territorial development
- less corridor bias
- more late-state map structure

Status:

- active design branch

### H5. Ambient resources and pressures may be dead systems and should be ablated if necessary

Why this is active:

- current telemetry says global resources do nothing
- there is a real possibility that one or both subsystems are adding complexity without shaping behavior

Experiment:

- run ablations:
  - resources removed or disabled
  - pressures removed or disabled
  - both removed
- compare against the current baseline before deciding whether to rework or replace them

What to measure:

- process distribution
- stock dynamics
- deaths
- map spread
- generator firing
- any meaningful change at all

Decision rule:

- if ablation changes almost nothing, the subsystem is currently dead weight
- if ablation changes behavior materially, the subsystem matters and needs redesign rather than removal

Status:

- active diagnostic branch

### H6. Corridor bias is partly generator anchoring, but the low-altitude world is still failing to turn placements into organic spread

Why this is active:

- `faction_seals_the_edge` tends to create settlements near existing settlement clusters
- other settlement-producing generators are more frontier-opening
- low map occupancy suggests the world is not exploiting the outward placements it already gets

Current evidence:

- `faction_seals_the_edge` holdfasts average much closer to existing settlements than refuge, rookery, or reefhold outputs

Experiment:

- after H1, rerun the same Monte Carlo baseline
- compare settlement distance-to-nearest-existing-settlement by generator
- compare total occupied share and anisotropy

Decision rule:

- if corridor bias remains strong after planner/process fixes, revisit specific settlement-producing generators
- if corridor bias drops materially, keep generator changes secondary

Status:

- active, but downstream of H1

### H7. Linear aging creates too much early-life treadmill and not enough late-life collapse

Why this is active:

- current aging is linear from birth
- that creates constant lifetime drag instead of a clearer late-life death curve

Experiment:

- replace linear aging with a nonlinear curve
- keep early-life drain lighter
- steepen late-life drain

What to measure:

- death age distribution by kind and subtype
- active population by kind over time
- process mix before and after
- whether constant survival pressure drops without making durable kinds effectively immortal

Success looks like:

- less perpetual maintenance burden early
- clearer late-life death pressure

Status:

- active follow-on

### H8. Cooperation planning is still a later option, but smaller request-style coordination remains plausible

Why this is deferred:

- joint planning adds coordination failure, synchronization, and much worse observability
- it should not be used until simpler changes around process economics, stock dynamics, and growth heuristics are explored first

Experiment trigger:

- only revisit this after the simpler branches above are tested

Status:

- deferred

## Immediate Next Steps

1. Reframe Monte Carlo analysis around non-movement process diversity and entity-level stock fluctuation.
2. Design an ablation pass for resources and pressures.
3. Explore multi-turn work as a narrow experimental branch.
4. Explore lightweight development heuristics for placement and growth.

## Questions To Keep Honest

- If movement is ignored, is the rest of the process deck healthy?
- Are individual stocks actually fluctuating, or merely pinned high/low?
- Do resources and pressures matter enough to justify their complexity?
- Would multi-turn work create better local dynamics, or only deepen existing hubs?
- Which spatial growth heuristics would create legible late-state development without over-scripting the world?
