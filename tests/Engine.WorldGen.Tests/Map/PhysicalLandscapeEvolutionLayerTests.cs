namespace Engine.WorldGen.Tests.Map;

public sealed class PhysicalLandscapeEvolutionLayerTests
{
    [Fact]
    public void LandscapeInitialization_CapturesBedrockAndTectonicHistoryAndRejectsSurfaceMismatch()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LandscapeInitializationLayer>();
        Assert.Equal(result.Map.Elevation.ToArray(), result.Map.BedrockElevation.ToArray());
        Assert.Equal(result.Map.TectonicStrain.ToArray(), result.Map.TectonicActivityHistory.ToArray());

        result.Map.SedimentThickness[0] = 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void ImpactCratering_ExcavatesFracturedBasinsRaisesRimsAndDepositsConservedEjecta()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<ImpactCrateringLayer>();
        Assert.Contains(result.Map.ImpactExcavation.ToArray(), value => value > 0);
        Assert.Contains(result.Map.ImpactRimUplift.ToArray(), value => value > 0);
        Assert.Contains(result.Map.ImpactEjectaThickness.ToArray(), value => value > 0);
        Assert.Contains(
            Enumerable.Range(0, result.Map.Grid.TriangleCount),
            index => (result.Map.FeatureFlags[index] &
                Engine.Constants.PhysicalGeographyFeatureCodes.ImpactCenter) != 0 &&
                result.Map.FractureConductance[index] > 0);
        Assert.Equal(
            result.Map.SedimentProducedTotal,
            result.Map.SedimentDepositedTotal + result.Map.SedimentExportedTotal);

        result.Map.ImpactHeat[0] = -1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void FluvialErosion_CutsBedrockAndProducesConservedSedimentSupply()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<FluvialErosionLayer>();
        Assert.Contains(result.Map.CumulativeFluvialErosion.ToArray(), value => value > 0);
        Assert.Contains(result.Map.SedimentSupply.ToArray(), value => value > 0);
        Assert.Equal(
            result.Map.SedimentProducedTotal,
            result.Map.SedimentDepositedTotal + result.Map.SedimentExportedTotal +
                result.Map.SedimentSupply.ToArray().Sum(value => (long)value));

        result.Map.CumulativeFluvialErosion[0] = -1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void SedimentTransport_DepositsAndExportsEverySuppliedUnitAndRejectsNegativeThickness()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<SedimentTransportLayer>();
        Assert.Contains(result.Map.SedimentThickness.ToArray(), value => value > 0);
        Assert.All(result.Map.SedimentSupply.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(
            result.Map.SedimentProducedTotal,
            result.Map.SedimentDepositedTotal + result.Map.SedimentExportedTotal);

        result.Map.SedimentThickness[0] = -1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void CoastalEvolution_UsesWaveHistoryAndRejectsUnsupportedErosion()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<CoastalErosionLayer>();
        var eroded = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .Where(index => result.Map.CumulativeCoastalErosion[index] > 0)
            .ToArray();
        Assert.NotEmpty(eroded);
        Assert.All(eroded, index => Assert.True(result.Map.WaveExposureHistory[index] > 0));

        var unsupported = Enumerable.Range(0, result.Map.Grid.TriangleCount)
            .First(index => result.Map.WaveExposureHistory[index] == 0);
        result.Map.CumulativeCoastalErosion[unsupported] = 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void GlacialEvolution_RecordsMaximumIceErosionAndConservesDepositedSediment()
    {
        var erosion = PhysicalGeographyLayerTestHarness.GenerateThrough<GlacialErosionLayer>();
        var eroded = Enumerable.Range(0, erosion.Map.Grid.TriangleCount)
            .Where(index => erosion.Map.CumulativeGlacialErosion[index] > 0)
            .ToArray();
        Assert.NotEmpty(eroded);
        Assert.All(eroded, index => Assert.True(
            erosion.Map.MaximumIceFlux[index] >=
                PhysicalGeographyLayerTestHarness.Profile.GlacierFluxThreshold));

        var deposition = PhysicalGeographyLayerTestHarness.GenerateThrough<GlacialDepositionLayer>();
        Assert.Contains(deposition.Map.CumulativeGlacialDeposition.ToArray(), value => value > 0);
        Assert.All(deposition.Map.SedimentSupply.ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(
            deposition.Map.SedimentProducedTotal,
            deposition.Map.SedimentDepositedTotal + deposition.Map.SedimentExportedTotal);

        deposition.Map.SedimentSupply[0] = 1;
        Assert.Throws<InvalidOperationException>(() =>
            deposition.Layer.Validate(deposition.Map, deposition.Context));
    }
}
