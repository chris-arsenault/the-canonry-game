namespace Engine.Behaviors;

using Engine.Kinds;

/// <summary>
/// Registry of processes and decisions per entity kind.
/// Processes are always available. Decisions are card-gated (added/removed
/// when cards enter/leave the tableau).
/// </summary>
public sealed class ActivityRegistry
{
    private readonly Dictionary<EntityKind, List<IProcess>> _processes = [];
    private readonly Dictionary<EntityKind, List<IDecision>> _decisions = [];

    // --- Process registration (permanent, always available) ---

    public void RegisterProcess(EntityKind kind, IProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!_processes.TryGetValue(kind, out var list))
        {
            list = [];
            _processes[kind] = list;
        }

        list.Add(process);
    }

    public IReadOnlyList<IProcess> GetProcesses(EntityKind kind)
    {
        return _processes.TryGetValue(kind, out var list) ? list : [];
    }

    // --- Decision registration (dynamic, card-gated) ---

    public void RegisterDecision(EntityKind kind, IDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (!_decisions.TryGetValue(kind, out var list))
        {
            list = [];
            _decisions[kind] = list;
        }

        list.Add(decision);
    }

    /// <summary>Remove all decisions associated with a specific card.</summary>
    public void UnregisterDecisionsByCard(string cardId)
    {
        foreach (var (_, list) in _decisions)
        {
            list.RemoveAll(d => d.CardId == cardId);
        }
    }

    public IReadOnlyList<IDecision> GetDecisions(EntityKind kind)
    {
        return _decisions.TryGetValue(kind, out var list) ? list : [];
    }

    public bool HasProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) && list.Count > 0;

    public bool HasDecisions(EntityKind kind) =>
        _decisions.TryGetValue(kind, out var list) && list.Count > 0;
}
