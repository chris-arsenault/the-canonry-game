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

    /// <summary>
    /// Step-cost factors movement profiles pay to cross directional edge
    /// classes (river, ridge, cliff). Crossings only surcharge - factors
    /// below 1 are clamped - and a road-negated class has its surcharge
    /// erased by road quality (bridges emerge from travel).
    /// </summary>
    public IReadOnlyList<CrossingProfileConfig> CrossingProfiles { get; init; } = [];

    /// <summary>Rules for persistent non-graph actors caused by named entities or campaign edges.</summary>
    public IReadOnlyList<MinorActorRuleConfig> MinorActors { get; init; } = [];

    public MinorActorCampaignConfig? MinorActorCampaign { get; init; }

    public MapFrontierConfig? MapFrontier { get; init; }

    /// <summary>Relationship kind whose holder is a laden carrier (road-speed bonus).</summary>
    public string? CarrierRelationshipKind { get; init; }

    /// <summary>
    /// Step-cost multiplier per unit of hex roughness: broken ground within
    /// a terrain is slower than smooth ground of the same terrain. Zero
    /// disables the modifier.
    /// </summary>
    public float MovementRoughnessCostWeight { get; init; }

    /// <summary>Discrete movement capability classes used by route planning and terrain traversal.</summary>
    public IReadOnlyList<MovementProfileConfig> MovementProfiles { get; init; } = [];

    /// <summary>Culture definitions.</summary>
    public required IReadOnlyList<CultureConfig> Cultures { get; init; }

    /// <summary>
    /// Stable name of the neutral world culture. Produced entities with
    /// <see cref="ProducedCultureSource.World"/> belong to it: they
    /// participate in the simulation without extending any contending
    /// culture's footprint. Required when any generator uses World.
    /// </summary>
    public string? WorldCulture { get; init; }

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
    /// Signed placement taste weights over the geography axes: this kind's
    /// functional relationship to the land, stated once and applied to
    /// every placement of the kind.
    /// </summary>
    public IReadOnlyList<PlacementTasteWeightConfig> PlacementTaste { get; init; } = [];

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
    public required int TargetTicks { get; init; }
    public float FlatDrainShare { get; init; } = 0.2f;
}

/// <summary>One signed placement taste weight on a named geography axis.</summary>
public sealed class PlacementTasteWeightConfig
{
    /// <summary>Axis name; see <see cref="PlacementTasteAxisNames"/>.</summary>
    public required string Axis { get; init; }

    public required float Weight { get; init; }
}

/// <summary>
/// The canonical placement-taste axis vocabulary: the one mapping from
/// authored axis names to kernel axis codes, shared by domain authoring,
/// compilation, and seed-time scoring.
/// </summary>
public static class PlacementTasteAxisNames
{
    public const string Capacity = "capacity";
    public const string Temperature = "temperature";
    public const string Moisture = "moisture";
    public const string Elevation = "elevation";
    public const string Relief = "relief";
    public const string Ice = "ice";
    public const string River = "river";
    public const string Fire = "fire";
    public const string Aurora = "aurora";
    public const string Pressure = "pressure";
    public const string Water = "water";
    public const string Coast = "coast";

    private static readonly Dictionary<string, int> Codes =
        new(StringComparer.Ordinal)
        {
            [Capacity] = PlacementTasteAxisCodes.Capacity,
            [Temperature] = PlacementTasteAxisCodes.Temperature,
            [Moisture] = PlacementTasteAxisCodes.Moisture,
            [Elevation] = PlacementTasteAxisCodes.Elevation,
            [Relief] = PlacementTasteAxisCodes.Relief,
            [Ice] = PlacementTasteAxisCodes.IceThickness,
            [River] = PlacementTasteAxisCodes.RiverCoverage,
            [Fire] = PlacementTasteAxisCodes.FireMean,
            [Aurora] = PlacementTasteAxisCodes.AuroraMean,
            [Pressure] = PlacementTasteAxisCodes.PressureMean,
            [Water] = PlacementTasteAxisCodes.WaterFraction,
            [Coast] = PlacementTasteAxisCodes.CoastAdjacency,
        };

    public static int CodeOf(string axis) =>
        Codes.TryGetValue(axis, out var code)
            ? code
            : throw new InvalidOperationException($"Unknown placement taste axis '{axis}'.");
}

