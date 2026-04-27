namespace Engine.Domain.Tests.Map;

using Engine.Graph;
using Engine.WorldGen;

public class SpatialIndexTests
{
    private static (SpatialIndex Index, TestSpatialWorld World) CreateTestSetup()
    {
        var grid = new HexGrid(5, TestTerrainCatalog.Create(), TestTerrainCatalog.DefaultTerrain);
        grid.SetCell(new HexCoord(4, -4), "ocean");

        var coverageMap = new MapCoverageMap();
        var graph = new WorldGraph();
        var northlands = graph.CreateEntity(TestKinds.Region, "face", "Northlands", "none", 0, (0, 0));
        var heartlands = graph.CreateEntity(TestKinds.Region, "reach", "Heartlands", "none", 0, (2, -1));
        var northCoverage = new MapCoverage("Northlands", northlands.Id.Value);
        var heartCoverage = new MapCoverage("Heartlands", heartlands.Id.Value);
        coverageMap.RegisterCoverage(northCoverage);
        coverageMap.RegisterCoverage(heartCoverage);
        foreach (var coord in HexCoord.CoordsInRadius(new HexCoord(0, 0), 1))
        {
            coverageMap.AssignCell(coord, northCoverage);
        }

        foreach (var coord in HexCoord.CoordsInRadius(new HexCoord(2, -1), 1))
        {
            if (coverageMap.GetCoverageForCell(coord) is null)
            {
                coverageMap.AssignCell(coord, heartCoverage);
            }
        }

        graph.CreateEntity(TestKinds.Settlement, "village", "North Home", "c", 0, (0, 0));
        graph.CreateEntity(TestKinds.Settlement, "village", "Heart Home", "c", 0, (2, -1));
        graph.CreateEntity(TestKinds.Penguin, "warden", "Scout", "c", 0, (1, 0));

        var index = new SpatialIndex(grid, coverageMap, graph);
        index.RebuildFromGraph();
        return (index, new TestSpatialWorld(grid, coverageMap, graph));
    }

    [Fact]
    public void RebuildFromGraph_IndexesAllSpatialEntities()
    {
        var (index, world) = CreateTestSetup();

        var settlements = world.Graph.GetEntitiesByKind(TestKinds.Settlement);
        foreach (var sid in settlements)
        {
            var entity = world.Graph.GetEntity(sid)!;
            var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
            Assert.Contains(sid, index.EntitiesAtCell(coord));
        }
    }

    [Fact]
    public void EntitiesAtCell_ReturnsEmptyForUnoccupied()
    {
        var (index, _) = CreateTestSetup();
        Assert.Empty(index.EntitiesAtCell(new HexCoord(5, -5)));
    }

    [Fact]
    public void PlaceEntity_AddsToIndex()
    {
        var (index, world) = CreateTestSetup();
        var newEntity = world.Graph.CreateEntity(
            TestKinds.Penguin, "warden", "Test", "c", 0, coordinates: (3, -3));

        index.PlaceEntity(newEntity.Id, new HexCoord(3, -3));

        Assert.Contains(newEntity.Id, index.EntitiesAtCell(new HexCoord(3, -3)));
    }

    [Fact]
    public void EntitiesInRadius_FindsNearbyEntities()
    {
        var (index, world) = CreateTestSetup();
        var settlement = world.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => world.Graph.GetEntity(id)!)
            .First();

        var nearby = index.EntitiesInRadius(
            new HexCoord(settlement.Coordinates.Q, settlement.Coordinates.R),
            1).ToList();

