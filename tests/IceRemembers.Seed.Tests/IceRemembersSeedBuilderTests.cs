namespace IceRemembers.Seed.Tests;

using Engine.Graph;
using Engine.WorldGen;
using EntityKind = Engine.Kinds.KindId;
using K = IceRemembers.IceRemembersKinds;

public sealed class IceRemembersSeedBuilderTests
{
    private readonly TestWorld _world = IceRemembersSeedBuilder.BuildDefault(seed: 42);

    [Fact]
    public void BuildDefault_IsDeterministicWithSeed()
    {
        var world2 = IceRemembersSeedBuilder.BuildDefault(seed: 42);

        Assert.Equal(_world.Grid.CellCount, world2.Grid.CellCount);
        Assert.Equal(_world.Graph.EntityCount, world2.Graph.EntityCount);
        Assert.Equal(_world.Seed, world2.Seed);
        Assert.Equal(
            IceRemembersSeedQualityValidator.TerrainFingerprint(_world),
            IceRemembersSeedQualityValidator.TerrainFingerprint(world2));
        Assert.Equal(
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(_world),
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(world2));
    }

    [Fact]
    public void BuildDefault_ProducesMeaningfullyDifferentWorldsAcrossSeeds()
    {
        var worldA = IceRemembersSeedBuilder.BuildDefault(seed: 42);
        var worldB = IceRemembersSeedBuilder.BuildDefault(seed: 43);

        Assert.NotEqual(worldA.Seed, worldB.Seed);
        Assert.NotEqual(
            IceRemembersSeedQualityValidator.TerrainFingerprint(worldA),
            IceRemembersSeedQualityValidator.TerrainFingerprint(worldB));
        Assert.NotEqual(
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(worldA),
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(worldB));
    }

    [Fact]
    public void BuildDefault_PassesSeedQualityValidationAcrossSeeds()
    {
        var issues = new List<string>();
        for (var seed = 0; seed < 20; seed++)
        {
            var world = IceRemembersSeedBuilder.BuildDefault(seed);
            var report = IceRemembersSeedQualityValidator.Validate(world);
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
        var accepted = IceRemembersSeedBuilder.SelectAcceptedSeed(42);
        var direct = IceRemembersSeedBuilder.BuildDefault(seed: 42);
        var replayed = IceRemembersSeedBuilder.BuildEmpty(accepted);

        IceRemembersSeedBuilder.Populate(replayed, accepted);

        Assert.Equal(
            IceRemembersSeedQualityValidator.TerrainFingerprint(direct),
            IceRemembersSeedQualityValidator.TerrainFingerprint(replayed));
        Assert.Equal(
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(direct),
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(replayed));
    }

    [Fact]
    public void BuildStartupSeed_PreservesGeneratedMapEntitiesAndRelationships()
    {
        var startupSeed = IceRemembersSeedBuilder.BuildStartupSeed(seed: 42);

        Assert.Equal(_world.Grid.Radius, startupSeed.MapRadius);
        Assert.Equal(_world.Grid.CellCount, startupSeed.Cells.Count);
        Assert.Equal(_world.Graph.EntityCount, startupSeed.Entities.Count);
        Assert.Contains(startupSeed.Entities, entity => entity.DisplayName == "Frosthaven");
        Assert.Contains(startupSeed.Cells, cell => cell.OwnerSeedEntityId is not null);
        Assert.Contains(startupSeed.Resources, resource =>
            resource.Resource == IceRemembersResource.Food.ToDomainName() &&
            resource.Value > 0f);
        Assert.Contains(startupSeed.Relationships, relationship =>
            relationship.RelationshipKind == IceRemembersRelationship.ControlledBy.ToDomainName());
    }

    [Fact]
    public void Grid_UsesIceRemembersTerrainCatalogAndAmbientResourceBands()
    {
        var terrainNames = _world.Grid.AllCells
            .Select(cell => cell.Terrain.StableName)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(terrainNames.Count >= 4, $"Only {terrainNames.Count} terrain types found.");
        Assert.Contains(T(IceRemembersTerrain.Tundra), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Crags), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Ocean), terrainNames);

