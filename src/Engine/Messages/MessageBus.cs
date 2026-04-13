namespace Engine.Messages;

/// <summary>
/// Base type for all simulation messages.
/// </summary>
public abstract class SimulationMessage(int tick)
{
    public int Tick { get; } = tick;
}

/// <summary>
/// Simple pub/sub message bus. Systems publish messages, subscribers receive all of them.
/// Subscribers pattern-match on message type if they need specific types.
/// </summary>
public sealed class MessageBus
{
    private readonly List<Action<SimulationMessage>> _subscribers = [];

    public void Subscribe(Action<SimulationMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscribers.Add(handler);
    }

    public void Publish(SimulationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        foreach (var handler in _subscribers)
        {
            handler(message);
        }
    }

    public void Clear()
    {
        _subscribers.Clear();
    }
}
