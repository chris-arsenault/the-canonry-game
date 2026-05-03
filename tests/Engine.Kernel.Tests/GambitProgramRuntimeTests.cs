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
    public void SuccessfulGambitRecordsProvenanceInvocationAndMutationOutput()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        const int gambitId = 100;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var events = ReadAvailableEvents(simulation, cursor);
        var attributeChanged = Assert.Single(events, evt => evt.EventType == KernelEventType.AttributeChanged);
        var recorded = Assert.Single(
            events,
            evt => evt.EventType == KernelEventType.ProvenanceInvocationRecorded).AsProvenanceInvocationRecorded();

        Assert.True(attributeChanged.ProvenanceInvocationId > 0);
        Assert.Equal(attributeChanged.ProvenanceInvocationId, recorded.InvocationId);
        Assert.Equal(ProvenanceProducerKindCodes.Gambit, recorded.ProducerKindCode);
        Assert.Equal(gambitId, recorded.ProducerId);
        Assert.Equal(entityId, recorded.PrimaryEntityId);
        Assert.Equal(1, recorded.OutputCount);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GambitExecuted &&
            evt.ProvenanceInvocationId == recorded.InvocationId);
        var outputCursor = simulation.World.OutputsOfProvenanceInvocation(recorded.InvocationId);
        Assert.True(simulation.World.MoveNextProvenanceOutput(ref outputCursor, out var outputId));
        Assert.Equal(ProvenanceOutputKindCodes.AttributeChanged, simulation.World.ProvenanceOutputKindCode(outputId));
        Assert.Equal(attributeId, simulation.World.ProvenanceOutputValueId(outputId));
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

    [Fact]
    public void ContestedInfluencePreconditionRequiresOverlap()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        simulation.World.SetMapRadius(1);
        simulation.World.SetCell(0, 0, terrainId: 1);
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, 15f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 0, 0, 60f, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 105,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(
            gambit,
            BehaviorPreconditionCodes.ContestedInfluence,
            PlaneCodes.Physical,
            x: 12f,
            y: 55f);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void ContestedInfluencePreconditionRejectsSeparatedInfluence()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        simulation.World.SetMapRadius(1);
        simulation.World.SetCell(0, 0, terrainId: 1);
        simulation.World.SetCell(1, 0, terrainId: 1);
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, 15f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 1, 0, 60f, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 106,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(
            gambit,
            BehaviorPreconditionCodes.ContestedInfluence,
            PlaneCodes.Physical,
            x: 12f,
            y: 55f);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void SuccessfulGambitCanMoveTargetToSelfPhysical()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 107,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.MoveTargetToSelfPhysical);

        simulation.AdvanceTicks(1);

        Assert.Equal(simulation.World.EntityPhysicalQ(actor), simulation.World.EntityPhysicalQ(target));
        Assert.Equal(simulation.World.EntityPhysicalR(actor), simulation.World.EntityPhysicalR(target));
    }

    [Fact]
    public void SuccessfulGambitCanTransformTargetCoverageTerrain()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int originalTerrainId = 1;
        const int transformedTerrainId = 2;
        simulation.RegisterTerrain(originalTerrainId, boundaryGroupId: 1);
        simulation.RegisterTerrain(transformedTerrainId, boundaryGroupId: 1);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.CreateCell(1, 0, originalTerrainId, target);
        simulation.CreateCell(1, -1, originalTerrainId, target);
        simulation.CreateCell(-1, 0, originalTerrainId, ownerEntityId: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 108,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.SetTargetCoverageTerrain, transformedTerrainId);

        simulation.AdvanceTicks(1);

        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(1, 0));
        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(1, -1));
        Assert.Equal(originalTerrainId, simulation.World.CellTerrainId(-1, 0));
    }

    [Fact]
    public void SelfHasOutgoingRelationshipPreconditionCanBlockActorRegardlessOfTarget()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var heldTarget = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Neutral,
            actor,
            heldTarget,
            0.9f,
            tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 109,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.SelfHasOutgoingRelationship, relationshipKindId);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.Not);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void EndpointRelationshipCountBelowCountsReciprocalPairAsOneCounterpart()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var existingCounterpart = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Positive, actor, existingCounterpart, 0.9f, tick: 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Positive, existingCounterpart, actor, 0.9f, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 110,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.EndpointRelationshipCountBelow, relationshipKindId, BehaviorEndpointCodes.Self, 2);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void EndpointRelationshipCountBelowCanBlockTargetAlreadyHeldByAnotherActor()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var otherActor = simulation.CreateEntity(actorKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Neutral, otherActor, target, 0.9f, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 111,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.EndpointRelationshipCountBelow, relationshipKindId, BehaviorEndpointCodes.Target, 1);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void SharedInfluencePreconditionRequiresOverlap()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        simulation.World.SetMapRadius(1);
        simulation.World.SetCell(0, 0, terrainId: 1);
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, 25f, tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 0, 0, 25f, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 112,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(
            gambit,
            BehaviorPreconditionCodes.SharedInfluence,
            PlaneCodes.Physical,
            x: 20f,
            y: 20f);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void ActorTraitDecisionModifierCanMakeZeroScoreGambitActionable()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        const int traitId = 70;
        const int gambitId = 120;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.World.AddTrait(actor, traitId, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 0f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 5f);
        simulation.Gambits.RegisterTraitModifier(
            gambitId,
            traitId,
            TraitEffectBearerCodes.Actor,
            scoreDelta: 1f,
            successChanceDelta: 0f);

        simulation.AdvanceTicks(1);

        Assert.Equal(15f, simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void TargetTraitDecisionModifierCanRaiseGambitSuccessChance()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int traitId = 71;
        const int gambitId = 121;
        simulation.RegisterAttribute(actorKindId, attributeId, 0f, 100f, 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.World.AddTrait(target, traitId, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: 0f);
        simulation.Gambits.AddPrecondition(gambit, BehaviorPreconditionCodes.TargetIsNotSelf);
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: 5f);
        simulation.Gambits.RegisterTraitModifier(
            gambitId,
            traitId,
            TraitEffectBearerCodes.GambitTarget,
            scoreDelta: 0f,
            successChanceDelta: 1f);

        simulation.AdvanceTicks(1);

        Assert.Equal(15f, simulation.World.GetAttribute(actor, attributeId));
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

    private static KernelEvent[] ReadAvailableEvents(Simulation simulation, KernelEventCursor cursor)
    {
        var result = new List<KernelEvent>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            result.Add(read.Event);
        }

        return result.ToArray();
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
