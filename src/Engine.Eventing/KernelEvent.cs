namespace Engine.Eventing;

public enum KernelEventType
{
    None = 0,
    EntityCreated = 1,
    EntityStatusChanged = 2,
    EntityMoved = 3,
    AttributeChanged = 4,
    ResourceChanged = 5,
    RelationshipChanged = 6,
    ProcessEvaluated = 7,
    ProcessExecuted = 8,
    GoalGenerated = 9,
    DegeneracyDetected = 10,
    GeneratorEvaluated = 11,
    GeneratorFired = 12,
    DecisionExecuted = 13,
}

[Flags]
public enum KernelEventState
{
    None = 0,
    Succeeded = 1 << 0,
    Stale = 1 << 1,
    Truncated = 1 << 2,
    Overflowed = 1 << 3,
}

public readonly record struct EntityCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int PhysicalQ,
    int PhysicalR,
    int SemanticQ,
    int SemanticR,
    int SemanticS);

public readonly record struct EntityStatusChangedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int KindId,
    int SubtypeId,
    int PreviousStatusCode,
    int CurrentStatusCode);

public readonly record struct EntityMovedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int PlaneCode,
    int FromQ,
    int FromR,
    int FromS,
    int ToQ,
    int ToR,
    int ToS);

public readonly record struct AttributeChangedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int AttributeId,
    int SourceCodeId,
    float OldValue,
    float NewValue);

public readonly record struct ResourceChangedKernelEvent(
    int Tick,
    long WorldVersion,
    int ResourceId,
    int Q,
    int R,
    int SourceCodeId,
    float OldValue,
    float NewValue);

public readonly record struct RelationshipChangedKernelEvent(
    int Tick,
    long WorldVersion,
    int ChangeCodeId,
    int RelationshipKindId,
    int PolarityCode,
    int SourceEntityId,
    int TargetEntityId,
    int FactId0,
    int FactId1,
    int FactId2,
    int FactId3,
    int FactCount,
    float PreviousStrength,
    float NewStrength);

public readonly record struct ProcessEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int KindId,
    int SelectedGoalId,
    int ExecutedProcessId,
    int CandidateCount,
    KernelEventState State);

public readonly record struct ProcessExecutedKernelEvent(
    int Tick,
    long WorldVersion,
    int ActorId,
    int ActorKindId,
    int ProcessId,
    int AffectedEntityId0,
    int AffectedEntityId1,
    int AffectedEntityCount,
    KernelEventState State);

public readonly record struct GoalGeneratedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int KindId,
    int GoalCount,
    int ActionableGoalCount,
    KernelEventState State);

public readonly record struct DegeneracyDetectedKernelEvent(
    int Tick,
    long WorldVersion,
    int DetectorId,
    int SeverityCode,
    int ReasonCodeId);

public readonly record struct GeneratorEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    int GeneratorId,
    int StatusCode,
    int SelectedTargetEntityId,
    int CandidateCount,
    int FailedFireCount);

public readonly record struct GeneratorFiredKernelEvent(
    int Tick,
    long WorldVersion,
    int GeneratorId,
    int TargetEntityId,
    int CreatedEntityCount,
    int CreatedRelationshipCount);

public readonly record struct DecisionExecutedKernelEvent(
    int Tick,
    long WorldVersion,
    int ActorId,
    int ActorKindId,
    int DecisionId,
    int CardId,
    KernelEventState State);

