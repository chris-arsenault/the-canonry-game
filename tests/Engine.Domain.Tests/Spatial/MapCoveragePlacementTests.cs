namespace Engine.Domain.Tests.Spatial;

using Engine.Domain.Spatial;

public sealed class MapCoveragePlacementTests
{
    [Fact]
    public void CreateCoverage_PrefersPreferredTerrainOverOriginWhenBoundariesAreRelaxed()
    {
        var grid = SpatialTestData.Grid(2);
        var map = new MapCoverageMap();
        var tundraPatch = new HashSet<HexCoord>
        {
            new(1, 0),
            new(1, -1),
            new(2, -1),
            new(2, 0),
        };
        foreach (var coord in tundraPatch)
        {
            grid.SetCell(coord, SpatialTestData.Tundra);
        }
        var blocker = new MapCoverage("Existing", 99);
        map.RegisterCoverage(blocker);
        map.AssignCell(new HexCoord(0, 0), blocker);

        var result = map.CreateCoverage(grid, new MapCoveragePlacementRequest
        {
            Name = "Sanctuary",
            OwnerEntityId = 10,
            Origin = new HexCoord(0, 0),
            Mode = CoveragePlacementMode.CreateNew,
            DesiredCellCount = 3,
            MinimumCellCount = 3,
            AllowedTerrains = new HashSet<string>(StringComparer.Ordinal)
            {
                SpatialTestData.Wastes,
                SpatialTestData.Tundra,
            },
            PreferredTerrains = new HashSet<string>(StringComparer.Ordinal)
            {
                SpatialTestData.Tundra,
            },
            RespectTerrainBoundaries = false,
            TransformTerrain = SpatialTestData.Crags,
        });

        Assert.Contains(result.Anchor, tundraPatch);
        Assert.Equal(3, result.ClaimedCells.Count);
        Assert.All(result.ClaimedCells, coord => Assert.Contains(coord, tundraPatch));
        Assert.All(result.ClaimedCells, coord =>
        {
            Assert.Same(result.Coverage, map.GetCoverageForCell(coord));
            Assert.Equal(SpatialTestData.Crags, grid.GetCell(coord)!.Terrain.StableName);
        });
        Assert.Same(blocker, map.GetCoverageForCell(new HexCoord(0, 0)));
    }

    [Fact]
    public void CreateCoverage_RelaxesTerrainBoundaryWhenLocalBandHasNoEligibleSeed()
    {
        var grid = SpatialTestData.Grid(2);
        var map = new MapCoverageMap();
        var origin = new HexCoord(0, 0);
        var crags = new HexCoord(1, 0);
        grid.SetCell(origin, SpatialTestData.Tundra);
        grid.SetCell(crags, SpatialTestData.Crags);

        var result = map.CreateCoverage(grid, new MapCoveragePlacementRequest
        {
            Name = "Crag Hold",
            OwnerEntityId = 11,
            Origin = origin,
            Mode = CoveragePlacementMode.CreateNew,
            DesiredCellCount = 1,
            MinimumCellCount = 1,
            AllowedTerrains = new HashSet<string>(StringComparer.Ordinal)
            {
                SpatialTestData.Crags,
            },
        });

        Assert.Equal(crags, result.Anchor);
        Assert.Same(result.Coverage, map.GetCoverageForCell(crags));
        Assert.Null(map.GetCoverageForCell(origin));
    }

    [Fact]
    public void CreateCoverage_RollsBackRegistrationWhenMinimumClaimCannotBeMet()
    {
        var grid = SpatialTestData.Grid(1);
        var map = new MapCoverageMap();

        Assert.Throws<InvalidOperationException>(() => map.CreateCoverage(
            grid,
            new MapCoveragePlacementRequest
            {
                Name = "Too Large",
                OwnerEntityId = 12,
                Origin = new HexCoord(0, 0),
                Mode = CoveragePlacementMode.CreateNew,
                DesiredCellCount = 10,
                MinimumCellCount = 10,
            }));

        Assert.Null(map.GetCoverage(12));
        Assert.Null(map.GetCoverageByName("Too Large"));
        Assert.All(grid.AllCells, cell => Assert.Null(map.GetCoverageForCell(cell.Coord)));
    }

