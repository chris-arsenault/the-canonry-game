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
/// Gambit grants: gambits that only exist while the actor holds an artifact
/// or practices an ability of a specific subtype. Losing the granting
/// relationship revokes the gambit. Longevity grants are the in-universe
/// legendary mechanism — renewal is an active, visible biography beat, and
/// its cadence (entity cooldown vs aging drain) bounds how far past the
/// lifecycle band a bearer can live.
/// </summary>
public static partial class IceRemembersDefinition
{
    private const float GrantArtifactMinHold = 0.5f;
    private const float GrantPracticeMinStrength = 0.3f;

    public static IReadOnlyList<GambitId> GrantGambitIds { get; } =
    [
        GambitId.PenguinDrawsOnTheVessel,
        GambitId.OrcaDrawsOnTheVessel,
        GambitId.MonsterDrawsOnTheVessel,
        GambitId.PenguinUnmakesTheBinding,
        GambitId.PenguinCallsTheDeepCurrent,
        GambitId.PenguinTurnsTheBattle,
        GambitId.PenguinBindsWhatWasLoosed,
        GambitId.PenguinKeepsWintersPatience,
        GambitId.PenguinMendsAnother,
        GambitId.PenguinPassesTheTeaching,
        GambitId.PenguinReadsTheIce,
        GambitId.PenguinStillsTheWaters,
    ];

    private static IEnumerable<GambitDefinition> CreateGrantGambits()
    {
        yield return DrawOnTheVessel(GambitId.PenguinDrawsOnTheVessel, K.Penguin, Attr.Vitality);
        yield return DrawOnTheVessel(GambitId.OrcaDrawsOnTheVessel, K.Orca, Attr.Endurance);
        yield return MonsterDrawsOnTheVessel();
        yield return PenguinUnmakesTheBinding();
        yield return PenguinCallsTheDeepCurrent();
        yield return PenguinTurnsTheBattle();
        yield return PenguinBindsWhatWasLoosed();
        yield return PenguinKeepsWintersPatience();
        yield return PenguinMendsAnother();
        yield return PenguinPassesTheTeaching();
        yield return PenguinReadsTheIce();
        yield return PenguinStillsTheWaters();
    }

    private static SelfHasRelationshipToEntityOfSubtype HeldArtifactGrant(string subtype) =>
        new(L(Rel.Holds), K.Artifact, subtype, minStrength: GrantArtifactMinHold);

    private static SelfHasRelationshipToEntityOfSubtype PracticedAbilityGrant(string subtype) =>
        new(L(Rel.PracticedBy), K.Ability, subtype, minStrength: GrantPracticeMinStrength, incoming: true);

