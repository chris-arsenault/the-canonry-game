namespace IceRemembers;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Graph;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using Culture = IceRemembers.IceRemembersCulture;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using TravelStructure = IceRemembers.IceRemembersTravelStructure;

// The deliberate journeys. On the Berg a deed only exists if the ice saw
// it: a walker far from the hearth-light raises a stone so the walk is
// recorded, and an orca sounds an unclaimed passage so the deep carries
// the route in its pressure-song. Both acts cost what the lore says such
// acts cost - flesh for the walk, interior distance for the dive.
public static partial class IceRemembersDefinition
{
    private static GambitDefinition OrcaLaysDeepCurrent()
    {
        var gambit = NewGambit(
            GambitId.OrcaLaysDeepCurrent,
            "Lay a Deep Current",
            K.Orca,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
                new CultureHoldsTech("pressure_diving"),
                new TargetHasRelationshipToSelf(L(Rel.ClaimedBy)),
                new WithinPlaneDistancePrecondition(16, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.HomeBase), maximumExclusive: 1),
                new SelfAttributeThreshold(A(Attr.Endurance), min: 42f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Presence), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.8f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(8f), 0.45f),
                    (Normalized(Expr.Self(A(Attr.Endurance))), 0.35f),
                    (Normalized(Expr.Target(A(Attr.Presence))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 170,
            entityCooldown: 50);
        AddRelationshipWrite(gambit, Rel.HomeBase, K.PointOfInterest);
        AddRelationshipWrite(gambit, Rel.ControlledBy, K.PointOfInterest);
        gambit.Writes.Add(AttributeRef.StructuralFact("travel_structure", K.Orca));
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Endurance), K.Orca));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Presence), K.PointOfInterest));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.HomeBase, RelationshipPolarity.Neutral, 1f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ControlledBy, RelationshipPolarity.Neutral, 0.74f));
        gambit.Mutations.Add(new BuildTravelStructureLineMutation(
            TravelStructure.DeepCurrent.ToDomainName(),
            PlaneCodes.Physical,
            maxDistance: 16));
        gambit.Description = "An orca that already holds a hunting-ground claim settles a current into it. The current is a travel structure; the home-base edge makes the claim usable by later content.";
        return gambit;
    }

    private static GambitDefinition PenguinRaisesTheWaymark()
    {
        var gambit = NewGambit(
            GambitId.PenguinRaisesTheWaymark,
            "Raise the Waymark",
            K.Penguin,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.HomeBase)),
                Not(new WithinPlaneDistancePrecondition(8, PrimaryPlane.Physical)),
                Not(new SelfHasOutgoingRelationship(L(Rel.Memorializes))),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 45f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.9f,
                dynamicRange: 5f,
                Weighted3(
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.6f),
                    (Normalized(Expr.Target(A(Attr.TempHp))), 0.2f),
                    (Normalized(Expr.Target(A(Attr.TempHp))), 0.2f))),
            success: InitialGambitSuccessChance,
            cooldown: 200,
            entityCooldown: 60);
        AddRelationshipWrite(gambit, Rel.Memorializes, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Writes.Add(AttributeRef.StructuralFact("travel_structure", K.Penguin));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.Memorializes, RelationshipPolarity.Positive, 0.6f));
        gambit.Mutations.Add(new BuildTravelStructureLineMutation(
            TravelStructure.IceCauseway.ToDomainName(),
            PlaneCodes.Physical,
            maxDistance: 20));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Vitality), -6f));
        gambit.Description = "A walker far past the hearth-light commits the walk to memory: a causeway line is packed toward home so others can follow, and the deed is offered to the ice. The walk costs flesh - it always does. The stone comes later, when the ice answers.";
        return gambit;
    }

    private static GambitDefinition OrcaSoundsTheFarPassage()
    {
        var gambit = NewGambit(
            GambitId.OrcaSoundsTheFarPassage,
            "Sound the Far Passage",
            K.Orca,
            K.Region,
            All(
                new TargetIsNotSelf(),
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.TheIce)),
                Not(new EndpointCultureIs(TransferEndpoint.Self, C(Culture.TheIce))),
                Not(new SelfHasRelationshipToTarget(L(Rel.ConduitOf))),
                new WithinPlaneDistancePrecondition(24, PrimaryPlane.Physical),
                Not(new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical)),
                new SelfAttributeThreshold(A(Attr.Endurance), min: 50f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.8f,
                dynamicRange: 5f,
                Weighted3(
                    (Normalized(Expr.Self(A(Attr.Endurance))), 0.6f),
                    (Normalized(Expr.Target(A(Attr.Stability))), 0.2f),
                    (Normalized(Expr.Target(A(Attr.Stability))), 0.2f))),
            success: InitialGambitSuccessChance,
            cooldown: 220,
            entityCooldown: 70);
        AddRelationshipWrite(gambit, Rel.ConduitOf, K.Region);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Endurance), K.Orca));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Stability), K.Region));
        gambit.Writes.Add(AttributeRef.StructuralFact("travel_structure", K.Orca));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.ConduitOf, RelationshipPolarity.Positive, 0.6f));
        gambit.Mutations.Add(new BuildTravelStructureLineMutation(
            TravelStructure.DeepCurrent.ToDomainName(),
            PlaneCodes.Physical,
            maxDistance: 24));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Endurance), -5f));
        gambit.Description = "A bull dives past the last claimed water and lets the pressure carry the shape of an unheld reach back through the deep. The passage joins the pod's song-map as a current others can ride. Sustained sounding costs interior distance - the diver comes back a little farther away each time.";
        return gambit;
    }
}
