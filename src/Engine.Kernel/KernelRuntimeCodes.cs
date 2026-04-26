namespace Engine.Kernel;

public readonly record struct KernelRuntimeCodes(
    int RelationshipCreatedCodeId,
    int RelationshipCulledCodeId,
    int RelationshipStrengthChangedCodeId,
    int ProcessSourceCodeId,
    int CardSourceCodeId,
    int UpkeepSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3);
}
