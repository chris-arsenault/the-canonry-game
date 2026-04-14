namespace Engine.Tests;

using Engine.Attributes;
using Engine.Behaviors;
using Engine.Graph;
using Engine.Kinds;
using Engine.Processes;
using Engine.Resources;
using Engine.Simulation;
using Engine.Tests.Behaviors;

internal static class TestHelpers
{
    internal static SemanticPlaneDefinition CreateIdeologyPlane() => new(
        "ideology",
        "x",
        "y",
        [
            "low-low", "mid-low", "high-low",
            "low-mid", "mid-mid", "high-mid",
            "low-high", "mid-high", "high-high",
        ]);
}

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
        if (_worldState.Resources is null)
        {
            return 0f;
        }

        var coord = new Engine.Map.HexCoord(coordinates.Q, coordinates.R);
        return resourceType switch
        {
            ResourceType.Sustenance => _worldState.Resources.Sustenance.GetStockpile(coord),
            ResourceType.Gold => _worldState.Resources.Gold.GetStockpile(coord),
            ResourceType.Population => _worldState.Resources.PopulationLayer.GetStockpile(coord),
            _ => 0f,
        };
    }

    public Entity CreateEntity(EntityKind kind, string subtype, string name, string culture, (int Q, int R)? coordinates) =>
        _worldState.Graph.CreateEntity(kind, subtype, name, culture, CurrentTick, coordinates);

    public void CreateRelationship(string kind, RelationshipPolarity polarity, EntityId sourceId, EntityId targetId, float strength) =>
        _worldState.Graph.CreateRelationship(kind, polarity, sourceId, targetId, strength, CurrentTick);

    public void ModifyAttribute(EntityId entityId, string attributeName, float delta) =>
        _worldState.Graph.GetEntity(entityId)?.Attributes.Modify(attributeName, delta);

    public void ModifyPressure(string pressureName, EntityId scopeId, float delta) { }

    public void SetTag(EntityId entityId, string key, string value) { }

    public void SetEntityHistorical(EntityId entityId) =>
        _worldState.Graph.SetEntityHistorical(entityId, CurrentTick);

    public float ConsumeResource((int Q, int R) coordinates, ResourceType resourceType, float amount)
    {
        if (_worldState.Resources is null)
        {
            return 0f;
        }

        var coord = new Engine.Map.HexCoord(coordinates.Q, coordinates.R);
        var layer = resourceType switch
        {
            ResourceType.Sustenance => _worldState.Resources.Sustenance,
            ResourceType.Gold => _worldState.Resources.Gold,
            ResourceType.Population => _worldState.Resources.PopulationLayer,
            _ => null,
        };

        if (layer is null)
        {
            return 0f;
        }

        var available = layer.GetStockpile(coord);
        var consumed = Math.Min(amount, available);
        layer.AddToStockpile(coord, -consumed);
        return consumed;
    }

    public void SetCoordinates(EntityId entityId, int q, int r) =>
        _worldState.Graph.SetCoordinates(entityId, q, r);

    public void CullRelationship(EntityId sourceId, string relationshipKind)
    {
        var relationship = _worldState.Graph.OutgoingRelationships(sourceId, relationshipKind).FirstOrDefault();
        if (relationship is not null)
        {
            _worldState.Graph.CullRelationship(relationship, CurrentTick);
        }
    }
}
