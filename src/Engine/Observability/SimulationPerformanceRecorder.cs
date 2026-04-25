namespace Engine.Observability;

using System.Diagnostics;
using System.Globalization;

using Engine.Clock;
using Engine.Eventing;
using Engine.Graph;
using Engine.Simulation;

public sealed class SimulationPerformanceRecorder : IDisposable
    {
        private readonly SimulationRunner _sim;
        private readonly KernelEventCursor _kernelCursor;
        private readonly SequencedKernelEvent[] _kernelEventBuffer = new SequencedKernelEvent[256];
        private readonly Stopwatch _simulationStopwatch = Stopwatch.StartNew();
    private readonly Dictionary<int, TickMessageCounters> _messageCounters = [];
    private readonly List<double> _sampledTickMilliseconds = [];
    private readonly int _requestedTicks;
    private readonly int _startTick;
    private readonly int _tickSampleCadence;
    private readonly int _systemSampleCadence;
    private readonly int _gen0Start;
    private readonly int _gen1Start;
    private readonly int _gen2Start;
    private readonly Func<int, bool>? _previousTickTimingSampleFilter;
    private readonly Func<int, bool>? _previousSystemTimingSampleFilter;
    private readonly bool _fullTelemetryEnabled;
    private long _peakManagedMemoryBytes;
    private bool _completed;
    private bool _disposed;

    public string RunId { get; }
    public int? Seed { get; }
    public string Domain { get; }
    public string StartedUtc { get; }
    public PerformanceTelemetryBatch Batch { get; } = new();

    public SimulationPerformanceRecorder(
        SimulationRunner sim,
        string runId,
        int requestedTicks,
        int? seed = null,
        string domain = "IceRemembers",
        int tickSampleCadence = 1,
        int systemSampleCadence = 1,
        bool fullTelemetryEnabled = false)
    {
        ArgumentNullException.ThrowIfNull(sim);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentOutOfRangeException.ThrowIfNegative(requestedTicks);

        _sim = sim;
        RunId = runId;
        Seed = seed;
        Domain = domain;
        StartedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        _requestedTicks = requestedTicks;
        _startTick = sim.Clock.CurrentTick;
        _tickSampleCadence = tickSampleCadence;
        _systemSampleCadence = systemSampleCadence;
        _fullTelemetryEnabled = fullTelemetryEnabled;
        _gen0Start = GC.CollectionCount(0);
        _gen1Start = GC.CollectionCount(1);
        _gen2Start = GC.CollectionCount(2);
        _peakManagedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);

        _previousTickTimingSampleFilter = sim.Clock.TickTimingSampleFilter;
        _previousSystemTimingSampleFilter = sim.Clock.SystemTimingSampleFilter;
        sim.Clock.TickTimingSampleFilter = ShouldSampleTickTiming;
        sim.Clock.SystemTimingSampleFilter = ShouldSampleSystemTiming;
        sim.Clock.TickTimed += OnTickTimed;
        if (_systemSampleCadence > 0)
        {
            sim.Clock.SystemTimed += OnSystemTimed;
        }

        _kernelCursor = sim.KernelEvents.CreateCursor(KernelEventCursorStart.Latest);
    }

    public PerformanceTelemetryBatch Complete()
    {
        if (_completed)
        {
            return Batch;
        }

        _simulationStopwatch.Stop();
        _completed = true;
        UpdatePeakManagedMemory();

        Batch.RunPerformance.Add(new RunPerformanceRow(
            RunId,
            Seed,
            Domain,
            StartedUtc,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            _requestedTicks,
            Math.Max(0, _sim.Clock.CurrentTick - _startTick),
            _simulationStopwatch.Elapsed.TotalMilliseconds,
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
        if (_disposed) return;
        _disposed = true;

        Complete();
        _kernelCursor.Dispose();
        _sim.Clock.TickTimed -= OnTickTimed;
        _sim.Clock.SystemTimed -= OnSystemTimed;
        _sim.Clock.TickTimingSampleFilter = _previousTickTimingSampleFilter;
        _sim.Clock.SystemTimingSampleFilter = _previousSystemTimingSampleFilter;
    }

    private void DrainKernelEvents(int tick)
    {
        while (true)
        {
            var read = _sim.KernelEvents.Read(_kernelCursor, _kernelEventBuffer);
            if (read == 0)
            {
                return;
            }

            for (var i = 0; i < read; i++)
            {
                var kernelEvent = _kernelEventBuffer[i].Event;
                if (kernelEvent.Tick > tick || !ShouldSampleTick(kernelEvent.Tick))
                {
                    continue;
                }

                var counters = GetCounters(kernelEvent.Tick);
                counters.TotalMessages++;

                switch (kernelEvent.EventType)
                {
                    case KernelEventType.ProcessEvaluated:
                        var process = kernelEvent.AsProcessEvaluated();
                        counters.ProcessEvents++;
                        counters.ProcessCandidates += process.CandidateCount;
                        break;
                    case KernelEventType.GeneratorEvaluated:
                    case KernelEventType.GeneratorFired:
                        counters.GeneratorEvents++;
                        break;
                }
            }
        }
    }

    private void OnTickTimed(object? sender, TickTimingEventArgs timing)
    {
        if (!ShouldSampleTick(timing.Tick))
        {
            return;
        }

        DrainKernelEvents(timing.Tick);
        var elapsedMilliseconds = timing.Elapsed.TotalMilliseconds;
        _sampledTickMilliseconds.Add(elapsedMilliseconds);
        UpdatePeakManagedMemory();

        _messageCounters.TryGetValue(timing.Tick, out var counters);
        Batch.TickPerformance.Add(new TickPerformanceRow(
            RunId,
            timing.Tick,
            elapsedMilliseconds,
            CountActiveEntities(),
            CountActiveRelationships(),
            counters?.ProcessEvents ?? 0,
            counters?.ProcessCandidates ?? 0,
            counters?.GeneratorEvents ?? 0,
            counters?.TotalMessages ?? 0,
            _peakManagedMemoryBytes));

        _messageCounters.Remove(timing.Tick);
    }

    private void OnSystemTimed(object? sender, TickSystemTimingEventArgs timing)
    {
        if (!ShouldSampleSystem(timing.Tick))
        {
            return;
        }

        Batch.SystemPerformance.Add(new SystemPerformanceRow(
            RunId,
            timing.Tick,
            timing.Phase.ToString(),
            timing.SystemName,
            timing.Elapsed.TotalMilliseconds));
    }

    private TickMessageCounters GetCounters(int tick)
    {
        if (!_messageCounters.TryGetValue(tick, out var counters))
        {
            counters = new TickMessageCounters();
            _messageCounters[tick] = counters;
        }

        return counters;
    }

    private bool ShouldSampleTickTiming(int tick) => ShouldSampleTick(tick);

    private bool ShouldSampleSystemTiming(int tick) => ShouldSampleSystem(tick);

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

    private int CountActiveEntities() =>
        _sim.Graph.AllEntities.Values.Count(entity => entity.Status == EntityStatus.Active);

    private int CountActiveRelationships()
    {
        var count = 0;
        foreach (var (entityId, _) in _sim.Graph.AllEntities)
        {
            count += _sim.Graph.OutgoingRelationships(entityId).Count(relationship => relationship.IsActive);
        }

        return count;
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

    private sealed class TickMessageCounters
    {
        public int ProcessEvents { get; set; }
        public int ProcessCandidates { get; set; }
        public int GeneratorEvents { get; set; }
        public int TotalMessages { get; set; }
    }
}
