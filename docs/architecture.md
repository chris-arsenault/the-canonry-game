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

**Phase 2 — Goal-Driven Process Evaluation:**
```
EntityEvaluator
  → Regenerate goals (every 10 ticks or on sub-goal completion)
  → Select goal (weighted random by urgency)
  → Find serving processes (writes to goal's target attribute)
  → Filter by cooldowns and preconditions (PreconditionResult with diagnostics)
  → Weighted random selection, execute mutations
  → Goal provider set as target on mutation context
  → Cooldowns applied with ±25% jitter
  → If no viable process for selected goal, fall through to next goal
```

**Phase 3 — Decisions (card-gated, separate from processes):**
```
EntityEvaluator
  → Score viable decisions (card-gated only)
  → Weighted random selection, execute
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

Entity-Owned Attributes (IEntityProperty)
  ├── ScalarValue: base + tick effects + static effects, trend tracking
  ├── SemanticPlane: 2D (X,Y) with per-component effects, label reduction, distance
  └── AttributeSet: Dictionary<string, IEntityProperty> per entity

Effect Composition
  ├── Tick effects: base += sum(deltas) * product(multipliers) each tick
  ├── Static effects: effective = base composed on read
  └── Effects carry source ID for cleanup when source is removed

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

## Goal-Driven Behavioral Model

```
Goal System:
  Entity maintains max 3 primary goals + sub-goals
  Goals: (attribute, provider, urgency) — what to change, who to act on
  Generated from attribute deficits + process wiring contracts
  Provider found by querying graph for entities matching Target-scoped refs
  Sub-goals generated from blocked precondition results (BlockingAttributes)
  Regenerated every 10 ticks or when sub-goal fires

Process Evaluation (per entity per tick):
  1. Select goal (weighted random by urgency)
  2. Find serving processes (writes to goal's target attribute)
  3. Filter by cooldowns + preconditions (PreconditionResult with diagnostics)
  4. Weighted random select, execute with goal's provider as target
  5. Fall through to next goal if no viable process

ProcessDefinition:
  ├── Id, Name, Role (Process | Decision), TargetKind
  ├── Precondition: composable tree → PreconditionResult (passed, diagnostic, blockers)
  ├── ScoreExpression: IExpression evaluated against entity state
  ├── ProviderScoreExpression: IExpression evaluated per candidate provider (#82)
  ├── Mutations: ordered list (CalculateMutation, ModifySelf/Target, variable chaining)
  ├── Cooldown + EntityCooldown (with ±25% jitter)
  ├── Reads: HashSet<AttributeRef> with Self/Target scope
  └── Writes: HashSet<AttributeRef> with Self/Target scope

Expression System (IExpression):
  ├── Attribute paths: self.hunger, target.ideology.x
  ├── Arithmetic: +, -, *, /
  ├── Built-ins: distance, semantic_distance, min, max, clamp, abs, jitter
  ├── Context variables: CalculateMutation stores, subsequent mutations read
  └── Used by: scoring, provider scoring, calculate mutations
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
2. **Uniform evaluation** — all entities go through the same goal/process evaluator. No per-kind code. Entity kinds differ only in registered processes and attributes.
3. **Processes, decisions, and cards are data** — DSL-defined, mutable at runtime, interpreted by executors. Provider scoring is process-defined, not engine-hardcoded.
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
    Graph/          WorldGraph, Entity, EntityGoal, Relationship, EntityId
    Kinds/          EntityKind, KindConfig, KindRegistry, RelationshipConfig
    Attributes/     IEntityProperty, ScalarValue, SemanticPlane, SemanticPlaneDefinition,
                    AttributeEffect, AttributeSet, EffectSystem
    Processes/      ProcessDefinition, DataDrivenProcess, DataDrivenDecision,
                    IMutationContext, IPrecondition, PreconditionResult, IMutation,
                    IExpression, Expression, Variance
    Behaviors/      IProcess, IDecision, ActivityRegistry, EntityEvaluator,
                    GoalGenerator, GraphAttributeQuery, IWorldState
    Map/            HexCoord, HexCell, HexGrid, TerrainType, Region, SpatialIndex, TestMapBuilder
    Clock/          TickClock, TickPhase, SimulationSpeed
    Relationships/  StrengthModifier, DefaultModifiers, DispositionCalculator, RelationshipSystem
    Lifecycle/      AgingDrain, LifecycleSystem, MaintenanceSystem
    Resources/      ResourceType, ResourceLayer, ResourceSystem
    Pressure/       ContributingFactor, PressureDefinition, PressureSystem
    Cards/          CardDefinition, CardInstance, CardExecutor
    Tableau/        TableauManager
    Wiring/         AttributeRef (Self/Target scope), WiringContract, WiringValidator
    Domain/         DomainConfig, DomainLoader, TestDomain
    Messages/       MessageBus, SimulationMessage
    Events/         GameEventArgs, EventLog
    Degeneracy/     IDegeneracyDetector, DefaultDetectors, DetectorRegistry
    Persistence/    SaveState, SaveManager
    Simulation/     SimulationRunner, WorldState
  UI/
    Main.cs, HexMapRenderer.cs, EntityBrowser.cs, EntityCard.cs, HealthGraphPanel.cs
tests/
  Engine.Tests/     384 tests mirroring src/Engine structure
```
