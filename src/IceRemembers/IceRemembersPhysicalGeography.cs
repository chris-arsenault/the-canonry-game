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
    PackIce,
    SeaIce,
    IceShelf,
    Polynya,
    Snowfield,
    PolarBarrens,
    WetTundra,
    ColdSteppe,
    ScreeField,
    RiverValley,
    RiverChannel,
    Floodplain,
    Delta,
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
        "pack_ice",
        "sea_ice",
        "ice_shelf",
        "polynya",
        "snowfield",
        "polar_barrens",
        "wet_tundra",
        "cold_steppe",
        "scree_field",
        "river_valley",
        "river_channel",
        "floodplain",
        "delta",
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
        TectonicProvinceCount = 30,
        ContinentalAssemblyCount = 5,
        ContinentalCrustFraction = 0.53d,
        ContinentalTransitionWidth = 14d,
        RidgeWidth = 10d,
        MantlePlumeCount = 4,
        MantlePlumeRadius = 12d,
        MantlePlumeMinimumSpacing = 42d,
        SeaLevel = -0.04d,
        IslandTriangleThreshold = 680,
        RiverAccumulationThreshold = 9200,
        TemperatureBaseline = 125,
        OceanCurrentTemperatureStrength = 80,
        OceanCurrentWarmthStrength = 55,
        OceanCurrentSmoothingIterations = 3,
        OceanCurrentHeatIterations = 6,
        WarmthPocketCount = 16,
        HeartWarmthRadius = 31d,
        HeartWarmthStrength = 430,
    };

    public static IceRemembersMicrogeographyGenerationProfile MicrogeographyProfile { get; } = new()
    {
        ColdSteppeMinimumTemperature = 210,
    };

    public static IReadOnlyList<IceRemembersWorldGenerationEpoch> WorldGenerationEpochs { get; } =
    [
        new()
        {
            Physical = new PhysicalGeographyEpochDefinition
            {
                StableName = "crust-assembly",
                TectonicStrength = 1000,
                HydrologyStrength = 180,
                GlaciationStrength = 100,
                CoastalStrength = 120,
                TemperatureOffset = 145,
                SeaLevelOffset = 35,
            },
            FireStrength = 650,
            AuroraStrength = 160,
            PressureStrength = 320,
        },
        new()
        {
            Physical = new PhysicalGeographyEpochDefinition
            {
                StableName = "orogeny-and-wet-erosion",
                TectonicStrength = 850,
                HydrologyStrength = 1000,
                GlaciationStrength = 90,
                CoastalStrength = 580,
                TemperatureOffset = 230,
                SeaLevelOffset = 20,
            },
            FireStrength = 900,
            AuroraStrength = 380,
            PressureStrength = 560,
        },
        new()
        {
            Physical = new PhysicalGeographyEpochDefinition
            {
                StableName = "glacial-maximum",
                TectonicStrength = 360,
                HydrologyStrength = 620,
                GlaciationStrength = 1000,
                CoastalStrength = 760,
                TemperatureOffset = -140,
                SeaLevelOffset = -45,
            },
            FireStrength = 720,
            AuroraStrength = 1000,
            PressureStrength = 760,
        },
        new()
        {
            Physical = new PhysicalGeographyEpochDefinition
            {
                StableName = "retreat-and-present",
                TectonicStrength = 220,
                HydrologyStrength = 860,
                GlaciationStrength = 460,
                CoastalStrength = 1000,
                TemperatureOffset = 65,
            },
            FireStrength = 1000,
            AuroraStrength = 760,
            PressureStrength = 1000,
        },
    ];

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
        if (field.DeltaIntensity[triangleIndex] > 0 || field.FloodplainIntensity[triangleIndex] > 0)
        {
            return IceRemembersPhysicalMaterial.TundraSoil;
        }

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
