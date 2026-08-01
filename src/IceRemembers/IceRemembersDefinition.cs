namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Kinds;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Verb = IceRemembers.IceRemembersVerbId;

/// <summary>
/// The Ice Remembers domain definition. This provides the engine with the
/// world's canonical kinds, relationships, cultures, and semantics.
/// </summary>
public static partial class IceRemembersDefinition
{
    private const float CultureSlotCultureInfluenceBenefitScale = 10f;

    public static DomainConfig Create()
    {
        var kinds = CreateKinds();
        var semantics = IceRemembersSemanticDefinition.Create();
        return new DomainConfig
        {
            Kinds = kinds,
            Gambits = CreateGambits(),
            Relationships = CreateRelationships(),
            Lifecycles = CreateLifecycles(),
            Terrains = IceRemembersTerrainCatalog.DomainTerrains,
            CrossingProfiles = IceRemembersTerrainCatalog.CrossingProfiles,
            // Broken ground slows travel within a terrain: rough crags are
            // worse than smooth crags, straight from the roughness fact.
            MovementRoughnessCostWeight = 0.3f,
            TravelStructures = IceRemembersTravelStructures.DomainTravelStructures,
            MinorActors = CreateMinorActorRules(),
            MinorActorCampaign = new MinorActorCampaignConfig { RelationshipKind = L(Rel.WagesCampaignAgainst) },
            CarrierRelationshipKind = L(Rel.Carries),
            MapFrontier = CreateMapFrontier(),
            MovementProfiles = IceRemembersMovementProfiles.DomainProfiles,
            Cultures = CreateCultures(),
            // Shared consequences - named wars, relics, broken lands - belong
            // to the ice itself, not to a contending culture.
            WorldCulture = C(Culture.TheIce),
            CulturePower = CreateCulturePowerDefinition(),
            Tags = ["hunted", "terrified", "bereaved", "fervent", "oathbreaker", "profaner"],
            Gameplay = CreateGameplayDefinition(),
            Semantics = semantics,
            Chronicle = CreateChronicleDefinition(),
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string C(Culture culture) => culture.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string V(Verb verb) => verb.ToVerbId();
    private static string G(GambitId gambit) => gambit.ToGambitId();
    private static string TerrainName(Terrain terrain) => terrain.ToDomainName();

    private static CulturePowerConfig CreateCulturePowerDefinition() =>
        new()
        {
            CustodyKinds = [K.Artifact.Value, K.Ability.Value],
            CustodyRelationships =
            [
                L(Rel.Holds),
                L(Rel.Anchored),
                L(Rel.PracticedBy),
                L(Rel.ConduitOf),
            ],
            ClaimRelationship = L(Rel.ClaimedBy),
            WarRelationship = L(Rel.AtWarWith),
            OfferingRelationships = [L(Rel.OfferedTo), L(Rel.ConsignedTo)],
        };

    private static ChronicleConfig CreateChronicleDefinition() =>
        new()
        {
            Queries =
            [
                new ChronicleQueryConfig
                {
                    Id = "artifact_cleansing_awakes_spring",
                    Parent = new ChronicleProducerRef
                    {
                        Kind = ChronicleProducerKind.Gambit,
                        StableName = G(GambitId.ArtifactCleansesScar),
                    },
                    TriggerRelationshipKind = L(Rel.CleansedBy),
                    Child = new ChronicleProducerRef
                    {
                        Kind = ChronicleProducerKind.Generator,
                        StableName = "what_the_spring_remembers",
                    },
                },
            ],
        };

    private static Dictionary<EntityKind, KindConfig> CreateKinds() =>
        IceRemembersEntities.CreateKindConfigs();

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
            InitialInfluence = CreateInitialInfluenceRules(),
            // What each artifact subtype becomes when it installs as a
            // district (docs/design/artifact-districts.md): relics and seals
            // shelter their keepers, instruments and tomes press the city's
            // voice, weapons ward the walls. Road districts wait on a road
            // application pass.
            DistrictBonuses =
            [
                new DistrictBonusConfig("relic", DistrictBonusKind.Healing, 0.003f),
                new DistrictBonusConfig("seal", DistrictBonusKind.Healing, 0.002f),
                new DistrictBonusConfig("instrument", DistrictBonusKind.Culture, 2.0f),
                new DistrictBonusConfig("tome", DistrictBonusKind.Culture, 1.5f),
                new DistrictBonusConfig("weapon", DistrictBonusKind.Healing, 0.002f),
            ],
            KindStages = CreateStageCatalogs(),
            Effects = CreateEffects(),
            Locality = CreateLocality(),
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
        IceRemembersEntities.CreateInitialInfluenceRules();

    // Named maturity ladders (docs/design/entity-stages.md). Band
    // multipliers stretch the kind's base band; worker bonus, ambient
    // emission, and district capacity step at promotions.
    private static IReadOnlyList<KindStagesConfig> CreateStageCatalogs() =>
    [
        new KindStagesConfig(K.Settlement.Value,
        [
            new StageConfig("town", "Town", 1f, 0, 0.05f, 1),
            new StageConfig("city", "City", 3f, 3, 0.15f, 3),
            new StageConfig("capital", "Capital", 6f, 6, 0.4f, 5),
        ]),
        new KindStagesConfig(K.Faction.Value,
        [
            new StageConfig("warband", "Warband", 1f, 0, 0.02f, 0),
            new StageConfig("order", "Order", 2f, 2, 0.06f, 0),
            new StageConfig("dynasty", "Dynasty", 4f, 4, 0.15f, 0),
        ]),
        new KindStagesConfig(K.Artifact.Value,
        [
            new StageConfig("trinket", "Trinket", 0.3f, 0, 0f, 0),
            new StageConfig("famed", "Famed Relic", 1f, 0, 0f, 0),
            new StageConfig("legend", "Legend", 10f, 0, 0.05f, 0),
        ]),
        new KindStagesConfig(K.Canon.Value,
        [
            new StageConfig("edict", "Edict", 0.5f, 0, 0.02f, 0),
            new StageConfig("living_word", "Living Word", 1f, 0, 0.05f, 0),
            new StageConfig("doctrine", "Doctrine", 3f, 0, 0.1f, 0),
        ]),
    ];

    private static MapFrontierConfig CreateMapFrontier() => new()
    {
        // The world opens during the Thaw (and, dramatically, the Great Thaw)
        // and is reclaimed in the Long Dark (and the catastrophic Great Freeze).
        ThawAge = "age_the_thaw",
        RefreezeAge = "age_the_long_dark",
        AdditionalThawAges = ["age_the_great_thaw"],
        AdditionalRefreezeAges = ["age_the_great_freeze"],
        FrozenTerrain = Terrain.DeepIce.ToDomainName(),
        MaxRadius = 128,
        // Fog of war retreats at entity speed: everyone reveals the ground
        // around them on arrival; ranging kinds carry the light farther,
        // and ships farthest of all.
        BaseRevealRadius = 1,
        KindRevealRadii =
        [
            new KindRevealRadiusConfig { Kind = K.Penguin, Radius = 2 },
            new KindRevealRadiusConfig { Kind = K.Settlement, Radius = 2 },
            new KindRevealRadiusConfig { Kind = K.Orca, Radius = 3 },
            new KindRevealRadiusConfig { Kind = K.Transport, Radius = 3 },
        ],
        // The ice is pressure, not a mass ledger: cold, ice-thick ground
        // freezes first and melts last; roads, culture, and warm refuges
        // hold it off. It only moves while its ages hold the Ice track.
        FrostTemperatureCeiling = 64,
        IceThicknessPressureWeight = 0.25f,
        GeothermalRefugeWeight = 0.5f,
        FreezeAgePressureScalar = 1f,
        ThawAgePressureScalar = 0.5f,
        BaseIceResistance = 8f,
        CultureMassResistanceScale = 4f,
        RoadIceResistance = 40f,
        IceAdvanceBudgetPerRun = 16,
        IceRetreatBudgetPerRun = 48,
        // The frozen sea breathes with the same ages: a Long Dark closes
        // harbors and sea lanes while opening slow walking routes over the
        // ice; a Thaw reopens the water from its fringes.
        SeaIceCoverTerrain = Terrain.SeaIce.ToDomainName(),
        SeaIceEligibleTerrains =
        [
            Terrain.Ocean.ToDomainName(),
            Terrain.Shelf.ToDomainName(),
            Terrain.Reef.ToDomainName(),
            Terrain.Fjord.ToDomainName(),
            Terrain.Lake.ToDomainName(),
        ],
        SeaIceFreezeBudgetPerRun = 12,
        SeaIceMeltBudgetPerRun = 12,
        // Idea-space is born as a heart and reveals the varied latent semantic
        // field wherever culture's idea-mass presses its frontier.
        SemanticGrowthEnabled = true,
        SemanticMaxRadius = 128,
        SemanticFrontierMassThreshold = 25f,
        SemanticBudgetPerRun = 48,
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
            new() { Name = L(Rel.ControlledBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true, Maintenance = ControlledByMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true, IsAffiliation = true, IsCultureAnchor = true },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f, Maintenance = AgencyBondMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyConflictMaintenance() },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true, Maintenance = AgencyBondMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, Maintenance = AgencyBondMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset, TargetFollowsSourcePhysical = true, IsAffiliation = true },
            new() { Name = L(Rel.CreatedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = L(Rel.Memorializes), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.ClaimedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agency, Maintenance = ControlledByMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.TradeRoute), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true, CullThreshold = 0.05f, Maintenance = SpatialBondMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.FoundedFrom), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = L(Rel.NativeTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsAffiliation = true },
            new() { Name = L(Rel.Anchored), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true, Maintenance = ContactBondMaintenance(), IsAffiliation = true },
            new() { Name = L(Rel.ConduitOf), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatialOrAsset, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.PartOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = asset, IsAffiliation = true },
            new() { Name = L(Rel.WardAgainst), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = asset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.05f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.CorruptedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.CleansedBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f, Maintenance = SemanticBondMaintenance() },
            new() { Name = L(Rel.AtWarWith), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.02f, Maintenance = AgencyConflictMaintenance() },
            // Grudges fade unless re-pressed: betrayal memory decays to cull like
            // rivalry does, instead of accreting forever.
            new() { Name = L(Rel.BetrayedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyConflictMaintenance() },
            new() { Name = L(Rel.PracticedBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AbilityPracticeMaintenance() },
            new() { Name = L(Rel.TaughtBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.OfferedTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = spatial, CullThreshold = 0.02f, Maintenance = OfferingMaintenance() },
            new() { Name = L(Rel.PreysOn), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = spatialOrAsset, CullThreshold = 0.03f, Maintenance = PredationMaintenance() },
            new() { Name = L(Rel.ConsignedTo), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true, CullThreshold = 0.02f, Maintenance = SemanticBondMaintenance() },
            // A campaign is a committed push, not a standing hatred: it decays
            // toward its cull threshold unless the war keeps feeding it, so
            // columns march for seasons, not forever.
            new() { Name = L(Rel.WagesCampaignAgainst), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agencyOrSpatialOrAsset, IsExclusive = true, CullThreshold = 0.15f, Maintenance = CampaignMaintenance() },
            // A carried passenger follows the transport bodily; the tie is
            // exclusive (one load at a time) and set down explicitly, so it
            // has no drift maintenance - load and deposit gambits own its life.
            new() { Name = L(Rel.Carries), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agencyOrSpatialOrAsset, IsExclusive = true, TargetFollowsSourcePhysical = true, CullThreshold = 0.02f },
        ];
    }

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> CampaignMaintenance() =>
    [
        AlwaysDecay(0.0016f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> ControlledByMaintenance() =>
    [
        AlwaysDecay(0.0004f),
        TargetInfluenceAtSource(55f, 0.0018f),
        ContactMemory(12f, 0.0008f),
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

    /// <summary>
    /// Offerings fade unless renewed: worship is a flow, not a purchase. The
    /// decay is what makes sacred sites ebb when a cult goes quiet.
    /// </summary>
    /// <summary>
    /// Predation ebbs unless the hunt is pressed: the ambient conflict
    /// substrate that ordinary raiding renews and settlements resist.
    /// </summary>
    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> PredationMaintenance() =>
    [
        AlwaysDecay(0.0009f),
        ContactMemory(8f, 0.0012f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> OfferingMaintenance() =>
    [
        // Renewal above decay: a devotee who keeps returning to the site
        // deepens the offering (+0.0006 net in contact), a third of the
        // growth rate tended friendships get; abandoned offerings fade at
        // the full decay. Previously renewal exactly equaled decay, so
        // worship could never climb and everything downstream of offering
        // strength starved (the track fidelity pass: devotion at 11.6% of
        // authorship, 1.8% of entities).
        AlwaysDecay(0.0010f),
        ContactMemory(10f, 0.0016f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> SemanticBondMaintenance() =>
    [
        AlwaysDecay(0.0003f),
        Distance(PrimaryPlane.Semantic, 2, 0.0008f),
        ContactMemory(12f, 0.0008f),
    ];

    private static IReadOnlyList<RelationshipMaintenanceRuleConfig> AbilityPracticeMaintenance() =>
    [
        AlwaysDecay(0.0006f),
        SameCulture(0.0003f),
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

    private static Dictionary<EntityKind, LifecycleConfig> CreateLifecycles() =>
        IceRemembersEntities.CreateLifecycleConfigs();

    private static EffectsConfig CreateEffects()
    {
        var effects = new List<AttributeDeltaEffectConfig>();
        foreach (var entity in IceRemembersEntities.All)
        {
            AddHealthEffects(
                effects,
                entity.Kind,
                entity.HealthAttribute,
                entity.PrimaryPlane,
                IceRemembersEntities.HealthEffectTimeScale(entity.LifecycleBand));
        }


        return GameplayEffectSet.From(
            effects,
            [
                new RelationshipTagRuleConfig
                {
                    Kind = K.Faction.Value,
                    Tag = "oathbreaker",
                    RelationshipKind = L(Rel.AtWarWith),
                    RelationshipDirection = EffectRelationshipDirection.Outgoing,
                    SetAtOrAbove = 0.3f,
                    ClearBelow = 0.15f,
                    InsideCoverageOwnerKind = K.Canon.Value,
                    InsideCoverageOwnerSubtype = "accord",
                    InsideCoveragePlane = "semantic",
                },
                new RelationshipTagRuleConfig
                {
                    Kind = K.Faction.Value,
                    Tag = "profaner",
                    RelationshipKind = L(Rel.OfferedTo),
                    RelationshipDirection = EffectRelationshipDirection.Outgoing,
                    SetAtOrAbove = 0.3f,
                    ClearBelow = 0.15f,
                    InsideCoverageOwnerKind = K.Canon.Value,
                    InsideCoverageOwnerSubtype = "taboo",
                    InsideCoveragePlane = "semantic",
                },
                new RelationshipTagRuleConfig
                {
                    Kind = K.Faction.Value,
                    Tag = "fervent",
                    RelationshipKind = L(Rel.OfferedTo),
                    RelationshipDirection = EffectRelationshipDirection.Outgoing,
                    SetAtOrAbove = 0.75f,
                    ClearBelow = 0.4f,
                },
            ]);
    }

    private static void AddHealthEffects(
        List<AttributeDeltaEffectConfig> effects,
        EntityKind kind,
        Attr attribute,
        PrimaryPlane plane,
        float timeScale)
    {
        // Settlement survival is a budget of terms, none singly decisive
        // (docs/design/settlement-population.md): worker maintenance is the
        // graduated backbone, and ambient channels — the masses and the
        // institutions — are gated so they matter without carrying a
        // settlement alone against Monumental drain.
        var ambient = kind == K.Settlement ? 0.4f : 1f;
        AddHealthEffectRules(effects, kind, attribute, plane, timeScale, ambient);
    }

    private static void AddHealthEffectRules(
        List<AttributeDeltaEffectConfig> effects,
        EntityKind kind,
        Attr attribute,
        PrimaryPlane plane,
        float timeScale,
        float ambient)
    {
        effects.AddRange(
        [
            InfluenceShelter(kind, attribute, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.06f * timeScale, threshold: 45f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.HomeBase, EffectRelationshipDirection.Outgoing, 0.05f * timeScale, threshold: 40f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.Anchored, EffectRelationshipDirection.Outgoing, 0.05f * timeScale, threshold: 40f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.ControlledBy, EffectRelationshipDirection.Either, 0.04f * timeScale, threshold: 45f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.ClaimedBy, EffectRelationshipDirection.Either, 0.035f * timeScale, threshold: 35f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.MemberOf, EffectRelationshipDirection.Either, 0.035f * timeScale, threshold: 35f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.LedBy, EffectRelationshipDirection.Either, 0.035f * timeScale * ambient, threshold: 35f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.TradeRoute, EffectRelationshipDirection.Either, 0.025f * timeScale * ambient, threshold: 35f, plane: plane),
            InfluenceShelter(kind, attribute, Rel.Memorializes, EffectRelationshipDirection.Either, 0.025f * timeScale * ambient, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.RivalOf, -0.07f * timeScale, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.AtWarWith, -0.10f * timeScale, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.PreysOn, -0.06f * timeScale, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.CorruptedBy, -0.08f * timeScale, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.BetrayedBy, -0.06f * timeScale, threshold: 35f, plane: plane),
            RelationshipSupport(kind, attribute, Rel.AlliedWith, 0.035f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.Anchored, 0.025f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.ClaimedBy, 0.02f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.MemberOf, 0.02f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.LedBy, 0.02f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.TradeRoute, 0.025f * timeScale * ambient, maxDistance: 2, plane: plane),
            RelationshipSupport(kind, attribute, Rel.Memorializes, 0.02f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.CleansedBy, 0.025f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.ConduitOf, 0.025f * timeScale * ambient, maxDistance: 1, plane: plane),
            RelationshipStress(kind, attribute, Rel.RivalOf, -0.04f * timeScale, maxDistance: 2, plane: plane),
            RelationshipStress(kind, attribute, Rel.AtWarWith, -0.07f * timeScale, maxDistance: 3, plane: plane),
            RelationshipStress(kind, attribute, Rel.CorruptedBy, -0.05f * timeScale, maxDistance: 2, plane: plane),
            RelationshipStress(kind, attribute, Rel.BetrayedBy, -0.045f * timeScale, maxDistance: 1, plane: plane),
            RelationshipStress(kind, attribute, Rel.WardAgainst, -0.03f * timeScale, maxDistance: 1, plane: plane),
            CultureInfluenceShelter(kind, attribute, 0.03f * timeScale * ambient, threshold: 35f, plane: plane),
            CultureSlotEffect(kind, attribute, EffectCultureSlotPolarity.Positive, 0.085f * timeScale * ambient, threshold: 2.4f, plane: plane),
            CultureSlotEffect(kind, attribute, EffectCultureSlotPolarity.Neutral, 0.04f * timeScale * ambient, threshold: 2.4f, plane: plane),
            CultureSlotEffect(kind, attribute, EffectCultureSlotPolarity.Negative, -0.035f * timeScale, threshold: 2.4f, plane: plane),
            LocalityEffect(kind, attribute, Effect.MapSupportAtLocation,
                0.035f * timeScale * ambient, threshold: 5f, plane: plane),
            LocalityEffect(kind, attribute, Effect.MapCrowdingComfort,
                0.025f * timeScale, threshold: 0.5f, plane: plane),
            LocalityEffect(kind, attribute, Effect.MapThreatAtLocation, -0.04f * timeScale, threshold: 4f, plane: plane),
            LocalityEffect(kind, attribute, Effect.MapCrowdingLoad, -0.08f * timeScale, threshold: 0.5f, plane: plane),
            // The carrying-capacity governor is a population-rate control, not
            // a lifespan pacing effect: it stays at full strength so growth
            // still equilibrates against culture support at the new time scale.
            CarryingStrain(kind, attribute, plane, timeScale: 1f),
            JurisdictionEffect(kind, attribute, K.Region.Value, null, "physical", 0.02f * timeScale * ambient),
            JurisdictionEffect(kind, attribute, K.Canon.Value, "accord", "semantic", 0.03f * timeScale * ambient),
            JurisdictionEffect(kind, attribute, K.Canon.Value, "taboo", "semantic", -0.03f * timeScale),
        ]);
        AddRangeAttrition(effects, kind, attribute, timeScale);
    }

    // The soft leash's teeth: mobile mortals bleed slowly on ground their
    // culture does not hold. A middle band on purpose - real pressure that
    // turns long overstays fatal without making every scouting trip a death
    // sentence. Ranging kinds tolerate the far dark far better; the deep's
    // own kinds and static anchors feel nothing.
    private static readonly Dictionary<string, float> RangeAttritionScale =
        new(StringComparer.Ordinal)
        {
            [IceRemembersKinds.Penguin.Value] = 1f,
            [IceRemembersKinds.Faction.Value] = 0.6f,
            [IceRemembersKinds.Transport.Value] = 0.35f,
            [IceRemembersKinds.Orca.Value] = 0.25f,
        };

    private const float RangeAttritionSupportThreshold = 12f;
    private const float RangeAttritionBaseDelta = -0.12f;

    private static void AddRangeAttrition(
        List<AttributeDeltaEffectConfig> effects,
        EntityKind kind,
        Attr attribute,
        float timeScale)
    {
        if (!RangeAttritionScale.TryGetValue(kind.Value, out var scale))
        {
            return;
        }

        effects.Add(RangeAttritionEffect(kind, attribute, scale, timeScale));
    }

    private static AttributeDeltaEffectConfig RangeAttritionEffect(
        EntityKind kind,
        Attr attribute,
        float scale,
        float timeScale) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.BeyondCultureSupport,
            Plane = "physical",
            Threshold = RangeAttritionSupportThreshold,
            DeltaPerTick = RangeAttritionBaseDelta * scale * timeScale,
        };

    /// <summary>
    /// Physical capacity derives from each hex's real facts: warmth (with
    /// geothermal refuges) sets suitability, rivers and floodplains enrich
    /// it, broken ground suppresses it, and open water carries
    /// temperature-driven marine life. Public so seed-time placement can
    /// price capacity with the exact same formula the kernel runs.
    /// </summary>
    public static PhysicalCapacityFormulaConfig PhysicalCapacityFormula { get; } = new()
    {
        ColdFloorTemperature = 180,
        WarmPeakTemperature = 500,
        GeothermalBlend = 0.5f,
        FreshwaterWeight = 0.6f,
        FertileWeight = 0.8f,
        ReliefComfort = 0.25f,
        ReliefPenalty = 1.2f,
        RoughnessPenalty = 0.5f,
        LandScale = 4f,
        MarineScale = 1.6f,
    };

    private static LocalityConfig CreateLocality() =>
        new()
        {
            SemanticBaseCapacity = 2f,
            CrowdingRadius = 1,
            PhysicalCapacityFormula = PhysicalCapacityFormula,
            Terrains = CreateTerrainLocality(),
            Kinds = IceRemembersEntities.CreateKindLocality(),
            Relationships =
            [
                RelationshipLocality(Rel.AlliedWith, support: 2.0f, threat: 0f),
                RelationshipLocality(Rel.HomeBase, support: 2.5f, threat: 0f),
                RelationshipLocality(Rel.Anchored, support: 2.0f, threat: 0f),
                RelationshipLocality(Rel.ControlledBy, support: 1.2f, threat: 0f),
                RelationshipLocality(Rel.ClaimedBy, support: 1.0f, threat: 0f),
                RelationshipLocality(Rel.MemberOf, support: 1.1f, threat: 0f),
                RelationshipLocality(Rel.LedBy, support: 1.0f, threat: 0f),
                RelationshipLocality(Rel.TradeRoute, support: 1.4f, threat: 0f),
                RelationshipLocality(Rel.Memorializes, support: 1.1f, threat: 0f),
                RelationshipLocality(Rel.CleansedBy, support: 1.5f, threat: 0f),
                RelationshipLocality(Rel.ConduitOf, support: 1.2f, threat: 0f),
                RelationshipLocality(Rel.RivalOf, support: 0f, threat: 1.5f),
                RelationshipLocality(Rel.AtWarWith, support: 0f, threat: 3.0f),
                RelationshipLocality(Rel.CorruptedBy, support: 0f, threat: 2.0f),
                RelationshipLocality(Rel.BetrayedBy, support: 0f, threat: 1.5f),
                RelationshipLocality(Rel.WardAgainst, support: 0f, threat: 1.0f),
            ],
        };

    // Physical capacity is fact-derived through the capacity formula, so
    // terrain rows remain only for the semantic plane, whose hexes carry no
    // generated facts.
    private static IReadOnlyList<TerrainLocalityConfig> CreateTerrainLocality() =>
    [
        TerrainLocality(Terrain.Lineage, 2.4f),
        TerrainLocality(Terrain.Yoke, 1.6f),
        TerrainLocality(Terrain.Witness, 3.2f),
        TerrainLocality(Terrain.Silence, 0.8f),
        TerrainLocality(Terrain.Severance, 0.6f),
        TerrainLocality(Terrain.Breakaway, 1.4f),
        TerrainLocality(Terrain.BeaconSpire, 3.6f),
        TerrainLocality(Terrain.TabooRidge, 0.4f),
        TerrainLocality(Terrain.CavePocket, 0.9f),
        TerrainLocality(Terrain.OpenField, 1.8f),
    ];

    private static TerrainLocalityConfig TerrainLocality(Terrain terrain, float baseCapacity) =>
        new()
        {
            Terrain = TerrainName(terrain),
            BaseCapacity = baseCapacity,
        };

    private static RelationshipLocalityConfig RelationshipLocality(Rel relationship, float support, float threat) =>
        new()
        {
            RelationshipKind = L(relationship),
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

    private static AttributeDeltaEffectConfig CultureInfluenceShelter(
        EntityKind kind,
        Attr attribute,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.OwnCultureInfluenceAtLocation,
            Plane = GameplayPlaneName(plane),
            Threshold = threshold,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig CultureSlotEffect(
        EntityKind kind,
        Attr attribute,
        EffectCultureSlotPolarity polarity,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.CultureSlotOccupancy,
            Plane = GameplayPlaneName(plane),
            CultureSlotPolarity = polarity,
            Threshold = threshold,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig InfluenceStress(
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

    /// <summary>
    /// Local carrying capacity: crowding drains health where the entity's own
    /// culture mass does not support the density. Asymptotic discount
    /// (scale/(scale+mass)) — never a hard cap; growing territory is the
    /// escape, and future artifacts/abilities can raise support the same way.
    /// </summary>
    private static AttributeDeltaEffectConfig CarryingStrain(
        EntityKind kind,
        Attr attribute,
        PrimaryPlane plane,
        float timeScale) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.CrowdingBeyondCultureSupport,
            Plane = GameplayPlaneName(plane),
            Threshold = 0.8f,
            SupportScale = 150f,
            DeltaPerTick = -0.06f * timeScale,
        };

    /// <summary>
    /// Living under the law (or the land): entities standing inside a named
    /// coverage patch on the given plane feel it — regions sustain their own
    /// ground, accords shelter, taboos strain.
    /// </summary>
    private static AttributeDeltaEffectConfig JurisdictionEffect(
        EntityKind kind,
        Attr attribute,
        string ownerKind,
        string? ownerSubtype,
        string plane,
        float deltaPerTick) =>
        new()
        {
            Kind = kind.Value,
            Attribute = A(attribute),
            Condition = Effect.InsideOwnedCoverage,
            Plane = plane,
            CoverageOwnerKind = ownerKind,
            CoverageOwnerSubtype = ownerSubtype,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig LocalityEffect(
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

    private static List<CultureConfig> CreateCultures() =>
    [
        CreateAuroraStackCulture(),
        CreateNightshelfCulture(),
        CreateWakeSingersCulture(),
        CreateTheIceCulture(),
    ];

    private static CultureConfig CreateAuroraStackCulture() =>
        CultureDefinition(
            Culture.AuroraStack,
            "Aurora Stack",
            physicalProduction: 1.1f,
            semanticProduction: 0.9f,
            slots:
            [
                Slot("high_rookery", "High Rookery", CultureSlotPolarity.Positive, [K.Settlement], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 4f), Score(CultureSlotScoreSignal.PhysicalCoverage, 0.85f),
                    Score(CultureSlotScoreSignal.PhysicalInfluence, 0.018f), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.75f, Rel.TradeRoute),
                    Score(CultureSlotScoreSignal.PhysicalCultureSupport, 0.55f), Score(CultureSlotScoreSignal.PhysicalInfluenceProduction, 0.012f),
                ], "The settlement whose public ground makes the Stack legible.", cultureBenefit: 0.015f),
                Slot("open_court", "Open Court", CultureSlotPolarity.Positive, [K.Faction, K.Penguin], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 3f), Score(CultureSlotScoreSignal.SemanticInfluence, 0.018f),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.8f, Rel.MemberOf), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.8f, Rel.AlliedWith),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.55f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.012f),
                ], "The visible voice that keeps rival rookeries talking.", entityBenefit: 0.07f, cultureBenefit: 0.01f),
                Slot("hearth_keeper", "Hearth Keeper", CultureSlotPolarity.Positive, [K.Settlement, K.Penguin], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 3f), Score(CultureSlotScoreSignal.PhysicalCoverage, 0.65f),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.HomeBase), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.HomeBase),
                    Score(CultureSlotScoreSignal.PhysicalCultureSupport, 0.5f), Score(CultureSlotScoreSignal.PhysicalInfluenceProduction, 0.01f),
                ], "The body or place most responsible for sheltering the culture.", entityBenefit: 0.06f, cultureBenefit: 0.01f),
                Slot("beacon_bearer", "Beacon Bearer", CultureSlotPolarity.Neutral, [K.Artifact, K.Transport, K.Penguin], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.5f), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.Holds),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.7f, Rel.Memorializes), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.55f, Rel.ConduitOf),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.45f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.01f),
                ], "The carried sign that lets scattered parties recognize one another.", entityBenefit: 0.055f),
                Slot("public_rival", "Public Rival", CultureSlotPolarity.Negative, [K.Faction, K.Penguin, K.Orca], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.5f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1f, Rel.RivalOf),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 1f, Rel.RivalOf), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1.1f, Rel.AtWarWith),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.35f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.008f),
                ], "The adversary whose opposition organizes the Stack's public stance.", entityBenefit: 0.04f),
                Slot("broken_oath", "Broken Oath", CultureSlotPolarity.Negative, [], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1.2f, Rel.BetrayedBy),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.BetrayedBy), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.8f, Rel.CorruptedBy),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.3f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.006f),
                ], "The shame or breach that the culture cannot stop answering.", entityBenefit: 0.035f),
            ]);

    private static CultureConfig CreateNightshelfCulture() =>
        CultureDefinition(
            Culture.Nightshelf,
            "Nightshelf",
            // 1.0 keeps base-stage emission (0.25/run) above the 0.2 decay
            // floor; at the old 0.75 a lone Nightshelf holding was net
            // negative everywhere and the culture could never hold ground.
            // Semantic eased from 2.5: that multiplier was balanced for a
            // ~60-entity culture, and once the count engines were fixed it
            // pinned the semantic plane at 0.84-0.92 (r16f fan).
            physicalProduction: 1f,
            semanticProduction: 1.8f,
            slots:
            [
                Slot("ledger_seat", "Ledger Seat", CultureSlotPolarity.Positive, [K.Faction], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 4f), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.ControlledBy),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.7f, Rel.MemberOf), Score(CultureSlotScoreSignal.SemanticInfluence, 0.016f),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.6f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.012f),
                ], "The faction that turns private obligation into policy.", entityBenefit: 0.075f, cultureBenefit: 0.015f),
                Slot("charter_harbor", "Charter Harbor", CultureSlotPolarity.Positive, [K.Settlement, K.Transport], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 3f), Score(CultureSlotScoreSignal.PhysicalCoverage, 0.75f),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.85f, Rel.TradeRoute), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.85f, Rel.TradeRoute),
                    Score(CultureSlotScoreSignal.PhysicalCultureSupport, 0.55f), Score(CultureSlotScoreSignal.PhysicalInfluenceProduction, 0.012f),
                ], "The route-node that makes Nightshelf bargains move.", cultureBenefit: 0.012f),
                Slot("quiet_patron", "Quiet Patron", CultureSlotPolarity.Neutral, [K.Faction, K.Penguin, K.Artifact], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.75f), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 1f, Rel.ClaimedBy),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1f, Rel.ClaimedBy), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.5f, Rel.Holds),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.45f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.01f),
                ], "The hand behind claims, custody, and sponsorship.", entityBenefit: 0.06f, cultureBenefit: 0.006f),
                Slot("seal_of_passage", "Seal of Passage", CultureSlotPolarity.Positive, [K.Artifact, K.Ability, K.Transport], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.5f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.8f, Rel.ConduitOf),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.75f, Rel.PracticedBy), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.75f, Rel.Holds),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.45f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.01f),
                ], "The portable proof that turns travel into recognized passage.", entityBenefit: 0.055f, cultureBenefit: 0.008f),
                Slot("black_ice_broker", "Black-Ice Broker", CultureSlotPolarity.Negative, [K.Faction, K.Penguin, K.Transport], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.25f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1f, Rel.BetrayedBy),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.8f, Rel.RivalOf), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.7f, Rel.CorruptedBy),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.35f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.008f),
                ], "The bargain-maker whose leverage corrodes trust.", entityBenefit: 0.04f, cultureBenefit: 0.004f),
                Slot("debt_anchor", "Debt Anchor", CultureSlotPolarity.Negative, [K.Settlement, K.PointOfInterest, K.Artifact], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.25f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.ControlledBy),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.ClaimedBy), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.8f, Rel.CorruptedBy),
                    Score(CultureSlotScoreSignal.PhysicalCultureSupport, 0.35f), Score(CultureSlotScoreSignal.PhysicalInfluenceProduction, 0.008f),
                ], "The place or object around which obligations become a trap.", entityBenefit: 0.035f),
            ]);

    private static CultureConfig CreateWakeSingersCulture() =>
        CultureDefinition(
            Culture.WakeSingers,
            "Wake-Singers",
            [
                Slot("oldest_voice", "Oldest Voice", CultureSlotPolarity.Positive, [K.Orca], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 4f), Score(CultureSlotScoreSignal.SemanticInfluence, 0.02f),
                    Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.LedBy), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.7f, Rel.AlliedWith),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.6f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.012f),
                ], "The whale whose memory gives the song a lead line.", entityBenefit: 0.08f, cultureBenefit: 0.015f),
                Slot("chorus_mouth", "Chorus Mouth", CultureSlotPolarity.Positive, [K.Orca, K.Ability, K.PointOfInterest], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 3f), Score(CultureSlotScoreSignal.SemanticInfluence, 0.018f),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.8f, Rel.TaughtBy), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.8f, Rel.PracticedBy),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.55f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.012f),
                ], "The singer, form, or site through which the wake spreads.", entityBenefit: 0.065f, cultureBenefit: 0.01f),
                Slot("reef_warden", "Reef Warden", CultureSlotPolarity.Neutral, [K.Orca, K.Monster, K.Artifact, K.Region], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.75f), Score(CultureSlotScoreSignal.PhysicalCoverage, 0.6f),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.WardAgainst), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.7f, Rel.Anchored),
                    Score(CultureSlotScoreSignal.PhysicalCultureSupport, 0.45f), Score(CultureSlotScoreSignal.PhysicalInfluenceProduction, 0.01f),
                ], "The boundary keeper that holds the old lines in place.", entityBenefit: 0.055f, cultureBenefit: 0.006f),
                Slot("memory_carver", "Memory Carver", CultureSlotPolarity.Positive, [K.Artifact, K.PointOfInterest, K.Settlement], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.5f), Score(CultureSlotScoreSignal.SemanticCoverage, 0.7f),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.Memorializes), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.7f, Rel.ConduitOf),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.45f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.01f),
                ], "The thing that gives the song an enduring cut in the world.", entityBenefit: 0.06f, cultureBenefit: 0.008f),
                Slot("deep_hunger", "Deep Hunger", CultureSlotPolarity.Negative, [K.Monster, K.Orca], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2.25f), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1f, Rel.CorruptedBy),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 1f, Rel.AtWarWith), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.8f, Rel.WardAgainst),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.35f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.008f),
                ], "The appetite or threat that pulls the wake out of harmony.", entityBenefit: 0.04f, cultureBenefit: 0.004f),
                Slot("dissonant_wake", "Dissonant Wake", CultureSlotPolarity.Negative, [], [
                    Score(CultureSlotScoreSignal.ActiveEntity, 2f), Score(CultureSlotScoreSignal.IncomingRelationshipStrength, 0.9f, Rel.CorruptedBy),
                    Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.RivalOf), Score(CultureSlotScoreSignal.OutgoingRelationshipStrength, 0.9f, Rel.BetrayedBy),
                    Score(CultureSlotScoreSignal.SemanticCultureSupport, 0.3f), Score(CultureSlotScoreSignal.SemanticInfluenceProduction, 0.006f),
                ], "The trailing rupture that bends every song around it.", entityBenefit: 0.035f),
            ]);

    private static CultureConfig CreateTheIceCulture() =>
        CultureDefinition(
            Culture.TheIce,
            "The Ice",
            []);

    private static CultureConfig CultureDefinition(
        Culture culture,
        string displayName,
        IReadOnlyList<CultureSlotConfig> slots,
        float physicalProduction = 1f,
        float semanticProduction = 1f) =>
        new()
        {
            Id = C(culture),
            DisplayName = displayName,
            Slots = slots,
            PlacementTaste = IceRemembersPlacementTastes.ForCulture(culture),
            PhysicalInfluenceProductionMultiplier = physicalProduction,
            SemanticInfluenceProductionMultiplier = semanticProduction,
        };

    private static CultureSlotConfig Slot(
        string id,
        string displayName,
        CultureSlotPolarity polarity,
        EntityKind[] allowedKinds,
        CultureSlotScoreRuleConfig[] scoreRules,
        string description,
        float minimumScore = 2.4f,
        int minimumTenureTicks = 60,
        float displacementMargin = 0.2f,
        float entityBenefit = 0.05f,
        float cultureBenefit = 0.005f) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            Polarity = polarity,
            AllowedKinds = allowedKinds.ToHashSet(),
            ScoreRules = scoreRules,
            Hysteresis = new CultureSlotHysteresisConfig
            {
                MinimumScore = minimumScore,
                MinimumTenureTicks = minimumTenureTicks,
                DisplacementMargin = displacementMargin,
            },
            Benefits = new CultureSlotBenefitConfig
            {
                EntityInfluenceMultiplierDelta = entityBenefit,
                CultureInfluenceMultiplierDelta = cultureBenefit * CultureSlotCultureInfluenceBenefitScale,
            },
        };

    private static CultureSlotScoreRuleConfig Score(
        CultureSlotScoreSignal signal,
        float weight,
        Rel? relationship = null) =>
        new()
        {
            Signal = signal,
            Weight = weight,
            RelationshipKind = relationship is { } value ? L(value) : null,
        };
}
