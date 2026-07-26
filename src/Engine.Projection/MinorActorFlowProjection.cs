namespace Engine.Projection;

using Engine.Eventing;

/// <summary>
/// A trace of unnamed labor: where a minor-actor cohort just worked, and for
/// which culture. The kernel simulates this constantly; this projection keeps
/// a short ring of recent activity so the map can finally show it.
/// </summary>
public readonly record struct MinorActorActivityPoint(
    int Tick,
    int PlaneCode,
    int Q,
    int R,
    int CultureId);

public sealed class MinorActorFlowProjection : KernelEventProjector
{
    private const int Capacity = 768;

    private readonly MinorActorActivityPoint[] _activity = new MinorActorActivityPoint[Capacity];
    private int _next;
    private int _count;

    public long Version { get; private set; }

    public int CurrentTick { get; private set; }

    /// <summary>
    /// Copies newest-first activity for one plane at or after the requested tick,
    /// stopping once the monotonic event stream reaches expired history.
    /// </summary>
    public int CopyRecent(MinorActorActivityPoint[] buffer, int planeCode, int minimumTick)
    {
        System.ArgumentNullException.ThrowIfNull(buffer);
        var copied = 0;
        for (var i = 0; i < _count && copied < buffer.Length; i++)
        {
            var activity = _activity[(_next - 1 - i + Capacity) % Capacity];
            if (activity.Tick < minimumTick)
            {
                break;
            }

            if (activity.PlaneCode == planeCode)
            {
                buffer[copied++] = activity;
            }
        }

        return copied;
    }

    protected override void OnMinorActorVerbExecuted(long sequence, in MinorActorVerbExecutedKernelEvent evt)
    {
        _activity[_next] = new MinorActorActivityPoint(
            evt.Tick,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.CultureId);
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
