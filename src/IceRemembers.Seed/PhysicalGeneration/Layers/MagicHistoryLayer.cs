namespace IceRemembers.Seed;

internal sealed class MagicHistoryLayer : IIceRemembersMicrogeographyLayer
{
    public IceRemembersMicrogeographyLayerContract Contract { get; } = new(
        "magic-field-history",
        0,
        IceRemembersMicrogeographyFacts.FireField |
        IceRemembersMicrogeographyFacts.AuroraField |
        IceRemembersMicrogeographyFacts.PressureField,
        IceRemembersMicrogeographyFacts.MagicHistory);

    public void Apply(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        if (context.Epoch is null || context.EpochIndex != map.CompletedEpochCount)
        {
            throw new InvalidOperationException("Magic history was applied outside its configured epoch.");
        }

        for (var triangleIndex = 0; triangleIndex < map.Physical.Grid.TriangleCount; triangleIndex++)
        {
            map.FireHistory[triangleIndex] = checked(
                map.FireHistory[triangleIndex] + map.FireIntensity[triangleIndex]);
            map.AuroraHistory[triangleIndex] = checked(
                map.AuroraHistory[triangleIndex] + map.AuroraIntensity[triangleIndex]);
            map.PressureHistory[triangleIndex] = checked(
                map.PressureHistory[triangleIndex] + map.PressureIntensity[triangleIndex]);
            RecordFirstEpoch(map.FireFirstEpoch, triangleIndex, map.FireIntensity[triangleIndex], context.EpochIndex);
            RecordFirstEpoch(
                map.AuroraFirstEpoch,
                triangleIndex,
                map.AuroraIntensity[triangleIndex],
                context.EpochIndex);
            RecordFirstEpoch(
                map.PressureFirstEpoch,
                triangleIndex,
                map.PressureIntensity[triangleIndex],
                context.EpochIndex);
        }

        map.CompletedEpochCount++;
    }

    public void Validate(
        IceRemembersMicrogeographyMap map,
        IceRemembersMicrogeographyGenerationContext context)
    {
        var maximumHistory = checked(map.CompletedEpochCount * 1000);
        if (map.CompletedEpochCount != context.EpochIndex + 1 ||
            map.CompletedEpochCount > context.EpochCount ||
            !HistoryIsValid(map.FireHistory, maximumHistory) ||
            !HistoryIsValid(map.AuroraHistory, maximumHistory) ||
            !HistoryIsValid(map.PressureHistory, maximumHistory) ||
            !AgeIsValid(map.FireHistory, map.FireFirstEpoch, map.CompletedEpochCount) ||
            !AgeIsValid(map.AuroraHistory, map.AuroraFirstEpoch, map.CompletedEpochCount) ||
            !AgeIsValid(map.PressureHistory, map.PressureFirstEpoch, map.CompletedEpochCount))
        {
            throw new InvalidOperationException("Accumulated magic history is invalid.");
        }
    }

    private static bool HistoryIsValid(ReadOnlySpan<int> history, int maximum)
    {
        foreach (var value in history)
        {
            if (value < 0 || value > maximum)
            {
                return false;
            }
        }

        return true;
    }

    private static void RecordFirstEpoch(int[] ages, int index, int intensity, int epochIndex)
    {
        if (intensity > 0 && ages[index] < 0)
        {
            ages[index] = epochIndex;
        }
    }

    private static bool AgeIsValid(ReadOnlySpan<int> history, ReadOnlySpan<int> ages, int epochCount)
    {
        for (var index = 0; index < history.Length; index++)
        {
            if ((history[index] == 0) != (ages[index] == -1) || ages[index] >= epochCount)
            {
                return false;
            }
        }

        return true;
    }
}
