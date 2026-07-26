namespace Engine.Kernel;

internal sealed partial class WorldState
{
    internal long CultureFieldRevision(int cultureId) =>
        (uint)cultureId < (uint)_cultureFieldRevisions.Length
            ? ReadPublishedRevision(ref _cultureFieldRevisions[cultureId])
            : 0;

    internal long CultureWorkRevision(int cultureId) =>
        (uint)cultureId < (uint)_cultureWorkRevisions.Length
            ? ReadPublishedRevision(ref _cultureWorkRevisions[cultureId])
            : 0;

    private void MarkCultureFieldDirty(int cultureId)
    {
        if ((uint)cultureId == 0u || cultureId >= _cultureFieldRevisions.Length)
        {
            return;
        }

        if (_singleWorker)
        {
            if (_cultureFieldRevisions[cultureId] < long.MaxValue)
            {
                _cultureFieldRevisions[cultureId]++;
            }
        }
        else
        {
            IncrementSaturating(ref _cultureFieldRevisions[cultureId]);
        }

        MarkCultureWorkDirty(cultureId);
    }

    private void MarkCultureWorkDirty(int cultureId)
    {
        if ((uint)cultureId == 0u || cultureId >= _cultureWorkRevisions.Length)
        {
            return;
        }

        if (_singleWorker)
        {
            if (_cultureWorkRevisions[cultureId] < long.MaxValue)
            {
                _cultureWorkRevisions[cultureId]++;
            }

            return;
        }

        IncrementSaturating(ref _cultureWorkRevisions[cultureId]);
    }

    private void MarkEntityCultureWorkDirty(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _entityCultureId.Length)
        {
            return;
        }

        MarkCultureWorkDirty(_entityCultureId[entityId]);
    }

    private static void IncrementSaturating(ref long value)
    {
        var current = global::System.Threading.Volatile.Read(ref value);
        while (current < long.MaxValue)
        {
            var observed = global::System.Threading.Interlocked.CompareExchange(
                ref value,
                current + 1,
                current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    private long ReadPublishedRevision(ref long value) =>
        _singleWorker
            ? value
            : global::System.Threading.Volatile.Read(ref value);
}
