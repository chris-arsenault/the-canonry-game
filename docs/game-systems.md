# Game Systems

The Canonry Game is a deterministic simulation about named entities, their
relationships, and the map they inhabit. Systems are grouped by what they own:
runtime state, low-altitude physics, high-altitude events, player-facing cards,
and observability.

## Core Runtime State

The runtime world is a graph of named entities. Each entity has a kind, subtype,
culture, status, health, traits, physical coordinates, semantic coordinates,
and relationships. Relationships are typed, directional or symmetric, polar,
and strength-bearing.

The map has physical and semantic planes. The physical plane is a hex grid with
terrain, named coverage, and an integer z axis in `[-3, +3]` whose non-surface
cells are forged only by entity actions (Aurora spire tiers up, Nightshelf
tunnel galleries down). Regions and settlements are not a separate
abstraction from map coverage: they are named coverage over physical space. The
semantic plane is an analogous bounded axial q/r map with `s = -q-r`, its own
semantic terrain, culture influence, roads, and placement. It is not a
coordinate copy of the physical plane.

Domain content is authored in `DomainConfig` and Ice Remembers-specific files.
Reusable gameplay vocabulary lives in `Engine.Gameplay` and compiles down into
primitive kernel program data. The kernel executes only ids, numeric codes, and
fixed-capacity arrays.

## Runtime Altitudes

Low altitude is always-on per-tick physics. It keeps the world visibly alive,
ages entities, drifts existing relationship strength, and writes map signals
that later systems can read. It does not create named entities or declare major
story outcomes.

Minor actors are the low-salience labor layer. They are unnamed cohorts stored
inside a kernel program, not graph entities. They act more frequently than the
main graph cadence and do bounded local work for their source entity and
culture.

Gambits are high-stakes entity actions. A gambit has an actor, target, intrinsic
success criteria, and effects such as creating or changing relationships. A
gambit may use shared behavior bytecode, shared query predicates, and shared
effect execution, but it is not the removed low-altitude process system or the
removed generic planning program.

Generators are intermittent high-altitude events. They create named entities
and relationship bundles when their preconditions are met. Generator selection
should be shaped by the state created by low altitude and gambits.

The player-facing card/tableau layer controls which generators and gambits are
active. Cards determine what kinds of worlds can emerge; they should not be
mistaken for the low-altitude physics itself. Two one-shot intervention cards
(consecrate a hex for its dominant culture, favor an entity's influence
production) route through kernel commands under a dedicated `player` source
code, so player actions stay attributable in events and telemetry.

See [simulation-principles.md](./systems/simulation-principles.md) for the
quality goals used to judge whether these layers are working together.

## Low-Altitude Programs

The low-altitude rewrite replaced the old process deck with first-class kernel
programs:

- `LowAltitudeVerbs`: multi-tick movement, semantic movement, relationship
  strength mutation, culture influence growth/reduction, coverage expansion,
  anchor relocation, and work-tier growth.
- `Movement`: route planning, movement execution, road quality, and travel
  structures.
- `MapActivity`: physical and semantic presence heat, visit counts, and
  occupancy.
- `MapSignals`: terrain, entity, relationship, activity, crowding, support,
  threat, and attractor fields.
- `CultureBoundaries`: derived culture components, interiors, borders,
  contested cells, isolated pockets, holes, and strength bands.
- `CultureFieldDecay`: slow global decay of raw culture mass so territory
  reflects current production against loss instead of accumulated history.
- `Relationships`: existing relationship strength drift and culling.
- `Effects`: health modifiers derived from relationship,
  culture influence, distance, support, threat, and crowding evidence.
- `Upkeep`: lifecycle drain and historical-status transition on death.
- `MinorActors`: unnamed worker cohorts that maintain culture, roads, and
  source lifecycle without adding graph nodes.

The current default tick order is:

```text
Movement
LowAltitudeVerbs
MapActivity
MapSignals
CultureFieldDecay
CultureBoundaries
Relationships
Effects
Upkeep
Gambits
Generators
CultureSlots
MinorActors
```

`LowAltitudeVerbs` intentionally reads previous-tick map activity and map
signals so same-tick ordering does not create feedback loops. `MapSignals`,
`CultureBoundaries`, `Relationships`, and `Effects` read same-tick state after
movement and activity have been materialized.

By default, minor actors run every simulation tick. The main graph programs run
every `MinorActorMainTickRatio` ticks, currently `5`; generator pulse widths
and main-cadence ages are scaled by the same ratio.

## High-Altitude Systems

Gambits and generators are the primary graph-shaping systems.

Gambits represent entities doing meaningful things: wielding an artifact,
declaring war, betraying an ally, making a ward, or attempting a dangerous
ritual. They normally change relationship topology or polarity rather than
creating new entities.

Generators represent major world events where new named entities come into
being. "Created" means named by the simulation, not necessarily born from
nothing. Generators can also create relationships and apply secondary effects.

Both systems are data-defined through the domain and compiled into primitive
kernel program data. Their gates and mutations use shared kernel query and
effect primitives; pulse budgeting, target selection, scoring, and production
order remain caller policy.

## Observability

The kernel emits primitive events into a bounded event log. Projections,
run logs, UI state, Monte Carlo telemetry, and parquet exports are consumers of
that event stream. The kernel is not allowed to call UI, file I/O, console I/O,
or string-heavy observability directly.

Culture power is measured on three asymmetric ledgers (Witness, Shadow,
Wake) computed host-side from world state, exported as
`culture_power_snapshots`, and shown in the nations rows; see
[culture.md](./systems/culture.md).

Monte Carlo analysis should be performed against parquet output with SQL,
DuckDB, or Python. The stable simulation-quality metrics are described in
[simulation-principles.md](./systems/simulation-principles.md).

## Presentation

The Godot UI presents projection/read-model state. It must not compute
simulation truth, mutate runtime state, or redraw every panel just because a
kernel tick advanced. Current UI presentation principles are recorded in
[ui-principles.md](./design/ui-principles.md).

## System Index

- [01 Entity Graph](./systems/01-entity-graph.md)
- [02 Entity Kinds](./systems/02-entity-kinds.md)
- [03 Health And Traits](./systems/03-attributes.md)
- [04 Map Spatial](./systems/04-map-spatial.md)
- [05 Tick Clock](./systems/05-tick-clock.md)
- [06 Entity Behaviors](./systems/06-entity-behaviors.md)
- [07 Relationships](./systems/07-relationships.md)
- [08 Removed Resources / Economy](./systems/08-removed-resources-economy.md)
- [09 Lifecycle](./systems/09-lifecycle.md)
- [10 Map Visualization](./systems/10-map-visualization.md)
- [11 Removed Macro Aggregate](./systems/11-removed-macro-aggregate.md)
- [12 Cards And Tableau](./systems/12-deck.md)
- [13 Tableau Mana](./systems/13-tableau-mana.md)
- [15 Domain Definition Graph](./systems/15-domain-definition-graph.md)
- [16 Generator Executor](./systems/16-generator-executor.md)
- [17 System Executor](./systems/17-system-executor.md)
- [18 Action Executor](./systems/18-action-executor.md)
- [19 Narrative Domain](./systems/19-narrative-domain.md)
- [20 Generator Causal Limits](./systems/20-generator-causal-limits.md)
- [21 Minor Actors](./systems/21-minor-actors.md)
- [UI Principles](./design/ui-principles.md)
