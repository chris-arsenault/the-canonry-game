namespace Engine.Kernel;

internal sealed class GambitProgramScratch
{
    internal readonly bool[] PreconditionStack;
    internal readonly FixedScalar[] ExpressionStack;
    internal readonly int[] VariableIds;
    internal readonly FixedScalar[] VariableValues;
    internal EpochStampSet GambitVisited;

    public GambitProgramScratch(int entityCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(entityCapacity);
        PreconditionStack = new bool[64];
        ExpressionStack = new FixedScalar[32];
        VariableIds = new int[32];
        VariableValues = new FixedScalar[32];
        GambitVisited = new EpochStampSet(entityCapacity + 1);
    }
}
