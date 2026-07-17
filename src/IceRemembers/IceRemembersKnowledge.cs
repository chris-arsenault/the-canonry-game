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
    /// <summary>Number of tier bands; tier t spans semantic rings [t*TierWidth, (t+1)*TierWidth).</summary>
    public const int TierWidth = 8;

    public static IReadOnlyList<KnowledgeTech> Techs { get; } =
    [
        // Tier 0 - foundational, everyone learns these near home.
        new("first_remembrance", "The First Remembrance", 0, KnowledgeDiscipline.Remembrance),
        new("long_descent", "The Long Descent", 0, KnowledgeDiscipline.Deep),
        new("working_the_floe", "Working the Floe", 0, KnowledgeDiscipline.Craft),
        new("the_blooding", "The Blooding", 0, KnowledgeDiscipline.Hunt),
        new("the_first_cant", "The First Cant", 0, KnowledgeDiscipline.Song),
        new("reading_the_cold", "Reading the Cold", 0, KnowledgeDiscipline.Cold),

        // Tier 1 - the heart's craft.
        new("lineage_keeping", "Lineage-Keeping", 1, KnowledgeDiscipline.Remembrance),
        new("the_deep_listening", "The Deep Listening", 1, KnowledgeDiscipline.Deep),
        new("bone_and_baleen", "Bone and Baleen", 1, KnowledgeDiscipline.Craft),
        new("the_pod_break", "The Pod-Break", 1, KnowledgeDiscipline.Hunt),
        new("the_carried_name", "The Carried Name", 1, KnowledgeDiscipline.Song),
        new("thaw_sense", "Thaw-Sense", 1, KnowledgeDiscipline.Cold),

        // Tier 2 - reaches the heart's edge.
        new("the_canon_binding", "The Canon-Binding", 2, KnowledgeDiscipline.Remembrance),
        new("descent_rites", "Descent Rites", 2, KnowledgeDiscipline.Deep),
        new("ice_ship_shaping", "Ice-Ship Shaping", 2, KnowledgeDiscipline.Craft),
        new("the_flensing_cant", "The Flensing Cant", 2, KnowledgeDiscipline.Hunt),
        new("chorus_of_the_shelf", "Chorus of the Shelf", 2, KnowledgeDiscipline.Song),
        new("frost_cartography", "Frost Cartography", 2, KnowledgeDiscipline.Cold),

        // Tier 3 - out past the heart, earned by expansion.
        new("the_unforgetting", "The Unforgetting", 3, KnowledgeDiscipline.Remembrance),
        new("what_waits_below", "What Waits Below", 3, KnowledgeDiscipline.Deep),
        new("the_living_hull", "The Living Hull", 3, KnowledgeDiscipline.Craft),
        new("the_deep_war", "The Deep War", 3, KnowledgeDiscipline.Hunt),
        new("the_sung_vigil", "The Sung Vigil", 3, KnowledgeDiscipline.Song),
        new("command_of_the_freeze", "Command of the Freeze", 3, KnowledgeDiscipline.Cold),

        // Tier 4 - the far rim; few reach these.
        new("the_whole_remembrance", "The Whole Remembrance", 4, KnowledgeDiscipline.Remembrance),
        new("the_god_touched_depth", "The God-Touched Depth", 4, KnowledgeDiscipline.Deep),
        new("the_unmelting_work", "The Unmelting Work", 4, KnowledgeDiscipline.Craft),
    ];
}
