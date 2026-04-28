namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class ProcessProgramState : IKernelProgramState
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
    internal readonly PrimitiveIntTable WorkCommitmentProcessSlots;
    internal readonly PrimitiveIntTable WorkCommitmentTargetIds;
    internal readonly PrimitiveIntTable WorkCommitmentRemainingTicks;
    internal readonly PrimitiveIntTable OpportunityReservationTicks;
    internal readonly PrimitiveIntTable OpportunityReservationCounts;

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
        WorkCommitmentProcessSlots = new PrimitiveIntTable(processCommitmentCapacity);
        WorkCommitmentTargetIds = new PrimitiveIntTable(processCommitmentCapacity);
        WorkCommitmentRemainingTicks = new PrimitiveIntTable(processCommitmentCapacity);
        OpportunityReservationTicks = new PrimitiveIntTable(processCommitmentCapacity);
        OpportunityReservationCounts = new PrimitiveIntTable(processCommitmentCapacity);
    }

    public int ProgramId => KernelProgramCodes.Processes;

    public int SchemaVersion => 3;
}
