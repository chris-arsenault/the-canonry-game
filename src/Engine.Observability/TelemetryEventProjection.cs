namespace Engine.Observability;

using System.Collections.ObjectModel;

using Engine.Eventing;
using Engine.Projection;

internal sealed record ObservabilityProjectionMigrationItem(
    string Area,
    string RequiredPrimitiveSource,
    string Reason);

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
    private readonly Dictionary<int, Queue<PendingCreatedEntity>> _unassignedCreatedEntitiesByTick = [];
    private readonly HashSet<int> _exportedEntityNameSymbols = [];
    private readonly List<PendingGeneratorFire> _pendingGeneratorFires = [];
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

    public static IReadOnlyList<ObservabilityProjectionMigrationItem> FutureMigrationItems { get; } =
    [
        new(
            "generated entity attribution",
            "generator-fired event payload listing created entity IDs or creation-cause IDs on EntityCreated",
            "GeneratedEntityRow is inferred by matching EntityCreated events on the same tick until the kernel emits explicit attribution."),
        new(
            "entity display name seeds",
            "entity-created name seed or startup entity-name snapshot",
            "EntityCreatedKernelEvent carries identity IDs but not the stable user-space name seed.")
    ];

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
        entity.NameSeed = evt.EntityId;
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
        entity.Attributes[evt.AttributeId] = evt.NewValue;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        _batch.AttributeEvents.Add(new AttributeEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.AttributeId,
            evt.OldValue,
            evt.NewValue,
            evt.NewValue - evt.OldValue,
            evt.SourceCodeId));
        MarkProjected(sequence);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var key = new ObservedRelationshipKey(evt.SourceEntityId, evt.TargetEntityId, evt.RelationshipKindId);
        _relationships[key] = new ObservedRelationshipProjection(
            key,
            evt.ChangeCodeId,
            evt.PolarityCode,
            evt.NewStrength,
            evt.NewStrength > 0f,
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
            evt.PreviousStrength,
            evt.NewStrength,
            evt.ActionId,
            _currentProvenanceInvocationId));
        MarkProjected(sequence);
    }

    protected override void OnCellCreated(long sequence, in CellCreatedKernelEvent evt)
    {
        var key = new ObservedMapCellKey(PlaneCodes.Physical, evt.Q, evt.R, evt.Z);
        if (!_mapCells.ContainsKey(key))
        {
            RecordMapCellState(
                evt.Tick,
                evt.WorldVersion,
                (int)KernelEventType.CellCreated,
                evt.SourceCodeId,
                PlaneCodes.Physical,
                evt.Q,
                evt.R,
                evt.SuperregionId,
                evt.ContinentId,
                previousTerrainId: 0,
                evt.TerrainId,
                previousOwnerEntityId: 0,
                evt.OwnerEntityId,
                evt.Z);
        }

        MarkProjected(sequence);
    }

    protected override void OnCellChanged(long sequence, in CellChangedKernelEvent evt)
    {
        var key = new ObservedMapCellKey(PlaneCodes.Physical, evt.Q, evt.R, evt.Z);
        if (!_mapCells.TryGetValue(key, out var cell) ||
            cell.TerrainId != evt.CurrentTerrainId ||
            cell.OwnerEntityId != evt.CurrentOwnerEntityId)
        {
            RecordMapCellState(
                evt.Tick,
                evt.WorldVersion,
                (int)KernelEventType.CellChanged,
                evt.SourceCodeId,
                PlaneCodes.Physical,
                evt.Q,
                evt.R,
                evt.SuperregionId,
                evt.ContinentId,
                evt.PreviousTerrainId,
                evt.CurrentTerrainId,
                evt.PreviousOwnerEntityId,
                evt.CurrentOwnerEntityId,
                evt.Z);
        }

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
        _batch.CellInfluenceEvents.Add(new CellInfluenceEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.ActionId,
            evt.EntityId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.PreviousInfluence,
            evt.CurrentInfluence,
            evt.CurrentInfluence - evt.PreviousInfluence));
        MarkProjected(sequence);
    }

    protected override void OnCellCultureInfluenceChanged(long sequence, in CellCultureInfluenceChangedKernelEvent evt)
    {
        _batch.CellCultureInfluenceEvents.Add(new CellCultureInfluenceEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.CultureId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.PreviousShare,
            evt.CurrentShare,
            evt.CurrentShare - evt.PreviousShare,
            evt.PreviousRawInfluence,
            evt.CurrentRawInfluence,
            evt.CurrentTotalRawInfluence,
            evt.Z));
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
            evt.PreviousScore,
            evt.CurrentScore));
        MarkProjected(sequence);
    }

    protected override void OnMinorActorVerbExecuted(long sequence, in MinorActorVerbExecutedKernelEvent evt)
    {
        _batch.MinorActorVerbEvents.Add(new MinorActorVerbEventRow(
            _runId,
            evt.Tick,
            evt.ActorId,
            evt.SourceEntityId,
            evt.SourceRuleId,
            evt.SourceWorkTier,
            evt.EffectiveRadius,
            evt.CultureId,
            evt.VerbId,
            evt.PurposeCode,
            evt.ActionCode,
            evt.TargetingCode,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.StatusCode,
            evt.CellKindCode,
            evt.LocalRawMass,
            evt.ConnectedRawMass,
            evt.Score,
            evt.Delta));
        MarkProjected(sequence);
    }

    protected override void OnCellRoadChanged(long sequence, in CellRoadChangedKernelEvent evt)
    {
        _batch.CellRoadEvents.Add(new CellRoadEventRow(
            _runId,
            evt.Tick,
            evt.SourceCodeId,
            evt.PlaneCode,
            evt.Q,
            evt.R,
            evt.TravelCount,
            evt.PreviousQuality,
            evt.CurrentQuality,
            evt.CurrentQuality - evt.PreviousQuality));
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
            evt.Urgency,
            evt.RewardOrBaseScore,
            evt.RawWeight,
            evt.AdjustedWeight,
            evt.AffordanceScore,
            evt.AffordanceBonus));
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

        if (evt.CreatedEntityCount > 0)
        {
            _pendingGeneratorFires.Add(new PendingGeneratorFire(evt.Tick, evt.GeneratorId, causeId, evt.CreatedEntityCount));
            AssignGeneratedEntities(evt.Tick);
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
            evt.PhysicalLocalRawMass,
            evt.PhysicalConnectedRawMass,
            evt.SemanticFieldBandCode,
            evt.SemanticCellKindCode,
            evt.SemanticLocalRawMass,
            evt.SemanticConnectedRawMass,
            evt.PhysicalComponentId,
            evt.PhysicalComponentCellCount,
            evt.PhysicalComponentRawMass,
            evt.SemanticComponentId,
            evt.SemanticComponentCellCount,
            evt.SemanticComponentRawMass));
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
        if (!_unassignedCreatedEntitiesByTick.TryGetValue(evt.Tick, out var createdEntities))
        {
            createdEntities = [];
            _unassignedCreatedEntitiesByTick[evt.Tick] = createdEntities;
        }

        createdEntities.Enqueue(new PendingCreatedEntity(
            evt.EntityId,
            evt.KindId,
            evt.SubtypeId,
            evt.CultureId,
            evt.PhysicalQ,
            evt.PhysicalR,
            evt.SemanticQ,
            evt.SemanticR,
            evt.SemanticS));
        AssignGeneratedEntities(evt.Tick);
    }

    private void AssignGeneratedEntities(int tick)
    {
        if (!_unassignedCreatedEntitiesByTick.TryGetValue(tick, out var createdEntities))
        {
            return;
        }

        foreach (var pending in _pendingGeneratorFires.Where(pending => pending.Tick == tick && pending.Remaining > 0))
        {
            while (pending.Remaining > 0 && createdEntities.Count > 0)
            {
                var entity = createdEntities.Dequeue();
                RecordGeneratedEntity(tick, pending, entity);
                pending.Remaining--;
            }
        }

        if (createdEntities.Count == 0)
        {
            _unassignedCreatedEntitiesByTick.Remove(tick);
        }

        _pendingGeneratorFires.RemoveAll(pending => pending.Remaining == 0);
    }

    private void RecordGeneratedEntity(int tick, PendingGeneratorFire pending, PendingCreatedEntity entity)
    {
        RegisterEntityNameSymbol(GetOrCreateEntity(entity.EntityId));
        _batch.GeneratedEntities.Add(new GeneratedEntityRow(
            _runId,
            tick,
            pending.GeneratorId,
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
            pending.CauseId,
            pending.GeneratorId,
            OutputDomainCode: 1,
            entity.EntityId,
            OutputRelationshipKindId: 0,
            OutputSourceEntityId: 0,
            OutputTargetEntityId: 0,
            entity.PhysicalQ,
            entity.PhysicalR,
            OutputS: 0,
            pending.CauseId));
        AddPlacementEvent(
            tick,
            pending.CauseId,
            entity.EntityId,
            PlaneCodes.Physical,
            entity.PhysicalQ,
            entity.PhysicalR,
            -entity.PhysicalQ - entity.PhysicalR);
        AddPlacementEvent(
            tick,
            pending.CauseId,
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
