namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class EffectsProgramState : IKernelProgramState
{
    public int ProgramId => KernelProgramCodes.Effects;

    public int SchemaVersion => 1;
}
