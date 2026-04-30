# Active Hypotheses And Experiments

Working document for the current tuning pass. This is not a design manifesto. It is a live list of hypotheses, experiments, and stop conditions so the work stays methodical.

## Scope

Current priority order:

1. low-altitude movement / influence physics
2. sustainment / death pressure
3. frontier expansion as a byproduct of the above
4. generator tuning after the low-altitude systems behave sanely

Current decision:

- The old entity-process / GOAP verb experiments are superseded for now.
- The new low-altitude verb program is substantially better as the base map-life layer: mobile entities move with per-intent path/dwell/speed variation, and static physical entities affect the map through influence.
- Do not spend the next tuning pass trying to rescue the old process-deck movement verbs unless a later regression proves the new low-altitude layer is insufficient.
- Deferred generator note: when generator work resumes, locality targeting should be able to select cells/regions inside high influence, not only exact hex or coverage locality. This lets low-altitude settlement/faction influence become a generator affordance instead of a purely visual map layer.

Primary baseline used so far:

- `.artifacts/monte-carlo/baseline-long-batch-0`
- 200 runs, 1000 ticks

## Current Evidence Snapshot

The current working interpretation is not that movement itself is the problem.

Current data-backed issues:

- the world is physically sparse and does not grow organically
- individual entity stocks appear too binary instead of fluctuating over time, though attributes are intentionally less central during the low-altitude reset
- influence overlays look much more alive than the old entity verbs, but may form thin spider-leg shapes that need MC contiguity checks

Current resource stance:

- resource telemetry and parquet artifacts are retained for future work
- resources are not part of the active tuning analysis because they have been removed as a meaningful active system for now

Grounding numbers from the latest locality batch:

- movement-heavy executed share still dominates, but this is not itself a failure condition
- dead processes offered zero times: 12
- average physical occupied share: `1.7%`
- average physical anisotropy: `9.60`
- total generator fires improved, but frontier spread did not materially improve
- locality-planning produced only thin bundles; this is useful evidence, but locality is no longer the main framing for the next move

Important data caveats:

- settlement `temp_hp` band reporting is not reliable enough yet for fine-grained tuning because the analyzer output has previously conflated health with non-health attributes
- the locality experiment provided evidence, but should not dominate the next hypothesis ladder

## Experiment Ladder

### H1. Low-altitude movement / influence physics is the current baseline

Why this is active:

- the old entity-process verb model produced stiff, collapsed behavior
- the new low-altitude verbs are visually and mechanically stronger
- mobile entities now have divergent target offsets, dwell durations, path looseness, movement cadence, and dwell roaming
- settlements/factions now create map dynamism through non-exclusive influence rather than physical movement

Experiment:

- run the next MC baseline using the new low-altitude physics
- compare map utilization, entity roaming variance, and influence shape quality
- treat old GOAP/process movement tuning branches as abandoned unless this baseline regresses

What to measure:

- physical/semantic activity spread
- per-entity roam radius and unique visited cells
- influence contiguity by influencing entity
- influence island count and largest-component share
- settlement/faction coverage growth and HQ movement frequency

Success looks like:

- maps look alive per tick without collapsing into a single nexus
- influence is mostly one dominant component with acceptable tendrils, not many disconnected islands
- the new low-altitude layer remains clearly better than old entity-process verbs

Status:

- active baseline

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

### H3. Old multi-turn process work is superseded by low-altitude intent commitments

Why this is superseded:

- the original concern was that one-tick GOAP actions created planner churn and stiff behavior
- low-altitude verbs now already create multi-tick commitments with target pursuit, dwell, dwell roaming, and influence application
- revisiting old process-work commitments now risks tuning the wrong layer

Experiment:

- no active experiment
- only reopen if the new low-altitude layer fails to produce sufficient per-tick life

What to measure:

- none for the immediate ladder

Success looks like:

- old process-work experiments stay closed unless new data justifies reopening

Status:

- superseded

### H4. Spatial growth likely needs lightweight development heuristics, not just emergent free play

Why this is active:

- late-state target is closer to a Civ or SimCity map than to sparse wandering entities
- those games are emergent, but they also rely on hard procedural preferences
- current sim may need equivalent structure for placement and growth patterns

Experiment:

- explore lightweight development rules such as:
  - terrain desirability
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

### H5. Pressures may be a dead system and should be ablated if necessary

Why this is active:

- there is a real possibility that pressure is adding complexity without shaping behavior
- resources are excluded from the active analysis loop for now

Experiment:

- run a pressure-disabled ablation
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

### H8. Cooperation planning and request-style coordination are deferred

Why this is deferred:

- joint planning adds coordination failure, synchronization, and much worse observability
- the new low-altitude verb layer should be evaluated first
- request-style coordination should not be used to patch problems that movement/influence physics already solved

Experiment trigger:

- only revisit this after the simpler branches above are tested

Status:

- deferred

## Immediate Next Steps

1. Run a fresh MC baseline with the new low-altitude movement/influence physics.
2. Add influence-contiguity analysis to the MC reporting path.
3. Continue pressure ablation only after confirming the new low-altitude baseline is stable.
4. Keep old planner/process verb experiments closed unless the new baseline regresses.

## Questions To Keep Honest

- Are individual stocks actually fluctuating, or merely pinned high/low?
- Does pressure matter enough to justify its complexity?
- Is influence mostly contiguous, or is it producing many disconnected islands?
- Are settlement/faction influence verbs creating readable territory and contestation?
- Which spatial growth heuristics would create legible late-state development without over-scripting the world?
