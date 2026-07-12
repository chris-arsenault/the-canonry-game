# Simulation Principles

This document records durable simulation-quality goals. Use it to decide
whether a proposed system improves the simulation rather than merely making a
metric move.

## Quality Goals

The simulation is healthy when three broad outcomes hold together.

1. End-state entity diversity is high. The world should contain a meaningful
   mix of entity kinds, subtypes, statuses, and relationship patterns. The graph
   should not collapse into one repeated agency-heavy pattern.
2. The map is well used. Activity should expand away from the seed zone, avoid
   single-corridor degeneracy, and produce readable territorial shape.
3. The map looks alive per tick. Entities should move, shape culture
   influence, age, die, and change relationship strengths even when no
   generator fires.

These goals are coupled. A map can look busy while the graph is poor; a graph
can be diverse while every entity sits in one cluster. Tuning should measure all
three before declaring success.

## Game Horizon: Bootstrap Into Steady State

The mental model for play is SimCity: relaxed, guided evolution over a long
session. Current Monte Carlo evidence runs (~25k ticks) exercise only the
first phase of that game — the bootstrap from seed into a steady state. A
real play session extends this horizon 4x-10x-50x depending on player
engagement, so:

- Low absolute map coverage in evidence runs is expected and is not a defect.
  Judge territorial *shape* and *trend*, not end-state fill.
- Steady-state health matters more than end-state totals: a mechanism that is
  stable and self-sustaining at 25k ticks is presumed extensible; a mechanism
  that only looks balanced because the run ended is not.
- When a tuning decision would differ between "bootstrap phase" and "long
  steady state" (e.g. caps, decay equilibria, cadence intervals, carrying
  capacity), name the horizon assumption explicitly in the finding and prefer
  the choice that stays healthy under extension.
- Longer confirmation runs (100k+) are a legitimate ask when a mechanism's
  failure mode is slow accumulation or slow starvation.

## Altitude Boundaries

Low altitude is the per-tick substrate. It may move entities, change health,
read traits, write map activity and culture influence, run minor labor, and
drift existing relationship strengths. It should not create named entities,
introduce relationship kinds, or declare major narrative outcomes.

Gambits are entity-authored high-stakes actions. They are rarer than low
altitude verbs and can create, strengthen, weaken, or reverse relationships.
They normally do not create new entities.

Generators are intermittent high-altitude events. They create named entities
and new relationship structure. Generator firing should feel earned by the
state produced by low altitude and gambits.

## Low-Altitude Design Rules

Movement volume is not a failure by itself. The question is whether movement
produces useful roaming, activity spread, and generator/gambit affordances.

Locality is an implementation ingredient, not a primary success criterion.
Do not optimize for "more local actions" unless doing so improves map use,
entity roaming, culture influence shape, relationship dynamics, or lifecycle
churn.

Resources are not primary low-altitude drivers today. The former macro
aggregate layer has been removed rather than retained as telemetry. Current
low-altitude quality should be judged through movement, culture influence, map
signals, minor actor labor, health effects, upkeep, relationships, gambits, and
generators.

Health provides lifecycle state. Traits provide mostly static individual
variance. Neither should force a recovery treadmill where entities spend the
simulation maintaining stocks instead of producing visible map life.

## Map Metrics

Use cumulative activity, not final occupancy, to evaluate map utilization.
Useful physical and semantic metrics include:

- distinct activity cells or semantic points touched;
- normalized activity entropy;
- top-cell or top-point share;
- axial anisotropy across q/r/s spans;
- largest strict empty hex radius;
- bounding-box fill.

For culture influence, watch component quality per culture and plane:

- component count above a threshold;
- largest-component share;
- isolated influenced-cell count;
- contested-cell share;
- hole count;
- mean and p95 distance from culture anchors or long-lived source entities.

Thin tendrils are acceptable when one dominant component remains. Many detached
islands are a tuning problem.

## Entity Metrics

Judge roaming per entity and per plane:

- move count;
- total distance;
- unique cells or points visited;
- max and p95 radius from first observed position;
- path efficiency;
- zero-roam share;
- over-clustered roam share.

High movement with low unique destinations is not meaningful map life. Low
movement by static entities can still be healthy if they visibly affect the map
through culture influence, minor actor labor, or coverage.

## Verb And Gambit Metrics

Low-altitude verbs should have varied utilization across kinds without forcing
perfect equality. Dominant verbs are acceptable if they do not starve the rest
of a kind's behavior surface.

Track:

- execution count and share by kind;
- entropy by kind;
- active verb count;
- dead verb count;
- same-action same-target pileups;
- target concentration;
- relationship-strength deltas by relationship kind and polarity.
- routine versus opportunity selection bucket share;
- affordance score and bonus on selected low-altitude intents.

Gambits should be judged separately from low-altitude verbs. They are rarer and
should be evaluated by relationship impact, target diversity, and whether they
create useful preconditions for generators.

## Analysis Discipline

Exploratory simulation analysis should use DuckDB, SQL, or Python against the
Monte Carlo parquet output unless the task is specifically kernel behavior.
C# is appropriate for tests and implementation, not as the default ad hoc
analysis language.

Do not lead tuning analysis with generator fire counts unless the current task
is generator behavior. Generator counts are a downstream symptom. For
low-altitude tuning, lead with map utilization, roaming variance, culture
shape, minor actor labor, lifecycle churn, and relationship strength dynamics.

Road behavior should be judged from the Monte Carlo parquet contract:

- `cell_road_events` is the raw plane-aware source for road quality and travel
  count changes.
- `road_creation` answers whether roads are being created and where quality
  lands by plane.
- `road_usage` answers whether moves arrive on cells that were already roads
  before the move tick.
- `road_reinforcement` answers whether roaded cells receive later movement and
  repeated quality growth.

Do not infer road prioritization from those tables. Prioritization needs
candidate path/cost choice telemetry that compares selected and rejected
movement options.

Do not loosen tests to make a suite green. If a test encodes a design gap or
architecture invariant, fix the implementation or revise the design explicitly.
