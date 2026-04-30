namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Graph;
using Engine.Kinds;
using Engine.Pressure;
using Engine.Processes;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
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
public static class IceRemembersDefinition
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
            LowAltitudeVerbs = CreateLowAltitudeVerbs(),
            Relationships = CreateRelationships(),
            RelationshipFacts = CreateRelationshipFacts(),
            Pressures = CreatePressures(),
            Degeneracy = IceRemembersDegeneracyDefinition.Create(),
            Effects = CreateEffects(),
            Lifecycles = CreateLifecycles(),
            Resources = CreateResources(),
            Terrains = IceRemembersTerrainCatalog.DomainTerrains,
            Cultures = CreateCultures(),
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

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateLowAltitudeVerbs() =>
    [
        MovementVerb(Verb.PenguinExplore, "Explore the White", K.Penguin, LowAltitudeVerbTargetMode.Explore,
            ticks: (45, 120), dwell: 2, samples: 24, weights: (1.4f, 2.2f, 1.8f, 0.1f, 0.9f, 0.2f)),
        MovementVerb(Verb.PenguinVisitRookery, "Visit the Rookery", K.Penguin, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Settlement, ticks: (30, 90), dwell: 18, radius: 1, samples: 12, weights: (1.0f, 0.2f, 0.4f, 0.7f, 0.15f, 0.4f),
            affinityRelationshipKind: L(Rel.HomeBase), affinityWeight: 1.4f),
        MovementVerb(Verb.PenguinVisitRival, "Trail a Rival", K.Penguin, LowAltitudeVerbTargetMode.RelationshipTarget,
            relationshipKind: L(Rel.RivalOf), ticks: (25, 70), dwell: 8, radius: 1, weights: (0.45f, 0.2f, 0.4f, 0.2f, 0.1f, 0.6f),
            affinityRelationshipKind: L(Rel.RivalOf), affinityWeight: 1.6f),
        MovementVerb(Verb.PenguinGoFishing, "Go Fishing", K.Penguin, LowAltitudeVerbTargetMode.Terrain,
            terrains: MaritimeTerrains(), ticks: (35, 100), dwell: 16, radius: 1, samples: 28, weights: (1.1f, 1.2f, 1.4f, 0.3f, 0.35f, 0.3f)),
        MovementVerb(Verb.PenguinFollowArtifact, "Follow a Relic", K.Penguin, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Artifact, ticks: (25, 80), dwell: 10, radius: 1, samples: 10, weights: (0.65f, 0.2f, 0.4f, 0.15f, 0.2f, 1.6f),
            affinityRelationshipKind: L(Rel.Holds), affinityWeight: 1.8f),
        MovementVerb(Verb.ArtifactCirculate, "Circulate", K.Artifact, LowAltitudeVerbTargetMode.Explore,
            ticks: (55, 150), dwell: 4, samples: 24, weights: (1.2f, 1.7f, 1.3f, 0.35f, 0.45f, 0.4f)),
        MovementVerb(Verb.ArtifactSeekBearer, "Seek a Bearer", K.Artifact, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Penguin, ticks: (30, 90), dwell: 12, radius: 1, samples: 12, weights: (0.75f, 0.2f, 0.3f, 0.2f, 0.2f, 1.6f),
            affinityRelationshipKind: L(Rel.Holds), affinityDirection: LowAltitudeVerbRelationshipDirection.Incoming, affinityWeight: 1.6f),
        MovementVerb(Verb.ArtifactReturnToShrine, "Return to Shrine", K.Artifact, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 110), dwell: 18, radius: 1, samples: 12, weights: (0.75f, 0.2f, 0.4f, 0.55f, 0.1f, 0.7f),
            affinityRelationshipKind: L(Rel.Anchored), affinityWeight: 1.4f),
        MovementVerb(Verb.ArtifactDrawToScar, "Draw Toward a Scar", K.Artifact, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Region, ticks: (40, 120), dwell: 10, radius: 1, samples: 14, weights: (0.65f, 0.4f, 0.6f, 0.25f, 0.2f, 1.1f)),
        MovementVerb(Verb.ArtifactSinkSeaward, "Sink Seaward", K.Artifact, LowAltitudeVerbTargetMode.Terrain,
            terrains: MaritimeTerrains(), ticks: (35, 120), dwell: 14, radius: 1, samples: 28, weights: (0.7f, 1.0f, 1.1f, 0.25f, 0.2f, 0.5f)),
        InfluenceVerb(Verb.SettlementCastLamplight, "Cast Lamplight", K.Settlement,
            LowAltitudeVerbOperation.GrowInfluence, LowAltitudeVerbTargetMode.OwnInfluence,
            ticks: (35, 95), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 24, weights: (1.15f, 1.0f, 1.2f, 0.3f, 0.25f, 0.4f)),
        InfluenceVerb(Verb.SettlementBankTheHearth, "Bank the Hearth", K.Settlement,
            LowAltitudeVerbOperation.ConsolidateInfluence, LowAltitudeVerbTargetMode.OwnInfluence,
            ticks: (25, 70), dwell: 12, influence: (7f, 0, 0f, 0f, 0f), samples: 18, weights: (0.8f, 0.3f, 0.4f, 0.7f, 0.05f, 0.1f)),
        InfluenceVerb(Verb.SettlementDrownOutClaim, "Drown Out a Claim", K.Settlement,
            LowAltitudeVerbOperation.ReduceInfluence, LowAltitudeVerbTargetMode.ForeignInfluence,
            ticks: (30, 85), dwell: 8, influence: (5f, 1, 0f, 0f, 0f), samples: 24, weights: (0.65f, 0.2f, 0.3f, 0.4f, 0.05f, 0.2f)),
        InfluenceVerb(Verb.SettlementBuildTheRookery, "Build the Rookery", K.Settlement,
            LowAltitudeVerbOperation.ExpandCoverage, LowAltitudeVerbTargetMode.OwnInfluence,
            ticks: (45, 130), dwell: 16, influence: (3f, 1, 0.025f, 0f, 58f), samples: 24, weights: (0.35f, 0.4f, 0.5f, 0.4f, 0.05f, 0.3f)),
        InfluenceVerb(Verb.FactionSendEnvoys, "Send Envoys", K.Faction,
            LowAltitudeVerbOperation.GrowInfluence, LowAltitudeVerbTargetMode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 100), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 18, weights: (1.0f, 0.2f, 0.4f, 0.7f, 0.1f, 0.8f),
            affinityRelationshipKind: L(Rel.ControlledBy), affinityDirection: LowAltitudeVerbRelationshipDirection.Incoming, affinityWeight: 1.1f),
        InfluenceVerb(Verb.FactionHoldCouncil, "Hold Council", K.Faction,
            LowAltitudeVerbOperation.ConsolidateInfluence, LowAltitudeVerbTargetMode.OwnInfluence,
            ticks: (25, 80), dwell: 12, influence: (6f, 1, 0f, 0f, 0f), samples: 20, weights: (0.9f, 0.3f, 0.5f, 0.5f, 0.05f, 0.2f)),
        InfluenceVerb(Verb.FactionBreakRivalHold, "Break a Rival Hold", K.Faction,
            LowAltitudeVerbOperation.ReduceInfluence, LowAltitudeVerbTargetMode.ForeignInfluence,
            ticks: (30, 90), dwell: 8, influence: (5.5f, 1, 0f, 0f, 0f), samples: 26, weights: (0.7f, 0.2f, 0.3f, 0.4f, 0.05f, 0.35f)),
        InfluenceVerb(Verb.FactionMoveSeat, "Move the Seat", K.Faction,
            LowAltitudeVerbOperation.MoveHeadquarters, LowAltitudeVerbTargetMode.OwnInfluence,
            ticks: (45, 140), dwell: 18, influence: (2f, 0, 0f, 0.04f, 65f), samples: 24, weights: (0.25f, 0.3f, 0.7f, 0.25f, 0.1f, 0.4f)),
    ];

    private static LowAltitudeVerbConfig MovementVerb(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Base, float Explore, float Stale, float Hot, float Crowd, float Generator) weights,
        EntityKind? targetKind = null,
        string? relationshipKind = null,
        IReadOnlySet<string>? terrains = null,
        int radius = 0,
        int samples = 16,
        string? affinityRelationshipKind = null,
        LowAltitudeVerbRelationshipDirection affinityDirection = LowAltitudeVerbRelationshipDirection.Either,
        float affinityWeight = 0f) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind,
            Plane = PrimaryPlane.Physical,
            TargetMode = mode,
            TargetKind = targetKind,
            RelationshipKind = relationshipKind,
            Terrains = terrains,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            TargetRadius = radius,
            CandidateSampleCount = samples,
            Weight = weights.Base,
            ExploreWeight = weights.Explore,
            StalenessWeight = weights.Stale,
            HotspotWeight = weights.Hot,
            CrowdingWeight = weights.Crowd,
            GeneratorAffordanceWeight = weights.Generator,
            AffinityRelationshipKind = affinityRelationshipKind,
            AffinityRelationshipDirection = affinityDirection,
            RelationshipAffinityWeight = affinityWeight,
        };

    private static LowAltitudeVerbConfig InfluenceVerb(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbOperation operation,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Delta, int Radius, float CoverageChance, float HeadquartersChance, float Required) influence,
        (float Base, float Explore, float Stale, float Hot, float Crowd, float Generator) weights,
        EntityKind? targetKind = null,
        string? relationshipKind = null,
        int samples = 16,
        string? affinityRelationshipKind = null,
        LowAltitudeVerbRelationshipDirection affinityDirection = LowAltitudeVerbRelationshipDirection.Either,
        float affinityWeight = 0f) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind,
            Plane = PrimaryPlane.Physical,
            Operation = operation,
            TargetMode = mode,
            TargetKind = targetKind,
            RelationshipKind = relationshipKind,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            TargetRadius = 0,
            CandidateSampleCount = samples,
            Weight = weights.Base,
            ExploreWeight = weights.Explore,
            StalenessWeight = weights.Stale,
            HotspotWeight = weights.Hot,
            CrowdingWeight = weights.Crowd,
            GeneratorAffordanceWeight = weights.Generator,
            AffinityRelationshipKind = affinityRelationshipKind,
            AffinityRelationshipDirection = affinityDirection,
            RelationshipAffinityWeight = affinityWeight,
            InfluenceDelta = influence.Delta,
            InfluenceRadius = influence.Radius,
            CoverageGrowthChance = influence.CoverageChance,
            HeadquartersMoveChance = influence.HeadquartersChance,
            RequiredInfluence = influence.Required,
        };

    private static HashSet<string> MaritimeTerrains() =>
    [
        TerrainName(Terrain.Ocean),
        TerrainName(Terrain.Reef),
        TerrainName(Terrain.Shelf),
    ];

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
            Plane = plane,
            MaxDistance = maxDistance,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig ContactMemory(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.ContactMemory,
            Plane = PrimaryPlane.Physical,
            Threshold = threshold,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig SourceInfluenceAtTarget(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.SourceInfluenceAtTarget,
            Threshold = threshold,
            Delta = delta,
        };

    private static RelationshipMaintenanceRuleConfig TargetInfluenceAtSource(float threshold, float delta) =>
        new()
        {
            Condition = RelationshipMaintenanceCondition.TargetInfluenceAtSource,
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
        AddHealthEffects(effects, K.Settlement, Attr.TempHp);
        AddHealthEffects(effects, K.Faction, Attr.Cohesion);
        AddHealthEffects(effects, K.Penguin, Attr.Vitality);
        AddHealthEffects(effects, K.Artifact, Attr.Condition);
        return new EffectsConfig { AttributeDeltaEffects = effects };
    }

    private static void AddHealthEffects(List<AttributeDeltaEffectConfig> effects, EntityKind kind, Attr attribute)
    {
        effects.AddRange(
        [
            InfluenceShelter(kind, attribute, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.06f, threshold: 45f),
            InfluenceShelter(kind, attribute, Rel.HomeBase, EffectRelationshipDirection.Outgoing, 0.05f, threshold: 40f),
            InfluenceShelter(kind, attribute, Rel.Anchored, EffectRelationshipDirection.Outgoing, 0.05f, threshold: 40f),
            InfluenceShelter(kind, attribute, Rel.ControlledBy, EffectRelationshipDirection.Either, 0.04f, threshold: 45f),
            InfluencePressure(kind, attribute, Rel.RivalOf, -0.07f, threshold: 35f),
            InfluencePressure(kind, attribute, Rel.AtWarWith, -0.10f, threshold: 35f),
            InfluencePressure(kind, attribute, Rel.CorruptedBy, -0.08f, threshold: 35f),
            InfluencePressure(kind, attribute, Rel.BetrayedBy, -0.06f, threshold: 35f),
            RelationshipSupport(kind, attribute, Rel.AlliedWith, 0.035f, maxDistance: 1),
            RelationshipSupport(kind, attribute, Rel.CleansedBy, 0.025f, maxDistance: 1),
            RelationshipSupport(kind, attribute, Rel.ConduitOf, 0.025f, maxDistance: 1),
            RelationshipStress(kind, attribute, Rel.RivalOf, -0.04f, maxDistance: 2),
            RelationshipStress(kind, attribute, Rel.AtWarWith, -0.07f, maxDistance: 3),
            RelationshipStress(kind, attribute, Rel.CorruptedBy, -0.05f, maxDistance: 2),
            RelationshipStress(kind, attribute, Rel.BetrayedBy, -0.045f, maxDistance: 1),
            RelationshipStress(kind, attribute, Rel.WardAgainst, -0.03f, maxDistance: 1),
        ]);
    }

    private static AttributeDeltaEffectConfig InfluenceShelter(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        EffectRelationshipDirection direction,
        float deltaPerTick,
        float threshold) =>
        new()
        {
            Kind = kind,
            Attribute = A(attribute),
            Condition = EffectCondition.RelatedInfluenceAtPhysicalLocation,
            RelationshipKind = L(relationship),
            RelationshipDirection = direction,
            Threshold = threshold,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig InfluencePressure(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        float threshold) =>
        new()
        {
            Kind = kind,
            Attribute = A(attribute),
            Condition = EffectCondition.RelatedInfluenceAtPhysicalLocation,
            RelationshipKind = L(relationship),
            RelationshipDirection = EffectRelationshipDirection.Either,
            Threshold = threshold,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

    private static AttributeDeltaEffectConfig RelationshipSupport(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance) =>
        RelationshipAttributeDeltaEffect(kind, attribute, relationship, deltaPerTick, maxDistance);

    private static AttributeDeltaEffectConfig RelationshipStress(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance) =>
        RelationshipAttributeDeltaEffect(kind, attribute, relationship, deltaPerTick, maxDistance);

    private static AttributeDeltaEffectConfig RelationshipAttributeDeltaEffect(
        EntityKind kind,
        Attr attribute,
        Rel relationship,
        float deltaPerTick,
        int maxDistance) =>
        new()
        {
            Kind = kind,
            Attribute = A(attribute),
            Condition = EffectCondition.RelatedEntityDistance,
            RelationshipKind = L(relationship),
            RelationshipDirection = EffectRelationshipDirection.Either,
            Plane = PrimaryPlane.Physical,
            MaxDistance = maxDistance,
            MinimumRelationshipStrength = 0.15f,
            DeltaPerTick = deltaPerTick,
        };

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
