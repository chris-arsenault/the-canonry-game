namespace Engine.Kernel;

using Engine.Constants;

internal readonly record struct ProgramDependency(
    int ReaderProgramId,
    int WriterProgramId,
    int DependencyLag);

internal readonly record struct ProgramReadDependency(
    int ReaderProgramId,
    int SourceProgramId,
    int DependencyLag);

internal readonly record struct ProgramWriteDependency(
    int WriterProgramId,
    int TargetProgramId,
    int DependencyLag);

internal readonly record struct ProgramDependencyViolation(
    int ReaderProgramId,
    int WriterProgramId,
    int DependencyLag);

internal static class ProgramDependencyLag
{
    public static int SameTick => ProgramDependencyLagCodes.SameTick;
    public static int PreviousTick => ProgramDependencyLagCodes.PreviousTick;
}
