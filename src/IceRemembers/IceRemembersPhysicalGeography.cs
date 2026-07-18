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
    MoraineTill,
    Basalt,
}

public enum IceRemembersPhysicalSurface
{
    Tundra,
    OpenField,
    Ridge,
    Glacier,
    Moraine,
    Lake,
    Fjord,
    ExposedShelf,
    Shelf,
    Ocean,
    Reef,
    FirecoreVent,
    ThermalVein,
    HotSpring,
    BasaltFlat,
    ThawedHalo,
    AuroraField,
    LightWell,
    FrostPrism,
    MeltLine,
    PressureGodLocus,
    CrushZone,
    WakeScar,
    InterferenceLattice,
    RogueImpact,
    ThermalWarRim,
    SteamFracture,
    PressureAuroraBreach,
}

public enum IceRemembersPhysicalFeature
{
    FirecoreVent,
    ThermalVein,
    HotSpring,
    BasaltFlat,
    ThawedHalo,
    AuroraField,
    LightWell,
    FrostPrism,
    MeltLine,
    PressureGodLocus,
    CrushZone,
    WakeScar,
    InterferenceLattice,
    RogueImpact,
    ThermalWarRim,
    SteamFracture,
    PressureAuroraBreach,
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
        "moraine_till",
        "basalt",
    ];

    private static readonly string[] SurfaceNames =
    [
        "tundra",
        "open_field",
        "ridge",
        "glacier",
        "moraine",
        "lake",
        "fjord",
        "exposed_shelf",
        "shelf",
        "ocean",
        "reef",
        "firecore_vent",
        "thermal_vein",
        "hot_spring",
        "basalt_flat",
        "thawed_halo",
        "aurora_field",
        "light_well",
        "frost_prism",
        "melt_line",
        "pressure_god_locus",
        "crush_zone",
        "wake_scar",
        "interference_lattice",
        "rogue_impact",
        "thermal_war_rim",
        "steam_fracture",
        "pressure_aurora_breach",
    ];

    private static readonly string[] FeatureNames =
    [
        "firecore_vent",
        "thermal_vein",
        "hot_spring",
        "basalt_flat",
        "thawed_halo",
        "aurora_field",
        "light_well",
        "frost_prism",
        "melt_line",
        "pressure_god_locus",
        "crush_zone",
        "wake_scar",
        "interference_lattice",
        "rogue_impact",
        "thermal_war_rim",
        "steam_fracture",
        "pressure_aurora_breach",
    ];

    public static IReadOnlyList<string> MaterialPalette { get; } = MaterialNames;

    public static IReadOnlyList<string> SurfacePalette { get; } = SurfaceNames;

    public static IReadOnlyList<string> FeaturePalette { get; } = FeatureNames;

    public static PhysicalGeographyGenerationProfile GenerationProfile { get; } = new()
    {
        Padding = 24,
        ContinentalCenterCount = 5,
        ContinentalRadius = 96d,
        RidgeAxisCount = 8,
        RidgeHalfLength = 76d,
        RidgeWidth = 10d,
        SeaLevel = -0.04d,
        IslandTriangleThreshold = 680,
        RiverAccumulationThreshold = 9200,
        TemperatureBaseline = 125,
        WarmthPocketCount = 16,
        HeartWarmthRadius = 31d,
        HeartWarmthStrength = 350,
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

        return field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean
            ? ClassifyOceanFloor(field, triangleIndex)
            : ClassifyLandMaterial(field, triangleIndex);
    }

    private static IceRemembersPhysicalMaterial ClassifyLandMaterial(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Moraine)
        {
            return IceRemembersPhysicalMaterial.MoraineTill;
        }

        if (field.Geothermal[triangleIndex] >= 760)
        {
            return IceRemembersPhysicalMaterial.GeothermalCrust;
        }

        if (field.IceThickness[triangleIndex] >= 300)
        {
            return IceRemembersPhysicalMaterial.GlacialIce;
        }

        return ClassifyExposedLandMaterial(field, triangleIndex);
    }

    private static IceRemembersPhysicalMaterial ClassifyExposedLandMaterial(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.LandformCodes[triangleIndex] is PhysicalLandformCodes.Peak or PhysicalLandformCodes.Ridge)
        {
            return field.Slope[triangleIndex] >= 85
                ? IceRemembersPhysicalMaterial.ExposedBedrock
                : IceRemembersPhysicalMaterial.Scree;
        }

        if (field.Temperature[triangleIndex] <= 250)
        {
            return field.Moisture[triangleIndex] >= 560
                ? IceRemembersPhysicalMaterial.Snowpack
                : IceRemembersPhysicalMaterial.Permafrost;
        }

        return field.Moisture[triangleIndex] >= 330
            ? IceRemembersPhysicalMaterial.TundraSoil
            : IceRemembersPhysicalMaterial.Permafrost;
    }

    public static ulong FeatureFlag(IceRemembersPhysicalFeature feature) => 1UL << (int)feature;

    private static IceRemembersPhysicalMaterial ClassifyOceanFloor(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.BathymetricZoneCodes[triangleIndex] == PhysicalBathymetricZoneCodes.DeepOcean)
        {
            return IceRemembersPhysicalMaterial.DeepMarineSediment;
        }

        if (field.WaterDepths[triangleIndex] <= 125 &&
            field.Temperature[triangleIndex] >= 300 &&
            field.Geothermal[triangleIndex] >= 150)
        {
            return IceRemembersPhysicalMaterial.ReefStone;
        }

        return IceRemembersPhysicalMaterial.ShelfSediment;
    }
}
