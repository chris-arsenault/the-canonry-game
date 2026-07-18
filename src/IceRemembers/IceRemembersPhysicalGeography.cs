namespace IceRemembers;

using Engine.Constants;
using Engine.WorldGen;

public enum IceRemembersPhysicalMaterial
{
    ExposedBedrock,
    Scree,
    Permafrost,
    TundraSoil,
    Snowpack,
    GlacialIce,
    ShelfSediment,
    DeepMarineSediment,
    ReefStone,
    GeothermalCrust,
}

public static class IceRemembersPhysicalGeography
{
    private static readonly string[] MaterialNames =
    [
        "exposed_bedrock",
        "scree",
        "permafrost",
        "tundra_soil",
        "snowpack",
        "glacial_ice",
        "shelf_sediment",
        "deep_marine_sediment",
        "reef_stone",
        "geothermal_crust",
    ];

    public static IReadOnlyList<string> MaterialPalette { get; } = MaterialNames;

    public static PhysicalGeographyGenerationProfile GenerationProfile { get; } = new()
    {
        Padding = 24,
        PlateSpacing = 72d,
        PlateBoundaryWidth = 14d,
        SeaLevel = 0d,
        HeartBiasRadius = 36d,
        HeartLandBias = 0.28d,
        PaddingOceanBias = 0.78d,
        TemperatureBaseline = 0.35d,
        RiverAccumulationThreshold = 11500,
    };

    public static IceRemembersPhysicalMaterial ClassifyMaterial(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        ArgumentNullException.ThrowIfNull(field);
        if ((uint)triangleIndex >= (uint)field.Grid.TriangleCount)
        {
            throw new ArgumentOutOfRangeException(nameof(triangleIndex));
        }

        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            return ClassifyOceanFloor(field, triangleIndex);
        }

        return ClassifyLandSurface(field, triangleIndex);
    }

    private static IceRemembersPhysicalMaterial ClassifyLandSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {

        if ((field.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.GeothermalVent) != 0)
        {
            return IceRemembersPhysicalMaterial.GeothermalCrust;
        }

        if (IsPermanentIce(field, triangleIndex))
        {
            return IceRemembersPhysicalMaterial.GlacialIce;
        }

        var roughSurface = ClassifyRoughSurface(field, triangleIndex);
        if (roughSurface.HasValue)
        {
            return roughSurface.Value;
        }

        return ClassifyLowlandSurface(field, triangleIndex);
    }

    private static IceRemembersPhysicalMaterial? ClassifyRoughSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.LandformCodes[triangleIndex] is PhysicalLandformCodes.Peak or PhysicalLandformCodes.Ridge)
        {
            return field.Slope[triangleIndex] >= 90
                ? IceRemembersPhysicalMaterial.ExposedBedrock
                : IceRemembersPhysicalMaterial.Scree;
        }

        return field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Slope &&
            field.Slope[triangleIndex] >= 75
                ? IceRemembersPhysicalMaterial.Scree
                : null;
    }

    private static IceRemembersPhysicalMaterial ClassifyLowlandSurface(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.Temperature[triangleIndex] <= 285)
        {
            return field.Moisture[triangleIndex] >= 590
                ? IceRemembersPhysicalMaterial.Snowpack
                : IceRemembersPhysicalMaterial.Permafrost;
        }

        return field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
            field.Moisture[triangleIndex] >= 360
                ? IceRemembersPhysicalMaterial.TundraSoil
                : IceRemembersPhysicalMaterial.Permafrost;
    }

    private static IceRemembersPhysicalMaterial ClassifyOceanFloor(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.WaterDepths[triangleIndex] >= 360)
        {
            return IceRemembersPhysicalMaterial.DeepMarineSediment;
        }

        if (field.WaterDepths[triangleIndex] <= 125 &&
            field.Temperature[triangleIndex] >= 315 &&
            field.Geothermal[triangleIndex] >= 180)
        {
            return IceRemembersPhysicalMaterial.ReefStone;
        }

        return IceRemembersPhysicalMaterial.ShelfSediment;
    }

    private static bool IsPermanentIce(PhysicalGeographyField field, int triangleIndex) =>
        (field.FeatureFlags[triangleIndex] &
            (PhysicalGeographyFeatureCodes.PermanentSeaIce |
                PhysicalGeographyFeatureCodes.PermanentIceMargin)) != 0 ||
        (field.Temperature[triangleIndex] <= 215 && field.Moisture[triangleIndex] >= 400);
}
