namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class PlanningProgramRuntimeTests
{
    [Fact]
    public void GoapPlanReturnsImmediateAndRootActions()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int growthAttributeId = 10;
        const int blockerAttributeId = 11;
        simulation.RegisterAttribute(kindId, growthAttributeId, 0f, 100f, 10f);
        simulation.RegisterAttribute(kindId, blockerAttributeId, 0f, 100f, 0f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            growthAttributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
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

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        Assert.Equal(ActionFamilyCodes.Process, decision.ImmediateAction.ActionFamilyCode);
        Assert.Equal(stockpile, decision.ImmediateAction.ActionSlot);
        Assert.Equal(entityId, decision.ImmediateTargetEntityId);

        var header = simulation.Planners.PlanHeader(decision.Plan);
        Assert.Equal(PlannerKindCodes.Goap, header.PlannerKindCode);
        Assert.Equal(ActionFamilyCodes.Process, header.ActionFamilyCode);
        Assert.Equal(entityId, header.ActorEntityId);
        Assert.Equal(kindId, header.ActorKindId);
        Assert.Equal(2, header.StepCount);
        Assert.Equal(0, header.ImmediateStepIndex);
        Assert.Equal(1, header.RootStepIndex);
        Assert.Equal(entityId, header.RootTargetEntityId);
        Assert.True(header.CandidateCount > 0);
        Assert.True(header.Score > 0f);

        var immediate = simulation.Planners.PlanStep(decision.Plan, header.ImmediateStepIndex);
        var root = simulation.Planners.PlanStep(decision.Plan, header.RootStepIndex);
        Assert.Equal(stockpile, immediate.Action.ActionSlot);
        Assert.Equal(entityId, immediate.TargetEntityId);
        Assert.Equal(header.RootStepIndex, immediate.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Immediate, immediate.StepRoleCode);
        Assert.Equal(grow, root.Action.ActionSlot);
        Assert.Equal(entityId, root.TargetEntityId);
        Assert.Equal(-1, root.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Root, root.StepRoleCode);
    }

    [Fact]
    public void ProcessTickUsesPlannerFacadeAndStoresSelectedPlan()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 100, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(entityId, attributeId));
        var header = simulation.Planners.PlanHeader(simulation.Planners.LastPlan);
        Assert.Equal(PlanningStatusCodes.Succeeded, header.StatusCode);
        Assert.Equal(ActionFamilyCodes.Process, header.ActionFamilyCode);
        Assert.Equal(1, header.StepCount);
        var step = simulation.Planners.PlanStep(simulation.Planners.LastPlan, header.ImmediateStepIndex);
        Assert.Equal(process, step.Action.ActionSlot);
        Assert.Equal(entityId, step.TargetEntityId);
    }

    [Fact]
    public void DirectPlanDoesNotExecuteSelectedProcess()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 110, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        Assert.Equal(process, decision.ImmediateAction.ActionSlot);
        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void DirectProcessPlanEmitsPlanShapedKernelEvents()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        const int processId = 1100;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 3,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        Assert.Equal(KernelEventType.PlanStarted, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalConsidered, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalScored, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalSelected, ReadNextEvent(simulation, cursor).EventType);
        var step = ReadNextEvent(simulation, cursor);
        Assert.Equal(KernelEventType.PlanStepEmitted, step.EventType);
        Assert.Equal(processId, step.AsPlanStepEmitted().ProcessId);
        Assert.Equal(entityId, step.AsPlanStepEmitted().TargetEntityId);
        var committed = ReadNextEvent(simulation, cursor);
        Assert.Equal(KernelEventType.PlanCommitted, committed.EventType);
        Assert.Equal(1, committed.AsPlanCommitted().StepCount);
    }

    [Fact]
    public void DirectProcessPlanEmitsTraceEventsWhenEnabled()
    {
        var simulation = CreateSimulation(traceEnabled: true);
        const int kindId = 1;
        const int attributeId = 10;
        const int processId = 1101;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 3,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        var events = ReadAvailableEvents(simulation, cursor);
        var traceEvents = events
            .Where(evt => evt.EventType == KernelEventType.Trace)
            .Select(evt => evt.AsTrace())
            .ToArray();

        Assert.Contains(traceEvents, evt =>
            evt.TraceSubjectCode == TraceSubjectCodes.Goal &&
            evt.StatusCode == TraceStatusCodes.Offered &&
            evt.EffectValueId == attributeId);
        Assert.Contains(traceEvents, evt =>
            evt.TraceSubjectCode == TraceSubjectCodes.TargetDetail &&
            evt.StatusCode == TraceStatusCodes.Actionable &&
            evt.ActionId == processId &&
            evt.TargetEntityId == entityId);
        Assert.Contains(traceEvents, evt =>
            evt.TraceSubjectCode == TraceSubjectCodes.ActionSummary &&
            evt.StatusCode == TraceStatusCodes.Selected &&
            evt.ActionId == processId);
    }

    [Fact]
    public void FailedProcessPlanEmitsAbandonedPlanKernelEvents()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 4,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.NoCandidates, decision.StatusCode);
        var started = ReadNextEvent(simulation, cursor);
        Assert.Equal(KernelEventType.PlanStarted, started.EventType);
        Assert.Equal(PlanningStatusCodes.NoCandidates, started.AsPlanStarted().ReasonCodeId);
        var abandoned = ReadNextEvent(simulation, cursor);
        Assert.Equal(KernelEventType.PlanAbandoned, abandoned.EventType);
        Assert.Equal(PlanningStatusCodes.NoCandidates, abandoned.AsPlanAbandoned().ReasonCodeId);
    }

    [Fact]
    public void PlanWithoutObjectivesReturnsNoCandidates()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 120, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.NoCandidates, decision.StatusCode);
        Assert.Equal(0, decision.CandidateCount);
        Assert.Equal(0, decision.ImmediateAction.ActionSlot);
        Assert.Equal(0, decision.ImmediateTargetEntityId);
    }

    [Fact]
    public void PlanWithOnlyBlockedCandidatesReturnsNoWeight()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 130, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.NoWeight, decision.StatusCode);
        Assert.True(decision.CandidateCount > 0);
        Assert.Equal(0, decision.ImmediateAction.ActionSlot);
        Assert.Equal(0, decision.ImmediateTargetEntityId);
        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void UnsupportedPlannerKindReturnsExplicitStatus()
    {
        var simulation = CreateSimulation();
        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Htn,
                ActionFamilyCodes.Process,
                ActorEntityId: 1,
                ActorKindId: 1,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.UnsupportedPlanner, decision.StatusCode);
        Assert.Equal(ActionFamilyCodes.Process, decision.ImmediateAction.ActionFamilyCode);
        Assert.Equal(0, decision.ImmediateAction.ActionSlot);
    }

    [Fact]
    public void UnsupportedActionFamilyReturnsExplicitStatus()
    {
        var simulation = CreateSimulation();
        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCode: 999,
                ActorEntityId: 1,
                ActorKindId: 1,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.UnsupportedActionFamily, decision.StatusCode);
        Assert.Equal(999, decision.ImmediateAction.ActionFamilyCode);
        Assert.Equal(0, decision.ImmediateAction.ActionSlot);
    }

    [Fact]
    public void ConfiguredUnsupportedPlannerDoesNotSilentlyExecuteProcess()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var process = simulation.Processes.RegisterProcess(kindId, processId: 140, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.Planners.SetPlannerKind(PlannerKindCodes.Htn);
        simulation.AdvanceTicks(1);

        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));
    }

    [Fact]
    public void GoapGambitPlanUsesSharedPlanShape()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Gambits.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 300,
            targetMode: ProcessTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.SetGoalEffect(gambit, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Gambits.AddPrecondition(gambit, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Gambits.AddStep(gambit, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.Gambit,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        Assert.Equal(ActionFamilyCodes.Gambit, decision.ImmediateAction.ActionFamilyCode);
        Assert.Equal(gambit, decision.ImmediateAction.ActionSlot);
        Assert.Equal(entityId, decision.ImmediateTargetEntityId);
        Assert.Equal(10f, simulation.World.GetAttribute(entityId, attributeId));

        var header = simulation.Planners.PlanHeader(decision.Plan);
        Assert.Equal(PlannerKindCodes.Goap, header.PlannerKindCode);
        Assert.Equal(ActionFamilyCodes.Gambit, header.ActionFamilyCode);
        Assert.Equal(1, header.StepCount);
        Assert.Equal(0, header.ImmediateStepIndex);
        Assert.Equal(0, header.RootStepIndex);

        var step = simulation.Planners.PlanStep(decision.Plan, header.ImmediateStepIndex);
        Assert.Equal(ActionFamilyCodes.Gambit, step.Action.ActionFamilyCode);
        Assert.Equal(gambit, step.Action.ActionSlot);
        Assert.Equal(entityId, step.TargetEntityId);
        Assert.Equal(-1, step.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Immediate, step.StepRoleCode);

        Assert.Equal(KernelEventType.PlanStarted, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalConsidered, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalScored, ReadNextEvent(simulation, cursor).EventType);
        Assert.Equal(KernelEventType.GoalSelected, ReadNextEvent(simulation, cursor).EventType);
        var emittedStep = ReadNextEvent(simulation, cursor);
        Assert.Equal(KernelEventType.PlanStepEmitted, emittedStep.EventType);
        Assert.Equal(300, emittedStep.AsPlanStepEmitted().ProcessId);
        Assert.Equal(KernelEventType.PlanCommitted, ReadNextEvent(simulation, cursor).EventType);
    }

    [Fact]
    public void GambitTickUsesPlannerFacadeAndStoresSelectedPlan()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int attributeId = 10;
        simulation.RegisterAttribute(kindId, attributeId, 0f, 100f, 10f);
        simulation.Gambits.RegisterAttributeObjective(
            kindId,
            attributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var gambit = simulation.Gambits.RegisterGambit(
            kindId,
            gambitId: 310,
            targetMode: ProcessTargetModeCodes.Self,
            successChance: 1f);
        simulation.Gambits.SetGoalEffect(gambit, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        simulation.Gambits.AddPrecondition(gambit, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Gambits.AddExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Gambits.AddRewardExpressionOp(gambit, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Gambits.AddStep(gambit, ProcessOperationCodes.AddAttribute, attributeId, x: 20f);

        simulation.AdvanceTicks(1);

        Assert.Equal(30f, simulation.World.GetAttribute(entityId, attributeId));
        var header = simulation.Planners.PlanHeader(simulation.Planners.LastPlan);
        Assert.Equal(PlanningStatusCodes.Succeeded, header.StatusCode);
        Assert.Equal(ActionFamilyCodes.Gambit, header.ActionFamilyCode);
        var step = simulation.Planners.PlanStep(simulation.Planners.LastPlan, header.ImmediateStepIndex);
        Assert.Equal(gambit, step.Action.ActionSlot);
        Assert.Equal(entityId, step.TargetEntityId);
    }

    private static Simulation CreateSimulation(bool traceEnabled = false)
    {
        return new Simulation(new SimulationOptions
        {
            EntityCapacity = 8,
            RelationshipCapacity = 8,
            AttributeCapacity = 64,
            ResourceCapacity = 32,
            MapCellCapacity = 128,
            EventCapacity = 256,
            ProcessCapacity = 32,
            ProcessStepCapacity = 256,
            ProcessCooldownCapacity = 32,
            ProcessObjectiveCapacity = 16,
            ProcessProviderSelectorCapacity = 16,
            ProcessSupportRuleCapacity = 16,
            ProcessCommitmentCapacity = 32,
            GambitCapacity = 4,
            GambitStepCapacity = 32,
            GambitCooldownCapacity = 32,
            GambitObjectiveCapacity = 4,
            GambitProviderSelectorCapacity = 4,
            GambitSupportRuleCapacity = 4,
            GambitCommitmentCapacity = 8,
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
            TraceEnabled = traceEnabled,
            Seed = 1,
        });
    }

    private static KernelEvent ReadNextEvent(Simulation simulation, KernelEventCursor cursor)
    {
        Assert.True(simulation.Events.TryRead(cursor, out var read));
        return read.Event;
    }

    private static KernelEvent[] ReadAvailableEvents(Simulation simulation, KernelEventCursor cursor)
    {
        var events = new List<KernelEvent>();
        while (simulation.Events.TryRead(cursor, out var read))
        {
            events.Add(read.Event);
        }

        return events.ToArray();
    }
}
