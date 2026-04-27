# System Architecture

The engine is a deterministic microkernel hosted by a Godot game runtime.
The kernel mutates state on a single sim thread; everything else (UI, run
logs, telemetry, exports, Monte Carlo analysis) is an independent async
consumer of a primitive event stream.

For the rules that govern what may live where, see
`docs/kernel/simulation-kernel-discipline.md` (the controlling architecture
direction). For ongoing migration status, see
`docs/kernel/invarient_progress_tracking.md`.

## Project Topology

```
                 ┌────────────────────────────┐
                 │  TheCanonryGame.App        │  Godot autoload (Main.cs)
                 │  scenes/Main.tscn          │
                 └─────────────┬──────────────┘
                               │ creates
                 ┌─────────────▼──────────────┐
                 │  TheCanonryGame.Game       │  CanonryGameRuntime
                 │  GameRuntimeReadModels     │  GameIntentPort
                 │  GameRuntimeControls       │  GameRuntimeTableau
                 └─────────────┬──────────────┘
                               │ uses
        ┌──────────────────────▼─────────────────────────┐
        │             Engine.Host                         │
        │  KernelRuntimeFactory                           │
        │  KernelHostBuilder · KernelHost                 │
        │  KernelStartupContext · IKernelDomainModule     │
        │  KernelConsumerRegistry                         │
        └──┬──────────┬──────────┬─────────────┬─────────┘
           │          │          │             │
           ▼          ▼          ▼             ▼
   Engine.Domain  Engine.Kernel  Engine.Eventing  Engine.Projection
   (compile to    (deterministic   (bounded ring     (UI read models,
    stable IDs)    tick, fixed      buffer of         symbols, names)
                   buffers, IDs     primitive events,
                   only, no         independent
                   strings)         cursors)
                       ▲
                       │ emits events
                       │
                       ▼  (also consumed by)
                Engine.Observability  · Engine.Observability.Export
                (RunLogger, telemetry, planner traces, perf, exports)

   Engine.Kernel.Driver  — async edge: realtime/stepped/unpaced drivers,
                          time control, command submission/completion ring
   Engine.WorldGen       — hex grid, terrain catalog, semantic plane,
                          ambient resource layout
   Engine.Constants      — primitive ID code catalogs (no deps)
   Engine.Eventing.Generators / .Schema / Engine.Kernel.Schema — code-gen
                                                                 and runtime
                                                                 event schema
```

`Engine.Kernel` references only `Engine.Constants` and `Engine.Eventing`.
Build analyzers (`Engine.Kernel/BannedSymbols.txt`) reject `string`,
`StringBuilder`, `FormattableString`, LINQ, `List<T>`/`Dictionary<,>`/
`HashSet<>`/`Queue<>`/`Stack<>`, `IEnumerable<T>`/`IReadOnlyList<T>`,
`Task`/`ValueTask`/`Thread`/`Channel`, `Stopwatch`, `Console`, `File`,
`Directory`. Implicit usings are disabled. Overflow behavior is explicit on
every fixed-capacity buffer.

## Runtime Composition

```
1. Godot loads scenes/Main.tscn → TheCanonryGame.App.Main (Node2D).
2. Main._Ready() caches scene nodes, builds the UI theme, then calls
   InitializeKernelHost() → CanonryGameRuntimeFactory.Start(seedPolicy).
3. CanonryGameRuntimeFactory uses a static KernelRuntimeFactory registered
   for the IceRemembers domain (IceRemembersRuntime.CreateRuntimeFactory).
4. KernelRuntimeFactory.StartUiRuntime(...) builds a KernelHost via
   KernelHostBuilder:
     · WithDomainConfig(IceRemembersDefinition.Create()) → DomainCompiler
       compiles strings into stable runtime IDs and produces CompiledDomain.
     · WithDomainStartupSeed(...) supplies the IceRemembersStartupSeed.
     · ConfigureSymbols(...) registers the projection symbol catalog.
     · WithDriverConfig(RealtimeDriverConfig) selects ticks-per-second,
       MaxTicksPerSlice, and SliceInterval based on KernelFunctionProfile.
     · RegisterProjection(...) adds HexMapProjection, UiProjectionState,
       UiChronicleProjection.
5. KernelHost owns the Simulation (kernel), the KernelEventLog, the driver,
   the command ring, the consumer registry, and a ProjectionSymbolCatalog.
6. RunLogger is constructed against the host's events and symbols.
7. The driver runs on its own task; consumers drain off the sim thread.
8. Each Godot frame, Main._Process(delta) calls runtime.Update(), which
   pumps consumers (DrainConsumers, RunLogger.DrainAvailable) and returns a
   GameRuntimePulse. UI panels refresh when TicksProcessed > 0.
```

