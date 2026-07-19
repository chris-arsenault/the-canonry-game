namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalHydrologyLayerTests
{
    [Fact]
    public void LakeBasins_FillConnectedDepressionsWithOneSurfaceAndRejectInconsistentDepth()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LakeBasinLayer>();
        var lake = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Lake);
        var bodyId = result.Map.WaterBodyIds[lake];
        var members = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterBodyIds[index] == bodyId)
            .ToArray();
        var surfaces = members
            .Select(index => result.Map.Elevation[index] + result.Map.WaterDepths[index])
            .Distinct()
            .ToArray();
        Assert.Single(surfaces);
        Assert.True(members.Length >= PhysicalGeographyLayerTestHarness.Profile.MinimumLakeTriangles);
        Assert.Contains(members, index => result.Map.LakeOutletTargets[index] >= 0);

        result.Map.WaterDepths[members[0]]++;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void FlowRouting_ResolvesFlatsIntoAcyclicAdjacentPathsAndRejectsNonAdjacentTarget()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<FlowRoutingLayer>();
        AssertEveryDryTriangleReachesWater(result.Map);
        var dry = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None);
        var nonNeighbor = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.Grid.EdgeLeadingTo(dry, index) < 0);
        result.Map.FlowTargets[dry] = nonNeighbor;
        result.Map.FlowEdges[dry] = 0;

        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void FlatFlowTieBreak_RotatesWithGeometryAndIgnoresCandidateStorageIds()
    {
        var original = new[]
        {
            new ConditionedFlowResolver.GeometricFlowCandidate(100, 10, 1, 1d, 0d),
            new ConditionedFlowResolver.GeometricFlowCandidate(2, 10, 1, 0d, 1d),
        };
        var reversed = original.Reverse().ToArray();
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(original, 1d, 0d, 0d, 0d));
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(reversed, 1d, 0d, 0d, 0d));

        var renumbered = new[]
        {
            original[0] with { Value = 7 },
            original[1] with { Value = 99 },
        };
        Assert.Equal(99, ConditionedFlowResolver.SelectByGeometry(renumbered, 1d, 0d, 0d, 0d));

        var rotated = new[]
        {
            original[0] with { DirectionX = 0d, DirectionY = 1d },
            original[1] with { DirectionX = -1d, DirectionY = 0d },
        };
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(rotated, 0d, 1d, 0d, 0d));
    }

    [Fact]
    public void RiverNetwork_UsesAccumulatedTributariesAndRejectsDownstreamLoss()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<RiverNetworkLayer>();
        var rivers = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.RiverOrders[index] > 0)
            .ToArray();
        Assert.NotEmpty(rivers);
        Assert.Contains(rivers, index => result.Map.RiverOrders[index] > 1);
        Assert.True(rivers.Select(index => result.Map.FlowEdges[index]).Distinct().Count() > 1);
        Assert.All(rivers, index =>
        {
            var target = result.Map.FlowTargets[index];
            Assert.True(target >= 0);
            Assert.True(result.Map.FlowAccumulation[target] >= result.Map.FlowAccumulation[index]);
        });

        var river = rivers[0];
        result.Map.FlowAccumulation[result.Map.FlowTargets[river]] =
            result.Map.FlowAccumulation[river] - 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    private static void AssertEveryDryTriangleReachesWater(PhysicalGeographyField map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var visited = new HashSet<int>();
            var current = triangleIndex;
            while (map.WaterKindCodes[current] == PhysicalWaterKindCodes.None)
            {
                Assert.True(visited.Add(current), "Drainage path contains a cycle.");
                current = map.FlowTargets[current];
                Assert.True(current >= 0, "Dry drainage path has no outlet.");
            }
        }
    }
}
