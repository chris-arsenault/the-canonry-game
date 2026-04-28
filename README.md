# The Canonry Game

A high-altitude civilization simulation with roguelite card mechanics. The
player does not command the world — they define its **nature** through a card
tableau. Generators determine what the world becomes, systems determine how
forces behave, actions determine what actors can do. The simulation runs
underneath; the deck is a theory the player tests against emergent reality.

Built on Godot 4.6 + .NET 8, with a deterministic C# microkernel hosted by the
Godot game runtime. First narrative domain: **The Ice Remembers** — penguin
civilizations on Aurora Berg, orca threats, the Long Dark.

> Status: the engine has been migrated from a monolithic `Engine` project into
> a microkernel split. Project boundaries, primitive event log, stable-ID
> domain compilation, and async projection consumers are in place. Hot-path
> allocation cleanup in the planner/evaluator and graph storage is ongoing —
> see `docs/kernel/invarient_progress_tracking.md`.

## Architecture at a Glance

```
                        ┌──────────────────────────┐
                        │  TheCanonryGame.App      │  Godot autoload (Main.cs)
                        │  scenes/Main.tscn        │
                        └──────────────┬───────────┘
                                       │ owns
                        ┌──────────────▼───────────┐
                        │  TheCanonryGame.Game     │  CanonryGameRuntime
                        │  GameRuntimeReadModels   │  GameIntentPort
                        │  GameRuntimeControls     │  GameRuntimeTableau
                        └──────────────┬───────────┘
                                       │ wires
            ┌──────────────────────────▼────────────────────────────┐
            │                  Engine.Host                           │
            │  KernelHostBuilder · KernelRuntimeFactory · KernelHost │
            └──┬──────────────┬─────────────┬──────────────┬────────┘
               │              │             │              │
               ▼              ▼             ▼              ▼
       Engine.Domain    Engine.Kernel   Engine.Eventing  Engine.Projection
       (compile to IDs) (deterministic   (primitive       (UI read models,
                         tick, fixed       append-only      symbols, names)
                         buffers, IDs      event log,
                         only)             cursors)
                              │
                              │ also writes events to
                              ▼
                       Engine.Observability  (run logs, telemetry,
                                              planner traces, perf,
                                              Monte Carlo export via
                                              Engine.Observability.Export)
```

The kernel is the only state mutator. Everything downstream (UI projections,
run logs, telemetry, exports) is an **independent async consumer** of a
primitive event stream with its own cursor. A slow consumer cannot stall the
kernel or another consumer.

See `docs/architecture.md` for the full picture.

## Project Layout

### Engine (`src/Engine.*`)

| Project | Role |
| --- | --- |
| `Engine.Constants` | Primitive ID code catalog: program codes, status codes, operation codes, expression codes, polarity codes. No external dependencies. |
| `Engine.Domain` | Stable-ID registry and domain compilation. Strings allowed as input; emits `CompiledDomain` containing `DomainIdRegistry`, kernel data layout, program models, and projection symbols. |
| `Engine.Kernel` | Deterministic simulation core. Fixed-capacity buffers, primitive IDs only, single writer thread. Build-banned: strings, LINQ, allocating collections, async, logging, I/O. |
| `Engine.Kernel.Driver` | Time control (`IKernelDriverTimeControl`), command submission (`ICommandSubmitter`), realtime/stepped/unpaced drivers. The async edge of the kernel. |
| `Engine.Kernel.Schema` | Helpers for kernel event semantic kinds and validation. |
| `Engine.Eventing` | Bounded ring-buffer `KernelEventLog` with independent `KernelEventCursor` read heads and explicit overflow accounting. |
| `Engine.Eventing.Generators` | Roslyn source generators for `KernelEvent` layout. |
| `Engine.Eventing.Schema` | Runtime event schema registry (`KernelEventSchemaRegistry`). |
| `Engine.Projection` | UI-facing read models built from primitive events: `HexMapProjection`, `UiProjectionState`, `UiChronicleProjection`, `ProjectionSymbolCatalog`, `ProjectionEntityNameSynthesis`. |
| `Engine.Observability` | Telemetry/performance/run-log consumers: `TelemetryEventProjection`, `PlannerTraceProjection`, `RunLogger`, `KernelTelemetryRecorder`, `KernelPerformanceRecorder`. |
| `Engine.Observability.Export` | Parquet/CSV exporters for telemetry and Monte Carlo output. |
| `Engine.Host` | Wires kernel + domain + eventing + projections + observability into a runtime. `KernelHostBuilder`, `KernelHost`, `KernelRuntimeFactory`, `KernelStartupContext`, `IKernelDomainModule`. |
| `Engine.WorldGen` | Procedural hex grid, terrain catalog, semantic plane coordinates, ambient resource layout. No external dependencies. |

### Domain (`src/IceRemembers*`)

