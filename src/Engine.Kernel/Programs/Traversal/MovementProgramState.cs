namespace Engine.Kernel;

internal sealed partial class MovementProgramState : IKernelProgramState
{
    internal const int RoadMarkNone = 0;
    internal const int RoadMarkLow = 1;
    internal const int RoadMarkHigh = 2;
    internal const float LowRoadQualityCheckpoint = 25f;
    internal const float HighRoadQualityCheckpoint = 75f;

    private readonly PrimitiveFloatTable _roadQuality;
    private readonly PrimitiveFloatTable _roadQualityFloors;
    private readonly PrimitiveIntTable _roadTravelCounts;
    private readonly PrimitiveIntTable _lastRoadTicks;
    private readonly PrimitiveIntTable _travelStructureIds;
    private readonly PrimitiveIntTable _localRoadMarks;
    private readonly PrimitiveIntTable _regionalRoadMarks;
    private readonly PrimitiveIntTable _localRoadMarkScratch;
    private readonly PrimitiveIntTable _regionalRoadMarkScratch;
    private readonly int[] _routeCachePlaneCodes;
    private readonly int[] _routeCacheProfileIds;
    private readonly int[] _routeCacheLevelSizes;
    private readonly int[] _routeCacheSourceKeys;
    private readonly int[] _routeCacheTargetKeys;
    private readonly int[] _routeCacheTopologyEpochs;
    private readonly int[] _routeCacheRoadEpochs;
    private readonly int[] _routeCacheNextSegments;
    private readonly int[] _routeCachePortalQ;
    private readonly int[] _routeCachePortalR;
    private readonly bool[] _routeCacheOccupied;
    private int _pathingTopologyEpoch = 1;
    private int _roadMarkEpoch = 1;
    private int _routeCacheCount;
    private bool _localRoadMarksDirty;
    private bool _regionalRoadMarksDirty;

