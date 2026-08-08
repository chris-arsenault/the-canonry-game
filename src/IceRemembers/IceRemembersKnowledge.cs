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

    // The recut catalog (docs/design/cultural-expansion-and-knowledge.md):
    // most techs are enablement gates whose effect lives as a
    // CultureHoldsTech precondition or generator RequiredTech on the content
    // they open; the Grants list holds only multipliers at >= 2x / <= 0.5x.
    // Tiers 0-1 are mundane arts; tiers 2-4 climb the Three Fires.
    public static IReadOnlyList<KnowledgeTech> Techs { get; } =
    [
        new("record_carving", "Record-Carving", 0, KnowledgeDiscipline.Remembrance,
            [],
            "Gate: readers can sing fading canons back into observance (Read the Ice)."),
        new("breath_holding_dives", "Breath-Holding Dives", 0, KnowledgeDiscipline.Deep,
            [VerbWeight(Verb.ArtifactSinkSeaward, 2.0f)],
            "What sinks beneath the ice can be brought back; sunken relics surface twice as often."),
        new("bone_tools", "Bone Tools", 0, KnowledgeDiscipline.Craft,
            [Global(GrantDomain.MinorActorWork, 2.0f)],
            "Every working flipper cuts twice as deep; all minor-actor labour doubles."),
        new("ice_fishing", "Ice Fishing", 0, KnowledgeDiscipline.Hunt,
            [VerbWeight(Verb.PenguinGoFishing, 2.0f), VerbWeight(Verb.OrcaHuntTheMark, 2.0f)],
            "The first provisioning art: fishing and the ordinary hunt double."),
        new("call_signals", "Call-Signals", 0, KnowledgeDiscipline.Song,
            [Relationship(Rel.AlliedWith, 2.0f)],
            "Voices carry across the ice; allied bonds maintain twice as strongly."),
        new("windbreaks", "Windbreaks", 0, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Physical, 0.5f)],
            "Walls against the wind: physical culture weathers at half speed."),

        new("rookery_schools", "Rookery Schools", 1, KnowledgeDiscipline.Remembrance,
            [],
            "Gate: practitioners may take students; teaching lineages exist (Pass the Teaching)."),
        new("deep_netting", "Deep Netting", 1, KnowledgeDiscipline.Deep,
            [Relationship(Rel.OfferedTo, 2.0f)],
            "Nets drawn from the dark water feed the sites; offering bonds maintain twice as strongly."),
        new("ice_block_building", "Ice-Block Building", 1, KnowledgeDiscipline.Craft,
            [VerbWeight(Verb.SettlementBankTheHearth, 2.0f)],
            "Cut block holds heat; settlements bank their hearths twice as often."),
        new("breathing_hole_watch", "The Breathing-Hole Watch", 1, KnowledgeDiscipline.Hunt,
            [],
            "Gate: hunters learn the wait at the holes and can corner great beasts (Corner the Beast)."),
        new("echo_hunting", "Echo-Hunting", 1, KnowledgeDiscipline.Song,
            [],
            "Gate: sung echoes mark territory; hunting grounds can be claimed (Claim the Hunting Ground)."),
        new("aurora_light_spectacles", "Aurora Light Spectacles", 1, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.Attraction, PlaneCodes.Physical, 2.0f)],
            "Caught light draws the living; physical attraction doubles."),

        new("lineage_charts", "Lineage Charts", 2, KnowledgeDiscipline.Remembrance,
            [Relationship(Rel.MemberOf, 2.0f), Relationship(Rel.LedBy, 2.0f), Relationship(Rel.TaughtBy, 2.0f)],
            "Aurora-Light's first ledger: descent, leadership, and teaching bonds maintain twice as strongly."),
        new("trench_charting", "Trench Charting", 2, KnowledgeDiscipline.Deep,
            [],
            "Gate: charted trenches let conduit-mapping expeditions exist (conduits_map_hidden_peaks)."),
        new("carved_ways", "Carved Ways", 2, KnowledgeDiscipline.Craft,
            [Kind(GrantDomain.MovementStepRate, K.Penguin, 0.5f), Kind(GrantDomain.MovementStepRate, K.Faction, 0.5f)],
            "Runner-carved roads between the holds: penguins and factions travel at half cost."),
        new("long_dark_larders", "Long-Dark Larders", 2, KnowledgeDiscipline.Hunt,
            [Plane(GrantDomain.CarryingCapacity, PlaneCodes.Physical, 2.0f)],
            "Stores against the dark: the land carries twice the living."),
        new("long_distance_calls", "Long-Distance Calls", 2, KnowledgeDiscipline.Song,
            [],
            "Gate: subsonic calls reach the pods; factions can parley across the water (Parley with the Pod)."),
        new("thaw_forecasting", "Thaw Forecasting", 2, KnowledgeDiscipline.Cold,
            [Global(GrantDomain.FrontierThawThreshold, 0.5f)],
            "Reading the turn of seasons: frontier cells open at half the mass threshold during the Thaw."),

        new("stone_libraries", "Stone Libraries", 3, KnowledgeDiscipline.Remembrance,
            [Plane(GrantDomain.CulturePersistence, PlaneCodes.Semantic, 0.5f)],
            "What is carved in stone the ice cannot blur; semantic culture fades at half speed."),
        new("pressure_diving", "Pressure Diving", 3, KnowledgeDiscipline.Deep,
            [],
            "Gate: divers carry the weight of the deep; deep currents are laid and charged sites answer (Lay the Deep Current; The Current Provides)."),
        new("ice_ship_hulls", "Ice-Ship Hulls", 3, KnowledgeDiscipline.Craft,
            [Kind(GrantDomain.MovementStepRate, K.Transport, 0.5f)],
            "Gate and hull: transports travel at half cost, and charters can find harbors (charters_find_harbors)."),
        new("pack_hunting", "Pack Hunting", 3, KnowledgeDiscipline.Hunt,
            [],
            "Gate: the coordinated kill - monsters slain, orca brought down, trade routes raided (Slay the Monster; Slay the Orca; Raid the Trade Route)."),
        new("pod_coordination", "Pod Coordination", 3, KnowledgeDiscipline.Song,
            [Purpose(MinorActorRoleCodes.CampaignColumn, 2.0f)],
            "Gate and column: banner-marches exist (March the Banners) and campaign columns work twice as hard."),
        new("ice_cellars", "Ice Cellars", 3, KnowledgeDiscipline.Cold,
            [Plane(GrantDomain.DecayTideResistance, PlaneCodes.Physical, 0.5f)],
            "Cold kept against the cold: Long-Dark decay tides lose half their force."),

        new("abyssal_mapping", "Abyssal Mapping", 4, KnowledgeDiscipline.Deep,
            [],
            "Gate: the way down to the patient attention is known - elders descend to the god and relics are given to the deep."),
        new("before_times_plate", "Plate of the Before Times", 4, KnowledgeDiscipline.Craft,
            [],
            "Gate: hulls sheathed in salvaged serial-numbered metal survey the far reaches and open conduits (Survey the Region; Open a Conduit)."),
        new("storm_prediction", "Storm Prediction", 4, KnowledgeDiscipline.Cold,
            [Global(GrantDomain.FrontierReclaimThreshold, 0.5f), Plane(GrantDomain.DecayTideResistance, PlaneCodes.Physical, 0.5f)],
            "Reading what the ice half-remembers of weather to come: refreeze needs half the mass and decay tides halve."),
    ];

    private static KnowledgeGrantConfig Plane(KnowledgeGrantDomain domain, int planeCode, float multiplier) =>
        new(domain, multiplier, TargetCode: planeCode);

    private static KnowledgeGrantConfig Global(KnowledgeGrantDomain domain, float multiplier) =>
        new(domain, multiplier);

    private static KnowledgeGrantConfig VerbWeight(IceRemembersVerbId verb, float multiplier) =>
        new(GrantDomain.VerbWeight, multiplier, verb.ToVerbId());

    private static KnowledgeGrantConfig Relationship(IceRemembersRelationship relationship, float multiplier) =>
        new(GrantDomain.RelationshipMaintenance, multiplier, relationship.ToDomainName());

    private static KnowledgeGrantConfig Kind(KnowledgeGrantDomain domain, KindId kind, float multiplier) =>
        new(domain, multiplier, kind.Value);

    private static KnowledgeGrantConfig Purpose(int purposeCode, float multiplier) =>
        new(GrantDomain.MinorActorWork, multiplier, TargetCode: purposeCode);
}
