namespace Engine.Domain.IceRemembers;

using Engine.Domain.Config;
using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Resources;
using Engine.Wiring;
using Attr = Engine.Domain.IceRemembers.IceRemembersAttributeId;
using Culture = Engine.Domain.IceRemembers.IceRemembersCulture;
using Fact = Engine.Domain.IceRemembers.IceRemembersStructuralFact;
using K = Engine.Domain.IceRemembers.IceRemembersKinds;
using Press = Engine.Domain.IceRemembers.IceRemembersPressure;
using Rel = Engine.Domain.IceRemembers.IceRemembersRelationship;
using Res = Engine.Domain.IceRemembers.IceRemembersResource;

/// <summary>
/// The Ice Remembers domain definition. This provides the engine with the
/// world's canonical kinds, relationships, resources, cultures, and semantics.
/// </summary>
public static class IceRemembersDefinition
{
    public static DomainConfig Create()
    {
        var kinds = CreateKinds();
        var semantics = IceRemembersSemanticDefinition.Create();
        return new DomainConfig
        {
            Kinds = kinds,
            Processes = Engine.Domain.Graph.RuntimeProcessGenerator.Generate(semantics, kinds),
            Relationships = CreateRelationships(),
            Pressures = CreatePressures(),
            Lifecycles = CreateLifecycles(),
            Resources = CreateResources(),
            Cultures = CreateCultures(),
            Semantics = semantics,
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string C(Culture culture) => culture.ToDomainName();
    private static string F(Fact fact) => fact.ToDomainName();
    private static string L(Rel relationship) => relationship.ToDomainName();
    private static string P(Press pressure) => pressure.ToDomainName();
    private static string R(Res resource) => resource.ToDomainName();
    private static ResourceType T(Res resource) => resource.ToResourceType();

    private static Dictionary<EntityKind, KindConfig> CreateKinds()
    {
        return new Dictionary<EntityKind, KindConfig>
        {
            [K.Region] = new()
            {
                Category = EntityCategory.Spatial,
                Subtypes = new HashSet<string> { "face", "shelf", "depths", "reach", "sea", "scar" },
                HexToHealthRecoveryHosts =
                [
                    new RecoveryHostQueryConfig
                    {
                        AnyOfKinds = new HashSet<EntityKind> { K.Settlement, K.Artifact },
                    },
                ],
                Attributes =
                [
                    new() { Name = A(Attr.Stability), DisplayName = "Stability", Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 80f },
                    new() { Name = A(Attr.Holding), DisplayName = "Holding", Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 50f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Pristiness), DisplayName = "Pristiness", Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 70f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.Stability),
                Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself — the land's own tendencies — distinct from the people, factions, or settlements that happen to occupy it.",
                PrimaryPlane = PrimaryPlane.Semantic,
            },
            [K.Settlement] = new()
            {
                Category = EntityCategory.Spatial,
                Subtypes = new HashSet<string> { "rookery", "colony", "holdfast", "sanctuary", "harbor", "reefhold" },
                HexToHealthRecoveryHosts =
                [
                    new RecoveryHostQueryConfig
                    {
                        AnyOfKinds = new HashSet<EntityKind> { K.Region, K.Artifact },
                    },
                ],
                Attributes =
                [
                    new() { Name = A(Attr.Residents), DisplayName = "Residents", Category = AttributeCategory.Health, Min = 0f, Max = 100000f, Default = 10f, Baseline = 20f },
                    new() { Name = A(Attr.Dominion), DisplayName = "Dominion", Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 20f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Shelter), DisplayName = "Shelter", Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 60f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.Residents),
                PrimaryPlane = PrimaryPlane.Semantic,
            },
            [K.Faction] = new()
            {
                Category = EntityCategory.Agency,
                Subtypes = new HashSet<string> { "order", "compact", "cell", "choir" },
                HexToHealthRecoveryHosts =
                [
                    new RecoveryHostQueryConfig
                    {
                        AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                    },
                ],
                Attributes =
                [
                    new() { Name = A(Attr.Cohesion), DisplayName = "Cohesion", Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = A(Attr.Reach), DisplayName = "Reach", Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 30f, DriftRate = -0.3f },
                    new()
                    {
                        Name = A(Attr.Provisions),
                        DisplayName = "Provisions",
                        CultureDisplayNames = new Dictionary<string, string>
                        {
                            [C(Culture.AuroraStack)] = "Aurora Light",
                            [C(Culture.Nightshelf)] = "Fire-Cores",
                            [C(Culture.WakeSingers)] = "Offerings",
                        },
                        Category = AttributeCategory.Need,
                        Min = 0f,
                        Max = 100f,
                        Default = 50f,
                        DriftRate = -0.3f,
                    },
                ],
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
            },
            [K.Penguin] = new()
            {
                Category = EntityCategory.Agency,
                Subtypes = new HashSet<string> { "warden", "broker", "witness", "exile" },
                HexToHealthRecoveryHosts =
                [
                    new RecoveryHostQueryConfig
                    {
                        AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                    },
                ],
                Attributes =
                [
                    new() { Name = A(Attr.Vitality), DisplayName = "Vitality", Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f },
                    new()
                    {
                        Name = A(Attr.Echo),
                        DisplayName = "Echo",
                        CultureDisplayNames = new Dictionary<string, string>
                        {
                            [C(Culture.AuroraStack)] = "Witness",
                            [C(Culture.Nightshelf)] = "Echo",
                            [C(Culture.WakeSingers)] = "History",
                        },
                        Category = AttributeCategory.Capacity,
                        Min = 0f,
                        Max = 100f,
                        Default = 50f,
                        DriftRate = -0.3f,
                    },
                    new()
                    {
                        Name = A(Attr.Warmth),
                        DisplayName = "Warmth",
                        Category = AttributeCategory.Need,
                        Min = 0f,
                        Max = 100f,
                        Default = 100f,
                        DriftRate = -0.35f,
                        LocationResourceDriftResource = T(Res.Food),
                        LocationResourceDriftPerUnit = 0.04f,
                        LocationResourceDriftMinOffset = 0f,
                        LocationResourceDriftMaxOffset = 0.27f,
                        CriticalEffect = new CriticalEffectConfig
                        {
                            TargetAttribute = A(Attr.Vitality),
                            DeltaPerTick = -0.20f,
                            Description = "Critical warmth loss drains vitality",
                        },
                    },
                ],
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
            },
            [K.Artifact] = new()
            {
                Category = EntityCategory.Asset,
                Subtypes = new HashSet<string> { "relic", "instrument", "weapon", "tome", "seal" },
                HexToHealthRecoveryHosts =
                [
                    new RecoveryHostQueryConfig
                    {
                        AnyOfKinds = new HashSet<EntityKind> { K.Settlement },
                    },
                ],
                Attributes =
                [
                    new() { Name = A(Attr.Condition), DisplayName = "Condition", Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new()
                    {
                        Name = A(Attr.Power),
                        DisplayName = "Power",
                        CultureDisplayNames = new Dictionary<string, string>
                        {
                            [C(Culture.AuroraStack)] = "Light",
                            [C(Culture.Nightshelf)] = "Heat",
                            [C(Culture.WakeSingers)] = "Pressure",
                        },
                        Category = AttributeCategory.Capacity,
                        Min = 0f,
                        Max = 100f,
                        Default = 10f,
                        DriftRate = -0.3f,
                    },
                    new() { Name = A(Attr.Resonance), DisplayName = "Resonance", Category = AttributeCategory.Drive, Min = 0f, Max = 100f, Default = 90f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.Condition),
                PrimaryPlane = PrimaryPlane.Physical,
            },
        };
    }

    private static List<RelationshipConfig> CreateRelationships()
    {
        var spatial = new HashSet<EntityCategory> { EntityCategory.Spatial };
        var agency = new HashSet<EntityCategory> { EntityCategory.Agency };
        var asset = new HashSet<EntityCategory> { EntityCategory.Asset };
        var spatialOrAsset = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Asset };
        var agencyOrSpatialOrAsset = new HashSet<EntityCategory> { EntityCategory.Agency, EntityCategory.Spatial, EntityCategory.Asset };

        return
        [
            new() { Name = L(Rel.ControlledBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset },
            new() { Name = L(Rel.CreatedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = L(Rel.Memorializes), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset },
            new() { Name = L(Rel.ClaimedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatialOrAsset, ValidTargetCategories = agency },
            new() { Name = L(Rel.TradeRoute), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true, CullThreshold = 0.05f },
            new() { Name = L(Rel.FoundedFrom), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = L(Rel.NativeTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatial },
            new() { Name = L(Rel.Anchored), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.ConduitOf), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = asset, ValidTargetCategories = spatialOrAsset },
            new() { Name = L(Rel.PartOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = asset },
            new() { Name = L(Rel.WardAgainst), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = asset, ValidTargetCategories = asset, CullThreshold = 0.05f },
            new() { Name = L(Rel.CorruptedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f },
            new() { Name = L(Rel.CleansedBy), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agencyOrSpatialOrAsset, ValidTargetCategories = agencyOrSpatialOrAsset, CullThreshold = 0.02f },
            new() { Name = L(Rel.AtWarWith), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.02f },
            new() { Name = L(Rel.BetrayedBy), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency },
        ];
    }

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
            [K.Settlement] = new() { HealthAttribute = A(Attr.Residents), BaseRate = 0.002f, AgeCoefficient = 0.00008f },
            [K.Faction] = new() { HealthAttribute = A(Attr.Cohesion), BaseRate = 0.01f, AgeCoefficient = 0.00005f },
            [K.Penguin] = new() { HealthAttribute = A(Attr.Vitality), BaseRate = 0.02f, AgeCoefficient = 0.0005f },
            [K.Artifact] = new() { HealthAttribute = A(Attr.Condition), BaseRate = 0.01f, AgeCoefficient = 0.0001f },
            [K.Region] = new() { HealthAttribute = A(Attr.Stability), BaseRate = 0.001f, AgeCoefficient = 0.00002f },
        };
    }

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
