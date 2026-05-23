namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed class CultureBoundaryProgramData : IKernelProgramData
{
    public int ProgramId => KernelProgramCodes.CultureBoundaries;

    public int SchemaVersion => 1;
}
