namespace Engine.Graph;

using Engine.Kinds;
using Engine.WorldGen;

/// <summary>
/// Spatial query API over the hex grid. Provides entity-location queries,
/// pathfinding, and spatial aggregation used by entity behaviors, authored
/// preconditions, and relationship strength modifiers.
/// </summary>
public sealed class SpatialIndex
{
    private readonly HexGrid _grid;
    private readonly MapCoverageMap _coverageMap;
    private readonly WorldGraph _graph;

    // Entity -> cell mapping (mirrors Entity.Coordinates but indexed for reverse lookup)
    private readonly Dictionary<HexCoord, HashSet<EntityId>> _entitiesAtCell = [];

    public SpatialIndex(HexGrid grid, MapCoverageMap coverageMap, WorldGraph graph)
    {
        _grid = grid;
        _coverageMap = coverageMap;
        _graph = graph;
    }

    public HexGrid Grid => _grid;

    /// <summary>
    /// Register an entity at a cell. Call when entities are created or move.
    /// </summary>
    public void PlaceEntity(EntityId entityId, HexCoord coord)
    {
        // Remove from old position if any
        RemoveEntity(entityId);

        if (!_entitiesAtCell.TryGetValue(coord, out var set))
        {
            set = [];
            _entitiesAtCell[coord] = set;
        }

        set.Add(entityId);
    }

    /// <summary>
    /// Remove an entity from its current cell.
    /// </summary>
    public void RemoveEntity(EntityId entityId)
    {
        var entity = _graph.GetEntity(entityId);
        if (entity is null)
        {
            return;
        }

        var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
        if (_entitiesAtCell.TryGetValue(coord, out var set))
        {
            set.Remove(entityId);
        }
    }

    /// <summary>
    /// Rebuild the spatial index from all entities in the graph.
    /// Call after loading a save or initial world setup.
    /// </summary>
    public void RebuildFromGraph()
    {
        _entitiesAtCell.Clear();

        foreach (var (id, entity) in _graph.AllEntities)
        {
            var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
            if (!_entitiesAtCell.TryGetValue(coord, out var set))
            {
                set = [];
                _entitiesAtCell[coord] = set;
            }

            set.Add(id);
        }
    }

    // --- Cell queries ---

    /// <summary>Get all entities at a specific cell.</summary>
    public IEnumerable<EntityId> EntitiesAtCell(HexCoord coord)
    {
        return _entitiesAtCell.TryGetValue(coord, out var set) ? set : [];
    }

    /// <summary>Get all entities within a hex radius of a center, optionally filtered by kind.</summary>
    public IEnumerable<EntityId> EntitiesInRadius(HexCoord center, int radius, EntityKind? kindFilter = null)
    {
        foreach (var coord in HexCoord.CoordsInRadius(center, radius))
        {
            if (!_entitiesAtCell.TryGetValue(coord, out var set))
            {
                continue;
            }

            foreach (var entityId in set)
            {
                if (kindFilter is null)
                {
                    yield return entityId;
                }
                else
                {
                    var entity = _graph.GetEntity(entityId);
                    if (entity?.Kind == kindFilter)
                    {
                        yield return entityId;
                    }
                }
            }
        }
    }

    /// <summary>Get all entities in a region, optionally filtered by kind.</summary>
    public IEnumerable<EntityId> EntitiesInCoverage(EntityId regionEntityId, EntityKind? kindFilter = null)
    {
        var coverage = _coverageMap.GetCoverage(regionEntityId.Value);
        if (coverage is null)
        {
            yield break;
        }

        foreach (var coord in coverage.Cells)
        {
            if (!_entitiesAtCell.TryGetValue(coord, out var set))
            {
                continue;
            }

            foreach (var entityId in set)
            {
                if (kindFilter is null)
                {
                    yield return entityId;
                }
                else
                {
                    var entity = _graph.GetEntity(entityId);
                    if (entity?.Kind == kindFilter)
                    {
                        yield return entityId;
                    }
                }
            }
        }
    }

