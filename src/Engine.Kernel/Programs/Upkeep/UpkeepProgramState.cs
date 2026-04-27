namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class UpkeepProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Upkeep;

    public int SchemaVersion => 1;
}
