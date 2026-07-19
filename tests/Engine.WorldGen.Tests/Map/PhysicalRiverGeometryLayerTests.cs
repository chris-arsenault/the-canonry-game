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

        result.Map.RiverChannelStrength[spines[0]] = 0;
        result.Map.RiverChannelOrders[spines[0]] = 0;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Floodplains_OccupyLowDryRiverMarginsAndRejectWaterCoverage()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<RiverChannelGeometryLayer>();
        var (source, target) = FindDryChannelMargin(result.Map);
        result.Map.RiverChannelOrders[source] = Math.Max(
            result.Map.RiverChannelOrders[source],
            result.Context.Profile.FloodplainMinimumRiverOrder);
        result.Map.RiverChannelStrength[source] = 1000;
        result.Map.Slope[target] = 0;
        result.Map.Elevation[target] = result.Map.Elevation[source];
        result.Map.DrainageBasinIds[target] = result.Map.DrainageBasinIds[source];

        var layer = new FloodplainLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            result.Context.Profile,
            layer.Contract);
        layer.Apply(result.Map, context);
        layer.Validate(result.Map, context);
        Assert.True(result.Map.FloodplainIntensity[target] > 0);

        result.Map.WaterKindCodes[target] = PhysicalWaterKindCodes.Lake;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(result.Map, context));
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

    private static (int Source, int Target) FindDryChannelMargin(PhysicalGeographyField map)
    {
        for (var source = 0; source < map.Grid.TriangleCount; source++)
        {
            if (map.RiverChannelStrength[source] <= 0)
            {
                continue;
            }

            for (var edge = 0; edge < 3; edge++)
            {
                var target = map.Grid.NeighborTriangleIndex(source, edge);
                if (target >= 0 && map.WaterKindCodes[target] == PhysicalWaterKindCodes.None &&
                    map.RiverChannelStrength[target] == 0)
                {
                    return (source, target);
                }
            }
        }

        throw new InvalidOperationException("Test map has no dry river margin.");
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
        map.CumulativeFluvialErosion[mouth] = profile.DeltaMinimumSedimentDelivery;
        map.WaveExposure[ocean] = 0;
        map.OceanCurrentStrength[ocean] = 0;
        map.BathymetricZoneCodes[ocean] = PhysicalBathymetricZoneCodes.Shelf;
    }
}
