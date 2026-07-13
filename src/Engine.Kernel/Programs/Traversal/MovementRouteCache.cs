namespace Engine.Kernel;

internal sealed class MovementRouteCache
{
    private readonly int[] _planeCodes;
    private readonly int[] _profileIds;
    private readonly int[] _levelSizes;
    private readonly int[] _sourceKeys;
    private readonly int[] _targetKeys;
    private readonly int[] _topologyEpochs;
    private readonly int[] _roadEpochs;
    private readonly int[] _nextSegments;
    private readonly int[] _portalQ;
    private readonly int[] _portalR;
    private readonly int[] _versions;

    internal MovementRouteCache(int capacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _planeCodes = new int[capacity];
        _profileIds = new int[capacity];
        _levelSizes = new int[capacity];
        _sourceKeys = new int[capacity];
        _targetKeys = new int[capacity];
        _topologyEpochs = new int[capacity];
        _roadEpochs = new int[capacity];
        _nextSegments = new int[capacity];
        _portalQ = new int[capacity];
        _portalR = new int[capacity];
        _versions = new int[capacity];
    }

    internal bool TryGet(
        int planeCode,
        int movementProfileId,
        int segmentSize,
        int sourceKey,
        int targetKey,
        int topologyEpoch,
        int roadEpoch,
        out int nextSegment,
        out int portalQ,
        out int portalR)
    {
        var slot = Slot(
            planeCode,
            movementProfileId,
            segmentSize,
            sourceKey,
            targetKey,
            topologyEpoch,
            roadEpoch);
        var before = global::System.Threading.Volatile.Read(ref _versions[slot]);
        if (before == 0 || (before & 1) != 0)
        {
            nextSegment = 0;
            portalQ = 0;
            portalR = 0;
            return false;
        }

        var cachedPlaneCode = _planeCodes[slot];
        var cachedProfileId = _profileIds[slot];
        var cachedLevelSize = _levelSizes[slot];
        var cachedSourceKey = _sourceKeys[slot];
        var cachedTargetKey = _targetKeys[slot];
        var cachedTopologyEpoch = _topologyEpochs[slot];
        var cachedRoadEpoch = _roadEpochs[slot];
        var cachedNextSegment = _nextSegments[slot];
        var cachedPortalQ = _portalQ[slot];
        var cachedPortalR = _portalR[slot];
        var after = global::System.Threading.Volatile.Read(ref _versions[slot]);
        if (before == after &&
            EntryMatches(
                planeCode,
                movementProfileId,
                segmentSize,
                sourceKey,
                targetKey,
                topologyEpoch,
                roadEpoch,
                cachedPlaneCode,
                cachedProfileId,
                cachedLevelSize,
                cachedSourceKey,
                cachedTargetKey,
                cachedTopologyEpoch,
                cachedRoadEpoch))
        {
            nextSegment = cachedNextSegment;
            portalQ = cachedPortalQ;
            portalR = cachedPortalR;
            return true;
        }

        nextSegment = 0;
        portalQ = 0;
        portalR = 0;
        return false;
    }

    private static bool EntryMatches(
        int planeCode,
        int movementProfileId,
        int segmentSize,
        int sourceKey,
        int targetKey,
        int topologyEpoch,
        int roadEpoch,
        int cachedPlaneCode,
        int cachedProfileId,
        int cachedLevelSize,
        int cachedSourceKey,
        int cachedTargetKey,
        int cachedTopologyEpoch,
        int cachedRoadEpoch) =>
        cachedPlaneCode == planeCode &&
        cachedProfileId == movementProfileId &&
        cachedLevelSize == segmentSize &&
        cachedSourceKey == sourceKey &&
        cachedTargetKey == targetKey &&
        cachedTopologyEpoch == topologyEpoch &&
        cachedRoadEpoch == roadEpoch;

    internal void Store(
        int planeCode,
        int movementProfileId,
        int segmentSize,
        int sourceKey,
        int targetKey,
        int topologyEpoch,
        int roadEpoch,
        int nextSegment,
        int portalQ,
        int portalR)
    {
        var slot = Slot(
            planeCode,
            movementProfileId,
            segmentSize,
            sourceKey,
            targetKey,
            topologyEpoch,
            roadEpoch);
        var version = global::System.Threading.Volatile.Read(ref _versions[slot]);
        if ((version & 1) != 0 ||
            global::System.Threading.Interlocked.CompareExchange(
                ref _versions[slot],
                version + 1,
                version) != version)
        {
            return;
        }

        _planeCodes[slot] = planeCode;
        _profileIds[slot] = movementProfileId;
        _levelSizes[slot] = segmentSize;
        _sourceKeys[slot] = sourceKey;
        _targetKeys[slot] = targetKey;
        _topologyEpochs[slot] = topologyEpoch;
        _roadEpochs[slot] = roadEpoch;
        _nextSegments[slot] = nextSegment;
        _portalQ[slot] = portalQ;
        _portalR[slot] = portalR;
        global::System.Threading.Volatile.Write(ref _versions[slot], version + 2);
    }

    private int Slot(
        int planeCode,
        int movementProfileId,
        int segmentSize,
        int sourceKey,
        int targetKey,
        int topologyEpoch,
        int roadEpoch)
    {
        unchecked
        {
            var hash = (uint)planeCode * 2654435761u;
            hash ^= (uint)movementProfileId * 2246822519u;
            hash ^= (uint)segmentSize * 3266489917u;
            hash ^= (uint)sourceKey * 668265263u;
            hash ^= (uint)targetKey * 374761393u;
            hash ^= (uint)topologyEpoch * 1274126177u;
            hash ^= (uint)roadEpoch * 1597334677u;
            return (int)(hash % (uint)_versions.Length);
        }
    }
}
