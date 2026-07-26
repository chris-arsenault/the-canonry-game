namespace Engine.Kernel;

/// <summary>
/// Radius-two culture facts and bounded dirty-cell tracking. Connected
/// components are deliberately absent from the runtime model.
/// </summary>
internal sealed class CultureLocality
{
    internal const int InteriorHoleAdjacentSupportThreshold = 4;

    private static readonly FixedScalar RingTwoConnectionWeight = FixedScalar.FromRatio(7, 20);
    private static readonly FixedScalar ContestMarginShare = 35;
    private const int CoordinateOffset = 32768;
    private const int CoordinateMask = 65535;
    private static readonly int[] NeighborQOffsets = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborROffsets = [0, -1, -1, 0, 1, 1];
    private static readonly int[] RingTwoQOffsets = [2, 2, 2, 1, 0, -1, -2, -2, -2, -1, 0, 1];
    private static readonly int[] RingTwoROffsets = [0, -1, -2, -2, -2, -1, 0, 1, 2, 2, 2, 1];

    private readonly CultureLocalityDirtyCell[] _dirtySources;
    private readonly long[] _dirtyLookupKeys;
    private readonly int[] _dirtyLookupIndexes;
    private readonly int[] _dirtyUsedBuckets;
    private readonly long[] _drainLookupKeys;
    private readonly int[] _drainLookupIndexes;
    private readonly int[] _drainUsedBuckets;
    private readonly CultureLocalityCacheEntry[] _cellCache;
    private readonly long[] _cellRevisions;
    private readonly int _dirtyLookupMask;
    private readonly int _drainLookupMask;
    private readonly int _cellCacheMask;
    private int _dirtySourceCount;
    private int _dirtyUsedBucketCount;
    private int _drainUsedBucketCount;
    private long _revision;

    public CultureLocality(int cellCapacity, int mapCellCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(cellCapacity);
        NumericGuard.ThrowIfNegativeOrZero(mapCellCapacity);
        _dirtySources = new CultureLocalityDirtyCell[cellCapacity];
        var lookupCapacity = NextPowerOfTwo(cellCapacity * 2);
        _dirtyLookupKeys = new long[lookupCapacity];
        _dirtyLookupIndexes = new int[lookupCapacity];
        _dirtyUsedBuckets = new int[lookupCapacity];
        _drainLookupKeys = new long[lookupCapacity];
        _drainLookupIndexes = new int[lookupCapacity];
        _drainUsedBuckets = new int[lookupCapacity];
        var cellCacheCapacity = NextPowerOfTwo(cellCapacity);
        _cellCache = new CultureLocalityCacheEntry[cellCacheCapacity];
        _cellRevisions = new long[mapCellCapacity + 1];
        _dirtyLookupMask = lookupCapacity - 1;
        _drainLookupMask = lookupCapacity - 1;
        _cellCacheMask = cellCacheCapacity - 1;
    }

    internal long Revision => _revision;

    internal void Invalidate()
    {
        _revision++;
        global::System.Array.Clear(_cellCache);
        global::System.Array.Clear(_cellRevisions);
        ResetDirtySources();
    }

    internal void MarkDirty(WorldState world, int planeCode, int q, int r)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        ValidatePlane(planeCode);
        _revision++;
        InvalidateRadiusTwo(world, planeCode, q, r);
        var key = Pack(planeCode, q, r);
        if (!TryAdd(key, _dirtyLookupKeys, _dirtyLookupIndexes, _dirtyUsedBuckets, _dirtyLookupMask, ref _dirtyUsedBucketCount))
        {
            return;
        }

        if (_dirtySourceCount >= _dirtySources.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _dirtySources[_dirtySourceCount] = new CultureLocalityDirtyCell(planeCode, q, r);
        _dirtySourceCount++;
    }

    internal int DrainDirtyCells(
        WorldState world,
        int planeCode,
        CultureLocalityDirtyCell[] destination)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        global::System.ArgumentNullException.ThrowIfNull(destination);
        ValidatePlane(planeCode);
        ResetLookup(_drainLookupIndexes, _drainUsedBuckets, ref _drainUsedBucketCount);
        var destinationCount = 0;
        var retainedCount = 0;
        ResetLookup(_dirtyLookupIndexes, _dirtyUsedBuckets, ref _dirtyUsedBucketCount);
        for (var i = 0; i < _dirtySourceCount; i++)
        {
            var source = _dirtySources[i];
            if (source.PlaneCode != planeCode)
            {
                _dirtySources[retainedCount] = source;
                AddExisting(
                    Pack(source.PlaneCode, source.Q, source.R),
                    _dirtyLookupKeys,
                    _dirtyLookupIndexes,
                    _dirtyUsedBuckets,
                    _dirtyLookupMask,
                    ref _dirtyUsedBucketCount);
                retainedCount++;
                continue;
            }

            AddRadiusTwo(world, source, destination, ref destinationCount);
        }

