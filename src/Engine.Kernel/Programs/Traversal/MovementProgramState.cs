namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MovementProgramState : IKernelProgramState
{
    internal const int RoadMarkNone = 0;
    internal const int RoadMarkLow = 1;
    internal const int RoadMarkHigh = 2;
    internal static readonly FixedScalar LowRoadQualityCheckpoint = 25;
    internal static readonly FixedScalar HighRoadQualityCheckpoint = 75;

    private readonly int[] _roadQuality;
    private readonly int[] _roadQualityFloors;
    private readonly int[] _roadTravelCounts;
    private readonly int[] _lastRoadTicks;
    private readonly int[] _travelStructureIds;
    private readonly int[] _roadCellIds;
    private readonly int[] _roadCellPositions;
    private readonly int[] _travelStructureCellIds;
    private readonly int[] _travelStructureCellPositions;
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
    private int _roadCellCount;
    private int _travelStructureCellCount;
    private int _routeCacheCount;
    private bool _localRoadMarksDirty;
    private bool _regionalRoadMarksDirty;

    public MovementProgramState(int cellCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(cellCapacity);
        _roadQuality = new int[cellCapacity + 1];
        _roadQualityFloors = new int[cellCapacity + 1];
        _roadTravelCounts = new int[cellCapacity + 1];
        _lastRoadTicks = new int[cellCapacity + 1];
        _travelStructureIds = new int[cellCapacity + 1];
        _roadCellIds = new int[cellCapacity];
        _roadCellPositions = new int[cellCapacity + 1];
        _travelStructureCellIds = new int[cellCapacity];
        _travelStructureCellPositions = new int[cellCapacity + 1];
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

    public int SchemaVersion => 7;

    internal int RoadCellCount => _roadCellCount;

    internal int TravelStructureCellCount => _travelStructureCellCount;

    internal int PathingTopologyEpoch => _pathingTopologyEpoch;

    internal int RoadMarkEpoch => _roadMarkEpoch;

    internal int RouteCacheCount => _routeCacheCount;

    public FixedScalar RoadQuality(
        int cellId,
        int tick,
        FixedScalar roadDecayPerTick,
        int decayCadenceTicks) =>
        KernelFixedPoint.RoadQualityScalar(
            DecayedRoadQualityUnits(cellId, tick, roadDecayPerTick, decayCadenceTicks));

    public int RoadTravelCount(int cellId) => _roadTravelCounts[cellId];

    public int TravelStructureId(int cellId) => _travelStructureIds[cellId];

    internal bool SetTravelStructure(int cellId, int travelStructureId)
    {
        var previous = SetTravelStructureOwned(
            cellId,
            travelStructureId);
        if (previous == travelStructureId)
        {
            return false;
        }

        ReconcileTravelStructureOwned(
            cellId,
            previous,
            travelStructureId);
        return true;
    }

    internal int SetTravelStructureOwned(
        int cellId,
        int travelStructureId)
    {
        var previous = _travelStructureIds[cellId];
        _travelStructureIds[cellId] = travelStructureId;
        return previous;
    }

    internal void ReconcileTravelStructureOwned(
        int cellId,
        int previous,
        int travelStructureId)
    {
        if (previous == 0 && travelStructureId > 0)
        {
            AddActiveCell(
                _travelStructureCellIds,
                _travelStructureCellPositions,
                ref _travelStructureCellCount,
                cellId);
        }
        else if (previous > 0 && travelStructureId == 0)
        {
            RemoveActiveCell(
                _travelStructureCellIds,
                _travelStructureCellPositions,
                ref _travelStructureCellCount,
                cellId);
        }

        RecordTopologyChanged();
    }

    internal void RecordTravel(
        int cellId,
        int tick,
        FixedScalar roadBuild,
        FixedScalar roadDecayPerTick,
        int decayCadenceTicks)
    {
        var decayedQuality = DecayedRoadQualityUnits(
            cellId,
            tick,
            roadDecayPerTick,
            decayCadenceTicks);
        var nextQuality = KernelFixedPoint.SaturatingAdd(
            decayedQuality,
            KernelFixedPoint.RoadWorkUnits(roadBuild),
            0,
            KernelFixedPoint.RoadQualityMaximumUnits);
        var previousMark = RoadMarkForQualityUnits(decayedQuality);
        var nextMark = RoadMarkForQualityUnits(nextQuality);
        var nextFloor = QualityFloorUnits(nextQuality);
        if (nextFloor > _roadQualityFloors[cellId])
        {
            _roadQualityFloors[cellId] = nextFloor;
        }

        if (_roadQuality[cellId] == 0)
        {
            AddActiveCell(_roadCellIds, _roadCellPositions, ref _roadCellCount, cellId);
        }

        _roadQuality[cellId] = nextQuality;
        _roadTravelCounts[cellId]++;
        _lastRoadTicks[cellId] = tick;
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

    internal void ForEachRoadCell(global::System.Action<int, FixedScalar, int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var index = 0; index < _roadCellCount; index++)
        {
            GetRoadCell(index, out var cellId, out var quality, out var lastTick, out var travelCount);
            visitor(cellId, quality, lastTick, travelCount);
        }
    }

    internal void ForEachTravelStructureCell(global::System.Action<int, int> visitor)
    {
        global::System.ArgumentNullException.ThrowIfNull(visitor);
        for (var index = 0; index < _travelStructureCellCount; index++)
        {
            var cellId = _travelStructureCellIds[index];
            visitor(cellId, _travelStructureIds[cellId]);
        }
    }

    internal void GetRoadCell(
        int index,
        out int cellId,
        out FixedScalar quality,
        out int lastTick,
        out int travelCount)
    {
        cellId = _roadCellIds[index];
        quality = KernelFixedPoint.RoadQualityScalar(_roadQuality[cellId]);
        lastTick = _lastRoadTicks[cellId];
        travelCount = _roadTravelCounts[cellId];
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

    private int DecayedRoadQualityUnits(
        int cellId,
        int tick,
        FixedScalar roadDecayPerTick,
        int decayCadenceTicks)
    {
        NumericGuard.ThrowIfNegativeOrZero(decayCadenceTicks);
        var roadQuality = _roadQuality[cellId];
        if (roadQuality == 0)
        {
            return 0;
        }

        var floor = _roadQualityFloors[cellId];
        if (roadQuality <= 0)
        {
            return floor > roadQuality ? floor : roadQuality;
        }

        var startedTick = _lastRoadTicks[cellId];
        var elapsedCadences = tick <= startedTick
            ? 0
            : (tick - startedTick) / decayCadenceTicks;
        if (elapsedCadences <= 0)
        {
            return floor > roadQuality ? floor : roadQuality;
        }

        var decayRatio = KernelFixedPoint.RatioUnits(Clamp(roadDecayPerTick, 0, 1));
        var compoundedRatio = PowRatio(decayRatio, elapsedCadences);
        var decayed = KernelFixedPoint.MultiplyByRatio(
            roadQuality,
            compoundedRatio,
            0,
            KernelFixedPoint.RoadQualityMaximumUnits);
        return floor > decayed ? floor : decayed;
    }

    private static int QualityFloorUnits(int roadQualityUnits)
    {
        if (roadQualityUnits >= KernelFixedPoint.RoadQualityUnits(HighRoadQualityCheckpoint))
        {
            return KernelFixedPoint.RoadQualityUnits(HighRoadQualityCheckpoint);
        }

        return roadQualityUnits >= KernelFixedPoint.RoadQualityUnits(LowRoadQualityCheckpoint)
            ? KernelFixedPoint.RoadQualityUnits(LowRoadQualityCheckpoint)
            : 0;
    }

    internal static int RoadMarkForQuality(FixedScalar roadQuality)
    {
        if (roadQuality >= HighRoadQualityCheckpoint)
        {
            return RoadMarkHigh;
        }

        return roadQuality >= LowRoadQualityCheckpoint
            ? RoadMarkLow
            : RoadMarkNone;
    }

    private static int RoadMarkForQualityUnits(int roadQualityUnits)
    {
        if (roadQualityUnits >= KernelFixedPoint.RoadQualityUnits(HighRoadQualityCheckpoint))
        {
            return RoadMarkHigh;
        }

        return roadQualityUnits >= KernelFixedPoint.RoadQualityUnits(LowRoadQualityCheckpoint)
            ? RoadMarkLow
            : RoadMarkNone;
    }

    private static int PowRatio(int ratioUnits, int exponent)
    {
        var result = KernelFixedPoint.RatioScale;
        var factor = ratioUnits;
        var remaining = exponent;
        while (remaining > 0)
        {
            if ((remaining & 1) != 0)
            {
                result = KernelFixedPoint.MultiplyByRatio(
                    result,
                    factor,
                    0,
                    KernelFixedPoint.RatioScale);
            }

            remaining >>= 1;
            if (remaining > 0)
            {
                factor = KernelFixedPoint.MultiplyByRatio(
                    factor,
                    factor,
                    0,
                    KernelFixedPoint.RatioScale);
            }
        }

        return result;
    }

    private static FixedScalar Clamp(FixedScalar value, FixedScalar minimum, FixedScalar maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static void AddActiveCell(
        int[] activeCellIds,
        int[] activePositions,
        ref int activeCount,
        int cellId)
    {
        activeCellIds[activeCount] = cellId;
        activePositions[cellId] = activeCount + 1;
        activeCount++;
    }

    private static void RemoveActiveCell(
        int[] activeCellIds,
        int[] activePositions,
        ref int activeCount,
        int cellId)
    {
        var position = activePositions[cellId] - 1;
        var lastPosition = --activeCount;
        var movedCellId = activeCellIds[lastPosition];
        activeCellIds[position] = movedCellId;
        activePositions[movedCellId] = position + 1;
        activeCellIds[lastPosition] = 0;
        activePositions[cellId] = 0;
    }
}
