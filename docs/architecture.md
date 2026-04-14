# System Architecture

## Overview

```
┌─────────────────────────────────────────────────────┐
│                    Godot Layer                       │
│  Main.cs → HexMapRenderer, EntityDetailPanel        │
│  (Renders simulation state, handles input)           │
└──────────────────────┬──────────────────────────────┘
                       │ reads
┌──────────────────────▼──────────────────────────────┐
│              SimulationRunner                         │
│  (Wires all systems, owns tick clock)                │
└──────────────────────┬──────────────────────────────┘
                       │ orchestrates
┌──────────────────────▼──────────────────────────────┐
│                   TickClock                           │
│  Single clock, strict phases per tick                │
│  Systems register with phase + cadence               │
└──────────────────────┬──────────────────────────────┘
                       │
     ┌─────────────────┼─────────────────────┐
     ▼                 ▼                     ▼
Phases 1-3         Phase 4              Phases 5-8
LOW ALTITUDE       BRIDGE              MID ALTITUDE
```

## Phase Execution (Per Tick)

**Phase 1 — Physics (automatic, unconditional):**
```
LifecycleSystem         → Per-kind aging drain on health metrics, death transitions
RelationshipSystem      → Strength modifiers (proximity, culture, competing claims), auto-culling
ResourceSystem          → Sustenance + gold production/consumption, population dynamics, intra-national flow
```

**Phase 2 — Processes (utility AI, always available, one per entity per tick):**
```
EntityEvaluator (Layer 2)
  → For each active entity, score all viable processes via utility AI
  → Weighted random selection from ALL viable (no top-N cap)
  → Execute selected process's mutations
  → Processes are data-defined in DomainConfig (ProcessDefinition with Role=Process)
  → Multi-effect and cross-entity (NPC eat reduces hunger AND settlement food)
```

**Phase 3 — Decisions (utility AI, card-gated, one per entity per tick):**
```
EntityEvaluator (Layer 3)
  → For each active entity, score all viable decisions (card-gated only)
  → Weighted random selection, same as processes
  → Execute selected decision's mutations
  → Decisions are data-defined (ProcessDefinition with Role=Decision, EnablingCardId)
  → Consume population. Produce event log entries.
```

**Phase 4 — Bridge (every 5 ticks):**
```
PressureSystem          → Computes 4 regional pressures from low-altitude outputs
  ├── Military tension (nation count, rivalries - alliances)
  ├── Economic stress (low-prosperity settlements)
  ├── Cultural friction (culture diversity)
  └── Demographic pressure (settlement density)
```

**Phase 5 — Mid Altitude (card-controlled):**
```
TableauManager          → Evaluates active generator/system cards, fires when preconditions met
  └── CardExecutor      → Precondition trees (AND/OR), mutation execution
```

**Phase 6 — Observability (every 10 ticks):**
```
DetectorRegistry        → 3 degeneracy detectors → EventLog warnings
```

**Wiring Validation (startup only):**
```
WiringValidator         → Checks all process/decision contracts for coherent feedback loops
  ├── No dead-end attributes (written but never read)
  ├── No orphan attributes (read but never written)
  ├── Every entity kind has at least one feedback cycle
  └── Every entity kind with health has a sustaining path
```

## Data Architecture

```
WorldGraph (QuikGraph BidirectionalGraph)
  ├── Entity: id, kind, subtype, name, status, culture, coordinates
  ├── Relationship: kind, polarity, strength 0-1, start/end dates
  ├── Secondary index: Dictionary<EntityKind, HashSet<EntityId>>
  └── Historical: culled relationships retained with end dates

AttributeStore
  ├── Per-entity scalars: (EntityId, name) → float, clamped to min/max
  └── Rate-of-change ring buffer for trend display

EffectSystem (PF2e stacking)
  ├── Same-type: take best bonus / worst penalty
  ├── Cross-type: always stack
  └── Indexed by target AND source for auto-cleanup

SemanticPlaneStore
  ├── 2D continuous positions per entity per plane
  ├── 3x3 reducer → 9 categorical labels
  └── Euclidean distance for disposition computation

ResourceLayer (map-graph properties)
  ├── Per-cell: production, consumption, stockpile
  └── Three layers: Sustenance, Gold, Population

DomainConfig (data-defined, loaded at startup)
  ├── Kinds: per-kind attribute schemas, subtypes, health attributes
  ├── Processes: ProcessDefinition with Role=Process (always available)
  ├── Decisions: ProcessDefinition with Role=Decision (card-gated)
  ├── Cards: CardDefinition (generator, system, action)
  ├── Relationships: RelationshipConfig (valid categories, polarity)
  ├── Pressures: PressureConfig (scope, homeostasis, smoothing)
  ├── Resources: ResourceConfig
  └── Cultures: CultureConfig
```

