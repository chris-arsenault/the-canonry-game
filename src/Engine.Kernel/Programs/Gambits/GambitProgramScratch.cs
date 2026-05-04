namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly int[] PathOpenSet;
    internal EpochStampSet PathOpen;
    internal EpochStampSet PathClosed;
    internal EpochStampedArray<int> PathCameFrom;
    internal EpochStampedArray<float> PathGScore;
    internal EpochStampedArray<float> PathFScore;
    internal readonly bool[] PreconditionStack;
    internal readonly float[] ExpressionStack;
    internal readonly int[] VariableIds;
    internal readonly float[] VariableValues;
    internal EpochStampSet GambitVisited;

    public GambitProgramScratch(int pathCapacity, int entityCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        PathOpenSet = new int[pathCapacity];
        PathOpen = new EpochStampSet(pathCapacity);
        PathClosed = new EpochStampSet(pathCapacity);
        PathCameFrom = new EpochStampedArray<int>(pathCapacity);
        PathGScore = new EpochStampedArray<float>(pathCapacity);
        PathFScore = new EpochStampedArray<float>(pathCapacity);
        PreconditionStack = new bool[64];
        ExpressionStack = new float[32];
        VariableIds = new int[32];
        VariableValues = new float[32];
        GambitVisited = new EpochStampSet(entityCapacity + 1);
    }
}
