namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class UpkeepProgramData : IKernelProgramData
{
    internal readonly int[] UpkeepKindIds;
    internal readonly int[] UpkeepAttributeIds;
    internal readonly int[] LocationResourceIds;
    internal readonly int[] CriticalTargetAttributeIds;
    internal readonly float[] DriftRates;
    internal readonly float[] LocationResourceDriftPerUnit;
    internal readonly float[] LocationResourceDriftMinOffset;
    internal readonly float[] LocationResourceDriftMaxOffset;
    internal readonly float[] CriticalThresholds;
    internal readonly float[] CriticalDeltas;
    internal readonly int[] LifecycleKindIds;
    internal readonly int[] LifecycleAttributeIds;
    internal readonly float[] LifecycleBaseRates;
    internal readonly float[] LifecycleAgeCoefficients;
    internal readonly float[] LifecycleVariances;
    internal int UpkeepRuleCount;
    internal int LifecycleRuleCount;

    public UpkeepProgramData(int upkeepRuleCapacity, int lifecycleRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(upkeepRuleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifecycleRuleCapacity);
        UpkeepKindIds = new int[upkeepRuleCapacity + 1];
        UpkeepAttributeIds = new int[upkeepRuleCapacity + 1];
        LocationResourceIds = new int[upkeepRuleCapacity + 1];
        CriticalTargetAttributeIds = new int[upkeepRuleCapacity + 1];
        DriftRates = new float[upkeepRuleCapacity + 1];
        LocationResourceDriftPerUnit = new float[upkeepRuleCapacity + 1];
        LocationResourceDriftMinOffset = new float[upkeepRuleCapacity + 1];
        LocationResourceDriftMaxOffset = new float[upkeepRuleCapacity + 1];
        CriticalThresholds = new float[upkeepRuleCapacity + 1];
        CriticalDeltas = new float[upkeepRuleCapacity + 1];
        LifecycleKindIds = new int[lifecycleRuleCapacity + 1];
        LifecycleAttributeIds = new int[lifecycleRuleCapacity + 1];
        LifecycleBaseRates = new float[lifecycleRuleCapacity + 1];
        LifecycleAgeCoefficients = new float[lifecycleRuleCapacity + 1];
        LifecycleVariances = new float[lifecycleRuleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Upkeep;

    public int SchemaVersion => 1;
}
