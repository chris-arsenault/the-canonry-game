namespace Engine.Domain.Compilation.KernelPrograms;

public enum BehaviorProgramTargetMode
{
    Self,
    AnyOther,
    KindOther,
}

public enum BehaviorProgramOperation
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
    CreateEndpointRelationship,
    SetEndpointRelationshipStrength,
    MoveTargetToSelfPhysical,
    SetTargetCoverageTerrain,
}

public enum BehaviorProgramEndpoint
{
    None = 0,
    Self = 1,
    Target = 2,
}

public enum BehaviorProgramRelationshipTraversalDirection
{
    None = 0,
    Outgoing = 1,
    Incoming = 2,
}

public enum BehaviorProgramPrecondition
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
    SelfHasOutgoingRelationship,
    RelatedEntityMatchesTarget,
    RelationshipExists,
    PressureRange,
    CooldownElapsed,
    And,
    Or,
    Not,
    EntityCountAtLeast,
    HasColocatedEntity,
    ContestedInfluence,
    SharedInfluence,
    EndpointRelationshipCountBelow,
}

public enum BehaviorProgramExpression
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

public enum BehaviorProgramEffectScope
{
    SelfAttribute,
    TargetAttribute,
    Resource,
    Structural,
    Relational,
}

public enum BehaviorProgramProviderSelector
{
    TargetAttribute,
    Structural,
    Resource,
    LocationAttribute,
    OutgoingRelationship,
    IncomingRelationship,
}

public sealed record BehaviorProgramEffectRule(
    BehaviorProgramEffectScope Scope,
    int ValueId,
    int KindId);

public sealed record BehaviorProgramProviderSelectorRule(
    BehaviorProgramProviderSelector Selector,
    int KindId,
    int ValueId,
    int RelationshipKindId,
    int FactId,
    float MinimumStrength);

public sealed record BehaviorProgramSupportRule(
    BehaviorProgramEffectScope EffectScope,
    int EffectValueId,
    int EffectKindId,
    float DesiredMinimum,
    float UrgencyScale,
    IReadOnlyList<BehaviorProgramExpressionOp> CurrentValueExpression);

public sealed record BehaviorProgramPreconditionOp(
    BehaviorProgramPrecondition Code,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    float X = 0f,
    float Y = 0f);

public sealed record BehaviorProgramExpressionOp(
    BehaviorProgramExpression Code,
    int A = 0,
    float X = 0f);

public sealed record BehaviorProgramStepRule(
    BehaviorProgramOperation Operation,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    float X = 0f,
    float Y = 0f,
    IReadOnlyList<BehaviorProgramExpressionOp>? Expression = null);
