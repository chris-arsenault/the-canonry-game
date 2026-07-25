namespace Engine.Projection;

using Engine.Constants;
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
    IReadOnlyList<HexCultureInfluenceProjection> CultureInfluences,
    float RoadQuality,
    int RoadTravelCount,
    int TravelStructureId,
    int Tick,
    long WorldVersion,
    long Sequence,
    int ForgedSpireTiers = 0,
    int ForgedTunnelDepth = 0);

public sealed record HexInfluenceProjection(
    int EntityId,
    float Value,
    int Tick,
    long Sequence);

public sealed record HexCultureInfluenceProjection(
    int CultureId,
    float Share,
    int Tick,
    long Sequence,
    float RawMass = 0f,
    float TotalRawMass = 0f,
    int ForeignCultureId = 0,
    float ForeignShare = 0f,
    float ForeignRawMass = 0f,
    int CellKindCode = 0,
    int StrengthBandCode = 0);

public sealed record CultureTheaterProjection(
    int PlaneCode,
    int TheaterId,
    int DominantCultureId,
    int RivalCultureId,
    HexCoord Center,
    int CellCount,
    int ContestedCellCount,
    int ControlMismatchCellCount,
    float AverageDominantShare,
    float AverageMargin);

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

    public long CultureOverlayVersion { get; private set; }

    public long InfluenceOverlayVersion { get; private set; }

    public long RoadNetworkVersion { get; private set; }

    public long RouteNetworkVersion { get; private set; }

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

    public IReadOnlyList<CultureTheaterProjection> CultureTheaters(
        int planeCode,
        Func<int, int> ownerCultureResolver)
    {
        ArgumentNullException.ThrowIfNull(ownerCultureResolver);
        if (!TryGetSurface(planeCode, out var surface))
        {
            return [];
        }

        var contestedCells = surface.HexesById.Values
            .Where(cell => CellHasCultureContest(cell, ownerCultureResolver))
            .ToDictionary(cell => cell.Coord);
        if (contestedCells.Count == 0)
        {
            return [];
        }

        var theaters = new List<CultureTheaterProjection>();
        var visited = new HashSet<HexCoord>();
        foreach (var start in contestedCells.Keys.OrderBy(coord => coord.Q).ThenBy(coord => coord.R))
        {
            if (!visited.Add(start))
            {
                continue;
            }

            theaters.Add(BuildCultureTheater(
                planeCode,
                theaters.Count + 1,
                start,
                contestedCells,
                visited,
                ownerCultureResolver));
        }

        return theaters
            .OrderByDescending(theater => theater.ControlMismatchCellCount + theater.ContestedCellCount)
            .ThenByDescending(theater => theater.CellCount)
            .ToArray();
    }

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
    }

    // Forged cells above or below the surface never overwrite the surface hex
    // record; they only raise the column's forged tier/depth summary.
    private void UpdateForgedColumn(long sequence, int tick, long worldVersion, int planeCode, int q, int r, int z)
    {
        var existing = EnsureCell(sequence, tick, worldVersion, planeCode, q, r);
        var spireTiers = z > 0 ? Math.Max(existing.ForgedSpireTiers, z) : existing.ForgedSpireTiers;
        var tunnelDepth = z < 0 ? Math.Max(existing.ForgedTunnelDepth, -z) : existing.ForgedTunnelDepth;
        if (spireTiers == existing.ForgedSpireTiers && tunnelDepth == existing.ForgedTunnelDepth)
        {
            MarkProjected(sequence, tick, worldVersion);
            return;
        }

        GetSurface(planeCode).HexesById[existing.HexId] = existing with
        {
            ForgedSpireTiers = spireTiers,
            ForgedTunnelDepth = tunnelDepth,
            Tick = tick,
            WorldVersion = worldVersion,
            Sequence = sequence,
        };
        CellVersion++;
        MarkCellDirty(planeCode, existing.HexId);
        MarkProjected(sequence, tick, worldVersion);
    }

    protected override void OnCellInfluenceChanged(long sequence, in CellInfluenceChangedKernelEvent evt)
    {
        var existing = EnsureCell(sequence, evt.Tick, evt.WorldVersion, evt.PlaneCode, evt.Q, evt.R);

        var entityId = evt.EntityId;
        var influenceWasActive = existing.Influences.Any(influence => influence.EntityId == entityId);
        var currentInfluence = FixedScalar.FromUnits(evt.CurrentInfluenceUnits).ToSingle();
        var influenceIsActive = evt.CurrentInfluenceUnits > 0;
        var influences = UpsertInfluence(existing.Influences, entityId, currentInfluence, evt.Tick, sequence);
        GetSurface(evt.PlaneCode).HexesById[existing.HexId] = existing with
        {
            Influences = influences,
            Tick = evt.Tick,
            WorldVersion = evt.WorldVersion,
            Sequence = sequence,
        };
        UpdateOwnerColorMembership(entityId, influenceWasActive, influenceIsActive);
        CellVersion++;
        InfluenceOverlayVersion++;
        MarkCellDirty(evt.PlaneCode, existing.HexId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    public void SetCultureInfluencesFromLocality(
        int planeCode,
        HexCoord coord,
        IReadOnlyList<HexCultureInfluenceProjection> cultureInfluences,
        int tick,
        long worldVersion)
    {
        var existing = EnsureCell(
            existingSequence(planeCode, coord),
            tick,
            worldVersion,
            planeCode,
            coord.Q,
            coord.R);
        var ordered = cultureInfluences
            .Where(influence => influence.Share > 0f || influence.RawMass > 0f)
            .OrderByDescending(influence => influence.Share)
            .ThenByDescending(influence => influence.RawMass)
            .ThenBy(influence => influence.CultureId)
            .ToArray();
        if (CultureInfluencesEqual(existing.CultureInfluences, ordered))
        {
            return;
        }

        GetSurface(planeCode).HexesById[existing.HexId] = existing with
        {
            CultureInfluences = ordered,
            Tick = tick,
            WorldVersion = worldVersion,
            Sequence = existing.Sequence,
        };
        CellVersion++;
        CultureOverlayVersion++;
        MarkCellDirty(planeCode, existing.HexId);

        long existingSequence(int targetPlaneCode, HexCoord targetCoord) =>
            TryGetHex(targetPlaneCode, targetCoord, out var current) ? current.Sequence : 0;
    }

    protected override void OnMapCellStateChanged(long sequence, in MapCellStateChangedKernelEvent evt)
    {
        if (evt.Z != 0)
        {
            UpdateForgedColumn(sequence, evt.Tick, evt.WorldVersion, evt.PlaneCode, evt.Q, evt.R, evt.Z);
            return;
        }

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
            RoadQuality = KernelFixedPoint.RoadQualityValue(evt.CurrentQualityUnits),
            RoadTravelCount = evt.TravelCount,
            Tick = evt.Tick,
            WorldVersion = evt.WorldVersion,
            Sequence = sequence,
        };
        CellVersion++;
        RoadNetworkVersion++;
        MarkCellDirty(evt.PlaneCode, existing.HexId);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnCellTravelStructureChanged(long sequence, in CellTravelStructureChangedKernelEvent evt)
    {
        var existing = EnsureCell(sequence, evt.Tick, evt.WorldVersion, evt.PlaneCode, evt.Q, evt.R);
        GetSurface(evt.PlaneCode).HexesById[existing.HexId] = existing with
        {
            TravelStructureId = evt.CurrentTravelStructureId,
            Tick = evt.Tick,
            WorldVersion = evt.WorldVersion,
            Sequence = sequence,
        };
        CellVersion++;
        RouteNetworkVersion++;
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

        surface.HexesById[hexId] = CreateCellProjection(
            existing,
            hexId,
            planeCode,
            coord,
            terrainId,
            ownerEntityId,
            superregionId,
            continentId,
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

    private static HexProjection CreateCellProjection(
        HexProjection? existing,
        int hexId,
        int planeCode,
        HexCoord coord,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId,
        int tick,
        long worldVersion,
        long sequence)
    {
        var influences = Array.Empty<HexInfluenceProjection>();
        var cultureInfluences = Array.Empty<HexCultureInfluenceProjection>();
        var roadQuality = 0f;
        var roadTravelCount = 0;
        var travelStructureId = 0;
        if (existing is not null)
        {
            influences = [.. existing.Influences];
            cultureInfluences = [.. existing.CultureInfluences];
            roadQuality = existing.RoadQuality;
            roadTravelCount = existing.RoadTravelCount;
            travelStructureId = existing.TravelStructureId;
        }

        return new HexProjection(
            hexId,
            planeCode,
            coord,
            terrainId,
            ownerEntityId,
            superregionId,
            continentId,
            influences,
            cultureInfluences,
            roadQuality,
            roadTravelCount,
            travelStructureId,
            tick,
            worldVersion,
            sequence);
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

    private static bool CultureInfluencesEqual(
        IReadOnlyList<HexCultureInfluenceProjection> existing,
        HexCultureInfluenceProjection[] replacement)
    {
        if (existing.Count != replacement.Length)
        {
            return false;
        }

        for (var i = 0; i < existing.Count; i++)
        {
            var left = existing[i];
            var right = replacement[i];
            if (!CultureInfluenceCoreEqual(left, right) ||
                !CultureInfluenceForeignEqual(left, right) ||
                !CultureInfluenceBoundaryEqual(left, right))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CultureInfluenceCoreEqual(
        HexCultureInfluenceProjection left,
        HexCultureInfluenceProjection right) =>
        left.CultureId == right.CultureId &&
        FloatEqual(left.Share, right.Share) &&
        FloatEqual(left.RawMass, right.RawMass) &&
        FloatEqual(left.TotalRawMass, right.TotalRawMass);

    private static bool CultureInfluenceForeignEqual(
        HexCultureInfluenceProjection left,
        HexCultureInfluenceProjection right) =>
        left.ForeignCultureId == right.ForeignCultureId &&
        FloatEqual(left.ForeignShare, right.ForeignShare) &&
        FloatEqual(left.ForeignRawMass, right.ForeignRawMass);

    private static bool CultureInfluenceBoundaryEqual(
        HexCultureInfluenceProjection left,
        HexCultureInfluenceProjection right) =>
        left.CellKindCode == right.CellKindCode &&
        left.StrengthBandCode == right.StrengthBandCode;

    private static bool FloatEqual(float left, float right) =>
        Math.Abs(left - right) <= 0.0001f;

    private static bool CellHasCultureContest(
        HexProjection cell,
        Func<int, int> ownerCultureResolver)
    {
        if (cell.CultureInfluences.Count == 0)
        {
            return false;
        }

        var dominant = cell.CultureInfluences[0];
        if (dominant.CellKindCode == CultureFieldCellKindCodes.Contested)
        {
            return true;
        }

        if (dominant.CellKindCode != CultureFieldCellKindCodes.None)
        {
            return CellHasControlMismatch(cell, dominant.CultureId, ownerCultureResolver);
        }

        var runnerUp = cell.CultureInfluences.Count > 1 ? cell.CultureInfluences[1] : null;
        var margin = dominant.Share - (runnerUp?.Share ?? 0f);
        if (dominant.Share < 60f || margin < 25f)
        {
            return true;
        }

        return CellHasControlMismatch(cell, dominant.CultureId, ownerCultureResolver);
    }

    private static CultureTheaterProjection BuildCultureTheater(
        int planeCode,
        int theaterId,
        HexCoord start,
        Dictionary<HexCoord, HexProjection> contestedCells,
        HashSet<HexCoord> visited,
        Func<int, int> ownerCultureResolver)
    {
        var cells = CollectTheaterCells(start, contestedCells, visited);
        var dominantCultureId = MostCommonCulture(cells, static cell => cell.CultureInfluences[0].CultureId);
        var rivalCultureId = MostCommonCulture(cells, cell => RivalCultureId(cell, ownerCultureResolver));
        var stats = CultureTheaterStats.From(cells, ownerCultureResolver);

        return new CultureTheaterProjection(
            planeCode,
            theaterId,
            dominantCultureId,
            rivalCultureId,
            stats.Center,
            stats.CellCount,
            stats.ContestedCellCount,
            stats.ControlMismatchCellCount,
            stats.AverageDominantShare,
            stats.AverageMargin);
    }

    private static List<HexProjection> CollectTheaterCells(
        HexCoord start,
        Dictionary<HexCoord, HexProjection> contestedCells,
        HashSet<HexCoord> visited)
    {
        var queue = new Queue<HexCoord>();
        var cells = new List<HexProjection>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var coord = queue.Dequeue();
            if (!contestedCells.TryGetValue(coord, out var cell))
            {
                continue;
            }

            cells.Add(cell);
            EnqueueUnvisitedContestNeighbors(coord, contestedCells, visited, queue);
        }

        return cells;
    }

    private static void EnqueueUnvisitedContestNeighbors(
        HexCoord coord,
        Dictionary<HexCoord, HexProjection> contestedCells,
        HashSet<HexCoord> visited,
        Queue<HexCoord> queue)
    {
        foreach (var neighbor in coord.Neighbors())
        {
            if (contestedCells.ContainsKey(neighbor) && visited.Add(neighbor))
            {
                queue.Enqueue(neighbor);
            }
        }
    }

    private static int RivalCultureId(HexProjection cell, Func<int, int> ownerCultureResolver)
    {
        var dominantCultureId = cell.CultureInfluences[0].CultureId;
        var ownerCultureId = cell.OwnerEntityId > 0 ? ownerCultureResolver(cell.OwnerEntityId) : 0;
        if (ownerCultureId > 0 && ownerCultureId != dominantCultureId)
        {
            return ownerCultureId;
        }

        return cell.CultureInfluences.Count > 1 ? cell.CultureInfluences[1].CultureId : 0;
    }

    private static int MostCommonCulture(
        IEnumerable<HexProjection> cells,
        Func<HexProjection, int> selector) =>
        cells
            .Select(selector)
            .Where(cultureId => cultureId > 0)
            .GroupBy(cultureId => cultureId)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();

    private readonly record struct CultureTheaterStats(
        HexCoord Center,
        int CellCount,
        int ContestedCellCount,
        int ControlMismatchCellCount,
        float AverageDominantShare,
        float AverageMargin)
    {
        public static CultureTheaterStats From(
            IReadOnlyList<HexProjection> cells,
            Func<int, int> ownerCultureResolver)
        {
            var accumulator = new CultureTheaterStatsAccumulator();
            foreach (var cell in cells)
            {
                accumulator.Add(cell, ownerCultureResolver);
            }

            return accumulator.ToStats();
        }
    }

    private struct CultureTheaterStatsAccumulator
    {
        private int _contested;
        private int _mismatch;
        private float _dominantShareTotal;
        private float _marginTotal;
        private int _qTotal;
        private int _rTotal;
        private int _count;

        public void Add(HexProjection cell, Func<int, int> ownerCultureResolver)
        {
            var dominant = cell.CultureInfluences[0];
            var runnerUpShare = cell.CultureInfluences.Count > 1 ? cell.CultureInfluences[1].Share : 0f;
            var margin = dominant.Share - runnerUpShare;
            var contested = dominant.CellKindCode == CultureFieldCellKindCodes.None
                ? dominant.Share < 60f || margin < 25f
                : dominant.CellKindCode == CultureFieldCellKindCodes.Contested;
            _contested += contested ? 1 : 0;
            _mismatch += CellHasControlMismatch(cell, dominant.CultureId, ownerCultureResolver) ? 1 : 0;
            _dominantShareTotal += dominant.Share;
            _marginTotal += margin;
            _qTotal += cell.Coord.Q;
            _rTotal += cell.Coord.R;
            _count++;
        }

        public readonly CultureTheaterStats ToStats()
        {
            var q = (int)MathF.Round(_qTotal / (float)_count, MidpointRounding.AwayFromZero);
            var r = (int)MathF.Round(_rTotal / (float)_count, MidpointRounding.AwayFromZero);
            return new CultureTheaterStats(
                new HexCoord(q, r),
                _count,
                _contested,
                _mismatch,
                _dominantShareTotal / _count,
                _marginTotal / _count);
        }
    }

    private static bool CellHasControlMismatch(
        HexProjection cell,
        int dominantCultureId,
        Func<int, int> ownerCultureResolver)
    {
        var ownerCultureId = cell.OwnerEntityId > 0 ? ownerCultureResolver(cell.OwnerEntityId) : 0;
        return ownerCultureId > 0 && ownerCultureId != dominantCultureId;
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
