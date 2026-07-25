namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class ForgeCellVerticalTests
{
    private const int CultureId = 1;
    private const int KindId = 1;
    private const int SpireTerrainId = 7;
    private const int TunnelTerrainId = 8;

    [Fact]
    public void ForgingExtendsTheColumnThenCreepsToNeighbors()
    {
        var simulation = CreateSimulation();
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(simulation.TryForgeCellVertical(entity, direction: 1, SpireTerrainId, tick: 0));
        }

        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 0, 0, 1));
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 0, 0, 2));
        Assert.True(simulation.World.CellIsKnown(PlaneCodes.Physical, 0, 0, 3));
        Assert.Equal(SpireTerrainId, simulation.World.CellTerrainId(PlaneCodes.Physical, 0, 0, 2));

        // Column is full at +3; the next forge creeps to a known neighbor column.
        Assert.True(simulation.TryForgeCellVertical(entity, direction: 1, SpireTerrainId, tick: 0));
        var neighborForged = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            var q = HexQ(direction);
            var r = HexR(direction);
            if (simulation.World.CellIsKnown(PlaneCodes.Physical, q, r, 1))
            {
                neighborForged++;
            }
        }

        Assert.Equal(1, neighborForged);
    }

    [Fact]
    public void ForgingDepositsOwnCultureMassAtTheForgedCell()
    {
        var simulation = CreateSimulation();
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        Assert.True(simulation.TryForgeCellVertical(entity, direction: -1, TunnelTerrainId, tick: 0));

        Assert.Equal(
            WorldState.CultureClaimedRawMass,
            simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0, -1));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void ForgedTunnelsAreWalkableViaVerticalWander()
    {
        var simulation = CreateSimulation();
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);
        _ = simulation.TryForgeCellVertical(entity, direction: -1, TunnelTerrainId, tick: 0);

        var movedDown = false;
        for (var attempt = 0; attempt < 200 && !movedDown; attempt++)
        {
            _ = simulation.Movement.Wander(simulation, entity, PlaneCodes.Physical, tick: attempt);
            movedDown = simulation.World.EntityPhysicalZ(entity) == -1;
        }

        Assert.True(movedDown);
    }

    private static int HexQ(int direction) => direction switch
    {
        0 => 1,
        1 => 1,
        2 => 0,
        3 => -1,
        4 => -1,
        _ => 0,
    };

    private static int HexR(int direction) => direction switch
    {
        0 => 0,
        1 => -1,
        2 => -1,
        3 => 0,
        4 => 1,
        _ => 1,
    };

    private static Simulation CreateSimulation()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EntityCapacity = 16,
            MapCellCapacity = 512,
            MapInfluenceCapacity = 512,
            EventCapacity = 8192,
            SystemCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerPulse = FixedScalar.FromSingle(0f),
                        MinimumDecayPerPulse = FixedScalar.FromSingle(0f),
                    },
                },
            },
        });
        simulation.World.SetMapRadius(4);
        simulation.RegisterTerrain(1, boundaryGroupId: 1);
        simulation.RegisterTerrain(SpireTerrainId, boundaryGroupId: 2);
        simulation.RegisterTerrain(TunnelTerrainId, boundaryGroupId: 3);
        for (var q = -2; q <= 2; q++)
        {
            var minR = global::System.Math.Max(-2, -q - 2);
            var maxR = global::System.Math.Min(2, -q + 2);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.World.SetCell(PlaneCodes.Physical, q, r, terrainId: 1);
            }
        }

        return simulation;
    }
}