## Kernel Tick

`Engine.Kernel.Simulation : IKernel` exposes:

- `int CurrentTick`
- `long CurrentVersion`
- `int AdvanceTicks(int count, in AdvanceOptions options = default)` —
  the only entry point that mutates state, asserted to run on a single
  writer thread (DEBUG-time check).

Each tick:

1. `CurrentTick++` and `World.BeginTick(CurrentTick)`.
2. `DrainCommands()` — applies queued external commands (e.g. enable/disable
   a generator). Stale-version commands are rejected and emit a
   `CommandRejected` event. Successful commands emit `CommandApplied`.
3. `TickSystems()` — for each registered system whose cadence divides the
   current tick, enter an execution context (program ID + source code ID)
   and call `system.Tick(this, CurrentTick)`. The execution context is
   stamped onto every event emitted from inside that program.
4. Emit `TickCompleted`.

### Program registration order

`Simulation` registers programs at construction in this order. Cadence is
fixed; planning is currently invoked from inside Processes, not as a
top-level program.

| # | Program | Cadence | Code path |
| --- | --- | --- | --- |
| 1 | Upkeep | 1 | `Engine.Kernel/Programs/Upkeep` |
| 2 | Relationships | 1 | `Engine.Kernel/Programs/Relationships` |
| 3 | Processes | 1 | `Engine.Kernel/Programs/Processes` |
| 4 | Gambits | 1 | `Engine.Kernel/Programs/Gambits` |
| 5 | Pressures | 5 | `Engine.Kernel/Programs/Pressures` |
| 6 | Generators | 1 | `Engine.Kernel/Programs/Generators` |
| 7 | Degeneracy | 10 | `Engine.Kernel/Programs/Degeneracy` |

`KernelProgramRegistry.SetEnabled(programId, bool)` lets the host pause an
individual program at runtime. Each program owns a `Data`/`State`/`Scratch`
triple sized at construction from `SimulationOptions` capacities; the kernel
performs no per-tick allocation in steady state.

### What the kernel owns

- `WorldState` — entity ↔ attribute table, terrain cells, resources,
  relationship adjacency, spatial indexes (all primitive ID, fixed buffer).
- `KernelDataSet` (`KernelEntityData`, `KernelTerrainData`) —
  pre-allocated primitive tables for attributes and terrain.
- Program runtimes — `UpkeepProgramRuntime`, `RelationshipProgramRuntime`,
  `ProcessProgramRuntime`, `GambitProgramRuntime`, `PressureProgramRuntime`,
  `GeneratorProgramRuntime`, `DegeneracyProgramRuntime`,
  `PlanningProgramRuntime`.
- `KernelEventLog` — bounded ring of `SequencedKernelEvent`, with explicit
  overflow accounting and cursor-based reads.
- `KernelCommandRing` — bounded queue of incoming commands.
- `RandomSource` — the deterministic PRNG; its state is part of the
  snapshot.
- `KernelExecutionContext` — current program ID + source code ID stamped
  onto every event written.

### What the kernel never does

- Allocate per tick in steady state (no LINQ, no transient lists/dicts, no
  closures).
- Create or look up strings at runtime.
- Read or write files, log, or block on async work.
- Format text, build diagnostic messages, or construct presentation objects.
- Touch UI types, telemetry batches, or run-log records.

## Domain Compilation

```
DomainConfig (Engine.Domain/Config)
  ├── Kinds, Subtypes, Cultures, Terrains
  ├── Attribute schemas per kind (min/max/default/variance)
  ├── Processes / Gambits / Generators (preconditions, scoring, mutations)
  ├── Relationships (kinds, polarities, valid category pairs)
  ├── Pressures (scope, baseline, smoothing)
  ├── Resources (ambient hex layout)
  ├── Lifecycle rules
  └── Degeneracy detectors
                ↓
        DomainCompiler.Compile(config)
                ↓
        CompiledDomain
          ├── DomainIdRegistry  (stable primitive IDs, hard-fails on
          │                      duplicate or unknown lookup)
          ├── DomainConfig       (retained as authoring metadata)
          ├── DefinitionGraph    (validated process/gambit wiring)
          ├── RuntimeCodes       (source code IDs per program)
          ├── KernelData         (pre-sized capacities for kernel buffers)
          ├── Programs           (KernelProgramModel per program)
          └── Symbols            (ID → display name catalog for projections)
```

After compilation:

- The kernel only sees primitive IDs.
- Display names live in `ProjectionSymbolCatalog`, which projections read
  off the sim thread to build human-readable UI text.
- Wiring validation runs at compile time. Coverage gaps, dead-end goals,
  unknown attribute references, and orphan processes fail the build, not
  the tick.

