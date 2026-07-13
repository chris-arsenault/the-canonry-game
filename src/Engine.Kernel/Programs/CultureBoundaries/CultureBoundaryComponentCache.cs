namespace Engine.Kernel;

internal sealed class CultureBoundaryComponentCache
{
    private const int CoordinateOffset = 32768;
    private const int CoordinateMask = 65535;

    private readonly int[] _lookupKeys;
    private readonly int[] _lookupComponentIds;
    private readonly int[] _usedLookupBuckets;
    private readonly int[] _pendingKeys;
    private readonly int[] _componentCellCounts;
    private readonly float[] _componentRawMasses;
    private readonly float[] _componentScoreMasses;
    private readonly int _lookupMask;
    private int _usedLookupBucketCount;
    private int _pendingHead;
    private int _pendingTail;
    private int _componentCount;
    private int _largestComponentId;
    private int _largestComponentCellCount;
    private int _boundaryVersion = -1;
    private int _cultureId;
    private int _planeCode;

    public CultureBoundaryComponentCache(int cellCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        var lookupCapacity = NextPowerOfTwo(cellCapacity * 2);
        _lookupKeys = new int[lookupCapacity];
        _lookupComponentIds = new int[lookupCapacity];
        _usedLookupBuckets = new int[cellCapacity];
        _pendingKeys = new int[cellCapacity];
        _componentCellCounts = new int[cellCapacity + 1];
        _componentRawMasses = new float[cellCapacity + 1];
        _componentScoreMasses = new float[cellCapacity + 1];
        _lookupMask = lookupCapacity - 1;
    }

    public int CultureId => _cultureId;

    public int PlaneCode => _planeCode;

    public int BoundaryVersion => _boundaryVersion;

    public int ComponentCount => _componentCount;

    public int LargestComponentId => _largestComponentId;

    public int LargestComponentCellCount => _largestComponentCellCount;

    public void Invalidate()
    {
        _boundaryVersion = -1;
    }

    public void Build(WorldState world, int cultureId, int planeCode, int boundaryVersion)
    {
        global::System.ArgumentNullException.ThrowIfNull(world);
        Clear();
        _cultureId = cultureId;
        _planeCode = planeCode;
        var cursor = world.CultureInfluenceCells(cultureId, planeCode);
        while (world.MoveNextCultureInfluenceCell(ref cursor, out var q, out var r, out _))
        {
            if (!CellSupported(world, cultureId, planeCode, q, r))
            {
                continue;
            }

            var key = Pack(q, r);
            if (ComponentIdAt(key) > 0)
            {
                continue;
            }

            BuildComponent(world, cultureId, planeCode, key);
        }

        _boundaryVersion = boundaryVersion;
    }

    public int ComponentIdAt(int q, int r) => ComponentIdAt(Pack(q, r));

    public CultureBoundaryComponent ComponentAt(int cultureId, int planeCode, int componentId)
    {
        if (componentId <= 0 || componentId > _componentCount)
        {
            return new CultureBoundaryComponent(cultureId, planeCode, 0, 0, 0f, 0f, false);
        }

        return new CultureBoundaryComponent(
            cultureId,
            planeCode,
            componentId,
            _componentCellCounts[componentId],
            _componentRawMasses[componentId],
            _componentScoreMasses[componentId],
            componentId == _largestComponentId);
    }

    public void ApplyStableMassChange(int q, int r, float previousRaw, float currentRaw)
    {
        if (previousRaw < WorldState.CultureClaimedRawMass ||
            currentRaw < WorldState.CultureClaimedRawMass)
        {
            return;
        }

        var componentId = ComponentIdAt(q, r);
        if (componentId <= 0 || componentId > _componentCount)
        {
            return;
        }

        _componentRawMasses[componentId] += currentRaw - previousRaw;
        _componentScoreMasses[componentId] += CellScoreMass(currentRaw) - CellScoreMass(previousRaw);
    }

    public CultureBoundaryAttachment AttachmentAt(int q, int r)
    {
        var attached = default(AttachedComponentSet);
        var componentCount = 0;
        var cellCount = 0;
        var rawMass = 0f;
        var scoreMass = 0f;
        var centerComponentId = ComponentIdAt(q, r);
        if (centerComponentId > 0)
        {
            AddAttachedComponent(
                centerComponentId,
                ref attached,
                ref componentCount,
                ref cellCount,
                ref rawMass,
                ref scoreMass);
        }

        for (var direction = 0; direction < 6; direction++)
        {
            var componentId = ComponentIdAt(
                q + DirectionQ(direction),
                r + DirectionR(direction));
            if (componentId <= 0)
            {
                continue;
            }

            AddAttachedComponent(
                componentId,
                ref attached,
                ref componentCount,
                ref cellCount,
                ref rawMass,
                ref scoreMass);
        }

        return new CultureBoundaryAttachment(componentCount, cellCount, rawMass, scoreMass);
    }

    public void ForEachComponentCell(int componentId, ComponentCellVisitor visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        if (componentId <= 0 || componentId > _componentCount)
        {
            return;
        }

        for (var i = 0; i < _usedLookupBucketCount; i++)
        {
            var bucket = _usedLookupBuckets[i];
            if (_lookupComponentIds[bucket] != componentId)
            {
                continue;
            }

            var key = _lookupKeys[bucket];
            visitor(UnpackQ(key), UnpackR(key));
        }
    }

    private void Clear()
    {
        for (var i = 0; i < _usedLookupBucketCount; i++)
        {
            _lookupComponentIds[_usedLookupBuckets[i]] = 0;
        }

        if (_componentCount > 0)
        {
            global::System.Array.Clear(_componentCellCounts, 1, _componentCount);
            global::System.Array.Clear(_componentRawMasses, 1, _componentCount);
            global::System.Array.Clear(_componentScoreMasses, 1, _componentCount);
        }

        _usedLookupBucketCount = 0;
        _componentCount = 0;
        _largestComponentId = 0;
        _largestComponentCellCount = 0;
    }

