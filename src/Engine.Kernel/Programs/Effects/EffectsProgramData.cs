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
    internal readonly FixedScalar[] AttributeDeltaThresholds;
    internal readonly FixedScalar[] AttributeDeltaMinimumRelationshipStrengths;
    internal readonly FixedScalar[] AttributeDeltaDeltas;
    internal readonly int[] AttributeDeltaRequiredTargetTraitIds;
    internal readonly int[] AttributeDeltaRequiredRelatedTraitIds;
    internal readonly int[] AttributeDeltaCultureIds;
    internal readonly int[] AttributeDeltaCultureSlotIds;
    internal readonly int[] AttributeDeltaCultureSlotPolarityCodes;
    internal readonly FixedScalar[] AttributeDeltaSupportScales;
    internal readonly int[] AttributeDeltaCoverageOwnerKindIds;
    internal readonly int[] AttributeDeltaCoverageOwnerSubtypeIds;
    internal readonly int[] FirstAttributeDeltaRuleByKind;
    internal readonly int[] LastAttributeDeltaRuleByKind;
    internal readonly int[] NextAttributeDeltaRuleByKind;
    internal readonly int[] TagRuleCoverageOwnerKindIds;
    internal readonly int[] TagRuleCoverageOwnerSubtypeIds;
    internal readonly int[] TagRuleCoveragePlaneCodes;
    internal int AttributeDeltaRuleCount;
    internal readonly int[] TagRuleKindIds;
    internal readonly int[] TagRuleTagIds;
    internal readonly int[] TagRuleRelationshipKindIds;
    internal readonly int[] TagRuleDirectionCodes;
    internal readonly FixedScalar[] TagRuleSetThresholds;
    internal readonly FixedScalar[] TagRuleClearThresholds;
    internal readonly int[] FirstTagRuleByKind;
    internal readonly int[] LastTagRuleByKind;
    internal readonly int[] NextTagRuleByKind;
    internal int TagRuleCount;
    internal readonly int[] LifecycleKindIds;
    internal readonly int[] LifecycleAttributeIds;
    internal readonly int[] LifecycleTargetTicks;
    internal readonly int[] LifecycleNominalHealthUnits;
    internal readonly FixedScalar[] LifecycleFlatDrainShares;
    internal readonly FixedScalar[] LifecycleVariances;
    internal readonly int[] LifecycleRuleByKind;
    internal int LifecycleRuleCount;

#pragma warning disable MA0051
    public EffectsProgramData(
        int effectRuleCapacity,
        int lifecycleRuleCapacity = 256,
        int kindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(effectRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(lifecycleRuleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        AttributeDeltaSupportScales = new FixedScalar[effectRuleCapacity + 1];
        AttributeDeltaCoverageOwnerKindIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCoverageOwnerSubtypeIds = new int[effectRuleCapacity + 1];
        TagRuleCoverageOwnerKindIds = new int[effectRuleCapacity + 1];
        TagRuleCoverageOwnerSubtypeIds = new int[effectRuleCapacity + 1];
        TagRuleCoveragePlaneCodes = new int[effectRuleCapacity + 1];
        TagRuleKindIds = new int[effectRuleCapacity + 1];
        TagRuleTagIds = new int[effectRuleCapacity + 1];
        TagRuleRelationshipKindIds = new int[effectRuleCapacity + 1];
        TagRuleDirectionCodes = new int[effectRuleCapacity + 1];
        TagRuleSetThresholds = new FixedScalar[effectRuleCapacity + 1];
        TagRuleClearThresholds = new FixedScalar[effectRuleCapacity + 1];
        AttributeDeltaKindIds = new int[effectRuleCapacity + 1];
        AttributeDeltaAttributeIds = new int[effectRuleCapacity + 1];
        AttributeDeltaConditionCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaRelationshipKindIds = new int[effectRuleCapacity + 1];
        AttributeDeltaRelationshipDirectionCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaPlaneCodes = new int[effectRuleCapacity + 1];
        AttributeDeltaMaxDistances = new int[effectRuleCapacity + 1];
        AttributeDeltaThresholds = new FixedScalar[effectRuleCapacity + 1];
        AttributeDeltaMinimumRelationshipStrengths = new FixedScalar[effectRuleCapacity + 1];
        AttributeDeltaDeltas = new FixedScalar[effectRuleCapacity + 1];
        AttributeDeltaRequiredTargetTraitIds = new int[effectRuleCapacity + 1];
        AttributeDeltaRequiredRelatedTraitIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureSlotIds = new int[effectRuleCapacity + 1];
        AttributeDeltaCultureSlotPolarityCodes = new int[effectRuleCapacity + 1];
        FirstAttributeDeltaRuleByKind = new int[kindCapacity + 1];
        LastAttributeDeltaRuleByKind = new int[kindCapacity + 1];
        NextAttributeDeltaRuleByKind = new int[effectRuleCapacity + 1];
        FirstTagRuleByKind = new int[kindCapacity + 1];
        LastTagRuleByKind = new int[kindCapacity + 1];
        NextTagRuleByKind = new int[effectRuleCapacity + 1];
        LifecycleKindIds = new int[lifecycleRuleCapacity + 1];
        LifecycleAttributeIds = new int[lifecycleRuleCapacity + 1];
        LifecycleTargetTicks = new int[lifecycleRuleCapacity + 1];
        LifecycleNominalHealthUnits = new int[lifecycleRuleCapacity + 1];
        LifecycleFlatDrainShares = new FixedScalar[lifecycleRuleCapacity + 1];
        LifecycleVariances = new FixedScalar[lifecycleRuleCapacity + 1];
        LifecycleRuleByKind = new int[kindCapacity + 1];
    }
#pragma warning restore MA0051

    public int ProgramId => KernelProgramCodes.Effects;

    public int SchemaVersion => 7;
}
