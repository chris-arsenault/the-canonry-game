namespace Engine.WorldGen;

using Engine.Constants;

internal static class PhysicalClimate
{
    public static void Generate(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var oceanDistance = PhysicalHydrology.MeasureOceanDistance(field);
        GenerateGeothermal(field, seed, profile);
        var pockets = CreateWarmthPockets(field, oceanDistance, seed, profile);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var positiveElevation = Math.Max(0, field.Elevation[triangleIndex]);
            var lapse = positiveElevation * profile.ElevationTemperatureLapse / profile.ElevationScale;
            var oceanModeration = oceanDistance[triangleIndex] < 0
                ? 0
                : Math.Max(
                    0,
                    profile.OceanModerationStrength -
                        (oceanDistance[triangleIndex] * profile.OceanModerationStrength /
                            profile.OceanModerationDistance));
            var warmth = WarmthAt(x, y, pockets);
            var geothermal = field.Geothermal[triangleIndex] * profile.GeothermalTemperatureStrength / 1000;
            field.Temperature[triangleIndex] = Math.Clamp(
                profile.TemperatureBaseline - lapse + oceanModeration + geothermal + warmth,
                0,
                1000);
            GenerateMoisture(field, oceanDistance, triangleIndex, x, y, seed, profile);
            SetClimateFeatures(field, triangleIndex, profile);
        }
    }

    private static void GenerateGeothermal(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var x = field.Grid.TriangleX(triangleIndex);
            var y = field.Grid.TriangleY(triangleIndex);
            var ridgeRoot = field.RidgeInfluence[triangleIndex] * 0.36d;
            var broad = PhysicalNoise.UnitNoise(x, y, seed ^ 0x152b, 41d, 3) * 0.18d;
            var hotspotNoise = PhysicalNoise.UnitNoise(x, y, seed ^ 0x37c1, 19d, 2);
            var hotspot = Math.Max(0d, hotspotNoise - 0.76d) * 3.2d;
            field.Geothermal[triangleIndex] = ScaleUnit(Math.Clamp(ridgeRoot + broad + hotspot, 0d, 1d));
        }

        ForceVerdantPeakSources(field, seed, profile);
    }

    private static void ForceVerdantPeakSources(
        PhysicalGeographyField field,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        const int sourceCount = 4;
        for (var sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
        {
            var best = -1;
            var bestScore = long.MinValue;
            for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
            {
                if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None ||
                    field.Elevation[triangleIndex] < profile.PeakElevationThreshold)
                {
                    continue;
                }

                var separation = MinimumSelectedSeparation(field, triangleIndex, sourceIndex, seed);
                var score = (field.Elevation[triangleIndex] * 1000L) +
                    (long)(field.RidgeInfluence[triangleIndex] * 200_000f) + separation;
                if (score > bestScore)
                {
                    best = triangleIndex;
                    bestScore = score;
                }
            }

            if (best >= 0)
            {
                PaintGeothermalSource(field, best, 8d, 900);
            }
        }
    }

    private static long MinimumSelectedSeparation(
        PhysicalGeographyField field,
        int triangleIndex,
        int sourceIndex,
        int seed) =>
        (long)(PhysicalNoise.HashUnit(seed ^ 0x6a2d, triangleIndex, sourceIndex) * 50_000d);

    private static void PaintGeothermalSource(
        PhysicalGeographyField field,
        int source,
        double radius,
        int strength)
    {
        var sourceX = field.Grid.TriangleX(source);
        var sourceY = field.Grid.TriangleY(source);
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var dx = field.Grid.TriangleX(triangleIndex) - sourceX;
            var dy = field.Grid.TriangleY(triangleIndex) - sourceY;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance <= radius)
            {
                var amount = 1d - (distance / radius);
                field.Geothermal[triangleIndex] = Math.Max(
                    field.Geothermal[triangleIndex],
                    (int)Math.Round(strength * amount, MidpointRounding.AwayFromZero));
            }
        }
    }

    private static WarmthPocket[] CreateWarmthPockets(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        var pockets = new WarmthPocket[profile.WarmthPocketCount + 1];
        pockets[0] = new WarmthPocket(0d, 0d, profile.HeartWarmthRadius, profile.HeartWarmthStrength);
        for (var index = 1; index < pockets.Length; index++)
        {
            var category = (WarmthCategory)((index - 1) % 4);
            var source = SelectWarmthSource(field, oceanDistance, seed, index, category, profile);
            var radiusAmount = PhysicalNoise.HashUnit(seed ^ 0x14d3, index, 0);
            var strengthAmount = PhysicalNoise.HashUnit(seed ^ 0x29f1, index, 1);
            pockets[index] = new WarmthPocket(
                field.Grid.TriangleX(source),
                field.Grid.TriangleY(source),
                Lerp(profile.WarmthPocketMinRadius, profile.WarmthPocketMaxRadius, radiusAmount),
                (int)Math.Round(
                    Lerp(profile.WarmthPocketMinStrength, profile.WarmthPocketMaxStrength, strengthAmount),
                    MidpointRounding.AwayFromZero));
        }

        return pockets;
    }

    private static int SelectWarmthSource(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int seed,
        int pocketIndex,
        WarmthCategory category,
        PhysicalGeographyGenerationProfile profile)
    {
        var best = -1;
        var bestScore = double.MinValue;
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (!EligibleWarmthSource(field, oceanDistance, triangleIndex, category, profile))
            {
                continue;
            }

            var hash = PhysicalNoise.HashUnit(seed ^ 0x4b67, triangleIndex, pocketIndex);
            var geothermalBonus = category == WarmthCategory.Peak
                ? field.Geothermal[triangleIndex] / 1000d
                : 0d;
            var score = hash + geothermalBonus;
            if (score > bestScore)
            {
                best = triangleIndex;
                bestScore = score;
            }
        }

        return best >= 0 ? best : field.Grid.TriangleIndex(field.Grid.ConfiguredHexIndexAt(0), 0);
    }

    private static bool EligibleWarmthSource(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex,
        WarmthCategory category,
        PhysicalGeographyGenerationProfile profile) => category switch
    {
        WarmthCategory.Ocean => field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean,
        WarmthCategory.Shore => IsShoreSource(field, oceanDistance, triangleIndex),
        WarmthCategory.Inland => IsInlandSource(field, oceanDistance, triangleIndex, profile),
        WarmthCategory.Peak => IsPeakSource(field, triangleIndex, profile),
        _ => false,
    };

    private static bool IsShoreSource(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex) =>
        field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
        oceanDistance[triangleIndex] is >= 1 and <= 12;

    private static bool IsInlandSource(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
        oceanDistance[triangleIndex] >= 24 &&
        field.Elevation[triangleIndex] < profile.PeakElevationThreshold;

    private static bool IsPeakSource(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile) =>
        field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
        field.Elevation[triangleIndex] >= profile.PeakElevationThreshold &&
        field.Geothermal[triangleIndex] >= 450;

    private static int WarmthAt(double x, double y, ReadOnlySpan<WarmthPocket> pockets)
    {
        var strongest = 0d;
        foreach (var pocket in pockets)
        {
            var dx = x - pocket.X;
            var dy = y - pocket.Y;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));
            if (distance < pocket.Radius)
            {
                var amount = 1d - (distance / pocket.Radius);
                strongest = Math.Max(strongest, pocket.Strength * amount * amount);
            }
        }

        return (int)Math.Round(strongest, MidpointRounding.AwayFromZero);
    }

    private static void GenerateMoisture(
        PhysicalGeographyField field,
        int[] oceanDistance,
        int triangleIndex,
        double x,
        double y,
        int seed,
        PhysicalGeographyGenerationProfile profile)
    {
        if (field.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
        {
            field.Moisture[triangleIndex] = 1000;
            field.Runoff[triangleIndex] = 1000;
            return;
        }

        var exposure = oceanDistance[triangleIndex] < 0
            ? 0
            : Math.Max(0, 650 - (oceanDistance[triangleIndex] * 650 / profile.OceanMoistureDistance));
        var weather = (int)Math.Round(
            PhysicalNoise.UnitNoise(x, y, seed ^ 0x5e29, 33d, 3) * 260d,
            MidpointRounding.AwayFromZero);
        var upliftDrying = (int)Math.Round(
            field.RidgeInfluence[triangleIndex] * 170f,
            MidpointRounding.AwayFromZero);
        var thawMoisture = Math.Max(0, field.Temperature[triangleIndex] - 260) / 2;
        field.Moisture[triangleIndex] = Math.Clamp(90 + exposure + weather + thawMoisture - upliftDrying, 0, 1000);
        var meltFactor = 320 + (field.Temperature[triangleIndex] * 680 / 1000);
        field.Runoff[triangleIndex] = Math.Max(1, field.Moisture[triangleIndex] * meltFactor / 1000);
    }

    private static void SetClimateFeatures(
        PhysicalGeographyField field,
        int triangleIndex,
        PhysicalGeographyGenerationProfile profile)
    {
        var flags = field.FeatureFlags[triangleIndex] &
            ~(PhysicalGeographyFeatureCodes.GeothermalVent |
                PhysicalGeographyFeatureCodes.PermanentSeaIce |
                PhysicalGeographyFeatureCodes.PermanentIceMargin);
        if (field.Geothermal[triangleIndex] >= profile.GeothermalVentThreshold)
        {
            flags |= PhysicalGeographyFeatureCodes.GeothermalVent;
        }

        if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean &&
            field.Temperature[triangleIndex] <= profile.PermanentSeaIceTemperature)
        {
            flags |= PhysicalGeographyFeatureCodes.PermanentSeaIce;
        }

        if (field.Temperature[triangleIndex] > profile.PermanentSeaIceTemperature &&
            field.Temperature[triangleIndex] <= profile.IceMarginTemperature)
        {
            flags |= PhysicalGeographyFeatureCodes.PermanentIceMargin;
        }

        field.FeatureFlags[triangleIndex] = flags;
    }

    private static int ScaleUnit(double value) =>
        (int)Math.Round(Math.Clamp(value, 0d, 1d) * 1000d, MidpointRounding.AwayFromZero);

    private static double Lerp(double first, double second, double amount) =>
        first + ((second - first) * amount);

    private readonly record struct WarmthPocket(double X, double Y, double Radius, int Strength);

    private enum WarmthCategory
    {
        Ocean,
        Shore,
        Inland,
        Peak,
    }
}

