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
    int PlaneCode,
    HexCoord Coord,
    int TerrainId,
    int OwnerEntityId,
    int SuperregionId,
    int ContinentId,
    IReadOnlyList<HexInfluenceProjection> Influences,
    float RoadQuality,
    int RoadTravelCount,
    int Tick,
    long WorldVersion,
    long Sequence);

public sealed record HexInfluenceProjection(
    int EntityId,
    float Value,
    int Tick,
    long Sequence);

public interface IHexSurfaceIndex
{
    IReadOnlyDictionary<int, HexProjection> HexesById(int planeCode);

    IReadOnlyDictionary<HexCoord, int> HexIdsByCoord(int planeCode);

    IReadOnlyCollection<HexProjection> Hexes(int planeCode);

    bool TryGetHex(int planeCode, int hexId, out HexProjection hex);

    bool TryGetHex(int planeCode, HexCoord coord, out HexProjection hex);

    IReadOnlyList<HexCoord> GetNeighborCoords(int planeCode, HexCoord coord);

    IReadOnlyList<HexProjection> GetNeighbors(int planeCode, HexCoord coord);
}

public sealed class HexMapProjection : KernelEventProjector, IHexSurfaceIndex
{
    private readonly Dictionary<int, HexSurfaceProjection> _surfacesByPlane = [];
    private readonly Dictionary<int, int> _ownerColorUseCounts = [];
    private readonly List<int> _ownerColorEntityIds = [];

    public IReadOnlyDictionary<int, HexProjection> HexesById(int planeCode) => GetSurface(planeCode).HexesById;

    public IReadOnlyDictionary<HexCoord, int> HexIdsByCoord(int planeCode) => GetSurface(planeCode).HexIdsByCoord;

    public IReadOnlyCollection<HexProjection> Hexes(int planeCode) => GetSurface(planeCode).HexesById.Values;

    public int MapRadius { get; private set; }

    public long LastProjectedSequence { get; private set; } = -1;

    public int CurrentTick { get; private set; }

    public long WorldVersion { get; private set; }

    public long CellVersion { get; private set; }

    public long FrameVersion { get; private set; }

    public long OwnerColorVersion { get; private set; }

    public IReadOnlyList<int> OwnerColorEntityIds => _ownerColorEntityIds;

    public long DroppedEvents { get; private set; }

    public bool IsComplete => DroppedEvents == 0;

    public bool TryGetHex(int planeCode, int hexId, out HexProjection hex)
    {
        if (TryGetSurface(planeCode, out var surface) &&
            surface.HexesById.TryGetValue(hexId, out var resolved))
        {
            hex = resolved;
            return true;
        }

        hex = null!;
        return false;
    }

    public bool TryGetHex(int planeCode, HexCoord coord, out HexProjection hex)
    {
        if (TryGetSurface(planeCode, out var surface) &&
            surface.HexIdsByCoord.TryGetValue(coord, out var hexId))
        {
            return TryGetHex(planeCode, hexId, out hex);
        }

        hex = null!;
        return false;
    }

    public IReadOnlyList<HexCoord> GetNeighborCoords(int planeCode, HexCoord coord)
    {
        var neighbors = new List<HexCoord>(6);
        if (!TryGetSurface(planeCode, out var surface))
        {
            return neighbors;
        }

        foreach (var candidate in coord.Neighbors().Where(surface.HexIdsByCoord.ContainsKey))
        {
            neighbors.Add(candidate);
        }

        return neighbors;
    }

    public IReadOnlyList<HexProjection> GetNeighbors(int planeCode, HexCoord coord)
    {
        var neighbors = new List<HexProjection>(6);
        foreach (var candidate in coord.Neighbors())
        {
            if (TryGetHex(planeCode, candidate, out var hex))
            {
                neighbors.Add(hex);
            }
        }

        return neighbors;
    }

    public IReadOnlyCollection<int> DirtyCellIds(int planeCode) =>
        TryGetSurface(planeCode, out var surface)
            ? surface.DirtyCellIds
            : [];

