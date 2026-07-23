namespace IceRemembers;

using Engine.Behaviors;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Graph;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;

// The thin target columns get their verbs: transports can be taken into a
// port's service, and a pod can be bargained with. Both are the Accord's
// arithmetic applied wider - survival can be negotiated, for a price, for
// a while.
public static partial class IceRemembersDefinition
{
    private static GambitDefinition SettlementHiresTheHull()
    {
        var gambit = NewGambit(
            GambitId.SettlementHiresTheHull,
            "Hire the Hull",
            K.Settlement,
            K.Transport,
            All(
                new TargetIsNotSelf(),
                new WithinTravelReachPrecondition(75f, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.ControlledBy), maximumExclusive: 1),
                Not(new TargetHasRelationshipToSelf(L(Rel.ControlledBy))),
                new SelfAttributeThreshold(A(Attr.TempHp), min: 40f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Integrity), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.2f,
                dynamicRange: 5f,
                Weighted3(
                    (DistanceFalloff(8f), 0.5f),
                    (Normalized(Expr.Self(A(Attr.TempHp))), 0.25f),
                    (Normalized(Expr.Target(A(Attr.Integrity))), 0.25f))),
            success: InitialGambitSuccessChance,
            cooldown: 160,
            entityCooldown: 50);
        AddRelationshipWrite(gambit, Rel.ControlledBy, K.Transport);
        AddRelationshipWrite(gambit, Rel.HomeBase, K.Transport);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Integrity), K.Transport));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ControlledBy, RelationshipPolarity.Neutral, 0.6f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.HomeBase, RelationshipPolarity.Positive, 0.7f));
        gambit.Description = "A sound port takes an unclaimed hull into its service: the route is posted, the schedule kept, and the vessel has a home water to answer to. Charters and cargoes find hulls that belong somewhere.";
        return gambit;
    }

    private static GambitDefinition FactionParleysWithThePod()
    {
        var gambit = NewGambit(
            GambitId.FactionParleysWithThePod,
            "Parley with the Pod",
            K.Faction,
            K.Orca,
            All(
                new TargetIsNotSelf(),
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.WakeSingers)),
                Not(new EndpointCultureIs(TransferEndpoint.Self, C(Culture.WakeSingers))),
                Not(new SelfHasRelationshipToTarget(L(Rel.AlliedWith))),
                new WithinPlaneDistancePrecondition(14, PrimaryPlane.Physical),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 40f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Endurance), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.7f,
                dynamicRange: 5f,
                Weighted3(
                    (DistanceFalloff(10f), 0.4f),
                    (Normalized(Expr.Self(A(Attr.Cohesion))), 0.4f),
                    (Normalized(Expr.Target(A(Attr.Endurance))), 0.2f))),
            success: InitialGambitSuccessChance,
            cooldown: 260,
            entityCooldown: 90);
        AddRelationshipWrite(gambit, Rel.AlliedWith, K.Orca);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Endurance), K.Orca));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AlliedWith, RelationshipPolarity.Neutral, 0.3f));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Cohesion), -3f));
        gambit.Description = "The Accord taught the peoples that survival can be negotiated. A faction tries the same arithmetic on the deep: easier hunting elsewhere, a season of quiet bought at the ice-edge. The pod keeps such bargains exactly as long as they profit, and the buying costs standing at home.";
        return gambit;
    }
}
