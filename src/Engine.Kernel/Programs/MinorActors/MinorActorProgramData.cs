namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramData : IKernelProgramData
{
    internal readonly int[] SourceKindIds;
    internal readonly int[] SourceSubtypeIds;
    internal readonly int[] PurposeCodes;
    internal readonly int[] ActorKindCodes;
    internal readonly int[] ActionCodes;
    internal readonly int[] PlaneCodes;
    internal readonly int[] TtlPulses;
    internal readonly int[] EmitCadencePulses;
    internal readonly int[] MaxActivePerSource;
    internal readonly int[] SpawnRadii;
    internal readonly int[] MoveChancePermille;
    internal readonly int[] ActionCadencePulses;
    internal readonly float[] WorkDeltas;
    internal readonly int[] VerbSourceRuleIds;
    internal readonly int[] VerbIds;
    internal readonly int[] VerbActionCodes;
    internal readonly int[] VerbTargetingCodes;
    internal readonly int[] VerbPlaneCodes;
    internal readonly int[] VerbRadii;
    internal readonly int[] VerbSampleCounts;
    internal readonly float[] VerbWeights;
    internal readonly float[] VerbWorkDeltaMultipliers;

    public MinorActorProgramData(int ruleCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruleCapacity);
        var verbCapacity = ruleCapacity * 8;
        SourceKindIds = new int[ruleCapacity + 1];
        SourceSubtypeIds = new int[ruleCapacity + 1];
        PurposeCodes = new int[ruleCapacity + 1];
        ActorKindCodes = new int[ruleCapacity + 1];
        ActionCodes = new int[ruleCapacity + 1];
        PlaneCodes = new int[ruleCapacity + 1];
        TtlPulses = new int[ruleCapacity + 1];
        EmitCadencePulses = new int[ruleCapacity + 1];
        MaxActivePerSource = new int[ruleCapacity + 1];
        SpawnRadii = new int[ruleCapacity + 1];
        MoveChancePermille = new int[ruleCapacity + 1];
        ActionCadencePulses = new int[ruleCapacity + 1];
        WorkDeltas = new float[ruleCapacity + 1];
        VerbSourceRuleIds = new int[verbCapacity + 1];
        VerbIds = new int[verbCapacity + 1];
        VerbActionCodes = new int[verbCapacity + 1];
        VerbTargetingCodes = new int[verbCapacity + 1];
        VerbPlaneCodes = new int[verbCapacity + 1];
        VerbRadii = new int[verbCapacity + 1];
        VerbSampleCounts = new int[verbCapacity + 1];
        VerbWeights = new float[verbCapacity + 1];
        VerbWorkDeltaMultipliers = new float[verbCapacity + 1];
    }

    public int ProgramId => KernelProgramCodes.MinorActors;

    public int SchemaVersion => 2;

    public int RuleCapacity => SourceKindIds.Length - 1;

    public int VerbCapacity => VerbIds.Length - 1;

    public int RuleCount { get; private set; }

    public int VerbCount { get; private set; }

    public int RegisterEmitterRule(
        int sourceKindId,
        int sourceSubtypeId,
        int purposeCode,
        int actorKindCode,
        int actionCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta)
    {
        ValidateRule(
            sourceKindId,
            sourceSubtypeId,
            purposeCode,
            actorKindCode,
            actionCode,
            planeCode,
            ttlPulses,
            emitCadencePulses,
            maxActivePerSource,
            spawnRadius,
            moveChancePermille,
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
        ActionCodes[rule] = actionCode;
        PlaneCodes[rule] = planeCode;
        TtlPulses[rule] = ttlPulses;
        EmitCadencePulses[rule] = emitCadencePulses;
        MaxActivePerSource[rule] = maxActivePerSource;
        SpawnRadii[rule] = spawnRadius;
        MoveChancePermille[rule] = moveChancePermille;
        ActionCadencePulses[rule] = actionCadencePulses;
        WorkDeltas[rule] = workDelta;
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
        float weight,
        float workDeltaMultiplier)
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
        return verb;
    }

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
        int actionCode,
        int planeCode,
        int ttlPulses,
        int emitCadencePulses,
        int maxActivePerSource,
        int spawnRadius,
        int moveChancePermille,
        int actionCadencePulses,
        float workDelta)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(sourceSubtypeId);
        ValidatePurposeCode(purposeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actorKindCode);
        ValidateActionCode(actionCode);
        ValidatePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ttlPulses);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emitCadencePulses);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxActivePerSource);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(spawnRadius);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(moveChancePermille, 1000);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actionCadencePulses);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(workDelta);
    }

    private static void ValidateVerb(
        int sourceRuleId,
        int verbId,
        int actionCode,
        int targetingCode,
        int planeCode,
        int radius,
        int sampleCount,
        float weight,
        float workDeltaMultiplier)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRuleId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(verbId);
        ValidateActionCode(actionCode);
        ValidateTargetingCode(targetingCode);
        ValidatePlaneCode(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleCount);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(weight);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(workDeltaMultiplier);
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
                MinorActorPurposeCodes.RoadLabor))
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
}
