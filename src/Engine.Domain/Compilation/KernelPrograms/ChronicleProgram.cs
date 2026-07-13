namespace Engine.Domain.Compilation.KernelPrograms;

/// <summary>Compiled story archetypes for the Chronicle Director program.</summary>
public sealed class ChronicleProgram
{
    public required IReadOnlyList<CompiledChronicleArchetypeRule> Archetypes { get; init; }
}

public sealed record CompiledChronicleArchetypeRule(
    int ArchetypeId,
    int MaxActive,
    int CastingCadenceTicks,
    int PrimaryKindId,
    int PrimaryRequiredRelKindId,
    float PrimaryRequiredRelMinStrength,
    IReadOnlyList<CompiledChronicleRole> SupportingRoles,
    IReadOnlyList<CompiledChronicleAct> Acts,
    IReadOnlyList<CompiledChronicleResolution> Resolutions);

public sealed record CompiledChronicleRole(
    int KindId,
    int RelatedKindId,
    bool RelatedIncoming,
    float RelatedMinStrength);

public sealed record CompiledChronicleAct(
    int DeadlineTicks,
    int AdvanceConditionKind,
    int AdvanceRoleA,
    int AdvanceRoleB,
    int AdvanceRelKindId,
    float AdvanceMinStrength,
    IReadOnlyList<CompiledChronicleBias> Bias,
    IReadOnlyList<CompiledChronicleSustain> Sustain);

public sealed record CompiledChronicleBias(
    int ActorRole,
    int TargetRole,
    int GambitId,
    float ScoreBonus);

public sealed record CompiledChronicleSustain(
    int Role,
    int AttributeId,
    float DeltaPerPulse);

public sealed record CompiledChronicleResolution(
    int OutcomeCodeId,
    int Weight,
    int WhenConditionKind,
    int WhenRoleA,
    int WhenRoleB,
    int WhenRelKindId,
    float WhenMinStrength,
    int HistoricalRoleMask,
    IReadOnlyList<CompiledChronicleResolutionRelationship> Relationships);

public sealed record CompiledChronicleResolutionRelationship(
    int SourceRole,
    int TargetRole,
    int RelKindId,
    int PolarityCode,
    float Strength,
    bool CullInstead);
