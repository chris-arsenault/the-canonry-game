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
    internal float PreviousTotalCultureMass;
    internal int PreviousMassSampleTick;
    internal float MassGrowthPermille;

    public WorldBeatsProgramState(int ageCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ageCapacity);
        OpenAgeSlots = new int[WorldBeatTrackCodes.Count];
        OpenedTicks = new int[WorldBeatTrackCodes.Count];
        LastClosedTicks = new int[ageCapacity + 1];
        for (var slot = 0; slot < LastClosedTicks.Length; slot++)
        {
            LastClosedTicks[slot] = NeverClosed;
        }

        PreviousMassSampleTick = -1;
    }

    public int ProgramId => KernelProgramCodes.WorldBeats;

    public int SchemaVersion => 1;
}
