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
    private readonly Behaviors.NeedResolver? _needResolver;

    public string Name => _definition.Name;

    public DataDrivenProcess(ProcessDefinition definition, Behaviors.NeedResolver? needResolver = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _needResolver = needResolver;
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

        // Directed processes: resolve target before mutations
        if (_definition.IsDirected && _needResolver is not null)
        {
            ResolveTarget(entity, worldState, context);
        }

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

    private void ResolveTarget(Entity entity, IWorldState worldState, ProcessMutationContext context)
    {
        // Step 1: NeedResolver — what does the entity need?
        var need = _needResolver!.Resolve(entity, worldState.Attributes);
        if (need is null) return;

        // Step 2-4: GraphAttributeQuery — find providers, weight by value * inverse distance
        foreach (var providerRef in need.ProviderRefs)
        {
            var candidates = Behaviors.GraphAttributeQuery.FindProviders(
                providerRef, entity, worldState, topN: 5);

            if (candidates.Count == 0) continue;

            // Step 5: Weighted random pick from top 5
            var targetId = Behaviors.GraphAttributeQuery.WeightedRandomPick(candidates, context.Random);
            if (targetId is null) continue;

            var target = worldState.Graph.GetEntity(targetId.Value);
            if (target?.Coordinates is null) continue;

            // Set resolved target on context for mutations to read
            context.ResolvedTarget = target.Coordinates.Value;
            return;
        }

        // No target found — mutations will see ResolvedTarget = null (wander)
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
internal sealed class ProcessMutationContext : IMutationContext, IWorldStateAccessor
{
    private readonly Entity _entity;
    private readonly IWorldState _worldState;

    public int CurrentTick => _worldState.CurrentTick;
    public EntityId SelfEntityId => _entity.Id;
    public Random Random { get; }
    public (int Q, int R)? ResolvedTarget { get; set; }
    public IWorldState WorldState => _worldState;

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

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
        _worldState.Attributes.Modify(entityId, attributeName, delta);

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