## Event Boundary

The kernel writes to a single bounded `KernelEventLog`. Each event is a
fixed-size struct (sequence, tick, world version, event type, program ID,
source code ID, plus a small primitive payload). The kernel's write API is
non-blocking (`TryWrite(in KernelEvent)`); mismatches with the schema
registry fail loudly.

```
Simulation
  WriteKernelEvent(in KernelEvent)
        │
        ▼
  KernelEventLog (ring buffer, bounded capacity, overflow counters)
        │
        │  independent cursors, each consumer paces itself
        │
        ├── Engine.Projection.HexMapProjection      → MapArea, HexMapRenderer
        ├── Engine.Projection.UiProjectionState     → EntityBrowser, EntityCardLayer
        ├── Engine.Projection.UiChronicleProjection → ChroniclePanel
        ├── Engine.Observability.TelemetryEventProjection → telemetry rows
        ├── Engine.Observability.PlannerTraceProjection   → planner traces
        ├── Engine.Observability.RunLogger                → on-disk run log
        ├── Engine.Observability.KernelTelemetryRecorder  → tick aggregates
        └── Engine.Observability.KernelPerformanceRecorder → perf summaries
                                                            (exported via
                                                             Engine.Observability.Export)
```

Event types include entity creation/move, attribute and resource changes,
relationship create/strength/cull, process/gambit evaluation and execution,
generator evaluation/fired, degeneracy detection, command applied/rejected,
plan started/scored/committed/abandoned, and `TickCompleted`. Events carry
only IDs, scalars, enums, and flags — never names, formatted text, or
diagnostic prose.

`KernelHost.DrainConsumers()` advances every registered projection cursor
in turn. Slow consumers lag independently; the kernel never waits for them.
`RunLogger.DrainAvailable()` runs on the same pump but writes off the sim
thread.

## Driver and Commands

`Engine.Kernel.Driver` provides three driver shapes:

- `RealtimeKernelDriver` — paced by wall clock, configured by
  `RealtimeDriverConfig(TicksPerSecond, MaxTicksPerSlice, SliceInterval)`.
- `SteppedKernelDriver` — explicit per-step requests (used for `Step`).
- `UnpacedKernelDriver` — runs as fast as possible (used for batch /
  Monte Carlo).

`IKernelDriverTimeControl` exposes pause, resume, single-step, and
`SetTickRate`. The driver is the only place that calls
`Simulation.AdvanceTicks` outside of tests.

External intent crosses the boundary as `KernelCommand` records on a
`KernelCommandRing`. The submitter (e.g. `GameIntentPort.RequestGenerator
Enabled`) issues the command stamped with the world version it was issued
against. The kernel rejects stale-version commands unless the command type
is version-tolerant. Both outcomes are observable as
`CommandApplied` / `CommandRejected` events.

## Persistence

`Engine.Kernel/Persistence` defines a versioned binary snapshot:

- `KernelSnapshot.SaveKernelState(Simulation, Stream)` /
  `LoadKernelState(Stream)` — round-tripped through
  `KernelSnapshotBinary`.
- `SimulationSnapshot` — current tick, random-source state,
  per-program snapshots.
- `WorldStateSnapshot` — entity attributes, resources, relationships,
  cells, all stored as `PrimitiveTableSnapshot` (int and float tables).
- Per-program snapshots — `ProcessProgramSnapshot`,
  `GambitProgramSnapshot`, `GeneratorProgramSnapshot`,
  `PressureProgramSnapshot`, `DegeneracyProgramSnapshot`,
  `UpkeepProgramSnapshot`, `RelationshipProgramSnapshot`,
  `PlanningProgramSnapshot`, `KernelProgramRegistrySnapshot`.

Loading replays as a controlled seed: the kernel emits
`Initialized`/`Seeded` events as it rehydrates, and projections rebuild
their read models from those events without any save-format awareness of
their own.

## Observability

`Engine.Observability` consumes the same primitive event stream as
`Engine.Projection`, but builds different products:

- `RunLogger` — append-only on-disk record of every event, resolved
  through `ProjectionSymbolCatalog` for human-readable lines. Default path:
  `RunLogger.DefaultLogPath()`.
- `TelemetryEventProjection` + `KernelTelemetryRecorder` — actor / action
  / target / outcome rows aggregated into `TelemetryBatch` records.
- `PlannerTraceProjection` — reconstructs planner runs from
  `PlanStarted` / `GoalConsidered` / `GoalScored` / `GoalSelected` /
  `PlanStepEmitted` / `PlanCommitted` / `PlanAbandoned` events.
- `KernelPerformanceRecorder` — per-tick timing aggregates into
  `PerformanceTelemetryBatch`.
