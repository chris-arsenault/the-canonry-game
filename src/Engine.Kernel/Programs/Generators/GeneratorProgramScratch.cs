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

    public GeneratorProgramScratch(
        int generatorCapacity,
        int scratchCellCapacity,
        int resolvedReferenceCapacity,
        int retryTargetCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scratchCellCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolvedReferenceCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryTargetCapacity);
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
    }
}
