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
    private static GambitDefinition FactionMarchesTheBanners()
    {
        var gambit = NewGambit(
            GambitId.FactionMarchesTheBanners,
            "March the Banners",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new CultureHoldsTech("pod_coordination"),
                // A campaign is war made territorial: it needs a living war
                // and a faction cohesive enough to keep a column in the field.
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith), minStrength: 0.30f),
                Not(new SelfHasRelationshipToTarget(L(Rel.WagesCampaignAgainst))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 40f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 80);
        AddRelationshipWrite(gambit, Rel.WagesCampaignAgainst, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.WagesCampaignAgainst, RelationshipPolarity.Negative, 0.85f));
        gambit.Description = "A faction at war commits to the field: banners march on the enemy's ground and labor columns follow. The campaign presses until the war ends or the column exhausts itself.";
        return gambit;
    }
}
