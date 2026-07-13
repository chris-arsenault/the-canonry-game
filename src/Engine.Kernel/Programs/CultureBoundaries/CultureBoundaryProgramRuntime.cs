namespace Engine.Kernel;

internal sealed class CultureBoundaryProgramRuntime :
    IKernelProgramRuntime<CultureBoundaryProgramData, CultureBoundaryProgramState>
{
    internal const int InteriorHoleAdjacentSupportThreshold = 4;

    private const int CacheSlotCount = 16;
    private const int DirtyCoordinateOffset = 32768;
    private const int DirtyCoordinateMask = 65535;
    private const int DirtyLocalDependencyRadius = 2;
    private const int DirtyComponentDependencyRadius = 1;
    private const float CultureFieldRingTwoConnectionWeight = 7f / 20f;
    private const float CultureFieldContestMarginShare = 35f;
    private static readonly int[] NeighborQOffsets = [1, 1, 0, -1, -1, 0];
    private static readonly int[] NeighborROffsets = [0, -1, -1, 0, 1, 1];
    private static readonly int[] RingTwoQOffsets = [2, 2, 2, 1, 0, -1, -2, -2, -2, -1, 0, 1];
    private static readonly int[] RingTwoROffsets = [0, -1, -2, -2, -2, -1, 0, 1, 2, 2, 2, 1];

    private readonly CultureBoundaryComponentCache[] _caches;
    private readonly int[] _physicalVersions;
    private readonly int[] _semanticVersions;
    private readonly CultureBoundaryDirtySource[] _dirtySources;
    private readonly long[] _dirtySourceLookupKeys;
    private readonly int[] _dirtySourceLookupIndexes;
    private readonly int[] _dirtySourceUsedBuckets;
    private readonly long[] _dirtyCellLookupKeys;
    private readonly int[] _dirtyCellLookupIndexes;
    private readonly int[] _dirtyCellUsedBuckets;
    private readonly int[] _dirtyComponentIdScratch = new int[7];
    private readonly int _dirtySourceLookupMask;
    private readonly int _dirtyCellLookupMask;
    private int _dirtySourceCount;
    private int _dirtySourceUsedBucketCount;
    private int _dirtyCellCount;
    private int _dirtyCellUsedBucketCount;
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

        // Raw culture influence is one aggregate fixed-capacity table across
        // every culture and plane, so it also bounds the number of distinct
        // surface keys that can be dirty at once.
        var dirtySourceCapacity = cellCapacity;
        _dirtySources = new CultureBoundaryDirtySource[dirtySourceCapacity];
        var dirtySourceLookupCapacity = NextPowerOfTwo(dirtySourceCapacity * 2);
        _dirtySourceLookupKeys = new long[dirtySourceLookupCapacity];
        _dirtySourceLookupIndexes = new int[dirtySourceLookupCapacity];
        _dirtySourceUsedBuckets = new int[dirtySourceLookupCapacity];
        _dirtySourceLookupMask = dirtySourceLookupCapacity - 1;
        var dirtyCellLookupCapacity = NextPowerOfTwo(cellCapacity * 2);
        _dirtyCellLookupKeys = new long[dirtyCellLookupCapacity];
        _dirtyCellLookupIndexes = new int[dirtyCellLookupCapacity];
        _dirtyCellUsedBuckets = new int[dirtyCellLookupCapacity];
        _dirtyCellLookupMask = dirtyCellLookupCapacity - 1;
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

    internal void MarkDirty(
        int cultureId,
        int planeCode,
        int q,
        int r,
        float previousRaw,
        float currentRaw,
        bool topologyMayHaveChanged)
    {
        ValidateCultureAndPlane(cultureId, planeCode);
        if (topologyMayHaveChanged)
        {
            MarkDirty(cultureId, planeCode);
        }
        else
        {
            ApplyStableMassChange(cultureId, planeCode, q, r, previousRaw, currentRaw);
            _revision++;
        }

        var key = PackDirtySourceKey(cultureId, planeCode, q, r);
        var index = DirtySourceIndexOf(key);
        if (index >= 0)
        {
            if (topologyMayHaveChanged && !_dirtySources[index].TopologyMayHaveChanged)
            {
                _dirtySources[index] = _dirtySources[index] with { TopologyMayHaveChanged = true };
            }

            return;
        }

        if (_dirtySourceCount >= _dirtySources.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _dirtySources[_dirtySourceCount] = new CultureBoundaryDirtySource(
            key,
            cultureId,
            planeCode,
            q,
            r,
            topologyMayHaveChanged);
        AddDirtySourceLookup(key, _dirtySourceCount);
        _dirtySourceCount++;
    }

    private void ApplyStableMassChange(int cultureId, int planeCode, int q, int r, float previousRaw, float currentRaw)
    {
        var cache = _caches[CacheIndex(cultureId, planeCode)];
        if (cache.CultureId != cultureId ||
            cache.PlaneCode != planeCode ||
            cache.BoundaryVersion != VersionFor(cultureId, planeCode))
        {
            return;
        }

        cache.ApplyStableMassChange(q, r, previousRaw, currentRaw);
    }

    internal int DrainDirtyCells(
        WorldState world,
        int planeCode,
        CultureBoundaryDirtyCell[] destination)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        global::System.ArgumentNullException.ThrowIfNull(destination);
        if (planeCode != PlaneCodes.Physical && planeCode != PlaneCodes.Semantic)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }

        ResetDirtyCellLookup();
        ResetDirtySourceLookup();
        var originalSourceCount = _dirtySourceCount;
        var remainingSourceCount = 0;
        for (var i = 0; i < originalSourceCount; i++)
        {
            var source = _dirtySources[i];
            if (source.PlaneCode != planeCode)
            {
                _dirtySources[remainingSourceCount] = source;
                AddDirtySourceLookup(source.Key, remainingSourceCount);
                remainingSourceCount++;
                continue;
            }

            AddDirtyCellsInRadius(
                world,
                source.PlaneCode,
                source.Q,
                source.R,
                DirtyLocalDependencyRadius,
                destination);
            if (source.TopologyMayHaveChanged)
            {
                AddAffectedComponentCells(world, source, destination);
            }
        }

        _dirtySourceCount = remainingSourceCount;
        var dirtyCellCount = _dirtyCellCount;
        ResetDirtyCellLookup();
        return dirtyCellCount;
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
        for (var i = 0; i < NeighborQOffsets.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + NeighborQOffsets[i],
                r + NeighborROffsets[i]);
            adjacentRawTotal += raw;
            strongestAdjacentRaw = global::System.MathF.Max(strongestAdjacentRaw, raw);
            supportedAdjacentCount += CultureFieldSupportedCellCount(raw);
        }

        for (var i = 0; i < RingTwoQOffsets.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + RingTwoQOffsets[i],
                r + RingTwoROffsets[i]);
            ringTwoRawTotal += raw;
            strongestRingTwoRaw = global::System.MathF.Max(strongestRingTwoRaw, raw);
            supportedRingTwoCount += CultureFieldSupportedCellCount(raw);
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

    private void AddAffectedComponentCells(
        WorldState world,
        CultureBoundaryDirtySource source,
        CultureBoundaryDirtyCell[] destination)
    {
        var cache = EnsureCache(world, source.CultureId, source.PlaneCode);
        var componentCount = 0;
        AddComponentId(cache.ComponentIdAt(source.Q, source.R), _dirtyComponentIdScratch, ref componentCount);
        for (var direction = 0; direction < 6; direction++)
        {
            AddComponentId(
                cache.ComponentIdAt(
                    source.Q + DirectionQ(direction),
                    source.R + DirectionR(direction)),
                _dirtyComponentIdScratch,
                ref componentCount);
        }

        for (var i = 0; i < componentCount; i++)
        {
            cache.ForEachComponentCell(
                _dirtyComponentIdScratch[i],
                (q, r) => AddDirtyCellsInRadius(
                    world,
                    source.PlaneCode,
                    q,
                    r,
                    DirtyComponentDependencyRadius,
                    destination));
        }
    }

    private static void AddComponentId(int componentId, int[] componentIds, ref int componentCount)
    {
        if (componentId <= 0)
        {
            return;
        }

        for (var i = 0; i < componentCount; i++)
        {
            if (componentIds[i] == componentId)
            {
                return;
            }
        }

        componentIds[componentCount] = componentId;
        componentCount++;
    }

    private void AddDirtyCellsInRadius(
        WorldState world,
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        CultureBoundaryDirtyCell[] destination)
    {
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                AddDirtyCell(world, planeCode, centerQ + dq, centerR + dr, destination);
            }
        }
    }

    private void AddDirtyCell(
        WorldState world,
        int planeCode,
        int q,
        int r,
        CultureBoundaryDirtyCell[] destination)
    {
        if (!world.CellIsKnown(planeCode, q, r))
        {
            return;
        }

        if (!TryAddDirtyCellKey(PackDirtyCellKey(planeCode, q, r)))
        {
            return;
        }

        if (_dirtyCellCount >= destination.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        destination[_dirtyCellCount] = new CultureBoundaryDirtyCell(planeCode, q, r);
        _dirtyCellCount++;
    }

    private int DirtySourceIndexOf(long key)
    {
        var bucket = LookupBucket(key, _dirtySourceLookupMask);
        for (var probe = 0; probe < _dirtySourceLookupIndexes.Length; probe++)
        {
            var index = _dirtySourceLookupIndexes[bucket];
            if (index == 0)
            {
                return -1;
            }

            if (_dirtySourceLookupKeys[bucket] == key)
            {
                return index - 1;
            }

            bucket = (bucket + 1) & _dirtySourceLookupMask;
        }

        return -1;
    }

    private void AddDirtySourceLookup(long key, int sourceIndex)
    {
        var bucket = LookupBucket(key, _dirtySourceLookupMask);
        for (var probe = 0; probe < _dirtySourceLookupIndexes.Length; probe++)
        {
            if (_dirtySourceLookupIndexes[bucket] == 0)
            {
                if (_dirtySourceUsedBucketCount >= _dirtySourceUsedBuckets.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                _dirtySourceLookupKeys[bucket] = key;
                _dirtySourceLookupIndexes[bucket] = sourceIndex + 1;
                _dirtySourceUsedBuckets[_dirtySourceUsedBucketCount] = bucket;
                _dirtySourceUsedBucketCount++;
                return;
            }

            if (_dirtySourceLookupKeys[bucket] == key)
            {
                _dirtySourceLookupIndexes[bucket] = sourceIndex + 1;
                return;
            }

            bucket = (bucket + 1) & _dirtySourceLookupMask;
        }

        throw new global::System.InvalidOperationException();
    }

    private void ResetDirtySourceLookup()
    {
        for (var i = 0; i < _dirtySourceUsedBucketCount; i++)
        {
            _dirtySourceLookupIndexes[_dirtySourceUsedBuckets[i]] = 0;
        }

        _dirtySourceUsedBucketCount = 0;
    }

    private bool TryAddDirtyCellKey(long key)
    {
        var bucket = LookupBucket(key, _dirtyCellLookupMask);
        for (var probe = 0; probe < _dirtyCellLookupIndexes.Length; probe++)
        {
            var index = _dirtyCellLookupIndexes[bucket];
            if (index == 0)
            {
                if (_dirtyCellUsedBucketCount >= _dirtyCellUsedBuckets.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                _dirtyCellLookupKeys[bucket] = key;
                _dirtyCellLookupIndexes[bucket] = _dirtyCellCount + 1;
                _dirtyCellUsedBuckets[_dirtyCellUsedBucketCount] = bucket;
                _dirtyCellUsedBucketCount++;
                return true;
            }

            if (_dirtyCellLookupKeys[bucket] == key)
            {
                return false;
            }

            bucket = (bucket + 1) & _dirtyCellLookupMask;
        }

        throw new global::System.InvalidOperationException();
    }

    private void ResetDirtyCellLookup()
    {
        for (var i = 0; i < _dirtyCellUsedBucketCount; i++)
        {
            _dirtyCellLookupIndexes[_dirtyCellUsedBuckets[i]] = 0;
        }

        _dirtyCellUsedBucketCount = 0;
        _dirtyCellCount = 0;
    }

    private static long PackDirtySourceKey(int cultureId, int planeCode, int q, int r) =>
        ((long)cultureId << 34) | ((long)planeCode << 32) | (uint)PackDirtyCoordinateKey(q, r);

    private static long PackDirtyCellKey(int planeCode, int q, int r) =>
        ((long)planeCode << 32) | (uint)PackDirtyCoordinateKey(q, r);

    private static int PackDirtyCoordinateKey(int q, int r) =>
        ((q + DirtyCoordinateOffset) << 16) | ((r + DirtyCoordinateOffset) & DirtyCoordinateMask);

    private static int LookupBucket(long key, int mask)
    {
        var hash = (ulong)key;
        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        hash *= 0xc4ceb9fe1a85ec53UL;
        hash ^= hash >> 33;
        return (int)(hash & (uint)mask);
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

    private readonly record struct CultureBoundaryDirtySource(
        long Key,
        int CultureId,
        int PlaneCode,
        int Q,
        int R,
        bool TopologyMayHaveChanged);
}
