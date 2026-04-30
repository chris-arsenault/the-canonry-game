namespace Engine.Projection;

using Engine.Eventing;

public sealed record UiProjectionMigrationItem(
    string Area,
    string RequiredPrimitiveSource,
    string Reason);

public sealed class UiProjectionState : KernelEventProjector
{
    private readonly Dictionary<int, UiEntityProjection> _entities = [];
    private readonly Dictionary<UiRelationshipKey, UiRelationshipProjection> _relationships = [];
    private readonly Dictionary<UiResourceKey, UiResourceProjection> _resources = [];
    private readonly Dictionary<int, UiGeneratorProjection> _generators = [];
    private readonly List<UiGeneratorFireProjection> _generatorFires = [];
    private readonly List<UiDegeneracyProjection> _degeneracyEvents = [];

    public static IReadOnlyList<UiProjectionMigrationItem> FutureMigrationItems { get; } =
    [
        new(
            "entity display names",
            "entity-created name seed or startup entity-name snapshot",
            "EntityCreatedKernelEvent currently carries primitive identity but not display-name seed material."),
        new(
            "initial attributes",
            "startup attribute snapshot or attribute-created/initialized events",
            "AttributeChangedKernelEvent can replay deltas after the cursor starts, but not untouched initial values."),
        new(
            "map terrain and coverage",
            "cell projection wired into the UI renderer",
            "CellCreatedKernelEvent carries startup map state, but the UI renderer still needs to consume the dedicated map projection."),
        new(
            "relationship removal semantics",
            "explicit relationship inactive/cull code contract",
            "Projection currently treats zero-or-lower new strength as inactive because no generic active flag exists.")
    ];

    public IReadOnlyDictionary<int, UiEntityProjection> Entities => _entities;

    public IReadOnlyDictionary<UiRelationshipKey, UiRelationshipProjection> Relationships => _relationships;

    public IReadOnlyDictionary<UiResourceKey, UiResourceProjection> Resources => _resources;

    public IReadOnlyDictionary<int, UiGeneratorProjection> Generators => _generators;

    public IReadOnlyList<UiGeneratorFireProjection> GeneratorFires => _generatorFires;

    public IReadOnlyList<UiDegeneracyProjection> DegeneracyEvents => _degeneracyEvents;

    public long LastProjectedSequence { get; private set; } = -1;

    public int CurrentTick { get; private set; }

    public long WorldVersion { get; private set; }

    public long DroppedEvents { get; private set; }

    public bool IsComplete => DroppedEvents == 0;

    public override void OnEventsDropped(long droppedEventCount, long nextSequence)
    {
        DroppedEvents += droppedEventCount;
        LastProjectedSequence = Math.Max(LastProjectedSequence, nextSequence - 1);
    }

