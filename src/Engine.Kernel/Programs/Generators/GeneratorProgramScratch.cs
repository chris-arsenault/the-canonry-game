namespace Engine.Kernel;

internal sealed class GeneratorProgramScratch
{
    internal readonly int[] PulseStatusCodes;
    internal readonly int[] PulseCandidateCounts;
    internal readonly int[] PulseSelectionWeights;
    internal readonly int[] PulseSelectedTargetIds;
    internal readonly int[] PulseFailedFireCounts;
    internal readonly int[] Q;
    internal readonly int[] R;
    internal readonly int[] Score;
    internal readonly int[] VisitedQ;
    internal readonly int[] VisitedR;
    internal readonly int[] ClaimedQ;
    internal readonly int[] ClaimedR;
    internal readonly int[] ConnectivityQ;
    internal readonly int[] ConnectivityR;
    internal readonly int[] ConnectivityQueueQ;
    internal readonly int[] ConnectivityQueueR;
    internal readonly int[] RefCodes;
    internal readonly int[] RefEntityIds;
    internal readonly int[] CreatedEntityIds;
    internal readonly int[] RetryRejectedTargetIds;
    internal readonly int[] PlannedProducedSlots;
    internal readonly int[] PlannedSubtypeIds;
    internal readonly int[] PlannedCultureIds;
    internal readonly int[] PlannedSpawnPhysicalQ;
    internal readonly int[] PlannedSpawnPhysicalR;
    internal readonly int[] PlannedFinalPhysicalQ;
    internal readonly int[] PlannedFinalPhysicalR;
    internal readonly int[] PlannedSemanticQ;
    internal readonly int[] PlannedSemanticR;
    internal readonly int[] PlannedCoverageStarts;
    internal readonly int[] PlannedCoverageCounts;
    internal readonly int[] PlannedCoverageCellQ;
    internal readonly int[] PlannedCoverageCellR;

    public GeneratorProgramScratch(
        int generatorCapacity,
        int scratchCellCapacity,
        int resolvedReferenceCapacity,
        int retryTargetCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(generatorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(scratchCellCapacity);
        NumericGuard.ThrowIfNegativeOrZero(resolvedReferenceCapacity);
        NumericGuard.ThrowIfNegativeOrZero(retryTargetCapacity);
        PulseStatusCodes = new int[generatorCapacity + 1];
        PulseCandidateCounts = new int[generatorCapacity + 1];
        PulseSelectionWeights = new int[generatorCapacity + 1];
        PulseSelectedTargetIds = new int[generatorCapacity + 1];
        PulseFailedFireCounts = new int[generatorCapacity + 1];
        Q = new int[scratchCellCapacity];
        R = new int[scratchCellCapacity];
        Score = new int[scratchCellCapacity];
        VisitedQ = new int[scratchCellCapacity];
        VisitedR = new int[scratchCellCapacity];
        ClaimedQ = new int[scratchCellCapacity];
        ClaimedR = new int[scratchCellCapacity];
        ConnectivityQ = new int[scratchCellCapacity];
        ConnectivityR = new int[scratchCellCapacity];
        ConnectivityQueueQ = new int[scratchCellCapacity];
        ConnectivityQueueR = new int[scratchCellCapacity];
        RefCodes = new int[resolvedReferenceCapacity];
        RefEntityIds = new int[resolvedReferenceCapacity];
        CreatedEntityIds = new int[resolvedReferenceCapacity];
        RetryRejectedTargetIds = new int[retryTargetCapacity];
        PlannedProducedSlots = new int[resolvedReferenceCapacity];
        PlannedSubtypeIds = new int[resolvedReferenceCapacity];
        PlannedCultureIds = new int[resolvedReferenceCapacity];
        PlannedSpawnPhysicalQ = new int[resolvedReferenceCapacity];
        PlannedSpawnPhysicalR = new int[resolvedReferenceCapacity];
        PlannedFinalPhysicalQ = new int[resolvedReferenceCapacity];
        PlannedFinalPhysicalR = new int[resolvedReferenceCapacity];
        PlannedSemanticQ = new int[resolvedReferenceCapacity];
        PlannedSemanticR = new int[resolvedReferenceCapacity];
        PlannedCoverageStarts = new int[resolvedReferenceCapacity];
        PlannedCoverageCounts = new int[resolvedReferenceCapacity];
        PlannedCoverageCellQ = new int[scratchCellCapacity];
        PlannedCoverageCellR = new int[scratchCellCapacity];
    }
}
