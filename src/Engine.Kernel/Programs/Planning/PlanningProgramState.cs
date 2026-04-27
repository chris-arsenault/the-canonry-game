namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class PlanningProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Planning;

    public int SchemaVersion => 1;
}
