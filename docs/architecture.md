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
  consumer-side UI/read-model projections

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

## Kernel Tick

`Simulation.AdvanceTicks` is the single mutation entrypoint. Each tick drains
commands, runs registered programs in order, emits primitive events, and
advances the world version.

Current default program order:

```text
LowAltitudeVerbs
MapActivity
MapSignals
Relationships
Effects
Upkeep
Gambits
Generators
Degeneracy
```

The program dependency metadata makes the load-bearing ordering explicit.
`LowAltitudeVerbs` consumes previous-tick activity/signals. `MapSignals`
consumes same-tick activity. `Effects` consumes same-tick map signals.

## Runtime State

The kernel world contains:

- entities, health, traits, status, physical and semantic coordinates;
- relationships and relationship strength;
- physical map cells, terrain, named coverage, and map influence layers;
- program data/state/scratch for each registered kernel program.

Semantic domain reasoning and authored graph validation happen outside the
kernel in the definition graph. The runtime entity graph is the simulation
state; the definition graph is the authored-domain contract.

## Program Families

Low-altitude programs produce map life and slow state drift:

- low-altitude verbs;
- map activity;
- map signals;
- relationship maintenance;
- health effects;
- upkeep/lifecycle.

High-altitude programs produce significant graph changes:

- gambits;
- generators;
- degeneracy detectors.

The former macro aggregate layer has been removed. Generator, gambit, trait,
and future card work use shared query, effect, and decision-modifier
primitives. Relationship strength and locality queries are the default numeric
gates when health, resources, influence, coverage, or map signals are not the
actual subject.

The detailed execution boundary is recorded in
[shared-query-effect-executor.md](./kernel/shared-query-effect-executor.md).

## Event Boundary

Kernel events are fixed primitive records. Strings, display names, and
human-readable diagnostics are resolved by consumers through symbol catalogs.
The kernel can emit typed event ids and numeric payload slots; it cannot write
logs, allocate trace objects, or query projection state.

Parquet exports and analysis reports are observability products, not kernel
state. They must be reconstructable from the event stream and symbol tables.
