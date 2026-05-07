namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Domain.Graph;
using Engine.Gameplay;
using Engine.Kinds;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Effect = Engine.Gameplay.AttributeEffectConditions;
using GambitId = IceRemembers.IceRemembersGambitId;
using Profile = IceRemembers.IceRemembersMovementProfile;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Trait = IceRemembers.IceRemembersTraitId;
using Verb = IceRemembers.IceRemembersVerbId;

public enum IceRemembersLifecycleBand
{
    Brief,
    Standard,
    Enduring,
}

public sealed class IceRemembersEntityDefinition
{
    public required EntityKind Kind { get; init; }
    public required EntityCategory Category { get; init; }
    public required IReadOnlySet<string> Subtypes { get; init; }
    public required Attr HealthAttribute { get; init; }
    public required string HealthDisplayName { get; init; }
    public required PrimaryPlane PrimaryPlane { get; init; }
    public required IceRemembersLifecycleBand LifecycleBand { get; init; }
    public IReadOnlyList<TraitConfig> Traits { get; init; } = [];
    public string? Description { get; init; }
    public IReadOnlyList<PrimaryPlane> InitialInfluencePlanes { get; init; } = [PrimaryPlane.Semantic];
    public required IceRemembersKindMapSignalProfile MapSignal { get; init; }
    public bool CreatesSemanticStock { get; init; } = true;
    public string SemanticStockNotes { get; init; } = "Health (integrity). Dies at 0.";
    public required MovementProfileAssignmentConfig MovementProfiles { get; init; }
    public required IceRemembersEntityPlacementProfiles Placement { get; init; }
}

public sealed record IceRemembersKindMapSignalProfile(
    int Radius,
    float Capacity,
    float Support,
    float Threat,
    float BaseMass,
    float CoverageScale,
    float InfluenceScale,
    float ActivityScale,
    float HealthScale,
    int AttractorRadius);

public sealed record IceRemembersPlacementTerrainProfile(
    IReadOnlySet<string> PreferredTerrains,
    IReadOnlyList<string> PreferredTerrainOrder,
    IReadOnlySet<string> AvoidedTerrains,
    string? FallbackTerrain);

public sealed record IceRemembersTerrainProfile(
    IReadOnlyList<Terrain> PreferredTerrains,
    IReadOnlyList<Terrain> AvoidedTerrains,
    Terrain? FallbackTerrain)
{
    public IceRemembersPlacementTerrainProfile ToPlacementProfile()
    {
        var preferred = TerrainNames(PreferredTerrains);
        var avoided = TerrainNames(AvoidedTerrains);
        return new IceRemembersPlacementTerrainProfile(
            new HashSet<string>(preferred, StringComparer.Ordinal),
            preferred,
            new HashSet<string>(avoided, StringComparer.Ordinal),
            FallbackTerrain?.ToDomainName());
    }

    private static List<string> TerrainNames(IEnumerable<Terrain> terrains)
    {
        var result = new List<string>();
        foreach (var terrain in terrains)
        {
            result.Add(terrain.ToDomainName());
        }

        return result;
    }
}

public sealed record IceRemembersTerrainProfileRule(
    string? Subtype,
    string? Culture,
    IceRemembersTerrainProfile Profile)
{
    public bool Matches(string? subtype, string? culture) =>
        (Subtype is null || string.Equals(Subtype, subtype, StringComparison.Ordinal)) &&
        (Culture is null || string.Equals(Culture, culture, StringComparison.Ordinal));
}

public sealed class IceRemembersEntityPlacementProfiles
{
    public required IceRemembersTerrainProfile PhysicalDefault { get; init; }
    public IReadOnlyList<IceRemembersTerrainProfileRule> PhysicalRules { get; init; } = [];
    public required IceRemembersTerrainProfile SemanticDefault { get; init; }
    public IReadOnlyList<IceRemembersTerrainProfileRule> SemanticRules { get; init; } = [];

    public IceRemembersPlacementTerrainProfile Physical(string? subtype, string? culture) =>
        ResolvePhysical(subtype, culture).ToPlacementProfile();

    public IceRemembersPlacementTerrainProfile Semantic(string? subtype, string? culture) =>
        ResolveSemantic(subtype, culture).ToPlacementProfile();

    public IceRemembersTerrainProfile ResolvePhysical(string? subtype, string? culture) =>
        Resolve(PhysicalDefault, PhysicalRules, subtype, culture);

    public IceRemembersTerrainProfile ResolveSemantic(string? subtype, string? culture) =>
        Resolve(SemanticDefault, SemanticRules, subtype, culture);

