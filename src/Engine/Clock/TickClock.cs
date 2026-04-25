namespace Engine.Clock;

using System.Diagnostics;

/// <summary>
/// Single simulation clock. Manages tick advancement, speed control,
/// cadence-based system registration, and strict phase ordering.
/// </summary>
public sealed class TickClock
{
    private readonly List<SystemRegistration> _registrations = [];

    public int CurrentTick { get; private set; }
    public SimulationSpeed Speed { get; set; } = SimulationSpeed.Normal;
    public Func<int, bool>? TickTimingSampleFilter { get; set; }
    public Func<int, bool>? SystemTimingSampleFilter { get; set; }

    public event EventHandler<TickTimingEventArgs>? TickTimed;
    public event EventHandler<TickSystemTimingEventArgs>? SystemTimed;

    /// <summary>
    /// Register a system callback to run at a specific phase and cadence.
    /// Systems with cadence N run every N ticks. Minimum cadence is 1.
    /// </summary>
    public void Register(string systemName, TickPhase phase, int cadence, Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cadence);

        _registrations.Add(new SystemRegistration(systemName, phase, cadence, callback));
    }

    /// <summary>
    /// Unregister all callbacks for a named system.
    /// </summary>
    public void Unregister(string systemName)
    {
        _registrations.RemoveAll(r => r.SystemName == systemName);
    }

    /// <summary>
    /// Advance the simulation by one tick. Executes all registered systems
    /// in strict phase order, respecting cadence.
    /// Returns true if a tick was processed, false if paused.
    /// </summary>
    public bool Tick()
    {
        if (Speed == SimulationSpeed.Paused)
        {
            return false;
        }

        CurrentTick++;
        var collectTickTiming = TickTimed is not null && ShouldCollectTickTiming(CurrentTick);
        var tickStopwatch = collectTickTiming ? Stopwatch.StartNew() : null;
        ExecutePhases();
        if (tickStopwatch is not null)
        {
            tickStopwatch.Stop();
            TickTimed?.Invoke(this, new TickTimingEventArgs(CurrentTick, tickStopwatch.Elapsed));
        }

        return true;
    }

    /// <summary>
    /// Advance the simulation by the number of ticks indicated by current speed.
    /// Call this once per frame. Returns the number of ticks actually processed.
    /// </summary>
    public int AdvanceFrame()
    {
        var ticksToProcess = (int)Speed;
        var processed = 0;

        for (var i = 0; i < ticksToProcess; i++)
        {
            if (Tick())
            {
                processed++;
            }
        }

        return processed;
    }

    /// <summary>
    /// Get all system names registered for a specific phase.
    /// </summary>
    public IEnumerable<string> GetSystemsInPhase(TickPhase phase)
    {
        return _registrations
            .Where(r => r.Phase == phase)
            .Select(r => r.SystemName);
    }

    /// <summary>
    /// Check which registered systems would run on a given tick.
    /// </summary>
    public IEnumerable<string> GetSystemsRunningOnTick(int tick)
    {
        return _registrations
            .Where(r => tick % r.Cadence == 0)
            .Select(r => r.SystemName);
    }

    private void ExecutePhases()
    {
        var systemTimed = SystemTimed;
        var collectSystemTiming = systemTimed is not null && ShouldCollectSystemTiming(CurrentTick);

        // Execute all phases in strict order
        foreach (TickPhase phase in Enum.GetValues<TickPhase>())
        {
            foreach (var registration in _registrations.Where(r => r.Phase == phase))
            {
                if (CurrentTick % registration.Cadence == 0)
                {
                    if (!collectSystemTiming)
                    {
                        registration.Callback(CurrentTick);
                        continue;
                    }

                    var systemStopwatch = Stopwatch.StartNew();
                    try
                    {
                        registration.Callback(CurrentTick);
                    }
                    finally
                    {
                        systemStopwatch.Stop();
                        systemTimed?.Invoke(this, new TickSystemTimingEventArgs(
                            CurrentTick,
                            registration.Phase,
                            registration.SystemName,
                            systemStopwatch.Elapsed));
                    }
                }
            }
        }
    }

    private bool ShouldCollectTickTiming(int tick) => TickTimingSampleFilter?.Invoke(tick) ?? true;
    private bool ShouldCollectSystemTiming(int tick) => SystemTimingSampleFilter?.Invoke(tick) ?? true;

    private sealed record SystemRegistration(
        string SystemName,
        TickPhase Phase,
        int Cadence,
        Action<int> Callback);
}

public sealed class TickTimingEventArgs(int tick, TimeSpan elapsed) : EventArgs
{
    public int Tick { get; } = tick;
    public TimeSpan Elapsed { get; } = elapsed;
}

public sealed class TickSystemTimingEventArgs(
    int tick,
    TickPhase phase,
    string systemName,
    TimeSpan elapsed)
    : EventArgs
{
    public int Tick { get; } = tick;
    public TickPhase Phase { get; } = phase;
    public string SystemName { get; } = systemName;
    public TimeSpan Elapsed { get; } = elapsed;
}
