namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly bool[] PreconditionStack;
    internal readonly float[] ExpressionStack;
    internal readonly int[] VariableIds;
    internal readonly float[] VariableValues;
    internal EpochStampSet GambitVisited;

    public GambitProgramScratch(int entityCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        PreconditionStack = new bool[64];
        ExpressionStack = new float[32];
        VariableIds = new int[32];
        VariableValues = new float[32];
        GambitVisited = new EpochStampSet(entityCapacity + 1);
    }
}
