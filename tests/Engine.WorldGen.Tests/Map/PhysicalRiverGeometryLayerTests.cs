namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalRiverGeometryLayerTests
{
    [Fact]
    public void RiverChannels_CoverEverySpineWidenMajorFlowsAndRejectMissingSpineCoverage()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<RiverChannelGeometryLayer>();
        var spines = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.RiverOrders[index] > 0)
            .ToArray();
        Assert.NotEmpty(spines);
        Assert.All(spines, index =>
        {
            Assert.True(result.Map.RiverChannelStrength[index] > 0);
            Assert.True(result.Map.RiverChannelOrders[index] >= result.Map.RiverOrders[index]);
        });
        Assert.Contains(
            Enumerable.Range(0, result.Map.Grid.TriangleCount),
            index => result.Map.RiverChannelStrength[index] > 0 && result.Map.RiverOrders[index] == 0);
        var dominantUpstream = RiverChannelGeometryLayer.BuildDominantUpstream(result.Map);
        Assert.All(
            Enumerable.Range(0, result.Map.Grid.TriangleCount)
                .Where(index => result.Map.RiverChannelStrength[index] > 0 &&
                    result.Map.RiverOrders[index] == 0),
            index => Assert.True(HasCrossValleySpineProvenance(
                result.Map,
                result.Context.Profile,
                dominantUpstream,
                index)));

        result.Map.RiverChannelStrength[spines[0]] = 0;
        result.Map.RiverChannelOrders[spines[0]] = 0;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Floodplains_OccupyLowDryRiverMarginsAndRejectWaterCoverage()
    {
        var grid = new PhysicalGeographyGrid(7, 7);
        var map = new PhysicalGeographyField(grid);
        var source = Enumerable.Range(0, grid.TriangleCount).First(index =>
            Enumerable.Range(0, 3).Count(edge => grid.NeighborTriangleIndex(index, edge) >= 0) >= 2);
        var neighbors = Enumerable.Range(0, 3)
            .Select(edge => grid.NeighborTriangleIndex(source, edge))
            .Where(index => index >= 0)
            .Take(2)
            .ToArray();
        var target = neighbors[0];
        var downstream = neighbors[1];
        var profile = PhysicalGeographyLayerTestHarness.Profile;
        map.RiverChannelOrders[source] = profile.FloodplainMinimumRiverOrder;
        map.RiverChannelStrength[source] = 1000;
        map.FlowTargets[source] = downstream;
        map.DrainageBasinIds[source] = 1;
        map.DrainageBasinIds[target] = 1;

        var layer = new FloodplainLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            profile,
            layer.Contract);
        layer.Apply(map, context);
        layer.Validate(map, context);
        Assert.True(map.FloodplainIntensity[target] > 0);

        map.WaterKindCodes[target] = PhysicalWaterKindCodes.Lake;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(map, context));
    }

    [Fact]
    public void Deltas_FanIntoShelteredShelfAndRejectCoverageWithoutChannelProvenance()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<FloodplainLayer>();
        var mouth = FindRiverMouth(result.Map);
        var ocean = result.Map.FlowTargets[mouth];
        PrepareDeltaMouth(result.Map, result.Context.Profile, mouth, ocean);
        var layer = new DeltaFormationLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            result.Context.Profile,
            layer.Contract);

        layer.Apply(result.Map, context);
        layer.Validate(result.Map, context);
        Assert.Equal(1000, result.Map.DeltaIntensity[mouth]);
        Assert.True(result.Map.DeltaIntensity[ocean] > 0);
        Assert.True(result.Map.RiverChannelStrength[ocean] > 0);

        result.Map.RiverChannelStrength[ocean] = 0;
        result.Map.RiverChannelOrders[ocean] = 0;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(result.Map, context));
    }

    [Fact]
    public void DeltaFormation_RejectsHighEnergyMouthWithoutSuppressingTheRiverSpine()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<FloodplainLayer>();
        var mouth = FindRiverMouth(result.Map);
        var ocean = result.Map.FlowTargets[mouth];
        PrepareDeltaMouth(result.Map, result.Context.Profile, mouth, ocean);
        result.Map.WaveExposure[ocean] = result.Context.Profile.DeltaMaximumWaveEnergy + 1;
        var layer = new DeltaFormationLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            result.Context.Profile,
            layer.Contract);

        layer.Apply(result.Map, context);
        Assert.Equal(0, result.Map.DeltaIntensity[mouth]);
        Assert.True(result.Map.RiverChannelStrength[mouth] > 0);
    }

    [Fact]
    public void Profile_RejectsInvalidRiverGeometryControls()
    {
        var profile = PhysicalGeographyLayerTestHarness.Profile with
        {
            RiverChannelWidthDecay = 0,
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    private static int FindRiverMouth(PhysicalGeographyField map) =>
        Enumerable.Range(0, map.Grid.TriangleCount).First(index =>
            map.RiverOrders[index] > 0 && map.FlowTargets[index] >= 0 &&
            map.WaterKindCodes[map.FlowTargets[index]] == PhysicalWaterKindCodes.Ocean);

    private static void PrepareDeltaMouth(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int mouth,
        int ocean)
    {
        map.RiverOrders[mouth] = Math.Max(map.RiverOrders[mouth], profile.DeltaMinimumRiverOrder);
        map.FlowAccumulation[mouth] = checked(
            profile.RiverAccumulationThreshold * profile.DeltaDischargeMultiplier);
        map.CumulativeFluvialErosion[mouth] =
            PhysicalElevation.FromUnits(profile.DeltaMinimumSedimentDelivery);
        map.WaveExposure[ocean] = 0;
        map.OceanCurrentStrength[ocean] = 0;
        map.BathymetricZoneCodes[ocean] = PhysicalBathymetricZoneCodes.Shelf;
    }

    private static bool HasCrossValleySpineProvenance(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] dominantUpstream,
        int channel)
    {
        var visited = new HashSet<int> { channel };
        var frontier = new Queue<(int TriangleIndex, int Depth)>();
        frontier.Enqueue((channel, 0));
        while (frontier.TryDequeue(out var current))
        {
            if (current.Depth > 0 && map.RiverOrders[current.TriangleIndex] > 0 &&
                map.DrainageBasinIds[current.TriangleIndex] == map.DrainageBasinIds[channel] &&
                Math.Abs(map.Elevation[current.TriangleIndex] - map.Elevation[channel]) <=
                    PhysicalElevation.FromUnits(profile.RiverChannelMaximumBankRelief) &&
                RiverChannelGeometryLayer.LongitudinalAlignment(
                    map,
                    dominantUpstream,
                    current.TriangleIndex,
                    channel) <= profile.RiverChannelMaximumLongitudinalAlignment / 1000d)
            {
                return true;
            }

            if (current.Depth >= profile.RiverChannelMaximumExpansionSteps)
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(current.TriangleIndex, edge);
                if (neighbor >= 0 && visited.Add(neighbor))
                {
                    frontier.Enqueue((neighbor, current.Depth + 1));
                }
            }
        }

        return false;
    }
}
