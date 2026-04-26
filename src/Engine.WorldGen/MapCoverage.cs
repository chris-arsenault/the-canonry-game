namespace Engine.WorldGen;

/// <summary>
/// A named physical footprint on the map, owned by a Region entity.
/// Coverage is sparse by design: uncovered hexes are wilderness.
/// </summary>
public sealed class MapCoverage
{
    public string Name { get; }
    public int OwnerEntityId { get; }

    private readonly HashSet<HexCoord> _cells = [];

    public IReadOnlySet<HexCoord> Cells => _cells;
    public int CellCount => _cells.Count;

    public MapCoverage(string name, int ownerEntityId)
    {
        Name = name;
        OwnerEntityId = ownerEntityId;
    }

    internal void AddCell(HexCoord coord) => _cells.Add(coord);
    internal void RemoveCell(HexCoord coord) => _cells.Remove(coord);

    public bool ContainsCell(HexCoord coord) => _cells.Contains(coord);
}

public enum CoveragePlacementMode
{
    CreateNew,
    SplitFromExisting,
}

public sealed class MapCoveragePlacementRequest
{
    public required string Name { get; init; }
    public required int OwnerEntityId { get; init; }
    public required HexCoord Origin { get; init; }
    public required CoveragePlacementMode Mode { get; init; }
    public int? SourceCoverageEntityId { get; init; }
    public int DesiredCellCount { get; init; } = 18;
    public int MinimumCellCount { get; init; } = 8;
    public int MinimumSourceRemainingCellCount { get; init; } = 8;
    public IReadOnlySet<string>? PreferredTerrains { get; init; }
    public IReadOnlySet<string>? AllowedTerrains { get; init; }
    public bool RespectTerrainBoundaries { get; init; } = true;
    public string? TransformTerrain { get; init; }
}

public sealed class MapCoveragePlacementResult
{
    public required MapCoverage Coverage { get; init; }
    public required HexCoord Anchor { get; init; }
    public required IReadOnlyList<HexCoord> ClaimedCells { get; init; }
}

/// <summary>
/// Sparse physical coverage index. This replaces the old assumption that all
/// map hexes belong to a region. Uncovered cells are intentional wilderness.
///
/// Extension points:
/// - Terrain grouping comes from the terrain catalog's boundary group values.
/// - Footprint scoring is centralized in <see cref="ScoreCell"/>.
/// - Region creation policies should flow through placement requests rather than
///   bespoke generator code, so later domain-specific coverage rules only need
///   to extend this one seam.
/// </summary>
public sealed class MapCoverageMap
{
    private readonly Dictionary<HexCoord, MapCoverage> _cellToCoverage = [];
    private readonly Dictionary<int, MapCoverage> _coverages = [];
    private readonly Dictionary<string, MapCoverage> _coveragesByName = [];

    public IReadOnlyCollection<MapCoverage> AllCoverages => _coverages.Values;

    public void RegisterCoverage(MapCoverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        if (_coverages.ContainsKey(coverage.OwnerEntityId))
        {
            throw new InvalidOperationException(
                $"Coverage already exists for region entity {coverage.OwnerEntityId}.");
        }

        _coverages[coverage.OwnerEntityId] = coverage;
        _coveragesByName[coverage.Name] = coverage;
    }

    /// <summary>
    /// Low-level authoring/test seam. Claims an uncovered cell for a coverage.
    /// Runtime region generation should prefer <see cref="CreateCoverage"/>.
    /// </summary>
    public void AssignCell(HexCoord coord, MapCoverage coverage)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        if (_cellToCoverage.TryGetValue(coord, out var existing) && existing != coverage)
        {
            throw new InvalidOperationException(
                $"Cell {coord} is already claimed by coverage '{existing.Name}'.");
        }

