namespace Engine.Domain.Config;

using Engine.Kinds;
using Engine.WorldGen;

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
    public IReadOnlyList<ISemanticGeneratorEffect> Effects { get; init; } = [];
    public required IReadOnlyList<SemanticGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; } = 1;
    public int? MaxRunsPerSim { get; init; }
}

public sealed class SemanticGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AllowWilderness { get; init; }
    public IReadOnlySet<string>? RequiredOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredIncomingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredRelationships { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredOutgoingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredIncomingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredRelationshipRequirements { get; init; } = [];
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
}

public sealed class SemanticGeneratorRelationshipRequirement
{
    public required string RelationshipKind { get; init; }
    public float MinimumStrength { get; init; }
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
    public IReadOnlySet<string>? PreferredTerrains { get; init; }
    public IReadOnlySet<string>? AllowedTerrains { get; init; }
    public bool RespectTerrainBoundaries { get; init; } = true;
    public string? TransformTerrain { get; init; }
}

public sealed class SemanticEntityQuery
{
    public IReadOnlySet<string>? Subtypes { get; init; }
    public bool AdjacentToTarget { get; init; }
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
    public QueryTargetCultureMatch TargetCultureMatch { get; init; } = QueryTargetCultureMatch.Any;
}

public sealed class SemanticGeneratorBindingTarget
{
    public required string Ref { get; init; }
    public required EntityKind Kind { get; init; }
    public SemanticEntityQuery? Query { get; init; }
}

public enum QueryTargetCultureMatch
{
    Any,
    SameAsTarget,
    DifferentFromTarget,
}

public interface ISemanticGeneratorEffect
{
    string Describe();
}

/// <summary>Sets a resolved secondary binding entity to historical status after the generator fires.</summary>
public sealed class SetBindingHistoricalEffect(string bindingRef) : ISemanticGeneratorEffect
{
    public string BindingRef { get; } = bindingRef;

    public string Describe() => $"set_binding_historical({BindingRef})";
}

/// <summary>Removes relationships of a kind from the target, or a selected relationship instance.</summary>
public sealed class RemoveTargetRelationshipsEffect(
    string relationshipKind,
    GeneratorRelationshipInstanceSelection instanceSelection = GeneratorRelationshipInstanceSelection.AllTargetRelationships,
    bool includeReciprocal = false) : ISemanticGeneratorEffect
{
    public string RelationshipKind { get; } = relationshipKind;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"remove_target_relationships({RelationshipKind}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
}

public enum GeneratorRelationshipInstanceSelection
{
    AllTargetRelationships,
    TriggerRelationship,
}

/// <summary>Adjusts relationship strength on the target, or on a selected relationship instance.</summary>
public sealed class AdjustTargetRelationshipStrengthEffect(
    string relationshipKind,
    float delta,
    GeneratorRelationshipInstanceSelection instanceSelection = GeneratorRelationshipInstanceSelection.TriggerRelationship,
    bool includeReciprocal = false) : ISemanticGeneratorEffect
{
    public string RelationshipKind { get; } = relationshipKind;

    public float Delta { get; } = delta;

    public GeneratorRelationshipInstanceSelection InstanceSelection { get; } = instanceSelection;

    public bool IncludeReciprocal { get; } = includeReciprocal;

    public string Describe() =>
        $"adjust_target_relationship_strength({RelationshipKind}, delta={Delta}, instance={InstanceSelection}, reciprocal={IncludeReciprocal})";
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
}

public enum SemanticPlacement
{
    On,
    Near,
    Across,
    InRegionLocality,
}
