namespace Engine.Cards;

#pragma warning disable CA1062 // Parameters validated by EntityEvaluator

using Engine.Behaviors;
using Engine.Graph;
using Engine.Messages;

/// <summary>
/// Interprets a ProcessDefinition as an IProcess. The engine evaluates data;
/// no C# process classes needed. One data language, three uses.
/// </summary>
public sealed class DataDrivenProcess : IProcess
{
    private readonly ProcessDefinition _definition;

    public string Name => _definition.Name;

    public DataDrivenProcess(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
    }

    public bool CanExecute(Entity entity, IWorldState worldState)
    {
        // Create a minimal card context for precondition evaluation
        var context = new ProcessMutationContext(entity, worldState);
        return _definition.Precondition.Evaluate(context);
    }

    public float Score(Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState);
        return _definition.ScoreExpression.Evaluate(entity, context);
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState);

        foreach (var mutation in _definition.Mutations)
        {
            mutation.Execute(context);
        }

        var description = _definition.DescriptionTemplate
            .Replace("{name}", entity.Name, StringComparison.Ordinal)
            .Replace("{process}", _definition.Name, StringComparison.Ordinal);

        return new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, _definition.Name, description);
    }
}

/// <summary>
/// Interprets a ProcessDefinition with Role=Decision as an IDecision.
/// </summary>
public sealed class DataDrivenDecision : IDecision
{
    private readonly ProcessDefinition _definition;

    public string Name => _definition.Name;
    public string? CardId => _definition.EnablingCardId;

    public DataDrivenDecision(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
    }

    public bool CanExecute(Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState);
        return _definition.Precondition.Evaluate(context);
    }

    public float Score(Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState);
        return _definition.ScoreExpression.Evaluate(entity, context);
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState)
    {
        var context = new ProcessMutationContext(entity, worldState);

        foreach (var mutation in _definition.Mutations)
        {
            mutation.Execute(context);
        }

        var description = _definition.DescriptionTemplate
            .Replace("{name}", entity.Name, StringComparison.Ordinal)
            .Replace("{process}", _definition.Name, StringComparison.Ordinal);

        return new DecisionExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, _definition.Name,
            description, _definition.EnablingCardId);
    }
}

/// <summary>
/// Bridges IWorldState to IMutationContext for data-driven process precondition/mutation evaluation.
/// Uses the entity being evaluated as the implicit "self" for attribute reads.
/// </summary>
internal sealed class ProcessMutationContext : IMutationContext
{
    private readonly Entity _entity;
    private readonly IWorldState _worldState;

    public int CurrentTick => _worldState.CurrentTick;
    public EntityId SelfEntityId => _entity.Id;
    public Random Random { get; }

    public ProcessMutationContext(Entity entity, IWorldState worldState, Random? random = null)
    {
        _entity = entity;
        _worldState = worldState;
        Random = random ?? System.Random.Shared;
    }

    public float GetPressure(string pressureName, EntityId scopeId) => 0f; // TODO: wire pressure
    public float GetAttribute(EntityId entityId, string attributeName) =>
        _worldState.Attributes.Get(entityId, attributeName);

    public int CountEntitiesByKind(Kinds.EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        _worldState.Graph.OutgoingRelationships(sourceId, kind)
            .FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() => null;
    public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);

    public Entity CreateEntity(Kinds.EntityKind kind, string subtype, string name, string culture,
        (int Q, int R)? coordinates) =>
        _worldState.Graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);

    public void CreateRelationship(string kind, RelationshipPolarity polarity,
        EntityId sourceId, EntityId targetId, float strength) =>
        _worldState.Graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
        _worldState.Attributes.Modify(entityId, attributeName, delta);

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { } // TODO: wire pressure
    public void SetTag(EntityId entityId, string key, string value) { }
    public void SetEntityHistorical(EntityId entityId) =>
        _worldState.Graph.SetEntityHistorical(entityId, CurrentTick);
}
