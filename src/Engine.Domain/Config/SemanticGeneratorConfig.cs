namespace Engine.Domain.Config;

using Engine.Constants;
using Engine.Kinds;
using Engine.WorldGen;

public sealed class SemanticGeneratorConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? CausalLimit { get; init; }
    public required SemanticGeneratorTarget Target { get; init; }
    public IReadOnlyList<SemanticGeneratorBindingTarget> SecondaryTargets { get; init; } = [];
    public required IReadOnlyList<SemanticProducedEntity> Produces { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationship> Relationships { get; init; } = [];
    public IReadOnlyList<ISemanticGeneratorEffect> Effects { get; init; } = [];
    public required IReadOnlyList<SemanticGeneratorTrigger> Triggers { get; init; }
    public int PulseWeight { get; init; } = 1;
    public int? MaxRunsPerSim { get; init; }

    /// <summary>
    /// When positive, this is a timeline generator: it fires on its own
    /// cadence (roughly every CadenceTicks, offset by CadenceJitterTicks)
    /// outside the shared population-proportional pulse pool. Target
    /// eligibility still applies; a cadence firing with no eligible target
    /// passes quietly.
    /// </summary>
    public int CadenceTicks { get; init; }

    /// <summary>Uniform jitter applied to each cadence interval.</summary>
    public int CadenceJitterTicks { get; init; }
}

public sealed class SemanticGeneratorTarget
{
    public required EntityKind Kind { get; init; }
    public IReadOnlySet<string>? Subtypes { get; init; }
    public IReadOnlySet<string>? RequiredOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredIncomingRelationships { get; init; }
    public IReadOnlySet<string>? RequiredRelationships { get; init; }
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredOutgoingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredIncomingRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorRelationshipRequirement> RequiredRelationshipRequirements { get; init; } = [];
    public IReadOnlyList<SemanticGeneratorTerrainCountRequirement> TerrainCountRequirements { get; init; } = [];
    public IReadOnlySet<string>? WithoutOutgoingRelationships { get; init; }
    public IReadOnlySet<string>? WithoutIncomingRelationships { get; init; }
}

public sealed class SemanticGeneratorRelationshipRequirement
{
    public required string RelationshipKind { get; init; }
    public float MinimumStrength { get; init; }
}

public sealed class SemanticGeneratorTerrainCountRequirement
{
    public required string TerrainName { get; init; }
    public int Radius { get; init; }
    public int MaximumExclusive { get; init; }
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
    public IReadOnlySet<string>? PhysicalPlacementPreferredTerrains { get; init; }
    public IReadOnlySet<string>? PhysicalPlacementAllowedTerrains { get; init; }
    public IReadOnlySet<string>? PhysicalPlacementAvoidedTerrains { get; init; }
    public string? PhysicalPlacementFallbackTerrain { get; init; }
    public SemanticPlacement SemanticPlacement { get; init; } = SemanticPlacement.Near;
    public IReadOnlySet<string>? SemanticPlacementPreferredTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAllowedTerrains { get; init; }
    public IReadOnlySet<string>? SemanticPlacementAvoidedTerrains { get; init; }
    public string? SemanticPlacementFallbackTerrain { get; init; }
    public string PlacementRef { get; init; } = "$target";
    /// <summary>
    /// Ref of the bound entity whose culture the produced entity inherits.
    /// Exactly one of this or <see cref="Culture"/> must be set.
    /// </summary>
    public string? InheritCultureFrom { get; init; }

    /// <summary>
    /// Authored culture for the produced entity, independent of the target's
    /// culture. Enables cross-cultural generators where one culture's activity
    /// creates another culture's entities.
    /// </summary>
    public string? Culture { get; init; }
    public GeneratorCulturePlacement CulturePlacement { get; init; } = GeneratorCulturePlacement.PreferInheritedCulture;
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
    public bool SamePhysicalCultureComponentAsTarget { get; init; }
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

public enum GeneratorCulturePlacement
{
    Ignore,
    PreferInheritedCulture,
    HostileIntrusion,
    CultureInterior,
    CultureFrontier,
    CultureRoute,
    Wilderness,
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

/// <summary>Transforms a bounded map patch around a resolved generator binding.</summary>
public sealed class SetTerrainPatchEffect(
    string centerRef,
    string terrain,
    int radius,
    int maxCellCount = 0,
    bool respectTerrainBoundaries = true,
    float chance = 1f,
    int planeCode = PlaneCodes.Physical) : ISemanticGeneratorEffect
{
    public string CenterRef { get; } = centerRef;

    public string Terrain { get; } = terrain;

    public int Radius { get; } = radius;

    public int MaxCellCount { get; } = maxCellCount;

    public bool RespectTerrainBoundaries { get; } = respectTerrainBoundaries;

    public float Chance { get; } = chance;

    public int PlaneCode { get; } = planeCode;

    public IReadOnlySet<string>? PreferredTerrains { get; init; }

    public IReadOnlySet<string>? AllowedTerrains { get; init; }

    public string Describe() =>
        $"set_terrain_patch(center={CenterRef}, terrain={Terrain}, radius={Radius}, max={MaxCellCount}, chance={Chance}, plane={PlaneCode})";
}

public sealed class SemanticGeneratorRelationship
{
    public required string SourceRef { get; init; }
    public required string DestinationRef { get; init; }
    public required string Kind { get; init; }
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
