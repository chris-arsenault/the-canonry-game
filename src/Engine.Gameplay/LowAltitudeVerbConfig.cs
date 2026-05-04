namespace Engine.Gameplay;

/// <summary>
/// Low-altitude intent. A verb chooses a mid-term target, commits for a short
/// duration, and produces movement or analogous map influence.
/// </summary>
public abstract class LowAltitudeVerbConfig
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required string Kind { get; init; }
    public string? Plane { get; init; }
    public abstract LowAltitudeVerbOperation Operation { get; }
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
    public ForeignInfluenceTargetingConfig? ForeignInfluenceTargeting { get; init; }
    public string? AffinityRelationshipKind { get; init; }
    public LowAltitudeVerbRelationshipDirection AffinityRelationshipDirection { get; init; } =
        LowAltitudeVerbRelationshipDirection.Either;
    public float RelationshipAffinityWeight { get; init; }
    public virtual float InfluenceDelta => 0f;
    public virtual int InfluenceRadius => 0;
    public virtual float CoverageGrowthChance => 0f;
    public virtual float HeadquartersMoveChance => 0f;
    public virtual float RequiredInfluence => 0f;
    public virtual float RelationshipStrengthDelta => 0f;
}

public sealed class ForeignInfluenceTargetingConfig
{
    public bool BlockPositiveRelationships { get; init; }
    public bool AllowNegativeRelationships { get; init; }
    public float MinimumRelationshipStrength { get; init; } = 0.05f;
    public float AllowUnrelatedInfluenceAtOrAbove { get; init; } = -1f;
}

public sealed class MovementLowAltitudeVerbConfig : LowAltitudeVerbConfig
{
    public override LowAltitudeVerbOperation Operation => LowAltitudeVerbOperation.Move;
}

public sealed class InfluenceLowAltitudeVerbConfig : LowAltitudeVerbConfig
{
    public required LowAltitudeVerbOperation InfluenceOperation { get; init; }
    public required float Delta { get; init; }
    public int Radius { get; init; }

    public override LowAltitudeVerbOperation Operation => InfluenceOperation;
    public override float InfluenceDelta => Delta;
    public override int InfluenceRadius => Radius;
}

public sealed class CoverageExpansionLowAltitudeVerbConfig : LowAltitudeVerbConfig
{
    public required float Delta { get; init; }
    public int Radius { get; init; }
    public required float GrowthChance { get; init; }
    public required float RequiredMapValue { get; init; }

    public override LowAltitudeVerbOperation Operation => LowAltitudeVerbOperation.ExpandCoverage;
    public override float InfluenceDelta => Delta;
    public override int InfluenceRadius => Radius;
    public override float CoverageGrowthChance => GrowthChance;
    public override float RequiredInfluence => RequiredMapValue;
}

public sealed class AnchorRelocationLowAltitudeVerbConfig : LowAltitudeVerbConfig
{
    public required float Delta { get; init; }
    public int Radius { get; init; }
    public required float RelocationChance { get; init; }
    public required float RequiredMapValue { get; init; }

    public override LowAltitudeVerbOperation Operation => LowAltitudeVerbOperation.MoveHeadquarters;
    public override float InfluenceDelta => Delta;
    public override int InfluenceRadius => Radius;
    public override float HeadquartersMoveChance => RelocationChance;
    public override float RequiredInfluence => RequiredMapValue;
}

public sealed class RelationshipStrengthLowAltitudeVerbConfig : LowAltitudeVerbConfig
{
    public required float Delta { get; init; }

    public override LowAltitudeVerbOperation Operation => LowAltitudeVerbOperation.AdjustRelationshipStrength;
    public override float RelationshipStrengthDelta => Delta;
}

public enum LowAltitudeVerbOperation
{
    Move,
    GrowInfluence,
    ConsolidateInfluence,
    ReduceInfluence,
    ExpandCoverage,
    MoveHeadquarters,
    AdjustRelationshipStrength,
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
