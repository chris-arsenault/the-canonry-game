namespace Engine.Behaviors;

using Engine.Graph;
using Engine.Kinds;
using Engine.Map;
using Engine.Resources;
using Engine.Wiring;

public interface ICachedWorldState : IWorldState
{
    WorldStateQueryCache QueryCache { get; }
}

public sealed class WorldStateQueryCache(IWorldState worldState)
{
    private readonly IWorldState _worldState = worldState;
    private int _tick = int.MinValue;
    private readonly Dictionary<EntityId, Entity?> _entitiesById = [];
    private IReadOnlyList<Entity>? _activeEntities;
    private readonly Dictionary<EntityKind, IReadOnlyList<EntityId>> _activeEntitiesByKind = [];
    private readonly Dictionary<AttributeRef, IReadOnlyList<EntityId>> _providerCandidates = [];
    private readonly Dictionary<(EntityId SourceId, string? Kind), IReadOnlyList<Relationship>> _outgoingRelationships = [];
    private readonly Dictionary<(EntityId TargetId, string? Kind), IReadOnlyList<Relationship>> _incomingRelationships = [];
    private readonly Dictionary<(EntityId SourceId, string Kind), EntityId?> _relatedEntities = [];
    private readonly Dictionary<(EntityId SourceId, EntityId TargetId, string Kind), Relationship?> _relationships = [];

    public void BeginTick(int tick)
    {
        if (_tick == tick)
        {
            return;
        }

        _tick = tick;
        Clear();
    }

    public void InvalidateEntities()
    {
        _entitiesById.Clear();
        _activeEntities = null;
        _activeEntitiesByKind.Clear();
        _providerCandidates.Clear();
    }

    public void InvalidateResources()
    {
        _providerCandidates.Clear();
    }

    public void InvalidateRelationships()
    {
        _outgoingRelationships.Clear();
        _incomingRelationships.Clear();
        _relatedEntities.Clear();
        _relationships.Clear();
    }

    public Entity? GetEntity(EntityId id)
    {
        EnsureCurrentTick();
        if (_entitiesById.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var entity = _worldState.Graph.GetEntity(id);
        _entitiesById[id] = entity;
        return entity;
    }

    public IReadOnlyList<Entity> ActiveEntities()
    {
        EnsureCurrentTick();
        return _activeEntities ??= _worldState.Graph.AllEntities.Values
            .Where(entity => entity.Status == EntityStatus.Active)
            .ToArray();
    }

    public IReadOnlyList<EntityId> ActiveEntityIdsByKind(EntityKind kind)
    {
        EnsureCurrentTick();
        if (_activeEntitiesByKind.TryGetValue(kind, out var cached))
        {
            return cached;
        }

        var ids = _worldState.Graph.GetEntitiesByKind(kind)
            .Where(id => _worldState.Graph.GetEntity(id)?.Status == EntityStatus.Active)
            .ToArray();
        _activeEntitiesByKind[kind] = ids;
        return ids;
    }

    public IReadOnlyList<EntityId> FindProviderCandidates(AttributeRef selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureCurrentTick();

        if (_providerCandidates.TryGetValue(selector, out var cached))
        {
            return cached;
        }

        var candidates = selector.Scope == RefScope.Resource
            ? FindResourceCandidates(selector.Name)
            : FindStableProviderCandidates(selector);
        _providerCandidates[selector] = candidates;
        return candidates;
    }

    public IReadOnlyList<Relationship> OutgoingRelationships(EntityId sourceId, string? kind = null)
    {
        EnsureCurrentTick();
        var key = (sourceId, kind);
        if (_outgoingRelationships.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var relationships = _worldState.Graph.OutgoingRelationships(sourceId, kind).ToArray();
        _outgoingRelationships[key] = relationships;
        return relationships;
    }

    public IReadOnlyList<Relationship> IncomingRelationships(EntityId targetId, string? kind = null)
    {
        EnsureCurrentTick();
        var key = (targetId, kind);
        if (_incomingRelationships.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var relationships = _worldState.Graph.IncomingRelationships(targetId, kind).ToArray();
        _incomingRelationships[key] = relationships;
        return relationships;
    }

    public EntityId? FindRelatedEntity(EntityId sourceId, string relationshipKind)
    {
        EnsureCurrentTick();
        var key = (sourceId, relationshipKind);
        if (_relatedEntities.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var relationships = OutgoingRelationships(sourceId, relationshipKind);
        var related = relationships.Count == 0 ? (EntityId?)null : relationships[0].TargetId;
        _relatedEntities[key] = related;
        return related;
    }

    public Relationship? FindRelationship(EntityId sourceId, EntityId targetId, string kind)
    {
        EnsureCurrentTick();
        var key = (sourceId, targetId, kind);
        if (_relationships.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var relationship = OutgoingRelationships(sourceId, kind).FirstOrDefault(r => r.TargetId == targetId);
        _relationships[key] = relationship;
        return relationship;
    }

    private void EnsureCurrentTick() => BeginTick(_worldState.CurrentTick);

    private void Clear()
    {
        _entitiesById.Clear();
        _activeEntities = null;
        _activeEntitiesByKind.Clear();
        _providerCandidates.Clear();
        InvalidateRelationships();
    }

    private IReadOnlyList<EntityId> FindStableProviderCandidates(AttributeRef selector)
    {
        if (selector.Scope == RefScope.Structural && selector.Kind.HasValue)
        {
            return ActiveEntityIdsByKind(selector.Kind.Value);
        }

        if (selector.Kind.HasValue)
        {
            var attributeName = selector.Name;
            return ActiveEntityIdsByKind(selector.Kind.Value)
                .Where(id => _worldState.Graph.GetEntity(id)?.Attributes.Has(attributeName) == true)
                .ToArray();
        }

        return [];
    }

    private List<EntityId> FindResourceCandidates(string resourceName)
    {
        var resourceType = resourceName switch
        {
            "food" => ResourceType.Food,
            "wealth" => ResourceType.Wealth,
            "population" => ResourceType.Population,
            _ => (ResourceType?)null,
        };

        if (resourceType is null || _worldState.Resources is null)
        {
            return [];
        }

        var layer = resourceType.Value switch
        {
            ResourceType.Food => _worldState.Resources.Food,
            ResourceType.Wealth => _worldState.Resources.Wealth,
            ResourceType.Population => _worldState.Resources.PopulationLayer,
            _ => null,
        };

        if (layer is null)
        {
            return [];
        }

        var candidates = new List<EntityId>();
        foreach (var entity in ActiveEntities())
        {
            var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
            if (layer.GetStockpile(coord) > 0f)
            {
                candidates.Add(entity.Id);
            }
        }

        return candidates;
    }
}
