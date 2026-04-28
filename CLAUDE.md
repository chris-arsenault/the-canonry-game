# CLAUDE.md

## Project Overview

A high-altitude civilization simulation game with roguelite card mechanics. The player defines the world's nature through a card tableau: generators determine what the world becomes, systems determine how forces behave, actions determine what actors can do.

Built with Godot 4 + C#. First narrative domain: The Ice Remembers (penguin civilizations, orca threats, the Long Dark).

## Architecture Principles

These are inviolable. Every commit must respect them.

### Critical Reference: Simulation Kernel Discipline

Before changing engine runtime code, read `docs/kernel/simulation-kernel-discipline.md`.

That document is the controlling architecture direction for simulation-thread
work: no runtime string creation in the kernel, no runtime string lookup after
domain registration, no steady-state hot-path allocations, primitive event
boundaries, and one sim writer thread.

The kernel split is in place: `Engine.Kernel`, `Engine.Domain`,
`Engine.Eventing`, `Engine.Eventing.Schema`, `Engine.Host`,
`Engine.Kernel.Driver`, `Engine.Kernel.Schema`, `Engine.Projection`,
`Engine.Observability`, `Engine.Observability.Export`, `Engine.WorldGen`,
`Engine.Constants`. `Engine.Kernel/BannedSymbols.txt` enforces the no-string,
no-LINQ, no-allocation, no-async, no-I/O rules at build time.

Remaining migration work (planner/evaluator allocations, graph storage,
versioned indexes) is tracked in
`docs/kernel/invarient_progress_tracking.md`. Existing violations are
migration debt, not precedent. New work should move the engine toward the
kernel model and should not add avoidable new violations.

### 1. No Domain Mixing

Engine code NEVER references domain content. No penguins, no orcas, no soup, no ice magic in the engine. The engine knows about entity kinds, attributes, relationships, cards, and pressure. The domain module knows about specific subtypes, specific cards, specific naming rules, and specific ground truths.

Test: if you grep the engine codebase for any domain-specific term and find a match, that's a bug.

### 2. Systems Are Architecturally Separable

Each game system (entity graph, tick clock, entity behaviors, relationships, pressure, deck, tableau, card executors, etc.) is a separate module with a defined interface. Systems communicate through the entity graph and defined events, not through direct coupling.

A system can be:
- Tested in isolation (with mocked dependencies)
- Extended without modifying other systems
- Replaced with an alternative implementation
- Disabled for debugging without breaking the simulation

### 3. Uniform Evaluation — No Per-Kind Code

All entities go through the same evaluator. The engine has zero per-kind behavioral code. Entity kinds differ only in which processes are registered for them and what attributes they have. The evaluator selects goals, finds serving processes, and executes mutations identically for settlements, NPCs, nations, artifacts, and regions.

Physics (lifecycle drain, maintenance) runs unconditionally. Processes are goal-driven with sub-goal generation from blocked preconditions. Decisions are card-gated. Data flow is one-directional within a tick.

### 4. Everything Is Data and Process-Defined

Processes, decisions, and cards are data-driven: preconditions (returning PreconditionResult with diagnostics and blocking attributes), IExpression scoring, mutations with context variables, and wiring contracts with Self/Target scope. Provider scoring is process-defined via expressions — the engine does NOT hardcode distance, attribute comparisons, or any other scoring metric.

This enables:
- Domain-defined processes without engine changes
- Goals and sub-goals derived from attribute deficits and wiring contracts
- Provider scoring customized per process
- Wiring validation at startup
- Changing a process = changing data in the domain config, not recompiling

### 5. Entity Kinds Are Engine, Subtypes Are Domain

The engine defines entity kind classes: Region, Settlement, Nation, NPC, Artifact (MVP), plus post-MVP kinds. Each kind participates in the three-layer behavioral model (physics, processes, decisions).

The domain module defines: subtypes within each kind, attribute schemas per subtype, relationship kind catalog, pressure definitions, process definitions, decision definitions, and card content. A new domain can define entirely new subtypes, processes, and relationships without engine changes.

### 6. Everything Is Serializable

Every system must support full state serialization from day one. If a system introduces state that can't be serialized, that's a bug. Save/load is not a feature added later -- it's an architectural constraint that keeps systems honest about their state boundaries.

### 7. Everything Is Visible

No hidden simulation state. No hidden directors. No behind-the-scenes event curation. If the player (or developer) inspects any entity, relationship, pressure, or card, they see the full truth. Debug tools and player-facing UI read the same state.

### 8. Relationships: Topology From Cards, Strength From Physics

New relationships are created only by card execution (generators, actions). Low-altitude physics modifies the STRENGTH of existing relationships but never creates or destroys relationship links. Automatic culling (setting end date at low strength) is the one exception -- it's lifecycle, not physics.

### 9. Culture Is an Attribute

Culture is a static, domain-defined classification. It is NOT an entity kind. Entities have a culture attribute that determines naming, attribute tendencies, and cultural friction with different-culture neighbors. Culture does not act, spread, merge, or die. Entities of different cultures interact, creating dynamics enacted by nations and NPCs.

### 10. Typed Effect System

