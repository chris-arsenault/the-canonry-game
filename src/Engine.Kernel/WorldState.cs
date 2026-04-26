namespace Engine.Kernel;

using Engine.Eventing;

public sealed class WorldState
{
    private const int OutgoingRelationshipDirectionCode = 1;
    private const int IncomingRelationshipDirectionCode = 2;

    private readonly KernelEventLog _events;
    private readonly PrimitiveFloatTable _attributes;
    private readonly PrimitiveFloatTable _resources;
    private readonly PrimitiveIntTable _entityTags;
    private readonly PrimitiveIntTable _mapCellPassability;
    private readonly PrimitiveIntTable _mapCellTerrainIds;
    private readonly PrimitiveIntTable _mapCellCoverageOwners;
    private readonly float[] _attributeMinimums;
    private readonly float[] _attributeMaximums;
    private readonly bool[] _attributeBoundsRegistered;
    private readonly bool[] _terrainRegistered;
    private readonly bool[] _terrainPassable;
    private readonly int[] _terrainBoundaryGroupIds;
    private readonly bool[] _entityAllocated;
    private readonly int[] _entityStatus;
    private readonly int[] _entityKindId;
    private readonly int[] _entitySubtypeId;
    private readonly int[] _entityCultureId;
    private readonly int[] _entityPhysicalQ;
    private readonly int[] _entityPhysicalR;
    private readonly int[] _entitySemanticQ;
    private readonly int[] _entitySemanticR;
    private readonly int[] _entitySemanticS;
    private readonly int[] _entityCreatedAt;
    private readonly int[] _entityUpdatedAt;
    private readonly int[] _firstEntityByKind;
    private readonly int[] _nextEntityByKind;
    private readonly PrimitiveIntTable _firstPhysicalEntityAtCell;
    private readonly PrimitiveIntTable _firstSemanticEntityAtCell;
    private readonly int[] _nextPhysicalEntityAtCell;
    private readonly int[] _nextSemanticEntityAtCell;
    private readonly bool[] _relationshipAllocated;
    private readonly bool[] _relationshipActive;
    private readonly int[] _relationshipKindId;
    private readonly int[] _relationshipPolarityCode;
    private readonly int[] _relationshipSourceId;
    private readonly int[] _relationshipTargetId;
    private readonly int[] _relationshipFactId0;
    private readonly int[] _relationshipFactId1;
    private readonly int[] _relationshipFactId2;
    private readonly int[] _relationshipFactId3;
    private readonly int[] _relationshipFactCount;
    private readonly int[] _relationshipStartTick;
    private readonly int[] _relationshipEndTick;
    private readonly float[] _relationshipStrength;
    private readonly bool[] _relationshipInDeltaGraph;
    private readonly int[] _outgoingFrozenOffsets;
    private readonly int[] _incomingFrozenOffsets;
    private readonly int[] _outgoingFrozenRelationships;
    private readonly int[] _incomingFrozenRelationships;
    private readonly int[] _outgoingDeltaOffsets;
    private readonly int[] _incomingDeltaOffsets;
    private readonly int[] _outgoingDeltaRelationships;
    private readonly int[] _incomingDeltaRelationships;
    private readonly int[] _relationshipBuildCursors;
    private readonly int[] _graphVisitedEpochs;
    private readonly int[] _graphFrontierA;
    private readonly int[] _graphFrontierB;
    private readonly int _relationshipCreatedCodeId;
    private readonly int _relationshipCulledCodeId;
    private readonly int _relationshipStrengthChangedCodeId;
    private readonly int _relationshipEntityCapacity;
    private readonly int _relationshipKindCapacity;
    private readonly int _relationshipGraphDeltaMergeThreshold;
    private int _entityHighWatermark;
    private int _relationshipHighWatermark;
    private int _mapRadius;
    private int _relationshipGraphDeltaCount;
    private int _graphTraversalEpoch;

