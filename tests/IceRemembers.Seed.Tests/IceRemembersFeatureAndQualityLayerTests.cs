namespace IceRemembers.Seed.Tests;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

public sealed class IceRemembersFeatureAndQualityLayerTests
{
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
    public void QualityValidation_UsesConfigurableTargetsAndStillRejectsInvalidCodes()
    {
        var map = IceRemembersMicrogeographyLayerTestHarness.GenerateComplete();
        var layer = new IceRemembersMicrogeographyValidationLayer();
        var permissive = WithLargestNaturalTarget(new ProportionRangeTarget(0d, 1d));
        layer.Validate(map, Context(layer, permissive));

        var impossible = WithLargestNaturalTarget(new ProportionRangeTarget(0d, 0d));
        Assert.Throws<InvalidOperationException>(() => layer.Validate(map, Context(layer, impossible)));

        map.SurfaceIndexes[0] = IceRemembersPhysicalGeography.SurfacePalette.Count;
        Assert.Throws<InvalidOperationException>(() => layer.Validate(map, Context(layer, permissive)));
    }

    private static IceRemembersMicrogeographyGenerationProfile WithLargestNaturalTarget(
        ProportionRangeTarget target) =>
        IceRemembersMicrogeographyLayerTestHarness.Profile with
        {
            QualityTargets = new IceRemembersMicrogeographyQualityTargets
            {
                MinimumEvaluationRadius = 0,
                LargestNaturalSurfaceCoverage = target,
                LargestFinalSurfaceCoverage = new ProportionRangeTarget(0d, 1d),
                MagicOverrideCoverage = new ProportionRangeTarget(0d, 1d),
                MeltLineCoverage = new ProportionRangeTarget(0d, 1d),
                FireCoreCount = new CountTarget(0, int.MaxValue),
                WarmHabitatComponentCount = new CountTarget(0, int.MaxValue),
            },
        };

    private static IceRemembersMicrogeographyGenerationContext Context(
        IceRemembersMicrogeographyValidationLayer layer,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        new(IceRemembersMicrogeographyLayerTestHarness.Seed, profile, layer.Contract);

    private static bool Has(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        IceRemembersPhysicalFeature feature) =>
        (map.SignificantFeatureFlags[triangleIndex] &
            IceRemembersPhysicalGeography.FeatureFlag(feature)) != 0;
}
