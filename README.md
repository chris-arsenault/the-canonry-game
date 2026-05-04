# The Canonry Game

A high-altitude civilization simulation with roguelite card mechanics. The
player does not command the world directly; they define its nature through a
card tableau. Low-altitude physics keeps the world alive, gambits let entities
take meaningful actions, and generators create new named entities when the
world state warrants them.

Built on Godot 4.6 and .NET 8. First narrative domain: **The Ice Remembers**.

## Architecture

The runtime is a deterministic C# microkernel hosted by Godot. The kernel is
the only state mutator. UI projections, run logs, telemetry, exports, and
Monte Carlo analysis consume primitive events through independent cursors.

```text
TheCanonryGame.App      Godot entrypoint
TheCanonryGame.Game     runtime facade and player intent port
TheCanonryGame.UI       Godot UI scripts

Engine.Host             runtime wiring and startup
Engine.Domain           domain ids, compilation, definition graph
Engine.Gameplay         reusable gameplay vocabulary and validation
Engine.Kernel           deterministic mutation, fixed buffers, primitive ids
Engine.Eventing         primitive event log and schema
Engine.Projection       UI/read-model projections
Engine.Observability    telemetry, run logs, parquet export inputs
Engine.WorldGen         hex grid, terrain, coverage, startup map generation

IceRemembers            domain content
IceRemembers.Seed       startup seed module
```

See [docs/architecture.md](docs/architecture.md) and
[docs/kernel/simulation-kernel-discipline.md](docs/kernel/simulation-kernel-discipline.md).

## Current Tick Programs

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

The removed low-altitude process deck and macro aggregate layer are not part of
the current runtime. Gambits now execute opportunistically from shared
query/effect/decision primitives instead of routing through planner
infrastructure, and those primitives are also used by generators, traits, and
low-altitude trait modifiers.

## Commands

```bash
make ci         # lint + build + test
make build      # restore + release build
make test       # run xunit tests
make lint       # analyzer lint + format verification
make clean      # remove .artifacts/
```

`make test` runs the already-built Release test outputs. Use `make build`
first after a clean checkout or after package cache cleanup.

For targeted observability and Monte Carlo changes:

```bash
dotnet test tests/Engine.Observability.Tests/Engine.Observability.Tests.csproj -c Release
dotnet test tests/MonteCarlo.Tests/MonteCarlo.Tests.csproj -c Release
```

The default test suite excludes the long Monte Carlo output-gap acceptance
ledger. Run it explicitly when changing Monte Carlo parquet output contracts,
including raw event tables or derived analysis parquet:

```bash
CANONRY_RUN_MONTE_CARLO_ACCEPTANCE=1 dotnet test tests/MonteCarlo.Tests/MonteCarlo.Tests.csproj -c Release
```

The output-gap ledger in
`tests/MonteCarlo.Tests/Observability/MonteCarloOutputGapTests.cs` is the
contract source for canonical raw and analysis parquet outputs. Road behavior
is covered there as `cell_road_events`, `road_creation`, `road_usage`, and
`road_reinforcement`; prioritization analysis is intentionally excluded until
candidate path/cost choice telemetry exists.

To play in Godot:

1. Open the project in Godot 4.6.
2. Run `scenes/Main.tscn`.
3. Controls: `Space` pause/resume, `Right` step one tick, `1`/`2`/`3` set tick
   rate, `R` reset with new seed, `N` toggle names, `G` toggle readings,
   `WASD` pan, scroll to zoom.

## Documentation

- [docs/game-systems.md](docs/game-systems.md): current system overview.
- [docs/systems/simulation-principles.md](docs/systems/simulation-principles.md):
  durable simulation quality goals and analysis criteria.
- [docs/systems/06-entity-behaviors.md](docs/systems/06-entity-behaviors.md):
  current low-altitude verbs and gambits boundary.
- [docs/systems/16-generator-executor.md](docs/systems/16-generator-executor.md):
  generator model.
- [docs/systems/18-action-executor.md](docs/systems/18-action-executor.md):
  gambit/action model.
- [docs/kernel/shared-query-effect-executor.md](docs/kernel/shared-query-effect-executor.md):
  shared query, effect, and decision-modifier execution boundary.
- [docs/lore/lore-bible.md](docs/lore/lore-bible.md): Ice Remembers world
  reference.

Research under `docs/research/` is historical background. The implemented
architecture is described by `docs/architecture.md`, `docs/game-systems.md`,
and the numbered systems docs.

## Architecture Rules

Engine projects must remain domain-agnostic. Domain content compiles to stable
ids before it reaches the kernel.

`Engine.Kernel` cannot use runtime strings, LINQ, allocation-heavy collections,
async, threads, console/file I/O, or presentation objects. It mutates fixed
state and emits primitive events.

Gameplay vocabulary belongs in `Engine.Gameplay`, domain content belongs in
`IceRemembers`, and the kernel executes primitive compiled data.

Everything important must be visible through events, projections, snapshots, or
telemetry. No hidden storyteller and no uninspectable simulation state.
