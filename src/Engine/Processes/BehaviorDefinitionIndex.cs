namespace Engine.Processes;

using Engine.Kinds;

/// <summary>
/// Definition-backed index of active process and decision definitions.
/// This is a lookup structure only — behavior semantics live on ProcessDefinition.
/// </summary>
public sealed class BehaviorDefinitionIndex
{
    private readonly Dictionary<EntityKind, List<ProcessDefinition>> _processes = [];
    private readonly Dictionary<EntityKind, List<ProcessDefinition>> _decisions = [];
    private readonly Dictionary<EntityKind, Dictionary<string, ProcessDefinition>> _processesById = [];

    public void Register(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var target = definition.Role == DefinitionRole.Process ? _processes : _decisions;
        if (!target.TryGetValue(definition.TargetKind, out var list))
        {
            list = [];
            target[definition.TargetKind] = list;
        }

        list.Add(definition);
        if (definition.Role == DefinitionRole.Process)
        {
            if (!_processesById.TryGetValue(definition.TargetKind, out var byId))
            {
                byId = new Dictionary<string, ProcessDefinition>(StringComparer.Ordinal);
                _processesById[definition.TargetKind] = byId;
            }

            byId[definition.Id] = definition;
        }
    }

    public IReadOnlyList<ProcessDefinition> GetProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) ? list : [];

    public IReadOnlyDictionary<string, ProcessDefinition> GetProcessesById(EntityKind kind) =>
        _processesById.TryGetValue(kind, out var byId) ? byId : EmptyProcessesById;

    public bool TryGetProcess(EntityKind kind, string processId, out ProcessDefinition? process)
    {
        ArgumentNullException.ThrowIfNull(processId);

        if (_processesById.TryGetValue(kind, out var byId))
        {
            return byId.TryGetValue(processId, out process);
        }

        process = null;
        return false;
    }

    public IReadOnlyList<ProcessDefinition> GetDecisions(EntityKind kind) =>
        _decisions.TryGetValue(kind, out var list) ? list : [];

    public void UnregisterDecisionsByCard(string cardId)
    {
        foreach (var (_, list) in _decisions)
        {
            list.RemoveAll(d => string.Equals(d.EnablingCardId, cardId, StringComparison.Ordinal));
        }
    }

    public bool HasProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) && list.Count > 0;

    public bool HasDecisions(EntityKind kind) =>
        _decisions.TryGetValue(kind, out var list) && list.Count > 0;

    private static readonly IReadOnlyDictionary<string, ProcessDefinition> EmptyProcessesById =
        new Dictionary<string, ProcessDefinition>(StringComparer.Ordinal);
}
