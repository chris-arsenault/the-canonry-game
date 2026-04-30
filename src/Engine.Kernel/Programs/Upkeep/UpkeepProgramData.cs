namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class UpkeepProgramData : IKernelProgramData
{
    internal readonly int[] LifecycleKindIds;
    internal readonly int[] LifecycleAttributeIds;
    internal readonly float[] LifecycleBaseRates;
    internal readonly float[] LifecycleAgeCoefficients;
    internal readonly float[] LifecycleVariances;
    internal int LifecycleRuleCount;

    public UpkeepProgramData(int lifecycleRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifecycleRuleCapacity);
        LifecycleKindIds = new int[lifecycleRuleCapacity + 1];
        LifecycleAttributeIds = new int[lifecycleRuleCapacity + 1];
        LifecycleBaseRates = new float[lifecycleRuleCapacity + 1];
        LifecycleAgeCoefficients = new float[lifecycleRuleCapacity + 1];
        LifecycleVariances = new float[lifecycleRuleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Upkeep;

    public int SchemaVersion => 2;
}
