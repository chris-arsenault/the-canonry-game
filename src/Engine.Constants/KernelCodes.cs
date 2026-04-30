namespace Engine.Constants;

public static class EntityStatusCodes
{
    public const int Active = 1;
    public const int Historical = 2;
}

public static class PlaneCodes
{
    public const int None = 0;
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
    public const int MoveTowardTargetPhysical = 13;
    public const int WanderPhysical = 14;
    public const int MoveTowardTargetSemantic = 15;
    public const int WanderSemantic = 16;
    public const int CreateEntity = 17;
    public const int AddPressure = 18;
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

public static class RelationshipPolarityCodes
{
    public const int Positive = 0;
    public const int Negative = 1;
    public const int Neutral = 2;
}

public static class RelationshipMaintenanceConditionCodes
{
    public const int Always = 1;
    public const int SameCulture = 2;
    public const int DifferentCulture = 3;
    public const int PhysicalDistanceWithin = 4;
    public const int SemanticDistanceWithin = 5;
    public const int PhysicalCoPresence = 6;
    public const int SemanticCoPresence = 7;
    public const int SourceInfluenceAtTarget = 8;
    public const int TargetInfluenceAtSource = 9;
    public const int ContactMemory = 10;
}

public static class ProcessEffectScopeCodes
{
    public const int SelfAttribute = 1;
    public const int TargetAttribute = 2;
    public const int Resource = 3;
    public const int Structural = 4;
    public const int Relational = 5;
}

public static class ProcessObjectiveUrgencyCodes
{
    public const int StandardNeed = 1;
    public const int Baseline = 2;
    public const int RangeDefault = 3;
    public const int Fixed = 4;
}

public static class ProcessProviderSelectorCodes
{
    public const int TargetAttribute = 1;
    public const int Structural = 2;
    public const int Resource = 3;
    public const int LocationAttribute = 4;
    public const int OutgoingRelationship = 5;
    public const int IncomingRelationship = 6;
}

public static class ProcessWorkCommitmentCodes
{
    public const int Off = 1;
    public const int Pilot = 2;
}

public static class UpkeepAgingModelCodes
{
    public const int Linear = 1;
    public const int LateLife = 2;
}

public static class EffectConditionCodes
{
    public const int RelationshipMapValueAtLocation = 1;
    public const int RelatedEntityDistance = 2;
    public const int LocalPositiveSignal = 3;
    public const int LocalNegativeSignal = 4;
    public const int LocalPressureSignal = 5;
    public const int LocalComfortSignal = 6;
}

public static class RelationshipMatchDirectionCodes
{
    public const int Outgoing = 1;
    public const int Incoming = 2;
    public const int Either = 3;
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
    public const int InCoverageLocality = 4;
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
    public const int Coverage = 1;
    public const int Entity = 2;
}

public static class DegeneracyMetricCodes
{
    public const int AttributeDeclineRatio = 1;
    public const int RelationshipEndpointDominanceRatio = 2;
    public const int ActiveKindCount = 3;
}

public static class DegeneracyThresholdDirectionCodes
{
    public const int HighIsBad = 1;
    public const int LowIsBad = 2;
}

public static class DegeneracyRelationshipEndpointCodes
{
    public const int Source = 1;
    public const int Target = 2;
}

public static class DegeneracySeverityCodes
{
    public const int Normal = 0;
    public const int Warning = 1;
    public const int Critical = 2;
}

public static class KernelProgramCodes
{
    public const int Upkeep = 1;
    public const int Processes = 2;
    public const int Relationships = 3;
    public const int Pressures = 4;
    public const int Generators = 5;
    public const int Degeneracy = 6;
    public const int Gambits = 7;
    public const int Planning = 8;
    public const int MapActivity = 10;
    public const int LowAltitudeVerbs = 11;
    public const int Effects = 12;
    public const int MapSignals = 13;
}

public static class ProgramDependencyLagCodes
{
    public const int SameTick = 1;
    public const int PreviousTick = 2;
}

public static class ProgramDependencyResourceCodes
{
    public const int MapActivitySignals = 1;
    public const int MapSignals = 2;
}

public static class TargetSelectorCodes
{
    public const int Explore = 1;
    public const int EntityKind = 2;
    public const int RelationshipTarget = 3;
    public const int Terrain = 4;
    public const int SelfMapValue = 5;
    public const int OtherMapValue = 6;
}

public static class ProgramOpcodeCodes
{
    public const int MoveActor = 1;
    public const int AddOwnedCellScalar = 2;
    public const int SubtractOtherCellScalar = 3;
    public const int TryClaimCell = 4;
    public const int TryRelocateEntity = 5;
}

public static class LowAltitudeVerbRelationshipDirectionCodes
{
    public const int Outgoing = 1;
    public const int Incoming = 2;
    public const int Either = 3;
}

public static class ProgramEnabledOverrideCodes
{
    public const int Unspecified = -1;
    public const int Disabled = 0;
    public const int Enabled = 1;
}

public static class PlannerKindCodes
{
    public const int Goap = 1;
    public const int Htn = 2;
    public const int Opportunity = 3;
}

public static class ActionFamilyCodes
{
    public const int Process = 1;
    public const int Gambit = 2;
    public const int LowAltitudeVerb = 3;
}

public static class PlanningStatusCodes
{
    public const int None = 0;
    public const int Succeeded = 1;
    public const int NoCandidates = 2;
    public const int NoWeight = 3;
    public const int UnsupportedPlanner = 4;
    public const int UnsupportedActionFamily = 5;
}

public static class PlanningStepRoleCodes
{
    public const int Immediate = 1;
    public const int Root = 2;
    public const int Blocker = 3;
    public const int Support = 4;
    public const int Decomposition = 5;
}

public static class TraceSubjectCodes
{
    public const int Goal = 1;
    public const int ActionSummary = 2;
    public const int TargetDetail = 3;
    public const int Support = 4;
    public const int Locality = 5;
    public const int MovementIntent = 6;
    public const int InfluenceIntent = 7;
}

public static class TraceStatusCodes
{
    public const int None = 0;
    public const int Offered = 1;
    public const int BelowUrgency = 2;
    public const int OnCooldown = 3;
    public const int Disabled = 4;
    public const int NoTarget = 5;
    public const int NoReward = 6;
    public const int PreconditionFailed = 7;
    public const int Actionable = 8;
    public const int SupportSelected = 9;
    public const int Selected = 10;
    public const int CommittedByHysteresis = 11;
    public const int CapacityReached = 12;
}

public static class KernelCommandTypeCodes
{
    public const int None = 0;
    public const int NoOp = 1;
    public const int SetGeneratorEnabled = 2;
}

public static class KernelCommandRejectReasonCodes
{
    public const int None = 0;
    public const int StaleVersion = 1;
    public const int UnsupportedCommand = 2;
    public const int InvalidCommand = 3;
}

public static class GeneratorProgramConstants
{
    public const int TargetReferenceCode = 1;
    public const int DefaultCadenceTicks = 25;
}
