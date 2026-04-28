namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class ProcessProgramRuntime : IKernelProgramRuntime<ProcessProgramData, ProcessProgramState>
{
    internal const int ActionHistoryLength = 60;
    private const int RecencyWindow = 10;
    private const float RecencyDecayFactor = 0.5f;
    private const float MinimumObjectiveUrgency = 0.01f;
    private const float MinimumSelfDirectedReward = 1f;
    private const float MinimumActionableScoreWeight = 0.1f;
    private const float SubGoalUrgencyScale = 0.5f;
    private const float DepthPenalty = 0.85f;
    private const float TargetSwitchHysteresisRatio = 0.2f;
    private const int MaxPlanningDepth = 2;
    private const float StandardNeedComfortable = 70f;
    private const float StandardNeedConcern = 40f;
    private const float StandardNeedCritical = 15f;
    private const int PreconditionFailed = 0;
    private const int PreconditionPassed = 1;

    private readonly ProcessProgramScratch _scratch;
    private int[] _processKindIds => Data.ProcessKindIds;
    private int[] _processIds => Data.ProcessIds;
    private int[] _processCooldownTicks => Data.ProcessCooldownTicks;
    private int[] _processEntityCooldownTicks => Data.ProcessEntityCooldownTicks;
    private int[] _processTargetKindIds => Data.ProcessTargetKindIds;
    private int[] _processTargetModes => Data.ProcessTargetModes;
    private int[] _processPreconditionStarts => Data.ProcessPreconditionStarts;
    private int[] _processPreconditionCounts => Data.ProcessPreconditionCounts;
    private int[] _processExpressionStarts => Data.ProcessExpressionStarts;
    private int[] _processExpressionCounts => Data.ProcessExpressionCounts;
    private int[] _processRewardExpressionStarts => Data.ProcessRewardExpressionStarts;
    private int[] _processRewardExpressionCounts => Data.ProcessRewardExpressionCounts;
    private int[] _processStepStarts => Data.ProcessStepStarts;
    private int[] _processStepCounts => Data.ProcessStepCounts;
    private int[] _processProviderSelectorStarts => Data.ProcessProviderSelectorStarts;
    private int[] _processProviderSelectorCounts => Data.ProcessProviderSelectorCounts;
    private int[] _processSupportRuleStarts => Data.ProcessSupportRuleStarts;
    private int[] _processSupportRuleCounts => Data.ProcessSupportRuleCounts;
    private int[] _processGoalEffectStarts => Data.ProcessGoalEffectStarts;
    private int[] _processGoalEffectCounts => Data.ProcessGoalEffectCounts;
    private int[] _goalEffectScopeCodes => Data.GoalEffectScopeCodes;
    private int[] _goalEffectValueIds => Data.GoalEffectValueIds;
    private int[] _goalEffectKindIds => Data.GoalEffectKindIds;
    private bool[] _processEnabled => State.ProcessEnabled;
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
    private int[] _actionHistoryProcessIds => State.ActionHistoryProcessIds;
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
    private int[] _processPlanEffectKeys => _scratch.ProcessPlanEffectKeys;
    private int[] _processPlanEffectScopeCodes => _scratch.ProcessPlanEffectScopeCodes;
    private int[] _processPlanEffectValueIds => _scratch.ProcessPlanEffectValueIds;
    private int[] _processPlanEffectKindIds => _scratch.ProcessPlanEffectKindIds;
    private int[] _processPlanGoalCandidateIndexes => _scratch.ProcessPlanGoalCandidateIndexes;
    private int[] _processPlanRootProcessSlots => _scratch.ProcessPlanRootProcessSlots;
    private int[] _processPlanRootTargetIds => _scratch.ProcessPlanRootTargetIds;
    private int[] _processPlanProcessSlots => _scratch.ProcessPlanProcessSlots;
    private int[] _processPlanTargetIds => _scratch.ProcessPlanTargetIds;
    private int[] _processPlanLocalityKeys => _scratch.ProcessPlanLocalityKeys;
    private float[] _processPlanWeights => _scratch.ProcessPlanWeights;
    private float[] _processPlanAdjustedWeights => _scratch.ProcessPlanAdjustedWeights;
    private bool[] _preconditionStack => _scratch.PreconditionStack;
    private float[] _expressionStack => _scratch.ExpressionStack;
    private int[] _variableIds => _scratch.VariableIds;
    private float[] _variableValues => _scratch.VariableValues;
    private int[] _providerSelectorCodes => Data.ProviderSelectorCodes;
    private int[] _providerSelectorKindIds => Data.ProviderSelectorKindIds;
    private int[] _providerSelectorValueIds => Data.ProviderSelectorValueIds;
    private int[] _providerSelectorRelationshipKindIds => Data.ProviderSelectorRelationshipKindIds;
    private int[] _providerSelectorFactIds => Data.ProviderSelectorFactIds;
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
    private PrimitiveIntTable _committedPlanProcessSlots => State.CommittedPlanProcessSlots;
    private PrimitiveIntTable _committedPlanLocalities => State.CommittedPlanLocalities;
    private ref EpochStampSet _processVisited => ref _scratch.ProcessVisited;
    private ref int _processCount => ref Data.ProcessCount;
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
    private ref int _openProcessSlot => ref Data.OpenProcessSlot;

    private const int ProcessLocalityCommitmentKey = 1;
    private const float LocalityGoalSelectionBoost = 3f;
    private const float LocalityAdditionalPlanWeight = 0.65f;
    private const float LocalityDensityBonus = 0.15f;
    private const float LocalityDistinctEffectBonus = 0.08f;
    private const float LocalityDistancePenaltyPerStep = 0.05f;
    private const float LocalityCommitmentBonus = 0.2f;
    private const float LocalityAlreadyThereBonus = 0.15f;

    internal ProcessProgramRuntime(
        ProcessProgramData data,
        ProcessProgramState state,
        ProcessProgramScratch scratch)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        Data = data;
        State = state;
        _scratch = scratch;
    }

    public ProcessProgramData Data { get; }

    public ProcessProgramState State { get; }

    public int Count => _processCount;

    public int ObjectiveCount => _objectiveCount;

    public int RegisterProcess(
        int kindId,
        int processId,
        int cooldownTicks = 0,
        int entityCooldownTicks = 0,
        int targetMode = ProcessTargetModeCodes.Self,
        int targetKindId = 0)
    {
        ValidatePositive(kindId);
        ValidatePositive(processId);
        ValidatePositive(targetMode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cooldownTicks);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(entityCooldownTicks);
        if (_processCount + 1 >= _processKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _processCount++;
        var slot = _processCount;
        _processKindIds[slot] = kindId;
        _processIds[slot] = processId;
        _processCooldownTicks[slot] = cooldownTicks;
        _processEntityCooldownTicks[slot] = entityCooldownTicks;
        _processTargetModes[slot] = targetMode;
        _processTargetKindIds[slot] = targetKindId;
        _processPreconditionStarts[slot] = _preconditionCount + 1;
        _processExpressionStarts[slot] = _expressionCount + 1;
        _processRewardExpressionStarts[slot] = _rewardExpressionCount + 1;
        _processStepStarts[slot] = _stepCount + 1;
        _processProviderSelectorStarts[slot] = _providerSelectorCount + 1;
        _processSupportRuleStarts[slot] = _supportRuleCount + 1;
        _processGoalEffectStarts[slot] = _goalEffectCount + 1;
        _processEnabled[slot] = true;
        _openProcessSlot = slot;
        return slot;
    }

    public void AddPrecondition(
        int processSlot,
        int preconditionCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f)
    {
        EnsureOpenProcess(processSlot);
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
        _processPreconditionCounts[processSlot]++;
    }

    public void AddExpressionOp(int processSlot, int expressionCode, int a = 0, float x = 0f)
    {
        EnsureOpenProcess(processSlot);
        if (_expressionCount + 1 >= _expressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _expressionCount++;
        _expressionCodes[_expressionCount] = expressionCode;
        _expressionA[_expressionCount] = a;
        _expressionX[_expressionCount] = x;
        _processExpressionCounts[processSlot]++;
    }

    public void AddRewardExpressionOp(int processSlot, int expressionCode, int a = 0, float x = 0f)
    {
        EnsureOpenProcess(processSlot);
        if (_rewardExpressionCount + 1 >= _rewardExpressionCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _rewardExpressionCount++;
        _rewardExpressionCodes[_rewardExpressionCount] = expressionCode;
        _rewardExpressionA[_rewardExpressionCount] = a;
        _rewardExpressionX[_rewardExpressionCount] = x;
        _processRewardExpressionCounts[processSlot]++;
    }

    public void SetGoalEffect(int processSlot, int scopeCode, int valueId, int kindId)
    {
        AddGoalEffect(processSlot, scopeCode, valueId, kindId);
    }

    public void AddGoalEffect(int processSlot, int scopeCode, int valueId, int kindId)
    {
        EnsureOpenProcess(processSlot);
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
        _processGoalEffectCounts[processSlot]++;
    }

    public void AddProviderSelector(
        int processSlot,
        int selectorCode,
        int kindId = 0,
        int valueId = 0,
        int relationshipKindId = 0,
        int factId = 0,
        float minimumStrength = 0f)
    {
        EnsureOpenProcess(processSlot);
        ValidatePositive(selectorCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(valueId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(factId);
        if (_providerSelectorCount + 1 >= _providerSelectorCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _providerSelectorCount++;
        _providerSelectorCodes[_providerSelectorCount] = selectorCode;
        _providerSelectorKindIds[_providerSelectorCount] = kindId;
        _providerSelectorValueIds[_providerSelectorCount] = valueId;
        _providerSelectorRelationshipKindIds[_providerSelectorCount] = relationshipKindId;
        _providerSelectorFactIds[_providerSelectorCount] = factId;
        _providerSelectorMinimumStrengths[_providerSelectorCount] = minimumStrength;
        _processProviderSelectorCounts[processSlot]++;
    }

    public int AddSupportRule(
        int processSlot,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float desiredMinimum,
        float urgencyScale = 1f)
    {
        EnsureOpenProcess(processSlot);
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
        _processSupportRuleCounts[processSlot]++;
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
        var slot = RegisterObjective(kindId, urgencyCode, ProcessEffectScopeCodes.SelfAttribute, attributeId, kindId);
        _objectiveAttributeIds[slot] = attributeId;
        _objectiveMinimums[slot] = minimum;
        _objectiveMaximums[slot] = maximum;
        _objectiveDefaults[slot] = defaultValue;
        _objectiveBaselines[slot] = baseline;
        return slot;
    }

    public int RegisterStandingObjective(
        int kindId,
        int scopeCode,
        int valueId,
        int effectKindId,
        float baseUrgency) =>
        RegisterFixedObjective(kindId, scopeCode, valueId, effectKindId, baseUrgency);

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

        var slot = RegisterObjective(kindId, ProcessObjectiveUrgencyCodes.Fixed, scopeCode, valueId, effectKindId);
        _objectiveBaseUrgencies[slot] = baseUrgency;
        return slot;
    }

    public int AddStep(
        int processSlot,
        int operationCode,
        int a = 0,
        int b = 0,
        int c = 0,
        int d = 0,
        float x = 0f,
        float y = 0f)
    {
        EnsureOpenProcess(processSlot);
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
        _processStepCounts[processSlot]++;
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

    public void DisableProcess(int processSlot)
    {
        EnsureProcess(processSlot);
        _processEnabled[processSlot] = false;
    }

    public void EnableProcess(int processSlot)
    {
        EnsureProcess(processSlot);
        _processEnabled[processSlot] = true;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        var entityHighWatermark = world.EntityHighWatermark;
        for (var entityId = 1; entityId <= entityHighWatermark; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            TickEntity(simulation, entityId, tick);
        }
    }

    private void TickEntity(Simulation simulation, int entityId, int tick)
    {
        if (tick < _entityCooldowns.GetOrDefault(entityId, 1))
        {
            return;
        }

        var world = simulation.World;
        var kindId = world.EntityKindId(entityId);
        if (HasProcessObjectives(kindId))
        {
            TickEntityPlanned(simulation, entityId, kindId, tick);
            return;
        }

        var candidateCount = 0;
        var totalWeight = EvaluateProcessWeights(simulation, entityId, kindId, tick, ref candidateCount);
        if (candidateCount == 0)
        {
            return;
        }

        if (totalWeight <= 0f)
        {
            WriteProcessEvaluated(simulation, tick, entityId, kindId, 0, candidateCount, KernelEventState.None);
            return;
        }

        var roll = simulation.NextFloat() * totalWeight;
        var selectedProcessSlot = SelectProcessSlot(simulation, entityId, kindId, tick, roll, out var selectedTargetId);
        if (selectedProcessSlot == 0)
        {
            return;
        }

        RunSelectedProcess(simulation, entityId, selectedTargetId, selectedProcessSlot, kindId, tick, candidateCount);
    }

    private float EvaluateProcessWeights(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        ref int candidateCount)
    {
        var totalWeight = 0f;
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (ProcessCanRunForEntity(processSlot, kindId, entityId, tick))
            {
                totalWeight += EvaluateTargets(
                    simulation,
                    processSlot,
                    entityId,
                    tick,
                    ref candidateCount,
                    selectRoll: -1f,
                    out _);
            }
        }

        return totalWeight;
    }

    private int SelectProcessSlot(
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
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (!ProcessCanRunForEntity(processSlot, kindId, entityId, tick))
            {
                continue;
            }

            cumulative += EvaluateTargets(
                simulation,
                processSlot,
                entityId,
                tick,
                ref ignoredCandidateCount,
                roll - cumulative,
                out selectedTargetId);
            if (selectedTargetId > 0)
            {
                return processSlot;
            }
        }

        return 0;
    }

    private void RunSelectedProcess(
        Simulation simulation,
        int entityId,
        int selectedTargetId,
        int selectedProcessSlot,
        int kindId,
        int tick,
        int candidateCount)
    {
        var processId = _processIds[selectedProcessSlot];
        ExecuteProcess(simulation, entityId, selectedTargetId, selectedProcessSlot, tick);
        RecordAction(entityId, processId, tick);
        ApplyProcessCooldowns(simulation, entityId, selectedProcessSlot, processId, tick);
        WriteProcessEvaluated(simulation, tick, entityId, kindId, processId, candidateCount, KernelEventState.Succeeded);
        WriteProcessExecuted(simulation, tick, entityId, selectedTargetId, kindId, processId);
    }

    private void ApplyProcessCooldowns(
        Simulation simulation,
        int entityId,
        int selectedProcessSlot,
        int processId,
        int tick)
    {
        var cooldown = _processCooldownTicks[selectedProcessSlot];
        if (cooldown > 0)
        {
            _cooldowns.Set(entityId, processId, tick + CooldownWithJitter(simulation, cooldown));
        }

        var entityCooldown = _processEntityCooldownTicks[selectedProcessSlot];
        if (entityCooldown > 0)
        {
            _entityCooldowns.Set(entityId, 1, tick + CooldownWithJitter(simulation, entityCooldown));
        }
    }

    private bool ProcessCanRunForEntity(int processSlot, int kindId, int entityId, int tick)
    {
        return _processEnabled[processSlot] &&
               _processKindIds[processSlot] == kindId &&
               tick >= _cooldowns.GetOrDefault(entityId, _processIds[processSlot]);
    }

    private bool HasProcessObjectives(int kindId)
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

    private void TickEntityPlanned(Simulation simulation, int entityId, int kindId, int tick)
    {
        var decision = simulation.Planners.Plan(
            simulation,
            new PlanningRequest(
                simulation.Planners.PlannerKindCode,
                ActionFamilyCodes.Process,
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
            WriteProcessEvaluated(simulation, tick, entityId, kindId, 0, decision.CandidateCount, KernelEventState.None);
            return;
        }

        if (decision.StatusCode != PlanningStatusCodes.Succeeded ||
            decision.ImmediateAction.ActionSlot == 0 ||
            decision.ImmediateTargetEntityId == 0)
        {
            return;
        }

        var selectedProcessSlot = decision.ImmediateAction.ActionSlot;
        var selectedTargetId = decision.ImmediateTargetEntityId;
        RunSelectedProcess(
            simulation,
            entityId,
            selectedTargetId,
            selectedProcessSlot,
            kindId,
            tick,
            decision.CandidateCount);
    }

    private void ApplyLocalityBundling(Simulation simulation, int entityId, int tick, int planCount, bool traceEnabled)
    {
        var world = simulation.World;
        var selectedLocalityKey = SelectLocalityBundle(world, entityId, planCount);
        for (var plan = 1; plan <= planCount; plan++)
        {
            _processPlanAdjustedWeights[plan] = _processPlanWeights[plan] *
                (_processPlanLocalityKeys[plan] == selectedLocalityKey ? LocalityGoalSelectionBoost : 1f);
        }

        if (traceEnabled)
        {
            EmitLocalityTraceEvents(simulation, entityId, tick, planCount, selectedLocalityKey);
        }
    }

    private void EmitLocalityTraceEvents(
        Simulation simulation,
        int entityId,
        int tick,
        int planCount,
        int selectedLocalityKey)
    {
        for (var plan = 1; plan <= planCount; plan++)
        {
            var localityKey = _processPlanLocalityKeys[plan];
            if (AlreadyScoredLocality(planCount, plan, localityKey))
            {
                continue;
            }

            var score = LocalityBundleScore(simulation.World, entityId, planCount, localityKey);
            var anchorTargetId = FirstTargetAtLocality(planCount, localityKey);
            EmitProcessTrace(
                simulation,
                tick,
                entityId,
                TraceSubjectCodes.Locality,
                plan,
                0,
                0,
                _processPlanEffectScopeCodes[plan],
                _processPlanEffectValueIds[plan],
                _processPlanEffectKindIds[plan],
                anchorTargetId,
                localityKey == selectedLocalityKey ? TraceStatusCodes.Selected : TraceStatusCodes.Offered,
                0f,
                score,
                score,
                score);
        }
    }

    private int SelectLocalityBundle(WorldState world, int entityId, int planCount)
    {
        var bestKey = 0;
        var bestScore = float.NegativeInfinity;
        var committedKey = _committedPlanLocalities.GetOrDefault(entityId, ProcessLocalityCommitmentKey);
        var committedScore = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            var key = _processPlanLocalityKeys[plan];
            if (AlreadyScoredLocality(planCount, plan, key))
            {
                continue;
            }

            var score = LocalityBundleScore(world, entityId, planCount, key);
            if (key == committedKey)
            {
                committedScore = score;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }

        if (committedKey != 0 &&
            committedScore > 0f &&
            bestScore <= committedScore * (1f + TargetSwitchHysteresisRatio))
        {
            return committedKey;
        }

        return bestKey;
    }

    private bool AlreadyScoredLocality(int planCount, int plan, int localityKey)
    {
        for (var previous = 1; previous < plan && previous <= planCount; previous++)
        {
            if (_processPlanLocalityKeys[previous] == localityKey)
            {
                return true;
            }
        }

        return false;
    }

    private float LocalityBundleScore(WorldState world, int entityId, int planCount, int localityKey)
    {
        var count = 0;
        var distinctEffects = 0;
        var totalWeight = 0f;
        var maxWeight = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            if (_processPlanLocalityKeys[plan] != localityKey)
            {
                continue;
            }

            count++;
            totalWeight += _processPlanWeights[plan];
            if (_processPlanWeights[plan] > maxWeight)
            {
                maxWeight = _processPlanWeights[plan];
            }

            if (FirstPlanForEffectAtLocality(plan, localityKey, _processPlanEffectKeys[plan]))
            {
                distinctEffects++;
            }
        }

        if (count == 0)
        {
            return 0f;
        }

        var rootTargetId = FirstTargetAtLocality(planCount, localityKey);
        var distance = rootTargetId > 0
            ? HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalQ(rootTargetId),
                world.EntityPhysicalR(rootTargetId))
            : 0;
        var committedKey = _committedPlanLocalities.GetOrDefault(entityId, ProcessLocalityCommitmentKey);
        return maxWeight +
               ((totalWeight - maxWeight) * LocalityAdditionalPlanWeight) +
               ((count - 1) * LocalityDensityBonus) +
               ((distinctEffects - 1) * LocalityDistinctEffectBonus) +
               (committedKey == localityKey ? LocalityCommitmentBonus : 0f) +
               (distance == 0 ? LocalityAlreadyThereBonus : 0f) -
               (distance * LocalityDistancePenaltyPerStep);
    }

    private bool FirstPlanForEffectAtLocality(int plan, int localityKey, int effectKey)
    {
        for (var previous = 1; previous < plan; previous++)
        {
            if (_processPlanLocalityKeys[previous] == localityKey &&
                _processPlanEffectKeys[previous] == effectKey)
            {
                return false;
            }
        }

        return true;
    }

    private int FirstTargetAtLocality(int planCount, int localityKey)
    {
        for (var plan = 1; plan <= planCount; plan++)
        {
            if (_processPlanLocalityKeys[plan] == localityKey)
            {
                return _processPlanRootTargetIds[plan];
            }
        }

        return 0;
    }

}