- `Engine.Observability.Export` — Parquet/CSV writers for telemetry and
  Monte Carlo runs.

Observability never references kernel state directly. Everything flows
through the event log.

## Domain and Seeding

```
IceRemembers
  ├── IceRemembersDefinition.Create()  → DomainConfig
  ├── Kinds, Attributes, Relationships, Pressures, Resources
  ├── Generators, Degeneracy detectors, Gambits, Processes
  ├── Terrain catalog (Face, Shelf, Depths, Reach, Sea, Scar, …)
  └── IceRemembersEntityNameSynthesizer (for projections)

IceRemembers.Seed
  ├── IceRemembersKernelDomainModule : IKernelDomainModule
  │     · Name, CreateDomainConfig(), BuildStartupSeed(...),
  │       ConfigureProjectionSymbols(...)
  ├── IceRemembersStartupSeed : IKernelStartupSeeder
  │     · Lays down hex cells, regions, settlements, factions, people
  │     · Seeds initial relationships and ambient resources
  └── IceRemembersSeedQualityValidator
        · Hard-fails the build when initial conditions degrade
```

`KernelRuntimeFactory` resolves the requested domain by name, builds the
startup seed deterministically from `seedPolicy.ResolveBaseSeed()`, and
hands the resulting `DomainStartupSeed` to `KernelHostBuilder` via
`WithDomainStartupSeed`.

## Game Runtime Surface

`TheCanonryGame.Game.CanonryGameRuntime` is the only thing the Godot scene
talks to.

| Surface | What it offers |
| --- | --- |
| `ReadModels.Map` | `HexMapProjection` — physical/semantic plane cells, per-cell terrain, resources, occupants. |
| `ReadModels.Entities` | `UiProjectionState` — entity list, attributes, relationships, generator fires, current tick, world version. |
| `ReadModels.Chronicle` | `UiChronicleProjection` — narrative one-liners. |
| `ReadModels.Symbols` | `IProjectionSymbolCatalog` — ID → display name resolution. |
| `Controls` | `Pause`, `Resume`, `Step(ticks)`, `SetTickRate(ticksPerSecond)`. |
| `Intents.RequestGeneratorEnabled(generatorId, enabled)` | Submits a `SetGeneratorEnabled` command stamped with current world version. |
| `Tableau` | `GameRuntimeTableau` — generator card surface backed by intents and projection state. |
| `Update()` | Pumps consumers, returns `GameRuntimePulse(previousTick, currentTick)`. |
| `RunLogPath` | Path of the active run log on disk. |

`GameRuntimePulse.TicksProcessed > 0` is what the Godot scene watches each
frame to decide whether to refresh UI panels.

## Architectural Principles

These hold across the kernel split:

1. **No domain mixing.** Engine projects (`Engine.*`) never reference domain
   content. Domain content lives in `IceRemembers*`. Grep `src/Engine.*` for
   any domain term — the result must be empty.
2. **No runtime strings in the kernel.** All names compile to stable
   primitive IDs at startup. `Engine.Kernel` build-bans `string`,
   `StringBuilder`, and `FormattableString`.
3. **No hot-path allocations.** `Engine.Kernel` build-bans LINQ, common
   heap collections, async primitives, logging types, timing types, and
   I/O. Use preallocated arrays, ring buffers, spans, and fixed-capacity
   tables. Overflow is explicit and observable.
4. **Primitive event boundary.** The kernel emits primitive structs into
   `KernelEventLog`. Consumers resolve IDs to text and build rich objects
   off the sim thread.
5. **One sim writer thread.** Off-thread work is read-only over immutable
   primitive snapshots and may return primitive intent records for the sim
   thread to validate.
6. **Uniform evaluation.** All entity kinds run through the same program
   runtimes. They differ only in registered processes/gambits/generators
   and attribute schemas.
7. **Strict tick ordering.** Programs execute in registration order; later
   writes are visible to earlier programs only on the next tick.
8. **Wiring validation.** Coverage and feedback-loop checks run at domain
   compile time, not at tick time.
9. **Everything serializable.** `KernelSnapshot` round-trips kernel state,
   program state, and PRNG state through versioned binary.
10. **Everything visible.** No hidden storyteller. Projections render the
    same primitive events the run log records.

## Migration Status

The kernel split itself is in place. Open work:

- Planner / evaluator hot-path allocations.
- Replacement of remaining graph storage with stable-handle adjacency.
- Per-query cache → mutation-maintained versioned indexes with primitive
  keys.
- Telemetry/logging in observability still own some object allocation that
  should be moved to fixed buffers or pooled scratch.

See `docs/kernel/invarient_progress_tracking.md` for the live punch list
and `docs/kernel/kernel-architecture-migration-plan.md` for the ordered
plan.
