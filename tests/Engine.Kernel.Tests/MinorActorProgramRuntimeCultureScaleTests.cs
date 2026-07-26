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
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerRun = FixedScalar.Zero,
                        MinimumDecayPerRun = FixedScalar.Zero,
                    },
                },
            },
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
            actionCadenceTicks: 10,
            workDelta: FixedScalar.FromSingle(2f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(
            (before + FixedScalar.FromSingle(0.5f)).Units,
            simulation.World.GetCellCultureInfluenceRaw(1, PlaneCodes.Physical, 0, 0).Units);
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
                    CultureField = new CultureFieldTuningOptions
                    {
                        DecayPerRun = FixedScalar.Zero,
                        MinimumDecayPerRun = FixedScalar.Zero,
                    },
                },
            },
        });
        simulation.World.SetCell(PlaneCodes.Physical, 0, 0, terrainId: 1);
        simulation.ModifyCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(8f), tick: 0);
        SeedSourceEntity(simulation);
        simulation.ModifyCellCultureInfluenceRaw(
            CultureId,
            PlaneCodes.Physical,
            0,
            0,
            FixedScalar.FromSingle(30f),
            tick: 0);
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.CultureCleanup,
            MinorActorVerbTargetingCodes.ReduceOtherCulture,
            PlaneCodes.Physical,
            radius: 1,
            sampleCount: 8,
            weight: FixedScalar.FromSingle(1f),
            workDeltaMultiplier: FixedScalar.FromSingle(1f));
        _ = simulation.MinorActors.Spawn(
            cultureId: 1,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlTicks: 10,
            actionCadenceTicks: 10,
            workDelta: FixedScalar.FromSingle(4f),
            tick: 0);

        _ = simulation.AdvanceTicks(1);

        Assert.Equal(
            FixedScalar.FromSingle(7f).Units,
            simulation.World.GetCellCultureInfluenceRaw(2, PlaneCodes.Physical, 0, 0).Units);
    }
}
