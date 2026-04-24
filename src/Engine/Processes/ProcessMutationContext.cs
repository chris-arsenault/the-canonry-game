namespace Engine.Processes;

using Engine.Behaviors;
using Engine.Graph;
using Engine.Messages;

/// <summary>
/// Bridges IWorldState to IMutationContext for data-driven precondition and mutation evaluation.
/// Uses the entity being evaluated as the implicit "self" for attribute reads.
/// </summary>
public sealed class ProcessMutationContext : IMutationContext, IWorldStateAccessor
{
    private readonly Entity _entity;
    private readonly IWorldState _worldState;
    private readonly string? _definitionId;
    private readonly MessageBus? _messageBus;
    private Dictionary<string, float>? _variables;

    public int CurrentTick => _worldState.CurrentTick;
    public EntityId SelfEntityId => _entity.Id;
    public Random Random { get; }
    public EntityId? ResolvedTargetId => Goal?.TargetEntityId;
    public IWorldState WorldState => _worldState;

    /// <summary>The goal this process is executing against. Null when no goal.</summary>
    public EntityGoal? Goal { get; }

    public ProcessMutationContext(Entity entity, IWorldState worldState,
        Random? random = null, EntityGoal? goal = null, string? definitionId = null,
        MessageBus? messageBus = null)
    {
        _entity = entity;
        _worldState = worldState;
        Goal = goal;
        _definitionId = definitionId;
        _messageBus = messageBus;
        Random = random ?? System.Random.Shared;
    }

    public void SetVariable(string name, float value)
    {
        _variables ??= [];
        _variables[name] = value;
    }

    public float GetVariable(string name) =>
        _variables is not null && _variables.TryGetValue(name, out var value) ? value : 0f;

    public float GetPressure(string pressureName, EntityId scopeId) =>
        _worldState.Pressure?.GetValue(pressureName, scopeId) ?? 0f;

    public float GetAttribute(EntityId entityId, string attributeName)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        return entity?.Attributes.GetValue(attributeName) ?? 0f;
    }

    public int CountEntitiesByKind(EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        _worldState.Graph.OutgoingRelationships(sourceId, kind)
            .FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() =>
        _definitionId is not null && _entity.LastDefinitionExecutionTicks.TryGetValue(_definitionId, out var tick)
            ? tick
            : null;

    public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind)
    {
        var rel = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        return rel?.TargetId;
    }

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind);

    public float GetResource((int Q, int R) coordinates, Resources.ResourceType resourceType)
    {
        if (_worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        return resourceType switch
        {
            Resources.ResourceType.Food => _worldState.Resources.Food.GetStockpile(coord),
            Resources.ResourceType.Wealth => _worldState.Resources.Wealth.GetStockpile(coord),
            Resources.ResourceType.Population => _worldState.Resources.PopulationLayer.GetStockpile(coord),
            _ => 0f,
        };
    }

    public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture,
        (int Q, int R)? coordinates)
    {
        var entity = _worldState.Graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);
        if (coordinates is not null)
        {
            _worldState.SpatialIndex.PlaceEntity(entity.Id, new Map.HexCoord(coordinates.Value.Q, coordinates.Value.R));
        }

        return entity;
    }

    public void CreateRelationship(string kind, RelationshipPolarity polarity,
        EntityId sourceId, EntityId targetId, float strength) =>
        _worldState.Graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        var oldValue = entity?.Attributes.GetValue(attributeName);
        entity?.Attributes.Modify(attributeName, delta);
        if (entity is not null && oldValue is { } before)
        {
            var after = entity.Attributes.GetValue(attributeName);
            if (Math.Abs(after - before) > float.Epsilon)
            {
                _messageBus?.Publish(new AttributeChangedMessage(
                    CurrentTick,
                    entityId,
                    attributeName,
                    before,
                    after,
                    _definitionId ?? "process"));
            }
        }
    }

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { } // TODO: wire pressure
    public void ModifyResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float delta)
    {
        if (_worldState.Resources is null) return;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        var layer = resourceType switch
        {
            Resources.ResourceType.Food => _worldState.Resources.Food,
            Resources.ResourceType.Wealth => _worldState.Resources.Wealth,
            Resources.ResourceType.Population => _worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return;
        var before = layer.GetStockpile(coord);
        layer.AddToStockpile(coord, delta);
        var after = layer.GetStockpile(coord);
        if (Math.Abs(after - before) > float.Epsilon)
        {
            _messageBus?.Publish(new ResourceChangedMessage(
                CurrentTick,
                resourceType.ToString(),
                coordinates,
                before,
                after,
                _definitionId ?? "process"));
        }
    }

    public void SetTag(EntityId entityId, string key, string value) { }

    public void SetEntityHistorical(EntityId entityId)
    {
        _worldState.SpatialIndex.RemoveEntity(entityId);
        _worldState.Graph.SetEntityHistorical(entityId, CurrentTick);
    }

    public float ConsumeResource((int Q, int R) coordinates, Resources.ResourceType resourceType, float amount)
    {
        if (_worldState.Resources is null) return 0f;
        var coord = new Map.HexCoord(coordinates.Q, coordinates.R);
        var layer = resourceType switch
        {
            Resources.ResourceType.Food => _worldState.Resources.Food,
            Resources.ResourceType.Wealth => _worldState.Resources.Wealth,
            Resources.ResourceType.Population => _worldState.Resources.PopulationLayer,
            _ => null,
        };
        if (layer is null) return 0f;
        var available = layer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        layer.AddToStockpile(coord, -consumed);
        if (consumed > 0f)
        {
            _messageBus?.Publish(new ResourceChangedMessage(
                CurrentTick,
                resourceType.ToString(),
                coordinates,
                available,
                available - consumed,
                _definitionId ?? "process"));
        }
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        var previous = entity?.Coordinates;
        _worldState.SpatialIndex.RemoveEntity(entityId);
        _worldState.Graph.SetCoordinates(entityId, q, r);
        _worldState.SpatialIndex.PlaceEntity(entityId, new Map.HexCoord(q, r));
        if (previous is { } from && from != (q, r))
        {
            _messageBus?.Publish(new EntityMovedMessage(CurrentTick, entityId, from, (q, r)));
        }
    }

    public void SetSemanticCoordinates(EntityId entityId, int q, int r)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        if (entity is null)
        {
            return;
        }

        var previous = entity.SemanticPosition;
        entity.SemanticPosition = new Engine.Map.SemanticHexCoord(q, r);
        if (previous != entity.SemanticPosition)
        {
            _messageBus?.Publish(new EntitySemanticMovedMessage(
                CurrentTick,
                entityId,
                (previous.Q, previous.R, previous.S),
                (entity.SemanticPosition.Q, entity.SemanticPosition.R, entity.SemanticPosition.S)));
        }
    }

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var rel = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        if (rel is not null)
        {
            _worldState.Graph.CullRelationship(rel, CurrentTick);
        }
    }
}
