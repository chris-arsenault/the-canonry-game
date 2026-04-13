namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class TestMapBuilderTests
{
    private readonly TestWorld _world = TestMapBuilder.BuildDefault();

    // --- Determinism ---

    [Fact]
    public void BuildDefault_IsDeterministic()
    {
        var world2 = TestMapBuilder.BuildDefault();
        Assert.Equal(_world.Grid.CellCount, world2.Grid.CellCount);
        Assert.Equal(_world.Graph.EntityCount, world2.Graph.EntityCount);
    }

    // --- Grid ---

    [Fact]
    public void Grid_HasCorrectSize()
    {
        Assert.Equal(91, _world.Grid.CellCount); // radius 5
    }

    [Fact]
    public void Grid_HasVariedTerrain()
    {
        var terrainTypes = _world.Grid.AllCells
            .Select(c => c.Terrain)
            .Distinct()
            .ToHashSet();

        Assert.True(terrainTypes.Count >= 4, $"Only {terrainTypes.Count} terrain types found");
        Assert.Contains(TerrainType.Plains, terrainTypes);
        Assert.Contains(TerrainType.Mountain, terrainTypes);
        Assert.Contains(TerrainType.Water, terrainTypes);
    }

    // --- Regions ---

    [Fact]
    public void AllCells_AssignedToRegions()
    {
        foreach (var cell in _world.Grid.AllCells)
        {
            Assert.NotNull(_world.RegionMap.GetRegionForCell(cell.Coord));
        }
    }

    [Fact]
    public void HasThreeRegions()
    {
        Assert.Equal(3, _world.RegionMap.AllRegions.Count);
    }

    [Fact]
    public void Regions_HaveDistinctNames()
    {
        var names = _world.RegionMap.AllRegions.Select(r => r.Name).ToHashSet();
        Assert.Equal(3, names.Count);
    }

    // --- Entities ---

    [Fact]
    public void Has2Nations()
    {
        Assert.Equal(2, _world.Graph.GetEntitiesByKind(EntityKind.Nation).Count);
    }

    [Fact]
    public void Has5Settlements()
    {
        Assert.Equal(5, _world.Graph.GetEntitiesByKind(EntityKind.Settlement).Count);
    }

    [Fact]
    public void Has3Npcs()
    {
        Assert.Equal(3, _world.Graph.GetEntitiesByKind(EntityKind.Npc).Count);
    }

    [Fact]
    public void Has1Artifact()
    {
        Assert.Single(_world.Graph.GetEntitiesByKind(EntityKind.Artifact));
    }

    [Fact]
    public void Has3Regions()
    {
        Assert.Equal(3, _world.Graph.GetEntitiesByKind(EntityKind.Region).Count);
    }

    // --- Relationships ---

    [Fact]
    public void AllSettlements_HaveControlledBy()
    {
        foreach (var settlementId in _world.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var controlledBy = _world.Graph.OutgoingRelationships(settlementId, "controlled_by").ToList();
            Assert.Single(controlledBy);
        }
    }

    [Fact]
    public void AllNpcs_HaveLoyalty()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(EntityKind.Npc))
        {
            var loyalty = _world.Graph.OutgoingRelationships(npcId, "loyal_to").ToList();
            Assert.Single(loyalty);
        }
    }

    [Fact]
    public void AllNpcs_HaveLocation()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(EntityKind.Npc))
        {
            var location = _world.Graph.OutgoingRelationships(npcId, "located_at").ToList();
            Assert.Single(location);
        }
    }

    [Fact]
    public void Artifact_IsHeldByNpc()
    {
        var artifactId = _world.Graph.GetEntitiesByKind(EntityKind.Artifact).First();
        var holders = _world.Graph.IncomingRelationships(artifactId, "holds").ToList();
        Assert.Single(holders);

        var holder = _world.Graph.GetEntity(holders[0].SourceId);
        Assert.NotNull(holder);
        Assert.Equal(EntityKind.Npc, holder.Kind);
    }

    [Fact]
    public void Regions_HaveBorderRelationships()
    {
        var regionIds = _world.Graph.GetEntitiesByKind(EntityKind.Region);
        var totalBorders = 0;
        foreach (var regionId in regionIds)
        {
            totalBorders += _world.Graph.OutgoingRelationships(regionId, "borders").Count();
        }

        // Should have border relationships (at least some regions border each other)
        Assert.True(totalBorders > 0);
    }

    // --- No orphans ---

    [Fact]
    public void NoOrphanedSettlements()
    {
        foreach (var sid in _world.Graph.GetEntitiesByKind(EntityKind.Settlement))
        {
            var entity = _world.Graph.GetEntity(sid);
            Assert.NotNull(entity);
            Assert.NotNull(entity.Coordinates);
        }
    }
}
