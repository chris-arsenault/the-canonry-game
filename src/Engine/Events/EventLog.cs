namespace Engine.Events;

using Engine.Graph;

/// <summary>
/// Central event log. All systems emit events here. Provides filtering,
/// entity-linking, and acts as both player UI data source and debugging backbone.
/// Ring buffer: oldest events evicted when capacity is reached.
/// </summary>
public sealed class EventLog
{
    private readonly LinkedList<GameEventArgs> _events = new();

    /// <summary>Maximum number of events retained. Oldest evicted first.</summary>
    public int Capacity { get; }

    /// <summary>Total events ever emitted (including evicted).</summary>
    public int TotalEmitted { get; private set; }

    /// <summary>Current number of events in the log.</summary>
    public int Count => _events.Count;

    /// <summary>Fired when a new event is emitted.</summary>
    public event EventHandler<GameEventArgs>? EventEmitted;

    public EventLog(int capacity = 1000)
    {
        Capacity = capacity;
    }

    /// <summary>
    /// Emit a new event to the log.
    /// </summary>
    public void Emit(GameEventArgs gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);

        _events.AddFirst(gameEvent); // Most recent first
        TotalEmitted++;

        // Evict oldest if over capacity
        while (_events.Count > Capacity)
        {
            _events.RemoveLast();
        }

        EventEmitted?.Invoke(this, gameEvent);
    }

    /// <summary>Convenience: emit with inline construction.</summary>
    public void Emit(int tick, EventType type, string description, string sourceSystem,
        params EntityId[] involvedEntities)
    {
        Emit(new GameEventArgs(tick, type, description, sourceSystem, involvedEntities));
    }

    /// <summary>Get all events in the log (most recent first).</summary>
    public IEnumerable<GameEventArgs> All() => _events;

    /// <summary>Filter events by type.</summary>
    public IEnumerable<GameEventArgs> ByType(EventType type) =>
        _events.Where(e => e.Type == type);

    /// <summary>Filter events involving a specific entity.</summary>
    public IEnumerable<GameEventArgs> ByEntity(EntityId entityId) =>
        _events.Where(e => e.InvolvedEntities.Contains(entityId));

    /// <summary>Filter events within a tick range.</summary>
    public IEnumerable<GameEventArgs> ByTickRange(int fromTick, int toTick) =>
        _events.Where(e => e.Tick >= fromTick && e.Tick <= toTick);

    /// <summary>Get the N most recent events.</summary>
    public IEnumerable<GameEventArgs> Recent(int count) =>
        _events.Take(count);

    /// <summary>Clear all events.</summary>
    public void Clear()
    {
        _events.Clear();
    }
}
