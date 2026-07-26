# System Architecture

The engine is a deterministic microkernel hosted by the Godot game runtime.
Kernel phases own mutation explicitly. The coordinator runs admission,
structural reconciliation, strategic work, and publication. A fixed-lifetime
worker set evaluates eligible frozen ranges and reduces entity-, relationship-,
and cell-owned destinations concurrently. Workers join at every published
boundary before coordinator reconciliation or later phases. UI, projections,
run logs, telemetry, exports, and Monte Carlo analysis consume primitive records
outside kernel state.

The controlling kernel rules live in
[simulation-kernel-discipline.md](./kernel/simulation-kernel-discipline.md).

The selected parallel architecture is a typed-owner bulk-synchronous scheduler,
not a persistent geographic-actor model. Evaluation partitions by stable IDs
or bounded work chunks; mutation partitions independently by entity,
relationship, or cell storage owner. Geography is useful for cell/map state but
is not the universal shard key: measured relationship and gambit traffic is too
nonlocal and current geographic load is too skewed. The persistent evaluation
workers, frozen range contract, typed-owner reduction, and partitioned event
publication are implemented. Strategic ownership and public worker-count wiring
remain separately gated work. Workers must not be used to preserve accidental
superlinear candidate scans.

The first owner-ready storage batch is complete. Entity coordinates remain
entity-owned while physical and semantic cell-membership chains have a bounded
spatial-index owner. Relationship slots remain relationship-owned while
incident adjacency, frozen CSR, delta chains, and graph compaction have a
bounded endpoint-index owner. Typed deltas now route to those destinations
without locks or duplicate compatibility state. Spatial membership, coverage
reverse indexes, culture-row storage, relationship culling, and lifecycle
retirement still reconcile on the coordinator where structural mutation crosses
owners.

The evaluation-worker batch is also complete. Each simulation owns a bounded
worker set that starts background threads lazily and reuses them until the host
shuts down. Effects and relationship maintenance can evaluate disjoint frozen
ranges concurrently. Low-altitude verbs can do so when trace capture is off.
Minor actors already use the common range interface but remain one-range until
their shared actor indexes and encounters receive typed ownership. Worker-local
append arenas feed destination-owner reducers. Entity slots own attributes,
tags, movement coordinates, and influence credits; relationship slots own
strength; cells own terrain, coverage values, culture writes, and culture
reductions. Primitive events use the same worker partitioning and are compacted,
sorted, and published only after the phase barrier.

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
locality rules, effects, and map control live in `Engine.Gameplay`; the kernel
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

Consumers must not stall kernel advancement or retain live kernel references.
If a consumer needs richer materialized state, it builds that state from
primitive events or published snapshots on its own side of the boundary.

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

`Simulation.AdvanceTicks` is the single mutation entrypoint. Each tick
publishes six explicit phases:

1. command admission;
2. local-view preparation;
3. local evaluation against a frozen active-entity and active-relationship
   view;
4. deterministic local-delta resolution;
5. strategic programs; and
6. tick publication.

Programs retain registration order inside their assigned phase, but local
programs no longer depend on mutation becoming visible during their own
evaluation pass. They emit fixed-capacity typed deltas for movement,
attributes, culture, culture reduction, relationship strength/end, tags, cell
state, and lifecycle. Resolution sorts by destination and stable logical source,
merges additive values before clamping, chooses competing writes explicitly,
and applies retirement only after accepted local state changes.

Semantic events are buffered and sorted within the publishing phase rather than
leaking evaluator order. `PhaseCompleted` records the tick, phase revision,
work count, and mutation count; projections expose the latest published
boundary. Random choices use stable keys derived from seed, tick,
phase, program, logical occurrence, rule, purpose, and attempt. There is no
mutable sequential random stream whose consumption can shift when evaluation
order changes.

Gambit preconditions compile into one bounded branch-plan representation.
Actor-only values are prepared once per actor and gambit; candidate branches
short-circuit through explicit true/false destinations. When every successful
branch requires the same relationship to the target, the compiled plan
publishes that source and the kernel walks the relationship index directly
rather than scanning all entities.

Current default program order:

```text
Movement
LowAltitudeVerbs
CultureFieldDecay
Relationships
Effects
Gambits
ChronicleDirector
Generators
WorldBeats
MapFrontier
CultureSlots
Knowledge
MinorActors
```

The program dependency metadata makes load-bearing ordering explicit.
Locality and culture-locality facts are direct bounded queries over current
world state, not scheduled programs or materialized fields. `LowAltitudeVerbs`,
`Effects`, generators, culture slots, and minor actors read those facts only
where they act. Culture-locality queries use fixed-capacity, revision-stamped
memoization for repeated reads of the same culture and cell; culture mutation
invalidates entries by advancing the revision, without maintaining connected
components or another source of truth.

The kernel has one tick clock. Movement, verbs, relationships, effects,
gambits, chronicles, generators, and minor actors each receive one opportunity
per tick; slower programs retain explicit tick cadences. Authored durations are
direct tick values with no main-cadence mapper. There is no sub-tick counter or
second calendar.
`CultureSlots` and `Knowledge` retain their slower schedule, but their work is
revision- and due-driven: unchanged slots and cultures are not globally rebuilt.

