namespace Engine.WorldGen.Tests.Map;

public sealed class PhysicalLandscapeEvolutionLayerTests
{
    [Fact]
    public void LandscapeErosion_RecordsBedrockUnloadingWithoutMutatingCrustalSupport()
    {
        var map = new PhysicalGeographyField(new PhysicalGeographyGrid(1, 1));
        const int triangle = 0;
        map.BedrockElevation[triangle] = PhysicalElevation.FromUnits(100);
        LandscapeSurface.Rebuild(map, triangle);
        var crustThickness = map.CrustThickness[triangle];

        var removed = LandscapeSurface.Erode(
            map,
            triangle,
            PhysicalElevation.FromUnits(10));

        Assert.Equal(PhysicalElevation.FromUnits(10), removed);
        Assert.Equal(PhysicalElevation.FromUnits(90), map.Elevation[triangle]);
        Assert.Equal(crustThickness, map.CrustThickness[triangle]);
        Assert.Equal(PhysicalElevation.FromUnits(-10), map.SurfaceRockLoad[triangle]);
    }

    [Fact]
    public void MassWasting_ReducesBlockFacesWhileConservingLocalMaterial()
    {
        var map = new PhysicalGeographyField(new PhysicalGeographyGrid(7, 7));
        const int high = 0;
        map.BedrockElevation[high] = PhysicalElevation.FromUnits(1000);
        LandscapeSurface.Rebuild(map, high);
        var beforeSum = map.Elevation.ToArray().Sum();
        var beforeMaximumStep = MaximumNeighborStep(map);
        var layer = new MassWastingLayer();
        var profile = PhysicalGeographyLayerTestHarness.Profile with
        {
            MassWastingIterations = 1,
            MassWastingStableStep = 100,
            MassWastingTransportFraction = 500,
            MaximumMassWastingTransportPerPass = 80,
        };
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            profile,
            layer.Contract);

        layer.Apply(map, context);
        layer.Validate(map, context);

        Assert.True(MaximumNeighborStep(map) < beforeMaximumStep);
        Assert.InRange(
            PhysicalElevation.ToUnits(
                PhysicalElevation.FromUnits(1000) - map.Elevation[high]),
            0d,
            profile.MaximumMassWastingTransportPerPass);
        Assert.Equal(beforeSum, map.Elevation.ToArray().Sum());
        Assert.Equal(map.SedimentProducedTotal, map.SedimentDepositedTotal);
    }

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
    public void ImpactCratering_ExcavatesFracturedBasinsRaisesRimsDepositsEjectaAndHardensRock()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<ImpactCrateringLayer>();
        Assert.Contains(result.Map.ImpactExcavation.ToArray(), value => value > 0);
        Assert.Contains(result.Map.ImpactRimUplift.ToArray(), value => value > 0);
        Assert.Contains(result.Map.ImpactEjectaThickness.ToArray(), value => value > 0);

        // Shocked rock resists erosion long enough for the ring to outlast the
        // country around it.
        Assert.Contains(
            result.Map.RockResistance.ToArray(),
            value => value > Engine.Constants.PhysicalRock.BaselineResistance);
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
                result.Map.SedimentSupply.ToArray().Sum());

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
            erosion.Map.MaximumGroundedIceFlux[index] >=
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

    private static long MaximumNeighborStep(PhysicalGeographyField map)
    {
        long maximum = 0;
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor >= 0)
                {
                    maximum = Math.Max(
                        maximum,
                        Math.Abs(map.Elevation[triangleIndex] - map.Elevation[neighbor]));
                }
            }
        }

        return maximum;
    }
}
