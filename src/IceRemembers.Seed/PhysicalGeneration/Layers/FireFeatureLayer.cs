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
            triangleIndex => map.FireIntensity[triangleIndex] >= 320 &&
                map.Physical.FractureConductance[triangleIndex] >= 430,
            triangleIndex => map.FireIntensity[triangleIndex] >= 320 - hysteresis &&
                map.Physical.FractureConductance[triangleIndex] >= 320);
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
            triangleIndex => map.FireIntensity[triangleIndex] >= 560 &&
                map.Physical.Slope[triangleIndex] <= PhysicalElevation.FromUnits(45),
            triangleIndex => map.FireIntensity[triangleIndex] >= 560 - hysteresis &&
                map.Physical.Slope[triangleIndex] <= PhysicalElevation.FromUnits(60));
        AddRegionFeature(
            map,
            region,
            IceRemembersPhysicalFeature.ThawedHalo,
            triangleIndex => map.FireIntensity[triangleIndex] >= 210 &&
                map.Physical.Temperature[triangleIndex] >= 250,
            triangleIndex => IsThawedHaloRetained(map, context, triangleIndex));
    }

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
    private static bool IsHotSpringSeed(
        IceRemembersMicrogeographyMap map,
        int triangleIndex)
    {
        if (map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
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
