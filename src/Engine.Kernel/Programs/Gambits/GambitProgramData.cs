namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class GambitProgramData : IKernelProgramData
{
    internal int[] GambitKindIds = [];
    internal int[] GambitIds = [];
    internal int[] GambitCooldownTicks = [];
    internal int[] GambitEntityCooldownTicks = [];
    internal float[] GambitSuccessChances = [];
    internal int[] GambitTargetKindIds = [];
    internal int[] GambitTargetModes = [];
    internal int[] GambitPreconditionStarts = [];
    internal int[] GambitPreconditionCounts = [];
    internal int[] GambitExpressionStarts = [];
    internal int[] GambitExpressionCounts = [];
    internal int[] GambitRewardExpressionStarts = [];
    internal int[] GambitRewardExpressionCounts = [];
    internal int[] GambitStepStarts = [];
    internal int[] GambitStepCounts = [];
    internal int[] GambitProviderSelectorStarts = [];
    internal int[] GambitProviderSelectorCounts = [];
    internal int[] GambitSupportRuleStarts = [];
    internal int[] GambitSupportRuleCounts = [];
    internal int[] GambitGoalEffectStarts = [];
    internal int[] GambitGoalEffectCounts = [];
    internal int[] GoalEffectScopeCodes = [];
    internal int[] GoalEffectValueIds = [];
    internal int[] GoalEffectKindIds = [];
    internal int[] PreconditionCodes = [];
    internal int[] PreA = [];
    internal int[] PreB = [];
    internal int[] PreC = [];
    internal int[] PreD = [];
    internal float[] PreX = [];
    internal float[] PreY = [];
    internal int[] ExpressionCodes = [];
    internal int[] ExpressionA = [];
    internal float[] ExpressionX = [];
    internal int[] RewardExpressionCodes = [];
    internal int[] RewardExpressionA = [];
    internal float[] RewardExpressionX = [];
    internal int[] StepOperationCodes = [];
    internal int[] StepA = [];
    internal int[] StepB = [];
    internal int[] StepC = [];
    internal int[] StepD = [];
    internal float[] StepX = [];
    internal float[] StepY = [];
    internal int[] StepExpressionStarts = [];
    internal int[] StepExpressionCounts = [];
    internal int[] MutationExpressionCodes = [];
    internal int[] MutationExpressionA = [];
    internal float[] MutationExpressionX = [];
    internal int[] ObjectiveKindIds = [];
    internal int[] ObjectiveCodes = [];
    internal int[] ObjectiveEffectScopeCodes = [];
    internal int[] ObjectiveEffectValueIds = [];
    internal int[] ObjectiveEffectKindIds = [];
    internal int[] ObjectiveAttributeIds = [];
    internal float[] ObjectiveMinimums = [];
    internal float[] ObjectiveMaximums = [];
    internal float[] ObjectiveDefaults = [];
    internal float[] ObjectiveBaselines = [];
    internal float[] ObjectiveBaseUrgencies = [];
    internal int[] ProviderSelectorCodes = [];
    internal int[] ProviderSelectorKindIds = [];
    internal int[] ProviderSelectorValueIds = [];
    internal int[] ProviderSelectorRelationshipKindIds = [];
    internal int[] ProviderSelectorFactIds = [];
    internal float[] ProviderSelectorMinimumStrengths = [];
    internal int[] SupportEffectScopeCodes = [];
    internal int[] SupportEffectValueIds = [];
    internal int[] SupportEffectKindIds = [];
    internal int[] SupportExpressionStarts = [];
    internal int[] SupportExpressionCounts = [];
    internal float[] SupportDesiredMinimums = [];
    internal float[] SupportUrgencyScales = [];
    internal int[] SupportExpressionCodes = [];
    internal int[] SupportExpressionA = [];
    internal float[] SupportExpressionX = [];
    internal int[] TraitModifierGambitIds = [];
    internal int[] TraitModifierTraitIds = [];
    internal int[] TraitModifierBearerCodes = [];
    internal float[] TraitModifierScoreDeltas = [];
    internal float[] TraitModifierSuccessChanceDeltas = [];
    internal int GambitCount;
    internal int PreconditionCount;
    internal int ExpressionCount;
    internal int RewardExpressionCount;
    internal int StepCount;
    internal int MutationExpressionCount;
    internal int ObjectiveCount;
    internal int ProviderSelectorCount;
    internal int SupportRuleCount;
    internal int SupportExpressionCount;
    internal int GoalEffectCount;
    internal int TraitModifierCount;
    internal int OpenGambitSlot;

    public GambitProgramData(
        int gambitCapacity,
        int stepCapacity,
        int gambitObjectiveCapacity,
        int gambitProviderSelectorCapacity,
        int gambitSupportRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitObjectiveCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitProviderSelectorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitSupportRuleCapacity);
        InitializeGambitArrays(gambitCapacity);
        InitializeStepArrays(stepCapacity);
        InitializeObjectiveArrays(gambitObjectiveCapacity);
        InitializeProviderSelectorArrays(gambitProviderSelectorCapacity);
        InitializeSupportRuleArrays(gambitSupportRuleCapacity, stepCapacity);
    }

    private void InitializeGambitArrays(int gambitCapacity)
    {
        GambitKindIds = new int[gambitCapacity + 1];
        GambitIds = new int[gambitCapacity + 1];
        GambitCooldownTicks = new int[gambitCapacity + 1];
        GambitEntityCooldownTicks = new int[gambitCapacity + 1];
        GambitSuccessChances = new float[gambitCapacity + 1];
        GambitTargetKindIds = new int[gambitCapacity + 1];
        GambitTargetModes = new int[gambitCapacity + 1];
        GambitPreconditionStarts = new int[gambitCapacity + 1];
        GambitPreconditionCounts = new int[gambitCapacity + 1];
        GambitExpressionStarts = new int[gambitCapacity + 1];
        GambitExpressionCounts = new int[gambitCapacity + 1];
        GambitRewardExpressionStarts = new int[gambitCapacity + 1];
        GambitRewardExpressionCounts = new int[gambitCapacity + 1];
        GambitStepStarts = new int[gambitCapacity + 1];
        GambitStepCounts = new int[gambitCapacity + 1];
        GambitProviderSelectorStarts = new int[gambitCapacity + 1];
        GambitProviderSelectorCounts = new int[gambitCapacity + 1];
        GambitSupportRuleStarts = new int[gambitCapacity + 1];
        GambitSupportRuleCounts = new int[gambitCapacity + 1];
        GambitGoalEffectStarts = new int[gambitCapacity + 1];
        GambitGoalEffectCounts = new int[gambitCapacity + 1];
    }

    private void InitializeStepArrays(int stepCapacity)
    {
        GoalEffectScopeCodes = new int[stepCapacity + 1];
        GoalEffectValueIds = new int[stepCapacity + 1];
        GoalEffectKindIds = new int[stepCapacity + 1];
        PreconditionCodes = new int[stepCapacity + 1];
        PreA = new int[stepCapacity + 1];
        PreB = new int[stepCapacity + 1];
        PreC = new int[stepCapacity + 1];
        PreD = new int[stepCapacity + 1];
        PreX = new float[stepCapacity + 1];
        PreY = new float[stepCapacity + 1];
        ExpressionCodes = new int[stepCapacity + 1];
        ExpressionA = new int[stepCapacity + 1];
        ExpressionX = new float[stepCapacity + 1];
        RewardExpressionCodes = new int[stepCapacity + 1];
        RewardExpressionA = new int[stepCapacity + 1];
        RewardExpressionX = new float[stepCapacity + 1];
        StepOperationCodes = new int[stepCapacity + 1];
        StepA = new int[stepCapacity + 1];
        StepB = new int[stepCapacity + 1];
        StepC = new int[stepCapacity + 1];
        StepD = new int[stepCapacity + 1];
        StepX = new float[stepCapacity + 1];
        StepY = new float[stepCapacity + 1];
        StepExpressionStarts = new int[stepCapacity + 1];
        StepExpressionCounts = new int[stepCapacity + 1];
        MutationExpressionCodes = new int[stepCapacity + 1];
        MutationExpressionA = new int[stepCapacity + 1];
        MutationExpressionX = new float[stepCapacity + 1];
        TraitModifierGambitIds = new int[stepCapacity + 1];
        TraitModifierTraitIds = new int[stepCapacity + 1];
        TraitModifierBearerCodes = new int[stepCapacity + 1];
        TraitModifierScoreDeltas = new float[stepCapacity + 1];
        TraitModifierSuccessChanceDeltas = new float[stepCapacity + 1];
    }

    private void InitializeObjectiveArrays(int gambitObjectiveCapacity)
    {
        ObjectiveKindIds = new int[gambitObjectiveCapacity + 1];
        ObjectiveCodes = new int[gambitObjectiveCapacity + 1];
        ObjectiveEffectScopeCodes = new int[gambitObjectiveCapacity + 1];
        ObjectiveEffectValueIds = new int[gambitObjectiveCapacity + 1];
        ObjectiveEffectKindIds = new int[gambitObjectiveCapacity + 1];
        ObjectiveAttributeIds = new int[gambitObjectiveCapacity + 1];
        ObjectiveMinimums = new float[gambitObjectiveCapacity + 1];
        ObjectiveMaximums = new float[gambitObjectiveCapacity + 1];
        ObjectiveDefaults = new float[gambitObjectiveCapacity + 1];
        ObjectiveBaselines = new float[gambitObjectiveCapacity + 1];
        ObjectiveBaseUrgencies = new float[gambitObjectiveCapacity + 1];
    }

    private void InitializeProviderSelectorArrays(int gambitProviderSelectorCapacity)
    {
        ProviderSelectorCodes = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorKindIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorValueIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorRelationshipKindIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorFactIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorMinimumStrengths = new float[gambitProviderSelectorCapacity + 1];
    }

    private void InitializeSupportRuleArrays(int gambitSupportRuleCapacity, int stepCapacity)
    {
        SupportEffectScopeCodes = new int[gambitSupportRuleCapacity + 1];
        SupportEffectValueIds = new int[gambitSupportRuleCapacity + 1];
        SupportEffectKindIds = new int[gambitSupportRuleCapacity + 1];
        SupportExpressionStarts = new int[gambitSupportRuleCapacity + 1];
        SupportExpressionCounts = new int[gambitSupportRuleCapacity + 1];
        SupportDesiredMinimums = new float[gambitSupportRuleCapacity + 1];
        SupportUrgencyScales = new float[gambitSupportRuleCapacity + 1];
        SupportExpressionCodes = new int[stepCapacity + 1];
        SupportExpressionA = new int[stepCapacity + 1];
        SupportExpressionX = new float[stepCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Gambits;

    public int SchemaVersion => 1;

    internal DecisionModifier ReadDecisionModifier(int modifier) =>
        new(
            TraitModifierGambitIds[modifier],
            0,
            TraitModifierTraitIds[modifier],
            TraitModifierBearerCodes[modifier],
            TraitModifierScoreDeltas[modifier],
            TraitModifierSuccessChanceDeltas[modifier]);
}
