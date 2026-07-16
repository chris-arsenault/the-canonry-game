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

/// <summary>
/// Carriers: transports pick up a passenger, haul it across the ice - faster
/// on good roads (the movement road-speed bonus reads the carries edge) - and
/// set it down where there is life. The transport's ordinary journey verbs do
/// the hauling; these gambits only load and deposit. A carried entity follows
/// the transport bodily through the carries relationship.
/// </summary>
public static partial class IceRemembersDefinition
{
    private static GambitDefinition TransportTakesUpThePassenger()
    {
        var gambit = NewGambit(
            GambitId.TransportTakesUpThePassenger,
            "Take Up the Passenger",
            K.Transport,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(2, PrimaryPlane.Physical),
                // One load at a time, and never a passenger already aboard
                // some other transport.
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.Carries), maximumExclusive: 1),
                Not(new TargetHasRelationshipToSelf(L(Rel.Carries))),
                new SelfAttributeThreshold(A(Attr.Integrity), min: 40f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 3f,
                Mean2(DistanceFalloff(3f), Normalized(Expr.Self(A(Attr.Integrity))))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 80);
        AddRelationshipWrite(gambit, Rel.Carries, K.Penguin);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Integrity), K.Transport));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.Carries, RelationshipPolarity.Neutral, 1f));
        gambit.Mutations.Add(new MoveTargetToSelfPhysicalMutation());
        gambit.Description = "A transport takes on a traveller for the crossing. The passenger rides with the hull from here on, and the ice remembers where the caravan is bound.";
        return gambit;
    }

    private static GambitDefinition TransportSetsDownThePassenger()
    {
        var gambit = NewGambit(
            GambitId.TransportSetsDownThePassenger,
            "Set Down the Passenger",
            K.Transport,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.Carries)),
                // Set down where there is life - a port, a holding, other
                // named company - never in the empty white.
                new HasColocatedNamedEntityPrecondition()),
            scoreExpression: GambitScore(
                baseline: 2.5f,
                dynamicRange: 1f,
                Normalized(Expr.Self(A(Attr.Integrity)))),
            success: InitialGambitSuccessChance,
            cooldown: 60,
            entityCooldown: 40);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Integrity), K.Transport));
        gambit.Mutations.Add(new CullEndpointIncomingRelationshipsMutation(TransferEndpoint.Target, L(Rel.Carries)));
        gambit.Description = "The crossing ends: the transport sets its passenger down where the living gather. A traveller has been carried to new ground, and the caravan is free for the next load.";
        return gambit;
    }
}
