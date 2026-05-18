namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.WorldGen;

using K = IceRemembers.IceRemembersKinds;

public sealed class IceRemembersSeedBuilderTests
{
    private static readonly Lazy<IceRemembersStartupSeedResult> Result42 =
        new(() => IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42));

    private static IceRemembersStartupSeedResult Result => Result42.Value;

    private static DomainStartupSeed Seed => Result.StartupSeed;

    [Fact]
    public void BuildAcceptedStartupSeed_IsDeterministicWithSeed()
    {
        var result2 = IceRemembersSeedBuilder.BuildAcceptedStartupSeed(42);

        Assert.Equal(Seed.Cells.Count, result2.StartupSeed.Cells.Count);
        Assert.Equal(Seed.Entities.Count, result2.StartupSeed.Entities.Count);
        Assert.Equal(Result.Seed, result2.Seed);
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
        foreach (var seed in new[] { 0, 5, 11, 17 })
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

        Assert.Equal(Result.Seed, accepted);
    }

    [Fact]
    public void BuildStartupSeed_PreservesGeneratedMapEntitiesAndRelationships()
    {
        var startupSeed = IceRemembersSeedBuilder.BuildStartupSeed(seed: 42);
        var physicalCells = startupSeed.Cells
            .Where(cell => cell.PlaneCode == PlaneCodes.Physical)
            .ToArray();
        var semanticCells = startupSeed.Cells
            .Where(cell => cell.PlaneCode == PlaneCodes.Semantic)
            .ToArray();

        Assert.Equal(40, startupSeed.MapRadius);
        Assert.Equal(3 * 40 * 40 + 3 * 40 + 1, physicalCells.Length);
        Assert.Equal(3 * 40 * 40 + 3 * 40 + 1, semanticCells.Length);
        Assert.Equal(Seed.Entities.Count, startupSeed.Entities.Count);
        Assert.Equal(7, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent));
        Assert.Equal(17, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Superregion));
        Assert.Contains(startupSeed.Entities, entity => entity.DisplayName == "Aurora Stack");
        Assert.Contains(physicalCells, cell => cell.OwnerSeedEntityId is not null);
        Assert.DoesNotContain(semanticCells, cell => cell.OwnerSeedEntityId is not null);
        Assert.All(startupSeed.Cells, cell =>
        {
            Assert.True(cell.SuperregionId > 0, $"Cell {cell.Q},{cell.R} has no superregion.");
            Assert.True(cell.ContinentId > 0, $"Cell {cell.Q},{cell.R} has no continent.");
        });
        Assert.Contains(startupSeed.Relationships, relationship =>
            relationship.RelationshipKind == IceRemembersRelationship.ControlledBy.ToDomainName());
    }

    [Fact]
    public void Grid_UsesIceRemembersTerrainCatalog()
    {
        var terrainNames = PhysicalCells()
            .Select(cell => cell.Terrain)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(terrainNames.Count >= 4, $"Only {terrainNames.Count} terrain types found.");
        Assert.Contains(T(IceRemembersTerrain.Tundra), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Crags), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Ocean), terrainNames);
    }

    [Fact]
    public void TerrainGeneration_UsesExpandedMapInsteadOfOldTundraBands()
    {
        var cells = PhysicalCells();
        var terrainCounts = cells
            .GroupBy(cell => cell.Terrain, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var total = cells.Length;
        var tundraShare = terrainCounts.GetValueOrDefault(T(IceRemembersTerrain.Tundra)) / (float)total;
        var wetShare = cells.Count(cell =>
            IsAnyTerrain(cell.Terrain, IceRemembersTerrain.Ocean, IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef)) /
            (float)total;
        var largestShare = terrainCounts.Values.Max() / (float)total;

        Assert.True(tundraShare < 0.55f, $"Tundra dominates expanded map at {tundraShare:P1}.");
        Assert.True(wetShare >= 0.18f, $"Expected meaningful ocean/shelf/reef separation, got {wetShare:P1}.");
        Assert.True(largestShare < 0.65f, $"Single terrain dominates expanded map at {largestShare:P1}.");
    }

    [Fact]
    public void SemanticTerrainGeneration_MaterializesConceptualMap()
    {
        var semanticCells = SemanticCells();
        var terrainNames = semanticCells
            .Select(cell => cell.Terrain)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(3 * 40 * 40 + 3 * 40 + 1, semanticCells.Length);
        Assert.DoesNotContain(T(IceRemembersTerrain.Wastes), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.OpenField), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Lineage), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Yoke), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Witness), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Silence), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Severance), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.Breakaway), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.BeaconSpire), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.TabooRidge), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.CavePocket), terrainNames);
        Assert.All(semanticCells, cell =>
        {
            Assert.Equal(PlaneCodes.Semantic, cell.PlaneCode);
            Assert.True(cell.SuperregionId >= 100, $"Semantic cell {cell.Q},{cell.R} has physical superregion {cell.SuperregionId}.");
            Assert.True(cell.ContinentId >= 100, $"Semantic cell {cell.Q},{cell.R} has physical continent {cell.ContinentId}.");
        });
    }

    [Fact]
    public void SeedRoster_KeepsFixedAnchorsAndExpectedCounts()
    {
        Assert.Equal(4, EntitiesByKind(K.Region).Length);
        Assert.Equal(4, EntitiesByKind(K.Settlement).Length);
        Assert.Equal(4, EntitiesByKind(K.Faction).Length);
        Assert.Equal(3, EntitiesByKind(K.Penguin).Length);
        Assert.Equal(4, EntitiesByKind(K.Artifact).Length);
        Assert.Equal(2, EntitiesByKind(K.Transport).Length);
        Assert.Equal(2, EntitiesByKind(K.Occurrence).Length);

        var names = Seed.Entities.Select(entity => entity.DisplayName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Northlands", names);
        Assert.Contains("Heartlands", names);
        Assert.Contains("Coastlands", names);
        Assert.Contains("Farwake", names);
        Assert.Contains("Aurora Stack", names);
        Assert.Contains("Nightfall Shelf", names);
        Assert.Contains("The Drowned Ledge", names);
        Assert.Contains("The Still Pools", names);
        Assert.Contains("Council of Luminaries", names);
        Assert.Contains("The Icebound Exchange", names);
        Assert.Contains("Midnight Claws", names);
        Assert.Contains("The Wake-Singers", names);
        Assert.Contains("High-Beak Auditor Selka", names);
        Assert.Contains("Shadow-Broker Nyla", names);
        Assert.Contains("Keeper Hollows", names);
        Assert.Contains("Grandmother Deep", names);
        Assert.Contains("Tidescream", names);
        Assert.Contains("The Radiant Ring", names);
        Assert.Contains("The Black Harpoon", names);
        Assert.Contains("The Hollows Chart", names);
        Assert.Contains("The Dreaming Tooth", names);
        Assert.Contains("Aurora Charter", names);
        Assert.Contains("Nightfall Cutter", names);
        Assert.Contains("Lantern Vigil", names);
        Assert.Contains("Cinder Muster", names);
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
        }

        foreach (var artifact in EntitiesByKind(K.Artifact))
        {
            var anchored = Outgoing(artifact, "anchored").ToList();
            Assert.Single(anchored);

            var location = Seed.Entities.Single(entity => entity.SeedEntityId == anchored[0].TargetSeedEntityId);
            Assert.Contains(location.Kind, new[] { K.Settlement.Value, K.PointOfInterest.Value });
        }
    }

    [Fact]
    public void SeedEntities_UseExpandedSemanticPlane()
    {
        var semanticCoords = Seed.Entities
            .Select(SemanticCoord)
            .ToArray();
        var maxDistance = semanticCoords.Max(coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord));

        Assert.True(
            maxDistance >= 30,
            $"Expected at least one seed entity outside semantic radius 30 on radius-{Seed.MapRadius} map, got max radius {maxDistance}.");
    }

    [Fact]
    public void DreamingTooth_IsClaimedWithoutBeingPhysicallyHeld()
    {
        var dreamingTooth = EntityNamed(K.Artifact.Value, "The Dreaming Tooth");
        var corpseCurrent = EntityNamed(K.PointOfInterest.Value, "The Corpse Current");
        var wakeSingers = EntityNamed(K.Faction.Value, "The Wake-Singers");

        Assert.Contains(Seed.Relationships, relationship =>
            relationship.RelationshipKind == "anchored" &&
            relationship.SourceSeedEntityId == dreamingTooth.SeedEntityId &&
            relationship.TargetSeedEntityId == corpseCurrent.SeedEntityId);
        Assert.Contains(Seed.Relationships, relationship =>
            relationship.RelationshipKind == "claimed_by" &&
            relationship.SourceSeedEntityId == dreamingTooth.SeedEntityId &&
            relationship.TargetSeedEntityId == wakeSingers.SeedEntityId);
        Assert.DoesNotContain(Seed.Relationships, relationship =>
            relationship.RelationshipKind == "holds" &&
            relationship.TargetSeedEntityId == dreamingTooth.SeedEntityId);
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

        foreach (var settlementName in new[] { "Nightfall Shelf", "The Drowned Ledge" })
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

        var auroraStack = EntityNamed(K.Settlement.Value, "Aurora Stack");
        var auroraStackCell = CellAt(Coord(auroraStack));
        Assert.NotNull(auroraStackCell);
        Assert.True(
            IsAnyTerrain(auroraStackCell.Terrain, IceRemembersTerrain.Ice, IceRemembersTerrain.Tundra, IceRemembersTerrain.Shelf),
            $"Aurora Stack should be on cold terrain, got {auroraStackCell.Terrain}.");
    }

    [Fact]
    public void SeedSubdivisions_CreateLargeScaleGeographyWithoutGraphEntities()
    {
        var cells = PhysicalCells();
        var continents = cells
            .Select(cell => cell.ContinentId)
            .Distinct()
            .ToArray();
        var superregions = cells
            .Select(cell => cell.SuperregionId)
            .Distinct()
            .ToArray();

        Assert.True(continents.Length >= 3, $"Expected at least 3 continents, got {continents.Length}.");
        Assert.True(superregions.Length >= 6, $"Expected at least 6 superregions, got {superregions.Length}.");
        Assert.Equal(7, Seed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent));
        Assert.Equal(17, Seed.MapSubdivisions.Count(subdivision =>
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
        var physicalCells = PhysicalCells();
        var cells = physicalCells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        var boundaryEdges = 0;
        var wetBoundaryEdges = 0;
        foreach (var cell in physicalCells)
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
        var cells = PhysicalCells();
        foreach (var continent in cells.GroupBy(cell => cell.ContinentId))
        {
            var landCells = continent.Count(cell => !IsWaterBoundaryTerrain(cell.Terrain));
            Assert.True(
                landCells >= 80,
                $"Continent {continent.Key} has only {landCells} non-water cells.");
        }

        foreach (var superregion in cells.GroupBy(cell => cell.SuperregionId))
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
        var council = EntityNamed(K.Faction.Value, "Council of Luminaries");
        var claws = EntityNamed(K.Faction.Value, "Midnight Claws");
        var wakeSingers = EntityNamed(K.Faction.Value, "The Wake-Singers");

        var nearDistance = HexCoord.Distance(Coord(council), Coord(claws));
        var farDistance = Math.Min(
            HexCoord.Distance(Coord(wakeSingers), Coord(council)),
            HexCoord.Distance(Coord(wakeSingers), Coord(claws)));

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
        PhysicalCells().FirstOrDefault(cell => cell.Q == coord.Q && cell.R == coord.R);

    private bool NamedCoverageIsSparse()
    {
        var cells = PhysicalCells();
        var covered = cells.Count(cell => cell.OwnerSeedEntityId is not null);
        return covered / (float)cells.Length is >= 0.08f and <= 0.20f;
    }

    private static DomainSeedCell[] PhysicalCells() =>
        Seed.Cells
            .Where(cell => cell.PlaneCode == PlaneCodes.Physical)
            .ToArray();

    private static DomainSeedCell[] SemanticCells() =>
        Seed.Cells
            .Where(cell => cell.PlaneCode == PlaneCodes.Semantic)
            .ToArray();

    private static HexCoord Coord(DomainSeedEntity entity) => new(entity.PhysicalQ, entity.PhysicalR);

    private static SemanticHexCoord SemanticCoord(DomainSeedEntity entity) => new(entity.SemanticQ, entity.SemanticR);

    private static string T(IceRemembersTerrain terrain) => terrain.ToDomainName();

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