internal static class PhysicalGlaciation
{
    public static void Apply(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        CalculateIceThickness(field, profile);
        AccumulateIceFlux(field);
        CarveGlacialLandforms(field, profile);
    }

    private static void CalculateIceThickness(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            var cold = Math.Max(0, profile.IceTemperatureThreshold - field.Temperature[triangleIndex]);
            var altitude = Math.Max(0, field.Elevation[triangleIndex]) / 3;
            var concavity = LocalConcavity(field, triangleIndex);
            var moisture = field.Moisture[triangleIndex] / 5;
            var oceanPenalty = field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean ? 160 : 0;
            field.IceThickness[triangleIndex] = Math.Clamp(
                (cold * 3) + altitude + concavity + moisture - oceanPenalty,
                0,
                1000);
        }
    }

    private static int LocalConcavity(PhysicalGeographyField field, int triangleIndex)
    {
        var total = 0;
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += field.Elevation[neighbor] - field.Elevation[triangleIndex];
                count++;
            }
        }

        return count == 0 ? 0 : Math.Max(0, total / count) * 2;
    }

    private static void AccumulateIceFlux(PhysicalGeographyField field)
    {
        var targets = new int[field.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < targets.Length; triangleIndex++)
        {
            var height = field.Elevation[triangleIndex] + (field.IceThickness[triangleIndex] / 4);
            var target = -1;
            var targetHeight = height;
            for (var edge = 0; edge < 3; edge++)
            {
                var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
                if (neighbor < 0)
                {
                    continue;
                }

                var neighborHeight = field.Elevation[neighbor] + (field.IceThickness[neighbor] / 4);
                if (neighborHeight < targetHeight)
                {
                    target = neighbor;
                    targetHeight = neighborHeight;
                }
            }

            targets[triangleIndex] = target;
            field.IceFlux[triangleIndex] = field.IceThickness[triangleIndex];
        }

        var order = PhysicalTerrainFormation.ElevationOrder(field.Elevation);
        for (var orderIndex = order.Length - 1; orderIndex >= 0; orderIndex--)
        {
            var triangleIndex = order[orderIndex];
            var target = targets[triangleIndex];
            if (target >= 0)
            {
                field.IceFlux[target] = SaturatingAdd(field.IceFlux[target], field.IceFlux[triangleIndex]);
            }
        }
    }

    private static void CarveGlacialLandforms(
        PhysicalGeographyField field,
        PhysicalGeographyGenerationProfile profile)
    {
        var originalElevation = field.Elevation.ToArray();
        for (var triangleIndex = 0; triangleIndex < field.Grid.TriangleCount; triangleIndex++)
        {
            if (field.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            var flux = field.IceFlux[triangleIndex];
            if (flux >= profile.GlacierFluxThreshold)
            {
                var neighborMean = NeighborMean(originalElevation, field.Grid, triangleIndex);
                field.Elevation[triangleIndex] = (originalElevation[triangleIndex] + neighborMean) / 2 -
                    Math.Min(24, flux / Math.Max(1, profile.GlacierFluxThreshold));
                field.LandformCodes[triangleIndex] = PhysicalLandformCodes.UValley;
            }
            else if (field.IceThickness[triangleIndex] >= profile.GlacierThicknessThreshold &&
                field.Elevation[triangleIndex] >= profile.PeakElevationThreshold &&
                LocalConcavity(field, triangleIndex) > 20)
            {
                field.Elevation[triangleIndex] -= 18;
                field.LandformCodes[triangleIndex] = PhysicalLandformCodes.Cirque;
            }
            else if (field.IceThickness[triangleIndex] >= profile.GlacierThicknessThreshold &&
                flux < profile.GlacierFluxThreshold / 2 &&
                HasHighFluxNeighbor(field, triangleIndex, profile.GlacierFluxThreshold))
            {
                field.Elevation[triangleIndex] += 20;
                field.LandformCodes[triangleIndex] = PhysicalLandformCodes.Moraine;
            }
        }
    }

    private static int NeighborMean(int[] elevation, PhysicalGeographyGrid grid, int triangleIndex)
    {
        var total = 0;
        var count = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0)
            {
                total += elevation[neighbor];
                count++;
            }
        }

        return count == 0 ? elevation[triangleIndex] : total / count;
    }

    private static bool HasHighFluxNeighbor(
        PhysicalGeographyField field,
        int triangleIndex,
        int threshold)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = field.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && field.IceFlux[neighbor] >= threshold)
            {
                return true;
            }
        }

        return false;
    }

    private static int SaturatingAdd(int first, int second) =>
        first > int.MaxValue - second ? int.MaxValue : first + second;
}
