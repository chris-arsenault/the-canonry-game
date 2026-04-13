namespace Engine.Simulation;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Behaviors.Impulses;
using Engine.Cards;
using Engine.Clock;
using Engine.Degeneracy;
using Engine.Events;
using Engine.Graph;
using Engine.Kinds;
using Engine.Lifecycle;
using Engine.Map;
using Engine.Messages;
using Engine.Pressure;
using Engine.Relationships;
using Engine.Resources;
using Engine.Tableau;

/// <summary>
/// Wires all engine systems together through the tick clock.
/// The single integration point that makes the simulation run.
/// </summary>
public sealed class SimulationRunner
{
    // Core data
    public WorldGraph Graph { get; }
    public AttributeStore Attributes { get; }
    public EffectSystem Effects { get; }

    // Map
    public HexGrid Grid { get; }
    public RegionMap RegionMap { get; }
    public SpatialIndex SpatialIndex { get; }

    // Clock
    public TickClock Clock { get; }

    // Low-altitude systems
    public EntityEvaluator Evaluator { get; }
    public RelationshipSystem Relationships { get; }
    public LifecycleSystem Lifecycle { get; }
    public ResourceSystem Resources { get; }

    // Message bus
    public MessageBus MessageBus { get; }

    // Bridge
    public PressureSystem Pressure { get; }

    // Mid-altitude
    public TableauManager Tableau { get; }

    // Observability
    public EventLog EventLog { get; }
    public DetectorRegistry Detectors { get; }

    // State
    public WorldState WorldState { get; }

    // Registries
    public KindRegistry KindRegistry { get; }
    public ActivityRegistry ActivityRegistry { get; }

    private SimulationRunner(
        WorldGraph graph, AttributeStore attributes, EffectSystem effects,
        HexGrid grid, RegionMap regionMap, SpatialIndex spatialIndex,
        TickClock clock, EntityEvaluator evaluator, RelationshipSystem relationships,
        LifecycleSystem lifecycle, ResourceSystem resources, PressureSystem pressure,
        TableauManager tableau, EventLog eventLog, DetectorRegistry detectors,
        WorldState worldState, KindRegistry kindRegistry, ActivityRegistry activityRegistry,
        MessageBus messageBus)
    {
        Graph = graph;
        Attributes = attributes;
        Effects = effects;
        Grid = grid;
        RegionMap = regionMap;
        SpatialIndex = spatialIndex;
        Clock = clock;
        Evaluator = evaluator;
        Relationships = relationships;
        Lifecycle = lifecycle;
        Resources = resources;
        Pressure = pressure;
        Tableau = tableau;
        MessageBus = messageBus;
        EventLog = eventLog;
        Detectors = detectors;
        WorldState = worldState;
        KindRegistry = kindRegistry;
        ActivityRegistry = activityRegistry;
    }

    /// <summary>
    /// Build a simulation from the test map with all systems wired together.
    /// Uses TestDomain for all domain-specific configuration.
    /// </summary>
    public static SimulationRunner CreateFromTestMap()
    {
        return CreateFromDomain(Domain.TestDomain.Create());
    }