Attribute effects are **emitted by source entities** (e.g., an artifact granting a buff to its wielder, a region granting effects to agents at its hexes) and flow to affected entities along topological connections (a `Holds` edge, hex containment, etc.). Relationships and positional connections are the **paths effects flow along — not the carriers of effect data.** Effect magnitudes and types are defined on the source entity; relationship kinds themselves do not carry typed-effect data as edge properties.

Stacking follows the Pathfinder 2e typed bonus pattern:
- Effects are categorized by type (political, cultural, artifact, environmental, etc.)
- Same-type effects do NOT stack (take best bonus / worst penalty)
- Cross-type effects always stack
- Every effect carries source metadata (which source entity, via which connection)
- Effective attribute values are computed as: base + sum(filtered effects)

**Relationships are used separately — as graph topology for action/generator targeting, variable resolution, wiring, and mutation (filters like `has_relationship`, `lacks_relationship`, `graph_path`, etc.). Do not confuse the typed-effect system with the relationship catalog.**

### 11. Strict Tick Phase Ordering

Within a single tick, systems execute in a strict, defined order. Results from phase N are visible to phase N+1 but NOT to phase N. No system reaches backward within a tick. This ensures determinism and debuggability.

### 12. Anti-Degeneracy Is Structural

Anti-degeneracy mechanisms (shock absorbers, card responses, UI warnings) are architecturally committed, not bolted on after the fact. Each MVP degeneracy pattern has a codified detector. The detector system is a registry that new patterns can be added to.

## Architecture Decisions

### Entity Graph: QuikGraph, Not Homegrown
The entity graph uses **QuikGraph** (`QuikGraph` NuGet package) for graph data structures and traversal. **DO NOT reimplement graph traversal, adjacency lists, or query algorithms from scratch** -- this has proven problematic in past projects and is effectively writing a graph database.

QuikGraph provides: `BidirectionalGraph` for directed in/out adjacency, `TaggedEdge` for typed relationship properties, and built-in BFS/DFS. We wrap it with a `WorldGraph` class that adds: secondary indexes by entity kind, historical relationship filtering (active vs ended), and integration with the attribute system.

Save/load uses MessagePack-CSharp for binary serialization (fast, compact). SQLite may be added as a debugging persistence layer for ad-hoc save-file queries. See `docs/research/graph-library-evaluation.md`.

## Development Commands

```bash
make ci       # lint + build + test (run before every commit)
make lint     # Roslyn analyzer lint + format verification
make build    # restore + release build
make test     # run xunit tests
make clean    # remove .artifacts/
```

## Development Workflow

For each issue, follow this sequence exactly:

1. **Move ticket to In Progress** on the GitHub project board
2. **Write code** in the appropriate engine project under `src/Engine.*` (or
   the domain projects under `src/IceRemembers*`, or the application
   projects under `src/TheCanonryGame.*`). See "What belongs where" below.
3. **Write tests** in the matching `tests/<project>.Tests/` directory,
   mirroring the source structure.
4. **Run `make ci`** -- must pass clean (0 warnings, 0 errors, all tests green)
5. **Domain contamination check:** `grep -ri 'penguin\|orca\|soup\|ice magic\|long dark\|frost' src/Engine.*/` must return zero matches. Engine projects must never reference domain content.
6. **Validate acceptance criteria** from the issue against the implementation
7. **Commit** with message referencing the issue: `git commit -m "Description (#N)"`. Use `Closes #N` in the body if the issue is fully complete.
8. **Update the issue** with commit hash and any notes. Move ticket to Done.

