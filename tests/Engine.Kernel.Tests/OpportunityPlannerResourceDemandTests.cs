namespace Engine.Kernel.Tests;

using Engine.Eventing;
using Engine.Kernel;

public sealed class OpportunityPlannerResourceDemandTests
{
    [Fact]
    public void OpportunityProcessPlannerCanUseResourceProductionAsSupportWork()
    {
        var simulation = CreateSimulation();
        const int kindId = 1;
        const int healthAttributeId = 10;
        const int sourceAttributeId = 11;
        const int resourceId = 20;
        simulation.RegisterAttribute(kindId, healthAttributeId, 0f, 100f, 10f);
        simulation.RegisterAttribute(kindId, sourceAttributeId, 0f, 100f, 80f);
        simulation.Processes.RegisterAttributeObjective(
            kindId,
            healthAttributeId,
            ProcessObjectiveUrgencyCodes.StandardNeed,
            0f,
            100f,
            10f,
            baseline: 0f);
        var entityId = simulation.CreateEntity(kindId, 1, 1, 0, 0, 0, 0, 0, 0);

        var recover = simulation.Processes.RegisterProcess(kindId, processId: 155, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(recover, ProcessEffectScopeCodes.SelfAttribute, healthAttributeId, kindId);
        simulation.Processes.AddPrecondition(recover, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddPrecondition(recover, ProcessPreconditionCodes.LocationResourceRange, resourceId, 1, 1, 0, x: 1f);
        simulation.Processes.AddExpressionOp(recover, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(recover, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(recover, ProcessOperationCodes.AddAttribute, healthAttributeId, x: 20f);

        var produceResource = simulation.Processes.RegisterProcess(kindId, processId: 156, targetMode: ProcessTargetModeCodes.Self);
        simulation.Processes.SetGoalEffect(produceResource, ProcessEffectScopeCodes.Resource, resourceId, kindId: 0);
        simulation.Processes.AddPrecondition(produceResource, ProcessPreconditionCodes.TargetIsSelf);
        simulation.Processes.AddExpressionOp(produceResource, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(produceResource, ProcessExpressionCodes.SelfAttribute, sourceAttributeId);
        simulation.Processes.AddStep(produceResource, ProcessOperationCodes.AddLocationResource, resourceId, x: 10f);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Opportunity,
                ActionFamilyCodes.Process,
                entityId,
                kindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        var header = simulation.Planners.PlanHeader(decision.Plan);
        Assert.Equal(produceResource, decision.ImmediateAction.ActionSlot);
        Assert.Equal(recover, simulation.Planners.PlanStep(decision.Plan, header.RootStepIndex).Action.ActionSlot);
    }

    [Fact]
    public void OpportunityProcessPlannerEmitsLocalityTraceForCandidateBundles()
    {
        var simulation = CreateSimulation(traceEnabled: true);
        const int actorKindId = 1;
        const int targetKindId = 2;
        const int effectAttributeId = 10;
        const int markerAttributeId = 20;
        simulation.RegisterAttribute(actorKindId, effectAttributeId, 0f, 100f, 10f);
        simulation.RegisterAttribute(targetKindId, effectAttributeId, 0f, 100f, 10f);
        simulation.RegisterAttribute(targetKindId, markerAttributeId, 0f, 100f, 0f);
        var actorId = simulation.CreateEntity(actorKindId, 1, 1, 0, 0, 0, 0, 0, 0);
        var targetId = simulation.CreateEntity(targetKindId, 1, 1, 2, 0, 0, 0, 0, 0);
        var process = simulation.Processes.RegisterProcess(
            actorKindId,
            processId: 170,
            targetMode: ProcessTargetModeCodes.KindOther,
            targetKindId: targetKindId);
        simulation.Processes.SetGoalEffect(process, ProcessEffectScopeCodes.TargetAttribute, effectAttributeId, targetKindId);
        simulation.Processes.AddPrecondition(process, ProcessPreconditionCodes.TargetIsNotSelf);
        simulation.Processes.AddExpressionOp(process, ProcessExpressionCodes.Literal, x: 1f);
        simulation.Processes.AddRewardExpressionOp(process, ProcessExpressionCodes.Literal, x: 10f);
        simulation.Processes.AddStep(process, ProcessOperationCodes.AddTargetAttribute, markerAttributeId, x: 1f);
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.Latest);

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Opportunity,
                ActionFamilyCodes.Process,
                actorId,
                actorKindId,
                Tick: 0,
                SelectionRoll: 0f));

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        var traceEvents = ReadAvailableEvents(simulation, cursor)
            .Where(evt => evt.EventType == KernelEventType.Trace)
            .Select(evt => evt.AsTrace())
            .ToArray();
        var selectedLocality = Assert.Single(traceEvents, evt =>
            evt.TraceSubjectCode == TraceSubjectCodes.Locality &&
            evt.StatusCode == TraceStatusCodes.Selected &&
            evt.TargetEntityId == targetId);
        Assert.Equal(1, selectedLocality.EffectScopeCode);
        Assert.Equal(1, selectedLocality.EffectValueId);
        Assert.Equal(1, selectedLocality.EffectKindId);
        Assert.Equal(2f, selectedLocality.RawWeight);
        Assert.NotEqual(0, selectedLocality.CandidateIndex);
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
            EffectRuleCapacity = 8,
            RelationshipRuleCapacity = 8,
            KindIdCapacity = 8,
            TerrainIdCapacity = 8,
            KernelTuning = new KernelTuningOptions(),
            TraceEnabled = traceEnabled,
            Seed = 1,
        });
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
