namespace Engine.Kernel;

internal sealed class GeneratorProgramScratch
{
    internal readonly int[] PulseStatusCodes;
    internal readonly int[] PulseCandidateCounts;
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

    public GeneratorProgramScratch(int generatorCapacity, int scratchCellCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scratchCellCapacity);
        PulseStatusCodes = new int[generatorCapacity + 1];
        PulseCandidateCounts = new int[generatorCapacity + 1];
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
    }
}
