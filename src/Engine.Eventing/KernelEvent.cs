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
    GambitExecuted = 13,
    CommandApplied = 14,
    CommandRejected = 15,
    TickCompleted = 16,
    GeneratorEnabledChanged = 17,
    GambitEvaluated = 18,
    CellCreated = 20,
    PlanStarted = 100,
    GoalConsidered = 101,
    GoalScored = 102,
    GoalSelected = 103,
    PlanStepEmitted = 104,
    PlanCommitted = 105,
    PlanAbandoned = 106,
}

[Flags]
public enum KernelEventState
{
    None = 0,
    Succeeded = 1 << 0,
    Stale = 1 << 1,
    Truncated = 1 << 2,
    Overflowed = 1 << 3,
    Failed = 1 << 4,
}

public readonly record struct EntityCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int SourceCodeId,
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
    int SourceCodeId,
    int KindId,
    int SubtypeId,
    int PreviousStatusCode,
    int CurrentStatusCode);

public readonly record struct EntityMovedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int SourceCodeId,
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
    int SourceCodeId,
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

public readonly record struct GambitEvaluatedKernelEvent(
    int Tick,
    long WorldVersion,
    int EntityId,
    int KindId,
    int ExecutedGambitId,
    int CandidateCount,
    KernelEventState State);

public readonly record struct GambitExecutedKernelEvent(
    int Tick,
    long WorldVersion,
    int ActorId,
    int ActorKindId,
    int GambitId,
    int AffectedEntityId0,
    int AffectedEntityId1,
    int AffectedEntityCount,
    KernelEventState State);

public readonly record struct CommandAppliedKernelEvent(
    int Tick,
    long WorldVersion,
    int CommandId,
    int CommandType,
    int IssueVersion);

public readonly record struct CommandRejectedKernelEvent(
    int Tick,
    long WorldVersion,
    int CommandId,
    int CommandType,
    int IssueVersion,
    int ReasonCodeId);

public readonly record struct TickCompletedKernelEvent(
    int Tick,
    long WorldVersion);

public readonly record struct GeneratorEnabledChangedKernelEvent(
    int Tick,
    long WorldVersion,
    int GeneratorId,
    int Enabled);

public readonly record struct CellCreatedKernelEvent(
    int Tick,
    long WorldVersion,
    int Q,
    int R,
    int SourceCodeId,
    int TerrainId,
    int OwnerEntityId);

public readonly record struct PlanStartedKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int ReasonCodeId,
    int CandidateCount);

public readonly record struct GoalConsideredKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int GoalId,
    int CandidateIndex,
    float PreliminaryScore);

public readonly record struct GoalScoredKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int GoalId,
    int ReasonCodeId,
    int ScoringPassIndex,
    float ScoreDelta,
    float RunningTotal);

public readonly record struct GoalSelectedKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int GoalId,
    float FinalScore);

public readonly record struct PlanStepEmittedKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int GoalId,
    int StepIndex,
    int ProcessId,
    int TargetEntityId,
    int RelationshipKindId);

public readonly record struct PlanCommittedKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int StepCount,
    int ReasonCodeId,
    float TotalScore);

public readonly record struct PlanAbandonedKernelEvent(
    int Tick,
    long WorldVersion,
    int PlanId,
    int EntityId,
    int ReasonCodeId);

public readonly partial record struct KernelEvent
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
    private readonly int _k;
    private readonly int _l;
    private readonly float _x;
    private readonly float _y;
    private readonly float _z;
    private readonly float _w;

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
        int k = 0,
        int l = 0,
        float x = 0f,
        float y = 0f,
        float z = 0f,
        float w = 0f)
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
        _k = k;
        _l = l;
        _x = x;
        _y = y;
        _z = z;
        _w = w;
    }

    public KernelEventType EventType { get; }
    public int Tick { get; }
    public long WorldVersion { get; }

    public const int IntSlotCount = 12;
    public const int FloatSlotCount = 4;

    public int GetIntSlot(int slot) => slot switch
    {
        0 => _a,
        1 => _b,
        2 => _c,
        3 => _d,
        4 => _e,
        5 => _f,
        6 => _g,
        7 => _h,
        8 => _i,
        9 => _j,
        10 => _k,
        11 => _l,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event int slots are numbered 0 through 11."),
    };

    public float GetFloatSlot(int slot) => slot switch
    {
        0 => _x,
        1 => _y,
        2 => _z,
        3 => _w,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Kernel event float slots are numbered 0 through 3."),
    };
}

public readonly record struct SequencedKernelEvent(long Sequence, KernelEvent Event);
