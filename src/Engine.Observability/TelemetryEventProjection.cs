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
    private readonly TelemetryBatch _batch;
    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly Dictionary<int, ObservedEntityProjection> _entities = [];
    private readonly Dictionary<ObservedRelationshipKey, ObservedRelationshipProjection> _relationships = [];
    private readonly Dictionary<ObservedMapCellKey, ObservedMapCellProjection> _mapCells = [];
    private readonly Dictionary<ResourceCellKey, float> _resourceValues = [];
    private readonly Dictionary<PressureKey, float> _pressureValues = [];
    private readonly Dictionary<(int EntityId, int PlanId), int> _selectedLocalityByEntityPlan = [];
    private readonly Dictionary<int, GeneratorStateProjection> _generatorStates = [];
    private readonly HashSet<int> _recordedMapSnapshotTicks = [];
    private readonly HashSet<PlannerStepKey> _recordedPlannerSteps = [];
    private readonly Dictionary<int, Queue<ObservedEntityProjection>> _unassignedCreatedEntitiesByTick = [];
    private readonly HashSet<int> _exportedEntityNameSymbols = [];
    private readonly List<PendingGeneratorFire> _pendingGeneratorFires = [];
    private int _totalGeneratorFires;

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
            "planner goal detail rows",
            "goal-created/goal-updated events with desired effect, target, urgency, parent, and actionable state",
            "GoalGeneratedKernelEvent only reports counts, so PlannerGoalRow cannot be rebuilt without reading live entity goals."),
        new(
            "resource snapshots",
            "startup resource snapshots plus resource aggregate events",
            "The event stream has resource deltas but no untouched initial stockpile state."),
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

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
    }

    public void RecordTickSnapshot(int tick)
    {
        var activeEntities = _entities.Values.Where(entity => IsActiveStatus(entity.StatusCode)).ToList();
        var historicalCount = _entities.Values.Count(entity => IsHistoricalStatus(entity.StatusCode));
        var activeRelationships = _relationships.Values.Where(relationship => relationship.IsActive).ToList();
        var coveredCells = _mapCells.Values.Count(cell => cell.OwnerEntityId > 0);
        var coverageCount = _mapCells.Values
            .Where(cell => cell.OwnerEntityId > 0)
            .Select(cell => cell.OwnerEntityId)
            .Distinct()
            .Count();

        _batch.TickSnapshots.Add(new TickSnapshotRow(
            _runId,
            tick,
            activeEntities.Count,
            historicalCount,
            _totalGeneratorFires,
            activeRelationships.Count,
            coverageCount,
            coveredCells,
            _mapCells.Count - coveredCells,
            ComputePhysicalSpread(activeEntities),
            ComputeSemanticSpread(activeEntities)));
    }

    public void RecordKindSnapshots(int tick)
    {
        foreach (var group in _entities.Values.GroupBy(entity => (entity.KindId, entity.SubtypeId)))
        {
            var healthValues = group
                .Select(ObservedHealth)
                .Where(value => value > 0f)
                .ToList();
            _batch.KindSnapshots.Add(new KindSnapshotRow(
                _runId,
                tick,
                group.Key.KindId,
                group.Key.SubtypeId,
                group.Count(entity => IsActiveStatus(entity.StatusCode)),
                group.Count(entity => IsHistoricalStatus(entity.StatusCode)),
                healthValues.Count == 0 ? 0f : healthValues.Average(),
                healthValues.Count == 0 ? 0f : healthValues.Min(),
                healthValues.Count == 0 ? 0f : healthValues.Max()));
        }
    }

    public void RecordEntitySnapshots(int tick)
    {
        foreach (var entity in _entities.Values.OrderBy(entity => entity.EntityId))
        {
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

            foreach (var attribute in entity.Attributes.OrderBy(attribute => attribute.Key))
            {
                _batch.EntityAttributeSnapshots.Add(new EntityAttributeSnapshotRow(
                    _runId,
                    tick,
                    entity.EntityId,
                    attribute.Key,
                    attribute.Value));
            }
        }
    }

    public void RecordRelationshipSnapshots(int tick)
    {
        foreach (var group in _relationships.Values
                     .Where(relationship => relationship.IsActive)
                     .GroupBy(relationship => relationship.RelationshipKindId))
        {
            var strengths = group.Select(relationship => relationship.Strength).ToList();
            _batch.RelationshipSnapshots.Add(new RelationshipSnapshotRow(
                _runId,
                tick,
                group.Key,
                strengths.Count,
                strengths.Count == 0 ? 0f : strengths.Average(),
                strengths.Count == 0 ? 0f : strengths.Min(),
                strengths.Count == 0 ? 0f : strengths.Max()));
        }
    }

    public void RecordMapSnapshots(int tick)
    {
        if (!_recordedMapSnapshotTicks.Add(tick))
        {
            return;
        }

        var activeEntitiesByCell = _entities.Values
            .Where(entity => IsActiveStatus(entity.StatusCode))
            .GroupBy(entity => new ObservedMapCellKey(entity.PhysicalQ, entity.PhysicalR))
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var group in _mapCells.Values
                     .OrderBy(cell => cell.TerrainId)
                     .GroupBy(cell => cell.TerrainId))
        {
            _batch.MapSnapshots.Add(new MapSnapshotRow(
                _runId,
                tick,
                group.Key,
                group.Count(),
                group.Count(cell => cell.OwnerEntityId > 0),
                group.Sum(cell => activeEntitiesByCell.GetValueOrDefault(cell.Key))));
        }

        RecordCoverageSnapshots(tick, activeEntitiesByCell);
        RecordResourceSnapshots(tick);
        RecordPressureSnapshots(tick);
        RecordGeneratorStateSnapshots(tick);
    }

    private void RecordCoverageSnapshots(
        int tick,
        Dictionary<ObservedMapCellKey, int> activeEntitiesByCell)
    {
        foreach (var group in _mapCells.Values
                     .Where(cell => cell.OwnerEntityId > 0)
                     .GroupBy(cell => cell.OwnerEntityId))
        {
            var cells = group.ToList();
            _batch.CoverageSnapshots.Add(new CoverageSnapshotRow(
                _runId,
                tick,
                group.Key,
                CoverageKindCode: 1,
                cells.Count,
                cells.Min(cell => cell.Key.Q),
                cells.Max(cell => cell.Key.Q),
                cells.Min(cell => cell.Key.R),
                cells.Max(cell => cell.Key.R),
                cells.Sum(cell => activeEntitiesByCell.GetValueOrDefault(cell.Key))));

            foreach (var terrain in cells.GroupBy(cell => cell.TerrainId))
            {
                _batch.CoverageTerrainSnapshots.Add(new CoverageTerrainSnapshotRow(
                    _runId,
                    tick,
                    group.Key,
                    terrain.Key,
                    terrain.Count()));
            }
        }
    }

    private void RecordResourceSnapshots(int tick)
    {
        foreach (var group in _resourceValues.GroupBy(pair => pair.Key.ResourceId))
        {
            var values = SnapshotStats.From(group.Select(pair => pair.Value));
            var terrainRows = new List<ResourceTerrainSnapshotRow>();
            foreach (var terrainGroup in group.GroupBy(pair => TerrainFor(pair.Key.Q, pair.Key.R)))
            {
                var terrainValues = SnapshotStats.From(terrainGroup.Select(pair => pair.Value));
                terrainRows.Add(new ResourceTerrainSnapshotRow(
                    _runId,
                    tick,
                    group.Key,
                    terrainGroup.Key,
                    terrainValues.Total,
                    terrainValues.Average,
                    terrainValues.Minimum,
                    terrainValues.Maximum));
            }

            _batch.ResourceSnapshots.Add(new ResourceSnapshotRow(
                _runId,
                tick,
                group.Key,
                terrainRows.Sum(row => row.Total),
                values.Average,
                values.Minimum,
                values.Maximum));
            foreach (var row in terrainRows)
            {
                _batch.ResourceTerrainSnapshots.Add(row);
            }
        }
    }

    private void RecordPressureSnapshots(int tick)
    {
        foreach (var pair in _pressureValues)
        {
            _batch.PressureSnapshots.Add(new PressureSnapshotRow(
                _runId,
                tick,
                pair.Key.PressureId,
                pair.Key.ScopeCode,
                pair.Key.ScopeEntityId,
                pair.Value));
            _batch.PressureSourceSnapshots.Add(new PressureSourceSnapshotRow(
                _runId,
                tick,
                pair.Key.PressureId,
                pair.Key.ScopeCode,
                pair.Key.ScopeEntityId,
                SourceCodeId: 0,
                pair.Value));
        }
    }

    private void RecordGeneratorStateSnapshots(int tick)
    {
        foreach (var state in _generatorStates.Values)
        {
            _batch.GeneratorStateSnapshots.Add(new GeneratorStateSnapshotRow(
                _runId,
                tick,
                state.GeneratorId,
                state.Enabled,
                Cadence: 0,
                CooldownRemaining: 0,
                state.RunCount,
                state.LastRunTick));
        }
    }

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

        TrackCreatedEntityForGeneratorAttribution(entity);
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
            MovementDistance(evt)));
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

    protected override void OnResourceChanged(long sequence, in ResourceChangedKernelEvent evt)
    {
        _resourceValues[new ResourceCellKey(evt.ResourceId, evt.Q, evt.R)] = evt.NewValue;
        _batch.ResourceEvents.Add(new ResourceEventRow(
            _runId,
            evt.Tick,
            evt.ResourceId,
            evt.Q,
            evt.R,
            evt.OldValue,
            evt.NewValue,
            evt.NewValue - evt.OldValue,
            evt.SourceCodeId));
        MarkProjected(sequence);
    }

    protected override void OnPressureChanged(long sequence, in PressureChangedKernelEvent evt)
    {
        var key = new PressureKey(evt.PressureId, evt.ScopeCode, evt.ScopeEntityId);
        _pressureValues[key] = evt.NewValue;
        _batch.PressureEvents.Add(new PressureEventRow(
            _runId,
            evt.Tick,
            evt.PressureId,
            evt.ScopeCode,
            evt.ScopeEntityId,
            evt.SourceCodeId,
            evt.OldValue,
            evt.NewValue,
            evt.NewValue - evt.OldValue));
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
            evt.FactId0,
            evt.FactId1,
            evt.FactId2,
            evt.FactId3,
            evt.FactCount,
            CauseId: 0));
        MarkProjected(sequence);
    }

    protected override void OnCellCreated(long sequence, in CellCreatedKernelEvent evt)
    {
        var key = new ObservedMapCellKey(evt.Q, evt.R);
        UpsertMapCell(
            key,
            evt.TerrainId,
            evt.OwnerEntityId,
            evt.SuperregionId,
            evt.ContinentId,
            evt.Tick,
            evt.WorldVersion);
        var causeId = evt.SourceCodeId;
        _batch.MapCellEvents.Add(new MapCellEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.CellCreated,
            evt.SourceCodeId,
            evt.Q,
            evt.R,
            evt.SuperregionId,
            evt.ContinentId,
            PreviousTerrainId: 0,
            evt.TerrainId,
            PreviousOwnerEntityId: 0,
            evt.OwnerEntityId,
            causeId));
        _batch.PlacementEvents.Add(new PlacementEventRow(
            _runId,
            evt.Tick,
            causeId,
            AnchorEntityId: evt.OwnerEntityId,
            PlacementModeCode: 1,
            PlaneCode: PlaneCodes.Physical,
            evt.Q,
            evt.R,
            S: 0,
            evt.TerrainId,
            evt.OwnerEntityId,
            RejectionReasonCode: 0,
            Selected: true));
        MarkProjected(sequence);
    }

    protected override void OnCellChanged(long sequence, in CellChangedKernelEvent evt)
    {
        var key = new ObservedMapCellKey(evt.Q, evt.R);
        UpsertMapCell(
            key,
            evt.CurrentTerrainId,
            evt.CurrentOwnerEntityId,
            evt.SuperregionId,
            evt.ContinentId,
            evt.Tick,
            evt.WorldVersion);
        _batch.MapCellEvents.Add(new MapCellEventRow(
            _runId,
            evt.Tick,
            (int)KernelEventType.CellChanged,
            evt.SourceCodeId,
            evt.Q,
            evt.R,
            evt.SuperregionId,
            evt.ContinentId,
            evt.PreviousTerrainId,
            evt.CurrentTerrainId,
            evt.PreviousOwnerEntityId,
            evt.CurrentOwnerEntityId,
            CauseId: 0));
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

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence);
    }

    protected override void OnGoalScored(long sequence, in GoalScoredKernelEvent evt)
    {
        _batch.PlannerScores.Add(new PlannerScoreRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.GoalId,
            CandidateIndex: 0,
            evt.ReasonCodeId,
            evt.ScoreDelta,
            evt.RunningTotal));
        MarkProjected(sequence);
    }

    protected override void OnPlanStepEmitted(long sequence, in PlanStepEmittedKernelEvent evt)
    {
        var step = new PlannerStepRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.PlanId,
            evt.GoalId,
            evt.StepIndex,
            evt.ActionId,
            evt.TargetEntityId,
            evt.RelationshipKindId,
            PlanningStepRoleCodes.Immediate);
        if (evt.ActionId > 0 && _recordedPlannerSteps.Add(PlannerStepKey.From(step)))
        {
            _batch.PlannerSteps.Add(step);
            AddSelectedPlannerStepCandidate(step);
        }

        MarkProjected(sequence);
    }

    protected override void OnTrace(long sequence, in TraceKernelEvent evt)
    {
        _batch.TraceEvents.Add(new TraceRow(
            _runId,
            evt.Tick,
            evt.PlanId,
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
            evt.Urgency,
            evt.RewardOrBaseScore,
            evt.RawWeight,
            evt.AdjustedWeight));
        ProjectSemanticPlannerTrace(evt);
        MarkProjected(sequence);
    }

    protected override void OnDegeneracyDetected(long sequence, in DegeneracyDetectedKernelEvent evt)
    {
        // MIGRATION: TelemetryBatch has no degeneracy table yet. The schema event is
        // preserved by the event log and can be projected once the table contract exists.
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
            SaturationRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            WildernessRejectedCount: 0,
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
        var causeId = (int)Math.Min(sequence + 1, int.MaxValue);
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
            SaturationRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            WildernessRejectedCount: 0,
            TriggerRejectedCount: 0,
            CandidateCount: 0,
            FailedFireCount: 0,
            evt.TargetEntityId == 0 ? null : evt.TargetEntityId,
            evt.CreatedEntityCount,
            evt.CreatedRelationshipCount,
            causeId));

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
        var displayName = _symbols?.ResolveEntityName(key, $"E{entity.EntityId}") ?? $"E{entity.EntityId}";
        _batch.EntityNameSymbols.Add(new EntityNameSymbolRow(
            _runId,
            entity.EntityId,
            entity.KindId,
            entity.SubtypeId,
            entity.CultureId,
            nameSeed,
            displayName));
    }

    private void TrackCreatedEntityForGeneratorAttribution(ObservedEntityProjection entity)
    {
        if (entity.CreatedTick is not { } createdTick)
        {
            return;
        }

        if (!_unassignedCreatedEntitiesByTick.TryGetValue(createdTick, out var createdEntities))
        {
            createdEntities = [];
            _unassignedCreatedEntitiesByTick[createdTick] = createdEntities;
        }

        createdEntities.Enqueue(entity);
        AssignGeneratedEntities(createdTick);
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
                RegisterEntityNameSymbol(entity);
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
                pending.Remaining--;
            }
        }

        if (createdEntities.Count == 0)
        {
            _unassignedCreatedEntitiesByTick.Remove(tick);
        }

        _pendingGeneratorFires.RemoveAll(pending => pending.Remaining == 0);
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

    private static float ComputePhysicalSpread(List<ObservedEntityProjection> entities)
    {
        var max = 0;
        for (var i = 0; i < entities.Count; i++)
        {
            for (var j = i + 1; j < entities.Count; j++)
            {
                max = Math.Max(
                    max,
                    AxialDistance(
                        entities[i].PhysicalQ,
                        entities[i].PhysicalR,
                        entities[j].PhysicalQ,
                        entities[j].PhysicalR));
            }
        }

        return max;
    }

    private static float ComputeSemanticSpread(List<ObservedEntityProjection> entities)
    {
        var max = 0;
        for (var i = 0; i < entities.Count; i++)
        {
            for (var j = i + 1; j < entities.Count; j++)
            {
                max = Math.Max(
                    max,
                    CubeDistance(
                        entities[i].SemanticQ,
                        entities[i].SemanticR,
                        entities[i].SemanticS,
                        entities[j].SemanticQ,
                        entities[j].SemanticR,
                        entities[j].SemanticS));
            }
        }

        return max;
    }

    private static int AxialDistance(int fromQ, int fromR, int toQ, int toR)
    {
        var fromS = -fromQ - fromR;
        var toS = -toQ - toR;
        return CubeDistance(fromQ, fromR, fromS, toQ, toR, toS);
    }

    private static int CubeDistance(int fromQ, int fromR, int fromS, int toQ, int toR, int toS) =>
        (Math.Abs(fromQ - toQ) + Math.Abs(fromR - toR) + Math.Abs(fromS - toS)) / 2;
}
