namespace Engine.Kernel.Tests;

using Engine.Constants;
using Engine.Eventing;
using Engine.Kernel;

public sealed class PlanningProgramRuntimeTests
{
    [Fact]
    public void PlannerKindFor_UsesEntityOverrideBeforeKindOverrideBeforeDefault()
    {
        var runtime = CreateRuntime();
        runtime.SetPlannerKind(PlannerKindCodes.Goap);
        runtime.SetKindPlannerKind(kindId: 7, PlannerKindCodes.Opportunity);
        runtime.SetEntityPlannerKind(entityId: 42, PlannerKindCodes.Htn);

        Assert.Equal(PlannerKindCodes.Htn, runtime.PlannerKindFor(actorKindId: 7, actorEntityId: 42));
        Assert.Equal(PlannerKindCodes.Opportunity, runtime.PlannerKindFor(actorKindId: 7, actorEntityId: 99));
        Assert.Equal(PlannerKindCodes.Goap, runtime.PlannerKindFor(actorKindId: 8, actorEntityId: 99));
    }

    [Fact]
    public void Constructor_AppliesConfiguredPlanningOverrides()
    {
        var runtime = CreateRuntime(new KernelProgramTuningOptions
        {
            Planning = new PlanningProgramTuningOptions
            {
                DefaultPlannerKindCode = PlannerKindCodes.Opportunity,
                KindPlannerOverrides =
                [
                    new PlanningKindPlannerOverride(7, PlannerKindCodes.Goap),
                ],
                EntityPlannerOverrides =
                [
                    new PlanningEntityPlannerOverride(42, PlannerKindCodes.Htn),
                ],
            },
        });

        Assert.Equal(PlannerKindCodes.Opportunity, runtime.PlannerKindFor(actorKindId: 8, actorEntityId: 99));
        Assert.Equal(PlannerKindCodes.Goap, runtime.PlannerKindFor(actorKindId: 7, actorEntityId: 99));
        Assert.Equal(PlannerKindCodes.Htn, runtime.PlannerKindFor(actorKindId: 7, actorEntityId: 42));
    }

    [Fact]
    public void WriteSelectedPlan_RecordsImmediateAndRootSteps()
    {
        var runtime = CreateRuntime();
        var request = new PlanningRequest(
            PlannerKindCodes.Goap,
            ActionFamilyCodes.LowAltitudeVerb,
            ActorEntityId: 17,
            ActorKindId: 7,
            Tick: 5,
            SelectionRoll: 0.25f);

        var decision = runtime.WriteSelectedPlan(
            request,
            objectiveEffectKey: 88,
            rootActionSlot: 9,
            rootTargetEntityId: 44,
            immediateActionSlot: 3,
            immediateTargetEntityId: 22,
            localityKey: 66,
            candidateCount: 4,
            score: 12.5f);

        Assert.Equal(PlanningStatusCodes.Succeeded, decision.StatusCode);
        Assert.Equal(new PlanningActionHandle(ActionFamilyCodes.LowAltitudeVerb, 3), decision.ImmediateAction);
        Assert.Equal(22, decision.ImmediateTargetEntityId);
        Assert.Equal(4, decision.CandidateCount);
        Assert.Equal(12.5f, decision.Score);

        var header = runtime.PlanHeader(decision.Plan);
        Assert.Equal(PlannerKindCodes.Goap, header.PlannerKindCode);
        Assert.Equal(ActionFamilyCodes.LowAltitudeVerb, header.ActionFamilyCode);
        Assert.Equal(17, header.ActorEntityId);
        Assert.Equal(7, header.ActorKindId);
        Assert.Equal(88, header.ObjectiveEffectKey);
        Assert.Equal(2, header.StepCount);
        Assert.Equal(0, header.ImmediateStepIndex);
        Assert.Equal(1, header.RootStepIndex);
        Assert.Equal(44, header.RootTargetEntityId);
        Assert.Equal(66, header.LocalityKey);
        Assert.Equal(PlanningStatusCodes.Succeeded, header.StatusCode);

        var immediate = runtime.PlanStep(decision.Plan, 0);
        Assert.Equal(new PlanningActionHandle(ActionFamilyCodes.LowAltitudeVerb, 3), immediate.Action);
        Assert.Equal(22, immediate.TargetEntityId);
        Assert.Equal(0, immediate.SatisfiesEffectKey);
        Assert.Equal(1, immediate.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Immediate, immediate.StepRoleCode);

        var root = runtime.PlanStep(decision.Plan, 1);
        Assert.Equal(new PlanningActionHandle(ActionFamilyCodes.LowAltitudeVerb, 9), root.Action);
        Assert.Equal(44, root.TargetEntityId);
        Assert.Equal(88, root.SatisfiesEffectKey);
        Assert.Equal(-1, root.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Root, root.StepRoleCode);
    }