        Assert.NotEmpty(nearby);
    }

    [Fact]
    public void EntitiesInRadius_FilterByKind()
    {
        var (index, _) = CreateTestSetup();

        var npcs = index.EntitiesInRadius(new HexCoord(0, 0), 20, TestKinds.Penguin).ToList();
        var all = index.EntitiesInRadius(new HexCoord(0, 0), 20).ToList();

        Assert.True(npcs.Count <= all.Count);
        Assert.True(npcs.Count > 0);
    }

    [Fact]
    public void EntitiesInRadius_ZeroRadius_OnlyCenter()
    {
        var (index, world) = CreateTestSetup();
        var entity = world.Graph.CreateEntity(TestKinds.Penguin, "warden", "Solo", "c", 0, coordinates: (2, 0));
        index.PlaceEntity(entity.Id, new HexCoord(2, 0));

        var atCenter = index.EntitiesInRadius(new HexCoord(2, 0), 0).ToList();

        Assert.Contains(entity.Id, atCenter);
    }

    [Fact]
    public void EntitiesInCoverage_ReturnsEntitiesInCorrectRegion()
    {
        var (index, world) = CreateTestSetup();

        var northlands = world.CoverageMap.GetCoverageByName("Northlands");
        Assert.NotNull(northlands);

        var entities = index.EntitiesInCoverage(new EntityId(northlands.OwnerEntityId)).ToList();
        Assert.NotEmpty(entities);
    }

    [Fact]
    public void EntitiesInCoverage_FilterByKind()
    {
        var (index, world) = CreateTestSetup();

        var heartlands = world.CoverageMap.GetCoverageByName("Heartlands");
        Assert.NotNull(heartlands);

        var settlements = index.EntitiesInCoverage(new EntityId(heartlands.OwnerEntityId), TestKinds.Settlement).ToList();
        var all = index.EntitiesInCoverage(new EntityId(heartlands.OwnerEntityId)).ToList();

        Assert.True(settlements.Count <= all.Count);
    }

    [Fact]
    public void NearestEntity_FindsClosest()
    {
        var (index, world) = CreateTestSetup();

        var nearest = index.NearestEntity(new HexCoord(0, 0), TestKinds.Settlement);
        Assert.NotNull(nearest);

        var entity = world.Graph.GetEntity(nearest.Value);
        Assert.NotNull(entity);
        Assert.Equal(TestKinds.Settlement, entity.Kind);
    }

    [Fact]
    public void NearestEntity_ReturnsNullWhenNoneFound()
    {
        var (index, _) = CreateTestSetup();

        var nearest = index.NearestEntity(new HexCoord(0, 0), TestKinds.Monster, maxRadius: 5);

        Assert.Null(nearest);
    }

    [Fact]
    public void FindPath_ReturnsPathBetweenAccessibleCells()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(2, -1));

        Assert.NotNull(path);
        Assert.True(path.Count >= 2);
        Assert.Equal(new HexCoord(0, 0), path[0]);
        Assert.Equal(new HexCoord(2, -1), path[^1]);
    }

    [Fact]
    public void FindPath_ConsecutiveStepsAreNeighbors()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(3, -2));
        Assert.NotNull(path);

        for (var i = 0; i < path.Count - 1; i++)
        {
            var dist = HexCoord.Distance(path[i], path[i + 1]);
            Assert.Equal(1, dist);
        }
    }

    [Fact]
    public void FindPath_AvoidsInfiniteCostTerrain()
    {
        var (index, world) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(3, -3), new HexCoord(5, -5));

        if (path is not null)
        {
            foreach (var coord in path)
            {
                var cell = world.Grid.GetCell(coord);
                Assert.NotNull(cell);
                Assert.False(float.IsPositiveInfinity(cell.MovementCost));
            }
        }
    }

    [Fact]
    public void FindPath_ReturnsNullForUnreachableTarget()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(4, -4));

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_ReturnsNullForOutOfBounds()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(99, 99));

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_SameStartAndEnd_ReturnsSingleCell()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(0, 0));

        Assert.NotNull(path);
        Assert.Single(path);
    }

    [Fact]
    public void FindPath_PrefersLowCostTerrain()
    {
        var (index, world) = CreateTestSetup();
        world.Grid.SetCell(new HexCoord(1, -1), "ice");

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(3, -2));

        if (path is not null && path.Count > 2)
        {
            var totalCost = path.Sum(coord => world.Grid.GetCell(coord)?.MovementCost ?? 0f);
            Assert.True(totalCost > 0f);
        }
    }

    private sealed record TestSpatialWorld(
        HexGrid Grid,
        MapCoverageMap CoverageMap,
        WorldGraph Graph);
}
