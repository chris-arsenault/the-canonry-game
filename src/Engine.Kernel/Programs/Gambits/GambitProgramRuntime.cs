namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GambitProgramRuntime : IKernelProgramRuntime<GambitProgramData, GambitProgramState>
{
    internal const int GambitHistoryLength = 60;
    private const int RecencyWindow = 10;
    private const float RecencyDecayFactor = 0.5f;
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
    private int[] _gambitStepStarts => Data.GambitStepStarts;
    private int[] _gambitStepCounts => Data.GambitStepCounts;
    private int[] _gambitProviderSelectorStarts => Data.GambitProviderSelectorStarts;
    private int[] _gambitProviderSelectorCounts => Data.GambitProviderSelectorCounts;
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
    private int[] _actionHistoryTicks => State.ActionHistoryTicks;
    private int[] _actionHistoryGambitIds => State.ActionHistoryGambitIds;
    private int[] _actionHistoryCounts => State.ActionHistoryCounts;
    private int[] _actionHistoryNextSlots => State.ActionHistoryNextSlots;
    private bool[] _preconditionStack => _scratch.PreconditionStack;
    private float[] _expressionStack => _scratch.ExpressionStack;
    private int[] _variableIds => _scratch.VariableIds;
    private float[] _variableValues => _scratch.VariableValues;
    private int[] _providerSelectorCodes => Data.ProviderSelectorCodes;
    private int[] _providerSelectorKindIds => Data.ProviderSelectorKindIds;
    private int[] _providerSelectorValueIds => Data.ProviderSelectorValueIds;
    private int[] _providerSelectorRelationshipKindIds => Data.ProviderSelectorRelationshipKindIds;
    private float[] _providerSelectorMinimumStrengths => Data.ProviderSelectorMinimumStrengths;
    private ref int _gambitCount => ref Data.GambitCount;
    private ref int _preconditionCount => ref Data.PreconditionCount;
    private ref int _expressionCount => ref Data.ExpressionCount;
    private ref int _stepCount => ref Data.StepCount;
    private ref int _mutationExpressionCount => ref Data.MutationExpressionCount;
    private ref int _providerSelectorCount => ref Data.ProviderSelectorCount;
    private ref int _traitModifierCount => ref Data.TraitModifierCount;
    private ref int _openGambitSlot => ref Data.OpenGambitSlot;

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
        _gambitStepStarts[slot] = _stepCount + 1;
        _gambitProviderSelectorStarts[slot] = _providerSelectorCount + 1;
        _gambitEnabled[slot] = true;
        _openGambitSlot = slot;
        return slot;
    }

    /// <summary>
    /// Chronicle-granted gambits are inert unless an open beat of a running
    /// chronicle grants them to the acting entity's role.
    /// </summary>
    public void MarkChronicleGranted(int gambitSlot)
    {
        EnsureOpenGambit(gambitSlot);
        Data.GambitChronicleGranted[gambitSlot] = 1;
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
        _preA[_preconditionCount] = preconditionCode == BehaviorPreconditionCodes.CooldownElapsed
            ? Tuning.ScaleMainTicks(a)
            : a;
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

    public bool TrySetGambitEnabledById(int gambitId, bool enabled)
    {
        ValidatePositive(gambitId);
        var found = false;
        for (var slot = 1; slot <= _gambitCount; slot++)
        {
            if (_gambitIds[slot] == gambitId)
            {
                _gambitEnabled[slot] = enabled;
                found = true;
            }
        }

        return found;
    }

    public bool TryGetGambitEnabledById(int gambitId, out bool enabled)
    {
        ValidatePositive(gambitId);
        for (var slot = 1; slot <= _gambitCount; slot++)
        {
            if (_gambitIds[slot] == gambitId)
            {
                enabled = _gambitEnabled[slot];
                return true;
            }
        }

        enabled = false;
        return false;
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
            if (GambitCanRunForEntity(simulation, gambitSlot, kindId, entityId, tick))
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
            if (!GambitCanRunForEntity(simulation, gambitSlot, kindId, entityId, tick))
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
        simulation.Chronicles.NoteExecution(entityId, selectedTargetId, gambitId, state == KernelEventState.Succeeded);
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
        var cooldown = Tuning.ScaleMainTicks(_gambitCooldownTicks[selectedGambitSlot]);
        if (cooldown > 0)
        {
            _cooldowns.Set(entityId, gambitId, tick + CooldownWithJitter(simulation, cooldown));
        }

        var entityCooldown = Tuning.ScaleMainTicks(_gambitEntityCooldownTicks[selectedGambitSlot]);
        if (entityCooldown > 0)
        {
            _entityCooldowns.Set(entityId, 1, tick + CooldownWithJitter(simulation, entityCooldown));
        }
    }

    private bool GambitCanRunForEntity(Simulation simulation, int gambitSlot, int kindId, int entityId, int tick)
    {
        return _gambitEnabled[gambitSlot] &&
               _gambitKindIds[gambitSlot] == kindId &&
               tick >= _cooldowns.GetOrDefault(entityId, _gambitIds[gambitSlot]) &&
               (Data.GambitChronicleGranted[gambitSlot] == 0 ||
                   simulation.Chronicles.GrantsGambit(entityId, _gambitIds[gambitSlot]));
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

    internal int GambitIdForSlot(int gambitSlot)
    {
        EnsureGambit(gambitSlot);
        return _gambitIds[gambitSlot];
    }

}