    public MovementProgramState(int cellCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCapacity);
        _roadQuality = new PrimitiveFloatTable(cellCapacity);
        _roadQualityFloors = new PrimitiveFloatTable(cellCapacity);
        _roadTravelCounts = new PrimitiveIntTable(cellCapacity);
        _lastRoadTicks = new PrimitiveIntTable(cellCapacity);
        _travelStructureIds = new PrimitiveIntTable(cellCapacity);
        _localRoadMarks = new PrimitiveIntTable(cellCapacity);
        _regionalRoadMarks = new PrimitiveIntTable(cellCapacity);
        _localRoadMarkScratch = new PrimitiveIntTable(cellCapacity);
        _regionalRoadMarkScratch = new PrimitiveIntTable(cellCapacity);
        _routeCachePlaneCodes = new int[cellCapacity];
        _routeCacheProfileIds = new int[cellCapacity];
        _routeCacheLevelSizes = new int[cellCapacity];
        _routeCacheSourceKeys = new int[cellCapacity];
        _routeCacheTargetKeys = new int[cellCapacity];
        _routeCacheTopologyEpochs = new int[cellCapacity];
        _routeCacheRoadEpochs = new int[cellCapacity];
        _routeCacheNextSegments = new int[cellCapacity];
        _routeCachePortalQ = new int[cellCapacity];
        _routeCachePortalR = new int[cellCapacity];
        _routeCacheOccupied = new bool[cellCapacity];
    }

    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 6;

    internal int RoadCellCount => _roadQuality.Count;

    internal int TravelStructureCellCount => _travelStructureIds.Count;

    internal int PathingTopologyEpoch => _pathingTopologyEpoch;

    internal int RoadMarkEpoch => _roadMarkEpoch;

    internal int RouteCacheCount => _routeCacheCount;

    public float RoadQuality(int planeCode, int q, int r, int tick, float roadDecayPerTick, int mainTickRatio) =>
        DecayedRoadQuality(planeCode, CoordinateKey(q, r), tick, roadDecayPerTick, mainTickRatio);

    public int RoadTravelCount(int planeCode, int q, int r) =>
        _roadTravelCounts.GetOrDefault(planeCode, CoordinateKey(q, r));

    public int TravelStructureId(int planeCode, int q, int r) =>
        _travelStructureIds.GetOrDefault(planeCode, CoordinateKey(q, r));

    internal bool SetTravelStructure(int planeCode, int q, int r, int travelStructureId)
    {
        var key = CoordinateKey(q, r);
        if (_travelStructureIds.GetOrDefault(planeCode, key) == travelStructureId)
        {
            return false;
        }

        _travelStructureIds.Set(planeCode, key, travelStructureId);
        RecordTopologyChanged();
        return true;
    }

    internal void RecordTravel(
        int planeCode,
        int q,
        int r,
        int tick,
        float roadBuild,
        float roadDecayPerTick,
        int mainTickRatio)
    {
        var key = CoordinateKey(q, r);
        var decayedQuality = DecayedRoadQuality(planeCode, key, tick, roadDecayPerTick, mainTickRatio);
        var nextQuality = Clamp(decayedQuality + roadBuild, 0f, 100f);
        var previousMark = RoadMarkForQuality(decayedQuality);
        var nextMark = RoadMarkForQuality(nextQuality);
        var nextFloor = QualityFloor(nextQuality);
        if (nextFloor > _roadQualityFloors.GetOrDefault(planeCode, key))
        {
            _roadQualityFloors.Set(planeCode, key, nextFloor);
        }

        _roadQuality.Set(planeCode, key, nextQuality);
        _roadTravelCounts.Add(planeCode, key, 1);
        _lastRoadTicks.Set(planeCode, key, tick);
        if (nextMark > previousMark)
        {
            _localRoadMarksDirty = true;
            _regionalRoadMarksDirty = true;
        }
    }

    internal void RecordTopologyChanged()
    {
        _pathingTopologyEpoch++;
        ClearRouteCache();
    }

    internal int RoadMark(int planeCode, int segmentSize, int segment)
    {
        return RoadMarkTable(segmentSize).GetOrDefault(planeCode, segment);
    }

    internal bool RoadMarksDirty(int segmentSize) =>
        segmentSize == 5 ? _localRoadMarksDirty : _regionalRoadMarksDirty;

    internal void MarkRoadMarksClean(int segmentSize)
    {
        if (segmentSize == 5)
        {
            _localRoadMarksDirty = false;
            return;
        }

        _regionalRoadMarksDirty = false;
    }

    internal void BeginRoadMarkReconciliation(int segmentSize)
    {
        RoadMarkScratchTable(segmentSize).Clear();
    }

    internal void AccumulateRoadMark(int planeCode, int segmentSize, int segment, int mark)
    {
        if (mark <= RoadMarkNone)
        {
            return;
        }

        var scratch = RoadMarkScratchTable(segmentSize);
        if (mark > scratch.GetOrDefault(planeCode, segment))
        {
            scratch.Set(planeCode, segment, mark);
        }
    }

    internal void CommitRoadMarkReconciliation(int segmentSize)
    {
        var active = RoadMarkTable(segmentSize);
        var scratch = RoadMarkScratchTable(segmentSize);
        if (RoadMarkTablesMatch(active, scratch))
        {
            return;
        }

        CopyRoadMarks(scratch, active);
        _roadMarkEpoch++;
        ClearRouteCache();
    }

    internal bool TryGetRouteCache(
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
        var slot = RouteCacheSlot(
            planeCode,
            movementProfileId,
            segmentSize,
            sourceKey,
            targetKey,
            topologyEpoch,
            roadEpoch);
        if (_routeCacheOccupied[slot] &&
            RouteCacheEntryMatches(
                slot,
                planeCode,
                movementProfileId,
                segmentSize,
                sourceKey,
                targetKey,
                topologyEpoch,
                roadEpoch))
        {
            nextSegment = _routeCacheNextSegments[slot];
            portalQ = _routeCachePortalQ[slot];
            portalR = _routeCachePortalR[slot];
            return true;
        }

        nextSegment = 0;
        portalQ = 0;
        portalR = 0;
        return false;
    }

    internal void StoreRouteCache(
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
        var slot = RouteCacheSlot(
            planeCode,
            movementProfileId,
            segmentSize,
            sourceKey,
            targetKey,
            topologyEpoch,
            roadEpoch);
        if (!_routeCacheOccupied[slot])
        {
            _routeCacheCount++;
        }

        _routeCacheOccupied[slot] = true;
        _routeCachePlaneCodes[slot] = planeCode;
        _routeCacheProfileIds[slot] = movementProfileId;
        _routeCacheLevelSizes[slot] = segmentSize;
        _routeCacheSourceKeys[slot] = sourceKey;
        _routeCacheTargetKeys[slot] = targetKey;
        _routeCacheTopologyEpochs[slot] = topologyEpoch;
        _routeCacheRoadEpochs[slot] = roadEpoch;
        _routeCacheNextSegments[slot] = nextSegment;
        _routeCachePortalQ[slot] = portalQ;
        _routeCachePortalR[slot] = portalR;
    }

    internal void ForEachRoadCell(global::System.Action<int, int, float, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var index = 0; index < _roadQuality.Count; index++)
        {
            GetRoadCell(index, out var planeCode, out var coordinateKey, out var quality, out var lastTick, out var travelCount);
            visitor(
                planeCode,
                coordinateKey,
                quality,
                lastTick,
                travelCount);
        }
    }

    internal void ForEachTravelStructureCell(global::System.Action<int, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var index = 0; index < _travelStructureIds.Count; index++)
        {
            _travelStructureIds.GetEntry(index, out var planeCode, out var coordinateKey, out var travelStructureId);
            visitor(planeCode, coordinateKey, travelStructureId);
        }
    }

    internal void GetRoadCell(
        int index,
        out int planeCode,
        out int coordinateKey,
        out float quality,
        out int lastTick,
        out int travelCount)
    {
        _roadQuality.GetEntry(index, out planeCode, out coordinateKey, out quality);
        lastTick = _lastRoadTicks.GetOrDefault(planeCode, coordinateKey);
        travelCount = _roadTravelCounts.GetOrDefault(planeCode, coordinateKey);
    }

    private void ClearRouteCache()
    {
        if (_routeCacheCount == 0)
        {
            return;
        }

        global::System.Array.Clear(_routeCacheOccupied);
        _routeCacheCount = 0;
    }

    private int RouteCacheSlot(
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
            return (int)(hash % (uint)_routeCacheOccupied.Length);
        }
    }

    private bool RouteCacheEntryMatches(
        int slot,
        int planeCode,
        int movementProfileId,
        int segmentSize,
        int sourceKey,
        int targetKey,
        int topologyEpoch,
        int roadEpoch)
    {
        return _routeCachePlaneCodes[slot] == planeCode &&
            _routeCacheProfileIds[slot] == movementProfileId &&
            _routeCacheLevelSizes[slot] == segmentSize &&
            _routeCacheSourceKeys[slot] == sourceKey &&
            _routeCacheTargetKeys[slot] == targetKey &&
            _routeCacheTopologyEpochs[slot] == topologyEpoch &&
            _routeCacheRoadEpochs[slot] == roadEpoch;
    }

    private PrimitiveIntTable RoadMarkTable(int segmentSize)
    {
        return segmentSize == 5 ? _localRoadMarks : _regionalRoadMarks;
    }

    private PrimitiveIntTable RoadMarkScratchTable(int segmentSize)
    {
        return segmentSize == 5 ? _localRoadMarkScratch : _regionalRoadMarkScratch;
    }

    private static bool RoadMarkTablesMatch(PrimitiveIntTable active, PrimitiveIntTable scratch)
    {
        if (active.Count != scratch.Count)
        {
            return false;
        }

        for (var index = 0; index < active.Count; index++)
        {
            active.GetEntry(index, out var planeCode, out var segment, out var mark);
            if (scratch.GetOrDefault(planeCode, segment) != mark)
            {
                return false;
            }
        }

        return true;
    }

    private static void CopyRoadMarks(PrimitiveIntTable source, PrimitiveIntTable destination)
    {
        destination.Clear();
        for (var index = 0; index < source.Count; index++)
        {
            source.GetEntry(index, out var planeCode, out var segment, out var mark);
            if (mark > RoadMarkNone)
            {
                destination.Set(planeCode, segment, mark);
            }
        }
    }

    private float DecayedRoadQuality(int planeCode, int key, int tick, float roadDecayPerTick, int mainTickRatio)
    {
        if (!_roadQuality.TryGet(planeCode, key, out var roadQuality))
        {
            return 0f;
        }

        var floor = _roadQualityFloors.GetOrDefault(planeCode, key);
        if (roadQuality <= 0f)
        {
            return floor > roadQuality ? floor : roadQuality;
        }

        var elapsedTicks = MainTickAge(_lastRoadTicks.GetOrDefault(planeCode, key), tick, mainTickRatio);
        if (elapsedTicks <= 0)
        {
            return floor > roadQuality ? floor : roadQuality;
        }

        var decayed = roadQuality * (float)global::System.Math.Pow(Clamp(roadDecayPerTick, 0f, 1f), elapsedTicks);
        return floor > decayed ? floor : decayed;
    }

    private static int MainTickAge(int startedTick, int currentTick, int mainTickRatio)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mainTickRatio);
        return currentTick <= startedTick ? 0 : (currentTick - startedTick) / mainTickRatio;
    }

    private static float QualityFloor(float roadQuality)
    {
        if (roadQuality >= HighRoadQualityCheckpoint)
        {
            return HighRoadQualityCheckpoint;
        }

        return roadQuality >= LowRoadQualityCheckpoint
            ? LowRoadQualityCheckpoint
            : 0f;
    }

    internal static int RoadMarkForQuality(float roadQuality)
    {
        if (roadQuality >= HighRoadQualityCheckpoint)
        {
            return RoadMarkHigh;
        }

        return roadQuality >= LowRoadQualityCheckpoint
            ? RoadMarkLow
            : RoadMarkNone;
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static int CoordinateKey(int q, int r)
    {
        unchecked
        {
            var hash = (uint)q * 2654435761u;
            hash ^= (uint)r * 2246822519u;
            var value = (int)(hash & 2147483647u);
            return value == 0 ? 1 : value;
        }
    }
}
