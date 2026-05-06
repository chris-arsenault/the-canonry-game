namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GeneratorProgramState : IKernelProgramState
{
    internal readonly int[] RunCounts;
    internal readonly bool[] Enabled;
    internal int CurrentPulseIndex;
    internal int FiresThisPulse;

    public GeneratorProgramState(int generatorCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorCapacity);
        RunCounts = new int[generatorCapacity + 1];
        Enabled = new bool[generatorCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Generators;

    public int SchemaVersion => 2;
}
