namespace Engine.Domain.Tests.Behaviors;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Resources;
using Engine.WorldGen;

/// <summary>
/// Minimal IWorldState stub for testing impulse evaluation without full world setup.
/// </summary>
internal sealed class StubWorldState : IWorldState, IWorldVersionSink
{
    public WorldGraph Graph { get; } = new();
    public HexGrid Grid { get; }
    public MapCoverageMap CoverageMap { get; } = new();
    public SpatialIndex SpatialIndex { get; }
    public StubWorldResources? StubResources { get; }
    public IWorldResourceReader? Resources => StubResources;
    public int CurrentTick { get; set; }
    public long WorldVersion { get; private set; }

    public StubWorldState(int gridRadius = 1, bool withResources = false)
    {
        Grid = new HexGrid(gridRadius, TestTerrainCatalog.Create(), TestTerrainCatalog.DefaultTerrain);
        SpatialIndex = new SpatialIndex(Grid, CoverageMap, Graph);
        if (withResources)
        {
            StubResources = new StubWorldResources();
        }
    }

    public void AdvanceWorldVersion()
    {
        WorldVersion++;
    }
}

internal sealed class StubWorldResources : IWorldResourceReader
{
    private readonly Dictionary<(ResourceType Resource, HexCoord Coord), float> _values = [];

    public float GetStockpile(ResourceType resourceType, HexCoord coord) =>
        _values.GetValueOrDefault((resourceType, coord));

    public void SetStockpile(HexCoord coord, ResourceType resourceType, float value) =>
        _values[(resourceType, coord)] = Math.Max(0f, value);

    public void AddToStockpile(HexCoord coord, ResourceType resourceType, float delta) =>
        SetStockpile(coord, resourceType, GetStockpile(resourceType, coord) + delta);
}

internal static class TestTerrainCatalog
{
    public const string DefaultTerrain = "wastes";

    public static TerrainCatalog Create() => new(
    [
        new TerrainDefinition(DefaultTerrain, "Wastes", 1f, 0.2f, "land"),
        new TerrainDefinition("tundra", "Tundra", 1f, 0.6f, "land"),
        new TerrainDefinition("ice", "Ice", 2f, 0.1f, "frozen"),
        new TerrainDefinition("ocean", "Ocean", float.PositiveInfinity, 0f, "water"),
    ]);
}
