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
/// Hunt-chronicle gambits: the world-side actuators the Hunt archetype's
/// beats charge and grant. Forsaking ends a cooled monster war the way peace
/// ends a faction war; cornering is chronicle-granted - only an open Beast
/// at Bay beat puts it in a hunter's hands.
/// </summary>
public static partial class IceRemembersDefinition
{
    private static GambitDefinition PenguinForsakesTheHunt()
    {
        var gambit = NewGambit(
            GambitId.PenguinForsakesTheHunt,
            "Forsake the Hunt",
            K.Penguin,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                // Forsaking is walking away from a cooled war, the penguin
                // counterpart of suing for peace. A hot war cannot simply be
                // abandoned; grievance must burn down first.
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith), minStrength: 0.5f))),
            scoreExpression: GambitScore(
                baseline: 0.4f,
                dynamicRange: 4f,
                InverseNormalized(Expr.Self(A(Attr.Vitality)))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 60);
        AddRelationshipWrite(gambit, Rel.PreysOn, K.Penguin);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.PreysOn, RelationshipPolarity.Negative, 0.5f));
        gambit.Description = "A worn hunter walks away from a cooled war with a beast. The war edge clears - and the beast, unbroken, learns the hunter's scent.";
        return gambit;
    }

    private static GambitDefinition PenguinCornersTheBeast()
    {
        var gambit = NewGambit(
            GambitId.PenguinCornersTheBeast,
            "Corner the Beast",
            K.Penguin,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                new WithinPlaneDistancePrecondition(24, PrimaryPlane.Physical),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 15f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 4f,
                Mean2(
                    DistanceFalloff(10f),
                    Normalized(Expr.Self(A(Attr.Vitality))))),
            success: 0.45f,
            cooldown: 60,
            entityCooldown: 15);
        gambit.ChronicleGranted = true;
        AddRelationshipWrite(gambit, Rel.CleansedBy, K.Monster);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.CleansedBy, RelationshipPolarity.Positive, 0.84f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Self, Rel.CleansedBy, RelationshipPolarity.Positive, 0.70f));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new SetTargetHistoricalMutation());
        gambit.Description = "Chronicle-granted: when a hunt reaches its reckoning, the hunter can corner the beast for the closing blow. Only the story opens this door; the blow can still miss.";
        return gambit;
    }
}
