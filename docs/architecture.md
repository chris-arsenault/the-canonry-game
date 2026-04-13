# System Architecture (v0.1.0)

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
│  Single clock, 5 strict phases per tick              │
│  Systems register with phase + cadence               │
└──────────────────────┬──────────────────────────────┘
                       │
     ┌─────────────────┼─────────────────────┐
     ▼                 ▼                     ▼
Phase 1            Phase 2              Phases 4-5
LOW ALTITUDE       BRIDGE              MID ALTITUDE
```

## Phase Execution (Per Tick)

**Phase 1 — Low Altitude (always-on physics):**
```
BehaviorSystem          → Utility AI evaluates impulses for all agency entities
  ├── Settlement: PopulationGrowth, Development
  ├── Nation: BorderTension, Consolidation
  ├── NPC: Hunger, Ambition
  └── Artifact: Degradation, Resonance
RelationshipSystem      → Strength modifiers (proximity, culture, neglect), auto-culling
LifecycleSystem         → Per-kind aging drain on health metrics, death transitions
ResourceSystem          → Sustenance + gold production/consumption, intra-national flow
```

**Phase 2 — Bridge (every 5 ticks):**
```
PressureSystem          → Computes 4 regional pressures from low-altitude outputs
  ├── Military tension (nation count, rivalries - alliances)
  ├── Economic stress (low-prosperity settlements)
  ├── Cultural friction (culture diversity)
  └── Demographic pressure (settlement density)
```

**Phase 4 — Mid Altitude (card-controlled):**
```
TableauManager          → Evaluates active cards, fires when preconditions met
  └── CardExecutor      → Precondition trees (AND/OR), mutation execution
```

**Phase 5 — Observability (every 10 ticks):**
```
DetectorRegistry        → 3 degeneracy detectors → EventLog warnings
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
  └── Two layers: Sustenance, Gold
```

## Entity Model

```
5 MVP Kinds (of 11 total):
  Region    (Spatial)  — permanent, pressure scoping
  Settlement (Spatial) — population-based lifecycle, resource production
  Nation    (Agency)   — cohesion-based lifecycle, border tension
  NPC       (Agency)   — HP-based lifecycle, hunger/ambition
  Artifact  (Asset)    — condition-based lifecycle, resonance

Category-level relationship taxonomy (4x4, not 11x11):
  Spatial×Spatial: borders, contains
  Agency×Spatial: controlled_by, located_at, home_base
  Agency×Agency: allied_with, rival_of, led_by, member_of, loyal_to
  Agency×Asset: holds, created_by
  Asset×Spatial: stored_at
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

1. **No domain mixing** — engine code should work in terms of abstract properties and tags, not concrete resource/entity names. See known issue below.
2. **Altitude separation** — low-altitude writes attributes, pressure reads them, cards read pressure. One-directional within a tick.
3. **Cards are data** — DSL-defined, mutable at runtime, interpreted by executors.
4. **QuikGraph, not homegrown** — graph traversal via library, not custom implementation.
5. **Everything serializable** — every system supports MessagePack round-trip.
6. **Everything visible** — no hidden state, all attributes/effects inspectable.
7. **Strict tick ordering** — 5 phases, phase N results visible to N+1 only.

## Known Architecture Issue: Domain Mixing

The current implementation violates the "no domain mixing" principle. Instead of true architectural separation (tag-based duck typing), the engine uses concrete names:

- `ResourceType.Sustenance` and `ResourceType.Gold` are hard-coded enums
- `ResourceSystem` has `SustenanceProductionFactor`, `GoldConsumptionPerSettlement`
- Impulses reference specific attribute names ("hp", "hunger", "population") as string literals
- `DefaultPressures` hard-codes specific pressure definitions
- `DefaultRegistry` hard-codes specific subtypes ("village", "town", "warrior")
- `DefaultImpulses` hard-codes specific impulse configurations

The correct architecture: engine works with tagged properties. An NPC doesn't seek "sustenance" — it seeks any resource tagged `[satisfies:hunger]`. A resource isn't `Sustenance` — it's a map-layer resource with tags. The domain registers concrete names and tag mappings. The engine operates on tags.

This refactor is tracked as the next priority item. The current concrete implementation works for MVP validation but must be abstracted before Phase 2 (domain definition).

## File Structure

```
src/
  Engine/
    Graph/          WorldGraph, Entity, Relationship, EntityId, EntityStatus, GraphEvents
    Kinds/          EntityKind, KindDefinition, KindRegistry, RelationshipKindDefinition
    Attributes/     AttributeStore, SemanticPlane, EffectSystem
    Map/            HexCoord, HexCell, HexGrid, TerrainType, Region, SpatialIndex, TestMapBuilder
    Clock/          TickClock, TickPhase, SimulationSpeed
    Behaviors/      IImpulse, ImpulseRegistry, UtilityEvaluator, BehaviorSystem
      Impulses/     Settlement/Nation/NPC/Artifact impulses, DefaultImpulses
    Relationships/  StrengthModifier, DefaultModifiers, DispositionCalculator, RelationshipSystem
    Lifecycle/      AgingDrain, LifecycleSystem
    Resources/      ResourceType, ResourceLayer, ResourceSystem
    Pressure/       ContributingFactor, PressureDefinition, PressureSystem, DefaultPressures
    Cards/          IPrecondition, IMutation, CardDefinition, CardInstance, CardExecutor, ICardContext
    Tableau/        TableauManager
    Events/         GameEventArgs, EventLog
    Degeneracy/     IDegeneracyDetector, DefaultDetectors, DetectorRegistry
    Persistence/    SaveState, SaveManager
    Simulation/     SimulationRunner, WorldState
  UI/
    Main.cs, HexMapRenderer.cs, EntityDetailPanel.cs
tests/
  Engine.Tests/     355 tests mirroring src/Engine structure
```
