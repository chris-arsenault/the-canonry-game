namespace IceRemembers;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Graph;
using Engine.Wiring;

using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;

// Deepen the Holding reborn as stage promotion: only the four catalog
// kinds climb their named ladders, at standard success with hard gates
// (docs/design/entity-stages.md). The 12-kind tier grind — measured at
// half of all gambit executions while the mesh's real growth came from
// entity movement — is retired.
public static partial class IceRemembersDefinition
{
    public static readonly IReadOnlyList<EntityKind> StageCatalogKinds =
        [K.Settlement, K.Faction, K.Artifact, K.Canon];

    private const int StageCatalogMaxIndex = 2;

    private static readonly Dictionary<EntityKind, GambitId> WorkTierGambitIds = new()
    {
        [K.Settlement] = GambitId.SettlementDeepensTheHolding,
        [K.Faction] = GambitId.FactionDeepensTheHolding,
        [K.Artifact] = GambitId.ArtifactDeepensTheHolding,
        [K.Canon] = GambitId.CanonDeepensTheHolding,
    };

    private static readonly Dictionary<EntityKind, GambitId> CrownGambitIds = new()
    {
        [K.Settlement] = GambitId.SettlementCrownsTheHolding,
        [K.Faction] = GambitId.FactionCrownsTheHolding,
        [K.Artifact] = GambitId.ArtifactCrownsTheHolding,
        [K.Canon] = GambitId.CanonCrownsTheHolding,
    };

    private static IEnumerable<GambitDefinition> CreateWorkTierGambits()
    {
        foreach (var entity in IceRemembersEntities.All)
        {
            if (StageCatalogKinds.Contains(entity.Kind))
            {
                yield return EntityDeepensTheHolding(entity);
                yield return EntityCrownsTheHolding(entity);
            }
        }
    }

    // The two promotion rungs carry the pyramid: the first stage is earned,
    // the last is exceptional. Gates scale per stage per the design
    // (docs/design/entity-stages.md); round one shipped a flat gate and the
    // pyramid inverted (77% of promoted artifacts reached legend by 100k).
    private static GambitDefinition EntityDeepensTheHolding(IceRemembersEntityDefinition entity) =>
        EntityStageGambit(
            entity,
            WorkTierGambitId(entity.Kind),
            "Deepen the Holding",
            stageBelow: 1,
            massMultiplier: 3f,
            connectedMultiplier: 2f,
            healthMinimum: 55f,
            cooldown: 400,
            entityCooldown: 600,
            description: "A thriving holding rises to its middle stage - town to city, relic to famed. Rare, earned, and consequential: the stage sets its clock, its keepers, and its voice.");

    private static GambitDefinition EntityCrownsTheHolding(IceRemembersEntityDefinition entity) =>
        EntityStageGambit(
            entity,
            CrownGambitId(entity.Kind),
            "Crown the Holding",
            stageBelow: StageCatalogMaxIndex,
            requiredStage: 1,
            massMultiplier: 8f,
            connectedMultiplier: 4f,
            healthMinimum: 70f,
            cooldown: 800,
            entityCooldown: 1600,
            description: "A holding at its height reaches for its final stage - city to capital, famed to legend. The last rung is the steepest; few ever stand on it.");

    private static GambitDefinition EntityStageGambit(
        IceRemembersEntityDefinition entity,
        GambitId gambitId,
        string name,
        int stageBelow,
        float massMultiplier,
        float connectedMultiplier,
        float healthMinimum,
        int cooldown,
        int entityCooldown,
        string description,
        int requiredStage = 0)
    {
        var health = A(entity.HealthAttribute);
        IPrecondition tierGate = requiredStage > 0
            ? All(
                new SelfWorkTierBelowPrecondition(stageBelow),
                Not(new SelfWorkTierBelowPrecondition(requiredStage)))
            : new SelfWorkTierBelowPrecondition(stageBelow);
        var gambit = new GambitDefinition(
            G(gambitId),
            name,
            entity.Kind,
            All(
                new TargetIsSelf(),
                tierGate,
                new SelfCultureMassPrecondition(
                    NonMobilePrimaryPlane(entity.PrimaryPlane),
                    CulturalWorkTierMinimumRawMass * massMultiplier,
                    CulturalWorkTierMinimumConnectedRawMass * connectedMultiplier),
                new SelfAttributeThreshold(health, min: healthMinimum, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.35f,
                dynamicRange: 4f,
                Normalized(Expr.Self(health))))
        {
            SuccessChance = InitialGambitSuccessChance,
            Cooldown = cooldown,
            EntityCooldown = entityCooldown,
            Description = description,
        };
        gambit.Reads.Add(AttributeRef.Self(health, entity.Kind));
        gambit.Reads.Add(AttributeRef.Coordinates(entity.Kind));
        gambit.Reads.Add(AttributeRef.StructuralFact("work_tier", entity.Kind));
        gambit.Writes.Add(AttributeRef.StructuralFact("work_tier", entity.Kind));
        gambit.Mutations.Add(new IncreaseWorkTierMutation(maxTier: StageCatalogMaxIndex));
        return gambit;
    }

    private static GambitId WorkTierGambitId(EntityKind kind)
    {
        return WorkTierGambitIds.TryGetValue(kind, out var gambitId)
            ? gambitId
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Ice Remembers entity kind.");
    }

    private static GambitId CrownGambitId(EntityKind kind)
    {
        return CrownGambitIds.TryGetValue(kind, out var gambitId)
            ? gambitId
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Kind has no crown stage.");
    }
}
