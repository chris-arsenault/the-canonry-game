namespace Engine.Gameplay;

using Engine.Constants;

public sealed class LowAltitudeVerbProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<LowAltitudeVerbRule> Verbs { get; init; } = [];
    public IReadOnlyList<CompiledInitialInfluenceRule> InitialInfluence { get; init; } = [];
    public IReadOnlyList<CompiledDistrictBonusRule> DistrictBonuses { get; init; } = [];
    public IReadOnlyList<CompiledStageRule> StageRules { get; init; } = [];
    public IReadOnlyList<CompiledAffordanceRule> Affordances { get; init; } = [];
    public IReadOnlyList<CompiledTraitLowAltitudeWeightModifierRule> TraitWeightModifiers { get; init; } = [];
}

public sealed record CompiledStageRule(
    int KindId,
    int StageIndex,
    FixedScalar BandMultiplier,
    int WorkerBonus,
    FixedScalar AmbientEmission,
    int DistrictCapacity);

public sealed record CompiledDistrictBonusRule(
    int SubtypeId,
    int BonusKindCode,
    FixedScalar Magnitude);

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
    FixedScalar Weight,
    FixedScalar OpportunityBonusOverride,
    FixedScalar FitnessWeight,
    FixedScalar DistanceScaleOverride,
    bool ForeignInfluenceFilterEnabled,
    bool ForeignInfluenceBlockPositiveRelationships,
    bool ForeignInfluenceAllowNegativeRelationships,
    FixedScalar ForeignInfluenceMinimumRelationshipStrength,
    FixedScalar ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove,
    int FitnessRelationshipKindId,
    int FitnessRelationshipDirectionCode,
    int FitnessPolarityCode,
    FixedScalar FitnessThreshold,
    FixedScalar InfluenceDelta,
    int InfluenceRadius,
    FixedScalar CoverageGrowthChance,
    FixedScalar HeadquartersMoveChance,
    FixedScalar RequiredInfluence,
    FixedScalar RelationshipStrengthDelta,
    IReadOnlyList<CompiledProgramStep> PrimitiveSteps,
    int MinTemperature = 0,
    int SiteFlags = 0,
    bool RequireFogAdjacent = false,
    bool RestAtDestination = false);

public sealed record CompiledProgramStep(
    int Opcode,
    int IntArgument,
    FixedScalar ScalarArgument);

public sealed record CompiledInitialInfluenceRule(
    int KindId,
    int PlaneCode,
    FixedScalar InfluenceValue);

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
    FixedScalar Threshold,
    FixedScalar Weight);

public sealed record CompiledTraitLowAltitudeWeightModifierRule(
    int KindId,
    int TraitId,
    int VerbId,
    FixedScalar WeightDelta,
    FixedScalar OpportunityBonusDelta,
    FixedScalar FitnessWeightDelta);

public sealed class LocalityRules
{
    public FixedScalar SemanticBaseCapacity { get; init; } = FixedScalar.FromInt(2);
    public int CrowdingRadius { get; init; } = 1;
    public IReadOnlyList<TerrainLocalityRule> Terrains { get; init; } = [];
    public CompiledPhysicalCapacityFormula? PhysicalCapacityFormula { get; init; }
    public IReadOnlyList<KindLocalityRule> Kinds { get; init; } = [];
    public IReadOnlyList<RelationshipLocalityRule> Relationships { get; init; } = [];
}

public sealed record TerrainLocalityRule(int TerrainId, FixedScalar BaseCapacity);

/// <summary>Fact-derived physical capacity, temperatures quantized to byte scale.</summary>
public sealed record CompiledPhysicalCapacityFormula(
    FixedScalar ColdFloor,
    FixedScalar WarmPeak,
    FixedScalar GeothermalBlend,
    FixedScalar FreshwaterWeight,
    FixedScalar FertileWeight,
    FixedScalar ReliefComfort,
    FixedScalar ReliefPenalty,
    FixedScalar RoughnessPenalty,
    FixedScalar LandScale,
    FixedScalar MarineScale);

public sealed record KindLocalityRule(
    int KindId,
    int LocalityRadius,
    FixedScalar CapacityEmission,
    FixedScalar SupportEmission,
    FixedScalar ThreatEmission,
    FixedScalar BaseAttractorMass,
    FixedScalar CoverageAttractorScale,
    FixedScalar InfluenceAttractorScale,
    int AttractorRadius,
    IReadOnlyList<LocalityScalarInputRule> AttractorScalarInputs);

public sealed record LocalityScalarInputRule(
    int AttributeId,
    FixedScalar AttractorScale);

public sealed record RelationshipLocalityRule(
    int RelationshipKindId,
    FixedScalar SupportEmission,
    FixedScalar ThreatEmission);

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
    FixedScalar SetAtOrAbove,
    FixedScalar ClearBelow,
    int InsideCoverageOwnerKindId = 0,
    int InsideCoverageOwnerSubtypeId = 0,
    int InsideCoveragePlaneCode = PlaneCodes.Semantic);

public sealed record AttributeDeltaEffectRule(
    int KindId,
    int AttributeId,
    int ConditionCode,
    int RelationshipKindId,
    int RelationshipDirectionCode,
    int PlaneCode,
    int MaxDistance,
    FixedScalar Threshold,
    FixedScalar MinimumRelationshipStrength,
    FixedScalar DeltaPerTick,
    int RequiredTargetTraitId,
    int RequiredRelatedTraitId,
    int CultureId = 0,
    int CultureSlotId = 0,
    int CultureSlotPolarityCode = -1,
    FixedScalar SupportScale = default,
    int CoverageOwnerKindId = 0,
    int CoverageOwnerSubtypeId = 0);
