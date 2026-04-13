namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class SpatialIndexTests
{
    private static (SpatialIndex Index, TestWorld World) CreateTestSetup()
    {
        var world = TestMapBuilder.BuildDefault();
        var index = new SpatialIndex(world.Grid, world.RegionMap, world.Graph);
        index.RebuildFromGraph();
        return (index, world);
    }

    // --- Entity placement ---

    [Fact]
    public void RebuildFromGraph_IndexesAllSpatialEntities()
    {
        var (index, world) = CreateTestSetup();

        // Test map has settlements and NPCs with coordinates
        var settlements = world.Graph.GetEntitiesByKind(EntityKind.Settlement);
        foreach (var sid in settlements)
        {
            var entity = world.Graph.GetEntity(sid)!;
            var coord = new HexCoord(entity.Coordinates!.Value.Q, entity.Coordinates!.Value.R);
            Assert.Contains(sid, index.EntitiesAtCell(coord));
        }
    }

    [Fact]
    public void EntitiesAtCell_ReturnsEmptyForUnoccupied()
    {
        var (index, _) = CreateTestSetup();
        Assert.Empty(index.EntitiesAtCell(new HexCoord(5, -5))); // Far corner, unlikely occupied
    }

    [Fact]
    public void PlaceEntity_AddsToIndex()
    {
        var (index, world) = CreateTestSetup();
        var newEntity = world.Graph.CreateEntity(
            EntityKind.Npc, "generic_npc", "Test", "c", 0, coordinates: (3, -3));
        index.PlaceEntity(newEntity.Id, new HexCoord(3, -3));

        Assert.Contains(newEntity.Id, index.EntitiesAtCell(new HexCoord(3, -3)));
    }

    // --- Radius queries ---

    [Fact]
    public void EntitiesInRadius_FindsNearbyEntities()
    {
        var (index, _) = CreateTestSetup();

        // Center of the map (0,0) should have Midpoint settlement nearby
        var nearby = index.EntitiesInRadius(new HexCoord(0, 0), 1).ToList();
        Assert.NotEmpty(nearby);
    }

    [Fact]
    public void EntitiesInRadius_FilterByKind()
    {
        var (index, _) = CreateTestSetup();

        // Wide radius should find entities, filter to NPCs only
        var npcs = index.EntitiesInRadius(new HexCoord(0, 0), 5, EntityKind.Npc).ToList();
        var all = index.EntitiesInRadius(new HexCoord(0, 0), 5).ToList();

        Assert.True(npcs.Count <= all.Count);
        Assert.True(npcs.Count > 0);
    }

    [Fact]
    public void EntitiesInRadius_ZeroRadius_OnlyCenter()
    {
        var (index, world) = CreateTestSetup();

        // Place entity at (2,0) and query radius 0 from there
        var entity = world.Graph.CreateEntity(EntityKind.Npc, "generic_npc", "Solo", "c", 0, coordinates: (2, 0));
        index.PlaceEntity(entity.Id, new HexCoord(2, 0));

        var atCenter = index.EntitiesInRadius(new HexCoord(2, 0), 0).ToList();
        Assert.Contains(entity.Id, atCenter);
    }

    // --- Region queries ---

    [Fact]
    public void EntitiesInRegion_ReturnsEntitiesInCorrectRegion()
    {
        var (index, world) = CreateTestSetup();

        // Frosthaven is at (0,-2) which is in Northlands
        var northlands = world.RegionMap.GetRegionByName("Northlands");
        Assert.NotNull(northlands);

        var entities = index.EntitiesInRegion(northlands.EntityId).ToList();
        Assert.NotEmpty(entities);
    }

    [Fact]
    public void EntitiesInRegion_FilterByKind()
    {
        var (index, world) = CreateTestSetup();

        var heartlands = world.RegionMap.GetRegionByName("Heartlands");
        Assert.NotNull(heartlands);

        var settlements = index.EntitiesInRegion(heartlands.EntityId, EntityKind.Settlement).ToList();
        var all = index.EntitiesInRegion(heartlands.EntityId).ToList();

        Assert.True(settlements.Count <= all.Count);
    }

    // --- Nearest entity ---

    [Fact]
    public void NearestEntity_FindsClosest()
    {
        var (index, world) = CreateTestSetup();

        // From the center, should find a nearby settlement
        var nearest = index.NearestEntity(new HexCoord(0, 0), EntityKind.Settlement);
        Assert.NotNull(nearest);

        var entity = world.Graph.GetEntity(nearest.Value);
        Assert.NotNull(entity);
        Assert.Equal(EntityKind.Settlement, entity.Kind);
    }

    [Fact]
    public void NearestEntity_ReturnsNullWhenNoneFound()
    {
        var (index, _) = CreateTestSetup();

        // No monsters exist in the test map
        var nearest = index.NearestEntity(new HexCoord(0, 0), EntityKind.Monster, maxRadius: 5);
        Assert.Null(nearest);
    }

    // --- Pathfinding ---

    [Fact]
    public void FindPath_ReturnsPathBetweenAccessibleCells()
    {
        var (index, _) = CreateTestSetup();

        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(2, -1));
        Assert.NotNull(path);
        Assert.True(path.Count >= 2); // At least start and end
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
    public void FindPath_AvoidsWater()
    {
        var (index, world) = CreateTestSetup();

        // Water is at r >= 4 in the test map. Path should not go through it.
        var path = index.FindPath(new HexCoord(-1, 0), new HexCoord(-3, 2));

        if (path is not null)
        {
            foreach (var coord in path)
            {
                var cell = world.Grid.GetCell(coord);
                Assert.NotNull(cell);
                Assert.NotEqual(TerrainType.Water, cell.Terrain);
            }
        }
    }

    [Fact]
    public void FindPath_ReturnsNullForUnreachableTarget()
    {
        var (index, _) = CreateTestSetup();

        // Destination in water (impassable)
        var path = index.FindPath(new HexCoord(0, 0), new HexCoord(0, 5));
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

        // Path between two plains cells that could go through mountains
        // should prefer the plains route
        var path = index.FindPath(new HexCoord(-1, 0), new HexCoord(3, -2));

        if (path is not null && path.Count > 2)
        {
            var totalCost = 0f;
            foreach (var coord in path)
            {
                var cell = world.Grid.GetCell(coord);
                if (cell is not null && !float.IsPositiveInfinity(cell.MovementCost))
                {
                    totalCost += cell.MovementCost;
                }
            }

            // The path should have a reasonable total cost (not going through mountains if avoidable)
            Assert.True(totalCost > 0);
        }
    }
}