| Project | Role |
| --- | --- |
| `IceRemembers` | The Ice Remembers `DomainConfig`: kinds, attributes, processes, gambits, generators, relationships, pressures, degeneracy detectors, terrain, name synthesis. Compiled to primitive IDs by `DomainCompiler`. |
| `IceRemembers.Seed` | Startup seeding (`IKernelDomainModule` + `IKernelStartupSeeder`): places initial regions/settlements/factions/people on the map and seeds initial relationships. |

### Application (`src/TheCanonryGame.*`)

| Project | Role |
| --- | --- |
| `TheCanonryGame.App` | Godot scene script (`Main.cs`). Cached scene nodes, input handling, camera, runtime lifecycle. |
| `TheCanonryGame.Game` | Runtime facade: `CanonryGameRuntime`, `CanonryGameRuntimeFactory`, `GameRuntimeReadModels`, `GameRuntimeControls`, `GameIntentPort`, `GameRuntimeTableau`, `GameSeedPolicy`. |
| `TheCanonryGame.UI` | Godot scene scripts for the gameplay UI: `HexMapRenderer`, `EntityLayer`, `EntityBrowser`, `EntityCardLayer`, `TopBar`, `BottomBar`, `ChroniclePanel`, `TableauPanel`, `AttributeGraphPanel`. |
| `TheCanonryGame.UI.Core` | Theme tokens and UI primitives (`UiTokens`, `UiTheme`). |

### Tests (`tests/*`)

Each engine project has a corresponding `*.Tests` xUnit project mirroring the
source layout. `MonteCarlo.Tests` runs batch simulations against the kernel for
regression coverage and performance baselines.

## Tick Phase Order

`Engine.Kernel/Simulation.cs` registers the following programs in this exact
order. Each program runs at its registered cadence; results from program N are
visible to program N+1, but later writes are not visible to earlier programs
until the next tick.

| # | Program | Cadence | Project location |
| --- | --- | --- | --- |
| 1 | Upkeep | every tick | `Engine.Kernel/Programs/Upkeep` |
| 2 | Relationships | every tick | `Engine.Kernel/Programs/Relationships` |
| 3 | Processes | every tick | `Engine.Kernel/Programs/Processes` |
| 4 | Gambits | every tick | `Engine.Kernel/Programs/Gambits` |
| 5 | Pressures | every 5 ticks | `Engine.Kernel/Programs/Pressures` |
| 6 | Generators | every tick | `Engine.Kernel/Programs/Generators` |
| 7 | Degeneracy | every 10 ticks | `Engine.Kernel/Programs/Degeneracy` |

Planning runtime (`Engine.Kernel/Programs/Planning`) is wired but currently
invoked from inside the Processes program rather than as a top-level scheduled
program.

## Building & Running

```bash
make ci         # lint + build + test  (run before every commit)
make build      # restore + release build
make test       # run xunit tests
make lint       # Roslyn analyzer lint + format verification
make clean      # remove .artifacts/
```

To play in Godot:

1. Open the project in Godot 4.6.
2. Run `scenes/Main.tscn`.
3. Controls: `Space` pause/resume, `Right` step one tick, `1`/`2`/`3` set tick
   rate (1/5/10 ticks per second), `R` reset with new seed, `N` toggle entity
   names, `G` toggle attribute readings, `WASD` pan, scroll to zoom.

The Godot game starts a `CanonryGameRuntime` via
`CanonryGameRuntimeFactory.Start(seedPolicy)`. The factory registers the
IceRemembers domain module, builds a `KernelHost` with realtime driver
config, and exposes read models, time controls, and an intent port.

## Documentation

```
docs/
  architecture.md        # microkernel architecture and runtime composition
  game-systems.md        # system overview and MVP priority
  kernel/                # kernel discipline + migration tracking
  systems/               # per-system design docs (01–19)
  design/                # functional design areas
  lore/                  # The Ice Remembers world reference
  research/              # background research that informed the design
```

### Architecture and engine

- [`docs/architecture.md`](docs/architecture.md) — System architecture:
  project topology, runtime composition, kernel tick, event boundary,
  driver / commands, persistence, observability, and domain compilation.
- [`docs/game-systems.md`](docs/game-systems.md) — System overview and
  MVP priority order. The vertical slices that make up the simulation.
- [`docs/kernel/simulation-kernel-discipline.md`](docs/kernel/simulation-kernel-discipline.md)
  — Controlling architecture direction for sim-thread work. Read this
  before changing anything inside `Engine.Kernel`.
- [`docs/kernel/kernel-architecture-migration-plan.md`](docs/kernel/kernel-architecture-migration-plan.md)
  — The ordered migration steps from the legacy `Engine` to the kernel
  split.
- [`docs/kernel/invarient_progress_tracking.md`](docs/kernel/invarient_progress_tracking.md)
  — Status of remaining migration work against the kernel invariants.
