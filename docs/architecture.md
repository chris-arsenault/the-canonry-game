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
  domain-neutral global microtriangle topology, evolving tectonic assemblies,
  sparse impacts, causal uplift and erosion, connected land/water analysis,
  fixed-point physical floors, independent actual-water and conditioned
  drainage surfaces, water-balanced lakes, outlet-ranked hydrology, climate,
  glaciation, valley-shaped river geometry, and the phased physical-evolution
  session

IceRemembers
  authored domain content plus physical material/surface taxonomy, generation
  profile, configurable geological/magic epoch schedule, tunable generation
  controls, and non-blocking diagnostic goals

IceRemembers.Seed
  multi-epoch orchestration, dependency-ordered natural-surface and
  fire/aurora/pressure worldgen, projection of generated microtriangles into
  runtime cells, aggregate and authored hex-terrain synthesis,
  geography-shaped subdivisions, startup placement, and structural seed
  validation
```

`Engine.Kernel` must remain domain-agnostic. Domain names compile to stable ids
before reaching the kernel. Gameplay concepts such as low-altitude verb shapes,
map signals, effects, and map control live in `Engine.Gameplay`; the kernel
only executes the primitive program representation.

## Runtime Composition

Startup follows this shape:

1. Godot paints a modal startup surface, then schedules world construction off
   the render thread.
2. Fixed-seed interactive development runs prefer an explicit versioned
   `worlds/*.world` artifact for that seed, then restore the ordinary immutable
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

Pregenerated `.world` files serialize the same pre-kernel startup boundary but
are explicit local artifacts rather than cache entries. The application scans
its ordinary filesystem `worlds` directory (`<repo>/worlds` during development,
`<executable directory>/worlds` when exported), validates each file's format and
world-generation version, and offers valid files through the top-bar world
selector. These potentially large generated files are gitignored; selecting one
replaces the current runtime via the ordinary asynchronous startup/loading
surface. Release artifacts are built through `tools/WorldGenerator`, keeping
generation out of the Godot UI process.

Consumers must not stall kernel advancement. If a consumer needs richer
materialized state, it builds that state from events or snapshots on its own
side of the boundary.

The Godot UI follows the same consumer boundary. Projections expose versions,
dirty sets, and active-state indexes; render components own Godot subtrees and
present only when their inputs change. The stable UI rules are recorded in
[ui-principles.md](./design/ui-principles.md).

Physical-map presentation shares one policy for projected colors, structural
overlays, and view decisions. The live two-dimensional Godot node and
`tools/WorldGenerator capture` adapt that policy through the canvas painter.
The read-only Godot 3D inspector reuses the same colors while reconstructing a
chunked displaced mesh with smooth seam normals, level water bodies, and
terrain-following river and feature geometry. Neither renderer computes
worldgen or simulation facts. The capture path loads an ordinary `.world` and
writes deterministic offscreen PNG evidence.

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
worldgen first converts the requested kernel radius to a triangle count, then
builds one padded global equilateral-microtriangle lattice whose adjacent
centroids are all exactly one generation distance unit apart. The generator has
no hex coordinates, parent-cell indexes, sectors, or cell-boundary slope
correction. After generation, `IceRemembers.Seed` alone maps global triangles
into the kernel's cells and canonical 24-local-triangle ordering, translates
flow and edge masks, aggregates terrain facts, and creates subdivisions.
Reduced moving crust provinces
produce continental assemblies, continuous crust composition, ocean basins,
typed boundaries, strain, and persistent material history. Plate motion
source-scatters each equal-area crust column conservatively; convergent overlap
is retained as shortening surplus rather than multiplied into local thickness.
Connected plate-pair components spend finite deformation budgets on coherent
collision ranges, subduction belts, rifts, and transform relief. Each system
constructs both a broad flexed crustal root and narrower resistant summit rock.
Convergence between unlike transported continental assemblies also creates a
material suture whose pair identity and shortening strength advect with the
crust. Those inherited systems preserve major interior ranges after active
boundaries migrate; they are not synthetic elevation belts.
Continuous composition selects a modest foundation height and matching
foundation thickness; departure from that foundation supplies Airy support,
while oceanic age accumulates basin depth away from young spreading crust.
Explicit loads and support are reconciled regionally by lithospheric flexure.
Ocean coverage calibrates one stable sea datum. Ordinary margin evolution leaves
landward relief intact and caps oceanward deepening until the shelf and
continental rise meet the ambient basin without a fixed band ending; convergent
margins retain exceptional trenches. Marine deposition reuses only exported
sediment. Local mass wasting apportions one bounded outgoing budget per source
triangle. Four
authored but configurable epochs then reuse the same dependency-declaring
layers for incremental construction, erosion, connected-ocean bathymetry, conditioned
drainage, catchment-water-balanced lakes, lake-overflow outlet incision,
basin-shaped ocean currents, precipitation, climate, ice flow,
incremental bedrock erosion and regional unloading response, conserved sediment transport and deposition,
glacial carving, coastal evolution, fractures, channel widening, floodplains,
and delta fans. Sparse impacts precede the epochs so their excavated basins,
fractured rims, ejecta, and residual heat are subsequently eroded, flooded,
glaciated, or buried. Stable mantle-plume roots also precede the epochs; crust
motion advances their independently generated volcanic construction into
seamount/island chains, cones, calderas, and lava plains before each epoch's
climate and hydrology. Ice Remembers interleaves fire heat, aurora storage, and ocean
pressure with those physical phases, translating pressure into a generic
coastal-forcing field rather than importing domain concepts into physical
worldgen. Current magic fields, cumulative epoch histories, and first-active
epoch indexes remain separate;
feature interactions and final varied cold land/water surfaces classify only
after the final physical reconciliation.
It retains natural surfaces, anomaly flags, and all three intensity fields while
scored authored rules reduce each 24-triangle aggregate to the base terrain the
hex-scale kernel consumes. Causal controls shape ocean, current, wave, rain,
snow, ice, landform, drainage, warmth, and anomaly outcomes; their distributions
are non-blocking diagnostics. Structural graph, water-body, routing, and
projection contracts remain invariants, and fixed-seed Release renders remain
the quality evidence. Geographic
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
landform, fine physical floor, actual water surface, conditioned drainage
surface and routing rank, hydrology, river channel/floodplain/delta coverage,
climate, ice, bedrock, sediment, cumulative erosion and
deposition, maximum glaciation, wave history, fracture, current and cumulative fire/aurora/pressure,
their first-active epochs, and anomaly
fact. The UI can therefore inspect generated geography without reading live
kernel state or making latent cells part of gameplay.

The physical 3D inspector is a presentation-owned modal consumer of that same
immutable projection. Entering it pauses kernel playback and hides the complete
two-dimensional map/entity subtree; leaving it restores the previous plane and
playback state. Its mesh cache, orthographic camera, ray picking, wireframe,
water, feature, and vertical-exaggeration controls do not create kernel state or
make microtriangles interactive gameplay coordinates.

Provenance is kernel state and primitive event data. The kernel records
producer kind/id, parent invocation, trigger evidence, and outputs. It does not
interpret narrative chains. `Engine.Projection` exposes a generic provenance
read model over those records; domain content compiles chronicle query patterns
to runtime ids; `TheCanonryGame.Game` executes those patterns for presentation.

Parquet exports and analysis reports are observability products, not kernel
state. They must be reconstructable from the event stream and symbol tables.
