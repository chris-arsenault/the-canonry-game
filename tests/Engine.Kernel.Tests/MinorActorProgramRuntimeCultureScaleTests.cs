namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed partial class MinorActorProgramRuntimeTests
{
    [Fact]
    public void CultureExpansion_UsesConfiguredCultureInfluenceActionScale()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureInfluenceActionScale = FixedScalar.FromSingle(0.25f),
                },
            },
        });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: 0,
            sourceRuleId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureExpansion,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 2,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(2f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(0.5f), simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0));
    }

    [Fact]
    public void CultureCleanup_UsesConfiguredCultureInfluenceActionScale()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            MinorActorCapacity = 8,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    CultureInfluenceActionScale = FixedScalar.FromSingle(0.25f),
                },
            },
        });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(8f), tick: 0);
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: 0,
            sourceRuleId: 0,
            actorKindCode: MinorActorKindCodes.Worker,
            actionCode: MinorActorActionCodes.CultureCleanup,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlPulses: 2,
            moveChancePermille: 0,
            actionCadencePulses: 1,
            workDelta: FixedScalar.FromSingle(4f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(7f), simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0));
    }
}
