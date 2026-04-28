namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class ProcessProgramData : IKernelProgramData
{
    internal int[] ProcessKindIds = [];
    internal int[] ProcessIds = [];
    internal int[] ProcessCooldownTicks = [];
    internal int[] ProcessEntityCooldownTicks = [];
    internal int[] ProcessTargetKindIds = [];
    internal int[] ProcessTargetModes = [];
    internal int[] ProcessPreconditionStarts = [];
    internal int[] ProcessPreconditionCounts = [];
    internal int[] ProcessExpressionStarts = [];
    internal int[] ProcessExpressionCounts = [];
    internal int[] ProcessRewardExpressionStarts = [];
    internal int[] ProcessRewardExpressionCounts = [];
    internal int[] ProcessStepStarts = [];
    internal int[] ProcessStepCounts = [];
    internal int[] ProcessProviderSelectorStarts = [];
    internal int[] ProcessProviderSelectorCounts = [];
    internal int[] ProcessSupportRuleStarts = [];
    internal int[] ProcessSupportRuleCounts = [];
    internal int[] ProcessGoalEffectStarts = [];
    internal int[] ProcessGoalEffectCounts = [];
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
    internal int ProcessCount;
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
    internal int OpenProcessSlot;

    public ProcessProgramData(
        int processCapacity,
        int stepCapacity,
        int processObjectiveCapacity,
        int processProviderSelectorCapacity,
        int processSupportRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processObjectiveCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processProviderSelectorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processSupportRuleCapacity);
        InitializeProcessArrays(processCapacity);
        InitializeStepArrays(stepCapacity);
        InitializeObjectiveArrays(processObjectiveCapacity);
        InitializeProviderSelectorArrays(processProviderSelectorCapacity);
        InitializeSupportRuleArrays(processSupportRuleCapacity, stepCapacity);
    }

    private void InitializeProcessArrays(int processCapacity)
    {
        ProcessKindIds = new int[processCapacity + 1];
        ProcessIds = new int[processCapacity + 1];
        ProcessCooldownTicks = new int[processCapacity + 1];
        ProcessEntityCooldownTicks = new int[processCapacity + 1];
        ProcessTargetKindIds = new int[processCapacity + 1];
        ProcessTargetModes = new int[processCapacity + 1];
        ProcessPreconditionStarts = new int[processCapacity + 1];
        ProcessPreconditionCounts = new int[processCapacity + 1];
        ProcessExpressionStarts = new int[processCapacity + 1];
        ProcessExpressionCounts = new int[processCapacity + 1];
        ProcessRewardExpressionStarts = new int[processCapacity + 1];
        ProcessRewardExpressionCounts = new int[processCapacity + 1];
        ProcessStepStarts = new int[processCapacity + 1];
        ProcessStepCounts = new int[processCapacity + 1];
        ProcessProviderSelectorStarts = new int[processCapacity + 1];
        ProcessProviderSelectorCounts = new int[processCapacity + 1];
        ProcessSupportRuleStarts = new int[processCapacity + 1];
        ProcessSupportRuleCounts = new int[processCapacity + 1];
        ProcessGoalEffectStarts = new int[processCapacity + 1];
        ProcessGoalEffectCounts = new int[processCapacity + 1];
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
    }

    private void InitializeObjectiveArrays(int processObjectiveCapacity)
    {
        ObjectiveKindIds = new int[processObjectiveCapacity + 1];
        ObjectiveCodes = new int[processObjectiveCapacity + 1];
        ObjectiveEffectScopeCodes = new int[processObjectiveCapacity + 1];
        ObjectiveEffectValueIds = new int[processObjectiveCapacity + 1];
        ObjectiveEffectKindIds = new int[processObjectiveCapacity + 1];
        ObjectiveAttributeIds = new int[processObjectiveCapacity + 1];
        ObjectiveMinimums = new float[processObjectiveCapacity + 1];
        ObjectiveMaximums = new float[processObjectiveCapacity + 1];
        ObjectiveDefaults = new float[processObjectiveCapacity + 1];
        ObjectiveBaselines = new float[processObjectiveCapacity + 1];
        ObjectiveBaseUrgencies = new float[processObjectiveCapacity + 1];
    }

    private void InitializeProviderSelectorArrays(int processProviderSelectorCapacity)
    {
        ProviderSelectorCodes = new int[processProviderSelectorCapacity + 1];
        ProviderSelectorKindIds = new int[processProviderSelectorCapacity + 1];
        ProviderSelectorValueIds = new int[processProviderSelectorCapacity + 1];
        ProviderSelectorRelationshipKindIds = new int[processProviderSelectorCapacity + 1];
        ProviderSelectorFactIds = new int[processProviderSelectorCapacity + 1];
        ProviderSelectorMinimumStrengths = new float[processProviderSelectorCapacity + 1];
    }

    private void InitializeSupportRuleArrays(int processSupportRuleCapacity, int stepCapacity)
    {
        SupportEffectScopeCodes = new int[processSupportRuleCapacity + 1];
        SupportEffectValueIds = new int[processSupportRuleCapacity + 1];
        SupportEffectKindIds = new int[processSupportRuleCapacity + 1];
        SupportExpressionStarts = new int[processSupportRuleCapacity + 1];
        SupportExpressionCounts = new int[processSupportRuleCapacity + 1];
        SupportDesiredMinimums = new float[processSupportRuleCapacity + 1];
        SupportUrgencyScales = new float[processSupportRuleCapacity + 1];
        SupportExpressionCodes = new int[stepCapacity + 1];
        SupportExpressionA = new int[stepCapacity + 1];
        SupportExpressionX = new float[stepCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Processes;

    public int SchemaVersion => 1;
}
