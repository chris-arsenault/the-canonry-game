namespace Engine.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal sealed class KernelTelemetryRecorder : IDisposable
{
    private readonly KernelEventProjectionPump _pump;
    private bool _disposed;

    public KernelTelemetryRecorder(
        IKernelEventReader eventLog,
        string runId,
        IProjectionSymbolCatalog? symbols = null,
        int? seed = null,
        string domain = "IceRemembers",
        KernelEventCursorStart cursorStart = KernelEventCursorStart.EarliestAvailable,
        int batchSize = 512)
    {
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        RunId = TelemetrySymbolIds.RunId(runId, seed);
        var domainId = TelemetrySymbolIds.StableId(TelemetrySymbolKinds.Domain, domain);
        Batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Run, RunId, runId, runId));
        Batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Domain, domainId, domain, domain));
        TelemetrySymbolIds.AddProjectionSymbols(Batch, symbols);
        TelemetrySymbolIds.AddFixedCodeSymbols(Batch);
        Batch.Runs.Add(new RunRow(
            RunId,
            seed,
            domainId,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

        EventProjection = new TelemetryEventProjection(RunId, Batch, symbols);
        PlannerTraceProjection = new PlannerTraceProjection(RunId, Batch);
        _pump = new KernelEventProjectionPump(
            eventLog,
            cursorStart,
            [EventProjection, PlannerTraceProjection],
            batchSize);
    }

    public int RunId { get; }

    public TelemetryBatch Batch { get; } = new();

    internal TelemetryEventProjection EventProjection { get; }

    internal PlannerTraceProjection PlannerTraceProjection { get; }

    public long NextSequence => _pump.NextSequence;

    public long DroppedEvents => _pump.DroppedEvents;

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pump.DrainAvailable();
    }

    public void RecordTickSnapshot(int tick)
    {
        DrainAvailable();
        EventProjection.RecordTickSnapshot(tick);
    }

    public void RecordSnapshots(int tick, bool includeEntities = true)
    {
        DrainAvailable();
        EventProjection.RecordTickSnapshot(tick);
        EventProjection.RecordKindSnapshots(tick);
        EventProjection.RecordRelationshipSnapshots(tick);
        EventProjection.RecordMapSnapshots(tick);
        if (includeEntities)
        {
            EventProjection.RecordEntitySnapshots(tick);
        }
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
}
