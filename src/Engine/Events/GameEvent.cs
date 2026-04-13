namespace Engine.Events;

using Engine.Graph;

/// <summary>
/// Categories of game events for filtering.
/// </summary>
public enum EventType
{
    Creation,
    Destruction,
    Relationship,
    Conflict,
    Diplomacy,
    Lifecycle,
    Card,
    Threshold,
    Resource,
}

/// <summary>
/// A single event in the game log. Brief, entity-linked, filterable.
/// </summary>
public sealed class GameEventArgs : EventArgs
{
    public int Tick { get; }
    public EventType Type { get; }
    public string Description { get; }
    public string SourceSystem { get; }

    /// <summary>Entity IDs involved in this event. Clickable in UI.</summary>
    public IReadOnlyList<EntityId> InvolvedEntities { get; }

    public GameEventArgs(
        int tick,
        EventType type,
        string description,
        string sourceSystem,
        IReadOnlyList<EntityId>? involvedEntities = null)
    {
        Tick = tick;
        Type = type;
        Description = description;
        SourceSystem = sourceSystem;
        InvolvedEntities = involvedEntities ?? [];
    }
}
