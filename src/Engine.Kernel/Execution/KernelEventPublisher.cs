namespace Engine.Kernel;

using Engine.Eventing;

internal sealed class KernelEventPublisher : IKernelEventWriter
{
    private readonly KernelEventLog _log;
    private readonly DeltaAppendArena<BufferedKernelEvent> _arena;
    private readonly BufferedKernelEvent[] _buffer;
    private readonly bool _singleWorker;
    private int _phaseCode;
    private bool _buffering;

    public KernelEventPublisher(KernelEventLog log, int capacity, int workerCount)
    {
        _log = log;
        _arena = new DeltaAppendArena<BufferedKernelEvent>(capacity, workerCount);
        _buffer = _arena.Values;
        _singleWorker = workerCount == 1;
    }

    public int BufferedCount => _arena.Count;

    public void BeginPhase(int phaseCode)
    {
        if (_buffering)
        {
            throw new global::System.InvalidOperationException();
        }

        _phaseCode = phaseCode;
        _arena.Clear();
        _buffering = true;
    }

    public void PublishPhase()
    {
        if (!_buffering)
        {
            throw new global::System.InvalidOperationException();
        }

        var count = _arena.Compact();
        DeterministicSort.Sort(_buffer, count);
        for (var i = 0; i < count; i++)
        {
            if (!_log.TryWrite(_buffer[i].Event))
            {
                throw new global::System.InvalidOperationException();
            }
        }

        _arena.Clear();
        _buffering = false;
    }

    public bool TryWrite(in KernelEvent kernelEvent)
    {
        if (!_buffering)
        {
            return _log.TryWrite(kernelEvent);
        }

        var workerIndex = _singleWorker ? 0 : KernelWorkerThread.WorkerIndex;
        _arena.Add(workerIndex, new BufferedKernelEvent(_phaseCode, kernelEvent));
        return true;
    }

    private readonly record struct BufferedKernelEvent(int PhaseCode, KernelEvent Event)
        : IDeterministicOrder<BufferedKernelEvent>
    {
        public int CompareTo(BufferedKernelEvent other)
        {
            var comparison = CompareHeader(other);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareIntSlots(other);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareLongSlots(other);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Event.Tick.CompareTo(other.Event.Tick);
            return comparison != 0
                ? comparison
                : Event.WorldVersion.CompareTo(other.Event.WorldVersion);
        }

        private int CompareHeader(BufferedKernelEvent other)
        {
            var comparison = PhaseCode.CompareTo(other.PhaseCode);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Event.EventType.CompareTo(other.Event.EventType);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Event.ProgramId.CompareTo(other.Event.ProgramId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Event.SourceCodeId.CompareTo(other.Event.SourceCodeId);
            return comparison != 0
                ? comparison
                : Event.ProvenanceInvocationId.CompareTo(other.Event.ProvenanceInvocationId);
        }

        private int CompareIntSlots(BufferedKernelEvent other)
        {
            for (var slot = 0; slot < KernelEvent.IntSlotCount; slot++)
            {
                var comparison = Event.GetIntSlot(slot).CompareTo(other.Event.GetIntSlot(slot));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private int CompareLongSlots(BufferedKernelEvent other)
        {
            for (var slot = 0; slot < KernelEvent.LongSlotCount; slot++)
            {
                var comparison = Event.GetLongSlot(slot).CompareTo(other.Event.GetLongSlot(slot));
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            return 0;
        }
    }
}
