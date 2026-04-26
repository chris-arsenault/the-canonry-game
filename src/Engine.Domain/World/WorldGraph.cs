namespace Engine.Graph;

using Engine.Domain;
using Engine.Kinds;

using QuikGraph;
using QuikGraph.Algorithms.Search;

/// <summary>
/// The single source of truth for world state. Wraps QuikGraph's BidirectionalGraph
/// with entity storage, secondary indexes, and historical relationship support.
/// </summary>
public sealed class WorldGraph
{
    private readonly BidirectionalGraph<EntityId, RelationshipEdge> _graph = new();
    private readonly Dictionary<EntityId, Entity> _entities = [];
    private readonly Dictionary<EntityKind, HashSet<EntityId>> _kindIndex = [];

    // Historical (culled) relationships stored separately so QuikGraph only has active edges
    private readonly List<Relationship> _historicalRelationships = [];

    private int _nextId;

    // Events
    public event EventHandler<EntityCreatedEventArgs>? EntityCreated;
    public event EventHandler<EntityStatusChangedEventArgs>? EntityStatusChanged;
    public event EventHandler<RelationshipCreatedEventArgs>? RelationshipCreated;
    public event EventHandler<RelationshipCulledEventArgs>? RelationshipCulled;
    public event EventHandler<RelationshipStrengthChangedEventArgs>? RelationshipStrengthChanged;

    public WorldGraph()
    {
    }

    // --- Entity CRUD ---

    public Entity CreateEntity(
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        int tick,
        (int Q, int R)? coordinates = null,
        Engine.WorldGen.SemanticHexCoord? semanticPosition = null,
        SubtypeRuntimeId subtypeId = default,
        CultureRuntimeId cultureId = default)
    {
        var id = new EntityId(_nextId++);
        var entity = new Entity(id, kind, subtype, name, culture, tick, coordinates, subtypeId, cultureId);
        if (semanticPosition is { } semanticCoord)
        {
            entity.SemanticPosition = semanticCoord;
        }

        _entities[id] = entity;
        if (!_kindIndex.TryGetValue(kind, out var entitiesByKind))
        {
            entitiesByKind = [];
            _kindIndex[kind] = entitiesByKind;
        }

        entitiesByKind.Add(id);
        _graph.AddVertex(id);

        EntityCreated?.Invoke(this, new EntityCreatedEventArgs(entity));
        return entity;
    }

    public Entity? GetEntity(EntityId id)
    {
        return _entities.GetValueOrDefault(id);
    }

    public IReadOnlyCollection<EntityId> GetEntitiesByKind(EntityKind kind)
    {
        return _kindIndex.TryGetValue(kind, out var entities) ? entities : [];
    }

    public IReadOnlyDictionary<EntityId, Entity> AllEntities => _entities;

    public void SetCoordinates(EntityId id, int q, int r)
    {
        if (!_entities.TryGetValue(id, out var entity))
        {
            throw new ArgumentException($"Entity {id} not found", nameof(id));
        }

        entity.Coordinates = (q, r);
    }

    public void SetEntityHistorical(EntityId id, int tick)
    {
        if (!_entities.TryGetValue(id, out var entity))
        {
            throw new ArgumentException($"Entity {id} not found", nameof(id));
        }

        var previousStatus = entity.Status;
        entity.Status = EntityStatus.Historical;
        entity.UpdatedAt = tick;

        EntityStatusChanged?.Invoke(this, new EntityStatusChangedEventArgs(entity, previousStatus));
    }

    // --- Relationship CRUD ---

    public Relationship CreateRelationship(
        string kind,
        RelationshipPolarity polarity,
        EntityId sourceId,
        EntityId targetId,
        float strength,
        int tick,
        IEnumerable<string>? facts = null,
        RelationshipKindRuntimeId kindId = default,
        RelationshipFactRuntimeId factId0 = default,
        RelationshipFactRuntimeId factId1 = default,
        RelationshipFactRuntimeId factId2 = default,
        RelationshipFactRuntimeId factId3 = default,
        int factCount = 0)
    {
        ValidateEntityExists(sourceId);
        ValidateEntityExists(targetId);

        var relationship = new Relationship(
            kind,
            polarity,
            sourceId,
            targetId,
            strength,
            tick,
            facts,
            kindId,
            factId0,
            factId1,
            factId2,
            factId3,
            factCount);
        var edge = new RelationshipEdge(sourceId, targetId, relationship);
        _graph.AddEdge(edge);

        RelationshipCreated?.Invoke(this, new RelationshipCreatedEventArgs(relationship));
        return relationship;
    }

    public void UpdateRelationshipStrength(Relationship relationship, float newStrength, int tick = 0)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        var previousStrength = relationship.Strength;
        relationship.Strength = Math.Clamp(newStrength, 0f, 1f);

