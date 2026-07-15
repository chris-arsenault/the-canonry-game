namespace Engine.Domain.Compilation.KernelPrograms;

/// <summary>Compiled chronicle archetypes for the Chronicle Director program.</summary>
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
    int FadeOutcomeCodeId,
    int FadeGraceTicks,
    int FadeQuietTicks,
    IReadOnlyList<CompiledChronicleRole> SupportingRoles,
    IReadOnlyList<CompiledChronicleBeat> Beats);

public sealed record CompiledChronicleRole(
    int KindId,
    int RelatedKindId,
    bool RelatedIncoming,
    float RelatedMinStrength,
    bool Open = false);

public sealed record CompiledChronicleBeat(
    int BeatCodeId,
    float HeatGain,
    int TerminalOutcomeCodeId,
    CompiledChronicleCondition Open,
    int StrikeKind,
    int StrikeActorRole,
    int StrikeTargetRole,
    int StrikeGambitId,
    bool StrikeAcceptFailure,
    int StrikeJoinRole,
    CompiledChronicleCondition StrikeFact,
    IReadOnlyList<CompiledChronicleCharge> Charges,
    IReadOnlyList<CompiledChronicleGrant> Grants);

public sealed record CompiledChronicleCondition(
    int Kind,
    int RoleA,
    int RoleB,
    int RelOrAttrId,
    float Value);

public sealed record CompiledChronicleCharge(
    int ActorRole,
    int TargetRole,
    int GambitId,
    float ScoreBonus,
    float SuccessBonus,
    bool HeatScaled);

public sealed record CompiledChronicleGrant(
    int Role,
    int GambitId);