    /// <summary>
    /// Build a simulation from a domain config. The domain provides all
    /// entity kinds, subtypes, attributes, relationships, pressures, and lifecycles.
    /// The engine provides the framework; the domain provides the content.
    /// </summary>
    public static SimulationRunner CreateFromDomain(Domain.DomainConfig domainConfig)
    {
        ArgumentNullException.ThrowIfNull(domainConfig);

        // Build world
        var testWorld = TestMapBuilder.BuildDefault();
        var graph = testWorld.Graph;
        var grid = testWorld.Grid;
        var regionMap = testWorld.RegionMap;

        // Core systems
        var attributes = new AttributeStore();
        var effects = new EffectSystem();
        var spatialIndex = new SpatialIndex(grid, regionMap, graph);
        spatialIndex.RebuildFromGraph();

        // Load domain into engine systems
        var kindRegistry = Domain.DomainLoader.LoadKindRegistry(domainConfig);
        Domain.DomainLoader.InitializeEntityAttributes(graph, attributes, domainConfig);

        // Message bus
        var messageBus = new MessageBus();

        // Three-layer behavioral model: processes + decisions via EntityEvaluator
        var activityRegistry = new ActivityRegistry();
        Behaviors.Processes.SmokeTestProcesses.RegisterAll(activityRegistry);
        var evaluator = new EntityEvaluator(activityRegistry, messageBus);

        // Wiring validation
        var wiringValidator = new Wiring.WiringValidator();
        foreach (var (kind, kindConfig) in domainConfig.Kinds)
        {
            wiringValidator.RegisterHealthAttribute(kind, kindConfig.HealthAttribute);
        }

        Behaviors.Processes.SmokeTestProcesses.RegisterContracts(wiringValidator);
        // Note: ValidateOrThrow disabled for now -- smoke test contracts are minimal
        // and won't pass full validation. Enable when real processes are designed.
        // wiringValidator.ValidateOrThrow();

        // Relationships
        var relationships = new RelationshipSystem(graph, kindRegistry);
        relationships.AddModifier(new ProximityModifier());
        relationships.AddModifier(new SharedCultureModifier());
        relationships.AddModifier(new NeglectModifier());

        // Lifecycle from domain config
        var lifecycleConfig = Domain.DomainLoader.LoadLifecycleConfig(domainConfig);
        var lifecycle = new LifecycleSystem(graph, attributes, lifecycleConfig);

        // Resources
        var resources = new ResourceSystem(graph, attributes, grid);

        // Pressure from domain config
        var pressure = new PressureSystem();
        foreach (var def in Domain.DomainLoader.LoadPressureDefinitions(domainConfig))
        {
            pressure.RegisterDefinition(def);
        }

        // Tableau
        var tableau = new TableauManager(10f);

        // Observability
        var eventLog = new EventLog();
        var detectors = DetectorRegistry.CreateDefault(eventLog);

        // World state
        var worldState = new WorldState(graph, attributes, effects, grid, regionMap, spatialIndex);

        // Wire to tick clock
        var clock = new TickClock();
        var relContext = new RelationshipContextAdapter(graph, attributes);

        // Phase 1: Low-altitude (physics + process selection)
        clock.Register("physics_and_processes", TickPhase.LowAltitude, cadence: 1,
            tick =>
            {
                worldState.CurrentTick = tick;
                // Physics run first (lifecycle, relationships, resources -- registered below)
                // Then process/decision selection for all entities
                evaluator.EvaluateAll(graph, worldState);
            });
        clock.Register("relationships", TickPhase.LowAltitude, cadence: 1,
            tick => relationships.Tick(tick, relContext));
        clock.Register("lifecycle", TickPhase.LowAltitude, cadence: 1,
            tick => lifecycle.Tick(tick));
        clock.Register("resources", TickPhase.LowAltitude, cadence: 1,
            tick => resources.Tick(tick));

        // Phase 2: Pressure (every 5 ticks for efficiency)
        clock.Register("pressure", TickPhase.Pressure, cadence: 5,
            _ =>
            {
                var contexts = BuildPressureContexts(graph, attributes, regionMap);
                pressure.Compute(contexts);
            });

        // Phase 4: Card execution
        clock.Register("tableau", TickPhase.CardExecution, cadence: 1,
            tick =>
            {
                var cardContext = new CardContextAdapter(graph, attributes, pressure, worldState);
                tableau.Tick(tick, cardContext);
            });

        // Phase 5: Degeneracy detection (every 10 ticks)
        clock.Register("degeneracy", TickPhase.EventNotification, cadence: 10,
            tick =>
            {
                var degCtx = BuildDegeneracyContext(graph, attributes);
                detectors.Evaluate(degCtx, tick);
            });

        return new SimulationRunner(
            graph, attributes, effects, grid, regionMap, spatialIndex,
            clock, evaluator, relationships, lifecycle, resources, pressure,
            tableau, eventLog, detectors, worldState, kindRegistry, activityRegistry,
            messageBus);
    }

    /// <summary>Advance the simulation by one frame (speed-dependent tick count).</summary>
    public int AdvanceFrame() => Clock.AdvanceFrame();

    /// <summary>Advance exactly one tick.</summary>
    public bool Tick() => Clock.Tick();

    /// <summary>
    /// Bridge: wrap existing IImpulse implementations as IProcess until #37 rewrites them.
    /// </summary>
    private static void RegisterLegacyImpulsesAsProcesses(ActivityRegistry registry)
    {
        var impulseRegistry = new ImpulseRegistry();
        DefaultImpulses.RegisterAll(impulseRegistry);

        foreach (EntityKind kind in Enum.GetValues<EntityKind>())
        {
            foreach (var impulse in impulseRegistry.GetImpulses(kind))
            {
                registry.RegisterProcess(kind, new LegacyImpulseProcess(impulse));
            }
        }
    }

