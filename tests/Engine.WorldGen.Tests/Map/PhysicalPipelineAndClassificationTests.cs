namespace Engine.WorldGen.Tests.Map;

using Engine.Constants;

public sealed class PhysicalPipelineAndClassificationTests
{
    [Fact]
    public void Pipeline_RejectsMissingFactsAndDefaultGraphRebuildsEveryInvalidatedFact()
    {
        var profile = PhysicalGeographyLayerTestHarness.Profile;
        var missingMap = new PhysicalGeographyField(
            new PhysicalGeographyGrid(PhysicalGeographyLayerTestHarness.Radius, profile.Padding));
        var bathymetry = new BathymetryLayer();
        var isolated = new PhysicalGeographyPipeline(profile, [bathymetry]);
        Assert.Throws<InvalidOperationException>(() => isolated.ApplyLayer(
            bathymetry,
            missingMap,
            PhysicalGeographyLayerTestHarness.Seed));

        var available = PhysicalGeographyFacts.None;
        var graph = PhysicalGeographyPipeline.CreateDefault(profile);
        foreach (var layer in graph.Layers)
        {
            Assert.Equal(PhysicalGeographyFacts.None, layer.Contract.Requires & ~available);
            available &= ~layer.Contract.Invalidates;
            available |= layer.Contract.Produces;
        }

        Assert.True(available.HasFlag(PhysicalGeographyFacts.Validated));
    }

    [Fact]
    public void LandformClassification_PreservesGlacialFormsAndRejectsUnknownCode()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateThrough<LandformClassificationLayer>();
        Assert.Contains(result.Map.LandformCodes.ToArray(), code => code == PhysicalLandformCodes.Plain);
        Assert.Contains(result.Map.LandformCodes.ToArray(), code =>
            code is PhysicalLandformCodes.Ridge or PhysicalLandformCodes.Peak or PhysicalLandformCodes.Slope);
        Assert.Contains(result.Map.LandformCodes.ToArray(), code =>
            code is PhysicalLandformCodes.UValley or
                PhysicalLandformCodes.Cirque or
                PhysicalLandformCodes.Moraine or
                PhysicalLandformCodes.Fjord);

        result.Map.LandformCodes[0] = PhysicalLandformCodes.Max + 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void QualityValidation_UsesProfileCoverageTargetsRatherThanHardcodedDistributions()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateComplete();
        var permissive = WithTargets(
            ocean: new CoverageTarget(0.5d, 0.5d),
            landIce: new CoverageTarget(0.5d, 0.5d));
        ApplyValidation(result.Map, permissive);

        var impossibleOcean = WithTargets(
            ocean: new CoverageTarget(0.01d, 0.01d),
            landIce: new CoverageTarget(0.5d, 0.5d));
        Assert.Throws<InvalidOperationException>(() =>
            ApplyValidation(result.Map, impossibleOcean));

        var impossibleLandIce = WithTargets(
            ocean: new CoverageTarget(0.5d, 0.5d),
            landIce: new CoverageTarget(0.99d, 0.01d));
        Assert.Throws<InvalidOperationException>(() =>
            ApplyValidation(result.Map, impossibleLandIce));

        var impossibleLandmassShare = permissive with
        {
            QualityTargets = permissive.QualityTargets with
            {
                LargestLandmassLandCoverage = new CoverageTarget(0.99d, 0.01d),
            },
        };
        Assert.Throws<InvalidOperationException>(() =>
            ApplyValidation(result.Map, impossibleLandmassShare));
    }

    [Fact]
    public void QualityValidation_RejectsPrimitiveCorruptionIndependentlyOfTunableTargets()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateComplete();
        result.Map.WaterKindCodes[0] = PhysicalWaterKindCodes.Max + 1;
        var profile = WithTargets(
            ocean: new CoverageTarget(0.5d, 0.5d),
            landIce: new CoverageTarget(0.5d, 0.5d));

        Assert.Throws<InvalidOperationException>(() => ApplyValidation(result.Map, profile));
    }

    private static PhysicalGeographyGenerationProfile WithTargets(
        CoverageTarget ocean,
        CoverageTarget landIce) =>
        PhysicalGeographyLayerTestHarness.Profile with
        {
            QualityTargets = new PhysicalGeographyQualityTargets
            {
                MinimumEvaluationRadius = 0,
                OceanCoverage = ocean,
                LandIceCoverage = landIce,
                SeaIceCoverage = new CoverageTarget(0.5d, 0.5d),
                MajorLandmassCount = new CountTarget(0, int.MaxValue),
                InlandLakeBodyCount = new CountTarget(0, int.MaxValue),
                LargestLandmassLandCoverage = new CoverageTarget(0.5d, 0.5d),
                IslandLandCoverage = new CoverageTarget(0.5d, 0.5d),
                CoastEdgeDensity = new CoverageTarget(0.5d, 0.5d),
                MajorLandmassSpacing = new CoverageTarget(0.5d, 0.5d),
                HotspotLandmassCount = new CountTarget(0, int.MaxValue),
            },
        };

    private static void ApplyValidation(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile)
    {
        var layer = new PhysicalGeographyValidationLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            profile,
            layer.Contract);
        layer.Apply(map, context);
    }
}
