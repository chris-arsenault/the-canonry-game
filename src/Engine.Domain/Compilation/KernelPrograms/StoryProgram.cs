namespace Engine.Domain.Compilation.KernelPrograms;

/// <summary>Compiled story archetypes for the Chronicle Director program.</summary>
public sealed class StoryProgram
{
    public required IReadOnlyList<CompiledStoryArchetypeRule> Archetypes { get; init; }
}

public sealed record CompiledStoryArchetypeRule(
    int ArchetypeId,
    int MaxActive,
    int CastingCadenceTicks,
    int PrimaryKindId,
    int PrimaryRequiredRelKindId,
    float PrimaryRequiredRelMinStrength,
    IReadOnlyList<CompiledStoryRole> SupportingRoles,
    IReadOnlyList<CompiledStoryAct> Acts,
    IReadOnlyList<CompiledStoryResolution> Resolutions);

public sealed record CompiledStoryRole(
    int KindId,
    int RelatedKindId,
    bool RelatedIncoming,
    float RelatedMinStrength);

public sealed record CompiledStoryAct(
    int DeadlineTicks,
    int AdvanceConditionKind,
    int AdvanceRoleA,
    int AdvanceRoleB,
    int AdvanceRelKindId,
    float AdvanceMinStrength,
    IReadOnlyList<CompiledStoryBias> Bias,
    IReadOnlyList<CompiledStorySustain> Sustain);

public sealed record CompiledStoryBias(
    int ActorRole,
    int TargetRole,
    int GambitId,
    float ScoreBonus);

public sealed record CompiledStorySustain(
    int Role,
    int AttributeId,
    float DeltaPerPulse);

public sealed record CompiledStoryResolution(
    int OutcomeCodeId,
    int Weight,
    int WhenConditionKind,
    int WhenRoleA,
    int WhenRoleB,
    int WhenRelKindId,
    float WhenMinStrength,
    int HistoricalRoleMask,
    IReadOnlyList<CompiledStoryResolutionRelationship> Relationships);

public sealed record CompiledStoryResolutionRelationship(
    int SourceRole,
    int TargetRole,
    int RelKindId,
    int PolarityCode,
    float Strength,
    bool CullInstead);
