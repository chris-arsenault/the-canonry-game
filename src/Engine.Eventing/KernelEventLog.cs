namespace Engine.Eventing;

public interface IKernelEventWriter
{
    bool TryWrite(in KernelEvent kernelEvent);
}

public enum KernelEventOverflowPolicy
{
    DropOldest = 0,
    RejectWrite = 1,
}

public sealed class KernelEventLog : IKernelEventWriter
{
    private readonly KernelEventSlot[] _slots;
    private readonly KernelEventCursor?[] _cursors;
    private readonly object _cursorGate = new();
    private readonly KernelEventOverflowPolicy _overflowPolicy;
    private long _reservedNextSequence;
    private long _publishedNextSequence;
    private long _overwrittenSlotCount;
    private long _rejectedWriteCount;

    public KernelEventLog(
        int capacity,
        KernelEventOverflowPolicy overflowPolicy = KernelEventOverflowPolicy.DropOldest,
        int maxCursors = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCursors);

        _overflowPolicy = overflowPolicy;
        _slots = new KernelEventSlot[capacity];
        for (var i = 0; i < _slots.Length; i++)
        {
            _slots[i] = new KernelEventSlot();
        }

        _cursors = new KernelEventCursor[maxCursors];
    }

    public int Capacity => _slots.Length;

    public KernelEventOverflowPolicy OverflowPolicy => _overflowPolicy;

    public long NextSequence => Volatile.Read(ref _publishedNextSequence);

    public long OverwrittenSlotCount => Volatile.Read(ref _overwrittenSlotCount);

    public long RejectedWriteCount => Volatile.Read(ref _rejectedWriteCount);

    public long OldestAvailableSequence
    {
        get
        {
            var reservedNext = Volatile.Read(ref _reservedNextSequence);
            return Math.Max(0L, reservedNext - Capacity);
        }
    }

    public KernelEventCursor CreateCursor(KernelEventCursorStart start)
    {
        var next = NextSequence;
        var sequence = start switch
        {
            KernelEventCursorStart.EarliestAvailable => OldestAvailableSequence,
            KernelEventCursorStart.Latest => next,
            _ => next,
        };

        lock (_cursorGate)
        {
            for (var i = 0; i < _cursors.Length; i++)
            {
                if (_cursors[i] is not null)
                {
                    continue;
                }

                var cursor = new KernelEventCursor(this, i, sequence);
                _cursors[i] = cursor;
                return cursor;
            }
        }

        throw new InvalidOperationException("No kernel event cursor slots are available.");
    }

    public bool TryWrite(in KernelEvent kernelEvent)
    {
        var sequence = _reservedNextSequence;
        if (_overflowPolicy == KernelEventOverflowPolicy.RejectWrite && WouldOverwriteUnreadCursor(sequence))
        {
            Volatile.Write(ref _rejectedWriteCount, _rejectedWriteCount + 1);
            return false;
        }

        Volatile.Write(ref _reservedNextSequence, sequence + 1);
        if (sequence >= Capacity)
        {
            Volatile.Write(ref _overwrittenSlotCount, _overwrittenSlotCount + 1);
        }

        var slot = _slots[SlotIndex(sequence)];
        slot.Write(sequence, in kernelEvent);

        Volatile.Write(ref _publishedNextSequence, sequence + 1);
        return true;
    }

    public bool TryRead(KernelEventCursor cursor, out SequencedKernelEvent kernelEvent)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        cursor.ThrowIfDisposed();

        kernelEvent = default;
        SkipDroppedEvents(cursor);

        var sequence = cursor.NextSequence;
        if (sequence >= NextSequence)
        {
            return false;
        }

        var slot = _slots[SlotIndex(sequence)];
        if (!slot.TryRead(sequence, out var eventValue))
        {
            SkipDroppedEvents(cursor);
            return false;
        }

        cursor.Advance();
        kernelEvent = new SequencedKernelEvent(sequence, eventValue);
        return true;
    }

    public int Read(KernelEventCursor cursor, Span<SequencedKernelEvent> destination)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        cursor.ThrowIfDisposed();

        var count = 0;
        while (count < destination.Length && TryRead(cursor, out var kernelEvent))
        {
            destination[count] = kernelEvent;
            count++;
        }

        return count;
    }

    private int SlotIndex(long sequence) => (int)(sequence % Capacity);

    private bool WouldOverwriteUnreadCursor(long nextWriteSequence)
    {
        var oldestSequenceAfterWrite = nextWriteSequence - Capacity + 1;
        if (oldestSequenceAfterWrite <= 0)
        {
            return false;
        }

        for (var i = 0; i < _cursors.Length; i++)
        {
            var cursor = Volatile.Read(ref _cursors[i]);
            if (cursor is null || cursor.IsDisposed)
            {
                continue;
            }

            if (cursor.NextSequence < oldestSequenceAfterWrite)
            {
                return true;
            }
        }

        return false;
    }

    private void SkipDroppedEvents(KernelEventCursor cursor)
    {
        cursor.SkipTo(OldestAvailableSequence);
    }

    internal void ReleaseCursor(int slotIndex, KernelEventCursor cursor)
    {
        if ((uint)slotIndex >= (uint)_cursors.Length)
        {
            return;
        }

        if (ReferenceEquals(Volatile.Read(ref _cursors[slotIndex]), cursor))
        {
            Volatile.Write(ref _cursors[slotIndex], null);
        }
    }

    private sealed class KernelEventSlot
    {
        private const long UnpublishedSequence = -1L;

        private long _sequence = UnpublishedSequence;
        private KernelEvent _kernelEvent;

        public void Write(long sequence, in KernelEvent kernelEvent)
        {
            Volatile.Write(ref _sequence, UnpublishedSequence);
            _kernelEvent = kernelEvent;
            Volatile.Write(ref _sequence, sequence);
        }

        public bool TryRead(long sequence, out KernelEvent kernelEvent)
        {
            var before = Volatile.Read(ref _sequence);
            if (before != sequence)
            {
                kernelEvent = default;
                return false;
            }

            kernelEvent = _kernelEvent;
            var after = Volatile.Read(ref _sequence);
            if (after == sequence)
            {
                return true;
            }

            kernelEvent = default;
            return false;
        }
    }
}
