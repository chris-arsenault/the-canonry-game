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
        _ = PriorityFlood.FillFromOcean(map);

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
    public void FlatFlowTieBreak_RotatesWithGeometryAndIgnoresCandidateStorageIds()
    {
        var original = new[]
        {
            new ConditionedFlowResolver.GeometricFlowCandidate(100, 10, 1, 1d, 0d),
            new ConditionedFlowResolver.GeometricFlowCandidate(2, 10, 1, 0d, 1d),
        };
        var reversed = original.Reverse().ToArray();
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(original, 1d, 1d, 0d, 0d));
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(reversed, 1d, 1d, 0d, 0d));

        var renumbered = new[]
        {
            original[0] with { Value = 7 },
            original[1] with { Value = 99 },
        };
        Assert.Equal(99, ConditionedFlowResolver.SelectByGeometry(renumbered, 1d, 1d, 0d, 0d));

        var rotated = new[]
        {
            original[0] with { DirectionX = 0d, DirectionY = 1d },
            original[1] with { DirectionX = -1d, DirectionY = 0d },
        };
        Assert.Equal(2, ConditionedFlowResolver.SelectByGeometry(rotated, -1d, 1d, 0d, 0d));
    }

    [Fact]
    public void ConditionedFlow_RotatesWithTheCompleteTriangleGraph()
    {
        var grid = new PhysicalGeographyGrid(2, 1);
        var map = new PhysicalGeographyField(grid);
        for (var triangleIndex = 0; triangleIndex < grid.TriangleCount; triangleIndex++)
        {
            var boundary = grid.HexIsOnWorkBoundary(grid.TriangleHexIndex(triangleIndex));
            map.WaterKindCodes[triangleIndex] = boundary
                ? PhysicalWaterKindCodes.Ocean
                : PhysicalWaterKindCodes.None;
            map.DrainageElevations[triangleIndex] = boundary ? 0 : 100;
        }

        var targets = ConditionedFlowResolver.ResolveLandTargets(map);
        var rotation = BuildSixtyDegreeRotation(grid);
        for (var triangleIndex = 0; triangleIndex < grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            Assert.Equal(rotation[targets[triangleIndex]], targets[rotation[triangleIndex]]);
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

    private static int[] BuildSixtyDegreeRotation(PhysicalGeographyGrid grid)
    {
        const double cosine = 0.5d;
        var sine = Math.Sqrt(3d) / 2d;
        var rotation = new int[grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < grid.TriangleCount; triangleIndex++)
        {
            var x = grid.TriangleX(triangleIndex);
            var y = grid.TriangleY(triangleIndex);
            var rotatedX = (cosine * x) - (sine * y);
            var rotatedY = (sine * x) + (cosine * y);
            rotation[triangleIndex] = FindTriangleAt(grid, rotatedX, rotatedY);
        }

        return rotation;
    }

    private static int FindTriangleAt(PhysicalGeographyGrid grid, double x, double y)
    {
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var candidate = 0; candidate < grid.TriangleCount; candidate++)
        {
            var dx = grid.TriangleX(candidate) - x;
            var dy = grid.TriangleY(candidate) - y;
            var distance = (dx * dx) + (dy * dy);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        Assert.True(bestDistance < 0.000001d, "Rotated triangle did not map onto the canonical graph.");
        return best;
    }
}