Minor actors are persistent fixed-capacity task cohorts rather than emitted
TTL workers. Source/rule and campaign-relationship indexes own identity;
actor-cell links bound encounters to the same or adjacent cells. Cultural
fronts, road crews, and campaign columns publish lifecycle and work events so
projection can retain active map actors without promoting them to graph
entities.

## Runtime State

The kernel world contains:

- entities, health, traits, status, physical and semantic coordinates;
- relationships and relationship strength;
- physical and semantic map cells with independent live extents, effective
  terrain, physical base terrain and transient cover, named coverage, roads,
  and culture influence tile layers;
- installed radius-bounded per-hex latent geography: physical base terrain,
  superregion, and continent ids, plus varied semantic terrain;
- direct locality rules and bounded culture-locality dirty-cell tracking;
- a bounded stack of current provenance invocations and compact causal tokens
  on active entities and relationships;
- program data/state/scratch for each registered kernel program.

Entities and relationships occupy reusable internal slots with generation-safe
occurrence handles at every boundary where identity can survive a mutation.
Dense active vectors and incrementally maintained kind, culture, adjacency, and
occupancy indexes keep ordinary iteration proportional to live state. Retiring
an occurrence clears its direct attribute, trait, cooldown, intent, cohort,
district, and graph-owned rows before the slot generation advances.

Map coordinates have stable dense cell IDs. Terrain and other per-cell facts
are direct arrays; coverage, roads, travel memory, and culture influence add
dense active/dirty indexes so zero or absent rows do not remain scheduled work.
The coordinate registry is durable map identity, while reveal state remains an
independent gameplay fact.

Relationship endpoint adjacency uses sorted frozen CSR rows plus bounded
per-row delta chains. Edge mutation is local to the source and target rows;
deterministic queries merge frozen and delta order, and periodic compaction
rebuilds the disposable CSR from active edges. The endpoint index owns these
mutable arrays, while the hot read path uses direct read-only aliases to avoid
per-edge facade overhead. Culture-locality descriptors
similarly retain per-cell dependency revisions so a culture write invalidates
only the radius-two cells whose derived facts can change.

Durable additive state uses field-specific fixed-point integers: culture mass,
relationship strength, persistent attributes and health, road quality/work, and
persistent local modifiers. Each field owns its scale, bounds, and clamp rule;
there is no universal simulation scale. Other runtime values—candidate scores,
probabilities, ratios, heat, and path costs—use the signed `FixedScalar` type.
Authored rules may remain readable floats before the domain/gameplay compiler
boundary. `Engine.Kernel` and compiled runtime records contain no floating-point
members. Common bounded arithmetic stays in signed 64-bit operations and falls
back to `Int128` only on overflow; gameplay scoring uses explicit bounded
rational or polynomial curves instead of generic runtime transcendental math.

Retirement is final for an entity occurrence. It ends every incident
relationship and removes hosted districts before publishing the historical
status transition. Ended relationships and consumed districts release their
active slots; durable history belongs to the primitive event stream and its
consumer projections, not to parallel historical tables in the kernel.

Kernel snapshot format `55` persists active state, generation metadata, and the
phase revision, then rebuilds free lists, indexes, due queues,
and disposable caches on load. It does not serialize retired entity rows, ended
edges, expired cooldowns, completed provenance tables, route-cache contents, or
sequential RNG state. Worker count and logical shard count are host execution
topology, not simulation state, so a loading host supplies them rather than
reading them from the snapshot. The format intentionally has no reader for the
replaced binary layout.

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
- direct locality and culture-locality queries;
- relationship maintenance;
- health effects;
- merged effects/lifecycle resolution.

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
health, culture influence, or coverage are not the actual subject.

The detailed execution boundary is recorded in
[shared-query-effect-executor.md](./kernel/shared-query-effect-executor.md).

## Event Boundary

Kernel events are fixed primitive records. Strings, display names, and
human-readable diagnostics are resolved by consumers through symbol catalogs.
The kernel can emit typed event ids and numeric payload slots; it cannot write
logs, allocate trace objects, or query projection state.

State-change events for fixed-point fields carry the integer runtime units.
They provide no typed floating-point compatibility properties. Projection,
telemetry, and UI code convert explicit unit fields after consuming the event.

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

Provenance crosses the primitive event boundary. While an authored action runs,
the kernel retains only its bounded current frame and a structural output
count. Entity and relationship creation events carry the invocation token;
completion streams producer, parent, and trigger evidence.
`Engine.Projection` retains active causal chains plus a bounded recent window,
while the chronicle captures immutable rendered facts. Neither the kernel nor
the live projection retains an unbounded completed invocation archive.

Creation and lifecycle facts are explicit at that same boundary.
`EntityCreated` carries its stable name seed, initial coordinates, creation
cause, and birth-locality sample; relationship changes carry active/end state;
and `MapCellStateChanged` is the sole plane-aware map-cell lifecycle event.
Consumers do not recover those facts from retired rows or same-tick event
correlation.

Parquet exports and analysis reports are observability products, not kernel
state. Raw sampled culture cells cross the capture boundary; culture components
and holes are derived later in DuckDB rather than synchronously in the host.
