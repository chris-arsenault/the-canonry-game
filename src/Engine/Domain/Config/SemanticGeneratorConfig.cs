namespace Engine.Domain.Config;

using Engine.Kinds;
using Engine.Map;

public sealed class SemanticGeneratorConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required SemanticGeneratorTarget Target { get; init; }
    public SemanticTargetSaturation? TargetSaturation { get; init; }
    public IReadOnlyList<SemanticGeneratorBindingTarget> SecondaryTargets { get; init; } = [];
    public required IReadOnlyList<SemanticProducedEntity> Produces { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationship> Relationships { get; init; } = [];
    public required IReadOnlyList<SemanticGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; } = 1;
    public int? MaxRunsPerSim { get; init; }
}

public sealed class SemanticGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AllowWilderness { get; init; }
}

public sealed class SemanticTargetSaturation
{
    public required string RelationshipKind { get; init; }
    public string? RequiredFact { get; init; }
    public TargetSaturationDirection Direction { get; init; } = TargetSaturationDirection.Symmetric;
    public int MaxExisting { get; init; }
}

public enum TargetSaturationDirection
{
    OutgoingFromTarget,
    IncomingToTarget,
    Symmetric,
}

public sealed class SemanticProducedEntity
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public string? Subtype { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public required string Role { get; init; }
    public float CreateChance { get; init; } = 1f;
    public bool CreateIfMissing { get; init; } = true;
    public SemanticPlacement PhysicalPlacement { get; init; } = SemanticPlacement.Near;
    public SemanticPlacement SemanticPlacement { get; init; } = SemanticPlacement.Near;
    public string PlacementRef { get; init; } = "$target";
    public required string InheritCultureFrom { get; init; }
    public SemanticEntityQuery? Query { get; init; }
    public SemanticRegionCoverage? Coverage { get; init; }
}

public sealed class SemanticRegionCoverage
{
    public required CoveragePlacementMode Mode { get; init; }
    public string? SourceRef { get; init; }
    public int DesiredCellCount { get; init; } = 18;
    public int MinimumCellCount { get; init; } = 8;
    public int MinimumSourceRemainingCellCount { get; init; } = 8;
    public IReadOnlySet<TerrainType>? PreferredTerrains { get; init; }
    public IReadOnlySet<TerrainType>? AllowedTerrains { get; init; }
    public bool RespectTerrainBoundaries { get; init; } = true;
    public TerrainType? TransformTerrain { get; init; }
}

public sealed class SemanticEntityQuery
{
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AdjacentToTarget { get; init; }
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
    public QueryTargetCultureMatch TargetCultureMatch { get; init; } = QueryTargetCultureMatch.Any;
    public IReadOnlyList<SemanticQueryAttributeCondition> AttributeConditions { get; init; } = [];
}

public sealed class SemanticGeneratorBindingTarget
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public SemanticEntityQuery? Query { get; init; }
}

public sealed class SemanticQueryAttributeCondition
{
    public required string AttributeName { get; init; }
    public TriggerDirection Direction { get; init; } = TriggerDirection.Any;
}

public enum QueryTargetCultureMatch
{
    Any,
    SameAsTarget,
    DifferentFromTarget,
}

public sealed class SemanticGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required string Kind { get; init; }
    public IReadOnlySet<string> Facts { get; init; } = new HashSet<string>(StringComparer.Ordinal);
    public float Strength { get; init; } = 1f;
    public bool ReplaceExistingOutgoingOfSameKind { get; init; }
}

public sealed class SemanticGeneratorTrigger
{
    public required EntityKind OnKind { get; init; }
    public string? AttributeName { get; init; }
    public TriggerDirection Direction { get; init; } = TriggerDirection.Any;
}

public enum SemanticPlacement
{
    On,
    Near,
    Across,
    InRegionLocality,
}

public enum TriggerDirection
{
    High,
    Low,
    Critical,
    Any,
}
