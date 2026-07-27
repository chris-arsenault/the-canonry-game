namespace Engine.Kernel;

internal sealed class FrozenTickView
{
    private readonly int[] _entityIds;
    private readonly int[] _firstEntityByKind;
    private readonly int[] _nextEntityByKind;
    private readonly int[] _firstEntityByPhysicalSuperregion;
    private readonly int[] _nextEntityByPhysicalSuperregion;
    private readonly bool[] _occupiedPhysicalCells;
    private readonly bool[] _occupiedSemanticCells;
    private readonly int[] _relationshipIds;
    private readonly int[] _entityOwnerCounts;
    private readonly int[] _relationshipOwnerCounts;
    private readonly int[] _entityOwnerStarts;
    private readonly int[] _relationshipOwnerStarts;
    private readonly int[] _cultureIds;
    private readonly int[] _culturePlaneCodes;
    private readonly int[] _cultureQ;
    private readonly int[] _cultureR;
    private readonly int[] _cultureZ;
    private readonly int[] _cultureOwnerCounts;
    private readonly int[] _cultureOwnerStarts;
    private readonly int[] _ownerCursors;
    private readonly KernelOwnerTopology _topology;
    private int _entityCount;
    private int _relationshipCount;
    private int _cultureCount;

    public FrozenTickView(
        int entityCapacity,
        int relationshipCapacity,
        int cultureInfluenceCapacity,
        int mapCellCapacity,
        int kindIdCapacity,
        KernelOwnerTopology topology)
    {
        global::System.ArgumentNullException.ThrowIfNull(topology);
        _entityIds = new int[entityCapacity];
        _firstEntityByKind = new int[kindIdCapacity + 1];
        _nextEntityByKind = new int[entityCapacity + 1];
        _firstEntityByPhysicalSuperregion =
            new int[mapCellCapacity + 1];
        _nextEntityByPhysicalSuperregion =
            new int[entityCapacity + 1];
        _occupiedPhysicalCells = new bool[mapCellCapacity + 1];
        _occupiedSemanticCells = new bool[mapCellCapacity + 1];
        _relationshipIds = new int[relationshipCapacity];
        _cultureIds = new int[cultureInfluenceCapacity];
        _culturePlaneCodes = new int[cultureInfluenceCapacity];
        _cultureQ = new int[cultureInfluenceCapacity];
        _cultureR = new int[cultureInfluenceCapacity];
        _cultureZ = new int[cultureInfluenceCapacity];
        _entityOwnerCounts = new int[topology.PartitionCount];
        _relationshipOwnerCounts = new int[topology.PartitionCount];
        _cultureOwnerCounts = new int[topology.PartitionCount];
        _entityOwnerStarts = new int[topology.PartitionCount + 1];
        _relationshipOwnerStarts = new int[topology.PartitionCount + 1];
        _cultureOwnerStarts = new int[topology.PartitionCount + 1];
        _ownerCursors = new int[topology.PartitionCount];
        _topology = topology;
    }

    public int Tick { get; private set; }
    public long WorldVersion { get; private set; }
    public int EntityCount => _entityCount;
    public int RelationshipCount => _relationshipCount;
    public int CultureInfluenceCount => _cultureCount;
    public int OwnerPartitionCount => _topology.PartitionCount;

    public int CountFor(int rangeKindCode) => rangeKindCode switch
    {
        LocalEvaluationRangeKindCodes.Entities => _entityCount,
        LocalEvaluationRangeKindCodes.Relationships => _relationshipCount,
        _ => throw new global::System.ArgumentOutOfRangeException(nameof(rangeKindCode)),
    };

    public void Capture(WorldState world, int tick)
    {
        Tick = tick;
        WorldVersion = world.WorldVersion;
        CaptureEntities(world);
        CaptureRelationships(world);
        CaptureCultureInfluence(world);
    }

