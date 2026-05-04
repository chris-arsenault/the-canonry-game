namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Graph;
using Engine.Kinds;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;
using Verb = IceRemembers.IceRemembersVerbId;

/// <summary>
/// The Ice Remembers domain definition. This provides the engine with the
/// world's canonical kinds, relationships, cultures, and semantics.
/// </summary>
public static partial class IceRemembersDefinition
{
    private const float HealthDefault = 90f;
    private const float HealthDefaultVariance = 1f / 9f;
    private const float PositiveTraitAssignmentChance = 0.35f;
    private const float NegativeTraitAssignmentChance = 0.25f;
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
            Gambits = CreateGambits(),
            Relationships = CreateRelationships(),
            Degeneracy = IceRemembersDegeneracyDefinition.Create(),
            Lifecycles = CreateLifecycles(),
            Terrains = IceRemembersTerrainCatalog.DomainTerrains,
            Cultures = CreateCultures(),
            Gameplay = CreateGameplayDefinition(),
            Semantics = semantics,
            Chronicle = CreateChronicleDefinition(),
        };
    }

    private static string A(Attr attribute) => attribute.ToDomainName();
    private static string T(Trait trait) => trait.ToDomainName();
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
        new()
        {
            [K.Region] = CreateRegionKind(),
            [K.Settlement] = CreateSettlementKind(),
            [K.Faction] = CreateFactionKind(),
            [K.Penguin] = CreatePenguinKind(),
            [K.Artifact] = CreateArtifactKind(),
            [K.Orca] = CreateOrcaKind(),
            [K.PointOfInterest] = CreatePointOfInterestKind(),
            [K.Ability] = CreateAbilityKind(),
            [K.Monster] = CreateMonsterKind(),
            [K.Transport] = CreateTransportKind(),
            [K.Occurrence] = CreateOccurrenceKind(),
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
    [
        InitialInfluence(K.Region, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Settlement, PrimaryPlane.Physical, 35f),
        InitialInfluence(K.Settlement, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Faction, PrimaryPlane.Physical, 35f),
        InitialInfluence(K.Faction, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Penguin, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Artifact, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Orca, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.PointOfInterest, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Ability, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Monster, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Transport, PrimaryPlane.Semantic, 35f),
        InitialInfluence(K.Occurrence, PrimaryPlane.Semantic, 35f),
    ];

    private static InitialInfluenceRule InitialInfluence(EntityKind kind, PrimaryPlane plane, float value) =>
        new(kind.Value, GameplayPlaneName(plane), value);

    private static KindConfig CreateRegionKind() =>
        new()
        {
            Category = EntityCategory.Spatial,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "face", "shelf", "depths", "reach", "sea", "scar" },
            Health = Health(Attr.Stability, "Stability"),
            Traits =
            [
                PositiveTrait(
                    Trait.RegionShelteringGround,
                    "Sheltering Ground",
                    "Local support effects are stronger for this region.",
                    TraitMapDelta(K.Region, Attr.Stability, Effect.MapSupportAtLocation, 0.025f, 4f, PrimaryPlane.Semantic)),
                NegativeTrait(
                    Trait.RegionBrittleSeams,
                    "Brittle Seams",
                    "Stress and corruption effects bite harder around this region.",
                    TraitMapDelta(K.Region, Attr.Stability, Effect.MapThreatAtLocation, -0.03f, 3f, PrimaryPlane.Semantic)),
            ],
            Description = "A geographically connected area of the world. A region's semantic alignment is the spirit or animist ideals of the place itself — the land's own tendencies — distinct from the people, factions, or settlements that happen to occupy it.",
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateSettlementKind() =>
        new()
        {
            Category = EntityCategory.Spatial,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "rookery", "colony", "holdfast", "sanctuary", "harbor", "reefhold" },
            Health = Health(Attr.TempHp, "Temp HP"),
            Traits =
            [
                PositiveTrait(
                    Trait.SettlementProvidentHearth,
                    "Provident Hearth",
                    "Shelter and recovery effects are stronger for this settlement.",
                    TraitMapDelta(K.Settlement, Attr.TempHp, Effect.MapSupportAtLocation, 0.03f, 4f, PrimaryPlane.Semantic)),
                NegativeTrait(
                    Trait.SettlementOverextendedHalls,
                    "Overextended Halls",
                    "Crowding and raid load scale upward around this settlement.",
                    TraitMapDelta(K.Settlement, Attr.TempHp, Effect.MapCrowdingLoad, -0.04f, 0.5f, PrimaryPlane.Semantic)),
            ],
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateFactionKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "order", "compact", "cell", "choir" },
            Health = Health(Attr.Cohesion, "Cohesion"),
            Traits =
            [
                PositiveTrait(
                    Trait.FactionSteadyCadre,
                    "Steady Cadre",
                    "Faction gambits are more reliable and allied support coheres faster.",
                    TraitRelationshipDelta(K.Faction, Attr.Cohesion, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.025f, 1, PrimaryPlane.Semantic),
                    TraitGambit(GambitId.FactionDeclaresTerritorialWar, scoreDelta: -0.05f, successDelta: 0.08f),
                    TraitGambit(GambitId.FactionSuesForPeace, scoreDelta: 0.08f, successDelta: 0.08f)),
                NegativeTrait(
                    Trait.FactionFractiousCouncil,
                    "Fractious Council",
                    "Conflict gates favor this faction but execution is less reliable.",
                    TraitRelationshipDelta(K.Faction, Attr.Cohesion, Rel.RivalOf, EffectRelationshipDirection.Either, -0.025f, 1, PrimaryPlane.Semantic),
                    TraitGambit(GambitId.FactionDeclaresTerritorialWar, scoreDelta: 0.18f, successDelta: -0.05f),
                    TraitGambit(GambitId.FactionSuesForPeace, scoreDelta: -0.12f, successDelta: -0.04f)),
            ],
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreatePenguinKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "warden", "broker", "witness", "exile" },
            Health = Health(Attr.Vitality, "Vitality"),
            Traits =
            [
                PositiveTrait(
                    Trait.PenguinRangingHeart,
                    "Ranging Heart",
                    "Low-altitude movement biases toward exploration and stale cells.",
                    TraitVerbWeight(Verb.PenguinExplore, exploreDelta: 1.0f, stalenessDelta: 0.6f, hotspotDelta: -0.1f),
                    TraitVerbWeight(Verb.PenguinGoFishing, exploreDelta: 0.35f, stalenessDelta: 0.25f)),
                NegativeTrait(
                    Trait.PenguinHearthbound,
                    "Hearthbound",
                    "Low-altitude movement biases toward home bases and familiar safe cells.",
                    TraitVerbWeight(Verb.PenguinExplore, exploreDelta: -0.7f, stalenessDelta: -0.25f),
                    TraitVerbWeight(Verb.PenguinVisitRookery, weightDelta: 0.4f, hotspotDelta: 0.3f, relationshipAffinityDelta: 0.8f)),
            ],
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static KindConfig CreateArtifactKind() =>
        new()
        {
            Category = EntityCategory.Asset,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "relic", "instrument", "weapon", "tome", "seal" },
            Health = Health(Attr.Condition, "Condition"),
            Traits =
            [
                PositiveTrait(
                    Trait.ArtifactRestorativeAura,
                    "Restorative Aura",
                    "Wielders, anchored regions, and cleansing gambits receive restorative support.",
                    TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, 0.04f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                    TraitRelationshipDelta(K.Region, Attr.Stability, Rel.Anchored, EffectRelationshipDirection.Incoming, 0.03f, 1, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                    TraitGambit(GambitId.ArtifactCleansesScar, scoreDelta: 0.10f, successDelta: 0.10f)),
                NegativeTrait(
                    Trait.ArtifactVolatileResonance,
                    "Volatile Resonance",
                    "Wielders and anchored regions risk backlash while cleansing becomes tempting.",
                    TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, -0.035f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                    TraitRelationshipDelta(K.Region, Attr.Stability, Rel.Anchored, EffectRelationshipDirection.Incoming, -0.025f, 1, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.45f),
                    TraitGambit(GambitId.ArtifactCleansesScar, scoreDelta: 0.15f, successDelta: -0.08f)),
            ],
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static KindConfig CreateOrcaKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "matriarch", "bull", "scout", "elder" },
            Health = Health(Attr.Endurance, "Endurance"),
            Traits =
            [
                PositiveTrait(
                    Trait.OrcaSureWake,
                    "Sure Wake",
                    "Supportive relationships restore endurance more efficiently.",
                    TraitRelationshipDelta(K.Orca, Attr.Endurance, Rel.AlliedWith, EffectRelationshipDirection.Either, 0.03f, 2, PrimaryPlane.Physical)),
                NegativeTrait(
                    Trait.OrcaBloodFrenzy,
                    "Blood Frenzy",
                    "Conflict stress burns endurance faster.",
                    TraitRelationshipDelta(K.Orca, Attr.Endurance, Rel.RivalOf, EffectRelationshipDirection.Either, -0.035f, 2, PrimaryPlane.Physical)),
            ],
            Description = "A named orca, member of a matriarchal pod. Mobile actor on the physical plane, primarily aligned with the Wake-Singers but capable of independent allegiance.",
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static KindConfig CreatePointOfInterestKind() =>
        new()
        {
            Category = EntityCategory.Spatial,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "vent", "peak", "bloom", "shoal" },
            Health = Health(Attr.Presence, "Presence"),
            Traits =
            [
                PositiveTrait(
                    Trait.PointOfInterestGenerousBounty,
                    "Generous Bounty",
                    "Supportive map signals strengthen this place.",
                    TraitMapDelta(K.PointOfInterest, Attr.Presence, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Semantic)),
                NegativeTrait(
                    Trait.PointOfInterestTreacherousApproach,
                    "Treacherous Approach",
                    "Threat signals erode this place more quickly.",
                    TraitMapDelta(K.PointOfInterest, Attr.Presence, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Semantic)),
            ],
            Description = "A named natural feature with localized significance — a thermal vent, a peak, a krill bloom, a shoal. Smaller than a region, distinct from a settlement, never built.",
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateAbilityKind() =>
        new()
        {
            Category = EntityCategory.Asset,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "technique", "rite", "craft" },
            Health = Health(Attr.Clarity, "Clarity"),
            Traits =
            [
                PositiveTrait(
                    Trait.AbilityPracticedForm,
                    "Practiced Form",
                    "Bearers recover vitality from a clear, practiced ability.",
                    TraitRelationshipDelta(K.Penguin, Attr.Vitality, Rel.Holds, EffectRelationshipDirection.Outgoing, 0.02f, 0, PrimaryPlane.Physical, TraitEffectBearer.RelatedEntity, minStrength: 0.35f)),
                NegativeTrait(
                    Trait.AbilityBrittleLineage,
                    "Brittle Lineage",
                    "Threat and repeated stress erode this ability's clarity.",
                    TraitMapDelta(K.Ability, Attr.Clarity, Effect.MapThreatAtLocation, -0.025f, 3f, PrimaryPlane.Semantic)),
            ],
            Description = "A combat technique, magical rite, or piece of technology. Represented as a transferable physical carrier — a book, a marking, a tool — but lives on the semantic plane and may be held by multiple bearers at once.",
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static KindConfig CreateMonsterKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "leviathan", "wraith", "beast", "revenant" },
            Health = Health(Attr.Containment, "Containment"),
            Traits =
            [
                PositiveTrait(
                    Trait.MonsterWellBound,
                    "Well Bound",
                    "Supportive locality effects reinforce containment.",
                    TraitMapDelta(K.Monster, Attr.Containment, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Physical)),
                NegativeTrait(
                    Trait.MonsterHungryStirring,
                    "Hungry Stirring",
                    "Threat stress erodes containment faster.",
                    TraitMapDelta(K.Monster, Attr.Containment, Effect.MapThreatAtLocation, -0.04f, 3f, PrimaryPlane.Physical)),
            ],
            Description = "A bound or partially bound peril. Containment is its health-like integrity; static traits describe whether the binding is stable or the hunger is stirring. The status of a monster (sealed, unbound, risen) is tracked separately from its subtype.",
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static KindConfig CreateTransportKind() =>
        new()
        {
            Category = EntityCategory.Asset,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "trade", "battleship", "scout", "citadel" },
            Health = Health(Attr.Integrity, "Integrity"),
            Traits =
            [
                PositiveTrait(
                    Trait.TransportProvenMake,
                    "Proven Make",
                    "Maintenance and support effects preserve integrity.",
                    TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Physical)),
                NegativeTrait(
                    Trait.TransportHiddenFault,
                    "Hidden Fault",
                    "Threat and crowding effects damage integrity faster.",
                    TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Physical),
                    TraitMapDelta(K.Transport, Attr.Integrity, Effect.MapCrowdingLoad, -0.025f, 0.5f, PrimaryPlane.Physical)),
            ],
            Description = "A named, mobile, large named asset — trade fleet, battleship, scout, or citadel. Owned and commanded by Agency entities; significant in its own right rather than an inert tool.",
            PrimaryPlane = PrimaryPlane.Physical,
        };

    private static KindConfig CreateOccurrenceKind() =>
        new()
        {
            Category = EntityCategory.Agency,
            Subtypes = new HashSet<string>(StringComparer.Ordinal) { "war", "trade_conference", "plague", "festival" },
            Health = Health(Attr.Momentum, "Momentum"),
            Traits =
            [
                PositiveTrait(
                    Trait.OccurrenceGatheringForce,
                    "Gathering Force",
                    "Local support sustains and accelerates the event's momentum.",
                    TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapSupportAtLocation, 0.03f, 3f, PrimaryPlane.Semantic)),
                NegativeTrait(
                    Trait.OccurrenceGrindingAttrition,
                    "Grinding Attrition",
                    "Local threat and crowding burn momentum away.",
                    TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapThreatAtLocation, -0.035f, 3f, PrimaryPlane.Semantic),
                    TraitMapDelta(K.Occurrence, Attr.Momentum, Effect.MapCrowdingLoad, -0.025f, 0.5f, PrimaryPlane.Semantic)),
            ],
            Description = "A named, active, major event — war, trade conference, plague, festival. Born by generators when relationship and locality gates cross thresholds; resolves when momentum reaches zero or a gambit ends it. Lives on the semantic plane (its primary movement is through cultural/conflict space) while anchored to the world via relationships.",
            PrimaryPlane = PrimaryPlane.Semantic,
        };

    private static HealthConfig Health(
        Attr attribute,
        string displayName) =>
        new()
        {
            Name = A(attribute),
            DisplayName = displayName,
            Min = 0f,
            Max = 100f,
            Default = HealthDefault,
            DefaultVariance = HealthDefaultVariance,
        };

    private static TraitConfig PositiveTrait(
        Trait trait,
        string displayName,
        string effectSummary,
        params ITraitOperationConfig[] operations) =>
        new()
        {
            Name = T(trait),
            DisplayName = displayName,
            Polarity = TraitPolarity.Positive,
            AssignmentChance = PositiveTraitAssignmentChance,
            EffectSummary = effectSummary,
            Operations = operations,
        };

    private static TraitConfig NegativeTrait(
        Trait trait,
        string displayName,
        string effectSummary,
        params ITraitOperationConfig[] operations) =>
        new()
        {
            Name = T(trait),
            DisplayName = displayName,
            Polarity = TraitPolarity.Negative,
            AssignmentChance = NegativeTraitAssignmentChance,
            EffectSummary = effectSummary,
            Operations = operations,
        };

    private static TraitAttributeDeltaOperationConfig TraitMapDelta(
        EntityKind targetKind,
        Attr attribute,
        EffectCondition condition,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            TargetKind = targetKind.Value,
            Attribute = A(attribute),
            Condition = condition,
            TraitBearer = TraitEffectBearer.Target,
            Plane = GameplayPlaneName(plane),
            Threshold = threshold,
            DeltaPerTick = deltaPerTick,
        };

    private static TraitAttributeDeltaOperationConfig TraitRelationshipDelta(
        EntityKind targetKind,
        Attr attribute,
        Rel relationship,
        EffectRelationshipDirection direction,
        float deltaPerTick,
        int maxDistance,
        PrimaryPlane plane,
        TraitEffectBearer bearer = TraitEffectBearer.Target,
        float minStrength = 0.15f) =>
        new()
        {
            TargetKind = targetKind.Value,
            Attribute = A(attribute),
            Condition = Effect.RelatedEntityDistance,
            TraitBearer = bearer,
            RelationshipKind = L(relationship),
            RelationshipDirection = direction,
            Plane = GameplayPlaneName(plane),
            MaxDistance = maxDistance,
            MinimumRelationshipStrength = minStrength,
            DeltaPerTick = deltaPerTick,
        };

    private static TraitLowAltitudeWeightOperationConfig TraitVerbWeight(
        Verb verb,
        float weightDelta = 0f,
        float exploreDelta = 0f,
        float stalenessDelta = 0f,
        float hotspotDelta = 0f,
        float crowdingDelta = 0f,
        float generatorDelta = 0f,
        float relationshipAffinityDelta = 0f) =>
        new()
        {
            Verb = V(verb),
            WeightDelta = weightDelta,
            ExploreWeightDelta = exploreDelta,
            StalenessWeightDelta = stalenessDelta,
            HotspotWeightDelta = hotspotDelta,
            CrowdingWeightDelta = crowdingDelta,
            GeneratorAffordanceWeightDelta = generatorDelta,
            RelationshipAffinityWeightDelta = relationshipAffinityDelta,
        };

    private static TraitGambitModifierOperationConfig TraitGambit(
        GambitId gambit,
        float scoreDelta,
        float successDelta,
        TraitEffectBearer bearer = TraitEffectBearer.Actor) =>
        new()
        {
            Gambit = G(gambit),
            TraitBearer = bearer,
            ScoreDelta = scoreDelta,
            SuccessChanceDelta = successDelta,
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

    private static Dictionary<EntityKind, LifecycleConfig> CreateLifecycles()
    {
        return new Dictionary<EntityKind, LifecycleConfig>
        {
            [K.Settlement] = Lifecycle(Attr.TempHp, TargetLifespan.High),
            [K.Faction] = Lifecycle(Attr.Cohesion, TargetLifespan.Mid),
            [K.Penguin] = Lifecycle(Attr.Vitality, TargetLifespan.Short),
            [K.Artifact] = Lifecycle(Attr.Condition, TargetLifespan.High),
            [K.Region] = Lifecycle(Attr.Stability, TargetLifespan.Mid),
            [K.Orca] = Lifecycle(Attr.Endurance, TargetLifespan.Short),
            [K.PointOfInterest] = Lifecycle(Attr.Presence, TargetLifespan.High),
            [K.Ability] = Lifecycle(Attr.Clarity, TargetLifespan.Mid),
            [K.Monster] = Lifecycle(Attr.Containment, TargetLifespan.High),
            [K.Transport] = Lifecycle(Attr.Integrity, TargetLifespan.Mid),
            [K.Occurrence] = Lifecycle(Attr.Momentum, TargetLifespan.Mid),
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
            Health = A(healthAttribute),
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
        AddHealthEffects(effects, K.Orca, Attr.Endurance, PrimaryPlane.Physical);
        AddHealthEffects(effects, K.PointOfInterest, Attr.Presence, PrimaryPlane.Semantic);
        AddHealthEffects(effects, K.Ability, Attr.Clarity, PrimaryPlane.Semantic);
        AddHealthEffects(effects, K.Monster, Attr.Containment, PrimaryPlane.Physical);
        AddHealthEffects(effects, K.Transport, Attr.Integrity, PrimaryPlane.Physical);
        AddHealthEffects(effects, K.Occurrence, Attr.Momentum, PrimaryPlane.Semantic);
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
                KindSignal(K.Orca, Attr.Endurance, radius: 2, capacity: 0.4f, support: 0.3f, threat: 0.6f,
                    baseMass: 5f, coverageScale: 0f, influenceScale: 0.03f, activityScale: 0.04f, healthScale: 0.04f, attractorRadius: 2),
                KindSignal(K.PointOfInterest, Attr.Presence, radius: 1, capacity: 0.6f, support: 0.5f, threat: 0f,
                    baseMass: 4f, coverageScale: 0f, influenceScale: 0.03f, activityScale: 0.02f, healthScale: 0.04f, attractorRadius: 1),
                KindSignal(K.Ability, Attr.Clarity, radius: 1, capacity: 0.2f, support: 0.3f, threat: 0f,
                    baseMass: 3f, coverageScale: 0f, influenceScale: 0.02f, activityScale: 0.02f, healthScale: 0.03f, attractorRadius: 1),
                KindSignal(K.Monster, Attr.Containment, radius: 2, capacity: 0.3f, support: 0f, threat: 1.0f,
                    baseMass: 6f, coverageScale: 0f, influenceScale: 0.03f, activityScale: 0.04f, healthScale: 0.05f, attractorRadius: 2),
                KindSignal(K.Transport, Attr.Integrity, radius: 2, capacity: 0.5f, support: 0.6f, threat: 0.4f,
                    baseMass: 8f, coverageScale: 0f, influenceScale: 0.05f, activityScale: 0.06f, healthScale: 0.06f, attractorRadius: 2),
                KindSignal(K.Occurrence, Attr.Momentum, radius: 2, capacity: 0.4f, support: 0.5f, threat: 0.5f,
                    baseMass: 5f, coverageScale: 0f, influenceScale: 0.04f, activityScale: 0.05f, healthScale: 0.04f, attractorRadius: 2),
            ],
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
