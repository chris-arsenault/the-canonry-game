namespace Engine.Domain.Config;

using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Resources;
using Engine.Wiring;

/// <summary>
/// All domain-specific content that the engine needs. Attribute names, subtypes,
/// relationship kinds, pressure definitions, lifecycle config, and tuning values.
/// The engine never hard-codes domain strings -- everything comes from here.
/// Swap this object to swap the entire game world.
/// </summary>
public sealed class DomainConfig
{
    /// <summary>Kind definitions: valid subtypes, attribute schemas, health attributes.</summary>
    public required IReadOnlyDictionary<EntityKind, KindConfig> Kinds { get; init; }

    /// <summary>Data-defined high-stakes entity gambits.</summary>
    public IReadOnlyList<GambitDefinition> Gambits { get; init; } = [];

    /// <summary>Relationship kind definitions.</summary>
    public required IReadOnlyList<RelationshipConfig> Relationships { get; init; }

    /// <summary>Domain-declared facts that can annotate relationship instances.</summary>
    public IReadOnlyList<RelationshipFactConfig> RelationshipFacts { get; init; } = [];

    /// <summary>Pressure definitions.</summary>
    public required IReadOnlyList<PressureConfig> Pressures { get; init; }

    /// <summary>Degeneracy detector definitions.</summary>
    public DegeneracyConfig Degeneracy { get; init; } = new();

    /// <summary>Lifecycle drain configuration per kind.</summary>
    public required IReadOnlyDictionary<EntityKind, LifecycleConfig> Lifecycles { get; init; }

    /// <summary>Resource type definitions.</summary>
    public required IReadOnlyList<ResourceConfig> Resources { get; init; }

    /// <summary>Terrain definitions used by world generation and startup seeding.</summary>
    public required IReadOnlyList<TerrainConfig> Terrains { get; init; }

    /// <summary>Culture definitions.</summary>
    public required IReadOnlyList<CultureConfig> Cultures { get; init; }

    /// <summary>
    /// Reusable gameplay-system selections and authored gameplay parameters.
    /// Domain content chooses and names gameplay modules; Engine.Gameplay owns
    /// reusable mechanics, validation, and compile-down to kernel program data.
    /// </summary>
    public GameplayDefinition? Gameplay { get; init; }

    /// <summary>
    /// Optional higher-order semantic stock/flow model for reasoning and validation.
    /// This is domain-authored structured data, not derived from runtime behavior wiring.
    /// </summary>
    public SemanticDomainConfig? Semantics { get; init; }
}

/// <summary>Per-kind configuration: subtypes, attributes, health metric.</summary>
public sealed class KindConfig
{
    public EntityCategory Category { get; init; } = EntityCategory.Agency;
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required IReadOnlyList<AttributeConfig> Attributes { get; init; }
    public string? HealthAttribute { get; init; }

    /// <summary>
    /// Design-time prose: what this kind represents in the world — distinct from
    /// its attributes and mechanics. Used by tooling, UI flavor, and downstream
    /// readers to understand the kind's semantic identity.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The plane on which this kind primarily positions itself. Physical-plane kinds
    /// (such as penguins and artifacts in Ice Remembers) use the map hex grid and
    /// measure proximity via HexCoord.Distance. Semantic-plane kinds use SemanticHexCoord on the
    /// autonomy/resonance/tradition axes. Mobility/drift verbs and cross-entity verb
    /// proximity checks pick their metric from the owner's primary plane.
    /// </summary>
    public required PrimaryPlane PrimaryPlane { get; init; }
}

public enum PrimaryPlane
{
    Physical,
    Semantic,
}

/// <summary>
/// Category of an attribute within the uniform H + (C|P) + (N|D) invariant.
/// Every kind must have exactly 3 attributes: one Health, one from {Capacity, Projection}
/// (outward emission), and one from {Need, Drive} (internal state).
/// </summary>
public enum AttributeCategory
{
    /// <summary>Lifecycle anchor. Dies/destroyed at 0. Exactly one per kind.</summary>
    Health,

    /// <summary>Outward emission — tangible/mechanical working stock.</summary>
    Capacity,

    /// <summary>Outward emission — symbolic/reputational stock.</summary>
    Projection,

