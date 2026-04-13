namespace Engine.Behaviors;

#pragma warning disable CA1062 // Parameters validated by EntityEvaluator framework

using Engine.Graph;
using Engine.Messages;

/// <summary>
/// Bridge adapter: wraps an existing IImpulse as an IProcess.
/// Temporary until #37 rewrites all impulses as proper multi-effect processes.
/// </summary>
public sealed class LegacyImpulseProcess : IProcess
{
    private readonly IImpulse _impulse;

    public string Name => _impulse.Name;

    public LegacyImpulseProcess(IImpulse impulse)
    {
        ArgumentNullException.ThrowIfNull(impulse);
        _impulse = impulse;
    }

    public bool CanExecute(Entity entity, IWorldState worldState) => true;

    public float Score(Entity entity, IWorldState worldState) =>
        _impulse.Score(entity, worldState);

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        _impulse.Execute(entity, worldState);
        return new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, _impulse.Name,
            $"{entity.Name} performed {_impulse.Name}");
    }
}
