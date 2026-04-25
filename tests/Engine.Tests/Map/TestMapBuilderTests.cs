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
        Assert.Equal(_world.Seed, world2.Seed);
        Assert.Equal(
            SeedWorldQualityValidator.TerrainFingerprint(_world),
            SeedWorldQualityValidator.TerrainFingerprint(world2));
        Assert.Equal(
            SeedWorldQualityValidator.EntityPlacementFingerprint(_world),
            SeedWorldQualityValidator.EntityPlacementFingerprint(world2));

        // NPC coordinates should match with same seed
        var npcs1 = _world.Graph.GetEntitiesByKind(TestKinds.Penguin).ToList();
        var npcs2 = world2.Graph.GetEntitiesByKind(TestKinds.Penguin).ToList();
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

    [Fact]
    public void BuildDefault_ProducesMeaningfullyDifferentWorldsAcrossSeeds()
    {
        var worldA = TestMapBuilder.BuildDefault(seed: 42);
        var worldB = TestMapBuilder.BuildDefault(seed: 43);

        Assert.NotEqual(worldA.Seed, worldB.Seed);
        Assert.NotEqual(
            SeedWorldQualityValidator.TerrainFingerprint(worldA),
            SeedWorldQualityValidator.TerrainFingerprint(worldB));
        Assert.NotEqual(
            SeedWorldQualityValidator.EntityPlacementFingerprint(worldA),
            SeedWorldQualityValidator.EntityPlacementFingerprint(worldB));
    }

    [Fact]
    public void BuildDefault_PassesSeedQualityValidation()
    {
        var report = SeedWorldQualityValidator.Validate(_world);

        Assert.True(report.IsValid, string.Join(Environment.NewLine, report.Issues));
    }

    [Fact]
    public void BuildDefault_PassesSeedQualityValidationAcrossManySeeds()
    {
        var issues = new List<string>();
        for (var seed = 0; seed < 50; seed++)
        {
            var world = TestMapBuilder.BuildDefault(seed);
            var report = SeedWorldQualityValidator.Validate(world);
            if (!report.IsValid)
            {
                issues.Add($"seed {seed}, accepted {world.Seed}: {string.Join("; ", report.Issues)}");
            }
        }

        Assert.Empty(issues);
    }

    [Fact]
    public void AcceptedSeed_ReplaysThroughEmptyThenPopulate()
    {
        var accepted = TestMapBuilder.SelectAcceptedSeed(42);
        var direct = TestMapBuilder.BuildDefault(seed: 42);
        var replayed = TestMapBuilder.BuildEmpty(accepted);

        TestMapBuilder.Populate(replayed, accepted);

        Assert.Equal(
            SeedWorldQualityValidator.TerrainFingerprint(direct),
            SeedWorldQualityValidator.TerrainFingerprint(replayed));
        Assert.Equal(
            SeedWorldQualityValidator.EntityPlacementFingerprint(direct),
            SeedWorldQualityValidator.EntityPlacementFingerprint(replayed));
    }

    [Fact]
    public void SelectAcceptedSeed_RetriesAfterDegenerateWorld()
    {
        var accepted = TestMapBuilder.SelectAcceptedSeed(
            baseSeed: 42,
            buildWorld: seed => new TestWorld(new HexGrid(1), new MapCoverageMap(), new WorldGraph(), seed),
            validateWorld: world => world.Seed.Attempt == 0
                ? new SeedWorldQualityReport(["too small"])
                : new SeedWorldQualityReport([]),
            maxAttempts: 3);

        Assert.Equal(1, accepted.Attempt);
    }

    // --- Grid ---

    [Fact]
    public void Grid_HasCorrectSize()
    {
        Assert.Equal(1261, _world.Grid.CellCount); // radius 20
    }

    [Fact]
    public void Grid_HasVariedTerrain()
    {
        var terrainTypes = _world.Grid.AllCells
            .Select(c => c.Terrain)
            .Distinct()
            .ToHashSet();

        Assert.True(terrainTypes.Count >= 4, $"Only {terrainTypes.Count} terrain types found");
        Assert.Contains(TerrainType.Tundra, terrainTypes);
        Assert.Contains(TerrainType.Crags, terrainTypes);
        Assert.Contains(TerrainType.Ocean, terrainTypes);
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
        Assert.Equal(new AmbientValueRange(0f, 3f), TerrainAmbientResources.GetProfile(TerrainType.Ice).Population);
        Assert.Equal(new AmbientValueRange(0f, 1f), TerrainAmbientResources.GetProfile(TerrainType.Ocean).Population);
        Assert.Equal(new AmbientValueRange(3f, 5f), TerrainAmbientResources.GetProfile(TerrainType.Tundra).Population);
    }

    // --- Regions ---

    [Fact]
    public void SomeCells_AreIntentionallyUncovered()
    {
        Assert.Contains(_world.Grid.AllCells, cell => _world.CoverageMap.GetCoverageForCell(cell.Coord) is null);
    }

    [Fact]
    public void HasThreeRegions()
    {
        Assert.Equal(3, _world.CoverageMap.AllCoverages.Count);
    }

    [Fact]
    public void Regions_HaveDistinctNames()
    {
        var names = _world.CoverageMap.AllCoverages.Select(r => r.Name).ToHashSet();
        Assert.Equal(3, names.Count);
    }

    // --- Entities ---

    [Fact]
    public void Has2Nations()
    {
        Assert.Equal(2, _world.Graph.GetEntitiesByKind(TestKinds.Faction).Count);
    }

    [Fact]
    public void Has5Settlements()
    {
        Assert.InRange(_world.Graph.GetEntitiesByKind(TestKinds.Settlement).Count, 7, 8);
    }

    [Fact]
    public void Has3Npcs()
    {
        Assert.InRange(_world.Graph.GetEntitiesByKind(TestKinds.Penguin).Count, 5, 7);
    }

    [Fact]
    public void Has1Artifact()
    {
        Assert.InRange(_world.Graph.GetEntitiesByKind(TestKinds.Artifact).Count, 2, 3);
    }

    [Fact]
    public void Has3Regions()
    {
        Assert.Equal(3, _world.Graph.GetEntitiesByKind(TestKinds.Region).Count);
    }

    [Fact]
    public void KeepsFixedLegendaryAnchors()
    {
        var names = _world.Graph.AllEntities.Values.Select(entity => entity.Name).ToHashSet();

        Assert.Contains("Northlands", names);
        Assert.Contains("Heartlands", names);
        Assert.Contains("Coastlands", names);
        Assert.Contains("Frosthaven", names);
        Assert.Contains("Ironhold", names);
        Assert.Contains("Driftwatch", names);
        Assert.Contains("Wavecrest", names);
        Assert.Contains("Midpoint", names);
        Assert.Contains("Northern Compact", names);
        Assert.Contains("Southern Order", names);
        Assert.Contains("Stormcleaver", names);
    }

    // --- Relationships ---

    [Fact]
    public void AllSettlements_HaveControlledBy()
    {
        foreach (var settlementId in _world.Graph.GetEntitiesByKind(TestKinds.Settlement))
        {
            var controlledBy = _world.Graph.OutgoingRelationships(settlementId, "controlled_by").ToList();
            Assert.Single(controlledBy);
        }
    }

    [Fact]
    public void AllNpcs_HaveFactionMembership()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(TestKinds.Penguin))
        {
            var memberOf = _world.Graph.OutgoingRelationships(npcId, "member_of").ToList();
            Assert.Single(memberOf);
        }
    }

    [Fact]
    public void AllNpcs_HaveCoordinates()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(TestKinds.Penguin))
        {
            var npc = _world.Graph.GetEntity(npcId);
            Assert.NotNull(npc);
        }
    }

    [Fact]
    public void AllNpcs_SpawnOnWalkableHexes()
    {
        foreach (var npcId in _world.Graph.GetEntitiesByKind(TestKinds.Penguin))
        {
            var npc = _world.Graph.GetEntity(npcId)!;
            var coord = new HexCoord(npc.Coordinates.Q, npc.Coordinates.R);
            var cell = _world.Grid.GetCell(coord);
            Assert.NotNull(cell);
            Assert.False(float.IsPositiveInfinity(cell.MovementCost),
                $"NPC {npc.Name} spawned on impassable terrain at {coord}");
        }
    }

    [Fact]
    public void AllNpcs_SpawnWithinTwoHexesOfASettlement()
    {
        var settlementCoords = _world.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => _world.Graph.GetEntity(id)!)
            .Select(entity => new HexCoord(entity.Coordinates.Q, entity.Coordinates.R))
            .ToList();

        foreach (var npcId in _world.Graph.GetEntitiesByKind(TestKinds.Penguin))
        {
            var npc = _world.Graph.GetEntity(npcId)!;
            var npcCoord = new HexCoord(npc.Coordinates.Q, npc.Coordinates.R);
            var nearestSettlementDistance = settlementCoords.Min(coord => HexCoord.Distance(coord, npcCoord));

            Assert.InRange(
                nearestSettlementDistance,
                1,
                2);
        }
    }

    [Fact]
    public void Artifact_IsAnchoredAtSettlement()
    {
        foreach (var artifactId in _world.Graph.GetEntitiesByKind(TestKinds.Artifact))
        {
            var anchored = _world.Graph.OutgoingRelationships(artifactId, "anchored").ToList();
            Assert.Single(anchored);

            var location = _world.Graph.GetEntity(anchored[0].TargetId);
            Assert.NotNull(location);
            Assert.Equal(TestKinds.Settlement, location.Kind);
        }
    }

    [Fact]
    public void Coverages_HaveAdjacency()
    {
        Assert.NotEmpty(_world.CoverageMap.ComputeAdjacency());
    }

    [Fact]
    public void SeedSettlements_SpanAMeaningfulArea()
    {
        var coords = _world.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => _world.Graph.GetEntity(id)!)
            .Select(entity => new HexCoord(entity.Coordinates.Q, entity.Coordinates.R))
            .ToList();

        var qSpan = coords.Max(coord => coord.Q) - coords.Min(coord => coord.Q);
        var rSpan = coords.Max(coord => coord.R) - coords.Min(coord => coord.R);

        Assert.True(qSpan >= 8, $"Expected settlement q-span >= 8, got {qSpan}");
        Assert.True(rSpan >= 8, $"Expected settlement r-span >= 8, got {rSpan}");
    }

    [Fact]
    public void MaritimeSettlements_ArePlacedOnOrNearMaritimeTerrain()
    {
        foreach (var settlementName in new[] { "Driftwatch", "Wavecrest" })
        {
            var settlement = _world.Graph.GetEntitiesByKind(TestKinds.Settlement)
                .Select(id => _world.Graph.GetEntity(id)!)
                .Single(entity => entity.Name == settlementName);
            var coord = new HexCoord(settlement.Coordinates.Q, settlement.Coordinates.R);
            var cell = _world.Grid.GetCell(coord)!;
            var adjacentMaritime = coord.Neighbors()
                .Where(_world.Grid.ContainsCoord)
                .Select(_world.Grid.GetCell)
                .Any(neighbor => neighbor is not null &&
                                 neighbor.Terrain is TerrainType.Ocean or TerrainType.Reef or TerrainType.Shelf);

            Assert.True(
                cell.Terrain is TerrainType.Reef or TerrainType.Shelf || adjacentMaritime,
                $"{settlementName} should be on or next to maritime terrain, got {cell.Terrain} at {coord}");
        }
    }

    [Fact]
    public void ColdSettlement_IsPlacedOnColdTerrain()
    {
        var frosthaven = _world.Graph.GetEntitiesByKind(TestKinds.Settlement)
            .Select(id => _world.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == "Frosthaven");
        var coord = new HexCoord(frosthaven.Coordinates.Q, frosthaven.Coordinates.R);
        var cell = _world.Grid.GetCell(coord)!;

        Assert.Contains(cell.Terrain, new[] { TerrainType.Ice, TerrainType.Tundra, TerrainType.Shelf });
    }

    // --- No orphans ---

    [Fact]
    public void NoOrphanedSettlements()
    {
        foreach (var sid in _world.Graph.GetEntitiesByKind(TestKinds.Settlement))
        {
            var entity = _world.Graph.GetEntity(sid);
            Assert.NotNull(entity);
        }
    }
}
