namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed class PlayerInterventionCommandTests
{
    private const int CultureId = 1;
    private const int KindId = 1;

    [Fact]
    public void BlessCellCultureAddsRawMassOverTheRadius()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(4);
        SeedKnownDisc(simulation, radius: 4);

        Assert.True(simulation.Commands.TryEnqueue(
            KernelCommand.From(new BlessCellCultureKernelCommand(
                IssueVersion: (int)simulation.WorldVersion,
                CultureId,
                PlaneCodes.Physical,
                Q: 0,
                R: 0,
                Radius: 1,
                Amount: FixedScalar.FromSingle(120f))),
            out _));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(120f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
        Assert.Equal(FixedScalar.FromSingle(120f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 1, 0));
        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 2, 0));
    }

    [Fact]
    public void BlessCellCultureRejectsInvalidPayload()
    {
        var simulation = CreateSimulation();

        Assert.True(simulation.Commands.TryEnqueue(
            KernelCommand.From(new BlessCellCultureKernelCommand(
                IssueVersion: (int)simulation.WorldVersion,
                CultureId: 0,
                PlaneCodes.Physical,
                Q: 0,
                R: 0,
                Radius: 1,
                Amount: FixedScalar.FromSingle(120f))),
            out _));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(0f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void FavorEntityDoublesInfluenceProductionUntilExpiry()
    {
        var simulation = CreateSimulation();
        simulation.World.SetMapRadius(4);
        SeedKnownDisc(simulation, radius: 4);
        var entity = simulation.CreateEntity(KindId, 1, CultureId, 0, 0, 0, 0, 0, tick: 0);

        Assert.True(simulation.Commands.TryEnqueue(
            KernelCommand.From(new FavorEntityKernelCommand(
                IssueVersion: (int)simulation.WorldVersion,
                entity,
                DurationTicks: 10,
                MultiplierDelta: FixedScalar.FromSingle(1f))),
            out _));

        simulation.AdvanceTicks(1);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(10f), simulation.CurrentTick);
        Assert.Equal(FixedScalar.FromSingle(20f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));

        simulation.AdvanceTicks(15);

        _ = simulation.ModifyCellCultureInfluenceRawFromEntity(entity, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(10f), simulation.CurrentTick);
        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetCellCultureInfluenceRaw(CultureId, PlaneCodes.Physical, 0, 0));
    }

    private static void SeedKnownDisc(Simulation simulation, int radius)
    {
        for (var q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                simulation.World.SetCell(PlaneCodes.Physical, q, r, terrainId: 1);
            }
        }
    }

    private static Simulation CreateSimulation() =>
        new(new SimulationOptions
        {
            EntityCapacity = 16,
            MapCellCapacity = 256,
            MapInfluenceCapacity = 256,
            EventCapacity = 4096,
            SystemCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    // Interventions are tested against exact raw values; decay
                    // noise would smear them.
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerRun = FixedScalar.FromSingle(0f),
                        MinimumDecayPerRun = FixedScalar.FromSingle(0f),
                    },
                },
            },
        });
}
