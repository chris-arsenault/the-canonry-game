namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class PressureProgramData : IKernelProgramData
{
    internal readonly int[] PressureIds;
    internal readonly int[] ScopeCodes;
    internal readonly float[] Baselines;
    internal readonly float[] HomeostasisRates;
    internal readonly float[] SmoothingFactors;
    internal int PressureCount;

    public PressureProgramData(int pressureCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pressureCapacity);
        PressureIds = new int[pressureCapacity + 1];
        ScopeCodes = new int[pressureCapacity + 1];
        Baselines = new float[pressureCapacity + 1];
        HomeostasisRates = new float[pressureCapacity + 1];
        SmoothingFactors = new float[pressureCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Pressures;

    public int SchemaVersion => 1;
}
