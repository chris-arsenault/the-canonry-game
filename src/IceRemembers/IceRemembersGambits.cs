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
using Terrain = IceRemembers.IceRemembersTerrain;
using TravelStructure = IceRemembers.IceRemembersTravelStructure;

public static partial class IceRemembersDefinition
{
    private const float InitialGambitSuccessChance = 0.25f;
    private const float WorkTierGambitSuccessChance = 0.75f;
    private const float CulturalWorkTierMinimumRawMass = 12f;
    private const float CulturalWorkTierMinimumConnectedRawMass = 90f;
    private const int SettlementTradeRouteMaxSemanticDistance = 12;
    private static readonly Dictionary<EntityKind, GambitId> WorkTierGambitIds = new()
    {
        [K.Region] = GambitId.RegionDeepensTheHolding,
        [K.Settlement] = GambitId.SettlementDeepensTheHolding,
        [K.Faction] = GambitId.FactionDeepensTheHolding,
        [K.Penguin] = GambitId.PenguinDeepensTheHolding,
        [K.Artifact] = GambitId.ArtifactDeepensTheHolding,
        [K.Orca] = GambitId.OrcaDeepensTheHolding,
        [K.PointOfInterest] = GambitId.PointOfInterestDeepensTheHolding,
        [K.Ability] = GambitId.AbilityDeepensTheHolding,
        [K.Monster] = GambitId.MonsterDeepensTheHolding,
        [K.Transport] = GambitId.TransportDeepensTheHolding,
        [K.Occurrence] = GambitId.OccurrenceDeepensTheHolding,
        [K.Canon] = GambitId.CanonDeepensTheHolding,
    };

    private static List<GambitDefinition> CreateGambits()
    {
        var gambits = new List<GambitDefinition>
        {
            PenguinWieldsArtifact(),
            FactionDeclaresRivalry(),
            FactionDeclaresTerritorialWar(),
            FactionSwearsColdAccord(),
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
            PenguinSlaysOrca(),
        };
        gambits.AddRange(CreateWorkTierGambits());
        gambits.AddRange(CreateForgingGambits());
        gambits.AddRange(CreateGrantGambits());
        return gambits;
    }

    private static IEnumerable<GambitDefinition> CreateForgingGambits()
    {
        yield return ForgingGambit(
            GambitId.SettlementRaisesTheSpire,
            "Raise the Spire",
            K.Settlement,
            Culture.AuroraStack,
            direction: 1,
            Terrain.SpireTier,
            "An entrenched Aurora settlement raises a new crystalline tier above its column. Height is witnessed permanence: each tier adds ground the ice can see.");
        yield return ForgingGambit(
            GambitId.SettlementDigsTheGallery,
            "Dig the Gallery",
            K.Settlement,
            Culture.Nightshelf,
            direction: -1,
            Terrain.TunnelGallery,
            "A Nightshelf settlement cuts a new gallery beneath its column. What is carved in darkness belongs to the Shelf alone.");
        yield return ForgingGambit(
            GambitId.FactionDigsTheGallery,
            "Dig the Gallery",
            K.Faction,
            Culture.Nightshelf,
            direction: -1,
            Terrain.TunnelGallery,
            "A Nightshelf faction extends the hidden labyrinth. Tunnels are territory no surface overlay will ever show.");
    }

