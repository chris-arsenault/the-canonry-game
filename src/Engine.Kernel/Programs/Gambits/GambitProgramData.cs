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
    internal int[] GambitStepStarts = [];
    internal int[] GambitStepCounts = [];
    internal int[] GambitProviderSelectorStarts = [];
    internal int[] GambitProviderSelectorCounts = [];
    internal int[] GambitChronicleGranted = [];
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
    internal int[] ProviderSelectorCodes = [];
    internal int[] ProviderSelectorKindIds = [];
    internal int[] ProviderSelectorValueIds = [];
    internal int[] ProviderSelectorRelationshipKindIds = [];
    internal float[] ProviderSelectorMinimumStrengths = [];
    internal int[] TraitModifierGambitIds = [];
    internal int[] TraitModifierTraitIds = [];
    internal int[] TraitModifierBearerCodes = [];
    internal float[] TraitModifierScoreDeltas = [];
    internal float[] TraitModifierSuccessChanceDeltas = [];
    internal float[] AgeScoreMultipliers = [];
    internal float[] AgeSuccessMultipliers = [];
    internal int GambitCount;
    internal int PreconditionCount;
    internal int ExpressionCount;
    internal int StepCount;
    internal int MutationExpressionCount;
    internal int ProviderSelectorCount;
    internal int TraitModifierCount;
    internal int OpenGambitSlot;

    public GambitProgramData(
        int gambitCapacity,
        int stepCapacity,
        int gambitProviderSelectorCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gambitProviderSelectorCapacity);
        InitializeGambitArrays(gambitCapacity);
        InitializeStepArrays(stepCapacity);
        InitializeProviderSelectorArrays(gambitProviderSelectorCapacity);
    }

    private void InitializeGambitArrays(int gambitCapacity)
    {
        GambitKindIds = new int[gambitCapacity + 1];
        GambitIds = new int[gambitCapacity + 1];
        AgeScoreMultipliers = new float[gambitCapacity + 1];
        global::System.Array.Fill(AgeScoreMultipliers, 1f);
        GambitCooldownTicks = new int[gambitCapacity + 1];
        GambitEntityCooldownTicks = new int[gambitCapacity + 1];
        GambitSuccessChances = new float[gambitCapacity + 1];
        AgeSuccessMultipliers = new float[gambitCapacity + 1];
        global::System.Array.Fill(AgeSuccessMultipliers, 1f);
        GambitTargetKindIds = new int[gambitCapacity + 1];
        GambitTargetModes = new int[gambitCapacity + 1];
        GambitPreconditionStarts = new int[gambitCapacity + 1];
        GambitPreconditionCounts = new int[gambitCapacity + 1];
        GambitExpressionStarts = new int[gambitCapacity + 1];
        GambitExpressionCounts = new int[gambitCapacity + 1];
        GambitStepStarts = new int[gambitCapacity + 1];
        GambitStepCounts = new int[gambitCapacity + 1];
        GambitProviderSelectorStarts = new int[gambitCapacity + 1];
        GambitProviderSelectorCounts = new int[gambitCapacity + 1];
        GambitChronicleGranted = new int[gambitCapacity + 1];
    }

    private void InitializeStepArrays(int stepCapacity)
    {
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

    private void InitializeProviderSelectorArrays(int gambitProviderSelectorCapacity)
    {
        ProviderSelectorCodes = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorKindIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorValueIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorRelationshipKindIds = new int[gambitProviderSelectorCapacity + 1];
        ProviderSelectorMinimumStrengths = new float[gambitProviderSelectorCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Gambits;

    public int SchemaVersion => 3;

    internal DecisionModifier ReadDecisionModifier(int modifier) =>
        new(
            TraitModifierGambitIds[modifier],
            0,
            TraitModifierTraitIds[modifier],
            TraitModifierBearerCodes[modifier],
            TraitModifierScoreDeltas[modifier],
            TraitModifierSuccessChanceDeltas[modifier]);
}
