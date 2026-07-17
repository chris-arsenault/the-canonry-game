namespace IceRemembers;

/// <summary>
/// The authored technology list. Each tech is a discrete, named thing that sits
/// at one hex of the semantic (idea) plane, placed by its tier (radius band from
/// centre) and its discipline (one of six directions, the ES2-style spokes). A
/// culture comes to know a tech when its idea-mass reaches that hex and holds it.
///
/// This list is hand-authored and meant to GROW: adding a technology is adding a
/// line here (and giving it a tier + discipline). There is no (tier x terrain)
/// cap and nothing procedural - the catalogue is exactly what is written below.
/// Discipline is the angular spoke, not the terrain, because idea-ground opened
/// beyond the heart is uniform open field; terrain flavours the heart only.
/// </summary>
public enum KnowledgeDiscipline
{
    Remembrance = 0, // keeping what the ice would forget
    Deep = 1,        // descent, the god below, what waits
    Craft = 2,       // making: hulls, tools, ice-work
    Hunt = 3,        // war, predation, the blooding
    Song = 4,        // voice, cant, the carried name
    Cold = 5,        // the ice itself: thaw, freeze, weather
}

public sealed record KnowledgeTech(string Id, string Name, int Tier, KnowledgeDiscipline Discipline);

public static class IceRemembersKnowledge
{
    /// <summary>Ring width of a tier band; tier t spans semantic rings [t*TierWidth, (t+1)*TierWidth).</summary>
    public static int TierWidth => 8;

    public static IReadOnlyList<KnowledgeTech> Techs { get; } =
    [
        // Tier 0 - foundational, everyone learns these near home.
        new("record_carving", "Record-Carving", 0, KnowledgeDiscipline.Remembrance),
        new("breath_holding_dives", "Breath-Holding Dives", 0, KnowledgeDiscipline.Deep),
        new("bone_tools", "Bone Tools", 0, KnowledgeDiscipline.Craft),
        new("ice_fishing", "Ice Fishing", 0, KnowledgeDiscipline.Hunt),
        new("call_signals", "Call-Signals", 0, KnowledgeDiscipline.Song),
        new("windbreaks", "Windbreaks", 0, KnowledgeDiscipline.Cold),

        // Tier 1 - the heart's craft.
        new("rookery_schools", "Rookery Schools", 1, KnowledgeDiscipline.Remembrance),
        new("deep_netting", "Deep Netting", 1, KnowledgeDiscipline.Deep),
        new("ice_block_building", "Ice-Block Building", 1, KnowledgeDiscipline.Craft),
        new("improved_ice_fishing", "Improved Ice Fishing", 1, KnowledgeDiscipline.Hunt),
        new("echo_hunting", "Echo-Hunting", 1, KnowledgeDiscipline.Song),
        new("aurora_light_spectacles", "Aurora Light Spectacles", 1, KnowledgeDiscipline.Cold),

        // Tier 2 - reaches the heart's edge.
        new("lineage_charts", "Lineage Charts", 2, KnowledgeDiscipline.Remembrance),
        new("trench_charting", "Trench Charting", 2, KnowledgeDiscipline.Deep),
        new("sled_building", "Sled Building", 2, KnowledgeDiscipline.Craft),
        new("krill_traps", "Krill Traps", 2, KnowledgeDiscipline.Hunt),
        new("long_distance_calls", "Long-Distance Calls", 2, KnowledgeDiscipline.Song),
        new("thaw_forecasting", "Thaw Forecasting", 2, KnowledgeDiscipline.Cold),

        // Tier 3 - out past the heart, earned by expansion.
        new("stone_libraries", "Stone Libraries", 3, KnowledgeDiscipline.Remembrance),
        new("pressure_diving", "Pressure Diving", 3, KnowledgeDiscipline.Deep),
        new("ice_ship_hulls", "Ice-Ship Hulls", 3, KnowledgeDiscipline.Craft),
        new("pack_hunting", "Pack Hunting", 3, KnowledgeDiscipline.Hunt),
        new("pod_coordination", "Pod Coordination", 3, KnowledgeDiscipline.Song),
        new("ice_cellars", "Ice Cellars", 3, KnowledgeDiscipline.Cold),

        // Tier 4 - the far rim; few reach these.
        new("abyssal_mapping", "Abyssal Mapping", 4, KnowledgeDiscipline.Deep),
        new("reinforced_hulls", "Reinforced Hulls", 4, KnowledgeDiscipline.Craft),
        new("storm_prediction", "Storm Prediction", 4, KnowledgeDiscipline.Cold),
    ];
}
