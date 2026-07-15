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

    /// <summary>Copies recent pulses into the caller's buffer; returns the count.</summary>
    public int CopyRecent(MinorActorPulse[] buffer)
    {
        System.ArgumentNullException.ThrowIfNull(buffer);
        var take = System.Math.Min(_count, buffer.Length);
        for (var i = 0; i < take; i++)
        {
            buffer[i] = _pulses[(_next - 1 - i + Capacity) % Capacity];
        }

        return take;
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
