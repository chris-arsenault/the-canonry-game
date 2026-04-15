namespace Engine.Tests.Map;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;

public class TestMapBuilderTests
{
    private readonly TestWorld _world = TestMapBuilder.BuildDefault(seed: 42);

    // --- Determinism ---

    [Fact]
    public void BuildDefault_IsDeterministicWithSeed()
    {
        var world2 = TestMapBuilder.BuildDefault(seed: 42);
        Assert.Equal(_world.Grid.CellCount, world2.Grid.CellCount);
        Assert.Equal(_world.Graph.EntityCount, world2.Graph.EntityCount);

        // NPC coordinates should match with same seed
        var npcs1 = _world.Graph.GetEntitiesByKind(EntityKind.Npc).ToList();
        var npcs2 = world2.Graph.GetEntitiesByKind(EntityKind.Npc).ToList();
        for (var i = 0; i < npcs1.Count; i++)
        {
            var e1 = _world.Graph.GetEntity(npcs1[i])!;
            var e2 = world2.Graph.GetEntity(npcs2[i])!;
            Assert.Equal(e1.Coordinates, e2.Coordinates);
        }

        var cells1 = _world.Grid.AllCells.OrderBy(c => c.Coord.Q).ThenBy(c => c.Coord.R).ToList();
        var cells2 = world2.Grid.AllCells.OrderBy(c => c.Coord.Q).ThenBy(c => c.Coord.R).ToList();
        for (var i = 0; i < cells1.Count; i++)
        {
            Assert.Equal(cells1[i].AmbientFood, cells2[i].AmbientFood);
            Assert.Equal(cells1[i].AmbientWealth, cells2[i].AmbientWealth);
            Assert.Equal(cells1[i].AmbientPopulation, cells2[i].AmbientPopulation);
        }
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

    [Fact]
    public void AllCells_HaveAmbientResourcesWithinTerrainProfiles()
    {
        foreach (var cell in _world.Grid.AllCells)
        {
            var profile = TerrainAmbientResources.GetProfile(cell.Terrain);

            Assert.True(profile.Food.Contains(cell.AmbientFood),
                $"{cell.Coord} food {cell.AmbientFood} outside [{profile.Food.Min}, {profile.Food.Max}]");
            Assert.True(profile.Wealth.Contains(cell.AmbientWealth),
                $"{cell.Coord} wealth {cell.AmbientWealth} outside [{profile.Wealth.Min}, {profile.Wealth.Max}]");
            Assert.True(profile.Population.Contains(cell.AmbientPopulation),
                $"{cell.Coord} population {cell.AmbientPopulation} outside [{profile.Population.Min}, {profile.Population.Max}]");
        }
    }

    [Fact]
    public void TerrainProfiles_UseExpectedPopulationBands()
    {
        Assert.Equal(new AmbientValueRange(0f, 3f), TerrainAmbientResources.GetProfile(TerrainType.Tundra).Population);
        Assert.Equal(new AmbientValueRange(0f, 1f), TerrainAmbientResources.GetProfile(TerrainType.Water).Population);
        Assert.Equal(new AmbientValueRange(3f, 5f), TerrainAmbientResources.GetProfile(TerrainType.Plains).Population);
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
        Assert.Equal(2, _world.Graph.GetEntitiesByKind(EntityKind.Faction).Count);
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
    public void AllNpcs_HaveCoordinates()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(EntityKind.Npc))
        {
            var npc = _world.Graph.GetEntity(npcId);
            Assert.NotNull(npc);
            Assert.NotNull(npc.Coordinates);
        }
    }

    [Fact]
    public void AllNpcs_SpawnOnWalkableHexes()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(EntityKind.Npc))
        {
            var npc = _world.Graph.GetEntity(npcId)!;
            var coord = new HexCoord(npc.Coordinates!.Value.Q, npc.Coordinates!.Value.R);
            var cell = _world.Grid.GetCell(coord);
            Assert.NotNull(cell);
            Assert.False(float.IsPositiveInfinity(cell.MovementCost),
                $"NPC {npc.Name} spawned on impassable terrain at {coord}");
        }
    }

    [Fact]
    public void Artifact_IsStoredAtSettlement()
    {
        var artifactId = _world.Graph.GetEntitiesByKind(EntityKind.Artifact).First();
        var storedAt = _world.Graph.OutgoingRelationships(artifactId, "stored_at").ToList();
        Assert.Single(storedAt);

        var location = _world.Graph.GetEntity(storedAt[0].TargetId);
        Assert.NotNull(location);
        Assert.Equal(EntityKind.Settlement, location.Kind);
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
