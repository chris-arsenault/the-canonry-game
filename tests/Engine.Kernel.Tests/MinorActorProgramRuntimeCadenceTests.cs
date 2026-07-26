namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed partial class MinorActorProgramRuntimeTests
{
    [Fact]
    public void MainProgramCadenceStillRunsMinorActorsOncePerTick()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    MainProgramCadenceTicks = 3,
                },
            },
        });
        SeedSourceEntity(simulation);
        var rule = RegisterCultureExpansionEmitter(simulation);
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlTicks: 3,
            actionCadenceTicks: 1,
            workDelta: FixedScalar.FromSingle(0f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.CurrentTick);
        Assert.Equal(1, simulation.MinorActors.State.ActiveCount);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(2, simulation.CurrentTick);
        Assert.Equal(0, simulation.MinorActors.State.ActiveCount);
    }

    [Fact]
    public void MinorActorsRunOncePerTick()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = CultureInfluenceActionScaleTuning(1f),
        });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        SeedSourceEntity(simulation);
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            FixedScalar.FromSingle(30f),
            tick: 0);
        var rule = RegisterCultureExpansionEmitter(simulation);
        var before = simulation.World.GetCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0);
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlTicks: 10,
            actionCadenceTicks: 1,
            workDelta: FixedScalar.FromSingle(1f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.CurrentTick);
        Assert.Equal(before + FixedScalar.One, simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
    }
}
