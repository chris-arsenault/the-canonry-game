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
public sealed class WorldState : IWorldState
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
    public int CurrentTick { get; set; }

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
        Graph = graph;
        Effects = effects;
        Pressure = pressure;
        Grid = grid;
        CoverageMap = coverageMap;
        SpatialIndex = spatialIndex;
        SemanticPlane = semanticPlane;
        Generators = generators;
        Resources = resources;
    }
}
