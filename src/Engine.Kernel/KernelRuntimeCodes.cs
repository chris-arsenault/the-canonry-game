namespace Engine.Kernel;

internal readonly record struct KernelRuntimeCodes(
    int RelationshipCreatedCodeId,
    int RelationshipCulledCodeId,
    int RelationshipStrengthChangedCodeId,
    int GambitSourceCodeId,
    int StartupSourceCodeId,
    int RelationshipSourceCodeId,
    int GeneratorSourceCodeId,
    int MapSourceCodeId,
    int LowAltitudeVerbSourceCodeId,
    int EffectsSourceCodeId,
    int PlayerSourceCodeId,
    int ChronicleSourceCodeId)
{
    public static KernelRuntimeCodes Default => new(1, 2, 3, 1, 3, 4, 5, 9, 11, 12, 13, 14);
}
