namespace Engine.Kernel.Tests;

using Engine.Constants;
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
        simulation.RegisterAttribute(kindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(entityId, attributeId));
        var executed = LastEvent<GambitExecutedKernelEvent>(
            simulation,
            KernelEventType.GambitExecuted,
            static evt => evt.AsGambitExecuted());
        Assert.Equal(100, executed.GambitId);
        Assert.Equal(KernelEventState.Succeeded, executed.State);
    }

    [Fact]
    public void SetGambitEnabledCommandDisablesGambitById()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 100,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        Assert.True(simulation.Commands.TryEnqueue(
            KernelCommand.From(new SetGambitEnabledKernelCommand(
                IssueVersion: (int)simulation.WorldVersion,
                GambitId: 100,
                Enabled: 0)),
            out _));

        simulation.AdvanceTicks(1);

        // Program controls publish at a window boundary. Owner ranges that
        // already captured this window may perform one final attempt.
        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(entityId, attributeId));
        Assert.True(simulation.Gambits.TryGetGambitEnabledById(100, out var enabled));
        Assert.False(enabled);

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void SuccessfulGambitRecordsProvenanceInvocationAndMutationOutput()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        const int gambitId = 100;
        simulation.RegisterAttribute(kindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));
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
        Assert.Equal(entityId, recorded.SecondaryEntityId);
        Assert.Equal(0, recorded.ParentInvocationId);
        Assert.Equal(ProvenanceTriggerKindCodes.Entity, recorded.TriggerKindCode);
        Assert.Equal(entityId, recorded.TriggerId);
        Assert.Equal(0, recorded.OutputCount);
        Assert.Contains(events, evt =>
            evt.EventType == KernelEventType.GambitExecuted &&
            evt.ProvenanceInvocationId == recorded.InvocationId);
    }

    [Fact]
    public void SuccessfulGambitRecordsProviderRelationshipTriggerAndParentInvocation()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        const int gambitId = 130;
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        int providerRelationship;
        using (simulation.EnterProvenanceInvocation(
                   ProvenanceProducerKindCodes.System,
                   producerId: 901,
                   primaryEntityId: actor))
        {
            providerRelationship = simulation.CreateRelationship(
                relationshipKindId,
                RelationshipPolarityCodes.Positive,
                actor,
                target,
                FixedScalar.FromSingle(0.9f),
                tick: 0);
        }

        var parentInvocationId = simulation.World.RelationshipCreatedByInvocationId(providerRelationship);
        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddProviderSelector(
            gambit,
            BehaviorProviderSelectorCodes.OutgoingRelationship,
            kindId: targetKindId,
            relationshipKindId: relationshipKindId,
            minimumStrength: FixedScalar.FromSingle(0.5f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var recorded = Assert.Single(
            ReadAvailableEvents(simulation, cursor),
            evt => evt.EventType == KernelEventType.ProvenanceInvocationRecorded).AsProvenanceInvocationRecorded();
        Assert.Equal(ProvenanceProducerKindCodes.Gambit, recorded.ProducerKindCode);
        Assert.Equal(gambitId, recorded.ProducerId);
        Assert.Equal(actor, recorded.PrimaryEntityId);
        Assert.Equal(target, recorded.SecondaryEntityId);
        Assert.Equal(parentInvocationId, recorded.ParentInvocationId);
        Assert.Equal(ProvenanceTriggerKindCodes.Relationship, recorded.TriggerKindCode);
        Assert.Equal(providerRelationship, recorded.TriggerId);
    }

    [Fact]
    public void SuccessfulGambitRecordsProviderAttributeTrigger()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int actorAttributeId = 10;
        const int providerAttributeId = 11;
        const int gambitId = 131;
        simulation.RegisterAttribute(actorKindId, actorAttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        simulation.RegisterAttribute(targetKindId, providerAttributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(1f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddProviderSelector(
            gambit,
            BehaviorProviderSelectorCodes.TargetAttribute,
            kindId: targetKindId,
            valueId: providerAttributeId);
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, actorAttributeId, x: FixedScalar.FromSingle(20f));
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var recorded = Assert.Single(
            ReadAvailableEvents(simulation, cursor),
            evt => evt.EventType == KernelEventType.ProvenanceInvocationRecorded).AsProvenanceInvocationRecorded();
        Assert.Equal(ProvenanceProducerKindCodes.Gambit, recorded.ProducerKindCode);
        Assert.Equal(gambitId, recorded.ProducerId);
        Assert.Equal(actor, recorded.PrimaryEntityId);
        Assert.Equal(target, recorded.SecondaryEntityId);
        Assert.Equal(0, recorded.ParentInvocationId);
        Assert.Equal(ProvenanceTriggerKindCodes.Attribute, recorded.TriggerKindCode);
        Assert.Equal(providerAttributeId, recorded.TriggerId);
    }

    [Fact]
    public void FailedGambitConsumesAttemptButDoesNotApplyMutation()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 101,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(0f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(entityId, attributeId));
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
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.CreateEndpointRelationship,
            relationshipKindId,
            BehaviorEndpointCodes.Target,
            BehaviorEndpointCodes.Self,
            RelationshipPolarityCodes.Negative,
            x: FixedScalar.FromSingle(0.75f));

        simulation.AdvanceTicks(1);

        var relationship = simulation.World.FindRelationship(target, actor, relationshipKindId);
        Assert.True(relationship > 0);
        Assert.Equal(RelationshipPolarityCodes.Negative, simulation.World.RelationshipPolarityCode(relationship));
        Assert.Equal(FixedScalar.FromSingle(0.75f), simulation.World.RelationshipStrength(relationship));
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
            FixedScalar.FromSingle(0.9f),
            tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 103,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.SetEndpointRelationshipStrength,
            relationshipKindId,
            BehaviorEndpointCodes.Target,
            BehaviorEndpointCodes.Self,
            x: FixedScalar.FromSingle(0.1f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(0.1f), simulation.World.RelationshipStrength(relationship));
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
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.SetHistorical,
            b: BehaviorEndpointCodes.Target);

        simulation.AdvanceTicks(1);

        Assert.Equal(EntityStatusCodes.Active, simulation.World.EntityStatus(actor));
        Assert.False(simulation.World.EntityExists(target));
    }

    [Fact]
    public void SuccessfulSelfGambitCanRetireActorWithCooldown()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 104,
            cooldownTicks: 10,
            entityCooldownTicks: 10,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(
            gambit,
            BehaviorExpressionCodes.Literal,
            x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.SetHistorical,
            b: BehaviorEndpointCodes.Target);

        simulation.AdvanceTicks(1);

        Assert.False(simulation.World.EntityExists(actor));
    }

    [Fact]
    public void ReawakenedDistrictCreatesFreshArtifactAndFreshRelationship()
    {
        var simulation = CreateSimulationWithInstalledDistrict(out var artifact, out var settlement);
        RegisterReawakenGambit(simulation);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        simulation.AdvanceTicks(1);

        var created = Assert.Single(
            ReadAvailableEvents(simulation, cursor),
            evt => evt.EventType == KernelEventType.EntityCreated).AsEntityCreated();
        AssertFreshReawakenedArtifact(simulation, created, artifact, settlement);
    }

    [Fact]
    public void SuccessfulGambitCanClaimTargetCultureFromSelfWhenTargetCultureMatches()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int actorCultureId = 2;
        const int iceCultureId = 4;
        _ = simulation.CreateEntity(actorKindId, 1, actorCultureId, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, iceCultureId, 1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 105,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.EndpointCultureIs,
                A: BehaviorEndpointCodes.Target,
                B: iceCultureId));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.SetTargetCultureFromSelf);

        simulation.AdvanceTicks(1);

        Assert.Equal(actorCultureId, simulation.World.EntityCultureId(target));
        Assert.Equal(actorCultureId, LastEvent<EntityCultureChangedKernelEvent>(
            simulation,
            KernelEventType.EntityCultureChanged,
            static evt => evt.AsEntityCultureChanged()).CurrentCultureId);
    }

    [Fact]
    public void EndpointCulturePreconditionBlocksAlreadyClaimedTarget()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int actorCultureId = 2;
        const int iceCultureId = 4;
        _ = simulation.CreateEntity(actorKindId, 1, actorCultureId, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, actorCultureId, 1, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 106,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.EndpointCultureIs,
                A: BehaviorEndpointCodes.Target,
                B: iceCultureId));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.SetTargetCultureFromSelf);

        simulation.AdvanceTicks(1);

        Assert.Equal(actorCultureId, simulation.World.EntityCultureId(target));
        Assert.DoesNotContain(
            ReadAvailableEvents(simulation, simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable)),
            evt => evt.EventType == KernelEventType.EntityCultureChanged);
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
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 2, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(15f), tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(60f), tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 105,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.ContestedInfluence,
                A: PlaneCodes.Physical,
                X: FixedScalar.FromSingle(12f),
                Y: FixedScalar.FromSingle(55f)));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(actor, attributeId));
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
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 2, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(15f), tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(60f), tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 106,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.ContestedInfluence,
                A: PlaneCodes.Physical,
                X: FixedScalar.FromSingle(12f),
                Y: FixedScalar.FromSingle(55f)));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(actor, attributeId));
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
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
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
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.SetTargetCoverageTerrain, transformedTerrainId);

        simulation.AdvanceTicks(1);

        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(1, 0));
        Assert.Equal(transformedTerrainId, simulation.World.CellTerrainId(1, -1));
        Assert.Equal(originalTerrainId, simulation.World.CellTerrainId(-1, 0));
    }

    [Fact]
    public void SuccessfulGambitCanBuildTravelStructureLine()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int terrainId = 1;
        const int travelStructureId = 9;
        simulation.RegisterTerrain(terrainId, boundaryGroupId: 1);
        simulation.Movement.RegisterTravelStructure(travelStructureId, PlaneCodes.Physical);
        simulation.CreateCell(0, 0, terrainId, ownerEntityId: 0);
        simulation.CreateCell(1, 0, terrainId, ownerEntityId: 0);
        simulation.CreateCell(2, 0, terrainId, ownerEntityId: 0);
        simulation.CreateCell(0, 1, terrainId, ownerEntityId: 0);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 109,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            gambit,
            BehaviorOperationCodes.BuildTravelStructureLine,
            travelStructureId,
            PlaneCodes.Physical,
            c: 4);

        simulation.AdvanceTicks(1);

        Assert.Equal(travelStructureId, simulation.Movement.TravelStructureId(PlaneCodes.Physical, 0, 0));
        Assert.Equal(travelStructureId, simulation.Movement.TravelStructureId(PlaneCodes.Physical, 1, 0));
        Assert.Equal(travelStructureId, simulation.Movement.TravelStructureId(PlaneCodes.Physical, 2, 0));
        Assert.Equal(0, simulation.Movement.TravelStructureId(PlaneCodes.Physical, 0, 1));
    }

    [Fact]
    public void SelfHasOutgoingRelationshipPreconditionCanBlockActorRegardlessOfTarget()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var heldTarget = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(
            relationshipKindId,
            RelationshipPolarityCodes.Neutral,
            actor,
            heldTarget,
            FixedScalar.FromSingle(0.9f),
            tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 109,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.SelfHasOutgoingRelationship,
                A: relationshipKindId,
                Negated: true));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void EndpointRelationshipCountBelowCountsReciprocalPairAsOneCounterpart()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var existingCounterpart = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Positive, actor, existingCounterpart, FixedScalar.FromSingle(0.9f), tick: 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Positive, existingCounterpart, actor, FixedScalar.FromSingle(0.9f), tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 110,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.EndpointRelationshipCountBelow,
                A: relationshipKindId,
                B: BehaviorEndpointCodes.Self,
                C: 2));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void EndpointRelationshipCountBelowCanBlockTargetAlreadyHeldByAnotherActor()
    {
        var simulation = CreateSimulation(seed: 1);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int relationshipKindId = 40;
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var otherActor = simulation.CreateEntity(actorKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, -1, 0, 0, 0, 0, 0);
        simulation.CreateRelationship(relationshipKindId, RelationshipPolarityCodes.Neutral, otherActor, target, FixedScalar.FromSingle(0.9f), tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 111,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.EndpointRelationshipCountBelow,
                A: relationshipKindId,
                B: BehaviorEndpointCodes.Target,
                C: 1));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(10f), simulation.World.GetAttribute(actor, attributeId));
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
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 2, 1, 0, 0, 0, 0, 0);
        simulation.SetCellInfluence(actor, PlaneCodes.Physical, 1, 0, FixedScalar.FromSingle(25f), tick: 0);
        simulation.SetCellInfluence(target, PlaneCodes.Physical, 0, 0, FixedScalar.FromSingle(25f), tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId: 112,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(
                BehaviorPreconditionCodes.SharedInfluence,
                A: PlaneCodes.Physical,
                X: FixedScalar.FromSingle(20f),
                Y: FixedScalar.FromSingle(20f)));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(20f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(30f), simulation.World.GetAttribute(actor, attributeId));
    }

    [Fact]
    public void WorkTierGambitRequiresSelfCultureMassAndIncrementsTier()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int gambitId = 122;
        simulation.World.SetMapRadius(2);
        simulation.World.SetCell(0, 0, terrainId: 1);
        var actor = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);
        foreach (var cell in new[] { (0, 0), (1, 0), (1, -1), (0, -1) })
        {
            simulation.ModifyCellCultureInfluenceRaw(1, PlaneCodes.Physical, cell.Item1, cell.Item2, FixedScalar.FromSingle(30f), tick: 0);
        }

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(BehaviorPreconditionCodes.TargetIsSelf),
            new(
                BehaviorPreconditionCodes.SelfCultureMass,
                A: PlaneCodes.Physical,
                X: FixedScalar.FromSingle(12f),
                Y: FixedScalar.FromSingle(90f)),
            new(BehaviorPreconditionCodes.SelfWorkTierBelow, A: 1));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.IncreaseWorkTier, 1);

        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.World.EntityWorkTier(actor));
    }

    [Fact]
    public void WorkTierGambitRejectsEntityOutsideCultureMass()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int gambitId = 123;
        simulation.World.SetMapRadius(1);
        simulation.World.SetCell(0, 0, terrainId: 1);
        var actor = simulation.CreateEntity(kindId, subtypeId: 1, cultureId: 1, 0, 0, 0, 0, 0, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            gambit,
            new(BehaviorPreconditionCodes.TargetIsSelf),
            new(
                BehaviorPreconditionCodes.SelfCultureMass,
                A: PlaneCodes.Physical,
                X: FixedScalar.FromSingle(12f),
                Y: FixedScalar.FromSingle(90f)),
            new(BehaviorPreconditionCodes.SelfWorkTierBelow, A: 1));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.IncreaseWorkTier, 1);

        simulation.AdvanceTicks(1);

        Assert.Equal(0, simulation.World.EntityWorkTier(actor));
    }

    [Fact]
    public void ActorTraitDecisionModifierCanMakeZeroScoreGambitActionable()
    {
        var simulation = CreateSimulation(seed: 1);
        const int kindId = 1;
        const int attributeId = 10;
        const int traitId = 70;
        const int gambitId = 120;
        simulation.RegisterAttribute(kindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.World.AddTrait(actor, traitId, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(0f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(5f));
        simulation.Gambits.RegisterTraitModifier(
            gambitId,
            traitId,
            TraitEffectBearerCodes.Actor,
            scoreDelta: FixedScalar.FromSingle(1f),
            successChanceDelta: FixedScalar.FromSingle(0f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(15f), simulation.World.GetAttribute(actor, attributeId));
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
        simulation.RegisterAttribute(actorKindId, attributeId, FixedScalar.FromSingle(0f), FixedScalar.FromSingle(100f), FixedScalar.FromSingle(10f));
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.World.AddTrait(target, traitId, tick: 0);

        var gambit = simulation.Gambits.RegisterGambit(
            actorKindId,
            gambitId,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: targetKindId,
            successChance: FixedScalar.FromSingle(0f));
        simulation.Gambits.SetConjunctivePreconditions(gambit, new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(gambit, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(gambit, BehaviorOperationCodes.AddAttribute, attributeId, x: FixedScalar.FromSingle(5f));
        simulation.Gambits.RegisterTraitModifier(
            gambitId,
            traitId,
            TraitEffectBearerCodes.GambitTarget,
            scoreDelta: FixedScalar.FromSingle(0f),
            successChanceDelta: FixedScalar.FromSingle(1f));

        simulation.AdvanceTicks(1);

        Assert.Equal(FixedScalar.FromSingle(15f), simulation.World.GetAttribute(actor, attributeId));
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

    private static Simulation CreateSimulationWithInstalledDistrict(out int artifact, out int settlement)
    {
        var simulation = CreateSimulation(seed: 1);
        simulation.RegisterTerrain(terrainId: 1, boundaryGroupId: 1);
        simulation.RegisterDistrictBonus(7, DistrictBonusKindCodes.Culture, FixedScalar.FromSingle(2f));
        artifact = simulation.CreateEntity(1, 7, 3, 2, 0, 1, -1, 0, tick: 0);
        settlement = simulation.CreateEntity(2, 1, 3, 0, 0, 6, -2, -4, tick: 0);
        simulation.CreateCell(2, 0, 1, artifact);
        _ = simulation.CreateRelationship(
            relationshipKindId: 4,
            RelationshipPolarityCodes.Positive,
            artifact,
            settlement,
            strength: FixedScalar.FromSingle(0.8f),
            tick: 0);

        var install = simulation.Gambits.RegisterGambit(
            kindId: 1,
            gambitId: 104,
            targetMode: BehaviorTargetModeCodes.KindOther,
            targetKindId: 2,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            install,
            new(BehaviorPreconditionCodes.TargetIsNotSelf));
        simulation.Gambits.AddExpressionOp(install, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(
            install,
            BehaviorOperationCodes.InstallDistrict,
            a: 5,
            c: RelationshipPolarityCodes.Positive,
            x: FixedScalar.FromSingle(0.6f));

        simulation.AdvanceTicks(1);

        Assert.False(simulation.World.EntityExists(artifact));
        Assert.Equal(1, simulation.World.ActiveDistrictCount(settlement));
        Assert.Equal(settlement, simulation.World.CoverageOwnerAt(2, 0));
        return simulation;
    }

    private static void RegisterReawakenGambit(Simulation simulation)
    {
        var reawaken = simulation.Gambits.RegisterGambit(
            kindId: 2,
            gambitId: 105,
            targetMode: BehaviorTargetModeCodes.Self,
            successChance: FixedScalar.FromSingle(1f));
        simulation.Gambits.SetConjunctivePreconditions(
            reawaken,
            new(BehaviorPreconditionCodes.TargetIsSelf));
        simulation.Gambits.AddExpressionOp(reawaken, BehaviorExpressionCodes.Literal, x: FixedScalar.FromSingle(1f));
        simulation.Gambits.AddStep(reawaken, BehaviorOperationCodes.ReawakenDistrict);
    }

    private static void AssertFreshReawakenedArtifact(
        Simulation simulation,
        EntityCreatedKernelEvent created,
        int retiredArtifact,
        int settlement)
    {
        Assert.NotEqual(retiredArtifact, created.EntityId);
        Assert.Equal(1, created.KindId);
        Assert.Equal(7, created.SubtypeId);
        Assert.Equal(3, created.CultureId);
        Assert.Equal(2, created.PhysicalQ);
        Assert.Equal(0, created.PhysicalR);
        Assert.Equal(6, created.SemanticQ);
        Assert.Equal(-2, created.SemanticR);
        Assert.Equal(-4, created.SemanticS);
        Assert.Equal(0, simulation.World.ActiveDistrictCount(settlement));
        Assert.True(
            simulation.World.TryResolveEntityOccurrence(
                created.EntityId,
                out var freshArtifact));

        var freshRelationship = simulation.World.FindRelationship(freshArtifact, settlement, kindId: 5);
        Assert.True(freshRelationship > 0);
        Assert.Equal(FixedScalar.FromSingle(0.6f), simulation.World.RelationshipStrength(freshRelationship));
    }

    private static Simulation CreateSimulation(int seed)
    {
        return KernelTestSimulation.Create(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 64,
            MapCellCapacity = 128,
            EventCapacity = 256,
            GambitCapacity = 4,
            GambitStepCapacity = 32,
            GeneratorCapacity = 4,
            GeneratorItemCapacity = 32,
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
