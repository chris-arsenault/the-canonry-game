namespace Engine.Domain;

using Engine.Kinds;
using Engine.Wiring;
using Engine.Resources;

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

    /// <summary>Data-defined processes and decisions.</summary>
    public required IReadOnlyList<Processes.ProcessDefinition> Processes { get; init; }

    /// <summary>Relationship kind definitions.</summary>
    public required IReadOnlyList<RelationshipConfig> Relationships { get; init; }

    /// <summary>Pressure definitions.</summary>
    public required IReadOnlyList<PressureConfig> Pressures { get; init; }

    /// <summary>Lifecycle drain configuration per kind.</summary>
    public required IReadOnlyDictionary<EntityKind, LifecycleConfig> Lifecycles { get; init; }

    /// <summary>Resource type definitions.</summary>
    public required IReadOnlyList<ResourceConfig> Resources { get; init; }

    /// <summary>Culture definitions.</summary>
    public required IReadOnlyList<CultureConfig> Cultures { get; init; }
}

/// <summary>Per-kind configuration: subtypes, attributes, health metric.</summary>
public sealed class KindConfig
{
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required IReadOnlyList<AttributeConfig> Attributes { get; init; }
    public string? HealthAttribute { get; init; }
    public IReadOnlyList<StandingDriveConfig> StandingDrives { get; init; } = [];
}

/// <summary>Low-urgency standing drive that participates in normal goal generation.</summary>
public sealed class StandingDriveConfig
{
    public required AttributeRef DesiredEffect { get; init; }
    public float BaseUrgency { get; init; }
}

/// <summary>Single attribute definition.</summary>
public sealed class AttributeConfig
{
    public required string Name { get; init; }
    public required float Min { get; init; }
    public required float Max { get; init; }
    public float Default { get; init; }

    /// <summary>Variance on default value at initialization. 0 = exact, 0.5 = ±50%.</summary>
    public float DefaultVariance { get; init; }

    /// <summary>
    /// Optional critical-range hook. When this attribute falls into the shared critical
    /// band for standard 0-100 scalar attributes, the engine applies the configured
    /// per-tick effect to another attribute on the same entity.
    /// </summary>
    public CriticalEffectConfig? CriticalEffect { get; init; }

    /// <summary>Legacy comfort parameter for non-standard scalar urgency calculation.
    /// Higher baseline = slower urgency decay as value grows. 0 = use default formula.</summary>
    public float Baseline { get; init; }

    /// <summary>Per-tick drift rate. Positive = increases each tick, negative = decreases.
    /// Zero (default) = no drift. e.g., satiety drifts -0.35/tick, condition drifts -0.01/tick.</summary>
    public float DriftRate { get; init; }

    /// <summary>
    /// Optional ambient location resource that adds a dynamic offset to this attribute's
    /// per-tick drift. Useful for sustainment behaviors like satiety falling more slowly
    /// on food-rich hexes than on barren ones.
    /// </summary>
    public ResourceType? LocationResourceDriftResource { get; init; }

    /// <summary>
    /// Per-unit offset applied from the ambient location resource. Negative values reduce
    /// drift on rich hexes; positive values amplify drift.
    /// </summary>
    public float LocationResourceDriftPerUnit { get; init; }

    /// <summary>
    /// Minimum dynamic offset contributed by the ambient location resource.
    /// </summary>
    public float LocationResourceDriftMinOffset { get; init; }

    /// <summary>
    /// Maximum dynamic offset contributed by the ambient location resource.
    /// </summary>
    public float LocationResourceDriftMaxOffset { get; init; }

    /// <summary>Semantic plane definition for 2D attributes. Null for scalar attributes.</summary>
    public Attributes.SemanticPlaneDefinition? PlaneDefinition { get; init; }

    /// <summary>Default X value for 2D attributes.</summary>
    public float DefaultX { get; init; }

    /// <summary>Default Y value for 2D attributes.</summary>
    public float DefaultY { get; init; }
}

/// <summary>
/// Domain-defined negative effect applied while an attribute remains in the shared
/// critical band. The engine owns when the critical band is active; the domain owns
/// what the penalty does.
/// </summary>
public sealed class CriticalEffectConfig
{
    public required string TargetAttribute { get; init; }
    public required float DeltaPerTick { get; init; }
    public string? Description { get; init; }
}

/// <summary>Relationship kind configuration.</summary>
public sealed class RelationshipConfig
{
    public required string Name { get; init; }
    public required Graph.RelationshipPolarity Polarity { get; init; }
    public required IReadOnlySet<EntityCategory> ValidSourceCategories { get; init; }
    public required IReadOnlySet<EntityCategory> ValidTargetCategories { get; init; }
    public bool IsSymmetric { get; init; }
    public bool IsExclusive { get; init; }
    public float CullThreshold { get; init; } = 0.05f;
}

/// <summary>Pressure definition configuration.</summary>
public sealed class PressureConfig
{
    public required string Name { get; init; }
    public required Pressure.PressureScope Scope { get; init; }
    public float Baseline { get; init; }
    public float HomeostasisRate { get; init; } = 0.1f;
    public float SmoothingFactor { get; init; } = 0.3f;
    // Contributing factors will be defined via the wiring contract system (#35)
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

/// <summary>Culture configuration.</summary>
public sealed class CultureConfig
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
}
