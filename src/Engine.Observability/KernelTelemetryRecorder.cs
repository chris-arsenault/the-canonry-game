namespace Engine.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal sealed class KernelTelemetryRecorder : IDisposable
{
    private readonly KernelEventProjectionPump _pump;
    private readonly int _mapSnapshotCadence;
    private readonly int _generatorSnapshotCadence;
    private bool _disposed;

    public KernelTelemetryRecorder(
        IKernelEventReader eventLog,
        string runId,
        IProjectionSymbolCatalog? symbols = null,
        int? seed = null,
        string domain = "IceRemembers",
        bool traceEnabled = false,
        int entitySnapshotCadence = 0,
        int mapSnapshotCadence = 0,
        int generatorSnapshotCadence = 0,
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
        Batch.ExperimentVariants.Add(new ExperimentVariantRow(
            RunId,
            ExperimentId: 1,
            VariantId: 1,
            ConfigHashId: domainId,
            GitShaId: 0,
            traceEnabled,
            entitySnapshotCadence,
            mapSnapshotCadence,
            generatorSnapshotCadence));

        _mapSnapshotCadence = mapSnapshotCadence;
        _generatorSnapshotCadence = generatorSnapshotCadence;
        EventProjection = new TelemetryEventProjection(RunId, Batch, symbols);
        _pump = new KernelEventProjectionPump(
            eventLog,
            cursorStart,
            EventProjection,
            batchSize);
    }

    public int RunId { get; }

    public TelemetryBatch Batch { get; } = new();

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

    public void RecordSnapshots(int tick, bool includeEntities = true)
    {
        DrainAvailable();
        EventProjection.RecordTickSnapshot(tick);
        EventProjection.RecordKindSnapshots(tick);
        EventProjection.RecordRelationshipSnapshots(tick);
        var includeGeneratorSnapshot = ShouldRecordSnapshot(tick, _generatorSnapshotCadence);
        if (ShouldRecordSnapshot(tick, _mapSnapshotCadence))
        {
            EventProjection.RecordMapSnapshots(tick, includeGeneratorSnapshot);
        }
        else if (includeGeneratorSnapshot)
        {
            EventProjection.RecordGeneratorStateSnapshots(tick);
        }

        if (includeEntities)
        {
            EventProjection.RecordEntitySnapshots(tick);
        }
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
