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

    /// <summary>
    /// Copies newest-first segments for one plane at or after the requested
    /// tick. Event ticks are monotonic, so the scan stops as soon as it reaches
    /// expired history instead of walking the entire retained ring.
    /// </summary>
    public int CopyRecent(EntityTrailSegment[] buffer, int planeCode, int minimumTick)
    {
        System.ArgumentNullException.ThrowIfNull(buffer);
        var copied = 0;
        for (var i = 0; i < _count && copied < buffer.Length; i++)
        {
            var segment = _segments[(_next - 1 - i + Capacity) % Capacity];
            if (segment.Tick < minimumTick)
            {
                break;
            }

            if (segment.PlaneCode == planeCode)
            {
                buffer[copied++] = segment;
            }
        }

        return copied;
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

    protected override void OnWatermarkAdvanced(
        long sequence,
        in WatermarkAdvancedKernelEvent evt) =>
        CurrentTick = evt.LowWatermark;
}
