namespace Engine.Processes;

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
    public ProcessDefinition Definition { get; }

    public string Name => Definition.Name;

    public DataDrivenProcess(ProcessDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
    }

    public bool CanExecute(Entity entity, IWorldState worldState, EntityGoal? goal)
    {
        var context = new ProcessMutationContext(entity, worldState, goal: goal);
        return Definition.Precondition.Evaluate(context).Passed;
    }

    public float Score(Entity entity, IWorldState worldState)
    {
        var exprCtx = new ExpressionContext(entity);
        return Definition.ScoreExpression.Evaluate(exprCtx);
    }

    public SimulationMessage Execute(Entity entity, IWorldState worldState, EntityGoal? goal)
    {
        var context = new ProcessMutationContext(entity, worldState, goal: goal);

        foreach (var mutation in Definition.Mutations)
        {
            mutation.Execute(context);
        }

        var description = Definition.DescriptionTemplate
            .Replace("{name}", entity.Name, StringComparison.Ordinal)
            .Replace("{process}", Definition.Name, StringComparison.Ordinal);

        return new ProcessExecutedMessage(
            worldState.CurrentTick, entity.Id, entity.Kind, Definition.Name, description);
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
        return _definition.Precondition.Evaluate(context).Passed;
    }

    public float Score(Entity entity, IWorldState worldState)
    {
        var exprCtx = new ExpressionContext(entity);
        return _definition.ScoreExpression.Evaluate(exprCtx);
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
public sealed class ProcessMutationContext : IMutationContext, IWorldStateAccessor
{
    private readonly Entity _entity;
    private readonly IWorldState _worldState;
    private Dictionary<string, float>? _variables;

    public int CurrentTick => _worldState.CurrentTick;
    public EntityId SelfEntityId => _entity.Id;
    public Random Random { get; }
    public EntityId? ResolvedTargetId => Goal?.Provider;
    public IWorldState WorldState => _worldState;

    /// <summary>The goal this process is executing against. Null when no goal.</summary>
    public EntityGoal? Goal { get; }

    public void SetVariable(string name, float value)
    {
        _variables ??= [];
        _variables[name] = value;
    }

    public float GetVariable(string name)
    {
        return _variables is not null && _variables.TryGetValue(name, out var value) ? value : 0f;
    }

    public ProcessMutationContext(Entity entity, IWorldState worldState,
        Random? random = null, EntityGoal? goal = null)
    {
        _entity = entity;
        _worldState = worldState;
        Goal = goal;
        Random = random ?? System.Random.Shared;
    }

    public float GetPressure(string pressureName, EntityId scopeId) => 0f; // TODO: wire pressure
    public float GetAttribute(EntityId entityId, string attributeName)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        return entity?.Attributes.GetValue(attributeName) ?? 0f;
    }

    public int CountEntitiesByKind(Kinds.EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        _worldState.Graph.OutgoingRelationships(sourceId, kind)
            .FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() => null;
    public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind)
    {
        var rel = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        return rel?.TargetId;
    }

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(Kinds.EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind);

    public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType)
    {
        if (_worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        return resourceType switch
        {
            Resources.ResourceType.Sustenance => _worldState.Resources.Sustenance.GetStockpile(coord),
            Resources.ResourceType.Gold => _worldState.Resources.Gold.GetStockpile(coord),
            Resources.ResourceType.Population => _worldState.Resources.PopulationLayer.GetStockpile(coord),
            _ => 0f,
        };
    }

    public Entity CreateEntity(Kinds.EntityKind kind, string subtype, string name, string culture,
        (int Q, int R)? coordinates) =>
        _worldState.Graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);

    public void CreateRelationship(string kind, RelationshipPolarity polarity,
        EntityId sourceId, EntityId targetId, float strength) =>
        _worldState.Graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        entity?.Attributes.Modify(attributeName, delta);
    }

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { } // TODO: wire pressure
    public void SetTag(EntityId entityId, string key, string value) { }
    public void SetEntityHistorical(EntityId entityId) =>
        _worldState.Graph.SetEntityHistorical(entityId, CurrentTick);

    public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount)
    {
        if (_worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        // ResourceSystem.ConsumePopulation works on the population layer;
        // for sustenance/gold we consume from stockpile directly
        var layer = resourceType switch
        {
            Resources.ResourceType.Sustenance => _worldState.Resources.Sustenance,
            Resources.ResourceType.Gold => _worldState.Resources.Gold,
            Resources.ResourceType.Population => _worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return 0f;
        var available = layer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        layer.AddToStockpile(coord, -consumed);
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r) =>
        _worldState.Graph.SetCoordinates(entityId, q, r);

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var rel = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        if (rel is not null)
        {
            _worldState.Graph.CullRelationship(rel, CurrentTick);
        }
    }
}
