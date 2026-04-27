namespace Engine.Projection;

using Engine.Eventing;

public readonly record struct HexCoord(int Q, int R)
{
    private static readonly HexCoord[] Directions =
    [
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1),
    ];

    public int S => -Q - R;

    public int DistanceFromOrigin => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;

    public static int Distance(HexCoord a, HexCoord b) =>
        (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;

    public HexCoord Neighbor(int direction)
    {
        var dir = Directions[direction % Directions.Length];
        return new HexCoord(Q + dir.Q, R + dir.R);
    }

    public IEnumerable<HexCoord> Neighbors()
    {
        for (var i = 0; i < Directions.Length; i++)
        {
            yield return Neighbor(i);
        }
    }

    public static IEnumerable<HexCoord> CoordsInRadius(HexCoord center, int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        for (var q = -radius; q <= radius; q++)
        {
            var r1 = Math.Max(-radius, -q - radius);
            var r2 = Math.Min(radius, -q + radius);
            for (var r = r1; r <= r2; r++)
            {
                yield return new HexCoord(center.Q + q, center.R + r);
            }
        }
    }

    public override string ToString() => $"({Q},{R})";
}

public sealed record HexProjection(
    int HexId,
    HexCoord Coord,
    int TerrainId,
    int OwnerEntityId,
    int Tick,
    long WorldVersion,
    long Sequence);

public interface IHexSurfaceIndex
{
    IReadOnlyDictionary<int, HexProjection> HexesById { get; }

    IReadOnlyDictionary<HexCoord, int> HexIdsByCoord { get; }

    bool TryGetHex(int hexId, out HexProjection hex);

    bool TryGetHex(HexCoord coord, out HexProjection hex);

    IReadOnlyList<HexCoord> GetNeighborCoords(HexCoord coord);

    IReadOnlyList<HexProjection> GetNeighbors(HexCoord coord);
}

public sealed class HexMapProjection : KernelEventProjector, IHexSurfaceIndex
{
    private readonly Dictionary<int, HexProjection> _hexesById = [];
    private readonly Dictionary<HexCoord, int> _hexIdsByCoord = [];

    public IReadOnlyDictionary<int, HexProjection> HexesById => _hexesById;

    public IReadOnlyDictionary<HexCoord, int> HexIdsByCoord => _hexIdsByCoord;

    public int MapRadius { get; private set; }

    public long LastProjectedSequence { get; private set; } = -1;

    public int CurrentTick { get; private set; }

    public long WorldVersion { get; private set; }

    public long DroppedEvents { get; private set; }

    public bool IsComplete => DroppedEvents == 0;

    public bool TryGetHex(int hexId, out HexProjection hex)
    {
        if (_hexesById.TryGetValue(hexId, out var resolved))
        {
            hex = resolved;
            return true;
        }

        hex = null!;
        return false;
    }

    public bool TryGetHex(HexCoord coord, out HexProjection hex)
    {
        if (_hexIdsByCoord.TryGetValue(coord, out var hexId))
        {
            return TryGetHex(hexId, out hex);
        }

        hex = null!;
        return false;
    }

    public IReadOnlyList<HexCoord> GetNeighborCoords(HexCoord coord)
    {
        var neighbors = new List<HexCoord>(6);
        foreach (var candidate in coord.Neighbors())
        {
            if (_hexIdsByCoord.ContainsKey(candidate))
            {
                neighbors.Add(candidate);
            }
        }

        return neighbors;
    }

    public IReadOnlyList<HexProjection> GetNeighbors(HexCoord coord)
    {
        var neighbors = new List<HexProjection>(6);
        foreach (var candidate in coord.Neighbors())
        {
            if (TryGetHex(candidate, out var hex))
            {
                neighbors.Add(hex);
            }
        }

        return neighbors;
    }

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
    }

    protected override void OnCellCreated(long sequence, in CellCreatedKernelEvent evt)
    {
        var coord = new HexCoord(evt.Q, evt.R);
        var hexId = CellId(evt.Q, evt.R);
        if (_hexesById.TryGetValue(hexId, out var existing))
        {
            _hexIdsByCoord.Remove(existing.Coord);
        }

        _hexesById[hexId] = new HexProjection(
            hexId,
            coord,
            evt.TerrainId,
            evt.OwnerEntityId,
            evt.Tick,
            evt.WorldVersion,
            sequence);
        _hexIdsByCoord[coord] = hexId;
        MapRadius = Math.Max(MapRadius, coord.DistanceFromOrigin);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private static int CellId(int q, int r)
    {
        var uq = ZigZag(q);
        var ur = ZigZag(r);
        var sum = uq + ur;
        var paired = ((sum * (sum + 1L)) / 2L) + ur;
        if (paired >= int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Hex coordinate ({q},{r}) is outside projection ID range.");
        }

        return (int)paired + 1;
    }

    private static long ZigZag(int value) =>
        value >= 0
            ? value * 2L
            : (-value * 2L) - 1L;

    private void MarkProjected(long sequence, int tick, long worldVersion)
    {
        LastProjectedSequence = sequence;
        CurrentTick = Math.Max(CurrentTick, tick);
        WorldVersion = Math.Max(WorldVersion, worldVersion);
    }
}
