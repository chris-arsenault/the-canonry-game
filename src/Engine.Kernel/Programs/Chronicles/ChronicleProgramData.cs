namespace Engine.Kernel;

/// <summary>
/// Fixed-capacity chronicle archetype definitions: cast (role queries) and an
/// unordered pool of beats. A beat opens on a world/heat condition, charges
/// the world while open (gambit bias, gambit grants), and strikes on an
/// execution pattern or state fact. Terminal beats end the chronicle with a
/// recognized outcome; nothing here writes world state. Definition-only;
/// live chronicles are in ChronicleProgramState.
/// </summary>
internal sealed partial class ChronicleProgramData : IKernelProgramData
{
    internal const int MaxRolesPerArchetype = 4;
    internal const int MaxBeatsPerArchetype = 64;

    internal int[] ArchetypeIds = [];
    internal int[] ArchetypeMaxActive = [];
    internal int[] ArchetypeCastingCadenceTicks = [];
    internal int[] ArchetypeRoleStarts = [];
    internal int[] ArchetypeRoleCounts = [];
    internal int[] ArchetypeBeatStarts = [];
    internal int[] ArchetypeBeatCounts = [];
    internal int[] ArchetypePrimaryKindIds = [];
    internal int[] ArchetypePrimaryRequiredRelKindIds = [];
    internal float[] ArchetypePrimaryRequiredRelMinStrengths = [];
    internal int[] ArchetypeFadeOutcomeCodeIds = [];

    // Roles (flat; role 0 of each archetype is the primary and carries no query).
    internal int[] RoleKindIds = [];
    internal int[] RoleRelatedKindIds = [];
    internal int[] RoleRelatedIncoming = [];
    internal float[] RoleRelatedMinStrengths = [];

    // Beats (flat).
    internal int[] BeatCodeIds = [];
    internal float[] BeatHeatGains = [];
    internal int[] BeatTerminalOutcomeCodeIds = [];
    internal int[] BeatOpenKinds = [];
    internal int[] BeatOpenRoleA = [];
    internal int[] BeatOpenRoleB = [];
    internal int[] BeatOpenRelOrAttrIds = [];
    internal float[] BeatOpenValues = [];
    internal int[] BeatStrikeKinds = [];
    internal int[] BeatStrikeActorRoles = [];
    internal int[] BeatStrikeTargetRoles = [];
    internal int[] BeatStrikeGambitIds = [];
    internal int[] BeatStrikeAcceptFailure = [];
    internal int[] BeatStrikeFactKinds = [];
    internal int[] BeatStrikeFactRoleA = [];
    internal int[] BeatStrikeFactRoleB = [];
    internal int[] BeatStrikeFactRelOrAttrIds = [];
    internal float[] BeatStrikeFactValues = [];
    internal int[] BeatChargeStarts = [];
    internal int[] BeatChargeCounts = [];
    internal int[] BeatGrantStarts = [];
    internal int[] BeatGrantCounts = [];

    // Charge rows (flat): score weight the open beat adds to role-pair gambits.
    internal int[] ChargeActorRoles = [];
    internal int[] ChargeTargetRoles = [];
    internal int[] ChargeGambitIds = [];
    internal float[] ChargeScoreBonuses = [];
    internal int[] ChargeHeatScaled = [];

    // Grant rows (flat): chronicle-granted gambits available while the beat is open.
    internal int[] GrantRoles = [];
    internal int[] GrantGambitIds = [];

    internal int ArchetypeCount;
    internal int RoleCount;
    internal int BeatCount;
    internal int ChargeCount;
    internal int GrantCount;

    public void Allocate(int archetypeCapacity, int itemCapacity)
    {
        AllocateArchetypes(archetypeCapacity);
        AllocateRoles(itemCapacity);
        AllocateBeats(itemCapacity);
        AllocateChargesAndGrants(itemCapacity);
    }

    private void AllocateArchetypes(int archetypeCapacity)
    {
        ArchetypeIds = new int[archetypeCapacity + 1];
        ArchetypeMaxActive = new int[archetypeCapacity + 1];
        ArchetypeCastingCadenceTicks = new int[archetypeCapacity + 1];
        ArchetypeRoleStarts = new int[archetypeCapacity + 1];
        ArchetypeRoleCounts = new int[archetypeCapacity + 1];
        ArchetypeBeatStarts = new int[archetypeCapacity + 1];
        ArchetypeBeatCounts = new int[archetypeCapacity + 1];
        ArchetypePrimaryKindIds = new int[archetypeCapacity + 1];
        ArchetypePrimaryRequiredRelKindIds = new int[archetypeCapacity + 1];
        ArchetypePrimaryRequiredRelMinStrengths = new float[archetypeCapacity + 1];
        ArchetypeFadeOutcomeCodeIds = new int[archetypeCapacity + 1];
    }

    private void AllocateRoles(int itemCapacity)
    {
        RoleKindIds = new int[itemCapacity + 1];
        RoleRelatedKindIds = new int[itemCapacity + 1];
        RoleRelatedIncoming = new int[itemCapacity + 1];
        RoleRelatedMinStrengths = new float[itemCapacity + 1];
    }

    private void AllocateBeats(int itemCapacity)
    {
        BeatCodeIds = new int[itemCapacity + 1];
        BeatHeatGains = new float[itemCapacity + 1];
        BeatTerminalOutcomeCodeIds = new int[itemCapacity + 1];
        BeatOpenKinds = new int[itemCapacity + 1];
        BeatOpenRoleA = new int[itemCapacity + 1];
        BeatOpenRoleB = new int[itemCapacity + 1];
        BeatOpenRelOrAttrIds = new int[itemCapacity + 1];
        BeatOpenValues = new float[itemCapacity + 1];
        BeatStrikeKinds = new int[itemCapacity + 1];
        BeatStrikeActorRoles = new int[itemCapacity + 1];
        BeatStrikeTargetRoles = new int[itemCapacity + 1];
        BeatStrikeGambitIds = new int[itemCapacity + 1];
        BeatStrikeAcceptFailure = new int[itemCapacity + 1];
        BeatStrikeFactKinds = new int[itemCapacity + 1];
        BeatStrikeFactRoleA = new int[itemCapacity + 1];
        BeatStrikeFactRoleB = new int[itemCapacity + 1];
        BeatStrikeFactRelOrAttrIds = new int[itemCapacity + 1];
        BeatStrikeFactValues = new float[itemCapacity + 1];
        BeatChargeStarts = new int[itemCapacity + 1];
        BeatChargeCounts = new int[itemCapacity + 1];
        BeatGrantStarts = new int[itemCapacity + 1];
        BeatGrantCounts = new int[itemCapacity + 1];
    }

    private void AllocateChargesAndGrants(int itemCapacity)
    {
        ChargeActorRoles = new int[itemCapacity + 1];
        ChargeTargetRoles = new int[itemCapacity + 1];
        ChargeGambitIds = new int[itemCapacity + 1];
        ChargeScoreBonuses = new float[itemCapacity + 1];
        ChargeHeatScaled = new int[itemCapacity + 1];
        GrantRoles = new int[itemCapacity + 1];
        GrantGambitIds = new int[itemCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.ChronicleDirector;

    public int SchemaVersion => 3;
}
