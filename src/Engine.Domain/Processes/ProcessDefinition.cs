namespace Engine.Processes;

using Engine.Kinds;

/// <summary>
/// A low-altitude, always-available entity behavior. Processes are routine
/// upkeep and activity: they produce local conditions rather than major outcomes.
/// </summary>
public sealed class ProcessDefinition : BehaviorDefinition
{
    public ProcessDefinition(
        string id,
        string name,
        EntityKind targetKind,
        IPrecondition precondition,
        IExpression scoreExpression)
        : base(id, name, targetKind, precondition, scoreExpression)
    {
    }
}
