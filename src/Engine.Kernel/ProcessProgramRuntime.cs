namespace Engine.Kernel;

using Engine.Eventing;

public sealed class ProcessProgramRuntime : ISimulationSystem
{
    private const int ActionHistoryLength = 60;
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

    private readonly int[] _processKindIds;
    private readonly int[] _processIds;
    private readonly int[] _processCooldownTicks;
    private readonly int[] _processEntityCooldownTicks;
    private readonly int[] _processTargetKindIds;
    private readonly int[] _processTargetModes;
    private readonly int[] _processPreconditionStarts;
    private readonly int[] _processPreconditionCounts;
    private readonly int[] _processExpressionStarts;
    private readonly int[] _processExpressionCounts;
    private readonly int[] _processRewardExpressionStarts;
    private readonly int[] _processRewardExpressionCounts;
    private readonly int[] _processStepStarts;
    private readonly int[] _processStepCounts;
    private readonly int[] _processProviderSelectorStarts;
    private readonly int[] _processProviderSelectorCounts;
    private readonly int[] _processSupportRuleStarts;
    private readonly int[] _processSupportRuleCounts;
    private readonly int[] _processGoalEffectStarts;
    private readonly int[] _processGoalEffectCounts;
    private readonly int[] _goalEffectScopeCodes;
    private readonly int[] _goalEffectValueIds;
    private readonly int[] _goalEffectKindIds;
    private readonly bool[] _processEnabled;
    private readonly int[] _preconditionCodes;
    private readonly int[] _preA;
    private readonly int[] _preB;
    private readonly int[] _preC;
    private readonly int[] _preD;
    private readonly float[] _preX;
    private readonly float[] _preY;
    private readonly int[] _expressionCodes;
    private readonly int[] _expressionA;
    private readonly float[] _expressionX;
    private readonly int[] _rewardExpressionCodes;
    private readonly int[] _rewardExpressionA;
    private readonly float[] _rewardExpressionX;
    private readonly int[] _stepOperationCodes;
    private readonly int[] _stepA;
    private readonly int[] _stepB;
    private readonly int[] _stepC;
    private readonly int[] _stepD;
    private readonly float[] _stepX;
    private readonly float[] _stepY;
    private readonly int[] _stepExpressionStarts;
    private readonly int[] _stepExpressionCounts;
    private readonly int[] _mutationExpressionCodes;
    private readonly int[] _mutationExpressionA;
    private readonly float[] _mutationExpressionX;
    private readonly PrimitiveIntTable _cooldowns;
    private readonly PrimitiveIntTable _entityCooldowns;
    private readonly int[] _pathOpenSet;
    private readonly bool[] _pathOpen;
    private readonly bool[] _pathClosed;
    private readonly int[] _pathCameFrom;
    private readonly float[] _pathGScore;
    private readonly float[] _pathFScore;
    private readonly int[] _actionHistoryTicks;
    private readonly int[] _actionHistoryProcessIds;
    private readonly int[] _actionHistoryCounts;
    private readonly int[] _actionHistoryNextSlots;
    private readonly int[] _objectiveKindIds;
    private readonly int[] _objectiveCodes;
    private readonly int[] _objectiveEffectScopeCodes;
    private readonly int[] _objectiveEffectValueIds;
    private readonly int[] _objectiveEffectKindIds;
    private readonly int[] _objectiveAttributeIds;
    private readonly float[] _objectiveMinimums;
    private readonly float[] _objectiveMaximums;
    private readonly float[] _objectiveDefaults;
    private readonly float[] _objectiveBaselines;
    private readonly float[] _objectiveBaseUrgencies;
    private readonly int[] _processPlanEffectKeys;
    private readonly int[] _processPlanRootProcessSlots;
    private readonly int[] _processPlanRootTargetIds;
    private readonly int[] _processPlanProcessSlots;
    private readonly int[] _processPlanTargetIds;
    private readonly int[] _processPlanLocalityKeys;
    private readonly float[] _processPlanWeights;
    private readonly float[] _processPlanAdjustedWeights;
    private readonly int[] _providerSelectorCodes;
    private readonly int[] _providerSelectorKindIds;
    private readonly int[] _providerSelectorValueIds;
    private readonly int[] _providerSelectorRelationshipKindIds;
    private readonly int[] _providerSelectorFactIds;
    private readonly float[] _providerSelectorMinimumStrengths;
    private readonly int[] _supportEffectScopeCodes;
    private readonly int[] _supportEffectValueIds;
    private readonly int[] _supportEffectKindIds;
    private readonly int[] _supportExpressionStarts;
    private readonly int[] _supportExpressionCounts;
    private readonly float[] _supportDesiredMinimums;
    private readonly float[] _supportUrgencyScales;
    private readonly int[] _supportExpressionCodes;
    private readonly int[] _supportExpressionA;
    private readonly float[] _supportExpressionX;
    private readonly PrimitiveIntTable _committedPlanTargets;
    private readonly PrimitiveIntTable _committedPlanProcessSlots;
    private readonly PrimitiveIntTable _committedPlanLocalities;
    private readonly int[] _processVisitedEpochs;
    private int _processCount;
    private int _preconditionCount;
    private int _expressionCount;
    private int _rewardExpressionCount;
    private int _stepCount;
    private int _mutationExpressionCount;
    private int _objectiveCount;
    private int _providerSelectorCount;
    private int _supportRuleCount;
    private int _supportExpressionCount;
    private int _goalEffectCount;
    private int _processVisitedEpoch;
    private int _openProcessSlot;

    private const int ProcessLocalityCommitmentKey = 1;
    private const float LocalityGoalSelectionBoost = 3f;
    private const float LocalityAdditionalPlanWeight = 0.65f;
    private const float LocalityDensityBonus = 0.15f;
    private const float LocalityDistinctEffectBonus = 0.08f;
    private const float LocalityDistancePenaltyPerStep = 0.05f;
    private const float LocalityCommitmentBonus = 0.2f;
    private const float LocalityAlreadyThereBonus = 0.15f;

