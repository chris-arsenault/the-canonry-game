namespace Engine.Kernel;

internal readonly record struct KernelRuntimeCodes(
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
    int PlanningSourceCodeId,
    int MovementSourceCodeId,
    int MapActivitySourceCodeId,
    int LowAltitudeVerbSourceCodeId,
    int EffectsSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13);
}
