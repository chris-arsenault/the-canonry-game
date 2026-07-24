namespace IceRemembers;

using Engine.Domain.Config;
using Engine.Gameplay;
using Engine.Kinds;

using static Engine.Gameplay.LowAltitudeVerbOperation;

using K = IceRemembers.IceRemembersKinds;
using Mode = Engine.Gameplay.LowAltitudeTargets;
using Op = Engine.Gameplay.LowAltitudeOperations;
using Rel = IceRemembers.IceRemembersRelationship;
using Terrain = IceRemembers.IceRemembersTerrain;
using Verb = IceRemembers.IceRemembersVerbId;

public static partial class IceRemembersDefinition
{
    private const int MovementCommitmentScale = 3;

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateLowAltitudeVerbs() =>
    [
        ..CreatePursuitVerbs(),
        ..CreateJourneyVerbs(),
        ..CreatePhysicalMovementVerbs(),
        ..CreateSemanticMovementVerbs(),
        ..CreateRelationshipStrengthMutationVerbs(),
        ..CreateSemanticInfluenceVerbs(),
        ..CreatePhysicalInfluenceVerbs(),
    ];

    // The tale draws its cast together: long-committed journeys toward a
    // chronicle costar, only offered while the story burns and the costar
    // is far. High weight - when the ice tells your tale, you answer.
    // Destination journeys: long-committed travel to structurally far
    // targets - the wandering season roams the deep white, pilgrimage seeks
    // the canons, far currents carry orcas across the map. These move the
    // median life, not just the storied one.
    private static IReadOnlyList<LowAltitudeVerbConfig> CreateJourneyVerbs() =>
    [
        MovementVerb(Verb.PenguinWanderingSeason, "The Wandering Season", K.Penguin, Mode.Explore,
            ticks: (300, 700), dwell: 12, radius: 22, samples: 30, weight: 0.5f, opportunityBonusOverride: 0.2f,
            distanceScale: 30f),
        MovementVerb(Verb.PenguinPilgrimage, "Pilgrimage", K.Penguin, Mode.EntityKind,
            targetKind: K.Canon, ticks: (300, 700), dwell: 40, radius: 1, samples: 10, weight: 0.5f,
            opportunityBonusOverride: 0.4f, distanceScale: 45f),
        MovementVerb(Verb.OrcaFarCurrents, "Ride the Far Currents", K.Orca, Mode.Explore,
            ticks: (300, 700), dwell: 12, radius: 26, samples: 30, weight: 0.6f, opportunityBonusOverride: 0.2f,
            distanceScale: 40f),
        MovementVerb(Verb.PenguinFollowTheGathering, "Follow the Gathering", K.Penguin, Mode.AttractorTrek,
            ticks: (300, 700), dwell: 25, radius: 30, samples: 24, weight: 0.5f, opportunityBonusOverride: 0.3f,
            distanceScale: 40f),
        MovementVerb(Verb.OrcaFollowTheGathering, "Follow the Gathering", K.Orca, Mode.AttractorTrek,
            ticks: (300, 700), dwell: 25, radius: 30, samples: 24, weight: 0.5f, opportunityBonusOverride: 0.3f,
            distanceScale: 40f),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreatePursuitVerbs() =>
    [
        MovementVerb(Verb.PenguinAnswerTheTale, "Answer the Tale", K.Penguin, Mode.ChroniclePursuit,
            ticks: (250, 600), dwell: 20, samples: 1, weight: 2.5f, opportunityBonusOverride: 1.5f),
        MovementVerb(Verb.OrcaAnswerTheTale, "Answer the Tale", K.Orca, Mode.ChroniclePursuit,
            ticks: (250, 600), dwell: 20, samples: 1, weight: 2.5f, opportunityBonusOverride: 1.5f),
        MovementVerb(Verb.MonsterAnswerTheTale, "Answer the Tale", K.Monster, Mode.ChroniclePursuit,
            ticks: (250, 600), dwell: 20, samples: 1, weight: 2.2f, opportunityBonusOverride: 1.5f),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreatePhysicalMovementVerbs() =>
    [
        MovementVerb(Verb.PenguinExplore, "Explore the White", K.Penguin, Mode.Explore,
            ticks: (45, 120), dwell: 2, samples: 24, weight: 1.4f, opportunityBonusOverride: 0.2f),
        MovementVerb(Verb.PenguinVisitRookery, "Visit the Rookery", K.Penguin, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (30, 90), dwell: 18, radius: 1, samples: 12, weight: 1.0f, opportunityBonusOverride: 0.4f,
            fitnessRelationshipKind: L(Rel.HomeBase), fitnessWeight: 1.4f),
        MovementVerb(Verb.PenguinVisitRival, "Trail a Rival", K.Penguin, Mode.RelationshipTarget,
            relationshipKind: L(Rel.RivalOf), ticks: (25, 70), dwell: 8, radius: 1, weight: 0.45f, opportunityBonusOverride: 0.6f,
            fitnessRelationshipKind: L(Rel.RivalOf), fitnessWeight: 1.6f),
        MovementVerb(Verb.PenguinGoFishing, "Go Fishing", K.Penguin, Mode.Terrain,
            terrains: MaritimeTerrains(), ticks: (35, 100), dwell: 16, radius: 1, samples: 28, weight: 1.1f, opportunityBonusOverride: 0.3f),
        .. CreateExplorationMovementVerbs(),
        MovementVerb(Verb.PenguinFollowArtifact, "Follow a Relic", K.Penguin, Mode.EntityKind,
            targetKind: K.Artifact, ticks: (25, 80), dwell: 10, radius: 1, samples: 10, weight: 0.65f, opportunityBonusOverride: 1.6f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessWeight: 1.8f),
        // Artifacts travel by their own will and then lie where they arrive:
        // long committed journeys, rest at the destination, no locality
        // pacing. The catalog stays — the jitter goes.
        MovementVerb(Verb.ArtifactCirculate, "Circulate", K.Artifact, Mode.Explore,
            ticks: (140, 380), dwell: 40, samples: 24, weight: 1.2f, opportunityBonusOverride: 0.4f,
            restAtDestination: true),
        MovementVerb(Verb.ArtifactSeekBearer, "Seek a Bearer", K.Artifact, Mode.EntityKind,
            targetKind: K.Penguin, ticks: (90, 240), dwell: 30, radius: 1, samples: 12, weight: 0.75f, opportunityBonusOverride: 1.6f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.6f,
            restAtDestination: true),
        MovementVerb(Verb.ArtifactReturnToShrine, "Return to Shrine", K.Artifact, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (110, 300), dwell: 50, radius: 1, samples: 12, weight: 0.75f, opportunityBonusOverride: 0.7f,
            fitnessRelationshipKind: L(Rel.Anchored), fitnessWeight: 1.4f,
            restAtDestination: true),
        MovementVerb(Verb.ArtifactDrawToScar, "Draw Toward a Scar", K.Artifact, Mode.EntityKind,
            targetKind: K.Region, ticks: (110, 300), dwell: 30, radius: 1, samples: 14, weight: 0.65f, opportunityBonusOverride: 1.1f,
            restAtDestination: true),
        MovementVerb(Verb.ArtifactSinkSeaward, "Sink Seaward", K.Artifact, Mode.Terrain,
            terrains: MaritimeTerrains(), ticks: (110, 300), dwell: 40, radius: 1, samples: 28, weight: 0.7f, opportunityBonusOverride: 0.5f,
            restAtDestination: true),
        MovementVerb(Verb.OrcaHuntTheMark, "Hunt the Mark", K.Orca, Mode.EntityKind,
            targetKind: K.Penguin, ticks: (30, 90), dwell: 10, radius: 1, samples: 14, weight: 0.7f, opportunityBonusOverride: 1.0f,
            fitnessRelationshipKind: L(Rel.RivalOf), fitnessWeight: 1.6f),
        MovementVerb(Verb.OrcaPatrolTheCurrents, "Patrol the Currents", K.Orca, Mode.Terrain,
            terrains: MaritimeTerrains(), ticks: (40, 120), dwell: 18, radius: 1, samples: 28, weight: 1.0f, opportunityBonusOverride: 0.3f),
        MovementVerb(Verb.OrcaSoundTheFarWater, "Sound the Far Water", K.Orca, Mode.Terrain,
            terrains: MaritimeTerrains(), fogAdjacent: true, ticks: (45, 130), dwell: 6, radius: 1, samples: 28, weight: 0.75f, opportunityBonusOverride: 0.4f,
            distanceScale: 14f, travelInfluence: (0.4f, 0)),
        MovementVerb(Verb.OrcaSoundThePod, "Sound the Pod", K.Orca, Mode.RelationshipTarget,
            relationshipKind: L(Rel.AlliedWith), ticks: (25, 75), dwell: 12, radius: 1, weight: 0.55f, opportunityBonusOverride: 0.7f,
            fitnessRelationshipKind: L(Rel.AlliedWith), fitnessWeight: 1.5f),
        MovementVerb(Verb.MonsterStirWithinBonds, "Stir Within Bonds", K.Monster, Mode.Explore,
            ticks: (50, 140), dwell: 6, samples: 18, weight: 0.6f, opportunityBonusOverride: 0.3f),
        // Heat is the real temperature field: any genuinely warm ground
        // draws the monster, not just the one Hot Springs terrain.
        MovementVerb(Verb.MonsterPressTowardHeat, "Press toward Heat", K.Monster, Mode.Terrain,
            minTemperature: 340, ticks: (40, 120), dwell: 14, radius: 1, samples: 24, weight: 0.7f, opportunityBonusOverride: 0.5f),
        MovementVerb(Verb.MonsterSurfaceAtTheFissure, "Surface at the Fissure", K.Monster, Mode.EntityKind,
            targetKind: K.PointOfInterest, ticks: (45, 130), dwell: 14, radius: 1, samples: 12, weight: 0.55f, opportunityBonusOverride: 1.1f),
        MovementVerb(Verb.AbilitySurfaceInTheReach, "Surface in the Reach", K.Ability, Mode.Explore,
            ticks: (60, 160), dwell: 6, samples: 22, weight: 0.5f, opportunityBonusOverride: 0.3f),
        .. CreateTransportMovementVerbs(),
    ];

    // The deliberate pushes into fog and the survival answers to the cold:
    // scouts hunt the edge of the known world (leash-exempt) and pack the
    // trail as they walk it - a faint influence deposit along the route is
    // the waymark the leash and the ice both read.
    private static IReadOnlyList<LowAltitudeVerbConfig> CreateExplorationMovementVerbs() =>
    [
        MovementVerb(Verb.PenguinScoutTheWhite, "Scout the White", K.Penguin, Mode.Terrain,
            fogAdjacent: true, ticks: (40, 110), dwell: 4, radius: 1, samples: 24, weight: 0.8f, opportunityBonusOverride: 0.4f,
            distanceScale: 14f, travelInfluence: (0.4f, 0)),
        // The frontier's answer to the cold: when the white bites, walkers
        // hole up where the ground itself is warm and wait out the worst.
        // Thermal sites pull the wintering toward the true springs.
        MovementVerb(Verb.PenguinWinterAtTheSprings, "Winter at the Springs", K.Penguin, Mode.Terrain,
            minTemperature: 400, sites: IceRemembersEntityDsl.Sites(IceRemembersPlacementSites.Thermal),
            ticks: (45, 130), dwell: 30, radius: 1, samples: 20, weight: 0.55f, opportunityBonusOverride: 0.3f),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateTransportMovementVerbs() =>
    [
        MovementVerb(Verb.TransportSailTheShelf, "Sail the Shelf", K.Transport, Mode.Terrain,
            terrains: MaritimeTerrains(), ticks: (35, 110), dwell: 12, radius: 1, samples: 28, weight: 1.0f, opportunityBonusOverride: 0.4f,
            travelInfluence: (0.6f, 0)),
        MovementVerb(Verb.TransportMakePort, "Make Port", K.Transport, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 110), dwell: 18, radius: 1, samples: 12, weight: 0.85f, opportunityBonusOverride: 0.6f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.5f,
            travelInfluence: (0.6f, 0)),
        MovementVerb(Verb.TransportPatrolTheReach, "Patrol the Reach", K.Transport, Mode.Explore,
            ticks: (45, 130), dwell: 8, samples: 24, weight: 0.9f, opportunityBonusOverride: 0.3f,
            travelInfluence: (0.6f, 0)),
        MovementVerb(Verb.TransportVoyageBeyondTheCharts, "Voyage Beyond the Charts", K.Transport, Mode.Terrain,
            terrains: MaritimeTerrains(), fogAdjacent: true, ticks: (50, 150), dwell: 6, radius: 1, samples: 28, weight: 0.8f, opportunityBonusOverride: 0.4f,
            travelInfluence: (0.6f, 0), distanceScale: 14f),
        MovementVerb(Verb.TransportAnswerTheCaptain, "Answer the Captain", K.Transport, Mode.RelationshipTarget,
            relationshipKind: L(Rel.Holds), ticks: (25, 75), dwell: 14, radius: 1, weight: 0.55f, opportunityBonusOverride: 0.5f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.7f,
            travelInfluence: (0.6f, 0)),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateSemanticMovementVerbs() =>
    [
        MovementVerb(Verb.SettlementDeliberate, "Deliberate", K.Settlement, Mode.EntityKind,
            targetKind: K.Faction, ticks: (50, 140), dwell: 14, radius: 1, samples: 14, weight: 0.7f, opportunityBonusOverride: 0.5f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.SettlementGossip, "Gossip", K.Settlement, Mode.Explore,
            ticks: (80, 220), dwell: 10, samples: 24, weight: 0.9f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic,
            travelInfluence: (0.3f, 0)),
        MovementVerb(Verb.FactionPreach, "Preach", K.Faction, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (45, 130), dwell: 12, radius: 1, samples: 16, weight: 0.85f, opportunityBonusOverride: 0.7f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.FactionQuarrel, "Quarrel", K.Faction, Mode.Explore,
            ticks: (70, 190), dwell: 8, samples: 24, weight: 0.75f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic,
            travelInfluence: (0.3f, 0)),
        MovementVerb(Verb.RegionCommune, "Commune", K.Region, Mode.EntityKind,
            targetKind: K.Artifact, ticks: (60, 160), dwell: 16, radius: 1, samples: 12, weight: 0.6f, opportunityBonusOverride: 0.5f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.RegionDream, "Dream", K.Region, Mode.Explore,
            ticks: (110, 300), dwell: 12, samples: 24, weight: 0.8f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic,
            travelInfluence: (0.3f, 0)),
        MovementVerb(Verb.OrcaCarryTheDeedName, "Carry the Deed-Name", K.Orca, Mode.Explore,
            ticks: (50, 140), dwell: 8, samples: 22, weight: 0.6f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.PointOfInterestResonateThroughTheStratum, "Resonate Through the Stratum", K.PointOfInterest, Mode.Explore,
            ticks: (60, 160), dwell: 14, samples: 22, weight: 0.55f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.PointOfInterestCallToAWanderer, "Call to a Wanderer", K.PointOfInterest, Mode.EntityKind,
            targetKind: K.Penguin, ticks: (50, 140), dwell: 12, radius: 1, samples: 14, weight: 0.55f, opportunityBonusOverride: 0.6f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.PointOfInterestEchoToAReader, "Echo to a Reader", K.PointOfInterest, Mode.EntityKind,
            targetKind: K.Ability, ticks: (55, 150), dwell: 12, radius: 1, samples: 12, weight: 0.5f, opportunityBonusOverride: 0.7f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.AbilityDriftToABearer, "Drift to a Bearer", K.Ability, Mode.EntityKind,
            targetKind: K.Penguin, ticks: (40, 120), dwell: 14, radius: 1, samples: 14, weight: 0.65f, opportunityBonusOverride: 1.0f,
            fitnessRelationshipKind: L(Rel.PracticedBy), fitnessDirection: LowAltitudeVerbRelationshipDirection.Outgoing, fitnessWeight: 1.6f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.AbilityMigrateTheForm, "Migrate the Form", K.Ability, Mode.EntityKind,
            targetKind: K.Region, ticks: (55, 150), dwell: 16, radius: 1, samples: 12, weight: 0.55f, opportunityBonusOverride: 0.7f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.AbilityPassThroughTheLineage, "Pass through the Lineage", K.Ability, Mode.RelationshipTarget,
            relationshipKind: L(Rel.PracticedBy), ticks: (35, 100), dwell: 12, radius: 1, weight: 0.55f, opportunityBonusOverride: 0.9f,
            fitnessRelationshipKind: L(Rel.PracticedBy), fitnessWeight: 1.5f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.AbilityRetreatToSanctuary, "Retreat to Sanctuary", K.Ability, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (45, 130), dwell: 18, radius: 1, samples: 12, weight: 0.5f, opportunityBonusOverride: 0.6f,
            fitnessRelationshipKind: L(Rel.Anchored), fitnessWeight: 1.4f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.MonsterReachThroughTheLayer, "Reach Through the Layer", K.Monster, Mode.Explore,
            ticks: (60, 170), dwell: 8, samples: 20, weight: 0.5f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.TransportCarryTheCharter, "Carry the Charter", K.Transport, Mode.EntityKind,
            targetKind: K.Faction, ticks: (45, 130), dwell: 12, radius: 1, samples: 12, weight: 0.55f, opportunityBonusOverride: 0.7f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.4f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.OccurrenceSpreadTheWord, "Spread the Word", K.Occurrence, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (40, 120), dwell: 14, radius: 1, samples: 14, weight: 0.7f, opportunityBonusOverride: 0.6f, plane: PrimaryPlane.Semantic),
        MovementVerb(Verb.OccurrenceDrawTheFaction, "Draw the Faction", K.Occurrence, Mode.RelationshipTarget,
            relationshipKind: L(Rel.AlliedWith), ticks: (35, 100), dwell: 14, radius: 1, weight: 0.55f, opportunityBonusOverride: 0.8f,
            fitnessRelationshipKind: L(Rel.AlliedWith), fitnessWeight: 1.5f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateRelationshipStrengthMutationVerbs() =>
    [
        ..CreateAgencyAndPracticeStrengthVerbs(),
        ..CreateCulturalContinuityStrengthVerbs(),
        ..CreatePointOfInterestClaimStrengthVerbs(),
        ..CreateRecoveryAndConduitStrengthVerbs(),
        ..CreateMonsterAndOccurrenceStrengthVerbs(),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateAgencyAndPracticeStrengthVerbs() =>
    [
        RelationshipStrengthVerb(Verb.FactionTendAlliance, "Tend Alliance", K.Faction, L(Rel.AlliedWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 12, delta: 0.0037f, radius: 1, samples: 12,
            weight: 0.55f, opportunityBonusOverride: 0.5f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionFrayAlliance, "Fray Alliance", K.Faction, L(Rel.AlliedWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 10, delta: -0.0037f, radius: 1, samples: 12,
            weight: 0.35f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionInflameRivalry, "Inflame Rivalry", K.Faction, L(Rel.RivalOf),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (30, 90), dwell: 8, delta: 0.0042f, radius: 1, samples: 12,
            weight: 0.45f, opportunityBonusOverride: 0.4f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionCoolRivalry, "Cool Rivalry", K.Faction, L(Rel.RivalOf),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (30, 90), dwell: 10, delta: -0.0042f, radius: 1, samples: 12,
            weight: 0.35f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionPressTheWar, "Press the War", K.Faction, L(Rel.AtWarWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 10, delta: 0.0039f, radius: 1, samples: 12,
            weight: 0.42f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionLetWarCool, "Let the War Cool", K.Faction, L(Rel.AtWarWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (40, 115), dwell: 12, delta: -0.0033f, radius: 1, samples: 12,
            weight: 0.34f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.70f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionFeedTheColumn, "Feed the Column", K.Faction, L(Rel.WagesCampaignAgainst),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 10, delta: 0.0035f, radius: 1, samples: 12,
            weight: 0.40f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionRenewTheAccord, "Renew the Accord", K.Faction, L(Rel.AlliedWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 10, delta: 0.0040f, radius: 1, samples: 12,
            weight: 0.42f, opportunityBonusOverride: 0.40f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.SettlementTrade, "Tend Trade Route", K.Settlement, L(Rel.TradeRoute),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 12, delta: 0.0044f, radius: 1, samples: 12,
            weight: 0.5f, opportunityBonusOverride: 0.55f, fitnessWeight: 0.8f),
        RelationshipStrengthVerb(Verb.AbilityReinforcePractice, "Reinforce Practice", K.Ability, L(Rel.PracticedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 12, delta: 0.0037f, radius: 1, samples: 10,
            weight: 0.45f, opportunityBonusOverride: 0.5f, fitnessWeight: 0.9f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.AbilityLetPracticeFade, "Let Practice Fade", K.Ability, L(Rel.PracticedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 10, delta: -0.0037f, radius: 1, samples: 10,
            weight: 0.35f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.OrcaReinforcePodTie, "Reinforce Pod Tie", K.Orca, L(Rel.MemberOf),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (30, 90), dwell: 10, delta: 0.0042f, radius: 1, samples: 10,
            weight: 0.45f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionPressTheHunt, "Press the Hunt", K.Faction, L(Rel.PreysOn),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 10, delta: 0.0038f, radius: 1, samples: 12,
            weight: 0.4f, opportunityBonusOverride: 0.4f, fitnessWeight: 0.75f),
        RelationshipStrengthVerb(Verb.SettlementBreakTheHunt, "Break the Hunt", K.Settlement, L(Rel.PreysOn),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 100), dwell: 12, delta: -0.0034f, radius: 1, samples: 12,
            weight: 0.4f, opportunityBonusOverride: 0.4f, fitnessWeight: 0.75f),
        RelationshipStrengthVerb(Verb.SettlementPanicDrawsThePods, "Panic Draws the Pods", K.Settlement, L(Rel.PreysOn),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 8, delta: 0.0018f, radius: 1, samples: 12,
            weight: 0.2f, opportunityBonusOverride: 0.25f, fitnessWeight: 0.6f),
        RelationshipStrengthVerb(Verb.FactionLeadTheRite, "Lead the Rite", K.Faction, L(Rel.OfferedTo),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 100), dwell: 12, delta: 0.0036f, radius: 1, samples: 12,
            weight: 0.4f, opportunityBonusOverride: 0.4f, fitnessWeight: 0.75f),
        RelationshipStrengthVerb(Verb.OrcaRenewTheOffering, "Renew the Offering", K.Orca, L(Rel.OfferedTo),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 12, delta: 0.0040f, radius: 1, samples: 12,
            weight: 0.5f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.8f),
        RelationshipStrengthVerb(Verb.OrcaPressRouteWar, "Press Route War", K.Orca, L(Rel.AtWarWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (32, 95), dwell: 10, delta: 0.0040f, radius: 1, samples: 12,
            weight: 0.46f, opportunityBonusOverride: 0.48f, fitnessWeight: 0.78f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.PenguinPressMonsterHunt, "Press Monster Hunt", K.Penguin, L(Rel.AtWarWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (32, 95), dwell: 10, delta: 0.0038f, radius: 1, samples: 12,
            weight: 0.42f, opportunityBonusOverride: 0.46f, fitnessWeight: 0.76f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateCulturalContinuityStrengthVerbs() =>
    [
        RelationshipStrengthVerb(Verb.SettlementRenewCharter, "Renew Charter", K.Settlement, L(Rel.ControlledBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0040f, radius: 1, samples: 12,
            weight: 0.44f, opportunityBonusOverride: 0.50f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.PointOfInterestHoldControlMark, "Hold the Control Mark", K.PointOfInterest, L(Rel.ControlledBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0038f, radius: 1, samples: 10,
            weight: 0.40f, opportunityBonusOverride: 0.46f, fitnessWeight: 0.78f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.FactionTendSeatControl, "Tend Seat Control", K.Faction, L(Rel.ControlledBy),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 105), dwell: 12, delta: 0.0038f, radius: 1, samples: 10,
            weight: 0.40f, opportunityBonusOverride: 0.46f, fitnessWeight: 0.78f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.ArtifactHoldItsAnchor, "Hold Its Anchor", K.Artifact, L(Rel.Anchored),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0036f, radius: 1, samples: 10,
            weight: 0.40f, opportunityBonusOverride: 0.44f, fitnessWeight: 0.76f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.RegionKeepMemorial, "Keep Memorial", K.Region, L(Rel.Memorializes),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (40, 120), dwell: 14, delta: 0.0034f, radius: 1, samples: 10,
            weight: 0.36f, opportunityBonusOverride: 0.40f, fitnessWeight: 0.72f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.OccurrenceHoldAssembly, "Hold Assembly", K.Occurrence, L(Rel.MemberOf),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 105), dwell: 12, delta: 0.0040f, radius: 1, samples: 12,
            weight: 0.44f, opportunityBonusOverride: 0.48f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.PenguinKeepTheOath, "Keep the Oath", K.Penguin, L(Rel.MemberOf),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 10, delta: 0.0036f, radius: 1, samples: 10,
            weight: 0.38f, opportunityBonusOverride: 0.42f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.PenguinCarryMandate, "Carry Mandate", K.Penguin, L(Rel.LedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 10, delta: 0.0037f, radius: 1, samples: 10,
            weight: 0.36f, opportunityBonusOverride: 0.42f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.OrcaCarryOldVoice, "Carry Old Voice", K.Orca, L(Rel.LedBy),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 100), dwell: 10, delta: 0.0037f, radius: 1, samples: 10,
            weight: 0.36f, opportunityBonusOverride: 0.42f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateRecoveryAndConduitStrengthVerbs() =>
    [
        RelationshipStrengthVerb(Verb.RegionDeepenCorruption, "Deepen Corruption", K.Region, L(Rel.CorruptedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (40, 120), dwell: 12, delta: 0.0036f, radius: 1, samples: 10,
            weight: 0.42f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.RegionCoolCorruption, "Cool Corruption", K.Region, L(Rel.CorruptedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (45, 130), dwell: 14, delta: -0.0030f, radius: 1, samples: 10,
            weight: 0.32f, opportunityBonusOverride: 0.30f, fitnessWeight: 0.65f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.ArtifactTendCleansing, "Tend Cleansing", K.Artifact, L(Rel.CleansedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (40, 120), dwell: 12, delta: 0.0034f, radius: 1, samples: 10,
            weight: 0.38f, opportunityBonusOverride: 0.42f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.RegionHoldCleansing, "Hold Cleansing", K.Region, L(Rel.CleansedBy),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (40, 120), dwell: 12, delta: 0.0031f, radius: 1, samples: 10,
            weight: 0.34f, opportunityBonusOverride: 0.38f, fitnessWeight: 0.72f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.RegionForgetCleansing, "Forget Cleansing", K.Region, L(Rel.CleansedBy),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (45, 135), dwell: 12, delta: -0.0028f, radius: 1, samples: 10,
            weight: 0.28f, opportunityBonusOverride: 0.30f, fitnessWeight: 0.65f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.AbilityChargeConduit, "Charge the Conduit", K.Ability, L(Rel.ConduitOf),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 12, delta: 0.0037f, radius: 1, samples: 10,
            weight: 0.45f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.ArtifactTightenWard, "Tighten the Ward", K.Artifact, L(Rel.WardAgainst),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0038f, radius: 1, samples: 10,
            weight: 0.40f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.75f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.TransportOverstrainConduit, "Overstrain the Conduit", K.Transport, L(Rel.ConduitOf),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 10, delta: -0.0037f, radius: 1, samples: 10,
            weight: 0.35f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.TransportChartConduit, "Chart the Conduit", K.Transport, L(Rel.ConduitOf),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 100), dwell: 12, delta: 0.0034f, radius: 1, samples: 10,
            weight: 0.38f, opportunityBonusOverride: 0.40f, fitnessWeight: 0.72f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.TransportCarryTheHaunting, "Carry the Haunting", K.Transport, L(Rel.CorruptedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 10, delta: 0.0034f, radius: 1, samples: 10,
            weight: 0.38f, opportunityBonusOverride: 0.38f, fitnessWeight: 0.70f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.TransportTightenTheWard, "Tighten the Ward", K.Transport, L(Rel.WardAgainst),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0034f, radius: 1, samples: 10,
            weight: 0.36f, opportunityBonusOverride: 0.38f, fitnessWeight: 0.72f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateMonsterAndOccurrenceStrengthVerbs() =>
    [
        RelationshipStrengthVerb(Verb.MonsterDeepenCorruption, "Deepen Corruption", K.Monster, L(Rel.CorruptedBy),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 100), dwell: 10, delta: 0.0037f, radius: 1, samples: 10,
            weight: 0.45f, opportunityBonusOverride: 0.4f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.MonsterHoldWard, "Hold the Ward", K.Monster, L(Rel.WardAgainst),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 105), dwell: 12, delta: 0.0032f, radius: 1, samples: 10,
            weight: 0.32f, opportunityBonusOverride: 0.36f, fitnessWeight: 0.68f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.MonsterGnawWard, "Gnaw the Ward", K.Monster, L(Rel.WardAgainst),
            LowAltitudeVerbRelationshipDirection.Incoming, ticks: (35, 105), dwell: 10, delta: -0.0038f, radius: 1, samples: 10,
            weight: 0.38f, opportunityBonusOverride: 0.40f, fitnessWeight: 0.70f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.OccurrenceExhaustWar, "Exhaust the War", K.Occurrence, L(Rel.AtWarWith),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (40, 120), dwell: 12, delta: -0.0031f, radius: 1, samples: 10,
            weight: 0.4f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreatePointOfInterestClaimStrengthVerbs() =>
    [
        RelationshipStrengthVerb(Verb.PointOfInterestDrawTheOffering, "Draw the Offering", K.PointOfInterest, L(Rel.OfferedTo),
            LowAltitudeVerbRelationshipDirection.Either, ticks: (35, 105), dwell: 12, delta: 0.0038f, radius: 1, samples: 10,
            weight: 0.45f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.8f),
        RelationshipStrengthVerb(Verb.PointOfInterestTightenClaim, "Tighten the Claim", K.PointOfInterest, L(Rel.ClaimedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (35, 105), dwell: 12, delta: 0.0035f, radius: 1, samples: 10,
            weight: 0.42f, opportunityBonusOverride: 0.45f, fitnessWeight: 0.8f, plane: PrimaryPlane.Semantic),
        RelationshipStrengthVerb(Verb.PointOfInterestLoosenClaim, "Loosen the Claim", K.PointOfInterest, L(Rel.ClaimedBy),
            LowAltitudeVerbRelationshipDirection.Outgoing, ticks: (40, 115), dwell: 12, delta: -0.0032f, radius: 1, samples: 10,
            weight: 0.35f, opportunityBonusOverride: 0.35f, fitnessWeight: 0.7f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateSemanticInfluenceVerbs() =>
    [
        ..CreatePenguinArtifactSemanticInfluenceVerbs(),
        ..CreateSettlementFactionRegionSemanticInfluenceVerbs(),
        ..CreateOrcaPointOfInterestSemanticInfluenceVerbs(),
        ..CreateAbilityMonsterTransportOccurrenceSemanticInfluenceVerbs(),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreatePenguinArtifactSemanticInfluenceVerbs() =>
    [
        InfluenceVerb(Verb.PenguinWitness, "Leave Witness", K.Penguin,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (30, 85), dwell: 8, influence: (2.5f, 1, 0f, 0f, 0f), samples: 18, weight: 0.7f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.PenguinCreateWorks, "Create Works", K.Penguin,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 12, influence: (4f, 0, 0f, 0f, 0f), samples: 16, weight: 0.45f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.PenguinBreakChorus, "Break the Chorus", K.Penguin,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (30, 85), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 22, weight: 0.5f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.ArtifactChannel, "Channel", K.Artifact,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 18, weight: 0.7f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.ArtifactDevote, "Consecrate a Wake", K.Artifact,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (40, 115), dwell: 12, influence: (5f, 0, 0f, 0f, 0f), samples: 16, weight: 0.45f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.ArtifactCorrupt, "Bleed Corruption", K.Artifact,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (35, 100), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.5f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateSettlementFactionRegionSemanticInfluenceVerbs() =>
    [
        InfluenceVerb(Verb.SettlementPropaganda, "Paint the Hearth-Signs", K.Settlement,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 95), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 22, weight: 0.85f, opportunityBonusOverride: 0.35f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.SettlementSanctify, "Sanctify the Public Floor", K.Settlement,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (30, 85), dwell: 12, influence: (5.5f, 0, 0f, 0f, 0f), samples: 18, weight: 0.65f, opportunityBonusOverride: 0.15f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.SettlementClaimsEcho, "Claim Echoes", K.Settlement,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (35, 100), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.45f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.FactionIndoctrinate, "Set the Doctrine", K.Faction,
            Op.GrowInfluence, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 100), dwell: 8, influence: (3f, 1, 0f, 0f, 0f), samples: 18, weight: 0.85f, opportunityBonusOverride: 0.7f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.FactionRally, "Rally the Oath", K.Faction,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (30, 90), dwell: 12, influence: (5f, 0, 0f, 0f, 0f), samples: 18, weight: 0.7f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.FactionAgitate, "Agitate the Rival Chorus", K.Faction,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (35, 100), dwell: 8, influence: (4.5f, 1, 0f, 0f, 0f), samples: 24, weight: 0.55f, opportunityBonusOverride: 0.35f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.RegionAttune, "Attune the Place-Spirit", K.Region,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (45, 125), dwell: 12, influence: (3f, 1, 0f, 0f, 0f), samples: 20, weight: 0.8f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.RegionRoot, "Root the Old Dream", K.Region,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (45, 125), dwell: 16, influence: (5f, 0, 0f, 0f, 0f), samples: 18, weight: 0.6f, opportunityBonusOverride: 0.15f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.RegionCleanse, "Cleanse the Thinness", K.Region,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (45, 130), dwell: 12, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.45f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateOrcaPointOfInterestSemanticInfluenceVerbs() =>
    [
        InfluenceVerb(Verb.OrcaSingTheLiturgy, "Sing the Liturgy", K.Orca,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 10, influence: (3.5f, 1, 0f, 0f, 0f), samples: 18, weight: 0.7f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.OrcaLayTheDeepCurrent, "Lay the Deep Current", K.Orca,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (40, 120), dwell: 14, influence: (5f, 0, 0f, 0f, 0f), samples: 16, weight: 0.5f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.PointOfInterestLayTheSign, "Lay the Sign", K.PointOfInterest,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (45, 125), dwell: 12, influence: (3f, 1, 0f, 0f, 0f), samples: 20, weight: 0.65f, opportunityBonusOverride: 0.3f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.PointOfInterestHoldTheLayer, "Hold the Layer", K.PointOfInterest,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (50, 140), dwell: 16, influence: (5f, 0, 0f, 0f, 0f), samples: 18, weight: 0.55f, opportunityBonusOverride: 0.15f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.PointOfInterestDrainTheApproach, "Drain the Approach", K.PointOfInterest,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (40, 115), dwell: 10, influence: (3.5f, 1, 0f, 0f, 0f), samples: 22, weight: 0.45f, opportunityBonusOverride: 0.25f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreateAbilityMonsterTransportOccurrenceSemanticInfluenceVerbs() =>
    [
        InfluenceVerb(Verb.AbilitySpreadTheForm, "Spread the Form", K.Ability,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 10, influence: (3f, 1, 0f, 0f, 0f), samples: 18, weight: 0.65f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.AbilityAnnealTheLineage, "Anneal the Lineage", K.Ability,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (40, 115), dwell: 14, influence: (4.5f, 0, 0f, 0f, 0f), samples: 16, weight: 0.5f, opportunityBonusOverride: 0.2f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.AbilitySuppressARivalForm, "Suppress a Rival Form", K.Ability,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (35, 100), dwell: 10, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.5f, opportunityBonusOverride: 0.3f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.MonsterHumBelow, "Hum Below", K.Monster,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (45, 130), dwell: 12, influence: (3f, 1, 0f, 0f, 0f), samples: 20, weight: 0.6f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.MonsterWhisperToABearer, "Whisper to a Bearer", K.Monster,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (40, 115), dwell: 10, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.5f, opportunityBonusOverride: 0.3f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.TransportCastTheLookout, "Cast the Lookout", K.Transport,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 8, influence: (3f, 1, 0f, 0f, 0f), samples: 18, weight: 0.7f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.OccurrenceGatherTheCrowds, "Gather the Crowds", K.Occurrence,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 10, influence: (4f, 1, 0f, 0f, 0f), samples: 18, weight: 0.75f, opportunityBonusOverride: 0.5f, plane: PrimaryPlane.Semantic),
        InfluenceVerb(Verb.OccurrenceBurnTheWitnesses, "Burn the Witnesses", K.Occurrence,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (40, 115), dwell: 10, influence: (4.5f, 1, 0f, 0f, 0f), samples: 22, weight: 0.6f, opportunityBonusOverride: 0.4f, plane: PrimaryPlane.Semantic),
    ];

    private static IReadOnlyList<LowAltitudeVerbConfig> CreatePhysicalInfluenceVerbs() =>
    [
        InfluenceVerb(Verb.PenguinScatterClaimMarks, "Scatter Claim-Marks", K.Penguin,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (25, 75), dwell: 6, influence: (3f, 1, 0f, 0f, 0f), samples: 24, weight: 0.6f, opportunityBonusOverride: 0.3f),
        InfluenceVerb(Verb.SettlementCastLamplight, "Cast Lamplight", K.Settlement,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 95), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 24, weight: 1.15f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.SettlementBankTheHearth, "Bank the Hearth", K.Settlement,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (25, 70), dwell: 12, influence: (7f, 0, 0f, 0f, 0f), samples: 18, weight: 0.8f, opportunityBonusOverride: 0.1f),
        InfluenceVerb(Verb.SettlementDrownOutClaim, "Drown Out a Claim", K.Settlement,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (30, 85), dwell: 8, influence: (5f, 1, 0f, 0f, 0f), samples: 24, weight: 0.65f, opportunityBonusOverride: 0.2f),
        InfluenceVerb(Verb.FactionSendEnvoys, "Send Envoys", K.Faction,
            Op.GrowInfluence, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 100), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 18, weight: 1.0f, opportunityBonusOverride: 0.8f,
            fitnessRelationshipKind: L(Rel.ControlledBy), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.1f),
        InfluenceVerb(Verb.FactionHoldCouncil, "Hold Council", K.Faction,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (25, 80), dwell: 12, influence: (6f, 1, 0f, 0f, 0f), samples: 20, weight: 0.9f, opportunityBonusOverride: 0.2f),
        InfluenceVerb(Verb.FactionBreakRivalHold, "Break a Rival Hold", K.Faction,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (30, 90), dwell: 8, influence: (5.5f, 1, 0f, 0f, 0f), samples: 26, weight: 0.7f, opportunityBonusOverride: 0.35f),
        // Temporarily disabled while validating whether seat relocation detaches factions from cultural masses.
        // InfluenceVerb(Verb.FactionMoveSeat, "Move the Seat", K.Faction,
        //     Op.MoveHeadquarters, Mode.OwnInfluence,
        //     ticks: (45, 140), dwell: 18, influence: (2f, 0, 0f, 0.04f, 65f), samples: 24, weight: 0.25f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.OrcaMarkTheHuntingGround, "Mark the Hunting Ground", K.Orca,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (30, 90), dwell: 8, influence: (3.5f, 1, 0f, 0f, 0f), samples: 22, weight: 0.85f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.OrcaCullTheEncroachment, "Cull the Encroachment", K.Orca,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (30, 90), dwell: 8, influence: (5f, 1, 0f, 0f, 0f), samples: 24, weight: 0.65f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.PointOfInterestPulseTheBounty, "Pulse the Bounty", K.PointOfInterest,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (40, 115), dwell: 12, influence: (3f, 1, 0f, 0f, 0f), samples: 20, weight: 0.75f, opportunityBonusOverride: 0.35f),
        InfluenceVerb(Verb.MonsterBleedDread, "Bleed Dread", K.Monster,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.7f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.MonsterStainTheIce, "Stain the Ice", K.Monster,
            Op.ConsolidateInfluence, Mode.OwnInfluence,
            ticks: (40, 115), dwell: 14, influence: (5f, 0, 0f, 0f, 0f), samples: 18, weight: 0.5f, opportunityBonusOverride: 0.2f),
        InfluenceVerb(Verb.TransportShowTheFlag, "Show the Flag", K.Transport,
            Op.GrowInfluence, Mode.OwnInfluence,
            ticks: (35, 100), dwell: 8, influence: (4f, 1, 0f, 0f, 0f), samples: 22, weight: 0.95f, opportunityBonusOverride: 0.4f),
        InfluenceVerb(Verb.TransportRunTheTrade, "Run the Trade", K.Transport,
            Op.GrowInfluence, Mode.EntityKind,
            targetKind: K.Settlement, ticks: (35, 100), dwell: 10, influence: (3.5f, 1, 0f, 0f, 0f), samples: 18, weight: 0.85f, opportunityBonusOverride: 0.7f,
            fitnessRelationshipKind: L(Rel.Holds), fitnessDirection: LowAltitudeVerbRelationshipDirection.Incoming, fitnessWeight: 1.2f),
        InfluenceVerb(Verb.TransportBreakARivalHold, "Break a Rival Hold", K.Transport,
            Op.ReduceInfluence, Mode.ForeignInfluence,
            ticks: (35, 100), dwell: 10, influence: (5f, 1, 0f, 0f, 0f), samples: 24, weight: 0.65f, opportunityBonusOverride: 0.4f),
    ];

    private static MovementLowAltitudeVerbConfig MovementVerb(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        float weight,
        float opportunityBonusOverride,
        EntityKind? targetKind = null,
        string? relationshipKind = null,
        IReadOnlySet<string>? terrains = null,
        int minTemperature = 0,
        IReadOnlySet<string>? sites = null,
        int radius = 0,
        int samples = 16,
        string? fitnessRelationshipKind = null,
        LowAltitudeVerbRelationshipDirection fitnessDirection = LowAltitudeVerbRelationshipDirection.Either,
        float fitnessWeight = 0f,
        (float Delta, int Radius)? travelInfluence = null,
        PrimaryPlane plane = PrimaryPlane.Physical,
        float distanceScale = 0f,
        bool fogAdjacent = false,
        bool restAtDestination = false) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind.Value,
            Plane = GameplayPlaneName(plane),
            TargetMode = mode,
            TargetKind = targetKind?.Value,
            RelationshipKind = relationshipKind,
            Terrains = terrains,
            MinTemperature = minTemperature,
            Sites = sites,
            RequireFogAdjacent = fogAdjacent,
            RestAtDestination = restAtDestination,
            MinCommitmentTicks = ScaleMovementTicks(ticks.Min),
            MaxCommitmentTicks = ScaleMovementTicks(ticks.Max),
            DwellTicks = dwell,
            TargetRadius = radius,
            CandidateSampleCount = samples,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            DistanceScaleOverride = distanceScale,
            FitnessRelationshipKind = fitnessRelationshipKind,
            FitnessRelationshipDirection = fitnessDirection,
            FitnessWeight = fitnessWeight,
            TravelInfluenceDelta = travelInfluence?.Delta ?? 0f,
            TravelInfluenceRadius = travelInfluence?.Radius ?? 0,
        };

    private static int ScaleMovementTicks(int ticks) => ticks * MovementCommitmentScale;

    private static RelationshipStrengthLowAltitudeVerbConfig RelationshipStrengthVerb(
        Verb verb,
        string displayName,
        EntityKind kind,
        string relationshipKind,
        LowAltitudeVerbRelationshipDirection relationshipDirection,
        (int Min, int Max) ticks,
        int dwell,
        float delta,
        float weight,
        float opportunityBonusOverride,
        int radius = 1,
        int samples = 12,
        float fitnessWeight = 0f,
        PrimaryPlane plane = PrimaryPlane.Physical) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind.Value,
            Plane = GameplayPlaneName(plane),
            TargetMode = Mode.RelationshipTarget,
            RelationshipKind = relationshipKind,
            RelationshipDirection = relationshipDirection,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            TargetRadius = radius,
            CandidateSampleCount = samples,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            FitnessRelationshipKind = relationshipKind,
            FitnessRelationshipDirection = relationshipDirection,
            FitnessWeight = fitnessWeight,
            FitnessPolarity = delta > 0f
                ? LowAltitudeVerbFitnessPolarity.Strengthen
                : LowAltitudeVerbFitnessPolarity.Weaken,
            FitnessThreshold = delta > 0f ? 0.75f : 0.35f,
            Delta = delta,
        };

    private static LowAltitudeVerbConfig InfluenceVerb(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbOperation operation,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Delta, int Radius, float CoverageChance, float HeadquartersChance, float Required) influence,
        float weight,
        float opportunityBonusOverride,
        EntityKind? targetKind = null,
        string? relationshipKind = null,
        int samples = 16,
        string? fitnessRelationshipKind = null,
        LowAltitudeVerbRelationshipDirection fitnessDirection = LowAltitudeVerbRelationshipDirection.Either,
        float fitnessWeight = 0f,
        PrimaryPlane plane = PrimaryPlane.Physical) =>
        operation switch
        {
            GrowInfluence or
            ConsolidateInfluence or
            ReduceInfluence => InfluenceVerbConfig(
                verb,
                displayName,
                kind,
                operation,
                mode,
                ticks,
                dwell,
                influence,
                weight,
                opportunityBonusOverride,
                targetKind,
                relationshipKind,
                samples,
                fitnessRelationshipKind,
                fitnessDirection,
                fitnessWeight,
                plane),
            ExpandCoverage => CoverageExpansionVerbConfig(
                verb,
                displayName,
                kind,
                mode,
                ticks,
                dwell,
                influence,
                weight,
                opportunityBonusOverride,
                targetKind,
                relationshipKind,
                samples,
                fitnessRelationshipKind,
                fitnessDirection,
                fitnessWeight,
                plane),
            MoveHeadquarters => AnchorRelocationVerbConfig(
                verb,
                displayName,
                kind,
                mode,
                ticks,
                dwell,
                influence,
                weight,
                opportunityBonusOverride,
                targetKind,
                relationshipKind,
                samples,
                fitnessRelationshipKind,
                fitnessDirection,
                fitnessWeight,
                plane),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

    private static InfluenceLowAltitudeVerbConfig InfluenceVerbConfig(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbOperation operation,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Delta, int Radius, float CoverageChance, float HeadquartersChance, float Required) influence,
        float weight,
        float opportunityBonusOverride,
        EntityKind? targetKind,
        string? relationshipKind,
        int samples,
        string? fitnessRelationshipKind,
        LowAltitudeVerbRelationshipDirection fitnessDirection,
        float fitnessWeight,
        PrimaryPlane plane) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind.Value,
            Plane = GameplayPlaneName(plane),
            InfluenceOperation = operation,
            TargetMode = mode,
            TargetKind = targetKind?.Value,
            RelationshipKind = relationshipKind,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            CandidateSampleCount = samples,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            ForeignInfluenceTargeting = ForeignInfluenceTargetingFor(operation, mode),
            FitnessRelationshipKind = fitnessRelationshipKind,
            FitnessRelationshipDirection = fitnessDirection,
            FitnessWeight = fitnessWeight,
            Delta = influence.Delta,
            Radius = influence.Radius,
        };

    private static ForeignInfluenceTargetingConfig? ForeignInfluenceTargetingFor(
        LowAltitudeVerbOperation operation,
        LowAltitudeVerbTargetMode mode) =>
        operation == ReduceInfluence && mode == Mode.ForeignInfluence
            ? new ForeignInfluenceTargetingConfig
            {
                BlockPositiveRelationships = true,
                AllowNegativeRelationships = true,
                MinimumRelationshipStrength = 0.05f,
                AllowUnrelatedInfluenceAtOrAbove = 50f,
            }
            : null;

    private static CoverageExpansionLowAltitudeVerbConfig CoverageExpansionVerbConfig(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Delta, int Radius, float CoverageChance, float HeadquartersChance, float Required) influence,
        float weight,
        float opportunityBonusOverride,
        EntityKind? targetKind,
        string? relationshipKind,
        int samples,
        string? fitnessRelationshipKind,
        LowAltitudeVerbRelationshipDirection fitnessDirection,
        float fitnessWeight,
        PrimaryPlane plane) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind.Value,
            Plane = GameplayPlaneName(plane),
            TargetMode = mode,
            TargetKind = targetKind?.Value,
            RelationshipKind = relationshipKind,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            CandidateSampleCount = samples,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            FitnessRelationshipKind = fitnessRelationshipKind,
            FitnessRelationshipDirection = fitnessDirection,
            FitnessWeight = fitnessWeight,
            Delta = influence.Delta,
            Radius = influence.Radius,
            GrowthChance = influence.CoverageChance,
            RequiredMapValue = influence.Required,
        };

    private static AnchorRelocationLowAltitudeVerbConfig AnchorRelocationVerbConfig(
        Verb verb,
        string displayName,
        EntityKind kind,
        LowAltitudeVerbTargetMode mode,
        (int Min, int Max) ticks,
        int dwell,
        (float Delta, int Radius, float CoverageChance, float HeadquartersChance, float Required) influence,
        float weight,
        float opportunityBonusOverride,
        EntityKind? targetKind,
        string? relationshipKind,
        int samples,
        string? fitnessRelationshipKind,
        LowAltitudeVerbRelationshipDirection fitnessDirection,
        float fitnessWeight,
        PrimaryPlane plane) =>
        new()
        {
            Id = V(verb),
            DisplayName = displayName,
            Kind = kind.Value,
            Plane = GameplayPlaneName(plane),
            TargetMode = mode,
            TargetKind = targetKind?.Value,
            RelationshipKind = relationshipKind,
            MinCommitmentTicks = ticks.Min,
            MaxCommitmentTicks = ticks.Max,
            DwellTicks = dwell,
            CandidateSampleCount = samples,
            Weight = weight,
            OpportunityBonusOverride = opportunityBonusOverride,
            FitnessRelationshipKind = fitnessRelationshipKind,
            FitnessRelationshipDirection = fitnessDirection,
            FitnessWeight = fitnessWeight,
            Delta = influence.Delta,
            Radius = influence.Radius,
            RelocationChance = influence.HeadquartersChance,
            RequiredMapValue = influence.Required,
        };

    // The water family, derived from the catalog: any terrain marked as
    // water joins automatically when the catalog grows.
    private static HashSet<string> MaritimeTerrains() =>
        IceRemembersTerrainCatalog.DomainTerrains
            .Where(terrain => terrain.IsWater)
            .Select(terrain => terrain.Name)
            .ToHashSet(StringComparer.Ordinal);
}
