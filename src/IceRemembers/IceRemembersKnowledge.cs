namespace IceRemembers;

/// <summary>
/// The authored technology list. Each tech is a discrete, named thing that sits
/// at one hex of the semantic (idea) plane, placed by its tier (radius band from
/// centre) and its discipline (one of six spokes). A culture comes to know a
/// tech when its idea-mass reaches that hex and holds it.
///
/// The point of a tech is its GRANT - what knowing it does in the world. Every
/// tech below carries an intended grant expressed against a real mechanic
/// (culture-influence production, minor-actor labour, movement/road, carrying
/// capacity, relationship maintenance, gambit success, generator pulse weight,
/// world-beat resilience, decay). These are the design intent; the grants are
/// not yet wired to runtime charges/unlocks/effects (that build needs a runtime
/// CultureKnows state - the sites move into the kernel then). The list is
/// hand-authored and grows by adding a line: name it, tier it, slot its
/// discipline, and say what it grants.
/// </summary>
public enum KnowledgeDiscipline
{
    Remembrance = 0, // keeping what the ice would forget: records, lore, canons
    Deep = 1,        // descent, the god below, the deep sea and its custody
    Craft = 2,       // making: tools, building, hulls, transport
    Hunt = 3,        // fishing, predation, war, food
    Song = 4,        // voice, calls, coordination
    Cold = 5,        // the ice itself: shelter, weather, aurora, survival
}

public sealed record KnowledgeTech(
    string Id,
    string Name,
    int Tier,
    KnowledgeDiscipline Discipline,
    string Grant);

public static class IceRemembersKnowledge
{
    /// <summary>Ring width of a tier band; tier t spans semantic rings [t*TierWidth, (t+1)*TierWidth).</summary>
    public static int TierWidth => 8;

    public static IReadOnlyList<KnowledgeTech> Techs { get; } =
    [
        // Tier 0 - foundational, everyone learns these near home.
        new("record_carving", "Record-Carving", 0, KnowledgeDiscipline.Remembrance,
            "Canons this culture keeps lose observance more slowly - its laws and customs persist."),
        new("breath_holding_dives", "Breath-Holding Dives", 0, KnowledgeDiscipline.Deep,
            "Opens the shallows of the deep: a small food/health gain and access to sunken relics."),
        new("bone_tools", "Bone Tools", 0, KnowledgeDiscipline.Craft,
            "Better tools: the culture's minor-actor labour (culture work) is more effective."),
        new("ice_fishing", "Ice Fishing", 0, KnowledgeDiscipline.Hunt,
            "A reliable catch: reduces starvation and health decay for the culture's living entities."),
        new("call_signals", "Call-Signals", 0, KnowledgeDiscipline.Song,
            "Signalling holds groups together: allied and pod bonds maintain stronger."),
        new("windbreaks", "Windbreaks", 0, KnowledgeDiscipline.Cold,
            "Shelter from the wind: reduces cold and health decay - entities live longer."),

        // Tier 1 - the heart's craft.
        new("rookery_schools", "Rookery Schools", 1, KnowledgeDiscipline.Remembrance,
            "The culture's entity- and settlement-founding generators fire more often (higher pulse weight)."),
        new("deep_netting", "Deep Netting", 1, KnowledgeDiscipline.Deep,
            "Deep fishing lifts the culture's carrying capacity (supports denser population)."),
        new("ice_block_building", "Ice-Block Building", 1, KnowledgeDiscipline.Craft,
            "Sturdier settlements: the culture's settlements hold more culture mass and resist decay."),
        new("improved_ice_fishing", "Improved Ice Fishing", 1, KnowledgeDiscipline.Hunt,
            "Food surplus: raises carrying capacity and settlement growth."),
        new("echo_hunting", "Echo-Hunting", 1, KnowledgeDiscipline.Song,
            "Echolocation: higher hunting success (predation gambits, orca hunts)."),
        new("aurora_light_spectacles", "Aurora Light Spectacles", 1, KnowledgeDiscipline.Cold,
            "Light-craft draws a crowd: raises the culture's attraction signal (gatherings, festivals)."),

        // Tier 2 - reaches the heart's edge.
        new("lineage_charts", "Lineage Charts", 2, KnowledgeDiscipline.Remembrance,
            "Affiliation bonds (member-of, allied-with, home-base) maintain stronger for the culture."),
        new("trench_charting", "Trench Charting", 2, KnowledgeDiscipline.Deep,
            "The culture's depth/conduit generators (reefholds, named depths) fire more often."),
        new("sled_building", "Sled Building", 2, KnowledgeDiscipline.Craft,
            "Overland haulage: faster physical movement and road-building for the culture."),
        new("krill_traps", "Krill Traps", 2, KnowledgeDiscipline.Hunt,
            "Bulk harvest: surplus fuels expansion - higher culture-influence production."),
        new("long_distance_calls", "Long-Distance Calls", 2, KnowledgeDiscipline.Song,
            "Word travels far: the culture's influence spreads and coordinates over distance (stronger border labour)."),
        new("thaw_forecasting", "Thaw Forecasting", 2, KnowledgeDiscipline.Cold,
            "Reading the melt: during Thaw beats the culture's frontier opens faster."),

        // Tier 3 - out past the heart, earned by expansion.
        new("stone_libraries", "Stone Libraries", 3, KnowledgeDiscipline.Remembrance,
            "The culture forgets less: its held knowledge (semantic mass) decays more slowly."),
        new("pressure_diving", "Pressure Diving", 3, KnowledgeDiscipline.Deep,
            "Feeds the shadow economy: stronger custody of relics and abilities drawn from the deep."),
        new("ice_ship_hulls", "Ice-Ship Hulls", 3, KnowledgeDiscipline.Craft,
            "Seagoing hulls: stronger transports/carriers and trade-route generators."),
        new("pack_hunting", "Pack Hunting", 3, KnowledgeDiscipline.Hunt,
            "Coordinated predation: higher success and score for the culture's hunt and slaying gambits."),
        new("pod_coordination", "Pod Coordination", 3, KnowledgeDiscipline.Song,
            "Group tactics: stronger campaigns and pod wars (column labour + war-gambit success)."),
        new("ice_cellars", "Ice Cellars", 3, KnowledgeDiscipline.Cold,
            "Cold stores carry the culture through the Long Dark: less decay during dark-age beats."),

        // Tier 4 - the far rim; few reach these.
        new("abyssal_mapping", "Abyssal Mapping", 4, KnowledgeDiscipline.Deep,
            "Reaches the god-depth: unlocks/boosts leviathan and offering-to-the-deep generators."),
        new("reinforced_hulls", "Reinforced Hulls", 4, KnowledgeDiscipline.Craft,
            "Deep-water ships: long voyages - far physical reach, trade, and sea-gambit success."),
        new("storm_prediction", "Storm Prediction", 4, KnowledgeDiscipline.Cold,
            "Weather-wise: fewer catastrophic losses through the Great Freeze and storms."),
    ];
}
