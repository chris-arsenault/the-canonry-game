namespace Engine.Degeneracy;

using Engine.Events;

/// <summary>
/// Registry of degeneracy detectors. Evaluates all detectors periodically
/// and emits warnings to the event log when severity changes.
/// </summary>
public sealed class DetectorRegistry
{
    private readonly List<IDegeneracyDetector> _detectors = [];
    private readonly Dictionary<string, Severity> _previousSeverity = [];
    private readonly EventLog? _eventLog;

    public DetectorRegistry(EventLog? eventLog = null)
    {
        _eventLog = eventLog;
    }

    public void Register(IDegeneracyDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _detectors.Add(detector);
        _previousSeverity[detector.Name] = Severity.Normal;
    }

    /// <summary>
    /// Evaluate all detectors against current world state.
    /// Emits events to the event log on severity transitions.
    /// Returns current severity for each detector.
    /// </summary>
    public IReadOnlyDictionary<string, Severity> Evaluate(IDegeneracyContext context, int currentTick)
    {
        ArgumentNullException.ThrowIfNull(context);

        var results = new Dictionary<string, Severity>();

        foreach (var detector in _detectors)
        {
            var severity = detector.Evaluate(context);
            results[detector.Name] = severity;

            var previous = _previousSeverity.GetValueOrDefault(detector.Name, Severity.Normal);

            // Emit event on severity transitions
            if (severity != previous)
            {
                if (severity > previous)
                {
                    _eventLog?.Emit(currentTick, EventType.Threshold,
                        $"WARNING: {detector.WarningMessage} (severity: {severity})",
                        "degeneracy_detector");
                }
                else
                {
                    _eventLog?.Emit(currentTick, EventType.Threshold,
                        $"RESOLVED: {detector.Name} returned to {severity}",
                        "degeneracy_detector");
                }
            }

            _previousSeverity[detector.Name] = severity;
        }

        return results;
    }

    /// <summary>Get the current severity for a named detector.</summary>
    public Severity GetSeverity(string detectorName) =>
        _previousSeverity.GetValueOrDefault(detectorName, Severity.Normal);

    /// <summary>Get all registered detector names.</summary>
    public IEnumerable<string> DetectorNames => _detectors.Select(d => d.Name);

    /// <summary>Register the 3 MVP detectors.</summary>
    public static DetectorRegistry CreateDefault(EventLog? eventLog = null)
    {
        var registry = new DetectorRegistry(eventLog);
        registry.Register(new SettlementDepopulationDetector());
        registry.Register(new PoliticalMonocultureDetector());
        registry.Register(new NpcExtinctionDetector());
        return registry;
    }
}