        _dirtySourceCount = retainedCount;
        ResetLookup(_drainLookupIndexes, _drainUsedBuckets, ref _drainUsedBucketCount);
        return destinationCount;
    }

    internal CultureLocalityCell CellAt(WorldState world, int cultureId, int planeCode, int q, int r)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        NumericGuard.ThrowIfNegativeOrZero(cultureId);
        ValidatePlane(planeCode);
        var revision = LocalityRevision(world, planeCode, q, r);
        var cacheBucket = CellCacheBucket(cultureId, planeCode, q, r);
        ref var cacheEntry = ref _cellCache[cacheBucket];
        if (cacheEntry.Revision == revision &&
            cacheEntry.CultureId == cultureId &&
            cacheEntry.PlaneCode == planeCode &&
            cacheEntry.Q == q &&
            cacheEntry.R == r)
        {
            return cacheEntry.Cell;
        }

        var cell = ComputeCell(world, cultureId, planeCode, q, r);
        cacheEntry = new CultureLocalityCacheEntry(
            revision,
            cultureId,
            planeCode,
            q,
            r,
            cell);
        return cell;
    }

    private long LocalityRevision(WorldState world, int planeCode, int q, int r)
    {
        return world.TryGetCellId(planeCode, q, r, z: 0, out var cellId)
            ? _cellRevisions[cellId]
            : _revision;
    }

    private void InvalidateRadiusTwo(WorldState world, int planeCode, int sourceQ, int sourceR)
    {
        const int radius = 2;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                if (world.TryGetCellId(
                        planeCode,
                        sourceQ + dq,
                        sourceR + dr,
                        z: 0,
                        out var cellId))
                {
                    _cellRevisions[cellId] = _revision;
                }
            }
        }
    }

    private static CultureLocalityCell ComputeCell(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        CultureMassAt(
            world,
            cultureId,
            planeCode,
            q,
            r,
            out var ownRaw,
            out var totalRaw,
            out var foreignCultureId,
            out var foreignRaw);
        var ownShare = ShareOf(ownRaw, totalRaw);
        var foreignShare = ShareOf(foreignRaw, totalRaw);
        NeighborhoodAt(
            world,
            cultureId,
            planeCode,
            q,
            r,
            out var adjacentRaw,
            out var ringTwoRaw,
            out var strongestAdjacent,
            out var strongestRingTwo,
            out var supportedAdjacent,
            out var supportedRingTwo);
        var localRaw = ownRaw + adjacentRaw;
        var connectedRaw = localRaw + (ringTwoRaw * RingTwoConnectionWeight);
        return new CultureLocalityCell(
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
            FixedScalar.Max(0, WorldState.CultureFullRawMass - ownRaw),
            supportedAdjacent,
            supportedRingTwo,
            strongestAdjacent,
            ringTwoRaw,
            strongestRingTwo,
            CellKind(ownRaw, ownShare, foreignRaw, foreignShare, supportedAdjacent, connectedRaw),
            WorldState.CultureFieldStrengthBand(ownRaw));
    }

    internal bool IsInteriorHoleAt(WorldState world, int cultureId, int planeCode, int q, int r) =>
        CellAt(world, cultureId, planeCode, q, r).IsInteriorHole;

    private void AddRadiusTwo(
        WorldState world,
        CultureLocalityDirtyCell source,
        CultureLocalityDirtyCell[] destination,
        ref int destinationCount)
    {
        const int radius = 2;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                var q = source.Q + dq;
                var r = source.R + dr;
                if (!world.CellIsKnown(source.PlaneCode, q, r))
                {
                    continue;
                }

                var key = Pack(source.PlaneCode, q, r);
                if (!TryAdd(
                        key,
                        _drainLookupKeys,
                        _drainLookupIndexes,
                        _drainUsedBuckets,
                        _drainLookupMask,
                        ref _drainUsedBucketCount))
                {
                    continue;
                }

                if (destinationCount >= destination.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                destination[destinationCount] = new CultureLocalityDirtyCell(source.PlaneCode, q, r);
                destinationCount++;
            }
        }
    }

    private static void NeighborhoodAt(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        out FixedScalar adjacentRaw,
        out FixedScalar ringTwoRaw,
        out FixedScalar strongestAdjacent,
        out FixedScalar strongestRingTwo,
        out int supportedAdjacent,
        out int supportedRingTwo)
    {
        adjacentRaw = 0;
        ringTwoRaw = 0;
        strongestAdjacent = 0;
        strongestRingTwo = 0;
        supportedAdjacent = 0;
        supportedRingTwo = 0;
        for (var i = 0; i < NeighborQOffsets.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + NeighborQOffsets[i],
                r + NeighborROffsets[i]);
            adjacentRaw += raw;
            strongestAdjacent = FixedScalar.Max(strongestAdjacent, raw);
            supportedAdjacent += raw >= WorldState.CultureClaimedRawMass ? 1 : 0;
        }

        for (var i = 0; i < RingTwoQOffsets.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + RingTwoQOffsets[i],
                r + RingTwoROffsets[i]);
            ringTwoRaw += raw;
            strongestRingTwo = FixedScalar.Max(strongestRingTwo, raw);
            supportedRingTwo += raw >= WorldState.CultureClaimedRawMass ? 1 : 0;
        }
    }

    private static void CultureMassAt(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        out FixedScalar ownRaw,
        out FixedScalar totalRaw,
        out int foreignCultureId,
        out FixedScalar foreignRaw)
    {
        ownRaw = 0;
        totalRaw = 0;
        foreignCultureId = 0;
        foreignRaw = 0;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            var raw = world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            totalRaw += raw;
            if (otherCultureId == cultureId)
            {
                ownRaw = raw;
                continue;
            }

            if (raw <= foreignRaw)
            {
                continue;
            }

            foreignCultureId = otherCultureId;
            foreignRaw = raw;
        }
    }

    private static FixedScalar ShareOf(FixedScalar raw, FixedScalar totalRaw) =>
        totalRaw > 0 ? raw * 100 / totalRaw : 0;
    private static int CellKind(
        FixedScalar ownRaw,
        FixedScalar ownShare,
        FixedScalar foreignRaw,
        FixedScalar foreignShare,
        int supportedAdjacent,
        FixedScalar connectedRaw)
    {
        if (IsEmpty(ownRaw, ownShare, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (IsContested(ownRaw, ownShare, foreignRaw, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (ownRaw >= WorldState.CultureControlledRawMass && supportedAdjacent >= 5)
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (ownRaw >= WorldState.CultureTraceRawMass && supportedAdjacent == 0)
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return ownRaw >= WorldState.CultureTraceRawMass || supportedAdjacent > 0
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool IsEmpty(FixedScalar ownRaw, FixedScalar ownShare, FixedScalar connectedRaw) =>
        ownRaw < WorldState.CultureTraceRawMass &&
        ownShare <= 0 &&
        connectedRaw < WorldState.CultureClaimedRawMass;

    private static bool IsContested(FixedScalar ownRaw, FixedScalar ownShare, FixedScalar foreignRaw, FixedScalar foreignShare) =>
        ownRaw >= WorldState.CultureTraceRawMass &&
        foreignRaw >= WorldState.CultureClaimedRawMass &&
        ownShare - foreignShare < ContestMarginShare;

    private void ResetDirtySources()
    {
        ResetLookup(_dirtyLookupIndexes, _dirtyUsedBuckets, ref _dirtyUsedBucketCount);
        _dirtySourceCount = 0;
    }

    private static bool TryAdd(
        long key,
        long[] keys,
        int[] indexes,
        int[] usedBuckets,
        int mask,
        ref int usedBucketCount)
    {
        var bucket = Bucket(key, mask);
        for (var probe = 0; probe < indexes.Length; probe++)
        {
            if (indexes[bucket] == 0)
            {
                AddExisting(key, keys, indexes, usedBuckets, mask, ref usedBucketCount);
                return true;
            }

            if (keys[bucket] == key)
            {
                return false;
            }

            bucket = (bucket + 1) & mask;
        }

        throw new global::System.InvalidOperationException();
    }

    private static void AddExisting(
        long key,
        long[] keys,
        int[] indexes,
        int[] usedBuckets,
        int mask,
        ref int usedBucketCount)
    {
        var bucket = Bucket(key, mask);
        while (indexes[bucket] != 0)
        {
            bucket = (bucket + 1) & mask;
        }

        keys[bucket] = key;
        indexes[bucket] = 1;
        usedBuckets[usedBucketCount] = bucket;
        usedBucketCount++;
    }

    private static void ResetLookup(int[] indexes, int[] usedBuckets, ref int usedBucketCount)
    {
        for (var i = 0; i < usedBucketCount; i++)
        {
            indexes[usedBuckets[i]] = 0;
        }

        usedBucketCount = 0;
    }

    private static long Pack(int planeCode, int q, int r) =>
        ((long)planeCode << 32) |
        (uint)(((q + CoordinateOffset) << 16) | ((r + CoordinateOffset) & CoordinateMask));

    private static int Bucket(long key, int mask)
    {
        var hash = (ulong)key;
        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        return (int)(hash & (uint)mask);
    }

    private int CellCacheBucket(int cultureId, int planeCode, int q, int r)
    {
        var hash = (ulong)Pack(planeCode, q, r);
        hash ^= unchecked((uint)cultureId * 0x9E3779B9U);
        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        return (int)(hash & (uint)_cellCacheMask);
    }

    private static int NextPowerOfTwo(int value)
    {
        var result = 1;
        while (result < value)
        {
            result <<= 1;
        }

        return result;
    }

    private static void ValidatePlane(int planeCode)
    {
        if (planeCode != PlaneCodes.Physical && planeCode != PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private readonly record struct CultureLocalityCacheEntry(
        long Revision,
        int CultureId,
        int PlaneCode,
        int Q,
        int R,
        CultureLocalityCell Cell);
}
