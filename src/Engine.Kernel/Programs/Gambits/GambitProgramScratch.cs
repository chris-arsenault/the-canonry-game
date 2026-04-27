namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal readonly bool[] PathOpen;
    internal readonly bool[] PathClosed;
    internal readonly int[] PathCameFrom;
    internal readonly float[] PathGScore;
    internal readonly float[] PathFScore;
    internal readonly int[] GambitPlanEffectKeys;
    internal readonly int[] GambitPlanRootGambitSlots;
    internal readonly int[] GambitPlanRootTargetIds;
    internal readonly int[] GambitPlanGambitSlots;
    internal readonly int[] GambitPlanTargetIds;
    internal readonly int[] GambitPlanLocalityKeys;
    internal readonly float[] GambitPlanWeights;
    internal readonly float[] GambitPlanAdjustedWeights;
    internal readonly int[] GambitVisitedEpochs;
    internal int GambitVisitedEpoch;

    public GambitProgramScratch(int pathCapacity, int entityCapacity, int gambitObjectiveCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitObjectiveCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new bool[pathCapacity];
        PathClosed = new bool[pathCapacity];
        PathCameFrom = new int[pathCapacity];
        PathGScore = new float[pathCapacity];
        PathFScore = new float[pathCapacity];
        GambitPlanEffectKeys = new int[gambitObjectiveCapacity + 1];
        GambitPlanRootGambitSlots = new int[gambitObjectiveCapacity + 1];
        GambitPlanRootTargetIds = new int[gambitObjectiveCapacity + 1];
        GambitPlanGambitSlots = new int[gambitObjectiveCapacity + 1];
        GambitPlanTargetIds = new int[gambitObjectiveCapacity + 1];
        GambitPlanLocalityKeys = new int[gambitObjectiveCapacity + 1];
        GambitPlanWeights = new float[gambitObjectiveCapacity + 1];
        GambitPlanAdjustedWeights = new float[gambitObjectiveCapacity + 1];
        GambitVisitedEpochs = new int[entityCapacity + 1];
    }
}
