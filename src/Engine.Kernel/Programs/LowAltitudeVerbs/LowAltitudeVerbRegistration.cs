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
    public float ExploreWeight { get; init; }
    public float StalenessWeight { get; init; }
    public float HotspotWeight { get; init; }
    public float CrowdingWeight { get; init; }
    public float GeneratorAffordanceWeight { get; init; }
    public int ForeignInfluenceFilterEnabled { get; init; }
    public int ForeignInfluenceBlockPositiveRelationships { get; init; }
    public int ForeignInfluenceAllowNegativeRelationships { get; init; }
    public float ForeignInfluenceMinimumRelationshipStrength { get; init; }
    public float ForeignInfluenceAllowUnrelatedInfluenceAtOrAbove { get; init; }
    public int AffinityRelationshipKindId { get; init; }
    public int AffinityRelationshipDirectionCode { get; init; }
    public float RelationshipAffinityWeight { get; init; }
    public float InfluenceDelta { get; init; }
    public int InfluenceRadius { get; init; }
    public float CoverageChance { get; init; }
    public float RelocationChance { get; init; }
    public float RequiredMapValue { get; init; }
    public float RelationshipStrengthDelta { get; init; }
}