- [`docs/systems/`](docs/systems/) — Per-system design docs (entity graph,
  attributes, map, tick clock, relationships, pressure, deck, tableau,
  generators, etc.). Numbered 01–19.
- [`MULTI_STEP.md`](MULTI_STEP.md) — How multi-step behavior emerges
  from the goal / sub-goal model rather than hardcoded chains.

### Design

Functional design areas — current and future state. Each file is the
canonical design for one area.

- [`docs/design/principles.md`](docs/design/principles.md) — Central
  thesis, the nine core principles, what this game is not, and the
  engine/domain boundary.
- [`docs/design/milestones.md`](docs/design/milestones.md) —
  MVP / Tech Demo / Early Access definitions of "done".
- [`docs/design/anti-degeneracy.md`](docs/design/anti-degeneracy.md) —
  Three-layer anti-degeneracy model (shock absorbers, card responses, UI
  warnings) and the per-pattern playbook.
- [`docs/design/entities.md`](docs/design/entities.md) — Entity kinds,
  attributes, the cross-entity RPS verb ring, and the relationship
  catalog.
- [`docs/design/generators.md`](docs/design/generators.md) — What
  generators are, the pulse model, hard invariants, the subtype
  proposal, and the current authored catalog.
- [`docs/design/loops.md`](docs/design/loops.md) — Active gameplay loops
  being built into the prototype.
- [`docs/design/asset-prompts.md`](docs/design/asset-prompts.md) —
  Art-asset reference prompts for the chronicle / map / UI.
- [`docs/design/test-requirements.md`](docs/design/test-requirements.md)
  — Engine test coverage targets. Some `Location:` paths still reference
  pre-kernel-split files; verify before writing tests.
- [`docs/design/post-mvp-backlog.md`](docs/design/post-mvp-backlog.md) —
  Deferred ideas.

### Lore (The Ice Remembers)

- [`docs/lore/lore-bible.md`](docs/lore/lore-bible.md) — World, peoples,
  geography, ground truths.
- [`docs/lore/eras/`](docs/lore/eras/) — Era frames:
  [the Great Thaw](docs/lore/eras/the-great-thaw.md),
  [the Faction Wars](docs/lore/eras/the-faction-wars.md),
  [the Clever Ice Age](docs/lore/eras/the-clever-ice-age.md),
  [the Orca Incursion](docs/lore/eras/the-orca-incursion.md).
- [`docs/lore/encyclopedia/`](docs/lore/encyclopedia/) — Per-kind
  reference material (artifact, nation, npc, region, settlement).

### Research

Background research that informed the design — kept so the reasoning is
visible without re-deriving it. Designs that crystallized out of this
research live under `docs/design/`.

- [`docs/research/`](docs/research/) — top-level
  ([requirements](docs/research/requirements.md),
  [tech-stack](docs/research/tech-stack.md),
  [exemplars](docs/research/exemplars.md)) plus
  [`cards/`](docs/research/cards/),
  [`simulation/`](docs/research/simulation/),
  [`graph/`](docs/research/graph/),
  [`pacing/`](docs/research/pacing/),
  [`modes/`](docs/research/modes/).

### Working notes

- [`CLAUDE.md`](CLAUDE.md) / [`CODEX.md`](CODEX.md) — Agent
  collaboration instructions and architectural guardrails.

## Architectural Principles

These remain inviolable across the kernel split:

1. **No domain mixing.** Engine projects never reference domain content. Grep
   `src/Engine.*` for any IceRemembers term and the result must be empty.
2. **No runtime strings in the kernel.** All domain names compile to stable
   primitive IDs at startup. The kernel does no string work — `string`,
   `StringBuilder`, and `FormattableString` are build-banned in
   `Engine.Kernel`.
3. **No hot-path allocations.** No LINQ, no `List<T>`/`Dictionary<T,U>`/
   `HashSet<T>`/`Queue<T>`/`Stack<T>`, no `Task`, no `Stopwatch`, no `File`/
   `Directory` in `Engine.Kernel`. Use preallocated arrays, ring buffers,
   spans, and fixed-capacity tables. Overflow is explicit.
4. **Primitive event boundary.** The kernel emits primitive structs into a
   bounded log. Consumers resolve IDs to text and build rich objects on their
   own threads.
5. **One sim writer thread.** Off-thread work is read-only over immutable
   primitive snapshots and may return primitive intent records for the sim
   thread to validate.
6. **Uniform evaluation.** All entity kinds go through the same program
   runtimes. Entity kinds differ only in registered processes/gambits/
   generators and attribute schemas.
7. **Everything serializable.** `KernelSnapshot` round-trips the full kernel
   state (programs, world state, random source) through versioned binary.
8. **Everything visible.** No hidden storyteller. Projections render the same
   primitive events the run log records.

See `CLAUDE.md` and `docs/kernel/simulation-kernel-discipline.md` for the
authoritative versions of these rules.