    [Fact]
    public void CreateCoverage_SplitsSourceCoverageAndReassignsClaimedCells()
    {
        var (grid, map, source) = CreateFullyClaimedMap(radius: 2);

        var result = map.CreateCoverage(grid, new MapCoveragePlacementRequest
        {
            Name = "Daughter Range",
            OwnerEntityId = 2,
            Origin = new HexCoord(2, 0),
            Mode = CoveragePlacementMode.SplitFromExisting,
            SourceCoverageEntityId = source.OwnerEntityId,
            DesiredCellCount = 3,
            MinimumCellCount = 3,
            MinimumSourceRemainingCellCount = grid.CellCount - 3,
        });

        Assert.Equal(3, result.ClaimedCells.Count);
        Assert.Equal(grid.CellCount - 3, source.CellCount);
        Assert.Equal(3, result.Coverage.CellCount);
        Assert.All(result.ClaimedCells, coord =>
        {
            Assert.DoesNotContain(coord, source.Cells);
            Assert.Same(result.Coverage, map.GetCoverageForCell(coord));
        });
    }

    [Fact]
    public void CreateCoverage_UsesOriginCoverageAsImplicitSplitSource()
    {
        var (grid, map, source) = CreateFullyClaimedMap(radius: 2);
        var origin = new HexCoord(2, 0);

        var result = map.CreateCoverage(grid, new MapCoveragePlacementRequest
        {
            Name = "Implicit Split",
            OwnerEntityId = 3,
            Origin = origin,
            Mode = CoveragePlacementMode.SplitFromExisting,
            DesiredCellCount = 1,
            MinimumCellCount = 1,
            MinimumSourceRemainingCellCount = grid.CellCount - 1,
        });

        Assert.Contains(origin, result.ClaimedCells);
        Assert.Same(result.Coverage, map.GetCoverageForCell(origin));
        Assert.Equal(grid.CellCount - 1, source.CellCount);
    }

    [Fact]
    public void CreateCoverage_RejectsSplitWhenSourceWouldFallBelowMinimum()
    {
        var (grid, map, source) = CreateFullyClaimedMap(radius: 1);

        Assert.Throws<InvalidOperationException>(() => map.CreateCoverage(
            grid,
            new MapCoveragePlacementRequest
            {
                Name = "Over Split",
                OwnerEntityId = 4,
                Origin = new HexCoord(0, 0),
                Mode = CoveragePlacementMode.SplitFromExisting,
                SourceCoverageEntityId = source.OwnerEntityId,
                DesiredCellCount = 1,
                MinimumCellCount = 1,
                MinimumSourceRemainingCellCount = grid.CellCount,
            }));

        Assert.Null(map.GetCoverage(4));
        Assert.Equal(grid.CellCount, source.CellCount);
        Assert.All(grid.AllCells, cell => Assert.Same(source, map.GetCoverageForCell(cell.Coord)));
    }

    [Fact]
    public void CreateCoverage_RollsBackSplitWhenNoSourceCoverageExists()
    {
        var grid = SpatialTestData.Grid(1);
        var map = new MapCoverageMap();

        Assert.Throws<InvalidOperationException>(() => map.CreateCoverage(
            grid,
            new MapCoveragePlacementRequest
            {
                Name = "No Source",
                OwnerEntityId = 5,
                Origin = new HexCoord(0, 0),
                Mode = CoveragePlacementMode.SplitFromExisting,
                DesiredCellCount = 1,
                MinimumCellCount = 1,
            }));

        Assert.Null(map.GetCoverage(5));
        Assert.Null(map.GetCoverageByName("No Source"));
    }

    [Fact]
    public void ComputeAggregates_IgnoresCoverageCellsOutsideGridAndUsesWastesFallback()
    {
        var grid = SpatialTestData.Grid(1);
        var coverage = new MapCoverage("Lost", 6);
        var map = new MapCoverageMap();
        map.RegisterCoverage(coverage);
        map.AssignCell(new HexCoord(99, 99), coverage);

        var aggregates = MapCoverageMap.ComputeAggregates(coverage, grid);

        Assert.Equal(SpatialTestData.Wastes, aggregates.DominantTerrain);
        Assert.Equal(0, aggregates.CellCount);
    }

    [Fact]
    public void RegisterCoverage_RejectsDuplicateOwnerEntityIds()
    {
        var map = new MapCoverageMap();
        map.RegisterCoverage(new MapCoverage("First", 7));

        Assert.Throws<InvalidOperationException>(() =>
            map.RegisterCoverage(new MapCoverage("Second", 7)));
    }

    private static (HexGrid Grid, MapCoverageMap Map, MapCoverage Source) CreateFullyClaimedMap(int radius)
    {
        var grid = SpatialTestData.Grid(radius);
        var map = new MapCoverageMap();
        var source = new MapCoverage("Source", 1);
        map.RegisterCoverage(source);
        foreach (var cell in grid.AllCells)
        {
            map.AssignCell(cell.Coord, source);
        }

        return (grid, map, source);
    }
}
