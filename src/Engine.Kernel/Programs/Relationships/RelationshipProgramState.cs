namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class RelationshipProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Relationships;

    public int SchemaVersion => 1;
}
