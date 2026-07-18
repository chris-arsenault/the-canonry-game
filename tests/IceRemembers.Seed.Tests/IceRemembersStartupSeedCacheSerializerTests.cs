namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.Domain.Startup;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Host;

public sealed class IceRemembersStartupSeedCacheSerializerTests
{
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

    private static DomainPhysicalGeographyBlueprint CreatePhysicalGeography()
    {
        var cells = Enumerable.Range(0, 7)
            .Select(index => new DomainPhysicalGeographyCell(
                index,
                0,
                "ice",
                1,
                1,
                index * PhysicalGeographyCodes.MicrotrianglesPerHex,
                0,
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
                0))
            .ToArray();
        var microtriangles = Enumerable.Repeat(
                new DomainPhysicalMicrotriangle(
                    0,
                    PhysicalLandformCodes.Plain,
                    10,
                    -20,
                    40,
                    0,
                    PhysicalWaterKindCodes.None,
                    0,
                    0,
                    1,
                    PhysicalGeographyCodes.NoFlowEdge,
                    0,
                    0,
                    0),
                7 * PhysicalGeographyCodes.MicrotrianglesPerHex)
            .ToArray();
        return new DomainPhysicalGeographyBlueprint(
            1,
            ["glacial_ice"],
            cells,
            microtriangles);
    }
}
