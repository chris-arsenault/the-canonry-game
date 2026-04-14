namespace Engine.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Map;

/// <summary>
/// Minimal IWorldState stub for testing impulse evaluation without full world setup.
/// </summary>
internal sealed class StubWorldState : IWorldState
{
    public WorldGraph Graph { get; } = new();
    public EffectSystem Effects { get; } = new();
    public HexGrid Grid { get; }
    public RegionMap RegionMap { get; } = new();
    public SpatialIndex SpatialIndex { get; }
    public Engine.Resources.ResourceSystem? Resources { get; }
    public int CurrentTick { get; set; }

    public StubWorldState(int gridRadius = 1, bool withResources = false)
    {
        Grid = new HexGrid(gridRadius);
        SpatialIndex = new SpatialIndex(Grid, RegionMap, Graph);
        if (withResources)
        {
            Resources = new Engine.Resources.ResourceSystem(Graph, Grid);
        }
    }
}
