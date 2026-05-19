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

public static class BehaviorOperationCodes
{
    public const int AddAttribute = 1;
    public const int SetAttribute = 2;
    public const int MovePhysical = 3;
    public const int MoveSemantic = 4;
    public const int SetStatus = 6;
    public const int CreateRelationship = 7;
    public const int SetRelationshipStrength = 8;
    public const int AddTargetAttribute = 9;
    public const int TransferAttribute = 10;
    public const int MoveTowardTargetPhysical = 13;
    public const int WanderPhysical = 14;
    public const int MoveTowardTargetSemantic = 15;
    public const int WanderSemantic = 16;
    public const int CreateEntity = 17;
    public const int SetHistorical = 20;
    public const int AddRelatedEntitiesAttribute = 21;
    public const int CalculateVariable = 22;
    public const int AddAttributeFromVariable = 23;
    public const int AddTargetAttributeFromVariable = 24;
    public const int CreateFixedRelationship = 25;
    public const int CreateEndpointRelationship = 26;
    public const int SetEndpointRelationshipStrength = 27;
    public const int MoveTargetToSelfPhysical = 28;
    public const int SetTargetCoverageTerrain = 29;
    public const int RemoveEndpointRelationship = 30;
    public const int BuildTravelStructureLine = 31;
    public const int SetTargetCultureFromSelf = 32;
}

public static class BehaviorTargetModeCodes
{
    public const int Self = 1;
    public const int AnyOther = 2;
    public const int KindOther = 3;
}

public static class BehaviorEndpointCodes
{
    public const int Self = 1;
    public const int Target = 2;
}

public static class BehaviorPreconditionCodes
{
    public const int TargetIsSelf = 1;
    public const int TargetIsNotSelf = 2;
    public const int SelfAttributeRange = 3;
    public const int TargetAttributeRange = 4;
    public const int PlaneDistance = 5;
    public const int Always = 7;
    public const int SelfHasRelationshipToTarget = 8;
    public const int TargetHasRelationshipToSelf = 9;
    public const int RelatedEntityMatchesTarget = 10;
    public const int RelationshipExists = 11;
    public const int CooldownElapsed = 13;
    public const int And = 14;
    public const int Or = 15;
    public const int Not = 16;
    public const int EntityCountAtLeast = 17;
    public const int HasColocatedEntity = 18;
    public const int ContestedInfluence = 19;
    public const int SelfHasOutgoingRelationship = 20;
    public const int SharedInfluence = 21;
    public const int EndpointRelationshipCountBelow = 22;
    public const int EndpointCultureIs = 23;
}