## Three-Layer Behavioral Model

```
Per entity per tick:

Layer 1: PHYSICS (automatic)
  LifecycleSystem, RelationshipSystem, ResourceSystem
  Not scored, not selected. Runs unconditionally.
  Produces: entropy, drift, flow.

Layer 2: PROCESSES (utility AI, always available)
  Data-defined ProcessDefinitions interpreted by DataDrivenProcess.
  One selected per entity per tick via weighted random.
  ALL entity kinds participate. Cross-entity mutations.
  Produces: conditions, tendencies, incremental changes.
  Does NOT produce outcomes.

Layer 3: DECISIONS (utility AI, card-gated)
  Data-defined ProcessDefinitions interpreted by DataDrivenDecision.
  One selected per entity per tick. Only when enabling card is active.
  ALL entity kinds participate. Consume population.
  Produces: new relationships, entities, major attribute shifts.
  Nearly always produces outcomes.

ProcessDefinition (shared DSL for processes and decisions):
  ├── Id, Name, Role (Process | Decision), TargetKind
  ├── EnablingCardId (null for processes, card ID for decisions)
  ├── Precondition: composable AND/OR tree
  ├── ScoreExpression: data-defined formula with variance
  ├── Mutations: ordered list with variance
  │     ModifySelfAttribute, ModifyRelatedAttribute (cross-entity),
  │     CreateEntity, CreateRelationship, ModifyPressure, SetTag
  ├── Reads: HashSet<AttributeRef> (wiring contract)
  └── Writes: HashSet<AttributeRef> (wiring contract)
```

## Entity Model

```
5 MVP Kinds (of 11 total):
  Region    (Spatial)  — permanent, pressure scoping, no lifecycle drain
  Settlement (Spatial) — population-based lifecycle, resource production
  Nation    (Agency)   — cohesion-based lifecycle, border tension
  NPC       (Agency)   — HP-based lifecycle, hunger/movement
  Artifact  (Asset)    — condition-based lifecycle, resonance

Category-level relationship taxonomy (4x4, not 11x11):
  Spatial×Spatial: borders, contains
  Agency×Spatial: controlled_by, located_at, home_base
  Agency×Agency: allied_with, rival_of, led_by, member_of, loyal_to
  Agency×Asset: holds, created_by
  Asset×Spatial: stored_at
  All×All: involved_in
```

## Card System

```
CardDefinition (mutable data, not code)
  ├── Type: Generator | System | Action
  ├── Precondition tree: AND/OR of leaf conditions
  │     PressureThreshold, AttributeThreshold, EntityCountCheck,
  │     RelationshipExists, CooldownCheck, AlwaysTrue
  ├── Mutations: ordered list executed on fire
  │     CreateEntity, CreateRelationship, ModifyAttribute,
  │     ModifyPressure, SetTag, SetHistorical
  ├── FailureMutations (action cards only)
  ├── Mana cost, cooldown, cadence, lifespan
  └── Tags for synergy

Action cards also register decisions into Layer 3:
  When card is in tableau → its decisions become available to entities
  When card is removed → its decisions disappear

CardInstance (runtime state per tableau card)
  ├── LastFiredTick, FireCount, LifespanRemaining
  └── PreconditionStatus (diagnostic)

TableauManager
  ├── Active cards with mana accounting
  ├── Lifespan expiration → mana return
  └── Events: CardFired, CardExpired
```

## Map

