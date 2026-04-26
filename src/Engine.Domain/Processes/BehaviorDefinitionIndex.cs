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
    }

    public IReadOnlyList<ProcessDefinition> GetProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) ? list : [];

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