    private void CaptureCultureInfluence(WorldState world)
    {
        global::System.Array.Clear(_cultureOwnerCounts);
        _cultureCount = 0;
        for (var cultureId = 1;
             cultureId <= world.CultureHighWatermark;
             cultureId++)
        {
            CountCultureInfluence(world, cultureId, PlaneCodes.Physical);
            CountCultureInfluence(world, cultureId, PlaneCodes.Semantic);
        }

        _cultureCount = BuildOwnerStarts(
            _cultureOwnerCounts,
            _cultureOwnerStarts);
        if (_cultureCount > _cultureIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        CopyOwnerStartsToCursors(_cultureOwnerStarts);
        for (var cultureId = 1;
             cultureId <= world.CultureHighWatermark;
             cultureId++)
        {
            CaptureCultureInfluence(
                world,
                cultureId,
                PlaneCodes.Physical);
            CaptureCultureInfluence(
                world,
                cultureId,
                PlaneCodes.Semantic);
        }
    }

    private void CountCultureInfluence(
        WorldState world,
        int cultureId,
        int planeCode)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, planeCode);
        while (world.MoveNextRawCultureInfluenceCellUnits(
                   ref cursor,
                   out var q,
                   out var r,
                   out var z,
                   out var rawUnits))
        {
            if (rawUnits > 0)
            {
                _cultureOwnerCounts[
                    _topology.CellOwner(planeCode, q, r, z)]++;
            }
        }
    }

    private void CaptureCultureInfluence(
        WorldState world,
        int cultureId,
        int planeCode)
    {
        var cursor = world.RawCultureInfluenceCellsOf(cultureId, planeCode);
        while (world.MoveNextRawCultureInfluenceCellUnits(
                   ref cursor,
                   out var q,
                   out var r,
                   out var z,
                   out var rawUnits))
        {
            if (rawUnits <= 0)
            {
                continue;
            }

            var owner = _topology.CellOwner(planeCode, q, r, z);
            var destination = _ownerCursors[owner]++;
            _cultureIds[destination] = cultureId;
            _culturePlaneCodes[destination] = planeCode;
            _cultureQ[destination] = q;
            _cultureR[destination] = r;
            _cultureZ[destination] = z;
        }
    }

    private void CaptureEntities(WorldState world)
    {
        global::System.Array.Clear(_entityOwnerCounts);
        global::System.Array.Clear(_firstEntityByKind);
        global::System.Array.Clear(_nextEntityByKind);
        global::System.Array.Clear(
            _firstEntityByPhysicalSuperregion);
        global::System.Array.Clear(
            _nextEntityByPhysicalSuperregion);
        global::System.Array.Clear(_occupiedPhysicalCells);
        global::System.Array.Clear(_occupiedSemanticCells);
        for (var entityId = world.FirstActiveEntity();
             entityId > 0;
             entityId = world.NextActiveEntity(entityId))
        {
            _entityOwnerCounts[_topology.EntityOwner(entityId)]++;
            CaptureEntityKind(world, entityId);
            CaptureEntityPhysicalSuperregion(world, entityId);
            CaptureEntityCellOccupancy(world, entityId);
        }

        _entityCount = BuildOwnerStarts(
            _entityOwnerCounts,
            _entityOwnerStarts);
        CopyOwnerStartsToCursors(_entityOwnerStarts);
        for (var entityId = world.FirstActiveEntity();
             entityId > 0;
             entityId = world.NextActiveEntity(entityId))
        {
            var owner = _topology.EntityOwner(entityId);
            _entityIds[_ownerCursors[owner]++] = entityId;
        }
    }

    private void CaptureEntityCellOccupancy(
        WorldState world,
        int entityId)
    {
        if (!world.TryGetCellId(
                PlaneCodes.Physical,
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalZ(entityId),
                out var physicalCellId) ||
            !world.TryGetCellId(
                PlaneCodes.Semantic,
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                z: 0,
                out var semanticCellId))
        {
            throw new global::System.InvalidOperationException();
        }

        _occupiedPhysicalCells[physicalCellId] = true;
        _occupiedSemanticCells[semanticCellId] = true;
    }

    private void CaptureEntityKind(WorldState world, int entityId)
    {
        var kindId = world.EntityKindId(entityId);
        if ((uint)kindId == 0u ||
            kindId >= _firstEntityByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _nextEntityByKind[entityId] =
            _firstEntityByKind[kindId];
        _firstEntityByKind[kindId] = entityId;
    }

    private void CaptureEntityPhysicalSuperregion(
        WorldState world,
        int entityId)
    {
        var superregionId = world.CellSuperregionId(
            PlaneCodes.Physical,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId));
        if ((uint)superregionId >=
            (uint)_firstEntityByPhysicalSuperregion.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _nextEntityByPhysicalSuperregion[entityId] =
            _firstEntityByPhysicalSuperregion[superregionId];
        _firstEntityByPhysicalSuperregion[superregionId] =
            entityId;
    }

    private void CaptureRelationships(WorldState world)
    {
        global::System.Array.Clear(_relationshipOwnerCounts);
        _relationshipCount = world.ActiveRelationshipCount;
        for (var index = 0; index < _relationshipCount; index++)
        {
            var relationshipId = world.ActiveRelationshipAt(index);
            _relationshipOwnerCounts[
                _topology.RelationshipOwner(relationshipId)]++;
        }

        _relationshipCount = BuildOwnerStarts(
            _relationshipOwnerCounts,
            _relationshipOwnerStarts);
        CopyOwnerStartsToCursors(_relationshipOwnerStarts);
        for (var index = 0; index < _relationshipCount; index++)
        {
            var relationshipId = world.ActiveRelationshipAt(index);
            var owner = _topology.RelationshipOwner(relationshipId);
            _relationshipIds[_ownerCursors[owner]++] = relationshipId;
        }
    }

    public int EntityAt(int index, bool reverse) =>
        _entityIds[reverse ? _entityCount - index - 1 : index];

    public int RelationshipAt(int index, bool reverse) =>
        _relationshipIds[reverse ? _relationshipCount - index - 1 : index];

    internal int FirstEntityInPhysicalSuperregion(
        int superregionId)
    {
        if ((uint)superregionId >=
            (uint)_firstEntityByPhysicalSuperregion.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(superregionId));
        }

        return _firstEntityByPhysicalSuperregion[superregionId];
    }

    internal int NextEntityInPhysicalSuperregion(int entityId)
    {
        if ((uint)entityId == 0u ||
            entityId >= _nextEntityByPhysicalSuperregion.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(entityId));
        }

        return _nextEntityByPhysicalSuperregion[entityId];
    }

    internal int FirstEntityOfKind(int kindId)
    {
        if ((uint)kindId == 0u ||
            kindId >= _firstEntityByKind.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(kindId));
        }

        return _firstEntityByKind[kindId];
    }

    internal int NextEntityOfKind(int entityId)
    {
        if ((uint)entityId == 0u ||
            entityId >= _nextEntityByKind.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(entityId));
        }

        return _nextEntityByKind[entityId];
    }

    internal bool CellIsOccupied(
        WorldState world,
        int planeCode,
        int q,
        int r)
    {
        if (!world.TryGetCellId(
                planeCode,
                q,
                r,
                z: 0,
                out var cellId))
        {
            return false;
        }

        if (planeCode == PlaneCodes.Physical)
        {
            return _occupiedPhysicalCells[cellId];
        }

        if (planeCode == PlaneCodes.Semantic)
        {
            return _occupiedSemanticCells[cellId];
        }

        throw new global::System.ArgumentOutOfRangeException(
            nameof(planeCode));
    }

    public FrozenTickRange WorkerRange(
        int rangeKindCode,
        int workerIndex,
        int workerCount,
        bool reverse) =>
        rangeKindCode switch
        {
            LocalEvaluationRangeKindCodes.Entities =>
                OwnerRange(
                    _entityIds,
                    _entityCount,
                    _entityOwnerStarts,
                    workerIndex,
                    workerCount,
                    reverse),
            LocalEvaluationRangeKindCodes.Relationships =>
                OwnerRange(
                    _relationshipIds,
                    _relationshipCount,
                    _relationshipOwnerStarts,
                    workerIndex,
                    workerCount,
                    reverse),
            _ => throw new global::System.ArgumentOutOfRangeException(nameof(rangeKindCode)),
        };

    internal FrozenTickRange OwnerRange(
        int rangeKindCode,
        int ownerIndex,
        bool reverse) =>
        rangeKindCode switch
        {
            LocalEvaluationRangeKindCodes.Entities =>
                ExactOwnerRange(
                    _entityIds,
                    _entityCount,
                    _entityOwnerStarts,
                    ownerIndex,
                    reverse),
            LocalEvaluationRangeKindCodes.Relationships =>
                ExactOwnerRange(
                    _relationshipIds,
                    _relationshipCount,
                    _relationshipOwnerStarts,
                    ownerIndex,
                    reverse),
            _ => throw new global::System.ArgumentOutOfRangeException(
                nameof(rangeKindCode)),
        };

    internal FrozenCultureInfluenceRange CultureOwnerRange(int ownerIndex)
    {
        if ((uint)ownerIndex >= (uint)_topology.PartitionCount)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }

        var start = _cultureOwnerStarts[ownerIndex];
        return new FrozenCultureInfluenceRange(
            _cultureIds,
            _culturePlaneCodes,
            _cultureQ,
            _cultureR,
            _cultureZ,
            start,
            _cultureOwnerStarts[ownerIndex + 1] - start);
    }

    private static FrozenTickRange ExactOwnerRange(
        int[] ids,
        int totalCount,
        int[] ownerStarts,
        int ownerIndex,
        bool reverse)
    {
        if ((uint)ownerIndex >= (uint)(ownerStarts.Length - 1))
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(ownerIndex));
        }

        var start = ownerStarts[ownerIndex];
        return new FrozenTickRange(
            ids,
            totalCount,
            start,
            ownerStarts[ownerIndex + 1] - start,
            reverse);
    }

    private FrozenTickRange OwnerRange(
        int[] ids,
        int totalCount,
        int[] ownerStarts,
        int workerIndex,
        int workerCount,
        bool reverse)
    {
        var firstOwner = _topology.FirstOwnerForWorker(
            workerIndex,
            workerCount);
        var ownerLimit = _topology.OwnerLimitForWorker(
            workerIndex,
            workerCount);
        var start = ownerStarts[firstOwner];
        return new FrozenTickRange(
            ids,
            totalCount,
            start,
            ownerStarts[ownerLimit] - start,
            reverse);
    }

    private static int BuildOwnerStarts(int[] counts, int[] starts)
    {
        var total = 0;
        for (var owner = 0; owner < counts.Length; owner++)
        {
            starts[owner] = total;
            total = checked(total + counts[owner]);
        }

        starts[counts.Length] = total;
        return total;
    }

    private void CopyOwnerStartsToCursors(int[] starts)
    {
        for (var owner = 0; owner < _ownerCursors.Length; owner++)
        {
            _ownerCursors[owner] = starts[owner];
        }
    }
}
