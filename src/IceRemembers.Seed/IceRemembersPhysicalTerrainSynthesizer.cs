namespace IceRemembers.Seed;

using System.Numerics;

using Engine.Constants;
using Engine.WorldGen;

using IceRemembers;

internal static class IceRemembersPhysicalTerrainSynthesizer
{
    private const int Ineligible = int.MinValue;

    public static IceRemembersTerrain Synthesize(
        PhysicalGeographyField field,
        int hexIndex,
        ReadOnlySpan<int> materialIndexes,
        PhysicalHexAggregate aggregate)
    {
        var facts = SynthesisFacts.Create(field, hexIndex, materialIndexes, aggregate);
        Span<TerrainCandidate> candidates = stackalloc TerrainCandidate[8];
        candidates[0] = new(IceRemembersTerrain.Tundra, ScoreTundra(facts));
        candidates[1] = new(IceRemembersTerrain.Wastes, ScoreWastes(facts));
        candidates[2] = new(IceRemembersTerrain.Ice, ScoreIce(facts));
        candidates[3] = new(IceRemembersTerrain.Crags, ScoreCrags(facts));
        candidates[4] = new(IceRemembersTerrain.HotSprings, ScoreHotSprings(facts));
        candidates[5] = new(IceRemembersTerrain.Shelf, ScoreShelf(facts));
        candidates[6] = new(IceRemembersTerrain.Reef, ScoreReef(facts));
        candidates[7] = new(IceRemembersTerrain.Ocean, ScoreOcean(facts));
        return BestCandidate(candidates);
    }

    private static int ScoreTundra(SynthesisFacts facts) =>
        (facts.Aggregate.LandCoverage * 35) +
        (facts.TundraSoil * 74) +
        (facts.Aggregate.MeanMoisture / 2) -
        (facts.Aggregate.PermanentIceCoverage * 20);

    private static int ScoreWastes(SynthesisFacts facts) =>
        facts.Aggregate.LandCoverage < 8
            ? Ineligible
            : (facts.Aggregate.LandCoverage * 30) +
                (facts.Permafrost * 92) +
                (facts.Scree * 18) +
                Math.Max(0, 520 - facts.Aggregate.MeanMoisture);

    private static int ScoreIce(SynthesisFacts facts) =>
        facts.Aggregate.PermanentIceCoverage < 5 && facts.GlacialIce + facts.Snowpack < 8
            ? Ineligible
            : (facts.Aggregate.PermanentIceCoverage * 125) +
                (facts.GlacialIce * 85) +
                (facts.Snowpack * 40) +
                Math.Max(0, 360 - facts.Aggregate.MeanTemperature) * 2;

    private static int ScoreCrags(SynthesisFacts facts) =>
        facts.Aggregate.LandCoverage < 8 ||
            (facts.Aggregate.PeakCount == 0 &&
                facts.Aggregate.Relief < 240 &&
                facts.Aggregate.Roughness < 130 &&
                facts.Aggregate.RidgeEdgeMask == 0 &&
                facts.Scree < 2)
            ? Ineligible
            : 800 +
                (facts.ExposedBedrock * 140) +
                (facts.Scree * 80) +
                (facts.Aggregate.PeakCount * 400) +
                (BitOperations.PopCount((uint)facts.Aggregate.RidgeEdgeMask) * 180) +
                (facts.Aggregate.Relief * 4) +
                (facts.Aggregate.Roughness * 2);

    private static int ScoreHotSprings(SynthesisFacts facts) =>
        facts.Aggregate.LandCoverage < 8 ||
            (facts.VentCount < 2 && facts.GeothermalCrust < 3 && facts.Aggregate.MaxFireIntensity < 500)
            ? Ineligible
            : (facts.VentCount * 310) +
                (facts.GeothermalCrust * 95) +
                facts.Aggregate.MeanGeothermal +
                facts.Aggregate.MeanFireIntensity;

    private static int ScoreShelf(SynthesisFacts facts) =>
        facts.Aggregate.WaterCoverage < 7 || facts.Aggregate.OceanCoverage >= 20
            ? Ineligible
            : (facts.Aggregate.WaterCoverage * 72) +
                (facts.ShelfSediment * 44) +
                (BitOperations.PopCount((uint)facts.Aggregate.CoastEdgeMask) * 95);

    private static int ScoreReef(SynthesisFacts facts) =>
        facts.Aggregate.OceanCoverage < 6 || facts.ReefStone < 2
            ? Ineligible
            : (facts.ReefStone * 145) +
                (facts.Aggregate.OceanCoverage * 54) +
                (facts.Aggregate.MeanTemperature / 2) +
                (facts.Aggregate.MeanGeothermal / 3);

    private static int ScoreOcean(SynthesisFacts facts) =>
        facts.Aggregate.OceanCoverage < 12
            ? Ineligible
            : (facts.Aggregate.OceanCoverage * 115) +
                (facts.DeepMarineSediment * 55) +
                (facts.ShelfSediment * 20);

    private static IceRemembersTerrain BestCandidate(ReadOnlySpan<TerrainCandidate> candidates)
    {
        var best = candidates[0];
        for (var candidateIndex = 1; candidateIndex < candidates.Length; candidateIndex++)
        {
            if (candidates[candidateIndex].Score > best.Score)
            {
                best = candidates[candidateIndex];
            }
        }

        return best.Terrain;
    }

    private readonly record struct TerrainCandidate(IceRemembersTerrain Terrain, int Score);

    private readonly record struct SynthesisFacts(
        PhysicalHexAggregate Aggregate,
        int ExposedBedrock,
        int Scree,
        int Permafrost,
        int TundraSoil,
        int Snowpack,
        int GlacialIce,
        int ShelfSediment,
        int DeepMarineSediment,
        int ReefStone,
        int GeothermalCrust,
        int VentCount)
    {
        public static SynthesisFacts Create(
            PhysicalGeographyField field,
            int hexIndex,
            ReadOnlySpan<int> materialIndexes,
            PhysicalHexAggregate aggregate)
        {
            Span<int> counts = stackalloc int[12];
            var ventCount = 0;
            for (var localTriangle = 0;
                localTriangle < PhysicalGeographyCodes.MicrotrianglesPerHex;
                localTriangle++)
            {
                var triangleIndex = field.Grid.TriangleIndex(hexIndex, localTriangle);
                counts[materialIndexes[triangleIndex]]++;
                if ((field.FeatureFlags[triangleIndex] &
                    PhysicalGeographyFeatureCodes.GeothermalVent) != 0)
                {
                    ventCount++;
                }
            }

            return new SynthesisFacts(
                aggregate,
                counts[(int)IceRemembersPhysicalMaterial.ExposedBedrock],
                counts[(int)IceRemembersPhysicalMaterial.Scree],
                counts[(int)IceRemembersPhysicalMaterial.Permafrost],
                counts[(int)IceRemembersPhysicalMaterial.TundraSoil],
                counts[(int)IceRemembersPhysicalMaterial.Snowpack],
                counts[(int)IceRemembersPhysicalMaterial.GlacialIce],
                counts[(int)IceRemembersPhysicalMaterial.ShelfSediment],
                counts[(int)IceRemembersPhysicalMaterial.DeepMarineSediment],
                counts[(int)IceRemembersPhysicalMaterial.ReefStone],
                counts[(int)IceRemembersPhysicalMaterial.GeothermalCrust],
                ventCount);
        }
    }
}
