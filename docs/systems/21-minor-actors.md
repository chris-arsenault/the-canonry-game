# System 21: Minor Actors

Minor actors are low-salience worker cohorts. They create background cultural
labor without adding more named entities to the graph.

## Contract

Minor actors are:

- unnamed and short-lived;
- stored inside the `MinorActors` kernel program;
- emitted by active named source entities;
- culture-bearing through their source entity;
- limited to bounded local labor verbs;
- visible through telemetry and map effects, not through the entity browser.

Minor actors are not:

- graph entities;
- relationship endpoints;
- culture slot occupants;
- generator or gambit targets;
- chronicle event sources;
- a hidden population/economy simulation.

## Cadence and Storage

The simulation advances one world pulse at a time. `MinorActors` alone runs
`MinorActorSubstepsPerPulse`, currently `5`, inside that pulse, so workers can
provide frequent low-impact labor without repeating generator, gambit,
relationship, or entity-resolution work.

Emitter and action cadence are authored per source rule. Current Ice Remembers
workers act every minor-actor pulse while alive; source emission cadence,
worker TTL, work delta, and active-worker cap are configured by the domain.

Workers remain individual. Their fields live in direct slot arrays, active
slots are dense and reusable, and a deterministic due heap visits only workers
whose next action or expiration is due. Per-source counts, emission deadlines,
and work tiers use direct source/rule slots rather than hash tables.

## Source Rules

Ice Remembers registers culture workers for every current entity kind on the
plane that is not the entity's primary mobility plane. A physical-primary
penguin can therefore support semantic culture labor, while a semantic-primary
region can support physical culture labor.

Transports also emit road crews on the physical plane. Road crews maintain road
quality and can spread culture along used routes.

Slots, work tier, and nexus standing modify source productivity:

- a culture slot occupant gets a higher effective active-worker limit and
  faster emission cadence;
- each work tier adds worker capacity and expands operating radius;
- slot benefits increase worker effect through the source slot multiplier;
- a source standing in an own-culture nexus cell on the labor plane gets a
  1.5x work delta multiplier, making established centers self-reinforcing.

## Verb Layer

Minor actors use a constrained verb layer, not free graph behavior. Workers
choose among local candidate cells within one step of their current position
and inside the source operating radius. Applying a verb can move the worker to
that selected local cell.

Current targeting modes:

- `ReinforceHomeMass`: strengthen existing or nearly enclosed own-culture mass.
- `ExtendConnectedBorder`: add culture at cells attached to a supported
  component edge.
- `BridgeGap`: connect nearby supported components or repair holes.
- `ReduceOtherCulture`: remove raw mass for all non-own cultures at the cell.
- `MaintainRoad`: reinforce road quality on cells with travel memory or culture
  support.
- `MaintainSourceLifecycle`: restore part of the source entity's age-related
  health drain when local culture support exists.

The selection is weighted random over positive-scoring candidates. Scores use
kernel culture-boundary fields such as raw mass, connected mass, supported
adjacency, component attachment, foreign pressure, and headroom.

## Telemetry

Minor actor telemetry should explain culture growth without promoting workers
to narrative entities. The event records source entity, source rule, work tier,
actor kind, purpose, verb/action/targeting code, plane, target cell, boundary
cell kind, local and connected raw mass, score, status, and applied delta.

Monte Carlo analysis should use this telemetry to answer whether culture grew
through worker action near existing masses, roads, borders, holes, and source
entities.
