namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class ProcessProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledProcessRule> Processes { get; init; } = [];
    public IReadOnlyList<ProcessObjectiveRule> Objectives { get; init; } = [];
}

public sealed record CompiledProcessRule(
    int KindId,
    int ProcessId,
    int CooldownTicks,
    int EntityCooldownTicks,
    ProcessProgramTargetMode TargetMode,
    int TargetKindId,
    int WorkCommitmentTicks,
    IReadOnlyList<ProcessProgramEffectRule> GoalEffects,
    IReadOnlyList<ProcessProgramProviderSelectorRule> ProviderSelectors,
    IReadOnlyList<ProcessProgramSupportRule> SupportRules,
    IReadOnlyList<ProcessProgramPreconditionOp> Preconditions,
    IReadOnlyList<ProcessProgramExpressionOp> ScoreExpression,
    IReadOnlyList<ProcessProgramExpressionOp> RewardExpression,
    IReadOnlyList<ProcessProgramStepRule> Steps);

public enum ProcessProgramTargetMode
{
    Self,
    AnyOther,
    KindOther,
}

public enum ProcessProgramOperation
{
    None,
    AddAttribute,
    SetAttribute,
    MovePhysical,
    MoveSemantic,
    AddResource,
    SetStatus,
    CreateRelationship,
    SetRelationshipStrength,
    AddTargetAttribute,
    TransferAttribute,
    AddLocationResource,
    MoveTowardTargetPhysical,
    WanderPhysical,
    MoveTowardTargetSemantic,
    WanderSemantic,
    CreateEntity,
    AddPressure,
    SetHistorical,
    AddRelatedEntitiesAttribute,
    CalculateVariable,
    AddAttributeFromVariable,
    AddTargetAttributeFromVariable,
    CreateFixedRelationship,
}

public enum ProcessProgramEndpoint
{
    None = 0,
    Self = 1,
    Target = 2,
}

public enum ProcessProgramRelationshipTraversalDirection
{
    None = 0,
    Outgoing = 1,
    Incoming = 2,
}

public enum ProcessProgramPrecondition
{
    TargetIsSelf,
    TargetIsNotSelf,
    SelfAttributeRange,
    TargetAttributeRange,
    PlaneDistance,
    LocationResourceRange,
    Always,
    SelfHasRelationshipToTarget,
    TargetHasRelationshipToSelf,
    RelatedEntityMatchesTarget,
    RelationshipExists,
    PressureRange,
    CooldownElapsed,
    And,
    Or,
    Not,
    EntityCountAtLeast,
    HasColocatedEntity,
}

public enum ProcessProgramExpression
{
    Literal,
    SelfAttribute,
    TargetAttribute,
    SelfLocationResource,
    TargetLocationResource,
    Add,
    Subtract,
    Multiply,
    Divide,
    Min,
    Max,
    Clamp,
    Abs,
    Variable,
    Distance,
}

public enum ProcessProgramEffectScope
{
    SelfAttribute,
    TargetAttribute,
    Resource,
    Structural,
    Relational,
}

public enum ProcessObjectiveUrgency
{
    StandardNeed,
    Baseline,
    RangeDefault,
    Fixed,
}

public enum ProcessProgramProviderSelector
{
    TargetAttribute,
    Structural,
    Resource,
    LocationAttribute,
    OutgoingRelationship,
    IncomingRelationship,
}

public sealed record ProcessObjectiveRule(
    int KindId,
    ProcessProgramEffectScope EffectScope,
    int EffectValueId,
    int EffectKindId,
    ProcessObjectiveUrgency Urgency,
    int AttributeId,
    float Minimum,
    float Maximum,
    float DefaultValue,
    float Baseline,
    float BaseUrgency);

public sealed record ProcessProgramEffectRule(
    ProcessProgramEffectScope Scope,
    int ValueId,
    int KindId);

public sealed record ProcessProgramProviderSelectorRule(
    ProcessProgramProviderSelector Selector,
    int KindId,
    int ValueId,
    int RelationshipKindId,
    int FactId,
    float MinimumStrength);

public sealed record ProcessProgramSupportRule(
    ProcessProgramEffectScope EffectScope,
    int EffectValueId,
    int EffectKindId,
    float DesiredMinimum,
    float UrgencyScale,
    IReadOnlyList<ProcessProgramExpressionOp> CurrentValueExpression);

public sealed record ProcessProgramPreconditionOp(
    ProcessProgramPrecondition Code,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    float X = 0f,
    float Y = 0f);

public sealed record ProcessProgramExpressionOp(
    ProcessProgramExpression Code,
    int A = 0,
    float X = 0f);

public sealed record ProcessProgramStepRule(
    ProcessProgramOperation Operation,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    float X = 0f,
    float Y = 0f,
    IReadOnlyList<ProcessProgramExpressionOp>? Expression = null);
