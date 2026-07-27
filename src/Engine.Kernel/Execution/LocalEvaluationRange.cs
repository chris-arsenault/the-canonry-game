namespace Engine.Kernel;

internal static class LocalEvaluationRangeKindCodes
{
    internal const int Entities = 1;
    internal const int Relationships = 2;
}

internal interface ILocalEvaluationRangeProgram : ISimulationSystem
{
    int RangeKindCode { get; }

    bool SupportsConcurrentRanges(Simulation simulation);

    void EvaluateRange(
        Simulation simulation,
        int tick,
        in FrozenTickRange range);

    void CompleteRanges(Simulation simulation, int tick);
}

/// <summary>
/// Marks a strategic program whose evaluation produces bounded intents.
/// The scheduler freezes the published world for the program, collects its
/// outputs, and resolves them through destination owners before the next
/// strategic program observes the world.
/// </summary>
internal interface IStrategicIntentProgram : ISimulationSystem
{
}

internal readonly struct FrozenTickRange
{
    private readonly int[] _ids;
    private readonly int _totalCount;
    private readonly int _start;
    private readonly bool _reverse;

    internal FrozenTickRange(
        int[] ids,
        int totalCount,
        int start,
        int count,
        bool reverse)
    {
        global::System.ArgumentNullException.ThrowIfNull(ids);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(start);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (totalCount > ids.Length || start > totalCount - count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(count));
        }

        _ids = ids;
        _totalCount = totalCount;
        _start = start;
        _reverse = reverse;
        Count = count;
    }

    internal int Count { get; }

    internal int At(int index)
    {
        if ((uint)index >= (uint)Count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        var logicalIndex = _start + index;
        return _ids[_reverse ? _totalCount - logicalIndex - 1 : logicalIndex];
    }
}
