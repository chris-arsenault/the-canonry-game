namespace Engine.Kernel;

using Engine.Constants;

internal readonly record struct ProgramMetadata(
    int ProgramId,
    ProgramReadDependency[] Reads,
    ProgramWriteDependency[] Writes);

internal readonly record struct ProgramReadDependency(
    int ResourceCode,
    int DependencyLag);

internal readonly record struct ProgramWriteDependency(
    int ResourceCode,
    int DependencyLag);

internal readonly record struct ProgramDependencyViolation(
    int ReaderProgramId,
    int WriterProgramId,
    int DependencyLag,
    int ResourceCode);

internal static class ProgramDependencyLag
{
    public static int SameTick => ProgramDependencyLagCodes.SameTick;
    public static int PreviousTick => ProgramDependencyLagCodes.PreviousTick;
}
