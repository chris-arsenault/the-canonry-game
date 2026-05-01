# Game Systems

The Canonry Game is a deterministic simulation about named entities, their
relationships, and the map they inhabit. Systems are grouped by what they own:
runtime state, low-altitude physics, high-altitude events, player-facing cards,
and observability.

## Core Runtime State

The runtime world is a graph of named entities. Each entity has a kind, subtype,
culture, status, attributes, physical coordinates, semantic coordinates, and
relationships. Relationships are typed, directional or symmetric, polar, and
strength-bearing.

The map is a physical hex grid with terrain and named coverage. Regions and
settlements are not a separate abstraction from map coverage: they are named
coverage over physical space. The semantic plane is currently an axial q/r
space with `s = -q-r`; it is treated as a bounded second plane, not a fully
independent 3D map.

Domain content is authored in `DomainConfig` and Ice Remembers-specific files.
Reusable gameplay vocabulary lives in `Engine.Gameplay` and compiles down into
primitive kernel program data. The kernel executes only ids, numeric codes, and
fixed-capacity arrays.

## Runtime Altitudes

Low altitude is always-on per-tick physics. It keeps the world visibly alive,
ages entities, drifts existing relationship strength, and writes map signals
that later systems can read. It does not create named entities or declare major
story outcomes.

Gambits are high-stakes entity actions. A gambit has an actor, target, intrinsic
success criteria, and effects such as creating or changing relationships. A
gambit may use the shared behavior bytecode and planner infrastructure, but it
is not the removed low-altitude process system.

Generators are intermittent high-altitude events. They create named entities
and relationship bundles when their preconditions are met. Generator selection
should be shaped by the state created by low altitude and gambits.

The long-term player card/tableau layer controls which generators, gambits, and
systems are active. Cards determine what kinds of worlds can emerge; they should
not be mistaken for the low-altitude physics itself.

See [simulation-principles.md](./systems/simulation-principles.md) for the
quality goals used to judge whether these layers are working together.

## Low-Altitude Programs

The low-altitude rewrite replaced the old process deck with first-class kernel
programs:

- `LowAltitudeVerbs`: multi-tick movement, semantic movement, influence growth,
  influence reduction, coverage expansion, and anchor relocation.
- `MapActivity`: physical and semantic presence heat, visit counts, and
  occupancy.
- `MapSignals`: terrain, entity, relationship, activity, crowding, support,
  threat, and attractor fields.
- `Relationships`: existing relationship strength drift and culling.
- `Effects`: health and attribute modifiers derived from relationship,
  influence, distance, support, threat, and crowding evidence.
- `Upkeep`: lifecycle drain and historical-status transition on death.

The current default tick order is:

```text
LowAltitudeVerbs
MapActivity
MapSignals
Relationships
Effects
Upkeep
Gambits
Pressures
Generators
Degeneracy
```

`LowAltitudeVerbs` intentionally reads previous-tick map activity and map
signals so same-tick ordering does not create feedback loops. `MapSignals`,
`Relationships`, and `Effects` read same-tick state after movement and activity
have been materialized.

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
kernel program data.

## Observability

The kernel emits primitive events into a bounded event log. Projections,
run logs, UI state, Monte Carlo telemetry, and parquet exports are consumers of
that event stream. The kernel is not allowed to call UI, file I/O, console I/O,
or string-heavy observability directly.

Monte Carlo analysis should be performed against parquet output with SQL,
DuckDB, or Python. The stable simulation-quality metrics are described in
[simulation-principles.md](./systems/simulation-principles.md).

## System Index

- [01 Entity Graph](./systems/01-entity-graph.md)
- [02 Entity Kinds](./systems/02-entity-kinds.md)
- [03 Attributes](./systems/03-attributes.md)
- [04 Map Spatial](./systems/04-map-spatial.md)
- [05 Tick Clock](./systems/05-tick-clock.md)
- [06 Entity Behaviors](./systems/06-entity-behaviors.md)
- [07 Relationships](./systems/07-relationships.md)
- [09 Lifecycle](./systems/09-lifecycle.md)
- [10 Map Visualization](./systems/10-map-visualization.md)
- [11 Pressure](./systems/11-pressure.md)
- [12 Deck](./systems/12-deck.md)
- [13 Tableau Mana](./systems/13-tableau-mana.md)
- [15 Domain Definition Graph](./systems/15-domain-definition-graph.md)
- [16 Generator Executor](./systems/16-generator-executor.md)
- [18 Action Executor](./systems/18-action-executor.md)
- [19 Narrative Domain](./systems/19-narrative-domain.md)
