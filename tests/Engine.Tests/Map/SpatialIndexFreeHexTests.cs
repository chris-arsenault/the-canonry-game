namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class SpatialIndexFreeHexTests
{
    [Fact]
    public void OriginFree_ReturnsOrigin()
    {
        var (index, _, _) = CreateIndex(radius: 3);

        var result = index.FindFreePhysicalHex(new HexCoord(0, 0));

        Assert.Equal(new HexCoord(0, 0), result);
    }

    [Fact]
    public void OriginOccupied_ReturnsNearestRing1FreeCell()
    {
        var (index, graph, _) = CreateIndex(radius: 3);
        PlaceEntity(graph, index, new HexCoord(0, 0));

        var result = index.FindFreePhysicalHex(new HexCoord(0, 0));

        Assert.NotNull(result);
        Assert.Equal(1, HexCoord.Distance(new HexCoord(0, 0), result!.Value));
    }

    [Fact]
    public void Surrounded_ReturnsRing2Cell()
    {
        var origin = new HexCoord(0, 0);
        var (index, graph, _) = CreateIndex(radius: 3);
        foreach (var coord in HexCoord.CoordsInRadius(origin, 1))
        {
            PlaceEntity(graph, index, coord);
        }

        var result = index.FindFreePhysicalHex(origin);

        Assert.NotNull(result);
        Assert.Equal(2, HexCoord.Distance(origin, result!.Value));
    }

    [Fact]
    public void FullyExhaustedWithinMaxRadius_ReturnsNull()
    {
        var origin = new HexCoord(0, 0);
        var (index, graph, _) = CreateIndex(radius: 2);
        foreach (var coord in HexCoord.CoordsInRadius(origin, 1))
        {
            PlaceEntity(graph, index, coord);
        }

        var result = index.FindFreePhysicalHex(origin, maxRadius: 1);

        Assert.Null(result);
    }

    [Fact]
    public void Physical_RespectsWalkability()
    {
        var origin = new HexCoord(0, 0);
        var (index, _, grid) = CreateIndex(radius: 3);
        grid.SetCell(origin, TerrainType.Water);

        var result = index.FindFreePhysicalHex(origin);

        Assert.NotNull(result);
        Assert.NotEqual(origin, result!.Value);
        Assert.Equal(1, HexCoord.Distance(origin, result.Value));
    }

    private static (SpatialIndex Index, WorldGraph Graph, HexGrid Grid) CreateIndex(int radius)
    {
        var grid = new HexGrid(radius);
        var regionMap = new RegionMap();
        var graph = new WorldGraph();
        var index = new SpatialIndex(grid, regionMap, graph);
        return (index, graph, grid);
    }

    private static void PlaceEntity(WorldGraph graph, SpatialIndex index, HexCoord coord)
    {
        var entity = graph.CreateEntity(
            EntityKind.Npc,
            "generic_npc",
            $"entity_{coord.Q}_{coord.R}",
            "culture_a",
            tick: 0,
            coordinates: (coord.Q, coord.R));
        index.PlaceEntity(entity.Id, coord);
    }
}
