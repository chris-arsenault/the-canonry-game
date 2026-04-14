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
    public AttributeStore Attributes { get; } = new();
    public EffectSystem Effects { get; } = new();
    public HexGrid Grid { get; } = new(1);
    public RegionMap RegionMap { get; } = new();
    public SpatialIndex SpatialIndex { get; }
    public Engine.Resources.ResourceSystem? Resources => null;
    public int CurrentTick { get; set; }

    public StubWorldState()
    {
        SpatialIndex = new SpatialIndex(Grid, RegionMap, Graph);
    }
}