    private void BuildComponent(WorldState world, int cultureId, int planeCode, int startKey)
    {
        _componentCount++;
        var componentId = _componentCount;
        AddLookup(startKey, componentId);
        _pendingHead = 0;
        _pendingTail = 0;
        Enqueue(startKey);

        while (TryDequeue(out var key))
        {
            var q = UnpackQ(key);
            var r = UnpackR(key);
            var raw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
            _componentCellCounts[componentId]++;
            _componentRawMasses[componentId] += raw;
            _componentScoreMasses[componentId] += CellScoreMass(raw);
            for (var direction = 0; direction < 6; direction++)
            {
                TryAddNeighbor(world, cultureId, planeCode, q, r, direction, componentId);
            }
        }

        if (_componentCellCounts[componentId] > _largestComponentCellCount)
        {
            _largestComponentId = componentId;
            _largestComponentCellCount = _componentCellCounts[componentId];
        }
    }

    private void TryAddNeighbor(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        int direction,
        int componentId)
    {
        var neighborQ = q + DirectionQ(direction);
        var neighborR = r + DirectionR(direction);
        if (!CellSupported(world, cultureId, planeCode, neighborQ, neighborR))
        {
            return;
        }

        var key = Pack(neighborQ, neighborR);
        if (ComponentIdAt(key) > 0)
        {
            return;
        }

        AddLookup(key, componentId);
        Enqueue(key);
    }

    private bool CellSupported(WorldState world, int cultureId, int planeCode, int q, int r) =>
        world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r) >= WorldState.CultureClaimedRawMass;

    private void Enqueue(int key)
    {
        if (_pendingTail >= _pendingKeys.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _pendingKeys[_pendingTail] = key;
        _pendingTail++;
    }

    private bool TryDequeue(out int key)
    {
        if (_pendingHead >= _pendingTail)
        {
            key = 0;
            return false;
        }

        key = _pendingKeys[_pendingHead];
        _pendingHead++;
        return true;
    }

    private int ComponentIdAt(int key)
    {
        var bucket = LookupBucket(key);
        for (var probe = 0; probe < _lookupKeys.Length; probe++)
        {
            var componentId = _lookupComponentIds[bucket];
            if (componentId == 0)
            {
                return 0;
            }

            if (_lookupKeys[bucket] == key)
            {
                return componentId;
            }

            bucket = (bucket + 1) & _lookupMask;
        }

        return 0;
    }

    private void AddLookup(int key, int componentId)
    {
        var bucket = LookupBucket(key);
        for (var probe = 0; probe < _lookupKeys.Length; probe++)
        {
            if (_lookupComponentIds[bucket] == 0)
            {
                if (_usedLookupBucketCount >= _usedLookupBuckets.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                _lookupKeys[bucket] = key;
                _lookupComponentIds[bucket] = componentId;
                _usedLookupBuckets[_usedLookupBucketCount] = bucket;
                _usedLookupBucketCount++;
                return;
            }

            if (_lookupKeys[bucket] == key)
            {
                _lookupComponentIds[bucket] = componentId;
                return;
            }

            bucket = (bucket + 1) & _lookupMask;
        }

        throw new global::System.InvalidOperationException();
    }

    private void AddAttachedComponent(
        int componentId,
        ref AttachedComponentSet attached,
        ref int componentCount,
        ref int cellCount,
        ref float rawMass,
        ref float scoreMass)
    {
        if (!attached.TryAdd(componentId))
        {
            return;
        }

        componentCount++;
        cellCount += _componentCellCounts[componentId];
        rawMass += _componentRawMasses[componentId];
        scoreMass += _componentScoreMasses[componentId];
    }

    private struct AttachedComponentSet
    {
        private int _first;
        private int _second;
        private int _third;
        private int _fourth;
        private int _fifth;
        private int _sixth;
        private int _seventh;
        private int _count;

        internal bool TryAdd(int componentId)
        {
            for (var index = 0; index < _count; index++)
            {
                if (At(index) == componentId)
                {
                    return false;
                }
            }

            SetAt(_count, componentId);
            _count++;
            return true;
        }

        private readonly int At(int index) => index switch
        {
            0 => _first,
            1 => _second,
            2 => _third,
            3 => _fourth,
            4 => _fifth,
            5 => _sixth,
            _ => _seventh,
        };

        private void SetAt(int index, int value)
        {
            switch (index)
            {
                case 0: _first = value; break;
                case 1: _second = value; break;
                case 2: _third = value; break;
                case 3: _fourth = value; break;
                case 4: _fifth = value; break;
                case 5: _sixth = value; break;
                default: _seventh = value; break;
            }
        }
    }

    private int LookupBucket(int key)
    {
        var hash = (uint)key;
        hash ^= hash >> 16;
        hash *= 2246822519u;
        hash ^= hash >> 13;
        hash *= 3266489917u;
        hash ^= hash >> 16;
        return (int)(hash & (uint)_lookupMask);
    }

    private static int Pack(int q, int r) =>
        ((q + CoordinateOffset) << 16) | ((r + CoordinateOffset) & CoordinateMask);

    private static int UnpackQ(int key) =>
        (int)(((uint)key >> 16) & CoordinateMask) - CoordinateOffset;

    private static int UnpackR(int key) =>
        (key & CoordinateMask) - CoordinateOffset;

    private static float CellScoreMass(float raw) =>
        Clamp01(raw / WorldState.CultureControlledRawMass);

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
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

    public delegate void ComponentCellVisitor(int q, int r);
}
