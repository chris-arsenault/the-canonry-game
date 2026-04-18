namespace Engine.Domain.TestDomain;

using Engine.Domain.Config;
using Engine.Processes;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Resources;
using Engine.Wiring;
using Attr = Engine.Domain.TestDomain.TestDomainAttributeId;
using Culture = Engine.Domain.TestDomain.TestDomainCulture;
using Fact = Engine.Domain.TestDomain.TestDomainStructuralFact;
using Press = Engine.Domain.TestDomain.TestDomainPressure;
using Rel = Engine.Domain.TestDomain.TestDomainRelationship;
using Res = Engine.Domain.TestDomain.TestDomainResource;

/// <summary>
/// Test domain providing generic placeholder content for Phase 1 development.
/// All values that were previously hard-coded in Default*.cs files live here.
/// Swap this with a real domain (The Ice Remembers) in Phase 2.
/// </summary>
public static class TestDomainDefinition
{
    public static DomainConfig Create()
    {
        var kinds = CreateKinds();
        var semantics = TestDomainSemanticDefinition.Create();
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
            [EntityKind.Region] = new()
            {
                Subtypes = new HashSet<string> { "generic_region", "tundra", "coastal", "mountain", "plains" },
                Attributes =
                [
                    new() { Name = A(Attr.GeologicStability), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 80f },
                    new() { Name = A(Attr.RegionalSafety), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 50f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Pristineness), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 70f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.GeologicStability),
                Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself — the land's own tendencies — distinct from the people, factions, or settlements that happen to occupy it.",
                PrimaryPlane = PrimaryPlane.Semantic,
            },
            [EntityKind.Settlement] = new()
            {
                Subtypes = new HashSet<string> { "generic_settlement", "village", "town", "city", "fortress", "ruin" },
                Attributes =
                [
                    new() { Name = A(Attr.Residents), Category = AttributeCategory.Health, Min = 0f, Max = 100000f, Default = 10f, Baseline = 20f },
                    new() { Name = A(Attr.Dominion), Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 20f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Sanitation), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 60f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.Residents),
                PrimaryPlane = PrimaryPlane.Semantic,
            },
            [EntityKind.Faction] = new()
            {
                Subtypes = new HashSet<string> { "generic_faction", "order", "cult", "company", "league" },
                Attributes =
                [
                    new() { Name = A(Attr.Cohesion), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 70f },
                    new() { Name = A(Attr.Influence), Category = AttributeCategory.Projection, Min = 0f, Max = 100f, Default = 30f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Provisions), Category = AttributeCategory.Need, Min = 0f, Max = 100f, Default = 50f, DriftRate = -0.3f },
                ],
                HealthAttribute = A(Attr.Cohesion),
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.TerritorialOrder), EntityKind.Faction),
                        BaseUrgency = 0.035f,
                    },
                ],
                PrimaryPlane = PrimaryPlane.Semantic,
            },
            [EntityKind.Npc] = new()
            {
                Subtypes = new HashSet<string> { "generic_npc", "hero", "merchant", "scholar", "warrior" },
                Attributes =
                [
                    new() { Name = A(Attr.Vitality), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f },
                    new() { Name = A(Attr.Motivation), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 50f, DriftRate = -0.3f },
                    new()
                    {
                        Name = A(Attr.Satiety),
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
                            Description = "Critical satiety loss drains vitality",
                        },
                    },
                ],
                HealthAttribute = A(Attr.Vitality),
                StandingDrives =
                [
                    new StandingDriveConfig
                    {
                        DesiredEffect = AttributeRef.StructuralFact(F(Fact.Roam), EntityKind.Npc),
                        BaseUrgency = 0.03f,
                    },
                ],
                PrimaryPlane = PrimaryPlane.Physical,
            },
            [EntityKind.Artifact] = new()
            {
                Subtypes = new HashSet<string> { "generic_artifact", "weapon", "armor", "instrument", "relic", "scroll" },
                Attributes =
                [
                    new() { Name = A(Attr.Condition), Category = AttributeCategory.Health, Min = 0f, Max = 100f, Default = 100f, DriftRate = -0.01f },
                    new() { Name = A(Attr.Power), Category = AttributeCategory.Capacity, Min = 0f, Max = 100f, Default = 10f, DriftRate = -0.3f },
                    new() { Name = A(Attr.Aura), Category = AttributeCategory.Drive, Min = 0f, Max = 100f, Default = 90f, DriftRate = -0.3f },
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
        var all = new HashSet<EntityCategory> { EntityCategory.Spatial, EntityCategory.Agency, EntityCategory.Asset, EntityCategory.Record };

        return
        [
            new() { Name = L(Rel.Borders), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial, IsSymmetric = true },
            new() { Name = L(Rel.Contains), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = spatial },
            new() { Name = L(Rel.ControlledBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = spatial, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.LocatedAt), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.HomeBase), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.AlliedWith), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency, IsSymmetric = true, CullThreshold = 0.1f },
            new() { Name = L(Rel.RivalOf), Polarity = RelationshipPolarity.Negative, ValidSourceCategories = agency, ValidTargetCategories = agency, CullThreshold = 0.05f },
            new() { Name = L(Rel.LedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency, IsExclusive = true },
            new() { Name = L(Rel.MemberOf), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = L(Rel.LoyalTo), Polarity = RelationshipPolarity.Positive, ValidSourceCategories = agency, ValidTargetCategories = agency },
            new() { Name = L(Rel.Holds), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = agency, ValidTargetCategories = asset },
            new() { Name = L(Rel.CreatedBy), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = agency },
            new() { Name = L(Rel.StoredAt), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = asset, ValidTargetCategories = spatial, IsExclusive = true },
            new() { Name = L(Rel.InvolvedIn), Polarity = RelationshipPolarity.Neutral, ValidSourceCategories = all, ValidTargetCategories = all },
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
            [EntityKind.Settlement] = new() { HealthAttribute = A(Attr.Residents), BaseRate = 0.002f, AgeCoefficient = 0.00008f },
            [EntityKind.Faction] = new() { HealthAttribute = A(Attr.Cohesion), BaseRate = 0.01f, AgeCoefficient = 0.00005f },
            [EntityKind.Npc] = new() { HealthAttribute = A(Attr.Vitality), BaseRate = 0.02f, AgeCoefficient = 0.0005f },
            [EntityKind.Artifact] = new() { HealthAttribute = A(Attr.Condition), BaseRate = 0.01f, AgeCoefficient = 0.0001f },
            [EntityKind.Region] = new() { HealthAttribute = A(Attr.GeologicStability), BaseRate = 0.001f, AgeCoefficient = 0.00002f },
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
            new() { Id = C(Culture.CultureA), DisplayName = "Culture A" },
            new() { Id = C(Culture.CultureB), DisplayName = "Culture B" },
        ];
    }
}
