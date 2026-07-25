namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed partial class MinorActorProgramRuntimeTests
{
    [Fact]
    public void ConfiguredSubstepsRunInsideOneWorldPulse()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    MinorActorSubstepsPerPulse = 3,
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
            ttlPulses: 3,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(0f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(3, simulation.MinorActors.State.Pulse);
        Assert.Equal(1, simulation.CurrentTick);
        Assert.Equal(0, simulation.MinorActors.State.ActiveCount);
    }

    [Fact]
    public void DefaultCadenceRunsMinorActorsAsHighFrequencyLabor()
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
            ttlPulses: 10,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(1f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(5, simulation.MinorActors.State.Pulse);
        Assert.Equal(before + FixedScalar.FromSingle(5f), simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
    }
}
