namespace Engine.Observability;

using System.Globalization;

using Engine.Eventing;
using Engine.Projection;

public sealed class KernelTelemetryRecorder : IDisposable
{
    private readonly KernelEventProjectionPump _pump;
    private bool _disposed;

    public KernelTelemetryRecorder(
        KernelEventLog eventLog,
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

        RunId = runId;
        Batch.Runs.Add(new RunRow(
            runId,
            seed,
            domain,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));

        EventProjection = new TelemetryEventProjection(runId, Batch, symbols);
        PlannerTraceProjection = new PlannerTraceProjection(runId, Batch, symbols);
        _pump = new KernelEventProjectionPump(
            eventLog,
            cursorStart,
            [EventProjection, PlannerTraceProjection],
            batchSize);
    }

    public string RunId { get; }

    public TelemetryBatch Batch { get; } = new();

    public TelemetryEventProjection EventProjection { get; }

    public PlannerTraceProjection PlannerTraceProjection { get; }

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
