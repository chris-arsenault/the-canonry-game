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
    public AttributeStore Attributes { get; }
    public EffectSystem Effects { get; }
    public HexGrid Grid { get; }
    public RegionMap RegionMap { get; }
    public SpatialIndex SpatialIndex { get; }
    public Resources.ResourceSystem? Resources { get; }
    public int CurrentTick { get; set; }

    public WorldState(
        WorldGraph graph,
        AttributeStore attributes,
        EffectSystem effects,
        HexGrid grid,
        RegionMap regionMap,
        SpatialIndex spatialIndex,
        Resources.ResourceSystem? resources = null)
    {
        Graph = graph;
        Attributes = attributes;
        Effects = effects;
        Grid = grid;
        RegionMap = regionMap;
        SpatialIndex = spatialIndex;
        Resources = resources;
    }
}
