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

    /// <summary>Rules for short-lived non-graph actors emitted by named entities.</summary>
    public IReadOnlyList<MinorActorRuleConfig> MinorActors { get; init; } = [];

    public MinorActorCampaignConfig? MinorActorCampaign { get; init; }

    public MapFrontierConfig? MapFrontier { get; init; }

    /// <summary>Relationship kind whose holder is a laden carrier (road-speed bonus).</summary>
    public string? CarrierRelationshipKind { get; init; }

    /// <summary>Discrete movement capability classes used by route planning and terrain traversal.</summary>
    public IReadOnlyList<MovementProfileConfig> MovementProfiles { get; init; } = [];

    /// <summary>Culture definitions.</summary>
    public required IReadOnlyList<CultureConfig> Cultures { get; init; }

    /// <summary>Domain vocabulary feeding the culture power ledgers.</summary>
    public CulturePowerConfig CulturePower { get; init; } = new();

    /// <summary>
    /// Global tag catalog. Tags are discrete per-entity status bits (max 31)
    /// used for authored state machines: set/cleared by effect rules and
    /// generators, read by generator targeting. Not scalar stocks.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

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
    public EntityTier Tier { get; init; } = EntityTier.None;
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
    /// neutral idea-space axes. Mobility/drift verbs and cross-entity verb
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

/// <summary>
/// Declares which domain vocabulary feeds the culture power ledgers: custody
/// kinds and relationships for the Shadow axis, claim and war relationships
/// for the Blood axis. Empty lists drop the corresponding ledger component.
/// </summary>
public sealed class CulturePowerConfig
{
    public IReadOnlyList<string> CustodyKinds { get; init; } = [];
    public IReadOnlyList<string> CustodyRelationships { get; init; } = [];
    public string? ClaimRelationship { get; init; }
    public string? WarRelationship { get; init; }

    /// <summary>
    /// Relationship kinds whose strength counts as devotion flow for the Wake
    /// axis. Offering strength is attributed to the culture of the receiving
    /// site (the relationship target).
    /// </summary>
    public IReadOnlyList<string> OfferingRelationships { get; init; } = [];
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

    /// <summary>
    /// When true, creating a relationship of this kind moves the source
    /// entity's culture home anchor to the target entity's position. Roaming
    /// entities remit a share of their culture influence production to their
    /// anchor cell, so anchor kinds define where mobile entities "bank" their
    /// cultural output.
    /// </summary>
    public bool IsCultureAnchor { get; init; }

    public float CullThreshold { get; init; } = 0.05f;
    public IReadOnlyList<RelationshipMaintenanceRuleConfig> Maintenance { get; init; } = [];

    /// <summary>
    /// When true, this relationship counts as a structural affiliation edge: it
    /// expresses where the source belongs, who controls it, or what territory it
    /// occupies. Read-side Stage projections sum the strengths of affiliation edges
    /// to derive entity significance. Affiliation edges are typically positive or
    /// neutral and persistent. Conflict and event-shaped edges should leave this false.
    /// </summary>
    public bool IsAffiliation { get; init; }
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

/// <summary>Names the relationship kind that carries campaigns for campaign-labor cohorts.</summary>
public sealed class MinorActorCampaignConfig
{
    public required string RelationshipKind { get; init; }
}

/// <summary>
/// The growing map: which Ice-track ages drive thaw and re-freeze, the
/// mass/extent bounds, and semantic growth. Age ids reference WorldBeatAges by
/// id; the frozen terrain references a terrain name. Revealed base terrain and
/// geography come from the startup seed's latent fields.
/// </summary>
public sealed class MapFrontierConfig
{
    public required string ThawAge { get; init; }
    public string? RefreezeAge { get; init; }
    /// <summary>Additional ages that also drive thaw (e.g. a stronger epochal thaw).</summary>
    public IReadOnlyList<string> AdditionalThawAges { get; init; } = [];
    /// <summary>Additional ages that also drive reclaim (e.g. a catastrophic freeze).</summary>
    public IReadOnlyList<string> AdditionalRefreezeAges { get; init; } = [];
    public required string FrozenTerrain { get; init; }
    public int MaxRadius { get; init; } = 40;
    public float FrontierMassThreshold { get; init; } = 60f;
    public float ReclaimMassCeiling { get; init; } = 1f;
    public int ThawBudgetPerPulse { get; init; } = 24;
    public int RefreezeBudgetPerPulse { get; init; } = 24;

