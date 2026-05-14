namespace Engine.Domain.Config;

using Engine.Constants;
using Engine.Gambits;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

/// <summary>
/// All domain-specific content that the engine needs. Health, traits,
/// subtypes, relationship kinds, lifecycle config, and tuning values.
/// The engine never hard-codes domain strings -- everything comes from here.
/// Swap this object to swap the entire game world.
/// </summary>
public sealed class DomainConfig
{
    /// <summary>Kind definitions: valid subtypes, health, and trait catalogs.</summary>
    public required IReadOnlyDictionary<EntityKind, KindConfig> Kinds { get; init; }

    /// <summary>Data-defined high-stakes entity gambits.</summary>
    public IReadOnlyList<GambitDefinition> Gambits { get; init; } = [];

    /// <summary>Relationship kind definitions.</summary>
    public required IReadOnlyList<RelationshipConfig> Relationships { get; init; }

    /// <summary>Lifecycle drain configuration per kind.</summary>
    public required IReadOnlyDictionary<EntityKind, LifecycleConfig> Lifecycles { get; init; }

    /// <summary>Terrain definitions used by world generation and startup seeding.</summary>
    public required IReadOnlyList<TerrainConfig> Terrains { get; init; }

    /// <summary>Non-entity, non-terrain travel structures that can alter movement over cells.</summary>
    public IReadOnlyList<TravelStructureConfig> TravelStructures { get; init; } = [];

    /// <summary>Discrete movement capability classes used by route planning and terrain traversal.</summary>
    public IReadOnlyList<MovementProfileConfig> MovementProfiles { get; init; } = [];

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

    /// <summary>
    /// Domain-authored chronicle chain queries compiled against this domain's
    /// runtime IDs. The engine stores generic provenance; the domain chooses
    /// which chains are narratively meaningful.
    /// </summary>
    public ChronicleConfig Chronicle { get; init; } = new();
}

public sealed class ChronicleConfig
{
    public IReadOnlyList<ChronicleQueryConfig> Queries { get; init; } = [];
}

public sealed class ChronicleQueryConfig
{
    public required string Id { get; init; }
    public required ChronicleProducerRef Parent { get; init; }
    public required string TriggerRelationshipKind { get; init; }
    public required ChronicleProducerRef Child { get; init; }
}

public sealed class ChronicleProducerRef
{
    public required ChronicleProducerKind Kind { get; init; }
    public required string StableName { get; init; }
}

public enum ChronicleProducerKind
{
    Gambit,
    Generator,
}

/// <summary>Per-kind configuration: subtypes, health scalar, and trait catalog.</summary>
public sealed class KindConfig
{
    public EntityCategory Category { get; init; } = EntityCategory.Agency;
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required HealthConfig Health { get; init; }
    public IReadOnlyList<TraitConfig> Traits { get; init; } = [];
    public MovementProfileAssignmentConfig MovementProfiles { get; init; } = new();

    /// <summary>
    /// Design-time prose: what this kind represents in the world — distinct from
    /// its health, traits, and mechanics. Used by tooling, UI flavor, and downstream
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

/// <summary>Single mutable health scalar definition for an entity kind.</summary>
public sealed class HealthConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public IReadOnlyDictionary<string, string>? CultureDisplayNames { get; init; }
    public required float Min { get; init; }
    public required float Max { get; init; }
    public float Default { get; init; }

    /// <summary>Variance on default value at initialization. 0 = exact, 0.5 = ±50%.</summary>
    public float DefaultVariance { get; init; }
}

public enum TraitPolarity
{
    Neutral,
    Positive,
    Negative,
}

/// <summary>
/// Mostly static entity differentiator. Traits are authored per kind and assigned
/// to individual entities at creation; their eventual effects should be explicit
/// behavior/effect rules rather than hidden scalar stock changes.
/// </summary>
public sealed class TraitConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? EffectSummary { get; init; }
    public TraitPolarity Polarity { get; init; } = TraitPolarity.Neutral;
    public IReadOnlyList<ITraitOperationConfig> Operations { get; init; } = [];

    /// <summary>Independent 0..1 chance that a newly created entity of this kind receives the trait.</summary>
    public float AssignmentChance { get; init; }
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

/// <summary>Lifecycle drain configuration for a kind.</summary>
public sealed class LifecycleConfig
{
    public required string Health { get; init; }
    public required float BaseRate { get; init; }
    public required float AgeCoefficient { get; init; }
}

/// <summary>Domain-authored terrain identity and runtime gameplay profile.</summary>
public sealed class TerrainConfig
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string BoundaryGroup { get; init; } = "default";
    public float MovementCost { get; init; } = 1f;
    public float InfluenceMultiplier { get; init; } = 1f;
    public float ClaimCostMultiplier { get; init; } = 1f;
    public float ClaimChanceMultiplier { get; init; } = 1f;
    public float RoadBuildMultiplier { get; init; } = 1f;
    public float RoadSpeedMultiplier { get; init; } = 1f;
    public IReadOnlyList<TerrainMovementProfileConfig> MovementProfiles { get; init; } = [];
    public IReadOnlyList<TerrainKindProfileConfig> KindProfiles { get; init; } = [];
}

public sealed class MovementProfileConfig
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
}

public sealed class MovementProfileAssignmentConfig
{
    public string? PhysicalDefault { get; init; }
    public string? SemanticDefault { get; init; }
    public IReadOnlyList<MovementProfileSubtypeRuleConfig> SubtypeRules { get; init; } = [];
    public IReadOnlyList<MovementProfileTraitRuleConfig> TraitRules { get; init; } = [];
}

public sealed class MovementProfileSubtypeRuleConfig
{
    public required string Subtype { get; init; }
    public string? PhysicalProfile { get; init; }
    public string? SemanticProfile { get; init; }
}

public sealed class MovementProfileTraitRuleConfig
{
    public required string Trait { get; init; }
    public string? PhysicalProfile { get; init; }
    public string? SemanticProfile { get; init; }
}

public sealed class TerrainMovementProfileConfig
{
    public required string Profile { get; init; }
    public required float MovementCost { get; init; }
}

public sealed class TravelStructureConfig
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public int PlaneCode { get; init; } = PlaneCodes.Physical;
    public IReadOnlyList<TravelStructureMovementProfileConfig> MovementProfiles { get; init; } = [];
}

public sealed class TravelStructureMovementProfileConfig
{
    public required string Profile { get; init; }
    public required float MovementCost { get; init; }
}

public sealed class TerrainKindProfileConfig
{
    public required EntityKind Kind { get; init; }
    public float? InfluenceMultiplier { get; init; }
    public float? ClaimCostMultiplier { get; init; }
    public float? ClaimChanceMultiplier { get; init; }
}

/// <summary>Culture configuration.</summary>
public sealed class CultureConfig
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
}
