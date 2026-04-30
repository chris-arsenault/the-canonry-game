namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Processes;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using Fact = IceRemembers.IceRemembersStructuralFact;
using K = IceRemembers.IceRemembersKinds;
using Press = IceRemembers.IceRemembersPressure;
using Rel = IceRemembers.IceRemembersRelationship;
using RelFact = IceRemembers.IceRemembersRelationshipFact;
using Res = IceRemembers.IceRemembersResource;
using Terrain = IceRemembers.IceRemembersTerrain;
using Verb = IceRemembers.IceRemembersVerbId;

/// <summary>
/// The Ice Remembers domain definition. This provides the engine with the
/// world's canonical kinds, relationships, resources, cultures, and semantics.
/// </summary>
public static partial class IceRemembersDefinition
{
    private const float HealthDefault = 90f;
    private const float HealthDefaultVariance = 1f / 9f;
    private const float NonHealthDefault = 50f;
    private const float NonHealthDefaultVariance = 0.4f;
    private const float NominalInitialHealth = 90f;
    private const float LifecycleFlatDrainShare = 0.2f;

    private enum TargetLifespan
    {
        Short,
        Mid,
        High,
    }

    public static DomainConfig Create()
    {
        var kinds = CreateKinds();
        var semantics = IceRemembersSemanticDefinition.Create();
        return new DomainConfig
        {
            Kinds = kinds,
            Processes = [],
            Relationships = CreateRelationships(),
            RelationshipFacts = CreateRelationshipFacts(),
            Pressures = CreatePressures(),
            Degeneracy = IceRemembersDegeneracyDefinition.Create(),
            Lifecycles = CreateLifecycles(),
            Resources = CreateResources(),
            Terrains = IceRemembersTerrainCatalog.DomainTerrains,
            Cultures = CreateCultures(),
            Gameplay = CreateGameplayDefinition(),
            Semantics = semantics,
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string C(Culture culture) => culture.ToDomainName();
    private static string F(Fact fact) => fact.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string LF(RelFact fact) => fact.ToDomainName();
    private static string P(Press pressure) => pressure.ToDomainName();
    private static string R(Res resource) => resource.ToDomainName();
    private static string V(Verb verb) => verb.ToVerbId();
    private static string TerrainName(Terrain terrain) => terrain.ToDomainName();

    private static Dictionary<EntityKind, KindConfig> CreateKinds() =>
        new()
        {
            [K.Region] = CreateRegionKind(),
            [K.Settlement] = CreateSettlementKind(),
            [K.Faction] = CreateFactionKind(),
            [K.Penguin] = CreatePenguinKind(),
            [K.Artifact] = CreateArtifactKind(),
        };

    private static GameplayDefinition CreateGameplayDefinition() =>
        new()
        {
            Modules =
            [
                new GameplayModule("low_altitude"),
                new GameplayModule("map_control"),
                new GameplayModule("map_signals"),
                new GameplayModule("attribute_effects"),
            ],
            LowAltitudeVerbs = CreateLowAltitudeVerbs(),
            SemanticPlane = new SemanticPlaneConfig
            {
                MinQ = -24,
                MaxQ = 24,
                MinR = -24,
                MaxR = 24,
            },
            InitialInfluence = CreateInitialInfluenceRules(),
            Effects = CreateEffects(),
            MapSignals = CreateMapSignals(),
            MapControl =
            [
                new MapControlDefinition(
                    "ice_remembers_influence",
                    Influence: "cell_influence",
                    Coverage: "named_coverage",
                    Territory: "physical_and_semantic_control"),
            ],
        };

    private static IReadOnlyList<InitialInfluenceRule> CreateInitialInfluenceRules() =>
    [
        InitialInfluence(K.Region, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Settlement, PrimaryPlane.Physical, 35f),
        InitialInfluence(K.Settlement, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Faction, PrimaryPlane.Physical, 35f),
        InitialInfluence(K.Faction, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Penguin, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Artifact, PrimaryPlane.Semantic, 35f),
    ];

    private static InitialInfluenceRule InitialInfluence(EntityKind kind, PrimaryPlane plane, float value) =>
        new(kind.Value, GameplayPlaneName(plane), value);

    private static KindConfig CreateRegionKind() =>
        new()
        {
            Category = EntityCategory.Spatial,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "face", "shelf", "depths", "reach", "sea", "scar" },
            ResourceRecoveryHosts =
            [
                new RecoveryHostQueryConfig
                {
                    AnyOfKinds = new HashSet<EntityKind> { K.Settlement, K.Artifact },
                },
            ],
            Attributes =
            [
                HealthAttribute(Attr.Stability, "Stability"),
                NonHealthAttribute(Attr.Holding, "Holding", AttributeCategory.Capacity),
                NonHealthAttribute(Attr.Pristiness, "Pristiness", AttributeCategory.Need),
            ],
            HealthAttribute = A(Attr.Stability),
            Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself — the land's own tendencies — distinct from the people, factions, or settlements that happen to occupy it.",
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateSettlementKind() =>
        new()
        {
            Category = EntityCategory.Spatial,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "rookery", "colony", "holdfast", "sanctuary", "harbor", "reefhold" },
            ResourceRecoveryHosts =
            [
                new RecoveryHostQueryConfig
                {
                    AnyOfKinds = new HashSet<EntityKind> { K.Region, K.Artifact },
                },
            ],
            Attributes =
            [
                HealthAttribute(Attr.TempHp, "Temp HP"),
                NonHealthAttribute(Attr.Dominion, "Dominion", AttributeCategory.Projection),
                NonHealthAttribute(Attr.Shelter, "Shelter", AttributeCategory.Need),
            ],
            HealthAttribute = A(Attr.TempHp),
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateFactionKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "order", "compact", "cell", "choir" },
            ResourceRecoveryHosts =
            [
                new RecoveryHostQueryConfig
                {
                    AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                },
            ],
            Attributes = CreateFactionAttributes(),
            HealthAttribute = A(Attr.Cohesion),
            StandingDrives =
            [
                new StandingDriveConfig
                {
                    DesiredEffect = AttributeRef.StructuralFact(F(Fact.TerritorialOrder), K.Faction),
                    BaseUrgency = 0.035f,
                },
            ],
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static IReadOnlyList<AttributeConfig> CreateFactionAttributes() =>
    [
        HealthAttribute(Attr.Cohesion, "Cohesion"),
        NonHealthAttribute(Attr.Reach, "Reach", AttributeCategory.Projection),
        new()
        {
            Name = A(Attr.Provisions),
            DisplayName = "Provisions",
            CultureDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [C(Culture.AuroraStack)] = "Aurora Light",
                [C(Culture.Nightshelf)] = "Fire-Cores",
                [C(Culture.WakeSingers)] = "Offerings",
            },
            Category = AttributeCategory.Need,
            Min = 0f,
            Max = 100f,
            Default = NonHealthDefault,
            DefaultVariance = NonHealthDefaultVariance,
        },
    ];

