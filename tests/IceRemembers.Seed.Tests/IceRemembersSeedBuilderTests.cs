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

        Assert.Equal(18, startupSeed.MapRadius);
        Assert.Equal((3 * 18 * 18) + (3 * 18) + 1, physicalCells.Length);
        Assert.Equal((3 * 18 * 18) + (3 * 18) + 1, semanticCells.Length);
        Assert.Equal(Seed.Entities.Count, startupSeed.Entities.Count);
        Assert.Equal(7, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Continent));
        Assert.Equal(17, startupSeed.MapSubdivisions.Count(subdivision =>
            subdivision.Level == DomainMapSubdivisionLevel.Superregion));
        Assert.Contains(startupSeed.Entities, entity => entity.DisplayName == "Aurora Stack");
        Assert.Contains(physicalCells, cell => cell.OwnerSeedEntityId is not null);
        // Semantic coverage exists by design now: canon jurisdictions own
        // patches of idea-space; any semantic owner must be a canon entity.
        var canonIds = Seed.Entities
            .Where(entity => entity.Kind == K.Canon.Value)
            .Select(entity => entity.SeedEntityId)
            .ToHashSet();
        Assert.Contains(semanticCells, cell => cell.OwnerSeedEntityId is not null);
        Assert.All(
            semanticCells.Where(cell => cell.OwnerSeedEntityId is not null),
            cell => Assert.Contains(cell.OwnerSeedEntityId!.Value, canonIds));
        Assert.All(startupSeed.Cells, cell =>
        {
            Assert.True(cell.SuperregionId > 0, $"Cell {cell.Q},{cell.R} has no superregion.");
            Assert.True(cell.ContinentId > 0, $"Cell {cell.Q},{cell.R} has no continent.");
        });
        Assert.Contains(startupSeed.Relationships, relationship =>
            relationship.RelationshipKind == IceRemembersRelationship.ControlledBy.ToDomainName());
    }

    [Fact]
    public void LatentGeography_CoversRadius128WithMicrohexAndSemanticVariation()
    {
        var physical = Assert.IsType<DomainPhysicalGeographyBlueprint>(Seed.PhysicalGeography);
        var semantic = Assert.IsType<DomainSemanticTerrainBlueprint>(Seed.SemanticTerrain);
        var expectedCells = (3 * 128 * 128) + (3 * 128) + 1;

        Assert.Equal(128, physical.Radius);
        Assert.Equal(expectedCells, physical.Cells.Count);
        Assert.Equal(expectedCells * PhysicalGeographyCodes.MicrotrianglesPerHex, physical.Microtriangles.Count);
        Assert.Equal(128, semantic.Radius);
        Assert.Equal(expectedCells, semantic.Cells.Count);

        Assert.All(physical.Cells.Select((cell, index) => (cell, index)), entry =>
            Assert.Equal(
                entry.index * PhysicalGeographyCodes.MicrotrianglesPerHex,
                entry.cell.MicrotriangleOffset));
        Assert.True(
            physical.Cells.Select(cell => cell.BaseTerrain).Distinct(StringComparer.Ordinal).Count() >= 6,
            "The latent physical disk must retain a broad terrain palette.");
        Assert.True(
            physical.Cells.Where(cell => HexDistance(cell.Q, cell.R) >= 96)
                .Select(cell => cell.BaseTerrain)
                .Distinct(StringComparer.Ordinal)
                .Count() >= 4,
            "Outer physical geography must not collapse to one frontier terrain.");
        Assert.Contains(
            physical.Microtriangles,
            triangle => triangle.RiverOrder > 0);
        Assert.True(
            semantic.Cells.Select(cell => cell.Terrain).Distinct(StringComparer.Ordinal).Count() >= 8,
            "The latent semantic disk must retain its authored terrain vocabulary.");
        Assert.True(
            semantic.Cells.Where(cell => HexDistance(cell.Q, cell.R) >= 96)
                .Select(cell => cell.Terrain)
                .Distinct(StringComparer.Ordinal)
                .Count() >= 5,
            "Outer semantic geography must not collapse to Open Field.");
    }

    [Fact]
    public void LatentPhysicalGeography_UsesIndependentMaterialsAndConsistentHexSummaries()
    {
        var physical = Assert.IsType<DomainPhysicalGeographyBlueprint>(Seed.PhysicalGeography);
        var gameplayTerrains = IceRemembersTerrainCatalog.CreateWorldGenCatalog()
            .Terrains
            .Select(terrain => terrain.StableName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(physical.MaterialPalette, gameplayTerrains.Contains);
        foreach (var cell in physical.Cells)
        {
            AssertCellSummaryMatchesMicrotriangles(physical, cell);
        }
    }

    [Fact]
    public void LatentPhysicalGeography_HasSeparatedWarmHabitatsWithoutSurfaceCollapse()
    {
        var physical = Assert.IsType<DomainPhysicalGeographyBlueprint>(Seed.PhysicalGeography);
        var largestFinalSurfaceShare = physical.Microtriangles
            .GroupBy(triangle => triangle.SurfaceIndex)
            .Max(group => group.Count()) /
            (double)physical.Microtriangles.Count;
        var meltLineIndex = physical.SurfacePalette
            .Select((surface, index) => (surface, index))
            .Single(entry => entry.surface == "melt_line")
            .index;
        var meltLineShare = physical.Microtriangles.Count(triangle =>
            triangle.SurfaceIndex == meltLineIndex) /
            (double)physical.Microtriangles.Count;

        Assert.True(
            largestFinalSurfaceShare < 0.65,
            $"A final surface collapsed the world at {largestFinalSurfaceShare:P1}.");
        Assert.True(meltLineShare < 0.05, $"Melt lines cover {meltLineShare:P1} of the world.");

        var habitableCells = physical.Cells
            .Where(cell => Microtriangles(physical, cell).Any(IsHabitable))
            .ToArray();
        Assert.True(CountConnectedCellComponents(habitableCells) >= 8);
        var heartTriangles = physical.Cells
            .Where(cell => HexDistance(cell.Q, cell.R) <= Seed.MapRadius)
            .SelectMany(cell => Microtriangles(physical, cell))
            .ToArray();
        Assert.True(
            heartTriangles.Any(IsHabitable),
            $"The starting heart has no habitable microgeography; maximum temperature is {heartTriangles.Max(triangle => triangle.Temperature)} and maximum moisture is {heartTriangles.Max(triangle => triangle.Moisture)}.");
        Assert.Contains(habitableCells, cell => HexDistance(cell.Q, cell.R) >= 64);
        Assert.Contains(habitableCells, cell => cell.OceanCoverage > 0);
        Assert.Contains(habitableCells, cell => cell.CoastEdgeMask != 0);
        Assert.Contains(habitableCells, cell => cell.LandCoverage > 0 && cell.CoastEdgeMask == 0);
        Assert.Contains(habitableCells, cell => cell.PeakCount > 0);

        var hotSpringFlag = IceRemembersPhysicalGeography.FeatureFlag(
            IceRemembersPhysicalFeature.HotSpring);
        Assert.Contains(physical.Cells, cell =>
            cell.PeakCount > 0 &&
            Microtriangles(physical, cell).Any(triangle =>
                IsHabitable(triangle) &&
                (triangle.SignificantFeatureFlags & hotSpringFlag) != 0));

        var pressureGodFlag = IceRemembersPhysicalGeography.FeatureFlag(
            IceRemembersPhysicalFeature.PressureGodLocus);
        Assert.Single(physical.Microtriangles, triangle =>
            (triangle.SignificantFeatureFlags & pressureGodFlag) != 0);
    }

    [Fact]
    public void LatentPhysicalGeography_SharedFeatureEdgesAgreeAcrossHexes()
    {
        var physical = Assert.IsType<DomainPhysicalGeographyBlueprint>(Seed.PhysicalGeography);
        var cells = physical.Cells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        foreach (var cell in physical.Cells)
        {
            var coord = new HexCoord(cell.Q, cell.R);
            for (var direction = 0; direction < 6; direction++)
            {
                if (cells.TryGetValue(coord.Neighbor(direction), out var neighbor))
                {
                    AssertSharedEdgeMasksAgree(cell, neighbor, direction);
                }
            }
        }
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
    public void TerrainGeneration_DoesNotCollapseTheStartupHeartToOneTerrain()
    {
        var cells = PhysicalCells();
        var terrainCounts = cells
            .GroupBy(cell => cell.Terrain, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var total = cells.Length;
        var largestShare = terrainCounts.Values.Max() / (float)total;

        Assert.True(largestShare < 0.65f, $"Single terrain dominates startup heart at {largestShare:P1}.");
    }

    [Fact]
    public void SemanticTerrainGeneration_MaterializesConceptualMap()
    {
        var semanticCells = SemanticCells();
        var terrainNames = semanticCells
            .Select(cell => cell.Terrain)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        // Idea-space is now born as a heart (radius 18) and grows through play.
        Assert.Equal(3 * 18 * 18 + 3 * 18 + 1, semanticCells.Length);
        Assert.DoesNotContain(T(IceRemembersTerrain.Wastes), terrainNames);
        Assert.Contains(T(IceRemembersTerrain.OpenField), terrainNames);
        // The heart carries varied, axis-driven idea-terrain rather than a
        // monoculture. Specialty rim terrains (beacon spires, taboo ridges, cave
        // pockets) and some axis basins are seeded toward the edge of the
        // full-scale field, so they emerge as idea-space grows outward from its
        // heart rather than all existing at birth.
        Assert.True(
            terrainNames.Count >= 5,
            $"Expected varied semantic terrain in the heart, got: {string.Join(", ", terrainNames)}.");
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
        Assert.Equal(5, EntitiesByKind(K.Settlement).Length);
        Assert.Equal(4, EntitiesByKind(K.Faction).Length);
        Assert.Equal(5, EntitiesByKind(K.Penguin).Length);
        Assert.Equal(6, EntitiesByKind(K.Artifact).Length);
        Assert.Equal(3, EntitiesByKind(K.Transport).Length);
        Assert.Equal(3, EntitiesByKind(K.Occurrence).Length);

        Assert.All(Seed.Entities, entity =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entity.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(entity.ReferenceName));
        });

        var names = Seed.Entities.Select(entity => entity.DisplayName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Aurora Stack", names);
        Assert.Contains("Nightshelf", names);
        Assert.Contains("The Icebound Exchange", names);
        Assert.Contains("Midnight Claws", names);
        Assert.Contains("The Wake-Singers", names);
        Assert.Contains("High-Beak Auditor Selka", names);
        Assert.Contains("Shadow-Broker Nyla", names);
        Assert.Contains("Grandmother Deep", names);

        Assert.Equal("Aurora", EntityNamed(K.Settlement.Value, "Aurora Stack").ReferenceName);
        Assert.Equal("Nightshelf", EntityNamed(K.Settlement.Value, "Nightshelf").ReferenceName);
        Assert.Equal("Selka", EntityNamed(K.Penguin.Value, "High-Beak Auditor Selka").ReferenceName);
        Assert.Equal("Nyla", EntityNamed(K.Penguin.Value, "Shadow-Broker Nyla").ReferenceName);
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
    public void SeedEntities_StayWithinSemanticHeart()
    {
        var semanticCoords = Seed.Entities
            .Select(SemanticCoord)
            .ToArray();
        var maxDistance = semanticCoords.Max(coord => SemanticHexCoord.Distance(SemanticHexCoord.Origin, coord));

        // Idea-space is born as a heart (radius 18) and grows outward through
        // play, so every seed entity must start inside known idea-ground.
        Assert.True(
            maxDistance <= 18,
            $"Seed entities must start inside the semantic heart (radius 18) on radius-{Seed.MapRadius} map, got max radius {maxDistance}.");
    }

    [Fact]
    public void Firstkilltooth_IsClaimedWithoutBeingPhysicallyHeld()
    {
        var dreamingTooth = EntityNamed(K.Artifact.Value, "Firstkilltooth Of Grund");
        var corpseCurrent = EntityNamed(K.PointOfInterest.Value, "Gulletbloom");
        var wakeSingers = EntityNamed(K.Faction.Value, "The Wake-Singers");

        Assert.Equal(IceRemembersCulture.WakeSingers.ToDomainName(), corpseCurrent.Culture);
        Assert.Contains(Seed.Relationships, relationship =>
            relationship.RelationshipKind == "claimed_by" &&
            relationship.SourceSeedEntityId == corpseCurrent.SeedEntityId &&
            relationship.TargetSeedEntityId == wakeSingers.SeedEntityId);
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

        Assert.True(qSpan >= 12, $"Expected settlement q-span >= 12, got {qSpan}.");
        Assert.True(rSpan >= 12, $"Expected settlement r-span >= 12, got {rSpan}.");
        Assert.True(NamedCoverageIsSparse());

        foreach (var settlementName in new[] { "Nightshelf", "Codex∴lockhold" })
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
    public void SeedSubdivisions_FormConnectedGeographicPartitions()
    {
        var physical = Assert.IsType<DomainPhysicalGeographyBlueprint>(Seed.PhysicalGeography);
        Assert.Equal(3, physical.Cells.Select(cell => cell.ContinentId).Distinct().Count());
        Assert.Equal(7, physical.Cells.Select(cell => cell.SuperregionId).Distinct().Count());
        AssertRegionsAreConnected(physical.Cells, cell => cell.ContinentId);
        AssertRegionsAreConnected(physical.Cells, cell => cell.SuperregionId);
    }

    [Fact]
    public void SeedSubdivisions_GiveEveryContinentLandAndSeedTheHeartWithPlayableRegions()
    {
        var cells = PhysicalCells();
        foreach (var continent in cells.GroupBy(cell => cell.ContinentId))
        {
            var landCells = continent.Count(cell => !IsWaterBoundaryTerrain(cell.Terrain));
            Assert.True(
                landCells >= 40,
                $"Continent {continent.Key} has only {landCells} non-water cells.");
        }

        // Most superregions carry playable land in the heart; a few outer
        // ones are all-water frontier at seed and develop as the map thaws.
        var populatedSuperregions = cells
            .GroupBy(cell => cell.SuperregionId)
            .Count(superregion => superregion.Count(cell => !IsWaterBoundaryTerrain(cell.Terrain)) >= 8);
        Assert.True(
            populatedSuperregions >= 5,
            $"Only {populatedSuperregions} superregions have playable land in the heart.");
    }

    [Fact]
    public void SeedCivilizations_SpanTheHeartWithNearAndFarNeighbors()
    {
        var council = EntityNamed(K.Faction.Value, "The Meridian (starlight-bound)");
        var claws = EntityNamed(K.Faction.Value, "Midnight Claws");
        var wakeSingers = EntityNamed(K.Faction.Value, "The Wake-Singers");

        var distances = new[]
        {
            HexCoord.Distance(Coord(council), Coord(claws)),
            HexCoord.Distance(Coord(council), Coord(wakeSingers)),
            HexCoord.Distance(Coord(claws), Coord(wakeSingers)),
        };
        var closest = distances.Min();
        var farthest = distances.Max();

        // In the thawed heart the founding civilizations sit at varied
        // distances: at least one pair is close enough for early contact,
        // and the spread reaches across the heart. Which clan is remote is a
        // property of the grown world, not the bootstrap.
        Assert.True(closest <= 26, $"Expected a close civilization pair, got closest {closest}.");
        Assert.True(
            farthest >= 12,
            $"Expected civilizations to span the heart, got farthest {farthest}: council {Coord(council)}, claws {Coord(claws)}, Wake-Singers {Coord(wakeSingers)}.");
        Assert.True(farthest > closest, $"Expected a spread of civilizations, got closest {closest}, farthest {farthest}.");
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
        // The thawed heart is intentionally dense - co-location by design -
        // so named coverage runs higher than on the eventual grown map.
        return covered / (float)cells.Length is >= 0.08f and <= 0.45f;
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

    private static int HexDistance(int q, int r) =>
        (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;

    private static bool IsAnyTerrain(string terrainName, params IceRemembersTerrain[] terrains) =>
        terrains.Any(terrain => string.Equals(terrainName, T(terrain), StringComparison.Ordinal));

    private static bool IsWaterBoundaryTerrain(string terrainName) =>
        IsAnyTerrain(terrainName, IceRemembersTerrain.Ocean, IceRemembersTerrain.Shelf, IceRemembersTerrain.Reef);

    private static IEnumerable<DomainPhysicalMicrotriangle> Microtriangles(
        DomainPhysicalGeographyBlueprint physical,
        DomainPhysicalGeographyCell cell)
    {
        for (var localTriangle = 0;
            localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
            localTriangle++)
        {
            yield return physical.Microtriangles[cell.MicrotriangleOffset + localTriangle];
        }
    }

    private static bool IsHabitable(DomainPhysicalMicrotriangle triangle) =>
        triangle.Temperature >= 335 && triangle.Moisture >= 260;

    private static int CountConnectedCellComponents(
        IReadOnlyCollection<DomainPhysicalGeographyCell> cells)
    {
        var remaining = cells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        var componentCount = 0;
        var queue = new Queue<HexCoord>();
        while (remaining.Count > 0)
        {
            componentCount++;
            queue.Enqueue(remaining.First().Key);
            while (queue.TryDequeue(out var coord))
            {
                if (!remaining.Remove(coord))
                {
                    continue;
                }

                foreach (var neighbor in coord.Neighbors())
                {
                    if (remaining.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return componentCount;
    }

    private static void AssertCellSummaryMatchesMicrotriangles(
        DomainPhysicalGeographyBlueprint physical,
        DomainPhysicalGeographyCell cell)
    {
        var minElevation = int.MaxValue;
        var maxElevation = int.MinValue;
        var waterCoverage = 0;
        var oceanCoverage = 0;
        var lakeCoverage = 0;
        for (var localTriangle = 0;
            localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
            localTriangle++)
        {
            var triangle = physical.Microtriangles[cell.MicrotriangleOffset + localTriangle];
            minElevation = Math.Min(minElevation, triangle.Elevation);
            maxElevation = Math.Max(maxElevation, triangle.Elevation);
            waterCoverage += triangle.WaterKindCode != PhysicalWaterKindCodes.None ? 1 : 0;
            oceanCoverage += triangle.WaterKindCode == PhysicalWaterKindCodes.Ocean ? 1 : 0;
            lakeCoverage += triangle.WaterKindCode == PhysicalWaterKindCodes.Lake ? 1 : 0;
        }

        Assert.Equal(minElevation, cell.MinElevation);
        Assert.Equal(maxElevation, cell.MaxElevation);
        Assert.Equal(cell.MaxElevation - cell.MinElevation, cell.Relief);
        Assert.Equal(waterCoverage, cell.WaterCoverage);
        Assert.Equal(oceanCoverage, cell.OceanCoverage);
        Assert.Equal(lakeCoverage, cell.LakeCoverage);
        Assert.Equal(PhysicalGeographyCodes.MicrotrianglesPerHex, cell.LandCoverage + cell.WaterCoverage);
    }

    private static void AssertSharedEdgeMasksAgree(
        DomainPhysicalGeographyCell cell,
        DomainPhysicalGeographyCell neighbor,
        int direction)
    {
        var opposite = (direction + 3) % 6;
        Assert.Equal(MaskBit(cell.RiverEdgeMask, direction), MaskBit(neighbor.RiverEdgeMask, opposite));
        Assert.Equal(MaskBit(cell.CoastEdgeMask, direction), MaskBit(neighbor.CoastEdgeMask, opposite));
        Assert.Equal(MaskBit(cell.RidgeEdgeMask, direction), MaskBit(neighbor.RidgeEdgeMask, opposite));
        Assert.Equal(MaskBit(cell.CliffEdgeMask, direction), MaskBit(neighbor.CliffEdgeMask, opposite));
        Assert.Equal(MaskBit(cell.PassEdgeMask, direction), MaskBit(neighbor.PassEdgeMask, opposite));
    }

    private static int MaskBit(int mask, int direction) => (mask >> direction) & 1;

    private static void AssertRegionsAreConnected(
        IReadOnlyList<DomainPhysicalGeographyCell> cells,
        Func<DomainPhysicalGeographyCell, int> regionSelector)
    {
        var cellsByCoord = cells.ToDictionary(cell => new HexCoord(cell.Q, cell.R));
        foreach (var region in cells.GroupBy(regionSelector))
        {
            var expected = region.Select(cell => new HexCoord(cell.Q, cell.R)).ToHashSet();
            var visited = new HashSet<HexCoord>();
            var queue = new Queue<HexCoord>();
            queue.Enqueue(expected.First());
            while (queue.TryDequeue(out var coord))
            {
                if (!visited.Add(coord))
                {
                    continue;
                }

                foreach (var neighbor in coord.Neighbors())
                {
                    if (expected.Contains(neighbor) && cellsByCoord.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Assert.True(
                expected.SetEquals(visited),
                $"Region {region.Key} is split into disconnected components.");
        }
    }
}
