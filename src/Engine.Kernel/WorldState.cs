namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class WorldState
{
    private const int OutgoingRelationshipDirectionCode = 1;
    private const int IncomingRelationshipDirectionCode = 2;

    private readonly KernelEventLog _events;
    private readonly PrimitiveFloatTable _attributes;
    private readonly PrimitiveIntTable _traits;
    private readonly PrimitiveIntTable _mapCellTerrainIds;
    private readonly PrimitiveIntTable _mapCellSuperregionIds;
    private readonly PrimitiveIntTable _mapCellContinentIds;
    private readonly MapLayerSet _mapLayers;
    private readonly KernelDataSet _kernelData;
    private readonly KernelGrowableStoragePolicy _growableStoragePolicy;
    private CultureBoundaryProgramRuntime? _cultureBoundaries;
    private bool[] _entityAllocated = [];
    private int[] _entityStatus = [];
    private int[] _entityKindId = [];
    private int[] _entitySubtypeId = [];
    private int[] _entityCultureId = [];
    private float[] _entityCultureInfluenceMultiplierDeltas = [];
    private float[] _entityPhysicalInfluenceProduction = [];
    private float[] _entitySemanticInfluenceProduction = [];
    private float[] _cultureInfluenceMultiplierDeltas = [];
    private int[] _entityPhysicalQ = [];
    private int[] _entityPhysicalR = [];
    private int[] _entitySemanticQ = [];
    private int[] _entitySemanticR = [];
    private int[] _entitySemanticS = [];
    private int[] _entityPhysicalZ = [];
    private int[] _entityAnchorPhysicalQ = [];
    private int[] _entityAnchorPhysicalR = [];
    private int[] _entityAnchorSemanticQ = [];
    private int[] _entityAnchorSemanticR = [];
    private float[] _entityFavorMultiplierDelta = [];
    private int[] _entityFavorExpiresAtTick = [];
    private int[] _entityCreatedAt = [];
    private int[] _entityUpdatedAt = [];
    private int[] _firstEntityByKind = [];
    private int[] _nextEntityByKind = [];
    private int[] _firstEntityByCulture = [];
    private int[] _nextEntityByCulture = [];
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
    private int[] _relationshipStartTick = [];
    private int[] _relationshipEndTick = [];
    private float[] _relationshipStrength = [];
    private int[] _entityCreatedByInvocationId = [];
    private int[] _relationshipCreatedByInvocationId = [];
    private int[] _relationshipLastChangedByInvocationId = [];
    private int[] _provenanceInvocationTick = [];
    private int[] _provenanceInvocationProducerKindCode = [];
    private int[] _provenanceInvocationProducerId = [];
    private int[] _provenanceInvocationProgramId = [];
    private int[] _provenanceInvocationSourceCodeId = [];
    private int[] _provenanceInvocationActionId = [];
    private int[] _provenanceInvocationPrimaryEntityId = [];
    private int[] _provenanceInvocationSecondaryEntityId = [];
    private int[] _provenanceInvocationParentInvocationId = [];
    private int[] _provenanceInvocationTriggerKindCode = [];
    private int[] _provenanceInvocationTriggerId = [];
    private int[] _provenanceInvocationFirstOutputId = [];
    private int[] _provenanceInvocationLastOutputId = [];
    private int[] _provenanceInvocationOutputCount = [];
    private int[] _provenanceOutputInvocationId = [];
    private int[] _provenanceOutputKindCode = [];
    private int[] _provenanceOutputId = [];
    private int[] _provenanceOutputSubjectEntityId = [];
    private int[] _provenanceOutputSecondaryEntityId = [];
    private int[] _provenanceOutputValueId = [];
    private int[] _provenanceOutputChangeCodeId = [];
    private int[] _provenanceOutputNextId = [];
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
    private int _cultureHighWatermark;
    private int[] _entityTagBits = [];
    private int _relationshipHighWatermark;
    private int _provenanceInvocationHighWatermark;
    private int _provenanceOutputHighWatermark;
    private int _mapRadius;
    private int _relationshipGraphDeltaCount;

    public WorldState(KernelEventLog events, SimulationOptions options, KernelDataSet kernelData)
    {
        global::System.ArgumentNullException.ThrowIfNull(events); global::System.ArgumentNullException.ThrowIfNull(options);
        global::System.ArgumentNullException.ThrowIfNull(kernelData); _events = events; _kernelData = kernelData;
        _growableStoragePolicy = KernelGrowableStoragePolicy.FromOptions(options);
        _attributes = new PrimitiveFloatTable(options.AttributeCapacity);
        _traits = new PrimitiveIntTable(options.TraitCapacity);
        _mapCellTerrainIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellSuperregionIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapCellContinentIds = new PrimitiveIntTable(options.MapCellCapacity);
        _mapLayers = new MapLayerSet(
            options.EntityCapacity,
            options.CultureIdCapacity,
            options.MapCellCapacity,
            options.MapInfluenceCapacity);
        InitializeEntityStorage(options);
        InitializeRelationshipStorage(options);
        InitializeRelationshipGraphStorage(options);
        InitializeProvenanceStorage(options);
        InitializeRelationshipRuntimeCodes(options);
    }

    private void InitializeEntityStorage(SimulationOptions options)
    {
        _entityAllocated = new bool[options.EntityCapacity + 1];
        _entityStatus = new int[options.EntityCapacity + 1];
        _entityTagBits = new int[options.EntityCapacity + 1];
        _entityKindId = new int[options.EntityCapacity + 1];
        _entitySubtypeId = new int[options.EntityCapacity + 1];
        _entityCultureId = new int[options.EntityCapacity + 1];
        _entityCultureInfluenceMultiplierDeltas = new float[options.EntityCapacity + 1];
        _entityPhysicalInfluenceProduction = new float[options.EntityCapacity + 1];
        _entitySemanticInfluenceProduction = new float[options.EntityCapacity + 1];
        _cultureInfluenceMultiplierDeltas = new float[options.CultureIdCapacity + 1];
        _entityPhysicalQ = new int[options.EntityCapacity + 1];
        _entityPhysicalR = new int[options.EntityCapacity + 1];
        _entitySemanticQ = new int[options.EntityCapacity + 1];
        _entitySemanticR = new int[options.EntityCapacity + 1];
        _entitySemanticS = new int[options.EntityCapacity + 1];
        _entityPhysicalZ = new int[options.EntityCapacity + 1];
        _entityAnchorPhysicalQ = new int[options.EntityCapacity + 1];
        _entityAnchorPhysicalR = new int[options.EntityCapacity + 1];
        _entityAnchorSemanticQ = new int[options.EntityCapacity + 1];
        _entityAnchorSemanticR = new int[options.EntityCapacity + 1];
        _entityFavorMultiplierDelta = new float[options.EntityCapacity + 1];
        _entityFavorExpiresAtTick = new int[options.EntityCapacity + 1];
        _entityCreatedAt = new int[options.EntityCapacity + 1];
        _entityUpdatedAt = new int[options.EntityCapacity + 1];
        _firstEntityByKind = new int[options.KindIdCapacity + 1];
        _nextEntityByKind = new int[options.EntityCapacity + 1];
        _firstEntityByCulture = new int[options.CultureIdCapacity + 1];
        _nextEntityByCulture = new int[options.EntityCapacity + 1];
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

    internal void AttachCultureBoundaries(CultureBoundaryProgramRuntime cultureBoundaries)
    {
        global::System.ArgumentNullException.ThrowIfNull(cultureBoundaries);
        _cultureBoundaries = cultureBoundaries;
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
        int tick,
        in KernelExecutionContext context)
    {
        ValidateRelationshipKindIndex(kindId);
        ValidatePositive(subtypeId);
        EnsureCultureIndex(cultureId);
        if (entityId == 0)
        {
            entityId = FindEntitySlot();
        }
        else if ((uint)entityId >= (uint)_entityAllocated.Length || _entityAllocated[entityId])
        {
            throw new global::System.InvalidOperationException();
        }

        EnsureProvenanceOutputCapacity(context);
        _entityAllocated[entityId] = true;
        _entityStatus[entityId] = EntityStatusCodes.Active;
        _entityKindId[entityId] = kindId;
        _entitySubtypeId[entityId] = subtypeId;
        _entityCultureId[entityId] = cultureId;
        _cultureHighWatermark = global::System.Math.Max(_cultureHighWatermark, cultureId);
        _entityPhysicalQ[entityId] = physicalQ;
        _entityPhysicalR[entityId] = physicalR;
        _entitySemanticQ[entityId] = semanticQ;
        _entitySemanticR[entityId] = semanticR;
        _entitySemanticS[entityId] = semanticS;
        _entityAnchorPhysicalQ[entityId] = physicalQ;
        _entityAnchorPhysicalR[entityId] = physicalR;
        _entityAnchorSemanticQ[entityId] = semanticQ;
        _entityAnchorSemanticR[entityId] = semanticR;
        _entityCreatedAt[entityId] = tick;
        _entityUpdatedAt[entityId] = tick;
        InsertEntityKindIndex(entityId, kindId);
        InsertEntityCultureIndex(entityId, cultureId);
        InsertPhysicalOccupant(entityId, physicalQ, physicalR);
        InsertSemanticOccupant(entityId, semanticQ, semanticR);
        if (entityId > _entityHighWatermark)
        {
            _entityHighWatermark = entityId;
        }

        AdvanceWorldVersion();
        RecordProvenanceEntityCreated(entityId, kindId, context);
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

        EnsureProvenanceOutputCapacity(context);
        _entityStatus[entityId] = statusCode;
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.EntityStatusChanged,
            entityId,
            entityId,
            0,
            statusCode,
            ProvenanceOutputKindCodes.EntityStatusChanged);
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

    public void SetEntityCulture(int entityId, int cultureId, int tick) =>
        SetEntityCulture(entityId, cultureId, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void SetEntityCulture(int entityId, int cultureId, int tick, in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureEntity(entityId);
        EnsureCultureIndex(cultureId);
        var previous = _entityCultureId[entityId];
        if (previous == cultureId)
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        RemoveEntityCultureIndex(entityId, previous);
        _entityCultureId[entityId] = cultureId;
        _cultureHighWatermark = global::System.Math.Max(_cultureHighWatermark, cultureId);
        InsertEntityCultureIndex(entityId, cultureId);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.EntityCultureChanged,
            entityId,
            entityId,
            0,
            cultureId,
            ProvenanceOutputKindCodes.EntityCultureChanged);
        Write(KernelEvent.From(new EntityCultureChangedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            context.SourceCodeId,
            _entityKindId[entityId],
            _entitySubtypeId[entityId],
            previous,
            cultureId)),
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

        EnsureProvenanceOutputCapacity(context);
        _attributes.Set(entityId, attributeId, clamped);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.AttributeChanged,
            entityId,
            entityId,
            0,
            attributeId,
            ProvenanceOutputKindCodes.AttributeChanged);
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

    public bool HasTrait(int entityId, int traitId)
    {
        EnsureEntity(entityId);
        ValidatePositive(traitId);
        return _traits.GetOrDefault(entityId, traitId) != 0;
    }

    public void AddTrait(int entityId, int traitId, int tick)
    {
        AddTrait(entityId, traitId, tick, new KernelExecutionContext(0, _defaultSourceCodeId));
    }

    public void AddTrait(int entityId, int traitId, int tick, in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        ValidatePositive(traitId);
        ValidatePositive(context.SourceCodeId);
        if (HasTrait(entityId, traitId))
        {
            return;
        }

        EnsureProvenanceOutputCapacity(context);
        _traits.Set(entityId, traitId, 1);
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.TraitAssigned,
            entityId,
            entityId,
            0,
            traitId,
            ProvenanceOutputKindCodes.TraitAssigned);
        Write(KernelEvent.From(new TraitAssignedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            traitId,
            context.SourceCodeId)),
            context);
    }

    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick)
    {
        return CreateRelationship(
            kindId,
            polarityCode,
            sourceEntityId,
            targetEntityId,
            strength,
            tick,
            new KernelExecutionContext(0, _defaultSourceCodeId));
    }

    public int CreateRelationship(
        int kindId,
        int polarityCode,
        int sourceEntityId,
        int targetEntityId,
        float strength,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        ValidatePositive(kindId);
        EnsureEntity(sourceEntityId);
        EnsureEntity(targetEntityId);
        var relationshipId = FindRelationshipSlot();
        EnsureProvenanceOutputCapacity(context);
        _relationshipAllocated[relationshipId] = true;
        _relationshipActive[relationshipId] = true;
        _relationshipKindId[relationshipId] = kindId;
        _relationshipPolarityCode[relationshipId] = polarityCode;
        _relationshipSourceId[relationshipId] = sourceEntityId;
        _relationshipTargetId[relationshipId] = targetEntityId;
        _relationshipStrength[relationshipId] = Clamp01(strength);
        _relationshipStartTick[relationshipId] = tick;
        _relationshipEndTick[relationshipId] = 0;
        if (relationshipId > _relationshipHighWatermark)
        {
            _relationshipHighWatermark = relationshipId;
        }

        AddRelationshipToGraphDelta(relationshipId);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.RelationshipChanged,
            relationshipId,
            sourceEntityId,
            targetEntityId,
            kindId,
            _relationshipCreatedCodeId);
        if (context.ProvenanceInvocationId > 0)
        {
            _relationshipCreatedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
            _relationshipLastChangedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
        }
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

        EnsureProvenanceOutputCapacity(context);
        var previous = _relationshipStrength[relationshipId];
        _relationshipActive[relationshipId] = false;
        _relationshipEndTick[relationshipId] = tick;
        RemoveRelationshipFromGraphDelta(relationshipId);
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.RelationshipChanged,
            relationshipId,
            _relationshipSourceId[relationshipId],
            _relationshipTargetId[relationshipId],
            _relationshipKindId[relationshipId],
            _relationshipCulledCodeId);
        if (context.ProvenanceInvocationId > 0)
        {
            _relationshipLastChangedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
        }
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

        EnsureProvenanceOutputCapacity(context);
        _relationshipStrength[relationshipId] = next;
        AdvanceWorldVersion();
        RecordProvenanceOutput(
            context,
            ProvenanceOutputKindCodes.RelationshipChanged,
            relationshipId,
            _relationshipSourceId[relationshipId],
            _relationshipTargetId[relationshipId],
            _relationshipKindId[relationshipId],
            _relationshipStrengthChangedCodeId);
        if (context.ProvenanceInvocationId > 0)
        {
            _relationshipLastChangedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
        }
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

    public int FindRelationshipIncludingHistoricalEndpoints(int sourceEntityId, int targetEntityId, int kindId)
    {
        EnsureEntity(sourceEntityId);
        EnsureEntity(targetEntityId);
        ValidatePositive(kindId);
        for (var relationshipId = 1; relationshipId <= _relationshipHighWatermark; relationshipId++)
        {
            if (!_relationshipActive[relationshipId] ||
                _relationshipSourceId[relationshipId] != sourceEntityId ||
                _relationshipTargetId[relationshipId] != targetEntityId ||
                _relationshipKindId[relationshipId] != kindId)
            {
                continue;
            }

            return relationshipId;
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

    public int FirstEntityOfCulture(int cultureId)
    {
        EnsureCultureIndex(cultureId);
        return _firstEntityByCulture[cultureId];
    }

    public int NextEntityOfCulture(int entityId)
    {
        EnsureEntity(entityId);
        return _nextEntityByCulture[entityId];
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
            if (RelationshipIsHotActive(frozenRelationshipId) && !_relationshipInDeltaGraph[frozenRelationshipId])
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
            if (RelationshipIsHotActive(deltaRelationshipId))
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

    public void CullIncomingRelationshipsOfKind(
        int targetEntityId,
        int kindId,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureEntity(targetEntityId);
        ValidateRelationshipKindIndex(kindId);
        while (true)
        {
            var cursor = IncomingRelationshipsOfKind(targetEntityId, kindId);
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

    public bool EntityHasTag(int entityId, int tagId)
    {
        EnsureEntity(entityId);
        return tagId is > 0 and < 32 && (_entityTagBits[entityId] & (1 << tagId)) != 0;
    }

    public int EntityTagBits(int entityId)
    {
        EnsureEntity(entityId);
        return _entityTagBits[entityId];
    }

    internal bool SetEntityTagBit(int entityId, int tagId, bool assigned, int tick, in KernelExecutionContext context)
    {
        EnsureEntity(entityId);
        if (tagId is <= 0 or >= 32)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(tagId));
        }

        var mask = 1 << tagId;
        var current = (_entityTagBits[entityId] & mask) != 0;
        if (current == assigned)
        {
            return false;
        }

        _entityTagBits[entityId] = assigned
            ? _entityTagBits[entityId] | mask
            : _entityTagBits[entityId] & ~mask;
        _entityUpdatedAt[entityId] = tick;
        AdvanceWorldVersion();
        return true;
    }

    public int EntityCreatedAt(int entityId)
    {
        EnsureEntity(entityId);
        return _entityCreatedAt[entityId];
    }

    public int EntityHighWatermark => _entityHighWatermark;

    internal int CultureHighWatermark => _cultureHighWatermark;

    internal int RelationshipHighWatermark => _relationshipHighWatermark;

    public bool RelationshipIsActive(int relationshipId)
    {
        return RelationshipIsHotActive(relationshipId);
    }

    public bool RelationshipIsStoredActive(int relationshipId)
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

}
