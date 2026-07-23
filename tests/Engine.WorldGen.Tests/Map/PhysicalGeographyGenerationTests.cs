namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalGeographyGenerationTests
{
    private const int ContentTriangleCount = 11_256;

    private static readonly PhysicalGeographyGenerationProfile TestProfile = new()
    {
        BoundaryPaddingSteps = 48,
        TectonicProvinceCount = 6,
        ContinentalAssemblyCount = 2,
        ContinentalTransitionWidth = 8d,
        ContinentalWarpScale = 12d,
        ContinentalWarpStrength = 4d,
        RidgeWidth = 4d,
        MantlePlumeCount = 2,
        MantlePlumeRadius = 4d,
        VolcanicConeRadius = 2d,
        MantlePlumeMinimumSpacing = 8d,
        ElevationVariationScale = 7d,
        WarmthPocketCount = 5,
        HeartWarmthRadius = 10d,
        OceanMoistureDistance = 30,
        RiverAccumulationThreshold = 2200,
        RiverMinimumAccumulationThreshold = 500,
        RiverBasinHeadwaterFraction = 90,
    };

    [Fact]
    public void Generate_IsDeterministicForSeedAndProfile()
    {
        var first = PhysicalGeographyGenerator.Generate(ContentTriangleCount, 7319, TestProfile);
        var second = PhysicalGeographyGenerator.Generate(ContentTriangleCount, 7319, TestProfile);

        Assert.True(first.Elevation.SequenceEqual(second.Elevation));
        Assert.True(first.WaterKindCodes.SequenceEqual(second.WaterKindCodes));
        Assert.True(first.FlowEdges.SequenceEqual(second.FlowEdges));
        Assert.True(first.FlowAccumulation.SequenceEqual(second.FlowAccumulation));
        Assert.True(first.LandformCodes.SequenceEqual(second.LandformCodes));
    }

    [Fact]
    public void Generate_BuildsReciprocalTopologyAndAcyclicDrainageToOcean()
    {
        var field = PhysicalGeographyGenerator.Generate(ContentTriangleCount, 9173, TestProfile);
        var oceanCount = 0;
        var riverCount = 0;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            ValidateTriangleNeighbors(field.Grid, triangleIndex);
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                oceanCount++;
            }

            if (field.RiverOrders[triangleIndex] > 0)
            {
                riverCount++;
            }
        }

        Assert.True(oceanCount > 0);
        Assert.True(riverCount > 0);
    }

    private static void ValidateTriangleNeighbors(PhysicalGeographyGrid grid, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0)
            {
                continue;
            }

            Assert.Contains(
                triangleIndex,
                Enumerable.Range(0, 3).Select(neighborEdge =>
                    grid.NeighborTriangleIndex(neighbor, neighborEdge)));
        }
    }
}