    protected override void OnEntityCreated(long sequence, in EntityCreatedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.SubtypeId = evt.SubtypeId;
        entity.CultureId = evt.CultureId;
        // MIGRATION: EntityCreatedKernelEvent does not yet carry a name seed.
        // EntityId keeps generated projection names stable until the kernel adds it.
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
        entity.WasCreatedFromEvent = true;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
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
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
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

        entity.LastMove = new UiMovementProjection(
            evt.Tick,
            evt.PlaneCode,
            evt.FromQ,
            evt.FromR,
            evt.FromS,
            evt.ToQ,
            evt.ToR,
            evt.ToS);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnAttributeChanged(long sequence, in AttributeChangedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.Attributes[evt.AttributeId] = new UiAttributeProjection(
            evt.AttributeId,
            evt.SourceCodeId,
            evt.OldValue,
            evt.NewValue,
            evt.Tick,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnResourceChanged(long sequence, in ResourceChangedKernelEvent evt)
    {
        var key = new UiResourceKey(evt.ResourceId, evt.Q, evt.R);
        _resources[key] = new UiResourceProjection(
            evt.ResourceId,
            evt.Q,
            evt.R,
            evt.SourceCodeId,
            evt.OldValue,
            evt.NewValue,
            evt.Tick,
            evt.WorldVersion);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnRelationshipChanged(long sequence, in RelationshipChangedKernelEvent evt)
    {
        var key = new UiRelationshipKey(
            evt.SourceEntityId,
            evt.TargetEntityId,
            evt.RelationshipKindId);
        _relationships[key] = new UiRelationshipProjection(
            key,
            evt.ChangeCodeId,
            evt.SourceCodeId,
            evt.PolarityCode,
            evt.FactId0,
            evt.FactId1,
            evt.FactId2,
            evt.FactId3,
            evt.FactCount,
            evt.PreviousStrength,
            evt.NewStrength,
            evt.NewStrength > 0f,
            evt.Tick,
            evt.WorldVersion);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGoalGenerated(long sequence, in GoalGeneratedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.EntityId);
        entity.KindId = evt.KindId;
        entity.LastGoalGeneration = new UiGoalGenerationProjection(
            evt.Tick,
            evt.GoalCount,
            evt.ActionableGoalCount,
            evt.State,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnDegeneracyDetected(long sequence, in DegeneracyDetectedKernelEvent evt)
    {
        _degeneracyEvents.Add(new UiDegeneracyProjection(
            evt.Tick,
            evt.DetectorId,
            evt.SeverityCode,
            evt.ReasonCodeId,
            evt.WorldVersion));
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorEvaluated(long sequence, in GeneratorEvaluatedKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        generator.LastEvaluation = new UiGeneratorEvaluationProjection(
            evt.Tick,
            evt.StatusCode,
            evt.SelectedTargetEntityId,
            evt.CandidateCount,
            evt.FailedFireCount,
            evt.WorldVersion);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorFired(long sequence, in GeneratorFiredKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        var fire = new UiGeneratorFireProjection(
            evt.Tick,
            evt.GeneratorId,
            evt.TargetEntityId,
            evt.CreatedEntityCount,
            evt.CreatedRelationshipCount,
            evt.WorldVersion);
        generator.TotalFired++;
        generator.LastFire = fire;
        _generatorFires.Add(fire);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGambitExecuted(long sequence, in GambitExecutedKernelEvent evt)
    {
        var entity = GetOrCreateEntity(evt.ActorId);
        entity.KindId = evt.ActorKindId;
        entity.LastGambitExecution = new UiGambitExecutionProjection(
            evt.Tick,
            evt.GambitId,
            evt.AffectedEntityId0,
            evt.AffectedEntityId1,
            evt.AffectedEntityCount,
            evt.State,
            evt.WorldVersion);
        entity.LastUpdatedTick = evt.Tick;
        entity.WorldVersion = evt.WorldVersion;
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnTickCompleted(long sequence, in TickCompletedKernelEvent evt)
    {
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    protected override void OnGeneratorEnabledChanged(long sequence, in GeneratorEnabledChangedKernelEvent evt)
    {
        var generator = GetOrCreateGenerator(evt.GeneratorId);
        generator.IsEnabled = evt.Enabled != 0;
        generator.LastEnabledChanged = new UiGeneratorEnabledProjection(
            evt.Tick,
            evt.Enabled != 0,
            evt.WorldVersion);
        MarkProjected(sequence, evt.Tick, evt.WorldVersion);
    }

    private UiEntityProjection GetOrCreateEntity(int entityId)
    {
        if (_entities.TryGetValue(entityId, out var entity))
        {
            return entity;
        }

        entity = new UiEntityProjection(entityId);
        _entities.Add(entityId, entity);
        return entity;
    }

    private UiGeneratorProjection GetOrCreateGenerator(int generatorId)
    {
        if (_generators.TryGetValue(generatorId, out var generator))
        {
            return generator;
        }

        generator = new UiGeneratorProjection(generatorId);
        _generators.Add(generatorId, generator);
        return generator;
    }

    private void MarkProjected(long sequence, int tick, long worldVersion)
    {
        LastProjectedSequence = sequence;
        CurrentTick = Math.Max(CurrentTick, tick);
        WorldVersion = Math.Max(WorldVersion, worldVersion);
    }
}

public sealed class UiEntityProjection(int entityId)
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

    public bool WasCreatedFromEvent { get; internal set; }

    public Dictionary<int, UiAttributeProjection> Attributes { get; } = [];

    public UiMovementProjection? LastMove { get; internal set; }

    public UiGoalGenerationProjection? LastGoalGeneration { get; internal set; }

    public UiGambitExecutionProjection? LastGambitExecution { get; internal set; }
}

public sealed class UiGeneratorProjection(int generatorId)
{
    public int GeneratorId { get; } = generatorId;

    public bool IsEnabled { get; internal set; }

    public int TotalFired { get; internal set; }

    public UiGeneratorEnabledProjection? LastEnabledChanged { get; internal set; }

    public UiGeneratorEvaluationProjection? LastEvaluation { get; internal set; }

    public UiGeneratorFireProjection? LastFire { get; internal set; }
}

public readonly record struct UiRelationshipKey(
    int SourceEntityId,
    int TargetEntityId,
    int RelationshipKindId);

public readonly record struct UiResourceKey(
    int ResourceId,
    int Q,
    int R);

public sealed record UiAttributeProjection(
    int AttributeId,
    int SourceCodeId,
    float OldValue,
    float NewValue,
    int Tick,
    long WorldVersion);

public sealed record UiMovementProjection(
    int Tick,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS);

public sealed record UiRelationshipProjection(
    UiRelationshipKey Key,
    int ChangeCodeId,
    int SourceCodeId,
    int PolarityCode,
    int FactId0,
    int FactId1,
    int FactId2,
    int FactId3,
    int FactCount,
    float PreviousStrength,
    float NewStrength,
    bool IsActive,
    int Tick,
    long WorldVersion);

public sealed record UiResourceProjection(
    int ResourceId,
    int Q,
    int R,
    int SourceCodeId,
    float OldValue,
    float NewValue,
    int Tick,
    long WorldVersion);

public sealed record UiGeneratorEnabledProjection(
    int Tick,
    bool IsEnabled,
    long WorldVersion);

public sealed record UiGoalGenerationProjection(
    int Tick,
    int GoalCount,
    int ActionableGoalCount,
    KernelEventState State,
    long WorldVersion);

public sealed record UiGambitExecutionProjection(
    int Tick,
    int GambitId,
    int AffectedEntityId0,
    int AffectedEntityId1,
    int AffectedEntityCount,
    KernelEventState State,
    long WorldVersion);

public sealed record UiGeneratorEvaluationProjection(
    int Tick,
    int StatusCode,
    int SelectedTargetEntityId,
    int CandidateCount,
    int FailedFireCount,
    long WorldVersion);

public sealed record UiGeneratorFireProjection(
    int Tick,
    int GeneratorId,
    int TargetEntityId,
    int CreatedEntityCount,
    int CreatedRelationshipCount,
    long WorldVersion);

public sealed record UiDegeneracyProjection(
    int Tick,
    int DetectorId,
    int SeverityCode,
    int ReasonCodeId,
    long WorldVersion);
