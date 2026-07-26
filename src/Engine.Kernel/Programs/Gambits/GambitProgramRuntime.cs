namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class GambitProgramRuntime : IKernelProgramRuntime<GambitProgramData, GambitProgramState>
{
    private readonly GambitProgramScratch _scratch;
    private int[] _gambitKindIds => Data.GambitKindIds;
    private int[] _gambitIds => Data.GambitIds;
    private int[] _gambitActorKindIds => Data.GambitActorKindIds;
    private int[] _gambitFirstSlotsByActorKind => Data.GambitFirstSlotsByActorKind;
    private int[] _gambitLastSlotsByActorKind => Data.GambitLastSlotsByActorKind;
    private int[] _nextGambitSlotsByActorKind => Data.NextGambitSlotsByActorKind;

    /// <summary>Age charges: neutralize, then apply per open age each world-beat run.</summary>
    public void ResetAgeScoreMultipliers()
    {
        for (var slot = 1; slot <= Data.GambitCount; slot++)
        {
            Data.AgeScoreMultipliers[slot] = 1;
        }
    }

    public void ApplyAgeScoreMultiplier(int gambitId, FixedScalar multiplier)
    {
        for (var slot = 1; slot <= Data.GambitCount; slot++)
        {
            if (gambitId == 0 || _gambitIds[slot] == gambitId)
            {
                Data.AgeScoreMultipliers[slot] *= multiplier;
            }
        }
    }

    public void ResetAgeSuccessMultipliers()
    {
        for (var slot = 1; slot <= Data.GambitCount; slot++)
        {
            Data.AgeSuccessMultipliers[slot] = 1;
        }
    }

    public void ApplyAgeSuccessMultiplier(int gambitId, FixedScalar multiplier)
    {
        for (var slot = 1; slot <= Data.GambitCount; slot++)
        {
            if (gambitId == 0 || _gambitIds[slot] == gambitId)
            {
                Data.AgeSuccessMultipliers[slot] *= multiplier;
            }
        }
    }
    private int[] _gambitCooldownTicks => Data.GambitCooldownTicks;
    private int[] _gambitEntityCooldownTicks => Data.GambitEntityCooldownTicks;
    private FixedScalar[] _gambitSuccessChances => Data.GambitSuccessChances;
    private int[] _gambitTargetKindIds => Data.GambitTargetKindIds;
    private int[] _gambitTargetModes => Data.GambitTargetModes;
    private int[] _gambitPreconditionStarts => Data.GambitPreconditionStarts;
    private int[] _gambitPreconditionCounts => Data.GambitPreconditionCounts;
    private int[] _gambitPreconditionEntries => Data.GambitPreconditionEntries;
    private int[] _gambitActorPreconditionStarts => Data.GambitActorPreconditionStarts;
    private int[] _gambitActorPreconditionCounts => Data.GambitActorPreconditionCounts;
    private int[] _gambitActorPreconditionEntries => Data.GambitActorPreconditionEntries;
    private int[] _gambitActorValueCounts => Data.GambitActorValueCounts;
    private int[] _gambitRelationshipTargetDirectionCodes => Data.GambitRelationshipTargetDirectionCodes;
    private int[] _gambitRelationshipTargetKindIds => Data.GambitRelationshipTargetKindIds;
    private int[] _gambitRelationshipTargetHasMinimumStrength => Data.GambitRelationshipTargetHasMinimumStrength;
    private FixedScalar[] _gambitRelationshipTargetMinimumStrengths => Data.GambitRelationshipTargetMinimumStrengths;
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
    private FixedScalar[] _preX => Data.PreX;
    private FixedScalar[] _preY => Data.PreY;
    private int[] _preActorValueSlots => Data.PreActorValueSlots;
    private int[] _preRelationshipTargetSources => Data.PreRelationshipTargetSources;
    private int[] _preTrueDestinations => Data.PreTrueDestinations;
    private int[] _preFalseDestinations => Data.PreFalseDestinations;
    private int[] _actorValuePreconditionIndexes => Data.ActorValuePreconditionIndexes;
    private int[] _actorPreValueSlots => Data.ActorPreValueSlots;
    private int[] _actorPreTrueDestinations => Data.ActorPreTrueDestinations;
    private int[] _actorPreFalseDestinations => Data.ActorPreFalseDestinations;
    private int[] _expressionCodes => Data.ExpressionCodes;
    private int[] _expressionA => Data.ExpressionA;
    private FixedScalar[] _expressionX => Data.ExpressionX;
    private int[] _stepOperationCodes => Data.StepOperationCodes;
    private int[] _stepA => Data.StepA;
    private int[] _stepB => Data.StepB;
    private int[] _stepC => Data.StepC;
    private int[] _stepD => Data.StepD;
    private FixedScalar[] _stepX => Data.StepX;
    private FixedScalar[] _stepY => Data.StepY;
    private int[] _stepExpressionStarts => Data.StepExpressionStarts;
    private int[] _stepExpressionCounts => Data.StepExpressionCounts;
    private int[] _mutationExpressionCodes => Data.MutationExpressionCodes;
    private int[] _mutationExpressionA => Data.MutationExpressionA;
    private FixedScalar[] _mutationExpressionX => Data.MutationExpressionX;
    private int[] _cooldownExpiresAtTicks => State.CooldownExpiresAtTicks;
    private int[] _entityCooldownExpiresAtTicks => State.EntityCooldownExpiresAtTicks;
    private FixedScalar[] _expressionStack => _scratch.ExpressionStack;
    private int[] _variableIds => _scratch.VariableIds;
    private FixedScalar[] _variableValues => _scratch.VariableValues;
    private int[] _providerSelectorCodes => Data.ProviderSelectorCodes;
    private int[] _providerSelectorKindIds => Data.ProviderSelectorKindIds;
    private int[] _providerSelectorValueIds => Data.ProviderSelectorValueIds;
    private int[] _providerSelectorRelationshipKindIds => Data.ProviderSelectorRelationshipKindIds;
    private FixedScalar[] _providerSelectorMinimumStrengths => Data.ProviderSelectorMinimumStrengths;
    private ref int _gambitCount => ref Data.GambitCount;
    private ref int _preconditionCount => ref Data.PreconditionCount;
    private ref int _actorPreconditionCount => ref Data.ActorPreconditionCount;
    private ref int _expressionCount => ref Data.ExpressionCount;
    private ref int _stepCount => ref Data.StepCount;
    private ref int _mutationExpressionCount => ref Data.MutationExpressionCount;
    private ref int _providerSelectorCount => ref Data.ProviderSelectorCount;
    private ref int _traitModifierCount => ref Data.TraitModifierCount;
    private ref int _openGambitSlot => ref Data.OpenGambitSlot;
    private ref int _gambitActorKindCount => ref Data.GambitActorKindCount;

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
        FixedScalar? successChance = null)
    {
        var compiledSuccessChance = successChance ?? FixedScalar.One;
        ValidatePositive(kindId);
        ValidatePositive(gambitId);
        ValidatePositive(targetMode);
        NumericGuard.ThrowIfNegative(cooldownTicks);
        NumericGuard.ThrowIfNegative(entityCooldownTicks);
        if (compiledSuccessChance < 0 || compiledSuccessChance > 1)
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
        _gambitSuccessChances[slot] = compiledSuccessChance;
        _gambitTargetModes[slot] = targetMode;
        _gambitTargetKindIds[slot] = targetKindId;
        AddGambitToActorKindBucket(slot, kindId);
        _gambitPreconditionStarts[slot] = _preconditionCount + 1;
        _gambitActorPreconditionStarts[slot] = _actorPreconditionCount + 1;
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
        FixedScalar scoreDelta,
        FixedScalar successChanceDelta)
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

    public void AddCandidatePrecondition(
        int gambitSlot,
        int preconditionCode,
        int a,
        int b,
        int c,
        int d,
        FixedScalar x,
        FixedScalar y,
        int actorValueSlot,
        int relationshipTargetSource,
        int trueDestination,
        int falseDestination)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(preconditionCode);
        NumericGuard.ThrowIfNegative(actorValueSlot);
        if (relationshipTargetSource is not 0 and not 1)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(relationshipTargetSource));
        }
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
        _preActorValueSlots[_preconditionCount] = actorValueSlot;
        _preRelationshipTargetSources[_preconditionCount] = relationshipTargetSource;
        _preTrueDestinations[_preconditionCount] = trueDestination;
        _preFalseDestinations[_preconditionCount] = falseDestination;
        RecordActorValuePrecondition(gambitSlot, actorValueSlot);
        _gambitPreconditionCounts[gambitSlot]++;
    }

    private void RecordActorValuePrecondition(int gambitSlot, int actorValueSlot)
    {
        if (actorValueSlot == 0)
        {
            return;
        }

        var valueIndex = _gambitPreconditionStarts[gambitSlot] + actorValueSlot - 1;
        if (valueIndex >= _actorValuePreconditionIndexes.Length ||
            _actorValuePreconditionIndexes[valueIndex] != 0)
        {
            throw new global::System.InvalidOperationException();
        }

        _actorValuePreconditionIndexes[valueIndex] = _preconditionCount;
    }

    public void AddActorPrecondition(
        int gambitSlot,
        int actorValueSlot,
        int trueDestination,
        int falseDestination)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(actorValueSlot);
        if (_actorPreconditionCount + 1 >= _actorPreValueSlots.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _actorPreconditionCount++;
        _actorPreValueSlots[_actorPreconditionCount] = actorValueSlot;
        _actorPreTrueDestinations[_actorPreconditionCount] = trueDestination;
        _actorPreFalseDestinations[_actorPreconditionCount] = falseDestination;
        _gambitActorPreconditionCounts[gambitSlot]++;
    }

    public void ConfigurePreconditionPlan(
        int gambitSlot,
        int candidateEntry,
        int actorEntry,
        int actorValueCount,
        int relationshipTargetDirectionCode,
        int relationshipTargetKindId,
        int relationshipTargetHasMinimumStrength,
        FixedScalar relationshipTargetMinimumStrength)
    {
        EnsureOpenGambit(gambitSlot);
        NumericGuard.ThrowIfNegative(actorValueCount);
        ValidatePlanDestination(candidateEntry, _gambitPreconditionCounts[gambitSlot]);
        ValidatePlanDestination(actorEntry, _gambitActorPreconditionCounts[gambitSlot]);
        ValidateRelationshipTargetSource(
            relationshipTargetDirectionCode,
            relationshipTargetKindId,
            relationshipTargetHasMinimumStrength,
            relationshipTargetMinimumStrength);
        ValidatePreconditionPlanInstructions(gambitSlot, actorValueCount);

        _gambitPreconditionEntries[gambitSlot] = candidateEntry;
        _gambitActorPreconditionEntries[gambitSlot] = actorEntry;
        _gambitActorValueCounts[gambitSlot] = actorValueCount;
        _gambitRelationshipTargetDirectionCodes[gambitSlot] = relationshipTargetDirectionCode;
        _gambitRelationshipTargetKindIds[gambitSlot] = relationshipTargetKindId;
        _gambitRelationshipTargetHasMinimumStrength[gambitSlot] = relationshipTargetHasMinimumStrength;
        _gambitRelationshipTargetMinimumStrengths[gambitSlot] = relationshipTargetMinimumStrength;
    }

    private void ValidatePreconditionPlanInstructions(int gambitSlot, int actorValueCount)
    {
        if (actorValueCount >= _scratch.ActorPreconditionValues.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var candidateCount = _gambitPreconditionCounts[gambitSlot];
        var candidateStart = _gambitPreconditionStarts[gambitSlot];
        for (var local = 1; local <= candidateCount; local++)
        {
            var precondition = candidateStart + local - 1;
            var actorValueSlot = _preActorValueSlots[precondition];
            if (actorValueSlot < 0 || actorValueSlot > actorValueCount)
            {
                throw new global::System.InvalidOperationException();
            }

            ValidatePlanDestination(_preTrueDestinations[precondition], candidateCount);
            ValidatePlanDestination(_preFalseDestinations[precondition], candidateCount);
        }

        ValidateActorValuePreconditions(
            candidateStart,
            candidateCount,
            actorValueCount);

        var actorCount = _gambitActorPreconditionCounts[gambitSlot];
        var actorStart = _gambitActorPreconditionStarts[gambitSlot];
        for (var local = 1; local <= actorCount; local++)
        {
            var precondition = actorStart + local - 1;
            var actorValueSlot = _actorPreValueSlots[precondition];
            if (actorValueSlot <= 0 || actorValueSlot > actorValueCount)
            {
                throw new global::System.InvalidOperationException();
            }

            ValidatePlanDestination(_actorPreTrueDestinations[precondition], actorCount);
            ValidatePlanDestination(_actorPreFalseDestinations[precondition], actorCount);
        }
    }

    private void ValidateActorValuePreconditions(
        int candidateStart,
        int candidateCount,
        int actorValueCount)
    {
        var candidateEnd = candidateStart + candidateCount;
        for (var actorValueSlot = 1; actorValueSlot <= actorValueCount; actorValueSlot++)
        {
            var precondition = _actorValuePreconditionIndexes[
                candidateStart + actorValueSlot - 1];
            if (precondition < candidateStart ||
                precondition >= candidateEnd ||
                _preActorValueSlots[precondition] != actorValueSlot)
            {
                throw new global::System.InvalidOperationException();
            }
        }
    }

    private static void ValidatePlanDestination(int destination, int instructionCount)
    {
        if (destination is GambitPreconditionDestinationCodes.Failed or
            GambitPreconditionDestinationCodes.Passed)
        {
            return;
        }

        if (destination <= 0 || destination > instructionCount)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(destination));
        }
    }

    private static void ValidateRelationshipTargetSource(
        int directionCode,
        int relationshipKindId,
        int hasMinimumStrength,
        FixedScalar minimumStrength)
    {
        if (directionCode == 0)
        {
            if (!EmptyRelationshipTargetSourceIsValid(
                    relationshipKindId,
                    hasMinimumStrength,
                    minimumStrength))
            {
                throw new global::System.ArgumentOutOfRangeException(nameof(directionCode));
            }

            return;
        }

        if (!RelationshipTargetDirectionIsValid(directionCode) ||
            relationshipKindId <= 0 ||
            hasMinimumStrength is not 0 and not 1 ||
            minimumStrength < 0 ||
            minimumStrength > 1)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(directionCode));
        }
    }

    private static bool EmptyRelationshipTargetSourceIsValid(
        int relationshipKindId,
        int hasMinimumStrength,
        FixedScalar minimumStrength) =>
        relationshipKindId == 0 &&
        hasMinimumStrength == 0 &&
        minimumStrength == 0;

    private static bool RelationshipTargetDirectionIsValid(int directionCode) =>
        directionCode is GambitRelationshipTargetDirectionCodes.Outgoing or
            GambitRelationshipTargetDirectionCodes.Incoming;

    public void AddExpressionOp(int gambitSlot, int expressionCode, int a = 0, FixedScalar x = default)
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
        FixedScalar minimumStrength = default)
    {
        EnsureOpenGambit(gambitSlot);
        ValidatePositive(selectorCode);
        NumericGuard.ThrowIfNegative(kindId);
        NumericGuard.ThrowIfNegative(valueId);
        NumericGuard.ThrowIfNegative(relationshipKindId);
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
        FixedScalar x = default,
        FixedScalar y = default)
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

    public void AddStepExpressionOp(int stepSlot, int expressionCode, int a = 0, FixedScalar x = default)
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
        var world = simulation.World;
        if (world.EntityCreatedAt(entityId) >= tick ||
            tick < _entityCooldownExpiresAtTicks[entityId])
        {
            return;
        }

        var kindId = world.EntityKindId(entityId);
        var candidateCount = 0;
        var totalWeight = EvaluateGambitWeights(simulation, entityId, kindId, tick, ref candidateCount);
        if (candidateCount == 0)
        {
            return;
        }

        if (totalWeight <= 0)
        {
            WriteGambitEvaluated(
                simulation,
                tick,
                simulation.World.EntityOccurrenceHandle(entityId),
                kindId,
                0,
                candidateCount,
                KernelEventState.None);
            return;
        }

        var roll = simulation.RandomScalar(
            world.EntityOccurrenceHandle(entityId),
            kindId,
            KernelRandomPurposeCodes.GambitSelection,
            attempt: tick) * totalWeight;
        var selectedGambitSlot = SelectGambitCandidate(roll, out var selectedTargetId);
        if (selectedGambitSlot == 0)
        {
            return;
        }

        RunSelectedGambit(simulation, entityId, selectedTargetId, selectedGambitSlot, kindId, tick, candidateCount);
    }

    private FixedScalar EvaluateGambitWeights(
        Simulation simulation,
        int entityId,
        int kindId,
        int tick,
        ref int candidateCount)
    {
        _scratch.ResetCandidates();
        var totalWeight = FixedScalar.Zero;
        for (var gambitSlot = FirstGambitSlotForActorKind(kindId);
             gambitSlot > 0;
             gambitSlot = _nextGambitSlotsByActorKind[gambitSlot])
        {
            if (GambitCanRunForEntity(simulation, gambitSlot, kindId, entityId, tick))
            {
                totalWeight += EvaluateTargets(
                    simulation,
                    gambitSlot,
                    entityId,
                    tick,
                    ref candidateCount);
            }
        }

        return totalWeight;
    }

    private int SelectGambitCandidate(
        FixedScalar roll,
        out int selectedTargetId)
    {
        var cumulative = FixedScalar.Zero;
        selectedTargetId = 0;
        for (var i = 0; i < _scratch.CandidateCount; i++)
        {
            cumulative += _scratch.CandidateWeights[i];
            if (roll > cumulative)
            {
                continue;
            }

            selectedTargetId = _scratch.CandidateTargetIds[i];
            return _scratch.CandidateGambitSlots[i];
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
        var entityOccurrenceHandle = simulation.World.EntityOccurrenceHandle(entityId);
        var targetOccurrenceHandle = simulation.World.EntityOccurrenceHandle(selectedTargetId);
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

        if (simulation.World.EntityIsActive(entityId))
        {
            ApplyGambitCooldowns(
                simulation,
                entityId,
                entityOccurrenceHandle,
                selectedGambitSlot,
                tick);
        }
        WriteGambitEvaluated(
            simulation,
            tick,
            entityOccurrenceHandle,
            kindId,
            gambitId,
            candidateCount,
            KernelEventState.Succeeded);
        WriteGambitExecuted(
            simulation,
            tick,
            entityOccurrenceHandle,
            targetOccurrenceHandle,
            kindId,
            gambitId,
            state);
        simulation.Chronicles.NoteExecution(
            entityOccurrenceHandle,
            targetOccurrenceHandle,
            gambitId,
            state == KernelEventState.Succeeded);
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
        int entityOccurrenceHandle,
        int selectedGambitSlot,
        int tick)
    {
        var cooldown = _gambitCooldownTicks[selectedGambitSlot];
        if (cooldown > 0)
        {
            _cooldownExpiresAtTicks[CooldownIndex(entityId, selectedGambitSlot)] =
                tick + CooldownWithJitter(
                    simulation,
                    entityOccurrenceHandle,
                    _gambitIds[selectedGambitSlot],
                    cooldown,
                    attempt: 0);
        }

        var entityCooldown = _gambitEntityCooldownTicks[selectedGambitSlot];
        if (entityCooldown > 0)
        {
            _entityCooldownExpiresAtTicks[entityId] =
                tick + CooldownWithJitter(
                    simulation,
                    entityOccurrenceHandle,
                    _gambitIds[selectedGambitSlot],
                    entityCooldown,
                    attempt: 1);
        }
    }

    private bool GambitCanRunForEntity(Simulation simulation, int gambitSlot, int kindId, int entityId, int tick)
    {
        return _gambitEnabled[gambitSlot] &&
               _gambitKindIds[gambitSlot] == kindId &&
               tick >= _cooldownExpiresAtTicks[CooldownIndex(entityId, gambitSlot)] &&
               (Data.GambitChronicleGranted[gambitSlot] == 0 ||
                   simulation.Chronicles.GrantsGambit(entityId, _gambitIds[gambitSlot]));
    }

    private void AddGambitToActorKindBucket(int gambitSlot, int kindId)
    {
        var bucket = FindActorKindBucket(kindId);
        if (bucket == 0)
        {
            _gambitActorKindCount++;
            bucket = _gambitActorKindCount;
            _gambitActorKindIds[bucket] = kindId;
            _gambitFirstSlotsByActorKind[bucket] = gambitSlot;
            _gambitLastSlotsByActorKind[bucket] = gambitSlot;
            return;
        }

        _nextGambitSlotsByActorKind[_gambitLastSlotsByActorKind[bucket]] = gambitSlot;
        _gambitLastSlotsByActorKind[bucket] = gambitSlot;
    }

    private int FirstGambitSlotForActorKind(int kindId)
    {
        var bucket = FindActorKindBucket(kindId);
        return bucket == 0 ? 0 : _gambitFirstSlotsByActorKind[bucket];
    }

    private int FindActorKindBucket(int kindId)
    {
        for (var bucket = 1; bucket <= _gambitActorKindCount; bucket++)
        {
            if (_gambitActorKindIds[bucket] == kindId)
            {
                return bucket;
            }
        }

        return 0;
    }

    internal void NoteEntityRetired(int entityId)
    {
        if ((uint)entityId >= (uint)_entityCooldownExpiresAtTicks.Length)
        {
            return;
        }

        _entityCooldownExpiresAtTicks[entityId] = 0;
        global::System.Array.Clear(
            _cooldownExpiresAtTicks,
            entityId * State.CooldownStride,
            State.CooldownStride);
    }

    private int CooldownIndex(int entityId, int gambitSlot) =>
        (entityId * State.CooldownStride) + gambitSlot;

    private KernelEventState RollGambitState(Simulation simulation, int gambitSlot, int entityId, int targetId)
    {
        var successChance = Clamp01(
            (_gambitSuccessChances[gambitSlot] *
                Data.AgeSuccessMultipliers[gambitSlot] *
                simulation.Knowledge.Modifier(
                    simulation.World.EntityCultureId(entityId),
                    KnowledgeGrantDomainCodes.GambitSuccess,
                    _gambitIds[gambitSlot])) +
            GambitSuccessDecisionDelta(simulation.World, _gambitIds[gambitSlot], entityId, targetId) +
            simulation.Chronicles.SuccessBonus(entityId, targetId, _gambitIds[gambitSlot]));
        return successChance >= 1 || simulation.RandomScalar(
                simulation.World.EntityOccurrenceHandle(entityId),
                _gambitIds[gambitSlot],
                KernelRandomPurposeCodes.GambitSuccess,
                attempt: simulation.World.EntityOccurrenceHandleOrZero(targetId)) <= successChance
            ? KernelEventState.Succeeded
            : KernelEventState.Failed;
    }

    private FixedScalar GambitScoreDecisionDelta(WorldState world, int gambitId, int entityId, int targetId) =>
        DecisionModifierEvaluation.SumGambitValue(
            world,
            _traitModifierCount,
            Data.ReadDecisionModifier,
            gambitId,
            entityId,
            targetId,
            useValue1: false);

    private FixedScalar GambitSuccessDecisionDelta(WorldState world, int gambitId, int entityId, int targetId) =>
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