    /// <summary>Find the nearest entity to a cell, optionally filtered by kind.</summary>
    public EntityId? NearestEntity(HexCoord from, EntityKind? kindFilter = null, int maxRadius = 20)
    {
        for (var r = 0; r <= maxRadius; r++)
        {
            foreach (var entityId in EntitiesInRadius(from, r, kindFilter))
            {
                var entity = _graph.GetEntity(entityId);
                if (entity?.Coordinates is not null)
                {
                    var coord = new HexCoord(entity.Coordinates.Q, entity.Coordinates.R);
                    if (HexCoord.Distance(from, coord) <= r)
                    {
                        return entityId;
                    }
                }
            }
        }

        return null;
    }

    public HexCoord? FindFreePhysicalHex(HexCoord origin, int maxRadius = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRadius);

        for (var radius = 0; radius <= maxRadius; radius++)
        {
            foreach (var coord in HexCoord.CoordsInRadius(origin, radius))
            {
                if (HexCoord.Distance(origin, coord) != radius)
                {
                    continue;
                }

                var cell = _grid.GetCell(coord);
                if (cell is null || float.IsPositiveInfinity(cell.MovementCost))
                {
                    continue;
                }

                if (!_entitiesAtCell.TryGetValue(coord, out var occupants) || occupants.Count == 0)
                {
                    return coord;
                }
            }
        }

        return null;
    }

    public HexCoord? FindFreePhysicalHexInCoverage(EntityId regionEntityId)
    {
        var coverage = _coverageMap.GetCoverage(regionEntityId.Value);
        if (coverage is null || coverage.Cells.Count == 0)
        {
            return null;
        }

        var regionEntity = _graph.GetEntity(regionEntityId);
        var origin = regionEntity is null
            ? coverage.Cells.First()
            : new HexCoord(regionEntity.Coordinates.Q, regionEntity.Coordinates.R);

        foreach (var coord in coverage.Cells.OrderBy(cell => HexCoord.Distance(origin, cell)))
        {
            var cell = _grid.GetCell(coord);
            if (cell is null || float.IsPositiveInfinity(cell.MovementCost))
            {
                continue;
            }

            if (!_entitiesAtCell.TryGetValue(coord, out var occupants) || occupants.Count == 0)
            {
                return coord;
            }
        }

        return null;
    }

    public HexCoord? FindRandomFreePhysicalHexInCoverage(EntityId regionEntityId, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var coverage = _coverageMap.GetCoverage(regionEntityId.Value);
        if (coverage is null || coverage.Cells.Count == 0)
        {
            return null;
        }

        var candidates = coverage.Cells
            .Where(coord =>
            {
                var cell = _grid.GetCell(coord);
                if (cell is null || float.IsPositiveInfinity(cell.MovementCost))
                {
                    return false;
                }

                return !_entitiesAtCell.TryGetValue(coord, out var occupants) || occupants.Count == 0;
            })
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[random.Next(candidates.Count)];
    }

    // --- Pathfinding ---

    /// <summary>
    /// A* pathfinding on the hex grid. Returns ordered list of cells from start to end,
    /// or null if no path exists. Respects movement costs.
    /// </summary>
    public IReadOnlyList<HexCoord>? FindPath(HexCoord start, HexCoord end)
    {
        if (!_grid.ContainsCoord(start) || !_grid.ContainsCoord(end))
        {
            return null;
        }

        var endCell = _grid.GetCell(end);
        if (endCell is null || float.IsPositiveInfinity(endCell.MovementCost))
        {
            return null;
        }

        var openSet = new PriorityQueue<HexCoord, float>();
        var cameFrom = new Dictionary<HexCoord, HexCoord>();
        var gScore = new Dictionary<HexCoord, float> { [start] = 0f };

        openSet.Enqueue(start, HexCoord.Distance(start, end));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in _grid.GetNeighbors(current))
            {
                if (float.IsPositiveInfinity(neighbor.MovementCost))
                {
                    continue;
                }

                var tentativeG = gScore.GetValueOrDefault(current, float.PositiveInfinity) + neighbor.MovementCost;

                if (tentativeG < gScore.GetValueOrDefault(neighbor.Coord, float.PositiveInfinity))
                {
                    cameFrom[neighbor.Coord] = current;
                    gScore[neighbor.Coord] = tentativeG;
                    var fScore = tentativeG + HexCoord.Distance(neighbor.Coord, end);
                    openSet.Enqueue(neighbor.Coord, fScore);
                }
            }
        }

        return null; // No path found
    }

    private static List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
    {
        var path = new List<HexCoord> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
