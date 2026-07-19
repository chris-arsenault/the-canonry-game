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
  reusable gameplay vocabulary and validators, including knowledge grant
  domains; compiles authored gameplay into primitive kernel program data

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
  domain-neutral hex/triangle topology, causal uplift and erosion, connected
  land/water analysis, hydrology, climate, glaciation, fractures, aggregation,
  terrain catalogs, and map coverage

IceRemembers
  authored domain content plus physical material/surface taxonomy, generation
  profile, and tunable world-quality targets

IceRemembers.Seed
  dependency-ordered natural-surface and fire/aurora/pressure worldgen,
  authored hex-terrain synthesis, geography-shaped subdivisions, startup
  placement, and profile-driven seed validation
```

`Engine.Kernel` must remain domain-agnostic. Domain names compile to stable ids
before reaching the kernel. Gameplay concepts such as low-altitude verb shapes,
map signals, effects, and map control live in `Engine.Gameplay`; the kernel
only executes the primitive program representation.

## Runtime Composition

Startup follows this shape:

1. Godot paints a modal startup surface, then schedules world construction off
   the render thread.
2. Fixed-seed interactive development runs restore a versioned immutable
   startup-seed cache when available; random runs generate directly.
3. `Engine.Host` loads the Ice Remembers domain definition.
4. `Engine.Domain` registers stable ids and builds the definition graph.
5. `Engine.Gameplay` validates and compiles gameplay definitions.
6. `Engine.Host` installs compiled programs and seed data into the kernel.
7. Projections and observability consumers attach cursors to the event log.
8. Godot binds the completed projections on its render thread, then starts the
   realtime driver.

The cache boundary is the stable-name `DomainStartupSeedResult`, before domain
ids, kernel state, event cursors, and Godot objects exist. Cache keys include a
world-generation version and base seed; corrupt or stale snapshots regenerate.
Debug launches default to fixed seed `4200` so iteration reuses the same world.
`--random-seed`, `--no-world-cache`, and `--regenerate-world` retain explicit
control over startup behavior. The cache is a developer startup optimization,
not a simulation persistence or save-game format.

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
CultureFieldDecay
CultureBoundaries
Relationships
Effects
Upkeep
Gambits
ChronicleDirector
Generators
WorldBeats
MapFrontier
CultureSlots
Knowledge
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
- physical and semantic map cells with independent live extents, effective
  terrain, physical base terrain and transient cover, named coverage, roads,
  culture influence tile layers, and derived culture boundary caches;
- installed radius-bounded per-hex latent geography: physical base terrain,
  superregion, and continent ids, plus varied semantic terrain;
- provenance invocation/output history for high-altitude and authored effects;
- program data/state/scratch for each registered kernel program.

The kernel does not store the physical microtriangle field or the generation
process that produced it. Those immutable startup facts remain outside the
kernel in the stable-name seed blueprint and its read-only projection. The
kernel's frontier program needs only the resolved terrain and regional identity
for a coordinate.

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

Knowledge is a derived culture-scoped modifier layer across both altitudes.
Authored technology sites and stable grant targets are resolved by the host;
the kernel recomputes claims from semantic culture mass and exposes primitive
modifiers to existing programs. See
[27-knowledge.md](./systems/27-knowledge.md).

The Chronicle Director sits above both as a recognition layer: it casts story
archetypes over live entities, arms authored beats (gambit bias and
chronicle-granted gambits) while they are open, and records which beats the
cast actually strikes. It never writes world state; endings are recognized
facts or fades. See
[22-chronicle-director.md](./systems/22-chronicle-director.md).

The World Beats program is the same treatment applied to the world itself:
authored Ages on three regime tracks (Ice, Peoples, Memory) open and close on
recognized macro facts - never timers - and, while open, charge the rates of
existing generators, verbs, gambits, and the contest decay tide. Campaigns
(minor-actor labor columns projected at an enemy's frontier while a
`wages_campaign_against` edge lives) are the first age payload. See
[25-world-beats.md](./systems/25-world-beats.md).

The physical and semantic worlds grow from separately tracked radius-18
hearts toward an explicit fixed-capacity generation radius of 128. Seed/worldgen
installs deterministic terrain and geography for that whole latent field, but
`MapFrontier` materializes cells only where culture presses an edge. Physical
worldgen first builds one padded triangle graph. Reduced moving crust provinces
produce continental assemblies, ocean basins, typed boundaries, strain,
mountain systems, rifts, and sparse island arcs; erosion, connected-ocean bathymetry, conditioned
drainage, basin-shaped ocean currents, precipitation, climate, ice flow,
glacial carving, and fractures consume the prior
passes through independent dependency-declaring layers. Ice Remembers then runs
a second external layer pipeline that classifies varied cold land and water,
routes fire through fractures, aurora through cold ice reservoirs, and pressure
waves through ocean geometry.
It retains natural surfaces, anomaly flags, and all three intensity fields while
scored authored rules reduce each 24-triangle aggregate to the base terrain the
hex-scale kernel consumes. Ocean, current, wave, rain, snow, land-ice, sea-ice,
landmass, lake, surface
concentration, warmth, and anomaly distribution goals are tunable profile
targets; structural graph and routing contracts remain invariants. Geographic
subdivisions grow as connected partitions over those facts and never repaint
the physical field. During the Long Dark,
ordinary reclaim applies impassable Deep Ice as transient cover only to
frontier-grown physical cells. Durable terrain and the startup heart remain
intact underneath. Reach across the map is priced by travel cost, not raw hex
distance: a `WithinTravelReach` gate line-samples road-discounted terrain cost
between endpoints, so frozen frontier blocks reach and roads extend it. See
[26-growing-map.md](./systems/26-growing-map.md).

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

The Host also converts the immutable startup physical-geography blueprint into
a presentation-owned `PhysicalGeographyProjection`. It contains the complete
latent disk and is separate from the event-backed `HexMapProjection`, which
continues to represent only materialized cells and their current terrain. The
blueprint and projection retain every natural/final surface, material,
landform, hydrology, climate, ice, fracture, fire, aurora, pressure, and anomaly
fact. The UI can therefore inspect generated geography without reading live
kernel state or making latent cells part of gameplay.

Provenance is kernel state and primitive event data. The kernel records
producer kind/id, parent invocation, trigger evidence, and outputs. It does not
interpret narrative chains. `Engine.Projection` exposes a generic provenance
read model over those records; domain content compiles chronicle query patterns
to runtime ids; `TheCanonryGame.Game` executes those patterns for presentation.

Parquet exports and analysis reports are observability products, not kernel
state. They must be reconstructable from the event stream and symbol tables.
