namespace Engine.Kernel.Driver;

using Engine.Eventing;

public sealed class KernelCommandCompletionPump : IDisposable
{
    private readonly KernelEventLog _events;
    private readonly KernelCommandCompletionTable _completionTable;
    private readonly KernelEventCursor _cursor;
    private readonly SequencedKernelEvent[] _buffer;
    private bool _disposed;

    public KernelCommandCompletionPump(
        KernelEventLog events,
        KernelCommandCompletionTable completionTable,
        KernelEventCursorStart cursorStart = KernelEventCursorStart.EarliestAvailable,
        int batchSize = 256)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(completionTable);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        _events = events;
        _completionTable = completionTable;
        _cursor = events.CreateCursor(cursorStart);
        _buffer = new SequencedKernelEvent[batchSize];
    }

    public long DroppedEvents => _cursor.DroppedEvents;

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            var result = _events.ReadWithResult(_cursor, _buffer);
            if (result.Count == 0)
            {
                return;
            }

            for (var i = 0; i < result.Count; i++)
            {
                Project(_buffer[i].Event);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cursor.Dispose();
    }

    private void Project(in KernelEvent kernelEvent)
    {
        switch (kernelEvent.EventType)
        {
            case KernelEventType.CommandApplied:
                var applied = kernelEvent.AsCommandApplied();
                _completionTable.Complete(new KernelCommandResult(
                    applied.CommandId,
                    applied.CommandType,
                    Applied: true,
                    ReasonCodeId: KernelCommandRejectReasonCodes.None));
                break;
            case KernelEventType.CommandRejected:
                var rejected = kernelEvent.AsCommandRejected();
                _completionTable.Complete(new KernelCommandResult(
                    rejected.CommandId,
                    rejected.CommandType,
                    Applied: false,
                    rejected.ReasonCodeId));
                break;
        }
    }
}
