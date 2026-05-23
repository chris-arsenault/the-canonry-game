namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed class CultureBoundaryProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.CultureBoundaries;

    public int SchemaVersion => 1;
}
