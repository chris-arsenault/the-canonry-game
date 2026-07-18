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

        var majorLandmassIds = MajorLandmassIds(counts, profile.QualityTargets);
        return new QualityMeasurements(
            counts.OceanCount / (double)Math.Max(1, counts.TriangleCount),
            counts.LandIceCount / (double)Math.Max(1, counts.LandCount),
            counts.SeaIceCount / (double)Math.Max(1, counts.OceanCount),
            majorLandmassIds.Count,
            counts.LakeBodyIds.Count,
            counts.LandmassTriangleCounts.Count == 0
                ? 0d
                : counts.LandmassTriangleCounts.Values.Max() / (double)Math.Max(1, counts.LandCount),
            counts.IslandCount / (double)Math.Max(1, counts.LandCount),
            counts.CoastEdgeCount / (double)Math.Max(1, counts.LandCount * 3),
            MeasureLandmassSpacing(map, counts.LandmassCentroids, majorLandmassIds),
            counts.HotspotLandmassIds.Count);
    }

    private static HashSet<int> MajorLandmassIds(
        MutableQualityCounts counts,
        PhysicalGeographyQualityTargets targets)
    {
        var minimumTriangles = (int)Math.Ceiling(
            counts.LandCount * targets.MajorLandmassMinimumLandCoverage);
        return counts.LandmassTriangleCounts
            .Where(pair => pair.Value >= minimumTriangles)
            .Select(static pair => pair.Key)
            .ToHashSet();
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

        if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Lake)
        {
            counts.LakeBodyIds.Add(map.WaterBodyIds[triangleIndex]);
            return;
        }

        counts.LandCount++;
        counts.LandIceCount += map.IceThickness[triangleIndex] >= profile.GlacierThicknessThreshold ? 1 : 0;
        MeasureLandmass(map, triangleIndex, counts);
        MeasureHotspot(map, triangleIndex, counts);
        MeasureCoast(map, triangleIndex, counts);

    }

    private static void MeasureLandmass(
        PhysicalGeographyField map,
        int triangleIndex,
        MutableQualityCounts counts)
    {
        var landmassId = map.LandmassIds[triangleIndex];
        counts.LandmassTriangleCounts[landmassId] =
            counts.LandmassTriangleCounts.GetValueOrDefault(landmassId) + 1;
        if (!counts.LandmassCentroids.TryGetValue(landmassId, out var centroid))
        {
            centroid = new MutableCentroid();
            counts.LandmassCentroids.Add(landmassId, centroid);
        }

        centroid.X += map.Grid.TriangleX(triangleIndex);
        centroid.Y += map.Grid.TriangleY(triangleIndex);
        centroid.Count++;
        if (map.LandmassClassCodes[triangleIndex] == PhysicalLandmassClassCodes.Island)
        {
            counts.IslandCount++;
        }
    }

    private static void MeasureHotspot(
        PhysicalGeographyField map,
        int triangleIndex,
        MutableQualityCounts counts)
    {
        if ((map.FeatureFlags[triangleIndex] & PhysicalGeographyFeatureCodes.GeothermalVent) != 0 &&
            map.LandmassIds[triangleIndex] > 0)
        {
            counts.HotspotLandmassIds.Add(map.LandmassIds[triangleIndex]);
        }
    }

    private static void MeasureCoast(
        PhysicalGeographyField map,
        int triangleIndex,
        MutableQualityCounts counts)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
            {
                counts.CoastEdgeCount++;
            }
        }
    }

    private static double MeasureLandmassSpacing(
        PhysicalGeographyField map,
        IReadOnlyDictionary<int, MutableCentroid> centroids,
        HashSet<int> majorLandmassIds)
    {
        if (majorLandmassIds.Count < 2)
        {
            return 0d;
        }

        var values = majorLandmassIds.Select(id => centroids[id]).ToArray();
        var minimum = double.MaxValue;
        for (var first = 0; first < values.Length; first++)
        {
            var firstX = values[first].X / values[first].Count;
            var firstY = values[first].Y / values[first].Count;
            for (var second = first + 1; second < values.Length; second++)
            {
                var dx = firstX - (values[second].X / values[second].Count);
                var dy = firstY - (values[second].Y / values[second].Count);
                minimum = Math.Min(minimum, Math.Sqrt((dx * dx) + (dy * dy)));
            }
        }

        return Math.Clamp(minimum / Math.Max(1d, map.Grid.Radius * Math.Sqrt(3d) * 2d), 0d, 1d);
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
        ValidateCoverage(
            "Largest-landmass land",
            measurements.LargestLandmassLandCoverage,
            targets.LargestLandmassLandCoverage);
        ValidateCoverage("Island land", measurements.IslandLandCoverage, targets.IslandLandCoverage);
        ValidateCoverage("Coast-edge density", measurements.CoastEdgeDensity, targets.CoastEdgeDensity);
        ValidateCoverage(
            "Major-landmass spacing",
            measurements.MajorLandmassSpacing,
            targets.MajorLandmassSpacing);
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
        ValidateCount("Hotspot landmass", measurements.HotspotLandmassCount, targets.HotspotLandmassCount);
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

        public int IslandCount { get; set; }

        public int CoastEdgeCount { get; set; }

        public HashSet<int> LakeBodyIds { get; } = [];

        public HashSet<int> HotspotLandmassIds { get; } = [];

        public Dictionary<int, int> LandmassTriangleCounts { get; } = [];

        public Dictionary<int, MutableCentroid> LandmassCentroids { get; } = [];
    }

    private sealed class MutableCentroid
    {
        public double X { get; set; }

        public double Y { get; set; }

        public int Count { get; set; }
    }

    private sealed record QualityMeasurements(
        double OceanCoverage,
        double LandIceCoverage,
        double SeaIceCoverage,
        int MajorLandmassCount,
        int InlandLakeBodyCount,
        double LargestLandmassLandCoverage,
        double IslandLandCoverage,
        double CoastEdgeDensity,
        double MajorLandmassSpacing,
        int HotspotLandmassCount);
}