    private static GambitDefinition DrawOnTheVessel(GambitId id, EntityKind bearerKind, Attr health)
    {
        var gambit = NewGambit(
            id,
            "Draw on the Vessel",
            bearerKind,
            K.Artifact,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.Holds)),
                new EndpointSubtypeIs(TransferEndpoint.Target, "relic"),
                new SelfAttributeThreshold(A(health), min: null, max: 65f, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Condition), min: 25f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 2f,
                dynamicRange: 6f,
                InverseNormalized(Expr.Self(A(health)))),
            success: 0.85f,
            cooldown: 20,
            entityCooldown: 140);
        gambit.Reads.Add(AttributeRef.Self(A(health), bearerKind));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(health), 25f, variance: 0.15f));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Condition), -4f, variance: 0.2f));
        gambit.Description = "A bearer draws remembered life from a held relic. The relic wears a little with every drawing; legends slowly consume the vessels that keep them.";
        return gambit;
    }

    private static GambitDefinition MonsterDrawsOnTheVessel()
    {
        var gambit = NewGambit(
            GambitId.MonsterDrawsOnTheVessel,
            "Feed on the Hoard",
            K.Monster,
            K.Artifact,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.Holds)),
                new SelfAttributeThreshold(A(Attr.Containment), min: null, max: 65f, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Condition), min: 25f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 2f,
                dynamicRange: 6f,
                InverseNormalized(Expr.Self(A(Attr.Containment)))),
            success: 0.85f,
            cooldown: 20,
            entityCooldown: 160);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Containment), K.Monster));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Containment), 22f, variance: 0.15f));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Condition), -5f, variance: 0.2f));
        gambit.Description = "A monster gnaws remembered life from any artifact in its hoard. Hoarded monsters endure; this is why the old ones sleep on treasure, and why slaying one frees the vessels it fed on.";
        return gambit;
    }

    private static GambitDefinition PenguinUnmakesTheBinding()
    {
        var gambit = NewGambit(
            GambitId.PenguinUnmakesTheBinding,
            "Unmake the Binding",
            K.Penguin,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                HeldArtifactGrant("seal"),
                new OrPrecondition([
                    new SelfHasRelationshipToTarget(L(Rel.CorruptedBy)),
                    new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                ]),
                new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                Mean2(DistanceFalloff(6f), Normalized(Expr.Self(A(Attr.Vitality))))),
            success: 0.5f,
            cooldown: 60,
            entityCooldown: 40);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        AddRelationshipWrite(gambit, Rel.WardAgainst, K.Monster);
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.CorruptedBy)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.WardAgainst, RelationshipPolarity.Negative, 0.6f));
        gambit.Description = "A seal-bearer severs the binding between themselves and a monster — corruption lifted, war unmade — and lays a ward in its place.";
        return gambit;
    }

    private static GambitDefinition PenguinCallsTheDeepCurrent()
    {
        var gambit = NewGambit(
            GambitId.PenguinCallsTheDeepCurrent,
            "Call the Deep Current",
            K.Penguin,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                HeldArtifactGrant("instrument"),
                new WithinPlaneDistancePrecondition(40, PrimaryPlane.Physical),
                Not(new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical))),
            scoreExpression: GambitScore(
                baseline: 1f,
                dynamicRange: 5f,
                Mean2(
                    Expr.Score(Expr.Div(Expr.Distance(), Expr.Lit(40f))),
                    Normalized(Expr.Self(A(Attr.Vitality))))),
            success: 0.6f,
            cooldown: 45,
            entityCooldown: 90);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new MoveTowardTargetMutation());
        gambit.Description = "An instrument-bearer sounds the deep current and is carried far across the ice toward a distant settlement. Legends travel; their stories cross the map.";
        return gambit;
    }

    private static GambitDefinition PenguinTurnsTheBattle()
    {
        var gambit = NewGambit(
            GambitId.PenguinTurnsTheBattle,
            "Turn the Battle",
            K.Penguin,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                HeldArtifactGrant("weapon"),
                new SelfHasRelationshipToTarget(L(Rel.MemberOf)),
                Not(new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.AtWarWith), maximumExclusive: 1))),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                InverseNormalized(Expr.Target(A(Attr.Cohesion)))),
            success: 0.5f,
            cooldown: 30,
            entityCooldown: 60);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Cohesion), 15f, variance: 0.2f));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Vitality), -5f, variance: 0.2f));
        gambit.Description = "A weapon-bearer stands where their faction's war is breaking and turns it. The faction's cohesion rallies; the bearer pays in blood. Named heroes now appear inside wars.";
        return gambit;
    }

    private static GambitDefinition PenguinBindsWhatWasLoosed()
    {
        var gambit = NewGambit(
            GambitId.PenguinBindsWhatWasLoosed,
            "Bind What Was Loosed",
            K.Penguin,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                HeldArtifactGrant("tome"),
                Not(new SelfHasRelationshipToTarget(L(Rel.WardAgainst))),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Physical),
                new TargetAttributeThreshold(A(Attr.Containment), min: null, max: 80f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.25f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(5f), 0.35f),
                    (InverseNormalized(Expr.Target(A(Attr.Containment))), 0.45f),
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.20f))),
            success: 0.55f,
            cooldown: 60,
            entityCooldown: 35);
        AddRelationshipWrite(gambit, Rel.WardAgainst, K.Monster);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Containment), K.Monster));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Containment), 18f, variance: 0.15f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.WardAgainst, RelationshipPolarity.Negative, 0.72f));
        gambit.Description = "A tome-bearer reads a loosed monster back toward its binding. Stronger than an artifact working alone: the tome remembers how the old wards were written.";
        return gambit;
    }

    private static GambitDefinition PenguinKeepsWintersPatience()
    {
        var gambit = NewGambit(
            GambitId.PenguinKeepsWintersPatience,
            "Keep Winter's Patience",
            K.Penguin,
            K.Penguin,
            All(
                new TargetIsSelf(),
                PracticedAbilityGrant("rite"),
                new SelfAttributeThreshold(A(Attr.Vitality), min: null, max: 65f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 2f,
                dynamicRange: 6f,
                InverseNormalized(Expr.Self(A(Attr.Vitality)))),
            success: 0.85f,
            cooldown: 20,
            entityCooldown: 150);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Vitality), 20f, variance: 0.15f));
        gambit.Description = "A practitioner of the rites sits out a turning of the dark and lets the ice hold them still. Practice sustains; the discipline lapses if the practice edge decays.";
        return gambit;
    }

    private static GambitDefinition PenguinMendsAnother()
    {
        var gambit = NewGambit(
            GambitId.PenguinMendsAnother,
            "Mend Another",
            K.Penguin,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                PracticedAbilityGrant("craft"),
                new WithinPlaneDistancePrecondition(4, PrimaryPlane.Physical),
                new TargetAttributeThreshold(A(Attr.Vitality), min: null, max: 50f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                Weighted3(
                    (InverseNormalized(Expr.Target(A(Attr.Vitality))), 0.55f),
                    (DistanceFalloff(3f), 0.25f),
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.20f))),
            success: 0.7f,
            cooldown: 25,
            entityCooldown: 45);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Vitality), 22f, variance: 0.15f));
        gambit.Description = "A practitioner of the crafts mends a failing penguin. Healers extend other lives, seek out the failing, and become recurring cast in other characters' stories.";
        return gambit;
    }

    private static GambitDefinition PenguinPassesTheTeaching()
    {
        var gambit = NewGambit(
            GambitId.PenguinPassesTheTeaching,
            "Pass the Teaching",
            K.Penguin,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                PracticedAbilityGrant("technique"),
                Not(new TargetHasRelationshipToSelf(L(Rel.TaughtBy))),
                new WithinPlaneDistancePrecondition(5, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.TaughtBy), maximumExclusive: 4)),
            scoreExpression: GambitScore(
                baseline: 1f,
                dynamicRange: 5f,
                Mean2(DistanceFalloff(4f), Normalized(Expr.Self(A(Attr.Vitality))))),
            success: 0.5f,
            cooldown: 70,
            entityCooldown: 90);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        AddRelationshipWrite(gambit, Rel.TaughtBy, K.Penguin);
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.TaughtBy, RelationshipPolarity.Positive, 0.7f));
        gambit.Description = "A practitioner takes a student. The teaching edge is lineage topology: masters accumulate students, and generators can read schools grown from a single long life.";
        return gambit;
    }

    private static GambitDefinition PenguinReadsTheIce()
    {
        var gambit = NewGambit(
            GambitId.PenguinReadsTheIce,
            "Read the Ice",
            K.Penguin,
            K.Canon,
            All(
                new TargetIsNotSelf(),
                PracticedAbilityGrant("rite"),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Semantic),
                new TargetAttributeThreshold(A(Attr.Observance), min: null, max: 80f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.25f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Target(A(Attr.Observance)))),
            success: 0.6f,
            cooldown: 50,
            entityCooldown: 70);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Observance), K.Canon));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Observance), 12f, variance: 0.15f));
        gambit.Description = "A practitioner of the rites reads a fading canon back into observance. Scholars keep laws alive; laws remember the scholars who kept them.";
        return gambit;
    }

    private static GambitDefinition PenguinStillsTheWaters()
    {
        var gambit = NewGambit(
            GambitId.PenguinStillsTheWaters,
            "Still the Waters",
            K.Penguin,
            K.Occurrence,
            All(
                new TargetIsNotSelf(),
                PracticedAbilityGrant("technique"),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Semantic)),
            scoreExpression: GambitScore(
                baseline: 1.25f,
                dynamicRange: 5f,
                Normalized(Expr.Target(A(Attr.Momentum)))),
            success: 0.55f,
            cooldown: 55,
            entityCooldown: 70);
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Momentum), K.Occurrence));
        gambit.Mutations.Add(new ModifyTargetAttributeMutation(A(Attr.Momentum), -15f, variance: 0.2f));
        gambit.Description = "A practitioner of the techniques stills a raging occurrence, draining its momentum toward an early end. Some heroes are remembered for the disasters that did not happen.";
        return gambit;
    }
}
