namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class LowAltitudeVerbProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<LowAltitudeVerbRule> Verbs { get; init; } = [];
}

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
    int AffinityRelationshipKindId,
    int AffinityRelationshipDirectionCode,
    float RelationshipAffinityWeight,
    float InfluenceDelta,
    int InfluenceRadius,
    float CoverageGrowthChance,
    float HeadquartersMoveChance,
    float RequiredInfluence);
