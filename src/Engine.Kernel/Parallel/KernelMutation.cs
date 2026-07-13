namespace Engine.Kernel;

internal readonly record struct KernelMutation(
    int Code,
    int SubjectId,
    int ValueId,
    int IntValue,
    float FloatValue);

internal static class KernelMutationCodes
{
    internal const int ModifyAttribute = 1;
    internal const int SetEntityTag = 2;
}
