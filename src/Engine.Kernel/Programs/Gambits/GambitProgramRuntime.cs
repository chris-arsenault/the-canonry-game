namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime : IKernelProgramRuntime<GambitProgramData, GambitProgramState>
{
    internal const int GambitHistoryLength = 60;
    private const int RecencyWindow = 10;
    private const float RecencyDecayFactor = 0.5f;
    private const float MinimumObjectiveUrgency = 0.01f;
    private const float MinimumSelfDirectedReward = 1f;
    private const float MinimumActionableScoreWeight = 0.1f;
    private const float SubGoalUrgencyScale = 0.5f;
    private const float DepthPenalty = 0.85f;
    private const float TargetSwitchHysteresisRatio = 0.2f;
    private const int MaxPlanningDepth = 2;
    private const int PreconditionFailed = 0;
    private const int PreconditionPassed = 1;

    private readonly GambitProgramScratch _scratch;
    private int[] _gambitKindIds => Data.GambitKindIds;
    private int[] _gambitIds => Data.GambitIds;
    private int[] _gambitCooldownTicks => Data.GambitCooldownTicks;
    private int[] _gambitEntityCooldownTicks => Data.GambitEntityCooldownTicks;
    private float[] _gambitSuccessChances => Data.GambitSuccessChances;
    private int[] _gambitTargetKindIds => Data.GambitTargetKindIds;
    private int[] _gambitTargetModes => Data.GambitTargetModes;
    private int[] _gambitPreconditionStarts => Data.GambitPreconditionStarts;
    private int[] _gambitPreconditionCounts => Data.GambitPreconditionCounts;
    private int[] _gambitExpressionStarts => Data.GambitExpressionStarts;
    private int[] _gambitExpressionCounts => Data.GambitExpressionCounts;
    private int[] _gambitRewardExpressionStarts => Data.GambitRewardExpressionStarts;
    private int[] _gambitRewardExpressionCounts => Data.GambitRewardExpressionCounts;
    private int[] _gambitStepStarts => Data.GambitStepStarts;
    private int[] _gambitStepCounts => Data.GambitStepCounts;
    private int[] _gambitProviderSelectorStarts => Data.GambitProviderSelectorStarts;
    private int[] _gambitProviderSelectorCounts => Data.GambitProviderSelectorCounts;
    private int[] _gambitSupportRuleStarts => Data.GambitSupportRuleStarts;
    private int[] _gambitSupportRuleCounts => Data.GambitSupportRuleCounts;
    private int[] _gambitGoalEffectStarts => Data.GambitGoalEffectStarts;
    private int[] _gambitGoalEffectCounts => Data.GambitGoalEffectCounts;
    private int[] _goalEffectScopeCodes => Data.GoalEffectScopeCodes;
    private int[] _goalEffectValueIds => Data.GoalEffectValueIds;
    private int[] _goalEffectKindIds => Data.GoalEffectKindIds;
    private bool[] _gambitEnabled => State.GambitEnabled;
    private int[] _preconditionCodes => Data.PreconditionCodes;
    private int[] _preA => Data.PreA;
    private int[] _preB => Data.PreB;
    private int[] _preC => Data.PreC;
    private int[] _preD => Data.PreD;
    private float[] _preX => Data.PreX;
    private float[] _preY => Data.PreY;
    private int[] _expressionCodes => Data.ExpressionCodes;
    private int[] _expressionA => Data.ExpressionA;
    private float[] _expressionX => Data.ExpressionX;
    private int[] _rewardExpressionCodes => Data.RewardExpressionCodes;
    private int[] _rewardExpressionA => Data.RewardExpressionA;
    private float[] _rewardExpressionX => Data.RewardExpressionX;
    private int[] _stepOperationCodes => Data.StepOperationCodes;
    private int[] _stepA => Data.StepA;
    private int[] _stepB => Data.StepB;
    private int[] _stepC => Data.StepC;
    private int[] _stepD => Data.StepD;
    private float[] _stepX => Data.StepX;
    private float[] _stepY => Data.StepY;
    private int[] _stepExpressionStarts => Data.StepExpressionStarts;
    private int[] _stepExpressionCounts => Data.StepExpressionCounts;
    private int[] _mutationExpressionCodes => Data.MutationExpressionCodes;
    private int[] _mutationExpressionA => Data.MutationExpressionA;
    private float[] _mutationExpressionX => Data.MutationExpressionX;
    private PrimitiveIntTable _cooldowns => State.Cooldowns;
    private PrimitiveIntTable _entityCooldowns => State.EntityCooldowns;
    private int[] _pathOpenSet => _scratch.PathOpenSet;
    private ref EpochStampSet _pathOpen => ref _scratch.PathOpen;
    private ref EpochStampSet _pathClosed => ref _scratch.PathClosed;
    private ref EpochStampedArray<int> _pathCameFrom => ref _scratch.PathCameFrom;
    private ref EpochStampedArray<float> _pathGScore => ref _scratch.PathGScore;
    private ref EpochStampedArray<float> _pathFScore => ref _scratch.PathFScore;
    private int[] _actionHistoryTicks => State.ActionHistoryTicks;
    private int[] _actionHistoryGambitIds => State.ActionHistoryGambitIds;
    private int[] _actionHistoryCounts => State.ActionHistoryCounts;
    private int[] _actionHistoryNextSlots => State.ActionHistoryNextSlots;
    private int[] _objectiveKindIds => Data.ObjectiveKindIds;
    private int[] _objectiveCodes => Data.ObjectiveCodes;
    private int[] _objectiveEffectScopeCodes => Data.ObjectiveEffectScopeCodes;
    private int[] _objectiveEffectValueIds => Data.ObjectiveEffectValueIds;
    private int[] _objectiveEffectKindIds => Data.ObjectiveEffectKindIds;
    private int[] _objectiveAttributeIds => Data.ObjectiveAttributeIds;
    private float[] _objectiveMinimums => Data.ObjectiveMinimums;
    private float[] _objectiveMaximums => Data.ObjectiveMaximums;
    private float[] _objectiveDefaults => Data.ObjectiveDefaults;
    private float[] _objectiveBaselines => Data.ObjectiveBaselines;
    private float[] _objectiveBaseUrgencies => Data.ObjectiveBaseUrgencies;
    private int[] _gambitPlanEffectKeys => _scratch.GambitPlanEffectKeys;
    private int[] _gambitPlanRootGambitSlots => _scratch.GambitPlanRootGambitSlots;
    private int[] _gambitPlanRootTargetIds => _scratch.GambitPlanRootTargetIds;
    private int[] _gambitPlanGambitSlots => _scratch.GambitPlanGambitSlots;
    private int[] _gambitPlanTargetIds => _scratch.GambitPlanTargetIds;
    private int[] _gambitPlanLocalityKeys => _scratch.GambitPlanLocalityKeys;
    private float[] _gambitPlanWeights => _scratch.GambitPlanWeights;
    private float[] _gambitPlanAdjustedWeights => _scratch.GambitPlanAdjustedWeights;
    private bool[] _preconditionStack => _scratch.PreconditionStack;
    private float[] _expressionStack => _scratch.ExpressionStack;
    private int[] _variableIds => _scratch.VariableIds;
    private float[] _variableValues => _scratch.VariableValues;
    private int[] _providerSelectorCodes => Data.ProviderSelectorCodes;
    private int[] _providerSelectorKindIds => Data.ProviderSelectorKindIds;
    private int[] _providerSelectorValueIds => Data.ProviderSelectorValueIds;
    private int[] _providerSelectorRelationshipKindIds => Data.ProviderSelectorRelationshipKindIds;
    private float[] _providerSelectorMinimumStrengths => Data.ProviderSelectorMinimumStrengths;
    private int[] _supportEffectScopeCodes => Data.SupportEffectScopeCodes;
    private int[] _supportEffectValueIds => Data.SupportEffectValueIds;
    private int[] _supportEffectKindIds => Data.SupportEffectKindIds;
    private int[] _supportExpressionStarts => Data.SupportExpressionStarts;
    private int[] _supportExpressionCounts => Data.SupportExpressionCounts;
    private float[] _supportDesiredMinimums => Data.SupportDesiredMinimums;
    private float[] _supportUrgencyScales => Data.SupportUrgencyScales;
    private int[] _supportExpressionCodes => Data.SupportExpressionCodes;
    private int[] _supportExpressionA => Data.SupportExpressionA;
    private float[] _supportExpressionX => Data.SupportExpressionX;
    private PrimitiveIntTable _committedPlanTargets => State.CommittedPlanTargets;
    private PrimitiveIntTable _committedPlanGambitSlots => State.CommittedPlanGambitSlots;
    private PrimitiveIntTable _committedPlanLocalities => State.CommittedPlanLocalities;
    private PrimitiveIntTable _actionTargetReservationTicks => State.ActionTargetReservationTicks;
    private PrimitiveIntTable _actionTargetReservationCounts => State.ActionTargetReservationCounts;
    private ref EpochStampSet _gambitVisited => ref _scratch.GambitVisited;
    private ref int _gambitCount => ref Data.GambitCount;
    private ref int _preconditionCount => ref Data.PreconditionCount;
    private ref int _expressionCount => ref Data.ExpressionCount;
    private ref int _rewardExpressionCount => ref Data.RewardExpressionCount;
    private ref int _stepCount => ref Data.StepCount;
    private ref int _mutationExpressionCount => ref Data.MutationExpressionCount;
    private ref int _objectiveCount => ref Data.ObjectiveCount;
    private ref int _providerSelectorCount => ref Data.ProviderSelectorCount;
    private ref int _supportRuleCount => ref Data.SupportRuleCount;
    private ref int _supportExpressionCount => ref Data.SupportExpressionCount;
    private ref int _goalEffectCount => ref Data.GoalEffectCount;
    private ref int _traitModifierCount => ref Data.TraitModifierCount;
    private ref int _openGambitSlot => ref Data.OpenGambitSlot;

    private const int GambitLocalityCommitmentKey = 1;
    private const float LocalityGoalSelectionBoost = 3f;
    private const float LocalityAdditionalPlanWeight = 0.65f;
    private const float LocalityDensityBonus = 0.15f;
    private const float LocalityDistinctEffectBonus = 0.08f;
    private const float LocalityDistancePenaltyPerStep = 0.05f;
    private const float LocalityCommitmentBonus = 0.2f;
    private const float LocalityAlreadyThereBonus = 0.15f;

    internal GambitProgramRuntime(
        GambitProgramData data,
        GambitProgramState state,
        GambitProgramScratch scratch)
        : this(data, state, scratch, new KernelProgramTuningOptions())
    {
    }

    internal GambitProgramRuntime(
        GambitProgramData data,
        GambitProgramState state,
        GambitProgramScratch scratch,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        _scratch = scratch;
        Tuning = tuning;
    }

    public GambitProgramData Data { get; }

    public GambitProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int Count => _gambitCount;

    public int ObjectiveCount => _objectiveCount;

    public int RegisterGambit(
        int kindId,
        int gambitId,
        int cooldownTicks = 0,
        int entityCooldownTicks = 0,
        int targetMode = BehaviorTargetModeCodes.Self,
        int targetKindId = 0,
        float successChance = 1f)
    {
        ValidatePositive(kindId);
        ValidatePositive(gambitId);
        ValidatePositive(targetMode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cooldownTicks);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(entityCooldownTicks);
        if (successChance < 0f || successChance > 1f)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(successChance));
        }

        if (_gambitCount + 1 >= _gambitKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _gambitCount++;
        var slot = _gambitCount;
        _gambitKindIds[slot] = kindId;
        _gambitIds[slot] = gambitId;
        _gambitCooldownTicks[slot] = cooldownTicks;
        _gambitEntityCooldownTicks[slot] = entityCooldownTicks;
        _gambitSuccessChances[slot] = successChance;
        _gambitTargetModes[slot] = targetMode;
        _gambitTargetKindIds[slot] = targetKindId;
        _gambitPreconditionStarts[slot] = _preconditionCount + 1;
        _gambitExpressionStarts[slot] = _expressionCount + 1;
        _gambitRewardExpressionStarts[slot] = _rewardExpressionCount + 1;
        _gambitStepStarts[slot] = _stepCount + 1;
        _gambitProviderSelectorStarts[slot] = _providerSelectorCount + 1;
        _gambitSupportRuleStarts[slot] = _supportRuleCount + 1;
        _gambitGoalEffectStarts[slot] = _goalEffectCount + 1;
        _gambitEnabled[slot] = true;
        _openGambitSlot = slot;
        return slot;
    }

    public int RegisterTraitModifier(
        int gambitId,
        int traitId,
        int bearerCode,
        float scoreDelta,
        float successChanceDelta)
    {
        ValidatePositive(gambitId);
        ValidatePositive(traitId);
        if (bearerCode is not TraitEffectBearerCodes.Actor and not TraitEffectBearerCodes.GambitTarget)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(bearerCode));
        }

        if (_traitModifierCount + 1 >= Data.TraitModifierGambitIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _traitModifierCount++;
        Data.TraitModifierGambitIds[_traitModifierCount] = gambitId;
        Data.TraitModifierTraitIds[_traitModifierCount] = traitId;
        Data.TraitModifierBearerCodes[_traitModifierCount] = bearerCode;
        Data.TraitModifierScoreDeltas[_traitModifierCount] = scoreDelta;
        Data.TraitModifierSuccessChanceDeltas[_traitModifierCount] = successChanceDelta;
        return _traitModifierCount;
    }

    public void AddPrecondition(
        int gambitSlot,
        int preconditionCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f)
    {
        EnsureOpenGambit(gambitSlot);
        if (_preconditionCount + 1 >= _preconditionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _preconditionCount++;
        _preconditionCodes[_preconditionCount] = preconditionCode;
        _preA[_preconditionCount] = a;
        _preB[_preconditionCount] = b;
        _preC[_preconditionCount] = c;
        _preD[_preconditionCount] = d;
        _preX[_preconditionCount] = x;
        _preY[_preconditionCount] = y;
        _gambitPreconditionCounts[gambitSlot]++;
    }

    public void AddExpressionOp(int gambitSlot, int expressionCode, int a = 0, float x = 0f)
    {
        EnsureOpenGambit(gambitSlot);
        if (_expressionCount + 1 >= _expressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _expressionCount++;
        _expressionCodes[_expressionCount] = expressionCode;
        _expressionA[_expressionCount] = a;
        _expressionX[_expressionCount] = x;
        _gambitExpressionCounts[gambitSlot]++;
    }

    public void AddRewardExpressionOp(int gambitSlot, int expressionCode, int a = 0, float x = 0f)
    {
        EnsureOpenGambit(gambitSlot);
        if (_rewardExpressionCount + 1 >= _rewardExpressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _rewardExpressionCount++;
        _rewardExpressionCodes[_rewardExpressionCount] = expressionCode;
        _rewardExpressionA[_rewardExpressionCount] = a;
        _rewardExpressionX[_rewardExpressionCount] = x;
        _gambitRewardExpressionCounts[gambitSlot]++;
    }

    public void SetGoalEffect(int gambitSlot, int scopeCode, int valueId, int kindId)
    {
        AddGoalEffect(gambitSlot, scopeCode, valueId, kindId);
    }

    public void AddGoalEffect(int gambitSlot, int scopeCode, int valueId, int kindId)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(scopeCode);
        ValidatePositive(valueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(kindId);
        if (_goalEffectCount + 1 >= _goalEffectScopeCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _goalEffectCount++;
        _goalEffectScopeCodes[_goalEffectCount] = scopeCode;
        _goalEffectValueIds[_goalEffectCount] = valueId;
        _goalEffectKindIds[_goalEffectCount] = kindId;
        _gambitGoalEffectCounts[gambitSlot]++;
    }

    public void AddProviderSelector(
        int gambitSlot,
        int selectorCode,
        int kindId = 0,
        int valueId = 0,
        int relationshipKindId = 0,
        float minimumStrength = 0f)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(selectorCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(valueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        if (_providerSelectorCount + 1 >= _providerSelectorCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _providerSelectorCount++;
        _providerSelectorCodes[_providerSelectorCount] = selectorCode;
        _providerSelectorKindIds[_providerSelectorCount] = kindId;
        _providerSelectorValueIds[_providerSelectorCount] = valueId;
        _providerSelectorRelationshipKindIds[_providerSelectorCount] = relationshipKindId;
        _providerSelectorMinimumStrengths[_providerSelectorCount] = minimumStrength;
        _gambitProviderSelectorCounts[gambitSlot]++;
    }

    public int AddSupportRule(
        int gambitSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float desiredMinimum,
        float urgencyScale = 1f)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(effectScopeCode);
        ValidatePositive(effectValueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(effectKindId);
        if (desiredMinimum <= 0f || urgencyScale < 0f)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_supportRuleCount + 1 >= _supportEffectScopeCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _supportRuleCount++;
        _supportEffectScopeCodes[_supportRuleCount] = effectScopeCode;
        _supportEffectValueIds[_supportRuleCount] = effectValueId;
        _supportEffectKindIds[_supportRuleCount] = effectKindId;
        _supportDesiredMinimums[_supportRuleCount] = desiredMinimum;
        _supportUrgencyScales[_supportRuleCount] = urgencyScale;
        _supportExpressionStarts[_supportRuleCount] = _supportExpressionCount + 1;
        _gambitSupportRuleCounts[gambitSlot]++;
        return _supportRuleCount;
    }

    public void AddSupportExpressionOp(int supportRuleSlot, int expressionCode, int a = 0, float x = 0f)
    {
        EnsureSupportRule(supportRuleSlot);
        if (_supportExpressionCount + 1 >= _supportExpressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _supportExpressionCount++;
        _supportExpressionCodes[_supportExpressionCount] = expressionCode;
        _supportExpressionA[_supportExpressionCount] = a;
        _supportExpressionX[_supportExpressionCount] = x;
        _supportExpressionCounts[supportRuleSlot]++;
    }

    public int RegisterAttributeObjective(
        int kindId,
        int attributeId,
        int urgencyCode,
        float minimum,
        float maximum,
        float defaultValue,
        float baseline)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        ValidatePositive(urgencyCode);
        var slot = RegisterObjective(kindId, urgencyCode, BehaviorEffectScopeCodes.SelfAttribute, attributeId, kindId);
        _objectiveAttributeIds[slot] = attributeId;
        _objectiveMinimums[slot] = minimum;
        _objectiveMaximums[slot] = maximum;
        _objectiveDefaults[slot] = defaultValue;
        _objectiveBaselines[slot] = baseline;
        return slot;
    }

    public int RegisterFixedObjective(
        int kindId,
        int scopeCode,
        int valueId,
        int effectKindId,
        float baseUrgency)
    {
        ValidatePositive(kindId);
        ValidatePositive(scopeCode);
        ValidatePositive(valueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(effectKindId);
        if (baseUrgency < 0f)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = RegisterObjective(kindId, BehaviorObjectiveUrgencyCodes.Fixed, scopeCode, valueId, effectKindId);
        _objectiveBaseUrgencies[slot] = baseUrgency;
        return slot;
    }

    public int AddStep(
        int gambitSlot,
        int operationCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f)
    {
        EnsureOpenGambit(gambitSlot);
        if (_stepCount + 1 >= _stepOperationCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _stepCount++;
        _stepOperationCodes[_stepCount] = operationCode;
        _stepA[_stepCount] = a;
        _stepB[_stepCount] = b;
        _stepC[_stepCount] = c;
        _stepD[_stepCount] = d;
        _stepX[_stepCount] = x;
        _stepY[_stepCount] = y;
        _stepExpressionStarts[_stepCount] = _mutationExpressionCount + 1;
        _gambitStepCounts[gambitSlot]++;
        return _stepCount;
    }

    public void AddStepExpressionOp(int stepSlot, int expressionCode, int a = 0, float x = 0f)
    {
        if ((uint)stepSlot == 0u || stepSlot > _stepCount)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_mutationExpressionCount + 1 >= _mutationExpressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _mutationExpressionCount++;
        _mutationExpressionCodes[_mutationExpressionCount] = expressionCode;
        _mutationExpressionA[_mutationExpressionCount] = a;
        _mutationExpressionX[_mutationExpressionCount] = x;
        _stepExpressionCounts[stepSlot]++;
    }

    private int RegisterObjective(int kindId, int urgencyCode, int scopeCode, int valueId, int effectKindId)
    {
        if (_objectiveCount + 1 >= _objectiveKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _objectiveCount++;
        _objectiveKindIds[_objectiveCount] = kindId;
        _objectiveCodes[_objectiveCount] = urgencyCode;
        _objectiveEffectScopeCodes[_objectiveCount] = scopeCode;
        _objectiveEffectValueIds[_objectiveCount] = valueId;
        _objectiveEffectKindIds[_objectiveCount] = effectKindId;
        return _objectiveCount;
    }

    public void DisableGambit(int gambitSlot)
    {
        EnsureGambit(gambitSlot);
        _gambitEnabled[gambitSlot] = false;
    }

    public void EnableGambit(int gambitSlot)
    {
        EnsureGambit(gambitSlot);
        _gambitEnabled[gambitSlot] = true;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        KernelQuery.ForEachActiveEntity(
            world,
            entityId =>
            {
                TickEntity(simulation, entityId, tick);
                return true;
            });
    }

    private void TickEntity(Simulation simulation, int entityId, int tick)
    {
        if (tick < _entityCooldowns.GetOrDefault(entityId, 1))
        {
            return;
        }

        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        var plannerKindCode = simulation.Planners.PlannerKindFor(kindId, entityId);
        if (plannerKindCode != PlannerKindCodes.Goap || HasGambitObjectives(kindId))
        {
            TickEntityPlanned(simulation, entityId, kindId, plannerKindCode, tick);
            return;
        }

        var candidateCount = 0;
        var totalWeight = EvaluateGambitWeights(simulation, entityId, kindId, tick, ref candidateCount);
        if (candidateCount == 0)
        {
            return;
        }

        if (totalWeight <= 0f)
        {
            WriteGambitEvaluated(simulation, tick, entityId, kindId, 0, candidateCount, KernelEventState.None);
            return;
        }

        var roll = simulation.NextFloat() * totalWeight;
        var selectedGambitSlot = SelectGambitSlot(simulation, entityId, kindId, tick, roll, out var selectedTargetId);
        if (selectedGambitSlot == 0)
        {
            return;
        }

        RunSelectedGambit(simulation, entityId, selectedTargetId, selectedGambitSlot, kindId, tick, candidateCount);
    }

    private float EvaluateGambitWeights(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        ref int candidateCount)
    {
        var totalWeight = 0f;
        for (var gambitSlot = 1; gambitSlot <= _gambitCount; gambitSlot++)
        {
            if (GambitCanRunForEntity(gambitSlot, kindId, entityId, tick))
            {
                totalWeight += EvaluateTargets(
                    simulation,
                    gambitSlot,
                    entityId,
                    tick,
                    ref candidateCount,
                    selectRoll: -1f,
                    out _);
            }
        }

        return totalWeight;
    }

    private int SelectGambitSlot(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        float roll,
        out int selectedTargetId)
    {
        var cumulative = 0f;
        var ignoredCandidateCount = 0;
        selectedTargetId = 0;
        for (var gambitSlot = 1; gambitSlot <= _gambitCount; gambitSlot++)
        {
            if (!GambitCanRunForEntity(gambitSlot, kindId, entityId, tick))
            {
                continue;
            }

            cumulative += EvaluateTargets(
                simulation,
                gambitSlot,
                entityId,
                tick,
                ref ignoredCandidateCount,
                roll - cumulative,
                out selectedTargetId);
            if (selectedTargetId > 0)
            {
                return gambitSlot;
            }
        }

        return 0;
    }

    private void RunSelectedGambit(
        Simulation simulation,
        int entityId,
        int selectedTargetId,
        int selectedGambitSlot,
        int kindId,
        int tick,
        int candidateCount)
    {
        var gambitId = _gambitIds[selectedGambitSlot];
        var state = RollGambitState(simulation, selectedGambitSlot, entityId, selectedTargetId);
        using var provenanceScope = EnterGambitProvenance(
            simulation,
            selectedGambitSlot,
            gambitId,
            entityId,
            selectedTargetId);
        if (state == KernelEventState.Succeeded)
        {
            ExecuteGambit(simulation, entityId, selectedTargetId, selectedGambitSlot, tick);
        }

        RecordGambit(entityId, gambitId, tick);
        ApplyGambitCooldowns(simulation, entityId, selectedGambitSlot, gambitId, tick);
        WriteGambitEvaluated(simulation, tick, entityId, kindId, gambitId, candidateCount, KernelEventState.Succeeded);
        WriteGambitExecuted(simulation, tick, entityId, selectedTargetId, kindId, gambitId, state);
    }

    private KernelProvenanceScope EnterGambitProvenance(
        Simulation simulation,
        int gambitSlot,
        int gambitId,
        int entityId,
        int selectedTargetId)
    {
        ResolveGambitTriggerEvidence(
            simulation.World,
            gambitSlot,
            entityId,
            selectedTargetId,
            out var triggerKindCode,
            out var triggerId,
            out var parentInvocationId);
        return simulation.EnterProvenanceInvocation(
            ProvenanceProducerKindCodes.Gambit,
            gambitId,
            entityId,
            selectedTargetId,
            triggerKindCode,
            triggerId,
            parentInvocationId);
    }

    private void ApplyGambitCooldowns(
        Simulation simulation,
        int entityId,
        int selectedGambitSlot,
        int gambitId,
        int tick)
    {
        var cooldown = _gambitCooldownTicks[selectedGambitSlot];
        if (cooldown > 0)
        {
            _cooldowns.Set(entityId, gambitId, tick + CooldownWithJitter(simulation, cooldown));
        }

        var entityCooldown = _gambitEntityCooldownTicks[selectedGambitSlot];
        if (entityCooldown > 0)
        {
            _entityCooldowns.Set(entityId, 1, tick + CooldownWithJitter(simulation, entityCooldown));
        }
    }

    private bool GambitCanRunForEntity(int gambitSlot, int kindId, int entityId, int tick)
    {
        return _gambitEnabled[gambitSlot] &&
               _gambitKindIds[gambitSlot] == kindId &&
               tick >= _cooldowns.GetOrDefault(entityId, _gambitIds[gambitSlot]);
    }

    private KernelEventState RollGambitState(Simulation simulation, int gambitSlot, int entityId, int targetId)
    {
        var successChance = Clamp01(
            _gambitSuccessChances[gambitSlot] +
            GambitSuccessDecisionDelta(simulation.World, _gambitIds[gambitSlot], entityId, targetId));
        return successChance >= 1f || simulation.NextFloat() <= successChance
            ? KernelEventState.Succeeded
            : KernelEventState.Failed;
    }

    private float GambitScoreDecisionDelta(WorldState world, int gambitId, int entityId, int targetId) =>
        DecisionModifierEvaluation.SumGambitValue(
            world,
            _traitModifierCount,
            Data.ReadDecisionModifier,
            gambitId,
            entityId,
            targetId,
            useValue1: false);

    private float GambitSuccessDecisionDelta(WorldState world, int gambitId, int entityId, int targetId) =>
        DecisionModifierEvaluation.SumGambitValue(
            world,
            _traitModifierCount,
            Data.ReadDecisionModifier,
            gambitId,
            entityId,
            targetId,
            useValue1: true);

    private bool HasGambitObjectives(int kindId)
    {
        for (var objective = 1; objective <= _objectiveCount; objective++)
        {
            if (_objectiveKindIds[objective] == kindId)
            {
                return true;
            }
        }

        return false;
    }

    private void TickEntityPlanned(Simulation simulation, int entityId, int kindId, int plannerKindCode, int tick)
    {
        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                plannerKindCode,
                ActionFamilyCodes.Gambit,
                entityId,
                kindId,
                tick,
                simulation.NextFloat()));
        if (decision.StatusCode == PlanningStatusCodes.NoCandidates)
        {
            return;
        }

        if (decision.StatusCode == PlanningStatusCodes.NoWeight)
        {
            WriteGambitEvaluated(simulation, tick, entityId, kindId, 0, decision.CandidateCount, KernelEventState.None);
            return;
        }

        if (decision.StatusCode != PlanningStatusCodes.Succeeded ||
            decision.ImmediateAction.ActionSlot == 0 ||
            decision.ImmediateTargetEntityId == 0)
        {
            return;
        }

        var selectedGambitSlot = decision.ImmediateAction.ActionSlot;
        var selectedTargetId = decision.ImmediateTargetEntityId;
        RunSelectedGambit(
            simulation,
            entityId,
            selectedTargetId,
            selectedGambitSlot,
            kindId,
            tick,
            decision.CandidateCount);
    }

    internal PlanningDecision PlanGoap(Simulation simulation, in PlanningRequest request)
    {
        var candidateCount = 0;
        var totalWeight = EvaluateGambitPlanTotal(simulation, request, ref candidateCount);
        if (candidateCount == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoCandidates,
                request.ActionFamilyCode,
                candidateCount,
                0f);
        }

        if (totalWeight <= 0f)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        SelectGambitPlan(
            simulation,
            request,
            totalWeight,
            out var selectedGambitSlot,
            out var selectedTargetId,
            out var selectedRootGambitSlot,
            out var selectedRootTargetId,
            out var selectedEffectKey,
            out var selectedLocalityKey);
        if (selectedGambitSlot == 0 || selectedTargetId == 0)
        {
            return simulation.Planners.EmptyDecision(
                PlanningStatusCodes.NoWeight,
                request.ActionFamilyCode,
                candidateCount,
                totalWeight);
        }

        return simulation.Planners.WriteSelectedPlan(
            request,
            selectedEffectKey,
            selectedRootGambitSlot,
            selectedRootTargetId,
            selectedGambitSlot,
            selectedTargetId,
            selectedLocalityKey,
            candidateCount,
            totalWeight);
    }

    private float EvaluateGambitPlanTotal(Simulation simulation, in PlanningRequest request, ref int candidateCount)
    {
        return EvaluatePlannedCandidates(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            selectRoll: -1f,
            ref candidateCount,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _);
    }

    private void SelectGambitPlan(
        Simulation simulation,
        in PlanningRequest request,
        float totalWeight,
        out int selectedGambitSlot,
        out int selectedTargetId,
        out int selectedRootGambitSlot,
        out int selectedRootTargetId,
        out int selectedEffectKey,
        out int selectedLocalityKey)
    {
        var selectedCandidateCount = 0;
        _ = EvaluatePlannedCandidates(
            simulation,
            request.ActorEntityId,
            request.ActorKindId,
            request.Tick,
            ClampSelectionRoll(request.SelectionRoll) * totalWeight,
            ref selectedCandidateCount,
            out selectedGambitSlot,
            out selectedTargetId,
            out selectedRootGambitSlot,
            out selectedRootTargetId,
            out selectedEffectKey,
            out selectedLocalityKey);
    }

    internal int GambitIdForSlot(int gambitSlot)
    {
        EnsureGambit(gambitSlot);
        return _gambitIds[gambitSlot];
    }

}
