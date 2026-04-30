namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class MovementProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Movement;

    public int SchemaVersion => 1;
}