        if (Math.Abs(previousStrength - relationship.Strength) > float.Epsilon)
        {
            RelationshipStrengthChanged?.Invoke(
                this, new RelationshipStrengthChangedEventArgs(relationship, previousStrength, tick));
        }
    }

    public void CullRelationship(Relationship relationship, int tick)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        relationship.EndDate = tick;

        // Move from active graph to historical storage
        var edge = FindEdge(relationship);
        if (edge is not null)
        {
            _graph.RemoveEdge(edge);
        }

        _historicalRelationships.Add(relationship);
        RelationshipCulled?.Invoke(this, new RelationshipCulledEventArgs(relationship, tick));
    }

    // --- Adjacency Queries ---

    public IEnumerable<Relationship> OutgoingRelationships(EntityId entityId, string? kindFilter = null)
    {
        if (!_graph.ContainsVertex(entityId))
        {
            return [];
        }

        var edges = _graph.OutEdges(entityId).Select(e => e.Tag);
        return kindFilter is null ? edges : edges.Where(r => r.Kind == kindFilter);
    }

    public IEnumerable<Relationship> OutgoingRelationships(EntityId entityId, RelationshipKindRuntimeId kindFilter)
    {
        if (!_graph.ContainsVertex(entityId))
        {
            return [];
        }

        return _graph.OutEdges(entityId)
            .Select(e => e.Tag)
            .Where(r => r.KindId == kindFilter);
    }

    public IEnumerable<Relationship> IncomingRelationships(EntityId entityId, string? kindFilter = null)
    {
        if (!_graph.ContainsVertex(entityId))
        {
            return [];
        }

        var edges = _graph.InEdges(entityId).Select(e => e.Tag);
        return kindFilter is null ? edges : edges.Where(r => r.Kind == kindFilter);
    }

    public IEnumerable<Relationship> IncomingRelationships(EntityId entityId, RelationshipKindRuntimeId kindFilter)
    {
        if (!_graph.ContainsVertex(entityId))
        {
            return [];
        }

        return _graph.InEdges(entityId)
            .Select(e => e.Tag)
            .Where(r => r.KindId == kindFilter);
    }

    // --- Historical Queries ---

    /// <summary>
    /// Returns all relationships (active AND historical) between two entities,
    /// optionally filtered by relationship kind.
    /// </summary>
    public IEnumerable<Relationship> AllRelationshipsBetween(
        EntityId a, EntityId b, string? kindFilter = null)
    {
        // Active relationships (check both directions)
        var active = OutgoingRelationships(a)
            .Concat(OutgoingRelationships(b))
            .Where(r => (r.SourceId == a && r.TargetId == b) ||
                        (r.SourceId == b && r.TargetId == a));

        // Historical relationships
        var historical = _historicalRelationships
            .Where(r => (r.SourceId == a && r.TargetId == b) ||
                        (r.SourceId == b && r.TargetId == a));

        var all = active.Concat(historical);
        return kindFilter is null ? all : all.Where(r => r.Kind == kindFilter);
    }

    // --- Graph Traversal ---

    /// <summary>
    /// Returns all entities reachable within N hops from start, using BFS.
    /// Optional edge filter to constrain traversal to specific relationship kinds.
    /// </summary>
    public IEnumerable<Entity> EntitiesWithinHops(
        EntityId start, int maxHops, Func<Relationship, bool>? edgeFilter = null)
    {
        if (!_graph.ContainsVertex(start))
        {
            yield break;
        }

        var visited = new HashSet<EntityId> { start };
        var frontier = new Queue<(EntityId Id, int Depth)>();
        frontier.Enqueue((start, 0));

        while (frontier.Count > 0)
        {
            var (current, depth) = frontier.Dequeue();

            if (depth > 0 && _entities.TryGetValue(current, out var entity))
            {
                yield return entity;
            }

            if (depth >= maxHops)
            {
                continue;
            }

            foreach (var edge in _graph.OutEdges(current))
            {
                if (edgeFilter is not null && !edgeFilter(edge.Tag))
                {
                    continue;
                }

                if (visited.Add(edge.Target))
                {
                    frontier.Enqueue((edge.Target, depth + 1));
                }
            }
        }
    }

    // --- Stats ---

    public int EntityCount => _entities.Count;
    public int ActiveRelationshipCount => _graph.EdgeCount;
    public int HistoricalRelationshipCount => _historicalRelationships.Count;

    // --- Private helpers ---

    private void ValidateEntityExists(EntityId id)
    {
        if (!_entities.ContainsKey(id))
        {
            throw new ArgumentException($"Entity {id} not found", nameof(id));
        }
    }

    private RelationshipEdge? FindEdge(Relationship relationship)
    {
        if (!_graph.TryGetEdges(relationship.SourceId, relationship.TargetId, out var edges))
        {
            return null;
        }

        return edges.FirstOrDefault(e => ReferenceEquals(e.Tag, relationship));
    }
}