    public WorldState(KernelEventLog events, SimulationOptions options)
    {
        global::System.ArgumentNullException.ThrowIfNull(events);
        global::System.ArgumentNullException.ThrowIfNull(options);
        _events = events;
        _attributes = new PrimitiveFloatTable(options.AttributeCapacity);
        _resources = new PrimitiveFloatTable(options.ResourceCapacity);
        _entityTags = new PrimitiveIntTable(options.TagCapacity);
        _mapCellPassability = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellTerrainIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellCoverageOwners = new PrimitiveIntTable(options.MapCellCapacity);
        _attributeMinimums = new float[options.AttributeIdCapacity + 1];
        _attributeMaximums = new float[options.AttributeIdCapacity + 1];
        _attributeBoundsRegistered = new bool[options.AttributeIdCapacity + 1];
        _terrainRegistered = new bool[options.TerrainIdCapacity + 1];
        _terrainPassable = new bool[options.TerrainIdCapacity + 1];
        _terrainBoundaryGroupIds = new int[options.TerrainIdCapacity + 1];
        _entityAllocated = new bool[options.EntityCapacity + 1];
        _entityStatus = new int[options.EntityCapacity + 1];
        _entityKindId = new int[options.EntityCapacity + 1];
        _entitySubtypeId = new int[options.EntityCapacity + 1];
        _entityCultureId = new int[options.EntityCapacity + 1];
        _entityPhysicalQ = new int[options.EntityCapacity + 1];
        _entityPhysicalR = new int[options.EntityCapacity + 1];
        _entitySemanticQ = new int[options.EntityCapacity + 1];
        _entitySemanticR = new int[options.EntityCapacity + 1];
        _entitySemanticS = new int[options.EntityCapacity + 1];
        _entityCreatedAt = new int[options.EntityCapacity + 1];
        _entityUpdatedAt = new int[options.EntityCapacity + 1];
        _firstEntityByKind = new int[options.KindIdCapacity + 1];
        _nextEntityByKind = new int[options.EntityCapacity + 1];
        _firstPhysicalEntityAtCell = new PrimitiveIntTable(options.MapCellCapacity);
        _firstSemanticEntityAtCell = new PrimitiveIntTable(options.EntityCapacity);
        _nextPhysicalEntityAtCell = new int[options.EntityCapacity + 1];
        _nextSemanticEntityAtCell = new int[options.EntityCapacity + 1];
        _relationshipAllocated = new bool[options.RelationshipCapacity + 1];
        _relationshipActive = new bool[options.RelationshipCapacity + 1];
        _relationshipKindId = new int[options.RelationshipCapacity + 1];
        _relationshipPolarityCode = new int[options.RelationshipCapacity + 1];
        _relationshipSourceId = new int[options.RelationshipCapacity + 1];
        _relationshipTargetId = new int[options.RelationshipCapacity + 1];
        _relationshipFactId0 = new int[options.RelationshipCapacity + 1];
        _relationshipFactId1 = new int[options.RelationshipCapacity + 1];
        _relationshipFactId2 = new int[options.RelationshipCapacity + 1];
        _relationshipFactId3 = new int[options.RelationshipCapacity + 1];
        _relationshipFactCount = new int[options.RelationshipCapacity + 1];
        _relationshipStartTick = new int[options.RelationshipCapacity + 1];
        _relationshipEndTick = new int[options.RelationshipCapacity + 1];
        _relationshipStrength = new float[options.RelationshipCapacity + 1];
        _relationshipInDeltaGraph = new bool[options.RelationshipCapacity + 1];
        _relationshipEntityCapacity = options.EntityCapacity;
        _relationshipKindCapacity = options.RelationshipKindIdCapacity;
        var relationshipRowCount = checked(options.EntityCapacity * options.RelationshipKindIdCapacity);
        _outgoingFrozenOffsets = new int[relationshipRowCount + 1];
        _incomingFrozenOffsets = new int[relationshipRowCount + 1];
        _outgoingFrozenRelationships = new int[options.RelationshipCapacity];
        _incomingFrozenRelationships = new int[options.RelationshipCapacity];
        _outgoingDeltaOffsets = new int[relationshipRowCount + 1];
        _incomingDeltaOffsets = new int[relationshipRowCount + 1];
        _outgoingDeltaRelationships = new int[options.RelationshipCapacity];
        _incomingDeltaRelationships = new int[options.RelationshipCapacity];
        _relationshipBuildCursors = new int[relationshipRowCount + 1];
        _graphVisitedEpochs = new int[options.EntityCapacity + 1];
        _graphFrontierA = new int[options.EntityCapacity + 1];
        _graphFrontierB = new int[options.EntityCapacity + 1];
        _relationshipCreatedCodeId = options.RuntimeCodes.RelationshipCreatedCodeId;
        _relationshipCulledCodeId = options.RuntimeCodes.RelationshipCulledCodeId;
        _relationshipStrengthChangedCodeId = options.RuntimeCodes.RelationshipStrengthChangedCodeId;
        _relationshipGraphDeltaMergeThreshold = options.RelationshipGraphDeltaMergeThreshold;
    }

    public int CurrentTick { get; private set; }
    public long WorldVersion { get; private set; }
    public int EntityCapacity => _entityAllocated.Length - 1;
    public int RelationshipCapacity => _relationshipAllocated.Length - 1;
    public int MapRadius => _mapRadius;

    public void SetMapRadius(int radius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        _mapRadius = radius;
    }

