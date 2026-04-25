namespace Engine.Eventing;

public enum KernelEventCursorStart
{
    EarliestAvailable = 0,
    Latest = 1,
}

public sealed class KernelEventCursor : IDisposable
{
    private readonly KernelEventLog? _owner;
    private readonly int _slotIndex;
    private int _disposed;

    internal KernelEventCursor(KernelEventLog? owner, int slotIndex, long nextSequence)
    {
        _owner = owner;
        _slotIndex = slotIndex;
        NextSequence = nextSequence;
    }

    public long NextSequence { get; private set; }

    public long DroppedEvents { get; private set; }

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _owner?.ReleaseCursor(_slotIndex, this);
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    internal void Advance()
    {
        NextSequence++;
    }

    internal void SkipTo(long sequence)
    {
        if (sequence <= NextSequence)
        {
            return;
        }

        DroppedEvents += sequence - NextSequence;
        NextSequence = sequence;
    }
}
