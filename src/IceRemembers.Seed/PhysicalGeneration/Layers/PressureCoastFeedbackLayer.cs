namespace IceRemembers.Seed;

using Engine.Constants;

internal sealed class PressureCoastFeedbackLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "pressure-coast-feedback",
        0,
        IceRemembersMicrogeographyFacts.PressureField |
        IceRemembersMicrogeographyFacts.MagicHistory,
        IceRemembersMicrogeographyFacts.PressureCoastalFeedback,
        IceRemembersMicrogeographyFacts.FinalSurfaces |
        IceRemembersMicrogeographyFacts.Validated);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        map.Physical.ExternalCoastalForcing.Clear();
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (map.Physical.WaterKindCodes[triangleIndex] != PhysicalWaterKindCodes.None)
            {
                continue;
            }

            MeasureAdjacentOcean(
                map,
                triangleIndex,
                out var pressure,
                out var wave,
                out var current);
            if (pressure < context.Profile.PressureCoastalThreshold)
            {
                continue;
            }

            var pressureRange = Math.Max(1, 1000 - context.Profile.PressureCoastalThreshold);
            var forcing = Math.Clamp(
                ((pressure - context.Profile.PressureCoastalThreshold) * 1000 / pressureRange) +
                (wave * context.Profile.PressureCoastalWaveAmplification / 1000) +
                (current * context.Profile.PressureCoastalCurrentAmplification / 1000),
                1,
                1000);
            map.Physical.ExternalCoastalForcing[triangleIndex] = forcing;
            map.PressureCoastalHistory[triangleIndex] = checked(
                map.PressureCoastalHistory[triangleIndex] + forcing);
            if (map.PressureCoastalFirstEpoch[triangleIndex] < 0)
            {
                map.PressureCoastalFirstEpoch[triangleIndex] = context.EpochIndex;
            }
        }
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        _ = context;
        var maximumHistory = checked(map.CompletedEpochCount * 1000);
        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            if (!TriangleIsValid(map, triangleIndex, maximumHistory))
            {
                throw new InvalidOperationException("Pressure coastal feedback is invalid.");
            }
        }
    }

    private static bool TriangleIsValid(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        int maximumHistory)
    {
        var forcing = map.Physical.ExternalCoastalForcing[triangleIndex];
        var history = map.PressureCoastalHistory[triangleIndex];
        var firstEpoch = map.PressureCoastalFirstEpoch[triangleIndex];
        return forcing is >= 0 and <= 1000 &&
            (forcing == 0 || map.Physical.WaterKindCodes[triangleIndex] == PhysicalWaterKindCodes.None &&
                HasOceanNeighbor(map, triangleIndex)) &&
            history >= 0 && history <= maximumHistory &&
            (history == 0) == (firstEpoch == -1) && firstEpoch < map.CompletedEpochCount;
    }

    private static void MeasureAdjacentOcean(
        IceRemembersMicrogeographyMap map,
        int triangleIndex,
        out int pressure,
        out int wave,
        out int current)
    {
        pressure = 0;
        wave = 0;
        current = 0;
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Physical.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor < 0 ||
                map.Physical.WaterKindCodes[neighbor] != PhysicalWaterKindCodes.Ocean)
            {
                continue;
            }

            pressure = Math.Max(pressure, map.PressureIntensity[neighbor]);
            wave = Math.Max(wave, map.Physical.WaveExposure[neighbor]);
            current = Math.Max(current, map.Physical.OceanCurrentStrength[neighbor]);
        }
    }

    private static bool HasOceanNeighbor(IceRemembersMicrogeographyMap map, int triangleIndex)
    {
        for (var edge = 0; edge < 3; edge++)
        {
            var neighbor = map.Physical.Grid.NeighborTriangleIndex(triangleIndex, edge);
            if (neighbor >= 0 &&
                map.Physical.WaterKindCodes[neighbor] == PhysicalWaterKindCodes.Ocean)
            {
                return true;
            }
        }

        return false;
    }
}
