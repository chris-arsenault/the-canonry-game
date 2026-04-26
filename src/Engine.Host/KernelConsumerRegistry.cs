namespace Engine.Host;

using Engine.Eventing;
using Engine.Projection;

public sealed class KernelConsumerRegistry : IDisposable
{
    private readonly KernelEventLog _events;
    private readonly List<KernelConsumerRegistration> _registrations = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private bool _sealed;
    private bool _disposed;

    public KernelConsumerRegistry(KernelEventLog events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events;
    }

    public IReadOnlyList<KernelConsumerRegistration> Registrations => _registrations;

    public bool IsSealed => _sealed;

    public KernelConsumerRegistration RegisterProjection(
        string name,
        IKernelEventProjection projection,
        int batchSize = 256) =>
        RegisterProjections(name, [projection], batchSize);

    public KernelConsumerRegistration RegisterProjections(
        string name,
        IEnumerable<IKernelEventProjection> projections,
        int batchSize = 256)
    {
        ThrowIfDisposed();
        if (_sealed)
        {
            throw new InvalidOperationException(
                "Kernel event consumers must be registered before startup seed events are emitted.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        if (!_names.Add(name))
        {
            throw new InvalidOperationException($"Kernel event consumer '{name}' is already registered.");
        }

        var projectionArray = projections.ToArray();
        if (projectionArray.Length == 0)
        {
            throw new ArgumentException("At least one projection is required.", nameof(projections));
        }

        var pump = new KernelEventProjectionPump(
            _events,
            KernelEventCursorStart.EarliestAvailable,
            projectionArray,
            batchSize);
        var registration = new KernelConsumerRegistration(name, pump);
        _registrations.Add(registration);
        return registration;
    }

    public void DrainAll()
    {
        ThrowIfDisposed();
        foreach (var registration in _registrations)
        {
            registration.DrainAvailable();
        }
    }

    public void ThrowIfAnyDropped()
    {
        ThrowIfDisposed();
        foreach (var registration in _registrations)
        {
            if (registration.DroppedEvents != 0)
            {
                throw new InvalidOperationException(
                    $"Kernel event consumer '{registration.Name}' dropped {registration.DroppedEvents} event(s) during bootstrap.");
            }
        }
    }

    internal void Seal()
    {
        ThrowIfDisposed();
        _sealed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}

public sealed class KernelConsumerRegistration : IDisposable
{
    private readonly KernelEventProjectionPump _pump;
    private bool _disposed;

    internal KernelConsumerRegistration(string name, KernelEventProjectionPump pump)
    {
        Name = name;
        _pump = pump;
    }

    public string Name { get; }

    public long NextSequence => _pump.NextSequence;

    public long DroppedEvents => _pump.DroppedEvents;

    public void DrainAvailable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _pump.DrainAvailable();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pump.Dispose();
    }
}
