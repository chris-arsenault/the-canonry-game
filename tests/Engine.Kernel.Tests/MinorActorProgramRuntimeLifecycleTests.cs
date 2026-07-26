namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Kernel;

public sealed partial class MinorActorProgramRuntimeTests
{
    [Fact]
    public void LifecycleMaintenanceVerbRepairsSourceFromAgeLoadInsideSupportedMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        const int healthAttributeId = 10;
        simulation.RegisterAttribute(
            SourceKindId,
            healthAttributeId,
            minimum: 0,
            maximum: 100,
            defaultValue: 50);
        RegisterTestLifecycle(simulation, healthAttributeId);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 2);
        var source = simulation.CreateEntity(
            SourceKindId,
            subtypeId: 1,
            cultureId: CultureId,
            0,
            0,
            0,
            0,
            0,
            tick: 0);
        Assert.Equal(SourceEntityId, source);
        var rule = RegisterLifecycleMaintenanceEmitter(simulation);
        foreach (var cell in new[] { (0, 0), (1, 0), (0, -1), (1, -1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(
                CultureId,
                PlaneCodes.Physical,
                cell.Item1,
                cell.Item2,
                FixedScalar.FromSingle(30f),
                tick: 0);
        }

        SpawnLifecycleWorker(simulation, rule);
        var before = simulation.World.GetAttribute(SourceEntityId, healthAttributeId);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(MinorActorActionCodes.LifecycleMaintenance, evt.ActionCode);
        Assert.Equal(MinorActorVerbTargetingCodes.MaintainSourceLifecycle, evt.TargetingCode);
        Assert.Equal(TraceStatusCodes.Selected, evt.StatusCode);
        Assert.True(evt.DeltaUnits > FixedScalar.Zero.Units);
        Assert.True(simulation.World.GetAttribute(SourceEntityId, healthAttributeId) > before);
    }

    [Fact]
    public void LifecycleMaintenanceVerbRequiresSupportedCultureMass()
    {
        var simulation = CreateVerbSimulation(mainTickRatio: 1);
        const int healthAttributeId = 10;
        simulation.RegisterAttribute(
            SourceKindId,
            healthAttributeId,
            minimum: 0,
            maximum: 100,
            defaultValue: 50);
        RegisterTestLifecycle(simulation, healthAttributeId);
        SetKnownDisc(simulation, PlaneCodes.Physical, 0, 0, 2);
        var source = simulation.CreateEntity(
            SourceKindId,
            subtypeId: 1,
            cultureId: CultureId,
            0,
            0,
            0,
            0,
            0,
            tick: 0);
        Assert.Equal(SourceEntityId, source);
        var rule = RegisterLifecycleMaintenanceEmitter(simulation);
        SpawnLifecycleWorker(simulation, rule);
        var before = simulation.World.GetAttribute(SourceEntityId, healthAttributeId);

        _ = simulation.AdvanceTicks(1);

        var evt = Assert.Single(ReadMinorActorVerbEvents(simulation));
        Assert.Equal(TraceStatusCodes.NoTarget, evt.StatusCode);
        Assert.True(simulation.World.GetAttribute(SourceEntityId, healthAttributeId) < before);
    }

    private static void RegisterTestLifecycle(Simulation simulation, int healthAttributeId) =>
        simulation.Effects.RegisterLifecycle(
            SourceKindId,
            healthAttributeId,
            targetTicks: 10,
            nominalHealthUnits: KernelFixedPoint.AttributeUnits(50f),
            flatDrainShare: FixedScalar.Zero,
            variance: FixedScalar.Zero);

    private static int RegisterLifecycleMaintenanceEmitter(Simulation simulation)
    {
        var rule = RegisterCultureEmitter(simulation);
        _ = simulation.MinorActors.RegisterVerb(
            rule,
            CultureVerbId,
            MinorActorActionCodes.LifecycleMaintenance,
            MinorActorVerbTargetingCodes.MaintainSourceLifecycle,
            PlaneCodes.Physical,
            radius: 2,
            sampleCount: 64,
            weight: FixedScalar.One,
            workDeltaMultiplier: FixedScalar.One);
        return rule;
    }

    private static void SpawnLifecycleWorker(Simulation simulation, int rule) =>
        simulation.MinorActors.Spawn(
            cultureId: CultureId,
            sourceEntityId: SourceEntityId,
            sourceRuleId: rule,
            actorKindCode: MinorActorKindCodes.Worker,
            planeCode: PlaneCodes.Physical,
            q: 0,
            r: 0,
            ttlTicks: 4,
            actionCadenceTicks: 1,
            workDelta: FixedScalar.FromSingle(100f),
            tick: 0);
}
