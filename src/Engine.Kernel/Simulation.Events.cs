namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    private void WriteKernelEvent(in KernelEvent kernelEvent)
    {
        var contextualEvent = kernelEvent.WithExecutionContext(
            _executionContext.ProgramId,
            _executionContext.SourceCodeId);
        if (!Events.TryWrite(contextualEvent))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    internal void WriteGeneratorEnabledChanged(int generatorId, int enabled)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorEnabledChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            generatorId,
            enabled)));
    }

    internal void EmitProcessEvaluated(
        int tick,
        int entityId,
        int kindId,
        int selectedGoalId,
        int processId,
        int candidateCount,
        KernelEventState state)
    {
        WriteKernelEvent(KernelEvent.From(new ProcessEvaluatedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            kindId,
            selectedGoalId,
            processId,
            candidateCount,
            state)));
    }

    internal void EmitProcessExecuted(
        int tick,
        int entityId,
        int targetId,
        int kindId,
        int processId)
    {
        WriteKernelEvent(KernelEvent.From(new ProcessExecutedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            kindId,
            processId,
            entityId,
            targetId == entityId ? 0 : targetId,
            targetId == entityId ? 1 : 2,
            KernelEventState.Succeeded)));
    }

    internal void EmitGambitEvaluated(
        int tick,
        int entityId,
        int kindId,
        int gambitId,
        int candidateCount,
        KernelEventState state)
    {
        WriteKernelEvent(KernelEvent.From(new GambitEvaluatedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            kindId,
            gambitId,
            candidateCount,
            state)));
    }

    internal void EmitGambitExecuted(
        int tick,
        int entityId,
        int targetId,
        int kindId,
        int gambitId,
        KernelEventState state)
    {
        WriteKernelEvent(KernelEvent.From(new GambitExecutedKernelEvent(
            tick,
            WorldVersion,
            entityId,
            kindId,
            gambitId,
            entityId,
            targetId == entityId ? 0 : targetId,
            targetId == entityId ? 1 : 2,
            state)));
    }

    internal void EmitGeneratorEvaluated(
        int tick,
        int generatorId,
        int statusCode,
        int selectedTargetEntityId,
        int candidateCount,
        int failedFireCount)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            tick,
            WorldVersion,
            generatorId,
            statusCode,
            selectedTargetEntityId,
            candidateCount,
            failedFireCount)));
    }

    internal void EmitGeneratorFired(
        int tick,
        int generatorId,
        int selectedTargetEntityId,
        int createdEntityCount,
        int createdRelationshipCount)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorFiredKernelEvent(
            tick,
            WorldVersion,
            generatorId,
            selectedTargetEntityId,
            createdEntityCount,
            createdRelationshipCount)));
    }

    internal void EmitDegeneracyDetected(
        int tick,
        int detectorId,
        int severityCode,
        int reasonCodeId)
    {
        WriteKernelEvent(KernelEvent.From(new DegeneracyDetectedKernelEvent(
            tick,
            WorldVersion,
            detectorId,
            severityCode,
            reasonCodeId)));
    }

    internal void EmitPlanStarted(
        int tick,
        int planId,
        int entityId,
        int reasonCodeId,
        int candidateCount)
    {
        WriteKernelEvent(KernelEvent.From(new PlanStartedKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            reasonCodeId,
            candidateCount)));
    }

    internal void EmitGoalConsidered(
        int tick,
        int planId,
        int entityId,
        int goalId,
        int candidateIndex,
        float preliminaryScore)
    {
        WriteKernelEvent(KernelEvent.From(new GoalConsideredKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            goalId,
            candidateIndex,
            preliminaryScore)));
    }

    internal void EmitGoalScored(
        int tick,
        int planId,
        int entityId,
        int goalId,
        int reasonCodeId,
        int scoringPassIndex,
        float scoreDelta,
        float runningTotal)
    {
        WriteKernelEvent(KernelEvent.From(new GoalScoredKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            goalId,
            reasonCodeId,
            scoringPassIndex,
            scoreDelta,
            runningTotal)));
    }

    internal void EmitGoalSelected(
        int tick,
        int planId,
        int entityId,
        int goalId,
        float finalScore)
    {
        WriteKernelEvent(KernelEvent.From(new GoalSelectedKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            goalId,
            finalScore)));
    }

    internal void EmitPlanStepEmitted(
        int tick,
        int planId,
        int entityId,
        int goalId,
        int stepIndex,
        int processId,
        int targetEntityId,
        int relationshipKindId)
    {
        WriteKernelEvent(KernelEvent.From(new PlanStepEmittedKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            goalId,
            stepIndex,
            processId,
            targetEntityId,
            relationshipKindId)));
    }

    internal void EmitPlanCommitted(
        int tick,
        int planId,
        int entityId,
        int stepCount,
        int reasonCodeId,
        float totalScore)
    {
        WriteKernelEvent(KernelEvent.From(new PlanCommittedKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            stepCount,
            reasonCodeId,
            totalScore)));
    }

    internal void EmitPlanAbandoned(
        int tick,
        int planId,
        int entityId,
        int reasonCodeId)
    {
        WriteKernelEvent(KernelEvent.From(new PlanAbandonedKernelEvent(
            tick,
            WorldVersion,
            planId,
            entityId,
            reasonCodeId)));
    }

    private void WriteTickCompleted()
    {
        WriteKernelEvent(KernelEvent.From(new TickCompletedKernelEvent(
            CurrentTick,
            WorldVersion)));
    }

    private void WriteCommandApplied(in KernelCommand command)
    {
        WriteKernelEvent(KernelEvent.From(new CommandAppliedKernelEvent(
            CurrentTick,
            WorldVersion,
            command.CommandId,
            command.CommandType,
            command.IssueVersion)));
    }

    private void WriteCommandRejected(in KernelCommand command, int reasonCodeId)
    {
        WriteKernelEvent(KernelEvent.From(new CommandRejectedKernelEvent(
            CurrentTick,
            WorldVersion,
            command.CommandId,
            command.CommandType,
            command.IssueVersion,
            reasonCodeId)));
    }
}
