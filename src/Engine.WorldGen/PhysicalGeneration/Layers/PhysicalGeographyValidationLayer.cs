namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class PhysicalGeographyValidationLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "physical-quality-validation",
        0,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms,
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        ValidateCodes(map);
        ValidateWater(map);
        _ = FlowGraphAlgorithms.SourceToOutletOrder(map);
        ValidateRivers(map);
        if (map.Grid.Radius >= context.Profile.QualityTargets.MinimumEvaluationRadius)
        {
            ValidateConfigurableTargets(map, context.Profile);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = map;
        _ = context;
    }

    private static void ValidateCodes(PhysicalGeographyField map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.LandformCodes[triangleIndex] is < PhysicalLandformCodes.Min or > PhysicalLandformCodes.Max ||
                map.WaterKindCodes[triangleIndex] is < PhysicalWaterKindCodes.Min or > PhysicalWaterKindCodes.Max ||
                map.BathymetricZoneCodes[triangleIndex] is < PhysicalBathymetricZoneCodes.Min or
                    > PhysicalBathymetricZoneCodes.Max ||
                (map.FeatureFlags[triangleIndex] & ~PhysicalGeographyFeatureCodes.All) != 0)
            {
                throw new InvalidOperationException("Physical geography contains an invalid primitive code.");
            }
        }
    }

    private static void ValidateWater(PhysicalGeographyField map)
    {
        var ocean = 0;
        var lakeSurfaces = new Dictionary<int, int>();
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                ocean++;
                ValidateOceanTriangle(map, triangleIndex);
            }
            else if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
            {
                ValidateLakeTriangle(map, lakeSurfaces, triangleIndex);
            }
        }

        if (ocean == 0)
        {
            throw new InvalidOperationException("Physical geography contains no connected ocean.");
        }
    }

    private static void ValidateOceanTriangle(PhysicalGeographyField map, int triangleIndex)
    {
        if (map.WaterBodyIds[triangleIndex] != 1 || map.WaterDepths[triangleIndex] <= 0)
        {
            throw new InvalidOperationException("Ocean triangle lacks depth or body identity.");
        }
    }

    private static void ValidateLakeTriangle(
        PhysicalGeographyField map,
        Dictionary<int, int> lakeSurfaces,
        int triangleIndex)
    {
        var bodyId = map.WaterBodyIds[triangleIndex];
        var surface = map.Elevation[triangleIndex] + map.WaterDepths[triangleIndex];
        if (bodyId <= 1 || map.WaterDepths[triangleIndex] <= 0 ||
            (!lakeSurfaces.TryAdd(bodyId, surface) && lakeSurfaces[bodyId] != surface))
        {
            throw new InvalidOperationException("Lake triangle lacks a consistent water surface.");
        }
    }

    private static void ValidateRivers(PhysicalGeographyField map)
    {
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.RiverOrders[triangleIndex] <= 0)
            {
                continue;
            }

            var target = map.FlowTargets[triangleIndex];
            if (target < 0 || map.FlowAccumulation[target] < map.FlowAccumulation[triangleIndex])
            {
                throw new InvalidOperationException("River network does not continue toward an outlet.");
            }
        }
    }

    private static void ValidateConfigurableTargets(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile)
    {
        var measurements = MeasureQuality(map, profile);
        ValidateCoverageTargets(measurements, profile.QualityTargets);
        ValidateCountTargets(measurements, profile.QualityTargets);
    }

    private static QualityMeasurements MeasureQuality(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile)
    {
        var counts = new MutableQualityCounts();
        for (var configuredIndex = 0; configuredIndex < map.Grid.ConfiguredHexCount; configuredIndex++)
        {
            var hexIndex = map.Grid.ConfiguredHexIndexAt(configuredIndex);
            for (var local = 0; local < PhysicalGeographyCodes.MicrotrianglesPerHex; local++)
            {
                var triangleIndex = map.Grid.TriangleIndex(hexIndex, local);
                MeasureTriangle(map, profile, triangleIndex, counts);
            }
        }

        return new QualityMeasurements(
            counts.OceanCount / (double)Math.Max(1, counts.TriangleCount),
            counts.LandIceCount / (double)Math.Max(1, counts.LandCount),
            counts.SeaIceCount / (double)Math.Max(1, counts.OceanCount),
            counts.LandmassIds.Count,
            counts.LakeBodyIds.Count);
    }

    private static void MeasureTriangle(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        MutableQualityCounts counts)
    {
        counts.TriangleCount++;
        if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
        {
            counts.OceanCount++;
            counts.SeaIceCount += map.IceThickness[triangleIndex] > 0 ? 1 : 0;
            return;
        }

        counts.LandCount++;
        counts.LandIceCount += map.IceThickness[triangleIndex] >= profile.GlacierThicknessThreshold ? 1 : 0;
        if (map.LandmassClassCodes[triangleIndex] == PhysicalLandmassClassCodes.Continent)
        {
            counts.LandmassIds.Add(map.LandmassIds[triangleIndex]);
        }

        if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
        {
            counts.LakeBodyIds.Add(map.WaterBodyIds[triangleIndex]);
        }
    }

    private static void ValidateCoverageTargets(
        QualityMeasurements measurements,
        PhysicalGeographyQualityTargets targets)
    {
        if (!targets.OceanCoverage.Contains(measurements.OceanCoverage))
        {
            throw new InvalidOperationException(
                $"Ocean coverage {measurements.OceanCoverage:P1} misses configured target " +
                $"{targets.OceanCoverage.Minimum:P1}–{targets.OceanCoverage.Maximum:P1}.");
        }

        ValidateCoverage("Land-ice", measurements.LandIceCoverage, targets.LandIceCoverage);
        ValidateCoverage("Sea-ice", measurements.SeaIceCoverage, targets.SeaIceCoverage);
    }

    private static void ValidateCoverage(string name, double value, CoverageTarget target)
    {
        if (!target.Contains(value))
        {
            throw new InvalidOperationException(
                $"{name} coverage {value:P1} misses configured target " +
                $"{target.Minimum:P1}–{target.Maximum:P1}.");
        }
    }

    private static void ValidateCountTargets(
        QualityMeasurements measurements,
        PhysicalGeographyQualityTargets targets)
    {
        ValidateCount("Major landmass", measurements.MajorLandmassCount, targets.MajorLandmassCount);
        ValidateCount("Inland lake", measurements.InlandLakeBodyCount, targets.InlandLakeBodyCount);
    }

    private static void ValidateCount(string name, int value, CountTarget target)
    {
        if (!target.Contains(value))
        {
            throw new InvalidOperationException(
                $"{name} count {value} misses configured target {target.Minimum}–{target.Maximum}.");
        }
    }

    private sealed class MutableQualityCounts
    {
        public int TriangleCount { get; set; }

        public int OceanCount { get; set; }

        public int LandCount { get; set; }

        public int LandIceCount { get; set; }

        public int SeaIceCount { get; set; }

        public HashSet<int> LandmassIds { get; } = [];

        public HashSet<int> LakeBodyIds { get; } = [];
    }

    private sealed record QualityMeasurements(
        double OceanCoverage,
        double LandIceCoverage,
        double SeaIceCoverage,
        int MajorLandmassCount,
        int InlandLakeBodyCount);
}
