namespace Engine.Behaviors;

using Engine.Graph;

/// <summary>
/// A scoreable behavior that an entity can perform. Impulses are low-altitude,
/// always-available behaviors (move, consume, radiate influence). They are distinct
/// from card-gated Actions which produce significant world-state changes.
/// </summary>
public interface IImpulse
{
    /// <summary>Human-readable name for debugging and event log.</summary>
    string Name { get; }

    /// <summary>
    /// Score this impulse for the given entity. Higher score = more likely to be selected.
    /// Returns 0 if the impulse is not applicable. Score should be in range [0, 1].
    /// </summary>
    float Score(Entity entity, IWorldState worldState);

    /// <summary>
    /// Execute this impulse for the given entity. Modifies world state
    /// (attributes, positions, goal tags, etc.).
    /// </summary>
    void Execute(Entity entity, IWorldState worldState);
}

/// <summary>
/// Read-only view of the world state provided to impulse scoring and execution.
/// Keeps impulses decoupled from specific system implementations.
/// </summary>
public interface IWorldState
{
    WorldGraph Graph { get; }
    Engine.Attributes.AttributeStore Attributes { get; }
    Engine.Attributes.EffectSystem Effects { get; }
    Engine.Map.HexGrid Grid { get; }
    Engine.Map.RegionMap RegionMap { get; }
    Engine.Map.SpatialIndex SpatialIndex { get; }
    int CurrentTick { get; }
}
