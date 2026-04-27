namespace Engine.Domain.Tests;

using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Domain.Tests.Behaviors;
using Engine.WorldGen;

internal sealed class FixedRandom(params double[] values) : Random
{
    private readonly Queue<double> _values = new(values.Length > 0 ? values : [0.5]);

    protected override double Sample()
    {
        var value = _values.Count > 0 ? _values.Dequeue() : 0.5;
        return Math.Clamp(value, 0.0, 1.0);
    }
}

internal sealed class TestMutationContext : IMutationContext, IWorldStateAccessor
{
    private readonly Dictionary<string, float> _variables = [];
    private readonly StubWorldState _worldState;

    public int CurrentTick { get; set; }
    public EntityId SelfEntityId { get; set; }
    public Random Random { get; set; }
    public EntityId? ResolvedTargetId { get; set; }
    public IWorldState WorldState => _worldState;

    public TestMutationContext(
        Entity self,
        Entity? target = null,
        int currentTick = 0,
        Random? random = null,
        StubWorldState? worldState = null)
    {
        _worldState = worldState ?? new StubWorldState(gridRadius: 5, withResources: true);
        if (_worldState.Graph.GetEntity(self.Id) is null)
        {
            throw new ArgumentException("Self entity must belong to the world state's graph.", nameof(self));
        }

        if (target is not null && _worldState.Graph.GetEntity(target.Id) is null)
        {
            throw new ArgumentException("Target entity must belong to the world state's graph.", nameof(target));
        }

        SelfEntityId = self.Id;
        ResolvedTargetId = target?.Id;
        CurrentTick = currentTick;
        Random = random ?? new Random(42);
    }

    public void SetVariable(string name, float value) => _variables[name] = value;

    public float GetVariable(string name) => _variables.GetValueOrDefault(name);

    public float GetPressure(string pressureName, EntityId scopeId) => 0f;

    public float GetAttribute(EntityId entityId, string attributeName) =>
        _worldState.Graph.GetEntity(entityId)?.Attributes.GetValue(attributeName) ?? 0f;

    public int CountEntitiesByKind(EntityKind kind) => _worldState.Graph.GetEntitiesByKind(kind).Count;

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind) =>
        _worldState.Graph.OutgoingRelationships(sourceId, kind).FirstOrDefault(r => r.TargetId == targetId);

    public int? GetLastFiredTick() => null;

    public Entity? GetEntity(EntityId id) => _worldState.Graph.GetEntity(id);

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind) =>
        _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault()?.TargetId;

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind) =>
        _worldState.Graph.GetEntitiesByKind(kind);

    public float GetResource((int Q, int R) coordinates, ResourceType resourceType)
    {
        if (_worldState.StubResources is null)
        {
            return 0f;
        }

        var coord = new HexCoord(coordinates.Q, coordinates.R);
        return _worldState.StubResources.GetStockpile(resourceType, coord);
    }

    public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates)
    {
        var entity = _worldState.Graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);
        if (coordinates is not null)
        {
            _worldState.SpatialIndex.PlaceEntity(entity.Id, new HexCoord(coordinates.Value.Q, coordinates.Value.R));
        }

        return entity;
    }

    public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) =>
        _worldState.Graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
        _worldState.Graph.GetEntity(entityId)?.Attributes.Modify(attributeName, delta);

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }

    public void ModifyResource((int Q, int R) coordinates, ResourceType resourceType, float delta)
    {
        if (_worldState.StubResources is null)
        {
            return;
        }

        var coord = new HexCoord(coordinates.Q, coordinates.R);
        _worldState.StubResources.AddToStockpile(coord, resourceType, delta);
    }

    public void SetEntityHistorical(EntityId entityId)
    {
        _worldState.SpatialIndex.RemoveEntity(entityId);
        _worldState.Graph.SetEntityHistorical(entityId, CurrentTick);
    }

    public float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount)
    {
        if (_worldState.StubResources is null)
        {
            return 0f;
        }

        var coord = new HexCoord(coordinates.Q, coordinates.R);
        var available = _worldState.StubResources.GetStockpile(resourceType, coord);
        var consumed = Math.Min(amount, available);
        _worldState.StubResources.AddToStockpile(coord, resourceType, -consumed);
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r)
    {
        _worldState.SpatialIndex.RemoveEntity(entityId);
        _worldState.Graph.SetCoordinates(entityId, q, r);
        _worldState.SpatialIndex.PlaceEntity(entityId, new HexCoord(q, r));
    }

    public void SetSemanticCoordinates(EntityId entityId, int q, int r)
    {
        var entity = _worldState.Graph.GetEntity(entityId);
        if (entity is not null)
        {
            entity.SemanticPosition = new SemanticHexCoord(q, r);
        }
    }

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var relationship = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        if (relationship is not null)
        {
            _worldState.Graph.CullRelationship(relationship, CurrentTick);
        }
    }
}
