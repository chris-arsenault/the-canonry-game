namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalHydrologyLayerTests
{
    [Fact]
    public void LakeBasins_FillConnectedDepressionsWithOneSurfaceAndRejectInconsistentDepth()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LakeBasinLayer>();
        var members = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Lake)
            .GroupBy(index => result.Map.WaterBodyIds[index])
            .Select(static body => body.ToArray())
            .First(body => body.Select(index => result.Map.Elevation[index]).Distinct().Count() > 1);
        var surfaces = members
            .Select(index => result.Map.WaterSurfaceElevations[index])
            .Distinct()
            .ToArray();
        Assert.Single(surfaces);
        Assert.True(members.Length >= PhysicalGeographyLayerTestHarness.Profile.MinimumLakeTriangles);
        Assert.Contains(members, index => result.Map.LakeOutletTargets[index] >= 0);
        Assert.All(members, index => Assert.Equal(
            surfaces[0] - result.Map.Elevation[index],
            result.Map.WaterDepths[index]));

        result.Map.MutableWaterDepths[members[0]]++;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void DrainageConditioning_RetainsSubunitReliefWithoutChangingThePhysicalFloor()
    {
        Assert.Equal(128, PhysicalElevation.FromUnits(0.125d));
        Assert.Equal(0, PhysicalElevation.ToUnits(PhysicalElevation.FromUnits(0.25d)));

        var grid = new PhysicalGeographyGrid(2, 1);
        var map = new PhysicalGeographyField(grid);
        var boundaryFloor = PhysicalElevation.FromUnits(5.125d);
        var basinFloor = PhysicalElevation.FromUnits(0.125d);
        for (var triangleIndex = 0; triangleIndex < grid.TriangleCount; triangleIndex++)
        {
            var ocean = grid.HexIsOnWorkBoundary(grid.TriangleHexIndex(triangleIndex));
            map.WaterKindCodes[triangleIndex] = ocean
                ? PhysicalWaterKindCodes.Ocean
                : PhysicalWaterKindCodes.None;
            map.Elevation[triangleIndex] = ocean ? boundaryFloor : basinFloor;
        }

        var physicalFloor = map.Elevation.ToArray();
        PriorityFlood.FillFromOcean(map);

        Assert.Equal(physicalFloor, map.Elevation.ToArray());
        Assert.Contains(
            Enumerable.Range(0, grid.TriangleCount),
            index => map.DrainageElevations[index] > map.Elevation[index]);
        Assert.All(
            map.DrainageElevations.ToArray(),
            conditioned => Assert.NotEqual(0, conditioned % PhysicalElevation.SubunitsPerUnit));
        Assert.Contains(
            map.Elevation.ToArray(),
            floor => floor % PhysicalElevation.SubunitsPerUnit != 0);
    }

    [Fact]
    public void LakeFloorEvolution_PreservesTheSurfaceAndRecomputesDepth()
    {
        var map = new PhysicalGeographyField(new PhysicalGeographyGrid(1, 1));
        const int eroded = 0;
        const int deposited = 1;
        map.BedrockElevation[deposited] = PhysicalElevation.FromUnits(-2);
        LandscapeSurface.Rebuild(map, deposited);
        map.WaterKindCodes[eroded] = PhysicalWaterKindCodes.Lake;
        map.WaterKindCodes[deposited] = PhysicalWaterKindCodes.Lake;
        var lakeSurface = PhysicalElevation.FromUnits(10);
        map.SetWaterSurface(eroded, lakeSurface);
        map.SetWaterSurface(deposited, lakeSurface);

        _ = LandscapeSurface.Erode(map, eroded, PhysicalElevation.FromUnits(1.25d));
        _ = LandscapeSurface.Deposit(map, deposited, PhysicalElevation.FromUnits(3));

        Assert.Equal(lakeSurface, map.WaterSurfaceElevations[eroded]);
        Assert.Equal(lakeSurface, map.WaterSurfaceElevations[deposited]);
        Assert.NotEqual(map.Elevation[eroded], map.Elevation[deposited]);
        Assert.Equal(
            lakeSurface - map.Elevation[eroded],
            map.WaterDepths[eroded]);
        Assert.Equal(
            lakeSurface - map.Elevation[deposited],
            map.WaterDepths[deposited]);
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
    public void DrainageGuidance_UsesOnlyLegalOutletsAndIgnoresCrustMotion()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<DrainageGuidanceLayer>();
        var original = result.Map.DrainageGuidanceTargets.ToArray();
        for (var triangleIndex = 0; triangleIndex < result.Map.Grid.TriangleCount; triangleIndex++)
        {
            result.Map.CrustMotionX[triangleIndex] = (triangleIndex % 3) - 1;
            result.Map.CrustMotionY[triangleIndex] = 1 - (triangleIndex % 3);
        }

        result.Layer.Apply(result.Map, result.Context);

        Assert.Equal(original, result.Map.DrainageGuidanceTargets.ToArray());
        for (var triangleIndex = 0; triangleIndex < result.Map.Grid.TriangleCount; triangleIndex++)
        {
            if (result.Map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            Assert.True(ConditionedFlowResolver.IsLegalTarget(
                result.Map,
                triangleIndex,
                original[triangleIndex]));
        }
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
        Assert.Contains(rivers, index =>
            System.Numerics.BitOperations.PopCount((uint)result.Map.RiverSpineEdgeMasks[index]) >= 3);
        Assert.All(rivers, index =>
        {
            var target = result.Map.FlowTargets[index];
            Assert.True(target >= 0);
            Assert.True(result.Map.FlowAccumulation[target] >= result.Map.FlowAccumulation[index]);
            Assert.NotEqual(0, result.Map.RiverSpineEdgeMasks[index] &
                (1 << result.Map.FlowEdges[index]));
            var incomingEdge = result.Map.Grid.EdgeLeadingTo(target, index);
            Assert.NotEqual(0, result.Map.RiverSpineEdgeMasks[target] & (1 << incomingEdge));
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
