namespace Engine.Observability;

using System.Diagnostics;
using System.Globalization;

using Engine.Eventing;
using Engine.Projection;

public sealed class KernelPerformanceRecorder : IDisposable
{
    private readonly KernelPerformanceProjection _projection;
    private readonly KernelEventProjectionPump _pump;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<double> _sampledTickMilliseconds = [];
    private readonly int _requestedTicks;
    private readonly int _tickSampleCadence;
    private readonly int _systemSampleCadence;
    private readonly int _gen0Start;
    private readonly int _gen1Start;
    private readonly int _gen2Start;
    private readonly bool _fullTelemetryEnabled;
    private long _peakManagedMemoryBytes;
    private bool _completed;
    private bool _disposed;

    public KernelPerformanceRecorder(
        KernelEventLog eventLog,
        string runId,
        int requestedTicks,
        int? seed = null,
        string domain = "IceRemembers",
        int tickSampleCadence = 1,
        int systemSampleCadence = 1,
        bool fullTelemetryEnabled = false,
        KernelEventCursorStart cursorStart = KernelEventCursorStart.EarliestAvailable,
        int batchSize = 512)
    {
        ArgumentNullException.ThrowIfNull(eventLog);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentOutOfRangeException.ThrowIfNegative(requestedTicks);
        ArgumentOutOfRangeException.ThrowIfNegative(tickSampleCadence);
        ArgumentOutOfRangeException.ThrowIfNegative(systemSampleCadence);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        RunId = runId;
        Seed = seed;
        Domain = domain;
        StartedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        _requestedTicks = requestedTicks;
        _tickSampleCadence = tickSampleCadence;
        _systemSampleCadence = systemSampleCadence;
        _fullTelemetryEnabled = fullTelemetryEnabled;
        _gen0Start = GC.CollectionCount(0);
        _gen1Start = GC.CollectionCount(1);
        _gen2Start = GC.CollectionCount(2);
        _peakManagedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);

        _projection = new KernelPerformanceProjection();
        _pump = new KernelEventProjectionPump(eventLog, cursorStart, _projection, batchSize);
    }

    public string RunId { get; }

    public int? Seed { get; }

    public string Domain { get; }

    public string StartedUtc { get; }

    public PerformanceTelemetryBatch Batch { get; } = new();

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pump.DrainAvailable();
        UpdatePeakManagedMemory();
    }

    public void RecordTick(int tick, TimeSpan elapsed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ShouldSampleTick(tick))
        {
            return;
        }

        DrainAvailable();
        var counters = _projection.GetCounters(tick);
        var elapsedMilliseconds = elapsed.TotalMilliseconds;
        _sampledTickMilliseconds.Add(elapsedMilliseconds);
        Batch.TickPerformance.Add(new TickPerformanceRow(
            RunId,
            tick,
            elapsedMilliseconds,
            _projection.ActiveEntityCount,
            _projection.ActiveRelationshipCount,
            counters.ProcessEvents,
            counters.ProcessCandidates,
            counters.GeneratorEvents,
            counters.TotalMessages,
            _peakManagedMemoryBytes));
        _projection.ClearCounters(tick);
    }

    public void RecordSystemTiming(int tick, string phase, string systemName, TimeSpan elapsed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ShouldSampleSystem(tick))
        {
            return;
        }

        Batch.SystemPerformance.Add(new SystemPerformanceRow(
            RunId,
            tick,
            phase,
            systemName,
            elapsed.TotalMilliseconds));
    }

    public PerformanceTelemetryBatch Complete(int processedTicks)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(processedTicks);
        if (_completed)
        {
            return Batch;
        }

        DrainAvailable();
        _stopwatch.Stop();
        _completed = true;
        UpdatePeakManagedMemory();

        Batch.RunPerformance.Add(new RunPerformanceRow(
            RunId,
            Seed,
            Domain,
            StartedUtc,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            _requestedTicks,
            processedTicks,
            _stopwatch.Elapsed.TotalMilliseconds,
            Average(_sampledTickMilliseconds),
            Percentile(_sampledTickMilliseconds, 0.95d),
            _sampledTickMilliseconds.Count == 0 ? 0d : _sampledTickMilliseconds.Max(),
            GC.CollectionCount(0) - _gen0Start,
            GC.CollectionCount(1) - _gen1Start,
            GC.CollectionCount(2) - _gen2Start,
            _peakManagedMemoryBytes,
            _tickSampleCadence,
            _systemSampleCadence,
            _fullTelemetryEnabled));
        return Batch;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pump.Dispose();
        _stopwatch.Stop();
    }

    private bool ShouldSampleTick(int tick) => _tickSampleCadence > 0 && tick % _tickSampleCadence == 0;

    private bool ShouldSampleSystem(int tick) => _systemSampleCadence > 0 && tick % _systemSampleCadence == 0;

    private void UpdatePeakManagedMemory()
    {
        var current = GC.GetTotalMemory(forceFullCollection: false);
        if (current > _peakManagedMemoryBytes)
        {
            _peakManagedMemoryBytes = current;
        }
    }

    private static double Average(List<double> values) =>
        values.Count == 0 ? 0d : values.Average();

    private static double Percentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var ordered = values.Order().ToList();
        var index = (int)Math.Ceiling(percentile * ordered.Count) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Count - 1)];
    }
}

