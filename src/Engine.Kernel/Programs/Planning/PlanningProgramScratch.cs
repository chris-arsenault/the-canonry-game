namespace Engine.Kernel;

internal sealed class PlanningProgramScratch
{
    internal readonly int[] HeaderPlannerKindCodes;
    internal readonly int[] HeaderActionFamilyCodes;
    internal readonly int[] HeaderActorEntityIds;
    internal readonly int[] HeaderActorKindIds;
    internal readonly int[] HeaderObjectiveEffectKeys;
    internal readonly int[] HeaderStepStarts;
    internal readonly int[] HeaderStepCounts;
    internal readonly int[] HeaderImmediateStepIndexes;
    internal readonly int[] HeaderRootStepIndexes;
    internal readonly int[] HeaderRootTargetEntityIds;
    internal readonly int[] HeaderLocalityKeys;
    internal readonly int[] HeaderCandidateCounts;
    internal readonly float[] HeaderScores;
    internal readonly int[] HeaderStatusCodes;
    internal readonly int[] StepActionFamilyCodes;
    internal readonly int[] StepActionSlots;
    internal readonly int[] StepTargetEntityIds;
    internal readonly int[] StepSatisfiesEffectKeys;
    internal readonly int[] StepParentStepIndexes;
    internal readonly int[] StepRoleCodes;
    internal readonly float[] StepScores;
    internal int HeaderCount;
    internal int StepCount;

    public PlanningProgramScratch(int planCapacity, int planStepCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(planStepCapacity);
        HeaderPlannerKindCodes = new int[planCapacity + 1];
        HeaderActionFamilyCodes = new int[planCapacity + 1];
        HeaderActorEntityIds = new int[planCapacity + 1];
        HeaderActorKindIds = new int[planCapacity + 1];
        HeaderObjectiveEffectKeys = new int[planCapacity + 1];
        HeaderStepStarts = new int[planCapacity + 1];
        HeaderStepCounts = new int[planCapacity + 1];
        HeaderImmediateStepIndexes = new int[planCapacity + 1];
        HeaderRootStepIndexes = new int[planCapacity + 1];
        HeaderRootTargetEntityIds = new int[planCapacity + 1];
        HeaderLocalityKeys = new int[planCapacity + 1];
        HeaderCandidateCounts = new int[planCapacity + 1];
        HeaderScores = new float[planCapacity + 1];
        HeaderStatusCodes = new int[planCapacity + 1];
        StepActionFamilyCodes = new int[planStepCapacity + 1];
        StepActionSlots = new int[planStepCapacity + 1];
        StepTargetEntityIds = new int[planStepCapacity + 1];
        StepSatisfiesEffectKeys = new int[planStepCapacity + 1];
        StepParentStepIndexes = new int[planStepCapacity + 1];
        StepRoleCodes = new int[planStepCapacity + 1];
        StepScores = new float[planStepCapacity + 1];
    }

    internal void Reset()
    {
        HeaderCount = 0;
        StepCount = 0;
    }
}
