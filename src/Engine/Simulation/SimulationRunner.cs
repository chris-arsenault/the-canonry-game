namespace Engine.Simulation;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Cards;
using Engine.Processes;
using Engine.Clock;
using Engine.Degeneracy;
using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Domain.Loader;
using Engine.Domain.IceRemembers;
using Engine.Events;
using Engine.Generators;
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
    public DomainConfig DomainConfig { get; }
    public WorldGraph Graph { get; }
    public EffectSystem Effects { get; }

    // Map
    public HexGrid Grid { get; }
    public MapCoverageMap CoverageMap { get; }
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
    public DefinitionGraph DefinitionGraph { get; }

    // Registries
    public KindRegistry KindRegistry { get; }
    public BehaviorDefinitionIndex BehaviorDefinitions { get; }
    public GeneratorSystem GeneratorSystem { get; }
    public IReadOnlyDictionary<string, CardDefinition> SpawnCards { get; }

    private SimulationRunner(
        DomainConfig domainConfig,
        WorldGraph graph, EffectSystem effects,
        HexGrid grid, MapCoverageMap coverageMap, SpatialIndex spatialIndex,
        TickClock clock, EntityEvaluator evaluator, RelationshipSystem relationships,
        LifecycleSystem lifecycle, ResourceSystem resources, PressureSystem pressure,
        TableauManager tableau, EventLog eventLog, DetectorRegistry detectors,
        WorldState worldState, DefinitionGraph definitionGraph, KindRegistry kindRegistry,
        BehaviorDefinitionIndex behaviorDefinitions, GeneratorSystem generatorSystem,
        IReadOnlyDictionary<string, CardDefinition> spawnCards,
        MessageBus messageBus)
    {
        DomainConfig = domainConfig;
        Graph = graph;
        Effects = effects;
        Grid = grid;
        CoverageMap = coverageMap;
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
        DefinitionGraph = definitionGraph;
        KindRegistry = kindRegistry;
        BehaviorDefinitions = behaviorDefinitions;
        GeneratorSystem = generatorSystem;
        SpawnCards = spawnCards;
    }

    /// <summary>
    /// Build a simulation from the test map with all systems wired together.
    /// Uses IceRemembers for all domain-specific configuration.
    /// </summary>
    public static SimulationRunner CreateFromTestMap()
    {
        return CreateFromDomain(IceRemembersDefinition.Create());
    }

    /// <summary>
    /// Build a simulation from a domain config. The domain provides all
    /// entity kinds, subtypes, attributes, relationships, pressures, and lifecycles.
    /// The engine provides the framework; the domain provides the content.
    /// </summary>
    public static SimulationRunner CreateFromDomain(
        DomainConfig domainConfig,
        Random? random = null,
        MessageBus? messageBus = null,
        SimulationDiagnosticsOptions? diagnosticsOptions = null)
    {
        ArgumentNullException.ThrowIfNull(domainConfig);
        diagnosticsOptions ??= SimulationDiagnosticsOptions.Detailed;
        KindId.RegisterAll(domainConfig.Kinds.Keys);

        var definitionGraph = DomainLoader.BuildDefinitionGraph(domainConfig);

        // Message bus is needed before seed selection so procedural map generation
        // degeneracy can be observed and handled without crashing the executor.
        messageBus ??= new MessageBus();

        // Build an empty world first so we can wire per-entity subscribers (attribute
        // init, lifecycle drain) BEFORE any entity is created. Subsequent Populate
        // triggers WorldGraph.EntityCreated for each entity, and every subscriber
        // initializes that entity exactly once — no init-time sweeps.
        random ??= new Random();
        var worldSeed = TestMapBuilder.SelectAcceptedSeed(random, messageBus);
        var testWorld = TestMapBuilder.BuildEmpty(worldSeed);
        var graph = testWorld.Graph;
        var grid = testWorld.Grid;
        var coverageMap = testWorld.CoverageMap;

        // Core systems
        var effects = new EffectSystem();
        var spatialIndex = new SpatialIndex(grid, coverageMap, graph);
        var semanticPlane = new SemanticPlaneIndex(graph);

        // Load domain into engine systems
        var kindRegistry = DomainLoader.LoadKindRegistry(domainConfig);
        var attributeInitializer = new DomainEntityInitializer(graph, domainConfig, random);

        // Wiring validation: check domain coherence before starting
        var wiringValidator = DomainLoader.LoadAndValidateWiring(domainConfig);
        var wiringErrors = wiringValidator.Validate();
        // Log warnings but don't block startup (IceRemembers still has minimal wiring)
        foreach (var error in wiringErrors)
        {
            System.Diagnostics.Debug.WriteLine($"[WIRING] {error.Type}: {error.Message}");
        }

        // Three-layer behavioral model: ALL processes are data-defined in domain config
        var behaviorDefinitions = new BehaviorDefinitionIndex();
        DomainLoader.LoadProcesses(domainConfig, behaviorDefinitions);
        var goalGenerator = new Behaviors.GoalGenerator(domainConfig.Kinds, domainConfig.Processes);
        var evaluator = new EntityEvaluator(
            behaviorDefinitions,
            messageBus,
            goalGenerator: goalGenerator,
            kindConfigs: domainConfig.Kinds);
        evaluator.CaptureEvaluationCandidates = diagnosticsOptions.CaptureEvaluationCandidates;
        evaluator.PublishGoalDetails = diagnosticsOptions.PublishGoalDetails;

        // Relationships
        var relationships = new RelationshipSystem(graph, kindRegistry);
        relationships.AddModifier(new ProximityModifier());
        relationships.AddModifier(new SharedCultureModifier());
        relationships.AddModifier(new NeglectModifier());

        // Lifecycle from domain config — ctor subscribes to EntityCreated so every
        // entity created during Populate (and later by generators) gets its drain
        // effect without a retroactive sweep.
        var lifecycleConfig = DomainLoader.LoadLifecycleConfig(domainConfig);
        var lifecycle = new LifecycleSystem(graph, lifecycleConfig);

        // Bridge graph/lifecycle mutations onto the single simulation message bus.
        // Observers should subscribe here instead of wiring directly to subsystems.
        SimulationMessageBridge.Wire(graph, lifecycle, messageBus);

        // Resources
        var resources = new ResourceSystem(graph, grid);
        var locationDrift = new LocationResourceDriftSystem(graph, domainConfig.Kinds, resources);
        var criticalEffects = new Lifecycle.CriticalAttributeEffectSystem(graph, domainConfig.Kinds);

        // With all per-entity subscribers wired, populate the world. Entities get
        // their attributes + lifecycle drain + critical-effect hooks as they are
        // created. Spatial index catches up afterwards.
        TestMapBuilder.Populate(testWorld, worldSeed);
        spatialIndex.RebuildFromGraph();
        locationDrift.InitializeEffects();

        // Pressure from domain config
        var pressure = new PressureSystem();
        foreach (var def in DomainLoader.LoadPressureDefinitions(domainConfig))
        {
            pressure.RegisterDefinition(def);
        }

        // Tableau
        var tableau = new TableauManager(10f);

        // Observability
        var eventLog = new EventLog();
        var detectors = DetectorRegistry.CreateDefault(eventLog);

        var generators = new GeneratorInstanceRegistry();
        foreach (var generator in definitionGraph.Generators.Values)
        {
            generators.GetOrCreate(generator);
        }

        // World state
        var worldState = new WorldState(
            graph,
            effects,
            pressure,
            grid,
            coverageMap,
            spatialIndex,
            semanticPlane,
            generators,
            resources);
        var generatorSystem = new GeneratorSystem(
            graph,
            generators,
            definitionGraph,
            spatialIndex,
            semanticPlane,
            coverageMap,
            messageBus,
            random);
        var spawnCards = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

        foreach (var generator in definitionGraph.Generators.Values)
        {
            var definition = new CardDefinition(
                $"generator_{generator.Id}",
                generator.Name,
                CardType.Generator,
                new AlwaysTrue(),
                manaCost: 0,
                cooldown: 0,
                cadence: 0,
                lifespan: int.MaxValue,
                generatorId: generator.Id);
            spawnCards[generator.Id] = definition;
            _ = tableau.PlaceCard(definition, currentTick: 0)
                ?? throw new InvalidOperationException($"Failed to place generator card '{generator.Id}'.");
        }

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
                lifecycle.UpdateDrainRates(tick, worldState.QueryCache.ActiveEntities());
            });
        clock.Register("location_resource_drift", TickPhase.LowAltitude, cadence: 1,
            _ => locationDrift.UpdateDriftEffects(worldState.QueryCache.ActiveEntities()));
        clock.Register("critical_attribute_effects", TickPhase.LowAltitude, cadence: 1,
            _ => criticalEffects.UpdateEffects(worldState.QueryCache.ActiveEntities()));
        clock.Register("maintenance", TickPhase.LowAltitude, cadence: 1,
            _ => maintenance.Tick(worldState.QueryCache.ActiveEntities()));
        clock.Register("lifecycle_death_check", TickPhase.LowAltitude, cadence: 1,
            tick => lifecycle.CheckDeaths(tick, worldState.QueryCache.ActiveEntities()));
        clock.Register("relationships", TickPhase.LowAltitude, cadence: 1,
            tick => relationships.Tick(tick, relContext));
        clock.Register("resources", TickPhase.LowAltitude, cadence: 1,
            tick => resources.Tick(tick));
        clock.Register("processes", TickPhase.LowAltitude, cadence: 1,
            _ => evaluator.EvaluateAll(graph, worldState));
        clock.Register("health_recording", TickPhase.LowAltitude, cadence: 1,
            tick =>
            {
                foreach (var e in worldState.QueryCache.ActiveEntities())
                {
                    var healthAttr = domainConfig.Kinds.TryGetValue(e.Kind, out var kc) ? kc.HealthAttribute : null;
                    if (healthAttr is not null)
                    {
                        e.RecordHealth(tick, e.Attributes.GetValue(healthAttr));
                    }
                }
            });

        // Phase 2: Pressure (every 5 ticks for efficiency)
        clock.Register("pressure", TickPhase.Pressure, cadence: 5,
            _ =>
            {
                var contexts = BuildPressureContexts(graph, coverageMap);
                pressure.Compute(contexts);
            });

        // Phase 4: Card execution
        clock.Register("tableau", TickPhase.CardExecution, cadence: 1,
            tick =>
            {
                var cardContext = new CardMutationContext(graph, pressure, worldState, messageBus);
                tableau.Tick(tick, cardContext);
                var activeGeneratorIds = tableau.ActiveCards
                    .Select(card => card.Definition.GeneratorId)
                    .Where(generatorId => !string.IsNullOrWhiteSpace(generatorId))
                    .Select(generatorId => generatorId!)
                    .ToHashSet(StringComparer.Ordinal);
                generatorSystem.Tick(tick, activeGeneratorIds);
            });

        // Phase 5: Degeneracy detection (every 10 ticks)
        clock.Register("degeneracy", TickPhase.EventNotification, cadence: 10,
            tick =>
            {
                var degCtx = BuildDegeneracyContext(graph);
                detectors.Evaluate(degCtx, tick);
            });

        return new SimulationRunner(
            domainConfig, graph, effects, grid, coverageMap, spatialIndex,
            clock, evaluator, relationships, lifecycle, resources, pressure,
            tableau, eventLog, detectors, worldState, definitionGraph, kindRegistry,
            behaviorDefinitions, generatorSystem, spawnCards, messageBus);
    }

    /// <summary>Advance the simulation by one frame (speed-dependent tick count).</summary>
    public int AdvanceFrame() => Clock.AdvanceFrame();

    /// <summary>Advance exactly one tick.</summary>
    public bool Tick() => Clock.Tick();

    private static Dictionary<EntityId, IPressureContext> BuildPressureContexts(
        WorldGraph graph, MapCoverageMap coverageMap)
    {
        var contexts = new Dictionary<EntityId, IPressureContext>();
        foreach (var coverage in coverageMap.AllCoverages)
        {
            var entitiesInRegion = new List<EntityId>();
            foreach (var (eid, entity) in graph.AllEntities)
            {
                if (coverage.ContainsCell(new Map.HexCoord(entity.Coordinates.Q, entity.Coordinates.R)))
                {
                    entitiesInRegion.Add(eid);
                }
            }

            contexts[coverage.OwnerEntityId] = new SimplePressureContext(graph, entitiesInRegion);
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
    WorldGraph graph, PressureSystem pressure, WorldState worldState, MessageBus messageBus,
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
        GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;
    public int CountEntitiesByKind(EntityKind kind) => graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        worldState.QueryCache.FindRelationship(sourceId, targetId, kind);

    public int? GetLastFiredTick() => null; // Per-card, handled by CardInstance
    public Entity? GetEntity(EntityId id) => worldState.QueryCache.GetEntity(id);

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) =>
        worldState.QueryCache.FindRelatedEntity(sourceId, relationshipKind);

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) =>
        graph.GetEntitiesByKind(kind);

    public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType)
    {
        if (worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        return resourceType switch
        {
            Resources.ResourceType.Food => worldState.Resources.Food.GetStockpile(coord),
            Resources.ResourceType.Wealth => worldState.Resources.Wealth.GetStockpile(coord),
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
        var entity = graph.GetEntity(entityId);
        var oldValue = entity?.Attributes.GetValue(attributeName);
        entity?.Attributes.Modify(attributeName, delta);
        if (entity is not null && oldValue is { } before)
        {
            var after = entity.Attributes.GetValue(attributeName);
            if (Math.Abs(after - before) > float.Epsilon)
            {
                messageBus.Publish(new AttributeChangedMessage(
                    CurrentTick,
                    entityId,
                    attributeName,
                    before,
                    after,
                    "card"));
            }
        }
    }

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) =>
        pressure.ApplyDelta(pressureName, scopeId, delta);

    public void ModifyResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float delta)
    {
        if (worldState.Resources is null) return;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        var layer = resourceType switch
        {
            Resources.ResourceType.Food => worldState.Resources.Food,
            Resources.ResourceType.Wealth => worldState.Resources.Wealth,
            Resources.ResourceType.Population => worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return;
        var before = layer.GetStockpile(coord);
        layer.AddToStockpile(coord, delta);
        var after = layer.GetStockpile(coord);
        if (Math.Abs(after - before) > float.Epsilon)
        {
            worldState.QueryCache.InvalidateResources();
            messageBus.Publish(new ResourceChangedMessage(
                CurrentTick,
                resourceType.ToString(),
                coordinates,
                before,
                after,
                "card"));
        }
    }

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
            Resources.ResourceType.Food => worldState.Resources.Food,
            Resources.ResourceType.Wealth => worldState.Resources.Wealth,
            Resources.ResourceType.Population => worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return 0f;
        var available = layer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        layer.AddToStockpile(coord, -consumed);
        if (consumed > 0f)
        {
            worldState.QueryCache.InvalidateResources();
            messageBus.Publish(new ResourceChangedMessage(
                CurrentTick,
                resourceType.ToString(),
                coordinates,
                available,
                available - consumed,
                "card"));
        }
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r)
    {
        var entity = GetEntity(entityId);
        var previous = entity?.Coordinates;
        graph.SetCoordinates(entityId, q, r);
        if (previous is { } from && from != (q, r))
        {
            messageBus.Publish(new EntityMovedMessage(CurrentTick, entityId, from, (q, r)));
        }
    }

    public void SetSemanticCoordinates(EntityId entityId, int q, int r)
    {
        var entity = GetEntity(entityId);
        if (entity is null)
        {
            return;
        }

        var previous = entity.SemanticPosition;
        entity.SemanticPosition = new SemanticHexCoord(q, r);
        if (previous != entity.SemanticPosition)
        {
            messageBus.Publish(new EntitySemanticMovedMessage(
                CurrentTick,
                entityId,
                (previous.Q, previous.R, previous.S),
                (entity.SemanticPosition.Q, entity.SemanticPosition.R, entity.SemanticPosition.S)));
        }
    }

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var relationships = worldState.QueryCache.OutgoingRelationships(sourceId, relationshipKind);
        var rel = relationships.Count == 0 ? null : relationships[0];
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
        graph.GetEntitiesByKind(IceRemembersKinds.Settlement).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);

    public int DecliningSettlementCount =>
        graph.GetEntitiesByKind(IceRemembersKinds.Settlement).Count(id =>
            graph.GetEntity(id)?.Attributes.GetScalar("residents")?.Trend < 0);

    public int ActiveFactionCount =>
        graph.GetEntitiesByKind(IceRemembersKinds.Faction).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);

    public int MaxFactionSettlementCount
    {
        get
        {
            var max = 0;
            foreach (var factionId in graph.GetEntitiesByKind(IceRemembersKinds.Faction))
            {
                var count = graph.GetEntitiesByKind(IceRemembersKinds.Settlement)
                    .Count(sid => graph.OutgoingRelationships(sid, "controlled_by")
                        .Any(r => r.TargetId == factionId));
                max = Math.Max(max, count);
            }

            return max;
        }
    }

    public int TotalSettlementCount => graph.GetEntitiesByKind(IceRemembersKinds.Settlement).Count;

    public int ActiveNpcCount =>
        graph.GetEntitiesByKind(IceRemembersKinds.Penguin).Count(id =>
            graph.GetEntity(id)?.Status == EntityStatus.Active);
}
