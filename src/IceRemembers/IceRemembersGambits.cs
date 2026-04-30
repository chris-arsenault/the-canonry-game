namespace IceRemembers;

using Engine.Behaviors;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Graph;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

public static partial class IceRemembersDefinition
{
    private static IReadOnlyList<GambitDefinition> CreateGambits() =>
    [
        PenguinWieldsArtifact(),
        FactionDeclaresWar(),
        FactionBetraysOath(),
        PenguinSlaysRival(),
    ];

    private static GambitDefinition PenguinWieldsArtifact()
    {
        var gambit = NewGambit(
            GambitId.PenguinWieldsArtifact,
            "Wield an Artifact",
            K.Penguin,
            K.Artifact,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(1, PrimaryPlane.Physical),
                Not(new SelfHasRelationshipToTarget(L(Rel.Holds)))),
            score: 0.8f,
            reward: 8f,
            success: 0.78f,
            cooldown: 90,
            entityCooldown: 18);
        AddRelationshipContract(gambit, Rel.Holds, K.Artifact);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Echo), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Power), K.Artifact));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.Holds, RelationshipPolarity.Neutral, 0.82f));
        return gambit;
    }

    private static GambitDefinition FactionDeclaresWar()
    {
        var gambit = NewGambit(
            GambitId.FactionDeclaresWar,
            "Declare War Across the Ice",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                BeyondPlaneDistance(10, PrimaryPlane.Semantic),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new TargetHasRelationshipToSelf(L(Rel.AtWarWith))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 35f, max: null)),
            score: 0.55f,
            reward: 12f,
            success: 0.86f,
            cooldown: 140,
            entityCooldown: 30);
        AddRelationshipContract(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        return gambit;
    }

    private static GambitDefinition FactionBetraysOath()
    {
        var gambit = NewGambit(
            GambitId.FactionBetraysOath,
            "Betray an Oath",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.AlliedWith), minStrength: 0.35f),
                BeyondPlaneDistance(5, PrimaryPlane.Semantic),
                Not(new SelfHasRelationshipToTarget(L(Rel.BetrayedBy)))),
            score: 0.35f,
            reward: 9f,
            success: 0.68f,
            cooldown: 120,
            entityCooldown: 24);
        gambit.ProviderSelectors.Add(AttributeRef.TargetRelation(L(Rel.AlliedWith), K.Faction));
        AddRelationshipContract(gambit, Rel.BetrayedBy, K.Faction);
        gambit.Reads.Add(AttributeRef.TargetRelation(L(Rel.AlliedWith), K.Faction));
        gambit.Writes.Add(AttributeRef.TargetRelation(L(Rel.AlliedWith), K.Faction));
        gambit.Mutations.Add(new SetContextRelationshipStrengthMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AlliedWith), 0f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.BetrayedBy, RelationshipPolarity.Negative, 0.76f));
        return gambit;
    }

    private static GambitDefinition PenguinSlaysRival()
    {
        var gambit = NewGambit(
            GambitId.PenguinSlaysRival,
            "Slay a Rival",
            K.Penguin,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.RivalOf), minStrength: 0.25f),
                new WithinPlaneDistancePrecondition(1, PrimaryPlane.Physical),
                new TargetAttributeThreshold(A(Attr.Vitality), min: null, max: 55f)),
            score: 0.42f,
            reward: 10f,
            success: 0.58f,
            cooldown: 110,
            entityCooldown: 28);
        gambit.ProviderSelectors.Add(AttributeRef.TargetRelation(L(Rel.RivalOf), K.Penguin));
        AddRelationshipContract(gambit, Rel.BetrayedBy, K.Penguin);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new SetTargetHistoricalMutation());
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.BetrayedBy, RelationshipPolarity.Negative, 0.64f));
        return gambit;
    }

    private static GambitDefinition NewGambit(
        GambitId id,
        string name,
        EntityKind actorKind,
        EntityKind targetKind,
        IPrecondition precondition,
        float score,
        float reward,
        float success,
        int cooldown,
        int entityCooldown)
    {
        var gambit = new GambitDefinition(G(id), name, actorKind, precondition, Expr.Lit(score))
        {
            RewardExpression = Expr.Lit(reward),
            SuccessChance = success,
            Cooldown = cooldown,
            EntityCooldown = entityCooldown,
        };
        gambit.ProviderSelectors.Add(AttributeRef.Subtype(targetKind));
        gambit.Reads.Add(AttributeRef.Coordinates(actorKind));
        gambit.Reads.Add(AttributeRef.Coordinates(targetKind));
        return gambit;
    }

    private static void AddRelationshipContract(
        GambitDefinition gambit,
        Rel relationship,
        EntityKind targetKind)
    {
        var relation = AttributeRef.TargetRelation(L(relationship), targetKind);
        gambit.GoalEffects.Add(relation);
        gambit.Writes.Add(relation);
    }

    private static AndPrecondition All(params IPrecondition[] preconditions) =>
        new AndPrecondition(preconditions);

    private static NotPrecondition Not(IPrecondition precondition) =>
        new NotPrecondition(precondition);

    private static NotPrecondition BeyondPlaneDistance(int maxDistance, PrimaryPlane plane) =>
        Not(new WithinPlaneDistancePrecondition(maxDistance, plane));

    private static CreateContextRelationshipMutation Relationship(
        TransferEndpoint source,
        TransferEndpoint target,
        Rel relationship,
        RelationshipPolarity polarity,
        float strength) =>
        new(source, target, L(relationship), polarity, strength);
}
