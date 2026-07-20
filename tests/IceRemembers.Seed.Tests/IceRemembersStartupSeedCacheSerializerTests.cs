namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Host;

public sealed class IceRemembersStartupSeedCacheSerializerTests
{
    [Fact]
    public void PregeneratedWorldStore_DiscoversAndLoadsOnlyCurrentValidWorldFiles()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "canonry-pregenerated-world-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var expected = CreateStartupSeed();
            var currentPath = Path.Combine(directory, "current.world");
            IceRemembersPregeneratedWorldStore.Write(currentPath, expected);
            File.WriteAllText(Path.Combine(directory, "corrupt.world"), "not a world");
            WriteLegacyWorld(Path.Combine(directory, "legacy.world"), expected);

            var discovered = Assert.Single(IceRemembersPregeneratedWorldStore.Discover(directory));
            Assert.Equal(expected.Identity.BaseSeed, discovered.BaseSeed);
            Assert.Equal(Path.GetFullPath(currentPath), discovered.Path);
            Assert.True(discovered.SizeBytes > 0);

            var actual = IceRemembersPregeneratedWorldStore.Read(discovered);
            Assert.Equal(expected.Identity.BaseSeed, actual.Identity.BaseSeed);
            Assert.Equal(expected.Identity.Attempt, actual.Identity.Attempt);
            Assert.Equal(expected.Seed.PhysicalGeography!.Cells, actual.Seed.PhysicalGeography!.Cells);
            Assert.Equal(expected.Seed.PhysicalGeography.Microtriangles, actual.Seed.PhysicalGeography.Microtriangles);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RoundTrip_PreservesCompleteStartupSeed()
    {
        var expected = CreateStartupSeed();
        using var stream = new MemoryStream();

        IceRemembersStartupSeedCacheSerializer.Write(
            stream,
            worldGenerationVersion: 7,
            baseSeed: 42,
            expected);
        stream.Position = 0;
        var actual = IceRemembersStartupSeedCacheSerializer.Read(
            stream,
            expectedWorldGenerationVersion: 7,
            expectedBaseSeed: 42);

        Assert.Equal(expected.Identity.BaseSeed, actual.Identity.BaseSeed);
        Assert.Equal(expected.Identity.Attempt, actual.Identity.Attempt);
        Assert.Equal(expected.Identity.Components, actual.Identity.Components);
        Assert.Equal(expected.Seed.MapRadius, actual.Seed.MapRadius);
        Assert.Equal(expected.Seed.Entities, actual.Seed.Entities);
        Assert.Equal(expected.Seed.Cells, actual.Seed.Cells);
        Assert.Equal(expected.Seed.Relationships, actual.Seed.Relationships);
        Assert.Equal(expected.Seed.MapSubdivisions, actual.Seed.MapSubdivisions);
        Assert.Equal(expected.Seed.Attributes, actual.Seed.Attributes);
        AssertKnowledgeSitesEqual(expected.Seed.KnowledgeSites, actual.Seed.KnowledgeSites);
        Assert.Equal(expected.Seed.PhysicalGeography!.MaterialPalette, actual.Seed.PhysicalGeography!.MaterialPalette);
        Assert.Equal(
            expected.Seed.PhysicalGeography.GenerationEpochCount,
            actual.Seed.PhysicalGeography.GenerationEpochCount);
        Assert.Equal(expected.Seed.PhysicalGeography.SurfacePalette, actual.Seed.PhysicalGeography.SurfacePalette);
        Assert.Equal(expected.Seed.PhysicalGeography.FeaturePalette, actual.Seed.PhysicalGeography.FeaturePalette);
        AssertScalarFieldsEqual(
            expected.Seed.PhysicalGeography.ScalarFields,
            actual.Seed.PhysicalGeography.ScalarFields);
        Assert.Equal(expected.Seed.PhysicalGeography.Cells, actual.Seed.PhysicalGeography.Cells);
        Assert.Equal(expected.Seed.PhysicalGeography.Microtriangles, actual.Seed.PhysicalGeography.Microtriangles);
        Assert.Equal(expected.Seed.SemanticTerrain!.Cells, actual.Seed.SemanticTerrain!.Cells);
    }

