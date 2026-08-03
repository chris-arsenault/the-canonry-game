namespace Engine.Domain.Compilation.KernelPrograms;

using Engine.Constants;

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
    CullEndpointIncomingRelationships,
    ErodeTargetCulture,
    InstallDistrict,
    ReawakenDistrict,
    DemoteStage,
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
    EntityCountAtLeast,
    HasColocatedEntity,
    ContestedInfluence,
    SharedInfluence,
    EndpointRelationshipCountBelow,
    EndpointCultureIs,
    SelfCultureMass,
    SelfWorkTierBelow,
    SelfHasOutgoingRelationshipToKind,
    EndpointSubtypeIs,
    WithinTravelReach,
    SelfHexFactRange,
    TargetHexFactRange,
    SelfOnTerrainGroup,
    CultureHoldsTech,
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
    FixedScalar MinimumStrength);

public sealed record BehaviorProgramPreconditionInstruction(
    BehaviorProgramPrecondition Code,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    FixedScalar X = default,
    FixedScalar Y = default,
    int ActorValueSlot = 0,
    bool RelationshipTargetSource = false,
    int TrueDestination = GambitPreconditionDestinationCodes.Passed,
    int FalseDestination = GambitPreconditionDestinationCodes.Failed);

public sealed record BehaviorProgramActorPreconditionInstruction(
    int ActorValueSlot,
    int TrueDestination = GambitPreconditionDestinationCodes.Passed,
    int FalseDestination = GambitPreconditionDestinationCodes.Failed);

public sealed record BehaviorProgramRelationshipTargetSource(
    int DirectionCode,
    int RelationshipKindId,
    bool HasMinimumStrength,
    FixedScalar MinimumStrength);

public sealed record BehaviorProgramPreconditionPlan(
    IReadOnlyList<BehaviorProgramPreconditionInstruction> CandidateInstructions,
    int CandidateEntry,
    IReadOnlyList<BehaviorProgramActorPreconditionInstruction> ActorInstructions,
    int ActorEntry,
    int ActorValueCount,
    BehaviorProgramRelationshipTargetSource? RelationshipTargetSource);

public sealed record BehaviorProgramExpressionOp(
    BehaviorProgramExpression Code,
    int A = 0,
    FixedScalar X = default);

public sealed record BehaviorProgramStepRule(
    BehaviorProgramOperation Operation,
    int A = 0,
    int B = 0,
    int C = 0,
    int D = 0,
    FixedScalar X = default,
    FixedScalar Y = default,
    IReadOnlyList<BehaviorProgramExpressionOp>? Expression = null);
