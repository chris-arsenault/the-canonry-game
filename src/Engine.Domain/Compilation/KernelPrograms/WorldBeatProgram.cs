namespace Engine.Domain.Compilation.KernelPrograms;

/// <summary>Compiled age pool for the world-beats program.</summary>
public sealed class WorldBeatProgram
{
    public required IReadOnlyList<CompiledWorldBeatAge> Ages { get; init; }
}

public sealed record CompiledWorldBeatAge(
    int AgeId,
    int TrackCode,
    int Priority,
    int MinOpenTicks,
    int CooldownTicks,
    IReadOnlyList<CompiledWorldBeatCondition> OpenWhen,
    IReadOnlyList<CompiledWorldBeatCondition> CloseWhen,
    IReadOnlyList<CompiledWorldBeatCharge> Charges);

public sealed record CompiledWorldBeatCondition(
    int FactCode,
    int ComparatorCode,
    float Value,
    int ParamId);

public sealed record CompiledWorldBeatCharge(
    int DomainCode,
    int TargetId,
    float Multiplier);
