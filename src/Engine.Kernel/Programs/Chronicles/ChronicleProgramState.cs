namespace Engine.Kernel;

/// <summary>
/// Live chronicle table: which archetypes are running, which entities hold
/// each role, per-chronicle heat and beat open/struck masks, a suppression
/// ring of recently chronicled cast, and the pending queue of gambit
/// executions and generator fires the director consumes each run for
/// strike detection.
/// </summary>
internal sealed partial class ChronicleProgramState : IKernelProgramState
{
    internal const int SuppressionRingCapacity = 64;

    internal readonly bool[] Active;
    internal readonly int[] ArchetypeSlots;
    internal readonly int[] StartTicks;
    internal readonly int[] LastStrikeTicks;
    internal readonly FixedScalar[] Heat;
    internal readonly long[] OpenMasks;
    internal readonly long[] StruckMasks;
    internal readonly long[] RetiredRoleMasks;
    internal readonly int[] RoleEntityIds;
    internal readonly int[] ChronicleSequences;
    internal int NextChronicleSequence;
    internal readonly int[] ArchetypeActiveCounts;
    internal readonly int[] NextCastTicks;
    internal readonly int[] RecentCastEntityOccurrenceHandles;
    internal readonly int[] RecentCastTicks;
    internal readonly FixedScalar[] RecentCastHeat;
    internal int RecentCastNext;
    internal readonly int[] PendingKinds;
    internal readonly int[] PendingActorOccurrenceHandles;
    internal readonly int[] PendingTargetOccurrenceHandles;
    internal readonly int[] PendingProducerIds;
    internal readonly int[] PendingSucceeded;
    internal int PendingCount;
    internal int HighWatermark;
    internal int LegendaryResolutionCount;

    public ChronicleProgramState(int chronicleCapacity, int archetypeCapacity, int pendingExecutionCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(chronicleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(archetypeCapacity);
        NumericGuard.ThrowIfNegativeOrZero(pendingExecutionCapacity);
        Active = new bool[chronicleCapacity + 1];
        ArchetypeSlots = new int[chronicleCapacity + 1];
        StartTicks = new int[chronicleCapacity + 1];
        LastStrikeTicks = new int[chronicleCapacity + 1];
        Heat = new FixedScalar[chronicleCapacity + 1];
        OpenMasks = new long[chronicleCapacity + 1];
        StruckMasks = new long[chronicleCapacity + 1];
        RetiredRoleMasks = new long[chronicleCapacity + 1];
        RoleEntityIds = new int[((chronicleCapacity + 1) * ChronicleProgramData.MaxRolesPerArchetype) + 1];
        ChronicleSequences = new int[chronicleCapacity + 1];
        ArchetypeActiveCounts = new int[archetypeCapacity + 1];
        NextCastTicks = new int[archetypeCapacity + 1];
        RecentCastEntityOccurrenceHandles = new int[SuppressionRingCapacity];
        RecentCastTicks = new int[SuppressionRingCapacity];
        RecentCastHeat = new FixedScalar[SuppressionRingCapacity];
        PendingKinds = new int[pendingExecutionCapacity];
        PendingActorOccurrenceHandles = new int[pendingExecutionCapacity];
        PendingTargetOccurrenceHandles = new int[pendingExecutionCapacity];
        PendingProducerIds = new int[pendingExecutionCapacity];
        PendingSucceeded = new int[pendingExecutionCapacity];
    }

    public int ProgramId => KernelProgramCodes.ChronicleDirector;

    public int SchemaVersion => 9;
}
