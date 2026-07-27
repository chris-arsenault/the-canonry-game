namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class WorldState
{
    private const int OutgoingRelationshipDirectionCode = 1;
    private const int IncomingRelationshipDirectionCode = 2;

    private readonly IKernelEventWriter _events;
    private readonly int[] _attributes;
    private readonly bool[] _attributeAssigned;
    private readonly bool[] _traits;
    private readonly int _attributeStride;
    private readonly int _traitStride;
    private readonly MapCellRegistry _cells;
    private readonly EntitySpatialMembershipIndex _entitySpatialMembership;
    private RelationshipEndpointIndex _relationshipEndpoints;
    private readonly bool[] _endpointRelationshipInDeltaGraph;
    private readonly int[] _endpointOutgoingFrozenOffsets;
    private readonly int[] _endpointIncomingFrozenOffsets;
    private readonly int[] _endpointOutgoingFrozenRelationships;
    private readonly int[] _endpointIncomingFrozenRelationships;
    private readonly int[] _endpointFirstOutgoingDeltaRelationshipByRow;
    private readonly int[] _endpointFirstIncomingDeltaRelationshipByRow;
    private readonly int[] _endpointNextOutgoingDeltaRelationship;
    private readonly int[] _endpointNextIncomingDeltaRelationship;
    private readonly bool[] _mapCellKnown;
    private readonly int[] _mapCellTerrainIds;
    private readonly int[] _mapCellTerrainCoverIds;
    private readonly int[] _mapCellSuperregionIds;
    private readonly int[] _mapCellContinentIds;
    private readonly MapLayerSet _mapLayers;
    private readonly KernelDataSet _kernelData;
    private CultureLocality? _cultureLocality;
    private bool[] _entityAllocated = [];
    private int[] _entityStatus = [];
    private int[] _entityKindId = [];
    private int[] _entitySubtypeId = [];
    private int[] _entityCultureId = [];
    private int[] _entityCultureInfluenceMultiplierDeltas = [];
    private int[] _entityPhysicalInfluenceProduction = [];
    private int[] _entitySemanticInfluenceProduction = [];
    private int[] _cultureInfluenceMultiplierDeltas = [];
    private long[] _cultureFieldRevisions = [];
    private long[] _cultureWorkRevisions = [];
    private long[] _cultureFieldRevisionDeltas = [];
    private long[] _cultureWorkRevisionDeltas = [];
    private long[] _ownedPhysicalCultureMassDeltas = [];
    private int _cultureRevisionStride;
    private int[] _entityPhysicalQ = [];
    private int[] _entityPhysicalR = [];
    private int[] _entityPhysicalZ = [];
    private int[] _entitySemanticQ = [];
    private int[] _entitySemanticR = [];
    private int[] _entitySemanticS = [];
    private int[] _entityAnchorPhysicalQ = [];
    private int[] _entityAnchorPhysicalR = [];
    private int[] _entityAnchorSemanticQ = [];
    private int[] _entityAnchorSemanticR = [];
    private int[] _entityFavorMultiplierDelta = [];
    private int[] _entityFavorExpiresAtTick = [];
    private int[] _entityWorkTiers = [];
    private int[] _entityCreatedAt = [];
    private int[] _entityUpdatedAt = [];
    private int[] _firstEntityByKind = [];
    private int[] _nextEntityByKind = [];
    private int[] _previousEntityByKind = [];
    private int[] _firstEntityByCulture = [];
    private int[] _nextEntityByCulture = [];
    private int[] _previousEntityByCulture = [];
    private int[] _activeEntitySlots = [];
    private int[] _activeEntityPositions = [];
    private int[] _entityGenerations = [];
    private int[] _freeEntitySlots = [];
    private int _freeEntitySlotCount;
    private int _activeEntityCount;
    private bool[] _relationshipAllocated = [];
    private bool[] _relationshipActive = [];
    private int[] _relationshipKindId = [];
    private int[] _relationshipPolarityCode = [];
    private int[] _relationshipSourceId = [];
    private int[] _relationshipTargetId = [];
    private int[] _relationshipStartTick = [];
    private int[] _relationshipEndTick = [];
    private int[] _relationshipStrength = [];
    private int[] _activeRelationshipSlots = [];
    private int[] _activeRelationshipPositions = [];
    private int[] _relationshipGenerations = [];
    private int[] _freeRelationshipSlots = [];
    private int _freeRelationshipSlotCount;
    private int _activeRelationshipCount;
    private int[] _entityCreatedByInvocationId = [];
    private int[] _relationshipCreatedByInvocationId = [];
    private int[] _relationshipLastChangedByInvocationId = [];
    private EpochStampSet _graphVisited;
    private int[] _graphFrontierA = [];
    private int[] _graphFrontierB = [];
    private int _relationshipCreatedCodeId;
    private int _relationshipCulledCodeId;
    private int _relationshipStrengthChangedCodeId;
    private int _defaultSourceCodeId;
    private int _relationshipKindCapacity;
    private int _entityHighWatermark;
    private int _cultureHighWatermark;
    private int[] _entityTagBits = [];
    private int _relationshipHighWatermark;
    private long _totalPhysicalCultureRawMass;
    private int _physicalMapRadius;
    private int _semanticMapRadius;
    private int _physicalMapSegmentOriginRadius;
    private int _semanticMapSegmentOriginRadius;
    private readonly bool _singleWorker;

    public WorldState(IKernelEventWriter events, SimulationOptions options, KernelDataSet kernelData)
    {
        global::System.ArgumentNullException.ThrowIfNull(events); global::System.ArgumentNullException.ThrowIfNull(options);
        global::System.ArgumentNullException.ThrowIfNull(kernelData); _events = events; _kernelData = kernelData;
        _singleWorker = options.Execution.WorkerCount == 1;
        _attributeStride = checked(options.AttributeIdCapacity + 1);
        _traitStride = checked(options.TraitIdCapacity + 1);
        _attributes = new int[checked((options.EntityCapacity + 1) * _attributeStride)];
        _attributeAssigned = new bool[_attributes.Length];
        _traits = new bool[checked((options.EntityCapacity + 1) * _traitStride)];
        _cells = new MapCellRegistry(options.MapCellCapacity);
        _entitySpatialMembership = new EntitySpatialMembershipIndex(
            options.EntityCapacity,
            options.MapCellCapacity);
        _mapCellKnown = new bool[options.MapCellCapacity + 1];
        _mapCellTerrainIds = new int[options.MapCellCapacity + 1];
        _mapCellTerrainCoverIds = new int[options.MapCellCapacity + 1];
        _mapCellSuperregionIds = new int[options.MapCellCapacity + 1];
        _mapCellContinentIds = new int[options.MapCellCapacity + 1];
        _mapLayers = new MapLayerSet(
            _cells,
            options.EntityCapacity,
            options.CultureIdCapacity,
            options.MapInfluenceCapacity);
        InitializeEntityStorage(options);
        InitializeDistrictStorage(options);
        InitializeStageStorage(options);
        InitializeRelationshipStorage(options);
        _relationshipEndpoints = new RelationshipEndpointIndex(
            _entityAllocated,
            _entityStatus,
            _relationshipActive,
            _relationshipKindId,
            _relationshipSourceId,
            _relationshipTargetId,
            _relationshipGenerations,
            options.RelationshipKindIdCapacity,
            options.RelationshipGraphDeltaMergeThreshold);
        _endpointRelationshipInDeltaGraph = _relationshipEndpoints.RelationshipInDeltaGraph;
        _endpointOutgoingFrozenOffsets = _relationshipEndpoints.OutgoingFrozenOffsets;
        _endpointIncomingFrozenOffsets = _relationshipEndpoints.IncomingFrozenOffsets;
        _endpointOutgoingFrozenRelationships = _relationshipEndpoints.OutgoingFrozenRelationships;
        _endpointIncomingFrozenRelationships = _relationshipEndpoints.IncomingFrozenRelationships;
        _endpointFirstOutgoingDeltaRelationshipByRow =
            _relationshipEndpoints.FirstOutgoingDeltaRelationshipByRow;
        _endpointFirstIncomingDeltaRelationshipByRow =
            _relationshipEndpoints.FirstIncomingDeltaRelationshipByRow;
        _endpointNextOutgoingDeltaRelationship =
            _relationshipEndpoints.NextOutgoingDeltaRelationship;
        _endpointNextIncomingDeltaRelationship =
            _relationshipEndpoints.NextIncomingDeltaRelationship;
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
        _entityCultureInfluenceMultiplierDeltas = new int[options.EntityCapacity + 1];
        _entityPhysicalInfluenceProduction = new int[options.EntityCapacity + 1];
        _entitySemanticInfluenceProduction = new int[options.EntityCapacity + 1];
        _cultureInfluenceMultiplierDeltas = new int[options.CultureIdCapacity + 1];
        _cultureFieldRevisions = new long[options.CultureIdCapacity + 1];
        _cultureWorkRevisions = new long[options.CultureIdCapacity + 1];
        _cultureRevisionStride = options.CultureIdCapacity + 1;
        var revisionDeltaCapacity = checked(
            options.Execution.WorkerCount * _cultureRevisionStride);
        _cultureFieldRevisionDeltas = new long[revisionDeltaCapacity];
        _cultureWorkRevisionDeltas = new long[revisionDeltaCapacity];
        _ownedPhysicalCultureMassDeltas =
            new long[options.Execution.WorkerCount];
        _entityPhysicalQ = new int[options.EntityCapacity + 1];
        _entityPhysicalR = new int[options.EntityCapacity + 1];
        _entityPhysicalZ = new int[options.EntityCapacity + 1];
        _entitySemanticQ = new int[options.EntityCapacity + 1];
        _entitySemanticR = new int[options.EntityCapacity + 1];
        _entitySemanticS = new int[options.EntityCapacity + 1];
        _entityAnchorPhysicalQ = new int[options.EntityCapacity + 1];
        _entityAnchorPhysicalR = new int[options.EntityCapacity + 1];
        _entityAnchorSemanticQ = new int[options.EntityCapacity + 1];
        _entityAnchorSemanticR = new int[options.EntityCapacity + 1];
        _entityFavorMultiplierDelta = new int[options.EntityCapacity + 1];
        _entityFavorExpiresAtTick = new int[options.EntityCapacity + 1];
        _entityWorkTiers = new int[options.EntityCapacity + 1];
        _entityCreatedAt = new int[options.EntityCapacity + 1];
        _entityUpdatedAt = new int[options.EntityCapacity + 1];
        InitializeEntityIndexes(options);
    }

    private void InitializeEntityIndexes(SimulationOptions options)
    {
        _firstEntityByKind = new int[options.KindIdCapacity + 1];
        _nextEntityByKind = new int[options.EntityCapacity + 1];
        _previousEntityByKind = new int[options.EntityCapacity + 1];
        _firstEntityByCulture = new int[options.CultureIdCapacity + 1];
        _nextEntityByCulture = new int[options.EntityCapacity + 1];
        _previousEntityByCulture = new int[options.EntityCapacity + 1];
        _activeEntitySlots = new int[options.EntityCapacity];
        _activeEntityPositions = new int[options.EntityCapacity + 1];
        _entityGenerations = new int[options.EntityCapacity + 1];
        _freeEntitySlots = new int[options.EntityCapacity];
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
        _relationshipStrength = new int[options.RelationshipCapacity + 1];
        _activeRelationshipSlots = new int[options.RelationshipCapacity];
        _activeRelationshipPositions = new int[options.RelationshipCapacity + 1];
        _relationshipGenerations = new int[options.RelationshipCapacity + 1];
        _freeRelationshipSlots = new int[options.RelationshipCapacity];
        _relationshipKindCapacity = options.RelationshipKindIdCapacity;
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
    }

    private int _worldVersionTick = int.MinValue;
    private int _worldVersionGate;

    public int CurrentTick { get; private set; }
    public long WorldVersion { get; private set; }
    public int EntityCapacity => _entityAllocated.Length - 1;
    public int RelationshipCapacity => _relationshipAllocated.Length - 1;
    internal FixedScalar TotalPhysicalCultureRawMass =>
        KernelFixedPoint.CultureMassScalar(_totalPhysicalCultureRawMass);

    internal long TotalPhysicalCultureRawMassUnits => _totalPhysicalCultureRawMass;

    internal void AttachCultureLocality(CultureLocality cultureLocality)
    {
        global::System.ArgumentNullException.ThrowIfNull(cultureLocality);
        _cultureLocality = cultureLocality;
    }

    public void BeginTick(int tick)
    {
        NumericGuard.ThrowIfNegative(tick);
        CurrentTick = tick;
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

        if (previous == EntityStatusCodes.Active)
        {
            RemoveActiveEntity(entityId);
            RemoveEntityKindIndex(entityId, _entityKindId[entityId]);
            RemoveEntityCultureIndex(entityId, _entityCultureId[entityId]);
            _entitySpatialMembership.DetachPhysical(
                entityId,
                _cells.Require(
                    PlaneCodes.Physical,
                    _entityPhysicalQ[entityId],
                    _entityPhysicalR[entityId],
                    _entityPhysicalZ[entityId]));
            _entitySpatialMembership.DetachSemantic(
                entityId,
                _cells.Require(
                    PlaneCodes.Semantic,
                    _entitySemanticQ[entityId],
                    _entitySemanticR[entityId],
                    z: 0));
        }
        else if (statusCode == EntityStatusCodes.Active)
        {
            throw new global::System.InvalidOperationException();
        }

        _entityStatus[entityId] = statusCode;
        _entityUpdatedAt[entityId] = tick;
        MarkCultureWorkDirty(_entityCultureId[entityId]);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityStatusChangedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            context.SourceCodeId,
            _entityKindId[entityId],
            _entitySubtypeId[entityId],
            previous,
            statusCode)),
            context);
        ReleaseEntitySlot(entityId);
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

        var isActive = EntityIsActiveRaw(entityId);
        if (isActive)
        {
            RemoveEntityCultureIndex(entityId, previous);
        }

        _entityCultureId[entityId] = cultureId;
        _cultureHighWatermark = global::System.Math.Max(_cultureHighWatermark, cultureId);
        if (isActive)
        {
            InsertEntityCultureIndex(entityId, cultureId);
        }
        _entityUpdatedAt[entityId] = tick;
        MarkCultureWorkDirty(previous);
        MarkCultureWorkDirty(cultureId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new EntityCultureChangedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            context.SourceCodeId,
            _entityKindId[entityId],
            _entitySubtypeId[entityId],
            previous,
            cultureId)),
            context);
    }

    public bool HasTrait(int entityId, int traitId)
    {
        EnsureEntity(entityId);
        return _traits[TraitIndex(entityId, traitId)];
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

        _traits[TraitIndex(entityId, traitId)] = true;
        _entityUpdatedAt[entityId] = tick;
        MarkEntityCultureWorkDirty(entityId);
        AdvanceWorldVersion();
        Write(KernelEvent.From(new TraitAssignedKernelEvent(
            tick,
            WorldVersion,
            EntityOccurrenceHandle(entityId),
            traitId,
            context.SourceCodeId)),
            context);
    }

    private int TraitIndex(int entityId, int traitId)
    {
        if ((uint)traitId >= (uint)_traitStride || traitId == 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(traitId));
        }

        return checked((entityId * _traitStride) + traitId);
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
        RemoveActiveRelationship(relationshipId);
        _relationshipEndpoints.Detach(this, relationshipId);
        MarkEntityCultureWorkDirty(_relationshipSourceId[relationshipId]);
        MarkEntityCultureWorkDirty(_relationshipTargetId[relationshipId]);
        AdvanceWorldVersion();
        if (context.ProvenanceInvocationId > 0)
        {
            _relationshipLastChangedByInvocationId[relationshipId] = context.ProvenanceInvocationId;
        }
        WriteRelationshipChanged(relationshipId, tick, _relationshipCulledCodeId, context, previous, 0);
        ReleaseRelationshipSlot(relationshipId);
    }

    public void SetRelationshipStrength(int relationshipId, FixedScalar strength, int tick) =>
        SetRelationshipStrength(relationshipId, strength, tick, new KernelExecutionContext(0, _defaultSourceCodeId));

    public void SetRelationshipStrength(int relationshipId, FixedScalar strength, int tick, in KernelExecutionContext context)
    {
        SetRelationshipStrengthUnits(
            relationshipId,
            KernelFixedPoint.RelationshipStrengthUnits(Clamp01(strength)),
            tick,
            context);
    }

    private void SetRelationshipStrengthUnits(
        int relationshipId,
        int strengthUnits,
        int tick,
        in KernelExecutionContext context)
    {
        ValidatePositive(context.SourceCodeId);
        EnsureRelationship(relationshipId);
        if (!_relationshipActive[relationshipId])
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = _relationshipStrength[relationshipId];
        var next = global::System.Math.Clamp(
            strengthUnits,
            0,
            KernelFixedPoint.RelationshipStrengthMaximumUnits);
        if (next == previous)
        {
            return;
        }

        _relationshipStrength[relationshipId] = next;
        MarkEntityCultureWorkDirty(_relationshipSourceId[relationshipId]);
        MarkEntityCultureWorkDirty(_relationshipTargetId[relationshipId]);
        AdvanceWorldVersion();
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

    internal void ModifyRelationshipStrengthUnits(
        int relationshipId,
        int deltaUnits,
        int tick,
        in KernelExecutionContext context)
    {
        EnsureRelationship(relationshipId);
        var next = KernelFixedPoint.SaturatingAdd(
            _relationshipStrength[relationshipId],
            deltaUnits,
            0,
            KernelFixedPoint.RelationshipStrengthMaximumUnits);
        SetRelationshipStrengthUnits(relationshipId, next, tick, context);
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

    public int FirstActiveEntity() => _activeEntityCount > 0 ? _activeEntitySlots[0] : 0;

    public int NextActiveEntity(int entityId)
    {
        EnsureEntity(entityId);
        var nextPosition = _activeEntityPositions[entityId];
        return nextPosition < _activeEntityCount ? _activeEntitySlots[nextPosition] : 0;
    }

    internal int ActiveEntityAt(int index)
    {
        if ((uint)index >= (uint)_activeEntityCount)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        return _activeEntitySlots[index];
    }

    public int FirstActiveRelationship() =>
        _activeRelationshipCount > 0 ? _activeRelationshipSlots[0] : 0;

    public int NextActiveRelationship(int relationshipId)
    {
        if ((uint)relationshipId == 0u || relationshipId >= _activeRelationshipPositions.Length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(relationshipId));
        }

        var nextPosition = _activeRelationshipPositions[relationshipId];
        return nextPosition < _activeRelationshipCount
            ? _activeRelationshipSlots[nextPosition]
            : 0;
    }

    internal int ActiveRelationshipAt(int index)
    {
        if ((uint)index >= (uint)_activeRelationshipCount)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }

        return _activeRelationshipSlots[index];
    }

    internal int FirstIncidentRelationship(int entityId)
    {
        EnsureEntity(entityId);
        return _relationshipEndpoints.FirstIncidentRelationship(entityId);
    }

    internal int NextIncidentRelationship(int entityId, int relationshipId)
    {
        EnsureEntity(entityId);
        EnsureRelationship(relationshipId);
        return _relationshipEndpoints.NextIncidentRelationship(entityId, relationshipId);
    }

    public int FirstPhysicalEntityAt(int q, int r)
    {
        return TryGetCellId(PlaneCodes.Physical, q, r, z: 0, out var cellId)
            ? _entitySpatialMembership.FirstPhysicalEntity(cellId)
            : 0;
    }

    public int NextPhysicalEntityAt(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySpatialMembership.NextPhysicalEntity(entityId);
    }

    public int FirstSemanticEntityAt(int q, int r)
    {
        return TryGetCellId(PlaneCodes.Semantic, q, r, z: 0, out var cellId)
            ? _entitySpatialMembership.FirstSemanticEntity(cellId)
            : 0;
    }

    public int NextSemanticEntityAt(int entityId)
    {
        EnsureEntity(entityId);
        return _entitySpatialMembership.NextSemanticEntity(entityId);
    }

    public RelationshipGraphCursor OutgoingRelationshipsOfKind(int sourceEntityId, int kindId)
    {
        return OutgoingRelationshipsOfKindAbove(sourceEntityId, kindId, 0);
    }

    public RelationshipGraphCursor IncomingRelationshipsOfKind(int targetEntityId, int kindId)
    {
        return IncomingRelationshipsOfKindAbove(targetEntityId, kindId, 0);
    }

    public RelationshipGraphCursor OutgoingRelationshipsOfKindAbove(
        int sourceEntityId,
        int kindId,
        FixedScalar minimumStrength)
    {
        var row = RelationshipRowIndex(sourceEntityId, kindId);
        ValidateRelationshipThreshold(minimumStrength);
        return new RelationshipGraphCursor
        {
            DirectionCode = OutgoingRelationshipDirectionCode,
            FrozenIndex = _endpointOutgoingFrozenOffsets[row],
            FrozenEnd = _endpointOutgoingFrozenOffsets[row + 1],
            DeltaRelationshipId = _endpointFirstOutgoingDeltaRelationshipByRow[row],
            MinimumStrengthUnits = KernelFixedPoint.RelationshipStrengthUnits(minimumStrength),
        };
    }

    public RelationshipGraphCursor IncomingRelationshipsOfKindAbove(
        int targetEntityId,
        int kindId,
        FixedScalar minimumStrength)
    {
        var row = RelationshipRowIndex(targetEntityId, kindId);
        ValidateRelationshipThreshold(minimumStrength);
        return new RelationshipGraphCursor
        {
            DirectionCode = IncomingRelationshipDirectionCode,
            FrozenIndex = _endpointIncomingFrozenOffsets[row],
            FrozenEnd = _endpointIncomingFrozenOffsets[row + 1],
            DeltaRelationshipId = _endpointFirstIncomingDeltaRelationshipByRow[row],
            MinimumStrengthUnits = KernelFixedPoint.RelationshipStrengthUnits(minimumStrength),
        };
    }

    public bool MoveNextRelationship(ref RelationshipGraphCursor cursor, out int relationshipId)
    {
        relationshipId = 0;
        AdvanceFrozenRelationshipCursor(ref cursor);
        AdvanceDeltaRelationshipCursor(ref cursor);
        return TrySelectNextRelationship(ref cursor, out relationshipId);
    }

    private void AdvanceFrozenRelationshipCursor(ref RelationshipGraphCursor cursor)
    {
        while (cursor.FrozenIndex < cursor.FrozenEnd)
        {
            var frozenRelationshipId = FrozenCursorRelationshipId(cursor);
            if (frozenRelationshipId > 0 &&
                RelationshipIsHotActive(frozenRelationshipId) &&
                !_endpointRelationshipInDeltaGraph[frozenRelationshipId] &&
                _relationshipStrength[frozenRelationshipId] >=
                    cursor.MinimumStrengthUnits)
            {
                break;
            }

            cursor.FrozenIndex++;
        }
    }

    private void AdvanceDeltaRelationshipCursor(ref RelationshipGraphCursor cursor)
    {
        while (cursor.DeltaRelationshipId > 0)
        {
            var deltaRelationshipId = cursor.DeltaRelationshipId;
            if (RelationshipIsHotActive(deltaRelationshipId) &&
                _relationshipStrength[deltaRelationshipId] >=
                    cursor.MinimumStrengthUnits)
            {
                break;
            }

            cursor.DeltaRelationshipId = NextDeltaRelationship(cursor);
        }
    }

    private bool TrySelectNextRelationship(
        ref RelationshipGraphCursor cursor,
        out int relationshipId)
    {
        relationshipId = 0;
        var hasFrozen = cursor.FrozenIndex < cursor.FrozenEnd;
        var hasDelta = cursor.DeltaRelationshipId > 0;
        if (!hasFrozen && !hasDelta)
        {
            return false;
        }

        if (hasFrozen &&
            (!hasDelta ||
             FrozenCursorRelationshipId(cursor) < cursor.DeltaRelationshipId))
        {
            relationshipId = FrozenCursorRelationshipId(cursor);
            cursor.FrozenIndex++;
            return true;
        }

        relationshipId = cursor.DeltaRelationshipId;
        cursor.DeltaRelationshipId = NextDeltaRelationship(cursor);
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
        FixedScalar minimumStrength,
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
        FixedScalar minimumStrength,
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
        _relationshipEndpoints.Compact(this);
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

    internal int ActiveEntityCount => _activeEntityCount;

    internal int CultureHighWatermark => _cultureHighWatermark;

    internal int RelationshipHighWatermark => _relationshipHighWatermark;

    internal int ActiveRelationshipCount => _activeRelationshipCount;

    public bool RelationshipIsActive(int relationshipId)
    {
        return RelationshipIsHotActive(relationshipId);
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

    internal int RelationshipStartTick(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipStartTick[relationshipId];
    }

    public FixedScalar RelationshipStrength(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return KernelFixedPoint.RelationshipStrengthScalar(_relationshipStrength[relationshipId]);
    }

    internal int RelationshipStrengthUnits(int relationshipId)
    {
        EnsureRelationship(relationshipId);
        return _relationshipStrength[relationshipId];
    }

}
