namespace Engine.Gameplay;

/// <summary>
/// Low-altitude intent. A verb chooses a mid-term target, commits for a short
/// duration, and produces movement or analogous map influence.
/// </summary>
public sealed class LowAltitudeVerbConfig
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required string Kind { get; init; }
    public string? Plane { get; init; }
    public LowAltitudeVerbOperation Operation { get; init; } = LowAltitudeVerbOperation.Move;
    public required LowAltitudeVerbTargetMode TargetMode { get; init; }
    public string? TargetKind { get; init; }
    public string? RelationshipKind { get; init; }
    public LowAltitudeVerbRelationshipDirection RelationshipDirection { get; init; } =
        LowAltitudeVerbRelationshipDirection.Either;
    public IReadOnlySet<string>? Terrains { get; init; }
    public int MinCommitmentTicks { get; init; } = 30;
    public int MaxCommitmentTicks { get; init; } = 90;
    public int DwellTicks { get; init; } = 8;
    public int TargetRadius { get; init; }
    public int CandidateSampleCount { get; init; } = 16;
    public float Weight { get; init; } = 1f;
    public float ExploreWeight { get; init; } = 1f;
    public float StalenessWeight { get; init; } = 1f;
    public float HotspotWeight { get; init; } = 0.25f;
    public float CrowdingWeight { get; init; } = 0.5f;
    public float GeneratorAffordanceWeight { get; init; } = 0.5f;
    public string? AffinityRelationshipKind { get; init; }
    public LowAltitudeVerbRelationshipDirection AffinityRelationshipDirection { get; init; } =
        LowAltitudeVerbRelationshipDirection.Either;
    public float RelationshipAffinityWeight { get; init; }
    public float InfluenceDelta { get; init; }
    public int InfluenceRadius { get; init; }
    public float CoverageGrowthChance { get; init; }
    public float HeadquartersMoveChance { get; init; }
    public float RequiredInfluence { get; init; }
}

public enum LowAltitudeVerbOperation
{
    Move,
    GrowInfluence,
    ConsolidateInfluence,
    ReduceInfluence,
    ExpandCoverage,
    MoveHeadquarters,
}

public enum LowAltitudeVerbTargetMode
{
    Explore,
    EntityKind,
    RelationshipTarget,
    Terrain,
    OwnInfluence,
    ForeignInfluence,
}

public enum LowAltitudeVerbRelationshipDirection
{
    Outgoing,
    Incoming,
    Either,
}