    private static IceRemembersTerrainProfile Resolve(
        IceRemembersTerrainProfile fallback,
        IReadOnlyList<IceRemembersTerrainProfileRule> rules,
        string? subtype,
        string? culture)
    {
        foreach (var rule in rules)
        {
            if (rule.Matches(subtype, culture))
            {
                return rule.Profile;
            }
        }

        return fallback;
    }
}

public static class IceRemembersPlacementTerrainProfiles
{
    public static IceRemembersPlacementTerrainProfile Physical(
        EntityKind kind,
        string? subtype,
        string? culture = null) =>
        IceRemembersEntities.Entity(kind).Placement.Physical(subtype, culture);

    public static IceRemembersPlacementTerrainProfile Semantic(
        EntityKind kind,
        string? subtype,
        string? culture = null) =>
        IceRemembersEntities.Entity(kind).Placement.Semantic(subtype, culture);

    public static IReadOnlySet<string> SemanticPreferences(
        EntityKind kind,
        string? subtype,
        string? culture = null) =>
        Semantic(kind, subtype, culture).PreferredTerrains;

    public static IReadOnlyList<Terrain> SemanticPreferenceTerrains(
        EntityKind kind,
        string? subtype,
        string? culture = null) =>
        IceRemembersEntities.Entity(kind).Placement.ResolveSemantic(subtype, culture).PreferredTerrains;
}

public static class IceRemembersEntities
{
    private const float InitialInfluenceValue = 35f;
    private const float HealthDefault = 90f;
    private const float HealthDefaultVariance = 1f / 9f;
    private const float NominalInitialHealth = 90f;
    private const float LifecycleFlatDrainShare = 0.2f;

    private static readonly IReadOnlyList<EntityKind> KindSignalOrder =
    [
        IceRemembersKinds.Settlement,
        IceRemembersKinds.Faction,
        IceRemembersKinds.Penguin,
        IceRemembersKinds.Artifact,
        IceRemembersKinds.Region,
        IceRemembersKinds.Orca,
        IceRemembersKinds.PointOfInterest,
        IceRemembersKinds.Ability,
        IceRemembersKinds.Monster,
        IceRemembersKinds.Transport,
        IceRemembersKinds.Occurrence,
    ];

    public static IReadOnlyList<IceRemembersEntityDefinition> All { get; } =
    [
        RegionEntity.Definition,
        SettlementEntity.Definition,
        FactionEntity.Definition,
        PenguinEntity.Definition,
        ArtifactEntity.Definition,
        OrcaEntity.Definition,
        PointOfInterestEntity.Definition,
        AbilityEntity.Definition,
        MonsterEntity.Definition,
        TransportEntity.Definition,
        OccurrenceEntity.Definition,
    ];

    private static readonly Dictionary<EntityKind, IceRemembersEntityDefinition> ByKind =
        All.ToDictionary(entity => entity.Kind);

    public static IceRemembersEntityDefinition Entity(EntityKind kind) =>
        ByKind.TryGetValue(kind, out var entity)
            ? entity
            : throw new ArgumentException($"Unknown Ice Remembers entity kind '{kind.Value}'.", nameof(kind));

    public static Dictionary<EntityKind, KindConfig> CreateKindConfigs()
    {
        var result = new Dictionary<EntityKind, KindConfig>();
        foreach (var entity in All)
        {
            result.Add(entity.Kind, new KindConfig
            {
                Category = entity.Category,
                Subtypes = entity.Subtypes,
                Health = Health(entity.HealthAttribute, entity.HealthDisplayName),
                Traits = entity.Traits,
                MovementProfiles = entity.MovementProfiles,
                Description = entity.Description,
                PrimaryPlane = entity.PrimaryPlane,
            });
        }

        return result;
    }

    public static IReadOnlyList<InitialInfluenceRule> CreateInitialInfluenceRules()
    {
        var result = new List<InitialInfluenceRule>();
        foreach (var entity in All)
        {
            foreach (var plane in entity.InitialInfluencePlanes)
            {
                result.Add(new InitialInfluenceRule(
                    entity.Kind.Value,
                    GameplayPlaneName(plane),
                    InitialInfluenceValue));
            }
        }

        return result;
    }

    public static Dictionary<EntityKind, LifecycleConfig> CreateLifecycleConfigs()
    {
        var result = new Dictionary<EntityKind, LifecycleConfig>();
        foreach (var entity in All)
        {
            result.Add(entity.Kind, Lifecycle(entity.HealthAttribute, entity.LifecycleBand));
        }

        return result;
    }

