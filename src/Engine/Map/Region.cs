namespace Engine.Map;

using Engine.Graph;

/// <summary>
/// A geographic region: a named grouping of hex cells. Every cell belongs to exactly one region.
/// Regions are entities in the WorldGraph (EntityKind.Region) and serve as the scoping unit
/// for pressure computation.
/// </summary>
public sealed class Region
{
    public string Name { get; }
    public EntityId EntityId { get; }

    private readonly HashSet<HexCoord> _cells = [];

    public IReadOnlySet<HexCoord> Cells => _cells;
    public int CellCount => _cells.Count;

    public Region(string name, EntityId entityId)
    {
        Name = name;
        EntityId = entityId;
    }

    public void AddCell(HexCoord coord)
    {
        _cells.Add(coord);
    }

    public bool ContainsCell(HexCoord coord) => _cells.Contains(coord);
}

/// <summary>
/// Manages the mapping between hex cells and regions. Ensures every cell belongs to exactly one region.
/// Provides lookup in both directions: cell -> region, region -> cells.
/// Computes region adjacency from cell borders.
/// </summary>
public sealed class RegionMap
{
    private readonly Dictionary<HexCoord, Region> _cellToRegion = [];
    private readonly Dictionary<EntityId, Region> _regions = [];
    private readonly Dictionary<string, Region> _regionsByName = [];

    public IReadOnlyCollection<Region> AllRegions => _regions.Values;

    public void RegisterRegion(Region region)
    {
        ArgumentNullException.ThrowIfNull(region);
        _regions[region.EntityId] = region;
        _regionsByName[region.Name] = region;
    }

    /// <summary>
    /// Assign a cell to a region. A cell can only belong to one region.
    /// Throws if the cell is already assigned to a different region.
    /// </summary>
    public void AssignCell(HexCoord coord, Region region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (_cellToRegion.TryGetValue(coord, out var existing) && existing != region)
        {
            throw new InvalidOperationException(
                $"Cell {coord} is already assigned to region '{existing.Name}', cannot reassign to '{region.Name}'");
        }

        _cellToRegion[coord] = region;
        region.AddCell(coord);
    }

    /// <summary>Get the region containing a cell, or null if unassigned.</summary>
    public Region? GetRegionForCell(HexCoord coord)
    {
        return _cellToRegion.GetValueOrDefault(coord);
    }

    /// <summary>Get a region by its entity ID.</summary>
    public Region? GetRegion(EntityId entityId)
    {
        return _regions.GetValueOrDefault(entityId);
    }

    /// <summary>Get a region by name.</summary>
    public Region? GetRegionByName(string name)
    {
        return _regionsByName.GetValueOrDefault(name);
    }

    /// <summary>
    /// Compute which regions are adjacent (share at least one cell border).
    /// Returns pairs of adjacent region entity IDs.
    /// </summary>
    public IEnumerable<(EntityId A, EntityId B)> ComputeAdjacency()
    {
        var adjacentPairs = new HashSet<(EntityId, EntityId)>();

        foreach (var (coord, region) in _cellToRegion)
        {
            foreach (var neighborCoord in coord.Neighbors())
            {
                if (_cellToRegion.TryGetValue(neighborCoord, out var neighborRegion) &&
                    neighborRegion != region)
                {
                    var pair = region.EntityId.Value < neighborRegion.EntityId.Value
                        ? (region.EntityId, neighborRegion.EntityId)
                        : (neighborRegion.EntityId, region.EntityId);
                    adjacentPairs.Add(pair);
                }
            }
        }

        return adjacentPairs;
    }

    /// <summary>
    /// Compute aggregate attributes for a region from its constituent cells.
    /// </summary>
    public RegionAggregates ComputeAggregates(Region region, HexGrid grid)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(grid);

        var totalFertility = 0f;
        var terrainCounts = new Dictionary<TerrainType, int>();
        var allResources = new HashSet<string>();
        var cellCount = 0;

        foreach (var coord in region.Cells)
        {
            var cell = grid.GetCell(coord);
            if (cell is null)
            {
                continue;
            }

            cellCount++;
            totalFertility += cell.Fertility;

            if (!terrainCounts.TryGetValue(cell.Terrain, out var count))
            {
                count = 0;
            }

            terrainCounts[cell.Terrain] = count + 1;

            foreach (var resource in cell.Resources)
            {
                allResources.Add(resource);
            }
        }

        var avgFertility = cellCount > 0 ? totalFertility / cellCount : 0f;
        var dominantTerrain = terrainCounts.Count > 0
            ? terrainCounts.MaxBy(kv => kv.Value).Key
            : TerrainType.Plains;

        return new RegionAggregates(avgFertility, dominantTerrain, allResources, cellCount);
    }
}

/// <summary>
/// Computed aggregate properties of a region, derived from its constituent cells.
/// </summary>
public sealed class RegionAggregates
{
    public float AverageFertility { get; }
    public TerrainType DominantTerrain { get; }
    public IReadOnlySet<string> Resources { get; }
    public int CellCount { get; }

    public RegionAggregates(
        float averageFertility,
        TerrainType dominantTerrain,
        IReadOnlySet<string> resources,
        int cellCount)
    {
        AverageFertility = averageFertility;
        DominantTerrain = dominantTerrain;
        Resources = resources;
        CellCount = cellCount;
    }
}