    private static KindConfig CreatePenguinKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "warden", "broker", "witness", "exile" },
            ResourceRecoveryHosts =
            [
                new RecoveryHostQueryConfig
                {
                    AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                },
            ],
            Attributes = CreatePenguinAttributes(),
            HealthAttribute = A(Attr.Vitality),
            StandingDrives =
            [
                new StandingDriveConfig
                {
                    DesiredEffect = AttributeRef.StructuralFact(F(Fact.Roam), K.Penguin),
                    BaseUrgency = 0.03f,
                },
            ],
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static IReadOnlyList<AttributeConfig> CreatePenguinAttributes() =>
    [
        HealthAttribute(Attr.Vitality, "Vitality"),
        new()
        {
            Name = A(Attr.Echo),
            DisplayName = "Echo",
            CultureDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [C(Culture.AuroraStack)] = "Witness",
                [C(Culture.Nightshelf)] = "Echo",
                [C(Culture.WakeSingers)] = "History",
            },
            Category = AttributeCategory.Capacity,
            Min = 0f,
            Max = 100f,
            Default = NonHealthDefault,
            DefaultVariance = NonHealthDefaultVariance,
        },
        new()
        {
            Name = A(Attr.Warmth),
            DisplayName = "Warmth",
            Category = AttributeCategory.Need,
            Min = 0f,
            Max = 100f,
            Default = NonHealthDefault,
            DefaultVariance = NonHealthDefaultVariance,
        },
    ];

    private static KindConfig CreateArtifactKind() =>
        new()
        {
            Category = EntityCategory.Asset,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "relic", "instrument", "weapon", "tome", "seal" },
            ResourceRecoveryHosts =
            [
                new RecoveryHostQueryConfig
                {
                    AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                },
            ],
            Attributes = CreateArtifactAttributes(),
            HealthAttribute = A(Attr.Condition),
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static IReadOnlyList<AttributeConfig> CreateArtifactAttributes() =>
    [
        HealthAttribute(Attr.Condition, "Condition"),
        new()
        {
            Name = A(Attr.Power),
            DisplayName = "Power",
            CultureDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [C(Culture.AuroraStack)] = "Light",
                [C(Culture.Nightshelf)] = "Heat",
                [C(Culture.WakeSingers)] = "Pressure",
            },
            Category = AttributeCategory.Capacity,
            Min = 0f,
            Max = 100f,
            Default = NonHealthDefault,
            DefaultVariance = NonHealthDefaultVariance,
        },
        NonHealthAttribute(Attr.Resonance, "Resonance", AttributeCategory.Drive),
    ];

    private static AttributeConfig HealthAttribute(
        Attr attribute,
        string displayName) =>
        new()
        {
            Name = A(attribute),
            DisplayName = displayName,
            Category = AttributeCategory.Health,
            Min = 0f,
            Max = 100f,
            Default = HealthDefault,
            DefaultVariance = HealthDefaultVariance,
        };

    private static AttributeConfig NonHealthAttribute(
        Attr attribute,
        string displayName,
        AttributeCategory category) =>
        new()
        {
            Name = A(attribute),
            DisplayName = displayName,
            Category = category,
            Min = 0f,
            Max = 100f,
            Default = NonHealthDefault,
            DefaultVariance = NonHealthDefaultVariance,
        };

    private static List<RelationshipConfig> CreateRelationships()
    {
        var spatial = new HashSet<EntityCategory> { EntityCategory.Spatial };
        var agency = new HashSet<EntityCategory> { EntityCategory.Agency };
        var asset = new HashSet<EntityCategory> { EntityCategory.Asset };
        var spatialOrAsset = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Asset };
        var agencyOrSpatialOrAsset = new HashSet<EntityCategory> { EntityCategory.Agency, EntityCategory.Spatial, EntityCategory.Asset };

        return
        [
            new() { Name = L(Rel.ControlledBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true, Maintenance = ControlledByMaintenance() },
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true, Maintenance = HomeBaseMaintenance() },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyConflictMaintenance() },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset, Maintenance = ContactBondMaintenance() },
            new() { Name = L(Rel.CreatedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = L(Rel.Memorializes), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.ClaimedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agency, Maintenance = ControlledByMaintenance() },
            new() { Name = L(Rel.TradeRoute), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true, CullThreshold = 0.05f, Maintenance = SpatialBondMaintenance() },
            new() { Name = L(Rel.FoundedFrom), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = L(Rel.NativeTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatial },
            new() { Name = L(Rel.Anchored), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true, Maintenance = ContactBondMaintenance() },
            new() { Name = L(Rel.ConduitOf), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatialOrAsset, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.PartOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = asset },
            new() { Name = L(Rel.WardAgainst), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = asset, ValidTargetCategories = asset, CullThreshold = 0.05f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.CorruptedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.CleansedBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.AtWarWith), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.02f, Maintenance = AgencyConflictMaintenance() },
            new() { Name = L(Rel.BetrayedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, Maintenance = AgencyConflictMaintenance() },
        ];
    }

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> ControlledByMaintenance() =>
    [
        AlwaysDecay(0.0004f),
        TargetInfluenceAtSource(55f, 0.0018f),
        ContactMemory(12f, 0.0008f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> HomeBaseMaintenance() =>
    [
        AlwaysDecay(0.0005f),
        Distance(PrimaryPlane.Physical, 2, 0.0012f),
        ContactMemory(10f, 0.0012f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> AgencyBondMaintenance() =>
    [
        AlwaysDecay(0.0006f),
        SameCulture(0.0004f),
        Distance(PrimaryPlane.Physical, 3, 0.0007f),
        ContactMemory(10f, 0.0011f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> AgencyConflictMaintenance() =>
    [
        AlwaysDecay(0.0003f),
        DifferentCulture(0.0003f),
        Distance(PrimaryPlane.Physical, 3, 0.0006f),
        ContactMemory(8f, 0.0012f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> ContactBondMaintenance() =>
    [
        AlwaysDecay(0.0005f),
        Distance(PrimaryPlane.Physical, 2, 0.001f),
        ContactMemory(10f, 0.0013f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> SpatialBondMaintenance() =>
    [
        AlwaysDecay(0.0004f),
        Distance(PrimaryPlane.Physical, 6, 0.0008f),
        SourceInfluenceAtTarget(50f, 0.001f),
        TargetInfluenceAtSource(50f, 0.001f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> SemanticBondMaintenance() =>
    [
        AlwaysDecay(0.0003f),
        Distance(PrimaryPlane.Semantic, 2, 0.0008f),
        ContactMemory(12f, 0.0008f),
    ];

    private static RelationshipMaintenanceRuleConfig AlwaysDecay(float amount) =>
        new() { Condition = RelationshipMaintenanceCondition.Always, Delta = -amount };

    private static RelationshipMaintenanceRuleConfig SameCulture(float delta) =>
        new() { Condition = RelationshipMaintenanceCondition.SameCulture, Delta = delta };

    private static RelationshipMaintenanceRuleConfig DifferentCulture(float delta) =>
        new() { Condition = RelationshipMaintenanceCondition.DifferentCulture, Delta = delta };

    private static RelationshipMaintenanceRuleConfig Distance(PrimaryPlane plane, int maxDistance, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.DistanceWithin,
            Plane = GameplayPlaneName(plane),
            MaxDistance = maxDistance,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig ContactMemory(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.ContactMemory,
            Plane = GameplayPlaneName(PrimaryPlane.Physical),
            Threshold = threshold,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig SourceInfluenceAtTarget(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.SourceInfluenceAtTarget,
            Plane = GameplayPlaneName(PrimaryPlane.Physical),
            Threshold = threshold,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig TargetInfluenceAtSource(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.TargetInfluenceAtSource,
            Plane = GameplayPlaneName(PrimaryPlane.Physical),
            Threshold = threshold,
            Delta = delta,
        };

    private static List<RelationshipFactConfig> CreateRelationshipFacts() =>
    [
        new()
        {
            Name = LF(RelFact.CalledUnhuddled),
            DisplayName = "Called Unhuddled",
            Description = "This relationship was created when an unhuddled refuge gathered around a penguin.",
        },
        new()
        {
            Name = LF(RelFact.SealedThinPlace),
            DisplayName = "Sealed Thin Place",
            Description = "This relationship participates in a faction's sacrifice to contain a thin place.",
        },
        new()
        {
            Name = LF(RelFact.OpenedOldHolding),
            DisplayName = "Opened Old Holding",
            Description = "This relationship marks a region yielding something it had held.",
        },
        new()
        {
            Name = LF(RelFact.CrackedBerg),
            DisplayName = "Cracked Berg",
            Description = "This relationship marks an artifact rupturing the Berg into a scar.",
        },
        new()
        {
            Name = LF(RelFact.NamedOffice),
            DisplayName = "Named Office",
            Description = "This relationship marks authority hardening around a named office.",
        },
        new()
        {
            Name = LF(RelFact.RaisedReefhold),
            DisplayName = "Raised Reefhold",
            Description = "This relationship marks a harbor pushing a reefhold into named existence.",
        },
        new()
        {
            Name = LF(RelFact.YieldedTome),
            DisplayName = "Yielded Tome",
            Description = "This relationship marks a shelf yielding a tome and custody site.",
        },
    ];

    private static List<PressureConfig> CreatePressures()
    {
        return
        [
            new() { Name = P(Press.MilitaryTension), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.05f, SmoothingFactor = 0.2f },
            new() { Name = P(Press.EconomicStress), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.08f, SmoothingFactor = 0.25f },
            new() { Name = P(Press.CulturalFriction), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.06f, SmoothingFactor = 0.2f },
            new() { Name = P(Press.DemographicPressure), Scope = PressureScope.Regional, Baseline = 0f, HomeostasisRate = 0.1f, SmoothingFactor = 0.3f },
        ];
    }

    private static Dictionary<EntityKind, LifecycleConfig> CreateLifecycles()
    {
        return new Dictionary<EntityKind, LifecycleConfig>
        {
            [K.Settlement] = Lifecycle(Attr.TempHp, TargetLifespan.High),
            [K.Faction] = Lifecycle(Attr.Cohesion, TargetLifespan.Mid),
            [K.Penguin] = Lifecycle(Attr.Vitality, TargetLifespan.Short),
            [K.Artifact] = Lifecycle(Attr.Condition, TargetLifespan.High),
            [K.Region] = Lifecycle(Attr.Stability, TargetLifespan.Mid),
        };
    }

    private static LifecycleConfig Lifecycle(Attr healthAttribute, TargetLifespan lifespan)
    {
        var targetTicks = lifespan switch
        {
            TargetLifespan.Short => 500f,
            TargetLifespan.Mid => 800f,
            TargetLifespan.High => 1200f,
            _ => throw new ArgumentOutOfRangeException(nameof(lifespan), lifespan, null),
        };

        var baseRate = (NominalInitialHealth * LifecycleFlatDrainShare) / targetTicks;
        var ageCoefficient = (NominalInitialHealth * (1f - LifecycleFlatDrainShare) * 2f) / (targetTicks * targetTicks);
        return new LifecycleConfig
        {
            HealthAttribute = A(healthAttribute),
            BaseRate = baseRate,
            AgeCoefficient = ageCoefficient,
        };
    }

    private static EffectsConfig CreateEffects()
    {
        var effects = new List<AttributeDeltaEffectConfig>();
        AddHealthEffects(effects, K.Region, Attr.Stability, PrimaryPlane.Semantic);
        AddHealthEffects(effects, K.Settlement, Attr.TempHp, PrimaryPlane.Semantic);
        AddHealthEffects(effects, K.Faction, Attr.Cohesion, PrimaryPlane.Semantic);
        AddHealthEffects(effects, K.Penguin, Attr.Vitality, PrimaryPlane.Physical);
        AddHealthEffects(effects, K.Artifact, Attr.Condition, PrimaryPlane.Physical);
        return GameplayEffectSet.From(effects);
    }

    private static void AddHealthEffects(
        List<AttributeDeltaEffectConfig> effects,
        EntityKind kind,
        Attr attribute,
        PrimaryPlane plane)
    {
        effects.AddRange(
        [
            InfluenceShelter(kind, attribute, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.06f, threshold: 45f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.HomeBase, EffectRelationshipDirection.Outgoing, 0.05f, threshold: 40f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.Anchored, EffectRelationshipDirection.Outgoing, 0.05f, threshold: 40f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.ControlledBy, EffectRelationshipDirection.Either, 0.04f, threshold: 45f, plane: plane),
            InfluencePressure(kind, attribute, Rel.RivalOf, -0.07f, threshold: 35f, plane: plane),
            InfluencePressure(kind, attribute, Rel.AtWarWith, -0.10f, threshold: 35f, plane: plane),
            InfluencePressure(kind, attribute, Rel.CorruptedBy, -0.08f, threshold: 35f, plane: plane),
            InfluencePressure(kind, attribute, Rel.BetrayedBy, -0.06f, threshold: 35f, plane: plane),
            RelationshipSupport(kind, attribute, Rel.AlliedWith, 0.035f, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.CleansedBy, 0.025f, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.ConduitOf, 0.025f, maxDistance: 1, plane: plane),
            RelationshipStress(kind, attribute, Rel.RivalOf, -0.04f, maxDistance: 2, plane: plane),
            RelationshipStress(kind, attribute, Rel.AtWarWith, -0.07f, maxDistance: 3, plane: plane),
            RelationshipStress(kind, attribute, Rel.CorruptedBy, -0.05f, maxDistance: 2, plane: plane),
            RelationshipStress(kind, attribute, Rel.BetrayedBy, -0.045f, maxDistance: 1, plane: plane),
            RelationshipStress(kind, attribute, Rel.WardAgainst, -0.03f, maxDistance: 1, plane: plane),
            MapSignalEffect(kind, attribute, Effect.MapSupportAtLocation, 0.035f, threshold: 5f, plane: plane),
            MapSignalEffect(kind, attribute, Effect.MapCrowdingComfort, 0.025f, threshold: 0.5f, plane: plane),
            MapSignalEffect(kind, attribute, Effect.MapThreatAtLocation, -0.04f, threshold: 4f, plane: plane),
            MapSignalEffect(kind, attribute, Effect.MapCrowdingPressure, -0.08f, threshold: 0.5f, plane: plane),
        ]);
    }

    private static MapSignalsConfig CreateMapSignals() =>
        new()
        {
            SemanticBaseCapacity = 2f,
            CrowdingRadius = 1,
            Terrains =
            [
                TerrainSignal(Terrain.Wastes, 0.7f),
                TerrainSignal(Terrain.Crags, 0.8f),
                TerrainSignal(Terrain.Tundra, 2.0f),
                TerrainSignal(Terrain.Ice, 1.0f),
                TerrainSignal(Terrain.HotSprings, 4.0f),
                TerrainSignal(Terrain.Shelf, 1.3f),
                TerrainSignal(Terrain.Reef, 2.5f),
                TerrainSignal(Terrain.Ocean, 0.6f),
            ],
            Kinds =
            [
                KindSignal(K.Settlement, Attr.TempHp, radius: 2, capacity: 3.0f, support: 2.5f, threat: 0f,
                    baseMass: 12f, coverageScale: 0.35f, influenceScale: 0.12f, activityScale: 0.05f, healthScale: 0.08f, attractorRadius: 2),
                KindSignal(K.Faction, Attr.Cohesion, radius: 2, capacity: 1.0f, support: 1.2f, threat: 0f,
                    baseMass: 8f, coverageScale: 0.2f, influenceScale: 0.10f, activityScale: 0.04f, healthScale: 0.06f, attractorRadius: 2),
                KindSignal(K.Penguin, Attr.Vitality, radius: 1, capacity: 0.2f, support: 0.2f, threat: 0f,
                    baseMass: 2f, coverageScale: 0f, influenceScale: 0.02f, activityScale: 0.02f, healthScale: 0.02f, attractorRadius: 1),
                KindSignal(K.Artifact, Attr.Condition, radius: 1, capacity: 0.3f, support: 0.4f, threat: 0.2f,
                    baseMass: 10f, coverageScale: 0f, influenceScale: 0.04f, activityScale: 0.04f, healthScale: 0.08f, attractorRadius: 2),
                KindSignal(K.Region, Attr.Stability, radius: 2, capacity: 1.5f, support: 0.3f, threat: 0.3f,
                    baseMass: 6f, coverageScale: 0.10f, influenceScale: 0.03f, activityScale: 0.02f, healthScale: 0.03f, attractorRadius: 2),
            ],
            Relationships =
            [
                RelationshipSignal(Rel.AlliedWith, support: 2.0f, threat: 0f),
                RelationshipSignal(Rel.HomeBase, support: 2.5f, threat: 0f),
                RelationshipSignal(Rel.Anchored, support: 2.0f, threat: 0f),
                RelationshipSignal(Rel.ControlledBy, support: 1.2f, threat: 0f),
                RelationshipSignal(Rel.CleansedBy, support: 1.5f, threat: 0f),
                RelationshipSignal(Rel.ConduitOf, support: 1.2f, threat: 0f),
                RelationshipSignal(Rel.RivalOf, support: 0f, threat: 1.5f),
                RelationshipSignal(Rel.AtWarWith, support: 0f, threat: 3.0f),
                RelationshipSignal(Rel.CorruptedBy, support: 0f, threat: 2.0f),
                RelationshipSignal(Rel.BetrayedBy, support: 0f, threat: 1.5f),
                RelationshipSignal(Rel.WardAgainst, support: 0f, threat: 1.0f),
            ],
        };

    private static TerrainMapSignalConfig TerrainSignal(Terrain terrain, float baseCapacity) =>
        new()
        {
            Terrain = TerrainName(terrain),
            BaseCapacity = baseCapacity,
        };

    private static KindMapSignalConfig KindSignal(
        EntityKind kind,
        Attr healthAttribute,
        int radius,
        float capacity,
        float support,
        float threat,
        float baseMass,
        float coverageScale,
        float influenceScale,
        float activityScale,
        float healthScale,
        int attractorRadius) =>
        new()
        {
            Kind = kind.Value,
            SignalRadius = radius,
            CapacityEmission = capacity,
            SupportEmission = support,
            ThreatEmission = threat,
            BaseAttractorMass = baseMass,
            CoverageAttractorScale = coverageScale,
            InfluenceAttractorScale = influenceScale,
            ActivityAttractorScale = activityScale,
            AttractorRadius = attractorRadius,
            AttractorScalarInputs =
            [
                new MapSignalScalarInputConfig
                {
                    Attribute = A(healthAttribute),
                    AttractorScale = healthScale,
                },
            ],
        };

    private static RelationshipMapSignalConfig RelationshipSignal(Rel relationship, float support, float threat) =>
        new()
        {
            RelationshipKind = L(relationship),
            SignalRadius = 1,
            SupportEmission = support,
            ThreatEmission = threat,
        };

    private static AttributeDeltaEffectConfig InfluenceShelter(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        EffectRelationshipDirection direction,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.RelatedInfluenceAtLocation,
            RelationshipKind = L(relationship),
            RelationshipDirection = direction,
            Plane = GameplayPlaneName(plane),
            Threshold = threshold,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig InfluencePressure(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.RelatedInfluenceAtLocation,
            RelationshipKind = L(relationship),
            RelationshipDirection = EffectRelationshipDirection.Either,
            Plane = GameplayPlaneName(plane),
            Threshold = threshold,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig RelationshipSupport(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance,
        PrimaryPlane plane) =>
        RelationshipAttributeDeltaEffect(kind, attribute, relationship, deltaPerTick, maxDistance, plane);

    private static AttributeDeltaEffectConfig RelationshipStress(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance,
        PrimaryPlane plane) =>
        RelationshipAttributeDeltaEffect(kind, attribute, relationship, deltaPerTick, maxDistance, plane);

    private static AttributeDeltaEffectConfig MapSignalEffect(
        EntityKind kind,
        Attr attribute,
        EffectCondition condition,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = condition,
            Plane = GameplayPlaneName(plane),
            Threshold = threshold,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig RelationshipAttributeDeltaEffect(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.RelatedEntityDistance,
            RelationshipKind = L(relationship),
            RelationshipDirection = EffectRelationshipDirection.Either,
            Plane = GameplayPlaneName(plane),
            MaxDistance = maxDistance,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

    private static string GameplayPlaneName(PrimaryPlane plane) =>
        plane == PrimaryPlane.Semantic ? "semantic" : "physical";

    private static List<ResourceConfig> CreateResources()
    {
        return
        [
            new() { Name = R(Res.Food), DisplayName = "Food" },
            new() { Name = R(Res.Wealth), DisplayName = "Wealth" },
            new() { Name = R(Res.Population), DisplayName = "Population" },
        ];
    }

    private static List<CultureConfig> CreateCultures()
    {
        return
        [
            new() { Id = C(Culture.AuroraStack), DisplayName = "Aurora Stack" },
            new() { Id = C(Culture.Nightshelf), DisplayName = "Nightshelf" },
            new() { Id = C(Culture.WakeSingers), DisplayName = "Wake-Singers" },
        ];
    }
}
