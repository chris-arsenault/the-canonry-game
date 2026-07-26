namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class Simulation
{
    private void WriteKernelEvent(in KernelEvent kernelEvent)
    {
        var contextualEvent = kernelEvent.WithExecutionContext(
            _executionContext.ProgramId,
            _executionContext.SourceCodeId,
            _executionContext.ProvenanceInvocationId);
        if (!_eventPublisher.TryWrite(contextualEvent))
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

    internal void WriteGambitEnabledChanged(int gambitId, int enabled)
    {
        WriteKernelEvent(KernelEvent.From(new GambitEnabledChangedKernelEvent(
            CurrentTick,
            WorldVersion,
            gambitId,
            enabled)));
    }

    internal void EmitGambitEvaluated(
        int tick,
        int entityOccurrenceHandle,
        int kindId,
        int gambitId,
        int candidateCount,
        KernelEventState state)
    {
        WriteKernelEvent(KernelEvent.From(new GambitEvaluatedKernelEvent(
            tick,
            WorldVersion,
            entityOccurrenceHandle,
            kindId,
            gambitId,
            candidateCount,
            state)));
    }

    internal void EmitGambitExecuted(
        int tick,
        int entityOccurrenceHandle,
        int targetOccurrenceHandle,
        int kindId,
        int gambitId,
        KernelEventState state)
    {
        WriteKernelEvent(KernelEvent.From(new GambitExecutedKernelEvent(
            tick,
            WorldVersion,
            entityOccurrenceHandle,
            kindId,
            gambitId,
            entityOccurrenceHandle,
            targetOccurrenceHandle == entityOccurrenceHandle ? 0 : targetOccurrenceHandle,
            targetOccurrenceHandle == entityOccurrenceHandle ? 1 : 2,
            state)));
    }

    internal void EmitGeneratorEvaluated(
        int tick,
        int generatorId,
        int statusCode,
        int selectedTargetOccurrenceHandle,
        int candidateCount,
        int failedFireCount)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            tick,
            WorldVersion,
            generatorId,
            statusCode,
            selectedTargetOccurrenceHandle,
            candidateCount,
            failedFireCount)));
    }

    internal void EmitGeneratorFired(
        int tick,
        int generatorId,
        int selectedTargetOccurrenceHandle,
        int createdEntityCount,
        int createdRelationshipCount,
        int relationshipSpendCount,
        int outputLockRelationshipCreatedCount)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorFiredKernelEvent(
            tick,
            WorldVersion,
            generatorId,
            selectedTargetOccurrenceHandle,
            createdEntityCount,
            createdRelationshipCount,
            relationshipSpendCount,
            outputLockRelationshipCreatedCount)));
    }

    internal void EmitWorldBeatChanged(int tick, int trackCode, int ageId, int phaseCode)
    {
        WriteKernelEvent(KernelEvent.From(new WorldBeatChangedKernelEvent(
            tick,
            WorldVersion,
            trackCode,
            ageId,
            phaseCode)));
    }

    internal void EmitChronicleChanged(
        int tick,
        int storySequence,
        int archetypeId,
        int eventTypeCode,
        int actIndex,
        int outcomeCodeId,
        int roleEntityId0,
        int roleEntityId1,
        int roleEntityId2,
        int roleEntityId3,
        FixedScalar heat)
    {
        WriteKernelEvent(KernelEvent.From(new ChronicleChangedKernelEvent(
            tick,
            WorldVersion,
            storySequence,
            archetypeId,
            eventTypeCode,
            actIndex,
            outcomeCodeId,
            World.EntityOccurrenceHandleOrZero(roleEntityId0),
            World.EntityOccurrenceHandleOrZero(roleEntityId1),
            World.EntityOccurrenceHandleOrZero(roleEntityId2),
            World.EntityOccurrenceHandleOrZero(roleEntityId3),
            heat.Units)));
    }

    internal void EmitGeneratorTargetEvaluated(
        int tick,
        int generatorId,
        int targetEntityId,
        int targetKindId,
        int targetSubtypeId,
        int targetCultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS,
        int statusCode,
        int rejectionReasonCode,
        int physicalFieldBandCode,
        int physicalCellKindCode,
        int semanticFieldBandCode,
        int semanticCellKindCode,
        FixedScalar physicalLocalRawMass,
        FixedScalar physicalConnectedRawMass,
        FixedScalar semanticLocalRawMass,
        FixedScalar semanticConnectedRawMass)
    {
        WriteKernelEvent(KernelEvent.From(new GeneratorTargetEvaluatedKernelEvent(
            tick,
            WorldVersion,
            generatorId,
            World.EntityOccurrenceHandle(targetEntityId),
            targetKindId,
            targetSubtypeId,
            targetCultureId,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS,
            statusCode,
            rejectionReasonCode,
            physicalFieldBandCode,
            physicalCellKindCode,
            semanticFieldBandCode,
            semanticCellKindCode,
            physicalLocalRawMass.Units,
            physicalConnectedRawMass.Units,
            semanticLocalRawMass.Units,
            semanticConnectedRawMass.Units)));
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
        FixedScalar previousQuality,
        FixedScalar currentQuality,
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
                KernelFixedPoint.RoadQualityUnits(previousQuality),
                KernelFixedPoint.RoadQualityUnits(currentQuality))));
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
        FixedScalar previousScore,
        FixedScalar currentScore)
    {
        WriteKernelEvent(KernelEvent.From(new CultureSlotOccupancyChangedKernelEvent(
            tick,
            WorldVersion,
            _executionContext.SourceCodeId,
            cultureId,
            slotId,
            polarityCode,
            World.EntityOccurrenceHandleOrZero(previousEntityId),
            World.EntityOccurrenceHandleOrZero(currentEntityId),
            previousScore.Units,
            currentScore.Units)));
    }

    internal void EmitMinorActorCreated(
        int tick,
        int actorOccurrence,
        int sourceEntityOccurrence,
        int sourceRuleId,
        int bindingRelationshipOccurrence,
        int cultureId,
        int roleCode,
        int planeCode,
        int q,
        int r,
        int postureCode,
        FixedScalar strength)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorCreatedKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            sourceEntityOccurrence,
            sourceRuleId,
            bindingRelationshipOccurrence,
            cultureId,
            roleCode,
            planeCode,
            q,
            r,
            postureCode,
            strength.Units)));
    }

    internal void EmitMinorActorAssigned(
        int tick,
        int actorOccurrence,
        int sourceEntityOccurrence,
        int roleCode,
        int planeCode,
        int previousTargetQ,
        int previousTargetR,
        int targetQ,
        int targetR,
        int targetingCode,
        int postureCode,
        int nextReplanTick,
        int commitmentUntilTick,
        FixedScalar strength)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorAssignedKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            sourceEntityOccurrence,
            roleCode,
            planeCode,
            previousTargetQ,
            previousTargetR,
            targetQ,
            targetR,
            targetingCode,
            postureCode,
            nextReplanTick,
            commitmentUntilTick,
            strength.Units)));
    }

    internal void EmitMinorActorMoved(
        int tick,
        int actorOccurrence,
        int sourceEntityOccurrence,
        int cultureId,
        int roleCode,
        int planeCode,
        int fromQ,
        int fromR,
        int toQ,
        int toR,
        int targetQ,
        int targetR,
        int postureCode,
        FixedScalar strength)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorMovedKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            sourceEntityOccurrence,
            cultureId,
            roleCode,
            planeCode,
            fromQ,
            fromR,
            toQ,
            toR,
            targetQ,
            targetR,
            postureCode,
            strength.Units)));
    }

    internal void EmitMinorActorWorked(
        int tick,
        int actorOccurrence,
        int sourceEntityOccurrence,
        int sourceRuleId,
        int sourceWorkTier,
        int effectiveRadius,
        int cultureId,
        int roleCode,
        int actionCode,
        int targetingCode,
        int planeCode,
        int q,
        int r,
        int cellKindCode,
        FixedScalar localRawMass,
        FixedScalar connectedRawMass,
        FixedScalar strength,
        FixedScalar delta)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorWorkedKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            sourceEntityOccurrence,
            sourceRuleId,
            sourceWorkTier,
            effectiveRadius,
            cultureId,
            roleCode,
            actionCode,
            targetingCode,
            planeCode,
            q,
            r,
            cellKindCode,
            localRawMass.Units,
            connectedRawMass.Units,
            strength.Units,
            delta.Units)));
    }

    internal void EmitMinorActorEncountered(
        int tick,
        int actorOccurrence,
        int otherActorOccurrence,
        int actorCultureId,
        int otherCultureId,
        int actorRoleCode,
        int otherRoleCode,
        int planeCode,
        int q,
        int r,
        int encounterCode,
        int actorPostureCode,
        int otherPostureCode,
        FixedScalar actorStrength,
        FixedScalar otherStrength,
        FixedScalar effect)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorEncounteredKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            otherActorOccurrence,
            actorCultureId,
            otherCultureId,
            actorRoleCode,
            otherRoleCode,
            planeCode,
            q,
            r,
            encounterCode,
            actorPostureCode,
            otherPostureCode,
            actorStrength.Units,
            otherStrength.Units,
            effect.Units)));
    }

    internal void EmitMinorActorRetired(
        int tick,
        int actorOccurrence,
        int sourceEntityOccurrence,
        int sourceRuleId,
        int bindingRelationshipOccurrence,
        int cultureId,
        int roleCode,
        int planeCode,
        int q,
        int r,
        int reasonCode,
        int postureCode,
        FixedScalar strength)
    {
        WriteKernelEvent(KernelEvent.From(new MinorActorRetiredKernelEvent(
            tick,
            WorldVersion,
            actorOccurrence,
            sourceEntityOccurrence,
            sourceRuleId,
            bindingRelationshipOccurrence,
            cultureId,
            roleCode,
            planeCode,
            q,
            r,
            reasonCode,
            postureCode,
            strength.Units)));
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
        FixedScalar urgency,
        FixedScalar rewardOrBaseScore,
        FixedScalar rawWeight,
        FixedScalar adjustedWeight,
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
        FixedScalar affordanceScore = default,
        FixedScalar affordanceBonus = default)
    {
        if (!TraceEnabled())
        {
            return;
        }

        WriteKernelEvent(KernelEvent.From(new TraceKernelEvent(
            tick,
            WorldVersion,
            intentId,
            World.EntityOccurrenceHandle(entityId),
            traceSubjectCode,
            candidateIndex,
            parentCandidateIndex,
            actionFamilyCode,
            actionId,
            effectScopeCode,
            effectValueId,
            effectKindId,
            World.EntityOccurrenceHandleOrZero(targetEntityId),
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
            urgency.Units,
            rewardOrBaseScore.Units,
            rawWeight.Units,
            adjustedWeight.Units,
            affordanceScore.Units,
            affordanceBonus.Units)));
    }

    private void WriteTickCompleted()
    {
        WriteKernelEvent(KernelEvent.From(new TickCompletedKernelEvent(
            CurrentTick,
            WorldVersion)));
    }

    private void WritePhaseCompleted(
        int phaseCode,
        int workItemCount,
        long mutationCount,
        long phaseRevision)
    {
        WriteKernelEvent(KernelEvent.From(new PhaseCompletedKernelEvent(
            CurrentTick,
            WorldVersion,
            phaseCode,
            workItemCount,
            phaseRevision,
            mutationCount)));
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
