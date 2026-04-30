namespace Engine.Domain.Config;

using Engine.Gambits;
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

    /// <summary>Data-defined low-altitude processes.</summary>
    public required IReadOnlyList<Processes.ProcessDefinition> Processes { get; init; }

    /// <summary>Data-defined high-stakes entity gambits.</summary>
    public IReadOnlyList<GambitDefinition> Gambits { get; init; } = [];

    /// <summary>
    /// Data-defined low-altitude verbs. These are entity-centric, multi-tick
    /// intents that drive physical/semantic movement or analogous map dynamics.
    /// </summary>
    public IReadOnlyList<LowAltitudeVerbConfig> LowAltitudeVerbs { get; init; } = [];

    /// <summary>Relationship kind definitions.</summary>
    public required IReadOnlyList<RelationshipConfig> Relationships { get; init; }

    /// <summary>Domain-declared facts that can annotate relationship instances.</summary>
    public IReadOnlyList<RelationshipFactConfig> RelationshipFacts { get; init; } = [];

    /// <summary>Pressure definitions.</summary>
    public required IReadOnlyList<PressureConfig> Pressures { get; init; }

    /// <summary>Degeneracy detector definitions.</summary>
    public DegeneracyConfig Degeneracy { get; init; } = new();

    /// <summary>Relationship/map-derived effects applied by the kernel effects program.</summary>
    public EffectsConfig Effects { get; init; } = new();

    /// <summary>
    /// Dynamic spatial signals derived from terrain, influence, coverage, activity,
    /// occupancy, and relationships. These are consumed by effects and
    /// low-altitude verbs; they do not create or destroy graph structure.
    /// </summary>
    public MapSignalsConfig MapSignals { get; init; } = new();

    /// <summary>Lifecycle drain configuration per kind.</summary>
    public required IReadOnlyDictionary<EntityKind, LifecycleConfig> Lifecycles { get; init; }

    /// <summary>Resource type definitions.</summary>
    public required IReadOnlyList<ResourceConfig> Resources { get; init; }

    /// <summary>Terrain definitions used by world generation and startup seeding.</summary>
    public required IReadOnlyList<TerrainConfig> Terrains { get; init; }

    /// <summary>Culture definitions.</summary>
    public required IReadOnlyList<CultureConfig> Cultures { get; init; }

    /// <summary>
    /// Optional higher-order semantic stock/flow model for reasoning and validation.
    /// This is domain-authored structured data, not derived from runtime process wiring.
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
    public IReadOnlyList<StandingDriveConfig> StandingDrives { get; init; } = [];
    public IReadOnlyList<RecoveryHostQueryConfig> ResourceRecoveryHosts { get; init; } = [];

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

/// <summary>
/// ProcessProgram-visible host query for recovery verbs that read location
/// resources. `AnyOfKinds` is a disjunction: any matching named entity kind can
/// serve as the recovery site.
/// </summary>
public sealed class RecoveryHostQueryConfig
{
    public required IReadOnlySet<EntityKind> AnyOfKinds { get; init; }
}

public enum PrimaryPlane
{
    Physical,
    Semantic,
}

/// <summary>
/// Low-altitude intent. A verb chooses a mid-term target, commits for a short
/// duration, and produces movement or physical map influence.
/// </summary>
public sealed class LowAltitudeVerbConfig
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required EntityKind Kind { get; init; }
    public PrimaryPlane? Plane { get; init; }
    public LowAltitudeVerbOperation Operation { get; init; } = LowAltitudeVerbOperation.Move;
    public required LowAltitudeVerbTargetMode TargetMode { get; init; }
    public EntityKind? TargetKind { get; init; }
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

/// <summary>Low-urgency standing drive that participates in normal goal generation.</summary>
public sealed class StandingDriveConfig
{
    public required AttributeRef DesiredEffect { get; init; }
    public float BaseUrgency { get; init; }
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

    /// <summary>Legacy comfort parameter for non-standard scalar urgency calculation.
    /// Higher baseline = slower urgency decay as value grows. 0 = use default formula.</summary>
    public float Baseline { get; init; }
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
    public float CullThreshold { get; init; } = 0.05f;
    public IReadOnlyList<RelationshipMaintenanceRuleConfig> Maintenance { get; init; } = [];
}

public sealed class RelationshipMaintenanceRuleConfig
{
    public required RelationshipMaintenanceCondition Condition { get; init; }
    public PrimaryPlane? Plane { get; init; }
    public int MaxDistance { get; init; }
    public float Threshold { get; init; }
    public required float Delta { get; init; }
}

public enum RelationshipMaintenanceCondition
{
    Always,
    SameCulture,
    DifferentCulture,
    DistanceWithin,
    CoPresence,
    SourceInfluenceAtTarget,
    TargetInfluenceAtSource,
    ContactMemory,
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

public sealed class MapSignalsConfig
{
    public float SemanticBaseCapacity { get; init; } = 2f;
    public int CrowdingRadius { get; init; } = 1;
    public IReadOnlyList<TerrainMapSignalConfig> Terrains { get; init; } = [];
    public IReadOnlyList<KindMapSignalConfig> Kinds { get; init; } = [];
    public IReadOnlyList<RelationshipMapSignalConfig> Relationships { get; init; } = [];
}

public sealed class TerrainMapSignalConfig
{
    public required string Terrain { get; init; }
    public required float BaseCapacity { get; init; }
}

public sealed class KindMapSignalConfig
{
    public required EntityKind Kind { get; init; }
    public string? HealthAttribute { get; init; }
    public int SignalRadius { get; init; } = 1;
    public float CapacityEmission { get; init; }
    public float SupportEmission { get; init; }
    public float ThreatEmission { get; init; }
    public float BaseAttractorMass { get; init; } = 1f;
    public float CoverageAttractorScale { get; init; }
    public float InfluenceAttractorScale { get; init; }
    public float ActivityAttractorScale { get; init; }
    public float HealthAttractorScale { get; init; }
    public int AttractorRadius { get; init; } = 1;
}

public sealed class RelationshipMapSignalConfig
{
    public required string RelationshipKind { get; init; }
    public int SignalRadius { get; init; } = 1;
    public float SupportEmission { get; init; }
    public float ThreatEmission { get; init; }
}

public sealed class EffectsConfig
{
    public IReadOnlyList<AttributeDeltaEffectConfig> AttributeDeltaEffects { get; init; } = [];
}

/// <summary>
/// Domain-authored per-tick attribute delta derived from live graph/map evidence.
/// This is compiled to primitive ids and evaluated by the kernel effects program.
/// </summary>
public sealed class AttributeDeltaEffectConfig
{
    public required EntityKind Kind { get; init; }
    public required string Attribute { get; init; }
    public required EffectCondition Condition { get; init; }
    public string? RelationshipKind { get; init; }
    public EffectRelationshipDirection RelationshipDirection { get; init; } =
        EffectRelationshipDirection.Either;
    public PrimaryPlane Plane { get; init; } = PrimaryPlane.Physical;
    public int MaxDistance { get; init; }
    public float Threshold { get; init; }
    public float MinimumRelationshipStrength { get; init; } = 0.1f;
    public required float DeltaPerTick { get; init; }
}

public enum EffectCondition
{
    RelatedInfluenceAtPhysicalLocation,
    RelatedEntityDistance,
    MapSupportAtLocation,
    MapThreatAtLocation,
    MapCrowdingPressure,
    MapCrowdingComfort,
}

public enum EffectRelationshipDirection
{
    Outgoing,
    Incoming,
    Either,
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
