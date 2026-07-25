namespace Engine.Kernel;

using Engine.Constants;

/// <summary>
/// The live regime of the world: which age (if any) occupies each of the
/// three tracks, when it opened, when each age last closed (for cooldowns),
/// and the sampled culture-mass history the growth fact is recognized from.
/// </summary>
internal sealed class WorldBeatsProgramState : IKernelProgramState
{
    internal const int NeverClosed = -1_000_000;

    internal readonly int[] OpenAgeSlots;
    internal readonly int[] OpenedTicks;
    internal readonly int[] LastClosedTicks;
    internal readonly int[] ActiveRelationshipCountsByKind;
    internal readonly int[] EndedRelationshipCountsByKind;
    internal FixedScalar PreviousTotalCultureMass;
    internal int PreviousMassSampleTick;
    internal FixedScalar MassGrowthPermille;

    public WorldBeatsProgramState(int ageCapacity, int relationshipKindCapacity)
    {
        NumericGuard.ThrowIfNegativeOrZero(ageCapacity);
        NumericGuard.ThrowIfNegativeOrZero(relationshipKindCapacity);
        OpenAgeSlots = new int[WorldBeatTrackCodes.Count];
        OpenedTicks = new int[WorldBeatTrackCodes.Count];
        LastClosedTicks = new int[ageCapacity + 1];
        ActiveRelationshipCountsByKind = new int[relationshipKindCapacity + 1];
        EndedRelationshipCountsByKind = new int[relationshipKindCapacity + 1];
        for (var slot = 0; slot < LastClosedTicks.Length; slot++)
        {
            LastClosedTicks[slot] = NeverClosed;
        }

        PreviousMassSampleTick = -1;
    }

    public int ProgramId => KernelProgramCodes.WorldBeats;

    public int SchemaVersion => 3;
}
