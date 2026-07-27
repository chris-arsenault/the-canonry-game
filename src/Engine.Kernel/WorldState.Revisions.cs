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

        if (_singleWorker || KernelWorkerThread.Current is null)
        {
            if (_cultureFieldRevisions[cultureId] < long.MaxValue)
            {
                _cultureFieldRevisions[cultureId]++;
            }
        }
        else
        {
            var index = RevisionDeltaIndex(cultureId);
            _cultureFieldRevisionDeltas[index] = checked(
                _cultureFieldRevisionDeltas[index] + 1);
        }

        MarkCultureWorkDirty(cultureId);
    }

    private void MarkCultureWorkDirty(int cultureId)
    {
        if ((uint)cultureId == 0u || cultureId >= _cultureWorkRevisions.Length)
        {
            return;
        }

        if (_singleWorker || KernelWorkerThread.Current is null)
        {
            if (_cultureWorkRevisions[cultureId] < long.MaxValue)
            {
                _cultureWorkRevisions[cultureId]++;
            }

            return;
        }

        var index = RevisionDeltaIndex(cultureId);
        _cultureWorkRevisionDeltas[index] = checked(
            _cultureWorkRevisionDeltas[index] + 1);
    }

    private void MarkEntityCultureWorkDirty(int entityId)
    {
        if ((uint)entityId == 0u || entityId >= _entityCultureId.Length)
        {
            return;
        }

        MarkCultureWorkDirty(_entityCultureId[entityId]);
    }

    internal void PublishOwnedRevisionChanges()
    {
        if (_singleWorker)
        {
            return;
        }

        for (var cultureId = 1;
             cultureId < _cultureFieldRevisions.Length;
             cultureId++)
        {
            long fieldDelta = 0;
            long workDelta = 0;
            for (var workerIndex = 0;
                 workerIndex < _ownedPhysicalCultureMassDeltas.Length;
                 workerIndex++)
            {
                var index = checked(
                    workerIndex * _cultureRevisionStride + cultureId);
                fieldDelta = checked(
                    fieldDelta + _cultureFieldRevisionDeltas[index]);
                workDelta = checked(
                    workDelta + _cultureWorkRevisionDeltas[index]);
                _cultureFieldRevisionDeltas[index] = 0;
                _cultureWorkRevisionDeltas[index] = 0;
            }

            _cultureFieldRevisions[cultureId] = SaturatingRevisionAdd(
                _cultureFieldRevisions[cultureId],
                fieldDelta);
            _cultureWorkRevisions[cultureId] = SaturatingRevisionAdd(
                _cultureWorkRevisions[cultureId],
                workDelta);
        }

        long physicalCultureMassDelta = 0;
        for (var workerIndex = 0;
             workerIndex < _ownedPhysicalCultureMassDeltas.Length;
             workerIndex++)
        {
            physicalCultureMassDelta = checked(
                physicalCultureMassDelta +
                _ownedPhysicalCultureMassDeltas[workerIndex]);
            _ownedPhysicalCultureMassDeltas[workerIndex] = 0;
        }

        if (physicalCultureMassDelta != 0)
        {
            _totalPhysicalCultureRawMass = checked(
                _totalPhysicalCultureRawMass +
                physicalCultureMassDelta);
        }
    }

    private int RevisionDeltaIndex(int cultureId)
    {
        var worker = KernelWorkerThread.Current ??
            throw new global::System.InvalidOperationException();
        return checked(
            worker.WorkerIndex * _cultureRevisionStride + cultureId);
    }

    private static long SaturatingRevisionAdd(long current, long delta) =>
        delta >= long.MaxValue - current
            ? long.MaxValue
            : current + delta;

    private long ReadPublishedRevision(ref long value) =>
        _singleWorker
            ? value
            : global::System.Threading.Volatile.Read(ref value);
}