    private static void AssertKnowledgeSitesEqual(
        IReadOnlyList<DomainKnowledgeSite> expected,
        IReadOnlyList<DomainKnowledgeSite> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].TechId, actual[index].TechId);
            Assert.Equal(expected[index].Q, actual[index].Q);
            Assert.Equal(expected[index].R, actual[index].R);
            Assert.Equal(expected[index].Tier, actual[index].Tier);
            Assert.Equal(expected[index].Grants, actual[index].Grants);
        }
    }

    private static void AssertScalarFieldsEqual(
        IReadOnlyList<DomainPhysicalScalarField> expected,
        IReadOnlyList<DomainPhysicalScalarField> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].StableName, actual[index].StableName);
            Assert.Equal(expected[index].Values, actual[index].Values);
        }
    }

    private static DomainStartupSeedResult CreateStartupSeed()
    {
        var entities = new[]
        {
            new DomainSeedEntity(1, "penguin", "elder", "aurora", 0, 0, 0, 0, 0, "Edda", "edda"),
        };
        var cells = new[]
        {
            new DomainSeedCell(0, 0, "ice", 1, 1, 1),
        };
        var relationships = new[]
        {
            new DomainSeedRelationship("remembers", RelationshipPolarity.Positive, 1, 1, 0.75f),
        };
        var subdivisions = new[]
        {
            new DomainMapSubdivision(1, DomainMapSubdivisionLevel.Continent, "first", "The First"),
        };
        var attributes = new[]
        {
            new DomainSeedAttributeValue(1, "resolve", 0.5f),
        };
        var sites = new[]
        {
            new DomainKnowledgeSite(
                "memory",
                0,
                0,
                1,
                [new KnowledgeGrantConfig(KnowledgeGrantDomain.CulturePersistence, 1.2f, "aurora", 3)]),
        };
        var physical = CreatePhysicalGeography();
        var semantic = new DomainSemanticTerrainBlueprint(
            1,
            Enumerable.Range(0, 7)
                .Select(index => new DomainSemanticTerrainCell(index, 0, "silence", 1, 1))
                .ToArray());
        var seed = new DomainStartupSeed(
            1,
            entities,
            cells,
            relationships,
            subdivisions,
            attributes,
            sites,
            physical,
            semantic);
        return new DomainStartupSeedResult(
            seed,
            new StartupSeedIdentity(
                42,
                2,
                new Dictionary<string, int>(StringComparer.Ordinal) { ["terrain"] = 99 }));
    }

    private static void WriteLegacyWorld(string path, DomainStartupSeedResult startupSeed)
    {
        using var stream = File.Create(path);
        IceRemembersStartupSeedCacheSerializer.Write(
            stream,
            IceRemembersWorldGenerationVersion.Current - 1,
            startupSeed.Identity.BaseSeed,
            startupSeed);
    }

    private static DomainPhysicalGeographyBlueprint CreatePhysicalGeography()
    {
        const int triangleCount = 7 * PhysicalGeographyCodes.MicrotrianglesPerHex;
        return new DomainPhysicalGeographyBlueprint(
            1,
            4,
            ["glacial_ice"],
            ["tundra"],
            ["hot_spring"],
            [new DomainPhysicalScalarField(
                "fire_intensity",
                Enumerable.Repeat(7, triangleCount).ToArray())],
            CreatePhysicalCells(),
            CreatePhysicalMicrotriangles());
    }

    private static DomainPhysicalGeographyCell[] CreatePhysicalCells() =>
        Enumerable.Range(0, 7)
            .Select(index => new DomainPhysicalGeographyCell(
                index,
                0,
                "ice",
                1,
                1,
                CrossingWord: 0b1101,
                FieldWord: 0b101,
                InitialCoverTerrain: index == 0 ? "sea_ice" : null,
                index * PhysicalGeographyCodes.MicrotrianglesPerHex,
                0,
                0,
                1,
                10,
                5,
                15,
                10,
                3,
                -20,
                40,
                0,
                100,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0))
            .ToArray();

    private static DomainPhysicalMicrotriangle[] CreatePhysicalMicrotriangles() =>
        Enumerable.Repeat(
                new DomainPhysicalMicrotriangle(
                    MaterialIndex: 0,
                    BaseSurfaceIndex: 0,
                    SurfaceIndex: 0,
                    SignificantFeatureFlags: 1,
                    LandformCode: PhysicalLandformCodes.Plain,
                    Elevation: 10,
                    Slope: 0,
                    Temperature: -20,
                    Moisture: 40,
                    Geothermal: 0,
                    LandmassId: 1,
                    LandmassClassCode: PhysicalLandmassClassCodes.Continent,
                    ContinentalGroupId: 1,
                    BathymetricZoneCode: PhysicalBathymetricZoneCodes.Land,
                    WaterKindCode: PhysicalWaterKindCodes.None,
                    WaterDepth: 0,
                    WaterBodyId: 0,
                    DrainageBasinId: 1,
                    FlowEdge: PhysicalGeographyCodes.NoFlowEdge,
                    FlowAccumulation: 0,
                    RiverOrder: 0,
                    RiverSpineEdgeMask: 0,
                    RiverChannelStrength: 0,
                    RiverChannelOrder: 0,
                    FloodplainIntensity: 0,
                    DeltaIntensity: 0,
                    IceThickness: 0,
                    IceFlux: 0,
                    FractureConductance: 0,
                    HeatSourceDensity: 0,
                    FeatureFlags: 0),
                7 * PhysicalGeographyCodes.MicrotrianglesPerHex)
            .ToArray();
}
