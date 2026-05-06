namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Kinds;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using GambitId = IceRemembers.IceRemembersGambitId;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Verb = IceRemembers.IceRemembersVerbId;

/// <summary>
/// The Ice Remembers domain definition. This provides the engine with the
/// world's canonical kinds, relationships, cultures, and semantics.
/// </summary>
public static partial class IceRemembersDefinition
{
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
            Cultures = CreateCultures(),
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
            SemanticPlane = new SemanticPlaneConfig
            {
                MinQ = -20,
                MaxQ = 20,
                MinR = -20,
                MaxR = 20,
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
        IceRemembersEntities.CreateInitialInfluenceRules();

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
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyConflictMaintenance() },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, Maintenance = AgencyBondMaintenance() },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset, TargetFollowsSourcePhysical = true },
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
            new() { Name = L(Rel.PracticedBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AbilityPracticeMaintenance() },
            new() { Name = L(Rel.TaughtBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f, Maintenance = AgencyBondMaintenance() },
        ];
    }

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
            AddHealthEffects(effects, entity.Kind, entity.HealthAttribute, entity.PrimaryPlane);
        }

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
            InfluenceShelter(kind, attribute, Rel.TradeRoute, EffectRelationshipDirection.Either, 0.025f, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.RivalOf, -0.07f, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.AtWarWith, -0.10f, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.CorruptedBy, -0.08f, threshold: 35f, plane: plane),
            InfluenceStress(kind, attribute, Rel.BetrayedBy, -0.06f, threshold: 35f, plane: plane),
            RelationshipSupport(kind, attribute, Rel.AlliedWith, 0.035f, maxDistance: 1, plane: plane),
            RelationshipSupport(kind, attribute, Rel.TradeRoute, 0.025f, maxDistance: 2, plane: plane),
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
            MapSignalEffect(kind, attribute, Effect.MapCrowdingLoad, -0.08f, threshold: 0.5f, plane: plane),
        ]);
    }

    private static MapSignalsConfig CreateMapSignals() =>
        new()
        {
            SemanticBaseCapacity = 2f,
            CrowdingRadius = 1,
            Terrains = CreateTerrainSignals(),
            Kinds = IceRemembersEntities.CreateKindMapSignals(),
            Relationships =
            [
                RelationshipSignal(Rel.AlliedWith, support: 2.0f, threat: 0f),
                RelationshipSignal(Rel.HomeBase, support: 2.5f, threat: 0f),
                RelationshipSignal(Rel.Anchored, support: 2.0f, threat: 0f),
                RelationshipSignal(Rel.ControlledBy, support: 1.2f, threat: 0f),
                RelationshipSignal(Rel.TradeRoute, support: 1.4f, threat: 0f),
                RelationshipSignal(Rel.CleansedBy, support: 1.5f, threat: 0f),
                RelationshipSignal(Rel.ConduitOf, support: 1.2f, threat: 0f),
                RelationshipSignal(Rel.RivalOf, support: 0f, threat: 1.5f),
                RelationshipSignal(Rel.AtWarWith, support: 0f, threat: 3.0f),
                RelationshipSignal(Rel.CorruptedBy, support: 0f, threat: 2.0f),
                RelationshipSignal(Rel.BetrayedBy, support: 0f, threat: 1.5f),
                RelationshipSignal(Rel.WardAgainst, support: 0f, threat: 1.0f),
            ],
        };

    private static IReadOnlyList<TerrainMapSignalConfig> CreateTerrainSignals() =>
    [
        TerrainSignal(Terrain.Wastes, 0.7f),
        TerrainSignal(Terrain.Crags, 0.8f),
        TerrainSignal(Terrain.Tundra, 2.0f),
        TerrainSignal(Terrain.Ice, 1.0f),
        TerrainSignal(Terrain.HotSprings, 4.0f),
        TerrainSignal(Terrain.Shelf, 1.3f),
        TerrainSignal(Terrain.Reef, 2.5f),
        TerrainSignal(Terrain.Ocean, 0.6f),
        TerrainSignal(Terrain.Lineage, 2.4f),
        TerrainSignal(Terrain.Yoke, 1.6f),
        TerrainSignal(Terrain.Witness, 3.2f),
        TerrainSignal(Terrain.Silence, 0.8f),
        TerrainSignal(Terrain.Severance, 0.6f),
        TerrainSignal(Terrain.Breakaway, 1.4f),
        TerrainSignal(Terrain.BeaconSpire, 3.6f),
        TerrainSignal(Terrain.TabooRidge, 0.4f),
        TerrainSignal(Terrain.CavePocket, 0.9f),
        TerrainSignal(Terrain.OpenField, 1.8f),
    ];

    private static TerrainMapSignalConfig TerrainSignal(Terrain terrain, float baseCapacity) =>
        new()
        {
            Terrain = TerrainName(terrain),
            BaseCapacity = baseCapacity,
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
