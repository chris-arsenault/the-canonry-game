namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GambitProgramState : IKernelProgramState
{
    internal readonly bool[] GambitEnabled;
    internal readonly PrimitiveIntTable Cooldowns;
    internal readonly PrimitiveIntTable EntityCooldowns;
    internal readonly int[] ActionHistoryTicks;
    internal readonly int[] ActionHistoryGambitIds;
    internal readonly int[] ActionHistoryCounts;
    internal readonly int[] ActionHistoryNextSlots;
    internal readonly PrimitiveIntTable CommittedPlanTargets;
    internal readonly PrimitiveIntTable CommittedPlanGambitSlots;
    internal readonly PrimitiveIntTable CommittedPlanLocalities;
    internal readonly PrimitiveIntTable ActionTargetReservationTicks;
    internal readonly PrimitiveIntTable ActionTargetReservationCounts;

    public GambitProgramState(
        int gambitCapacity,
        int cooldownCapacity,
        int entityCapacity,
        int gambitCommitmentCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cooldownCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitCommitmentCapacity);
        GambitEnabled = new bool[gambitCapacity + 1];
        Cooldowns = new PrimitiveIntTable(cooldownCapacity);
        EntityCooldowns = new PrimitiveIntTable(cooldownCapacity);
        ActionHistoryTicks = new int[(entityCapacity + 1) * GambitProgramRuntime.GambitHistoryLength];
        ActionHistoryGambitIds = new int[(entityCapacity + 1) * GambitProgramRuntime.GambitHistoryLength];
        ActionHistoryCounts = new int[entityCapacity + 1];
        ActionHistoryNextSlots = new int[entityCapacity + 1];
        CommittedPlanTargets = new PrimitiveIntTable(gambitCommitmentCapacity);
        CommittedPlanGambitSlots = new PrimitiveIntTable(gambitCommitmentCapacity);
        CommittedPlanLocalities = new PrimitiveIntTable(gambitCommitmentCapacity);
        ActionTargetReservationTicks = new PrimitiveIntTable(gambitCommitmentCapacity);
        ActionTargetReservationCounts = new PrimitiveIntTable(gambitCommitmentCapacity);
    }

    public int ProgramId => KernelProgramCodes.Gambits;

    public int SchemaVersion => 2;
}