    private static GambitDefinition ForgingGambit(
        GambitId gambitId,
        string name,
        EntityKind actorKind,
        Culture culture,
        int direction,
        Terrain terrain,
        string description)
    {
        var entity = IceRemembersEntities.All.First(definition => definition.Kind == actorKind);
        var health = A(entity.HealthAttribute);
        var gambit = new GambitDefinition(
            G(gambitId),
            name,
            actorKind,
            All(
                new TargetIsSelf(),
                new EndpointCultureIs(TransferEndpoint.Self, C(culture)),
                new SelfCultureMassPrecondition(
                    PrimaryPlane.Physical,
                    minimumRawMass: 100f,
                    minimumConnectedRawMass: 300f),
                new SelfAttributeThreshold(health, min: 40f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.6f,
                dynamicRange: 2.5f,
                Normalized(Expr.Self(health))))
        {
            SuccessChance = 0.25f,
            Cooldown = 200,
            EntityCooldown = 320,
            Description = description,
        };
        gambit.Reads.Add(AttributeRef.Self(health, actorKind));
        gambit.Reads.Add(AttributeRef.Coordinates(actorKind));
        gambit.Writes.Add(AttributeRef.StructuralFact("forged_cells", actorKind));
        gambit.Mutations.Add(new ForgeCellVerticalMutation(direction, TerrainName(terrain)));
        return gambit;
    }

    private static IEnumerable<GambitDefinition> CreateWorkTierGambits()
    {
        foreach (var entity in IceRemembersEntities.All)
        {
            yield return EntityDeepensTheHolding(entity);
        }
    }

    private static GambitDefinition EntityDeepensTheHolding(IceRemembersEntityDefinition entity)
    {
        var health = A(entity.HealthAttribute);
        var gambit = new GambitDefinition(
            G(WorkTierGambitId(entity.Kind)),
            "Deepen the Holding",
            entity.Kind,
            All(
                new TargetIsSelf(),
                new SelfCultureMassPrecondition(
                    NonMobilePrimaryPlane(entity.PrimaryPlane),
                    CulturalWorkTierMinimumRawMass,
                    CulturalWorkTierMinimumConnectedRawMass),
                new SelfAttributeThreshold(health, min: 45f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.35f,
                dynamicRange: 4f,
                Normalized(Expr.Self(health))))
        {
            SuccessChance = WorkTierGambitSuccessChance,
            Cooldown = 260,
            EntityCooldown = 80,
            Description = "A durable entity inside a strong cultural mass deepens its holding, increasing the workers it can support and how far those workers can operate.",
        };
        gambit.Reads.Add(AttributeRef.Self(health, entity.Kind));
        gambit.Reads.Add(AttributeRef.Coordinates(entity.Kind));
        gambit.Reads.Add(AttributeRef.StructuralFact("work_tier", entity.Kind));
        gambit.Writes.Add(AttributeRef.StructuralFact("work_tier", entity.Kind));
        gambit.Mutations.Add(new IncreaseWorkTierMutation());
        return gambit;
    }

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

    private static GambitDefinition FactionDeclaresRivalry()
    {
        var gambit = NewGambit(
            GambitId.FactionDeclaresRivalry,
            "Declare Rivalry",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new ContestedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 8f, targetMinimum: 30f),
                Not(new SelfHasRelationshipToTarget(L(Rel.RivalOf))),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new SelfHasRelationshipToTarget(L(Rel.AlliedWith))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 30f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 2.5f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 80,
            entityCooldown: 30);
        AddRelationshipWrite(gambit, Rel.RivalOf, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.RivalOf, RelationshipPolarity.Negative, 0.6f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.RivalOf, RelationshipPolarity.Negative, 0.6f));
        gambit.Description = "Two factions contesting the same ground name each other rival. The first beat of the escalation chain: rivalry must be pressed before war can be declared, and peace buries it.";
        return gambit;
    }

