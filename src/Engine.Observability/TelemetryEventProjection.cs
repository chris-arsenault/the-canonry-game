namespace Engine.Observability;

using System.Collections.ObjectModel;

using Engine.Eventing;
using Engine.Projection;

internal sealed record ObservabilityProjectionMigrationItem(
    string Area,
    string RequiredPrimitiveSource,
    string Reason);

internal sealed class TelemetryEventProjection : KernelEventProjector
{
    private readonly int _runId;
    private readonly TelemetryBatch _batch;
    private readonly IProjectionSymbolCatalog? _symbols;
    private readonly Dictionary<int, ObservedEntityProjection> _entities = [];
    private readonly Dictionary<ObservedRelationshipKey, ObservedRelationshipProjection> _relationships = [];
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
            "process candidate rows and locality opportunities",
            "process-candidate and locality-opportunity trace events",
            "ProcessEvaluatedKernelEvent carries the selected goal/process and candidate count but not candidate diagnostics."),
        new(
            "map and resource snapshots",
            "startup map/resource snapshots plus coverage/resource aggregate events",
            "The event stream has resource deltas but no full grid, terrain, coverage, or untouched initial stockpile state."),
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

        _batch.TickSnapshots.Add(new TickSnapshotRow(
            _runId,
            tick,
            activeEntities.Count,
            historicalCount,
            _totalGeneratorFires,
            activeRelationships.Count,
            CoverageCount: 0,
            CoveredCells: 0,
            UncoveredCells: 0,
            ComputePhysicalSpread(activeEntities),
            ComputeSemanticSpread(activeEntities)));
    }

    public void RecordKindSnapshots(int tick)
    {
        foreach (var group in _entities.Values.GroupBy(entity => (entity.KindId, entity.SubtypeId)))
        {
            _batch.KindSnapshots.Add(new KindSnapshotRow(
                _runId,
                tick,
                group.Key.KindId,
                group.Key.SubtypeId,
                group.Count(entity => IsActiveStatus(entity.StatusCode)),
                group.Count(entity => IsHistoricalStatus(entity.StatusCode)),
                AverageHealth: 0f,
                MinimumHealth: 0f,
                MaximumHealth: 0f));
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
            evt.FactCount));
        MarkProjected(sequence);
    }

    protected override void OnProcessEvaluated(long sequence, in ProcessEvaluatedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        _batch.ProcessEvents.Add(new ProcessEventRow(
            _runId,
            evt.Tick,
            evt.EntityId,
            evt.KindId,
            evt.SelectedGoalId > 0 ? evt.SelectedGoalId : null,
            evt.ExecutedProcessId > 0 ? evt.ExecutedProcessId : null,
            evt.State.HasFlag(KernelEventState.Stale),
            (int)evt.State,
            evt.CandidateCount,
            VisitId: null,
            LocalityId: null,
            LocalityPlaneCode: null,
            LocalityQ: null,
            LocalityR: null,
            LocalityS: null,
            LocalityBundleScore: null,
            LocalityGoalCount: null,
            LocalityDistinctTargetCount: null,
            LocalityDistinctEffectCount: null,
            LocalityDistance: null,
            LocalityIsCurrent: null,
            LocalityIsCommitted: null,
            LocalityArrived: null,
            LocalityActionsRemaining: null,
            LocalityRealizedActions: null,
            LocalitySelectionReasonCode: null));
        MarkProjected(sequence);
    }

    protected override void OnProcessExecuted(long sequence, in ProcessExecutedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.ActorId);
        entity.KindId = evt.ActorKindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;

        _batch.ProcessEvents.Add(new ProcessEventRow(
            _runId,
            evt.Tick,
            evt.ActorId,
            evt.ActorKindId,
            SelectedGoalId: null,
            evt.ProcessId,
            evt.State.HasFlag(KernelEventState.Stale),
            (int)evt.State,
            CandidateCount: 0,
            VisitId: null,
            LocalityId: null,
            LocalityPlaneCode: null,
            LocalityQ: null,
            LocalityR: null,
            LocalityS: null,
            LocalityBundleScore: null,
            LocalityGoalCount: null,
            LocalityDistinctTargetCount: null,
            LocalityDistinctEffectCount: null,
            LocalityDistance: null,
            LocalityIsCurrent: null,
            LocalityIsCommitted: null,
            LocalityArrived: null,
            LocalityActionsRemaining: null,
            LocalityRealizedActions: null,
            LocalitySelectionReasonCode: null));
        MarkProjected(sequence);
    }

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        // MIGRATION: GoalGeneratedKernelEvent has counts only. Detailed PlannerGoalRow
        // output must wait for primitive goal payload events.
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
            evt.CreatedRelationshipCount));

        if (evt.CreatedEntityCount > 0)
        {
            _pendingGeneratorFires.Add(new PendingGeneratorFire(evt.Tick, evt.GeneratorId, evt.CreatedEntityCount));
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

    private sealed class PendingGeneratorFire(int tick, int generatorId, int remaining)
    {
        public int Tick { get; } = tick;

        public int GeneratorId { get; } = generatorId;

        public int Remaining { get; set; } = remaining;
    }
}

internal readonly record struct ObservedRelationshipKey(
    int SourceEntityId,
    int TargetEntityId,
    int RelationshipKindId);

internal sealed class ObservedRelationshipProjection(
    ObservedRelationshipKey key,
    int changeCodeId,
    int polarityCode,
    float strength,
    bool isActive,
    int lastUpdatedTick,
    long worldVersion)
{
    public ObservedRelationshipKey Key { get; } = key;

    public int RelationshipKindId => Key.RelationshipKindId;

    public int ChangeCodeId { get; } = changeCodeId;

    public int PolarityCode { get; } = polarityCode;

    public float Strength { get; } = strength;

    public bool IsActive { get; } = isActive;

    public int LastUpdatedTick { get; } = lastUpdatedTick;

    public long WorldVersion { get; } = worldVersion;
}

internal sealed class ObservedEntityProjection(int entityId)
{
    public int EntityId { get; } = entityId;

    public int KindId { get; internal set; }

    public int SubtypeId { get; internal set; }

    public int CultureId { get; internal set; }

    public int NameSeed { get; internal set; }

    public int? PreviousStatusCode { get; internal set; }

    public int StatusCode { get; internal set; }

    public int PhysicalQ { get; internal set; }

    public int PhysicalR { get; internal set; }

    public int SemanticQ { get; internal set; }

    public int SemanticR { get; internal set; }

    public int SemanticS { get; internal set; }

    public int? CreatedTick { get; internal set; }

    public int LastUpdatedTick { get; internal set; }

    public long WorldVersion { get; internal set; }

    public Dictionary<int, float> Attributes { get; } = [];
}
