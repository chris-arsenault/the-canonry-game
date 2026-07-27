namespace Engine.Kernel;

using Engine.Eventing;

/// <summary>
/// Publishes one append-only event segment per logical owner. The hot path
/// never takes a global event-log lock or sorts events across owners.
/// </summary>
internal sealed class KernelEventPublisher : IKernelEventWriter
{
    private readonly KernelEventLog _log;
    private readonly OwnerInboxArena<KernelEvent> _ownerSegments;
    private readonly bool _singleWorker;
    private bool _continuous;

    public KernelEventPublisher(
        KernelEventLog log,
        int capacity,
        int workerCount,
        int ownerCount)
    {
        _log = log;
        _ownerSegments = new OwnerInboxArena<KernelEvent>(
            capacity,
            workerCount,
            ownerCount);
        _singleWorker = workerCount == 1;
    }

    internal void BeginContinuous()
    {
        if (_continuous)
        {
            throw new global::System.InvalidOperationException();
        }

        _ownerSegments.Clear();
        global::System.Threading.Volatile.Write(ref _continuous, true);
    }

    internal void EndContinuous()
    {
        if (!_continuous)
        {
            throw new global::System.InvalidOperationException();
        }

        var count = _ownerSegments.Seal();
        var values = _ownerSegments.Values;
        for (var index = 0; index < count; index++)
        {
            if (!_log.TryWrite(values[index]))
            {
                throw new global::System.InvalidOperationException();
            }
        }

        _ownerSegments.Clear();
        global::System.Threading.Volatile.Write(ref _continuous, false);
    }

    public bool TryWrite(in KernelEvent kernelEvent)
    {
        if (!global::System.Threading.Volatile.Read(ref _continuous))
        {
            return _log.TryWrite(kernelEvent);
        }

        var ownerIndex = KernelWorkerThread.OwnerIndex;
        if (ownerIndex < 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _ownerSegments.Add(
            _singleWorker ? 0 : KernelWorkerThread.WorkerIndex,
            ownerIndex,
            kernelEvent);
        return true;
    }
}
