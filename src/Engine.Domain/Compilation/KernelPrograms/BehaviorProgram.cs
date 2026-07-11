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
    SetStatus,
    CreateRelationship,
    SetRelationshipStrength,
    AddTargetAttribute,
    TransferAttribute,
    MoveTowardTargetPhysical,
    WanderPhysical,
    MoveTowardTargetSemantic,
    WanderSemantic,
    CreateEntity,
    SetHistorical,
    AddRelatedEntitiesAttribute,
    CalculateVariable,
    AddAttributeFromVariable,
    AddTargetAttributeFromVariable,
    CreateFixedRelationship,
    CreateEndpointRelationship,
    SetEndpointRelationshipStrength,
    RemoveEndpointRelationship,
    MoveTargetToSelfPhysical,
    SetTargetCoverageTerrain,
    BuildTravelStructureLine,
    SetTargetCultureFromSelf,
    IncreaseWorkTier,
    ForgeCellVertical,
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
    Always,
    SelfHasRelationshipToTarget,
    TargetHasRelationshipToSelf,
    SelfHasOutgoingRelationship,
    RelatedEntityMatchesTarget,
    RelationshipExists,
    CooldownElapsed,
    And,
    Or,
    Not,
    EntityCountAtLeast,
    HasColocatedEntity,
    ContestedInfluence,
    SharedInfluence,
    EndpointRelationshipCountBelow,
    EndpointCultureIs,
    SelfCultureMass,
    SelfWorkTierBelow,
}

public enum BehaviorProgramExpression
{
    Literal,
    SelfAttribute,
    TargetAttribute,
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
    SameCulture,
}

public enum BehaviorProgramProviderSelector
{
    TargetAttribute,
    Structural,
    LocationAttribute,
    OutgoingRelationship,
    IncomingRelationship,
}

public sealed record BehaviorProgramProviderSelectorRule(
    BehaviorProgramProviderSelector Selector,
    int KindId,
    int ValueId,
    int RelationshipKindId,
    float MinimumStrength);

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
