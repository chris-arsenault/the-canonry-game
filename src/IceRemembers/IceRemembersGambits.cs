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
using Terrain = IceRemembers.IceRemembersTerrain;

public static partial class IceRemembersDefinition
{
    private const float InitialGambitSuccessChance = 0.25f;

    private static IReadOnlyList<GambitDefinition> CreateGambits() =>
    [
        PenguinWieldsArtifact(),
        FactionDeclaresTerritorialWar(),
        SettlementOpensTradeRoute(),
        ArtifactCleansesScar(),
        FactionSuesForPeace(),
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
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.Holds), maximumExclusive: 1),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.Holds), maximumExclusive: 1)),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Target(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 90,
            entityCooldown: 18);
        AddRelationshipWrite(gambit, Rel.Holds, K.Artifact);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.Holds, RelationshipPolarity.Neutral, 1f));
        gambit.Mutations.Add(new MoveTargetToSelfPhysicalMutation());
        gambit.Description = "A penguin within reach of an unclaimed artifact picks it up. The bond binds them physically — the artifact follows.";
        return gambit;
    }

    private static GambitDefinition FactionDeclaresTerritorialWar()
    {
        var gambit = NewGambit(
            GambitId.FactionDeclaresTerritorialWar,
            "Declare Territorial War",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new ContestedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 12f, targetMinimum: 55f),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new TargetHasRelationshipToSelf(L(Rel.AtWarWith))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 45f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 140,
            entityCooldown: 30);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        gambit.Description = "Two factions contesting the same ground formalise their conflict. War is declared in both directions; cohesion sours, the ice begins to record.";
        return gambit;
    }

    private static GambitDefinition SettlementOpensTradeRoute()
    {
        var gambit = NewGambit(
            GambitId.SettlementOpensTradeRoute,
            "Open a Trade Route",
            K.Settlement,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical),
                new WithinPlaneDistancePrecondition(3, PrimaryPlane.Semantic),
                new SharedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 20f, targetMinimum: 20f),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.TradeRoute), maximumExclusive: 2),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.TradeRoute), maximumExclusive: 2),
                Not(new SelfHasRelationshipToTarget(L(Rel.TradeRoute))),
                Not(new TargetHasRelationshipToSelf(L(Rel.TradeRoute))),
                new SelfAttributeThreshold(A(Attr.TempHp), min: 45f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.TempHp), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 4f,
                Mean2(
                    Normalized(Expr.Self(A(Attr.TempHp))),
                    Normalized(Expr.Target(A(Attr.TempHp))))),
            success: InitialGambitSuccessChance,
            cooldown: 110,
            entityCooldown: 24);
        AddRelationshipWrite(gambit, Rel.TradeRoute, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.TradeRoute, RelationshipPolarity.Positive, 0.72f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.TradeRoute, RelationshipPolarity.Positive, 0.72f));
        gambit.Description = "Two viable settlements with overlapping influence open a trade route. Wares move; brokers begin to read the path.";
        return gambit;
    }

    private static GambitDefinition ArtifactCleansesScar()
    {
        var gambit = NewGambit(
            GambitId.ArtifactCleansesScar,
            "Cleanse a Scar",
            K.Artifact,
            K.Region,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(2, PrimaryPlane.Physical),
                Not(new SelfHasRelationshipToTarget(L(Rel.CleansedBy))),
                new SelfHasRelationshipToTarget(L(Rel.Anchored), minStrength: 0.45f, behaviorRelevant: false),
                new SelfAttributeThreshold(A(Attr.Condition), min: 40f, max: null, behaviorRelevant: false),
                Not(new TargetHasRelationshipToSelf(L(Rel.CleansedBy)))),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 30);
        AddRelationshipWrite(gambit, Rel.CleansedBy, K.Region);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        gambit.Reads.Add(AttributeRef.TargetRelation(L(Rel.Anchored), K.Region));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.CleansedBy, RelationshipPolarity.Positive, 0.78f));
        gambit.Mutations.Add(new SetTargetCoverageTerrainMutation(Terrain.HotSprings.ToDomainName()));
        gambit.Description = "An intact artifact with a strong anchor to a nearby region cleanses the scar. The ice forgets the cold; hot springs surface.";
        return gambit;
    }

    private static GambitDefinition FactionSuesForPeace()
    {
        var gambit = NewGambit(
            GambitId.FactionSuesForPeace,
            "Sue for Peace",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: null, max: 35f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 160,
            entityCooldown: 40);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.BetrayedBy, RelationshipPolarity.Negative, 0.72f));
        gambit.Description = "A faction near collapse asks for terms. The war ends; what remains is the long memory of betrayal.";
        return gambit;
    }

    private static GambitDefinition NewGambit(
        GambitId id,
        string name,
        EntityKind actorKind,
        EntityKind targetKind,
        IPrecondition precondition,
        IExpression scoreExpression,
        float success,
        int cooldown,
        int entityCooldown)
    {
        var gambit = new GambitDefinition(G(id), name, actorKind, precondition, scoreExpression)
        {
            SuccessChance = success,
            Cooldown = cooldown,
            EntityCooldown = entityCooldown,
        };
        gambit.ProviderSelectors.Add(AttributeRef.Subtype(targetKind));
        gambit.Reads.Add(AttributeRef.Coordinates(actorKind));
        gambit.Reads.Add(AttributeRef.Coordinates(targetKind));
        return gambit;
    }

    private static void AddRelationshipWrite(
        GambitDefinition gambit,
        Rel relationship,
        EntityKind targetKind)
    {
        var relation = AttributeRef.TargetRelation(L(relationship), targetKind);
        gambit.Writes.Add(relation);
    }

    private static AndPrecondition All(params IPrecondition[] preconditions) =>
        new AndPrecondition(preconditions);

    private static NotPrecondition Not(IPrecondition precondition) =>
        new NotPrecondition(precondition);

    private static Add GambitScore(float baseline, float dynamicRange, IExpression factor) =>
        Expr.Add(Expr.Lit(baseline), Expr.Mul(Expr.Lit(dynamicRange), Expr.Score(factor)));

    private static Clamp Normalized(IExpression expression) =>
        Expr.Score(Expr.Div(expression, Expr.Lit(100f)));

    private static Subtract InverseNormalized(IExpression expression) =>
        Expr.Sub(Expr.Lit(1f), Normalized(expression));

    private static Divide Mean2(IExpression a, IExpression b) =>
        Expr.Div(Expr.Add(a, b), Expr.Lit(2f));

    private static CreateContextRelationshipMutation Relationship(
        TransferEndpoint source,
        TransferEndpoint target,
        Rel relationship,
        RelationshipPolarity polarity,
        float strength) =>
        new(source, target, L(relationship), polarity, strength);
}
