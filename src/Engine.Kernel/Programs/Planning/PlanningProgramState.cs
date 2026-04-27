namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class PlanningProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Planning;

    public int SchemaVersion => 1;
}
