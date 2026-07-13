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
    int MapActivitySourceCodeId,
    int MapSignalsSourceCodeId,
    int LowAltitudeVerbSourceCodeId,
    int EffectsSourceCodeId,
    int PlayerSourceCodeId,
    int ChronicleSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14);
}
