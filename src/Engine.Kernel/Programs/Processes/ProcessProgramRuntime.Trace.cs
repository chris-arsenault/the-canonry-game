namespace Engine.Kernel;

internal sealed partial class ProcessProgramRuntime
{
    private void EmitProcessTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int traceSubjectCode,
        int candidateIndex,
        int parentCandidateIndex,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float rawWeight,
        float adjustedWeight)
    {
        simulation.EmitTrace(
            tick,
            ActionFamilyCodes.Process,
            entityId,
            traceSubjectCode,
            candidateIndex,
            parentCandidateIndex,
            ActionFamilyCodes.Process,
            processSlot > 0 ? _processIds[processSlot] : 0,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            rewardOrBaseScore,
            rawWeight,
            adjustedWeight);
    }

    private void EmitGoalTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int objective,
        int processSlot,
        int targetEntityId,
        int statusCode,
        float urgency,
        float weight)
    {
        EmitProcessTrace(
            simulation,
            tick,
            entityId,
            TraceSubjectCodes.Goal,
            objective,
            0,
            processSlot,
            _objectiveEffectScopeCodes[objective],
            _objectiveEffectValueIds[objective],
            _objectiveEffectKindIds[objective],
            targetEntityId,
            statusCode,
            urgency,
            0f,
            weight,
            weight);
    }

    private void EmitActionSummaryTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int parentCandidateIndex,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rawWeight,
        float adjustedWeight)
    {
        EmitProcessTrace(
            simulation,
            tick,
            entityId,
            TraceSubjectCodes.ActionSummary,
            candidateIndex,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            0f,
            rawWeight,
            adjustedWeight);
    }

    private void EmitTargetTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int parentCandidateIndex,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float rawWeight,
        float adjustedWeight)
    {
        EmitProcessTrace(
            simulation,
            tick,
            entityId,
            TraceSubjectCodes.TargetDetail,
            candidateIndex,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            rewardOrBaseScore,
            rawWeight,
            adjustedWeight);
    }

    private void EmitSupportTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int parentCandidateIndex,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float adjustedWeight)
    {
        EmitProcessTrace(
            simulation,
            tick,
            entityId,
            TraceSubjectCodes.Support,
            candidateIndex,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            rewardOrBaseScore,
            0f,
            adjustedWeight);
    }

    private void EmitSupportTraceIfNeeded(
        Simulation simulation,
        int tick,
        int entityId, int candidateIndex, int parentCandidateIndex, int processSlot,
        int effectScopeCode, int effectValueId, int effectKindId, int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float adjustedWeight,
        bool traceEnabled)
    {
        if (!traceEnabled)
        {
            return;
        }

        EmitSupportTrace(
            simulation,
            tick,
            entityId,
            candidateIndex,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            rewardOrBaseScore,
            adjustedWeight);
    }

    private void EmitPlanTargetTraceIfNeeded(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int parentCandidateIndex,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int targetEntityId,
        int statusCode,
        float urgency,
        float rewardOrBaseScore,
        float rawWeight,
        float adjustedWeight,
        bool traceEnabled)
    {
        if (!traceEnabled)
        {
            return;
        }

        EmitTargetTrace(
            simulation,
            tick,
            entityId,
            candidateIndex,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            targetEntityId,
            statusCode,
            urgency,
            rewardOrBaseScore,
            rawWeight,
            adjustedWeight);
    }

    private void EmitUnplannedTargetTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int processSlot,
        int targetEntityId,
        int statusCode,
        float rewardOrBaseScore,
        float weight)
    {
        EmitTargetTrace(
            simulation,
            tick,
            entityId,
            candidateIndex,
            0,
            processSlot,
            0,
            0,
            0,
            targetEntityId,
            statusCode,
            0f,
            rewardOrBaseScore,
            weight,
            weight);
    }

    private void EmitUnplannedTargetTraceIfNeeded(
        Simulation simulation,
        int tick,
        int entityId,
        int candidateIndex,
        int processSlot,
        int targetEntityId,
        int statusCode,
        float rewardOrBaseScore,
        float weight,
        float selectRoll)
    {
        if (selectRoll >= 0f)
        {
            return;
        }

        EmitUnplannedTargetTrace(
            simulation,
            tick,
            entityId,
            candidateIndex,
            processSlot,
            targetEntityId,
            statusCode,
            rewardOrBaseScore,
            weight);
    }

    private bool ShouldSkipPlanProcess(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        bool traceEnabled,
        int parentCandidateIndex)
    {
        if (_processKindIds[processSlot] != kindId ||
            !ProcessServesEffect(processSlot, effectScopeCode, effectValueId, effectKindId))
        {
            return true;
        }

        if (!_processEnabled[processSlot])
        {
            EmitPlanProcessAvailabilityTrace(
                simulation,
                tick,
                entityId,
                processSlot,
                effectScopeCode,
                effectValueId,
                effectKindId,
                TraceStatusCodes.Disabled,
                urgency,
                traceEnabled,
                parentCandidateIndex);
            return true;
        }

        if (tick >= _cooldowns.GetOrDefault(entityId, _processIds[processSlot]))
        {
            return false;
        }

        EmitPlanProcessAvailabilityTrace(
            simulation,
            tick,
            entityId,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            TraceStatusCodes.OnCooldown,
            urgency,
            traceEnabled,
            parentCandidateIndex);
        return true;
    }

    private void EmitPlanProcessAvailabilityTrace(
        Simulation simulation,
        int tick,
        int entityId,
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int statusCode,
        float urgency,
        bool traceEnabled,
        int parentCandidateIndex)
    {
        if (!traceEnabled)
        {
            return;
        }

        EmitActionSummaryTrace(
            simulation,
            tick,
            entityId,
            processSlot,
            parentCandidateIndex,
            processSlot,
            effectScopeCode,
            effectValueId,
            effectKindId,
            0,
            statusCode,
            urgency,
            0f,
            0f);
    }
}
