namespace IceRemembers;

using Engine.Behaviors;
using Engine.Constants;
using Engine.Domain.Config;
using Engine.Gambits;
using Engine.Graph;
using Engine.Wiring;

using Attr = IceRemembers.IceRemembersAttributeId;
using GambitId = IceRemembers.IceRemembersGambitId;
using K = IceRemembers.IceRemembersKinds;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using TravelStructure = IceRemembers.IceRemembersTravelStructure;

public static partial class IceRemembersDefinition
{
    private const float InitialGambitSuccessChance = 0.25f;

    private static IReadOnlyList<GambitDefinition> CreateGambits() =>
    [
        PenguinWieldsArtifact(),
        FactionDeclaresTerritorialWar(),
        SettlementOpensTradeRoute(),
        SettlementRaisesCauseway(),
        ArtifactCleansesScar(),
        FactionSuesForPeace(),
        FactionClaimsPointOfInterest(),
        OrcaClaimsHuntingGround(),
        AbilitySharesPractice(),
        TransportOpensConduit(),
        FactionJoinsOccurrence(),
        ArtifactBindsMonster(),
        FactionChartersSettlement(),
        TransportSurveysRegion(),
        OrcaLaysDeepCurrent(),
        TransportWardsMonster(),
        MonsterHauntsTransport(),
        OrcaRaidsTradeRoute(),
        MonsterStealsArtifact(),
        PenguinSlaysMonster(),
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
                new ContestedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 8f, targetMinimum: 35f),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new TargetHasRelationshipToSelf(L(Rel.AtWarWith))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 34f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 65,
            entityCooldown: 14);
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
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.TradeRoute), maximumExclusive: 2),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.TradeRoute), maximumExclusive: 2),
                Not(new SelfHasRelationshipToTarget(L(Rel.TradeRoute))),
                Not(new TargetHasRelationshipToSelf(L(Rel.TradeRoute))),
                new SelfAttributeThreshold(A(Attr.TempHp), min: 45f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.TempHp), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.5f,
                dynamicRange: 8f,
                Weighted3(
                    (DistanceFalloff(5f), 0.75f),
                    (Expr.SameCulture(), 0.20f),
                    (Mean2(
                        Normalized(Expr.Self(A(Attr.TempHp))),
                        Normalized(Expr.Target(A(Attr.TempHp)))), 0.05f))),
            success: InitialGambitSuccessChance,
            cooldown: 220,
            entityCooldown: 52);
        AddRelationshipWrite(gambit, Rel.TradeRoute, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Mutations.Add(Relationship(
            TransferEndpoint.Self,
            TransferEndpoint.Target,
            Rel.TradeRoute,
            RelationshipPolarity.Positive,
            IceRemembersRelationshipStrengths.InitialTradeRoute));
        gambit.Mutations.Add(Relationship(
            TransferEndpoint.Target,
            TransferEndpoint.Self,
            Rel.TradeRoute,
            RelationshipPolarity.Positive,
            IceRemembersRelationshipStrengths.InitialTradeRoute));
        gambit.Description = "Two viable settlements open a trade route. Nearby settlements, and settlements of shared culture, are more likely to bind first.";
        return gambit;
    }

    private static GambitDefinition SettlementRaisesCauseway()
    {
        var gambit = NewGambit(
            GambitId.SettlementRaisesCauseway,
            "Raise a Causeway",
            K.Settlement,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(24, PrimaryPlane.Physical),
                new SelfHasRelationshipToTarget(L(Rel.TradeRoute)),
                new TargetHasRelationshipToSelf(L(Rel.TradeRoute)),
                new SelfAttributeThreshold(A(Attr.TempHp), min: 60f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.TempHp), min: 45f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.25f,
                dynamicRange: 4f,
                Weighted3(
                    (DistanceFalloff(10f), 0.65f),
                    (Expr.SameCulture(), 0.20f),
                    (Mean2(
                        Normalized(Expr.Self(A(Attr.TempHp))),
                        Normalized(Expr.Target(A(Attr.TempHp)))), 0.15f))),
            success: InitialGambitSuccessChance,
            cooldown: 260,
            entityCooldown: 80);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.TempHp), K.Settlement));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Writes.Add(AttributeRef.StructuralFact("travel_structure", K.Settlement));
        gambit.Mutations.Add(new BuildTravelStructureLineMutation(
            TravelStructure.IceCauseway.ToDomainName(),
            PlaneCodes.Physical,
            maxDistance: 24));
        gambit.Description = "Settlements with an existing trade bond can raise a physical ice causeway between them. The causeway is a travel structure, not terrain.";
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
                new SelfAttributeThreshold(A(Attr.Condition), min: 40f, max: null, behaviorRelevant: false),
                Not(new TargetHasRelationshipToSelf(L(Rel.CleansedBy)))),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Condition)))),
            success: InitialGambitSuccessChance,
            cooldown: 190,
            entityCooldown: 50);
        AddRelationshipWrite(gambit, Rel.CleansedBy, K.Region);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.CleansedBy, RelationshipPolarity.Positive, 0.78f));
        gambit.Mutations.Add(new SetTargetCoverageTerrainMutation(Terrain.HotSprings.ToDomainName()));
        gambit.Description = "An intact artifact brought near a scarred region cleanses it. The ice forgets the cold; hot springs surface.";
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
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 50,
            entityCooldown: 12);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.BetrayedBy, RelationshipPolarity.Negative, 0.72f));
        gambit.Description = "A faction near collapse asks for terms. The war ends; what remains is the long memory of betrayal.";
        return gambit;
    }

    private static GambitDefinition FactionClaimsPointOfInterest()
    {
        var gambit = NewGambit(
            GambitId.FactionClaimsPointOfInterest,
            "Claim a Named Place",
            K.Faction,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(18, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.ClaimedBy), maximumExclusive: 2),
                Not(new TargetHasRelationshipToSelf(L(Rel.ClaimedBy))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 35f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Presence), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(8f), 0.55f),
                    (Normalized(Expr.Self(A(Attr.Cohesion))), 0.25f),
                    (Normalized(Expr.Target(A(Attr.Presence))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 130,
            entityCooldown: 32);
        AddRelationshipWrite(gambit, Rel.ClaimedBy, K.PointOfInterest);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Presence), K.PointOfInterest));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ClaimedBy, RelationshipPolarity.Neutral, 0.74f));
        gambit.Description = "A faction with enough cohesion names a nearby feature as part of its holdings. Place claims stay graph topology; low altitude only maintains their strength.";
        return gambit;
    }

    private static GambitDefinition OrcaClaimsHuntingGround()
    {
        var gambit = NewGambit(
            GambitId.OrcaClaimsHuntingGround,
            "Claim a Hunting Ground",
            K.Orca,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(10, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.ClaimedBy), maximumExclusive: 2),
                Not(new TargetHasRelationshipToSelf(L(Rel.ClaimedBy))),
                new SelfAttributeThreshold(A(Attr.Endurance), min: 35f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Presence), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(6f), 0.50f),
                    (Normalized(Expr.Self(A(Attr.Endurance))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.Presence))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 30);
        AddRelationshipWrite(gambit, Rel.ClaimedBy, K.PointOfInterest);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Endurance), K.Orca));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Presence), K.PointOfInterest));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ClaimedBy, RelationshipPolarity.Neutral, 0.72f));
        gambit.Description = "A strong orca marks a named water-place as hunting ground. The same claim term can later be contested or maintained by ordinary relationship-strength verbs.";
        return gambit;
    }

    private static GambitDefinition AbilitySharesPractice()
    {
        var gambit = NewGambit(
            GambitId.AbilitySharesPractice,
            "Share a Practice",
            K.Ability,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(6, PrimaryPlane.Semantic),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.PracticedBy), maximumExclusive: 4),
                Not(new SelfHasRelationshipToTarget(L(Rel.PracticedBy))),
                new SelfAttributeThreshold(A(Attr.Clarity), min: 35f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Vitality), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.5f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(5f), 0.45f),
                    (Normalized(Expr.Self(A(Attr.Clarity))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.Vitality))), 0.25f))),
            success: InitialGambitSuccessChance,
            cooldown: 115,
            entityCooldown: 28);
        AddRelationshipWrite(gambit, Rel.PracticedBy, K.Penguin);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Clarity), K.Ability));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.PracticedBy, RelationshipPolarity.Positive, 0.70f));
        gambit.Description = "A clear technique, rite, or craft finds another practitioner. The practice edge is the topology later generators and low-altitude maintenance can read.";
        return gambit;
    }

    private static GambitDefinition TransportOpensConduit()
    {
        var gambit = NewGambit(
            GambitId.TransportOpensConduit,
            "Open a Conduit",
            K.Transport,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
                new SelfHasOutgoingRelationship(L(Rel.NativeTo)),
                new WithinPlaneDistancePrecondition(12, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.ConduitOf), maximumExclusive: 3),
                Not(new SelfHasRelationshipToTarget(L(Rel.ConduitOf))),
                new SelfAttributeThreshold(A(Attr.Integrity), min: 45f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Presence), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(8f), 0.50f),
                    (Normalized(Expr.Self(A(Attr.Integrity))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.Presence))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 40);
        AddRelationshipWrite(gambit, Rel.ConduitOf, K.PointOfInterest);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Integrity), K.Transport));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Presence), K.PointOfInterest));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.ConduitOf, RelationshipPolarity.Positive, 0.68f));
        gambit.Description = "A transport with a home port turns a named feature into a route-conduit. This depends on prior transport topology instead of replacing it.";
        return gambit;
    }

    private static GambitDefinition FactionJoinsOccurrence()
    {
        var gambit = NewGambit(
            GambitId.FactionJoinsOccurrence,
            "Join an Occurrence",
            K.Faction,
            K.Occurrence,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Semantic),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.MemberOf), maximumExclusive: 4),
                Not(new SelfHasRelationshipToTarget(L(Rel.MemberOf))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 30f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Momentum), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(6f), 0.35f),
                    (Expr.SameCulture(), 0.25f),
                    (Normalized(Expr.Target(A(Attr.Momentum))), 0.40f))),
            success: InitialGambitSuccessChance,
            cooldown: 140,
            entityCooldown: 34);
        AddRelationshipWrite(gambit, Rel.MemberOf, K.Occurrence);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Momentum), K.Occurrence));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.MemberOf, RelationshipPolarity.Positive, 0.68f));
        gambit.Description = "A faction lends itself to an event that has gathered enough momentum. Occurrences become social topology, not just mobile force.";
        return gambit;
    }

    private static GambitDefinition ArtifactBindsMonster()
    {
        var gambit = NewGambit(
            GambitId.ArtifactBindsMonster,
            "Bind a Monster",
            K.Artifact,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(6, PrimaryPlane.Semantic),
                Not(new SelfHasRelationshipToTarget(L(Rel.WardAgainst))),
                new SelfAttributeThreshold(A(Attr.Condition), min: 45f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Containment), min: null, max: 95f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.25f,
                dynamicRange: 6f,
                Weighted3(
                    (Normalized(Expr.Self(A(Attr.Condition))), 0.45f),
                    (InverseNormalized(Expr.Target(A(Attr.Containment))), 0.35f),
                    (DistanceFalloff(4f), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 170,
            entityCooldown: 45);
        AddRelationshipWrite(gambit, Rel.WardAgainst, K.Monster);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Condition), K.Artifact));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Containment), K.Monster));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.WardAgainst, RelationshipPolarity.Negative, 0.76f));
        gambit.Description = "An intact artifact brought close to an exposed monster lays a ward against it. The ward is graph topology that later low-altitude verbs can tighten or gnaw down.";
        return gambit;
    }

    private static GambitDefinition FactionChartersSettlement()
    {
        var gambit = NewGambit(
            GambitId.FactionChartersSettlement,
            "Charter a Settlement",
            K.Faction,
            K.Settlement,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(12, PrimaryPlane.Semantic),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.ControlledBy), maximumExclusive: 1),
                Not(new TargetHasRelationshipToSelf(L(Rel.ControlledBy))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 38f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.TempHp), min: 38f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(6f), 0.45f),
                    (Normalized(Expr.Self(A(Attr.Cohesion))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.TempHp))), 0.25f))),
            success: InitialGambitSuccessChance,
            cooldown: 130,
            entityCooldown: 34);
        AddRelationshipWrite(gambit, Rel.ControlledBy, K.Settlement);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.TempHp), K.Settlement));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.ControlledBy, RelationshipPolarity.Neutral, 0.76f));
        gambit.Description = "A cohesive faction gives a viable settlement a charter. The controlled-by edge lets later generators read civic authority instead of bare proximity.";
        return gambit;
    }

    private static GambitDefinition TransportSurveysRegion()
    {
        var gambit = NewGambit(
            GambitId.TransportSurveysRegion,
            "Survey a Region",
            K.Transport,
            K.Region,
            All(
                new TargetIsNotSelf(),
                new SelfHasOutgoingRelationship(L(Rel.NativeTo)),
                new WithinPlaneDistancePrecondition(14, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.ConduitOf), maximumExclusive: 4),
                Not(new SelfHasRelationshipToTarget(L(Rel.ConduitOf))),
                new SelfAttributeThreshold(A(Attr.Integrity), min: 42f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Stability), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(9f), 0.50f),
                    (Normalized(Expr.Self(A(Attr.Integrity))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.Stability))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 40);
        AddRelationshipWrite(gambit, Rel.ConduitOf, K.Region);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Integrity), K.Transport));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Stability), K.Region));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.ConduitOf, RelationshipPolarity.Positive, 0.68f));
        gambit.Description = "A ported transport surveys a region into the route graph. This gives generated depths and older regions the same conduit language used by points of interest.";
        return gambit;
    }

    private static GambitDefinition OrcaLaysDeepCurrent()
    {
        var gambit = NewGambit(
            GambitId.OrcaLaysDeepCurrent,
            "Lay a Deep Current",
            K.Orca,
            K.PointOfInterest,
            All(
                new TargetIsNotSelf(),
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

    private static GambitDefinition TransportWardsMonster()
    {
        var gambit = NewGambit(
            GambitId.TransportWardsMonster,
            "Ward a Monster",
            K.Transport,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Physical),
                Not(new SelfHasRelationshipToTarget(L(Rel.WardAgainst))),
                new SelfAttributeThreshold(A(Attr.Integrity), min: 42f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Containment), min: null, max: 92f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(5f), 0.25f),
                    (Normalized(Expr.Self(A(Attr.Integrity))), 0.40f),
                    (InverseNormalized(Expr.Target(A(Attr.Containment))), 0.35f))),
            success: InitialGambitSuccessChance,
            cooldown: 170,
            entityCooldown: 45);
        AddRelationshipWrite(gambit, Rel.WardAgainst, K.Monster);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Integrity), K.Transport));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Containment), K.Monster));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.WardAgainst, RelationshipPolarity.Negative, 0.72f));
        gambit.Description = "A proven transport uses hull, crew, and route discipline to lay a ward against a monster. This gives ward topology a non-artifact path.";
        return gambit;
    }

    private static GambitDefinition MonsterHauntsTransport()
    {
        var gambit = NewGambit(
            GambitId.MonsterHauntsTransport,
            "Haunt a Transport",
            K.Monster,
            K.Transport,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.CorruptedBy), maximumExclusive: 2),
                Not(new TargetHasRelationshipToSelf(L(Rel.CorruptedBy))),
                new SelfAttributeThreshold(A(Attr.Containment), min: null, max: 88f, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Integrity), min: 35f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(5f), 0.25f),
                    (InverseNormalized(Expr.Self(A(Attr.Containment))), 0.45f),
                    (Normalized(Expr.Target(A(Attr.Integrity))), 0.30f))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 42);
        AddRelationshipWrite(gambit, Rel.CorruptedBy, K.Transport);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Containment), K.Monster));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Integrity), K.Transport));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.CorruptedBy, RelationshipPolarity.Negative, 0.74f));
        gambit.Description = "A loosened monster haunts a nearby transport, turning the hull into corrupted topology that later generators can condense into outbreak aftermath.";
        return gambit;
    }

    private static GambitDefinition OrcaRaidsTradeRoute()
    {
        var gambit = NewGambit(
            GambitId.OrcaRaidsTradeRoute,
            "Raid a Trade Route",
            K.Orca,
            K.Penguin,
            All(
                new TargetIsNotSelf(),
                new SelfHasOutgoingRelationship(L(Rel.HomeBase)),
                new WithinPlaneDistancePrecondition(12, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.AtWarWith), maximumExclusive: 3),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.AtWarWith), maximumExclusive: 3),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new TargetHasRelationshipToSelf(L(Rel.AtWarWith))),
                new SelfAttributeThreshold(A(Attr.Endurance), min: 38f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Vitality), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.9f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(7f), 0.50f),
                    (Normalized(Expr.Self(A(Attr.Endurance))), 0.30f),
                    (Normalized(Expr.Target(A(Attr.Vitality))), 0.20f))),
            success: InitialGambitSuccessChance,
            cooldown: 150,
            entityCooldown: 40);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Penguin);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Endurance), K.Orca));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Vitality), K.Penguin));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AtWarWith, RelationshipPolarity.Negative, 0.78f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.AtWarWith, RelationshipPolarity.Negative, 0.78f));
        gambit.Description = "An orca with a settled current raids a nearby penguin route-participant. The raid becomes reciprocal war topology that route-war generators can name and spend.";
        return gambit;
    }

    private static GambitDefinition MonsterStealsArtifact()
    {
        var gambit = NewGambit(
            GambitId.MonsterStealsArtifact,
            "Steal an Artifact",
            K.Monster,
            K.Artifact,
            All(
                new TargetIsNotSelf(),
                new WithinPlaneDistancePrecondition(8, PrimaryPlane.Physical),
                new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.Holds), maximumExclusive: 1),
                new SelfAttributeThreshold(A(Attr.Containment), min: null, max: 95f, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Condition), min: 20f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 1.0f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(6f), 0.30f),
                    (InverseNormalized(Expr.Self(A(Attr.Containment))), 0.45f),
                    (Normalized(Expr.Target(A(Attr.Condition))), 0.25f))),
            success: InitialGambitSuccessChance,
            cooldown: 110,
            entityCooldown: 28);
        AddRelationshipWrite(gambit, Rel.Holds, K.Artifact);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Containment), K.Monster));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Condition), K.Artifact));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.Holds, RelationshipPolarity.Negative, 1f));
        gambit.Mutations.Add(new MoveTargetToSelfPhysicalMutation());
        gambit.Description = "A loosened monster takes an unheld artifact and physically carries it away. The holds edge is the theft signal that can draw hunters.";
        return gambit;
    }

    private static GambitDefinition PenguinSlaysMonster()
    {
        var gambit = NewGambit(
            GambitId.PenguinSlaysMonster,
            "Slay a Monster",
            K.Penguin,
            K.Monster,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                new WithinPlaneDistancePrecondition(24, PrimaryPlane.Physical),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 26f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Containment), min: null, max: 92f, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.8f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(10f), 0.35f),
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.35f),
                    (InverseNormalized(Expr.Target(A(Attr.Containment))), 0.30f))),
            success: InitialGambitSuccessChance,
            cooldown: 70,
            entityCooldown: 10);
        AddRelationshipWrite(gambit, Rel.CleansedBy, K.Monster);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Containment), K.Monster));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.CleansedBy, RelationshipPolarity.Positive, 0.84f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Self, Rel.CleansedBy, RelationshipPolarity.Positive, 0.70f));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new SetTargetHistoricalMutation());
        gambit.Description = "A penguin already bound into a monster hunt closes the hunt. The monster becomes historical, the war edge is cleared, and the penguin carries an active cleansing mark that can seed celebration.";
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

    private static Divide DistanceFalloff(float halfScoreDistance)
    {
        var normalizedDistance = Expr.Div(Expr.Distance(), Expr.Lit(halfScoreDistance));
        return Expr.Div(Expr.Lit(1f), Expr.Add(Expr.Lit(1f), Expr.Mul(normalizedDistance, normalizedDistance)));
    }

    private static Add Weighted3(
        (IExpression Factor, float Weight) a,
        (IExpression Factor, float Weight) b,
        (IExpression Factor, float Weight) c) =>
        Expr.Add(Expr.Add(WeightedTerm(a), WeightedTerm(b)), WeightedTerm(c));

    private static Multiply WeightedTerm((IExpression Factor, float Weight) term) =>
        Expr.Mul(Expr.Lit(term.Weight), term.Factor);

    private static CreateContextRelationshipMutation Relationship(
        TransferEndpoint source,
        TransferEndpoint target,
        Rel relationship,
        RelationshipPolarity polarity,
        float strength) =>
        new(source, target, L(relationship), polarity, strength);
}
