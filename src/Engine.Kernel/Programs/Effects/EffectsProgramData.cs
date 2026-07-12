namespace Engine.Kernel;

using Engine.Kernel.Programs;

internal sealed partial class EffectsProgramData : IKernelProgramData
{
    internal readonly int[] AttributeDeltaKindIds;
    internal readonly int[] AttributeDeltaAttributeIds;
    internal readonly int[] AttributeDeltaConditionCodes;
    internal readonly int[] AttributeDeltaRelationshipKindIds;
    internal readonly int[] AttributeDeltaRelationshipDirectionCodes;
    internal readonly int[] AttributeDeltaPlaneCodes;
    internal readonly int[] AttributeDeltaMaxDistances;
    internal readonly float[] AttributeDeltaThresholds;
    internal readonly float[] AttributeDeltaMinimumRelationshipStrengths;
    internal readonly float[] AttributeDeltaDeltas;
    internal readonly int[] AttributeDeltaRequiredTargetTraitIds;
    internal readonly int[] AttributeDeltaRequiredRelatedTraitIds;
    internal readonly int[] AttributeDeltaCultureIds;
    internal readonly int[] AttributeDeltaCultureSlotIds;
    internal readonly int[] AttributeDeltaCultureSlotPolarityCodes;
    internal int AttributeDeltaRuleCount;
    internal readonly int[] TagRuleKindIds;
    internal readonly int[] TagRuleTagIds;
    internal readonly int[] TagRuleRelationshipKindIds;
    internal readonly int[] TagRuleDirectionCodes;
    internal readonly float[] TagRuleSetThresholds;
    internal readonly float[] TagRuleClearThresholds;
    internal int TagRuleCount;

    public EffectsProgramData(int effectRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(effectRuleCapacity);
        TagRuleKindIds = new int[effectRuleCapacity + 1];
        TagRuleTagIds = new int[effectRuleCapacity + 1];
        TagRuleRelationshipKindIds = new int[effectRuleCapacity + 1];
        TagRuleDirectionCodes = new int[effectRuleCapacity + 1];
        TagRuleSetThresholds = new float[effectRuleCapacity + 1];
        TagRuleClearThresholds = new float[effectRuleCapacity + 1];
        AttributeDeltaKindIds = new int[effectRuleCapacity + 1];
        AttributeDeltaAttributeIds = new int[effectRuleCapacity + 1];
        AttributeDeltaConditionCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaRelationshipKindIds = new int[effectRuleCapacity + 1];
        AttributeDeltaRelationshipDirectionCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaPlaneCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaMaxDistances = new int[effectRuleCapacity + 1];
        AttributeDeltaThresholds = new float[effectRuleCapacity + 1];
        AttributeDeltaMinimumRelationshipStrengths = new float[effectRuleCapacity + 1];
        AttributeDeltaDeltas = new float[effectRuleCapacity + 1];
        AttributeDeltaRequiredTargetTraitIds = new int[effectRuleCapacity + 1];
        AttributeDeltaRequiredRelatedTraitIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureSlotIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureSlotPolarityCodes = new int[effectRuleCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.Effects;

    public int SchemaVersion => 3;
}