```
HexGrid (axial coordinates, pointy-top)
  ├── Radius-5 test map (91 cells)
  ├── 8 terrain types with movement cost + habitability
  └── Per-cell: terrain, elevation, fertility, resources

RegionMap
  ├── 3 regions (Northlands, Heartlands, Coastlands)
  ├── Cell → Region (exclusive, 1:1)
  └── Adjacency computed from cell borders

SpatialIndex
  ├── Cell → EntityId set (reverse lookup)
  ├── EntitiesInRadius, EntitiesInRegion, NearestEntity
  └── A* pathfinding with terrain costs
```

## Persistence

```
SaveManager (MessagePack binary)
  ├── SaveState DTO: entities, relationships, attributes, tick, mana
  ├── Versioned format (Version=1)
  └── Round-trip tested (serialize → deserialize → identical state)
```

## Key Architectural Principles

1. **No domain mixing** — engine code never references domain content (no penguins, no orcas, no soup). Engine knows entity kinds, attributes, relationships, cards, and pressure. Domain defines subtypes, processes, cards, naming rules.
2. **Altitude separation** — three layers (physics → processes → decisions) plus pressure bridge to card execution. One-directional within a tick.
3. **Processes, decisions, and cards are data** — DSL-defined, mutable at runtime, interpreted by executors. One data language, three uses.
4. **QuikGraph, not homegrown** — graph traversal via library, not custom implementation.
5. **Everything serializable** — every system supports MessagePack round-trip.
6. **Everything visible** — no hidden state, all attributes/effects inspectable. No hidden storyteller.
7. **Strict tick ordering** — phases execute in order, results from phase N visible to phase N+1 only.
8. **Wiring validation** — startup check that domain data produces coherent feedback loops.

## Known Architecture Issue: Domain Mixing

The current implementation partially violates the "no domain mixing" principle. Instead of true architectural separation (tag-based duck typing), the engine uses some concrete names:

- `ResourceType.Sustenance` and `ResourceType.Gold` are hard-coded enums
- `ResourceSystem` has `SustenanceProductionFactor`, `GoldConsumptionPerSettlement`

The correct architecture: engine works with tagged properties. An NPC doesn't seek "sustenance" — it seeks any resource tagged `[satisfies:hunger]`. A resource isn't `Sustenance` — it's a map-layer resource with tags. The domain registers concrete names and tag mappings. The engine operates on tags.

This refactor is tracked as #32. The current concrete implementation works for Phase 1 validation but must be abstracted before Phase 2 (domain definition).

## File Structure

```
src/
  Engine/
    Graph/          WorldGraph, Entity, Relationship, EntityId, EntityStatus, GraphEvents
    Kinds/          EntityKind, KindConfig, KindRegistry, RelationshipConfig
    Attributes/     AttributeStore, SemanticPlane, EffectSystem
    Map/            HexCoord, HexCell, HexGrid, TerrainType, Region, SpatialIndex, TestMapBuilder
    Clock/          TickClock, TickPhase, SimulationSpeed
    Behaviors/      IProcess, IDecision, ActivityRegistry, EntityEvaluator, IWorldState
    Relationships/  StrengthModifier, DefaultModifiers, DispositionCalculator, RelationshipSystem
    Lifecycle/      AgingDrain, LifecycleSystem
    Resources/      ResourceType, ResourceLayer, ResourceSystem
    Pressure/       ContributingFactor, PressureDefinition, PressureSystem, DefaultPressures
    Cards/          ProcessDefinition, DataDrivenProcess, DataDrivenDecision,
                    IPrecondition, IScoreExpression, IMutation, ICardContext,
                    CardDefinition, CardInstance, CardExecutor, Variance
    Tableau/        TableauManager
    Wiring/         AttributeRef, WiringContract, WiringValidator
    Domain/         DomainConfig, DomainLoader, TestDomain
    Messages/       MessageBus, SimulationMessage (ProcessExecuted, DecisionExecuted, etc.)
    Events/         GameEventArgs, EventLog
    Degeneracy/     IDegeneracyDetector, DefaultDetectors, DetectorRegistry
    Persistence/    SaveState, SaveManager
    Simulation/     SimulationRunner, WorldState
  UI/
    Main.cs, HexMapRenderer.cs, EntityDetailPanel.cs
tests/
  Engine.Tests/     388 tests mirroring src/Engine structure
```
