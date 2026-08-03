namespace Engine.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal sealed class KernelTelemetryRecorder : IDisposable
{
    private readonly KernelEventProjectionPump _pump;
    private readonly int _mapSnapshotCadence;
    private bool _disposed;

    public KernelTelemetryRecorder(
        IKernelEventReader eventLog,
        string runId,
        IProjectionSymbolCatalog? symbols = null,
        int? seed = null,
        string domain = "IceRemembers",
        int mapSnapshotCadence = 0,
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

        _mapSnapshotCadence = mapSnapshotCadence;
        EventProjection = new TelemetryEventProjection(RunId, Batch, symbols);
        _pump = new KernelEventProjectionPump(
            eventLog,
            cursorStart,
            EventProjection,
            batchSize);
    }

    public int RunId { get; }

    public TelemetryBatch Batch { get; private set; } = new();

    internal TelemetryEventProjection EventProjection { get; }

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

    public (int ActiveEntities, int CoveredCells) ProgressCounts()
    {
        DrainAvailable();
        return EventProjection.ProgressCounts();
    }

    public void RecordSnapshots(int tick, bool includeEntities = true)
    {
        DrainAvailable();
        EventProjection.RecordTickSnapshot(tick);
        EventProjection.RecordKindSnapshots(tick);
        if (ShouldRecordSnapshot(tick, _mapSnapshotCadence))
        {
            EventProjection.RecordMapSnapshots(tick);
        }

        if (includeEntities)
        {
            EventProjection.RecordEntitySnapshots(tick);
        }
    }

    public TelemetryBatch DetachBatch()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        DrainAvailable();
        var completed = Batch;
        Batch = new TelemetryBatch();
        EventProjection.ReplaceBatch(Batch);
        return completed;
    }

    private static bool ShouldRecordSnapshot(int tick, int cadence) =>
        cadence <= 0 || tick % cadence == 0;

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