    public ProcessProgramRuntime(
        int processCapacity,
        int stepCapacity,
        int cooldownCapacity,
        int pathCapacity,
        int entityCapacity,
        int processObjectiveCapacity,
        int processProviderSelectorCapacity,
        int processSupportRuleCapacity,
        int processCommitmentCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pathCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entityCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processObjectiveCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processProviderSelectorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processSupportRuleCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processCommitmentCapacity);
        _processKindIds = new int[processCapacity + 1];
        _processIds = new int[processCapacity + 1];
        _processCooldownTicks = new int[processCapacity + 1];
        _processEntityCooldownTicks = new int[processCapacity + 1];
        _processTargetKindIds = new int[processCapacity + 1];
        _processTargetModes = new int[processCapacity + 1];
        _processPreconditionStarts = new int[processCapacity + 1];
        _processPreconditionCounts = new int[processCapacity + 1];
        _processExpressionStarts = new int[processCapacity + 1];
        _processExpressionCounts = new int[processCapacity + 1];
        _processRewardExpressionStarts = new int[processCapacity + 1];
        _processRewardExpressionCounts = new int[processCapacity + 1];
        _processStepStarts = new int[processCapacity + 1];
        _processStepCounts = new int[processCapacity + 1];
        _processProviderSelectorStarts = new int[processCapacity + 1];
        _processProviderSelectorCounts = new int[processCapacity + 1];
        _processSupportRuleStarts = new int[processCapacity + 1];
        _processSupportRuleCounts = new int[processCapacity + 1];
        _processGoalEffectStarts = new int[processCapacity + 1];
        _processGoalEffectCounts = new int[processCapacity + 1];
        _goalEffectScopeCodes = new int[stepCapacity + 1];
        _goalEffectValueIds = new int[stepCapacity + 1];
        _goalEffectKindIds = new int[stepCapacity + 1];
        _processEnabled = new bool[processCapacity + 1];
        _preconditionCodes = new int[stepCapacity + 1];
        _preA = new int[stepCapacity + 1];
        _preB = new int[stepCapacity + 1];
        _preC = new int[stepCapacity + 1];
        _preD = new int[stepCapacity + 1];
        _preX = new float[stepCapacity + 1];
        _preY = new float[stepCapacity + 1];
        _expressionCodes = new int[stepCapacity + 1];
        _expressionA = new int[stepCapacity + 1];
        _expressionX = new float[stepCapacity + 1];
        _rewardExpressionCodes = new int[stepCapacity + 1];
        _rewardExpressionA = new int[stepCapacity + 1];
        _rewardExpressionX = new float[stepCapacity + 1];
        _stepOperationCodes = new int[stepCapacity + 1];
        _stepA = new int[stepCapacity + 1];
        _stepB = new int[stepCapacity + 1];
        _stepC = new int[stepCapacity + 1];
        _stepD = new int[stepCapacity + 1];
        _stepX = new float[stepCapacity + 1];
        _stepY = new float[stepCapacity + 1];
        _stepExpressionStarts = new int[stepCapacity + 1];
        _stepExpressionCounts = new int[stepCapacity + 1];
        _mutationExpressionCodes = new int[stepCapacity + 1];
        _mutationExpressionA = new int[stepCapacity + 1];
        _mutationExpressionX = new float[stepCapacity + 1];
        _cooldowns = new PrimitiveIntTable(cooldownCapacity);
        _entityCooldowns = new PrimitiveIntTable(cooldownCapacity);
        _pathOpenSet = new int[pathCapacity];
        _pathOpen = new bool[pathCapacity];
        _pathClosed = new bool[pathCapacity];
        _pathCameFrom = new int[pathCapacity];
        _pathGScore = new float[pathCapacity];
        _pathFScore = new float[pathCapacity];
        _actionHistoryTicks = new int[(entityCapacity + 1) * ActionHistoryLength];
        _actionHistoryProcessIds = new int[(entityCapacity + 1) * ActionHistoryLength];
        _actionHistoryCounts = new int[entityCapacity + 1];
        _actionHistoryNextSlots = new int[entityCapacity + 1];
        _objectiveKindIds = new int[processObjectiveCapacity + 1];
        _objectiveCodes = new int[processObjectiveCapacity + 1];
        _objectiveEffectScopeCodes = new int[processObjectiveCapacity + 1];
        _objectiveEffectValueIds = new int[processObjectiveCapacity + 1];
        _objectiveEffectKindIds = new int[processObjectiveCapacity + 1];
        _objectiveAttributeIds = new int[processObjectiveCapacity + 1];
        _objectiveMinimums = new float[processObjectiveCapacity + 1];
        _objectiveMaximums = new float[processObjectiveCapacity + 1];
        _objectiveDefaults = new float[processObjectiveCapacity + 1];
        _objectiveBaselines = new float[processObjectiveCapacity + 1];
        _objectiveBaseUrgencies = new float[processObjectiveCapacity + 1];
        _processPlanEffectKeys = new int[processObjectiveCapacity + 1];
        _processPlanRootProcessSlots = new int[processObjectiveCapacity + 1];
        _processPlanRootTargetIds = new int[processObjectiveCapacity + 1];
        _processPlanProcessSlots = new int[processObjectiveCapacity + 1];
        _processPlanTargetIds = new int[processObjectiveCapacity + 1];
        _processPlanLocalityKeys = new int[processObjectiveCapacity + 1];
        _processPlanWeights = new float[processObjectiveCapacity + 1];
        _processPlanAdjustedWeights = new float[processObjectiveCapacity + 1];
        _providerSelectorCodes = new int[processProviderSelectorCapacity + 1];
        _providerSelectorKindIds = new int[processProviderSelectorCapacity + 1];
        _providerSelectorValueIds = new int[processProviderSelectorCapacity + 1];
        _providerSelectorRelationshipKindIds = new int[processProviderSelectorCapacity + 1];
        _providerSelectorFactIds = new int[processProviderSelectorCapacity + 1];
        _providerSelectorMinimumStrengths = new float[processProviderSelectorCapacity + 1];
        _supportEffectScopeCodes = new int[processSupportRuleCapacity + 1];
        _supportEffectValueIds = new int[processSupportRuleCapacity + 1];
        _supportEffectKindIds = new int[processSupportRuleCapacity + 1];
        _supportExpressionStarts = new int[processSupportRuleCapacity + 1];
        _supportExpressionCounts = new int[processSupportRuleCapacity + 1];
        _supportDesiredMinimums = new float[processSupportRuleCapacity + 1];
        _supportUrgencyScales = new float[processSupportRuleCapacity + 1];
        _supportExpressionCodes = new int[stepCapacity + 1];
        _supportExpressionA = new int[stepCapacity + 1];
        _supportExpressionX = new float[stepCapacity + 1];
        _committedPlanTargets = new PrimitiveIntTable(processCommitmentCapacity);
        _committedPlanProcessSlots = new PrimitiveIntTable(processCommitmentCapacity);
        _committedPlanLocalities = new PrimitiveIntTable(processCommitmentCapacity);
        _processVisitedEpochs = new int[entityCapacity + 1];
    }

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
        var totalWeight = 0f;
        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (!ProcessCanRunForEntity(processSlot, kindId, entityId, tick))
            {
                continue;
            }

            totalWeight += EvaluateTargets(
                simulation,
                processSlot,
                entityId,
                tick,
                ref candidateCount,
                selectRoll: -1f,
                out _);
        }

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
        var selectedProcessSlot = 0;
        var selectedTargetId = 0;
        var cumulative = 0f;
        var ignoredCandidateCount = 0;
        for (var processSlot = 1; processSlot <= _processCount && selectedProcessSlot == 0; processSlot++)
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
                selectedProcessSlot = processSlot;
            }
        }

        if (selectedProcessSlot == 0)
        {
            return;
        }

        var processId = _processIds[selectedProcessSlot];
        ExecuteProcess(simulation, entityId, selectedTargetId, selectedProcessSlot, tick);
        RecordAction(entityId, processId, tick);
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

        WriteProcessEvaluated(simulation, tick, entityId, kindId, processId, candidateCount, KernelEventState.Succeeded);
        WriteProcessExecuted(simulation, tick, entityId, selectedTargetId, kindId, processId);
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
        var candidateCount = 0;
        var totalWeight = EvaluatePlannedCandidates(
            simulation,
            entityId,
            kindId,
            tick,
            selectRoll: -1f,
            ref candidateCount,
            out _,
            out _);
        if (candidateCount == 0)
        {
            return;
        }

        if (totalWeight <= 0f)
        {
            WriteProcessEvaluated(simulation, tick, entityId, kindId, 0, candidateCount, KernelEventState.None);
            return;
        }

        var selectedCandidateCount = 0;
        var roll = simulation.NextFloat() * totalWeight;
        _ = EvaluatePlannedCandidates(
            simulation,
            entityId,
            kindId,
            tick,
            roll,
            ref selectedCandidateCount,
            out var selectedProcessSlot,
            out var selectedTargetId);
        if (selectedProcessSlot == 0 || selectedTargetId == 0)
        {
            return;
        }

        var processId = _processIds[selectedProcessSlot];
        ExecuteProcess(simulation, entityId, selectedTargetId, selectedProcessSlot, tick);
        RecordAction(entityId, processId, tick);
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

        WriteProcessEvaluated(simulation, tick, entityId, kindId, processId, candidateCount, KernelEventState.Succeeded);
        WriteProcessExecuted(simulation, tick, entityId, selectedTargetId, kindId, processId);
    }

    private float EvaluatePlannedCandidates(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        float selectRoll,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var planCount = 0;
        for (var objective = 1; objective <= _objectiveCount; objective++)
        {
            if (_objectiveKindIds[objective] != kindId)
            {
                continue;
            }

            var urgency = ObjectiveUrgency(simulation.World, entityId, objective);
            if (urgency < MinimumObjectiveUrgency)
            {
                continue;
            }

            var effectKey = EffectKey(
                _objectiveEffectScopeCodes[objective],
                _objectiveEffectValueIds[objective],
                _objectiveEffectKindIds[objective]);
            var weight = EvaluateBestPlanForEffect(
                simulation,
                entityId,
                kindId,
                tick,
                _objectiveEffectScopeCodes[objective],
                _objectiveEffectValueIds[objective],
                _objectiveEffectKindIds[objective],
                forcedTargetId: 0,
                urgency,
                depth: 0,
                selectRoll: -1f,
                ref candidateCount,
                out var rootProcessSlot,
                out var rootTargetId,
                out selectedProcessSlot,
                out selectedTargetId);

            if (weight <= 0f || rootProcessSlot == 0 || rootTargetId == 0 || selectedProcessSlot == 0 || selectedTargetId == 0)
            {
                continue;
            }

            planCount++;
            _processPlanEffectKeys[planCount] = effectKey;
            _processPlanRootProcessSlots[planCount] = rootProcessSlot;
            _processPlanRootTargetIds[planCount] = rootTargetId;
            _processPlanProcessSlots[planCount] = selectedProcessSlot;
            _processPlanTargetIds[planCount] = selectedTargetId;
            _processPlanLocalityKeys[planCount] = LocalityKey(simulation.World, rootTargetId);
            _processPlanWeights[planCount] = weight;
        }

        if (planCount == 0)
        {
            return 0f;
        }

        ApplyLocalityBundling(simulation.World, entityId, planCount);

        var total = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            total += _processPlanAdjustedWeights[plan];
        }

        if (selectRoll < 0f)
        {
            return total;
        }

        var cumulative = 0f;
        for (var plan = 1; plan <= planCount; plan++)
        {
            cumulative += _processPlanAdjustedWeights[plan];
            if (selectRoll > cumulative)
            {
                continue;
            }

            selectedProcessSlot = _processPlanProcessSlots[plan];
            selectedTargetId = _processPlanTargetIds[plan];
            _committedPlanTargets.Set(entityId, _processPlanEffectKeys[plan], _processPlanRootTargetIds[plan]);
            _committedPlanProcessSlots.Set(entityId, _processPlanEffectKeys[plan], _processPlanRootProcessSlots[plan]);
            _committedPlanLocalities.Set(entityId, ProcessLocalityCommitmentKey, _processPlanLocalityKeys[plan]);
            return total;
        }

        return total;
    }

    private void ApplyLocalityBundling(WorldState world, int entityId, int planCount)
    {
        var selectedLocalityKey = SelectLocalityBundle(world, entityId, planCount);
        for (var plan = 1; plan <= planCount; plan++)
        {
            _processPlanAdjustedWeights[plan] = _processPlanWeights[plan] *
                (_processPlanLocalityKeys[plan] == selectedLocalityKey ? LocalityGoalSelectionBoost : 1f);
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

    private float EvaluateBestPlanForEffect(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        float selectRoll,
        ref int candidateCount,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        if (depth > MaxPlanningDepth)
        {
            return 0f;
        }

        var bestWeight = 0f;
        var bestRootProcessSlot = 0;
        var bestRootTargetId = 0;
        var bestProcessSlot = 0;
        var bestTargetId = 0;
        var committedWeight = 0f;
        var committedRootProcessSlot = 0;
        var committedRootTargetId = 0;
        var committedProcessSlot = 0;
        var committedTargetId = 0;
        var useCommitment = depth == 0;
        var effectKey = useCommitment ? EffectKey(effectScopeCode, effectValueId, effectKindId) : 0;
        var previousTargetId = useCommitment ? _committedPlanTargets.GetOrDefault(entityId, effectKey) : 0;
        var previousProcessSlot = useCommitment ? _committedPlanProcessSlots.GetOrDefault(entityId, effectKey) : 0;

        for (var processSlot = 1; processSlot <= _processCount; processSlot++)
        {
            if (!ProcessCanRunForEntity(processSlot, kindId, entityId, tick) ||
                !ProcessServesEffect(processSlot, effectScopeCode, effectValueId, effectKindId))
            {
                continue;
            }

            var weight = EvaluateBestTargetsForProcess(
                simulation,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                forcedTargetId,
                urgency,
                depth,
                ref candidateCount,
                out var candidateRootProcessSlot,
                out var candidateRootTargetId,
                out var candidateProcessSlot,
                out var candidateTargetId);

            if (candidateRootProcessSlot == previousProcessSlot && candidateRootTargetId == previousTargetId)
            {
                committedWeight = weight;
                committedRootProcessSlot = candidateRootProcessSlot;
                committedRootTargetId = candidateRootTargetId;
                committedProcessSlot = candidateProcessSlot;
                committedTargetId = candidateTargetId;
            }

            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestRootProcessSlot = candidateRootProcessSlot;
                bestRootTargetId = candidateRootTargetId;
                bestProcessSlot = candidateProcessSlot;
                bestTargetId = candidateTargetId;
            }

            if (useCommitment &&
                previousProcessSlot == processSlot &&
                previousTargetId > 0 &&
                TargetEligibleForProcess(simulation.World, entityId, processSlot, forcedTargetId, previousTargetId))
            {
                candidateCount++;
                var previousWeight = EvaluatePlanCandidate(
                    simulation,
                    entityId,
                    previousTargetId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    ref candidateCount,
                    out var previousLeafProcessSlot,
                    out var previousLeafTargetId);
                if (previousWeight > 0f)
                {
                    committedWeight = previousWeight;
                    committedRootProcessSlot = processSlot;
                    committedRootTargetId = previousTargetId;
                    committedProcessSlot = previousLeafProcessSlot;
                    committedTargetId = previousLeafTargetId;
                }
            }
        }

        if (useCommitment &&
            committedWeight > 0f &&
            bestWeight <= committedWeight * (1f + TargetSwitchHysteresisRatio))
        {
            bestWeight = committedWeight;
            bestRootProcessSlot = committedRootProcessSlot;
            bestRootTargetId = committedRootTargetId;
            bestProcessSlot = committedProcessSlot;
            bestTargetId = committedTargetId;
        }

        if (selectRoll >= 0f && selectRoll <= bestWeight && bestProcessSlot > 0)
        {
            selectedRootProcessSlot = bestRootProcessSlot;
            selectedRootTargetId = bestRootTargetId;
            selectedProcessSlot = bestProcessSlot;
            selectedTargetId = bestTargetId;
            if (useCommitment)
            {
                _committedPlanTargets.Set(entityId, effectKey, bestRootTargetId);
                _committedPlanProcessSlots.Set(entityId, effectKey, bestRootProcessSlot);
            }
        }
        else if (selectRoll < 0f && bestProcessSlot > 0)
        {
            selectedRootProcessSlot = bestRootProcessSlot;
            selectedRootTargetId = bestRootTargetId;
            selectedProcessSlot = bestProcessSlot;
            selectedTargetId = bestTargetId;
        }

        return bestWeight;
    }

    private float EvaluateBestTargetsForProcess(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        int forcedTargetId,
        float urgency,
        int depth,
        ref int candidateCount,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        if (forcedTargetId > 0)
        {
            if (!simulation.World.EntityIsActive(forcedTargetId))
            {
                return 0f;
            }

            candidateCount++;
            var weight = EvaluatePlanCandidate(
                simulation,
                entityId,
                forcedTargetId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                ref candidateCount,
                out selectedProcessSlot,
                out selectedTargetId);
            if (selectedProcessSlot > 0)
            {
                selectedRootProcessSlot = processSlot;
                selectedRootTargetId = forcedTargetId;
            }

            return weight;
        }

        var selectorStart = _processProviderSelectorStarts[processSlot];
        var selectorCount = _processProviderSelectorCounts[processSlot];
        if (selectorCount > 0)
        {
            return EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selectorStart,
                selectorCount,
                ref candidateCount,
                out selectedRootProcessSlot,
                out selectedRootTargetId,
                out selectedProcessSlot,
                out selectedTargetId);
        }

        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.Self)
        {
            candidateCount++;
            var weight = EvaluatePlanCandidate(
                simulation,
                entityId,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                ref candidateCount,
                out selectedProcessSlot,
                out selectedTargetId);
            if (selectedProcessSlot > 0)
            {
                selectedRootProcessSlot = processSlot;
                selectedRootTargetId = entityId;
            }

            return weight;
        }

        var bestWeight = 0f;
        var world = simulation.World;
        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.KindOther &&
            _processTargetKindIds[processSlot] > 0)
        {
            for (var targetId = world.FirstEntityOfKind(_processTargetKindIds[processSlot]);
                 targetId > 0;
                 targetId = world.NextEntityOfKind(targetId))
            {
                if (!world.EntityIsActive(targetId) || targetId == entityId)
                {
                    continue;
                }

                candidateCount++;
                var weight = EvaluatePlanCandidate(
                    simulation,
                    entityId,
                    targetId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    ref candidateCount,
                    out var planProcessSlot,
                    out var planTargetId);
                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    selectedRootProcessSlot = processSlot;
                    selectedRootTargetId = targetId;
                    selectedProcessSlot = planProcessSlot;
                    selectedTargetId = planTargetId;
                }
            }

            return bestWeight;
        }

        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            if (!world.EntityIsActive(targetId) || targetId == entityId)
            {
                continue;
            }

            candidateCount++;
            var weight = EvaluatePlanCandidate(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                ref candidateCount,
                out var planProcessSlot,
                out var planTargetId);
            if (weight > bestWeight)
            {
                bestWeight = weight;
                selectedRootProcessSlot = processSlot;
                selectedRootTargetId = targetId;
                selectedProcessSlot = planProcessSlot;
                selectedTargetId = planTargetId;
            }
        }

        return bestWeight;
    }

    private float EvaluateProviderSelectorTargets(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selectorStart,
        int selectorCount,
        ref int candidateCount,
        out int selectedRootProcessSlot,
        out int selectedRootTargetId,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedRootProcessSlot = 0;
        selectedRootTargetId = 0;
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        var epoch = NextProcessVisitedEpoch();
        var selectorEnd = selectorStart + selectorCount;
        for (var selector = selectorStart; selector < selectorEnd; selector++)
        {
            bestWeight = EvaluateProviderSelectorTargets(
                simulation,
                entityId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                epoch,
                bestWeight,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderSelectorTargets(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        int epoch,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        switch (_providerSelectorCodes[selector])
        {
            case ProcessProviderSelectorCodes.TargetAttribute:
            case ProcessProviderSelectorCodes.Structural:
            case ProcessProviderSelectorCodes.Resource:
                return EvaluateKindProviderSelector(
                    simulation,
                    entityId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    epoch,
                    bestWeight,
                    ref candidateCount,
                    ref selectedRootProcessSlot,
                    ref selectedRootTargetId,
                    ref selectedProcessSlot,
                    ref selectedTargetId);
            case ProcessProviderSelectorCodes.LocationAttribute:
                for (var targetId = world.FirstPhysicalEntityAt(world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId));
                     targetId > 0;
                     targetId = world.NextPhysicalEntityAt(targetId))
                {
                    bestWeight = EvaluateProviderTargetIfMatched(
                        simulation,
                        entityId,
                        targetId,
                        processSlot,
                        tick,
                        effectScopeCode,
                        effectValueId,
                        effectKindId,
                        urgency,
                        depth,
                        selector,
                        epoch,
                        bestWeight,
                        ref candidateCount,
                        ref selectedRootProcessSlot,
                        ref selectedRootTargetId,
                        ref selectedProcessSlot,
                        ref selectedTargetId);
                }

                return bestWeight;
            case ProcessProviderSelectorCodes.OutgoingRelationship:
                return EvaluateRelationshipProviderSelector(
                    simulation,
                    entityId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    epoch,
                    outgoing: true,
                    bestWeight,
                    ref candidateCount,
                    ref selectedRootProcessSlot,
                    ref selectedRootTargetId,
                    ref selectedProcessSlot,
                    ref selectedTargetId);
            case ProcessProviderSelectorCodes.IncomingRelationship:
                return EvaluateRelationshipProviderSelector(
                    simulation,
                    entityId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    epoch,
                    outgoing: false,
                    bestWeight,
                    ref candidateCount,
                    ref selectedRootProcessSlot,
                    ref selectedRootTargetId,
                    ref selectedProcessSlot,
                    ref selectedTargetId);
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private float EvaluateKindProviderSelector(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        int epoch,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        var kindId = _providerSelectorKindIds[selector];
        if (kindId > 0)
        {
            for (var targetId = world.FirstEntityOfKind(kindId);
                 targetId > 0;
                 targetId = world.NextEntityOfKind(targetId))
            {
                bestWeight = EvaluateProviderTargetIfMatched(
                    simulation,
                    entityId,
                    targetId,
                    processSlot,
                    tick,
                    effectScopeCode,
                    effectValueId,
                    effectKindId,
                    urgency,
                    depth,
                    selector,
                    epoch,
                    bestWeight,
                    ref candidateCount,
                    ref selectedRootProcessSlot,
                    ref selectedRootTargetId,
                    ref selectedProcessSlot,
                    ref selectedTargetId);
            }

            return bestWeight;
        }

        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            bestWeight = EvaluateProviderTargetIfMatched(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                epoch,
                bestWeight,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateRelationshipProviderSelector(
        Simulation simulation,
        int entityId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        int epoch,
        bool outgoing,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _providerSelectorRelationshipKindIds[selector],
                _providerSelectorMinimumStrengths[selector])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _providerSelectorRelationshipKindIds[selector],
                _providerSelectorMinimumStrengths[selector]);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            if (!world.RelationshipHasFact(relationshipId, _providerSelectorFactIds[selector]))
            {
                continue;
            }

            var targetId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            bestWeight = EvaluateProviderTargetIfMatched(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                effectScopeCode,
                effectValueId,
                effectKindId,
                urgency,
                depth,
                selector,
                epoch,
                bestWeight,
                ref candidateCount,
                ref selectedRootProcessSlot,
                ref selectedRootTargetId,
                ref selectedProcessSlot,
                ref selectedTargetId);
        }

        return bestWeight;
    }

    private float EvaluateProviderTargetIfMatched(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        int selector,
        int epoch,
        float bestWeight,
        ref int candidateCount,
        ref int selectedRootProcessSlot,
        ref int selectedRootTargetId,
        ref int selectedProcessSlot,
        ref int selectedTargetId)
    {
        var world = simulation.World;
        if (!world.EntityIsActive(targetId) || _processVisitedEpochs[targetId] == epoch)
        {
            return bestWeight;
        }

        var selectorKindId = _providerSelectorKindIds[selector];
        if (selectorKindId > 0 && world.EntityKindId(targetId) != selectorKindId)
        {
            return bestWeight;
        }

        if (ProviderSelectorRequiresAttribute(selector) &&
            !world.HasAttribute(targetId, _providerSelectorValueIds[selector]))
        {
            return bestWeight;
        }

        if (_providerSelectorCodes[selector] == ProcessProviderSelectorCodes.Resource &&
            world.GetResource(
                _providerSelectorValueIds[selector],
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId)) <= 0f)
        {
            return bestWeight;
        }

        _processVisitedEpochs[targetId] = epoch;
        candidateCount++;
        var weight = EvaluatePlanCandidate(
            simulation,
            entityId,
            targetId,
            processSlot,
            tick,
            effectScopeCode,
            effectValueId,
            effectKindId,
            urgency,
            depth,
            ref candidateCount,
            out var planProcessSlot,
            out var planTargetId);
        if (weight > bestWeight)
        {
            selectedRootProcessSlot = processSlot;
            selectedRootTargetId = targetId;
            selectedProcessSlot = planProcessSlot;
            selectedTargetId = planTargetId;
            return weight;
        }

        return bestWeight;
    }

    private float EvaluatePlanCandidate(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        int effectScopeCode,
        int effectValueId,
        int effectKindId,
        float urgency,
        int depth,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var world = simulation.World;
        var reward = EvaluateReward(world, processSlot, entityId, targetId);
        if (_processTargetModes[processSlot] == ProcessTargetModeCodes.Self && reward < MinimumSelfDirectedReward)
        {
            reward = MinimumSelfDirectedReward;
        }

        if (_processTargetModes[processSlot] != ProcessTargetModeCodes.Self && reward <= 0f)
        {
            return 0f;
        }

        var planningWeight = 1f + global::System.MathF.Log(1f + reward);
        var weightedUrgency = urgency * planningWeight * global::System.MathF.Pow(DepthPenalty, depth);
        var preconditionResult = PreconditionsResult(
            simulation,
            processSlot,
            entityId,
            targetId,
            tick,
            out var blockerScopeCode,
            out var blockerValueId,
            out var blockerKindId,
            out var blockerTargetId);
        if (preconditionResult == PreconditionFailed)
        {
            if (depth >= MaxPlanningDepth || blockerScopeCode == 0 || blockerTargetId == 0)
            {
                return 0f;
            }

            return EvaluateBestPlanForEffect(
                simulation,
                entityId,
                world.EntityKindId(entityId),
                tick,
                blockerScopeCode,
                blockerValueId,
                blockerKindId,
                blockerTargetId,
                weightedUrgency * SubGoalUrgencyScale,
                depth + 1,
                selectRoll: -1f,
                ref candidateCount,
                out _,
                out _,
                out selectedProcessSlot,
                out selectedTargetId);
        }

        var score = EvaluateScore(world, processSlot, entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _processIds[processSlot], tick - RecencyWindow);
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        var immediateWeight = weightedUrgency * Max(score, MinimumActionableScoreWeight);
        var bestWeight = immediateWeight;
        selectedProcessSlot = processSlot;
        selectedTargetId = targetId;

        if (preconditionResult == PreconditionPassed && depth < MaxPlanningDepth)
        {
            var supportWeight = EvaluateBestSupportPlan(
                simulation,
                entityId,
                targetId,
                processSlot,
                tick,
                weightedUrgency,
                score,
                depth + 1,
                ref candidateCount,
                out var supportProcessSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedProcessSlot = supportProcessSlot;
                selectedTargetId = supportTargetId;
            }
        }

        return bestWeight <= 0f ? 0f : bestWeight;
    }

    private float EvaluateBestSupportPlan(
        Simulation simulation,
        int entityId,
        int targetId,
        int processSlot,
        int tick,
        float weightedUrgency,
        float processScore,
        int depth,
        ref int candidateCount,
        out int selectedProcessSlot,
        out int selectedTargetId)
    {
        selectedProcessSlot = 0;
        selectedTargetId = 0;
        var bestWeight = 0f;
        var first = _processSupportRuleStarts[processSlot];
        var end = first + _processSupportRuleCounts[processSlot];
        var scoreHeadroom = 1f - Clamp01(processScore);
        for (var support = first; support < end; support++)
        {
            var currentValue = EvaluateSupportValue(simulation.World, support, entityId, targetId);
            var desiredMinimum = _supportDesiredMinimums[support];
            var deficitRatio = Clamp01((desiredMinimum - currentValue) / desiredMinimum);
            if (deficitRatio <= 0f)
            {
                continue;
            }

            var childBaseUrgency = weightedUrgency *
                SubGoalUrgencyScale *
                _supportUrgencyScales[support] *
                Max(deficitRatio, scoreHeadroom);
            if (childBaseUrgency <= 0f)
            {
                continue;
            }

            var forcedTargetId = SupportTargetId(entityId, targetId, _supportEffectScopeCodes[support]);
            var supportWeight = EvaluateBestPlanForEffect(
                simulation,
                entityId,
                simulation.World.EntityKindId(entityId),
                tick,
                _supportEffectScopeCodes[support],
                _supportEffectValueIds[support],
                _supportEffectKindIds[support],
                forcedTargetId,
                childBaseUrgency,
                depth,
                selectRoll: -1f,
                ref candidateCount,
                out _,
                out _,
                out var supportProcessSlot,
                out var supportTargetId);
            if (supportWeight > bestWeight)
            {
                bestWeight = supportWeight;
                selectedProcessSlot = supportProcessSlot;
                selectedTargetId = supportTargetId;
            }
        }

        return bestWeight;
    }

    private bool ProcessServesEffect(int processSlot, int effectScopeCode, int effectValueId, int effectKindId)
    {
        var first = _processGoalEffectStarts[processSlot];
        var end = first + _processGoalEffectCounts[processSlot];
        for (var effect = first; effect < end; effect++)
        {
            if (_goalEffectScopeCodes[effect] == effectScopeCode &&
                _goalEffectValueIds[effect] == effectValueId &&
                (effectKindId == 0 ||
                 _goalEffectKindIds[effect] == 0 ||
                 _goalEffectKindIds[effect] == effectKindId))
            {
                return true;
            }
        }

        return false;
    }

    private bool ProviderSelectorRequiresAttribute(int selector)
    {
        if (_providerSelectorValueIds[selector] == 0)
        {
            return false;
        }

        return _providerSelectorCodes[selector] is
            ProcessProviderSelectorCodes.TargetAttribute or
            ProcessProviderSelectorCodes.LocationAttribute or
            ProcessProviderSelectorCodes.OutgoingRelationship or
            ProcessProviderSelectorCodes.IncomingRelationship;
    }

    private bool TargetEligibleForProcess(
        WorldState world,
        int entityId,
        int processSlot,
        int forcedTargetId,
        int targetId)
    {
        if (!world.EntityIsActive(targetId))
        {
            return false;
        }

        if (forcedTargetId > 0)
        {
            return targetId == forcedTargetId;
        }

        var selectorStart = _processProviderSelectorStarts[processSlot];
        var selectorEnd = selectorStart + _processProviderSelectorCounts[processSlot];
        if (selectorEnd > selectorStart)
        {
            for (var selector = selectorStart; selector < selectorEnd; selector++)
            {
                if (ProviderSelectorMatchesTarget(world, entityId, targetId, selector))
                {
                    return true;
                }
            }

            return false;
        }

        return _processTargetModes[processSlot] switch
        {
            ProcessTargetModeCodes.Self => targetId == entityId,
            ProcessTargetModeCodes.KindOther => targetId != entityId &&
                _processTargetKindIds[processSlot] > 0 &&
                world.EntityKindId(targetId) == _processTargetKindIds[processSlot],
            _ => targetId != entityId,
        };
    }

    private bool ProviderSelectorMatchesTarget(WorldState world, int entityId, int targetId, int selector)
    {
        var selectorKindId = _providerSelectorKindIds[selector];
        if (selectorKindId > 0 && world.EntityKindId(targetId) != selectorKindId)
        {
            return false;
        }

        if (ProviderSelectorRequiresAttribute(selector) &&
            !world.HasAttribute(targetId, _providerSelectorValueIds[selector]))
        {
            return false;
        }

        return _providerSelectorCodes[selector] switch
        {
            ProcessProviderSelectorCodes.TargetAttribute => true,
            ProcessProviderSelectorCodes.Structural => true,
            ProcessProviderSelectorCodes.Resource => world.GetResource(
                _providerSelectorValueIds[selector],
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId)) > 0f,
            ProcessProviderSelectorCodes.LocationAttribute =>
                world.EntityPhysicalQ(entityId) == world.EntityPhysicalQ(targetId) &&
                world.EntityPhysicalR(entityId) == world.EntityPhysicalR(targetId),
            ProcessProviderSelectorCodes.OutgoingRelationship =>
                RelationshipSelectorMatchesTarget(world, entityId, targetId, selector),
            ProcessProviderSelectorCodes.IncomingRelationship =>
                RelationshipSelectorMatchesTarget(world, targetId, entityId, selector),
            _ => false,
        };
    }

    private bool RelationshipSelectorMatchesTarget(WorldState world, int sourceId, int targetId, int selector)
    {
        var relationshipKindId = _providerSelectorRelationshipKindIds[selector];
        if (relationshipKindId <= 0)
        {
            return false;
        }

        var relationshipId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        return relationshipId > 0 &&
               world.RelationshipStrength(relationshipId) >= _providerSelectorMinimumStrengths[selector] &&
               world.RelationshipHasFact(relationshipId, _providerSelectorFactIds[selector]);
    }

    private float EvaluateSupportValue(WorldState world, int supportRuleSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _supportExpressionStarts[supportRuleSlot],
            _supportExpressionCounts[supportRuleSlot],
            _supportExpressionCodes,
            _supportExpressionA,
            _supportExpressionX,
            defaultValue: 0f);
    }

    private static int SupportTargetId(int entityId, int targetId, int effectScopeCode)
    {
        return effectScopeCode switch
        {
            ProcessEffectScopeCodes.TargetAttribute or ProcessEffectScopeCodes.Relational => targetId,
            ProcessEffectScopeCodes.Structural when targetId != entityId => targetId,
            _ => entityId,
        };
    }

    private int NextProcessVisitedEpoch()
    {
        _processVisitedEpoch++;
        if (_processVisitedEpoch < int.MaxValue)
        {
            return _processVisitedEpoch;
        }

        global::System.Array.Clear(_processVisitedEpochs);
        _processVisitedEpoch = 1;
        return _processVisitedEpoch;
    }

    private static int EffectKey(int effectScopeCode, int effectValueId, int effectKindId)
    {
        unchecked
        {
            var hash = (uint)effectScopeCode * 73856093u;
            hash ^= (uint)effectValueId * 19349663u;
            hash ^= (uint)effectKindId * 83492791u;
            var key = (int)(hash & 2147483647u);
            return key == 0 ? 1 : key;
        }
    }

    private static int LocalityKey(WorldState world, int targetId)
    {
        unchecked
        {
            var q = world.EntityPhysicalQ(targetId) + 32768;
            var r = world.EntityPhysicalR(targetId) + 32768;
            var key = (q << 16) ^ (r & 65535);
            return key == 0 ? 1 : key;
        }
    }

    private float ObjectiveUrgency(WorldState world, int entityId, int objective)
    {
        return _objectiveCodes[objective] switch
        {
            ProcessObjectiveUrgencyCodes.StandardNeed => StandardNeedUrgency(world.GetAttribute(entityId, _objectiveAttributeIds[objective])),
            ProcessObjectiveUrgencyCodes.Baseline => BaselineUrgency(world.GetAttribute(entityId, _objectiveAttributeIds[objective]), _objectiveBaselines[objective]),
            ProcessObjectiveUrgencyCodes.RangeDefault => RangeUrgency(
                world.GetAttribute(entityId, _objectiveAttributeIds[objective]),
                _objectiveMinimums[objective],
                _objectiveMaximums[objective],
                _objectiveDefaults[objective]),
            ProcessObjectiveUrgencyCodes.Fixed => _objectiveBaseUrgencies[objective],
            _ => 0f,
        };
    }

    private float EvaluateTargets(
        Simulation simulation,
        int processSlot,
        int entityId,
        int tick,
        ref int candidateCount,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var targetMode = _processTargetModes[processSlot];
        if (targetMode == ProcessTargetModeCodes.Self)
        {
            candidateCount++;
            return EvaluateCandidate(
                simulation,
                processSlot,
                entityId,
                entityId,
                tick,
                selectRoll,
                out selectedTargetId);
        }

        var total = 0f;
        var world = simulation.World;
        if (targetMode == ProcessTargetModeCodes.KindOther && _processTargetKindIds[processSlot] > 0)
        {
            for (var targetId = world.FirstEntityOfKind(_processTargetKindIds[processSlot]);
                 targetId > 0;
                 targetId = world.NextEntityOfKind(targetId))
            {
                if (!world.EntityIsActive(targetId) || targetId == entityId)
                {
                    continue;
                }

                candidateCount++;
                var score = EvaluateCandidate(
                    simulation,
                    processSlot,
                    entityId,
                    targetId,
                    tick,
                    selectRoll < 0f ? -1f : selectRoll - total,
                    out selectedTargetId);
                total += score;
                if (selectedTargetId > 0)
                {
                    return total;
                }
            }

            return total;
        }

        for (var targetId = 1; targetId <= world.EntityHighWatermark; targetId++)
        {
            if (!world.EntityIsActive(targetId) || targetId == entityId)
            {
                continue;
            }

            candidateCount++;
            var score = EvaluateCandidate(
                simulation,
                processSlot,
                entityId,
                targetId,
                tick,
                selectRoll < 0f ? -1f : selectRoll - total,
                out selectedTargetId);
            total += score;
            if (selectedTargetId > 0)
            {
                return total;
            }
        }

        return total;
    }

    private float EvaluateCandidate(
        Simulation simulation,
        int processSlot,
        int entityId,
        int targetId,
        int tick,
        float selectRoll,
        out int selectedTargetId)
    {
        selectedTargetId = 0;
        var preconditionResult = PreconditionsResult(simulation, processSlot, entityId, targetId, tick);
        if (preconditionResult == PreconditionFailed)
        {
            return 0f;
        }

        var score = EvaluateScore(simulation.World, processSlot, entityId, targetId);
        var recentActionCount = CountRecentActions(entityId, _processIds[processSlot], tick - RecencyWindow);
        for (var i = 0; i < recentActionCount; i++)
        {
            score *= RecencyDecayFactor;
        }

        if (score <= 0f)
        {
            return 0f;
        }

        if (selectRoll >= 0f && selectRoll <= score)
        {
            selectedTargetId = targetId;
        }

        return score;
    }

    private int PreconditionsResult(Simulation simulation, int processSlot, int entityId, int targetId, int tick)
    {
        return PreconditionsResult(
            simulation,
            processSlot,
            entityId,
            targetId,
            tick,
            out _,
            out _,
            out _,
            out _);
    }

    private int PreconditionsResult(
        Simulation simulation,
        int processSlot,
        int entityId,
        int targetId,
        int tick,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        var world = simulation.World;
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        var first = _processPreconditionStarts[processSlot];
        var end = first + _processPreconditionCounts[processSlot];
        for (var pre = first; pre < end; pre++)
        {
            if (PreconditionIsCompositionOp(_preconditionCodes[pre]))
            {
                return ComposedPreconditionsResult(
                    simulation,
                    processSlot,
                    entityId,
                    targetId,
                    tick,
                    first,
                    end,
                    out blockerScopeCode,
                    out blockerValueId,
                    out blockerKindId,
                    out blockerTargetId);
            }
        }

        for (var pre = first; pre < end; pre++)
        {
            if (!PreconditionPasses(simulation, processSlot, pre, entityId, targetId, tick))
            {
                ResolvePreconditionBlocker(
                    world,
                    pre,
                    entityId,
                    targetId,
                    out blockerScopeCode,
                    out blockerValueId,
                    out blockerKindId,
                    out blockerTargetId);
                return PreconditionFailed;
            }
        }

        return PreconditionPassed;
    }

    private int ComposedPreconditionsResult(
        Simulation simulation,
        int processSlot,
        int entityId,
        int targetId,
        int tick,
        int first,
        int end,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        global::System.Span<bool> stack = stackalloc bool[64];
        var stackCount = 0;
        var failedBlockerPre = 0;
        for (var pre = first; pre < end; pre++)
        {
            switch (_preconditionCodes[pre])
            {
                case ProcessPreconditionCodes.And:
                {
                    var right = PopBool(stack, ref stackCount);
                    var left = PopBool(stack, ref stackCount);
                    PushBool(stack, ref stackCount, left && right);
                    break;
                }
                case ProcessPreconditionCodes.Or:
                {
                    var right = PopBool(stack, ref stackCount);
                    var left = PopBool(stack, ref stackCount);
                    PushBool(stack, ref stackCount, left || right);
                    break;
                }
                case ProcessPreconditionCodes.Not:
                    PushBool(stack, ref stackCount, !PopBool(stack, ref stackCount));
                    break;
                default:
                    var passes = PreconditionPasses(simulation, processSlot, pre, entityId, targetId, tick);
                    if (!passes && failedBlockerPre == 0)
                    {
                        failedBlockerPre = pre;
                    }

                    PushBool(stack, ref stackCount, passes);
                    break;
            }
        }

        var final = true;
        while (stackCount > 0)
        {
            final = PopBool(stack, ref stackCount) && final;
        }

        if (final)
        {
            return PreconditionPassed;
        }

        if (failedBlockerPre != 0)
        {
            ResolvePreconditionBlocker(
                simulation.World,
                failedBlockerPre,
                entityId,
                targetId,
                out blockerScopeCode,
                out blockerValueId,
                out blockerKindId,
                out blockerTargetId);
            return PreconditionFailed;
        }

        return PreconditionFailed;
    }

    private bool PreconditionPasses(Simulation simulation, int processSlot, int pre, int entityId, int targetId, int tick)
    {
        var world = simulation.World;
        switch (_preconditionCodes[pre])
        {
            case ProcessPreconditionCodes.Always:
                return true;
            case ProcessPreconditionCodes.TargetIsSelf:
                return targetId == entityId;
            case ProcessPreconditionCodes.TargetIsNotSelf:
                return targetId != entityId;
            case ProcessPreconditionCodes.SelfAttributeRange:
                return ValueInRange(world.GetAttribute(entityId, _preA[pre]), _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
            case ProcessPreconditionCodes.TargetAttributeRange:
                return ValueInRange(world.GetAttribute(targetId, _preA[pre]), _preB[pre], _preC[pre], _preX[pre], _preY[pre]);
            case ProcessPreconditionCodes.PlaneDistance:
                return PlaneDistance(world, entityId, targetId, _preB[pre]) <= _preA[pre];
            case ProcessPreconditionCodes.LocationResourceRange:
                return LocationResourceRangePasses(world, pre, entityId, targetId);
            case ProcessPreconditionCodes.SelfHasRelationshipToTarget:
                return RelationshipPasses(world, entityId, targetId, pre);
            case ProcessPreconditionCodes.TargetHasRelationshipToSelf:
                return RelationshipPasses(world, targetId, entityId, pre);
            case ProcessPreconditionCodes.RelatedEntityMatchesTarget:
                return RelationshipPasses(world, entityId, targetId, pre);
            case ProcessPreconditionCodes.RelationshipExists:
                return RelationshipPasses(world, _preC[pre], _preD[pre], pre);
            case ProcessPreconditionCodes.PressureRange:
                return PressureRangePasses(simulation, pre, entityId);
            case ProcessPreconditionCodes.CooldownElapsed:
                return CountRecentActions(entityId, _processIds[processSlot], tick - _preA[pre] + 1) == 0;
            case ProcessPreconditionCodes.EntityCountAtLeast:
                return ActiveEntityCountOfKind(world, _preA[pre]) >= _preB[pre];
            case ProcessPreconditionCodes.HasColocatedEntity:
                return HasColocatedEntity(world, entityId);
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private static bool PreconditionIsCompositionOp(int code)
    {
        return code is ProcessPreconditionCodes.And or
            ProcessPreconditionCodes.Or or
            ProcessPreconditionCodes.Not;
    }

    private static int ActiveEntityCountOfKind(WorldState world, int kindId)
    {
        var count = 0;
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (world.EntityIsActive(entityId))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasColocatedEntity(WorldState world, int entityId)
    {
        var q = world.EntityPhysicalQ(entityId);
        var r = world.EntityPhysicalR(entityId);
        for (var colocatedId = world.FirstPhysicalEntityAt(q, r);
             colocatedId > 0;
             colocatedId = world.NextPhysicalEntityAt(colocatedId))
        {
            if (colocatedId != entityId && world.EntityIsActive(colocatedId))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolvePreconditionBlocker(
        WorldState world,
        int pre,
        int entityId,
        int targetId,
        out int blockerScopeCode,
        out int blockerValueId,
        out int blockerKindId,
        out int blockerTargetId)
    {
        blockerScopeCode = 0;
        blockerValueId = 0;
        blockerKindId = 0;
        blockerTargetId = 0;
        switch (_preconditionCodes[pre])
        {
            case ProcessPreconditionCodes.SelfAttributeRange:
                blockerScopeCode = ProcessEffectScopeCodes.SelfAttribute;
                blockerValueId = _preA[pre];
                blockerKindId = world.EntityKindId(entityId);
                blockerTargetId = entityId;
                break;
            case ProcessPreconditionCodes.TargetAttributeRange:
                blockerScopeCode = ProcessEffectScopeCodes.TargetAttribute;
                blockerValueId = _preA[pre];
                blockerKindId = world.EntityKindId(targetId);
                blockerTargetId = targetId;
                break;
            case ProcessPreconditionCodes.LocationResourceRange:
                blockerScopeCode = ProcessEffectScopeCodes.Resource;
                blockerValueId = _preA[pre];
                blockerKindId = 0;
                blockerTargetId = _preB[pre] == 0 ? entityId : targetId;
                break;
            case ProcessPreconditionCodes.PlaneDistance:
                blockerScopeCode = ProcessEffectScopeCodes.Structural;
                blockerValueId = _preC[pre];
                blockerKindId = world.EntityKindId(entityId);
                blockerTargetId = targetId != entityId ? targetId : entityId;
                break;
            case ProcessPreconditionCodes.SelfHasRelationshipToTarget:
            case ProcessPreconditionCodes.TargetHasRelationshipToSelf:
            case ProcessPreconditionCodes.RelatedEntityMatchesTarget:
            case ProcessPreconditionCodes.RelationshipExists:
                blockerScopeCode = ProcessEffectScopeCodes.Relational;
                blockerValueId = _preA[pre];
                blockerKindId = world.EntityKindId(targetId);
                blockerTargetId = targetId;
                break;
        }
    }

    private bool LocationResourceRangePasses(WorldState world, int pre, int entityId, int targetId)
    {
        var locationEntityId = _preB[pre] == 0 ? entityId : targetId;
        var value = world.GetResource(
            _preA[pre],
            world.EntityPhysicalQ(locationEntityId),
            world.EntityPhysicalR(locationEntityId));
        return ValueInRange(value, _preC[pre], _preD[pre], _preX[pre], _preY[pre]);
    }

    private bool RelationshipPasses(WorldState world, int sourceId, int targetId, int pre)
    {
        var relationshipId = world.FindRelationship(sourceId, targetId, _preA[pre]);
        return relationshipId > 0 &&
               (_preB[pre] == 0 || world.RelationshipStrength(relationshipId) >= _preX[pre]);
    }

    private bool PressureRangePasses(Simulation simulation, int pre, int entityId)
    {
        var scopeEntityId = _preB[pre] > 0 ? _preB[pre] : entityId;
        var value = simulation.Pressures.GetValue(_preA[pre], scopeEntityId);
        return ValueInRange(value, _preC[pre], _preD[pre], _preX[pre], _preY[pre]);
    }

    private float EvaluateScore(WorldState world, int processSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _processExpressionStarts[processSlot],
            _processExpressionCounts[processSlot],
            _expressionCodes,
            _expressionA,
            _expressionX,
            defaultValue: 1f);
    }

    private float EvaluateReward(WorldState world, int processSlot, int entityId, int targetId)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _processRewardExpressionStarts[processSlot],
            _processRewardExpressionCounts[processSlot],
            _rewardExpressionCodes,
            _rewardExpressionA,
            _rewardExpressionX,
            defaultValue: 1f);
    }

    private float EvaluateExpression(
        WorldState world,
        int entityId,
        int targetId,
        int first,
        int count,
        int[] codes,
        int[] a,
        float[] x,
        float defaultValue)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            first,
            count,
            codes,
            a,
            x,
            defaultValue,
            default,
            default,
            0);
    }

    private float EvaluateExpression(
        WorldState world,
        int entityId,
        int targetId,
        int first,
        int count,
        int[] codes,
        int[] a,
        float[] x,
        float defaultValue,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        if (count == 0)
        {
            return defaultValue;
        }

        global::System.Span<float> stack = stackalloc float[32];
        var stackCount = 0;
        var end = first + count;
        for (var op = first; op < end; op++)
        {
            ExecuteExpressionOp(
                world,
                codes[op],
                a[op],
                x[op],
                entityId,
                targetId,
                stack,
                ref stackCount,
                variableIds,
                variableValues,
                variableCount);
        }

        return stackCount == 0 ? 0f : stack[stackCount - 1];
    }

    private void ExecuteExpressionOp(
        WorldState world,
        int code,
        int a,
        float x,
        int entityId,
        int targetId,
        global::System.Span<float> stack,
        ref int stackCount,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        switch (code)
        {
            case ProcessExpressionCodes.Literal:
                Push(stack, ref stackCount, x);
                break;
            case ProcessExpressionCodes.SelfAttribute:
                Push(stack, ref stackCount, world.GetAttribute(entityId, a));
                break;
            case ProcessExpressionCodes.TargetAttribute:
                Push(stack, ref stackCount, world.GetAttribute(targetId, a));
                break;
            case ProcessExpressionCodes.SelfLocationResource:
                Push(stack, ref stackCount, ResourceAtEntity(world, entityId, a));
                break;
            case ProcessExpressionCodes.TargetLocationResource:
                Push(stack, ref stackCount, ResourceAtEntity(world, targetId, a));
                break;
            case ProcessExpressionCodes.Add:
                PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) + Pop(stack, ref stackCount));
                break;
            case ProcessExpressionCodes.Subtract:
                Subtract(stack, ref stackCount);
                break;
            case ProcessExpressionCodes.Multiply:
                PushBinary(stack, ref stackCount, Pop(stack, ref stackCount) * Pop(stack, ref stackCount));
                break;
            case ProcessExpressionCodes.Divide:
                Divide(stack, ref stackCount);
                break;
            case ProcessExpressionCodes.Min:
                Min(stack, ref stackCount);
                break;
            case ProcessExpressionCodes.Max:
                Max(stack, ref stackCount);
                break;
            case ProcessExpressionCodes.Clamp:
                Clamp(stack, ref stackCount);
                break;
            case ProcessExpressionCodes.Abs:
                Push(stack, ref stackCount, Abs(Pop(stack, ref stackCount)));
                break;
            case ProcessExpressionCodes.Variable:
                Push(stack, ref stackCount, VariableValue(variableIds, variableValues, variableCount, a));
                break;
            case ProcessExpressionCodes.Distance:
                Push(stack, ref stackCount, PlaneDistance(world, entityId, targetId, PlaneCodes.Physical));
                break;
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private void ExecuteProcess(Simulation simulation, int entityId, int targetId, int processSlot, int tick)
    {
        global::System.Span<int> variableIds = stackalloc int[32];
        global::System.Span<float> variableValues = stackalloc float[32];
        var variableCount = 0;
        var firstStep = _processStepStarts[processSlot];
        var endStep = firstStep + _processStepCounts[processSlot];
        for (var step = firstStep; step < endStep; step++)
        {
            ExecuteStep(
                simulation,
                entityId,
                targetId,
                tick,
                step,
                variableIds,
                variableValues,
                ref variableCount);
        }
    }

    private void ExecuteStep(
        Simulation simulation,
        int entityId,
        int targetId,
        int tick,
        int step,
        global::System.Span<int> variableIds,
        global::System.Span<float> variableValues,
        ref int variableCount)
    {
        var world = simulation.World;
        switch (_stepOperationCodes[step])
        {
            case ProcessOperationCodes.AddAttribute:
                world.ModifyAttribute(entityId, _stepA[step], simulation.ProcessSourceCodeId, _stepX[step], tick);
                break;
            case ProcessOperationCodes.SetAttribute:
                simulation.SetAttribute(entityId, _stepA[step], simulation.ProcessSourceCodeId, _stepX[step], tick);
                break;
            case ProcessOperationCodes.AddTargetAttribute:
                world.ModifyAttribute(targetId, _stepA[step], simulation.ProcessSourceCodeId, _stepX[step], tick);
                break;
            case ProcessOperationCodes.TransferAttribute:
                TransferAttribute(world, simulation.ProcessSourceCodeId, entityId, targetId, tick, step);
                break;
            case ProcessOperationCodes.AddLocationResource:
                AddLocationResource(world, simulation.ProcessSourceCodeId, entityId, targetId, tick, step);
                break;
            case ProcessOperationCodes.HexToHealth:
                HexToHealth(world, simulation.ProcessSourceCodeId, entityId, targetId, tick, step);
                break;
            case ProcessOperationCodes.MovePhysical:
                world.MoveEntityPhysical(
                    entityId,
                    world.EntityPhysicalQ(entityId) + _stepA[step],
                    world.EntityPhysicalR(entityId) + _stepB[step],
                    tick);
                break;
            case ProcessOperationCodes.MoveSemantic:
                world.MoveEntitySemantic(
                    entityId,
                    world.EntitySemanticQ(entityId) + _stepA[step],
                    world.EntitySemanticR(entityId) + _stepB[step],
                    world.EntitySemanticS(entityId) + _stepC[step],
                    tick);
                break;
            case ProcessOperationCodes.MoveTowardTargetPhysical:
                MoveTowardTargetPhysical(world, entityId, targetId, tick);
                break;
            case ProcessOperationCodes.WanderPhysical:
                WanderPhysical(simulation, world, entityId, tick);
                break;
            case ProcessOperationCodes.MoveTowardTargetSemantic:
                MoveTowardTargetSemantic(world, entityId, targetId, tick);
                break;
            case ProcessOperationCodes.WanderSemantic:
                WanderSemantic(simulation, world, entityId, tick);
                break;
            case ProcessOperationCodes.AddResource:
                world.ModifyResource(
                    _stepA[step],
                    world.EntityPhysicalQ(entityId) + _stepB[step],
                    world.EntityPhysicalR(entityId) + _stepC[step],
                    simulation.ProcessSourceCodeId,
                    _stepX[step],
                    tick);
                break;
            case ProcessOperationCodes.SetStatus:
                world.SetEntityStatus(entityId, _stepA[step], tick);
                break;
            case ProcessOperationCodes.CreateRelationship:
                simulation.CreateRelationship(
                    _stepA[step],
                    _stepC[step],
                    entityId,
                    _stepB[step] > 0 ? _stepB[step] : targetId,
                    _stepX[step],
                    tick,
                    _stepD[step],
                    factCount: _stepD[step] > 0 ? 1 : 0);
                break;
            case ProcessOperationCodes.SetRelationshipStrength:
                SetRelationshipStrength(world, entityId, targetId, tick, step);
                break;
            case ProcessOperationCodes.CreateEntity:
                CreateEntity(simulation, entityId, tick, step);
                break;
            case ProcessOperationCodes.AddPressure:
                simulation.Pressures.ApplyDelta(_stepA[step], _stepB[step] > 0 ? _stepB[step] : entityId, _stepX[step]);
                world.AdvanceWorldVersion();
                break;
            case ProcessOperationCodes.SetEntityTag:
                world.SetEntityTag(
                    _stepA[step] > 0 ? _stepA[step] : entityId,
                    _stepB[step],
                    _stepC[step],
                    simulation.ProcessSourceCodeId,
                    tick);
                break;
            case ProcessOperationCodes.SetHistorical:
                world.SetEntityStatus(_stepA[step] > 0 ? _stepA[step] : entityId, EntityStatusCodes.Historical, tick);
                break;
            case ProcessOperationCodes.AddRelatedEntitiesAttribute:
                AddRelatedEntitiesAttribute(world, simulation.ProcessSourceCodeId, entityId, targetId, tick, step);
                break;
            case ProcessOperationCodes.CalculateVariable:
                SetVariable(
                    variableIds,
                    variableValues,
                    ref variableCount,
                    _stepA[step],
                    EvaluateMutationExpression(world, entityId, targetId, step, variableIds, variableValues, variableCount));
                break;
            case ProcessOperationCodes.AddAttributeFromVariable:
                world.ModifyAttribute(
                    entityId,
                    _stepA[step],
                    simulation.ProcessSourceCodeId,
                    VariableValue(variableIds, variableValues, variableCount, _stepB[step]) * (_stepX[step] == 0f ? 1f : _stepX[step]),
                    tick);
                break;
            case ProcessOperationCodes.AddTargetAttributeFromVariable:
                world.ModifyAttribute(
                    targetId,
                    _stepA[step],
                    simulation.ProcessSourceCodeId,
                    VariableValue(variableIds, variableValues, variableCount, _stepB[step]) * (_stepX[step] == 0f ? 1f : _stepX[step]),
                    tick);
                break;
            case ProcessOperationCodes.CreateFixedRelationship:
                simulation.CreateRelationship(
                    _stepA[step],
                    _stepD[step],
                    _stepB[step] > 0 ? _stepB[step] : entityId,
                    _stepC[step] > 0 ? _stepC[step] : targetId,
                    _stepX[step],
                    tick);
                break;
            default:
                throw new global::System.InvalidOperationException();
        }
    }

    private void TransferAttribute(
        WorldState world,
        int sourceCodeId,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var sourceId = EndpointEntityId(entityId, targetId, _stepA[step]);
        var destinationId = EndpointEntityId(entityId, targetId, _stepC[step]);
        var amount = _stepX[step];
        if (world.GetAttribute(sourceId, _stepB[step]) < amount)
        {
            return;
        }

        world.ModifyAttribute(sourceId, _stepB[step], sourceCodeId, -amount, tick);
        world.ModifyAttribute(destinationId, _stepD[step], sourceCodeId, amount, tick);
    }

    private void CreateEntity(Simulation simulation, int entityId, int tick, int step)
    {
        var world = simulation.World;
        var q = _stepD[step] == 0 ? world.EntityPhysicalQ(entityId) : (int)_stepX[step];
        var r = _stepD[step] == 0 ? world.EntityPhysicalR(entityId) : (int)_stepY[step];
        _ = simulation.CreateEntity(
            _stepA[step],
            _stepB[step],
            _stepC[step],
            q,
            r,
            world.EntitySemanticQ(entityId),
            world.EntitySemanticR(entityId),
            world.EntitySemanticS(entityId),
            tick);
    }

    private void AddRelatedEntitiesAttribute(
        WorldState world,
        int sourceCodeId,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var anchorId = _stepD[step] == 0 ? entityId : targetId;
        var cursor = _stepC[step] == RelationshipTraversalDirectionCodes.Incoming
            ? world.IncomingRelationshipsOfKind(anchorId, _stepA[step])
            : world.OutgoingRelationshipsOfKind(anchorId, _stepA[step]);
        while (world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var relatedEntityId = _stepC[step] == RelationshipTraversalDirectionCodes.Incoming
                ? world.RelationshipSourceId(relationshipId)
                : world.RelationshipTargetId(relationshipId);
            if (world.EntityIsActive(relatedEntityId) && world.HasAttribute(relatedEntityId, _stepB[step]))
            {
                world.ModifyAttribute(relatedEntityId, _stepB[step], sourceCodeId, _stepX[step], tick);
            }
        }
    }

    private void AddLocationResource(
        WorldState world,
        int sourceCodeId,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var locationEntityId = _stepB[step] == 0 ? entityId : targetId;
        world.ModifyResource(
            _stepA[step],
            world.EntityPhysicalQ(locationEntityId),
            world.EntityPhysicalR(locationEntityId),
            sourceCodeId,
            _stepX[step],
            tick);
    }

    private void HexToHealth(
        WorldState world,
        int sourceCodeId,
        int entityId,
        int targetId,
        int tick,
        int step)
    {
        var locationEntityId = _stepC[step] == 0 ? entityId : targetId;
        var resource = ResourceAtEntity(world, locationEntityId, _stepB[step]);
        if (resource <= 0f)
        {
            return;
        }

        world.ModifyAttribute(entityId, _stepA[step], sourceCodeId, resource * _stepX[step], tick);
    }

    private float EvaluateMutationExpression(
        WorldState world,
        int entityId,
        int targetId,
        int step,
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount)
    {
        return EvaluateExpression(
            world,
            entityId,
            targetId,
            _stepExpressionStarts[step],
            _stepExpressionCounts[step],
            _mutationExpressionCodes,
            _mutationExpressionA,
            _mutationExpressionX,
            defaultValue: 0f,
            variableIds,
            variableValues,
            variableCount);
    }

    private void SetRelationshipStrength(WorldState world, int entityId, int targetId, int tick, int step)
    {
        var relationshipTargetId = _stepB[step] > 0 ? _stepB[step] : targetId;
        var relationshipId = world.FindRelationship(entityId, relationshipTargetId, _stepA[step]);
        if (relationshipId == 0)
        {
            return;
        }

        world.SetRelationshipStrength(relationshipId, _stepX[step], tick);
    }

    private void MoveTowardTargetPhysical(WorldState world, int entityId, int targetId, int tick)
    {
        var currentQ = world.EntityPhysicalQ(entityId);
        var currentR = world.EntityPhysicalR(entityId);
        var targetQ = world.EntityPhysicalQ(targetId);
        var targetR = world.EntityPhysicalR(targetId);
        if (currentQ == targetQ && currentR == targetR)
        {
            return;
        }

        if (TryFindNextPhysicalPathStep(world, currentQ, currentR, targetQ, targetR, out var nextQ, out var nextR))
        {
            world.MoveEntityPhysical(entityId, nextQ, nextR, tick);
        }
    }

    private bool TryFindNextPhysicalPathStep(
        WorldState world,
        int startQ,
        int startR,
        int targetQ,
        int targetR,
        out int nextQ,
        out int nextR)
    {
        nextQ = startQ;
        nextR = startR;
        if (!world.CellIsKnown(startQ, startR) ||
            !world.CellIsKnown(targetQ, targetR) ||
            !world.CellIsPassable(targetQ, targetR))
        {
            return false;
        }

        var radius = world.MapRadius;
        var width = (radius * 2) + 1;
        if (width <= 0 || (long)width * width > _pathOpenSet.Length)
        {
            return false;
        }

        var startIndex = PhysicalPathIndex(radius, width, startQ, startR);
        var targetIndex = PhysicalPathIndex(radius, width, targetQ, targetR);
        if (startIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        global::System.Array.Clear(_pathOpen, 0, _pathOpen.Length);
        global::System.Array.Clear(_pathClosed, 0, _pathClosed.Length);
        global::System.Array.Fill(_pathCameFrom, -1);
        global::System.Array.Fill(_pathGScore, float.PositiveInfinity);
        global::System.Array.Fill(_pathFScore, float.PositiveInfinity);

        var openCount = 1;
        _pathOpenSet[0] = startIndex;
        _pathOpen[startIndex] = true;
        _pathGScore[startIndex] = 0f;
        _pathFScore[startIndex] = HexDistance(startQ, startR, targetQ, targetR);

        while (openCount > 0)
        {
            var openSlot = SelectLowestPathScore(openCount);
            var currentIndex = _pathOpenSet[openSlot];
            openCount--;
            _pathOpenSet[openSlot] = _pathOpenSet[openCount];
            _pathOpen[currentIndex] = false;
            _pathClosed[currentIndex] = true;

            if (currentIndex == targetIndex)
            {
                return TryResolveNextPhysicalPathStep(startIndex, targetIndex, radius, width, out nextQ, out nextR);
            }

            var currentQ = PhysicalPathQ(radius, width, currentIndex);
            var currentR = PhysicalPathR(radius, width, currentIndex);
            for (var direction = 0; direction < 6; direction++)
            {
                var neighborQ = currentQ + DirectionQ(direction);
                var neighborR = currentR + DirectionR(direction);
                if (!world.CellIsKnown(neighborQ, neighborR) || !world.CellIsPassable(neighborQ, neighborR))
                {
                    continue;
                }

                var neighborIndex = PhysicalPathIndex(radius, width, neighborQ, neighborR);
                if (neighborIndex < 0 || _pathClosed[neighborIndex])
                {
                    continue;
                }

                var tentativeGScore = _pathGScore[currentIndex] + 1f;
                if (tentativeGScore >= _pathGScore[neighborIndex])
                {
                    continue;
                }

                _pathCameFrom[neighborIndex] = currentIndex;
                _pathGScore[neighborIndex] = tentativeGScore;
                _pathFScore[neighborIndex] = tentativeGScore + HexDistance(neighborQ, neighborR, targetQ, targetR);

                if (!_pathOpen[neighborIndex])
                {
                    if (openCount >= _pathOpenSet.Length)
                    {
                        return false;
                    }

                    _pathOpenSet[openCount] = neighborIndex;
                    _pathOpen[neighborIndex] = true;
                    openCount++;
                }
            }
        }

        return false;
    }

    private int SelectLowestPathScore(int openCount)
    {
        var selectedSlot = 0;
        var selectedIndex = _pathOpenSet[0];
        var selectedScore = _pathFScore[selectedIndex];
        for (var slot = 1; slot < openCount; slot++)
        {
            var index = _pathOpenSet[slot];
            var score = _pathFScore[index];
            if (score < selectedScore)
            {
                selectedSlot = slot;
                selectedIndex = index;
                selectedScore = score;
            }
        }

        return selectedSlot;
    }

    private bool TryResolveNextPhysicalPathStep(
        int startIndex,
        int targetIndex,
        int radius,
        int width,
        out int nextQ,
        out int nextR)
    {
        var currentIndex = targetIndex;
        var parentIndex = _pathCameFrom[currentIndex];
        while (parentIndex != startIndex)
        {
            if (parentIndex < 0)
            {
                nextQ = 0;
                nextR = 0;
                return false;
            }

            currentIndex = parentIndex;
            parentIndex = _pathCameFrom[currentIndex];
        }

        nextQ = PhysicalPathQ(radius, width, currentIndex);
        nextR = PhysicalPathR(radius, width, currentIndex);
        return true;
    }

    private int PhysicalPathIndex(int radius, int width, int q, int r)
    {
        var adjustedQ = q + radius;
        var adjustedR = r + radius;
        if ((uint)adjustedQ >= (uint)width || (uint)adjustedR >= (uint)width)
        {
            return -1;
        }

        var index = (adjustedQ * width) + adjustedR;
        return (uint)index < (uint)_pathCameFrom.Length ? index : -1;
    }

    private static int PhysicalPathQ(int radius, int width, int index)
    {
        return (index / width) - radius;
    }

    private static int PhysicalPathR(int radius, int width, int index)
    {
        return (index % width) - radius;
    }

    private static void WanderPhysical(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var currentQ = world.EntityPhysicalQ(entityId);
        var currentR = world.EntityPhysicalR(entityId);
        var candidateCount = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            if (world.CellIsPassable(currentQ + DirectionQ(direction), currentR + DirectionR(direction)))
            {
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            return;
        }

        var selected = simulation.NextInt(candidateCount);
        var seen = 0;
        var selectedDirection = 0;
        for (var direction = 0; direction < 6; direction++)
        {
            if (!world.CellIsPassable(currentQ + DirectionQ(direction), currentR + DirectionR(direction)))
            {
                continue;
            }

            if (seen == selected)
            {
                selectedDirection = direction;
                break;
            }

            seen++;
        }

        world.MoveEntityPhysical(
            entityId,
            currentQ + DirectionQ(selectedDirection),
            currentR + DirectionR(selectedDirection),
            tick);
    }

    private static void MoveTowardTargetSemantic(WorldState world, int entityId, int targetId, int tick)
    {
        var currentQ = world.EntitySemanticQ(entityId);
        var currentR = world.EntitySemanticR(entityId);
        var targetQ = world.EntitySemanticQ(targetId);
        var targetR = world.EntitySemanticR(targetId);
        if (currentQ == targetQ && currentR == targetR)
        {
            return;
        }

        var bestQ = currentQ;
        var bestR = currentR;
        var bestDistance = HexDistance(currentQ, currentR, targetQ, targetR);
        for (var direction = 0; direction < 6; direction++)
        {
            var q = currentQ + DirectionQ(direction);
            var r = currentR + DirectionR(direction);
            var distance = HexDistance(q, r, targetQ, targetR);
            if (distance < bestDistance)
            {
                bestQ = q;
                bestR = r;
                bestDistance = distance;
            }
        }

        world.MoveEntitySemantic(entityId, bestQ, bestR, -bestQ - bestR, tick);
    }

    private static void WanderSemantic(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var direction = simulation.NextInt(6);
        var q = world.EntitySemanticQ(entityId) + DirectionQ(direction);
        var r = world.EntitySemanticR(entityId) + DirectionR(direction);
        world.MoveEntitySemantic(entityId, q, r, -q - r, tick);
    }

    private static int EndpointEntityId(int entityId, int targetId, int endpointCode)
    {
        return endpointCode == ProcessEndpointCodes.Target ? targetId : entityId;
    }

    private static float ResourceAtEntity(WorldState world, int entityId, int resourceId)
    {
        return world.GetResource(resourceId, world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId));
    }

    private static float VariableValue(
        global::System.ReadOnlySpan<int> variableIds,
        global::System.ReadOnlySpan<float> variableValues,
        int variableCount,
        int variableId)
    {
        for (var i = 0; i < variableCount; i++)
        {
            if (variableIds[i] == variableId)
            {
                return variableValues[i];
            }
        }

        return 0f;
    }

    private static void SetVariable(
        global::System.Span<int> variableIds,
        global::System.Span<float> variableValues,
        ref int variableCount,
        int variableId,
        float value)
    {
        for (var i = 0; i < variableCount; i++)
        {
            if (variableIds[i] == variableId)
            {
                variableValues[i] = value;
                return;
            }
        }

        if (variableCount >= variableIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        variableIds[variableCount] = variableId;
        variableValues[variableCount] = value;
        variableCount++;
    }

    private void RecordAction(int entityId, int processId, int tick)
    {
        if ((uint)entityId >= (uint)_actionHistoryCounts.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = _actionHistoryNextSlots[entityId];
        var index = ActionHistoryIndex(entityId, slot);
        _actionHistoryTicks[index] = tick;
        _actionHistoryProcessIds[index] = processId;
        _actionHistoryNextSlots[entityId] = (slot + 1) % ActionHistoryLength;
        if (_actionHistoryCounts[entityId] < ActionHistoryLength)
        {
            _actionHistoryCounts[entityId]++;
        }
    }

    private int CountRecentActions(int entityId, int processId, int sinceTick)
    {
        if ((uint)entityId >= (uint)_actionHistoryCounts.Length)
        {
            return 0;
        }

        var count = 0;
        var historyCount = _actionHistoryCounts[entityId];
        for (var slot = 0; slot < historyCount; slot++)
        {
            var index = ActionHistoryIndex(entityId, slot);
            if (_actionHistoryProcessIds[index] == processId && _actionHistoryTicks[index] >= sinceTick)
            {
                count++;
            }
        }

        return count;
    }

    private static int ActionHistoryIndex(int entityId, int slot)
    {
        return (entityId * ActionHistoryLength) + slot;
    }

    private static int CooldownWithJitter(Simulation simulation, int cooldown)
    {
        var jitter = 1f + ((simulation.NextFloat() * 0.5f) - 0.25f);
        var ticks = (int)(cooldown * jitter);
        return ticks < 1 ? 1 : ticks;
    }

    private static float StandardNeedUrgency(float value)
    {
        var clamped = Clamp(value, 0f, 100f);
        if (clamped >= StandardNeedComfortable)
        {
            return 0f;
        }

        if (clamped >= StandardNeedConcern)
        {
            var t = (StandardNeedComfortable - clamped) / (StandardNeedComfortable - StandardNeedConcern);
            return 0.25f * t;
        }

        if (clamped >= StandardNeedCritical)
        {
            var t = (StandardNeedConcern - clamped) / (StandardNeedConcern - StandardNeedCritical);
            return 0.25f + (0.45f * t);
        }

        var criticalT = (StandardNeedCritical - clamped) / StandardNeedCritical;
        return 0.70f + (0.30f * criticalT);
    }

    private static float BaselineUrgency(float value, float baseline)
    {
        return baseline > 0f ? baseline / (baseline + value) : 0f;
    }

    private static float RangeUrgency(float value, float minimum, float maximum, float defaultValue)
    {
        var range = maximum - minimum;
        return range > 0f ? Abs(value - defaultValue) / range : 0f;
    }

    private static float Max(float left, float right)
    {
        return left > right ? left : right;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static bool ValueInRange(float value, int hasMin, int hasMax, float min, float max)
    {
        if (hasMin != 0 && value < min)
        {
            return false;
        }

        return hasMax == 0 || value <= max;
    }

    private static int PlaneDistance(WorldState world, int entityId, int targetId, int planeCode)
    {
        return planeCode == PlaneCodes.Semantic
            ? HexDistance(
                world.EntitySemanticQ(entityId),
                world.EntitySemanticR(entityId),
                world.EntitySemanticQ(targetId),
                world.EntitySemanticR(targetId))
            : HexDistance(
                world.EntityPhysicalQ(entityId),
                world.EntityPhysicalR(entityId),
                world.EntityPhysicalQ(targetId),
                world.EntityPhysicalR(targetId));
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var aS = -aq - ar;
        var bS = -bq - br;
        return (Abs(aq - bq) + Abs(ar - br) + Abs(aS - bS)) / 2;
    }

    private static int DirectionQ(int direction)
    {
        return direction switch
        {
            0 => 1,
            1 => 1,
            2 => 0,
            3 => -1,
            4 => -1,
            _ => 0,
        };
    }

    private static int DirectionR(int direction)
    {
        return direction switch
        {
            0 => 0,
            1 => -1,
            2 => -1,
            3 => 0,
            4 => 1,
            _ => 1,
        };
    }

    private static void WriteProcessEvaluated(
        Simulation simulation,
        int tick,
        int entityId,
        int kindId,
        int processId,
        int candidateCount,
        KernelEventState state)
    {
        if (!simulation.Events.TryWrite(KernelEvent.From(new ProcessEvaluatedKernelEvent(
            tick,
            simulation.WorldVersion,
            entityId,
            kindId,
            0,
            processId,
            candidateCount,
            state))))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void WriteProcessExecuted(
        Simulation simulation,
        int tick,
        int entityId,
        int targetId,
        int kindId,
        int processId)
    {
        if (!simulation.Events.TryWrite(KernelEvent.From(new ProcessExecutedKernelEvent(
            tick,
            simulation.WorldVersion,
            entityId,
            kindId,
            processId,
            entityId,
            targetId == entityId ? 0 : targetId,
            targetId == entityId ? 1 : 2,
            KernelEventState.Succeeded))))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void Push(global::System.Span<float> stack, ref int stackCount, float value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static float Pop(global::System.Span<float> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static void PushBinary(global::System.Span<float> stack, ref int stackCount, float value)
    {
        Push(stack, ref stackCount, value);
    }

    private static void Subtract(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left - right);
    }

    private static void Divide(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, right == 0f ? 0f : left / right);
    }

    private static void Min(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left < right ? left : right);
    }

    private static void Max(global::System.Span<float> stack, ref int stackCount)
    {
        var right = Pop(stack, ref stackCount);
        var left = Pop(stack, ref stackCount);
        Push(stack, ref stackCount, left > right ? left : right);
    }

    private static void Clamp(global::System.Span<float> stack, ref int stackCount)
    {
        var max = Pop(stack, ref stackCount);
        var min = Pop(stack, ref stackCount);
        var value = Pop(stack, ref stackCount);
        if (value < min)
        {
            value = min;
        }
        else if (value > max)
        {
            value = max;
        }

        Push(stack, ref stackCount, value);
    }

    private static void PushBool(global::System.Span<bool> stack, ref int stackCount, bool value)
    {
        if (stackCount >= stack.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        stack[stackCount] = value;
        stackCount++;
    }

    private static bool PopBool(global::System.Span<bool> stack, ref int stackCount)
    {
        if (stackCount <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        stackCount--;
        return stack[stackCount];
    }

    private static float Abs(float value)
    {
        return value < 0f ? -value : value;
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private void EnsureOpenProcess(int processSlot)
    {
        EnsureProcess(processSlot);
        if (processSlot != _openProcessSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureProcess(int processSlot)
    {
        if ((uint)processSlot == 0u || processSlot > _processCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureSupportRule(int supportRuleSlot)
    {
        if ((uint)supportRuleSlot == 0u || supportRuleSlot > _supportRuleCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
