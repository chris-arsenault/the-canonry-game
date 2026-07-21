namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal sealed class FireFeatureLayer : IIceRemembersMicrogeographyLayer
{
    private static readonly ulong OwnedFeatures = IceRemembersFeatureMasks.Of(
        IceRemembersPhysicalFeature.FirecoreVent,
        IceRemembersPhysicalFeature.ThermalVein,
        IceRemembersPhysicalFeature.HotSpring,
        IceRemembersPhysicalFeature.HydrothermalVent,
        IceRemembersPhysicalFeature.BasaltFlat,
        IceRemembersPhysicalFeature.ThawedHalo);

    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "fire-features",
        0,
        IceRemembersMicrogeographyFacts.FireClimate,
        IceRemembersMicrogeographyFacts.FireFeatures,
        IceRemembersMicrogeographyFacts.Interactions |
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        ClearOwnedFeatures(map);
        var region = new CausalFeatureRegionClassifier(map.Physical.Grid);
        var hysteresis = context.Profile.MagicFeatureHysteresis;

        AddExactFeature(
            map,
            IceRemembersPhysicalFeature.FirecoreVent,
            triangleIndex => (map.Physical.FeatureFlags[triangleIndex] &
                PhysicalGeographyFeatureCodes.FireCore) != 0);
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.ThermalVein,
            triangleIndex => IsThermalVein(map, triangleIndex, 320, 430),
            triangleIndex => IsThermalVein(map, triangleIndex, 320 - hysteresis, 320));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.HotSpring,
            triangleIndex => IsHotSpringSeed(map, triangleIndex),
            triangleIndex => IsHotSpringRetained(map, triangleIndex, hysteresis));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.HydrothermalVent,
            triangleIndex => IsHydrothermalVentSeed(map, triangleIndex),
            triangleIndex => IsHydrothermalVentRetained(map, triangleIndex, hysteresis));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.BasaltFlat,
            triangleIndex => IsBasaltFlat(map, triangleIndex, 560, 45),
            triangleIndex => IsBasaltFlat(map, triangleIndex, 560 - hysteresis, 60));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.ThawedHalo,
            triangleIndex => IsLand(map, triangleIndex) &&
                map.FireIntensity[triangleIndex] >= 210 &&
                map.Physical.Temperature[triangleIndex] >= 250,
            triangleIndex => IsLand(map, triangleIndex) &&
                IsThawedHaloRetained(map, context, triangleIndex));
    }

    private static bool IsThermalVein(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int fire,
        int conductance) =>
        IsLand(map, triangleIndex) &&
        map.FireIntensity[triangleIndex] >= fire &&
        map.Physical.FractureConductance[triangleIndex] >= conductance;

    private static bool IsBasaltFlat(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int fire,
        int slopeUnits) =>
        IsLand(map, triangleIndex) &&
        map.FireIntensity[triangleIndex] >= fire &&
        map.Physical.Slope[triangleIndex] <= PhysicalElevation.FromUnits(slopeUnits);

    private static void ClearOwnedFeatures(IceRemembersMicrogeographyMap map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.SignificantFeatureFlags[triangleIndex] &= ~OwnedFeatures;
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            var flags = map.SignificantFeatureFlags[triangleIndex];
            if (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.FirecoreVent) !=
                ((map.Physical.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.FireCore) != 0) ||
                (IceRemembersFeatureMasks.Has(flags, IceRemembersPhysicalFeature.HotSpring) &&
                    !IsHotSpringRetained(map, triangleIndex, context.Profile.MagicFeatureHysteresis)))
            {
                throw new InvalidOperationException("Fire feature classification is inconsistent with its field.");
            }
        }
    }

    /// <summary>
    /// A hot spring is a land feature. Both of its routes were written for
    /// land - one wants a lake or river nearby, the other wants moisture - and
    /// over ocean every clause passes for free: seafloor near a plume is hot,
    /// tropical water is warm, and ocean is saturated by definition. That
    /// filled whole seas with hot springs. Seafloor vents are real, but they
    /// are a different feature and get their own.
    /// </summary>
    /// <summary>
    /// Open ocean. Fire features describe ground, and a seafloor near a plume
    /// satisfies most of their clauses for free - it is hot, it is flat, and
    /// it is wet - so without this check they march straight out to sea and
    /// whichever one is checked first wins the water.
    /// </summary>
    private static bool IsLand(IceRemembersMicrogeographyMap map, int triangleIndex) =>
        map.Physical.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.Ocean;

    private static bool IsHotSpringSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex)
    {
        if (!IsLand(map, triangleIndex))
        {
            return false;
        }

        var fire = map.FireIntensity[triangleIndex];
        var wetFireSpring = fire >= 430 &&
            (map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
                map.Physical.RiverOrders[triangleIndex] > 0 ||
                map.Physical.Moisture[triangleIndex] >= 650);
        var verdantGeothermalSpring = map.Physical.Geothermal[triangleIndex] >= 720 &&
            map.Physical.Temperature[triangleIndex] >= 335 &&
            map.Physical.Moisture[triangleIndex] >= 260;
        return wetFireSpring || verdantGeothermalSpring;
    }

    /// <summary>
    /// Seafloor hydrothermal vents: plume-fed ground under open water. Warm
    /// mineral-rich water is what makes a vent field worth anything, so this
    /// is the surface reef and krill can key on.
    /// </summary>
    private static bool IsHydrothermalVentSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex) =>
        map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
        map.Physical.Geothermal[triangleIndex] >= 720;

    private static bool IsHydrothermalVentRetained(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int hysteresis) =>
        map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
        map.Physical.Geothermal[triangleIndex] >= 720 - hysteresis;

    private static bool IsHotSpringRetained(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int hysteresis)
    {
        var wetFireSpring = map.FireIntensity[triangleIndex] >= 430 - hysteresis &&
            (map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake ||
                map.Physical.RiverOrders[triangleIndex] > 0 ||
                map.Physical.Moisture[triangleIndex] >= 560);
        var geothermalSpring = map.Physical.Geothermal[triangleIndex] >= 650 &&
            map.Physical.Temperature[triangleIndex] >= 300 &&
            map.Physical.Moisture[triangleIndex] >= 220;
        return wetFireSpring || geothermalSpring;
    }

    private static void AddExactFeature(
        IceRemembersMicrogeographyMap map,
        IceRemembersPhysicalFeature feature,
        Func<int, bool> includes)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            IceRemembersFeatureMasks.Add(
                ref map.SignificantFeatureFlags[triangleIndex],
                includes(triangleIndex),
                feature);
        }
    }

    /// <summary>
    /// A thaw halo is the ground a fire conduit has warmed, so it stays tied
    /// to the conduit that caused it and breaks up on a hashed scatter.
    /// Retaining on the raw fire-and-temperature thresholds alone painted the
    /// same broad wash over every warm region — the field-threshold
    /// anti-pattern that makes magic terrain read as bland.
    /// </summary>
    private static bool IsThawedHaloRetained(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context,
        int triangleIndex) =>
        map.FireIntensity[triangleIndex] >= 140 &&
        map.Physical.Temperature[triangleIndex] >= 225 &&
        map.Physical.FractureConductance[triangleIndex] >= 170 &&
        PhysicalNoise.HashUnit(context.Seed ^ 0x3f19, triangleIndex, 0x5b1) >= 0.14d;

    private static void AddRegionFeature(
        IceRemembersMicrogeographyMap map,
        CausalFeatureRegionClassifier region,
        IceRemembersPhysicalFeature feature,
        Func<int, bool> isSeed,
        Func<int, bool> isRetained)
    {
        region.Classify(isSeed, isRetained);
        AddExactFeature(map, feature, region.Includes);
    }
}
