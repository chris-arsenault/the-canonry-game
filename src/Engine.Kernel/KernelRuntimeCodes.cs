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
    int GeneratorSourceCodeId,
    int PressureSourceCodeId,
    int DegeneracySourceCodeId,
    int PlanningSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9);
}
