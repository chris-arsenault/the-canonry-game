namespace Engine.Kinds;

/// <summary>
/// The fundamental classes of entities in the simulation.
/// Entity kinds are engine-level and hard-coded.
/// Subtypes within each kind are domain-defined.
/// </summary>
public enum EntityKind
{
    // MVP kinds
    Region,
    Settlement,
    Nation,
    Npc,
    Artifact,

    // Post-MVP kinds (reserved, not implemented)
    PointOfInterest,
    Faction,
    Monster,
    Transportation,
    Ability,
    Occurrence,
}

/// <summary>
/// Category-level grouping for relationship constraint validation.
/// Relationships are defined at the category level (4x4), not kind level (11x11).
/// </summary>
public enum EntityCategory
{
    Spatial,
    Agency,
    Asset,
    Record,
}
