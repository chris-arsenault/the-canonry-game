namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    private const int OutgoingRelationshipDirectionCode = 1;
    private const int IncomingRelationshipDirectionCode = 2;

    private readonly KernelEventLog _events;
    private readonly PrimitiveFloatTable _attributes;
    private readonly PrimitiveFloatTable _resources;
    private readonly PrimitiveIntTable _mapCellTerrainIds;
    private readonly PrimitiveIntTable _mapCellSuperregionIds;
    private readonly PrimitiveIntTable _mapCellContinentIds;
    private readonly MapLayerSet _mapLayers;
    private readonly KernelDataSet _kernelData;
    private bool[] _entityAllocated = [];
    private int[] _entityStatus = [];
    private int[] _entityKindId = [];
    private int[] _entitySubtypeId = [];
    private int[] _entityCultureId = [];
    private int[] _entityPhysicalQ = [];
    private int[] _entityPhysicalR = [];
    private int[] _entitySemanticQ = [];
    private int[] _entitySemanticR = [];
    private int[] _entitySemanticS = [];
    private int[] _entityCreatedAt = [];
    private int[] _entityUpdatedAt = [];
    private int[] _firstEntityByKind = [];
    private int[] _nextEntityByKind = [];
    private PrimitiveIntTable _firstPhysicalEntityAtCell = null!;
    private PrimitiveIntTable _firstSemanticEntityAtCell = null!;
    private int[] _nextPhysicalEntityAtCell = [];
    private int[] _nextSemanticEntityAtCell = [];
    private bool[] _relationshipAllocated = [];
    private bool[] _relationshipActive = [];
    private int[] _relationshipKindId = [];
    private int[] _relationshipPolarityCode = [];
    private int[] _relationshipSourceId = [];
    private int[] _relationshipTargetId = [];
    private int[] _relationshipFactId0 = [];
    private int[] _relationshipFactId1 = [];
    private int[] _relationshipFactId2 = [];
    private int[] _relationshipFactId3 = [];
    private int[] _relationshipFactCount = [];
    private int[] _relationshipStartTick = [];
    private int[] _relationshipEndTick = [];
    private float[] _relationshipStrength = [];
    private bool[] _relationshipInDeltaGraph = [];
    private int[] _outgoingFrozenOffsets = [];
    private int[] _incomingFrozenOffsets = [];
    private int[] _outgoingFrozenRelationships = [];
    private int[] _incomingFrozenRelationships = [];
    private int[] _outgoingDeltaOffsets = [];
    private int[] _incomingDeltaOffsets = [];
    private int[] _outgoingDeltaRelationships = [];
    private int[] _incomingDeltaRelationships = [];
    private int[] _relationshipBuildCursors = [];
    private EpochStampSet _graphVisited;
    private int[] _graphFrontierA = [];
    private int[] _graphFrontierB = [];
    private int _relationshipCreatedCodeId;
    private int _relationshipCulledCodeId;
    private int _relationshipStrengthChangedCodeId;
    private int _defaultSourceCodeId;
    private int _relationshipEntityCapacity;
    private int _relationshipKindCapacity;
    private int _relationshipGraphDeltaMergeThreshold;
    private int _entityHighWatermark;
    private int _relationshipHighWatermark;
    private int _mapRadius;
    private int _relationshipGraphDeltaCount;

    public WorldState(KernelEventLog events, SimulationOptions options, KernelDataSet kernelData)
    {
        global::System.ArgumentNullException.ThrowIfNull(events); global::System.ArgumentNullException.ThrowIfNull(options);
        global::System.ArgumentNullException.ThrowIfNull(kernelData); _events = events; _kernelData = kernelData;
        _attributes = new PrimitiveFloatTable(options.AttributeCapacity);
        _resources = new PrimitiveFloatTable(options.ResourceCapacity);
        _mapCellTerrainIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellSuperregionIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellContinentIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapLayers = new MapLayerSet(options.EntityCapacity, options.MapCellCapacity, options.MapInfluenceCapacity);
        InitializeEntityStorage(options);
        InitializeRelationshipStorage(options);
        InitializeRelationshipGraphStorage(options);
        InitializeRelationshipRuntimeCodes(options);
    }

    private void InitializeEntityStorage(SimulationOptions options)
    {
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
    }

    private void InitializeRelationshipStorage(SimulationOptions options)
    {
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
    }

    private void InitializeRelationshipGraphStorage(SimulationOptions options)
    {
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
        _graphVisited = new EpochStampSet(options.EntityCapacity + 1);
        _graphFrontierA = new int[options.EntityCapacity + 1];
        _graphFrontierB = new int[options.EntityCapacity + 1];
    }

    private void InitializeRelationshipRuntimeCodes(SimulationOptions options)
    {
        _relationshipCreatedCodeId = options.RuntimeCodes.RelationshipCreatedCodeId;
        _relationshipCulledCodeId = options.RuntimeCodes.RelationshipCulledCodeId;
        _relationshipStrengthChangedCodeId = options.RuntimeCodes.RelationshipStrengthChangedCodeId;
        _defaultSourceCodeId = options.RuntimeCodes.StartupSourceCodeId;
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

    public void SetCell(int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, CoverageOwnerAt(q, r), CellSuperregionId(q, r), CellContinentId(q, r));
    }

    public bool CellIsKnown(int q, int r)
    {
        return _mapCellTerrainIds.TryGet(CoordinateKey(q, r), 1, out _);
    }

    public int CellTerrainId(int q, int r)
    {
        return _mapCellTerrainIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public void SetCellTerrain(int q, int r, int terrainId)
    {
        ValidatePositive(terrainId);
        WriteCell(q, r, terrainId, CoverageOwnerAt(q, r), CellSuperregionId(q, r), CellContinentId(q, r));
    }

    public void SetCellTerrain(int q, int r, int terrainId, int tick, in KernelExecutionContext context)
    {
        SetCellState(q, r, terrainId, CoverageOwnerAt(q, r), tick, context);
    }

    public void MaterializeCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId = 0,
        int continentId = 0)
    {
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(superregionId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(continentId);
        WriteCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
    }

    private void WriteCell(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId)
    {
        var key = CoordinateKey(q, r);
        _mapCellTerrainIds.Set(key, 1, terrainId);
        _mapCellSuperregionIds.Set(key, 1, superregionId);
        _mapCellContinentIds.Set(key, 1, continentId);
        _mapLayers.Coverage.SetValue(key, q, r, ownerEntityId);
    }

    internal int CellSuperregionId(int q, int r)
    {
        return _mapCellSuperregionIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    internal int CellContinentId(int q, int r)
    {
        return _mapCellContinentIds.GetOrDefault(CoordinateKey(q, r), 1);
    }

    public int TerrainBoundaryGroupId(int terrainId)
    {
        return _kernelData.Terrain.BoundaryGroupId(terrainId);
    }

    internal int CoverageOwnerAt(int q, int r)
    {
        return _mapLayers.Coverage.ValueAt(CoordinateKey(q, r));
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);
        _mapLayers.Coverage.SetValue(CoordinateKey(q, r), q, r, ownerEntityId);
    }

    internal void SetCoverageOwner(int q, int r, int ownerEntityId, int tick, in KernelExecutionContext context)
    {
        SetCellState(q, r, CellTerrainId(q, r), ownerEntityId, tick, context);
    }

    internal void SetCellState(
        int q,
        int r,
        int terrainId,
        int ownerEntityId,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(ownerEntityId);

        var previousTerrainId = CellTerrainId(q, r);
        var previousOwnerEntityId = CoverageOwnerAt(q, r);
        var superregionId = CellSuperregionId(q, r);
        var continentId = CellContinentId(q, r);
        if (previousTerrainId == terrainId && previousOwnerEntityId == ownerEntityId)
        {
            return;
        }

        WriteCell(q, r, terrainId, ownerEntityId, superregionId, continentId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new CellChangedKernelEvent(
            tick,
            WorldVersion,
            q,
            r,
            context.SourceCodeId,
            previousTerrainId,
            terrainId,
            previousOwnerEntityId,
            ownerEntityId,
            superregionId,
            continentId)),
            context);
    }

    internal int CoverageOwnerCellCount(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId);
    }

    internal bool CoverageOwnerHasCells(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.ValueCellCount(ownerEntityId) > 0;
    }

    internal SpatialLayerCellCursor CoverageCells(int ownerEntityId)
    {
        EnsureEntity(ownerEntityId);
        return _mapLayers.Coverage.CellsOfValue(ownerEntityId);
    }

    internal bool MoveNextCoverageCell(ref SpatialLayerCellCursor cursor, out int q, out int r)
    {
        return _mapLayers.Coverage.MoveNextCell(ref cursor, out q, out r);
    }

    public float GetCellInfluence(int entityId, int planeCode, int q, int r)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return _mapLayers.Influence.ValueAt(entityId, planeCode, CoordinateKey(q, r));
    }

    public void SetCellInfluence(
        int entityId,
        int planeCode,
        int q,
        int r,
        float value,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        ValidatePositive(context.SourceCodeId);
        var key = CoordinateKey(q, r);
        var clamped = ClampInfluence(value);
        var previous = _mapLayers.Influence.ValueAt(entityId, planeCode, key);
        if (global::System.MathF.Abs(clamped - previous) <= float.Epsilon)
        {
            return;
        }

        _mapLayers.Influence.SetValue(entityId, planeCode, key, clamped);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new CellInfluenceChangedKernelEvent(
            tick,
            WorldVersion,
            q,
            r,
            context.SourceCodeId,
            planeCode,
            entityId,
            context.ActionId,
            previous,
            clamped)),
            context);
    }

    public void ModifyCellInfluence(
        int entityId,
        int planeCode,
        int q,
        int r,
        float delta,
        int tick,
        in KernelExecutionContext context)
    {
        var previous = GetCellInfluence(entityId, planeCode, q, r);
        SetCellInfluence(entityId, planeCode, q, r, previous + delta, tick, context);
    }

    public void BeginTick(int tick)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(tick);
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

    public void SetEntityStatus(int entityId, int statusCode, int tick) =>
        SetEntityStatus(entityId, statusCode, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void SetEntityStatus(int entityId, int statusCode, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
            context.SourceCodeId,
            _entityKindId[entityId],
            _entitySubtypeId[entityId],
            previous,
            statusCode)),
            context);
    }

    public void MoveEntityPhysical(int entityId, int q, int r, int tick) =>
        MoveEntityPhysical(entityId, q, r, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void MoveEntityPhysical(int entityId, int q, int r, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
            context.SourceCodeId,
            PlaneCodes.Physical,
            fromQ,
            fromR,
            0,
            q,
            r,
            0)),
            context);
    }

    public void MoveEntitySemantic(int entityId, int q, int r, int s, int tick) =>
        MoveEntitySemantic(entityId, q, r, s, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void MoveEntitySemantic(int entityId, int q, int r, int s, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
            context.SourceCodeId,
            PlaneCodes.Semantic,
            fromQ,
            fromR,
            fromS,
            q,
            r,
            s)),
            context);
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

    public void SetAttribute(int entityId, int attributeId, float value, int tick, in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidatePositive(attributeId);
        ValidatePositive(context.SourceCodeId);
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
            context.SourceCodeId,
            previous,
            clamped)),
            context);
    }

    public void ModifyAttribute(int entityId, int attributeId, float delta, int tick, in KernelExecutionContext context)
    {
        var previous = GetAttribute(entityId, attributeId);
        SetAttribute(entityId, attributeId, previous + delta, tick, context);
    }

    public float GetResource(int resourceId, int q, int r)
    {
        ValidatePositive(resourceId);
        return _resources.GetOrDefault(resourceId, CoordinateKey(q, r));
    }

    public void SetResource(int resourceId, int q, int r, float value, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(resourceId);
        ValidatePositive(context.SourceCodeId);
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
            context.SourceCodeId,
            previous,
            clamped)),
            context);
    }

    public void ModifyResource(int resourceId, int q, int r, float delta, int tick, in KernelExecutionContext context)
    {
        var previous = GetResource(resourceId, q, r);
        SetResource(resourceId, q, r, previous + delta, tick, context);
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
        return CreateRelationship(
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount,
            new KernelExecutionContext(0, _defaultSourceCodeId));
    }

    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick,
        int factId0,
        int factId1,
        int factId2,
        int factId3,
        int factCount,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
        WriteRelationshipChanged(
            relationshipId,
            tick,
            _relationshipCreatedCodeId,
            context,
            0f,
            _relationshipStrength[relationshipId]);
        return relationshipId;
    }

    public void CullRelationship(int relationshipId, int tick) =>
        CullRelationship(relationshipId, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void CullRelationship(int relationshipId, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
        WriteRelationshipChanged(relationshipId, tick, _relationshipCulledCodeId, context, previous, 0f);
    }

    public void SetRelationshipStrength(int relationshipId, float strength, int tick) =>
        SetRelationshipStrength(relationshipId, strength, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void SetRelationshipStrength(int relationshipId, float strength, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
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
        AdvanceWorldVersion();
        WriteRelationshipChanged(
            relationshipId,
            tick,
            _relationshipStrengthChangedCodeId,
            context,
            previous,
            next);
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
        AdvanceFrozenRelationshipCursor(ref cursor);
        AdvanceDeltaRelationshipCursor(ref cursor);

        if (!TrySelectNextRelationship(ref cursor, out relationshipId))
        {
            return false;
        }

        return true;
    }

    private void AdvanceFrozenRelationshipCursor(ref RelationshipGraphCursor cursor)
    {
        while (cursor.FrozenIndex < cursor.FrozenEnd)
        {
            var frozenRelationshipId = FrozenCursorRelationshipId(cursor);
            if (_relationshipActive[frozenRelationshipId] && !_relationshipInDeltaGraph[frozenRelationshipId])
            {
                if (_relationshipStrength[frozenRelationshipId] >= cursor.MinimumStrength)
                {
                    break;
                }
            }

            cursor.FrozenIndex++;
        }
    }

    private void AdvanceDeltaRelationshipCursor(ref RelationshipGraphCursor cursor)
    {
        while (cursor.DeltaIndex < cursor.DeltaEnd)
        {
            var deltaRelationshipId = DeltaCursorRelationshipId(cursor);
            if (_relationshipActive[deltaRelationshipId])
            {
                if (_relationshipStrength[deltaRelationshipId] >= cursor.MinimumStrength)
                {
                    break;
                }
            }

            cursor.DeltaIndex++;
        }
    }

    private bool TrySelectNextRelationship(ref RelationshipGraphCursor cursor, out int relationshipId)
    {
        relationshipId = 0;
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

    public void CullOutgoingRelationshipsOfKind(int sourceEntityId, int kindId, int tick) =>
        CullOutgoingRelationshipsOfKind(sourceEntityId, kindId, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void CullOutgoingRelationshipsOfKind(
        int sourceEntityId,
        int kindId,
        int tick,
        in KernelExecutionContext context)
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

            CullRelationship(relationshipId, tick, context);
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

    public int RelationshipPolarityCode(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipPolarityCode[relationshipId];
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

}
