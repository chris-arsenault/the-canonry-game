namespace Engine.Observability.Tests.Observability;

using Engine.Constants;
using Engine.Eventing;
using Engine.Observability;
using Engine.Projection;

public sealed class PlannerTraceProjectionTests
{
    [Fact]
    public void Projection_ReassemblesPlannerTraceRowsFromPrimitiveEvents()
    {
        var log = new KernelEventLog(capacity: 32);
        var batch = new TelemetryBatch();
        var projection = new PlannerTraceProjection(1, batch);
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new PlanStartedKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            ReasonCodeId: 50,
            CandidateCount: 3))));
        Assert.True(log.TryWrite(KernelEvent.From(new GoalConsideredKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            GoalId: 30,
            CandidateIndex: 0,
            PreliminaryScore: 0.25f))));
        Assert.True(log.TryWrite(KernelEvent.From(new GoalScoredKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            GoalId: 30,
            ReasonCodeId: 51,
            ScoringPassIndex: 1,
            ScoreDelta: 0.75f,
            RunningTotal: 1.0f))));
        Assert.True(log.TryWrite(KernelEvent.From(new GoalSelectedKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            GoalId: 30,
            FinalScore: 1.0f))));
        Assert.True(log.TryWrite(KernelEvent.From(new PlanStepEmittedKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            GoalId: 30,
            StepIndex: 0,
            ProcessId: 40,
            TargetEntityId: 12,
            RelationshipKindId: 60))));
        Assert.True(log.TryWrite(KernelEvent.From(new PlanCommittedKernelEvent(
            Tick: 7,
            WorldVersion: 10,
            PlanId: 2,
            EntityId: 11,
            StepCount: 1,
            ReasonCodeId: 50,
            TotalScore: 1.0f))));

        pump.DrainAvailable();

        var trace = Assert.Single(batch.PlannerTraces);
        Assert.Equal(PlannerTraceStatusCodes.Committed, trace.StatusCode);
        Assert.Equal(3, trace.CandidateCount);
        Assert.Equal(50, trace.ReasonCodeId);
        Assert.Equal(1.0f, trace.TotalScore);

        var goal = Assert.Single(batch.PlannerTraceGoals);
        Assert.Equal(30, goal.GoalId);
        Assert.True(goal.WasSelected);
        Assert.Equal(1.0f, goal.FinalScore);

        var score = Assert.Single(batch.PlannerTraceScores);
        Assert.Equal(51, score.ReasonCodeId);
        Assert.Equal(1.0f, score.RunningTotal);

        var step = Assert.Single(batch.PlannerTraceSteps);
        Assert.Equal(40, step.ProcessId);
        Assert.Equal(60, step.RelationshipKindId);
    }

    [Fact]
    public void Projection_TruncatesOpenTraceWhenEventsDrop()
    {
        var log = new KernelEventLog(capacity: 1);
        var batch = new TelemetryBatch();
        var projection = new PlannerTraceProjection(1, batch);
        using var pump = new KernelEventProjectionPump(
            log,
            KernelEventCursorStart.EarliestAvailable,
            projection);

        Assert.True(log.TryWrite(KernelEvent.From(new PlanStartedKernelEvent(
            Tick: 1,
            WorldVersion: 1,
            PlanId: 1,
            EntityId: 10,
            ReasonCodeId: 0,
            CandidateCount: 1))));
        pump.DrainAvailable();

        Assert.True(log.TryWrite(KernelEvent.From(new PlanStartedKernelEvent(
            Tick: 2,
            WorldVersion: 2,
            PlanId: 2,
            EntityId: 11,
            ReasonCodeId: 0,
            CandidateCount: 1))));
        Assert.True(log.TryWrite(KernelEvent.From(new PlanStartedKernelEvent(
            Tick: 3,
            WorldVersion: 3,
            PlanId: 3,
            EntityId: 12,
            ReasonCodeId: 0,
            CandidateCount: 1))));

        pump.DrainAvailable();

        Assert.Contains(batch.PlannerTraces, trace => trace.StatusCode == PlannerTraceStatusCodes.Truncated);
    }
}