    [Fact]
    public void WriteSelectedPlan_RecordsSingleStepWhenImmediateActionIsRoot()
    {
        var runtime = CreateRuntime();
        var request = new PlanningRequest(
            PlannerKindCodes.Goap,
            ActionFamilyCodes.LowAltitudeVerb,
            ActorEntityId: 17,
            ActorKindId: 7,
            Tick: 5,
            SelectionRoll: 0.25f);

        var decision = runtime.WriteSelectedPlan(
            request,
            objectiveEffectKey: 88,
            rootActionSlot: 3,
            rootTargetEntityId: 22,
            immediateActionSlot: 3,
            immediateTargetEntityId: 22,
            localityKey: 66,
            candidateCount: 4,
            score: 12.5f);

        var header = runtime.PlanHeader(decision.Plan);
        Assert.Equal(1, header.StepCount);
        Assert.Equal(0, header.RootStepIndex);
        Assert.Equal(22, header.RootTargetEntityId);

        var step = runtime.PlanStep(decision.Plan, 0);
        Assert.Equal(new PlanningActionHandle(ActionFamilyCodes.LowAltitudeVerb, 3), step.Action);
        Assert.Equal(-1, step.ParentStepIndex);
        Assert.Equal(PlanningStepRoleCodes.Immediate, step.StepRoleCode);
    }

    [Fact]
    public void PlanHeaderAndPlanStep_RejectUnknownPlanHandlesAndIndexes()
    {
        var runtime = CreateRuntime();
        var request = new PlanningRequest(
            PlannerKindCodes.Goap,
            ActionFamilyCodes.LowAltitudeVerb,
            ActorEntityId: 17,
            ActorKindId: 7,
            Tick: 5,
            SelectionRoll: 0.25f);

        Assert.Throws<InvalidOperationException>(() => runtime.PlanHeader(default));
        var decision = runtime.WriteSelectedPlan(
            request,
            objectiveEffectKey: 88,
            rootActionSlot: 3,
            rootTargetEntityId: 22,
            immediateActionSlot: 3,
            immediateTargetEntityId: 22,
            localityKey: 66,
            candidateCount: 4,
            score: 12.5f);

        Assert.Throws<InvalidOperationException>(() => runtime.PlanHeader(new PlanningPlanHandle(99)));
        Assert.Throws<InvalidOperationException>(() => runtime.PlanStep(decision.Plan, 1));
    }

    [Fact]
    public void Plan_EmitsStartedAndAbandonedEventsForUnsupportedActionFamily()
    {
        var simulation = new Simulation(new SimulationOptions { EventCapacity = 32 });

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCodes.Goap,
                ActionFamilyCodes.LowAltitudeVerb,
                ActorEntityId: 17,
                ActorKindId: 7,
                Tick: 33,
                SelectionRoll: 0.25f));

        Assert.Equal(PlanningStatusCodes.UnsupportedActionFamily, decision.StatusCode);
        var events = ReadEvents(simulation);
        var started = Assert.Single(events, evt => evt.EventType == KernelEventType.PlanStarted).AsPlanStarted();
        var abandoned = Assert.Single(events, evt => evt.EventType == KernelEventType.PlanAbandoned).AsPlanAbandoned();

        Assert.Equal(33, started.Tick);
        Assert.Equal(ActionFamilyCodes.LowAltitudeVerb, started.PlanId);
        Assert.Equal(17, started.EntityId);
        Assert.Equal(PlanningStatusCodes.UnsupportedActionFamily, started.ReasonCodeId);
        Assert.Equal(0, started.CandidateCount);
        Assert.Equal(ActionFamilyCodes.LowAltitudeVerb, abandoned.PlanId);
        Assert.Equal(17, abandoned.EntityId);
        Assert.Equal(PlanningStatusCodes.UnsupportedActionFamily, abandoned.ReasonCodeId);
    }

    [Fact]
    public void Plan_UsesDefaultPlannerWhenRequestPlannerKindIsZero()
    {
        var simulation = new Simulation(new SimulationOptions
        {
            EventCapacity = 32,
            KernelTuning = new KernelTuningOptions
            {
                Programs = new KernelProgramTuningOptions
                {
                    Planning = new PlanningProgramTuningOptions
                    {
                        DefaultPlannerKindCode = PlannerKindCodes.Htn,
                    },
                },
            },
        });

        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                PlannerKindCode: 0,
                ActionFamilyCode: ActionFamilyCodes.Gambit,
                ActorEntityId: 17,
                ActorKindId: 7,
                Tick: 33,
                SelectionRoll: 0.25f));

        Assert.Equal(PlanningStatusCodes.UnsupportedPlanner, decision.StatusCode);
        var abandoned = Assert.Single(ReadEvents(simulation), evt => evt.EventType == KernelEventType.PlanAbandoned)
            .AsPlanAbandoned();
        Assert.Equal(PlanningStatusCodes.UnsupportedPlanner, abandoned.ReasonCodeId);
    }

    private static PlanningProgramRuntime CreateRuntime(KernelProgramTuningOptions? tuning = null) =>
        new(
            new PlanningProgramData(PlannerKindCodes.Goap),
            new PlanningProgramState(overrideCapacity: 8),
            new PlanningProgramScratch(planCapacity: 4, planStepCapacity: 8),
            tuning ?? new KernelProgramTuningOptions());

    private static KernelEvent[] ReadEvents(Simulation simulation)
    {
        using var cursor = simulation.Events.CreateCursor(KernelEventCursorStart.EarliestAvailable);
        var events = new List<KernelEvent>();
        while (simulation.Events.TryRead(cursor, out var sequenced))
        {
            events.Add(sequenced.Event);
        }

        return events.ToArray();
    }
}