### Commit conventions
- Commit to `main` directly (no branches for now)
- Reference the issue number in every commit message
- One logical change per commit (don't bundle unrelated work)
- `make ci` must pass before pushing. Do not push broken builds.

### What belongs where

Engine projects (no domain terms, no Godot, no presentation):

- `src/Engine.Kernel/` — deterministic tick mutation, fixed buffers, primitive
  IDs only. Build-banned: strings, LINQ, allocating collections, async,
  logging, I/O. Reference only `Engine.Constants` and `Engine.Eventing`.
- `src/Engine.Constants/` — primitive ID code catalogs. No external deps.
- `src/Engine.Eventing/` — bounded primitive event log with independent
  cursors.
- `src/Engine.Eventing.Schema/` and `src/Engine.Eventing.Generators/` —
  runtime schema registry and source generators for event layouts.
- `src/Engine.Domain/` — domain registration and compilation. Strings allowed
  here as input/metadata; emits `CompiledDomain` with stable IDs.
- `src/Engine.WorldGen/` — hex grid, terrain catalog, semantic plane, ambient
  resource layout.
- `src/Engine.Kernel.Driver/` — async edge: realtime/stepped/unpaced drivers,
  time control, command submission/completion.
- `src/Engine.Kernel.Schema/` — kernel event semantic helpers.
- `src/Engine.Projection/` — UI read models (`HexMapProjection`,
  `UiProjectionState`, `UiChronicleProjection`, symbol catalog, name
  synthesis). Consumes primitive events.
- `src/Engine.Observability/` — telemetry, run logs, planner traces, perf
  recorders. Consumes primitive events.
- `src/Engine.Observability.Export/` — Parquet/CSV exporters.
- `src/Engine.Host/` — wires kernel, domain, eventing, projections, and
  observability into a `KernelHost` runtime via `KernelHostBuilder` and
  `KernelRuntimeFactory`.

Domain projects (`The Ice Remembers` content; never referenced by `Engine.*`):

- `src/IceRemembers/` — `DomainConfig`: kinds, attributes, processes,
  gambits, generators, relationships, pressures, degeneracy detectors,
  terrain, name synthesis.
- `src/IceRemembers.Seed/` — `IKernelDomainModule` + `IKernelStartupSeeder`
  that places the initial world.

Application projects (Godot game and UI):

- `src/TheCanonryGame.App/` — Godot autoload script (`Main.cs`).
- `src/TheCanonryGame.Game/` — runtime facade: `CanonryGameRuntime`,
  read models, controls, intent port, tableau surface.
- `src/TheCanonryGame.UI/` — Godot scene scripts.
- `src/TheCanonryGame.UI.Core/` — theme tokens and UI primitives.

Tests live under `tests/<project>.Tests/` mirroring the source layout
(`Engine.Kernel.Tests`, `Engine.Domain.Tests`, `Engine.Eventing.Tests`,
`Engine.Host.Tests`, `Engine.Kernel.Driver.Tests`, `Engine.Projection.Tests`,
`Engine.Observability.Tests`, `Engine.WorldGen.Tests`,
`IceRemembers.Seed.Tests`, `TheCanonryGame.Game.Tests`,
`TheCanonryGame.UI.Tests`, plus `MonteCarlo.Tests` for batch runs).

## Repository Structure

```
src/
  Engine.Constants/            # Primitive ID code catalogs
  Engine.Domain/               # Domain registration + DomainCompiler
  Engine.Eventing/             # Bounded primitive event log
  Engine.Eventing.Generators/  # Source generators for event layouts
  Engine.Eventing.Schema/      # Runtime event schema registry
  Engine.Host/                 # KernelHostBuilder, KernelRuntimeFactory
  Engine.Kernel/               # Deterministic tick (BannedSymbols.txt)
  Engine.Kernel.Driver/        # Realtime/stepped/unpaced drivers, commands
  Engine.Kernel.Schema/        # Kernel event semantic helpers
  Engine.Observability/        # RunLogger, telemetry, planner trace, perf
  Engine.Observability.Export/ # Parquet/CSV exporters
  Engine.Projection/           # UI read models, symbol catalog
  Engine.WorldGen/             # Hex grid, terrain, semantic plane
  IceRemembers/                # Domain content (DomainConfig)
  IceRemembers.Seed/           # Domain module + startup seeder
  TheCanonryGame.App/          # Godot autoload (Main.cs)
  TheCanonryGame.Game/         # Runtime facade
  TheCanonryGame.UI/           # Godot scene scripts
  TheCanonryGame.UI.Core/      # Theme tokens
tests/
  <Project>.Tests/             # xUnit tests mirroring src/
  MonteCarlo.Tests/            # Batch simulation regressions
scenes/
  Main.tscn                    # Godot entry scene
docs/
  architecture.md              # Microkernel architecture (read this first)
  game-systems.md              # System overview and MVP priority
  kernel/                      # Kernel discipline + migration tracking
  systems/                     # Per-system design docs (01-19)
  design/                      # Functional design areas
    principles.md              # Central thesis, core principles, what we're not
    milestones.md              # MVP / Tech Demo / Early Access
    anti-degeneracy.md         # Three-layer anti-degeneracy mechanism
    entities.md                # Kinds, attributes, RPS verb ring, relationships
    generators.md              # Generator system + current catalog
    loops.md                   # Active gameplay loops
    asset-prompts.md           # Art-asset reference prompts
    test-requirements.md       # Engine test coverage targets
    post-mvp-backlog.md        # Deferred ideas
  lore/                        # The Ice Remembers world reference
    lore-bible.md
    eras/                      # Era frames (great thaw, faction wars, ...)
    encyclopedia/              # Per-kind reference (artifact, nation, ...)
  research/                    # Research corpus, organized by area
    requirements.md            # Original requirements summary
    tech-stack.md              # Engine/language evaluation
    exemplars.md               # Mechanical analysis of inspirational games
    cards/                     # Card-system research
    simulation/                # Process / behavior / planning research
    graph/                     # Graph storage and library evaluation
    pacing/                    # Tempo, attention, lose conditions
    modes/                     # Patron / favored-nation research
```

## Key Design Decisions

- **Central thesis:** "The world has physics that creates conditions. Your deck defines the world's nature."
- **MVP entity kinds:** Region, Settlement, Nation, NPC, Artifact (5 of 11 total)
- **Resources:** Gold and Soup on the map graph
- **Card removal:** Costly (hand card sacrifice + penalty), not free
- **Deck model:** Deck > hand > tableau, cycling, archetype-based starting decks
- **No hidden storyteller:** All simulation state visible
- **Contemplative tempo:** SimCity/Stellaris pacing, not RTS or crisis management
