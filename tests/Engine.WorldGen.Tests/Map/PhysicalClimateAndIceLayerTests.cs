namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalClimateAndIceLayerTests
{
    [Fact]
    public void OceanDistance_IsSharedDerivedGeographyAndRejectsContradictoryValues()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanDistanceLayer>();
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean &&
            result.Map.OceanDistance[index] == 0);
        var land = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None);
        Assert.True(result.Map.OceanDistance[land] > 0);

        result.Map.OceanDistance[land] = 0;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void AtmosphericFlow_FormsOneAdjacentUpwindGraphAndRejectsInvalidTargets()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<AtmosphericFlowLayer>();
        Assert.Contains(result.Map.UpwindTargets.ToArray(), target => target >= 0);
        Assert.True(result.Map.WindX.ToArray().Distinct().Take(3).Count() >= 3);

        result.Map.UpwindTargets[0] = 0;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void OceanCurrents_FormVariedOceanOnlyCirculationAndRejectLandFlux()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<OceanCurrentLayer>();
        var ocean = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean)
            .ToArray();
        Assert.NotEmpty(ocean);
        Assert.Contains(ocean, index => result.Map.OceanCurrentStrength[index] > 0);
        Assert.Contains(ocean, index => result.Map.OceanCurrentHeat[index] > 0);
        Assert.Contains(ocean, index => result.Map.WaveExposure[index] > 0);
        Assert.True(ocean.Select(index => (
            result.Map.OceanCurrentX[index] / 100,
            result.Map.OceanCurrentY[index] / 100)).Distinct().Take(3).Count() >= 3);

        var land = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None);
        result.Map.OceanCurrentStrength[land] = 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void WarmthSources_IncludeHeartAndSeparatedHabitatsAndRejectEmptyField()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<WarmthSourceLayer>();
        Assert.True(result.Map.Grid.TryGetHexIndex(new HexCoord(0, 0), out var heartHex));
        Assert.Contains(Enumerable.Range(0, PhysicalGeographyCodes.MicrotrianglesPerHex), local =>
            result.Map.Warmth[result.Map.Grid.TriangleIndex(heartHex, local)] > 0);
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.Grid.HexDistanceAt(result.Map.Grid.TriangleHexIndex(index)) >=
                result.Map.Grid.Radius / 2 &&
            result.Map.Warmth[index] > 0);

        result.Map.Warmth.Clear();
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Temperature_CombinesLapseOceanGeothermalAndWarmthAndRejectsOutOfRangeValue()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<TemperatureLayer>();
        Assert.True(result.Map.Temperature.ToArray().Max() > result.Map.Temperature.ToArray().Min());
        var hotSource = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .OrderByDescending(index => result.Map.Geothermal[index] + result.Map.Warmth[index])
            .First();
        Assert.True(result.Map.Temperature[hotSource] > 0);

        result.Map.Temperature[hotSource] = 1001;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Moisture_UsesOpenWaterAndVariedWeatherAndRejectsOutOfRangeValue()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<MoistureLayer>();
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.None &&
            result.Map.Moisture[index] == 1000);
        var landMoisture = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None)
            .Select(index => result.Map.Moisture[index])
            .Distinct()
            .Take(2)
            .ToArray();
        Assert.Equal(2, landMoisture.Length);

        result.Map.Moisture[0] = -1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Precipitation_SeparatesRainAndSnowAndRejectsInvalidTotals()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<PrecipitationLayer>();
        Assert.Contains(result.Map.Rainfall.ToArray(), value => value > 0);
        Assert.Contains(result.Map.Snowfall.ToArray(), value => value > 0);
        var landTotals = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None)
            .Select(index => result.Map.Rainfall[index] + result.Map.Snowfall[index])
            .Distinct()
            .Take(2)
            .ToArray();
        Assert.Equal(2, landTotals.Length);

        result.Map.Rainfall[0] = 1001;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void Runoff_UsesRainAndSnowmeltWhileStandingWaterContributesNoLocalFlux()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<RunoffLayer>();
        Assert.All(result.Map.Runoff.ToArray(), value => Assert.InRange(value, 0, 1000));
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.None &&
            result.Map.Runoff[index] == 0);
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None &&
            result.Map.Runoff[index] > 0);

        var water = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.None);
        result.Map.Runoff[water] = 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void GlacialMeltwater_AddsRetreatAndBasalDischargeWithoutTurningWaterIntoRunoff()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<GlacialRunoffLayer>();
        var meltwaterLand = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None &&
                result.Map.GlacialMeltwater[index] > 0)
            .ToArray();

        Assert.NotEmpty(meltwaterLand);
        Assert.Contains(meltwaterLand, index => result.Map.Runoff[index] > 0);
        Assert.DoesNotContain(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.None &&
            result.Map.GlacialMeltwater[index] != 0);

        var water = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] != PhysicalWaterKindCodes.None);
        result.Map.GlacialMeltwater[water] = 1;
        var layer = new GlacialMeltwaterLayer();
        Assert.Throws<InvalidOperationException>(() => layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void LandIce_UsesConfiguredCoverageTargetOnLandAndRejectsIceOnWater()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LandIceLayer>();
        var land = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.Grid.TriangleIsInsideConfiguredDisk(index) &&
                result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None)
            .ToArray();
        var frozen = land.Count(index =>
            result.Map.IceThickness[index] >=
                PhysicalGeographyLayerTestHarness.Profile.GlacierThicknessThreshold);
        var coverage = frozen / (double)land.Length;
        Assert.True(PhysicalGeographyLayerTestHarness.Profile.QualityTargets.LandIceCoverage.Contains(coverage));

        var ocean = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean);
        result.Map.IceThickness[ocean] = 250;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void IceFlow_AccumulatesDownhillWithoutLosingLocalMassAndRejectsFluxLoss()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<IceFlowLayer>();
        Assert.Contains(Enumerable.Range(0, result.Map.Grid.TriangleCount), index =>
            result.Map.IceFlux[index] > result.Map.IceThickness[index]);
        var frozen = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.IceThickness[index] > 0);
        result.Map.IceFlux[frozen] = result.Map.IceThickness[frozen] - 1;

        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void GlacialLandforms_RequireQualifyingIceFluxAndRejectUnsupportedOverprint()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<GlacialLandformLayer>();
        var glacial = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.LandformCodes[index] is
                PhysicalLandformCodes.UValley or
                PhysicalLandformCodes.Cirque or
                PhysicalLandformCodes.Moraine)
            .ToArray();
        Assert.NotEmpty(glacial);
        Assert.All(glacial, index => Assert.True(
            result.Map.IceFlux[index] >= PhysicalGeographyLayerTestHarness.Profile.GlacierFluxThreshold));

        var unsupported = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.IceFlux[index] <
                PhysicalGeographyLayerTestHarness.Profile.GlacierFluxThreshold);
        result.Map.LandformCodes[unsupported] = PhysicalLandformCodes.UValley;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void SeaIce_UsesItsOwnOceanCoverageTargetAndRejectsPermanentIceOnLand()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<SeaIceLayer>();
        var ocean = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.Grid.TriangleIsInsideConfiguredDisk(index) &&
                result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean)
            .ToArray();
        var frozen = ocean.Count(index => result.Map.IceThickness[index] > 0);
        var coverage = frozen / (double)ocean.Length;
        Assert.True(PhysicalGeographyLayerTestHarness.Profile.QualityTargets.SeaIceCoverage.Contains(coverage));
        Assert.Contains(ocean, index => result.Map.SeaIceSuitability[index] > 0);

        var land = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None);
        result.Map.FeatureFlags[land] |= PhysicalGeographyFeatureCodes.PermanentSeaIce;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }
}
