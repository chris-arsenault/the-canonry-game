namespace Engine.Kernel;

internal sealed class ProcessProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal readonly bool[] PathOpen;
    internal readonly bool[] PathClosed;
    internal readonly int[] PathCameFrom;
    internal readonly float[] PathGScore;
    internal readonly float[] PathFScore;
    internal readonly int[] ProcessPlanEffectKeys;
    internal readonly int[] ProcessPlanRootProcessSlots;
    internal readonly int[] ProcessPlanRootTargetIds;
    internal readonly int[] ProcessPlanProcessSlots;
    internal readonly int[] ProcessPlanTargetIds;
    internal readonly int[] ProcessPlanLocalityKeys;
    internal readonly float[] ProcessPlanWeights;
    internal readonly float[] ProcessPlanAdjustedWeights;
    internal readonly int[] ProcessVisitedEpochs;
    internal int ProcessVisitedEpoch;

    public ProcessProgramScratch(int pathCapacity, int entityCapacity, int processObjectiveCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processObjectiveCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new bool[pathCapacity];
        PathClosed = new bool[pathCapacity];
        PathCameFrom = new int[pathCapacity];
        PathGScore = new float[pathCapacity];
        PathFScore = new float[pathCapacity];
        ProcessPlanEffectKeys = new int[processObjectiveCapacity + 1];
        ProcessPlanRootProcessSlots = new int[processObjectiveCapacity + 1];
        ProcessPlanRootTargetIds = new int[processObjectiveCapacity + 1];
        ProcessPlanProcessSlots = new int[processObjectiveCapacity + 1];
        ProcessPlanTargetIds = new int[processObjectiveCapacity + 1];
        ProcessPlanLocalityKeys = new int[processObjectiveCapacity + 1];
        ProcessPlanWeights = new float[processObjectiveCapacity + 1];
        ProcessPlanAdjustedWeights = new float[processObjectiveCapacity + 1];
        ProcessVisitedEpochs = new int[entityCapacity + 1];
    }
}
