namespace Engine.Kernel;

using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime : IKernelProgramRuntime<GeneratorProgramData, GeneratorProgramState>
{
    private const int MaxResolvedReferences = 32;
    private const int MaxScratchCells = 4096;
    private readonly GeneratorProgramScratch _scratch;
    private int[] _generatorIds => Data.GeneratorIds;
    private int[] _cadences => Data.Cadences;
    private int[] _targetKindIds => Data.TargetKindIds;
    private int[] _targetAllowWilderness => Data.TargetAllowWilderness;
    private int[] _pulseWeights => Data.PulseWeights;
    private int[] _maxRuns => Data.MaxRuns;
    private int[] _runCounts => State.RunCounts;
    private bool[] _enabled => State.Enabled;
    private int[] _targetSubtypeStarts => Data.TargetSubtypeStarts;
    private int[] _targetSubtypeCounts => Data.TargetSubtypeCounts;
    private int[] _triggerStarts => Data.TriggerStarts;
    private int[] _triggerCounts => Data.TriggerCounts;
    private int[] _bindingStarts => Data.BindingStarts;
    private int[] _bindingCounts => Data.BindingCounts;
    private int[] _producedStarts => Data.ProducedStarts;
    private int[] _producedCounts => Data.ProducedCounts;
    private int[] _relationshipStarts => Data.RelationshipStarts;
    private int[] _relationshipCounts => Data.RelationshipCounts;
    private int[] _saturationRelationshipKindIds => Data.SaturationRelationshipKindIds;
    private int[] _saturationFactIds => Data.SaturationFactIds;
    private int[] _saturationDirectionCodes => Data.SaturationDirectionCodes;
    private int[] _saturationMaxExisting => Data.SaturationMaxExisting;
    private int[] _pulseStatusCodes => _scratch.PulseStatusCodes;
    private int[] _pulseCandidateCounts => _scratch.PulseCandidateCounts;
    private int[] _pulseSelectedTargetIds => _scratch.PulseSelectedTargetIds;
    private int[] _pulseFailedFireCounts => _scratch.PulseFailedFireCounts;
    private int[] _subtypeIds => Data.SubtypeIds;
    private int[] _relationshipKindFilterIds => Data.RelationshipKindFilterIds;
    private int[] _terrainFilterIds => Data.TerrainFilterIds;
    private int[] _attributeConditionAttributeIds => Data.AttributeConditionAttributeIds;
    private int[] _attributeConditionDirectionCodes => Data.AttributeConditionDirectionCodes;
    private float[] _attributeConditionThresholds => Data.AttributeConditionThresholds;
    private int[] _triggerKindIds => Data.TriggerKindIds;
    private int[] _triggerAttributeIds => Data.TriggerAttributeIds;
    private int[] _triggerDirectionCodes => Data.TriggerDirectionCodes;
    private float[] _triggerThresholds => Data.TriggerThresholds;
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
    private int[] _bindingAttributeConditionStarts => Data.BindingAttributeConditionStarts;
    private int[] _bindingAttributeConditionCounts => Data.BindingAttributeConditionCounts;
    private int[] _producedRefCodes => Data.ProducedRefCodes;
    private int[] _producedKindIds => Data.ProducedKindIds;
    private int[] _producedSubtypeIds => Data.ProducedSubtypeIds;
    private int[] _producedCreateIfMissing => Data.ProducedCreateIfMissing;
    private int[] _producedHasQuery => Data.ProducedHasQuery;
    private int[] _producedPhysicalPlacementCodes => Data.ProducedPhysicalPlacementCodes;
    private int[] _producedSemanticPlacementCodes => Data.ProducedSemanticPlacementCodes;
    private int[] _producedPlacementRefCodes => Data.ProducedPlacementRefCodes;
    private int[] _producedCultureRefCodes => Data.ProducedCultureRefCodes;
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
    private int[] _producedQueryAttributeConditionStarts => Data.ProducedQueryAttributeConditionStarts;
    private int[] _producedQueryAttributeConditionCounts => Data.ProducedQueryAttributeConditionCounts;
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
    private int[] _relationshipFactId0 => Data.RelationshipFactId0;
    private int[] _relationshipFactId1 => Data.RelationshipFactId1;
    private int[] _relationshipFactId2 => Data.RelationshipFactId2;
    private int[] _relationshipFactId3 => Data.RelationshipFactId3;
    private int[] _relationshipFactCounts => Data.RelationshipFactCounts;
    private float[] _relationshipStrengths => Data.RelationshipStrengths;
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
    private ref int _attributeConditionCount => ref Data.AttributeConditionCount;
    private ref int _triggerCount => ref Data.TriggerCount;
    private ref int _bindingCount => ref Data.BindingCount;
    private ref int _producedCount => ref Data.ProducedCount;
    private ref int _relationshipCount => ref Data.RelationshipCount;
    private ref int _openGeneratorSlot => ref Data.OpenGeneratorSlot;
    private ref int _openBindingSlot => ref Data.OpenBindingSlot;
    private ref int _openProducedSlot => ref Data.OpenProducedSlot;

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
        int cadence,
        int targetKindId,
        int pulseWeight,
        int maxRuns,
        bool targetAllowWilderness)
    {
        ValidatePositive(generatorId);
        ValidatePositive(cadence);
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
        _cadences[slot] = cadence;
        _targetKindIds[slot] = targetKindId;
        _targetAllowWilderness[slot] = targetAllowWilderness ? 1 : 0;
        _pulseWeights[slot] = pulseWeight;
        _maxRuns[slot] = maxRuns;
        _targetSubtypeStarts[slot] = _subtypeCount + 1;
        _triggerStarts[slot] = _triggerCount + 1;
        _bindingStarts[slot] = _bindingCount + 1;
        _producedStarts[slot] = _producedCount + 1;
        _relationshipStarts[slot] = _relationshipCount + 1;
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

    public void SetTargetSaturation(
        int generatorSlot,
        int relationshipKindId,
        int requiredFactId,
        int directionCode,
        int maxExisting)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(relationshipKindId);
        ValidatePositive(directionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(requiredFactId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxExisting);
        _saturationRelationshipKindIds[generatorSlot] = relationshipKindId;
        _saturationFactIds[generatorSlot] = requiredFactId;
        _saturationDirectionCodes[generatorSlot] = directionCode;
        _saturationMaxExisting[generatorSlot] = maxExisting;
    }

    public void AddTrigger(
        int generatorSlot,
        int kindId,
        int attributeId,
        int directionCode,
        float threshold)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(kindId);
        ValidatePositive(directionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(attributeId);
        if (_triggerCount + 1 >= _triggerKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _triggerCount++;
        _triggerKindIds[_triggerCount] = kindId;
        _triggerAttributeIds[_triggerCount] = attributeId;
        _triggerDirectionCodes[_triggerCount] = directionCode;
        _triggerThresholds[_triggerCount] = threshold;
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
        _bindingAttributeConditionStarts[_bindingCount] = _attributeConditionCount + 1;
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

    public void AddBindingAttributeCondition(
        int bindingSlot,
        int attributeId,
        int directionCode,
        float threshold)
    {
        EnsureOpenBinding(bindingSlot);
        AddAttributeCondition(attributeId, directionCode, threshold);
        _bindingAttributeConditionCounts[bindingSlot]++;
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
        int coverageTransformTerrainId)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(refCode);
        ValidatePositive(kindId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(subtypeId);
        ValidatePositive(queryCultureMatchCode);
        ValidatePositive(physicalPlacementCode);
        ValidatePositive(semanticPlacementCode);
        ValidatePositive(placementRefCode);
        ValidatePositive(cultureRefCode);
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
            coverageTransformTerrainId);
        _producedCounts[generatorSlot]++;
        _openProducedSlot = _producedCount;
        return _producedCount;
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
        int coverageTransformTerrainId)
    {
        _producedRefCodes[producedSlot] = refCode;
        _producedKindIds[producedSlot] = kindId;
        _producedSubtypeIds[producedSlot] = subtypeId;
        _producedCreateIfMissing[producedSlot] = createIfMissing;
        _producedHasQuery[producedSlot] = hasQuery;
        _producedPhysicalPlacementCodes[producedSlot] = physicalPlacementCode;
        _producedSemanticPlacementCodes[producedSlot] = semanticPlacementCode;
        _producedPlacementRefCodes[producedSlot] = placementRefCode;
        _producedCultureRefCodes[producedSlot] = cultureRefCode;
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
        _producedQueryAttributeConditionStarts[producedSlot] = _attributeConditionCount + 1;
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

    public void AddProducedQueryAttributeCondition(
        int producedSlot,
        int attributeId,
        int directionCode,
        float threshold)
    {
        EnsureOpenProduced(producedSlot);
        AddAttributeCondition(attributeId, directionCode, threshold);
        _producedQueryAttributeConditionCounts[producedSlot]++;
    }

    public void AddProducedCoveragePreferredTerrain(int producedSlot, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        AddTerrainFilterId(terrainId);
        _producedCoveragePreferredTerrainCounts[producedSlot]++;
    }

    public void AddProducedCoverageAllowedTerrain(int producedSlot, int terrainId)
    {
        EnsureOpenProduced(producedSlot);
        AddTerrainFilterId(terrainId);
        _producedCoverageAllowedTerrainCounts[producedSlot]++;
    }

    public void AddRelationship(
        int generatorSlot,
        int sourceRefCode,
        int destinationRefCode,
        int relationshipKindId,
        float strength,
        int replaceExistingOutgoing,
        int factId0 = 0,
        int factId1 = 0,
        int factId2 = 0,
        int factId3 = 0,
        int factCount = 0)
    {
        EnsureOpenGenerator(generatorSlot);
        ValidatePositive(sourceRefCode);
        ValidatePositive(destinationRefCode);
        ValidatePositive(relationshipKindId);
        ValidateFactCount(factCount);
        if (_relationshipCount + 1 >= _relationshipKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipCount++;
        _relationshipSourceRefCodes[_relationshipCount] = sourceRefCode;
        _relationshipDestinationRefCodes[_relationshipCount] = destinationRefCode;
        _relationshipKindIds[_relationshipCount] = relationshipKindId;
        _relationshipStrengths[_relationshipCount] = strength;
        _relationshipReplaceExistingOutgoing[_relationshipCount] = replaceExistingOutgoing;
        _relationshipFactId0[_relationshipCount] = factId0;
        _relationshipFactId1[_relationshipCount] = factId1;
        _relationshipFactId2[_relationshipCount] = factId2;
        _relationshipFactId3[_relationshipCount] = factId3;
        _relationshipFactCounts[_relationshipCount] = factCount;
        _relationshipCounts[generatorSlot]++;
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
        if (!EvaluateDueGenerators(simulation, tick))
        {
            return;
        }

        while (TryChooseEligibleGenerator(simulation, out var selectedGeneratorSlot))
        {
            if (TryFireEligibleGenerator(simulation, selectedGeneratorSlot, tick))
            {
                return;
            }
        }

        PublishDueGeneratorStats(simulation, tick);
    }

    private bool EvaluateDueGenerators(Simulation simulation, int tick)
    {
        var anyDue = false;
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            _pulseStatusCodes[slot] = GeneratorStatusCodes.NotEvaluated;
            _pulseCandidateCounts[slot] = 0;
            _pulseSelectedTargetIds[slot] = 0;
            _pulseFailedFireCounts[slot] = 0;
            if (!_enabled[slot] || tick <= 0 || tick % _cadences[slot] != 0)
            {
                continue;
            }

            anyDue = true;
            EvaluateGenerator(simulation, slot);
        }

        return anyDue;
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
        if (Fire(simulation, selectedGeneratorSlot, targetId, tick))
        {
            _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
            MarkEligibleGeneratorsNotSelected();
            PublishDueGeneratorStats(simulation, tick);
            return true;
        }

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
            if (Fire(simulation, selectedGeneratorSlot, targetId, tick))
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
                MarkEligibleGeneratorsNotSelected();
                PublishDueGeneratorStats(simulation, tick);
                return true;
            }

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

        var candidateCount = CountEligibleTargets(simulation.World, slot);
        _pulseCandidateCounts[slot] = candidateCount;
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
                totalWeight += _pulseWeights[slot];
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

            if (roll < _pulseWeights[slot])
            {
                selectedGeneratorSlot = slot;
                return true;
            }

            roll -= _pulseWeights[slot];
        }

        return false;
    }

    private int CountEligibleTargets(WorldState world, int generatorSlot)
    {
        var count = 0;
        global::System.Span<int> refCodes = _refCodes;
        global::System.Span<int> refEntityIds = _refEntityIds;
        for (var entityId = world.FirstEntityOfKind(_targetKindIds[generatorSlot]);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (!TargetPasses(world, generatorSlot, entityId, refCodes, refEntityIds, out _))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private int SelectEligibleTarget(Simulation simulation, int generatorSlot)
    {
        var selectedOrdinal = simulation.NextInt(_pulseCandidateCounts[generatorSlot]);
        var seen = 0;
        var world = simulation.World;
        global::System.Span<int> refCodes = _refCodes;
        global::System.Span<int> refEntityIds = _refEntityIds;
        for (var entityId = world.FirstEntityOfKind(_targetKindIds[generatorSlot]);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (!TargetPasses(world, generatorSlot, entityId, refCodes, refEntityIds, out _))
            {
                continue;
            }

            if (seen == selectedOrdinal)
            {
                return entityId;
            }

            seen++;
        }

        return 0;
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
            simulation.World,
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
        global::System.Span<int> refCodes = _refCodes;
        global::System.Span<int> refEntityIds = _refEntityIds;
        for (var entityId = world.FirstEntityOfKind(_targetKindIds[generatorSlot]);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (RejectedTargetContains(entityId, rejectedTargetIds, rejectedTargetCount) ||
                !TargetPasses(world, generatorSlot, entityId, refCodes, refEntityIds, out _))
            {
                continue;
            }

            if (seen == selectedOrdinal)
            {
                return entityId;
            }

            seen++;
        }

        return 0;
    }

    private int CountEligibleTargetsExcluding(
        WorldState world,
        int generatorSlot,
        int[] rejectedTargetIds,
        int rejectedTargetCount)
    {
        var count = 0;
        global::System.Span<int> refCodes = _refCodes;
        global::System.Span<int> refEntityIds = _refEntityIds;
        for (var entityId = world.FirstEntityOfKind(_targetKindIds[generatorSlot]);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (RejectedTargetContains(entityId, rejectedTargetIds, rejectedTargetCount) ||
                !TargetPasses(world, generatorSlot, entityId, refCodes, refEntityIds, out _))
            {
                continue;
            }

            count++;
        }

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

    private bool TargetPasses(
        WorldState world,
        int generatorSlot,
        int targetId,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        out int refCount)
    {
        refCount = 0;
        if (!world.EntityIsActive(targetId) ||
            world.EntityKindId(targetId) != _targetKindIds[generatorSlot] ||
            !SubtypeMatches(world.EntitySubtypeId(targetId), _targetSubtypeStarts[generatorSlot], _targetSubtypeCounts[generatorSlot]) ||
            ExceedsTargetSaturation(world, generatorSlot, targetId) ||
            !TriggersPass(world, generatorSlot, targetId))
        {
            return false;
        }

        var targetOriginQ = world.EntityPhysicalQ(targetId);
        var targetOriginR = world.EntityPhysicalR(targetId);
        if (_targetAllowWilderness[generatorSlot] != 0 &&
            !FindFreePhysicalHex(world, targetOriginQ, targetOriginR, maxRadius: 5, out targetOriginQ, out targetOriginR))
        {
            return false;
        }

        SetRef(refCodes, refEntityIds, ref refCount, GeneratorProgramConstants.TargetReferenceCode, targetId);
        return ResolveSecondaryBindings(
            world,
            generatorSlot,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            ref refCount,
            selectRandom: false,
            simulation: null);
    }

    private bool ResolveSecondaryBindings(
        WorldState world,
        int generatorSlot,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        ref int refCount,
        bool selectRandom,
        Simulation? simulation)
    {
        var first = _bindingStarts[generatorSlot];
        var end = first + _bindingCounts[generatorSlot];
        for (var binding = first; binding < end; binding++)
        {
            var entityId = SelectEntityForQuery(
                world,
                selectRandom ? simulation : null,
                _bindingKindIds[binding],
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                refCount,
                _bindingSubtypeStarts[binding],
                _bindingSubtypeCounts[binding],
                _bindingAdjacentToTarget[binding],
                _bindingCultureMatchCodes[binding],
                _bindingWithoutOutgoingStarts[binding],
                _bindingWithoutOutgoingCounts[binding],
                _bindingWithoutIncomingStarts[binding],
                _bindingWithoutIncomingCounts[binding],
                _bindingAttributeConditionStarts[binding],
                _bindingAttributeConditionCounts[binding]);
            if (entityId == 0)
            {
                return false;
            }

            SetRef(refCodes, refEntityIds, ref refCount, _bindingRefCodes[binding], entityId);
        }

        return true;
    }

}