    public static IReadOnlyList<KindMapSignalConfig> CreateKindMapSignals()
    {
        var result = new List<KindMapSignalConfig>();
        foreach (var kind in KindSignalOrder)
        {
            var entity = Entity(kind);
            var signal = entity.MapSignal;
            result.Add(new KindMapSignalConfig
            {
                Kind = entity.Kind.Value,
                SignalRadius = signal.Radius,
                CapacityEmission = signal.Capacity,
                SupportEmission = signal.Support,
                ThreatEmission = signal.Threat,
                BaseAttractorMass = signal.BaseMass,
                CoverageAttractorScale = signal.CoverageScale,
                InfluenceAttractorScale = signal.InfluenceScale,
                ActivityAttractorScale = signal.ActivityScale,
                AttractorRadius = signal.AttractorRadius,
                AttractorScalarInputs =
                [
                    new MapSignalScalarInputConfig
                    {
                        Attribute = entity.HealthAttribute.ToDomainName(),
                        AttractorScale = signal.HealthScale,
                    },
                ],
            });
        }

        return result;
    }

    public static IReadOnlyList<SemanticStockConfig> CreateSemanticStocks()
    {
        var result = new List<SemanticStockConfig>();
        foreach (var entity in All)
        {
            if (!entity.CreatesSemanticStock)
            {
                continue;
            }

            result.Add(new SemanticAttributeStockConfig
            {
                Kind = entity.Kind,
                AttributeName = entity.HealthAttribute.ToDomainName(),
                StockClass = SemanticStockClass.Anchor,
                FeedbackClass = SemanticFeedbackClass.Anchor,
                RequiredFlowCoverage = SemanticFlowCoverageRequirement.None,
                BoundsLabel = "0..100",
                ConservationLabel = "anchor",
                Notes = entity.SemanticStockNotes,
            });
        }

        return result;
    }

    private static HealthConfig Health(
        Attr attribute,
        string displayName) =>
        new()
        {
            Name = attribute.ToDomainName(),
            DisplayName = displayName,
            Min = 0f,
            Max = 100f,
            Default = HealthDefault,
            DefaultVariance = HealthDefaultVariance,
        };

    private static LifecycleConfig Lifecycle(Attr healthAttribute, IceRemembersLifecycleBand lifespan)
    {
        var targetTicks = lifespan switch
        {
            IceRemembersLifecycleBand.Brief => 500f,
            IceRemembersLifecycleBand.Standard => 800f,
            IceRemembersLifecycleBand.Enduring => 1200f,
            _ => throw new ArgumentOutOfRangeException(nameof(lifespan), lifespan, null),
        };

        var baseRate = (NominalInitialHealth * LifecycleFlatDrainShare) / targetTicks;
        var ageCoefficient = (NominalInitialHealth * (1f - LifecycleFlatDrainShare) * 2f) / (targetTicks * targetTicks);
        return new LifecycleConfig
        {
            Health = healthAttribute.ToDomainName(),
            BaseRate = baseRate,
            AgeCoefficient = ageCoefficient,
        };
    }

    internal static string GameplayPlaneName(PrimaryPlane plane) =>
        plane == PrimaryPlane.Semantic ? "semantic" : "physical";
}

internal static class IceRemembersEntityDsl
{
    private const float PositiveTraitAssignmentChance = 0.35f;
    private const float NegativeTraitAssignmentChance = 0.25f;

    private static readonly IReadOnlyList<Terrain> SemanticHostileTerrains =
    [
        Terrain.Severance,
        Terrain.TabooRidge,
        Terrain.CavePocket,
        Terrain.Silence,
    ];

    public static IReadOnlySet<string> Subtypes(params string[] subtypes) =>
        new HashSet<string>(subtypes, StringComparer.Ordinal);

