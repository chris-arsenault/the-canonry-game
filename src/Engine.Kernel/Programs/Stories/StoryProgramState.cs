namespace Engine.Kernel;

/// <summary>
/// Live story table: which archetypes are running, which entities hold each
/// role, and where each story is in its act structure.
/// </summary>
internal sealed class StoryProgramState : IKernelProgramState
{
    internal readonly bool[] Active;
    internal readonly int[] ArchetypeSlots;
    internal readonly int[] ActIndices;
    internal readonly int[] StartTicks;
    internal readonly int[] ActStartTicks;
    internal readonly int[] RoleEntityIds;
    internal readonly int[] ArchetypeActiveCounts;
    internal readonly int[] NextCastTicks;
    internal int HighWatermark;

    public StoryProgramState(int storyCapacity, int archetypeCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storyCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(archetypeCapacity);
        Active = new bool[storyCapacity + 1];
        ArchetypeSlots = new int[storyCapacity + 1];
        ActIndices = new int[storyCapacity + 1];
        StartTicks = new int[storyCapacity + 1];
        ActStartTicks = new int[storyCapacity + 1];
        RoleEntityIds = new int[((storyCapacity + 1) * StoryProgramData.MaxRolesPerArchetype) + 1];
        ArchetypeActiveCounts = new int[archetypeCapacity + 1];
        NextCastTicks = new int[archetypeCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.StoryDirector;

    public int SchemaVersion => 1;
}
