namespace Engine.Kernel;

using Engine.Kernel.Programs;

public sealed partial class ProcessProgramState : IKernelProgramState
{
    internal readonly bool[] ProcessEnabled;
    internal readonly PrimitiveIntTable Cooldowns;
    internal readonly PrimitiveIntTable EntityCooldowns;
    internal readonly int[] ActionHistoryTicks;
    internal readonly int[] ActionHistoryProcessIds;
    internal readonly int[] ActionHistoryCounts;
    internal readonly int[] ActionHistoryNextSlots;
    internal readonly PrimitiveIntTable CommittedPlanTargets;
    internal readonly PrimitiveIntTable CommittedPlanProcessSlots;
    internal readonly PrimitiveIntTable CommittedPlanLocalities;

    public ProcessProgramState(
        int processCapacity,
        int cooldownCapacity,
        int entityCapacity,
        int processCommitmentCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cooldownCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processCommitmentCapacity);
        ProcessEnabled = new bool[processCapacity + 1];
        Cooldowns = new PrimitiveIntTable(cooldownCapacity);
        EntityCooldowns = new PrimitiveIntTable(cooldownCapacity);
        ActionHistoryTicks = new int[(entityCapacity + 1) * ProcessProgramRuntime.ActionHistoryLength];
        ActionHistoryProcessIds = new int[(entityCapacity + 1) * ProcessProgramRuntime.ActionHistoryLength];
        ActionHistoryCounts = new int[entityCapacity + 1];
        ActionHistoryNextSlots = new int[entityCapacity + 1];
        CommittedPlanTargets = new PrimitiveIntTable(processCommitmentCapacity);
        CommittedPlanProcessSlots = new PrimitiveIntTable(processCommitmentCapacity);
        CommittedPlanLocalities = new PrimitiveIntTable(processCommitmentCapacity);
    }

    public int ProgramId => KernelProgramCodes.Processes;

    public int SchemaVersion => 1;
}
