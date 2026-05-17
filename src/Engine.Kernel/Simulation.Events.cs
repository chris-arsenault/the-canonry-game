namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class Simulation
{
    private void WriteKernelEvent(in KernelEvent kernelEvent)
    {
        var contextualEvent = kernelEvent.WithExecutionContext(
            _executionContext.ProgramId,
            _executionContext.SourceCodeId,
            _executionContext.ProvenanceInvocationId);
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

    internal void EmitProvenanceInvocationRecorded(
        int tick,
        int invocationId,
        int producerKindCode,
        int producerId,
        int primaryEntityId,
        int secondaryEntityId,
        int parentInvocationId,
        int triggerKindCode,
        int triggerId,
        int outputCount)
    {
        WriteKernelEvent(KernelEvent.From(new ProvenanceInvocationRecordedKernelEvent(
            tick,
            WorldVersion,
            invocationId,
            producerKindCode,
            producerId,
            primaryEntityId,
            secondaryEntityId,
            parentInvocationId,
            triggerKindCode,
            triggerId,
            outputCount)));
    }

    internal void EmitCellRoadChanged(
        int tick,
        int planeCode,
        int q,
        int r,
        float previousQuality,
        float currentQuality,
        int travelCount)
    {
        WriteKernelEvent(KernelEvent.From(new CellRoadChangedKernelEvent(
            tick,
            WorldVersion,
            planeCode,
            q,
            r,
            _executionContext.SourceCodeId,
            travelCount,
                previousQuality,
                currentQuality)));
    }

    internal void EmitCellTravelStructureChanged(
        int tick,
        int planeCode,
        int q,
        int r,
        int previousTravelStructureId,
        int currentTravelStructureId)
    {
        WriteKernelEvent(KernelEvent.From(new CellTravelStructureChangedKernelEvent(
            tick,
            WorldVersion,
            planeCode,
            q,
            r,
            _executionContext.SourceCodeId,
            previousTravelStructureId,
            currentTravelStructureId)));
    }

    internal void EmitCultureSlotOccupancyChanged(
        int tick,
        int cultureId,
        int slotId,
        int polarityCode,
        int previousEntityId,
        int currentEntityId,
        float previousScore,
        float currentScore)
    {
        WriteKernelEvent(KernelEvent.From(new CultureSlotOccupancyChangedKernelEvent(
            tick,
            WorldVersion,
            _executionContext.SourceCodeId,
            cultureId,
            slotId,
            polarityCode,
            previousEntityId,
            currentEntityId,
            previousScore,
            currentScore)));
    }

    internal bool TraceEnabled() => Options.TraceEnabled;

    internal void EmitTrace(
        int tick,
        int intentId,
        int entityId,
        int traceSubjectCode,
        int candidateIndex,
        int parentCandidateIndex,
        int actionFamilyCode,
        int actionId,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float rawWeight,
        float adjustedWeight,
        int affordanceSourceKindCode = 0,
        int affordanceSourceId = 0,
        int affordanceShapeCode = 0,
        int affordanceSourceKindMask = 0,
        int affordanceShapeMask = 0,
        int affordanceContributionCount = 0,
        int generatorAffordanceCount = 0,
        int gambitAffordanceCount = 0,
        int hiddenAffordanceCount = 0,
        int selectionBucketCode = 0,
        float affordanceScore = 0f,
        float affordanceBonus = 0f)
    {
        if (!TraceEnabled())
        {
            return;
        }

        WriteKernelEvent(KernelEvent.From(new TraceKernelEvent(
            tick,
            WorldVersion,
            intentId,
            entityId,
            traceSubjectCode,
            candidateIndex,
            parentCandidateIndex,
            actionFamilyCode,
            actionId,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            affordanceSourceKindCode,
            affordanceSourceId,
            affordanceShapeCode,
            affordanceSourceKindMask,
            affordanceShapeMask,
            affordanceContributionCount,
            generatorAffordanceCount,
            gambitAffordanceCount,
            hiddenAffordanceCount,
            selectionBucketCode,
            urgency,
            rewardOrBaseScore,
            rawWeight,
            adjustedWeight,
            affordanceScore,
            affordanceBonus)));
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
