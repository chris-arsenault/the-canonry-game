namespace Engine.Projection;

using Engine.Eventing;

/// <summary>
/// One hop of a named entity's journey: where it stepped from and to, on
/// which plane. The map layer resolves who and what color at draw time.
/// </summary>
public readonly record struct EntityTrailSegment(
    int Tick,
    int EntityId,
    int PlaneCode,
    int FromQ,
    int FromR,
    int ToQ,
    int ToR);

/// <summary>
/// A short ring of recent named-entity movement so the map can draw fading
/// trails - journeys become visible paths instead of tokens teleporting
/// between hexes.
/// </summary>
public sealed class EntityTrailProjection : KernelEventProjector
{
    private const int Capacity = 4096;

    private readonly EntityTrailSegment[] _segments = new EntityTrailSegment[Capacity];
    private int _next;
    private int _count;

    public long Version { get; private set; }

    public int CurrentTick { get; private set; }

    /// <summary>Copies recent segments into the caller's buffer; returns the count.</summary>
    public int CopyRecent(EntityTrailSegment[] buffer)
    {
        System.ArgumentNullException.ThrowIfNull(buffer);
        var take = System.Math.Min(_count, buffer.Length);
        for (var i = 0; i < take; i++)
        {
            buffer[i] = _segments[(_next - 1 - i + Capacity) % Capacity];
        }

        return take;
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
        _segments[_next] = new EntityTrailSegment(
            evt.Tick,
            evt.EntityId,
            evt.PlaneCode,
            evt.FromQ,
            evt.FromR,
            evt.ToQ,
            evt.ToR);
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
