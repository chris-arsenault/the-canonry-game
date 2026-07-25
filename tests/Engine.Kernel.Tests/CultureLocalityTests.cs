namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class CultureLocalityTests
{
    private const int CultureId = 1;
    private const int OtherCultureId = 2;

    [Fact]
    public void CellAt_DerivesCurrentLocalCultureFactsWithoutAComponentCache()
    {
        var simulation = CreateSimulation();
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureControlledRawMass);
        AddCulture(simulation, CultureId, 1, 0, WorldState.CultureClaimedRawMass);
        AddCulture(simulation, CultureId, 2, 0, WorldState.CultureClaimedRawMass);
        AddCulture(simulation, OtherCultureId, 0, 0, WorldState.CultureClaimedRawMass);

        var locality = simulation.CultureLocality.CellAt(
            simulation.World,
            CultureId,
            PlaneCodes.Physical,
            0,
            0);

        Assert.Equal(WorldState.CultureControlledRawMass, locality.RawMass);
        Assert.Equal(WorldState.CultureClaimedRawMass, locality.StrongestAdjacentRawMass);
        Assert.Equal(WorldState.CultureClaimedRawMass, locality.StrongestRingTwoRawMass);
        Assert.Equal(1, locality.SupportedAdjacentCount);
        Assert.Equal(1, locality.SupportedRingTwoCount);
        Assert.Equal(OtherCultureId, locality.ForeignCultureId);
        Assert.True(locality.ConnectedRawMass > locality.LocalRawMass);
    }

    [Fact]
    public void CellAt_DerivesInteriorHoleFromLocalSupport()
    {
        var simulation = CreateSimulation();
        foreach (var (q, r) in NeighborCells(0, 0))
        {
            AddCulture(simulation, CultureId, q, r, WorldState.CultureControlledRawMass);
        }

        var locality = simulation.CultureLocality.CellAt(
            simulation.World,
            CultureId,
            PlaneCodes.Physical,
            0,
            0);

        Assert.Equal(FixedScalar.FromSingle(0f), locality.RawMass);
        Assert.Equal(6, locality.SupportedAdjacentCount);
        Assert.True(locality.IsInteriorHole);
        Assert.True(simulation.CultureLocality.IsInteriorHoleAt(
            simulation.World,
            CultureId,
            PlaneCodes.Physical,
            0,
            0));
    }

    [Fact]
    public void DrainDirtyCells_RefreshesOnlyTheAffectedRadiusTwoNeighborhood()
    {
        var simulation = CreateSimulation();
        CreateKnownMap(simulation, radius: 4);
        AddCulture(simulation, CultureId, 0, 0, WorldState.CultureClaimedRawMass);
        var buffer = new CultureLocalityDirtyCell[simulation.Options.MapCellCapacity];

        var count = simulation.CultureLocality.DrainDirtyCells(
            simulation.World,
            PlaneCodes.Physical,
            buffer);

        Assert.Equal(19, count);
        Assert.Contains(new CultureLocalityDirtyCell(PlaneCodes.Physical, 2, 0), buffer[..count]);
        Assert.DoesNotContain(new CultureLocalityDirtyCell(PlaneCodes.Physical, 3, 0), buffer[..count]);
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

    private static void AddCulture(
        Simulation simulation,
        int cultureId,
        int q,
        int r,
        FixedScalar raw) =>
        simulation.ModifyCellCultureInfluenceRaw(
            cultureId,
            PlaneCodes.Physical,
            q,
            r,
            raw,
            tick: 0);

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
