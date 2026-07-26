namespace IceRemembers;

using Engine.Constants;
using Engine.Gameplay;
using Engine.Kinds;

using Gambit = IceRemembersGambitId;
using GrantDomain = Engine.Gameplay.KnowledgeGrantDomain;
using K = IceRemembersKinds;
using Rel = IceRemembersRelationship;
using Verb = IceRemembersVerbId;

/// <summary>
/// The six authored spokes of the semantic knowledge map.
/// </summary>
public enum KnowledgeDiscipline
{
    Remembrance = 0,
    Deep = 1,
    Craft = 2,
    Hunt = 3,
    Song = 4,
    Cold = 5,
}

/// <summary>
/// One named technology, its semantic placement, and the explicit gameplay
/// modifiers held while a culture claims its site.
/// </summary>
public sealed record KnowledgeTech(
    string Id,
    string Name,
    int Tier,
    KnowledgeDiscipline Discipline,
    IReadOnlyList<KnowledgeGrantConfig> Grants,
    string Grant);

public static class IceRemembersKnowledge
{
    /// <summary>Ring width of a tier band; tier t spans semantic rings [t*TierWidth, (t+1)*TierWidth).</summary>
    public static int TierWidth => 8;

    public static IReadOnlyList<KnowledgeTech> Techs { get; } =
    [
        new("record_carving", "Record-Carving", 0, KnowledgeDiscipline.Remembrance,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Semantic, 0.94f), Relationship(Rel.Memorializes, 1.25f)],
            "Semantic influence fades more slowly, and memorial bonds maintain more strongly."),
        new("breath_holding_dives", "Breath-Holding Dives", 0, KnowledgeDiscipline.Deep,
            [VerbWeight(Verb.PenguinGoFishing, 1.25f), VerbWeight(Verb.ArtifactSinkSeaward, 1.25f)],
            "Fishing and the recovery of sunken relics become more common."),
        new("bone_tools", "Bone Tools", 0, KnowledgeDiscipline.Craft,
            [Global(GrantDomain.MinorActorWork, 1.12f)],
            "All of the culture's minor-actor labour becomes more effective."),
        new("ice_fishing", "Ice Fishing", 0, KnowledgeDiscipline.Hunt,
            [Plane(GrantDomain.CarryingCapacity, PlaneCodes.Physical, 1.10f), VerbWeight(Verb.PenguinGoFishing, 1.20f)],
            "Fishing improves and physical settlements support denser populations."),
        new("call_signals", "Call-Signals", 0, KnowledgeDiscipline.Song,
            [Relationship(Rel.AlliedWith, 1.25f), VerbWeight(Verb.OrcaSoundThePod, 1.20f), VerbWeight(Verb.FactionSendEnvoys, 1.20f)],
            "Allied bonds hold and long-range coordination becomes more common."),
        new("windbreaks", "Windbreaks", 0, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Physical, 0.92f), VerbWeight(Verb.SettlementBankTheHearth, 1.25f)],
            "Physical culture weathers more slowly and hearths are banked more often."),

        new("rookery_schools", "Rookery Schools", 1, KnowledgeDiscipline.Remembrance,
            [Generator("what_the_artifact_knows", 1.25f), Generator("sanctuaries_anchor_colonies", 1.25f)],
            "Schools make knowledge-bearing and colony-founding developments more likely."),
        new("deep_netting", "Deep Netting", 1, KnowledgeDiscipline.Deep,
            [Plane(GrantDomain.CarryingCapacity, PlaneCodes.Physical, 1.14f), VerbWeight(Verb.PenguinGoFishing, 1.15f)],
            "Deep catches support denser physical settlement and more fishing."),
        new("ice_block_building", "Ice-Block Building", 1, KnowledgeDiscipline.Craft,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Physical, 0.86f), VerbWeight(Verb.SettlementBankTheHearth, 1.30f)],
            "Built settlements retain physical culture and tend their hearths more strongly."),
        new("improved_ice_fishing", "Improved Ice Fishing", 1, KnowledgeDiscipline.Hunt,
            [Plane(GrantDomain.CarryingCapacity, PlaneCodes.Physical, 1.18f), VerbWeight(Verb.PenguinGoFishing, 1.25f)],
            "Food surplus raises physical carrying capacity and fishing activity."),
        new("echo_hunting", "Echo-Hunting", 1, KnowledgeDiscipline.Song,
            [VerbWeight(Verb.OrcaHuntTheMark, 1.35f), GambitScore(Gambit.OrcaClaimsHuntingGround, 1.20f), GambitSuccess(Gambit.OrcaClaimsHuntingGround, 1.12f)],
            "Echolocation sharpens ordinary hunts and the claiming of hunting grounds."),
        new("aurora_light_spectacles", "Aurora Light Spectacles", 1, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.Attraction, PlaneCodes.Physical, 1.25f), VerbWeight(Verb.SettlementCastLamplight, 1.30f)],
            "Light-craft draws crowds and makes lamplight gatherings more common."),

        new("lineage_charts", "Lineage Charts", 2, KnowledgeDiscipline.Remembrance,
            [
                Relationship(Rel.MemberOf, 1.30f), Relationship(Rel.LedBy, 1.30f), Relationship(Rel.TaughtBy, 1.30f),
                VerbWeight(Verb.PenguinKeepTheOath, 1.20f), VerbWeight(Verb.PenguinCarryMandate, 1.20f),
                VerbWeight(Verb.OrcaCarryOldVoice, 1.20f), VerbWeight(Verb.AbilityPassThroughTheLineage, 1.20f),
            ],
            "Recorded descent preserves affiliation, teaching, and inherited obligations."),
        new("trench_charting", "Trench Charting", 2, KnowledgeDiscipline.Deep,
            [
                Generator("conduits_map_hidden_peaks", 1.25f), Generator("conduit_points_found_reefholds", 1.25f),
                Generator("deep_conduits_name_depths", 1.25f), VerbWeight(Verb.TransportChartConduit, 1.25f),
            ],
            "Mapped trenches encourage depth and conduit developments and active charting."),
        new("sled_building", "Sled Building", 2, KnowledgeDiscipline.Craft,
            [Kind(GrantDomain.MovementStepRate, K.Penguin, 0.90f), Kind(GrantDomain.MovementStepRate, K.Faction, 0.90f), Global(GrantDomain.RoadWork, 1.25f)],
            "Penguins and factions travel more cheaply, while road labour improves."),
        new("krill_traps", "Krill Traps", 2, KnowledgeDiscipline.Hunt,
            [Plane(GrantDomain.CultureProduction, PlaneCodes.Physical, 1.12f), VerbWeight(Verb.PointOfInterestPulseTheBounty, 1.20f)],
            "Bulk harvest feeds physical expansion and makes bounty sites more productive."),
        new("long_distance_calls", "Long-Distance Calls", 2, KnowledgeDiscipline.Song,
            [
                Plane(GrantDomain.CultureProduction, PlaneCodes.Semantic, 1.12f), VerbWeight(Verb.SettlementGossip, 1.25f),
                VerbWeight(Verb.OccurrenceSpreadTheWord, 1.25f), VerbWeight(Verb.OrcaCarryTheDeedName, 1.25f),
            ],
            "Semantic influence and the ordinary circulation of news become stronger."),
        new("thaw_forecasting", "Thaw Forecasting", 2, KnowledgeDiscipline.Cold,
            [Global(GrantDomain.FrontierThawThreshold, 0.82f)],
            "During the Thaw, the culture can open frontier cells at a lower mass threshold."),

        new("stone_libraries", "Stone Libraries", 3, KnowledgeDiscipline.Remembrance,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Semantic, 0.72f), VerbWeight(Verb.RegionKeepMemorial, 1.35f)],
            "Semantic culture fades much more slowly and memorial continuity strengthens."),
        new("pressure_diving", "Pressure Diving", 3, KnowledgeDiscipline.Deep,
            [
                Relationship(Rel.ConsignedTo, 1.30f), Relationship(Rel.Anchored, 1.30f), Relationship(Rel.ConduitOf, 1.30f),
                VerbWeight(Verb.ArtifactHoldItsAnchor, 1.20f), VerbWeight(Verb.AbilityChargeConduit, 1.20f), VerbWeight(Verb.TransportChartConduit, 1.20f),
            ],
            "Deep custody and conduit bonds hold while their ordinary maintenance becomes more active."),
        new("ice_ship_hulls", "Ice-Ship Hulls", 3, KnowledgeDiscipline.Craft,
            [
                Kind(GrantDomain.MovementStepRate, K.Transport, 0.85f), VerbWeight(Verb.TransportSailTheShelf, 1.25f),
                VerbWeight(Verb.TransportMakePort, 1.25f), VerbWeight(Verb.TransportRunTheTrade, 1.25f), Generator("charters_find_harbors", 1.20f),
            ],
            "Transports travel more cheaply and sailing, port, trade, and harbor activity rises."),
        new("pack_hunting", "Pack Hunting", 3, KnowledgeDiscipline.Hunt,
            [
                GambitScore(Gambit.PenguinSlaysMonster, 1.25f), GambitSuccess(Gambit.PenguinSlaysMonster, 1.15f),
                GambitScore(Gambit.PenguinCornersTheBeast, 1.25f), GambitSuccess(Gambit.PenguinCornersTheBeast, 1.15f),
                GambitScore(Gambit.PenguinSlaysOrca, 1.25f), GambitSuccess(Gambit.PenguinSlaysOrca, 1.15f),
            ],
            "Coordinated hunters score and succeed more often in major hunts and slayings."),
        new("pod_coordination", "Pod Coordination", 3, KnowledgeDiscipline.Song,
            [
                Purpose(MinorActorPurposeCodes.CampaignLabor, 1.25f), VerbWeight(Verb.OrcaReinforcePodTie, 1.25f),
                VerbWeight(Verb.FactionFeedTheColumn, 1.25f), GambitScore(Gambit.FactionMarchesTheBanners, 1.20f),
                GambitSuccess(Gambit.FactionMarchesTheBanners, 1.12f),
            ],
            "Campaign labour, pod upkeep, columns, and banner-marches become more effective."),
        new("ice_cellars", "Ice Cellars", 3, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.CarryingCapacity, PlaneCodes.Physical, 1.22f), Plane(GrantDomain.DecayTideResistance, PlaneCodes.Physical, 0.70f)],
            "Physical settlements carry more people and resist excess Long-Dark decay."),

        new("abyssal_mapping", "Abyssal Mapping", 4, KnowledgeDiscipline.Deep,
            [
                Generator("controlled_depths_wake_leviathans", 1.30f), Generator("elders_descend_to_the_god", 1.30f),
                Generator("relics_are_given_to_the_deep", 1.30f), Generator("the_current_provides", 1.30f),
                GambitScore(Gambit.OrcaLaysDeepCurrent, 1.25f), GambitSuccess(Gambit.OrcaLaysDeepCurrent, 1.15f),
                GambitScore(Gambit.PenguinCallsTheDeepCurrent, 1.25f), GambitSuccess(Gambit.PenguinCallsTheDeepCurrent, 1.15f),
            ],
            "Deep-current, offering, and leviathan developments rise, as do deep-current gambits."),
        new("reinforced_hulls", "Reinforced Hulls", 4, KnowledgeDiscipline.Craft,
            [
                Kind(GrantDomain.MovementStepRate, K.Transport, 0.75f), VerbWeight(Verb.TransportSailTheShelf, 1.30f),
                VerbWeight(Verb.TransportPatrolTheReach, 1.30f), VerbWeight(Verb.TransportRunTheTrade, 1.30f),
                GambitScore(Gambit.TransportSurveysRegion, 1.25f), GambitSuccess(Gambit.TransportSurveysRegion, 1.15f),
                GambitScore(Gambit.TransportOpensConduit, 1.25f), GambitSuccess(Gambit.TransportOpensConduit, 1.15f),
                GambitScore(Gambit.OrcaRaidsTradeRoute, 1.25f), GambitSuccess(Gambit.OrcaRaidsTradeRoute, 1.15f),
            ],
            "Deep-water transports travel farther and sea, trade, and route gambits improve."),
        new("storm_prediction", "Storm Prediction", 4, KnowledgeDiscipline.Cold,
            [Global(GrantDomain.FrontierReclaimThreshold, 0.50f), Plane(GrantDomain.DecayTideResistance, PlaneCodes.Physical, 0.65f)],
            "Refreeze needs less local mass and catastrophic physical decay tides lose force."),
    ];

    private static KnowledgeGrantConfig Plane(KnowledgeGrantDomain domain, int planeCode, float multiplier) =>
        new(domain, multiplier, TargetCode: planeCode);

    private static KnowledgeGrantConfig Global(KnowledgeGrantDomain domain, float multiplier) =>
        new(domain, multiplier);

    private static KnowledgeGrantConfig VerbWeight(IceRemembersVerbId verb, float multiplier) =>
        new(GrantDomain.VerbWeight, multiplier, verb.ToVerbId());

    private static KnowledgeGrantConfig Generator(string generator, float multiplier) =>
        new(GrantDomain.GeneratorFireWeight, multiplier, generator);

    private static KnowledgeGrantConfig GambitScore(IceRemembersGambitId gambit, float multiplier) =>
        new(GrantDomain.GambitScore, multiplier, gambit.ToGambitId());

    private static KnowledgeGrantConfig GambitSuccess(IceRemembersGambitId gambit, float multiplier) =>
        new(GrantDomain.GambitSuccess, multiplier, gambit.ToGambitId());

    private static KnowledgeGrantConfig Relationship(IceRemembersRelationship relationship, float multiplier) =>
        new(GrantDomain.RelationshipMaintenance, multiplier, relationship.ToDomainName());

    private static KnowledgeGrantConfig Kind(KnowledgeGrantDomain domain, KindId kind, float multiplier) =>
        new(domain, multiplier, kind.Value);

    private static KnowledgeGrantConfig Purpose(int purposeCode, float multiplier) =>
        new(GrantDomain.MinorActorWork, multiplier, TargetCode: purposeCode);
}
