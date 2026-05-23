namespace Engine.Kernel;

internal sealed class CultureBoundaryProgramRuntime :
    IKernelProgramRuntime<CultureBoundaryProgramData, CultureBoundaryProgramState>
{
    internal const int InteriorHoleAdjacentSupportThreshold = 4;

    private const int CacheSlotCount = 16;
    private const float CultureFieldRingTwoConnectionWeight = 7f / 20f;
    private const float CultureFieldContestMarginShare = 35f;

    private readonly CultureBoundaryComponentCache[] _caches;
    private readonly int[] _physicalVersions;
    private readonly int[] _semanticVersions;
    private long _revision;
    private int _rebuildCount;

    public CultureBoundaryProgramRuntime(
        CultureBoundaryProgramData data,
        CultureBoundaryProgramState state,
        int cellCapacity,
        int cultureIdCapacity)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureIdCapacity);
        Data = data;
        State = state;
        _physicalVersions = new int[cultureIdCapacity + 1];
        _semanticVersions = new int[cultureIdCapacity + 1];
        _caches = new CultureBoundaryComponentCache[CacheSlotCount];
        for (var i = 0; i < _caches.Length; i++)
        {
            _caches[i] = new CultureBoundaryComponentCache(cellCapacity);
        }
    }

    public CultureBoundaryProgramData Data { get; }

    public CultureBoundaryProgramState State { get; }

    internal int RebuildCount => _rebuildCount;

    internal long Revision => _revision;

    public void Tick(Simulation simulation, int tick)
    {
        _ = simulation;
        _ = tick;
    }

    internal void Invalidate()
    {
        for (var i = 0; i < _caches.Length; i++)
        {
            _caches[i].Invalidate();
        }

        _revision++;
    }

    internal void MarkDirty(int cultureId, int planeCode)
    {
        IncrementVersion(cultureId, planeCode);
        for (var i = 0; i < _caches.Length; i++)
        {
            var cache = _caches[i];
            if (cache.CultureId == cultureId && cache.PlaneCode == planeCode)
            {
                cache.Invalidate();
            }
        }
    }

    internal CultureBoundaryCell CellAt(WorldState world, int cultureId, int planeCode, int q, int r)
    {
        var cache = EnsureCache(world, cultureId, planeCode);
        var ownRaw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var ownShare = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        var totalRaw = CultureRawTotalAt(world, planeCode, q, r);
        var foreignShare = StrongestOtherCultureShare(
            world,
            cultureId,
            planeCode,
            q,
            r,
            out var foreignCultureId,
            out var foreignRaw);
        var neighborhood = CultureFieldNeighborhoodAt(world, cultureId, planeCode, q, r);
        var localRaw = ownRaw + neighborhood.AdjacentRawTotal;
        var connectedRaw = localRaw + (neighborhood.RingTwoRawTotal * CultureFieldRingTwoConnectionWeight);
        var cellKindCode = CultureFieldCellKind(
            ownRaw,
            ownShare,
            foreignRaw,
            foreignShare,
            neighborhood.SupportedAdjacentCount,
            connectedRaw);
        var componentId = cache.ComponentIdAt(q, r);
        var component = cache.ComponentAt(cultureId, planeCode, componentId);
        var attachment = cache.AttachmentAt(q, r);
        return new CultureBoundaryCell(
            cultureId,
            planeCode,
            q,
            r,
            ownShare,
            ownRaw,
            totalRaw,
            foreignCultureId,
            foreignShare,
            foreignRaw,
            localRaw,
            connectedRaw,
            global::System.MathF.Max(0f, WorldState.CultureFullRawMass - ownRaw),
            neighborhood.SupportedAdjacentCount,
            neighborhood.SupportedRingTwoCount,
            neighborhood.StrongestAdjacentRaw,
            neighborhood.RingTwoRawTotal,
            neighborhood.StrongestRingTwoRaw,
            cellKindCode,
            WorldState.CultureFieldStrengthBand(ownRaw),
            componentId,
            component.CellCount,
            component.RawMass,
            component.ScoreMass,
            component.IsLargestComponent,
            attachment.ComponentCount,
            attachment.CellCount,
            attachment.RawMass,
            attachment.ScoreMass);
    }

    internal CultureBoundaryComponent ComponentAt(WorldState world, int cultureId, int planeCode, int q, int r)
    {
        var cache = EnsureCache(world, cultureId, planeCode);
        var componentId = cache.ComponentIdAt(q, r);
        return cache.ComponentAt(cultureId, planeCode, componentId);
    }

    internal int ComponentCount(WorldState world, int cultureId, int planeCode) =>
        EnsureCache(world, cultureId, planeCode).ComponentCount;

    internal int LargestComponentId(WorldState world, int cultureId, int planeCode) =>
        EnsureCache(world, cultureId, planeCode).LargestComponentId;

    internal bool IsInteriorHole(WorldState world, int cultureId, int planeCode, int q, int r) =>
        IsInteriorHoleAt(world, cultureId, planeCode, q, r);

    internal bool IsInteriorHoleAt(WorldState world, int cultureId, int planeCode, int q, int r)
    {
        var cache = EnsureCache(world, cultureId, planeCode);
        var rawMass = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        if (rawMass >= WorldState.CultureControlledRawMass)
        {
            return false;
        }

        var attachment = cache.AttachmentAt(q, r);
        return attachment.ComponentCount == 1 &&
            CountSupportedAdjacentCells(world, cultureId, planeCode, q, r) >= InteriorHoleAdjacentSupportThreshold &&
            attachment.ScoreMass > 0f;
    }

    private CultureBoundaryComponentCache EnsureCache(WorldState world, int cultureId, int planeCode)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        ValidateCultureAndPlane(cultureId, planeCode);
        var cache = _caches[CacheIndex(cultureId, planeCode)];
        var version = VersionFor(cultureId, planeCode);
        if (cache.CultureId != cultureId ||
            cache.PlaneCode != planeCode ||
            cache.BoundaryVersion != version)
        {
            cache.Build(world, cultureId, planeCode, version);
            _rebuildCount++;
        }

        return cache;
    }

    private int VersionFor(int cultureId, int planeCode) =>
        planeCode == PlaneCodes.Semantic
            ? _semanticVersions[ValidateCultureVersionIndex(cultureId)]
            : _physicalVersions[ValidateCultureVersionIndex(cultureId)];

    private void IncrementVersion(int cultureId, int planeCode)
    {
        ValidateCultureAndPlane(cultureId, planeCode);
        cultureId = ValidateCultureVersionIndex(cultureId);
        if (planeCode == PlaneCodes.Semantic)
        {
            unchecked
            {
                _semanticVersions[cultureId]++;
            }
        }
        else
        {
            unchecked
            {
                _physicalVersions[cultureId]++;
            }
        }

        _revision++;
    }

    private static CultureBoundaryNeighborhood CultureFieldNeighborhoodAt(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var adjacentRawTotal = 0f;
        var ringTwoRawTotal = 0f;
        var strongestAdjacentRaw = 0f;
        var strongestRingTwoRaw = 0f;
        var supportedAdjacentCount = 0;
        var supportedRingTwoCount = 0;
        for (var dq = -2; dq <= 2; dq++)
        {
            var minDr = global::System.Math.Max(-2, -dq - 2);
            var maxDr = global::System.Math.Min(2, -dq + 2);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                if (dq == 0 && dr == 0)
                {
                    continue;
                }

                var raw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q + dq, r + dr);
                if (HexDistance(0, 0, dq, dr) == 1)
                {
                    adjacentRawTotal += raw;
                    strongestAdjacentRaw = global::System.MathF.Max(strongestAdjacentRaw, raw);
                    supportedAdjacentCount += CultureFieldSupportedCellCount(raw);
                    continue;
                }

                ringTwoRawTotal += raw;
                strongestRingTwoRaw = global::System.MathF.Max(strongestRingTwoRaw, raw);
                supportedRingTwoCount += CultureFieldSupportedCellCount(raw);
            }
        }

        return new CultureBoundaryNeighborhood(
            adjacentRawTotal,
            ringTwoRawTotal,
            strongestAdjacentRaw,
            strongestRingTwoRaw,
            supportedAdjacentCount,
            supportedRingTwoCount);
    }

    private static float CultureRawTotalAt(WorldState world, int planeCode, int q, int r)
    {
        var total = 0f;
        for (var cultureId = 1; cultureId <= world.CultureHighWatermark; cultureId++)
        {
            total += world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        }

        return total;
    }

    private static int CountSupportedAdjacentCells(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var count = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + DirectionQ(direction),
                r + DirectionR(direction));
            count += CultureFieldSupportedCellCount(raw);
        }

        return count;
    }

    private static float StrongestOtherCultureShare(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        out int foreignCultureId,
        out float rawMass)
    {
        var strongestShare = 0f;
        foreignCultureId = 0;
        rawMass = 0f;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == cultureId)
            {
                continue;
            }

            var share = world.GetCellCultureInfluence(otherCultureId, planeCode, q, r);
            if (share > strongestShare)
            {
                strongestShare = share;
                foreignCultureId = otherCultureId;
                rawMass = world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            }
        }

        return strongestShare;
    }

    private static int CultureFieldCellKind(
        float ownRaw,
        float ownShare,
        float foreignRaw,
        float foreignShare,
        int supportedAdjacentCount,
        float connectedRaw)
    {
        if (CultureFieldIsEmpty(ownRaw, ownShare, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (CultureFieldIsContested(ownRaw, ownShare, foreignRaw, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (CultureFieldIsInterior(ownRaw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (CultureFieldIsIsolated(ownRaw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return CultureFieldIsBorder(ownRaw, supportedAdjacentCount)
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool CultureFieldIsEmpty(float ownRaw, float ownShare, float connectedRaw) =>
        ownRaw < WorldState.CultureTraceRawMass &&
        ownShare <= 0f &&
        connectedRaw < WorldState.CultureClaimedRawMass;

    private static bool CultureFieldIsContested(float ownRaw, float ownShare, float foreignRaw, float foreignShare) =>
        ownRaw >= WorldState.CultureTraceRawMass &&
        foreignRaw >= WorldState.CultureClaimedRawMass &&
        ownShare - foreignShare < CultureFieldContestMarginShare;

    private static bool CultureFieldIsInterior(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= WorldState.CultureControlledRawMass && supportedAdjacentCount >= 5;

    private static bool CultureFieldIsIsolated(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= WorldState.CultureTraceRawMass && supportedAdjacentCount == 0;

    private static bool CultureFieldIsBorder(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= WorldState.CultureTraceRawMass || supportedAdjacentCount > 0;

    private static int CultureFieldSupportedCellCount(float raw) =>
        raw >= WorldState.CultureClaimedRawMass ? 1 : 0;

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (global::System.Math.Abs(aq - bq) +
            global::System.Math.Abs(ar - br) +
            global::System.Math.Abs(asCoord - bsCoord)) / 2;
    }

    private static int CacheIndex(int cultureId, int planeCode)
    {
        var planeOffset = planeCode == PlaneCodes.Physical ? 0 : 1;
        unchecked
        {
            return (((cultureId - 1) * 2) + planeOffset) & (CacheSlotCount - 1);
        }
    }

    private static int DirectionQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int DirectionR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };

    private static void ValidateCultureAndPlane(int cultureId, int planeCode)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cultureId);
        if (planeCode != PlaneCodes.Physical && planeCode != PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private int ValidateCultureVersionIndex(int cultureId)
    {
        if ((uint)cultureId >= (uint)_physicalVersions.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(cultureId));
        }

        return cultureId;
    }

    private readonly record struct CultureBoundaryNeighborhood(
        float AdjacentRawTotal,
        float RingTwoRawTotal,
        float StrongestAdjacentRaw,
        float StrongestRingTwoRaw,
        int SupportedAdjacentCount,
        int SupportedRingTwoCount);
}