    public void SetCell(int q, int r, int terrainId, bool passable)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, passable, CellCoverageOwner(q, r));
    }

    public void SetCellPassability(int q, int r, bool passable)
    {
        _mapCellPassability.Set(CoordinateKey(q, r), 1, passable ? 1 : 2);
    }

    public bool CellIsPassable(int q, int r)
    {
        return _mapCellPassability.GetOrDefault(CoordinateKey(q, r), 1) == 1;
    }

    public bool CellIsKnown(int q, int r)
    {
        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r), 1, out _);
    }

    public int CellTerrainId(int q, int r)
    {
        return _mapCellTerrainIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public void SetCellTerrain(int q, int r, int terrainId, bool passable)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, passable, CellCoverageOwner(q, r));
    }

    public void MaterializeCell(int q, int r, int terrainId, int ownerEntityId)
    {
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        WriteCell(q, r, terrainId, TerrainIsPassable(terrainId), ownerEntityId);
        AdvanceWorldVersion();
    }

    private void WriteCell(int q, int r, int terrainId, bool passable, int ownerEntityId)
    {
        var key = CoordinateKey(q, r);
        _mapCellTerrainIds.Set(key, 1, terrainId);
        _mapCellPassability.Set(key, 1, passable ? 1 : 2);
        _mapCellCoverageOwners.Set(key, 1, ownerEntityId);
    }

    public void RegisterTerrain(int terrainId, bool passable, int boundaryGroupId)
    {
        ValidatePositive(terrainId);
        ValidatePositive(boundaryGroupId);
        if ((uint)terrainId >= (uint)_terrainRegistered.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _terrainRegistered[terrainId] = true;
        _terrainPassable[terrainId] = passable;
        _terrainBoundaryGroupIds[terrainId] = boundaryGroupId;
    }

    public bool TerrainIsPassable(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _terrainPassable[terrainId];
    }

    public int TerrainBoundaryGroupId(int terrainId)
    {
        EnsureTerrain(terrainId);
        return _terrainBoundaryGroupIds[terrainId];
    }

    public int CellCoverageOwner(int q, int r)
    {
        return _mapCellCoverageOwners.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public void SetCellCoverageOwner(int q, int r, int ownerEntityId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        _mapCellCoverageOwners.Set(CoordinateKey(q, r), 1, ownerEntityId);
    }

    public int CoverageCellCount(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        var count = 0;
        for (var q = -_mapRadius; q <= _mapRadius; q++)
        {
            var minR = global::System.Math.Max(-_mapRadius, -q - _mapRadius);
            var maxR = global::System.Math.Min(_mapRadius, -q + _mapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (CellCoverageOwner(q, r) == ownerEntityId)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public void RegisterAttributeBounds(int attributeId, float minimum, float maximum)
    {
        ValidatePositive(attributeId);
        if (maximum < minimum)
        {
            throw new global::System.InvalidOperationException();
        }

        if ((uint)attributeId >= (uint)_attributeMinimums.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeMinimums[attributeId] = minimum;
        _attributeMaximums[attributeId] = maximum;
        _attributeBoundsRegistered[attributeId] = true;
    }

    public void BeginTick(int tick)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(tick);
        if (_relationshipGraphDeltaCount > 0)
        {
            CompactRelationshipGraph();
        }

        CurrentTick = tick;
    }

    public int MaterializeEntity(
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int tick)
    {
        ValidateRelationshipKindIndex(kindId);
        ValidatePositive(subtypeId);
        ValidatePositive(cultureId);
        if (entityId == 0)
        {
            entityId = FindEntitySlot();
        }
        else if ((uint)entityId >= (uint)_entityAllocated.Length || _entityAllocated[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        _entityAllocated[entityId] = true;
        _entityStatus[entityId] = EntityStatusCodes.Active;
        _entityKindId[entityId] = kindId;
        _entitySubtypeId[entityId] = subtypeId;
        _entityCultureId[entityId] = cultureId;
        _entityPhysicalQ[entityId] = physicalQ;
        _entityPhysicalR[entityId] = physicalR;
        _entitySemanticQ[entityId] = semanticQ;
        _entitySemanticR[entityId] = semanticR;
        _entitySemanticS[entityId] = semanticS;
        _entityCreatedAt[entityId] = tick;
        _entityUpdatedAt[entityId] = tick;
        InsertEntityKindIndex(entityId, kindId);
        InsertPhysicalOccupant(entityId, physicalQ, physicalR);
        InsertSemanticOccupant(entityId, semanticQ, semanticR);
        if (entityId > _entityHighWatermark)
        {
            _entityHighWatermark = entityId;
        }

        AdvanceWorldVersion();
        return entityId;
    }

    public void SetEntityStatus(int entityId, int statusCode, int tick)
    {
        EnsureEntity(entityId);
        ValidatePositive(statusCode);
        var previous = _entityStatus[entityId];
        if (previous == statusCode)
        {
            return;
        }

        _entityStatus[entityId] = statusCode;
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityStatusChangedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            _entityKindId[entityId],
            _entitySubtypeId[entityId],
            previous,
            statusCode)));
    }

    public void MoveEntityPhysical(int entityId, int q, int r, int tick)
    {
        EnsureEntity(entityId);
        var fromQ = _entityPhysicalQ[entityId];
        var fromR = _entityPhysicalR[entityId];
        if (fromQ == q && fromR == r)
        {
            return;
        }

        _entityPhysicalQ[entityId] = q;
        _entityPhysicalR[entityId] = r;
        RemovePhysicalOccupant(entityId, fromQ, fromR);
        InsertPhysicalOccupant(entityId, q, r);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            PlaneCodes.Physical,
            fromQ,
            fromR,
            0,
            q,
            r,
            0)));
    }

    public void MoveEntitySemantic(int entityId, int q, int r, int s, int tick)
    {
        EnsureEntity(entityId);
        var fromQ = _entitySemanticQ[entityId];
        var fromR = _entitySemanticR[entityId];
        var fromS = _entitySemanticS[entityId];
        if (fromQ == q && fromR == r && fromS == s)
        {
            return;
        }

        _entitySemanticQ[entityId] = q;
        _entitySemanticR[entityId] = r;
        _entitySemanticS[entityId] = s;
        RemoveSemanticOccupant(entityId, fromQ, fromR);
        InsertSemanticOccupant(entityId, q, r);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityMovedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            PlaneCodes.Semantic,
            fromQ,
            fromR,
            fromS,
            q,
            r,
            s)));
    }

    public float GetAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        return _attributes.GetOrDefault(entityId, attributeId);
    }

    public bool HasAttribute(int entityId, int attributeId)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        return _attributes.TryGet(entityId, attributeId, out _);
    }

    public void SetAttribute(int entityId, int attributeId, int sourceCodeId, float value, int tick)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        ValidatePositive(sourceCodeId);
        var clamped = ClampAttribute(attributeId, value);
        var previous = _attributes.GetOrDefault(entityId, attributeId);
        if (global::System.MathF.Abs(clamped - previous) <= float.Epsilon)
        {
            return;
        }

        _attributes.Set(entityId, attributeId, clamped);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        Write(KernelEvent.From(new AttributeChangedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            attributeId,
            sourceCodeId,
            previous,
            clamped)));
    }

    public void ModifyAttribute(int entityId, int attributeId, int sourceCodeId, float delta, int tick)
    {
        var previous = GetAttribute(entityId, attributeId);
        SetAttribute(entityId, attributeId, sourceCodeId, previous + delta, tick);
    }

    public int EntityTagValue(int entityId, int tagKeyId)
    {
        EnsureEntity(entityId);
        ValidatePositive(tagKeyId);
        return _entityTags.GetOrDefault(entityId, tagKeyId);
    }

    public void SetEntityTag(int entityId, int tagKeyId, int tagValueId, int sourceCodeId, int tick)
    {
        EnsureEntity(entityId);
        ValidatePositive(tagKeyId);
        ValidatePositive(tagValueId);
        ValidatePositive(sourceCodeId);
        var previous = _entityTags.GetOrDefault(entityId, tagKeyId);
        if (previous == tagValueId)
        {
            return;
        }

        _entityTags.Set(entityId, tagKeyId, tagValueId);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
    }

    public float GetResource(int resourceId, int q, int r)
    {
        ValidatePositive(resourceId);
        return _resources.GetOrDefault(resourceId, CoordinateKey(q, r));
    }

    public void SetResource(int resourceId, int q, int r, int sourceCodeId, float value, int tick)
    {
        ValidatePositive(resourceId);
        ValidatePositive(sourceCodeId);
        var key = CoordinateKey(q, r);
        var clamped = value < 0f ? 0f : value;
        var previous = _resources.GetOrDefault(resourceId, key);
        if (global::System.MathF.Abs(clamped - previous) <= float.Epsilon)
        {
            return;
        }

        _resources.Set(resourceId, key, clamped);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new ResourceChangedKernelEvent(
            tick,
            WorldVersion,
            resourceId,
            q,
            r,
            sourceCodeId,
            previous,
            clamped)));
    }

    public void ModifyResource(int resourceId, int q, int r, int sourceCodeId, float delta, int tick)
    {
        var previous = GetResource(resourceId, q, r);
        SetResource(resourceId, q, r, sourceCodeId, previous + delta, tick);
    }

    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick,
        int factId0 = 0,
        int factId1 = 0,
        int factId2 = 0,
        int factId3 = 0,
        int factCount = 0)
    {
        ValidatePositive(kindId);
        EnsureEntity(sourceEntityId);
        EnsureEntity(targetEntityId);
        ValidateFactCount(factCount);
        var relationshipId = FindRelationshipSlot();
        _relationshipAllocated[relationshipId] = true;
        _relationshipActive[relationshipId] = true;
        _relationshipKindId[relationshipId] = kindId;
        _relationshipPolarityCode[relationshipId] = polarityCode;
        _relationshipSourceId[relationshipId] = sourceEntityId;
        _relationshipTargetId[relationshipId] = targetEntityId;
        _relationshipStrength[relationshipId] = Clamp01(strength);
        _relationshipStartTick[relationshipId] = tick;
        _relationshipEndTick[relationshipId] = 0;
        _relationshipFactId0[relationshipId] = factId0;
        _relationshipFactId1[relationshipId] = factId1;
        _relationshipFactId2[relationshipId] = factId2;
        _relationshipFactId3[relationshipId] = factId3;
        _relationshipFactCount[relationshipId] = factCount;
        if (relationshipId > _relationshipHighWatermark)
        {
            _relationshipHighWatermark = relationshipId;
        }

        AddRelationshipToGraphDelta(relationshipId);
        AdvanceWorldVersion();
        WriteRelationshipChanged(relationshipId, tick, _relationshipCreatedCodeId, 0f, _relationshipStrength[relationshipId]);
        return relationshipId;
    }

    public void CullRelationship(int relationshipId, int tick)
    {
        EnsureRelationship(relationshipId);
        if (!_relationshipActive[relationshipId])
        {
            return;
        }

        var previous = _relationshipStrength[relationshipId];
        _relationshipActive[relationshipId] = false;
        _relationshipEndTick[relationshipId] = tick;
        RemoveRelationshipFromGraphDelta(relationshipId);
        AdvanceWorldVersion();
        WriteRelationshipChanged(relationshipId, tick, _relationshipCulledCodeId, previous, 0f);
    }

    public void SetRelationshipStrength(int relationshipId, float strength, int tick)
    {
        EnsureRelationship(relationshipId);
        if (!_relationshipActive[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = _relationshipStrength[relationshipId];
        var next = Clamp01(strength);
        if (global::System.MathF.Abs(next - previous) <= float.Epsilon)
        {
            return;
        }

        _relationshipStrength[relationshipId] = next;
        AddRelationshipToGraphDelta(relationshipId);
        AdvanceWorldVersion();
        WriteRelationshipChanged(relationshipId, tick, _relationshipStrengthChangedCodeId, previous, next);
    }

    public int FindRelationship(int sourceEntityId, int targetEntityId, int kindId)
    {
        EnsureEntity(sourceEntityId);
        EnsureEntity(targetEntityId);
        ValidatePositive(kindId);
        var cursor = OutgoingRelationshipsOfKind(sourceEntityId, kindId);
        while (MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (_relationshipTargetId[relationshipId] == targetEntityId)
            {
                return relationshipId;
            }
        }

        return 0;
    }

    public int FirstEntityOfKind(int kindId)
    {
        EnsureKindIndex(kindId);
        return _firstEntityByKind[kindId];
    }

    public int NextEntityOfKind(int entityId)
    {
        EnsureEntity(entityId);
        return _nextEntityByKind[entityId];
    }

    public int FirstPhysicalEntityAt(int q, int r)
    {
        return _firstPhysicalEntityAtCell.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public int NextPhysicalEntityAt(int entityId)
    {
        EnsureEntity(entityId);
        return _nextPhysicalEntityAtCell[entityId];
    }

    public int FirstSemanticEntityAt(int q, int r)
    {
        return _firstSemanticEntityAtCell.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public int NextSemanticEntityAt(int entityId)
    {
        EnsureEntity(entityId);
        return _nextSemanticEntityAtCell[entityId];
    }

    public RelationshipGraphCursor OutgoingRelationshipsOfKind(int sourceEntityId, int kindId)
    {
        return OutgoingRelationshipsOfKindAbove(sourceEntityId, kindId, 0f);
    }

    public RelationshipGraphCursor IncomingRelationshipsOfKind(int targetEntityId, int kindId)
    {
        return IncomingRelationshipsOfKindAbove(targetEntityId, kindId, 0f);
    }

    public RelationshipGraphCursor OutgoingRelationshipsOfKindAbove(
        int sourceEntityId,
        int kindId,
        float minimumStrength)
    {
        var row = RelationshipRowIndex(sourceEntityId, kindId);
        ValidateRelationshipThreshold(minimumStrength);
        return new RelationshipGraphCursor
        {
            DirectionCode = OutgoingRelationshipDirectionCode,
            FrozenIndex = _outgoingFrozenOffsets[row],
            FrozenEnd = _outgoingFrozenOffsets[row + 1],
            DeltaIndex = _outgoingDeltaOffsets[row],
            DeltaEnd = _outgoingDeltaOffsets[row + 1],
            MinimumStrength = minimumStrength,
        };
    }

    public RelationshipGraphCursor IncomingRelationshipsOfKindAbove(
        int targetEntityId,
        int kindId,
        float minimumStrength)
    {
        var row = RelationshipRowIndex(targetEntityId, kindId);
        ValidateRelationshipThreshold(minimumStrength);
        return new RelationshipGraphCursor
        {
            DirectionCode = IncomingRelationshipDirectionCode,
            FrozenIndex = _incomingFrozenOffsets[row],
            FrozenEnd = _incomingFrozenOffsets[row + 1],
            DeltaIndex = _incomingDeltaOffsets[row],
            DeltaEnd = _incomingDeltaOffsets[row + 1],
            MinimumStrength = minimumStrength,
        };
    }

    public bool MoveNextRelationship(ref RelationshipGraphCursor cursor, out int relationshipId)
    {
        relationshipId = 0;
        while (cursor.FrozenIndex < cursor.FrozenEnd)
        {
            var frozenRelationshipId = FrozenCursorRelationshipId(cursor);
            if (_relationshipActive[frozenRelationshipId] && !_relationshipInDeltaGraph[frozenRelationshipId])
            {
                if (_relationshipStrength[frozenRelationshipId] < cursor.MinimumStrength)
                {
                    cursor.FrozenIndex = cursor.FrozenEnd;
                    break;
                }

                break;
            }

            cursor.FrozenIndex++;
        }

        while (cursor.DeltaIndex < cursor.DeltaEnd)
        {
            var deltaRelationshipId = DeltaCursorRelationshipId(cursor);
            if (_relationshipActive[deltaRelationshipId])
            {
                if (_relationshipStrength[deltaRelationshipId] < cursor.MinimumStrength)
                {
                    cursor.DeltaIndex = cursor.DeltaEnd;
                    break;
                }

                break;
            }

            cursor.DeltaIndex++;
        }

        var hasFrozen = cursor.FrozenIndex < cursor.FrozenEnd;
        var hasDelta = cursor.DeltaIndex < cursor.DeltaEnd;
        if (!hasFrozen && !hasDelta)
        {
            return false;
        }

        if (hasFrozen && (!hasDelta || RelationshipComesBefore(
                FrozenCursorRelationshipId(cursor),
                DeltaCursorRelationshipId(cursor))))
        {
            relationshipId = FrozenCursorRelationshipId(cursor);
            cursor.FrozenIndex++;
            return true;
        }

        relationshipId = DeltaCursorRelationshipId(cursor);
        cursor.DeltaIndex++;
        return true;
    }

    public bool HasOutgoingRelationshipOfKind(int sourceEntityId, int kindId)
    {
        var cursor = OutgoingRelationshipsOfKind(sourceEntityId, kindId);
        return MoveNextRelationship(ref cursor, out _);
    }

    public bool HasIncomingRelationshipOfKind(int targetEntityId, int kindId)
    {
        var cursor = IncomingRelationshipsOfKind(targetEntityId, kindId);
        return MoveNextRelationship(ref cursor, out _);
    }

    public void CullOutgoingRelationshipsOfKind(int sourceEntityId, int kindId, int tick)
    {
        EnsureEntity(sourceEntityId);
        ValidateRelationshipKindIndex(kindId);
        while (true)
        {
            var cursor = OutgoingRelationshipsOfKind(sourceEntityId, kindId);
            if (!MoveNextRelationship(ref cursor, out var relationshipId))
            {
                return;
            }

            CullRelationship(relationshipId, tick);
        }
    }

    public int TraverseOutgoingRelationshipsOfKind<TVisitor>(
        int startEntityId,
        int kindId,
        float minimumStrength,
        int maxDepth,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        return TraverseRelationshipsOfKind(
            startEntityId,
            kindId,
            minimumStrength,
            maxDepth,
            OutgoingRelationshipDirectionCode,
            ref visitor);
    }

    public int TraverseIncomingRelationshipsOfKind<TVisitor>(
        int startEntityId,
        int kindId,
        float minimumStrength,
        int maxDepth,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        return TraverseRelationshipsOfKind(
            startEntityId,
            kindId,
            minimumStrength,
            maxDepth,
            IncomingRelationshipDirectionCode,
            ref visitor);
    }

    public void CompactRelationshipGraph()
    {
        BuildRelationshipCsr(_outgoingFrozenOffsets, _outgoingFrozenRelationships, outgoing: true);
        BuildRelationshipCsr(_incomingFrozenOffsets, _incomingFrozenRelationships, outgoing: false);
        global::System.Array.Clear(_outgoingDeltaOffsets);
        global::System.Array.Clear(_incomingDeltaOffsets);
        global::System.Array.Clear(_outgoingDeltaRelationships);
        global::System.Array.Clear(_incomingDeltaRelationships);
        global::System.Array.Clear(_relationshipInDeltaGraph);
        _relationshipGraphDeltaCount = 0;
    }

    public bool EntityExists(int entityId)
    {
        return (uint)entityId < (uint)_entityAllocated.Length && _entityAllocated[entityId];
    }

    public bool EntityIsActive(int entityId)
    {
        return EntityExists(entityId) && _entityStatus[entityId] == EntityStatusCodes.Active;
    }

    public int EntityStatus(int entityId)
    {
        EnsureEntity(entityId);
        return _entityStatus[entityId];
    }

    public int EntityKindId(int entityId)
    {
        EnsureEntity(entityId);
        return _entityKindId[entityId];
    }

    public int EntitySubtypeId(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySubtypeId[entityId];
    }

    public int EntityCultureId(int entityId)
    {
        EnsureEntity(entityId);
        return _entityCultureId[entityId];
    }

    public int EntityCreatedAt(int entityId)
    {
        EnsureEntity(entityId);
        return _entityCreatedAt[entityId];
    }

    public int EntityPhysicalQ(int entityId)
    {
        EnsureEntity(entityId);
        return _entityPhysicalQ[entityId];
    }

    public int EntityPhysicalR(int entityId)
    {
        EnsureEntity(entityId);
        return _entityPhysicalR[entityId];
    }

    public int EntitySemanticQ(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticQ[entityId];
    }

    public int EntitySemanticR(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticR[entityId];
    }

    public int EntitySemanticS(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySemanticS[entityId];
    }

    public int EntityHighWatermark => _entityHighWatermark;

    internal int RelationshipHighWatermark => _relationshipHighWatermark;

    public bool RelationshipIsActive(int relationshipId)
    {
        return (uint)relationshipId < (uint)_relationshipActive.Length && _relationshipActive[relationshipId];
    }

    public int RelationshipKindId(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipKindId[relationshipId];
    }

    public int RelationshipSourceId(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipSourceId[relationshipId];
    }

    public int RelationshipTargetId(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipTargetId[relationshipId];
    }

    public float RelationshipStrength(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipStrength[relationshipId];
    }

    public bool RelationshipHasFact(int relationshipId, int factId)
    {
        EnsureRelationship(relationshipId);
        if (factId == 0)
        {
            return true;
        }

        return (_relationshipFactCount[relationshipId] > 0 && _relationshipFactId0[relationshipId] == factId) ||
               (_relationshipFactCount[relationshipId] > 1 && _relationshipFactId1[relationshipId] == factId) ||
               (_relationshipFactCount[relationshipId] > 2 && _relationshipFactId2[relationshipId] == factId) ||
               (_relationshipFactCount[relationshipId] > 3 && _relationshipFactId3[relationshipId] == factId);
    }

    public void AdvanceWorldVersion()
    {
        WorldVersion++;
    }

    private int FindEntitySlot()
    {
        for (var i = 1; i < _entityAllocated.Length; i++)
        {
            if (!_entityAllocated[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private int FindRelationshipSlot()
    {
        for (var i = 1; i < _relationshipAllocated.Length; i++)
        {
            if (!_relationshipAllocated[i])
            {
                return i;
            }
        }

        throw new global::System.InvalidOperationException();
    }

    private void InsertEntityKindIndex(int entityId, int kindId)
    {
        EnsureKindIndex(kindId);
        _nextEntityByKind[entityId] = _firstEntityByKind[kindId];
        _firstEntityByKind[kindId] = entityId;
    }

    private void InsertPhysicalOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        _nextPhysicalEntityAtCell[entityId] = _firstPhysicalEntityAtCell.GetOrDefault(key, 1);
        _firstPhysicalEntityAtCell.Set(key, 1, entityId);
    }

    private void RemovePhysicalOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        var current = _firstPhysicalEntityAtCell.GetOrDefault(key, 1);
        if (current == entityId)
        {
            _firstPhysicalEntityAtCell.Set(key, 1, _nextPhysicalEntityAtCell[entityId]);
            _nextPhysicalEntityAtCell[entityId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = _nextPhysicalEntityAtCell[current];
            if (next == entityId)
            {
                _nextPhysicalEntityAtCell[current] = _nextPhysicalEntityAtCell[entityId];
                _nextPhysicalEntityAtCell[entityId] = 0;
                return;
            }

            current = next;
        }
    }

    private void InsertSemanticOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        _nextSemanticEntityAtCell[entityId] = _firstSemanticEntityAtCell.GetOrDefault(key, 1);
        _firstSemanticEntityAtCell.Set(key, 1, entityId);
    }

    private void RemoveSemanticOccupant(int entityId, int q, int r)
    {
        var key = CoordinateKey(q, r);
        var current = _firstSemanticEntityAtCell.GetOrDefault(key, 1);
        if (current == entityId)
        {
            _firstSemanticEntityAtCell.Set(key, 1, _nextSemanticEntityAtCell[entityId]);
            _nextSemanticEntityAtCell[entityId] = 0;
            return;
        }

        while (current > 0)
        {
            var next = _nextSemanticEntityAtCell[current];
            if (next == entityId)
            {
                _nextSemanticEntityAtCell[current] = _nextSemanticEntityAtCell[entityId];
                _nextSemanticEntityAtCell[entityId] = 0;
                return;
            }

            current = next;
        }
    }

    private int TraverseRelationshipsOfKind<TVisitor>(
        int startEntityId,
        int kindId,
        float minimumStrength,
        int maxDepth,
        int directionCode,
        ref TVisitor visitor)
        where TVisitor : struct, IRelationshipTraversalVisitor
    {
        EnsureEntity(startEntityId);
        ValidateRelationshipKindIndex(kindId);
        ValidateRelationshipThreshold(minimumStrength);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        if (maxDepth == 0)
        {
            return 0;
        }

        var epoch = NextGraphTraversalEpoch();
        var currentFrontier = _graphFrontierA;
        var nextFrontier = _graphFrontierB;
        currentFrontier[0] = startEntityId;
        var currentCount = 1;
        var visitedCount = 0;
        _graphVisitedEpochs[startEntityId] = epoch;

        for (var depth = 0; depth < maxDepth && currentCount > 0; depth++)
        {
            var nextCount = 0;
            for (var i = 0; i < currentCount; i++)
            {
                var cursor = directionCode == OutgoingRelationshipDirectionCode
                    ? OutgoingRelationshipsOfKindAbove(currentFrontier[i], kindId, minimumStrength)
                    : IncomingRelationshipsOfKindAbove(currentFrontier[i], kindId, minimumStrength);
                while (MoveNextRelationship(ref cursor, out var relationshipId))
                {
                    var nextEntityId = directionCode == OutgoingRelationshipDirectionCode
                        ? _relationshipTargetId[relationshipId]
                        : _relationshipSourceId[relationshipId];
                    if (_graphVisitedEpochs[nextEntityId] == epoch)
                    {
                        continue;
                    }

                    _graphVisitedEpochs[nextEntityId] = epoch;
                    visitor.Visit(nextEntityId, relationshipId, depth + 1);
                    nextFrontier[nextCount] = nextEntityId;
                    nextCount++;
                    visitedCount++;
                }
            }

            var swap = currentFrontier;
            currentFrontier = nextFrontier;
            nextFrontier = swap;
            currentCount = nextCount;
        }

        return visitedCount;
    }

    private void AddRelationshipToGraphDelta(int relationshipId)
    {
        if (!_relationshipActive[relationshipId])
        {
            return;
        }

        RemoveRelationshipFromGraphDelta(relationshipId);
        InsertDeltaRelationship(
            _outgoingDeltaOffsets,
            _outgoingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipSourceId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        InsertDeltaRelationship(
            _incomingDeltaOffsets,
            _incomingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipTargetId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = true;
        _relationshipGraphDeltaCount++;
        if (_relationshipGraphDeltaMergeThreshold > 0 &&
            _relationshipGraphDeltaCount >= _relationshipGraphDeltaMergeThreshold)
        {
            CompactRelationshipGraph();
        }
    }

    private void RemoveRelationshipFromGraphDelta(int relationshipId)
    {
        if (!_relationshipInDeltaGraph[relationshipId])
        {
            return;
        }

        RemoveDeltaRelationship(
            _outgoingDeltaOffsets,
            _outgoingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipSourceId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        RemoveDeltaRelationship(
            _incomingDeltaOffsets,
            _incomingDeltaRelationships,
            RelationshipRowIndexRaw(_relationshipTargetId[relationshipId], _relationshipKindId[relationshipId]),
            relationshipId);
        _relationshipInDeltaGraph[relationshipId] = false;
        _relationshipGraphDeltaCount--;
    }

    private void InsertDeltaRelationship(int[] offsets, int[] relationships, int row, int relationshipId)
    {
        var end = offsets[^1];
        if (end >= relationships.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var start = offsets[row];
        var insert = offsets[row + 1];
        for (var i = end; i > insert; i--)
        {
            relationships[i] = relationships[i - 1];
        }

        while (insert > start && RelationshipComesBefore(relationshipId, relationships[insert - 1]))
        {
            relationships[insert] = relationships[insert - 1];
            insert--;
        }

        relationships[insert] = relationshipId;
        for (var i = row + 1; i < offsets.Length; i++)
        {
            offsets[i]++;
        }
    }

    private void RemoveDeltaRelationship(int[] offsets, int[] relationships, int row, int relationshipId)
    {
        var start = offsets[row];
        var end = offsets[row + 1];
        for (var slot = start; slot < end; slot++)
        {
            if (relationships[slot] != relationshipId)
            {
                continue;
            }

            for (var i = slot; i < offsets[^1] - 1; i++)
            {
                relationships[i] = relationships[i + 1];
            }

            relationships[offsets[^1] - 1] = 0;
            for (var i = row + 1; i < offsets.Length; i++)
            {
                offsets[i]--;
            }

            return;
        }
    }

    private void BuildRelationshipCsr(int[] offsets, int[] relationships, bool outgoing)
    {
        global::System.Array.Clear(offsets);
        global::System.Array.Clear(relationships);
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipActive[relationshipId])
            {
                continue;
            }

            var entityId = outgoing ? _relationshipSourceId[relationshipId] : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndexRaw(entityId, _relationshipKindId[relationshipId]);
            offsets[row + 1]++;
        }

        for (var i = 1; i < offsets.Length; i++)
        {
            offsets[i] += offsets[i - 1];
        }

        global::System.Array.Copy(offsets, _relationshipBuildCursors, offsets.Length);
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipActive[relationshipId])
            {
                continue;
            }

            var entityId = outgoing ? _relationshipSourceId[relationshipId] : _relationshipTargetId[relationshipId];
            var row = RelationshipRowIndexRaw(entityId, _relationshipKindId[relationshipId]);
            var slot = _relationshipBuildCursors[row];
            relationships[slot] = relationshipId;
            _relationshipBuildCursors[row]++;
        }

        SortRelationshipCsrRows(offsets, relationships);
    }

    private void SortRelationshipCsrRows(int[] offsets, int[] relationships)
    {
        for (var row = 0; row < offsets.Length - 1; row++)
        {
            var start = offsets[row];
            var end = offsets[row + 1];
            for (var i = start + 1; i < end; i++)
            {
                var relationshipId = relationships[i];
                var j = i - 1;
                while (j >= start && RelationshipComesBefore(relationshipId, relationships[j]))
                {
                    relationships[j + 1] = relationships[j];
                    j--;
                }

                relationships[j + 1] = relationshipId;
            }
        }
    }

    private int FrozenCursorRelationshipId(RelationshipGraphCursor cursor)
    {
        return cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _outgoingFrozenRelationships[cursor.FrozenIndex]
            : _incomingFrozenRelationships[cursor.FrozenIndex];
    }

    private int DeltaCursorRelationshipId(RelationshipGraphCursor cursor)
    {
        return cursor.DirectionCode == OutgoingRelationshipDirectionCode
            ? _outgoingDeltaRelationships[cursor.DeltaIndex]
            : _incomingDeltaRelationships[cursor.DeltaIndex];
    }

    private bool RelationshipComesBefore(int leftRelationshipId, int rightRelationshipId)
    {
        var leftStrength = _relationshipStrength[leftRelationshipId];
        var rightStrength = _relationshipStrength[rightRelationshipId];
        if (leftStrength > rightStrength)
        {
            return true;
        }

        if (leftStrength < rightStrength)
        {
            return false;
        }

        return leftRelationshipId < rightRelationshipId;
    }

    private int RelationshipRowIndex(int entityId, int kindId)
    {
        EnsureEntity(entityId);
        ValidateRelationshipKindIndex(kindId);
        return RelationshipRowIndexRaw(entityId, kindId);
    }

    private int RelationshipRowIndexRaw(int entityId, int kindId)
    {
        return (kindId - 1) * _relationshipEntityCapacity + entityId - 1;
    }

    private int NextGraphTraversalEpoch()
    {
        _graphTraversalEpoch++;
        if (_graphTraversalEpoch <= 0)
        {
            global::System.Array.Clear(_graphVisitedEpochs);
            _graphTraversalEpoch = 1;
        }

        return _graphTraversalEpoch;
    }

    private void EnsureEntity(int entityId)
    {
        if (!EntityExists(entityId))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureRelationship(int relationshipId)
    {
        if ((uint)relationshipId >= (uint)_relationshipAllocated.Length || !_relationshipAllocated[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureKindIndex(int kindId)
    {
        ValidatePositive(kindId);
        if ((uint)kindId >= (uint)_firstEntityByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void ValidateRelationshipKindIndex(int kindId)
    {
        ValidatePositive(kindId);
        if ((uint)kindId > (uint)_relationshipKindCapacity)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureTerrain(int terrainId)
    {
        if ((uint)terrainId >= (uint)_terrainRegistered.Length || !_terrainRegistered[terrainId])
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void WriteRelationshipChanged(
        int relationshipId,
        int tick,
        int changeCodeId,
        float previousStrength,
        float newStrength)
    {
        Write(KernelEvent.From(new RelationshipChangedKernelEvent(
            tick,
            WorldVersion,
            changeCodeId,
            _relationshipKindId[relationshipId],
            _relationshipPolarityCode[relationshipId],
            _relationshipSourceId[relationshipId],
            _relationshipTargetId[relationshipId],
            _relationshipFactId0[relationshipId],
            _relationshipFactId1[relationshipId],
            _relationshipFactId2[relationshipId],
            _relationshipFactId3[relationshipId],
            _relationshipFactCount[relationshipId],
            previousStrength,
            newStrength)));
    }

    private void Write(in KernelEvent kernelEvent)
    {
        if (!_events.TryWrite(kernelEvent))
        {
            throw new global::System.InvalidOperationException();
        }
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

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateFactCount(int factCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(factCount);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(factCount, 4);
    }

    private static void ValidateRelationshipThreshold(float minimumStrength)
    {
        if (global::System.Single.IsNaN(minimumStrength))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private float ClampAttribute(int attributeId, float value)
    {
        if ((uint)attributeId < (uint)_attributeBoundsRegistered.Length && _attributeBoundsRegistered[attributeId])
        {
            if (value < _attributeMinimums[attributeId])
            {
                return _attributeMinimums[attributeId];
            }

            if (value > _attributeMaximums[attributeId])
            {
                return _attributeMaximums[attributeId];
            }
        }

        return value;
    }
}
