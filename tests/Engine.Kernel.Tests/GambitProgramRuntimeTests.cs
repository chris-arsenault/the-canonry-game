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
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

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
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: 0f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));
        var executed = LastEvent<GambitExecutedKernelEvent>(
            simulation,
            KernelEventType.GambitExecuted,
            static evt => evt.AsGambitExecuted());
        Assert.Equal(101, executed.GambitId);
        Assert.Equal(KernelEventState.Failed, executed.State);
    }

    [Fact]
    public void SuccessfulGambitCanCreateEndpointRelationshipFromTargetToSelf()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int relationshipKindId = 40;
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 102,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.CreateEndpointRelationship,
            relationshipKindId,
            BehaviorEndpointCodes.Target,
            BehaviorEndpointCodes.Self,
            RelationshipPolarityCodes.Negative,
            x: 0.75f);

        simulation.AdvanceTicks(1);

        var relationship = simulation.World.FindRelationship(target, actor, relationshipKindId);
        Assert.True(relationship > 0);
        Assert.Equal(RelationshipPolarityCodes.Negative, simulation.World.RelationshipPolarityCode(relationship));
        Assert.Equal(0.75f, simulation.World.RelationshipStrength(relationship));
    }

    [Fact]
    public void SuccessfulGambitCanSetEndpointRelationshipStrength()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int relationshipKindId = 40;
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var relationship = simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Positive,
            target,
            actor,
            0.9f,
            tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 103,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.SetEndpointRelationshipStrength,
            relationshipKindId,
            BehaviorEndpointCodes.Target,
            BehaviorEndpointCodes.Self,
            x: 0.1f);

        simulation.AdvanceTicks(1);

        Assert.Equal(0.1f, simulation.World.RelationshipStrength(relationship));
    }

    [Fact]
    public void SuccessfulGambitCanMakeTargetHistorical()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 104,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.SetHistorical,
            b: BehaviorEndpointCodes.Target);

        simulation.AdvanceTicks(1);

        Assert.Equal(EntityStatusCodes.Active, simulation.World.EntityStatus(actor));
        Assert.Equal(EntityStatusCodes.Historical, simulation.World.EntityStatus(target));
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
            EffectRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            Seed = seed,
        });
    }
}
