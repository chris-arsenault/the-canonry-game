namespace Engine.Observability;

using System.Collections.ObjectModel;

using Engine.Constants;
using Engine.Eventing;
using Engine.Projection;

internal sealed partial class TelemetryEventProjection : KernelEventProjector
{
    private readonly int _runId;
    private TelemetryBatch _batch;
    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly Dictionary<int, ObservedEntityProjection> _entities = [];
    private readonly Dictionary<ObservedRelationshipKey, ObservedRelationshipProjection> _relationships = [];
    private readonly Dictionary<ObservedMapCellKey, ObservedMapCellProjection> _mapCells = [];
    private readonly Dictionary<int, GeneratorStateProjection> _generatorStates = [];
    private readonly HashSet<int> _recordedMapSnapshotTicks = [];
    private readonly HashSet<int> _recordedGeneratorSnapshotTicks = [];
    private readonly Dictionary<int, List<PendingCreatedEntity>> _createdEntitiesByInvocation = [];
    private readonly HashSet<int> _exportedEntityNameSymbols = [];
    private readonly Dictionary<KindSnapshotKey, KindSnapshotAccumulator> _kindSnapshotAccumulators = [];
    private readonly List<KindSnapshotKey> _kindSnapshotKeys = [];
    private readonly Dictionary<int, RelationshipSnapshotAccumulator> _relationshipSnapshotAccumulators = [];
    private readonly List<int> _relationshipSnapshotKeys = [];
    private readonly Dictionary<ObservedMapCellKey, int> _activeEntitiesByCell = [];
    private readonly Dictionary<MapSnapshotKey, MapSnapshotAccumulator> _mapSnapshotAccumulators = [];
    private readonly List<MapSnapshotKey> _mapSnapshotKeys = [];
    private readonly Dictionary<CoverageSnapshotKey, CoverageSnapshotAccumulator> _coverageSnapshotAccumulators = [];
    private readonly List<CoverageSnapshotKey> _coverageSnapshotKeys = [];
    private readonly Dictionary<CoverageTerrainSnapshotKey, int> _coverageTerrainSnapshotCounts = [];
    private readonly List<CoverageTerrainSnapshotKey> _coverageTerrainSnapshotKeys = [];
    private readonly List<int> _entitySnapshotIds = [];
    private readonly List<int> _attributeSnapshotIds = [];
    private int _totalGeneratorFires;
    private int _currentProvenanceInvocationId;

    public TelemetryEventProjection(
        int runId,
        TelemetryBatch batch,
        IProjectionSymbolCatalog? symbols = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(runId);
        ArgumentNullException.ThrowIfNull(batch);

        _runId = runId;
        _batch = batch;
        _symbols = symbols;
    }

    public IReadOnlyDictionary<int, ObservedEntityProjection> Entities =>
        new ReadOnlyDictionary<int, ObservedEntityProjection>(_entities);

    public IReadOnlyDictionary<ObservedRelationshipKey, ObservedRelationshipProjection> Relationships =>
        new ReadOnlyDictionary<ObservedRelationshipKey, ObservedRelationshipProjection>(_relationships);

    public int TotalGeneratorFires => _totalGeneratorFires;

    public long LastProjectedSequence { get; private set; } = -1;

    public long DroppedEvents { get; private set; }

