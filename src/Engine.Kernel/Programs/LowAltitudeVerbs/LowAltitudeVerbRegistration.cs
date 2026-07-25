namespace Engine.Kernel;

internal readonly struct LowAltitudeVerbRegistration
{
    public int KindId { get; init; }
    public int VerbId { get; init; }
    public int PlaneCode { get; init; }
    public int OperationCode { get; init; }
    public int InfluenceTargetingCode { get; init; }
    public int TargetModeCode { get; init; }
    public int TargetKindId { get; init; }
    public int RelationshipKindId { get; init; }
    public int RelationshipDirectionCode { get; init; }
    public int MinTicks { get; init; }
    public int MaxTicks { get; init; }
    public int DwellTicks { get; init; }
    public int TargetRadius { get; init; }
    public int CandidateSampleCount { get; init; }
    public FixedScalar Weight { get; init; }
    public FixedScalar OpportunityBonusOverride { get; init; }
    public FixedScalar FitnessWeight { get; init; }
    public FixedScalar DistanceScaleOverride { get; init; }
    public int ForeignInfluenceFilterEnabled { get; init; }
    public int ForeignInfluenceBlockPositiveRelationships { get; init; }
    public int ForeignInfluenceAllowNegativeRelationships { get; init; }
    public FixedScalar ForeignInfluenceMinimumRelationshipStrength { get; init; }
    public FixedScalar ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove { get; init; }
    public int FitnessRelationshipKindId { get; init; }
    public int FitnessRelationshipDirectionCode { get; init; }
    public int FitnessPolarityCode { get; init; }
    public FixedScalar FitnessThreshold { get; init; }
    public FixedScalar InfluenceDelta { get; init; }
    public int InfluenceRadius { get; init; }
    public FixedScalar CoverageChance { get; init; }
    public FixedScalar RelocationChance { get; init; }
    public FixedScalar RequiredMapValue { get; init; }
    public FixedScalar RelationshipStrengthDelta { get; init; }
}
