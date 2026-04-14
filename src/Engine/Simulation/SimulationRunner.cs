namespace Engine.Simulation;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Cards;
using Engine.Processes;
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
        WorldGraph graph, EffectSystem effects,
        HexGrid grid, RegionMap regionMap, SpatialIndex spatialIndex,
        TickClock clock, EntityEvaluator evaluator, RelationshipSystem relationships,
        LifecycleSystem lifecycle, ResourceSystem resources, PressureSystem pressure,
        TableauManager tableau, EventLog eventLog, DetectorRegistry detectors,
        WorldState worldState, KindRegistry kindRegistry, ActivityRegistry activityRegistry,
        MessageBus messageBus)
    {
        Graph = graph;
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
        var effects = new EffectSystem();
        var spatialIndex = new SpatialIndex(grid, regionMap, graph);
        spatialIndex.RebuildFromGraph();

        // Load domain into engine systems
        var kindRegistry = Domain.DomainLoader.LoadKindRegistry(domainConfig);
        Domain.DomainLoader.InitializeEntityAttributes(graph, domainConfig);

        // Message bus
        var messageBus = new MessageBus();

        // Wiring validation: check domain coherence before starting
        var wiringValidator = Domain.DomainLoader.LoadAndValidateWiring(domainConfig);
        var wiringErrors = wiringValidator.Validate();
        // Log warnings but don't block startup (test domain has minimal wiring)
        foreach (var error in wiringErrors)
        {
            System.Diagnostics.Debug.WriteLine($"[WIRING] {error.Type}: {error.Message}");
        }

        // Three-layer behavioral model: ALL processes are data-defined in domain config
        var activityRegistry = new ActivityRegistry();
        Domain.DomainLoader.LoadProcesses(domainConfig, activityRegistry);
        var needResolver = new Behaviors.NeedResolver(domainConfig.Kinds, domainConfig.Processes);
        var evaluator = new EntityEvaluator(activityRegistry, messageBus, needResolver: needResolver);

        // Relationships
        var relationships = new RelationshipSystem(graph, kindRegistry);
        relationships.AddModifier(new ProximityModifier());
        relationships.AddModifier(new SharedCultureModifier());
        relationships.AddModifier(new NeglectModifier());

        // Lifecycle from domain config — register drain tick effects on all entities
        var lifecycleConfig = Domain.DomainLoader.LoadLifecycleConfig(domainConfig);
        var lifecycle = new LifecycleSystem(graph, lifecycleConfig);
        lifecycle.InitializeEffects();

        // Resources
        var resources = new ResourceSystem(graph, grid);

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
        var worldState = new WorldState(graph, effects, grid, regionMap, spatialIndex, resources);

        // Wire to tick clock
        var clock = new TickClock();
        var relContext = new RelationshipContextAdapter(graph);

        // Phase 1: Low-altitude physics
        // Order: update drain rates → apply tick effects → check death → resources → processes
        var maintenance = new Lifecycle.MaintenanceSystem(graph);
        clock.Register("lifecycle_drain_update", TickPhase.LowAltitude, cadence: 1,
            tick =>
            {
                worldState.CurrentTick = tick;
                lifecycle.UpdateDrainRates(tick);
            });
        clock.Register("maintenance", TickPhase.LowAltitude, cadence: 1,
            _ => maintenance.Tick());
        clock.Register("lifecycle_death_check", TickPhase.LowAltitude, cadence: 1,
            tick => lifecycle.CheckDeaths(tick));
        clock.Register("relationships", TickPhase.LowAltitude, cadence: 1,
            tick => relationships.Tick(tick, relContext));
        clock.Register("resources", TickPhase.LowAltitude, cadence: 1,
            tick => resources.Tick(tick));
        clock.Register("processes", TickPhase.LowAltitude, cadence: 1,
            _ => evaluator.EvaluateAll(graph, worldState));

        // Phase 2: Pressure (every 5 ticks for efficiency)
        clock.Register("pressure", TickPhase.Pressure, cadence: 5,
            _ =>
            {
                var contexts = BuildPressureContexts(graph, regionMap);
                pressure.Compute(contexts);
            });

        // Phase 4: Card execution
        clock.Register("tableau", TickPhase.CardExecution, cadence: 1,
            tick =>
            {
                var cardContext = new CardMutationContext(graph, pressure, worldState);
                tableau.Tick(tick, cardContext);
            });

        // Phase 5: Degeneracy detection (every 10 ticks)
        clock.Register("degeneracy", TickPhase.EventNotification, cadence: 10,
            tick =>
            {
                var degCtx = BuildDegeneracyContext(graph);
                detectors.Evaluate(degCtx, tick);
            });

        return new SimulationRunner(
            graph, effects, grid, regionMap, spatialIndex,
            clock, evaluator, relationships, lifecycle, resources, pressure,
            tableau, eventLog, detectors, worldState, kindRegistry, activityRegistry,
            messageBus);
    }

    /// <summary>Advance the simulation by one frame (speed-dependent tick count).</summary>
    public int AdvanceFrame() => Clock.AdvanceFrame();

    /// <summary>Advance exactly one tick.</summary>
    public bool Tick() => Clock.Tick();

    private static Dictionary<EntityId, IPressureContext> BuildPressureContexts(
        WorldGraph graph, RegionMap regionMap)
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

            contexts[region.EntityId] = new SimplePressureContext(graph, entitiesInRegion);
        }

        return contexts;
    }

    private static DegeneracyContextAdapter BuildDegeneracyContext(WorldGraph graph)
    {
        return new DegeneracyContextAdapter(graph);
    }
}

