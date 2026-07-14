namespace Engine.Kernel;

/// <summary>
/// Live chronicle table: which archetypes are running, which entities hold
/// each role, per-chronicle heat and beat open/struck masks, a suppression
/// ring of recently chronicled cast, and the pending queue of gambit
/// executions and generator fires the director consumes each pulse for
/// strike detection.
/// </summary>
internal sealed class ChronicleProgramState : IKernelProgramState
{
    internal const int SuppressionRingCapacity = 64;
    internal const int PendingExecutionCapacity = 256;

    internal readonly bool[] Active;
    internal readonly int[] ArchetypeSlots;
    internal readonly int[] StartTicks;
    internal readonly int[] LastStrikeTicks;
    internal readonly float[] Heat;
    internal readonly long[] OpenMasks;
    internal readonly long[] StruckMasks;
    internal readonly int[] RoleEntityIds;
    internal readonly int[] ChronicleSequences;
    internal int NextChronicleSequence;
    internal readonly int[] ArchetypeActiveCounts;
    internal readonly int[] NextCastTicks;
    internal readonly int[] RecentCastEntityIds;
    internal readonly int[] RecentCastTicks;
    internal readonly float[] RecentCastHeat;
    internal int RecentCastNext;
    internal readonly int[] PendingKinds;
    internal readonly int[] PendingActorIds;
    internal readonly int[] PendingTargetIds;
    internal readonly int[] PendingProducerIds;
    internal readonly int[] PendingSucceeded;
    internal int PendingCount;
    internal int HighWatermark;

    public ChronicleProgramState(int chronicleCapacity, int archetypeCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chronicleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(archetypeCapacity);
        Active = new bool[chronicleCapacity + 1];
        ArchetypeSlots = new int[chronicleCapacity + 1];
        StartTicks = new int[chronicleCapacity + 1];
        LastStrikeTicks = new int[chronicleCapacity + 1];
        Heat = new float[chronicleCapacity + 1];
        OpenMasks = new long[chronicleCapacity + 1];
        StruckMasks = new long[chronicleCapacity + 1];
        RoleEntityIds = new int[((chronicleCapacity + 1) * ChronicleProgramData.MaxRolesPerArchetype) + 1];
        ChronicleSequences = new int[chronicleCapacity + 1];
        ArchetypeActiveCounts = new int[archetypeCapacity + 1];
        NextCastTicks = new int[archetypeCapacity + 1];
        RecentCastEntityIds = new int[SuppressionRingCapacity];
        RecentCastTicks = new int[SuppressionRingCapacity];
        RecentCastHeat = new float[SuppressionRingCapacity];
        PendingKinds = new int[PendingExecutionCapacity];
        PendingActorIds = new int[PendingExecutionCapacity];
        PendingTargetIds = new int[PendingExecutionCapacity];
        PendingProducerIds = new int[PendingExecutionCapacity];
        PendingSucceeded = new int[PendingExecutionCapacity];
    }

    public int ProgramId => KernelProgramCodes.ChronicleDirector;

    public int SchemaVersion => 6;
}