public sealed class TerrainConfig
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string BoundaryGroup { get; init; } = "default";

    /// <summary>
    /// Whether this terrain is open water. Authored repaints never cross the
    /// land/water divide, coast adjacency derives from it, and water-family
    /// consumer sets are built from it instead of hand lists.
    /// </summary>
    public bool IsWater { get; init; }
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

public sealed class CrossingProfileConfig
{
    /// <summary>An Engine.Constants.PhysicalCrossingCodes class (River, Ridge, Cliff).</summary>
    public required int CrossingClass { get; init; }
    public required string Profile { get; init; }
    public required float Factor { get; init; }
    public bool RoadNegated { get; init; }
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

    /// <summary>Reveal radius for kinds without an authored override.</summary>
    public int BaseRevealRadius { get; init; } = 1;

    /// <summary>Per-kind reveal radius overrides (ranging kinds see farther).</summary>
    public IReadOnlyList<KindRevealRadiusConfig> KindRevealRadii { get; init; } = [];

    /// <summary>Byte-scale temperature under which ground can hold cold pressure.</summary>
    public int FrostTemperatureCeiling { get; init; } = 64;

    /// <summary>Pressure added per byte of resident ice thickness.</summary>
    public float IceThicknessPressureWeight { get; init; } = 0.25f;

    /// <summary>Pressure relieved per byte of geothermal heat.</summary>
    public float GeothermalRefugeWeight { get; init; } = 0.5f;

    /// <summary>Pressure scalar while a freeze age is open.</summary>
    public float FreezeAgePressureScalar { get; init; } = 1f;

    /// <summary>Pressure scalar while a thaw age is open.</summary>
    public float ThawAgePressureScalar { get; init; } = 0.5f;

    /// <summary>Resistance every known cell holds against the ice.</summary>
    public float BaseIceResistance { get; init; } = 8f;

    /// <summary>Resistance per point of strongest-culture raw mass.</summary>
    public float CultureMassResistanceScale { get; init; } = 4f;

    /// <summary>Resistance per road crossing the cell.</summary>
    public float RoadIceResistance { get; init; } = 40f;

    /// <summary>Maximum cells the ice covers per advance run.</summary>
    public int IceAdvanceBudgetPerRun { get; init; } = 24;

    /// <summary>Maximum covered cells melted per retreat run.</summary>
    public int IceRetreatBudgetPerRun { get; init; } = 24;

    /// <summary>
    /// Sea-ice cover breathing with the same ages: freeze ages extend this
    /// cover over the eligible water terrains from coasts and existing ice;
    /// thaw ages melt it back from the fringe. Null disables modulation.
    /// </summary>
    public string? SeaIceCoverTerrain { get; init; }
    public IReadOnlyList<string> SeaIceEligibleTerrains { get; init; } = [];
    public int SeaIceFreezeBudgetPerRun { get; init; } = 12;
    public int SeaIceMeltBudgetPerRun { get; init; } = 12;

    /// <summary>
    /// Enables continuous semantic growth from the installed latent terrain
    /// field. Ideas have no ice, so semantic growth has no age gate and never
    /// re-freezes.
    /// </summary>
    public bool SemanticGrowthEnabled { get; init; }
    public int SemanticMaxRadius { get; init; } = 40;
    public float SemanticFrontierMassThreshold { get; init; } = 30f;
    public int SemanticBudgetPerRun { get; init; } = 24;
}

public sealed class KindRevealRadiusConfig
{
    public required EntityKind Kind { get; init; }
    public required int Radius { get; init; }
}

public sealed class MinorActorRuleConfig
{
    public required EntityKind SourceKind { get; init; }
    public string? SourceSubtype { get; init; }
    public int RoleCode { get; init; } = MinorActorRoleCodes.CulturalFront;
    public int PlaneCode { get; init; } = PlaneCodes.Physical;
    public int AssignmentRadius { get; init; } = 5;
    public int Stride { get; init; } = 2;
    public int CommitmentTicks { get; init; } = 8;
    public int ReplanTicks { get; init; } = 12;
    public float BaseStrength { get; init; } = 1f;
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
    /// Signed placement taste weights over the geography axes: the
    /// culture's identity as a relationship to the world, stated once and
    /// added to every placement made under this culture.
    /// </summary>
    public IReadOnlyList<PlacementTasteWeightConfig> PlacementTaste { get; init; } = [];

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
