namespace Engine.Kernel;

internal readonly record struct KernelRuntimeCodes(
    int RelationshipCreatedCodeId,
    int RelationshipCulledCodeId,
    int RelationshipStrengthChangedCodeId,
    int GambitSourceCodeId,
    int UpkeepSourceCodeId,
    int StartupSourceCodeId,
    int RelationshipSourceCodeId,
    int GeneratorSourceCodeId,
    int DegeneracySourceCodeId,
    int PlanningSourceCodeId,
    int MapActivitySourceCodeId,
    int MapSignalsSourceCodeId,
    int LowAltitudeVerbSourceCodeId,
    int EffectsSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12);
}