public readonly record struct KernelEvent
{
    private readonly int _a;
    private readonly int _b;
    private readonly int _c;
    private readonly int _d;
    private readonly int _e;
    private readonly int _f;
    private readonly int _g;
    private readonly int _h;
    private readonly int _i;
    private readonly int _j;
    private readonly float _x;
    private readonly float _y;

    private KernelEvent(
        KernelEventType eventType,
        int tick,
        long worldVersion,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        int e = 0,
        int f = 0,
        int g = 0,
        int h = 0,
        int i = 0,
        int j = 0,
        float x = 0f,
        float y = 0f)
    {
        EventType = eventType;
        Tick = tick;
        WorldVersion = worldVersion;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _e = e;
        _f = f;
        _g = g;
        _h = h;
        _i = i;
        _j = j;
        _x = x;
        _y = y;
    }

    public KernelEventType EventType { get; }
    public int Tick { get; }
    public long WorldVersion { get; }

    public static KernelEvent From(in EntityCreatedKernelEvent payload) =>
        new(
            KernelEventType.EntityCreated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.SubtypeId,
            payload.CultureId,
            payload.PhysicalQ,
            payload.PhysicalR,
            payload.SemanticQ,
            payload.SemanticR,
            payload.SemanticS);

    public static KernelEvent From(in EntityStatusChangedKernelEvent payload) =>
        new(
            KernelEventType.EntityStatusChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.SubtypeId,
            payload.PreviousStatusCode,
            payload.CurrentStatusCode);

    public static KernelEvent From(in EntityMovedKernelEvent payload) =>
        new(
            KernelEventType.EntityMoved,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.PlaneCode,
            payload.FromQ,
            payload.FromR,
            payload.FromS,
            payload.ToQ,
            payload.ToR,
            payload.ToS);

    public static KernelEvent From(in AttributeChangedKernelEvent payload) =>
        new(
            KernelEventType.AttributeChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.AttributeId,
            payload.SourceCodeId,
            x: payload.OldValue,
            y: payload.NewValue);

    public static KernelEvent From(in ResourceChangedKernelEvent payload) =>
        new(
            KernelEventType.ResourceChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.ResourceId,
            payload.Q,
            payload.R,
            payload.SourceCodeId,
            x: payload.OldValue,
            y: payload.NewValue);

    public static KernelEvent From(in RelationshipChangedKernelEvent payload) =>
        new(
            KernelEventType.RelationshipChanged,
            payload.Tick,
            payload.WorldVersion,
            payload.ChangeCodeId,
            payload.RelationshipKindId,
            payload.PolarityCode,
            payload.SourceEntityId,
            payload.TargetEntityId,
            payload.FactId0,
            payload.FactId1,
            payload.FactId2,
            payload.FactId3,
            payload.FactCount,
            payload.PreviousStrength,
            payload.NewStrength);

    public static KernelEvent From(in ProcessEvaluatedKernelEvent payload) =>
        new(
            KernelEventType.ProcessEvaluated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.SelectedGoalId,
            payload.ExecutedProcessId,
            payload.CandidateCount,
            (int)payload.State);

    public static KernelEvent From(in ProcessExecutedKernelEvent payload) =>
        new(
            KernelEventType.ProcessExecuted,
            payload.Tick,
            payload.WorldVersion,
            payload.ActorId,
            payload.ActorKindId,
            payload.ProcessId,
            payload.AffectedEntityId0,
            payload.AffectedEntityId1,
            payload.AffectedEntityCount,
            (int)payload.State);

    public static KernelEvent From(in GoalGeneratedKernelEvent payload) =>
        new(
            KernelEventType.GoalGenerated,
            payload.Tick,
            payload.WorldVersion,
            payload.EntityId,
            payload.KindId,
            payload.GoalCount,
            payload.ActionableGoalCount,
            (int)payload.State);

    public static KernelEvent From(in DegeneracyDetectedKernelEvent payload) =>
        new(
            KernelEventType.DegeneracyDetected,
            payload.Tick,
            payload.WorldVersion,
            payload.DetectorId,
            payload.SeverityCode,
            payload.ReasonCodeId);

    public static KernelEvent From(in GeneratorEvaluatedKernelEvent payload) =>
        new(
            KernelEventType.GeneratorEvaluated,
            payload.Tick,
            payload.WorldVersion,
            payload.GeneratorId,
            payload.StatusCode,
            payload.SelectedTargetEntityId,
            payload.CandidateCount,
            payload.FailedFireCount);

    public static KernelEvent From(in GeneratorFiredKernelEvent payload) =>
        new(
            KernelEventType.GeneratorFired,
            payload.Tick,
            payload.WorldVersion,
            payload.GeneratorId,
            payload.TargetEntityId,
            payload.CreatedEntityCount,
            payload.CreatedRelationshipCount);

    public static KernelEvent From(in DecisionExecutedKernelEvent payload) =>
        new(
            KernelEventType.DecisionExecuted,
            payload.Tick,
            payload.WorldVersion,
            payload.ActorId,
            payload.ActorKindId,
            payload.DecisionId,
            payload.CardId,
            (int)payload.State);

    public EntityCreatedKernelEvent AsEntityCreated()
    {
        EnsureType(KernelEventType.EntityCreated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h, _i);
    }

    public EntityStatusChangedKernelEvent AsEntityStatusChanged()
    {
        EnsureType(KernelEventType.EntityStatusChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e);
    }

    public EntityMovedKernelEvent AsEntityMoved()
    {
        EnsureType(KernelEventType.EntityMoved);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h);
    }

    public AttributeChangedKernelEvent AsAttributeChanged()
    {
        EnsureType(KernelEventType.AttributeChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _x, _y);
    }

    public ResourceChangedKernelEvent AsResourceChanged()
    {
        EnsureType(KernelEventType.ResourceChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _x, _y);
    }

    public RelationshipChangedKernelEvent AsRelationshipChanged()
    {
        EnsureType(KernelEventType.RelationshipChanged);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _x, _y);
    }

    public ProcessEvaluatedKernelEvent AsProcessEvaluated()
    {
        EnsureType(KernelEventType.ProcessEvaluated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, (KernelEventState)_f);
    }

    public ProcessExecutedKernelEvent AsProcessExecuted()
    {
        EnsureType(KernelEventType.ProcessExecuted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e, _f, (KernelEventState)_g);
    }

    public GoalGeneratedKernelEvent AsGoalGenerated()
    {
        EnsureType(KernelEventType.GoalGenerated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, (KernelEventState)_e);
    }

    public DegeneracyDetectedKernelEvent AsDegeneracyDetected()
    {
        EnsureType(KernelEventType.DegeneracyDetected);
        return new(Tick, WorldVersion, _a, _b, _c);
    }

    public GeneratorEvaluatedKernelEvent AsGeneratorEvaluated()
    {
        EnsureType(KernelEventType.GeneratorEvaluated);
        return new(Tick, WorldVersion, _a, _b, _c, _d, _e);
    }

    public GeneratorFiredKernelEvent AsGeneratorFired()
    {
        EnsureType(KernelEventType.GeneratorFired);
        return new(Tick, WorldVersion, _a, _b, _c, _d);
    }

    public DecisionExecutedKernelEvent AsDecisionExecuted()
    {
        EnsureType(KernelEventType.DecisionExecuted);
        return new(Tick, WorldVersion, _a, _b, _c, _d, (KernelEventState)_e);
    }

    private void EnsureType(KernelEventType expected)
    {
        if (EventType != expected)
        {
            throw new InvalidOperationException($"Kernel event is {EventType}, not {expected}.");
        }
    }
}

public readonly record struct SequencedKernelEvent(long Sequence, KernelEvent Event);
