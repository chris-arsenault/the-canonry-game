namespace Engine.Processes;

using Engine.Cards;
using Engine.Kinds;

/// <summary>
/// Definition-backed index of active process and decision definitions.
/// This is a lookup structure only — behavior semantics live on ProcessDefinition.
/// </summary>
public sealed class BehaviorDefinitionIndex
{
    private readonly Dictionary<EntityKind, List<ProcessDefinition>> _processes = [];
    private readonly Dictionary<EntityKind, List<ProcessDefinition>> _decisions = [];
    private readonly Dictionary<EntityKind, Dictionary<ProcessRuntimeId, ProcessDefinition>> _processesByRuntimeId = [];
    private readonly Dictionary<ProcessRuntimeId, ProcessDefinition> _definitionsByRuntimeId = [];

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
        if (_definitionsByRuntimeId.TryGetValue(definition.RuntimeId, out var existing) &&
            !string.Equals(existing.Id, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Process runtime id collision: '{existing.Id}' and '{definition.Id}' both map to {definition.RuntimeId.Value}.");
        }

        _definitionsByRuntimeId[definition.RuntimeId] = definition;

        if (definition.Role == DefinitionRole.Process)
        {
            if (!_processesByRuntimeId.TryGetValue(definition.TargetKind, out var byRuntimeId))
            {
                byRuntimeId = [];
                _processesByRuntimeId[definition.TargetKind] = byRuntimeId;
            }

            byRuntimeId[definition.RuntimeId] = definition;
        }
    }

    public IReadOnlyList<ProcessDefinition> GetProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) ? list : [];

    public bool TryGetProcess(EntityKind kind, ProcessRuntimeId processId, out ProcessDefinition? process)
    {
        if (_processesByRuntimeId.TryGetValue(kind, out var byRuntimeId))
        {
            return byRuntimeId.TryGetValue(processId, out process);
        }

        process = null;
        return false;
    }

    public bool TryGetDefinition(ProcessRuntimeId processId, out ProcessDefinition? process) =>
        _definitionsByRuntimeId.TryGetValue(processId, out process);

    public IReadOnlyList<ProcessDefinition> GetDecisions(EntityKind kind) =>
        _decisions.TryGetValue(kind, out var list) ? list : [];

    public void UnregisterDecisionsByCard(CardRuntimeId cardId)
    {
        foreach (var (_, list) in _decisions)
        {
            list.RemoveAll(d => d.EnablingCardRuntimeId == cardId);
        }
    }

    public bool HasProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) && list.Count > 0;

    public bool HasDecisions(EntityKind kind) =>
        _decisions.TryGetValue(kind, out var list) && list.Count > 0;

}
