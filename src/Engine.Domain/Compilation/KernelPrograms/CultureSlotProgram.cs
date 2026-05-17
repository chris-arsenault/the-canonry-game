namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class CultureSlotProgram
{
    public bool EnabledByDefault { get; init; }
    public IReadOnlyList<CultureSlotRuntimeRule> Slots { get; init; } = [];
}

public sealed record CultureSlotRuntimeRule(
    int CultureId,
    int SlotId,
    int PolarityCode,
    IReadOnlyList<int> AllowedKindIds,
    IReadOnlyList<CultureSlotScoreRuntimeRule> ScoreRules,
    float MinimumScore,
    int MinimumTenureTicks,
    float DisplacementMargin,
    float EntityInfluenceMultiplierDelta,
    float CultureInfluenceMultiplierDelta);

public sealed record CultureSlotScoreRuntimeRule(
    int SignalCode,
    int RelationshipKindId,
    float Weight);
