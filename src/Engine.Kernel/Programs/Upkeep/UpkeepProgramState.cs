namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class UpkeepProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Upkeep;

    public int SchemaVersion => 1;
}
