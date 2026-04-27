namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal EpochStampSet PathOpen;
    internal EpochStampSet PathClosed;
    internal EpochStampedArray<int> PathCameFrom;
    internal EpochStampedArray<float> PathGScore;
    internal EpochStampedArray<float> PathFScore;
    internal readonly int[] GambitPlanEffectKeys;
    internal readonly int[] GambitPlanRootGambitSlots;
    internal readonly int[] GambitPlanRootTargetIds;
    internal readonly int[] GambitPlanGambitSlots;
    internal readonly int[] GambitPlanTargetIds;
    internal readonly int[] GambitPlanLocalityKeys;
    internal readonly float[] GambitPlanWeights;
    internal readonly float[] GambitPlanAdjustedWeights;
    internal EpochStampSet GambitVisited;

    public GambitProgramScratch(int pathCapacity, int entityCapacity, int gambitObjectiveCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitObjectiveCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new EpochStampSet(pathCapacity);
        PathClosed = new EpochStampSet(pathCapacity);
        PathCameFrom = new EpochStampedArray<int>(pathCapacity);
        PathGScore = new EpochStampedArray<float>(pathCapacity);
        PathFScore = new EpochStampedArray<float>(pathCapacity);
        GambitPlanEffectKeys = new int[gambitObjectiveCapacity + 1];
        GambitPlanRootGambitSlots = new int[gambitObjectiveCapacity + 1];
        GambitPlanRootTargetIds = new int[gambitObjectiveCapacity + 1];
        GambitPlanGambitSlots = new int[gambitObjectiveCapacity + 1];
        GambitPlanTargetIds = new int[gambitObjectiveCapacity + 1];
        GambitPlanLocalityKeys = new int[gambitObjectiveCapacity + 1];
        GambitPlanWeights = new float[gambitObjectiveCapacity + 1];
        GambitPlanAdjustedWeights = new float[gambitObjectiveCapacity + 1];
        GambitVisited = new EpochStampSet(entityCapacity + 1);
    }
}
