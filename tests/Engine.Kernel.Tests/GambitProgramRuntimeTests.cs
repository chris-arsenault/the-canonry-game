namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class GambitProgramRuntimeTests
{
    [Fact]
    public void ExecutesSuccessfulGambitInSeparateProgram()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 100,
            targetMode: ProcessTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(entityId, attributeId));
        var executed = LastEvent<GambitExecutedKernelEvent>(
            simulation,
            KernelEventType.GambitExecuted,
            static evt => evt.AsGambitExecuted());
        Assert.Equal(100, executed.GambitId);
        Assert.Equal(KernelEventState.Succeeded, executed.State);
    }

    [Fact]
    public void FailedGambitConsumesAttemptButDoesNotApplyMutation()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 101,
            targetMode: ProcessTargetModeCodes.Self,
            successChance: 0f);
        simulation.Gambits.AddPrecondition(gambit, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));
        var executed = LastEvent<GambitExecutedKernelEvent>(
            simulation,
            KernelEventType.GambitExecuted,
            static evt => evt.AsGambitExecuted());
        Assert.Equal(101, executed.GambitId);
        Assert.Equal(KernelEventState.Failed, executed.State);
    }

    private static T LastEvent<T>(
        Simulation simulation,
        KernelEventType eventType,
        Func<KernelEvent, T> project)
    {
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var found = default(T);
        var matched = false;
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            if (sequenced.Event.EventType != eventType)
            {
                continue;
            }

            found = project(sequenced.Event);
            matched = true;
        }

        Assert.True(matched);
        return found!;
    }

    private static Simulation CreateSimulation(int seed)
    {
        return new Simulation(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 64,
            ResourceCapacity = 32,
            MapCellCapacity = 128,
            EventCapacity = 256,
            ProcessCapacity = 4,
            ProcessStepCapacity = 32,
            ProcessCooldownCapacity = 32,
            ProcessObjectiveCapacity = 4,
            GambitCapacity = 4,
            GambitStepCapacity = 32,
            GambitCooldownCapacity = 32,
            GambitObjectiveCapacity = 4,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
            PressureCapacity = 4,
            PressureValueCapacity = 16,
            DegeneracyDetectorCapacity = 4,
            DegeneracyStateCapacity = 16,
            LifecycleRuleCapacity = 8,
            EntityAttributeCapacity = 8,
            UpkeepRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            Seed = seed,
        });
    }
}
