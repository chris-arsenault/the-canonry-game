namespace Engine.Kernel;

internal readonly record struct CultureInfluenceAttachment(
    float ComponentMass,
    int AdjacentSupportedCount,
    int ComponentCount);

internal sealed class CultureInfluenceComponentScratch
{
    private const float SupportedRawInfluence = 12f;
    private const float StrongRawInfluence = 30f;
    private const int CoordinateOffset = 32768;
    private const int CoordinateMask = 65535;
    private const int CellCapacity = 4096;
    private const int ComponentCapacity = 4096;
    private const int LookupCapacity = 8192;
    private const int LookupMask = LookupCapacity - 1;

    private readonly int[] _cellKeys = new int[CellCapacity];
    private readonly int[] _cellComponentIds = new int[CellCapacity];
    private readonly float[] _componentMasses = new float[ComponentCapacity + 1];
    private readonly int[] _pendingKeys = new int[CellCapacity];
    private readonly int[] _attachmentComponentIds = new int[7];
    private readonly int[] _lookupKeys = new int[LookupCapacity];
    private readonly int[] _lookupComponentIds = new int[LookupCapacity];
    private readonly int[] _usedLookupBuckets = new int[CellCapacity];
    private int _cellCount;
    private int _componentCount;
    private int _pendingHead;
    private int _pendingTail;
    private int _usedLookupBucketCount;

    internal void Build(WorldState world, int cultureId, int planeCode)
    {
        if (_componentCount > 0)
        {
            global::System.Array.Clear(_componentMasses, 1, _componentCount);
        }

        ClearLookup();
        _cellCount = 0;
        _componentCount = 0;
        _pendingHead = 0;
        _pendingTail = 0;

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
    }

    internal CultureInfluenceAttachment AttachmentAt(int q, int r)
    {
        var componentMass = 0f;
        var componentCount = 0;
        var adjacentSupportedCount = 0;
        var centerComponentId = ComponentIdAt(Pack(q, r));
        if (centerComponentId > 0)
        {
            AddAttachedComponent(centerComponentId, ref componentMass, ref componentCount);
        }

        for (var direction = 0; direction < 6; direction++)
        {
            var neighborComponentId = ComponentIdAt(Pack(q + DirectionQ(direction), r + DirectionR(direction)));
            if (neighborComponentId <= 0)
            {
                continue;
            }

            adjacentSupportedCount++;
            AddAttachedComponent(neighborComponentId, ref componentMass, ref componentCount);
        }

        return new CultureInfluenceAttachment(componentMass, adjacentSupportedCount, componentCount);
    }

    private void BuildComponent(WorldState world, int cultureId, int planeCode, int startKey)
    {
        _componentCount++;
        if (_componentCount > ComponentCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        var componentId = _componentCount;
        AddComponentCell(startKey, componentId);
        ClearPending();
        Enqueue(startKey);

        var mass = 0f;
        while (TryDequeue(out var key))
        {
            var q = UnpackQ(key);
            var r = UnpackR(key);
            mass += CellMass(world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r));
            for (var direction = 0; direction < 6; direction++)
            {
                TryAddNeighbor(world, cultureId, planeCode, q, r, direction, componentId);
            }
        }

        _componentMasses[componentId] = mass;
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

        var neighborKey = Pack(neighborQ, neighborR);
        if (ComponentIdAt(neighborKey) > 0)
        {
            return;
        }

        AddComponentCell(neighborKey, componentId);
        Enqueue(neighborKey);
    }

    private void AddComponentCell(int key, int componentId)
    {
        if (_cellCount >= CellCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        _cellKeys[_cellCount] = key;
        _cellComponentIds[_cellCount] = componentId;
        _cellCount++;
        AddLookup(key, componentId);
    }

    private int ComponentIdAt(int key)
    {
        var bucket = LookupBucket(key);
        for (var probe = 0; probe < LookupCapacity; probe++)
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

            bucket = (bucket + 1) & LookupMask;
        }

        return 0;
    }

    private void AddLookup(int key, int componentId)
    {
        var bucket = LookupBucket(key);
        for (var probe = 0; probe < LookupCapacity; probe++)
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

            bucket = (bucket + 1) & LookupMask;
        }

        throw new global::System.InvalidOperationException();
    }

    private void ClearLookup()
    {
        for (var i = 0; i < _usedLookupBucketCount; i++)
        {
            var bucket = _usedLookupBuckets[i];
            _lookupComponentIds[bucket] = 0;
        }

        _usedLookupBucketCount = 0;
    }

    private void AddAttachedComponent(int componentId, ref float componentMass, ref int componentCount)
    {
        for (var i = 0; i < componentCount; i++)
        {
            if (_attachmentComponentIds[i] == componentId)
            {
                return;
            }
        }

        _attachmentComponentIds[componentCount] = componentId;
        componentCount++;
        componentMass += _componentMasses[componentId];
    }

    private void ClearPending()
    {
        _pendingHead = 0;
        _pendingTail = 0;
    }

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

    private static bool CellSupported(WorldState world, int cultureId, int planeCode, int q, int r) =>
        world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r) >= SupportedRawInfluence;

    private static float CellMass(float raw) => Clamp01(raw / StrongRawInfluence);

    private static int Pack(int q, int r) =>
        ((q + CoordinateOffset) << 16) | ((r + CoordinateOffset) & CoordinateMask);

    private static int LookupBucket(int key)
    {
        var hash = (uint)key;
        hash ^= hash >> 16;
        hash *= 2246822519u;
        hash ^= hash >> 13;
        hash *= 3266489917u;
        hash ^= hash >> 16;
        return (int)(hash & LookupMask);
    }

    private static int UnpackQ(int key) =>
        (int)(((uint)key >> 16) & CoordinateMask) - CoordinateOffset;

    private static int UnpackR(int key) =>
        (key & CoordinateMask) - CoordinateOffset;

    private static int DirectionQ(int direction)
    {
        return direction switch
        {
            0 => 1,
            1 => 1,
            2 => 0,
            3 => -1,
            4 => -1,
            _ => 0,
        };
    }

    private static int DirectionR(int direction)
    {
        return direction switch
        {
            0 => 0,
            1 => -1,
            2 => -1,
            3 => 0,
            4 => 1,
            _ => 1,
        };
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
