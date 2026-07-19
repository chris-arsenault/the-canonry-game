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
    public void LandformClassification_PreservesPhysicalHistoryAndRejectsUnknownCode()
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

        var impactRim = Enumerable.Range(0, result.Map.Grid.TriangleCount).First(index =>
            result.Map.WaterKindCodes[index] == PhysicalWaterKindCodes.None &&
            result.Map.LandformCodes[index] is not
                (PhysicalLandformCodes.UValley or
                    PhysicalLandformCodes.Cirque or
                    PhysicalLandformCodes.Moraine));
        result.Map.ImpactExcavation[impactRim] = 0;
        result.Map.ImpactRimUplift[impactRim] =
            PhysicalElevation.FromUnits(result.Context.Profile.ImpactLandformMinimumRimUplift);
        result.Layer.Apply(result.Map, result.Context);
        Assert.Equal(PhysicalLandformCodes.ImpactRim, result.Map.LandformCodes[impactRim]);

        result.Map.LandformCodes[0] = PhysicalLandformCodes.Max + 1;
        Assert.Throws<InvalidOperationException>(() =>
            result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void StructuralValidation_RejectsPrimitiveCorruption()
    {
        var result = PhysicalGeographyLayerTestHarness.GenerateComplete();
        result.Map.WaterKindCodes[0] = PhysicalWaterKindCodes.Max + 1;
        var layer = new PhysicalGeographyValidationLayer();
        var context = new PhysicalGeographyGenerationContext(
            PhysicalGeographyLayerTestHarness.Seed,
            PhysicalGeographyLayerTestHarness.Profile,
            layer.Contract);

        Assert.Throws<InvalidOperationException>(() => layer.Apply(result.Map, context));
    }
}
