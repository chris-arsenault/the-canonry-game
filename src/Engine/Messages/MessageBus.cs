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

    public IDisposable Subscribe(Action<SimulationMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _subscribers.Add(handler);
        return new Subscription(_subscribers, handler);
    }

    public void Publish(SimulationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        for (var i = 0; i < _subscribers.Count; i++)
        {
            _subscribers[i](message);
        }
    }

    public void Clear()
    {
        _subscribers.Clear();
    }

    private sealed class Subscription(List<Action<SimulationMessage>> subscribers, Action<SimulationMessage> handler)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            subscribers.Remove(handler);
        }
    }
}
