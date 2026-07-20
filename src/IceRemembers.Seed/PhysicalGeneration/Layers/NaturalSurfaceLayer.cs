namespace IceRemembers.Seed;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal sealed class NaturalSurfaceLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "natural-surfaces",
        0,
        IceRemembersMicrogeographyFacts.None,
        IceRemembersMicrogeographyFacts.NaturalSurfaces,
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.BaseSurfaceIndexes[triangleIndex] = (int)Classify(
                map.Physical,
                triangleIndex,
                context.Profile);
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        foreach (var surfaceIndex in map.BaseSurfaceIndexes)
        {
            if (surfaceIndex is < (int)IceRemembersPhysicalSurface.Tundra or
                > (int)IceRemembersPhysicalSurface.Trench)
            {
                throw new InvalidOperationException("Natural surface classification produced an invalid surface.");
            }
        }
    }

    private static IceRemembersPhysicalSurface Classify(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean
            ? ClassifyOcean(field, triangleIndex, profile)
            : ClassifyLand(field, triangleIndex, profile);

    private static IceRemembersPhysicalSurface ClassifyOcean(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        var special = ClassifyOceanSpecial(field, triangleIndex, profile);
        if (special.HasValue)
        {
            return special.Value;
        }

        var shallow = field.BathymetricZoneCodes[triangleIndex] is
            PhysicalBathymetricZoneCodes.Shelf or PhysicalBathymetricZoneCodes.ShelfBreak;
        var frozen = ClassifyFrozenOcean(field, triangleIndex, shallow, profile);
        if (frozen.HasValue)
        {
            return frozen.Value;
        }

        if (IsTrench(field, triangleIndex, profile))
        {
            return IceRemembersPhysicalSurface.Trench;
        }

        if (field.OceanCurrentStrength[triangleIndex] >= profile.CurrentChannelMinimumStrength)
        {
            return IceRemembersPhysicalSurface.CurrentChannel;
        }

        return shallow ? IceRemembersPhysicalSurface.Shelf : IceRemembersPhysicalSurface.Ocean;
    }

    private static bool IsTrench(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        field.BathymetricZoneCodes[triangleIndex] == PhysicalBathymetricZoneCodes.DeepOcean &&
        field.TectonicBoundaryCodes[triangleIndex] is
            PhysicalTectonicBoundaryCodes.Convergent or PhysicalTectonicBoundaryCodes.Divergent &&
        field.TectonicStrain[triangleIndex] >= profile.TrenchMinimumStrain;

    private static IceRemembersPhysicalSurface? ClassifyOceanSpecial(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        if (field.DeltaIntensity[triangleIndex] > 0)
        {
            return IceRemembersPhysicalSurface.Delta;
        }

        if (field.LandformCodes[triangleIndex] == PhysicalLandformCodes.Fjord)
        {
            return IceRemembersPhysicalSurface.Fjord;
        }

        var shallow = field.BathymetricZoneCodes[triangleIndex] is
            PhysicalBathymetricZoneCodes.Shelf or PhysicalBathymetricZoneCodes.ShelfBreak;
        if (shallow && field.WaterDepths[triangleIndex] <=
                PhysicalElevation.FromUnits(profile.ReefMaximumDepth) &&
            (field.Temperature[triangleIndex] >= profile.ReefMinimumTemperature ||
                field.OceanCurrentHeat[triangleIndex] >= profile.ReefWarmCurrentHeat) &&
            field.Geothermal[triangleIndex] >= profile.ReefMinimumGeothermal)
        {
            return IceRemembersPhysicalSurface.Reef;
        }

        return null;
    }

    private static IceRemembersPhysicalSurface? ClassifyFrozenOcean(
        PhysicalGeographyField field,
        int triangleIndex,
        bool shallow,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        if (shallow && field.IceThickness[triangleIndex] >= profile.IceShelfMinimumThickness)
        {
            return IceRemembersPhysicalSurface.IceShelf;
        }

        if ((field.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.PermanentSeaIce) != 0)
        {
            return IceRemembersPhysicalSurface.PackIce;
        }

        if ((field.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.PermanentIceMargin) != 0 ||
            field.IceThickness[triangleIndex] >= profile.PackIceMinimumThickness)
        {
            return IceRemembersPhysicalSurface.SeaIce;
        }

        if (HasFrozenOceanNeighbor(field, triangleIndex))
        {
            return IceRemembersPhysicalSurface.Polynya;
        }

        return null;
    }

    private static bool HasFrozenOceanNeighbor(PhysicalGeographyField field, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean &&
                field.IceThickness[neighbor] > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static IceRemembersPhysicalSurface ClassifyLand(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        var physical = ClassifyLandPhysicalOverride(field, triangleIndex, profile);
        if (physical.HasValue)
        {
            return physical.Value;
        }

        var shape = ClassifyLandShape(field, triangleIndex, profile);
        return shape ?? ClassifyLandClimate(field, triangleIndex, profile);
    }

    private static IceRemembersPhysicalSurface? ClassifyLandPhysicalOverride(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        var water = ClassifyFluvialOverride(field, triangleIndex);
        if (water.HasValue)
        {
            return water.Value;
        }

        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
        {
            return IceRemembersPhysicalSurface.Lake;
        }

        if (field.BathymetricZoneCodes[triangleIndex] == PhysicalBathymetricZoneCodes.ExposedShelf)
        {
            return IceRemembersPhysicalSurface.ExposedShelf;
        }

        return (field.LandformCodes[triangleIndex], field.IceThickness[triangleIndex]) switch
        {
            (PhysicalLandformCodes.Moraine, _) => IceRemembersPhysicalSurface.Moraine,
            (_, var ice) when ice >= profile.GlacierMinimumThickness => IceRemembersPhysicalSurface.Glacier,
            (_, var ice) when ice >= profile.SnowfieldMinimumThickness => IceRemembersPhysicalSurface.Snowfield,
            _ => null,
        };
    }

    private static IceRemembersPhysicalSurface? ClassifyFluvialOverride(
        PhysicalGeographyField field,
        int triangleIndex)
    {
        if (field.DeltaIntensity[triangleIndex] > 0)
        {
            return IceRemembersPhysicalSurface.Delta;
        }

        return field.LandformCodes[triangleIndex] switch
        {
            PhysicalLandformCodes.Waterfall => IceRemembersPhysicalSurface.Waterfall,
            PhysicalLandformCodes.Gorge => IceRemembersPhysicalSurface.Gorge,
            _ when field.RiverChannelStrength[triangleIndex] > 0 =>
                IceRemembersPhysicalSurface.RiverChannel,
            _ when field.FloodplainIntensity[triangleIndex] > 0 =>
                IceRemembersPhysicalSurface.Floodplain,
            _ => null,
        };
    }

    private static IceRemembersPhysicalSurface? ClassifyLandShape(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        if (field.RiverOrders[triangleIndex] >= profile.RiverValleyMinimumOrder)
        {
            return IceRemembersPhysicalSurface.RiverValley;
        }

        if (BordersRiverChannel(field, triangleIndex, profile.RiverValleyMinimumOrder))
        {
            return IceRemembersPhysicalSurface.RiverValley;
        }

        if (field.CumulativeFluvialErosion[triangleIndex] >= PhysicalElevation.FromUnits(
                profile.AncestralRiverValleyMinimumErosion) &&
            field.Slope[triangleIndex] <= PhysicalElevation.FromUnits(
                profile.AncestralRiverValleyMaximumSlope))
        {
            return IceRemembersPhysicalSurface.RiverValley;
        }

        if (field.Slope[triangleIndex] >= PhysicalElevation.FromUnits(profile.ScreeFieldMinimumSlope))
        {
            return IceRemembersPhysicalSurface.ScreeField;
        }

        if (field.LandformCodes[triangleIndex] is
            PhysicalLandformCodes.Ridge or PhysicalLandformCodes.Peak or PhysicalLandformCodes.Pass or
            PhysicalLandformCodes.ImpactRim)
        {
            return IceRemembersPhysicalSurface.Ridge;
        }

        return null;
    }

    private static bool BordersRiverChannel(
        PhysicalGeographyField field,
        int triangleIndex,
        int minimumOrder)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.RiverChannelOrders[neighbor] >= minimumOrder)
            {
                return true;
            }
        }

        return false;
    }

    private static IceRemembersPhysicalSurface ClassifyLandClimate(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        if (IsOpenField(field, triangleIndex, profile))
        {
            return IceRemembersPhysicalSurface.OpenField;
        }

        if (field.Moisture[triangleIndex] >= profile.WetTundraMinimumMoisture)
        {
            return IceRemembersPhysicalSurface.WetTundra;
        }

        if (IsPolarBarrens(field, triangleIndex, profile))
        {
            return IceRemembersPhysicalSurface.PolarBarrens;
        }

        return IsColdSteppe(field, triangleIndex, profile)
            ? IceRemembersPhysicalSurface.ColdSteppe
            : IceRemembersPhysicalSurface.Tundra;
    }

    private static bool IsOpenField(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        field.Temperature[triangleIndex] >= profile.OpenFieldMinimumTemperature &&
        field.Moisture[triangleIndex] >= profile.OpenFieldMinimumMoisture;

    private static bool IsPolarBarrens(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile) =>
        field.Temperature[triangleIndex] <= profile.PolarBarrensMaximumTemperature &&
        field.Moisture[triangleIndex] <= profile.PolarBarrensMaximumMoisture;

    private static bool IsColdSteppe(
        PhysicalGeographyField field,
        int triangleIndex,
        IceRemembersMicrogeographyGenerationProfile profile)
    {
        return field.Temperature[triangleIndex] >= profile.ColdSteppeMinimumTemperature &&
        field.Moisture[triangleIndex] >= profile.ColdSteppeMinimumMoisture &&
        field.Moisture[triangleIndex] <= profile.ColdSteppeMaximumMoisture &&
        field.Slope[triangleIndex] <= PhysicalElevation.FromUnits(profile.ColdSteppeMaximumSlope);
    }
}
