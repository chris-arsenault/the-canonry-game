namespace Engine.Kernel;

public static class TickPhaseCodes
{
    public const int Low = 1;
    public const int Process = 2;
    public const int Pressure = 3;
    public const int Generator = 4;
    public const int Event = 5;
}

public static class EntityStatusCodes
{
    public const int Active = 1;
    public const int Historical = 2;
}

public static class PlaneCodes
{
    public const int Physical = 1;
    public const int Semantic = 2;
}

public static class ProcessOperationCodes
{
    public const int AddAttribute = 1;
    public const int SetAttribute = 2;
    public const int MovePhysical = 3;
    public const int MoveSemantic = 4;
    public const int AddResource = 5;
    public const int SetStatus = 6;
    public const int CreateRelationship = 7;
    public const int SetRelationshipStrength = 8;
    public const int AddTargetAttribute = 9;
    public const int TransferAttribute = 10;
    public const int AddLocationResource = 11;
    public const int HexToHealth = 12;
    public const int MoveTowardTargetPhysical = 13;
    public const int WanderPhysical = 14;
    public const int MoveTowardTargetSemantic = 15;
    public const int WanderSemantic = 16;
    public const int CreateEntity = 17;
    public const int AddPressure = 18;
    public const int SetEntityTag = 19;
    public const int SetHistorical = 20;
    public const int AddRelatedEntitiesAttribute = 21;
    public const int CalculateVariable = 22;
    public const int AddAttributeFromVariable = 23;
    public const int AddTargetAttributeFromVariable = 24;
    public const int CreateFixedRelationship = 25;
}

public static class ProcessTargetModeCodes
{
    public const int Self = 1;
    public const int AnyOther = 2;
    public const int KindOther = 3;
}

public static class ProcessEndpointCodes
{
    public const int Self = 1;
    public const int Target = 2;
}

public static class ProcessPreconditionCodes
{
    public const int TargetIsSelf = 1;
    public const int TargetIsNotSelf = 2;
    public const int SelfAttributeRange = 3;
    public const int TargetAttributeRange = 4;
    public const int PlaneDistance = 5;
    public const int LocationResourceRange = 6;
    public const int Always = 7;
    public const int SelfHasRelationshipToTarget = 8;
    public const int TargetHasRelationshipToSelf = 9;
    public const int RelatedEntityMatchesTarget = 10;
    public const int RelationshipExists = 11;
    public const int PressureRange = 12;
    public const int CooldownElapsed = 13;
    public const int And = 14;
    public const int Or = 15;
    public const int Not = 16;
    public const int EntityCountAtLeast = 17;
    public const int HasColocatedEntity = 18;
}

public static class ProcessExpressionCodes
{
    public const int Literal = 1;
    public const int SelfAttribute = 2;
    public const int TargetAttribute = 3;
    public const int SelfLocationResource = 4;
    public const int TargetLocationResource = 5;
    public const int Add = 6;
    public const int Subtract = 7;
    public const int Multiply = 8;
    public const int Divide = 9;
    public const int Min = 10;
    public const int Max = 11;
    public const int Clamp = 12;
    public const int Abs = 13;
    public const int Variable = 14;
    public const int Distance = 15;
}

public static class RelationshipTraversalDirectionCodes
{
    public const int Outgoing = 1;
    public const int Incoming = 2;
}

public static class PlannerEffectScopeCodes
{
    public const int SelfAttribute = 1;
    public const int TargetAttribute = 2;
    public const int Resource = 3;
    public const int Structural = 4;
    public const int Relational = 5;
}

public static class PlannerDemandCodes
{
    public const int StandardNeed = 1;
    public const int Baseline = 2;
    public const int RangeDefault = 3;
    public const int Fixed = 4;
}

public static class PlannerProviderSelectorCodes
{
    public const int TargetAttribute = 1;
    public const int Structural = 2;
    public const int Resource = 3;
    public const int LocationAttribute = 4;
    public const int OutgoingRelationship = 5;
    public const int IncomingRelationship = 6;
}

public static class GeneratorStatusCodes
{
    public const int Eligible = 1;
    public const int Fired = 2;
    public const int EligibleNotSelected = 3;
    public const int Blocked = 4;
    public const int FireFailed = 5;
    public const int MissingDefinition = 6;
    public const int MaxRuns = 7;
    public const int ZeroWeight = 8;
    public const int NotEvaluated = 9;
}

public static class GeneratorSaturationDirectionCodes
{
    public const int OutgoingFromTarget = 1;
    public const int IncomingToTarget = 2;
    public const int Symmetric = 3;
}

public static class GeneratorPlacementCodes
{
    public const int On = 1;
    public const int Near = 2;
    public const int Across = 3;
    public const int InRegionLocality = 4;
}

public static class GeneratorCultureMatchCodes
{
    public const int Any = 1;
    public const int SameAsTarget = 2;
    public const int DifferentFromTarget = 3;
}

public static class GeneratorTriggerDirectionCodes
{
    public const int High = 1;
    public const int Low = 2;
    public const int Critical = 3;
    public const int Any = 4;
}

public static class GeneratorCoverageModeCodes
{
    public const int None = 0;
    public const int CreateNew = 1;
    public const int SplitFromExisting = 2;
}

public static class PressureScopeCodes
{
    public const int Regional = 1;
    public const int Entity = 2;
}

public static class DegeneracyOperationCodes
{
    public const int DecliningAttributeRatio = 1;
    public const int RelationshipDominanceRatio = 2;
    public const int ActiveKindCountBelow = 3;
}

public static class DegeneracySeverityCodes
{
    public const int Normal = 0;
    public const int Warning = 1;
    public const int Critical = 2;
}
