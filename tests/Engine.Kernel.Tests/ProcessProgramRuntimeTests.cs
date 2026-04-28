namespace Engine.Kernel.Tests;

using Engine.Kernel;

public sealed partial class ProcessProgramRuntimeTests
{
    [Fact]
    public void ExecutesProcessThatServesAttributeObjective()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        RegisterNeed(simulation, kindId, attributeId, defaultValue: 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var processSlot = simulation.Processes.RegisterProcess(
            kindId,
            processId: 100,
            targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(
            processSlot,
            ProcessEffectScopeCodes.SelfAttribute,
            attributeId,
            kindId);
        simulation.Processes.AddPrecondition(processSlot, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(processSlot, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(processSlot, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(processSlot, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void PlaneDistanceBlockerRoutesToStructuralMobilityProcess()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int inRangeFactId = 30;
        RegisterNeed(simulation, actorKindId, attributeId, defaultValue: 10f);
        RegisterNeed(simulation, targetKindId, attributeId, defaultValue: 100f);
        RegisterKnownMap(simulation);
        var actorId = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);

        var consume = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 101,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(consume, ProcessEffectScopeCodes.SelfAttribute, attributeId, actorKindId);
        simulation.Processes.AddPrecondition(consume, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(
            consume,
            ProcessPreconditionCodes.PlaneDistance,
            0,
            PlaneCodes.Physical,
            inRangeFactId);
        simulation.Processes.AddExpressionOp(consume, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(consume, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(consume, ProcessOperationCodes.AddAttribute, attributeId, x: 50f);

        var move = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 102,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(move, ProcessEffectScopeCodes.Structural, inRangeFactId, actorKindId);
        simulation.Processes.AddPrecondition(move, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(move, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(move, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddStep(move, ProcessOperationCodes.MoveTowardTargetPhysical);

        simulation.AdvanceTicks(1);

        Assert.Equal(1, simulation.World.EntityPhysicalQ(actorId));
        Assert.Equal(0, simulation.World.EntityPhysicalR(actorId));
        Assert.Equal(10f, simulation.World.GetAttribute(actorId, attributeId));
    }

    [Fact]
    public void PhysicalPathScratchCanBeReusedAcrossSearches()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int attributeId = 10;
        const int inRangeFactId = 30;
        RegisterNeed(simulation, actorKindId, attributeId, defaultValue: 10f);
        RegisterNeed(simulation, targetKindId, attributeId, defaultValue: 100f);
        RegisterKnownMap(simulation);
        var firstActorId = simulation.CreateEntity(actorKindId, 1, 1, -2, 0, 0, 0, 0, 0);
        var secondActorId = simulation.CreateEntity(actorKindId, 1, 1, 0, -2, 0, 0, 0, 0);
        _ = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);

        var consume = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 101,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(consume, ProcessEffectScopeCodes.SelfAttribute, attributeId, actorKindId);
        simulation.Processes.AddPrecondition(consume, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(
            consume,
            ProcessPreconditionCodes.PlaneDistance,
            0,
            PlaneCodes.Physical,
            inRangeFactId);
        simulation.Processes.AddExpressionOp(consume, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(consume, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(consume, ProcessOperationCodes.AddAttribute, attributeId, x: 50f);

        var move = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 102,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(move, ProcessEffectScopeCodes.Structural, inRangeFactId, actorKindId);
        simulation.Processes.AddPrecondition(move, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(move, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(move, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddStep(move, ProcessOperationCodes.MoveTowardTargetPhysical);

        var firstBefore = HexDistance(-2, 0, 2, 0);
        var secondBefore = HexDistance(0, -2, 2, 0);

        simulation.AdvanceTicks(1);

        Assert.True(HexDistance(
            simulation.World.EntityPhysicalQ(firstActorId),
            simulation.World.EntityPhysicalR(firstActorId),
            2,
            0) < firstBefore);
        Assert.True(HexDistance(
            simulation.World.EntityPhysicalQ(secondActorId),
            simulation.World.EntityPhysicalR(secondActorId),
            2,
            0) < secondBefore);
    }

    [Fact]
    public void DoesNotExecuteProcessProgramProcessWhenNoObjectiveIsUrgent()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        RegisterNeed(simulation, kindId, attributeId, defaultValue: 100f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 190, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: -10f);

        simulation.AdvanceTicks(1);

        Assert.Equal(100f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void IgnoresProcessThatDoesNotServeObjectiveEffect()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int objectiveAttributeId = 10;
        const int unrelatedAttributeId = 11;
        RegisterNeed(simulation, kindId, objectiveAttributeId, defaultValue: 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 191, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, unrelatedAttributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, objectiveAttributeId, x: 50f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, objectiveAttributeId));
    }

    [Fact]
    public void ExecutesBaselineObjective()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.Baseline,
            0f,
            100f,
            defaultValue: 10f,
            baseline: 20f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 192, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 10f);

        simulation.AdvanceTicks(1);

        Assert.Equal(20f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void ExecutesRangeDefaultObjectiveWhenAttributeIsAwayFromDefault()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        const int markerAttributeId = 11;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 80f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.RangeDefault,
            0f,
            100f,
            defaultValue: 50f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 193, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, markerAttributeId, x: 1f);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(entityId, markerAttributeId));
    }

    [Fact]
    public void ExpandsBlockingPreconditionIntoSubGoal()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int growthAttributeId = 10;
        const int blockerAttributeId = 11;
        RegisterNeed(simulation, kindId, growthAttributeId, defaultValue: 10f);
        simulation.RegisterAttribute(kindId, blockerAttributeId, 0f, 100f, 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var grow = simulation.Processes.RegisterProcess(kindId, processId: 200, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(grow, ProcessEffectScopeCodes.SelfAttribute, growthAttributeId, kindId);
        simulation.Processes.AddPrecondition(grow, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddPrecondition(
            grow,
            ProcessPreconditionCodes.SelfAttributeRange,
            blockerAttributeId,
            1,
            0,
            x: 5f);
        simulation.Processes.AddExpressionOp(grow, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(grow, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(grow, ProcessOperationCodes.AddAttribute, growthAttributeId, x: 50f);

        var stockpile = simulation.Processes.RegisterProcess(kindId, processId: 201, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(stockpile, ProcessEffectScopeCodes.SelfAttribute, blockerAttributeId, kindId);
        simulation.Processes.AddPrecondition(stockpile, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(stockpile, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(stockpile, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(stockpile, ProcessOperationCodes.AddAttribute, blockerAttributeId, x: 5f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, growthAttributeId));
        Assert.Equal(5f, simulation.World.GetAttribute(entityId, blockerAttributeId));
    }

    [Fact]
    public void BlockedSelfDirectedPlanWithZeroRewardStillExecutesEnabler()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int growthAttributeId = 10;
        const int blockerAttributeId = 11;
        RegisterNeed(simulation, kindId, growthAttributeId, defaultValue: 10f);
        simulation.RegisterAttribute(kindId, blockerAttributeId, 0f, 100f, 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var grow = simulation.Processes.RegisterProcess(kindId, processId: 202, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(grow, ProcessEffectScopeCodes.SelfAttribute, growthAttributeId, kindId);
        simulation.Processes.AddPrecondition(grow, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddPrecondition(
            grow,
            ProcessPreconditionCodes.SelfAttributeRange,
            blockerAttributeId,
            1,
            0,
            x: 3f);
        simulation.Processes.AddExpressionOp(grow, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(grow, ProcessExpressionCodes.SelfAttribute, blockerAttributeId);
        simulation.Processes.AddRewardExpressionOp(grow, ProcessExpressionCodes.Literal, x: 3f);
        simulation.Processes.AddRewardExpressionOp(grow, ProcessExpressionCodes.Divide);
        simulation.Processes.AddStep(grow, ProcessOperationCodes.AddAttribute, growthAttributeId, x: 50f);

        var stockpile = simulation.Processes.RegisterProcess(kindId, processId: 203, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(stockpile, ProcessEffectScopeCodes.SelfAttribute, blockerAttributeId, kindId);
        simulation.Processes.AddPrecondition(stockpile, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(stockpile, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(stockpile, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(stockpile, ProcessOperationCodes.AddAttribute, blockerAttributeId, x: 3f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, growthAttributeId));
        Assert.Equal(3f, simulation.World.GetAttribute(entityId, blockerAttributeId));
    }

    [Fact]
    public void TargetAttributeBlockerExecutesEnablerAgainstParentTarget()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int needAttributeId = 10;
        const int blockerAttributeId = 11;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var consume = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 204,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(consume, ProcessEffectScopeCodes.SelfAttribute, needAttributeId, actorKindId);
        simulation.Processes.AddProviderSelector(consume, ProcessProviderSelectorCodes.Structural, targetKindId);
        simulation.Processes.AddPrecondition(consume, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(
            consume,
            ProcessPreconditionCodes.TargetAttributeRange,
            blockerAttributeId,
            1,
            0,
            x: 5f);
        simulation.Processes.AddExpressionOp(consume, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(consume, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(consume, ProcessOperationCodes.AddAttribute, needAttributeId, x: 50f);

        var aid = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 205,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(aid, ProcessEffectScopeCodes.TargetAttribute, blockerAttributeId, targetKindId);
        simulation.Processes.AddPrecondition(aid, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(aid, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(aid, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(aid, ProcessOperationCodes.AddTargetAttribute, blockerAttributeId, x: 5f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(actor, needAttributeId));
        Assert.Equal(5f, simulation.World.GetAttribute(target, blockerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(actor, blockerAttributeId));
    }

    [Fact]
    public void SelectsSupportRulePlanWhenImmediateProcessIsWeak()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int growthAttributeId = 10;
        const int foodResourceId = 20;
        RegisterNeed(simulation, kindId, growthAttributeId, defaultValue: 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var grow = simulation.Processes.RegisterProcess(kindId, processId: 210, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(grow, ProcessEffectScopeCodes.SelfAttribute, growthAttributeId, kindId);
        simulation.Processes.AddPrecondition(grow, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(grow, ProcessExpressionCodes.Literal, x: 0.05f);
        simulation.Processes.AddRewardExpressionOp(grow, ProcessExpressionCodes.Literal, x: 2f);
        var support = simulation.Processes.AddSupportRule(
            grow,
            ProcessEffectScopeCodes.Resource,
            foodResourceId,
            effectKindId: 0,
            desiredMinimum: 10f,
            urgencyScale: 8f);
        simulation.Processes.AddSupportExpressionOp(support, ProcessExpressionCodes.SelfLocationResource, foodResourceId);
        simulation.Processes.AddStep(grow, ProcessOperationCodes.AddAttribute, growthAttributeId, x: 50f);

        var gather = simulation.Processes.RegisterProcess(kindId, processId: 211, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(gather, ProcessEffectScopeCodes.Resource, foodResourceId, kindId: 0);
        simulation.Processes.AddPrecondition(gather, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(gather, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(gather, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(gather, ProcessOperationCodes.AddLocationResource, foodResourceId, x: 10f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, growthAttributeId));
        Assert.Equal(10f, simulation.World.GetResource(foodResourceId, 0, 0));
    }

    [Fact]
    public void NamedProviderSupplyModel_RoutesHexRecoveryThroughProviderAttribute()
    {
        var simulation = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Processes = new ProcessProgramTuningOptions
                {
                    ResourceSupplyModelCode = ProcessResourceSupplyModelCodes.NamedProviderNoHex,
                },
            },
        });
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int unusedTargetKindId = 9;
        const int healthAttributeId = 10;
        const int providerAttributeId = 11;
        const int resourceId = 20;
        RegisterNeed(simulation, actorKindId, healthAttributeId, defaultValue: 10f);
        simulation.RegisterAttribute(providerKindId, providerAttributeId, 0f, 100f, 20f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var provider = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var recover = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 300,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: unusedTargetKindId,
            resourceSupplyRoleCode: ProcessResourceSupplyRoleCodes.HexHealthRecovery,
            namedProviderKindId: providerKindId,
            namedProviderAttributeId: providerAttributeId);
        simulation.Processes.SetGoalEffect(recover, ProcessEffectScopeCodes.SelfAttribute, healthAttributeId, actorKindId);
        simulation.Processes.AddProviderSelector(recover, ProcessProviderSelectorCodes.Resource, unusedTargetKindId, resourceId);
        simulation.Processes.AddPrecondition(recover, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(recover, ProcessPreconditionCodes.LocationResourceRange, resourceId, 1, 1, 0, x: 1f);
        simulation.Processes.AddExpressionOp(recover, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(recover, ProcessExpressionCodes.TargetLocationResource, resourceId);
        simulation.Processes.AddStep(recover, ProcessOperationCodes.HexToHealth, healthAttributeId, resourceId, 1, x: 2f);

        simulation.AdvanceTicks(1);

        Assert.Equal(20f, simulation.World.GetAttribute(actor, healthAttributeId));
        Assert.Equal(15f, simulation.World.GetAttribute(provider, providerAttributeId));
        Assert.Equal(0f, simulation.World.GetResource(resourceId, 1, 0));
    }

    [Fact]
    public void NamedProviderSupplyModel_DisablesHexResourceProducerProcesses()
    {
        var simulation = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Processes = new ProcessProgramTuningOptions
                {
                    ResourceSupplyModelCode = ProcessResourceSupplyModelCodes.NamedProviderNoHex,
                },
            },
        });
        const int kindId = 1;
        const int resourceId = 20;
        _ = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gather = simulation.Processes.RegisterProcess(
            kindId,
            processId: 301,
            targetMode: ProcessTargetModeCodes.Self,
            resourceSupplyRoleCode: ProcessResourceSupplyRoleCodes.HexResourceProducer);
        simulation.Processes.AddPrecondition(gather, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(gather, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(gather, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddStep(gather, ProcessOperationCodes.AddLocationResource, resourceId, x: 10f);

        simulation.AdvanceTicks(1);

        Assert.Equal(0f, simulation.World.GetResource(resourceId, 0, 0));
    }

    [Fact]
    public void PilotWorkCommitments_ContinueSelectedProcessThroughProcessCooldown()
    {
        var simulation = CreateSimulation(new KernelTuningOptions
        {
            Programs = new KernelProgramTuningOptions
            {
                Processes = new ProcessProgramTuningOptions
                {
                    WorkCommitmentCode = ProcessWorkCommitmentCodes.Pilot,
                },
            },
        });
        const int kindId = 1;
        const int attributeId = 10;
        RegisterNeed(simulation, kindId, attributeId, defaultValue: 10f);
        var entity = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(
            kindId,
            processId: 302,
            cooldownTicks: 10,
            targetMode: ProcessTargetModeCodes.Self,
            workCommitmentTicks: 3);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 10f);

        simulation.AdvanceTicks(3);

        Assert.Equal(40f, simulation.World.GetAttribute(entity, attributeId));
    }

    [Fact]
    public void RetainsCommittedPlanWhenChallengerIsWithinHysteresis()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int desiredAttributeId = 10;
        const int rewardAttributeId = 11;
        const int markerAttributeId = 12;
        simulation.Processes.RegisterStandingObjective(
            actorKindId,
            ProcessEffectScopeCodes.TargetAttribute,
            desiredAttributeId,
            targetKindId,
            baseUrgency: 1f);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var committed = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var challenger = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        SetSetupAttribute(simulation, committed, rewardAttributeId, 100f);
        SetSetupAttribute(simulation, challenger, rewardAttributeId, 110f);
        simulation.World.SetEntityStatus(challenger, EntityStatusCodes.Historical, 0);

        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 220,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.TargetAttribute, desiredAttributeId, targetKindId);
        simulation.Processes.AddProviderSelector(
            process,
            ProcessProviderSelectorCodes.TargetAttribute,
            targetKindId,
            rewardAttributeId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.TargetAttribute, rewardAttributeId);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttribute, markerAttributeId, x: 1f);

        simulation.AdvanceTicks(1);
        simulation.World.SetEntityStatus(challenger, EntityStatusCodes.Active, simulation.CurrentTick);
        simulation.AdvanceTicks(1);

        Assert.Equal(2f, simulation.World.GetAttribute(committed, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(challenger, markerAttributeId));
    }

    [Fact]
    public void RecencyDecayShiftsRepeatedProcessProgramWinnerToAlternateProcess()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int effectAttributeId = 10;
        const int firstMarkerAttributeId = 11;
        const int secondMarkerAttributeId = 12;
        simulation.Processes.RegisterStandingObjective(
            kindId,
            ProcessEffectScopeCodes.SelfAttribute,
            effectAttributeId,
            kindId,
            baseUrgency: 1f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var first = simulation.Processes.RegisterProcess(kindId, processId: 222, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(first, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, kindId);
        simulation.Processes.AddPrecondition(first, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(first, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(first, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(first, ProcessOperationCodes.AddAttribute, firstMarkerAttributeId, x: 1f);

        var second = simulation.Processes.RegisterProcess(kindId, processId: 223, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(second, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, kindId);
        simulation.Processes.AddPrecondition(second, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(second, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(second, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(second, ProcessOperationCodes.AddAttribute, secondMarkerAttributeId, x: 1f);

        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(entityId, firstMarkerAttributeId));
        Assert.Equal(1f, simulation.World.GetAttribute(entityId, secondMarkerAttributeId));
    }

    [Fact]
    public void ProcessCooldownPreventsRepeatedProcessProgramExecution()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int effectAttributeId = 10;
        const int markerAttributeId = 11;
        simulation.Processes.RegisterStandingObjective(
            kindId,
            ProcessEffectScopeCodes.SelfAttribute,
            effectAttributeId,
            kindId,
            baseUrgency: 1f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(
            kindId,
            processId: 224,
            cooldownTicks: 100,
            targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, markerAttributeId, x: 1f);

        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(entityId, markerAttributeId));
    }

    [Fact]
    public void CooldownPreconditionUsesPerProcessLastExecutionTick()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int effectAttributeId = 10;
        const int markerAttributeId = 11;
        simulation.Processes.RegisterStandingObjective(
            kindId,
            ProcessEffectScopeCodes.SelfAttribute,
            effectAttributeId,
            kindId,
            baseUrgency: 1f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 225, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.CooldownElapsed, 3);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, markerAttributeId, x: 1f);

        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);
        simulation.AdvanceTicks(1);

        Assert.Equal(2f, simulation.World.GetAttribute(entityId, markerAttributeId));
    }

    [Fact]
    public void PressureRangePreconditionUsesKernelPressureValue()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int effectAttributeId = 10;
        const int pressureId = 20;
        RegisterNeed(simulation, kindId, effectAttributeId, defaultValue: 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        simulation.Pressures.RegisterPressure(
            pressureId,
            PressureScopeCodes.Entity,
            baseline: 0f,
            homeostasisRate: 0f,
            smoothingFactor: 1f);
        simulation.Pressures.ApplyDelta(pressureId, entityId, 4f);
        simulation.Pressures.Tick(simulation, tick: 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 226, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, effectAttributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddPrecondition(
            process,
            ProcessPreconditionCodes.PressureRange,
            pressureId,
            entityId,
            0,
            1,
            y: 5f);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, effectAttributeId, x: 50f);

        simulation.AdvanceTicks(1);

        Assert.Equal(60f, simulation.World.GetAttribute(entityId, effectAttributeId));
    }

    [Fact]
    public void LocalityBundleBiasesSelectionTowardSharedDestination()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int firstEffectId = 10;
        const int secondEffectId = 11;
        const int nearEffectId = 12;
        const int markerAttributeId = 30;
        simulation.Processes.RegisterStandingObjective(actorKindId, ProcessEffectScopeCodes.TargetAttribute, firstEffectId, targetKindId, 1f);
        simulation.Processes.RegisterStandingObjective(actorKindId, ProcessEffectScopeCodes.TargetAttribute, secondEffectId, targetKindId, 1f);
        simulation.Processes.RegisterStandingObjective(actorKindId, ProcessEffectScopeCodes.TargetAttribute, nearEffectId, targetKindId, 1f);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var farFirst = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        var farSecond = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        var near = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        SetSetupAttribute(simulation, farFirst, firstEffectId, 1f);
        SetSetupAttribute(simulation, farSecond, secondEffectId, 1f);
        SetSetupAttribute(simulation, near, nearEffectId, 1f);

        RegisterTargetMarkerProcess(simulation, actorKindId, targetKindId, processId: 230, firstEffectId, markerAttributeId, reward: 1f);
        RegisterTargetMarkerProcess(simulation, actorKindId, targetKindId, processId: 231, secondEffectId, markerAttributeId, reward: 1f);
        RegisterTargetMarkerProcess(simulation, actorKindId, targetKindId, processId: 232, nearEffectId, markerAttributeId, reward: 1.2f);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(farFirst, markerAttributeId) + simulation.World.GetAttribute(farSecond, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(near, markerAttributeId));
    }

    [Fact]
    public void ResourceProviderSelectorUsesIndexedResourceCandidates()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int needAttributeId = 10;
        const int foodResourceId = 20;
        const int markerAttributeId = 30;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var richProvider = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var emptyProvider = simulation.CreateEntity(providerKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        SetSetupResource(simulation, foodResourceId, 1, 0, 5f);

        var process = RegisterSelfObjectiveTargetMarkerProcess(
            simulation,
            actorKindId,
            needAttributeId,
            processId: 240,
            providerKindId,
            markerAttributeId);
        simulation.Processes.AddProviderSelector(process, ProcessProviderSelectorCodes.Resource, providerKindId, foodResourceId);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(richProvider, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(emptyProvider, markerAttributeId));
    }

    [Fact]
    public void StructuralProviderSelectorUsesKindIndex()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int needAttributeId = 10;
        const int rewardAttributeId = 20;
        const int markerAttributeId = 30;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var low = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var high = simulation.CreateEntity(providerKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        SetSetupAttribute(simulation, low, rewardAttributeId, 1f);
        SetSetupAttribute(simulation, high, rewardAttributeId, 10f);

        var process = RegisterSelfObjectiveTargetMarkerProcess(
            simulation,
            actorKindId,
            needAttributeId,
            processId: 250,
            providerKindId,
            markerAttributeId);
        simulation.Processes.AddProviderSelector(process, ProcessProviderSelectorCodes.Structural, providerKindId);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.TargetAttribute, rewardAttributeId);

        simulation.AdvanceTicks(1);

        Assert.Equal(0f, simulation.World.GetAttribute(low, markerAttributeId));
        Assert.Equal(1f, simulation.World.GetAttribute(high, markerAttributeId));
    }

    [Fact]
    public void LocationProviderSelectorUsesOccupancyIndex()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int needAttributeId = 10;
        const int locationAttributeId = 20;
        const int markerAttributeId = 30;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        _ = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var colocated = simulation.CreateEntity(providerKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var elsewhere = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        SetSetupAttribute(simulation, colocated, locationAttributeId, 1f);
        SetSetupAttribute(simulation, elsewhere, locationAttributeId, 1f);

        var process = RegisterSelfObjectiveTargetMarkerProcess(
            simulation,
            actorKindId,
            needAttributeId,
            processId: 260,
            providerKindId,
            markerAttributeId);
        simulation.Processes.AddProviderSelector(
            process,
            ProcessProviderSelectorCodes.LocationAttribute,
            providerKindId,
            locationAttributeId);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(colocated, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(elsewhere, markerAttributeId));
    }

    [Fact]
    public void RelationshipProviderSelectorUsesCsrFactAndStrengthFilters()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int needAttributeId = 10;
        const int markerAttributeId = 30;
        const int relationshipKindId = 40;
        const int factId = 50;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var matching = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var missingFact = simulation.CreateEntity(providerKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        var tooWeak = simulation.CreateEntity(providerKindId, 1, 1, 3, 0, 0, 0, 0, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, actor, matching, 0.9f, 0, factId, factCount: 1);
        simulation.World.CreateRelationship(relationshipKindId, 0, actor, missingFact, 0.9f, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, actor, tooWeak, 0.2f, 0, factId, factCount: 1);

        var process = RegisterSelfObjectiveTargetMarkerProcess(
            simulation,
            actorKindId,
            needAttributeId,
            processId: 270,
            providerKindId,
            markerAttributeId);
        simulation.Processes.AddProviderSelector(
            process,
            ProcessProviderSelectorCodes.OutgoingRelationship,
            providerKindId,
            relationshipKindId: relationshipKindId,
            factId: factId,
            minimumStrength: 0.5f);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(matching, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(missingFact, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(tooWeak, markerAttributeId));
    }

    [Fact]
    public void IncomingRelationshipProviderSelectorUsesCsrFactAndStrengthFilters()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int providerKindId = 2;
        const int needAttributeId = 10;
        const int markerAttributeId = 30;
        const int relationshipKindId = 40;
        const int factId = 50;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var matching = simulation.CreateEntity(providerKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        var missingFact = simulation.CreateEntity(providerKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        var tooWeak = simulation.CreateEntity(providerKindId, 1, 1, 3, 0, 0, 0, 0, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, matching, actor, 0.9f, 0, factId, factCount: 1);
        simulation.World.CreateRelationship(relationshipKindId, 0, missingFact, actor, 0.9f, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, tooWeak, actor, 0.2f, 0, factId, factCount: 1);

        var process = RegisterSelfObjectiveTargetMarkerProcess(
            simulation,
            actorKindId,
            needAttributeId,
            processId: 271,
            providerKindId,
            markerAttributeId);
        simulation.Processes.AddProviderSelector(
            process,
            ProcessProviderSelectorCodes.IncomingRelationship,
            providerKindId,
            relationshipKindId: relationshipKindId,
            factId: factId,
            minimumStrength: 0.5f);

        simulation.AdvanceTicks(1);

        Assert.Equal(1f, simulation.World.GetAttribute(matching, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(missingFact, markerAttributeId));
        Assert.Equal(0f, simulation.World.GetAttribute(tooWeak, markerAttributeId));
    }

    [Fact]
    public void RelationshipPreconditionBlockerExpandsToRelationalSubGoal()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int needAttributeId = 10;
        const int relationshipKindId = 40;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);

        var consume = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 280,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(consume, ProcessEffectScopeCodes.SelfAttribute, needAttributeId, actorKindId);
        simulation.Processes.AddProviderSelector(consume, ProcessProviderSelectorCodes.Structural, targetKindId);
        simulation.Processes.AddPrecondition(consume, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(
            consume,
            ProcessPreconditionCodes.SelfHasRelationshipToTarget,
            relationshipKindId);
        simulation.Processes.AddExpressionOp(consume, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(consume, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(consume, ProcessOperationCodes.AddAttribute, needAttributeId, x: 50f);

        var connect = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 281,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(connect, ProcessEffectScopeCodes.Relational, relationshipKindId, targetKindId);
        simulation.Processes.AddPrecondition(connect, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(connect, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(connect, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(connect, ProcessOperationCodes.CreateRelationship, relationshipKindId, x: 1f);

        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(actor, needAttributeId));
        Assert.True(simulation.World.FindRelationship(actor, target, relationshipKindId) > 0);
    }

    [Fact]
    public void TargetHasRelationshipToSelfPreconditionUsesIncomingRelationship()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int needAttributeId = 10;
        const int relationshipKindId = 40;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, target, actor, 0.7f, 0);

        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 282,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, needAttributeId, actorKindId);
        simulation.Processes.AddProviderSelector(process, ProcessProviderSelectorCodes.Structural, targetKindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(
            process,
            ProcessPreconditionCodes.TargetHasRelationshipToSelf,
            relationshipKindId,
            1,
            x: 0.5f);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, needAttributeId, x: 50f);

        simulation.AdvanceTicks(1);

        Assert.Equal(60f, simulation.World.GetAttribute(actor, needAttributeId));
    }

    [Fact]
    public void RelatedEntityMatchesTargetPreconditionUsesSelfRelationship()
    {
        var simulation = CreateSimulation();
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int needAttributeId = 10;
        const int relationshipKindId = 40;
        RegisterNeed(simulation, actorKindId, needAttributeId, defaultValue: 10f);
        var actor = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var target = simulation.CreateEntity(targetKindId, 1, 1, 1, 0, 0, 0, 0, 0);
        simulation.World.CreateRelationship(relationshipKindId, 0, actor, target, 1f, 0);

        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 283,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, needAttributeId, actorKindId);
        simulation.Processes.AddProviderSelector(process, ProcessProviderSelectorCodes.Structural, targetKindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.RelatedEntityMatchesTarget, relationshipKindId);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, needAttributeId, x: 50f);

        simulation.AdvanceTicks(1);

        Assert.Equal(60f, simulation.World.GetAttribute(actor, needAttributeId));
    }

}
