namespace Engine.Kernel;

internal readonly struct LowAltitudeVerbRegistration
{
    public int KindId { get; init; }
    public int VerbId { get; init; }
    public int PlaneCode { get; init; }
    public int OperationCode { get; init; }
    public int TargetModeCode { get; init; }
    public int TargetKindId { get; init; }
    public int RelationshipKindId { get; init; }
    public int RelationshipDirectionCode { get; init; }
    public int MinTicks { get; init; }
    public int MaxTicks { get; init; }
    public int DwellTicks { get; init; }
    public int TargetRadius { get; init; }
    public int CandidateSampleCount { get; init; }
    public float Weight { get; init; }
    public float OpportunityBonusOverride { get; init; }
    public float FitnessWeight { get; init; }
    public float DistanceScaleOverride { get; init; }
    public int ForeignInfluenceFilterEnabled { get; init; }
    public int ForeignInfluenceBlockPositiveRelationships { get; init; }
    public int ForeignInfluenceAllowNegativeRelationships { get; init; }
    public float ForeignInfluenceMinimumRelationshipStrength { get; init; }
    public float ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove { get; init; }
    public int FitnessRelationshipKindId { get; init; }
    public int FitnessRelationshipDirectionCode { get; init; }
    public int FitnessPolarityCode { get; init; }
    public float FitnessThreshold { get; init; }
    public float InfluenceDelta { get; init; }
    public int InfluenceRadius { get; init; }
    public float CoverageChance { get; init; }
    public float RelocationChance { get; init; }
    public float RequiredMapValue { get; init; }
    public float RelationshipStrengthDelta { get; init; }
}
