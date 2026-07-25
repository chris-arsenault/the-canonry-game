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
    FixedScalar MinimumScore,
    int MinimumTenureTicks,
    FixedScalar DisplacementMargin,
    FixedScalar EntityInfluenceMultiplierDelta,
    FixedScalar CultureInfluenceMultiplierDelta);

public sealed record CultureSlotScoreRuntimeRule(
    int SignalCode,
    int RelationshipKindId,
    FixedScalar Weight);
