namespace Engine.Kernel;

using Engine.Eventing;

internal sealed class KernelEventPublisher : IKernelEventWriter
{
    private readonly KernelEventLog _log;
    private readonly BufferedKernelEvent[] _buffer;
    private int _count;
    private int _phaseCode;
    private bool _buffering;

    public KernelEventPublisher(KernelEventLog log, int capacity)
    {
        _log = log;
        _buffer = new BufferedKernelEvent[capacity];
    }

    public int BufferedCount => _count;

    public void BeginPhase(int phaseCode)
    {
        if (_buffering)
        {
            throw new global::System.InvalidOperationException();
        }

        _phaseCode = phaseCode;
        _count = 0;
        _buffering = true;
    }

    public void PublishPhase()
    {
        if (!_buffering)
        {
            throw new global::System.InvalidOperationException();
        }

        DeterministicSort.Sort(_buffer, _count);
        for (var i = 0; i < _count; i++)
        {
            if (!_log.TryWrite(_buffer[i].Event))
            {
                throw new global::System.InvalidOperationException();
            }
        }

        _count = 0;
        _buffering = false;
    }

    public bool TryWrite(in KernelEvent kernelEvent)
    {
        if (!_buffering)
        {
            return _log.TryWrite(kernelEvent);
        }

        if (_count >= _buffer.Length)
        {
            return false;
        }

        _buffer[_count++] = new BufferedKernelEvent(_phaseCode, kernelEvent);
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
