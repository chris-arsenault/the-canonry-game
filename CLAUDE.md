# CLAUDE.md

## Project Overview

A high-altitude civilization simulation game with roguelite card mechanics. The player defines the world's nature through a card tableau: generators determine what the world becomes, systems determine how forces behave, actions determine what actors can do.

Built with Godot 4 + C#. First narrative domain: The Ice Remembers (penguin civilizations, orca threats, the Long Dark).

## Architecture Principles

These are inviolable. Every commit must respect them.

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

Relationship-derived attribute effects follow the Pathfinder 2e typed bonus pattern:
- Effects are categorized by type (political, cultural, artifact, environmental, etc.)
- Same-type effects do NOT stack (take best bonus / worst penalty)
- Cross-type effects always stack
- Every effect carries source metadata (which relationship, which entity)
- Effective attribute values are computed as: base + sum(filtered effects)

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
make lint     # build with -warnaserror (all analyzers at AnalysisMode=All)
make build    # restore + release build
make test     # run xunit tests
make clean    # remove .artifacts/
```

## Development Workflow

For each issue, follow this sequence exactly:

1. **Move ticket to In Progress** on the GitHub project board
2. **Write code** in the appropriate `src/Engine/` subfolder
3. **Write tests** in `tests/Engine.Tests/` mirroring the source structure
4. **Run `make ci`** -- must pass clean (0 warnings, 0 errors, all tests green)
5. **Domain contamination check:** `grep -ri 'penguin\|orca\|soup\|ice magic\|long dark\|frost' src/Engine/` must return zero matches. Engine code must never reference domain content.
6. **Validate acceptance criteria** from the issue against the implementation
7. **Commit** with message referencing the issue: `git commit -m "Description (#N)"`. Use `Closes #N` in the body if the issue is fully complete.
8. **Update the issue** with commit hash and any notes. Move ticket to Done.

### Commit conventions
- Commit to `main` directly (no branches for now)
- Reference the issue number in every commit message
- One logical change per commit (don't bundle unrelated work)
- `make ci` must pass before pushing. Do not push broken builds.

### What belongs where
- `src/Engine/` -- domain-agnostic simulation engine only. No domain terms, no Godot UI.
- `tests/Engine.Tests/` -- unit tests for engine code. Mirror the source folder structure.
- `src/Domain/` -- narrative domain content (Phase 2, not yet created)
- `src/UI/` -- Godot scenes and visualization (not yet created)

## Repository Structure

```
docs/                          # Design documentation
  design-principles.md         # Central thesis and core principles
  design-constraints.md        # What the game is NOT
  game-systems.md              # System overview and MVP priority
  anti-degeneracy.md           # Structural anti-degeneracy mechanisms
  milestones.md                # MVP / Tech Demo / Early Access definitions
  post-mvp-backlog.md          # Deferred ideas
  systems/                     # Per-system design docs (01-19)
  research/                    # Research corpus (exemplars, card mechanics, etc.)
```

## Key Design Decisions

- **Central thesis:** "The world has physics that creates conditions. Your deck defines the world's nature."
- **MVP entity kinds:** Region, Settlement, Nation, NPC, Artifact (5 of 11 total)
- **Resources:** Gold and Soup on the map graph
- **Card removal:** Costly (hand card sacrifice + penalty), not free
- **Deck model:** Deck > hand > tableau, cycling, archetype-based starting decks
- **No hidden storyteller:** All simulation state visible
- **Contemplative tempo:** SimCity/Stellaris pacing, not RTS or crisis management
