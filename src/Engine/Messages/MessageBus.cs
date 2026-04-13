namespace Engine.Messages;

/// <summary>
/// Base type for all simulation messages. Typed pub/sub: subscribers
/// register for specific message types, receive only those.
/// </summary>
public abstract class SimulationMessage
{
    public int Tick { get; }

    protected SimulationMessage(int tick)
    {
        Tick = tick;
    }
}

/// <summary>
/// Typed pub/sub message bus. Systems publish messages after state changes.
/// UI, EventLog, and other systems subscribe to message types they care about.
/// </summary>
public sealed class MessageBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = [];
    private readonly List<Action<SimulationMessage>> _globalSubscribers = [];

    /// <summary>Subscribe to a specific message type.</summary>
    public void Subscribe<T>(Action<T> handler) where T : SimulationMessage
    {
        ArgumentNullException.ThrowIfNull(handler);

        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = [];
            _subscribers[type] = list;
        }

        list.Add(handler);
    }

    /// <summary>Subscribe to ALL messages regardless of type.</summary>
    public void SubscribeAll(Action<SimulationMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _globalSubscribers.Add(handler);
    }

    /// <summary>Publish a message to all matching subscribers. Uses runtime type for dispatch.</summary>
    public void Publish<T>(T message) where T : SimulationMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        // Use runtime type for dispatch (not compile-time T) so that
        // Publish(msg) where msg is typed as SimulationMessage but is actually
        // ProcessExecutedMessage still dispatches to ProcessExecutedMessage subscribers.
        var runtimeType = message.GetType();

        if (_subscribers.TryGetValue(runtimeType, out var list))
        {
            foreach (var handler in list)
            {
                handler.DynamicInvoke(message);
            }
        }

        // Global subscribers
        foreach (var handler in _globalSubscribers)
        {
            handler(message);
        }
    }

    /// <summary>Remove all subscribers (for testing).</summary>
    public void Clear()
    {
        _subscribers.Clear();
        _globalSubscribers.Clear();
    }
}
