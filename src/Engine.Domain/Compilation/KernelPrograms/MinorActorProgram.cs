namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class MinorActorProgram
{
    public bool EnabledByDefault { get; init; }

    public IReadOnlyList<MinorActorEmitterRule> EmitterRules { get; init; } = [];
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
    float WorkDelta,
    IReadOnlyList<MinorActorVerbRule> Verbs);

public sealed record MinorActorVerbRule(
    int VerbId,
    int ActionCode,
    int TargetingCode,
    int PlaneCode,
    int Radius,
    int SampleCount,
    float Weight,
    float WorkDeltaMultiplier);
