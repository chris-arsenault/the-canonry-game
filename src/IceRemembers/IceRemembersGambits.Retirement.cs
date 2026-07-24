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

    private static GambitDefinition SettlementTheHoldWithers()
    {
        var gambit = NewGambit(
            GambitId.SettlementTheHoldWithers,
            "The Hold Withers",
            K.Settlement,
            K.Settlement,
            All(
                new TargetIsSelf(),
                Not(new SelfWorkTierBelowPrecondition(1)),
                new SelfAttributeThreshold(A(Attr.TempHp), min: null, max: 25f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 4f,
                InverseNormalized(Expr.Self(A(Attr.TempHp)))),
            success: InitialGambitSuccessChance,
            cooldown: 600,
            entityCooldown: 1000);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Writes.Add(AttributeRef.StructuralFact("work_tier", K.Settlement));
        // Hard times step the hold down a stage instead of killing it: the
        // partial recovery is the hysteresis that prevents demotion spirals,
        // and death waits at the base stage.
        gambit.Mutations.Add(new DemoteSelfStageMutation());
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.TempHp), delta: 30f));
        gambit.Description = "A starving city lets its outer rings go dark. Smaller, humbler - but alive. Only towns die outright.";
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
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.Holds), maximumExclusive: 1)),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 260,
            entityCooldown: 200);
        AddRelationshipWrite(gambit, Rel.ConsignedTo, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        gambit.Writes.Add(AttributeRef.StructuralFact("districts", K.Settlement));
        // The artifact becomes a district. Its tile joins the city's
        // footprint with the relic at its core, its subtype-registered bonus
        // becomes durable district state, the pinned consignment archives
        // which artifact was installed, and the install mutation also sets
        // the artifact historical until a reawakening. See
        // docs/design/artifact-districts.md.
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.ConsignedTo, RelationshipPolarity.Positive, 1f));
        gambit.Mutations.Add(new InstallDistrictMutation());
        gambit.Description = "A consigned artifact at rest among its keepers installs itself for good - shrine, library, hall, or chancery. The city grows a district around the sleeping core; the artifact keeps only its name in the record.";
        return gambit;
    }

    private static GambitDefinition SettlementWakesTheSleeper()
    {
        var gambit = NewGambit(
            GambitId.SettlementWakesTheSleeper,
            "Wake the Sleeper",
            K.Settlement,
            K.Settlement,
            All(
                new TargetIsSelf(),
                new SelfAttributeThreshold(A(Attr.TempHp), min: null, max: 35f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 4f,
                InverseNormalized(Expr.Self(A(Attr.TempHp)))),
            success: InitialGambitSuccessChance,
            cooldown: 400,
            entityCooldown: 300);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Writes.Add(AttributeRef.StructuralFact("districts", K.Settlement));
        // The reawakening: a settlement in need takes up its newest sleeping
        // core. The artifact returns to the world on its tile, the district
        // bonus ends, and the tile stays city ground. A settlement with no
        // sleeping core attempts and gains nothing.
        gambit.Mutations.Add(new ReawakenDistrictMutation());
        gambit.Description = "A hold in hard times wakes the relic sleeping under its district. The gift ends; the artifact walks the world again.";
        return gambit;
    }
}
