# System Architecture

The engine is a deterministic microkernel hosted by the Godot game runtime.
The kernel owns mutation. UI, projections, run logs, telemetry, exports, and
Monte Carlo analysis consume the primitive event stream outside the sim thread.

The controlling kernel rules live in
[simulation-kernel-discipline.md](./kernel/simulation-kernel-discipline.md).

## Project Topology

```text
TheCanonryGame.App
  Godot scene/runtime entrypoint

TheCanonryGame.Game
  game runtime facade, UI read models, player intent port

Engine.Host
  domain compilation, startup seeding, kernel construction, projections,
  driver wiring, symbol catalogs

Engine.Domain
  domain schema, stable id registry, definition graph, semantic validation

Engine.Gameplay
  reusable gameplay vocabulary and validators; compiles authored gameplay
  into primitive kernel program data

Engine.Kernel
  deterministic state mutation, fixed-capacity program data/state/scratch,
  no domain strings, no allocation-heavy runtime policy

Engine.Eventing
  primitive event records, schema metadata, ring buffer, cursors

Engine.Projection
  consumer-side UI/read-model projections, including generic provenance indexes

Engine.Observability
  run log, telemetry projection, parquet export, Monte Carlo analysis inputs

Engine.WorldGen
  physical map generation, terrain, coverage, startup placement
```

`Engine.Kernel` must remain domain-agnostic. Domain names compile to stable ids
before reaching the kernel. Gameplay concepts such as low-altitude verb shapes,
map signals, effects, and map control live in `Engine.Gameplay`; the kernel
only executes the primitive program representation.

## Runtime Composition

Startup follows this shape:

1. Godot creates the game runtime.
2. `Engine.Host` loads the Ice Remembers domain definition.
3. `Engine.Domain` registers stable ids and builds the definition graph.
4. `Engine.Gameplay` validates and compiles gameplay definitions.
5. `Engine.Host` installs compiled programs and seed data into the kernel.
6. Projections and observability consumers attach cursors to the event log.
7. The driver advances the kernel; consumers drain events independently.

Consumers must not stall kernel advancement. If a consumer needs richer
materialized state, it builds that state from events or snapshots on its own
side of the boundary.

The Godot UI follows the same consumer boundary. Projections expose versions,
dirty sets, and active-state indexes; render components own Godot subtrees and
present only when their inputs change. The stable UI rules are recorded in
[ui-principles.md](./design/ui-principles.md).

## Kernel Tick

`Simulation.AdvanceTicks` is the single mutation entrypoint. Each tick drains
commands, runs registered programs in order, emits primitive events, and
advances the world version.

Current default program order:

```text
Movement
LowAltitudeVerbs
MapActivity
MapSignals
CultureBoundaries
Relationships
Effects
Upkeep
Gambits
Generators
CultureSlots
MinorActors
```

The program dependency metadata makes the load-bearing ordering explicit.
`LowAltitudeVerbs` consumes previous-tick activity/signals. `MapSignals`
consumes same-tick activity. `CultureBoundaries` rebuilds derived culture
component caches from durable culture tile influence. `Effects` consumes
same-tick map signals and culture evidence.

The default cadence separates high-frequency minor labor from the main graph
cadence. `MinorActors` runs every simulation tick. The main graph programs run
every `MinorActorMainTickRatio` ticks, currently `5`, while `CultureSlots` runs
every ten main cadences. Generator pulse widths and other main-cadence tuning
are scaled through the same ratio so changing the ratio does not silently
retune graph pacing.

## Runtime State

The kernel world contains:

- entities, health, traits, status, physical and semantic coordinates;
- relationships and relationship strength;
- physical and semantic map cells, terrain, named coverage, roads, culture
  influence tile layers, and derived culture boundary caches;
- provenance invocation/output history for high-altitude and authored effects;
- program data/state/scratch for each registered kernel program.

Semantic domain reasoning and authored graph validation happen outside the
kernel in the definition graph. The runtime entity graph is the simulation
state; the definition graph is the authored-domain contract.

## Program Families

Low-altitude programs produce map life, relationship-strength drift, and slow
state mutation:

- movement routing and road reinforcement;
- low-altitude verbs for movement, culture influence, coverage, anchor
  relocation, work-tier growth, and explicit relationship-strength mutation;
- map activity;
- map signals;
- culture boundary derivation;
- relationship maintenance;
- health effects;
- upkeep/lifecycle.

Minor actors are a separate low-salience runtime layer. They are not graph
entities and do not create narrative events; they are unnamed cohorts emitted by
major entities to perform frequent local labor such as cultural reinforcement,
foreign-trace cleanup, road maintenance, and lifecycle support.

High-altitude programs produce significant graph changes:

- gambits;
- generators.

The Chronicle Director sits above both as a recognition layer: it casts story
archetypes over live entities, arms authored beats (gambit bias and
chronicle-granted gambits) while they are open, and records which beats the
cast actually strikes. It never writes world state; endings are recognized
facts or fades. See
[22-chronicle-director.md](./systems/22-chronicle-director.md).

The former macro aggregate layer has been removed. Generator, gambit, trait,
and card work use shared query, effect, and decision-modifier primitives.
Relationship strength and locality queries are the default numeric gates when
health, culture influence, coverage, or map signals are not the actual subject.

The detailed execution boundary is recorded in
[shared-query-effect-executor.md](./kernel/shared-query-effect-executor.md).

## Event Boundary

Kernel events are fixed primitive records. Strings, display names, and
human-readable diagnostics are resolved by consumers through symbol catalogs.
The kernel can emit typed event ids and numeric payload slots; it cannot write
logs, allocate trace objects, or query projection state.

Provenance is kernel state and primitive event data. The kernel records
producer kind/id, parent invocation, trigger evidence, and outputs. It does not
interpret narrative chains. `Engine.Projection` exposes a generic provenance
read model over those records; domain content compiles chronicle query patterns
to runtime ids; `TheCanonryGame.Game` executes those patterns for presentation.

Parquet exports and analysis reports are observability products, not kernel
state. They must be reconstructable from the event stream and symbol tables.
