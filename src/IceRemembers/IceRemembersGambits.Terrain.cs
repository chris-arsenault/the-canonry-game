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

// The ground has opinions. These gambits read the latent geology under an
// entity's feet - the geothermal veins the Nightshelf crack for fire-cores,
// the warmth that keeps a worn walker alive, the broken country a pass has
// to be found through - and act only where the land actually allows it.
public static partial class IceRemembersDefinition
{
    private static GambitDefinition PenguinSheltersAtTheVent()
    {
        var gambit = NewGambit(
            GambitId.PenguinSheltersAtTheVent,
            "Shelter at the Vent",
            K.Penguin,
            K.Penguin,
            All(
                new TargetIsSelf(),
                new SelfHexFactThreshold(PhysicalHexFact.Geothermal, min: 350),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 10f, max: 60f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                InverseNormalized(Expr.Self(A(Attr.Vitality)))),
            success: InitialGambitSuccessChance,
            cooldown: 60,
            entityCooldown: 120);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Vitality), 12f, variance: 0.2f));
        gambit.Description = "The cold does not hunt; it waits. A worn walker who reaches ground where the heat rises banks the vent-warmth into flesh and buys the walk more distance. Works only where the veins actually run - the ground decides.";
        return gambit;
    }

    private static GambitDefinition PenguinFindsThePass()
    {
        var gambit = NewGambit(
            GambitId.PenguinFindsThePass,
            "Find the Pass",
            K.Penguin,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical),
                Not(new WithinTravelReachPrecondition(45f, PrimaryPlane.Physical)),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 40f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.2f,
                dynamicRange: 5f,
                Weighted3(
                    (DistanceFalloff(6f), 0.5f),
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.3f),
                    (Normalized(Expr.Target(A(Attr.TempHp))), 0.2f))),
            success: InitialGambitSuccessChance,
            cooldown: 180,
            entityCooldown: 60);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Writes.Add(AttributeRef.StructuralFact("travel_structure", K.Penguin));
        gambit.Mutations.Add(new BuildTravelStructureLineMutation(
            TravelStructure.IceCauseway.ToDomainName(),
            PlaneCodes.Physical,
            maxDistance: 12));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Vitality), -4f));
        gambit.Description = "A settlement sits near as the skua flies and far as the land walks - ridge and broken water between. Mountains are crossed at passes, and passes are found by someone: a walker packs the causeway line through, and the near place becomes a reachable one. The finding costs flesh.";
        return gambit;
    }

    private static GambitDefinition RegionBreathesTheThaw()
    {
        var gambit = NewGambit(
            GambitId.RegionBreathesTheThaw,
            "Breathe the Thaw",
            K.Region,
            K.Region,
            All(
                new TargetIsSelf(),
                new EndpointSubtypeIs(TransferEndpoint.Self, "scar"),
                // Calibrated to the worldgen fact distribution: ~p90 of land
                // hexes, so warm-vein scars are uncommon but not mythical.
                new SelfHexFactThreshold(PhysicalHexFact.Geothermal, min: 250),
                new SelfAttributeThreshold(A(Attr.Stability), min: 5f, max: 65f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.2f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Stability)))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 200);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Stability), K.Region));
        gambit.Mutations.Add(new ModifySelfAttributeMutation(A(Attr.Stability), 6f, variance: 0.15f));
        gambit.Description = "Some scars sit over ground that was always warm. Where a geothermal vein runs beneath the wound, the land mends itself from below - slowly, without ceremony, the way the Berg has always healed. Scars on cold rock stay scars.";
        return gambit;
    }

    private static GambitDefinition FactionClaimsTheWarmGround()
    {
        var gambit = NewGambit(
            GambitId.FactionClaimsTheWarmGround,
            "Claim the Warm Ground",
            K.Faction,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
                new EndpointCultureIs(TransferEndpoint.Self, C(Culture.Nightshelf)),
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.TheIce)),
                new TargetHexFactThreshold(PhysicalHexFact.Geothermal, min: 400),
                new WithinTravelReachPrecondition(75f, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.ClaimedBy), maximumExclusive: 2),
                Not(new TargetHasRelationshipToSelf(L(Rel.ClaimedBy))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.8f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(8f), 0.5f),
                    (Normalized(Expr.Self(A(Attr.Cohesion))), 0.3f),
                    (Normalized(Expr.Target(A(Attr.Presence))), 0.2f))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 40);
        AddRelationshipWrite(gambit, Rel.ClaimedBy, K.PointOfInterest);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Presence), K.PointOfInterest));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ClaimedBy, RelationshipPolarity.Neutral, 0.74f));
        gambit.Mutations.Add(new SetTargetCultureFromSelfMutation());
        gambit.Description = "The Shelf reads the ground the way the Stack reads the sky. Where fire-core veins run shallow, a Nightshelf faction moves first and claims the place before anyone else knows what it sits on. What matters is what you can reach and what you're willing to pay.";
        return gambit;
    }
}
