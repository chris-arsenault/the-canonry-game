namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GeneratorProgramState : IKernelProgramState
{
    internal readonly int[] RunCounts;
    internal readonly bool[] Enabled;
    internal readonly int[] NextCadenceFireTicks;

    // Provincial pulses: one clock per registered physical superregion.
    // Slot 0 is unused; provinces occupy 1..ProvinceCount.
    internal readonly int[] ProvinceSrIds;
    internal readonly int[] ProvincePulseIndices;
    internal readonly int[] ProvinceFires;
    internal int ProvinceCount;

    public GeneratorProgramState(int generatorCapacity, int provinceCapacity = 64)
    {
        NumericGuard.ThrowIfNegativeOrZero(generatorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(provinceCapacity);
        RunCounts = new int[generatorCapacity + 1];
        Enabled = new bool[generatorCapacity + 1];
        NextCadenceFireTicks = new int[generatorCapacity + 1];
        ProvinceSrIds = new int[provinceCapacity + 1];
        ProvincePulseIndices = new int[provinceCapacity + 1];
        ProvinceFires = new int[provinceCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Generators;

    public int SchemaVersion => 4;
}
