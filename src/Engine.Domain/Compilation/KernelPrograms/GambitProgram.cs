namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class GambitProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledGambitRule> Gambits { get; init; } = [];
    public IReadOnlyList<CompiledTraitGambitModifierRule> TraitModifiers { get; init; } = [];
}

public sealed record CompiledTraitGambitModifierRule(
    int GambitId,
    int TraitId,
    int BearerCode,
    float ScoreDelta,
    float SuccessChanceDelta);

public sealed record CompiledGambitRule(
    int KindId,
    int GambitId,
    int CooldownTicks,
    int EntityCooldownTicks,
    BehaviorProgramTargetMode TargetMode,
    int TargetKindId,
    float SuccessChance,
    bool ChronicleGranted,
    IReadOnlyList<BehaviorProgramProviderSelectorRule> ProviderSelectors,
    IReadOnlyList<BehaviorProgramPreconditionOp> Preconditions,
    IReadOnlyList<BehaviorProgramExpressionOp> ScoreExpression,
    IReadOnlyList<BehaviorProgramStepRule> Steps);
