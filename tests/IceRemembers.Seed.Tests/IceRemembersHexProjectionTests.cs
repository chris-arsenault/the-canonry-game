namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;
using IceRemembers.Seed;

/// <summary>
/// Ladder and word contracts for the map-to-hex projection, asserted over a
/// complete generated small world so every rule is exercised against real
/// worldgen output rather than hand-picked fixtures.
/// </summary>
public sealed class IceRemembersHexProjectionTests
{
    private static readonly IceRemembersTerrain[] WaterTerrains =
    [
        IceRemembersTerrain.Ocean,
        IceRemembersTerrain.Shelf,
        IceRemembersTerrain.Reef,
        IceRemembersTerrain.Fjord,
        IceRemembersTerrain.Lake,
    ];

    private static readonly IceRemembersTerrain[] LandTerrains =
    [
        IceRemembersTerrain.Tundra,
        IceRemembersTerrain.Verdant,
        IceRemembersTerrain.Floodplain,
        IceRemembersTerrain.Wastes,
        IceRemembersTerrain.Crags,
        IceRemembersTerrain.Ice,
        IceRemembersTerrain.HotSprings,
        IceRemembersTerrain.AuroraField,
        IceRemembersTerrain.Basalt,
    ];

    [Fact]
    public void Projection_SatisfiesLadderAndWordContractsAcrossACompleteWorld()
    {
        var magic = IceRemembersMicrogeographyLayerTestHarness.GenerateComplete();
        var field = magic.Physical;
        var terrains = new HashSet<IceRemembersTerrain>();
        for (var configuredIndex = 0; configuredIndex < field.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = field.Grid.ConfiguredHexIndexAt(configuredIndex);
            var aggregate = Summarize(field, magic, hexIndex);
            var result = IceRemembersHexProjection.Project(field, magic, hexIndex, aggregate);
            terrains.Add(result.Terrain);
            AssertWaterPartition(aggregate, result);
            AssertCrossingWord(aggregate, result.CrossingWord);
            AssertFieldWord(aggregate, result.FieldWord);
            AssertInitialCover(result);
            Assert.Equal(result, IceRemembersHexProjection.Project(field, magic, hexIndex, aggregate));
        }

        // A complete world must not collapse into one or two identities.
        Assert.True(terrains.Count >= 3, $"projection produced only {terrains.Count} terrains");
    }

    private static void AssertWaterPartition(
        in PhysicalHexAggregate aggregate,
        in IceRemembersHexProjection.Result result)
    {
        if (aggregate.WaterCoverage >= 12)
        {
            Assert.Contains(result.Terrain, WaterTerrains);
            if (aggregate.LakeCoverage >= aggregate.OceanCoverage)
            {
                Assert.Equal(IceRemembersTerrain.Lake, result.Terrain);
            }
        }
        else
        {
            Assert.Contains(result.Terrain, LandTerrains);
        }
    }

    private static void AssertCrossingWord(in PhysicalHexAggregate aggregate, int crossingWord)
    {
        Assert.Equal(0, crossingWord & ~PhysicalCrossingCodes.WordMask);
        for (var direction = 0; direction < PhysicalCrossingCodes.DirectionCount; direction++)
        {
            var bit = 1 << direction;
            var crossingClass = PhysicalCrossingCodes.ClassAt(crossingWord, direction);
            switch (crossingClass)
            {
                case PhysicalCrossingCodes.Cliff:
                    Assert.NotEqual(0, aggregate.CliffEdgeMask & bit);
                    Assert.Equal(0, aggregate.PassEdgeMask & bit);
                    break;
                case PhysicalCrossingCodes.Ridge:
                    Assert.NotEqual(0, aggregate.RidgeEdgeMask & bit);
                    Assert.Equal(0, aggregate.PassEdgeMask & bit);
                    break;
                case PhysicalCrossingCodes.River:
                    Assert.NotEqual(0, aggregate.TrunkRiverEdgeMask & bit);
                    break;
                default:
                    Assert.Equal(PhysicalCrossingCodes.Open, crossingClass);
                    break;
            }
        }
    }

    private static void AssertFieldWord(in PhysicalHexAggregate aggregate, int fieldWord)
    {
        Assert.Equal(0, fieldWord & ~PhysicalHexFieldCodes.WordMask);
        var summit = (fieldWord & PhysicalHexFieldCodes.SummitFlag) != 0;
        Assert.Equal(aggregate.PeakCount > 0, summit);
    }

    private static void AssertInitialCover(in IceRemembersHexProjection.Result result)
    {
        if (result.InitialCover is null)
        {
            return;
        }

        Assert.Equal(IceRemembersTerrain.SeaIce, result.InitialCover);
        Assert.Contains(result.Terrain, WaterTerrains);
    }

    private static PhysicalHexAggregate Summarize(
        PhysicalGeographyField field,
        IceRemembersMicrogeographyMap magic,
        int hexIndex)
    {
        // Materials do not participate in the projection; a zeroed material
        // field keeps the aggregate cheap to build here.
        var materials = new int[field.Grid.TriangleCount];
        return PhysicalHexAggregator.Summarize(
            field,
            hexIndex,
            materials,
            IceRemembersPhysicalGeography.MaterialPalette.Count,
            magic.SurfaceIndexes,
            IceRemembersPhysicalGeography.SurfacePalette.Count,
            magic.SignificantFeatureFlags,
            magic.FireIntensity,
            magic.AuroraIntensity,
            magic.PressureIntensity,
            IceRemembersPhysicalGeography.GenerationProfile.CliffSlopeThreshold,
            IceRemembersPhysicalGeography.GenerationProfile.TrunkRiverOrderThreshold);
    }
}