    public static IceRemembersKindMapSignalProfile MapSignal(
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
        new(
            radius,
            capacity,
            support,
            threat,
            baseMass,
            coverageScale,
            influenceScale,
            activityScale,
            healthScale,
            attractorRadius);

    public static IceRemembersEntityPlacementProfiles Placement(
        IceRemembersTerrainProfile physicalDefault,
        IceRemembersTerrainProfile semanticDefault,
        IReadOnlyList<IceRemembersTerrainProfileRule>? physicalRules = null,
        IReadOnlyList<IceRemembersTerrainProfileRule>? semanticRules = null) =>
        new()
        {
            PhysicalDefault = physicalDefault,
            PhysicalRules = physicalRules ?? [],
            SemanticDefault = semanticDefault,
            SemanticRules = semanticRules ?? [],
        };

    public static MovementProfileAssignmentConfig MovementProfiles(
        Profile physicalDefault,
        Profile semanticDefault,
        IReadOnlyList<MovementProfileSubtypeRuleConfig>? subtypeRules = null,
        IReadOnlyList<MovementProfileTraitRuleConfig>? traitRules = null) =>
        new()
        {
            PhysicalDefault = physicalDefault.ToDomainName(),
            SemanticDefault = semanticDefault.ToDomainName(),
            SubtypeRules = subtypeRules ?? [],
            TraitRules = traitRules ?? [],
        };

    public static MovementProfileSubtypeRuleConfig MovementSubtypeRule(
        string subtype,
        Profile? physical = null,
        Profile? semantic = null) =>
        new()
        {
            Subtype = subtype,
            PhysicalProfile = physical?.ToDomainName(),
            SemanticProfile = semantic?.ToDomainName(),
        };

    public static MovementProfileTraitRuleConfig MovementTraitRule(
        Trait trait,
        Profile? physical = null,
        Profile? semantic = null) =>
        new()
        {
            Trait = trait.ToDomainName(),
            PhysicalProfile = physical?.ToDomainName(),
            SemanticProfile = semantic?.ToDomainName(),
        };

    public static IceRemembersTerrainProfile Land(params Terrain[] preferred) =>
        new(preferred, [Terrain.Ocean, Terrain.Reef], Terrain.Tundra);

    public static IceRemembersTerrainProfile Maritime(params Terrain[] preferred) =>
        new(preferred, [Terrain.Wastes, Terrain.Crags], Terrain.Tundra);

    public static IceRemembersTerrainProfile Semantic(params Terrain[] preferred) =>
        new(preferred, SemanticHostileTerrains, Terrain.OpenField);

    public static IceRemembersTerrainProfileRule SubtypeRule(
        string subtype,
        IceRemembersTerrainProfile profile) =>
        new(subtype, null, profile);

    public static IceRemembersTerrainProfileRule CultureRule(
        string culture,
        IceRemembersTerrainProfile profile) =>
        new(null, culture, profile);

    public static TraitConfig PositiveTrait(
        Trait trait,
        string displayName,
        string effectSummary,
        params ITraitOperationConfig[] operations) =>
        new()
        {
            Name = trait.ToDomainName(),
            DisplayName = displayName,
            Polarity = TraitPolarity.Positive,
            AssignmentChance = PositiveTraitAssignmentChance,
            EffectSummary = effectSummary,
            Operations = operations,
        };

    public static TraitConfig NegativeTrait(
        Trait trait,
        string displayName,
        string effectSummary,
        params ITraitOperationConfig[] operations) =>
        new()
        {
            Name = trait.ToDomainName(),
            DisplayName = displayName,
            Polarity = TraitPolarity.Negative,
            AssignmentChance = NegativeTraitAssignmentChance,
            EffectSummary = effectSummary,
            Operations = operations,
        };

    public static TraitAttributeDeltaOperationConfig TraitMapDelta(
        EntityKind targetKind,
        Attr attribute,
        EffectCondition condition,
        float deltaPerTick,
        float threshold,
        PrimaryPlane plane) =>
        new()
        {
            TargetKind = targetKind.Value,
            Attribute = attribute.ToDomainName(),
            Condition = condition,
            TraitBearer = TraitEffectBearer.Target,
            Plane = IceRemembersEntities.GameplayPlaneName(plane),
            Threshold = threshold,
            DeltaPerTick = deltaPerTick,
        };

    public static TraitAttributeDeltaOperationConfig TraitRelationshipDelta(
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
            Attribute = attribute.ToDomainName(),
            Condition = Effect.RelatedEntityDistance,
            TraitBearer = bearer,
            RelationshipKind = relationship.ToDomainName(),
            RelationshipDirection = direction,
            Plane = IceRemembersEntities.GameplayPlaneName(plane),
            MaxDistance = maxDistance,
            MinimumRelationshipStrength = minStrength,
            DeltaPerTick = deltaPerTick,
        };

    public static TraitLowAltitudeWeightOperationConfig TraitVerbWeight(
        Verb verb,
        float weightDelta = 0f,
        float opportunityBonusDelta = 0f,
        float fitnessWeightDelta = 0f) =>
        new()
        {
            Verb = verb.ToVerbId(),
            WeightDelta = weightDelta,
            OpportunityBonusDelta = opportunityBonusDelta,
            FitnessWeightDelta = fitnessWeightDelta,
        };

    public static TraitGambitModifierOperationConfig TraitGambit(
        GambitId gambit,
        float scoreDelta,
        float successDelta,
        TraitEffectBearer bearer = TraitEffectBearer.Actor) =>
        new()
        {
            Gambit = gambit.ToGambitId(),
            TraitBearer = bearer,
            ScoreDelta = scoreDelta,
            SuccessChanceDelta = successDelta,
        };
}