    /// <summary>Internal state — deficit-driven urge to refill.</summary>
    Need,

    /// <summary>Internal state — self-generating fuel for outward action.</summary>
    Drive,
}

/// <summary>Single attribute definition.</summary>
public sealed class AttributeConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public IReadOnlyDictionary<string, string>? CultureDisplayNames { get; init; }

    /// <summary>
    /// Category slot. Every kind must declare exactly 3 attributes: one Health,
    /// one outward (Capacity or Projection), one internal (Need or Drive). Defaults
    /// to Capacity for test scaffolds; canonical domain configs set this explicitly.
    /// </summary>
    public AttributeCategory Category { get; init; } = AttributeCategory.Capacity;

    public required float Min { get; init; }
    public required float Max { get; init; }
    public float Default { get; init; }

    /// <summary>Variance on default value at initialization. 0 = exact, 0.5 = ±50%.</summary>
    public float DefaultVariance { get; init; }
}

/// <summary>Relationship kind configuration.</summary>
public sealed class RelationshipConfig
{
    public required string Name { get; init; }
    public required Engine.Graph.RelationshipPolarity Polarity { get; init; }
    public required IReadOnlySet<EntityCategory> ValidSourceCategories { get; init; }
    public required IReadOnlySet<EntityCategory> ValidTargetCategories { get; init; }
    public bool IsSymmetric { get; init; }
    public bool IsExclusive { get; init; }
    public bool TargetFollowsSourcePhysical { get; init; }
    public float CullThreshold { get; init; } = 0.05f;
    public IReadOnlyList<RelationshipMaintenanceRuleConfig> Maintenance { get; init; } = [];
}

/// <summary>
/// A typed assertion attached to a relationship instance. Relationship kind keeps
/// broad graph meaning; facts preserve the more specific event meaning.
/// </summary>
public sealed class RelationshipFactConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}

/// <summary>Pressure definition configuration.</summary>
public sealed class PressureConfig
{
    public required string Name { get; init; }
    public required Engine.Pressure.PressureScope Scope { get; init; }
    public float Baseline { get; init; }
    public float HomeostasisRate { get; init; } = 0.1f;
    public float SmoothingFactor { get; init; } = 0.3f;
    // Contributing factors will be defined via the wiring contract system (#35)
}

public sealed class DegeneracyConfig
{
    public IReadOnlyList<DegeneracyDetectorConfig> Detectors { get; init; } = [];
}

public sealed class DegeneracyDetectorConfig
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public required string ReasonCode { get; init; }
    public required DegeneracyMetric Metric { get; init; }
    public DegeneracyThresholdDirection ThresholdDirection { get; init; } = DegeneracyThresholdDirection.HighIsBad;
    public DegeneracySeverity NoSubjectsSeverity { get; init; } = DegeneracySeverity.Normal;
    public required EntityKind PrimaryKind { get; init; }
    public EntityKind? SecondaryKind { get; init; }
    public string? Attribute { get; init; }
    public string? RelationshipKind { get; init; }
    public DegeneracyRelationshipEndpoint DominantRelationshipEndpoint { get; init; } = DegeneracyRelationshipEndpoint.Target;
    public required float WarningThreshold { get; init; }
    public required float CriticalThreshold { get; init; }
}

public enum DegeneracyMetric
{
    AttributeDeclineRatio,
    RelationshipEndpointDominanceRatio,
    ActiveKindCount,
}

public enum DegeneracyThresholdDirection
{
    HighIsBad,
    LowIsBad,
}

public enum DegeneracyRelationshipEndpoint
{
    Source,
    Target,
}

public enum DegeneracySeverity
{
    Normal,
    Warning,
    Critical,
}

/// <summary>Lifecycle drain configuration for a kind.</summary>
public sealed class LifecycleConfig
{
    public required string HealthAttribute { get; init; }
    public required float BaseRate { get; init; }
    public required float AgeCoefficient { get; init; }
}

/// <summary>Resource type configuration.</summary>
public sealed class ResourceConfig
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>Domain-authored terrain identity. Runtime map generation receives richer terrain profiles separately.</summary>
public sealed class TerrainConfig
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string BoundaryGroup { get; init; } = "default";
}

/// <summary>Culture configuration.</summary>
public sealed class CultureConfig
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
}