public static class BehaviorExpressionCodes
{
    public const int Literal = 1;
    public const int SelfAttribute = 2;
    public const int TargetAttribute = 3;
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
    public const int SameCulture = 16;
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

public static class BehaviorEffectScopeCodes
{
    public const int SelfAttribute = 1;
    public const int TargetAttribute = 2;
    public const int Structural = 4;
    public const int Relational = 5;
}

public static class BehaviorProviderSelectorCodes
{
    public const int TargetAttribute = 1;
    public const int Structural = 2;
    public const int LocationAttribute = 4;
    public const int OutgoingRelationship = 5;
    public const int IncomingRelationship = 6;
}

public static class AffordanceSourceKindCodes
{
    public const int None = 0;
    public const int Generator = 1;
    public const int Gambit = 2;
}

public static class LowAltitudeAffordanceShapeCodes
{
    public const int PresenceNearEntity = 1;
    public const int RelationshipStrengthChange = 3;
    public const int InfluenceChange = 4;
}

public static class AffordancePolarityCodes
{
    public const int None = 0;
    public const int Strengthen = 1;
    public const int Weaken = 2;
    public const int Increase = 3;
    public const int Reduce = 4;
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
    public const int LocalCrowdingLoadSignal = 5;
    public const int LocalComfortSignal = 6;
    public const int LocalCultureMapValue = 7;
    public const int CultureSlotOccupancy = 8;
}

public static class RelationshipMatchDirectionCodes
{
    public const int Outgoing = 1;
    public const int Incoming = 2;
    public const int Either = 3;
}

public static class TraitEffectBearerCodes
{
    public const int Target = 1;
    public const int RelatedEntity = 2;
    public const int Actor = 3;
    public const int GambitTarget = 4;
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

public static class GeneratorPlacementCodes
{
    public const int Area = 1;
    public const int Locality = 2;
}

public static class GeneratorCultureMatchCodes
{
    public const int Any = 1;
    public const int SameAsTarget = 2;
    public const int DifferentFromTarget = 3;
}

public static class GeneratorCulturePlacementCodes
{
    public const int Ignore = 1;
    public const int PreferInheritedCulture = 2;
}

public static class GeneratorCoverageModeCodes
{
    public const int None = 0;
    public const int CreateNew = 1;
    public const int SplitFromExisting = 2;
}

public static class KernelProgramCodes
{
    public const int Upkeep = 1;
    public const int Relationships = 3;
    public const int Generators = 5;
    public const int Gambits = 7;
    public const int Movement = 9;
    public const int MapActivity = 10;
    public const int LowAltitudeVerbs = 11;
    public const int Effects = 12;
    public const int MapSignals = 13;
    public const int CultureSlots = 14;
}

public static class CultureSlotScoreSignalCodes
{
    public const int ActiveEntity = 1;
    public const int PhysicalInfluence = 2;
    public const int SemanticInfluence = 3;
    public const int PhysicalCoverage = 4;
    public const int SemanticCoverage = 5;
    public const int OutgoingRelationshipStrength = 6;
    public const int IncomingRelationshipStrength = 7;
}

public static class MovementCauseCodes
{
    public const int Travel = 1;
    public const int Placement = 2;
    public const int Forced = 3;
    public const int Relocation = 4;
    public const int Follow = 5;
}

public static class ProgramDependencyLagCodes
{
    public const int SameTick = 1;
    public const int PreviousTick = 2;
}

public static class ProgramDependencyChannelCodes
{
    public const int MapActivitySignals = 1;
    public const int MapSignals = 2;
    public const int MovementSurface = 3;
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
    public const int AdjustRelationshipStrength = 6;
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

public static class ActionFamilyCodes
{
    public const int Gambit = 1;
    public const int LowAltitudeVerb = 2;
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
    public const int RelationshipIntent = 8;
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

public static class LowAltitudeVerbSelectionBucketCodes
{
    public const int None = 0;
    public const int Routine = 1;
    public const int Opportunity = 2;
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
    public const int DefaultPulseWidthTicks = 25;
    public const int DefaultFiresPerPulse = 1;
    public const int DefaultPlacementMinRadius = 0;
    public const int DefaultPlacementMaxRadius = 5;
    public const int FarPlacementMinRadius = 20;
    public const int UnboundedPlacementMaxRadius = int.MaxValue;
}

public static class GeneratorEffectTypeCodes
{
    public const int SetBindingHistorical = 1;
    public const int RemoveTargetRelationships = 2;
    public const int AdjustTargetRelationshipStrength = 3;
    public const int SetTerrainPatch = 4;
}

public static class GeneratorRelationshipInstanceSelectionCodes
{
    public const int AllTargetRelationships = 1;
    public const int TriggerRelationship = 2;
}

public static class TerrainKindProfileFieldCodes
{
    public const int InfluenceMultiplier = 1 << 0;
    public const int ClaimCostMultiplier = 1 << 1;
    public const int ClaimChanceMultiplier = 1 << 2;
}

public static class ProvenanceProducerKindCodes
{
    public const int Generator = 1;
    public const int Gambit = 2;
    public const int Action = 3;
    public const int System = 4;
}

public static class ProvenanceTriggerKindCodes
{
    public const int None = 0;
    public const int Entity = 1;
    public const int Relationship = 2;
    public const int Invocation = 3;
    public const int Attribute = 4;
}

public static class ProvenanceOutputKindCodes
{
    public const int EntityCreated = 1;
    public const int EntityStatusChanged = 2;
    public const int EntityMoved = 3;
    public const int AttributeChanged = 4;
    public const int TraitAssigned = 5;
    public const int EntityCultureChanged = 6;
    public const int RelationshipChanged = 7;
    public const int CellChanged = 8;
    public const int CellInfluenceChanged = 9;
}

public static class KernelCapacityCodes
{
    public const int ProvenanceInvocation = 1;
    public const int ProvenanceOutput = 2;
    public const int GeneratorTargetTerrainRequirement = 3;
}

public static class KernelGrowableStorageModeCodes
{
    public const int Fixed = 0;
    public const int Enabled = 1;
}

public static class KernelStorageCodes
{
    public const int ProvenanceInvocation = KernelCapacityCodes.ProvenanceInvocation;
    public const int ProvenanceOutput = KernelCapacityCodes.ProvenanceOutput;
}

public static class KernelStorageGrowthReasonCodes
{
    public const int Threshold = 1;
    public const int Reserve = 2;
}