public sealed class KernelPerformanceProjection : KernelEventProjector
{
    private readonly Dictionary<int, KernelTickMessageCounters> _messageCounters = [];
    private readonly HashSet<int> _activeEntities = [];
    private readonly HashSet<int> _historicalEntities = [];
    private readonly Dictionary<ObservedRelationshipKey, bool> _activeRelationships = [];

    public int ActiveEntityCount => _activeEntities.Count;

    public int ActiveRelationshipCount => _activeRelationships.Count(pair => pair.Value);

    public KernelTickMessageCounters GetCounters(int tick) =>
        _messageCounters.TryGetValue(tick, out var counters) ? counters : new KernelTickMessageCounters();

    public void ClearCounters(int tick)
    {
        _messageCounters.Remove(tick);
    }

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        _activeEntities.Add(evt.EntityId);
        _historicalEntities.Remove(evt.EntityId);
        Count(evt.Tick).TotalMessages++;
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        if (evt.CurrentStatusCode is UiProjectionStatusCodes.Historical or 2)
        {
            _activeEntities.Remove(evt.EntityId);
            _historicalEntities.Add(evt.EntityId);
        }
        else
        {
            _activeEntities.Add(evt.EntityId);
            _historicalEntities.Remove(evt.EntityId);
        }

        Count(evt.Tick).TotalMessages++;
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var key = new ObservedRelationshipKey(evt.SourceEntityId, evt.TargetEntityId, evt.RelationshipKindId);
        _activeRelationships[key] = evt.NewStrength > 0f;
        Count(evt.Tick).TotalMessages++;
    }

    protected override void OnProcessEvaluated(long sequence, in ProcessEvaluatedKernelEvent evt)
    {
        var counters = Count(evt.Tick);
        counters.TotalMessages++;
        counters.ProcessEvents++;
        counters.ProcessCandidates += evt.CandidateCount;
    }

    protected override void OnProcessExecuted(long sequence, in ProcessExecutedKernelEvent evt)
    {
        var counters = Count(evt.Tick);
        counters.TotalMessages++;
        counters.ProcessEvents++;
    }

    protected override void OnGeneratorEvaluated(long sequence, in GeneratorEvaluatedKernelEvent evt)
    {
        var counters = Count(evt.Tick);
        counters.TotalMessages++;
        counters.GeneratorEvents++;
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var counters = Count(evt.Tick);
        counters.TotalMessages++;
        counters.GeneratorEvents++;
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnResourceChanged(long sequence, in ResourceChangedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnDegeneracyDetected(long sequence, in DegeneracyDetectedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnDecisionExecuted(long sequence, in DecisionExecutedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnPlanStarted(long sequence, in PlanStartedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnGoalConsidered(long sequence, in GoalConsideredKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnGoalScored(long sequence, in GoalScoredKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnGoalSelected(long sequence, in GoalSelectedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnPlanStepEmitted(long sequence, in PlanStepEmittedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnPlanCommitted(long sequence, in PlanCommittedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    protected override void OnPlanAbandoned(long sequence, in PlanAbandonedKernelEvent evt) => Count(evt.Tick).TotalMessages++;

    private KernelTickMessageCounters Count(int tick)
    {
        if (_messageCounters.TryGetValue(tick, out var counters))
        {
            return counters;
        }

        counters = new KernelTickMessageCounters();
        _messageCounters[tick] = counters;
        return counters;
    }
}

public sealed class KernelTickMessageCounters
{
    public int ProcessEvents { get; set; }

    public int ProcessCandidates { get; set; }

    public int GeneratorEvents { get; set; }

    public int TotalMessages { get; set; }
}