    public void ClearDirtyCells(int planeCode)
    {
        if (TryGetSurface(planeCode, out var surface))
        {
            surface.DirtyCellIds.Clear();
        }
    }

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
    }

    protected override void OnCellCreated(long sequence, in CellCreatedKernelEvent evt)
    {
        UpsertCell(
            sequence,
            evt.Tick,
            evt.WorldVersion,
            PlaneCodes.Physical,
            evt.Q,
            evt.R,
            evt.TerrainId,
            evt.OwnerEntityId,
            evt.SuperregionId,
            evt.ContinentId);
    }

    protected override void OnCellChanged(long sequence, in CellChangedKernelEvent evt)
    {
        UpsertCell(
            sequence,
            evt.Tick,
            evt.WorldVersion,
            PlaneCodes.Physical,
            evt.Q,
            evt.R,
            evt.CurrentTerrainId,
            evt.CurrentOwnerEntityId,
            evt.SuperregionId,
            evt.ContinentId);
    }

    protected override void OnCellInfluenceChanged(long sequence, in CellInfluenceChangedKernelEvent evt)
    {
        var existing = EnsureCell(sequence, evt.Tick, evt.WorldVersion, evt.PlaneCode, evt.Q, evt.R);

        var entityId = evt.EntityId;
        var influenceWasActive = existing.Influences.Any(influence => influence.EntityId == entityId);
        var influenceIsActive = evt.CurrentInfluence > 0f;
        var influences = UpsertInfluence(existing.Influences, entityId, evt.CurrentInfluence, evt.Tick, sequence);
        GetSurface(evt.PlaneCode).HexesById[existing.HexId] = existing with
        {
            Influences = influences,
            Tick = evt.Tick,
            WorldVersion = evt.WorldVersion,
            Sequence = sequence,
        };
        UpdateOwnerColorMembership(entityId, influenceWasActive, influenceIsActive);
        CellVersion++;
        MarkCellDirty(evt.PlaneCode, existing.HexId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnMapCellStateChanged(long sequence, in MapCellStateChangedKernelEvent evt)
    {
        UpsertCell(
            sequence,
            evt.Tick,
            evt.WorldVersion,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.CurrentTerrainId,
            evt.CurrentOwnerEntityId,
            evt.SuperregionId,
            evt.ContinentId);
    }

    protected override void OnCellRoadChanged(long sequence, in CellRoadChangedKernelEvent evt)
    {
        var existing = EnsureCell(sequence, evt.Tick, evt.WorldVersion, evt.PlaneCode, evt.Q, evt.R);
        GetSurface(evt.PlaneCode).HexesById[existing.HexId] = existing with
        {
            RoadQuality = evt.CurrentQuality,
            RoadTravelCount = evt.TravelCount,
            Tick = evt.Tick,
            WorldVersion = evt.WorldVersion,
            Sequence = sequence,
        };
        CellVersion++;
        MarkCellDirty(evt.PlaneCode, existing.HexId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private void UpsertCell(
        long sequence,
        int tick,
        long worldVersion,
        int planeCode,
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId)
    {
        var surface = GetSurface(planeCode);
        var coord = new HexCoord(q, r);
        var hexId = CellId(q, r);
        if (surface.HexesById.TryGetValue(hexId, out var existing))
        {
            surface.HexIdsByCoord.Remove(existing.Coord);
        }

        surface.HexesById[hexId] = new HexProjection(
            hexId,
            planeCode,
            coord,
            terrainId,
            ownerEntityId,
            superregionId,
            continentId,
            existing?.Influences ?? [],
            existing?.RoadQuality ?? 0f,
            existing?.RoadTravelCount ?? 0,
            tick,
            worldVersion,
            sequence);
        surface.HexIdsByCoord[coord] = hexId;
        MapRadius = Math.Max(MapRadius, coord.DistanceFromOrigin);
        UpdateOwnerColorMembership(existing?.OwnerEntityId ?? 0, ownerEntityId);
        CellVersion++;
        FrameVersion++;
        MarkCellDirty(planeCode, hexId);
        MarkProjected(sequence, tick, worldVersion);
    }

    private void MarkCellDirty(int planeCode, int hexId)
    {
        GetSurface(planeCode).DirtyCellIds.Add(hexId);
    }

    private void UpdateOwnerColorMembership(int oldOwnerEntityId, int newOwnerEntityId)
    {
        if (oldOwnerEntityId == newOwnerEntityId)
        {
            return;
        }

        if (oldOwnerEntityId > 0)
        {
            DecrementOwnerColorUse(oldOwnerEntityId);
        }

        if (newOwnerEntityId > 0)
        {
            IncrementOwnerColorUse(newOwnerEntityId);
        }
    }

    private void UpdateOwnerColorMembership(int entityId, bool wasActive, bool isActive)
    {
        if (entityId <= 0 || wasActive == isActive)
        {
            return;
        }

        if (wasActive)
        {
            DecrementOwnerColorUse(entityId);
            return;
        }

        IncrementOwnerColorUse(entityId);
    }

    private void IncrementOwnerColorUse(int entityId)
    {
        if (_ownerColorUseCounts.TryGetValue(entityId, out var count))
        {
            _ownerColorUseCounts[entityId] = count + 1;
            return;
        }

        _ownerColorUseCounts[entityId] = 1;
        InsertOwnerColorEntityId(entityId);
        OwnerColorVersion++;
    }

    private void DecrementOwnerColorUse(int entityId)
    {
        if (!_ownerColorUseCounts.TryGetValue(entityId, out var count))
        {
            return;
        }

        if (count > 1)
        {
            _ownerColorUseCounts[entityId] = count - 1;
            return;
        }

        _ownerColorUseCounts.Remove(entityId);
        _ownerColorEntityIds.Remove(entityId);
        OwnerColorVersion++;
    }

    private void InsertOwnerColorEntityId(int entityId)
    {
        var index = _ownerColorEntityIds.BinarySearch(entityId);
        if (index < 0)
        {
            _ownerColorEntityIds.Insert(~index, entityId);
        }
    }

    private HexProjection EnsureCell(
        long sequence,
        int tick,
        long worldVersion,
        int planeCode,
        int q,
        int r)
    {
        var coord = new HexCoord(q, r);
        if (TryGetHex(planeCode, coord, out var existing))
        {
            return existing;
        }

        UpsertCell(
            sequence,
            tick,
            worldVersion,
            planeCode,
            q,
            r,
            terrainId: 1,
            ownerEntityId: 0,
            superregionId: 0,
            continentId: 0);
        _ = TryGetHex(planeCode, coord, out existing);
        return existing;
    }

    private HexSurfaceProjection GetSurface(int planeCode)
    {
        if (!_surfacesByPlane.TryGetValue(planeCode, out var surface))
        {
            surface = new HexSurfaceProjection();
            _surfacesByPlane[planeCode] = surface;
        }

        return surface;
    }

    private bool TryGetSurface(int planeCode, out HexSurfaceProjection surface) =>
        _surfacesByPlane.TryGetValue(planeCode, out surface!);

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

    private static HexInfluenceProjection[] UpsertInfluence(
        IReadOnlyList<HexInfluenceProjection> existing,
        int entityId,
        float value,
        int tick,
        long sequence)
    {
        var influences = new List<HexInfluenceProjection>(existing.Count + 1);
        var updated = false;
        foreach (var influence in existing)
        {
            if (influence.EntityId != entityId)
            {
                influences.Add(influence);
                continue;
            }

            updated = true;
            if (value > 0f)
            {
                influences.Add(new HexInfluenceProjection(entityId, value, tick, sequence));
            }
        }

        if (!updated && value > 0f)
        {
            influences.Add(new HexInfluenceProjection(entityId, value, tick, sequence));
        }

        return influences
            .OrderByDescending(influence => influence.Value)
            .ThenBy(influence => influence.EntityId)
            .ToArray();
    }

    private void MarkProjected(long sequence, int tick, long worldVersion)
    {
        LastProjectedSequence = sequence;
        CurrentTick = Math.Max(CurrentTick, tick);
        WorldVersion = Math.Max(WorldVersion, worldVersion);
    }

    private sealed class HexSurfaceProjection
    {
        public Dictionary<int, HexProjection> HexesById { get; } = [];

        public Dictionary<HexCoord, int> HexIdsByCoord { get; } = [];

        public HashSet<int> DirtyCellIds { get; } = [];
    }
}
