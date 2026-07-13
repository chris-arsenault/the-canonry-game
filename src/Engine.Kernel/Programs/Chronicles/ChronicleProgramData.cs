namespace Engine.Kernel;

/// <summary>
/// Fixed-capacity story archetype definitions: cast (role queries), plot
/// (acts with deadlines and role-state advance conditions), direction
/// (role-pair gambit bias and cast sustain), and guaranteed resolutions.
/// Definition-only; live stories are in ChronicleProgramState.
/// </summary>
internal sealed partial class ChronicleProgramData : IKernelProgramData
{
    internal const int MaxRolesPerArchetype = 4;

    internal int[] ArchetypeIds = [];
    internal int[] ArchetypeMaxActive = [];
    internal int[] ArchetypeCastingCadenceTicks = [];
    internal int[] ArchetypeRoleStarts = [];
    internal int[] ArchetypeRoleCounts = [];
    internal int[] ArchetypeActStarts = [];
    internal int[] ArchetypeActCounts = [];
    internal int[] ArchetypeResolutionStarts = [];
    internal int[] ArchetypeResolutionCounts = [];
    internal int[] ArchetypePrimaryKindIds = [];
    internal int[] ArchetypePrimaryRequiredRelKindIds = [];
    internal float[] ArchetypePrimaryRequiredRelMinStrengths = [];

    // Roles (flat; role 0 of each archetype is the primary and carries no query).
    internal int[] RoleKindIds = [];
    internal int[] RoleRelatedKindIds = [];
    internal int[] RoleRelatedIncoming = [];
    internal float[] RoleRelatedMinStrengths = [];

    // Acts (flat).
    internal int[] ActDeadlineTicks = [];
    internal int[] ActAdvanceConditionKinds = [];
    internal int[] ActAdvanceRoleA = [];
    internal int[] ActAdvanceRoleB = [];
    internal int[] ActAdvanceRelKindIds = [];
    internal float[] ActAdvanceMinStrengths = [];
    internal int[] ActBiasStarts = [];
    internal int[] ActBiasCounts = [];
    internal int[] ActSustainStarts = [];
    internal int[] ActSustainCounts = [];

    // Bias rows (flat).
    internal int[] BiasActorRoles = [];
    internal int[] BiasTargetRoles = [];
    internal int[] BiasGambitIds = [];
    internal float[] BiasScoreBonuses = [];

    // Sustain rows (flat).
    internal int[] SustainRoles = [];
    internal int[] SustainAttributeIds = [];
    internal float[] SustainDeltas = [];

    // Resolutions (flat).
    internal int[] ResolutionOutcomeCodeIds = [];
    internal int[] ResolutionWeights = [];
    internal int[] ResolutionWhenKinds = [];
    internal int[] ResolutionWhenRoleA = [];
    internal int[] ResolutionWhenRoleB = [];
    internal int[] ResolutionWhenRelKindIds = [];
    internal float[] ResolutionWhenMinStrengths = [];
    internal int[] ResolutionRelStarts = [];
    internal int[] ResolutionRelCounts = [];
    internal int[] ResolutionHistoricalRoleMasks = [];

    // Resolution relationship writes (flat).
    internal int[] ResolutionRelSourceRoles = [];
    internal int[] ResolutionRelTargetRoles = [];
    internal int[] ResolutionRelKindIds = [];
    internal int[] ResolutionRelPolarities = [];
    internal float[] ResolutionRelStrengths = [];
    internal int[] ResolutionRelCullInstead = [];

    internal int ArchetypeCount;
    internal int RoleCount;
    internal int ActCount;
    internal int BiasCount;
    internal int SustainCount;
    internal int ResolutionCount;
    internal int ResolutionRelCount;

    public void Allocate(int archetypeCapacity, int itemCapacity)
    {
        AllocateArchetypes(archetypeCapacity);
        AllocateRolesAndActs(itemCapacity);
        AllocateResolutions(itemCapacity);
    }

    private void AllocateArchetypes(int archetypeCapacity)
    {
        ArchetypeIds = new int[archetypeCapacity + 1];
        ArchetypeMaxActive = new int[archetypeCapacity + 1];
        ArchetypeCastingCadenceTicks = new int[archetypeCapacity + 1];
        ArchetypeRoleStarts = new int[archetypeCapacity + 1];
        ArchetypeRoleCounts = new int[archetypeCapacity + 1];
        ArchetypeActStarts = new int[archetypeCapacity + 1];
        ArchetypeActCounts = new int[archetypeCapacity + 1];
        ArchetypeResolutionStarts = new int[archetypeCapacity + 1];
        ArchetypeResolutionCounts = new int[archetypeCapacity + 1];
        ArchetypePrimaryKindIds = new int[archetypeCapacity + 1];
        ArchetypePrimaryRequiredRelKindIds = new int[archetypeCapacity + 1];
        ArchetypePrimaryRequiredRelMinStrengths = new float[archetypeCapacity + 1];
    }

    private void AllocateRolesAndActs(int itemCapacity)
    {
        RoleKindIds = new int[itemCapacity + 1];
        RoleRelatedKindIds = new int[itemCapacity + 1];
        RoleRelatedIncoming = new int[itemCapacity + 1];
        RoleRelatedMinStrengths = new float[itemCapacity + 1];
        ActDeadlineTicks = new int[itemCapacity + 1];
        ActAdvanceConditionKinds = new int[itemCapacity + 1];
        ActAdvanceRoleA = new int[itemCapacity + 1];
        ActAdvanceRoleB = new int[itemCapacity + 1];
        ActAdvanceRelKindIds = new int[itemCapacity + 1];
        ActAdvanceMinStrengths = new float[itemCapacity + 1];
        ActBiasStarts = new int[itemCapacity + 1];
        ActBiasCounts = new int[itemCapacity + 1];
        ActSustainStarts = new int[itemCapacity + 1];
        ActSustainCounts = new int[itemCapacity + 1];
        BiasActorRoles = new int[itemCapacity + 1];
        BiasTargetRoles = new int[itemCapacity + 1];
        BiasGambitIds = new int[itemCapacity + 1];
        BiasScoreBonuses = new float[itemCapacity + 1];
        SustainRoles = new int[itemCapacity + 1];
        SustainAttributeIds = new int[itemCapacity + 1];
        SustainDeltas = new float[itemCapacity + 1];
    }

    private void AllocateResolutions(int itemCapacity)
    {
        ResolutionOutcomeCodeIds = new int[itemCapacity + 1];
        ResolutionWeights = new int[itemCapacity + 1];
        ResolutionWhenKinds = new int[itemCapacity + 1];
        ResolutionWhenRoleA = new int[itemCapacity + 1];
        ResolutionWhenRoleB = new int[itemCapacity + 1];
        ResolutionWhenRelKindIds = new int[itemCapacity + 1];
        ResolutionWhenMinStrengths = new float[itemCapacity + 1];
        ResolutionRelStarts = new int[itemCapacity + 1];
        ResolutionRelCounts = new int[itemCapacity + 1];
        ResolutionHistoricalRoleMasks = new int[itemCapacity + 1];
        ResolutionRelSourceRoles = new int[itemCapacity + 1];
        ResolutionRelTargetRoles = new int[itemCapacity + 1];
        ResolutionRelKindIds = new int[itemCapacity + 1];
        ResolutionRelPolarities = new int[itemCapacity + 1];
        ResolutionRelStrengths = new float[itemCapacity + 1];
        ResolutionRelCullInstead = new int[itemCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.ChronicleDirector;

    public int SchemaVersion => 1;
}
