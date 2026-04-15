namespace Engine.Simulation;

using Engine.Attributes;
using Engine.Behaviors;
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
    public RegionMap RegionMap { get; }
    public SpatialIndex SpatialIndex { get; }
    public Resources.ResourceSystem? Resources { get; }
    public int CurrentTick { get; set; }

    public WorldState(
        WorldGraph graph,
        EffectSystem effects,
        Engine.Pressure.PressureSystem? pressure,
        HexGrid grid,
        RegionMap regionMap,
        SpatialIndex spatialIndex,
        Resources.ResourceSystem? resources = null)
    {
        Graph = graph;
        Effects = effects;
        Pressure = pressure;
        Grid = grid;
        RegionMap = regionMap;
        SpatialIndex = spatialIndex;
        Resources = resources;
    }
}
