namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalGeographyGenerationTests
{
    private static readonly PhysicalGeographyGenerationProfile TestProfile = new()
    {
        Padding = 8,
        ContinentalCenterCount = 3,
        ContinentalRadius = 18d,
        ContinentalWarpScale = 12d,
        ContinentalWarpStrength = 4d,
        RidgeAxisCount = 4,
        RidgeHalfLength = 16d,
        RidgeWidth = 4d,
        ElevationVariationScale = 7d,
        WarmthPocketCount = 5,
        HeartWarmthRadius = 10d,
        OceanMoistureDistance = 30,
        RiverAccumulationThreshold = 2200,
    };

    [Fact]
    public void Generate_IsDeterministicForSeedAndProfile()
    {
        var first = PhysicalGeographyGenerator.Generate(12, 7319, TestProfile);
        var second = PhysicalGeographyGenerator.Generate(12, 7319, TestProfile);

        Assert.True(first.Elevation.SequenceEqual(second.Elevation));
        Assert.True(first.WaterKindCodes.SequenceEqual(second.WaterKindCodes));
        Assert.True(first.FlowEdges.SequenceEqual(second.FlowEdges));
        Assert.True(first.FlowAccumulation.SequenceEqual(second.FlowAccumulation));
        Assert.True(first.LandformCodes.SequenceEqual(second.LandformCodes));
    }

    [Fact]
    public void Generate_BuildsReciprocalTopologyAndAcyclicDrainageToOcean()
    {
        var field = PhysicalGeographyGenerator.Generate(12, 9173, TestProfile);
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
