namespace Engine.Processes;

using Engine.Kinds;

/// <summary>
/// Definition-backed index of active process definitions.
/// This is a lookup structure only — behavior semantics live on ProcessDefinition.
/// </summary>
public sealed class BehaviorDefinitionIndex
{
    private readonly Dictionary<EntityKind, List<ProcessDefinition>> _processes = [];

    public void Register(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_processes.TryGetValue(definition.TargetKind, out var list))
        {
            list = [];
            _processes[definition.TargetKind] = list;
        }

        list.Add(definition);
    }

    public IReadOnlyList<ProcessDefinition> GetProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) ? list : [];

    public bool HasProcesses(EntityKind kind) =>
        _processes.TryGetValue(kind, out var list) && list.Count > 0;
}
