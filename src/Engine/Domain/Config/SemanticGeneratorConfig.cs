namespace Engine.Domain.Config;

using Engine.Kinds;

public sealed class SemanticGeneratorConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required SemanticGeneratorTarget Target { get; init; }
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
    public IReadOnlyList<SemanticSaturationLimit> SaturationLimits { get; init; } = [];
}

public sealed class SemanticSaturationLimit
{
    public required string RelationshipKind { get; init; }
    public required string Direction { get; init; }
    public required int MaxCount { get; init; }
}

public sealed class SemanticProducedEntity
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public required string Subtype { get; init; }
    public required string Role { get; init; }
    public float CreateChance { get; init; } = 1f;
    public SemanticPlacement Placement { get; init; } = SemanticPlacement.Near;
    public string? InheritCultureFrom { get; init; }
}

public sealed class SemanticGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required string Kind { get; init; }
    public float Strength { get; init; } = 1f;
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
}

public enum TriggerDirection
{
    High,
    Low,
    Critical,
    Any,
}