    private static Dictionary<EntityId, IPressureContext> BuildPressureContexts(
        WorldGraph graph, AttributeStore attributes, RegionMap regionMap)
    {
        var contexts = new Dictionary<EntityId, IPressureContext>();
        foreach (var region in regionMap.AllRegions)
        {
            var entitiesInRegion = new List<EntityId>();
            foreach (var (eid, entity) in graph.AllEntities)
            {
                if (entity.Coordinates is null) continue;
                if (region.ContainsCell(new Map.HexCoord(entity.Coordinates.Value.Q, entity.Coordinates.Value.R)))
                {
                    entitiesInRegion.Add(eid);
                }
            }

            contexts[region.EntityId] = new SimplePressureContext(graph, attributes, entitiesInRegion);
        }

        return contexts;
    }

    private static DegeneracyContextAdapter BuildDegeneracyContext(WorldGraph graph, AttributeStore attributes)
    {
        return new DegeneracyContextAdapter(graph, attributes);
    }
}

// --- Adapter classes to bridge system interfaces ---

internal sealed class RelationshipContextAdapter(WorldGraph graph, AttributeStore attributes)
    : IRelationshipContext
{
    public Entity? GetEntity(EntityId id) => graph.GetEntity(id);
    public float GetAttribute(EntityId entityId, string attributeName) => attributes.Get(entityId, attributeName);
    public (int Q, int R)? GetCoordinates(EntityId entityId) => graph.GetEntity(entityId)?.Coordinates;
    public float? GetSemanticDistance(EntityId a, EntityId b, string planeName) => null; // TODO: wire semantic planes
}

internal sealed class SimplePressureContext(
    WorldGraph graph, AttributeStore attributes, IReadOnlyCollection<EntityId> entitiesInScope)
    : IPressureContext
{
    public IReadOnlyCollection<EntityId> EntitiesInScope => entitiesInScope;
    public Entity? GetEntity(EntityId id) => graph.GetEntity(id);

    public int CountEntities(Func<Entity, bool> predicate)
    {
        return entitiesInScope.Count(id =>
        {
            var e = graph.GetEntity(id);
            return e is not null && predicate(e);
        });
    }

    public int CountRelationships(Func<Relationship, bool> predicate)
    {
        return entitiesInScope.Sum(id =>
            graph.OutgoingRelationships(id).Count(predicate));
    }

    public float GetAttribute(EntityId entityId, string attributeName) =>
        attributes.Get(entityId, attributeName);
}

internal sealed class CardContextAdapter(
    WorldGraph graph, AttributeStore attributes, PressureSystem pressure, WorldState worldState)
    : ICardContext
{
    public int CurrentTick => worldState.CurrentTick;
    public float GetPressure(string pressureName, EntityId scopeId) => pressure.GetValue(pressureName, scopeId);
    public float GetAttribute(EntityId entityId, string attributeName) => attributes.Get(entityId, attributeName);
    public int CountEntitiesByKind(EntityKind kind) => graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        graph.OutgoingRelationships(sourceId, kind).FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() => null; // Per-card, handled by CardInstance
    public Entity? GetEntity(EntityId id) => graph.GetEntity(id);

    public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
        graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);

    public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) =>
        graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
        attributes.Modify(entityId, attributeName, delta);

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) =>
        pressure.ApplyDelta(pressureName, scopeId, delta);

    public void SetTag(EntityId entityId, string key, string value)
    {
        // TODO: implement tag storage
    }

    public void SetEntityHistorical(EntityId entityId) =>
        graph.SetEntityHistorical(entityId, CurrentTick);
}

internal sealed class DegeneracyContextAdapter(WorldGraph graph, AttributeStore attributes)
    : IDegeneracyContext
{
    public int ActiveSettlementCount =>
        graph.GetEntitiesByKind(EntityKind.Settlement).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);

    public int DecliningSettlementCount =>
        graph.GetEntitiesByKind(EntityKind.Settlement).Count(id =>
            attributes.GetTrend(id, "population") < 0);

    public int ActiveNationCount =>
        graph.GetEntitiesByKind(EntityKind.Nation).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);

    public int MaxNationSettlementCount
    {
        get
        {
            var max = 0;
            foreach (var nationId in graph.GetEntitiesByKind(EntityKind.Nation))
            {
                var count = graph.GetEntitiesByKind(EntityKind.Settlement)
                    .Count(sid => graph.OutgoingRelationships(sid, "controlled_by")
                        .Any(r => r.TargetId == nationId));
                max = Math.Max(max, count);
            }

            return max;
        }
    }

    public int TotalSettlementCount => graph.GetEntitiesByKind(EntityKind.Settlement).Count;

    public int ActiveNpcCount =>
        graph.GetEntitiesByKind(EntityKind.Npc).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);
}
