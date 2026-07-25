namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MinorActorProgram
{
    public bool EnabledByDefault { get; init; }

    public IReadOnlyList<MinorActorEmitterRule> EmitterRules { get; init; } = [];

    /// <summary>Relationship kind whose living edges aim campaign-labor cohorts (0 = none).</summary>
    public int CampaignRelationshipKindId { get; init; }

    /// <summary>Relationship kind that marks a laden carrier for the movement road-speed bonus (0 = none).</summary>
    public int CarrierRelationshipKindId { get; init; }
}

public sealed record MinorActorEmitterRule(
    int SourceKindId,
    int SourceSubtypeId,
    int PurposeCode,
    int ActorKindCode,
    int ActionCode,
    int PlaneCode,
    int TtlPulses,
    int EmitCadencePulses,
    int MaxActivePerSource,
    int SpawnRadius,
    int MoveChancePermille,
    int ActionCadencePulses,
    FixedScalar WorkDelta,
    IReadOnlyList<MinorActorVerbRule> Verbs);

public sealed record MinorActorVerbRule(
    int VerbId,
    int ActionCode,
    int TargetingCode,
    int PlaneCode,
    int Radius,
    int SampleCount,
    FixedScalar Weight,
    FixedScalar WorkDeltaMultiplier);
