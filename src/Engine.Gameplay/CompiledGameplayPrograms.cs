namespace Engine.Gameplay;

public sealed class LowAltitudeVerbProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<LowAltitudeVerbRule> Verbs { get; init; } = [];
    public IReadOnlyList<CompiledInitialInfluenceRule> InitialInfluence { get; init; } = [];
    public IReadOnlyList<CompiledAffordanceRule> Affordances { get; init; } = [];
    public IReadOnlyList<CompiledTraitLowAltitudeWeightModifierRule> TraitWeightModifiers { get; init; } = [];
}

public sealed record LowAltitudeVerbRule(
    int KindId,
    int VerbId,
    int PlaneCode,
    int OperationCode,
    int InfluenceTargetingCode,
    int TargetModeCode,
    int TargetKindId,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    IReadOnlyList<int> TerrainIds,
    int MinCommitmentTicks,
    int MaxCommitmentTicks,
    int DwellTicks,
    int TargetRadius,
    int CandidateSampleCount,
    float Weight,
    float OpportunityBonusOverride,
    float FitnessWeight,
    float DistanceScaleOverride,
    bool ForeignInfluenceFilterEnabled,
    bool ForeignInfluenceBlockPositiveRelationships,
    bool ForeignInfluenceAllowNegativeRelationships,
    float ForeignInfluenceMinimumRelationshipStrength,
    float ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove,
    int FitnessRelationshipKindId,
    int FitnessRelationshipDirectionCode,
    int FitnessPolarityCode,
    float FitnessThreshold,
    float InfluenceDelta,
    int InfluenceRadius,
    float CoverageGrowthChance,
    float HeadquartersMoveChance,
    float RequiredInfluence,
    float RelationshipStrengthDelta,
    IReadOnlyList<CompiledProgramStep> PrimitiveSteps);

public sealed record CompiledProgramStep(
    int Opcode,
    int IntArgument,
    float FloatArgument);

public sealed record CompiledInitialInfluenceRule(
    int KindId,
    int PlaneCode,
    float InfluenceValue);

public sealed record CompiledAffordanceRule(
    int SourceKindCode,
    int SourceId,
    int ShapeCode,
    int ActorKindId,
    int PlaneCode,
    int CandidateKindId,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    int OperationCode,
    int PolarityCode,
    float Threshold,
    float Weight);

public sealed record CompiledTraitLowAltitudeWeightModifierRule(
    int KindId,
    int TraitId,
    int VerbId,
    float WeightDelta,
    float OpportunityBonusDelta,
    float FitnessWeightDelta);

public sealed class MapActivityProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public float PresenceHeat { get; init; } = 1f;
    public float HeatDecayPerTick { get; init; } = 0.99f;
    public float HeatEmaAlpha { get; init; } = 0.01f;
}

public sealed class MapSignalsProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public float SemanticBaseCapacity { get; init; } = 2f;
    public int CrowdingRadius { get; init; } = 1;
    public IReadOnlyList<TerrainMapSignalRule> Terrains { get; init; } = [];
    public IReadOnlyList<KindMapSignalRule> Kinds { get; init; } = [];
    public IReadOnlyList<RelationshipMapSignalRule> Relationships { get; init; } = [];
}

public sealed record TerrainMapSignalRule(int TerrainId, float BaseCapacity);

public sealed record KindMapSignalRule(
    int KindId,
    int SignalRadius,
    float CapacityEmission,
    float SupportEmission,
    float ThreatEmission,
    float BaseAttractorMass,
    float CoverageAttractorScale,
    float InfluenceAttractorScale,
    float ActivityAttractorScale,
    int AttractorRadius,
    IReadOnlyList<MapSignalScalarInputRule> AttractorScalarInputs);

public sealed record MapSignalScalarInputRule(
    int AttributeId,
    float AttractorScale);

public sealed record RelationshipMapSignalRule(
    int RelationshipKindId,
    int SignalRadius,
    float SupportEmission,
    float ThreatEmission);

public sealed class EffectsProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<AttributeDeltaEffectRule> AttributeDeltaEffects { get; init; } = [];
    public IReadOnlyList<RelationshipTagRule> RelationshipTagRules { get; init; } = [];
}

public sealed record RelationshipTagRule(
    int KindId,
    int TagId,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    float SetAtOrAbove,
    float ClearBelow);

public sealed record AttributeDeltaEffectRule(
    int KindId,
    int AttributeId,
    int ConditionCode,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    int PlaneCode,
    int MaxDistance,
    float Threshold,
    float MinimumRelationshipStrength,
    float DeltaPerTick,
    int RequiredTargetTraitId,
    int RequiredRelatedTraitId,
    int CultureId = 0,
    int CultureSlotId = 0,
    int CultureSlotPolarityCode = -1);
