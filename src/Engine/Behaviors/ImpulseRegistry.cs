namespace Engine.Behaviors;

using Engine.Kinds;

/// <summary>
/// Per-kind registry of available impulses. Each entity kind registers its impulse set at startup.
/// Action cards can register additional impulses dynamically when placed in the tableau.
/// </summary>
public sealed class ImpulseRegistry
{
    private readonly Dictionary<EntityKind, List<IImpulse>> _impulsesByKind = [];

    /// <summary>Register an impulse for a specific entity kind.</summary>
    public void Register(EntityKind kind, IImpulse impulse)
    {
        ArgumentNullException.ThrowIfNull(impulse);

        if (!_impulsesByKind.TryGetValue(kind, out var list))
        {
            list = [];
            _impulsesByKind[kind] = list;
        }

        list.Add(impulse);
    }

    /// <summary>Unregister an impulse by name for a kind. Used when action cards are removed.</summary>
    public void Unregister(EntityKind kind, string impulseName)
    {
        if (_impulsesByKind.TryGetValue(kind, out var list))
        {
            list.RemoveAll(i => i.Name == impulseName);
        }
    }

    /// <summary>Get all registered impulses for a kind.</summary>
    public IReadOnlyList<IImpulse> GetImpulses(EntityKind kind)
    {
        return _impulsesByKind.TryGetValue(kind, out var list) ? list : [];
    }

    /// <summary>Check if a kind has any registered impulses.</summary>
    public bool HasImpulses(EntityKind kind) =>
        _impulsesByKind.TryGetValue(kind, out var list) && list.Count > 0;
}
