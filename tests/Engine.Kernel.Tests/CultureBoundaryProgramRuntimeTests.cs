namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureBoundaryProgramRuntimeTests
{
    private const int CultureId = 1;
    private const int OtherCultureId = 2;

    [Fact]
    public void ComponentConstruction_TracksClaimedComponentsAndLargestComponent()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 1, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 5, 0, WorldState.CultureClaimedRawMass);

        var origin = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        var neighbor = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 1, 0);
        var remote = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 5, 0);

        Assert.Equal(2, simulation.CultureBoundaries.ComponentCount(simulation.World, CultureId, PlaneCodes.Physical));
        Assert.Equal(origin.ComponentId, neighbor.ComponentId);
        Assert.NotEqual(origin.ComponentId, remote.ComponentId);
        Assert.True(origin.IsLargestComponent);
        Assert.Equal(2, origin.ComponentCellCount);
        Assert.Equal(200f, origin.ComponentRawMass);
        Assert.Equal(1, remote.ComponentCellCount);
    }

    [Fact]
    public void CellAt_ClassifiesInteriorContestedAndIsolatedCells()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureControlledRawMass);
        foreach (var (q, r) in NeighborCells(0, 0))
        {
            AddCulture(simulation, CultureId, q, r, WorldState.CultureControlledRawMass);
        }

        AddCulture(simulation, CultureId, 6, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, OtherCultureId, 6, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 12, 0, WorldState.CultureClaimedRawMass);

        var interior = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        var contested = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 6, 0);
        var isolated = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 12, 0);

        Assert.Equal(CultureFieldCellKindCodes.Interior, interior.CellKindCode);
        Assert.Equal(CultureFieldStrengthBandCodes.Controlled, interior.StrengthBandCode);
        Assert.Equal(CultureFieldCellKindCodes.Contested, contested.CellKindCode);
        Assert.Equal(OtherCultureId, contested.ForeignCultureId);
        Assert.Equal(50f, contested.Share, precision: 3);
        Assert.Equal(CultureFieldCellKindCodes.Isolated, isolated.CellKindCode);
    }

    [Fact]
    public void CellAt_DerivesInteriorHoleFromAdjacentSingleComponent()
    {
        var simulation = CreateSimulation();
        foreach (var (q, r) in NeighborCells(0, 0))
        {
            AddCulture(simulation, CultureId, q, r, WorldState.CultureControlledRawMass);
        }

        var hole = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);

        Assert.Equal(0f, hole.RawMass);
        Assert.Equal(CultureFieldCellKindCodes.Border, hole.CellKindCode);
        Assert.True(hole.IsInteriorHole);
        Assert.True(simulation.CultureBoundaries.IsInteriorHole(simulation.World, CultureId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(1, hole.AttachedComponentCount);
        Assert.Equal(6, hole.SupportedAdjacentCount);
        Assert.Equal(6, hole.AttachedComponentCellCount);
    }

    [Fact]
    public void CellAt_DoesNotTreatGapBetweenTwoComponentsAsInteriorHole()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, -1, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 1, 0, WorldState.CultureControlledRawMass);

        var gap = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);

        Assert.False(gap.IsInteriorHole);
        Assert.Equal(2, gap.AttachedComponentCount);
    }

    [Fact]
    public void QueryCache_ReusesBoundaryVersionAndIgnoresUnrelatedWorldVersionChanges()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 2, 0, WorldState.CultureControlledRawMass);

        Assert.Equal(0, simulation.CultureBoundaries.RebuildCount);
        Assert.Equal(2, simulation.CultureBoundaries.ComponentCount(simulation.World, CultureId, PlaneCodes.Physical));
        var firstRebuildCount = simulation.CultureBoundaries.RebuildCount;
        _ = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 2, 0);

        Assert.Equal(firstRebuildCount, simulation.CultureBoundaries.RebuildCount);

        simulation.World.AdvanceWorldVersion();
        _ = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 2, 0);

        Assert.Equal(firstRebuildCount, simulation.CultureBoundaries.RebuildCount);

        AddCulture(simulation, CultureId, 1, 0, WorldState.CultureControlledRawMass);

        Assert.Equal(1, simulation.CultureBoundaries.ComponentCount(simulation.World, CultureId, PlaneCodes.Physical));
        Assert.True(simulation.CultureBoundaries.RebuildCount > firstRebuildCount);
    }

    [Fact]
    public void QueryCache_UpdatesMassWithoutRebuildWhenSupportTopologyDoesNotChange()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 1, 0, WorldState.CultureControlledRawMass);

        var initial = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        var firstRebuildCount = simulation.CultureBoundaries.RebuildCount;

        AddCulture(simulation, CultureId, 1, 0, 10f);

        var updated = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);

        Assert.Equal(firstRebuildCount, simulation.CultureBoundaries.RebuildCount);
        Assert.Equal(210f, updated.ComponentRawMass);
        Assert.Equal(initial.ComponentId, updated.ComponentId);
    }

    [Fact]
    public void SnapshotRestore_RebuildsBoundaryCacheFromCultureInfluence()
    {
        var simulation = CreateSimulation();
        foreach (var (q, r) in NeighborCells(0, 0))
        {
            AddCulture(simulation, CultureId, q, r, WorldState.CultureControlledRawMass);
        }

        _ = simulation.CultureBoundaries.CellAt(simulation.World, CultureId, PlaneCodes.Physical, 0, 0);
        using var stream = new MemoryStream();
        KernelSnapshot.SaveKernelState(simulation, stream);
        stream.Position = 0;

        var loaded = KernelSnapshot.LoadKernelState(stream);
        var loadedHole = loaded.CultureBoundaries.CellAt(loaded.World, CultureId, PlaneCodes.Physical, 0, 0);

        Assert.True(loadedHole.IsInteriorHole);
        Assert.Equal(1, loaded.CultureBoundaries.RebuildCount);
    }

    [Fact]
    public void DrainDirtyCells_SupportRemovalRefreshesSplitComponents()
    {
        var simulation = CreateSimulation();
        CreateKnownMap(simulation, radius: 6);
        for (var q = 0; q <= 6; q++)
        {
            AddCulture(simulation, CultureId, q, 0, WorldState.CultureClaimedRawMass);
        }

        DrainDirtyCells(simulation);

        AddCulture(simulation, CultureId, 3, 0, -WorldState.CultureClaimedRawMass);
        var dirty = DrainDirtyCells(simulation);

        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 0, 0), dirty);
        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 6, 0), dirty);
    }

    [Fact]
    public void DrainDirtyCells_SupportAdditionRefreshesMergedComponent()
    {
        var simulation = CreateSimulation();
        CreateKnownMap(simulation, radius: 6);
        for (var q = 0; q <= 2; q++)
        {
            AddCulture(simulation, CultureId, q, 0, WorldState.CultureClaimedRawMass);
        }

        for (var q = 4; q <= 6; q++)
        {
            AddCulture(simulation, CultureId, q, 0, WorldState.CultureClaimedRawMass);
        }

        DrainDirtyCells(simulation);

        AddCulture(simulation, CultureId, 3, 0, WorldState.CultureClaimedRawMass);
        var dirty = DrainDirtyCells(simulation);

        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 0, 0), dirty);
        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 6, 0), dirty);
    }

    [Fact]
    public void DrainDirtyCells_NonTopologyChangesStayLocal()
    {
        var simulation = CreateSimulation();
        CreateKnownMap(simulation, radius: 6);
        for (var q = 0; q <= 6; q++)
        {
            AddCulture(simulation, CultureId, q, 0, WorldState.CultureClaimedRawMass);
        }

        DrainDirtyCells(simulation);

        AddCulture(simulation, CultureId, 3, 0, 1f);
        var dirty = DrainDirtyCells(simulation);

        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 1, 0), dirty);
        Assert.Contains(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 5, 0), dirty);
        Assert.DoesNotContain(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 0, 0), dirty);
        Assert.DoesNotContain(new CultureBoundaryDirtyCell(PlaneCodes.Physical, 6, 0), dirty);
    }

    private static Simulation CreateSimulation() =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            MapCellCapacity = 128,
            MapInfluenceCapacity = 128,
            EventCapacity = 512,
            SystemCapacity = 32,
        });

    private static void AddCulture(Simulation simulation, int cultureId, int q, int r, float raw)
    {
        simulation.ModifyCellCultureInfluenceRaw(cultureId, PlaneCodes.Physical, q, r, raw, tick: 0);
    }

    private static List<CultureBoundaryDirtyCell> DrainDirtyCells(Simulation simulation)
    {
        var buffer = new CultureBoundaryDirtyCell[simulation.Options.MapCellCapacity];
        var count = simulation.CultureBoundaries.DrainDirtyCells(
            simulation.World,
            PlaneCodes.Physical,
            buffer);
        var dirty = new List<CultureBoundaryDirtyCell>();
        for (var i = 0; i < count; i++)
        {
            dirty.Add(buffer[i]);
        }

        return dirty;
    }

    private static void CreateKnownMap(Simulation simulation, int radius)
    {
        simulation.World.SetMapRadius(radius);
        simulation.RegisterTerrain(terrainId: 1, boundaryGroupId: 1);
        for (var q = -radius; q <= radius; q++)
        {
            var minR = Math.Max(-radius, -q - radius);
            var maxR = Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.CreateCell(q, r, terrainId: 1, ownerEntityId: 0);
            }
        }
    }

    private static (int Q, int R)[] NeighborCells(int q, int r) =>
    [
        (q + 1, r),
        (q + 1, r - 1),
        (q, r - 1),
        (q - 1, r),
        (q - 1, r + 1),
        (q, r + 1),
    ];
}