        _cellToCoverage[coord] = coverage;
        coverage.AddCell(coord);
    }

    public MapCoveragePlacementResult CreateCoverage(HexGrid grid, MapCoveragePlacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(request);

        if (_coverages.ContainsKey(request.OwnerEntityId))
        {
            throw new InvalidOperationException(
                $"Coverage already exists for region entity {request.OwnerEntityId}.");
        }

        var coverage = new MapCoverage(request.Name, request.OwnerEntityId);
        RegisterCoverage(coverage);

        try
        {
            var result = request.Mode switch
            {
                CoveragePlacementMode.CreateNew => CreateNewCoverage(grid, coverage, request),
                CoveragePlacementMode.SplitFromExisting => SplitCoverage(grid, coverage, request),
                _ => throw new InvalidOperationException($"Unhandled coverage placement mode '{request.Mode}'."),
            };

            if (request.TransformTerrain is { } transformedTerrain)
            {
                foreach (var cellCoord in result.ClaimedCells)
                {
                    var cell = grid.GetCell(cellCoord);
                    if (cell is null)
                    {
                        continue;
                    }

                    // Extension point: today transforms only rewrite terrain class.
                    // If region creation later needs richer effects (elevation, ambient
                    // resources, hazards), extend this seam rather than special-casing
                    // generators.
                    grid.SetCell(cellCoord, transformedTerrain, cell.Elevation, cell.Fertility);
                }
            }

            return result;
        }
        catch
        {
            _coverages.Remove(request.OwnerEntityId);
            _coveragesByName.Remove(request.Name);
            throw;
        }
    }

    public MapCoverage? GetCoverageForCell(HexCoord coord) =>
        _cellToCoverage.GetValueOrDefault(coord);

    public MapCoverage? GetCoverage(int entityId) =>
        _coverages.GetValueOrDefault(entityId);

    public MapCoverage? GetCoverageByName(string name) =>
        _coveragesByName.GetValueOrDefault(name);

    public IEnumerable<(int A, int B)> ComputeAdjacency()
    {
        var adjacentPairs = new HashSet<(int, int)>();

        foreach (var (coord, coverage) in _cellToCoverage)
        {
            foreach (var neighborCoord in coord.Neighbors())
            {
                if (_cellToCoverage.TryGetValue(neighborCoord, out var neighborCoverage) &&
                    neighborCoverage != coverage)
                {
                    var pair = coverage.OwnerEntityId < neighborCoverage.OwnerEntityId
                        ? (coverage.OwnerEntityId, neighborCoverage.OwnerEntityId)
                        : (neighborCoverage.OwnerEntityId, coverage.OwnerEntityId);
                    adjacentPairs.Add(pair);
                }
            }
        }

        return adjacentPairs;
    }

    public CoverageAggregates ComputeAggregates(MapCoverage coverage, HexGrid grid)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(grid);

        var totalFertility = 0f;
        var terrainCounts = new Dictionary<string, int>();
        var allResources = new HashSet<string>();
        var cellCount = 0;

        foreach (var coord in coverage.Cells)
        {
            var cell = grid.GetCell(coord);
            if (cell is null)
            {
                continue;
            }

            cellCount++;
            totalFertility += cell.Fertility;

            terrainCounts[cell.Terrain.StableName] =
                terrainCounts.GetValueOrDefault(cell.Terrain.StableName) + 1;

            foreach (var resource in cell.Resources)
            {
                allResources.Add(resource);
            }
        }

        var avgFertility = cellCount > 0 ? totalFertility / cellCount : 0f;
        var dominantTerrain = terrainCounts.Count > 0
            ? terrainCounts.MaxBy(kv => kv.Value).Key
            : "wastes";

        return new CoverageAggregates(avgFertility, dominantTerrain, allResources, cellCount);
    }

    private MapCoveragePlacementResult CreateNewCoverage(
        HexGrid grid,
        MapCoverage coverage,
        MapCoveragePlacementRequest request)
    {
        var anchor = FindNearestEligibleSeedCell(grid, request.Origin, request, sourceCoverage: null)
            ?? throw new InvalidOperationException(
                $"No uncovered terrain near {request.Origin} can seed coverage '{request.Name}'.");

        var claimed = BuildPatch(grid, anchor, request, sourceCoverage: null);
        if (claimed.Count < request.MinimumCellCount)
        {
            throw new InvalidOperationException(
                $"Coverage '{request.Name}' could only claim {claimed.Count} cells, below minimum {request.MinimumCellCount}.");
        }

        foreach (var coord in claimed)
        {
            AssignCell(coord, coverage);
        }

        return new MapCoveragePlacementResult
        {
            Coverage = coverage,
            Anchor = anchor,
            ClaimedCells = claimed,
        };
    }

    private MapCoveragePlacementResult SplitCoverage(
        HexGrid grid,
        MapCoverage coverage,
        MapCoveragePlacementRequest request)
    {
        var sourceCoverage = ResolveSourceCoverage(request)
            ?? throw new InvalidOperationException(
                $"Coverage '{request.Name}' requested SplitFromExisting but no source coverage was found.");

        var anchor = FindNearestEligibleSeedCell(grid, request.Origin, request, sourceCoverage)
            ?? throw new InvalidOperationException(
                $"Coverage '{request.Name}' could not find an eligible split seed near {request.Origin}.");

        var claimed = BuildPatch(grid, anchor, request, sourceCoverage);
        if (claimed.Count < request.MinimumCellCount)
        {
            throw new InvalidOperationException(
                $"Coverage '{request.Name}' could only split {claimed.Count} cells, below minimum {request.MinimumCellCount}.");
        }

        foreach (var coord in claimed)
        {
            ReassignCell(coord, sourceCoverage, coverage);
        }

        return new MapCoveragePlacementResult
        {
            Coverage = coverage,
            Anchor = anchor,
            ClaimedCells = claimed,
        };
    }

    private MapCoverage? ResolveSourceCoverage(MapCoveragePlacementRequest request)
    {
        if (request.SourceCoverageEntityId is { } sourceId)
        {
            return GetCoverage(sourceId);
        }

        return GetCoverageForCell(request.Origin);
    }

    private HexCoord? FindNearestEligibleSeedCell(
        HexGrid grid,
        HexCoord origin,
        MapCoveragePlacementRequest request,
        MapCoverage? sourceCoverage)
    {
        var originCell = grid.GetCell(origin);
        var boundaryGroup = originCell is null ? null : originCell.Terrain.BoundaryGroup;

        var strict = SearchEligibleSeedCell(grid, origin, request, sourceCoverage, boundaryGroup);
        if (strict is not null || !request.RespectTerrainBoundaries)
        {
            return strict;
        }

        // If the local terrain band is exhausted, relax the boundary requirement and
        // keep looking. This preserves terrain-aware seeding as the default without
        // making region creation impossible once nearby wilderness has been claimed.
        return SearchEligibleSeedCell(grid, origin, request, sourceCoverage, boundaryGroup: null);
    }

    private HexCoord? SearchEligibleSeedCell(
        HexGrid grid,
        HexCoord origin,
        MapCoveragePlacementRequest request,
        MapCoverage? sourceCoverage,
        string? boundaryGroup)
    {
        for (var radius = 0; radius <= grid.Radius; radius++)
        {
            var candidates = HexCoord.CoordsInRadius(origin, radius)
                .Where(coord => HexCoord.Distance(origin, coord) == radius)
                .Where(coord => IsEligibleSeedCell(grid, coord, request, sourceCoverage, boundaryGroup))
                .OrderBy(coord => ScoreCell(grid, coord, origin, request, boundaryGroup))
                .ToList();

            if (candidates.Count > 0)
            {
                return candidates[0];
            }
        }

        return null;
    }

    private bool IsEligibleSeedCell(
        HexGrid grid,
        HexCoord coord,
        MapCoveragePlacementRequest request,
        MapCoverage? sourceCoverage,
        string? boundaryGroup)
    {
        var cell = grid.GetCell(coord);
        if (cell is null)
        {
            return false;
        }

        if (request.Mode == CoveragePlacementMode.CreateNew)
        {
            if (_cellToCoverage.ContainsKey(coord))
            {
                return false;
            }
        }
        else
        {
            if (sourceCoverage is null || !sourceCoverage.ContainsCell(coord))
            {
                return false;
            }
        }

        if (!TerrainAllowed(cell.Terrain.StableName, request))
        {
            return false;
        }

        if (!request.RespectTerrainBoundaries || boundaryGroup is null)
        {
            return true;
        }

        return string.Equals(cell.Terrain.BoundaryGroup, boundaryGroup, StringComparison.Ordinal);
    }

    private List<HexCoord> BuildPatch(
        HexGrid grid,
        HexCoord anchor,
        MapCoveragePlacementRequest request,
        MapCoverage? sourceCoverage)
    {
        var claimed = new List<HexCoord>();
        var claimedSet = new HashSet<HexCoord>();
        var frontier = new List<HexCoord> { anchor };
        var visited = new HashSet<HexCoord>();
        var boundaryGroup = grid.GetCell(anchor)!.Terrain.BoundaryGroup;

        while (frontier.Count > 0 && claimed.Count < request.DesiredCellCount)
        {
            frontier.Sort((a, b) =>
                ScoreCell(grid, a, anchor, request, boundaryGroup)
                .CompareTo(ScoreCell(grid, b, anchor, request, boundaryGroup)));

            var current = frontier[0];
            frontier.RemoveAt(0);
            if (!visited.Add(current))
            {
                continue;
            }

            if (!CanClaimCell(grid, current, request, sourceCoverage, claimedSet, boundaryGroup))
            {
                continue;
            }

            claimed.Add(current);
            claimedSet.Add(current);

            foreach (var neighbor in current.Neighbors())
            {
                if (!visited.Contains(neighbor) && grid.ContainsCoord(neighbor))
                {
                    frontier.Add(neighbor);
                }
            }
        }

        return claimed;
    }

    private bool CanClaimCell(
        HexGrid grid,
        HexCoord coord,
        MapCoveragePlacementRequest request,
        MapCoverage? sourceCoverage,
        HashSet<HexCoord> alreadyClaimed,
        string? boundaryGroup)
    {
        if (!IsEligibleSeedCell(grid, coord, request, sourceCoverage, boundaryGroup))
        {
            return false;
        }

        if (request.Mode != CoveragePlacementMode.SplitFromExisting || sourceCoverage is null)
        {
            return true;
        }

        var removed = new HashSet<HexCoord>(alreadyClaimed) { coord };
        var remainingCount = sourceCoverage.CellCount - removed.Count;
        if (remainingCount < request.MinimumSourceRemainingCellCount)
        {
            return false;
        }

        return RemainingCoverageIsContiguous(sourceCoverage, removed);
    }

    private void ReassignCell(HexCoord coord, MapCoverage fromCoverage, MapCoverage toCoverage)
    {
        if (!_cellToCoverage.TryGetValue(coord, out var existing) || existing != fromCoverage)
        {
            throw new InvalidOperationException(
                $"Cell {coord} is not owned by source coverage '{fromCoverage.Name}'.");
        }

        fromCoverage.RemoveCell(coord);
        _cellToCoverage[coord] = toCoverage;
        toCoverage.AddCell(coord);
    }

    private static bool RemainingCoverageIsContiguous(MapCoverage coverage, HashSet<HexCoord> removed)
    {
        var remaining = coverage.Cells.Where(coord => !removed.Contains(coord)).ToList();
        if (remaining.Count == 0)
        {
            return false;
        }

        var visited = new HashSet<HexCoord>();
        var queue = new Queue<HexCoord>();
        queue.Enqueue(remaining[0]);
        visited.Add(remaining[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in current.Neighbors())
            {
                if (removed.Contains(neighbor) || !coverage.ContainsCell(neighbor) || !visited.Add(neighbor))
                {
                    continue;
                }

                queue.Enqueue(neighbor);
            }
        }

        return visited.Count == remaining.Count;
    }

    private static bool TerrainAllowed(string terrain, MapCoveragePlacementRequest request)
    {
        return request.AllowedTerrains is null || request.AllowedTerrains.Contains(terrain);
    }

    private static int ScoreCell(
        HexGrid grid,
        HexCoord coord,
        HexCoord origin,
        MapCoveragePlacementRequest request,
        string? boundaryGroup)
    {
        var cell = grid.GetCell(coord);
        if (cell is null)
        {
            return int.MaxValue;
        }
        var score = HexCoord.Distance(origin, coord) * 10;

        if (request.PreferredTerrains is { Count: > 0 } &&
            !request.PreferredTerrains.Contains(cell.Terrain.StableName))
        {
            score += 20;
        }

        if (request.RespectTerrainBoundaries &&
            boundaryGroup is not null &&
            !string.Equals(cell.Terrain.BoundaryGroup, boundaryGroup, StringComparison.Ordinal))
        {
            score += 100;
        }

        return score;
    }

}

public sealed class CoverageAggregates
{
    public float AverageFertility { get; }
    public string DominantTerrain { get; }
    public IReadOnlySet<string> Resources { get; }
    public int CellCount { get; }

    public CoverageAggregates(
        float averageFertility,
        string dominantTerrain,
        IReadOnlySet<string> resources,
        int cellCount)
    {
        AverageFertility = averageFertility;
        DominantTerrain = dominantTerrain;
        Resources = resources;
        CellCount = cellCount;
    }
}
