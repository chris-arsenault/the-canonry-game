namespace Engine.WorldGen;

using Engine.Constants;

internal sealed class CoastalErosionLayer : IPhysicalGeographyLayer
{
    public PhysicalGeographyLayerContract Contract { get; } = new(
        "coastal-erosion-and-deposition",
        0,
        PhysicalGeographyFacts.Elevation |
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.LandscapeHistory |
        PhysicalGeographyFacts.SedimentTransport,
        PhysicalGeographyFacts.Elevation | PhysicalGeographyFacts.CoastalEvolution,
        PhysicalGeographyFacts.Ocean |
        PhysicalGeographyFacts.Bathymetry |
        PhysicalGeographyFacts.Landmasses |
        PhysicalGeographyFacts.OceanCurrents |
        PhysicalGeographyFacts.Warmth |
        PhysicalGeographyFacts.Temperature |
        PhysicalGeographyFacts.AtmosphericFlow |
        PhysicalGeographyFacts.Moisture |
        PhysicalGeographyFacts.Precipitation |
        PhysicalGeographyFacts.Runoff |
        PhysicalGeographyFacts.Lakes |
        PhysicalGeographyFacts.FlowRouting |
        PhysicalGeographyFacts.Rivers |
        PhysicalGeographyFacts.LandIce |
        PhysicalGeographyFacts.IceFlow |
        PhysicalGeographyFacts.SeaIce |
        PhysicalGeographyFacts.Landforms |
        PhysicalGeographyFacts.Validated);

    public void Apply(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        var coastalWave = MeasureCoastalWaveHistory(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            var wave = coastalWave[triangleIndex];
            if (wave < context.Profile.CoastalErosionWaveThreshold ||
                map.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            var cut = ErosionAmount(map, context.Profile, triangleIndex, wave);
            cut = cut * context.CoastalStrength / 1000;
            if (cut <= 0)
            {
                continue;
            }

            _ = LandscapeSurface.Erode(map, triangleIndex, cut);
            map.CumulativeCoastalErosion[triangleIndex] = checked(
                map.CumulativeCoastalErosion[triangleIndex] + cut);
            map.SedimentProducedTotal = checked(map.SedimentProducedTotal + cut);
            DepositOrExport(map, context.Profile, coastalWave, triangleIndex, wave, cut);
        }
    }

    public void Validate(PhysicalGeographyField map, PhysicalGeographyGenerationContext context)
    {
        _ = context;
        LandscapeSurface.Validate(map);
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.CumulativeCoastalErosion[triangleIndex] > 0 &&
                map.WaveExposureHistory[triangleIndex] <= 0 &&
                map.ExternalCoastalForcingHistory[triangleIndex] <= 0)
            {
                throw new InvalidOperationException("Coastal erosion lacks wave-exposure history.");
            }
        }
    }

    private static int[] MeasureCoastalWaveHistory(PhysicalGeographyField map)
    {
        var coastalWave = new int[map.Grid.TriangleCount];
        for (var triangleIndex = 0; triangleIndex < map.Grid.TriangleCount; triangleIndex++)
        {
            if (map.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.Ocean)
            {
                map.WaveExposureHistory[triangleIndex] = Math.Max(
                    map.WaveExposureHistory[triangleIndex],
                    map.WaveExposure[triangleIndex]);
                continue;
            }

            var wave = AdjacentOceanWave(map, triangleIndex);
            coastalWave[triangleIndex] = wave;
            map.WaveExposureHistory[triangleIndex] = Math.Max(
                map.WaveExposureHistory[triangleIndex],
                wave);
        }

        return coastalWave;
    }

    private static int AdjacentOceanWave(PhysicalGeographyField map, int triangleIndex)
    {
        var wave = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 && map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
            {
                wave = Math.Max(wave, map.WaveExposure[neighbor]);
            }
        }

        return wave;
    }

    private static int ErosionAmount(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int triangleIndex,
        int wave)
    {
        var waveExcess = wave - profile.CoastalErosionWaveThreshold;
        var shelfSlope = Math.Clamp(map.Slope[triangleIndex], 0, 1000);
        var forcing = (waveExcess * profile.CoastalErosionStrength / 1000) +
            (shelfSlope / 18) + Math.Abs(profile.CoastalSeaLevelChange);
        var resistance = map.CrustTypeCodes[triangleIndex] ==
            PhysicalCrustTypeCodes.Continental ? 18 : 12;
        return Math.Clamp(
            forcing / resistance,
            0,
            profile.MaximumCoastalErosionPerPass);
    }

    private static void DepositOrExport(
        PhysicalGeographyField map,
        PhysicalGeographyGenerationProfile profile,
        int[] coastalWave,
        int source,
        int sourceWave,
        int sediment)
    {
        var target = FindShelteredTarget(map, coastalWave, source, sourceWave);
        var deposit = target < 0
            ? 0
            : sediment * profile.CoastalDepositionFraction / 1000;
        if (deposit > 0)
        {
            _ = LandscapeSurface.Deposit(map, target, deposit);
        }

        map.SedimentExportedTotal = checked(
            map.SedimentExportedTotal + sediment - deposit);
    }

    private static int FindShelteredTarget(
        PhysicalGeographyField map,
        int[] coastalWave,
        int source,
        int sourceWave)
    {
        var target = -1;
        var targetWave = sourceWave;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Grid.NeighborTriangleIndex(source, edge);
            if (neighbor < 0)
            {
                continue;
            }

            var wave = map.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean
                ? map.WaveExposure[neighbor]
                : coastalWave[neighbor];
            if (wave < targetWave)
            {
                target = neighbor;
                targetWave = wave;
            }
        }

        return target;
    }
}
