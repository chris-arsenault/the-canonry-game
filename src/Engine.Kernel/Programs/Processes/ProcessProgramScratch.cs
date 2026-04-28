namespace Engine.Kernel;

internal sealed class ProcessProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal EpochStampSet PathOpen;
    internal EpochStampSet PathClosed;
    internal EpochStampedArray<int> PathCameFrom;
    internal EpochStampedArray<float> PathGScore;
    internal EpochStampedArray<float> PathFScore;
    internal readonly int[] ProcessPlanEffectKeys;
    internal readonly int[] ProcessPlanEffectScopeCodes;
    internal readonly int[] ProcessPlanEffectValueIds;
    internal readonly int[] ProcessPlanEffectKindIds;
    internal readonly int[] ProcessPlanGoalCandidateIndexes;
    internal readonly int[] ProcessPlanRootProcessSlots;
    internal readonly int[] ProcessPlanRootTargetIds;
    internal readonly int[] ProcessPlanProcessSlots;
    internal readonly int[] ProcessPlanTargetIds;
    internal readonly int[] ProcessPlanLocalityKeys;
    internal readonly float[] ProcessPlanWeights;
    internal readonly float[] ProcessPlanAdjustedWeights;
    internal readonly bool[] PreconditionStack;
    internal readonly float[] ExpressionStack;
    internal readonly int[] VariableIds;
    internal readonly float[] VariableValues;
    internal EpochStampSet ProcessVisited;

    public ProcessProgramScratch(int pathCapacity, int entityCapacity, int processObjectiveCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processObjectiveCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new EpochStampSet(pathCapacity);
        PathClosed = new EpochStampSet(pathCapacity);
        PathCameFrom = new EpochStampedArray<int>(pathCapacity);
        PathGScore = new EpochStampedArray<float>(pathCapacity);
        PathFScore = new EpochStampedArray<float>(pathCapacity);
        ProcessPlanEffectKeys = new int[processObjectiveCapacity + 1];
        ProcessPlanEffectScopeCodes = new int[processObjectiveCapacity + 1];
        ProcessPlanEffectValueIds = new int[processObjectiveCapacity + 1];
        ProcessPlanEffectKindIds = new int[processObjectiveCapacity + 1];
        ProcessPlanGoalCandidateIndexes = new int[processObjectiveCapacity + 1];
        ProcessPlanRootProcessSlots = new int[processObjectiveCapacity + 1];
        ProcessPlanRootTargetIds = new int[processObjectiveCapacity + 1];
        ProcessPlanProcessSlots = new int[processObjectiveCapacity + 1];
        ProcessPlanTargetIds = new int[processObjectiveCapacity + 1];
        ProcessPlanLocalityKeys = new int[processObjectiveCapacity + 1];
        ProcessPlanWeights = new float[processObjectiveCapacity + 1];
        ProcessPlanAdjustedWeights = new float[processObjectiveCapacity + 1];
        PreconditionStack = new bool[64];
        ExpressionStack = new float[32];
        VariableIds = new int[32];
        VariableValues = new float[32];
        ProcessVisited = new EpochStampSet(entityCapacity + 1);
    }
}