    private static GambitDefinition FactionSwearsColdAccord()
    {
        var gambit = NewGambit(
            GambitId.FactionSwearsColdAccord,
            "Swear the Cold Accord",
            K.Faction,
            K.Faction,
            All(
                new TargetIsNotSelf(),
                new SharedInfluencePrecondition(PrimaryPlane.Physical, selfMinimum: 8f, targetMinimum: 8f),
                Not(new SelfHasRelationshipToTarget(L(Rel.AlliedWith))),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new SelfHasRelationshipToTarget(L(Rel.RivalOf))),
                // Alliances are sworn under duress: both factions must be
                // carrying at least one war.
                Not(new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.AtWarWith), maximumExclusive: 1)),
                Not(new EndpointRelationshipCountBelow(TransferEndpoint.Target, L(Rel.AtWarWith), maximumExclusive: 1))),
            scoreExpression: GambitScore(
                baseline: 3f,
                dynamicRange: 4f,
                InverseNormalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 120,
            entityCooldown: 60);
        AddRelationshipWrite(gambit, Rel.AlliedWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AlliedWith, RelationshipPolarity.Positive, 0.72f));
        gambit.Description = "Two embattled factions sharing ground swear the Cold Accord. Alliances are finally entity-authored: born from shared wartime duress, sustained only if renewed.";
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
                // War is the third beat of an escalation chain, never the
                // first: it requires a pressed rivalry, not just contested
                // ground. Peace clears the rivalry, so re-declaration needs
                // full re-escalation.
                new SelfHasRelationshipToTarget(L(Rel.RivalOf), minStrength: 0.35f),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith))),
                Not(new TargetHasRelationshipToSelf(L(Rel.AtWarWith))),
                new SelfAttributeThreshold(A(Attr.Cohesion), min: 34f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 4f,
                Normalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 200,
            entityCooldown: 100);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Target, TransferEndpoint.Self, Rel.AtWarWith, RelationshipPolarity.Negative, 0.88f));
        gambit.Description = "A pressed rivalry over contested ground formalises into war. War is declared in both directions; cohesion sours, the ice begins to record.";
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
                new WithinPlaneDistancePrecondition(SettlementTradeRouteMaxSemanticDistance, PrimaryPlane.Semantic),
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
                // Peace requires a cooled war: the drift verbs (Press the
                // War / Let the War Cool / Exhaust the War) are the campaign
                // plot, and terms only come when the war strength has burned
                // down. A fresh declaration cannot be peaced away.
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                Not(new SelfHasRelationshipToTarget(L(Rel.AtWarWith), minStrength: 0.35f))),
            scoreExpression: GambitScore(
                baseline: 4f,
                dynamicRange: 5f,
                InverseNormalized(Expr.Self(A(Attr.Cohesion)))),
            success: InitialGambitSuccessChance,
            cooldown: 100,
            entityCooldown: 50);
        AddRelationshipWrite(gambit, Rel.AtWarWith, K.Faction);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Cohesion), K.Faction));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.RivalOf)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.RivalOf)));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.BetrayedBy, RelationshipPolarity.Negative, 0.5f));
        gambit.Description = "An exhausted war ends in terms. The rivalry is buried with it — renewed war needs renewed grievance — but the capitulating side keeps a quieter memory of betrayal.";
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
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.TheIce)),
                Not(new EndpointCultureIs(TransferEndpoint.Self, C(Culture.TheIce))),
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
        gambit.Mutations.Add(new SetTargetCultureFromSelfMutation());
        gambit.Description = "A faction with enough cohesion claims an Ice-held named feature. The place enters the faction's culture and cannot be claimed again by another culture through this gambit.";
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
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.TheIce)),
                Not(new EndpointCultureIs(TransferEndpoint.Self, C(Culture.TheIce))),
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
        gambit.Mutations.Add(new SetTargetCultureFromSelfMutation());
        gambit.Description = "A strong orca marks an Ice-held named water-place as hunting ground. The place enters the orca's culture and is no longer open wilderness.";
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
                new EndpointCultureIs(TransferEndpoint.Target, C(Culture.TheIce)),
                Not(new EndpointCultureIs(TransferEndpoint.Self, C(Culture.TheIce))),
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
        gambit.Mutations.Add(new SetTargetCultureFromSelfMutation());
        gambit.Description = "A ported transport surveys an Ice-held region into the route graph. The region enters the transport's culture and stops being claimable wilderness.";
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

    private static GambitDefinition PenguinSlaysOrca()
    {
        var gambit = NewGambit(
            GambitId.PenguinSlaysOrca,
            "Drive Off an Orca",
            K.Penguin,
            K.Orca,
            All(
                new TargetIsNotSelf(),
                new SelfHasRelationshipToTarget(L(Rel.AtWarWith)),
                new TargetHasRelationshipToSelf(L(Rel.AtWarWith)),
                Not(new EndpointRelationshipCountBelow(TransferEndpoint.Self, L(Rel.Holds), maximumExclusive: 1, minStrength: 0.7f)),
                new WithinPlaneDistancePrecondition(18, PrimaryPlane.Physical),
                new SelfAttributeThreshold(A(Attr.Vitality), min: 28f, max: null, behaviorRelevant: false),
                new TargetAttributeThreshold(A(Attr.Endurance), min: 20f, max: null, behaviorRelevant: false)),
            scoreExpression: GambitScore(
                baseline: 0.7f,
                dynamicRange: 6f,
                Weighted3(
                    (DistanceFalloff(8f), 0.35f),
                    (Normalized(Expr.Self(A(Attr.Vitality))), 0.35f),
                    (InverseNormalized(Expr.Target(A(Attr.Endurance))), 0.30f))),
            success: InitialGambitSuccessChance,
            cooldown: 85,
            entityCooldown: 12);
        AddRelationshipWrite(gambit, Rel.CleansedBy, K.Orca);
        gambit.Reads.Add(AttributeRef.Self(A(Attr.Vitality), K.Penguin));
        gambit.Reads.Add(AttributeRef.Target(A(Attr.Endurance), K.Orca));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Target, Rel.CleansedBy, RelationshipPolarity.Positive, 0.78f));
        gambit.Mutations.Add(Relationship(TransferEndpoint.Self, TransferEndpoint.Self, Rel.CleansedBy, RelationshipPolarity.Positive, 0.62f));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Self, TransferEndpoint.Target, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new RemoveContextRelationshipMutation(TransferEndpoint.Target, TransferEndpoint.Self, L(Rel.AtWarWith)));
        gambit.Mutations.Add(new SetTargetHistoricalMutation());
        gambit.Description = "An artifact-bearing penguin already in a reciprocal orca war closes the raid. The orca becomes historical, the war edge is cleared, and the penguin carries a cleansing mark.";
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

    private static PrimaryPlane NonMobilePrimaryPlane(PrimaryPlane primaryPlane) =>
        primaryPlane == PrimaryPlane.Physical
            ? PrimaryPlane.Semantic
            : PrimaryPlane.Physical;

    private static GambitId WorkTierGambitId(EntityKind kind)
    {
        return WorkTierGambitIds.TryGetValue(kind, out var gambitId)
            ? gambitId
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Ice Remembers entity kind.");
    }
}
