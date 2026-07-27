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

internal interface IContinuousCellProgram : ISimulationSystem
{
    void EvaluateOwner(
        Simulation simulation,
        int tick,
        in FrozenCultureInfluenceRange range);
}

internal readonly struct FrozenTickRange
{
    private readonly int[] _ids;
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

        return _ids[
            _reverse
                ? _start + Count - index - 1
                : _start + index];
    }
}

internal readonly struct FrozenCultureInfluenceRange
{
    private readonly int[] _cultureIds;
    private readonly int[] _planeCodes;
    private readonly int[] _q;
    private readonly int[] _r;
    private readonly int[] _z;
    private readonly int _start;

    internal FrozenCultureInfluenceRange(
        int[] cultureIds,
        int[] planeCodes,
        int[] q,
        int[] r,
        int[] z,
        int start,
        int count)
    {
        global::System.ArgumentNullException.ThrowIfNull(cultureIds);
        global::System.ArgumentNullException.ThrowIfNull(planeCodes);
        global::System.ArgumentNullException.ThrowIfNull(q);
        global::System.ArgumentNullException.ThrowIfNull(r);
        global::System.ArgumentNullException.ThrowIfNull(z);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(start);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (start > cultureIds.Length - count ||
            planeCodes.Length != cultureIds.Length ||
            q.Length != cultureIds.Length ||
            r.Length != cultureIds.Length ||
            z.Length != cultureIds.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(count));
        }

        _cultureIds = cultureIds;
        _planeCodes = planeCodes;
        _q = q;
        _r = r;
        _z = z;
        _start = start;
        Count = count;
    }

    internal int Count { get; }

    internal void At(
        int index,
        out int cultureId,
        out int planeCode,
        out int q,
        out int r,
        out int z)
    {
        if ((uint)index >= (uint)Count)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        var slot = _start + index;
        cultureId = _cultureIds[slot];
        planeCode = _planeCodes[slot];
        q = _q[slot];
        r = _r[slot];
        z = _z[slot];
    }
}