    internal void ReplaceBatch(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        _batch = batch;
    }

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
        _createdEntitiesByInvocation.Clear();
    }

    protected override void OnProjectingEvent(in SequencedKernelEvent kernelEvent)
    {
        _currentProvenanceInvocationId = kernelEvent.Event.ProvenanceInvocationId;
    }

    public void RecordKindSnapshots(int tick)
    {
        _kindSnapshotAccumulators.Clear();
        _kindSnapshotKeys.Clear();
        foreach (var entity in _entities.Values)
        {
            var key = new KindSnapshotKey(entity.KindId, entity.SubtypeId);
            if (!_kindSnapshotAccumulators.TryGetValue(key, out var aggregate))
            {
                _kindSnapshotKeys.Add(key);
            }

            aggregate.Add(entity.StatusCode, ObservedHealth(entity));
            _kindSnapshotAccumulators[key] = aggregate;
        }

        for (var i = 0; i < _kindSnapshotKeys.Count; i++)
        {
            var key = _kindSnapshotKeys[i];
            var aggregate = _kindSnapshotAccumulators[key];
            _batch.KindSnapshots.Add(new KindSnapshotRow(
                _runId,
                tick,
                key.KindId,
                key.SubtypeId,
                aggregate.ActiveCount,
                aggregate.HistoricalCount,
                aggregate.AverageHealth,
                aggregate.MinimumHealth,
                aggregate.MaximumHealth));
        }
    }

    public void RecordEntitySnapshots(int tick)
    {
        _entitySnapshotIds.Clear();
        foreach (var entityId in _entities.Keys)
        {
            _entitySnapshotIds.Add(entityId);
        }

        _entitySnapshotIds.Sort();
        for (var entityIndex = 0; entityIndex < _entitySnapshotIds.Count; entityIndex++)
        {
            var entity = _entities[_entitySnapshotIds[entityIndex]];
            RegisterEntityNameSymbol(entity);
            _batch.EntitySnapshots.Add(new EntitySnapshotRow(
                _runId,
                tick,
                entity.EntityId,
                entity.KindId,
                entity.SubtypeId,
                entity.CultureId,
                entity.StatusCode,
                entity.PhysicalQ,
                entity.PhysicalR,
                entity.SemanticQ,
                entity.SemanticR,
                entity.SemanticS));

            _attributeSnapshotIds.Clear();
            foreach (var attributeId in entity.Attributes.Keys)
            {
                _attributeSnapshotIds.Add(attributeId);
            }

            _attributeSnapshotIds.Sort();
            for (var attributeIndex = 0; attributeIndex < _attributeSnapshotIds.Count; attributeIndex++)
            {
                var attributeId = _attributeSnapshotIds[attributeIndex];
                _batch.EntityAttributeSnapshots.Add(new EntityAttributeSnapshotRow(
                    _runId,
                    tick,
                    entity.EntityId,
                    attributeId,
                    entity.Attributes[attributeId]));
            }
        }
    }

    public void RecordRelationshipSnapshots(int tick)
    {
        _relationshipSnapshotAccumulators.Clear();
        _relationshipSnapshotKeys.Clear();
        foreach (var relationship in _relationships.Values)
        {
            if (!RelationshipIsHotActive(relationship))
            {
                continue;
            }

            var key = relationship.RelationshipKindId;
            if (!_relationshipSnapshotAccumulators.TryGetValue(key, out var aggregate))
            {
                _relationshipSnapshotKeys.Add(key);
            }

            aggregate.Add(relationship.Strength);
            _relationshipSnapshotAccumulators[key] = aggregate;
        }

        for (var i = 0; i < _relationshipSnapshotKeys.Count; i++)
        {
            var key = _relationshipSnapshotKeys[i];
            var aggregate = _relationshipSnapshotAccumulators[key];
            _batch.RelationshipSnapshots.Add(new RelationshipSnapshotRow(
                _runId,
                tick,
                key,
                aggregate.Count,
                aggregate.Average,
                aggregate.Minimum,
                aggregate.Maximum));
        }
    }

    public void RecordMapSnapshots(int tick, bool includeGeneratorState = true)
    {
        if (!_recordedMapSnapshotTicks.Add(tick))
        {
            return;
        }

        var activeEntitiesByCell = ActiveEntitiesByPlaneCell();
        _mapSnapshotAccumulators.Clear();
        _mapSnapshotKeys.Clear();
        foreach (var cell in _mapCells.Values)
        {
            var key = new MapSnapshotKey(cell.Key.PlaneCode, cell.TerrainId);
            if (!_mapSnapshotAccumulators.TryGetValue(key, out var aggregate))
            {
                _mapSnapshotKeys.Add(key);
            }

            aggregate.Add(cell.OwnerEntityId, activeEntitiesByCell.GetValueOrDefault(cell.Key));
            _mapSnapshotAccumulators[key] = aggregate;
        }

        _mapSnapshotKeys.Sort(static (left, right) =>
        {
            var planeComparison = left.PlaneCode.CompareTo(right.PlaneCode);
            return planeComparison != 0 ? planeComparison : left.TerrainId.CompareTo(right.TerrainId);
        });
        for (var i = 0; i < _mapSnapshotKeys.Count; i++)
        {
            var key = _mapSnapshotKeys[i];
            var aggregate = _mapSnapshotAccumulators[key];
            _batch.MapSnapshots.Add(new MapSnapshotRow(
                _runId,
                tick,
                key.PlaneCode,
                key.TerrainId,
                aggregate.CellCount,
                aggregate.CoveredCellCount,
                aggregate.ActiveEntityCount));
        }

        RecordCoverageSnapshots(tick, activeEntitiesByCell);
        if (includeGeneratorState)
        {
            RecordGeneratorStateSnapshots(tick);
        }
    }

    private bool RelationshipIsHotActive(ObservedRelationshipProjection relationship) =>
        relationship.IsActive &&
        _entities.TryGetValue(relationship.Key.SourceEntityId, out var source) &&
        IsActiveStatus(source.StatusCode) &&
        _entities.TryGetValue(relationship.Key.TargetEntityId, out var target) &&
        IsActiveStatus(target.StatusCode);

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.SubtypeId = evt.SubtypeId;
        entity.CultureId = evt.CultureId;
        entity.NameSeed = evt.NameSeed;
        entity.StatusCode = EntityStatusCodes.Active;
        entity.PhysicalQ = evt.PhysicalQ;
        entity.PhysicalR = evt.PhysicalR;
        entity.SemanticQ = evt.SemanticQ;
        entity.SemanticR = evt.SemanticR;
        entity.SemanticS = evt.SemanticS;
        entity.CreatedTick = evt.Tick;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        RegisterEntityNameSymbol(entity);
        _batch.EntityEvents.Add(new EntityEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.EntityCreated,
            evt.SourceCodeId,
            evt.EntityId,
            evt.KindId,
            evt.SubtypeId,
            evt.CultureId,
            PreviousStatusCode: null,
            CurrentStatusCode: entity.StatusCode,
            evt.PhysicalQ,
            evt.PhysicalR,
            evt.SemanticQ,
            evt.SemanticR,
            evt.SemanticS));
        if (evt.CultureId > 0)
        {
            _batch.CultureEntityBirthFieldEvents.Add(new CultureEntityBirthFieldEventRow(
                _runId,
                evt.Tick,
                evt.EntityId,
                evt.CultureId,
                evt.PhysicalFieldBandCode,
                evt.PhysicalCellKindCode,
                FixedScalar.FromUnits(evt.PhysicalLocalRawMassUnits).ToSingle(),
                FixedScalar.FromUnits(evt.PhysicalConnectedRawMassUnits).ToSingle(),
                evt.SemanticFieldBandCode,
                evt.SemanticCellKindCode,
                FixedScalar.FromUnits(evt.SemanticLocalRawMassUnits).ToSingle(),
                FixedScalar.FromUnits(evt.SemanticConnectedRawMassUnits).ToSingle()));
        }

        TrackCreatedEntityForGeneratorAttribution(evt);
        MarkProjected(sequence);
    }

    protected override void OnEntityStatusChanged(long sequence, in EntityStatusChangedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.SubtypeId = evt.SubtypeId;
        entity.PreviousStatusCode = evt.PreviousStatusCode;
        entity.StatusCode = evt.CurrentStatusCode;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        RegisterEntityNameSymbol(entity);
        _batch.EntityEvents.Add(new EntityEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.EntityStatusChanged,
            evt.SourceCodeId,
            evt.EntityId,
            entity.KindId,
            entity.SubtypeId,
            entity.CultureId,
            evt.PreviousStatusCode,
            evt.CurrentStatusCode,
            entity.PhysicalQ,
            entity.PhysicalR,
            entity.SemanticQ,
            entity.SemanticR,
            entity.SemanticS));

        if (IsHistoricalStatus(evt.CurrentStatusCode))
        {
            _batch.LifecycleEvents.Add(new LifecycleEventRow(
                _runId,
                evt.Tick,
                (int)KernelEventType.EntityStatusChanged,
                evt.EntityId,
                entity.KindId,
                evt.CurrentStatusCode));
            _batch.LifecycleReasonEvents.Add(new LifecycleReasonEventRow(
                _runId,
                evt.Tick,
                evt.EntityId,
                ReasonCodeId: evt.SourceCodeId,
                CauseId: evt.SourceCodeId,
                SourceCodeId: evt.SourceCodeId));
        }

        MarkProjected(sequence);
    }

    protected override void OnEntityMoved(long sequence, in EntityMovedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        if (evt.PlaneCode == PlaneCodes.Physical)
        {
            entity.PhysicalQ = evt.ToQ;
            entity.PhysicalR = evt.ToR;
        }
        else
        {
            entity.SemanticQ = evt.ToQ;
            entity.SemanticR = evt.ToR;
            entity.SemanticS = evt.ToS;
        }

        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        _batch.MovementEvents.Add(new MovementEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlaneCode,
            evt.SourceCodeId,
            evt.FromQ,
            evt.FromR,
            evt.FromS,
            evt.ToQ,
            evt.ToR,
            evt.ToS,
            MovementDistance(evt),
            evt.FromZ,
            evt.ToZ));
        MarkProjected(sequence);
    }

    protected override void OnChronicleChanged(long sequence, in ChronicleChangedKernelEvent evt)
    {
        _batch.ChronicleEvents.Add(new ChronicleEventRow(
            _runId,
            evt.Tick,
            evt.ChronicleSequence,
            evt.ArchetypeId,
            evt.EventTypeCode,
            evt.ActIndex,
            evt.OutcomeCodeId,
            evt.RoleEntityId0,
            evt.RoleEntityId1,
            evt.RoleEntityId2,
            evt.RoleEntityId3));
        MarkProjected(sequence);
    }

    protected override void OnWorldBeatChanged(long sequence, in WorldBeatChangedKernelEvent evt)
    {
        _batch.WorldBeatEvents.Add(new WorldBeatEventRow(
            _runId,
            evt.Tick,
            evt.TrackCode,
            evt.AgeId,
            evt.PhaseCode));
        MarkProjected(sequence);
    }

    protected override void OnEntityTagChanged(long sequence, in EntityTagChangedKernelEvent evt)
    {
        _batch.EntityTagEvents.Add(new EntityTagEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.TagId,
            evt.Assigned != 0,
            evt.SourceCodeId));
        MarkProjected(sequence);
    }

    protected override void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        var oldValue = KernelFixedPoint.AttributeValue(evt.OldValueUnits);
        var newValue = KernelFixedPoint.AttributeValue(evt.NewValueUnits);
        entity.Attributes[evt.AttributeId] = newValue;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        _batch.AttributeEvents.Add(new AttributeEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.AttributeId,
            oldValue,
            newValue,
            newValue - oldValue,
            evt.SourceCodeId));
        MarkProjected(sequence);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var key = new ObservedRelationshipKey(evt.SourceEntityId, evt.TargetEntityId, evt.RelationshipKindId);
        var previousStrength = KernelFixedPoint.RelationshipStrengthValue(evt.PreviousStrengthUnits);
        var newStrength = KernelFixedPoint.RelationshipStrengthValue(evt.NewStrengthUnits);
        _relationships[key] = new ObservedRelationshipProjection(
            key,
            evt.ChangeCodeId,
            evt.PolarityCode,
            newStrength,
            evt.IsActive != 0,
            evt.Tick,
            evt.WorldVersion);

        _batch.RelationshipEvents.Add(new RelationshipEventRow(
            _runId,
            evt.Tick,
            evt.ChangeCodeId,
            evt.SourceCodeId,
            evt.RelationshipKindId,
            evt.PolarityCode,
            evt.SourceEntityId,
            evt.TargetEntityId,
            previousStrength,
            newStrength,
            evt.ActionId,
            _currentProvenanceInvocationId));
        MarkProjected(sequence);
    }

    protected override void OnMapCellStateChanged(long sequence, in MapCellStateChangedKernelEvent evt)
    {
        RecordMapCellState(
            evt.Tick,
            evt.WorldVersion,
            (int)KernelEventType.MapCellStateChanged,
            evt.SourceCodeId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.SuperregionId,
            evt.ContinentId,
            evt.PreviousTerrainId,
            evt.CurrentTerrainId,
            evt.PreviousOwnerEntityId,
            evt.CurrentOwnerEntityId,
            evt.Z);
        MarkProjected(sequence);
    }

    protected override void OnCellInfluenceChanged(long sequence, in CellInfluenceChangedKernelEvent evt)
    {
        var previousInfluence = FixedScalar.FromUnits(evt.PreviousInfluenceUnits).ToSingle();
        var currentInfluence = FixedScalar.FromUnits(evt.CurrentInfluenceUnits).ToSingle();
        _batch.CellInfluenceEvents.Add(new CellInfluenceEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.ActionId,
            evt.EntityId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            previousInfluence,
            currentInfluence,
            currentInfluence - previousInfluence));
        MarkProjected(sequence);
    }

    protected override void OnCultureSlotOccupancyChanged(long sequence, in CultureSlotOccupancyChangedKernelEvent evt)
    {
        _batch.CultureSlotOccupancyEvents.Add(new CultureSlotOccupancyEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.CultureId,
            evt.SlotId,
            evt.PolarityCode,
            evt.PreviousEntityId,
            evt.CurrentEntityId,
            FixedScalar.FromUnits(evt.PreviousScoreUnits).ToSingle(),
            FixedScalar.FromUnits(evt.CurrentScoreUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnCellRoadChanged(long sequence, in CellRoadChangedKernelEvent evt)
    {
        var previousQuality = KernelFixedPoint.RoadQualityValue(evt.PreviousQualityUnits);
        var currentQuality = KernelFixedPoint.RoadQualityValue(evt.CurrentQualityUnits);
        _batch.CellRoadEvents.Add(new CellRoadEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.TravelCount,
            previousQuality,
            currentQuality,
            currentQuality - previousQuality));
        MarkProjected(sequence);
    }

    protected override void OnCellTravelStructureChanged(long sequence, in CellTravelStructureChangedKernelEvent evt)
    {
        MarkProjected(sequence);
    }

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence);
    }

    protected override void OnTrace(long sequence, in TraceKernelEvent evt)
    {
        _batch.TraceEvents.Add(new TraceRow(
            _runId,
            evt.Tick,
            evt.IntentId,
            evt.EntityId,
            evt.TraceSubjectCode,
            evt.CandidateIndex,
            evt.ParentCandidateIndex,
            evt.ActionFamilyCode,
            evt.ActionId,
            evt.EffectScopeCode,
            evt.EffectValueId,
            evt.EffectKindId,
            evt.TargetEntityId,
            evt.StatusCode,
            evt.AffordanceSourceKindCode,
            evt.AffordanceSourceId,
            evt.AffordanceShapeCode,
            evt.AffordanceSourceKindMask,
            evt.AffordanceShapeMask,
            evt.AffordanceContributionCount,
            evt.GeneratorAffordanceCount,
            evt.GambitAffordanceCount,
            evt.HiddenAffordanceCount,
            evt.SelectionBucketCode,
            FixedScalar.FromUnits(evt.UrgencyUnits).ToSingle(),
            FixedScalar.FromUnits(evt.RewardOrBaseScoreUnits).ToSingle(),
            FixedScalar.FromUnits(evt.RawWeightUnits).ToSingle(),
            FixedScalar.FromUnits(evt.AdjustedWeightUnits).ToSingle(),
            FixedScalar.FromUnits(evt.AffordanceScoreUnits).ToSingle(),
            FixedScalar.FromUnits(evt.AffordanceBonusUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnGeneratorEvaluated(long sequence, in GeneratorEvaluatedKernelEvent evt)
    {
        _ = GetGeneratorState(evt.GeneratorId);
        _batch.GeneratorEvents.Add(new GeneratorEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.GeneratorEvaluated,
            evt.GeneratorId,
            evt.StatusCode,
            TargetPoolCount: 0,
            InactiveRejectedCount: 0,
            SubtypeRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            TriggerRejectedCount: 0,
            evt.CandidateCount,
            evt.FailedFireCount,
            evt.SelectedTargetEntityId == 0 ? null : evt.SelectedTargetEntityId,
            CreatedEntityCount: 0,
            CreatedRelationshipCount: 0));
        MarkProjected(sequence);
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        _totalGeneratorFires++;
        var causeId = _currentProvenanceInvocationId == 0
            ? (int)Math.Min(sequence + 1, int.MaxValue)
            : _currentProvenanceInvocationId;
        var state = GetGeneratorState(evt.GeneratorId);
        state.RunCount++;
        state.LastRunTick = evt.Tick;
        _batch.GeneratorEvents.Add(new GeneratorEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.GeneratorFired,
            evt.GeneratorId,
            GeneratorStatusCodes.Fired,
            TargetPoolCount: 0,
            InactiveRejectedCount: 0,
            SubtypeRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            TriggerRejectedCount: 0,
            CandidateCount: 0,
            FailedFireCount: 0,
            evt.TargetEntityId == 0 ? null : evt.TargetEntityId,
            evt.CreatedEntityCount,
            evt.CreatedRelationshipCount,
            causeId,
            CreatedMapCellCount: 0,
            RelationshipSpendCount: evt.RelationshipSpendCount,
            OutputLockRelationshipCreatedCount: evt.OutputLockRelationshipCreatedCount));

        for (var i = 0; i < evt.CreatedRelationshipCount; i++)
        {
            _batch.GeneratorOutputs.Add(new GeneratorOutputRow(
                _runId,
                evt.Tick,
                causeId,
                evt.GeneratorId,
                OutputDomainCode: 2,
                OutputEntityId: 0,
                OutputRelationshipKindId: 0,
                OutputSourceEntityId: evt.TargetEntityId,
                OutputTargetEntityId: 0,
                OutputQ: 0,
                OutputR: 0,
                OutputS: 0,
                PlacementEventId: 0));
        }

        MarkProjected(sequence);
    }

    protected override void OnGeneratorTargetEvaluated(long sequence, in GeneratorTargetEvaluatedKernelEvent evt)
    {
        _batch.GeneratorTargetEvaluations.Add(new GeneratorTargetEvaluationRow(
            _runId,
            evt.Tick,
            evt.GeneratorId,
            evt.TargetEntityId,
            evt.TargetKindId,
            evt.TargetSubtypeId,
            evt.TargetCultureId,
            evt.PhysicalQ,
            evt.PhysicalR,
            evt.SemanticQ,
            evt.SemanticR,
            evt.SemanticS,
            evt.StatusCode,
            evt.RejectionReasonCode,
            evt.PhysicalFieldBandCode,
            evt.PhysicalCellKindCode,
            FixedScalar.FromUnits(evt.PhysicalLocalRawMassUnits).ToSingle(),
            FixedScalar.FromUnits(evt.PhysicalConnectedRawMassUnits).ToSingle(),
            evt.SemanticFieldBandCode,
            evt.SemanticCellKindCode,
            FixedScalar.FromUnits(evt.SemanticLocalRawMassUnits).ToSingle(),
            FixedScalar.FromUnits(evt.SemanticConnectedRawMassUnits).ToSingle()));
        MarkProjected(sequence);
    }

    protected override void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.ActorId);
        entity.KindId = evt.ActorKindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        _batch.GambitEvents.Add(new GambitEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.GambitExecuted,
            evt.GambitId,
            evt.ActorId,
            evt.ActorKindId,
            evt.AffectedEntityId1 == 0 ? evt.ActorId : evt.AffectedEntityId1,
            (int)evt.State,
            (evt.State & KernelEventState.Succeeded) != KernelEventState.None,
            evt.AffectedEntityCount));
        MarkProjected(sequence);
    }

    private ObservedEntityProjection GetOrCreateEntity(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            return entity;
        }

        entity = new ObservedEntityProjection(entityId)
        {
            NameSeed = entityId,
            StatusCode = EntityStatusCodes.Active,
        };
        _entities.Add(entityId, entity);
        return entity;
    }

    private void UpsertMapCell(
        ObservedMapCellKey key,
        int terrainId,
        int ownerEntityId,
        int superregionId,
        int continentId,
        int tick,
        long worldVersion)
    {
        _mapCells[key] = new ObservedMapCellProjection(
            key,
            terrainId,
            ownerEntityId,
            superregionId,
            continentId,
            tick,
            worldVersion);
    }

    private void RecordMapCellState(
        int tick,
        long worldVersion,
        int eventTypeCode,
        int sourceCodeId,
        int planeCode,
        int q,
        int r,
        int superregionId,
        int continentId,
        int previousTerrainId,
        int currentTerrainId,
        int previousOwnerEntityId,
        int currentOwnerEntityId,
        int z = 0)
    {
        var key = new ObservedMapCellKey(planeCode, q, r, z);
        UpsertMapCell(
            key,
            currentTerrainId,
            currentOwnerEntityId,
            superregionId,
            continentId,
            tick,
            worldVersion);
        var causeId = sourceCodeId;
        _batch.MapCellEvents.Add(new MapCellEventRow(
            _runId,
            tick,
            eventTypeCode,
            sourceCodeId,
            planeCode,
            q,
            r,
            superregionId,
            continentId,
            previousTerrainId,
            currentTerrainId,
            previousOwnerEntityId,
            currentOwnerEntityId,
            causeId,
            z));

        if (previousTerrainId == 0 && currentTerrainId != 0)
        {
            AddPlacementEvent(tick, causeId, currentOwnerEntityId, planeCode, q, r, -q - r);
        }
    }

    private void AddPlacementEvent(
        int tick,
        int causeId,
        int anchorEntityId,
        int planeCode,
        int q,
        int r,
        int s)
    {
        var key = new ObservedMapCellKey(planeCode, q, r);
        _mapCells.TryGetValue(key, out var cell);
        _batch.PlacementEvents.Add(new PlacementEventRow(
            _runId,
            tick,
            causeId,
            anchorEntityId,
            GeneratorPlacementCodes.Area,
            planeCode,
            q,
            r,
            s,
            TerrainId: cell?.TerrainId ?? 0,
            CoverageOwnerEntityId: cell?.OwnerEntityId ?? 0,
            RejectionReasonCode: 0,
            Selected: true));
    }

    private void RegisterEntityNameSymbol(ObservedEntityProjection entity)
    {
        if (!_exportedEntityNameSymbols.Add(entity.EntityId))
        {
            return;
        }

        var nameSeed = entity.NameSeed == 0 ? entity.EntityId : entity.NameSeed;
        var key = new ProjectionEntityNameKey(
            entity.EntityId,
            entity.KindId,
            entity.SubtypeId,
            entity.CultureId,
            nameSeed);
        var names = _symbols is not null && _symbols.TryResolveEntityNames(key, out var resolvedNames)
            ? resolvedNames
            : ProjectionEntityNames.FromDisplayName($"E{entity.EntityId}");
        _batch.EntityNameSymbols.Add(new EntityNameSymbolRow(
            _runId,
            entity.EntityId,
            entity.KindId,
            entity.SubtypeId,
            entity.CultureId,
            nameSeed,
            names.DisplayName,
            names.ReferenceName));
    }

    private void TrackCreatedEntityForGeneratorAttribution(in EntityCreatedKernelEvent evt)
    {
        if (_currentProvenanceInvocationId <= 0)
        {
            return;
        }

        if (!_createdEntitiesByInvocation.TryGetValue(
                _currentProvenanceInvocationId,
                out var createdEntities))
        {
            createdEntities = [];
            _createdEntitiesByInvocation[_currentProvenanceInvocationId] = createdEntities;
        }

        createdEntities.Add(new PendingCreatedEntity(
            evt.EntityId,
            evt.KindId,
            evt.SubtypeId,
            evt.CultureId,
            evt.PhysicalQ,
            evt.PhysicalR,
            evt.SemanticQ,
            evt.SemanticR,
            evt.SemanticS));
    }

    protected override void OnProvenanceInvocationRecorded(
        long sequence,
        in ProvenanceInvocationRecordedKernelEvent evt)
    {
        if (evt.ProducerKindCode != ProvenanceProducerKindCodes.Generator ||
            !_createdEntitiesByInvocation.Remove(evt.InvocationId, out var createdEntities))
        {
            MarkProjected(sequence);
            return;
        }

        foreach (var entity in createdEntities)
        {
            RecordGeneratedEntity(
                evt.Tick,
                evt.ProducerId,
                evt.InvocationId,
                entity);
        }
        MarkProjected(sequence);
    }

    private void RecordGeneratedEntity(
        int tick,
        int generatorId,
        int causeId,
        PendingCreatedEntity entity)
    {
        RegisterEntityNameSymbol(GetOrCreateEntity(entity.EntityId));
        _batch.GeneratedEntities.Add(new GeneratedEntityRow(
            _runId,
            tick,
            generatorId,
            entity.EntityId,
            entity.KindId,
            entity.SubtypeId,
            entity.CultureId,
            entity.PhysicalQ,
            entity.PhysicalR,
            entity.SemanticQ,
            entity.SemanticR,
            entity.SemanticS));
        _batch.GeneratorOutputs.Add(new GeneratorOutputRow(
            _runId,
            tick,
            causeId,
            generatorId,
            OutputDomainCode: 1,
            entity.EntityId,
            OutputRelationshipKindId: 0,
            OutputSourceEntityId: 0,
            OutputTargetEntityId: 0,
            entity.PhysicalQ,
            entity.PhysicalR,
            OutputS: 0,
            causeId));
        AddPlacementEvent(
            tick,
            causeId,
            entity.EntityId,
            PlaneCodes.Physical,
            entity.PhysicalQ,
            entity.PhysicalR,
            -entity.PhysicalQ - entity.PhysicalR);
        AddPlacementEvent(
            tick,
            causeId,
            entity.EntityId,
            PlaneCodes.Semantic,
            entity.SemanticQ,
            entity.SemanticR,
            entity.SemanticS);
    }

    private void MarkProjected(long sequence)
    {
        LastProjectedSequence = sequence;
    }

    private static bool IsActiveStatus(int statusCode) => statusCode == EntityStatusCodes.Active;

    private static bool IsHistoricalStatus(int statusCode) =>
        statusCode == EntityStatusCodes.Historical;

    private static int MovementDistance(EntityMovedKernelEvent evt) =>
        evt.PlaneCode == PlaneCodes.Physical
            ? AxialDistance(evt.FromQ, evt.FromR, evt.ToQ, evt.ToR)
            : CubeDistance(evt.FromQ, evt.FromR, evt.FromS, evt.ToQ, evt.ToR, evt.ToS);

    private static int AxialDistance(int fromQ, int fromR, int toQ, int toR)
    {
        var fromS = -fromQ - fromR;
        var toS = -toQ - toR;
        return CubeDistance(fromQ, fromR, fromS, toQ, toR, toS);
    }

    private static int CubeDistance(int fromQ, int fromR, int fromS, int toQ, int toR, int toS) =>
        (Math.Abs(fromQ - toQ) + Math.Abs(fromR - toR) + Math.Abs(fromS - toS)) / 2;

}
