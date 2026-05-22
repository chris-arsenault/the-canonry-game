namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime : IKernelProgramRuntime<GeneratorProgramData, GeneratorProgramState>
{
    private const int MaxResolvedReferences = 32;
    private const int MaxScratchCells = 4096;
    private const int SlotFillBasePulseWeightBonus = 8;
    private const int SlotFillAdditionalPulseWeightBonus = 2;
    private const int SlotFillMaxPulseWeightBonus = 16;
    private readonly GeneratorProgramScratch _scratch;
    private int[] _generatorIds => Data.GeneratorIds;
    private int[] _targetKindIds => Data.TargetKindIds;
    private int[] _pulseWeights => Data.PulseWeights;
    private int[] _maxRuns => Data.MaxRuns;
    private int[] _runCounts => State.RunCounts;
    private bool[] _enabled => State.Enabled;
    private ref int _currentPulseIndex => ref State.CurrentPulseIndex;
    private ref int _firesThisPulse => ref State.FiresThisPulse;
    private int[] _targetSubtypeStarts => Data.TargetSubtypeStarts;
    private int[] _targetSubtypeCounts => Data.TargetSubtypeCounts;
    private int[] _targetRequiredOutgoingStarts => Data.TargetRequiredOutgoingStarts;
    private int[] _targetRequiredOutgoingCounts => Data.TargetRequiredOutgoingCounts;
    private int[] _targetRequiredIncomingStarts => Data.TargetRequiredIncomingStarts;
    private int[] _targetRequiredIncomingCounts => Data.TargetRequiredIncomingCounts;
    private int[] _targetRequiredAnyStarts => Data.TargetRequiredAnyStarts;
    private int[] _targetRequiredAnyCounts => Data.TargetRequiredAnyCounts;
    private int[] _targetWithoutOutgoingStarts => Data.TargetWithoutOutgoingStarts;
    private int[] _targetWithoutOutgoingCounts => Data.TargetWithoutOutgoingCounts;
    private int[] _targetWithoutIncomingStarts => Data.TargetWithoutIncomingStarts;
    private int[] _targetWithoutIncomingCounts => Data.TargetWithoutIncomingCounts;
    private int[] _targetTerrainRequirementStarts => Data.TargetTerrainRequirementStarts;
    private int[] _targetTerrainRequirementCounts => Data.TargetTerrainRequirementCounts;
    private int[] _triggerStarts => Data.TriggerStarts;
    private int[] _triggerCounts => Data.TriggerCounts;
    private int[] _bindingStarts => Data.BindingStarts;
    private int[] _bindingCounts => Data.BindingCounts;
    private int[] _producedStarts => Data.ProducedStarts;
    private int[] _producedCounts => Data.ProducedCounts;
    private int[] _relationshipStarts => Data.RelationshipStarts;
    private int[] _relationshipCounts => Data.RelationshipCounts;
    private int[] _effectStarts => Data.EffectStarts;
    private int[] _effectCounts => Data.EffectCounts;
    private int[] _pulseStatusCodes => _scratch.PulseStatusCodes;
    private int[] _pulseCandidateCounts => _scratch.PulseCandidateCounts;
    private int[] _pulseSelectionWeights => _scratch.PulseSelectionWeights;
    private int[] _pulseSelectedTargetIds => _scratch.PulseSelectedTargetIds;
    private int[] _pulseFailedFireCounts => _scratch.PulseFailedFireCounts;
    private int[] _subtypeIds => Data.SubtypeIds;
    private int[] _relationshipKindFilterIds => Data.RelationshipKindFilterIds;
    private float[] _relationshipKindFilterMinimumStrengths => Data.RelationshipKindFilterMinimumStrengths;
    private int[] _relationshipKindFilterAllowsSlotCausalLimitMultiplier => Data.RelationshipKindFilterAllowsSlotCausalLimitMultiplier;
    private int[] _terrainFilterIds => Data.TerrainFilterIds;
    private int[] _targetTerrainRequirementTerrainIds => Data.TargetTerrainRequirementTerrainIds;
    private int[] _targetTerrainRequirementRadii => Data.TargetTerrainRequirementRadii;
    private int[] _targetTerrainRequirementMaximumExclusive => Data.TargetTerrainRequirementMaximumExclusive;
    private int[] _triggerKindIds => Data.TriggerKindIds;
    private int[] _bindingRefCodes => Data.BindingRefCodes;
    private int[] _bindingKindIds => Data.BindingKindIds;
    private int[] _bindingAdjacentToTarget => Data.BindingAdjacentToTarget;
    private int[] _bindingCultureMatchCodes => Data.BindingCultureMatchCodes;
    private int[] _bindingSubtypeStarts => Data.BindingSubtypeStarts;
    private int[] _bindingSubtypeCounts => Data.BindingSubtypeCounts;
    private int[] _bindingWithoutOutgoingStarts => Data.BindingWithoutOutgoingStarts;
    private int[] _bindingWithoutOutgoingCounts => Data.BindingWithoutOutgoingCounts;
    private int[] _bindingWithoutIncomingStarts => Data.BindingWithoutIncomingStarts;
    private int[] _bindingWithoutIncomingCounts => Data.BindingWithoutIncomingCounts;
    private int[] _producedRefCodes => Data.ProducedRefCodes;
    private int[] _producedKindIds => Data.ProducedKindIds;
    private int[] _producedSubtypeIds => Data.ProducedSubtypeIds;
    private int[] _producedCreateIfMissing => Data.ProducedCreateIfMissing;
    private int[] _producedHasQuery => Data.ProducedHasQuery;
    private int[] _producedPhysicalPlacementCodes => Data.ProducedPhysicalPlacementCodes;
    private int[] _producedSemanticPlacementCodes => Data.ProducedSemanticPlacementCodes;
    private int[] _producedPhysicalPlacementMinRadii => Data.ProducedPhysicalPlacementMinRadii;
    private int[] _producedPhysicalPlacementMaxRadii => Data.ProducedPhysicalPlacementMaxRadii;
    private int[] _producedSemanticPlacementMinRadii => Data.ProducedSemanticPlacementMinRadii;
    private int[] _producedSemanticPlacementMaxRadii => Data.ProducedSemanticPlacementMaxRadii;
    private int[] _producedPlacementPreferredTerrainStarts => Data.ProducedPlacementPreferredTerrainStarts;
    private int[] _producedPlacementPreferredTerrainCounts => Data.ProducedPlacementPreferredTerrainCounts;
    private int[] _producedPlacementAllowedTerrainStarts => Data.ProducedPlacementAllowedTerrainStarts;
    private int[] _producedPlacementAllowedTerrainCounts => Data.ProducedPlacementAllowedTerrainCounts;
    private int[] _producedPlacementAvoidedTerrainStarts => Data.ProducedPlacementAvoidedTerrainStarts;
    private int[] _producedPlacementAvoidedTerrainCounts => Data.ProducedPlacementAvoidedTerrainCounts;
    private int[] _producedPlacementFallbackTerrainIds => Data.ProducedPlacementFallbackTerrainIds;
    private int[] _producedPlacementRefCodes => Data.ProducedPlacementRefCodes;
    private int[] _producedCultureRefCodes => Data.ProducedCultureRefCodes;
    private int[] _producedCulturePlacementCodes => Data.ProducedCulturePlacementCodes;
    private float[] _producedCreateChances => Data.ProducedCreateChances;
    private int[] _producedSubtypeChoiceStarts => Data.ProducedSubtypeChoiceStarts;
    private int[] _producedSubtypeChoiceCounts => Data.ProducedSubtypeChoiceCounts;
    private int[] _producedQuerySubtypeStarts => Data.ProducedQuerySubtypeStarts;
    private int[] _producedQuerySubtypeCounts => Data.ProducedQuerySubtypeCounts;
    private int[] _producedQueryAdjacentToTarget => Data.ProducedQueryAdjacentToTarget;
    private int[] _producedQueryCultureMatchCodes => Data.ProducedQueryCultureMatchCodes;
    private int[] _producedQueryWithoutOutgoingStarts => Data.ProducedQueryWithoutOutgoingStarts;
    private int[] _producedQueryWithoutOutgoingCounts => Data.ProducedQueryWithoutOutgoingCounts;
    private int[] _producedQueryWithoutIncomingStarts => Data.ProducedQueryWithoutIncomingStarts;
    private int[] _producedQueryWithoutIncomingCounts => Data.ProducedQueryWithoutIncomingCounts;
    private int[] _producedCoverageModeCodes => Data.ProducedCoverageModeCodes;
    private int[] _producedCoverageSourceRefCodes => Data.ProducedCoverageSourceRefCodes;
    private int[] _producedCoverageDesiredCellCounts => Data.ProducedCoverageDesiredCellCounts;
    private int[] _producedCoverageMinimumCellCounts => Data.ProducedCoverageMinimumCellCounts;
    private int[] _producedCoverageMinimumSourceRemainingCellCounts => Data.ProducedCoverageMinimumSourceRemainingCellCounts;
    private int[] _producedCoverageRespectTerrainBoundaries => Data.ProducedCoverageRespectTerrainBoundaries;
    private int[] _producedCoverageTransformTerrainIds => Data.ProducedCoverageTransformTerrainIds;
    private int[] _producedCoveragePreferredTerrainStarts => Data.ProducedCoveragePreferredTerrainStarts;
    private int[] _producedCoveragePreferredTerrainCounts => Data.ProducedCoveragePreferredTerrainCounts;
    private int[] _producedCoverageAllowedTerrainStarts => Data.ProducedCoverageAllowedTerrainStarts;
    private int[] _producedCoverageAllowedTerrainCounts => Data.ProducedCoverageAllowedTerrainCounts;
    private int[] _relationshipSourceRefCodes => Data.RelationshipSourceRefCodes;
    private int[] _relationshipDestinationRefCodes => Data.RelationshipDestinationRefCodes;
    private int[] _relationshipKindIds => Data.RelationshipKindIds;
    private int[] _relationshipReplaceExistingOutgoing => Data.RelationshipReplaceExistingOutgoing;
    private float[] _relationshipStrengths => Data.RelationshipStrengths;
    private int[] _effectTypeCodes => Data.EffectTypeCodes;
    private int[] _effectRefOrKindIds => Data.EffectRefOrKindIds;
    private int[] _effectInstanceSelectionCodes => Data.EffectInstanceSelectionCodes;
    private int[] _effectOptionCodes => Data.EffectOptionCodes;
    private int[] _effectPlaneCodes => Data.EffectPlaneCodes;
    private int[] _effectRadii => Data.EffectRadii;
    private int[] _effectMaxCellCounts => Data.EffectMaxCellCounts;
    private int[] _effectPreferredTerrainStarts => Data.EffectPreferredTerrainStarts;
    private int[] _effectPreferredTerrainCounts => Data.EffectPreferredTerrainCounts;
    private int[] _effectAllowedTerrainStarts => Data.EffectAllowedTerrainStarts;
    private int[] _effectAllowedTerrainCounts => Data.EffectAllowedTerrainCounts;
    private float[] _effectValues => Data.EffectValues;
    private int[] _scratchQ => _scratch.Q;
    private int[] _scratchR => _scratch.R;
    private int[] _scratchScore => _scratch.Score;
    private int[] _scratchVisitedQ => _scratch.VisitedQ;
    private int[] _scratchVisitedR => _scratch.VisitedR;
    private int[] _scratchClaimedQ => _scratch.ClaimedQ;
    private int[] _scratchClaimedR => _scratch.ClaimedR;
    private int[] _scratchConnectivityQ => _scratch.ConnectivityQ;
    private int[] _scratchConnectivityR => _scratch.ConnectivityR;
    private int[] _scratchConnectivityQueueQ => _scratch.ConnectivityQueueQ;
    private int[] _scratchConnectivityQueueR => _scratch.ConnectivityQueueR;
    private int[] _refCodes => _scratch.RefCodes;
    private int[] _refEntityIds => _scratch.RefEntityIds;
    private int[] _createdEntityIds => _scratch.CreatedEntityIds;
    private int[] _retryRejectedTargetIds => _scratch.RetryRejectedTargetIds;
    private ref int _generatorCount => ref Data.GeneratorCount;
    private ref int _subtypeCount => ref Data.SubtypeCount;
    private ref int _relationshipKindFilterCount => ref Data.RelationshipKindFilterCount;
    private ref int _terrainFilterCount => ref Data.TerrainFilterCount;
    private ref int _targetTerrainRequirementCount => ref Data.TargetTerrainRequirementCount;
    private ref int _triggerCount => ref Data.TriggerCount;
    private ref int _bindingCount => ref Data.BindingCount;
    private ref int _producedCount => ref Data.ProducedCount;
    private ref int _relationshipCount => ref Data.RelationshipCount;
    private ref int _effectCount => ref Data.EffectCount;
    private ref int _openGeneratorSlot => ref Data.OpenGeneratorSlot;
    private ref int _openBindingSlot => ref Data.OpenBindingSlot;
    private ref int _openProducedSlot => ref Data.OpenProducedSlot;
    private ref int _openEffectSlot => ref Data.OpenEffectSlot;

    public GeneratorProgramRuntime(GeneratorProgramData data, GeneratorProgramState state)
        : this(data, state, CreateScratch(data), new KernelProgramTuningOptions())
    {
    }

    internal GeneratorProgramRuntime(
        GeneratorProgramData data,
        GeneratorProgramState state,
        GeneratorProgramScratch scratch)
        : this(data, state, scratch, new KernelProgramTuningOptions())
    {
    }

    internal GeneratorProgramRuntime(
        GeneratorProgramData data,
        GeneratorProgramState state,
        GeneratorProgramScratch scratch,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(scratch);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        if (tuning.Generators.PulseWidthTicks <= 0 || tuning.Generators.FiresPerPulse < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(tuning));
        }

        Data = data;
        State = state;
        _scratch = scratch;
        Tuning = tuning;
    }

    private static GeneratorProgramScratch CreateScratch(GeneratorProgramData data)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        return new GeneratorProgramScratch(
            data.GeneratorIds.Length - 1,
            MaxScratchCells,
            MaxResolvedReferences,
            MaxScratchCells);
    }

    public GeneratorProgramData Data { get; }

    public GeneratorProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int Count => _generatorCount;

    public int RegisterGenerator(
        int generatorId,
        int targetKindId,
        int pulseWeight,
        int maxRuns)
    {
        ValidatePositive(generatorId);
        ValidatePositive(targetKindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(pulseWeight);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxRuns);
        if (_generatorCount + 1 >= _generatorIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _generatorCount++;
        var slot = _generatorCount;
        _generatorIds[slot] = generatorId;
        _targetKindIds[slot] = targetKindId;
        _pulseWeights[slot] = pulseWeight;
        _maxRuns[slot] = maxRuns;
        _targetSubtypeStarts[slot] = _subtypeCount + 1;
        _targetRequiredOutgoingStarts[slot] = _relationshipKindFilterCount + 1;
        _targetRequiredIncomingStarts[slot] = _relationshipKindFilterCount + 1;
        _targetRequiredAnyStarts[slot] = _relationshipKindFilterCount + 1;
        _targetWithoutOutgoingStarts[slot] = _relationshipKindFilterCount + 1;
        _targetWithoutIncomingStarts[slot] = _relationshipKindFilterCount + 1;
        _targetTerrainRequirementStarts[slot] = _targetTerrainRequirementCount + 1;
        _triggerStarts[slot] = _triggerCount + 1;
        _bindingStarts[slot] = _bindingCount + 1;
        _producedStarts[slot] = _producedCount + 1;
        _relationshipStarts[slot] = _relationshipCount + 1;
        _effectStarts[slot] = _effectCount + 1;
        _enabled[slot] = true;
        _openGeneratorSlot = slot;
        return slot;
    }

    public void AddTargetSubtype(int generatorSlot, int subtypeId)
    {
        EnsureOpenGenerator(generatorSlot);
        AddSubtypeId(subtypeId);
        _targetSubtypeCounts[generatorSlot]++;
    }

    public void AddTargetRequiredOutgoingRelationship(int generatorSlot, int relationshipKindId, float minimumStrength = 0f)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetRequiredOutgoingCounts[generatorSlot] == 0)
        {
            _targetRequiredOutgoingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilter(relationshipKindId, minimumStrength);
        _targetRequiredOutgoingCounts[generatorSlot]++;
    }

    public void AddTargetRequiredIncomingRelationship(int generatorSlot, int relationshipKindId, float minimumStrength = 0f)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetRequiredIncomingCounts[generatorSlot] == 0)
        {
            _targetRequiredIncomingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilter(relationshipKindId, minimumStrength);
        _targetRequiredIncomingCounts[generatorSlot]++;
    }

    public void AddTargetRequiredRelationship(int generatorSlot, int relationshipKindId, float minimumStrength = 0f)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetRequiredAnyCounts[generatorSlot] == 0)
        {
            _targetRequiredAnyStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilter(relationshipKindId, minimumStrength);
        _targetRequiredAnyCounts[generatorSlot]++;
    }

    public void AddTargetWithoutOutgoingRelationship(int generatorSlot, int relationshipKindId)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetWithoutOutgoingCounts[generatorSlot] == 0)
        {
            _targetWithoutOutgoingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilterId(relationshipKindId, allowSlotCausalLimitMultiplier: true);
        _targetWithoutOutgoingCounts[generatorSlot]++;
    }

    public void AddTargetWithoutIncomingRelationship(int generatorSlot, int relationshipKindId)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetWithoutIncomingCounts[generatorSlot] == 0)
        {
            _targetWithoutIncomingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilterId(relationshipKindId, allowSlotCausalLimitMultiplier: true);
        _targetWithoutIncomingCounts[generatorSlot]++;
    }

    public void AddTargetTerrainCountBelow(int generatorSlot, int terrainId, int radius, int maximumExclusive)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(terrainId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(radius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumExclusive);
        if (_targetTerrainRequirementCount + 1 >= _targetTerrainRequirementTerrainIds.Length)
        {
            throw new KernelCapacityExceededException(
                KernelCapacityCodes.GeneratorTargetTerrainRequirement,
                _targetTerrainRequirementCount + 1,
                _targetTerrainRequirementTerrainIds.Length - 1);
        }

        _targetTerrainRequirementCount++;
        _targetTerrainRequirementTerrainIds[_targetTerrainRequirementCount] = terrainId;
        _targetTerrainRequirementRadii[_targetTerrainRequirementCount] = radius;
        _targetTerrainRequirementMaximumExclusive[_targetTerrainRequirementCount] = maximumExclusive;
        _targetTerrainRequirementCounts[generatorSlot]++;
    }

    public void AddTrigger(int generatorSlot, int kindId)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(kindId);
        if (_triggerCount + 1 >= _triggerKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _triggerCount++;
        _triggerKindIds[_triggerCount] = kindId;
        _triggerCounts[generatorSlot]++;
    }

    public int AddSecondaryBinding(
        int generatorSlot,
        int refCode,
        int kindId,
        int adjacentToTarget,
        int cultureMatchCode)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(refCode);
        ValidatePositive(kindId);
        ValidatePositive(cultureMatchCode);
        if (_bindingCount + 1 >= _bindingRefCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _bindingCount++;
        _bindingRefCodes[_bindingCount] = refCode;
        _bindingKindIds[_bindingCount] = kindId;
        _bindingAdjacentToTarget[_bindingCount] = adjacentToTarget;
        _bindingCultureMatchCodes[_bindingCount] = cultureMatchCode;
        _bindingSubtypeStarts[_bindingCount] = _subtypeCount + 1;
        _bindingWithoutOutgoingStarts[_bindingCount] = _relationshipKindFilterCount + 1;
        _bindingWithoutIncomingStarts[_bindingCount] = _relationshipKindFilterCount + 1;
        _bindingCounts[generatorSlot]++;
        _openBindingSlot = _bindingCount;
        return _bindingCount;
    }

    public void AddBindingSubtype(int bindingSlot, int subtypeId)
    {
        EnsureOpenBinding(bindingSlot);
        AddSubtypeId(subtypeId);
        _bindingSubtypeCounts[bindingSlot]++;
    }

    public void AddBindingWithoutOutgoingRelationship(int bindingSlot, int relationshipKindId)
    {
        EnsureOpenBinding(bindingSlot);
        AddRelationshipKindFilterId(relationshipKindId);
        _bindingWithoutOutgoingCounts[bindingSlot]++;
    }

    public void AddBindingWithoutIncomingRelationship(int bindingSlot, int relationshipKindId)
    {
        EnsureOpenBinding(bindingSlot);
        AddRelationshipKindFilterId(relationshipKindId);
        _bindingWithoutIncomingCounts[bindingSlot]++;
    }

    public int AddProduced(
        int generatorSlot,
        int refCode,
        int kindId,
        int subtypeId,
        int createIfMissing,
        int hasQuery,
        int queryAdjacentToTarget,
        int queryCultureMatchCode,
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int cultureRefCode,
        float createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        int coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId,
        int physicalPlacementMinRadius = GeneratorProgramConstants.DefaultPlacementMinRadius,
        int physicalPlacementMaxRadius = GeneratorProgramConstants.DefaultPlacementMaxRadius,
        int semanticPlacementMinRadius = GeneratorProgramConstants.DefaultPlacementMinRadius,
        int semanticPlacementMaxRadius = GeneratorProgramConstants.DefaultPlacementMaxRadius,
        int culturePlacementCode = GeneratorCulturePlacementCodes.PreferInheritedCulture)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(refCode);
        ValidatePositive(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(subtypeId);
        ValidatePositive(queryCultureMatchCode);
        ValidatePositive(physicalPlacementCode);
        ValidatePositive(semanticPlacementCode);
        ValidateProducedPlacementRadii(
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius);
        ValidatePositive(placementRefCode);
        ValidatePositive(cultureRefCode);
        ValidatePositive(culturePlacementCode);
        if (createChance < 0f || createChance > 1f)
        {
            throw new global::System.InvalidOperationException();
        }

        if (_producedCount + 1 >= _producedRefCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _producedCount++;
        StoreProducedDefinition(
            _producedCount,
            refCode,
            kindId,
            subtypeId,
            createIfMissing,
            hasQuery,
            queryAdjacentToTarget,
            queryCultureMatchCode,
            physicalPlacementCode,
            semanticPlacementCode,
            placementRefCode,
            cultureRefCode,
            createChance,
            coverageModeCode,
            coverageSourceRefCode,
            coverageDesiredCellCount,
            coverageMinimumCellCount,
            coverageMinimumSourceRemainingCellCount,
            coverageRespectTerrainBoundaries,
            coverageTransformTerrainId,
            physicalPlacementMinRadius,
            physicalPlacementMaxRadius,
            semanticPlacementMinRadius,
            semanticPlacementMaxRadius,
            culturePlacementCode);
        _producedCounts[generatorSlot]++;
        _openProducedSlot = _producedCount;
        return _producedCount;
    }

    private static void ValidateProducedPlacementRadii(
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(physicalPlacementMinRadius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(physicalPlacementMaxRadius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(semanticPlacementMinRadius);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(semanticPlacementMaxRadius);
        if (physicalPlacementMaxRadius < physicalPlacementMinRadius ||
            semanticPlacementMaxRadius < semanticPlacementMinRadius)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void StoreProducedDefinition(
        int producedSlot,
        int refCode,
        int kindId,
        int subtypeId,
        int createIfMissing,
        int hasQuery,
        int queryAdjacentToTarget,
        int queryCultureMatchCode,
        int physicalPlacementCode,
        int semanticPlacementCode,
        int placementRefCode,
        int cultureRefCode,
        float createChance,
        int coverageModeCode,
        int coverageSourceRefCode,
        int coverageDesiredCellCount,
        int coverageMinimumCellCount,
        int coverageMinimumSourceRemainingCellCount,
        int coverageRespectTerrainBoundaries,
        int coverageTransformTerrainId,
        int physicalPlacementMinRadius,
        int physicalPlacementMaxRadius,
        int semanticPlacementMinRadius,
        int semanticPlacementMaxRadius,
        int culturePlacementCode)
    {
        _producedRefCodes[producedSlot] = refCode;
        _producedKindIds[producedSlot] = kindId;
        _producedSubtypeIds[producedSlot] = subtypeId;
        _producedCreateIfMissing[producedSlot] = createIfMissing;
        _producedHasQuery[producedSlot] = hasQuery;
        _producedPhysicalPlacementCodes[producedSlot] = physicalPlacementCode;
        _producedSemanticPlacementCodes[producedSlot] = semanticPlacementCode;
        _producedPhysicalPlacementMinRadii[producedSlot] = physicalPlacementMinRadius;
        _producedPhysicalPlacementMaxRadii[producedSlot] = physicalPlacementMaxRadius;
        _producedSemanticPlacementMinRadii[producedSlot] = semanticPlacementMinRadius;
        _producedSemanticPlacementMaxRadii[producedSlot] = semanticPlacementMaxRadius;
        _producedPlacementRefCodes[producedSlot] = placementRefCode;
        _producedCultureRefCodes[producedSlot] = cultureRefCode;
        _producedCulturePlacementCodes[producedSlot] = culturePlacementCode;
        _producedCreateChances[producedSlot] = createChance;
        _producedQueryAdjacentToTarget[producedSlot] = queryAdjacentToTarget;
        _producedQueryCultureMatchCodes[producedSlot] = queryCultureMatchCode;
        _producedCoverageModeCodes[producedSlot] = coverageModeCode;
        _producedCoverageSourceRefCodes[producedSlot] = coverageSourceRefCode;
        _producedCoverageDesiredCellCounts[producedSlot] = coverageDesiredCellCount;
        _producedCoverageMinimumCellCounts[producedSlot] = coverageMinimumCellCount;
        _producedCoverageMinimumSourceRemainingCellCounts[producedSlot] = coverageMinimumSourceRemainingCellCount;
        _producedCoverageRespectTerrainBoundaries[producedSlot] = coverageRespectTerrainBoundaries;
        _producedCoverageTransformTerrainIds[producedSlot] = coverageTransformTerrainId;
        _producedSubtypeChoiceStarts[producedSlot] = _subtypeCount + 1;
        _producedQuerySubtypeStarts[producedSlot] = _subtypeCount + 1;
        _producedQueryWithoutOutgoingStarts[producedSlot] = _relationshipKindFilterCount + 1;
        _producedQueryWithoutIncomingStarts[producedSlot] = _relationshipKindFilterCount + 1;
        InitializeProducedPlacementTerrainFilters(producedSlot);
        _producedCoveragePreferredTerrainStarts[producedSlot] = _terrainFilterCount + 1;
        _producedCoverageAllowedTerrainStarts[producedSlot] = _terrainFilterCount + 1;
    }

    public void AddProducedSubtypeChoice(int producedSlot, int subtypeId)
    {
        EnsureOpenProduced(producedSlot);
        AddSubtypeId(subtypeId);
        _producedSubtypeChoiceCounts[producedSlot]++;
    }

    public void AddProducedQuerySubtype(int producedSlot, int subtypeId)
    {
        EnsureOpenProduced(producedSlot);
        AddSubtypeId(subtypeId);
        _producedQuerySubtypeCounts[producedSlot]++;
    }

    public void AddProducedQueryWithoutOutgoingRelationship(int producedSlot, int relationshipKindId)
    {
        EnsureOpenProduced(producedSlot);
        AddRelationshipKindFilterId(relationshipKindId);
        _producedQueryWithoutOutgoingCounts[producedSlot]++;
    }

    public void AddProducedQueryWithoutIncomingRelationship(int producedSlot, int relationshipKindId)
    {
        EnsureOpenProduced(producedSlot);
        AddRelationshipKindFilterId(relationshipKindId);
        _producedQueryWithoutIncomingCounts[producedSlot]++;
    }

    public void DisableGenerator(int generatorSlot)
    {
        EnsureGenerator(generatorSlot);
        _enabled[generatorSlot] = false;
    }

    public void EnableGenerator(int generatorSlot)
    {
        EnsureGenerator(generatorSlot);
        _enabled[generatorSlot] = true;
    }

    public bool TrySetGeneratorEnabledById(int generatorId, bool enabled)
    {
        ValidatePositive(generatorId);
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_generatorIds[slot] == generatorId)
            {
                _enabled[slot] = enabled;
                return true;
            }
        }

        return false;
    }

    public bool TryGetGeneratorEnabledById(int generatorId, out bool enabled)
    {
        ValidatePositive(generatorId);
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_generatorIds[slot] == generatorId)
            {
                enabled = _enabled[slot];
                return true;
            }
        }

        enabled = false;
        return false;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        ResetPulseBudgetIfNeeded(tick);
        if (!HasGeneratorFireBudget() || !EvaluateGenerators(simulation, tick))
        {
            return;
        }

        while (TryChooseEligibleGenerator(simulation, out var selectedGeneratorSlot))
        {
            if (TryFireEligibleGenerator(simulation, selectedGeneratorSlot, tick))
            {
                _firesThisPulse++;
                return;
            }
        }

        PublishGeneratorEvaluationStats(simulation, tick);
    }

    private void ResetPulseBudgetIfNeeded(int tick)
    {
        if (tick <= 0)
        {
            return;
        }

        var pulseIndex = (tick - 1) / Tuning.ScaleMainTicks(Tuning.Generators.PulseWidthTicks);
        if (_currentPulseIndex == pulseIndex)
        {
            return;
        }

        _currentPulseIndex = pulseIndex;
        _firesThisPulse = 0;
    }

    private bool HasGeneratorFireBudget() =>
        _firesThisPulse < Tuning.Generators.FiresPerPulse;

    private bool EvaluateGenerators(Simulation simulation, int tick)
    {
        var anyEvaluated = false;
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            _pulseStatusCodes[slot] = GeneratorStatusCodes.NotEvaluated;
            _pulseCandidateCounts[slot] = 0;
            _pulseSelectionWeights[slot] = 0;
            _pulseSelectedTargetIds[slot] = 0;
            _pulseFailedFireCounts[slot] = 0;
            if (!_enabled[slot] || tick <= 0)
            {
                continue;
            }

            anyEvaluated = true;
            EvaluateGenerator(simulation, slot);
        }

        return anyEvaluated;
    }

    private bool TryFireEligibleGenerator(Simulation simulation, int selectedGeneratorSlot, int tick)
    {
        return Tuning.Generators.RetryFireTargets
            ? TryFireEligibleGeneratorWithRetry(simulation, selectedGeneratorSlot, tick)
            : TryFireEligibleGeneratorOnce(simulation, selectedGeneratorSlot, tick);
    }

    private bool TryFireEligibleGeneratorOnce(Simulation simulation, int selectedGeneratorSlot, int tick)
    {
        var targetId = SelectEligibleTarget(simulation, selectedGeneratorSlot);
        if (targetId == 0)
        {
            _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
            return false;
        }

        _pulseSelectedTargetIds[selectedGeneratorSlot] = targetId;
        if (Fire(simulation, selectedGeneratorSlot, targetId, tick, out var failureReasonCode))
        {
            _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
            MarkEligibleGeneratorsNotSelected();
            PublishGeneratorEvaluationStats(simulation, tick);
            return true;
        }

        PublishGeneratorTargetEvaluation(
            simulation,
            selectedGeneratorSlot,
            targetId,
            GeneratorStatusCodes.FireFailed,
            failureReasonCode);
        _pulseFailedFireCounts[selectedGeneratorSlot]++;
        _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
        return false;
    }

    private bool TryFireEligibleGeneratorWithRetry(Simulation simulation, int selectedGeneratorSlot, int tick)
    {
        var rejectedTargetCount = 0;
        while (true)
        {
            var targetId = SelectEligibleTarget(
                simulation,
                selectedGeneratorSlot,
                _retryRejectedTargetIds,
                rejectedTargetCount);
            if (targetId == 0)
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
                return false;
            }

            _pulseSelectedTargetIds[selectedGeneratorSlot] = targetId;
            if (Fire(simulation, selectedGeneratorSlot, targetId, tick, out var failureReasonCode))
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
                MarkEligibleGeneratorsNotSelected();
                PublishGeneratorEvaluationStats(simulation, tick);
                return true;
            }

            PublishGeneratorTargetEvaluation(
                simulation,
                selectedGeneratorSlot,
                targetId,
                GeneratorStatusCodes.FireFailed,
                failureReasonCode);
            _pulseFailedFireCounts[selectedGeneratorSlot]++;
            if (rejectedTargetCount >= _retryRejectedTargetIds.Length)
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
                return false;
            }

            _retryRejectedTargetIds[rejectedTargetCount] = targetId;
            rejectedTargetCount++;
        }
    }

    private void MarkEligibleGeneratorsNotSelected()
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_pulseStatusCodes[slot] == GeneratorStatusCodes.Eligible)
            {
                _pulseStatusCodes[slot] = GeneratorStatusCodes.EligibleNotSelected;
            }
        }
    }

    private void EvaluateGenerator(Simulation simulation, int slot)
    {
        if (_maxRuns[slot] > 0 && _runCounts[slot] >= _maxRuns[slot])
        {
            _pulseStatusCodes[slot] = GeneratorStatusCodes.MaxRuns;
            return;
        }

        if (_pulseWeights[slot] == 0)
        {
            _pulseStatusCodes[slot] = GeneratorStatusCodes.ZeroWeight;
            return;
        }

        var (candidateCount, slotFillBonus) = CountEligibleTargetsAndSlotFillBonus(simulation, slot);
        _pulseCandidateCounts[slot] = candidateCount;
        _pulseSelectionWeights[slot] = candidateCount == 0 ? 0 : _pulseWeights[slot] + slotFillBonus;
        _pulseStatusCodes[slot] = candidateCount == 0
            ? GeneratorStatusCodes.Blocked
            : GeneratorStatusCodes.Eligible;
    }

    private bool TryChooseEligibleGenerator(Simulation simulation, out int selectedGeneratorSlot)
    {
        selectedGeneratorSlot = 0;
        var totalWeight = 0;
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_pulseStatusCodes[slot] == GeneratorStatusCodes.Eligible)
            {
                totalWeight += _pulseSelectionWeights[slot];
            }
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        var roll = simulation.NextInt(totalWeight);
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_pulseStatusCodes[slot] != GeneratorStatusCodes.Eligible)
            {
                continue;
            }

            if (roll < _pulseSelectionWeights[slot])
            {
                selectedGeneratorSlot = slot;
                return true;
            }

            roll -= _pulseSelectionWeights[slot];
        }

        return false;
    }

    private (int CandidateCount, int SlotFillBonus) CountEligibleTargetsAndSlotFillBonus(
        Simulation simulation,
        int generatorSlot)
    {
        var count = 0;
        var maxSlotFitCount = 0;
        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                var passes = TargetPasses(
                    simulation,
                    generatorSlot,
                    entityId,
                    refCodes,
                    refEntityIds,
                    out var refCount,
                    out var rejectionReasonCode);
                PublishGeneratorTargetEvaluation(
                    simulation,
                    generatorSlot,
                    entityId,
                    passes ? GeneratorStatusCodes.Eligible : GeneratorStatusCodes.Blocked,
                    rejectionReasonCode);
                if (passes)
                {
                    count++;
                    var slotFitCount = ProducedUnoccupiedSlotFitCount(
                        simulation,
                        generatorSlot,
                        refCodes,
                        refEntityIds,
                        refCount);
                    if (slotFitCount > maxSlotFitCount)
                    {
                        maxSlotFitCount = slotFitCount;
                    }
                }

                return true;
            });

        return (count, SlotFillPulseWeightBonus(maxSlotFitCount));
    }

    private int ProducedUnoccupiedSlotFitCount(
        Simulation simulation,
        int generatorSlot,
        int[] refCodes,
        int[] refEntityIds,
        int refCount)
    {
        var count = 0;
        var world = simulation.World;
        var firstProduced = _producedStarts[generatorSlot];
        var endProduced = firstProduced + _producedCounts[generatorSlot];
        for (var produced = firstProduced; produced < endProduced; produced++)
        {
            if (_producedCreateIfMissing[produced] == 0 || _producedCreateChances[produced] <= 0f)
            {
                continue;
            }

            var cultureSourceId = ResolveRef(refCodes, refEntityIds, refCount, _producedCultureRefCodes[produced]);
            if (cultureSourceId <= 0)
            {
                continue;
            }

            count += simulation.CultureSlots.UnoccupiedSlotFitCount(
                world.EntityCultureId(cultureSourceId),
                _producedKindIds[produced]);
        }

        return count;
    }

    private static int SlotFillPulseWeightBonus(int slotFitCount)
    {
        if (slotFitCount <= 0)
        {
            return 0;
        }

        return global::System.Math.Min(
            SlotFillMaxPulseWeightBonus,
            SlotFillBasePulseWeightBonus + ((slotFitCount - 1) * SlotFillAdditionalPulseWeightBonus));
    }

    private int SelectEligibleTarget(Simulation simulation, int generatorSlot)
    {
        var selectedOrdinal = simulation.NextInt(_pulseCandidateCounts[generatorSlot]);
        var seen = 0;
        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        var selectedTargetId = 0;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                if (!TargetPasses(simulation, generatorSlot, entityId, refCodes, refEntityIds, out _))
                {
                    return true;
                }

                if (seen == selectedOrdinal)
                {
                    selectedTargetId = entityId;
                    return false;
                }

                seen++;
                return true;
            });

        return selectedTargetId;
    }

    private int SelectEligibleTarget(
        Simulation simulation,
        int generatorSlot,
        int[] rejectedTargetIds,
        int rejectedTargetCount)
    {
        if (rejectedTargetCount == 0)
        {
            return SelectEligibleTarget(simulation, generatorSlot);
        }

        var candidateCount = CountEligibleTargetsExcluding(
            simulation,
            generatorSlot,
            rejectedTargetIds,
            rejectedTargetCount);
        if (candidateCount == 0)
        {
            return 0;
        }

        var selectedOrdinal = simulation.NextInt(candidateCount);
        var seen = 0;
        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        var selectedTargetId = 0;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                if (RejectedTargetContains(entityId, rejectedTargetIds, rejectedTargetCount) ||
                    !TargetPasses(simulation, generatorSlot, entityId, refCodes, refEntityIds, out _))
                {
                    return true;
                }

                if (seen == selectedOrdinal)
                {
                    selectedTargetId = entityId;
                    return false;
                }

                seen++;
                return true;
            });

        return selectedTargetId;
    }

    private int CountEligibleTargetsExcluding(
        Simulation simulation,
        int generatorSlot,
        int[] rejectedTargetIds,
        int rejectedTargetCount)
    {
        var count = 0;
        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                if (!RejectedTargetContains(entityId, rejectedTargetIds, rejectedTargetCount) &&
                    TargetPasses(simulation, generatorSlot, entityId, refCodes, refEntityIds, out _))
                {
                    count++;
                }

                return true;
            });

        return count;
    }

    private static bool RejectedTargetContains(int entityId, int[] rejectedTargetIds, int rejectedTargetCount)
    {
        for (var i = 0; i < rejectedTargetCount; i++)
        {
            if (rejectedTargetIds[i] == entityId)
            {
                return true;
            }
        }

        return false;
    }

}