    /// <summary>
    /// Enables continuous semantic growth from the installed latent terrain
    /// field. Ideas have no ice, so semantic growth has no age gate and never
    /// re-freezes.
    /// </summary>
    public bool SemanticGrowthEnabled { get; init; }
    public int SemanticMaxRadius { get; init; } = 40;
    public float SemanticFrontierMassThreshold { get; init; } = 30f;
    public int SemanticBudgetPerPulse { get; init; } = 24;
}

public sealed class MinorActorRuleConfig
{
    public required EntityKind SourceKind { get; init; }
    public string? SourceSubtype { get; init; }
    public int PurposeCode { get; init; } = MinorActorPurposeCodes.CulturalLabor;
    public int ActorKindCode { get; init; } = MinorActorKindCodes.Worker;
    public int ActionCode { get; init; } = MinorActorActionCodes.CultureExpansion;
    public int PlaneCode { get; init; } = PlaneCodes.Physical;
    public int TtlPulses { get; init; } = 80;
    public int EmitCadencePulses { get; init; } = 60;
    public int MaxActivePerSource { get; init; } = 1;
    public int SpawnRadius { get; init; } = 1;
    public int MoveChancePermille { get; init; } = 250;
    public int ActionCadencePulses { get; init; } = 8;
    public float WorkDelta { get; init; } = 0.25f;
    public IReadOnlyList<MinorActorVerbConfig> Verbs { get; init; } = [];
}

public sealed class MinorActorVerbConfig
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public int ActionCode { get; init; } = MinorActorActionCodes.CultureExpansion;
    public int TargetingCode { get; init; } = MinorActorVerbTargetingCodes.ReinforceHomeMass;
    public int PlaneCode { get; init; } = PlaneCodes.None;
    public int Radius { get; init; } = 4;
    public int SampleCount { get; init; } = 32;
    public float Weight { get; init; } = 1f;
    public float WorkDeltaMultiplier { get; init; } = 1f;
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
    public IReadOnlyList<CultureSlotConfig> Slots { get; init; } = [];

    /// <summary>
    /// Multiplier on positive entity-driven culture influence production on
    /// the physical plane. Lets a culture's identity live on its own plane
    /// rather than tying all power to population.
    /// </summary>
    public float PhysicalInfluenceProductionMultiplier { get; init; } = 1f;

    /// <summary>Multiplier on positive semantic-plane influence production.</summary>
    public float SemanticInfluenceProductionMultiplier { get; init; } = 1f;
}

public enum CultureSlotPolarity
{
    Positive = RelationshipPolarityCodes.Positive,
    Neutral = RelationshipPolarityCodes.Neutral,
    Negative = RelationshipPolarityCodes.Negative,
}

public enum CultureSlotScoreSignal
{
    None = 0,
    ActiveEntity = CultureSlotScoreSignalCodes.ActiveEntity,
    PhysicalInfluence = CultureSlotScoreSignalCodes.PhysicalInfluence,
    SemanticInfluence = CultureSlotScoreSignalCodes.SemanticInfluence,
    PhysicalCoverage = CultureSlotScoreSignalCodes.PhysicalCoverage,
    SemanticCoverage = CultureSlotScoreSignalCodes.SemanticCoverage,
    OutgoingRelationshipStrength = CultureSlotScoreSignalCodes.OutgoingRelationshipStrength,
    IncomingRelationshipStrength = CultureSlotScoreSignalCodes.IncomingRelationshipStrength,
    PhysicalCultureSupport = CultureSlotScoreSignalCodes.PhysicalCultureSupport,
    SemanticCultureSupport = CultureSlotScoreSignalCodes.SemanticCultureSupport,
    PhysicalInfluenceProduction = CultureSlotScoreSignalCodes.PhysicalInfluenceProduction,
    SemanticInfluenceProduction = CultureSlotScoreSignalCodes.SemanticInfluenceProduction,
}

public sealed class CultureSlotConfig
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public CultureSlotPolarity Polarity { get; init; } = CultureSlotPolarity.Neutral;
    public IReadOnlySet<EntityKind> AllowedKinds { get; init; } = new HashSet<EntityKind>();
    public IReadOnlyList<CultureSlotScoreRuleConfig> ScoreRules { get; init; } = [];
    public CultureSlotHysteresisConfig Hysteresis { get; init; } = new();
    public CultureSlotBenefitConfig Benefits { get; init; } = new();
}

public sealed class CultureSlotScoreRuleConfig
{
    public required CultureSlotScoreSignal Signal { get; init; }
    public float Weight { get; init; } = 1f;
    public string? RelationshipKind { get; init; }
}

public sealed class CultureSlotHysteresisConfig
{
    public float MinimumScore { get; init; } = 1f;
    public int MinimumTenureTicks { get; init; } = 40;
    public float DisplacementMargin { get; init; } = 0.15f;
}

public sealed class CultureSlotBenefitConfig
{
    public float EntityInfluenceMultiplierDelta { get; init; } = 0.05f;
    public float CultureInfluenceMultiplierDelta { get; init; }
}
