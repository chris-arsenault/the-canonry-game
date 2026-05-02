namespace Engine.Gameplay;

public sealed class LowAltitudeVerbProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public CompiledSemanticPlaneBounds SemanticBounds { get; init; } = new(-24, 24, -24, 24);
    public IReadOnlyList<LowAltitudeVerbRule> Verbs { get; init; } = [];
    public IReadOnlyList<CompiledInitialInfluenceRule> InitialInfluence { get; init; } = [];
    public IReadOnlyList<CompiledGeneratorAffordanceRule> GeneratorAffordances { get; init; } = [];
    public IReadOnlyList<CompiledTraitLowAltitudeWeightModifierRule> TraitWeightModifiers { get; init; } = [];
}

public sealed record CompiledSemanticPlaneBounds(
    int MinQ,
    int MaxQ,
    int MinR,
    int MaxR);

public sealed record LowAltitudeVerbRule(
    int KindId,
    int VerbId,
    int PlaneCode,
    int OperationCode,
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
    float ExploreWeight,
    float StalenessWeight,
    float HotspotWeight,
    float CrowdingWeight,
    float GeneratorAffordanceWeight,
    bool ForeignInfluenceFilterEnabled,
    bool ForeignInfluenceBlockPositiveRelationships,
    bool ForeignInfluenceAllowNegativeRelationships,
    float ForeignInfluenceMinimumRelationshipStrength,
    float ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove,
    int AffinityRelationshipKindId,
    int AffinityRelationshipDirectionCode,
    float RelationshipAffinityWeight,
    float InfluenceDelta,
    int InfluenceRadius,
    float CoverageGrowthChance,
    float HeadquartersMoveChance,
    float RequiredInfluence,
    IReadOnlyList<CompiledProgramStep> PrimitiveSteps);

public sealed record CompiledProgramStep(
    int Opcode,
    int IntArgument,
    float FloatArgument);

public sealed record CompiledInitialInfluenceRule(
    int KindId,
    int PlaneCode,
    float InfluenceValue);

public sealed record CompiledGeneratorAffordanceRule(
    int ActorKindId,
    int CandidateKindId,
    int BindingKindId,
    float AffordanceScore);

public sealed record CompiledTraitLowAltitudeWeightModifierRule(
    int KindId,
    int TraitId,
    int VerbId,
    float WeightDelta,
    float ExploreWeightDelta,
    float StalenessWeightDelta,
    float HotspotWeightDelta,
    float CrowdingWeightDelta,
    float GeneratorAffordanceWeightDelta,
    float RelationshipAffinityWeightDelta);

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
}

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
    int RequiredRelatedTraitId);
