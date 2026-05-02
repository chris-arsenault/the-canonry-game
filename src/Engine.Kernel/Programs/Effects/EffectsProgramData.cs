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
    internal int AttributeDeltaRuleCount;

    public EffectsProgramData(int effectRuleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(effectRuleCapacity);
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
    }

    public int ProgramId => KernelProgramCodes.Effects;

    public int SchemaVersion => 1;
}
