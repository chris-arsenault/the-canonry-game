namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GeneratorProgramState : IKernelProgramState
{
    internal readonly int[] RunCounts;
    internal readonly bool[] Enabled;

    // Provincial fire windows: one schedule per registered physical superregion.
    // Slot 0 is unused; provinces occupy 1..ProvinceCount.
    internal readonly int[] ProvinceSrIds;
    internal readonly int[] ProvinceWindowIndices;
    internal readonly int[] FiresInCurrentWindow;
    internal int ProvinceCount;

    public GeneratorProgramState(int generatorCapacity, int provinceCapacity = 64)
    {
        NumericGuard.ThrowIfNegativeOrZero(generatorCapacity);
        NumericGuard.ThrowIfNegativeOrZero(provinceCapacity);
        RunCounts = new int[generatorCapacity + 1];
        Enabled = new bool[generatorCapacity + 1];
        ProvinceSrIds = new int[provinceCapacity + 1];
        ProvinceWindowIndices = new int[provinceCapacity + 1];
        FiresInCurrentWindow = new int[provinceCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Generators;

    public int SchemaVersion => 4;
}
