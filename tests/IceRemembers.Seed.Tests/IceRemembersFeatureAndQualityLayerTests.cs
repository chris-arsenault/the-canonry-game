namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersFeatureAndQualityLayerTests
{
    [Fact]
    public void CausalFeatureRegions_GrowOnlyThroughSupportedNeighborsAndClearBetweenUses()
    {
        var grid = PhysicalGeographyGrid.CreateWithPadding(1, 1);
        const int seed = 0;
        var retainedNeighbor = Enumerable.Range(0, 3)
            .Select(edge => grid.NeighborTriangleIndex(seed, edge))
            .First(index => index >= 0);
        var unsupported = Enumerable.Range(0, grid.TriangleCount)
            .First(index => index != seed && index != retainedNeighbor);
        var classifier = new CausalFeatureRegionClassifier(grid);

        classifier.Classify(
            index => index == seed,
            index => index == retainedNeighbor);

        Assert.True(classifier.Includes(seed));
        Assert.True(classifier.Includes(retainedNeighbor));
        Assert.False(classifier.Includes(unsupported));

        classifier.Classify(index => index == unsupported, _ => false);

        Assert.False(classifier.Includes(seed));
        Assert.True(classifier.Includes(unsupported));
    }

    [Fact]
    public void GeneratedFireFeatures_DeriveFromFireAndWetHeatAndRejectUnsupportedSpring()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<FireFeatureLayer>();
        var core = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .First(index => (result.Map.Physical.FeatureFlags[index] &
                PhysicalGeographyFeatureCodes.FireCore) != 0);
        Assert.True(Has(result.Map, core, IceRemembersPhysicalFeature.FirecoreVent));

        var unsupported = core == 0 ? 1 : 0;
        result.Map.FireIntensity[unsupported] = 0;
        result.Map.Physical.Geothermal[unsupported] = 0;
        result.Map.Physical.Moisture[unsupported] = 0;
        result.Map.Physical.RiverOrders[unsupported] = 0;
        result.Map.Physical.WaterKindCodes[unsupported] = PhysicalWaterKindCodes.None;
        result.Map.SignificantFeatureFlags[unsupported] |=
            IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.HotSpring);
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void AuroraFeatures_ClassifyReservoirLandformsAndRejectUnsupportedMeltLine()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateBefore<AuroraFeatureLayer>();
        const int triangleIndex = 0;
        result.Map.AuroraIntensity[triangleIndex] = 720;
        result.Map.Physical.IceThickness[triangleIndex] = 200;
        result.Map.Physical.Temperature[triangleIndex] = 310;
        result.Map.Physical.LandformCodes[triangleIndex] = PhysicalLandformCodes.Basin;
        result.Pipeline.ApplyLayer(result.Layer, result.Map, IceRemembersMicrogeographyLayerTestHarness.Seed);

        Assert.True(Has(result.Map, triangleIndex, IceRemembersPhysicalFeature.LightWell));
        Assert.True(Has(result.Map, triangleIndex, IceRemembersPhysicalFeature.MeltLine));

        result.Map.AuroraIntensity[triangleIndex] = 0;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void PressureFeatures_PreserveLocusWakesAndImpactsAndRejectFieldMismatch()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateThrough<PressureFeatureLayer>();
        Assert.True(Has(
            result.Map,
            result.Map.DeadGodTriangleIndex,
            IceRemembersPhysicalFeature.PressureGodLocus));
        var wake = Enumerable.Range(0, result.Map.PressureWake.Length)
            .First(index => result.Map.PressureWake[index]);
        Assert.True(Has(result.Map, wake, IceRemembersPhysicalFeature.WakeScar));

        result.Map.PressureWake[wake] = false;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void MagicInteractions_RequireOverlappingCausalFieldsAndRejectInvalidBreach()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateBefore<MagicInteractionLayer>();
        var ocean = Enumerable.Range(0, result.Map.Physical.Grid.TriangleCount)
            .First(index => result.Map.Physical.WaterKindCodes[index] == PhysicalWaterKindCodes.Ocean);
        result.Map.PressureIntensity[ocean] = 500;
        result.Map.AuroraIntensity[ocean] = 500;
        result.Map.Physical.IceThickness[ocean] = 250;
        result.Pipeline.ApplyLayer(result.Layer, result.Map, IceRemembersMicrogeographyLayerTestHarness.Seed);

        Assert.True(Has(result.Map, ocean, IceRemembersPhysicalFeature.PressureAuroraBreach));
        result.Map.Physical.WaterKindCodes[ocean] = PhysicalWaterKindCodes.None;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void FinalSurfaces_UseExplicitFeaturePrecedenceAndRejectInvalidSurface()
    {
        var result = IceRemembersMicrogeographyLayerTestHarness.GenerateBefore<FinalSurfaceLayer>();
        const int triangleIndex = 0;
        result.Map.BaseSurfaceIndexes[triangleIndex] = (int)IceRemembersPhysicalSurface.Tundra;
        result.Map.SignificantFeatureFlags[triangleIndex] |=
            IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.SteamFracture) |
            IceRemembersPhysicalGeography.FeatureFlag(IceRemembersPhysicalFeature.PressureAuroraBreach);
        result.Pipeline.ApplyLayer(result.Layer, result.Map, IceRemembersMicrogeographyLayerTestHarness.Seed);

        Assert.Equal(
            (int)IceRemembersPhysicalSurface.PressureAuroraBreach,
            result.Map.SurfaceIndexes[triangleIndex]);
        result.Map.SurfaceIndexes[triangleIndex] = IceRemembersPhysicalGeography.SurfacePalette.Count;
        Assert.Throws<InvalidOperationException>(() => result.Layer.Validate(result.Map, result.Context));
    }

    [Fact]
    public void StructuralValidation_RejectsInvalidCodes()
    {
        var map = IceRemembersMicrogeographyLayerTestHarness.GenerateComplete();
        var layer = new IceRemembersMicrogeographyValidationLayer();
        var context = new IceRemembersMicrogeographyGenerationContext(
            IceRemembersMicrogeographyLayerTestHarness.Seed,
            IceRemembersMicrogeographyLayerTestHarness.Profile,
            layer.Contract);
        layer.Validate(map, context);

        map.SurfaceIndexes[0] = IceRemembersPhysicalGeography.SurfacePalette.Count;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(map, context));
    }

    private static bool Has(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        IceRemembersPhysicalFeature feature) =>
        (map.SignificantFeatureFlags[triangleIndex] &
            IceRemembersPhysicalGeography.FeatureFlag(feature)) != 0;
}
