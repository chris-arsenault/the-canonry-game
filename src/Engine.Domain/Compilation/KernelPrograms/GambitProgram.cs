namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class GambitProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledGambitRule> Gambits { get; init; } = [];
}

public sealed record CompiledGambitRule(
    int KindId,
    int GambitId,
    int CooldownTicks,
    int EntityCooldownTicks,
    ProcessProgramTargetMode TargetMode,
    int TargetKindId,
    float SuccessChance,
    IReadOnlyList<ProcessProgramEffectRule> GoalEffects,
    IReadOnlyList<ProcessProgramProviderSelectorRule> ProviderSelectors,
    IReadOnlyList<ProcessProgramSupportRule> SupportRules,
    IReadOnlyList<ProcessProgramPreconditionOp> Preconditions,
    IReadOnlyList<ProcessProgramExpressionOp> ScoreExpression,
    IReadOnlyList<ProcessProgramExpressionOp> RewardExpression,
    IReadOnlyList<ProcessProgramStepRule> Steps);
