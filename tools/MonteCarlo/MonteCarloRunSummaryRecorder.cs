namespace MonteCarloTool;

using Engine.Eventing;
using Engine.Projection;

internal sealed class MonteCarloRunSummaryRecorder : IDisposable
{
    private readonly MonteCarloRunSummaryProjection _projection = new();
    private readonly KernelEventProjectionPump _pump;
    private readonly int? _mapAttempt;
    private bool _disposed;

    public MonteCarloRunSummaryRecorder(MonteCarloKernelRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        _mapAttempt = run.WorldSeed?.Attempt;
        _pump = new KernelEventProjectionPump(
            run.Events,
            KernelEventCursorStart.Latest,
            _projection);
    }

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pump.DrainAvailable();
    }

    public MonteCarloRunSummary Snapshot()
    {
        DrainAvailable();
        return new MonteCarloRunSummary(
            _projection.GeneratorFireCount,
            _mapAttempt,
            _projection.DroppedEvents);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pump.Dispose();
    }

    private sealed class MonteCarloRunSummaryProjection : KernelEventProjector
    {
        public int GeneratorFireCount { get; private set; }

        public long DroppedEvents { get; private set; }

        public override void OnEventsDropped(long droppedEventCount, long nextSequence)
        {
            _ = nextSequence;
            DroppedEvents += droppedEventCount;
        }

        protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
        {
            _ = sequence;
            _ = evt;
            GeneratorFireCount++;
        }
    }
}
