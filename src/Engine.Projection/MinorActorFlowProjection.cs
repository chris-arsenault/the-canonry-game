namespace Engine.Projection;

using Engine.Eventing;

/// <summary>
/// A pulse of unnamed labor: where a minor-actor cohort just worked, and for
/// which culture. The kernel simulates this constantly; this projection keeps
/// a short ring of recent pulses so the map can finally show it.
/// </summary>
public readonly record struct MinorActorPulse(
    int Tick,
    int PlaneCode,
    int Q,
    int R,
    int CultureId);

public sealed class MinorActorFlowProjection : KernelEventProjector
{
    private const int Capacity = 768;

    private readonly MinorActorPulse[] _pulses = new MinorActorPulse[Capacity];
    private int _next;
    private int _count;

    public long Version { get; private set; }

    public int CurrentTick { get; private set; }

    /// <summary>
    /// Copies newest-first pulses for one plane at or after the requested tick,
    /// stopping once the monotonic event stream reaches expired history.
    /// </summary>
    public int CopyRecent(MinorActorPulse[] buffer, int planeCode, int minimumTick)
    {
        System.ArgumentNullException.ThrowIfNull(buffer);
        var copied = 0;
        for (var i = 0; i < _count && copied < buffer.Length; i++)
        {
            var pulse = _pulses[(_next - 1 - i + Capacity) % Capacity];
            if (pulse.Tick < minimumTick)
            {
                break;
            }

            if (pulse.PlaneCode == planeCode)
            {
                buffer[copied++] = pulse;
            }
        }

        return copied;
    }

    protected override void OnMinorActorVerbExecuted(long sequence, in MinorActorVerbExecutedKernelEvent evt)
    {
        _pulses[_next] = new MinorActorPulse(evt.Tick, evt.PlaneCode, evt.Q, evt.R, evt.CultureId);
        _next = (_next + 1) % Capacity;
        if (_count < Capacity)
        {
            _count++;
        }

        Version++;
    }

    protected override void OnTickCompleted(long sequence, in TickCompletedKernelEvent evt) =>
        CurrentTick = evt.Tick;
}
