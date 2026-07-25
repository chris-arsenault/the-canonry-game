namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramData : IKernelProgramData
{
    internal readonly int[] SourceKindIds;
    internal readonly int[] SourceSubtypeIds;
    internal readonly int[] PurposeCodes;
    internal readonly int[] ActorKindCodes;
    internal readonly int[] PlaneCodes;
    internal readonly int[] TtlPulses;
    internal readonly int[] EmitCadencePulses;
    internal readonly int[] MaxActivePerSource;
    internal readonly int[] SpawnRadii;
    internal readonly int[] ActionCadencePulses;
    internal readonly FixedScalar[] WorkDeltas;
    internal readonly int[] RuleMaxVerbRadii;
    internal readonly int[] VerbSourceRuleIds;
    internal readonly int[] VerbIds;
    internal readonly int[] VerbActionCodes;
    internal readonly int[] VerbTargetingCodes;
    internal readonly int[] VerbPlaneCodes;
    internal readonly int[] VerbRadii;
    internal readonly int[] VerbSampleCounts;
    internal readonly FixedScalar[] VerbWeights;
    internal readonly FixedScalar[] VerbWorkDeltaMultipliers;
    internal readonly int[] FirstRuleBySourceKind;
    internal readonly int[] LastRuleBySourceKind;
    internal readonly int[] NextRuleBySourceKind;
    internal readonly int[] FirstVerbBySourceRule;
    internal readonly int[] LastVerbBySourceRule;
    internal readonly int[] NextVerbBySourceRule;

    public MinorActorProgramData(int ruleCapacity, int kindCapacity = 256)
    {
        NumericGuard.ThrowIfNegativeOrZero(ruleCapacity);
        NumericGuard.ThrowIfNegativeOrZero(kindCapacity);
        var verbCapacity = ruleCapacity * 8;
        SourceKindIds = new int[ruleCapacity + 1];
        SourceSubtypeIds = new int[ruleCapacity + 1];
        PurposeCodes = new int[ruleCapacity + 1];
        ActorKindCodes = new int[ruleCapacity + 1];
        PlaneCodes = new int[ruleCapacity + 1];
        TtlPulses = new int[ruleCapacity + 1];
        EmitCadencePulses = new int[ruleCapacity + 1];
        MaxActivePerSource = new int[ruleCapacity + 1];
        SpawnRadii = new int[ruleCapacity + 1];
        ActionCadencePulses = new int[ruleCapacity + 1];
        WorkDeltas = new FixedScalar[ruleCapacity + 1];
        RuleMaxVerbRadii = new int[ruleCapacity + 1];
        VerbSourceRuleIds = new int[verbCapacity + 1];
        VerbIds = new int[verbCapacity + 1];
        VerbActionCodes = new int[verbCapacity + 1];
        VerbTargetingCodes = new int[verbCapacity + 1];
        VerbPlaneCodes = new int[verbCapacity + 1];
        VerbRadii = new int[verbCapacity + 1];
        VerbSampleCounts = new int[verbCapacity + 1];
        VerbWeights = new FixedScalar[verbCapacity + 1];
        VerbWorkDeltaMultipliers = new FixedScalar[verbCapacity + 1];
        FirstRuleBySourceKind = new int[kindCapacity + 1];
        LastRuleBySourceKind = new int[kindCapacity + 1];
        NextRuleBySourceKind = new int[ruleCapacity + 1];
        FirstVerbBySourceRule = new int[ruleCapacity + 1];
        LastVerbBySourceRule = new int[ruleCapacity + 1];
        NextVerbBySourceRule = new int[verbCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 4;

    public int RuleCapacity => SourceKindIds.Length - 1;

    public int VerbCapacity => VerbIds.Length - 1;

    public int RuleCount { get; private set; }

    public int VerbCount { get; private set; }

    public int RegisterEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int actionCadencePulses,
        FixedScalar workDelta)
    {
        ValidateRule(
            sourceKindId,
            sourceSubtypeId,
            purposeCode,
            actorKindCode,
            planeCode,
            ttlPulses,
            emitCadencePulses,
            maxActivePerSource,
            spawnRadius,
            actionCadencePulses,
            workDelta);

        if (RuleCount >= RuleCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        var rule = ++RuleCount;
        SourceKindIds[rule] = sourceKindId;
        SourceSubtypeIds[rule] = sourceSubtypeId;
        PurposeCodes[rule] = purposeCode;
        ActorKindCodes[rule] = actorKindCode;
        PlaneCodes[rule] = planeCode;
        TtlPulses[rule] = ttlPulses;
        EmitCadencePulses[rule] = emitCadencePulses;
        MaxActivePerSource[rule] = maxActivePerSource;
        SpawnRadii[rule] = spawnRadius;
        ActionCadencePulses[rule] = actionCadencePulses;
        WorkDeltas[rule] = workDelta;
        AppendIndex(
            sourceKindId,
            rule,
            FirstRuleBySourceKind,
            LastRuleBySourceKind,
            NextRuleBySourceKind);
        return rule;
    }

    public int RegisterVerb(
        int sourceRuleId,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        FixedScalar weight,
        FixedScalar workDeltaMultiplier)
    {
        ValidateVerb(
            sourceRuleId,
            verbId,
            actionCode,
            targetingCode,
            planeCode,
            radius,
            sampleCount,
            weight,
            workDeltaMultiplier);

        if (VerbCount >= VerbCapacity)
        {
            throw new global::System.InvalidOperationException();
        }

        var verb = ++VerbCount;
        VerbSourceRuleIds[verb] = sourceRuleId;
        VerbIds[verb] = verbId;
        VerbActionCodes[verb] = actionCode;
        VerbTargetingCodes[verb] = targetingCode;
        VerbPlaneCodes[verb] = planeCode;
        VerbRadii[verb] = radius;
        VerbSampleCounts[verb] = sampleCount;
        VerbWeights[verb] = weight;
        VerbWorkDeltaMultipliers[verb] = workDeltaMultiplier;
        RuleMaxVerbRadii[sourceRuleId] = global::System.Math.Max(RuleMaxVerbRadii[sourceRuleId], radius);
        AppendIndex(
            sourceRuleId,
            verb,
            FirstVerbBySourceRule,
            LastVerbBySourceRule,
            NextVerbBySourceRule);
        return verb;
    }

    public int FirstVerbForRule(int rule) =>
        (uint)rule < (uint)FirstVerbBySourceRule.Length ? FirstVerbBySourceRule[rule] : 0;

    public bool RuleHasVerbs(int rule) => FirstVerbForRule(rule) > 0;

    public int NextVerbForRule(int verb) => NextVerbBySourceRule[verb];

    public int FirstRuleForSourceKind(int kindId) =>
        (uint)kindId < (uint)FirstRuleBySourceKind.Length ? FirstRuleBySourceKind[kindId] : 0;

    public int NextRuleForSourceKind(int rule) => NextRuleBySourceKind[rule];

    /// <summary>Domain-configured relationship kind that carries a campaign (0 = campaigns disabled).</summary>
    internal int CampaignRelationshipKindId;

    public bool RuleMatches(WorldState world, int entityId, int rule)
    {
        if (!world.EntityIsActive(entityId) ||
            world.EntityKindId(entityId) != SourceKindIds[rule])
        {
            return false;
        }

        var subtypeId = SourceSubtypeIds[rule];
        return subtypeId == 0 || world.EntitySubtypeId(entityId) == subtypeId;
    }

    private static void ValidateRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int actionCadencePulses,
        FixedScalar workDelta)
    {
        NumericGuard.ThrowIfNegativeOrZero(sourceKindId);
        NumericGuard.ThrowIfNegative(sourceSubtypeId);
        ValidatePurposeCode(purposeCode);
        NumericGuard.ThrowIfNegativeOrZero(actorKindCode);
        ValidatePlaneCode(planeCode);
        NumericGuard.ThrowIfNegativeOrZero(ttlPulses);
        NumericGuard.ThrowIfNegativeOrZero(emitCadencePulses);
        NumericGuard.ThrowIfNegativeOrZero(maxActivePerSource);
        NumericGuard.ThrowIfNegative(spawnRadius);
        NumericGuard.ThrowIfNegativeOrZero(actionCadencePulses);
        NumericGuard.ThrowIfNegative(workDelta);
    }

    private static void ValidateVerb(
        int sourceRuleId,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        FixedScalar weight,
        FixedScalar workDeltaMultiplier)
    {
        NumericGuard.ThrowIfNegativeOrZero(sourceRuleId);
        NumericGuard.ThrowIfNegativeOrZero(verbId);
        ValidateActionCode(actionCode);
        ValidateTargetingCode(targetingCode);
        ValidatePlaneCode(planeCode);
        NumericGuard.ThrowIfNegative(radius);
        NumericGuard.ThrowIfNegativeOrZero(sampleCount);
        NumericGuard.ThrowIfNegative(weight);
        NumericGuard.ThrowIfNegative(workDeltaMultiplier);
    }

    private static void ValidateActionCode(int actionCode)
    {
        if (actionCode is not (
                MinorActorActionCodes.CultureExpansion or
                MinorActorActionCodes.RoadMaintenance or
                MinorActorActionCodes.CultureCleanup or
                MinorActorActionCodes.LifecycleMaintenance))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(actionCode));
        }
    }

    private static void ValidatePurposeCode(int purposeCode)
    {
        if (purposeCode is not (
                MinorActorPurposeCodes.CulturalLabor or
                MinorActorPurposeCodes.RoadLabor or
                MinorActorPurposeCodes.CampaignLabor))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(purposeCode));
        }
    }

    private static void ValidateTargetingCode(int targetingCode)
    {
        if (targetingCode is not (
                MinorActorVerbTargetingCodes.ReinforceHomeMass or
                MinorActorVerbTargetingCodes.ExtendConnectedBorder or
                MinorActorVerbTargetingCodes.MaintainRoad or
                MinorActorVerbTargetingCodes.BridgeGap or
                MinorActorVerbTargetingCodes.ReduceOtherCulture or
                MinorActorVerbTargetingCodes.MaintainSourceLifecycle))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(targetingCode));
        }
    }

    private static void ValidatePlaneCode(int planeCode)
    {
        if (planeCode is not (Engine.Constants.PlaneCodes.Physical or Engine.Constants.PlaneCodes.Semantic))
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(planeCode));
        }
    }

    private static void AppendIndex(
        int key,
        int item,
        int[] firstByKey,
        int[] lastByKey,
        int[] nextByItem)
    {
        if ((uint)key >= (uint)firstByKey.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = lastByKey[key];
        if (previous > 0)
        {
            nextByItem[previous] = item;
        }
        else
        {
            firstByKey[key] = item;
        }

        lastByKey[key] = item;
    }
}
