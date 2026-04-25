namespace Engine.Simulation;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Generators;
using Engine.Graph;
using Engine.Map;

/// <summary>
/// Concrete IWorldState implementation. Bundles all system references
/// for impulse evaluation and behavior execution.
/// </summary>
public sealed class WorldState : ICachedWorldState, IWorldVersionSink
    {
    public WorldGraph Graph { get; }
    public EffectSystem Effects { get; }
    public Engine.Pressure.PressureSystem? Pressure { get; }
    public HexGrid Grid { get; }
    public MapCoverageMap CoverageMap { get; }
    public SpatialIndex SpatialIndex { get; }
    public SemanticPlaneIndex SemanticPlane { get; }
    public GeneratorInstanceRegistry Generators { get; }
    public Resources.ResourceSystem? Resources { get; }
    private int _currentTick;
    private long _worldVersion;

    public int CurrentTick
    {
        get => _currentTick;
        set
        {
            _currentTick = value;
            QueryCache.BeginTick(value);
            AdvanceWorldVersion();
        }
    }

    public long WorldVersion => _worldVersion;

    public WorldStateQueryCache QueryCache { get; }

    public WorldState(
        WorldGraph graph,
        EffectSystem effects,
        Engine.Pressure.PressureSystem? pressure,
        HexGrid grid,
        MapCoverageMap coverageMap,
        SpatialIndex spatialIndex,
        SemanticPlaneIndex semanticPlane,
        GeneratorInstanceRegistry generators,
        Resources.ResourceSystem? resources = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(coverageMap);
        ArgumentNullException.ThrowIfNull(spatialIndex);
        ArgumentNullException.ThrowIfNull(semanticPlane);
        ArgumentNullException.ThrowIfNull(generators);

        Graph = graph;
        Effects = effects;
        Pressure = pressure;
        Grid = grid;
        CoverageMap = coverageMap;
        SpatialIndex = spatialIndex;
        SemanticPlane = semanticPlane;
        Generators = generators;
        Resources = resources;
        QueryCache = new WorldStateQueryCache(this);

        graph.EntityCreated += (_, _) =>
        {
            QueryCache.InvalidateEntities();
            AdvanceWorldVersion();
        };
        graph.EntityStatusChanged += (_, _) =>
        {
            QueryCache.InvalidateEntities();
            AdvanceWorldVersion();
        };
        graph.RelationshipCreated += (_, _) =>
        {
            QueryCache.InvalidateRelationships();
            AdvanceWorldVersion();
        };
        graph.RelationshipCulled += (_, _) =>
        {
            QueryCache.InvalidateRelationships();
            AdvanceWorldVersion();
        };
        graph.RelationshipStrengthChanged += (_, _) =>
        {
            QueryCache.InvalidateRelationships();
            AdvanceWorldVersion();
        };
    }

    public void AdvanceWorldVersion()
    {
        _worldVersion++;
    }
}
