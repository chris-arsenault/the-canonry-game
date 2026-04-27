namespace Engine.Kernel;

public readonly record struct KernelRuntimeCodes(
    int RelationshipCreatedCodeId,
    int RelationshipCulledCodeId,
    int RelationshipStrengthChangedCodeId,
    int ProcessSourceCodeId,
    int GambitSourceCodeId,
    int UpkeepSourceCodeId,
    int StartupSourceCodeId,
    int RelationshipSourceCodeId,
    int GeneratorSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3, 4, 5, 6);
}