// --- Adapter classes to bridge system interfaces ---

internal sealed class RelationshipContextAdapter(WorldGraph graph)
    : IRelationshipContext
{
    public Entity? GetEntity(EntityId id) => graph.GetEntity(id);
    public float GetAttribute(EntityId entityId, string attributeName) =>
        graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
    public (int Q, int R)? GetCoordinates(EntityId entityId) => graph.GetEntity(entityId)?.Coordinates;
    public float? GetSemanticDistance(EntityId a, EntityId b, string planeName) => null; // TODO: wire semantic planes
}

internal sealed class SimplePressureContext(
    WorldGraph graph, IReadOnlyCollection<EntityId> entitiesInScope)
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
        graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
}

internal sealed class CardMutationContext(
    WorldGraph graph, PressureSystem pressure, WorldState worldState,
    Random? random = null)
    : IMutationContext
{
    private Dictionary<string, float>? _variables;
    public int CurrentTick => worldState.CurrentTick;
    public EntityId SelfEntityId => new EntityId(-1); // Card context has no "self" -- cards target explicitly
    public Random Random { get; } = random ?? System.Random.Shared;
    public EntityId? ResolvedTargetId => null;
    public void SetVariable(string name, float value) { _variables ??= []; _variables[name] = value; }
    public float GetVariable(string name) => _variables is not null && _variables.TryGetValue(name, out var v) ? v : 0f;
    public float GetPressure(string pressureName, EntityId scopeId) => pressure.GetValue(pressureName, scopeId);
    public float GetAttribute(EntityId entityId, string attributeName) =>
        graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
    public int CountEntitiesByKind(EntityKind kind) => graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        graph.OutgoingRelationships(sourceId, kind).FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() => null; // Per-card, handled by CardInstance
    public Entity? GetEntity(EntityId id) => graph.GetEntity(id);

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) =>
        graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault()?.TargetId;

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) =>
        graph.GetEntitiesByKind(kind);

    public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType)
    {
        if (worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        return resourceType switch
        {
            Resources.ResourceType.Sustenance => worldState.Resources.Sustenance.GetStockpile(coord),
            Resources.ResourceType.Gold => worldState.Resources.Gold.GetStockpile(coord),
            Resources.ResourceType.Population => worldState.Resources.PopulationLayer.GetStockpile(coord),
            _ => 0f,
        };
    }

    public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
        graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);

    public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) =>
        graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta)
    {
        graph.GetEntity(entityId)?.Attributes.Modify(attributeName, delta);
    }

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) =>
        pressure.ApplyDelta(pressureName, scopeId, delta);

    public void SetTag(EntityId entityId, string key, string value)
    {
        // TODO: implement tag storage
    }

    public void SetEntityHistorical(EntityId entityId) =>
        graph.SetEntityHistorical(entityId, CurrentTick);

    public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount)
    {
        if (worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        var layer = resourceType switch
        {
            Resources.ResourceType.Sustenance => worldState.Resources.Sustenance,
            Resources.ResourceType.Gold => worldState.Resources.Gold,
            Resources.ResourceType.Population => worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return 0f;
        var available = layer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        layer.AddToStockpile(coord, -consumed);
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r) =>
        graph.SetCoordinates(entityId, q, r);

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var rel = graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        if (rel is not null)
        {
            graph.CullRelationship(rel, CurrentTick);
        }
    }
}

internal sealed class DegeneracyContextAdapter(WorldGraph graph)
    : IDegeneracyContext
{
    public int ActiveSettlementCount =>
        graph.GetEntitiesByKind(EntityKind.Settlement).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);

    public int DecliningSettlementCount =>
        graph.GetEntitiesByKind(EntityKind.Settlement).Count(id =>
            graph.GetEntity(id)?.Attributes["population"]?.Trend < 0);

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
