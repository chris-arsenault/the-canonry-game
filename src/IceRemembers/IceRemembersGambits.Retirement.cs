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

// Willful artifacts: detachment and self-retirement. Artifacts are near
// sentient - they express their own will. A held artifact can slip its
// bearer's grasp and wander again; a consigned artifact resting at a
// settlement can install itself as a permanent shrine, leaving the active
// world while its gift endures on the host.
public static partial class IceRemembersDefinition
{
    private static GambitDefinition ArtifactSlipsTheGrasp()
    {
        var gambit = NewGambit(
            GambitId.ArtifactSlipsTheGrasp,
            "Slip the Grasp",
            K.Artifact,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new TargetHasRelationshipToSelf(L(Rel.Holds))),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 4f,
                InverseNormalized(Expr.Self(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 220,
            entityCooldown: 120);
        AddRelationshipWrite(gambit, Rel.Holds, K.Penguin);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.Holds)));
        gambit.Description = "The artifact wills itself free: it slips its bearer's grasp and lies where it falls, waiting for the ice to carry it a new story.";
        return gambit;
    }

    private static GambitDefinition FactionRazesTheHollowHold()
    {
        var gambit = NewGambit(
            GambitId.FactionRazesTheHollowHold,
            "Raze the Hollow Hold",
            K.Faction,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new SelfHasOutgoingRelationship(L(Rel.AtWarWith), minStrength: 0.4f),
                new TargetAttributeThreshold(A(Attr.TempHp), min: null, max: 30f),
                new ContestedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 8f, targetMinimum: 8f)),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Target(A(Attr.TempHp)))),
            success: InitialGambitSuccessChance,
            cooldown: 300,
            entityCooldown: 150);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        // War finishes what solitude started: a hollowed-out settlement on
        // contested ground falls, and the razing scars the ground around it.
        gambit.Mutations.Add(new ErodeTargetCultureMutation(radius: 3, amountPerCell: 20f));
        gambit.Mutations.Add(new SetTargetHistoricalMutation());
        gambit.Description = "A faction at war puts a hollowed hold to the torch. Only the weak fall this way - a peopled hearth is not worth the price.";
        return gambit;
    }

    private static GambitDefinition ArtifactEnshrinesItself()
    {
        var gambit = NewGambit(
            GambitId.ArtifactEnshrinesItself,
            "Enshrine Itself",
            K.Artifact,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(1, PrimaryPlane.Physical),
                new SelfHasRelationshipToTarget(L(Rel.ConsignedTo), minStrength: 0.5f),
                Not(new TargetHasRelationshipToSelf(L(Rel.Holds)))),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 260,
            entityCooldown: 200);
        AddRelationshipWrite(gambit, Rel.ConsignedTo, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        // The durable gift, per the legacy design: the shrine endows the host
        // with a permanent work tier — keepers who tend the hold from now on.
        // The tier is countable and reversible (a future reactivation can
        // take it back), and the consignment pinned to full strength is the
        // archival record of exactly which artifact was installed.
        gambit.Mutations.Add(new IncreaseTargetWorkTierMutation());
        gambit.Mutations.Add(new SetContextRelationshipStrengthMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.ConsignedTo), 1f));
        gambit.Mutations.Add(new SetSelfHistoricalMutation());
        gambit.Description = "A consigned artifact at rest among its keepers installs itself for good - museum, shrine, ward, or foundation stone. The settlement keeps the gift; the artifact keeps only its name in the record.";
        return gambit;
    }
}
