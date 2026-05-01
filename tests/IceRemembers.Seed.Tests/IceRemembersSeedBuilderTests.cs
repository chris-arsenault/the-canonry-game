namespace IceRemembers.Seed.Tests;

using Engine.Domain.Startup;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;

public sealed class IceRemembersSeedBuilderTests
{
    private readonly IceRemembersStartupSeedResult _result = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42);

    private DomainStartupSeed Seed => _result.StartupSeed;

    [Fact]
    public void BuildAcceptedStartupSeed_IsDeterministicWithSeed()
    {
        var result2 = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42);

        Assert.Equal(Seed.Cells.Count, result2.StartupSeed.Cells.Count);
        Assert.Equal(Seed.Entities.Count, result2.StartupSeed.Entities.Count);
        Assert.Equal(_result.Seed, result2.Seed);
        Assert.Equal(
            IceRemembersSeedQualityValidator.TerrainFingerprint(Seed),
            IceRemembersSeedQualityValidator.TerrainFingerprint(result2.StartupSeed));
        Assert.Equal(
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(Seed),
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(result2.StartupSeed));
    }

    [Fact]
    public void BuildAcceptedStartupSeed_ProducesMeaningfullyDifferentWorldsAcrossSeeds()
    {
        var resultA = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42);
        var resultB = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(43);

        Assert.NotEqual(resultA.Seed, resultB.Seed);
        Assert.NotEqual(
            IceRemembersSeedQualityValidator.TerrainFingerprint(resultA.StartupSeed),
            IceRemembersSeedQualityValidator.TerrainFingerprint(resultB.StartupSeed));
        Assert.NotEqual(
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(resultA.StartupSeed),
            IceRemembersSeedQualityValidator.EntityPlacementFingerprint(resultB.StartupSeed));
    }

    [Fact]
    public void BuildAcceptedStartupSeed_PassesSeedQualityValidationAcrossSeeds()
    {
        var issues = new List<string>();
        for (var seed = 0; seed < 20; seed++)
        {
            var result = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(seed);
            var report = IceRemembersSeedQualityValidator.Validate(result.StartupSeed);
            if (!report.IsValid)
            {
                issues.Add($"seed {seed}, accepted {result.Seed}: {string.Join("; ", report.Issues)}");
            }
        }

        Assert.Empty(issues);
    }

    [Fact]
    public void SelectAcceptedSeed_ReturnsAcceptedSeedUsedForStartupSeed()
    {
        var accepted = IceRemembersSeedBuilder.SelectAcceptedSeed(42);

        Assert.Equal(_result.Seed, accepted);
    }

    [Fact]
    public void BuildStartupSeed_PreservesGeneratedMapEntitiesAndRelationships()
    {
        var startupSeed = IceRemembersSeedBuilder.BuildStartupSeed(seed: 42);

        Assert.Equal(40, startupSeed.MapRadius);
        Assert.Equal(3 * 40 * 40 + 3 * 40 + 1, startupSeed.Cells.Count);
        Assert.Equal(Seed.Entities.Count, startupSeed.Entities.Count);
        Assert.Equal(3, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent));
        Assert.Equal(7, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Superregion));
        Assert.Contains(startupSeed.Entities, entity => entity.DisplayName == "Frosthaven");
        Assert.Contains(startupSeed.Cells, cell => cell.OwnerSeedEntityId is not null);
        Assert.All(startupSeed.Cells, cell =>
        {
            Assert.True(cell.SuperregionId > 0, $"Cell {cell.Q},{cell.R} has no superregion.");
            Assert.True(cell.ContinentId > 0, $"Cell {cell.Q},{cell.R} has no continent.");
        });
        Assert.Contains(startupSeed.Resources, resource =>
            resource.Resource == IceRemembersResource.Food.ToDomainName() &&
            resource.Value > 0f);
        Assert.Contains(startupSeed.Relationships, relationship =>
            relationship.RelationshipKind == IceRemembersRelationship.ControlledBy.ToDomainName());
    }

    [Fact]
    public void Grid_UsesIceRemembersTerrainCatalogAndAmbientResourceBands()
    {
        var terrainNames = Seed.Cells
            .Select(cell => cell.Terrain)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(terrainNames.Count >= 4, $"Only {terrainNames.Count} terrain types found.");
        Assert.Contains(T(IceRemembersTerrain.Tundra), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Crags), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Ocean), terrainNames);

        var catalog = IceRemembersTerrainCatalog.CreateWorldGenCatalog();
        foreach (var resource in Seed.Resources)
        {
            var terrain = TerrainAt(resource.Q, resource.R);
            var range = catalog.Resolve(terrain).AmbientResources[resource.Resource];
            Assert.True(
                range.Contains(resource.Value),
                $"{resource.Q},{resource.R} {resource.Resource} {resource.Value} outside [{range.Min}, {range.Max}] for {terrain}");
        }
    }

    [Fact]
    public void TerrainGeneration_UsesExpandedMapInsteadOfOldTundraBands()
    {
        var terrainCounts = Seed.Cells
            .GroupBy(cell => cell.Terrain, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var total = Seed.Cells.Count;
        var tundraShare = terrainCounts.GetValueOrDefault(T(IceRemembersTerrain.Tundra)) / (float)total;
        var wetShare = Seed.Cells.Count(cell =>
            IsAnyTerrain(cell.Terrain, IceRemembersTerrain.Ocean, IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef)) /
            (float)total;
        var largestShare = terrainCounts.Values.Max() / (float)total;

        Assert.True(tundraShare < 0.55f, $"Tundra dominates expanded map at {tundraShare:P1}.");
        Assert.True(wetShare >= 0.18f, $"Expected meaningful ocean/shelf/reef separation, got {wetShare:P1}.");
        Assert.True(largestShare < 0.65f, $"Single terrain dominates expanded map at {largestShare:P1}.");
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
        Assert.Equal(6, EntitiesByKind(K.Region).Length);
        Assert.InRange(EntitiesByKind(K.Settlement).Length, 10, 12);
        Assert.Equal(3, EntitiesByKind(K.Faction).Length);
        Assert.InRange(EntitiesByKind(K.Penguin).Length, 6, 8);
        Assert.InRange(EntitiesByKind(K.Artifact).Length, 2, 3);

        var names = Seed.Entities.Select(entity => entity.DisplayName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Northlands", names);
        Assert.Contains("Heartlands", names);
        Assert.Contains("Coastlands", names);
        Assert.Contains("Farwake", names);
        Assert.Contains("Blackwater", names);
        Assert.Contains("White Empty", names);
        Assert.Contains("Frosthaven", names);
        Assert.Contains("Ironhold", names);
        Assert.Contains("Driftwatch", names);
        Assert.Contains("Wavecrest", names);
        Assert.Contains("Midpoint", names);
        Assert.Contains("Stillwake", names);
        Assert.Contains("Deepcall", names);
        Assert.Contains("Northern Compact", names);
        Assert.Contains("Southern Order", names);
        Assert.Contains("Farwake Choir", names);
        Assert.Contains("Stormcleaver", names);
    }

    [Fact]
    public void SeedEntities_HaveExpectedRelationshipsAndPlacement()
    {
        foreach (var settlement in EntitiesByKind(K.Settlement))
        {
            Assert.Single(Outgoing(settlement, "controlled_by"));
        }

        foreach (var penguin in EntitiesByKind(K.Penguin))
        {
            Assert.Single(Outgoing(penguin, "member_of"));

            var cell = CellAt(Coord(penguin));
            Assert.NotNull(cell);
            var terrain = IceRemembersTerrainCatalog.CreateWorldGenCatalog().Resolve(cell.Terrain);
            Assert.False(float.IsPositiveInfinity(terrain.MovementCost),
                $"Penguin {penguin.DisplayName} spawned on blocked terrain cost at {Coord(penguin)}.");
        }

        foreach (var artifact in EntitiesByKind(K.Artifact))
        {
            var anchored = Outgoing(artifact, "anchored").ToList();
            Assert.Single(anchored);

            var location = Seed.Entities.Single(entity => entity.SeedEntityId == anchored[0].TargetSeedEntityId);
            Assert.Equal(K.Settlement.Value, location.Kind);
        }
    }

    [Fact]
    public void SeedSettlements_SpanMeaningfulPhysicalAndTerrainSpace()
    {
        var settlementCoords = EntitiesByKind(K.Settlement)
            .Select(Coord)
            .ToList();

        var qSpan = settlementCoords.Max(coord => coord.Q) - settlementCoords.Min(coord => coord.Q);
        var rSpan = settlementCoords.Max(coord => coord.R) - settlementCoords.Min(coord => coord.R);

        Assert.True(qSpan >= 20, $"Expected settlement q-span >= 20, got {qSpan}.");
        Assert.True(rSpan >= 20, $"Expected settlement r-span >= 20, got {rSpan}.");
        Assert.True(NamedCoverageIsSparse());

        foreach (var settlementName in new[] { "Driftwatch", "Wavecrest" })
        {
            var settlement = EntityNamed(K.Settlement.Value, settlementName);
            var coord = Coord(settlement);
            var cell = CellAt(coord);
            var adjacentMaritime = coord.Neighbors()
                .Select(CellAt)
                .Any(neighbor => neighbor is not null &&
                    IsAnyTerrain(neighbor.Terrain, IceRemembersTerrain.Ocean, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf));

            Assert.NotNull(cell);
            Assert.True(
                IsAnyTerrain(cell.Terrain, IceRemembersTerrain.Reef, IceRemembersTerrain.Shelf) || adjacentMaritime,
                $"{settlementName} should be on or next to maritime terrain, got {cell.Terrain} at {coord}.");
        }

        var frosthaven = EntityNamed(K.Settlement.Value, "Frosthaven");
        var frosthavenCell = CellAt(Coord(frosthaven));
        Assert.NotNull(frosthavenCell);
        Assert.True(
            IsAnyTerrain(frosthavenCell.Terrain, IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf),
            $"Frosthaven should be on cold terrain, got {frosthavenCell.Terrain}.");
    }

    [Fact]
    public void SeedSubdivisions_CreateLargeScaleGeographyWithoutGraphEntities()
    {
        var continents = Seed.Cells
            .Select(cell => cell.ContinentId)
            .Distinct()
            .ToArray();
        var superregions = Seed.Cells
            .Select(cell => cell.SuperregionId)
            .Distinct()
            .ToArray();

        Assert.True(continents.Length >= 3, $"Expected at least 3 continents, got {continents.Length}.");
        Assert.True(superregions.Length >= 6, $"Expected at least 6 superregions, got {superregions.Length}.");
        Assert.Equal(3, Seed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent));
        Assert.Equal(7, Seed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Superregion));
        Assert.Contains(Seed.MapSubdivisions, subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent &&
            subdivision.StableName == "near_shelf" &&
            subdivision.DisplayName == "The Near Shelf");
        Assert.Contains(Seed.MapSubdivisions, subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Superregion &&
            subdivision.StableName == "farwake_reach" &&
            subdivision.DisplayName == "Farwake Reach");
        Assert.DoesNotContain(Seed.Entities, entity => entity.Kind == K.Region.Value && entity.DisplayName == "Continent 1");
    }

    [Fact]
    public void SeedSubdivisions_SeparateContinentsWithWaterBands()
    {
        var cells = Seed.Cells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        var boundaryEdges = 0;
        var wetBoundaryEdges = 0;
        foreach (var cell in Seed.Cells)
        {
            var coord = new HexCoord(cell.Q, cell.R);
            foreach (var neighborCoord in coord.Neighbors())
            {
                if (CompareCoords(coord, neighborCoord) >= 0 ||
                    !cells.TryGetValue(neighborCoord, out var neighbor) ||
                    cell.ContinentId == neighbor.ContinentId)
                {
                    continue;
                }

                boundaryEdges++;
                if (IsWaterBoundaryTerrain(cell.Terrain) || IsWaterBoundaryTerrain(neighbor.Terrain))
                {
                    wetBoundaryEdges++;
                }
            }
        }

        Assert.True(boundaryEdges > 0, "Expected continent boundary edges.");
        Assert.True(
            wetBoundaryEdges / (float)boundaryEdges >= 0.75f,
            $"Expected water-separated continents, got {wetBoundaryEdges}/{boundaryEdges} wet boundary edges.");
    }

    [Fact]
    public void SeedSubdivisions_GiveEveryContinentAndSuperregionLandToPlayOn()
    {
        foreach (var continent in Seed.Cells.GroupBy(cell => cell.ContinentId))
        {
            var landCells = continent.Count(cell => !IsWaterBoundaryTerrain(cell.Terrain));
            Assert.True(
                landCells >= 80,
                $"Continent {continent.Key} has only {landCells} non-water cells.");
        }

        foreach (var superregion in Seed.Cells.GroupBy(cell => cell.SuperregionId))
        {
            var landCells = superregion.Count(cell => !IsWaterBoundaryTerrain(cell.Terrain));
            Assert.True(
                landCells >= 20,
                $"Superregion {superregion.Key} has only {landCells} non-water cells.");
        }
    }

    [Fact]
    public void SeedCivilizations_HaveTwoNearbyClustersAndOneRemoteCluster()
    {
        var northern = EntityNamed(K.Faction.Value, "Northern Compact");
        var southern = EntityNamed(K.Faction.Value, "Southern Order");
        var farwake = EntityNamed(K.Faction.Value, "Farwake Choir");

        var nearDistance = HexCoord.Distance(Coord(northern), Coord(southern));
        var farDistance = Math.Min(
            HexCoord.Distance(Coord(farwake), Coord(northern)),
            HexCoord.Distance(Coord(farwake), Coord(southern)));

        Assert.True(nearDistance <= 28, $"Expected close civilization pair, got distance {nearDistance}.");
        Assert.True(farDistance >= 25, $"Expected remote civilization, got distance {farDistance}.");
        Assert.True(
            farDistance > nearDistance,
            $"Expected remote civilization to sit farther out than the close pair; near {nearDistance}, far {farDistance}.");
    }

    private DomainSeedEntity[] EntitiesByKind(Engine.Kinds.KindId kind) =>
        Seed.Entities
            .Where(entity => entity.Kind == kind.Value)
            .ToArray();

    private DomainSeedEntity EntityNamed(string kind, string name) =>
        Seed.Entities.Single(entity => entity.Kind == kind && entity.DisplayName == name);

    private IEnumerable<DomainSeedRelationship> Outgoing(DomainSeedEntity entity, string relationshipKind) =>
        Seed.Relationships.Where(relationship =>
            relationship.SourceSeedEntityId == entity.SeedEntityId &&
            relationship.RelationshipKind == relationshipKind);

    private DomainSeedCell? CellAt(HexCoord coord) =>
        Seed.Cells.FirstOrDefault(cell => cell.Q == coord.Q && cell.R == coord.R);

    private string TerrainAt(int q, int r) =>
        Seed.Cells.Single(cell => cell.Q == q && cell.R == r).Terrain;

    private bool NamedCoverageIsSparse()
    {
        var covered = Seed.Cells.Count(cell => cell.OwnerSeedEntityId is not null);
        return covered / (float)Seed.Cells.Count is >= 0.08f and <= 0.20f;
    }

    private static HexCoord Coord(DomainSeedEntity entity) => new(entity.PhysicalQ, entity.PhysicalR);

    private static string T(IceRemembersTerrain terrain) => terrain.ToDomainName();

    private static AmbientValueRange PopulationRange(TerrainCatalog catalog, IceRemembersTerrain terrain) =>
        catalog.Resolve(T(terrain)).AmbientResources[IceRemembersResource.Population.ToDomainName()];

    private static bool IsAnyTerrain(string terrainName, params IceRemembersTerrain[] terrains) =>
        terrains.Any(terrain => string.Equals(terrainName, T(terrain), StringComparison.Ordinal));

    private static bool IsWaterBoundaryTerrain(string terrainName) =>
        IsAnyTerrain(terrainName, IceRemembersTerrain.Ocean, IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef);

    private static int CompareCoords(HexCoord left, HexCoord right)
    {
        var q = left.Q.CompareTo(right.Q);
        return q != 0 ? q : left.R.CompareTo(right.R);
    }
}