        foreach (var cell in _world.Grid.AllCells)
        {
            foreach (var (resourceName, range) in cell.Terrain.AmbientResources)
            {
                var value = cell.AmbientResource(resourceName);
                Assert.True(
                    range.Contains(value),
                    $"{cell.Coord} {resourceName} {value} outside [{range.Min}, {range.Max}] for {cell.Terrain.StableName}");
            }
        }
    }

    [Fact]
    public void TerrainProfiles_UseExpectedPopulationBands()
    {
        var catalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();

        Assert.Equal(
            new AmbientValueRange(0f, 3f),
            PopulationRange(catalog, IceRemembersTerrain.Ice));
        Assert.Equal(
            new AmbientValueRange(0f, 1f),
            PopulationRange(catalog, IceRemembersTerrain.Ocean));
        Assert.Equal(
            new AmbientValueRange(3f, 5f),
            PopulationRange(catalog, IceRemembersTerrain.Tundra));
    }

    [Fact]
    public void SeedRoster_KeepsFixedAnchorsAndExpectedCounts()
    {
        Assert.Equal(3, _world.Graph.GetEntitiesByKind(K.Region).Count);
        Assert.InRange(_world.Graph.GetEntitiesByKind(K.Settlement).Count, 7, 8);
        Assert.Equal(2, _world.Graph.GetEntitiesByKind(K.Faction).Count);
        Assert.InRange(_world.Graph.GetEntitiesByKind(K.Penguin).Count, 5, 7);
        Assert.InRange(_world.Graph.GetEntitiesByKind(K.Artifact).Count, 2, 3);

        var names = _world.Graph.AllEntities.Values.Select(entity => entity.Name).ToHashSet(StringComparer.Ordinal);
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

    [Fact]
    public void SeedEntities_HaveExpectedRelationshipsAndPlacement()
    {
        foreach (var settlementId in _world.Graph.GetEntitiesByKind(K.Settlement))
        {
            Assert.Single(_world.Graph.OutgoingRelationships(settlementId, "controlled_by"));
        }

        foreach (var penguinId in _world.Graph.GetEntitiesByKind(K.Penguin))
        {
            Assert.Single(_world.Graph.OutgoingRelationships(penguinId, "member_of"));

            var penguin = _world.Graph.GetEntity(penguinId)!;
            var coord = Coord(penguin);
            var cell = _world.Grid.GetCell(coord);
            Assert.NotNull(cell);
            Assert.False(float.IsPositiveInfinity(cell.MovementCost),
                $"Penguin {penguin.Name} spawned on blocked terrain cost at {coord}.");
        }

        foreach (var artifactId in _world.Graph.GetEntitiesByKind(K.Artifact))
        {
            var anchored = _world.Graph.OutgoingRelationships(artifactId, "anchored").ToList();
            Assert.Single(anchored);

            var location = _world.Graph.GetEntity(anchored[0].TargetId);
            Assert.NotNull(location);
            Assert.Equal(K.Settlement, location.Kind);
        }
    }

    [Fact]
    public void SeedSettlements_SpanMeaningfulPhysicalAndTerrainSpace()
    {
        var settlementCoords = _world.Graph.GetEntitiesByKind(K.Settlement)
            .Select(id => _world.Graph.GetEntity(id)!)
            .Select(Coord)
            .ToList();

        var qSpan = settlementCoords.Max(coord => coord.Q) - settlementCoords.Min(coord => coord.Q);
        var rSpan = settlementCoords.Max(coord => coord.R) - settlementCoords.Min(coord => coord.R);

        Assert.True(qSpan >= 8, $"Expected settlement q-span >= 8, got {qSpan}.");
        Assert.True(rSpan >= 8, $"Expected settlement r-span >= 8, got {rSpan}.");
        Assert.NotEmpty(_world.CoverageMap.ComputeAdjacency());

        foreach (var settlementName in new[] { "Driftwatch", "Wavecrest" })
        {
            var settlement = EntityNamed(K.Settlement, settlementName);
            var coord = Coord(settlement);
            var cell = _world.Grid.GetCell(coord)!;
            var adjacentMaritime = coord.Neighbors()
                .Where(_world.Grid.ContainsCoord)
                .Select(_world.Grid.GetCell)
                .Any(neighbor => neighbor is not null &&
                    IsAnyTerrain(neighbor, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf));

            Assert.True(
                IsAnyTerrain(cell, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) || adjacentMaritime,
                $"{settlementName} should be on or next to maritime terrain, got {cell.Terrain.StableName} at {coord}.");
        }

        var frosthaven = EntityNamed(K.Settlement, "Frosthaven");
        var frosthavenCell = _world.Grid.GetCell(Coord(frosthaven))!;
        Assert.True(
            IsAnyTerrain(frosthavenCell, IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf),
            $"Frosthaven should be on cold terrain, got {frosthavenCell.Terrain.StableName}.");
    }

    private Entity EntityNamed(EntityKind kind, string name) =>
        _world.Graph.GetEntitiesByKind(kind)
            .Select(id => _world.Graph.GetEntity(id)!)
            .Single(entity => entity.Name == name);

    private static HexCoord Coord(Entity entity) => new(entity.Coordinates.Q, entity.Coordinates.R);

    private static string T(IceRemembersTerrain terrain) => terrain.ToDomainName();

    private static AmbientValueRange PopulationRange(TerrainCatalog catalog, IceRemembersTerrain terrain) =>
        catalog.Resolve(T(terrain)).AmbientResources[IceRemembersResource.Population.ToDomainName()];

    private static bool IsAnyTerrain(HexCell cell, params IceRemembersTerrain[] terrains) =>
        terrains.Any(terrain => string.Equals(cell.Terrain.StableName, T(terrain), StringComparison.Ordinal));
}
