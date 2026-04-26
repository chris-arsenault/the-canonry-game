namespace Engine.Kernel;

using Engine.Eventing;

public sealed class GeneratorProgramRuntime : ISimulationSystem
{
    public const int TargetReferenceCode = 1;

    private const int MaxResolvedReferences = 32;
    private const int MaxScratchCells = 4096;
    private readonly int[] _generatorIds;
    private readonly int[] _cadences;
    private readonly int[] _targetKindIds;
    private readonly int[] _targetAllowWilderness;
    private readonly int[] _pulseWeights;
    private readonly int[] _maxRuns;
    private readonly int[] _runCounts;
    private readonly bool[] _enabled;
    private readonly int[] _targetSubtypeStarts;
    private readonly int[] _targetSubtypeCounts;
    private readonly int[] _triggerStarts;
    private readonly int[] _triggerCounts;
    private readonly int[] _bindingStarts;
    private readonly int[] _bindingCounts;
    private readonly int[] _producedStarts;
    private readonly int[] _producedCounts;
    private readonly int[] _relationshipStarts;
    private readonly int[] _relationshipCounts;
    private readonly int[] _saturationRelationshipKindIds;
    private readonly int[] _saturationFactIds;
    private readonly int[] _saturationDirectionCodes;
    private readonly int[] _saturationMaxExisting;
    private readonly int[] _pulseStatusCodes;
    private readonly int[] _pulseCandidateCounts;
    private readonly int[] _pulseSelectedTargetIds;
    private readonly int[] _pulseFailedFireCounts;

    private readonly int[] _subtypeIds;
    private readonly int[] _relationshipKindFilterIds;
    private readonly int[] _terrainFilterIds;
    private readonly int[] _attributeConditionAttributeIds;
    private readonly int[] _attributeConditionDirectionCodes;
    private readonly float[] _attributeConditionThresholds;
    private readonly int[] _triggerKindIds;
    private readonly int[] _triggerAttributeIds;
    private readonly int[] _triggerDirectionCodes;
    private readonly float[] _triggerThresholds;

    private readonly int[] _bindingRefCodes;
    private readonly int[] _bindingKindIds;
    private readonly int[] _bindingAdjacentToTarget;
    private readonly int[] _bindingCultureMatchCodes;
    private readonly int[] _bindingSubtypeStarts;
    private readonly int[] _bindingSubtypeCounts;
    private readonly int[] _bindingWithoutOutgoingStarts;
    private readonly int[] _bindingWithoutOutgoingCounts;
    private readonly int[] _bindingWithoutIncomingStarts;
    private readonly int[] _bindingWithoutIncomingCounts;
    private readonly int[] _bindingAttributeConditionStarts;
    private readonly int[] _bindingAttributeConditionCounts;

    private readonly int[] _producedRefCodes;
    private readonly int[] _producedKindIds;
    private readonly int[] _producedSubtypeIds;
    private readonly int[] _producedCreateIfMissing;
    private readonly int[] _producedHasQuery;
    private readonly int[] _producedPhysicalPlacementCodes;
    private readonly int[] _producedSemanticPlacementCodes;
    private readonly int[] _producedPlacementRefCodes;
    private readonly int[] _producedCultureRefCodes;
    private readonly float[] _producedCreateChances;
    private readonly int[] _producedSubtypeChoiceStarts;
    private readonly int[] _producedSubtypeChoiceCounts;
    private readonly int[] _producedQuerySubtypeStarts;
    private readonly int[] _producedQuerySubtypeCounts;
    private readonly int[] _producedQueryAdjacentToTarget;
    private readonly int[] _producedQueryCultureMatchCodes;
    private readonly int[] _producedQueryWithoutOutgoingStarts;
    private readonly int[] _producedQueryWithoutOutgoingCounts;
    private readonly int[] _producedQueryWithoutIncomingStarts;
    private readonly int[] _producedQueryWithoutIncomingCounts;
    private readonly int[] _producedQueryAttributeConditionStarts;
    private readonly int[] _producedQueryAttributeConditionCounts;
    private readonly int[] _producedCoverageModeCodes;
    private readonly int[] _producedCoverageSourceRefCodes;
    private readonly int[] _producedCoverageDesiredCellCounts;
    private readonly int[] _producedCoverageMinimumCellCounts;
    private readonly int[] _producedCoverageMinimumSourceRemainingCellCounts;
    private readonly int[] _producedCoverageRespectTerrainBoundaries;
    private readonly int[] _producedCoverageTransformTerrainIds;
    private readonly int[] _producedCoverageTransformPassable;
    private readonly int[] _producedCoveragePreferredTerrainStarts;
    private readonly int[] _producedCoveragePreferredTerrainCounts;
    private readonly int[] _producedCoverageAllowedTerrainStarts;
    private readonly int[] _producedCoverageAllowedTerrainCounts;

    private readonly int[] _relationshipSourceRefCodes;
    private readonly int[] _relationshipDestinationRefCodes;
    private readonly int[] _relationshipKindIds;
    private readonly int[] _relationshipReplaceExistingOutgoing;
    private readonly int[] _relationshipFactId0;
    private readonly int[] _relationshipFactId1;
    private readonly int[] _relationshipFactId2;
    private readonly int[] _relationshipFactId3;
    private readonly int[] _relationshipFactCounts;
    private readonly float[] _relationshipStrengths;

    private readonly int[] _scratchQ;
    private readonly int[] _scratchR;
    private readonly int[] _scratchScore;
    private readonly int[] _scratchVisitedQ;
    private readonly int[] _scratchVisitedR;
    private readonly int[] _scratchClaimedQ;
    private readonly int[] _scratchClaimedR;
    private readonly int[] _scratchConnectivityQ;
    private readonly int[] _scratchConnectivityR;
    private readonly int[] _scratchConnectivityQueueQ;
    private readonly int[] _scratchConnectivityQueueR;

    private int _generatorCount;
    private int _subtypeCount;
    private int _relationshipKindFilterCount;
    private int _terrainFilterCount;
    private int _attributeConditionCount;
    private int _triggerCount;
    private int _bindingCount;
    private int _producedCount;
    private int _relationshipCount;
    private int _openGeneratorSlot;
    private int _openBindingSlot;
    private int _openProducedSlot;

    public GeneratorProgramRuntime(int generatorCapacity, int itemCapacity)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generatorCapacity);
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(itemCapacity);
        _generatorIds = new int[generatorCapacity + 1];
        _cadences = new int[generatorCapacity + 1];
        _targetKindIds = new int[generatorCapacity + 1];
        _targetAllowWilderness = new int[generatorCapacity + 1];
        _pulseWeights = new int[generatorCapacity + 1];
        _maxRuns = new int[generatorCapacity + 1];
        _runCounts = new int[generatorCapacity + 1];
        _enabled = new bool[generatorCapacity + 1];
        _targetSubtypeStarts = new int[generatorCapacity + 1];
        _targetSubtypeCounts = new int[generatorCapacity + 1];
        _triggerStarts = new int[generatorCapacity + 1];
        _triggerCounts = new int[generatorCapacity + 1];
        _bindingStarts = new int[generatorCapacity + 1];
        _bindingCounts = new int[generatorCapacity + 1];
        _producedStarts = new int[generatorCapacity + 1];
        _producedCounts = new int[generatorCapacity + 1];
        _relationshipStarts = new int[generatorCapacity + 1];
        _relationshipCounts = new int[generatorCapacity + 1];
        _saturationRelationshipKindIds = new int[generatorCapacity + 1];
        _saturationFactIds = new int[generatorCapacity + 1];
        _saturationDirectionCodes = new int[generatorCapacity + 1];
        _saturationMaxExisting = new int[generatorCapacity + 1];
        _pulseStatusCodes = new int[generatorCapacity + 1];
        _pulseCandidateCounts = new int[generatorCapacity + 1];
        _pulseSelectedTargetIds = new int[generatorCapacity + 1];
        _pulseFailedFireCounts = new int[generatorCapacity + 1];

        _subtypeIds = new int[itemCapacity + 1];
        _relationshipKindFilterIds = new int[itemCapacity + 1];
        _terrainFilterIds = new int[itemCapacity + 1];
        _attributeConditionAttributeIds = new int[itemCapacity + 1];
        _attributeConditionDirectionCodes = new int[itemCapacity + 1];
        _attributeConditionThresholds = new float[itemCapacity + 1];
        _triggerKindIds = new int[itemCapacity + 1];
        _triggerAttributeIds = new int[itemCapacity + 1];
        _triggerDirectionCodes = new int[itemCapacity + 1];
        _triggerThresholds = new float[itemCapacity + 1];

        _bindingRefCodes = new int[itemCapacity + 1];
        _bindingKindIds = new int[itemCapacity + 1];
        _bindingAdjacentToTarget = new int[itemCapacity + 1];
        _bindingCultureMatchCodes = new int[itemCapacity + 1];
        _bindingSubtypeStarts = new int[itemCapacity + 1];
        _bindingSubtypeCounts = new int[itemCapacity + 1];
        _bindingWithoutOutgoingStarts = new int[itemCapacity + 1];
        _bindingWithoutOutgoingCounts = new int[itemCapacity + 1];
        _bindingWithoutIncomingStarts = new int[itemCapacity + 1];
        _bindingWithoutIncomingCounts = new int[itemCapacity + 1];
        _bindingAttributeConditionStarts = new int[itemCapacity + 1];
        _bindingAttributeConditionCounts = new int[itemCapacity + 1];

        _producedRefCodes = new int[itemCapacity + 1];
        _producedKindIds = new int[itemCapacity + 1];
        _producedSubtypeIds = new int[itemCapacity + 1];
        _producedCreateIfMissing = new int[itemCapacity + 1];
        _producedHasQuery = new int[itemCapacity + 1];
        _producedPhysicalPlacementCodes = new int[itemCapacity + 1];
        _producedSemanticPlacementCodes = new int[itemCapacity + 1];
        _producedPlacementRefCodes = new int[itemCapacity + 1];
        _producedCultureRefCodes = new int[itemCapacity + 1];
        _producedCreateChances = new float[itemCapacity + 1];
        _producedSubtypeChoiceStarts = new int[itemCapacity + 1];
        _producedSubtypeChoiceCounts = new int[itemCapacity + 1];
        _producedQuerySubtypeStarts = new int[itemCapacity + 1];
        _producedQuerySubtypeCounts = new int[itemCapacity + 1];
        _producedQueryAdjacentToTarget = new int[itemCapacity + 1];
        _producedQueryCultureMatchCodes = new int[itemCapacity + 1];
        _producedQueryWithoutOutgoingStarts = new int[itemCapacity + 1];
        _producedQueryWithoutOutgoingCounts = new int[itemCapacity + 1];
        _producedQueryWithoutIncomingStarts = new int[itemCapacity + 1];
        _producedQueryWithoutIncomingCounts = new int[itemCapacity + 1];
        _producedQueryAttributeConditionStarts = new int[itemCapacity + 1];
        _producedQueryAttributeConditionCounts = new int[itemCapacity + 1];
        _producedCoverageModeCodes = new int[itemCapacity + 1];
        _producedCoverageSourceRefCodes = new int[itemCapacity + 1];
        _producedCoverageDesiredCellCounts = new int[itemCapacity + 1];
        _producedCoverageMinimumCellCounts = new int[itemCapacity + 1];
        _producedCoverageMinimumSourceRemainingCellCounts = new int[itemCapacity + 1];
        _producedCoverageRespectTerrainBoundaries = new int[itemCapacity + 1];
        _producedCoverageTransformTerrainIds = new int[itemCapacity + 1];
        _producedCoverageTransformPassable = new int[itemCapacity + 1];
        _producedCoveragePreferredTerrainStarts = new int[itemCapacity + 1];
        _producedCoveragePreferredTerrainCounts = new int[itemCapacity + 1];
        _producedCoverageAllowedTerrainStarts = new int[itemCapacity + 1];
        _producedCoverageAllowedTerrainCounts = new int[itemCapacity + 1];

        _relationshipSourceRefCodes = new int[itemCapacity + 1];
        _relationshipDestinationRefCodes = new int[itemCapacity + 1];
        _relationshipKindIds = new int[itemCapacity + 1];
        _relationshipReplaceExistingOutgoing = new int[itemCapacity + 1];
        _relationshipFactId0 = new int[itemCapacity + 1];
        _relationshipFactId1 = new int[itemCapacity + 1];
        _relationshipFactId2 = new int[itemCapacity + 1];
        _relationshipFactId3 = new int[itemCapacity + 1];
        _relationshipFactCounts = new int[itemCapacity + 1];
        _relationshipStrengths = new float[itemCapacity + 1];

        _scratchQ = new int[MaxScratchCells];
        _scratchR = new int[MaxScratchCells];
        _scratchScore = new int[MaxScratchCells];
        _scratchVisitedQ = new int[MaxScratchCells];
        _scratchVisitedR = new int[MaxScratchCells];
        _scratchClaimedQ = new int[MaxScratchCells];
        _scratchClaimedR = new int[MaxScratchCells];
        _scratchConnectivityQ = new int[MaxScratchCells];
        _scratchConnectivityR = new int[MaxScratchCells];
        _scratchConnectivityQueueQ = new int[MaxScratchCells];
        _scratchConnectivityQueueR = new int[MaxScratchCells];
    }

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
        int coverageTransformTerrainId,
        int coverageTransformPassable)
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
        _producedRefCodes[_producedCount] = refCode;
        _producedKindIds[_producedCount] = kindId;
        _producedSubtypeIds[_producedCount] = subtypeId;
        _producedCreateIfMissing[_producedCount] = createIfMissing;
        _producedHasQuery[_producedCount] = hasQuery;
        _producedPhysicalPlacementCodes[_producedCount] = physicalPlacementCode;
        _producedSemanticPlacementCodes[_producedCount] = semanticPlacementCode;
        _producedPlacementRefCodes[_producedCount] = placementRefCode;
        _producedCultureRefCodes[_producedCount] = cultureRefCode;
        _producedCreateChances[_producedCount] = createChance;
        _producedQueryAdjacentToTarget[_producedCount] = queryAdjacentToTarget;
        _producedQueryCultureMatchCodes[_producedCount] = queryCultureMatchCode;
        _producedCoverageModeCodes[_producedCount] = coverageModeCode;
        _producedCoverageSourceRefCodes[_producedCount] = coverageSourceRefCode;
        _producedCoverageDesiredCellCounts[_producedCount] = coverageDesiredCellCount;
        _producedCoverageMinimumCellCounts[_producedCount] = coverageMinimumCellCount;
        _producedCoverageMinimumSourceRemainingCellCounts[_producedCount] = coverageMinimumSourceRemainingCellCount;
        _producedCoverageRespectTerrainBoundaries[_producedCount] = coverageRespectTerrainBoundaries;
        _producedCoverageTransformTerrainIds[_producedCount] = coverageTransformTerrainId;
        _producedCoverageTransformPassable[_producedCount] = coverageTransformPassable;
        _producedSubtypeChoiceStarts[_producedCount] = _subtypeCount + 1;
        _producedQuerySubtypeStarts[_producedCount] = _subtypeCount + 1;
        _producedQueryWithoutOutgoingStarts[_producedCount] = _relationshipKindFilterCount + 1;
        _producedQueryWithoutIncomingStarts[_producedCount] = _relationshipKindFilterCount + 1;
        _producedQueryAttributeConditionStarts[_producedCount] = _attributeConditionCount + 1;
        _producedCoveragePreferredTerrainStarts[_producedCount] = _terrainFilterCount + 1;
        _producedCoverageAllowedTerrainStarts[_producedCount] = _terrainFilterCount + 1;
        _producedCounts[generatorSlot]++;
        _openProducedSlot = _producedCount;
        return _producedCount;
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

        if (!anyDue)
        {
            return;
        }

        while (TryChooseEligibleGenerator(simulation, out var selectedGeneratorSlot))
        {
            var targetId = SelectEligibleTarget(simulation, selectedGeneratorSlot);
            if (targetId == 0)
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
                continue;
            }

            _pulseSelectedTargetIds[selectedGeneratorSlot] = targetId;
            if (Fire(simulation, selectedGeneratorSlot, targetId, tick))
            {
                _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
                for (var slot = 1; slot <= _generatorCount; slot++)
                {
                    if (_pulseStatusCodes[slot] == GeneratorStatusCodes.Eligible)
                    {
                        _pulseStatusCodes[slot] = GeneratorStatusCodes.EligibleNotSelected;
                    }
                }

                PublishDueGeneratorStats(simulation, tick);
                return;
            }

            _pulseFailedFireCounts[selectedGeneratorSlot]++;
            _pulseStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
        }

        PublishDueGeneratorStats(simulation, tick);
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
        global::System.Span<int> refCodes = stackalloc int[MaxResolvedReferences];
        global::System.Span<int> refEntityIds = stackalloc int[MaxResolvedReferences];
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
        global::System.Span<int> refCodes = stackalloc int[MaxResolvedReferences];
        global::System.Span<int> refEntityIds = stackalloc int[MaxResolvedReferences];
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

        SetRef(refCodes, refEntityIds, ref refCount, TargetReferenceCode, targetId);
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

    private bool Fire(Simulation simulation, int generatorSlot, int targetId, int tick)
    {
        var world = simulation.World;
        var targetOriginQ = world.EntityPhysicalQ(targetId);
        var targetOriginR = world.EntityPhysicalR(targetId);
        if (_targetAllowWilderness[generatorSlot] != 0 &&
            !FindFreePhysicalHex(world, targetOriginQ, targetOriginR, maxRadius: 5, out targetOriginQ, out targetOriginR))
        {
            return false;
        }

        global::System.Span<int> refCodes = stackalloc int[MaxResolvedReferences];
        global::System.Span<int> refEntityIds = stackalloc int[MaxResolvedReferences];
        global::System.Span<int> createdEntityIds = stackalloc int[MaxResolvedReferences];
        var refCount = 0;
        var createdEntityCount = 0;
        SetRef(refCodes, refEntityIds, ref refCount, TargetReferenceCode, targetId);

        if (!ResolveSecondaryBindings(
                world,
                generatorSlot,
                targetId,
                targetOriginQ,
                targetOriginR,
                refCodes,
                refEntityIds,
                ref refCount,
                selectRandom: true,
                simulation))
        {
            return false;
        }

        var firstProduced = _producedStarts[generatorSlot];
        var endProduced = firstProduced + _producedCounts[generatorSlot];
        for (var produced = firstProduced; produced < endProduced; produced++)
        {
            if (_producedCreateChances[produced] < 1f &&
                simulation.NextFloat() > _producedCreateChances[produced])
            {
                SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], 0);
                continue;
            }

            if (_producedHasQuery[produced] != 0)
            {
                var boundEntityId = SelectEntityForQuery(
                    world,
                    simulation,
                    _producedKindIds[produced],
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    refCount,
                    _producedQuerySubtypeStarts[produced],
                    _producedQuerySubtypeCounts[produced],
                    _producedQueryAdjacentToTarget[produced],
                    _producedQueryCultureMatchCodes[produced],
                    _producedQueryWithoutOutgoingStarts[produced],
                    _producedQueryWithoutOutgoingCounts[produced],
                    _producedQueryWithoutIncomingStarts[produced],
                    _producedQueryWithoutIncomingCounts[produced],
                    _producedQueryAttributeConditionStarts[produced],
                    _producedQueryAttributeConditionCounts[produced]);
                if (boundEntityId > 0)
                {
                    SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], boundEntityId);
                    continue;
                }
            }

            if (_producedCreateIfMissing[produced] == 0)
            {
                SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], 0);
                continue;
            }

            var subtypeId = ResolveProducedSubtype(simulation, produced);
            if (subtypeId == 0)
            {
                MarkCreatedEntitiesHistorical(world, createdEntityIds, createdEntityCount, tick);
                return false;
            }

            var cultureSourceId = ResolveRef(refCodes, refEntityIds, refCount, _producedCultureRefCodes[produced]);
            if (cultureSourceId == 0)
            {
                MarkCreatedEntitiesHistorical(world, createdEntityIds, createdEntityCount, tick);
                return false;
            }

            var placementAnchorId = ResolveRef(refCodes, refEntityIds, refCount, _producedPlacementRefCodes[produced]);
            if (placementAnchorId == 0)
            {
                placementAnchorId = targetId;
            }

            ResolvePhysicalPlacement(
                simulation,
                placementAnchorId,
                _producedPhysicalPlacementCodes[produced],
                out var physicalQ,
                out var physicalR);
            ResolveSemanticPlacement(
                simulation,
                placementAnchorId,
                _producedSemanticPlacementCodes[produced],
                out var semanticQ,
                out var semanticR);

            var createdEntityId = simulation.CreateEntity(
                _producedKindIds[produced],
                subtypeId,
                world.EntityCultureId(cultureSourceId),
                physicalQ,
                physicalR,
                semanticQ,
                semanticR,
                -semanticQ - semanticR,
                tick);
            createdEntityIds[createdEntityCount] = createdEntityId;
            createdEntityCount++;
            SetRef(refCodes, refEntityIds, ref refCount, _producedRefCodes[produced], createdEntityId);

            if (_producedCoverageModeCodes[produced] != GeneratorCoverageModeCodes.None)
            {
                if (!CreateCoverage(simulation, produced, createdEntityId, refCodes, refEntityIds, refCount, physicalQ, physicalR, out var anchorQ, out var anchorR))
                {
                    MarkCreatedEntitiesHistorical(world, createdEntityIds, createdEntityCount, tick);
                    return false;
                }

                if (anchorQ != physicalQ || anchorR != physicalR)
                {
                    world.MoveEntityPhysical(createdEntityId, anchorQ, anchorR, tick);
                }
            }
        }

        var createdRelationshipCount = CreateRelationships(
            simulation,
            world,
            generatorSlot,
            refCodes,
            refEntityIds,
            refCount,
            tick);
        _runCounts[generatorSlot]++;
        WriteGeneratorFired(simulation, tick, generatorSlot, targetId, createdEntityCount, createdRelationshipCount);
        return true;
    }

    private int CreateRelationships(
        Simulation simulation,
        WorldState world,
        int generatorSlot,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int tick)
    {
        var created = 0;
        var first = _relationshipStarts[generatorSlot];
        var end = first + _relationshipCounts[generatorSlot];
        for (var relationship = first; relationship < end; relationship++)
        {
            var sourceId = ResolveRef(refCodes, refEntityIds, refCount, _relationshipSourceRefCodes[relationship]);
            var destinationId = ResolveRef(refCodes, refEntityIds, refCount, _relationshipDestinationRefCodes[relationship]);
            if (sourceId == 0 || destinationId == 0)
            {
                continue;
            }

            if (_relationshipReplaceExistingOutgoing[relationship] != 0)
            {
                CullOutgoingRelationshipsOfKind(world, sourceId, _relationshipKindIds[relationship], tick);
            }

            simulation.CreateRelationship(
                _relationshipKindIds[relationship],
                0,
                sourceId,
                destinationId,
                _relationshipStrengths[relationship],
                tick,
                _relationshipFactId0[relationship],
                _relationshipFactId1[relationship],
                _relationshipFactId2[relationship],
                _relationshipFactId3[relationship],
                _relationshipFactCounts[relationship]);
            created++;
        }

        return created;
    }

    private static void CullOutgoingRelationshipsOfKind(WorldState world, int sourceEntityId, int relationshipKindId, int tick)
    {
        world.CullOutgoingRelationshipsOfKind(sourceEntityId, relationshipKindId, tick);
    }

    private bool ExceedsTargetSaturation(WorldState world, int generatorSlot, int targetId)
    {
        if (_saturationRelationshipKindIds[generatorSlot] == 0)
        {
            return false;
        }

        var count = 0;
        var direction = _saturationDirectionCodes[generatorSlot];
        if (direction == GeneratorSaturationDirectionCodes.IncomingToTarget)
        {
            var incoming = world.IncomingRelationshipsOfKind(targetId, _saturationRelationshipKindIds[generatorSlot]);
            while (world.MoveNextRelationship(ref incoming, out var relationshipId))
            {
                if (TargetSaturationRelationshipMatches(world, generatorSlot, relationshipId))
                {
                    count++;
                }
            }

            return count > _saturationMaxExisting[generatorSlot];
        }

        var outgoing = world.OutgoingRelationshipsOfKind(targetId, _saturationRelationshipKindIds[generatorSlot]);
        while (world.MoveNextRelationship(ref outgoing, out var relationshipId))
        {
            if (TargetSaturationRelationshipMatches(world, generatorSlot, relationshipId))
            {
                count++;
            }
        }

        if (direction == GeneratorSaturationDirectionCodes.Symmetric)
        {
            var incoming = world.IncomingRelationshipsOfKind(targetId, _saturationRelationshipKindIds[generatorSlot]);
            while (world.MoveNextRelationship(ref incoming, out var relationshipId))
            {
                if (TargetSaturationRelationshipMatches(world, generatorSlot, relationshipId))
                {
                    count++;
                }
            }
        }

        return count > _saturationMaxExisting[generatorSlot];
    }

    private bool TargetSaturationRelationshipMatches(WorldState world, int generatorSlot, int relationshipId)
    {
        return world.RelationshipHasFact(relationshipId, _saturationFactIds[generatorSlot]);
    }

    private bool TriggersPass(WorldState world, int generatorSlot, int targetId)
    {
        var first = _triggerStarts[generatorSlot];
        var end = first + _triggerCounts[generatorSlot];
        for (var trigger = first; trigger < end; trigger++)
        {
            if (TriggerPasses(world, trigger, targetId))
            {
                return true;
            }
        }

        return false;
    }

    private bool TriggerPasses(WorldState world, int trigger, int targetId)
    {
        var direction = _triggerDirectionCodes[trigger];
        if (direction == GeneratorTriggerDirectionCodes.Any)
        {
            return true;
        }

        if (_triggerAttributeIds[trigger] == 0)
        {
            return false;
        }

        return DirectionPasses(
            world.GetAttribute(targetId, _triggerAttributeIds[trigger]),
            direction,
            _triggerThresholds[trigger]);
    }

    private int SelectEntityForQuery(
        WorldState world,
        Simulation? simulation,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        var candidateCount = CountMatchingEntitiesForQuery(
            world,
            kindId,
            targetId,
            targetOriginQ,
            targetOriginR,
            refCodes,
            refEntityIds,
            refCount,
            subtypeStart,
            subtypeCount,
            adjacentToTarget,
            cultureMatchCode,
            withoutOutgoingStart,
            withoutOutgoingCount,
            withoutIncomingStart,
            withoutIncomingCount,
            attributeConditionStart,
            attributeConditionCount);
        if (candidateCount == 0)
        {
            return 0;
        }

        var selectedOrdinal = simulation is null ? 0 : simulation.NextInt(candidateCount);
        var seen = 0;
        for (var entityId = 1; entityId <= world.EntityHighWatermark; entityId++)
        {
            if (!EntityMatchesQuery(
                    world,
                    entityId,
                    kindId,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    refCount,
                    subtypeStart,
                    subtypeCount,
                    adjacentToTarget,
                    cultureMatchCode,
                    withoutOutgoingStart,
                    withoutOutgoingCount,
                    withoutIncomingStart,
                    withoutIncomingCount,
                    attributeConditionStart,
                    attributeConditionCount))
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

    private int CountMatchingEntitiesForQuery(
        WorldState world,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        var count = 0;
        for (var entityId = world.FirstEntityOfKind(kindId);
             entityId > 0;
             entityId = world.NextEntityOfKind(entityId))
        {
            if (EntityMatchesQuery(
                    world,
                    entityId,
                    kindId,
                    targetId,
                    targetOriginQ,
                    targetOriginR,
                    refCodes,
                    refEntityIds,
                    refCount,
                    subtypeStart,
                    subtypeCount,
                    adjacentToTarget,
                    cultureMatchCode,
                    withoutOutgoingStart,
                    withoutOutgoingCount,
                    withoutIncomingStart,
                    withoutIncomingCount,
                    attributeConditionStart,
                    attributeConditionCount))
            {
                count++;
            }
        }

        return count;
    }

    private bool EntityMatchesQuery(
        WorldState world,
        int entityId,
        int kindId,
        int targetId,
        int targetOriginQ,
        int targetOriginR,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int subtypeStart,
        int subtypeCount,
        int adjacentToTarget,
        int cultureMatchCode,
        int withoutOutgoingStart,
        int withoutOutgoingCount,
        int withoutIncomingStart,
        int withoutIncomingCount,
        int attributeConditionStart,
        int attributeConditionCount)
    {
        return world.EntityIsActive(entityId) &&
               entityId != targetId &&
               !RefAlreadyBound(refEntityIds, refCount, entityId) &&
               world.EntityKindId(entityId) == kindId &&
               SubtypeMatches(world.EntitySubtypeId(entityId), subtypeStart, subtypeCount) &&
               CultureMatches(world, entityId, targetId, cultureMatchCode) &&
               RelationshipFiltersPass(world, entityId, withoutOutgoingStart, withoutOutgoingCount, outgoing: true) &&
               RelationshipFiltersPass(world, entityId, withoutIncomingStart, withoutIncomingCount, outgoing: false) &&
               AttributeConditionsPass(world, entityId, attributeConditionStart, attributeConditionCount) &&
               (adjacentToTarget == 0 ||
                HexDistance(world.EntityPhysicalQ(entityId), world.EntityPhysicalR(entityId), targetOriginQ, targetOriginR) == 1);
    }

    private bool SubtypeMatches(int subtypeId, int start, int count)
    {
        if (count == 0)
        {
            return true;
        }

        var end = start + count;
        for (var i = start; i < end; i++)
        {
            if (_subtypeIds[i] == subtypeId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CultureMatches(WorldState world, int entityId, int targetId, int cultureMatchCode)
    {
        return cultureMatchCode switch
        {
            GeneratorCultureMatchCodes.SameAsTarget => world.EntityCultureId(entityId) == world.EntityCultureId(targetId),
            GeneratorCultureMatchCodes.DifferentFromTarget => world.EntityCultureId(entityId) != world.EntityCultureId(targetId),
            _ => true,
        };
    }

    private bool RelationshipFiltersPass(
        WorldState world,
        int entityId,
        int start,
        int count,
        bool outgoing)
    {
        var end = start + count;
        for (var filter = start; filter < end; filter++)
        {
            var cursor = outgoing
                ? world.OutgoingRelationshipsOfKind(entityId, _relationshipKindFilterIds[filter])
                : world.IncomingRelationshipsOfKind(entityId, _relationshipKindFilterIds[filter]);
            if (world.MoveNextRelationship(ref cursor, out _))
            {
                return false;
            }
        }

        return true;
    }

    private bool AttributeConditionsPass(WorldState world, int entityId, int start, int count)
    {
        var end = start + count;
        for (var condition = start; condition < end; condition++)
        {
            if (!DirectionPasses(
                    world.GetAttribute(entityId, _attributeConditionAttributeIds[condition]),
                    _attributeConditionDirectionCodes[condition],
                    _attributeConditionThresholds[condition]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool DirectionPasses(float value, int directionCode, float threshold)
    {
        return directionCode switch
        {
            GeneratorTriggerDirectionCodes.Low => value <= threshold,
            GeneratorTriggerDirectionCodes.Critical => value <= threshold,
            GeneratorTriggerDirectionCodes.High => value >= threshold,
            GeneratorTriggerDirectionCodes.Any => true,
            _ => false,
        };
    }

    private int ResolveProducedSubtype(Simulation simulation, int producedSlot)
    {
        if (_producedSubtypeIds[producedSlot] > 0)
        {
            return _producedSubtypeIds[producedSlot];
        }

        var count = _producedSubtypeChoiceCounts[producedSlot];
        if (count == 0)
        {
            return 0;
        }

        var selected = simulation.NextInt(count);
        return _subtypeIds[_producedSubtypeChoiceStarts[producedSlot] + selected];
    }

    private void ResolvePhysicalPlacement(
        Simulation simulation,
        int anchorEntityId,
        int placementCode,
        out int q,
        out int r)
    {
        var world = simulation.World;
        var originQ = world.EntityPhysicalQ(anchorEntityId);
        var originR = world.EntityPhysicalR(anchorEntityId);
        switch (placementCode)
        {
            case GeneratorPlacementCodes.On:
                if (!IsPhysicalOccupied(world, originQ, originR))
                {
                    q = originQ;
                    r = originR;
                    return;
                }

                if (FindFreePhysicalHex(world, originQ, originR, maxRadius: 5, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
            case GeneratorPlacementCodes.Across:
                var mirroredQ = -originQ;
                var mirroredR = -originR;
                if (IsFreePhysical(world, mirroredQ, mirroredR))
                {
                    q = mirroredQ;
                    r = mirroredR;
                    return;
                }

                if (FindFreePhysicalHex(world, mirroredQ, mirroredR, world.MapRadius, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
            case GeneratorPlacementCodes.InCoverageLocality:
                if (FindRandomFreePhysicalHexInCoverage(simulation, anchorEntityId, out q, out r))
                {
                    return;
                }

                goto default;
            default:
                if (FindFreePhysicalHex(world, originQ, originR, maxRadius: 5, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
        }
    }

    private static void ResolveSemanticPlacement(
        Simulation simulation,
        int anchorEntityId,
        int placementCode,
        out int q,
        out int r)
    {
        var world = simulation.World;
        var originQ = world.EntitySemanticQ(anchorEntityId);
        var originR = world.EntitySemanticR(anchorEntityId);
        switch (placementCode)
        {
            case GeneratorPlacementCodes.On:
                if (!IsSemanticOccupied(world, originQ, originR))
                {
                    q = originQ;
                    r = originR;
                    return;
                }

                if (FindFreeSemanticHex(world, originQ, originR, maxRadius: 5, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
            case GeneratorPlacementCodes.Across:
                var mirroredQ = -originQ;
                var mirroredR = -originR;
                if (!IsSemanticOccupied(world, mirroredQ, mirroredR))
                {
                    q = mirroredQ;
                    r = mirroredR;
                    return;
                }

                if (FindFreeSemanticHex(world, mirroredQ, mirroredR, maxRadius: 24, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
            default:
                if (FindFreeSemanticHex(world, originQ, originR, maxRadius: 5, out q, out r))
                {
                    return;
                }

                q = originQ;
                r = originR;
                return;
        }
    }

    private bool CreateCoverage(
        Simulation simulation,
        int producedSlot,
        int ownerEntityId,
        global::System.Span<int> refCodes,
        global::System.Span<int> refEntityIds,
        int refCount,
        int originQ,
        int originR,
        out int anchorQ,
        out int anchorR)
    {
        anchorQ = originQ;
        anchorR = originR;
        var world = simulation.World;
        var sourceOwnerId = 0;
        if (_producedCoverageModeCodes[producedSlot] == GeneratorCoverageModeCodes.SplitFromExisting)
        {
            if (_producedCoverageSourceRefCodes[producedSlot] > 0)
            {
                sourceOwnerId = ResolveRef(refCodes, refEntityIds, refCount, _producedCoverageSourceRefCodes[producedSlot]);
            }

            if (sourceOwnerId == 0)
            {
                sourceOwnerId = world.CellCoverageOwner(originQ, originR);
            }

            if (sourceOwnerId == 0)
            {
                return false;
            }
        }

        if (!FindCoverageSeedCell(world, producedSlot, originQ, originR, sourceOwnerId, out anchorQ, out anchorR))
        {
            return false;
        }

        var claimedCount = BuildCoveragePatch(world, producedSlot, anchorQ, anchorR, sourceOwnerId);
        if (claimedCount < _producedCoverageMinimumCellCounts[producedSlot])
        {
            return false;
        }

        for (var i = 0; i < claimedCount; i++)
        {
            var q = _scratchClaimedQ[i];
            var r = _scratchClaimedR[i];
            world.SetCellCoverageOwner(q, r, ownerEntityId);
            if (_producedCoverageTransformTerrainIds[producedSlot] > 0)
            {
                world.SetCellTerrain(
                    q,
                    r,
                    _producedCoverageTransformTerrainIds[producedSlot],
                    _producedCoverageTransformPassable[producedSlot] != 0);
            }
        }

        return true;
    }

    private bool FindCoverageSeedCell(
        WorldState world,
        int producedSlot,
        int originQ,
        int originR,
        int sourceOwnerId,
        out int seedQ,
        out int seedR)
    {
        var boundaryGroupId = 0;
        if (world.CellIsKnown(originQ, originR))
        {
            boundaryGroupId = world.TerrainBoundaryGroupId(world.CellTerrainId(originQ, originR));
        }

        if (SearchCoverageSeedCell(world, producedSlot, originQ, originR, sourceOwnerId, boundaryGroupId, out seedQ, out seedR))
        {
            return true;
        }

        if (_producedCoverageRespectTerrainBoundaries[producedSlot] == 0 || boundaryGroupId == 0)
        {
            return false;
        }

        return SearchCoverageSeedCell(world, producedSlot, originQ, originR, sourceOwnerId, boundaryGroupId: 0, out seedQ, out seedR);
    }

    private bool SearchCoverageSeedCell(
        WorldState world,
        int producedSlot,
        int originQ,
        int originR,
        int sourceOwnerId,
        int boundaryGroupId,
        out int seedQ,
        out int seedR)
    {
        seedQ = 0;
        seedR = 0;
        var bestScore = int.MaxValue;
        var found = false;
        for (var radius = 0; radius <= world.MapRadius; radius++)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                var minDr = global::System.Math.Max(-radius, -dq - radius);
                var maxDr = global::System.Math.Min(radius, -dq + radius);
                for (var dr = minDr; dr <= maxDr; dr++)
                {
                    var q = originQ + dq;
                    var r = originR + dr;
                    if (HexDistance(originQ, originR, q, r) != radius ||
                        !CoverageCellIsEligible(world, producedSlot, q, r, sourceOwnerId, boundaryGroupId, alreadyClaimedCount: 0))
                    {
                        continue;
                    }

                    var score = ScoreCoverageCell(world, producedSlot, q, r, originQ, originR, boundaryGroupId);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        seedQ = q;
                        seedR = r;
                        found = true;
                    }
                }
            }

            if (found)
            {
                return true;
            }
        }

        return false;
    }

    private int BuildCoveragePatch(
        WorldState world,
        int producedSlot,
        int anchorQ,
        int anchorR,
        int sourceOwnerId)
    {
        var frontierCount = 1;
        var visitedCount = 0;
        var claimedCount = 0;
        _scratchQ[0] = anchorQ;
        _scratchR[0] = anchorR;
        var boundaryGroupId = world.TerrainBoundaryGroupId(world.CellTerrainId(anchorQ, anchorR));
        var desiredCount = _producedCoverageDesiredCellCounts[producedSlot];
        while (frontierCount > 0 && claimedCount < desiredCount)
        {
            var selected = SelectBestFrontierCell(world, producedSlot, frontierCount, anchorQ, anchorR, boundaryGroupId);
            var currentQ = _scratchQ[selected];
            var currentR = _scratchR[selected];
            frontierCount--;
            _scratchQ[selected] = _scratchQ[frontierCount];
            _scratchR[selected] = _scratchR[frontierCount];

            if (ScratchContains(_scratchVisitedQ, _scratchVisitedR, visitedCount, currentQ, currentR))
            {
                continue;
            }

            _scratchVisitedQ[visitedCount] = currentQ;
            _scratchVisitedR[visitedCount] = currentR;
            visitedCount++;

            if (!CoverageCellIsEligible(world, producedSlot, currentQ, currentR, sourceOwnerId, boundaryGroupId, claimedCount))
            {
                continue;
            }

            _scratchClaimedQ[claimedCount] = currentQ;
            _scratchClaimedR[claimedCount] = currentR;
            claimedCount++;

            for (var direction = 0; direction < 6; direction++)
            {
                var q = currentQ + DirectionQ(direction);
                var r = currentR + DirectionR(direction);
                if (frontierCount >= _scratchQ.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                if (world.CellIsKnown(q, r) &&
                    !ScratchContains(_scratchVisitedQ, _scratchVisitedR, visitedCount, q, r))
                {
                    _scratchQ[frontierCount] = q;
                    _scratchR[frontierCount] = r;
                    frontierCount++;
                }
            }
        }

        return claimedCount;
    }

    private int SelectBestFrontierCell(
        WorldState world,
        int producedSlot,
        int frontierCount,
        int anchorQ,
        int anchorR,
        int boundaryGroupId)
    {
        var best = 0;
        var bestScore = int.MaxValue;
        for (var i = 0; i < frontierCount; i++)
        {
            var score = ScoreCoverageCell(world, producedSlot, _scratchQ[i], _scratchR[i], anchorQ, anchorR, boundaryGroupId);
            _scratchScore[i] = score;
            if (score < bestScore)
            {
                best = i;
                bestScore = score;
            }
        }

        return best;
    }

    private bool CoverageCellIsEligible(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int sourceOwnerId,
        int boundaryGroupId,
        int alreadyClaimedCount)
    {
        if (!world.CellIsKnown(q, r))
        {
            return false;
        }

        var mode = _producedCoverageModeCodes[producedSlot];
        if (mode == GeneratorCoverageModeCodes.CreateNew)
        {
            if (world.CellCoverageOwner(q, r) != 0)
            {
                return false;
            }
        }
        else if (mode == GeneratorCoverageModeCodes.SplitFromExisting)
        {
            if (sourceOwnerId == 0 || world.CellCoverageOwner(q, r) != sourceOwnerId)
            {
                return false;
            }

            var remaining = world.CoverageCellCount(sourceOwnerId) - alreadyClaimedCount - 1;
            if (remaining < _producedCoverageMinimumSourceRemainingCellCounts[producedSlot])
            {
                return false;
            }

            if (!RemainingCoverageIsContiguous(world, sourceOwnerId, q, r, alreadyClaimedCount))
            {
                return false;
            }
        }

        var terrainId = world.CellTerrainId(q, r);
        if (!TerrainInFilter(terrainId, _producedCoverageAllowedTerrainStarts[producedSlot], _producedCoverageAllowedTerrainCounts[producedSlot], emptyPasses: true))
        {
            return false;
        }

        return _producedCoverageRespectTerrainBoundaries[producedSlot] == 0 ||
               boundaryGroupId == 0 ||
               world.TerrainBoundaryGroupId(terrainId) == boundaryGroupId;
    }

    private bool RemainingCoverageIsContiguous(
        WorldState world,
        int sourceOwnerId,
        int removedQ,
        int removedR,
        int alreadyClaimedCount)
    {
        var firstQ = 0;
        var firstR = 0;
        var remainingCount = 0;
        for (var q = -world.MapRadius; q <= world.MapRadius; q++)
        {
            var minR = global::System.Math.Max(-world.MapRadius, -q - world.MapRadius);
            var maxR = global::System.Math.Min(world.MapRadius, -q + world.MapRadius);
            for (var r = minR; r <= maxR; r++)
            {
                if (world.CellCoverageOwner(q, r) != sourceOwnerId ||
                    CoverageRemovalContains(q, r, removedQ, removedR, alreadyClaimedCount))
                {
                    continue;
                }

                if (remainingCount == 0)
                {
                    firstQ = q;
                    firstR = r;
                }

                remainingCount++;
            }
        }

        if (remainingCount == 0)
        {
            return false;
        }

        var visitedCount = 1;
        var queueHead = 0;
        var queueCount = 1;
        _scratchConnectivityQ[0] = firstQ;
        _scratchConnectivityR[0] = firstR;
        _scratchConnectivityQueueQ[0] = firstQ;
        _scratchConnectivityQueueR[0] = firstR;

        while (queueHead < queueCount)
        {
            var currentQ = _scratchConnectivityQueueQ[queueHead];
            var currentR = _scratchConnectivityQueueR[queueHead];
            queueHead++;

            for (var direction = 0; direction < 6; direction++)
            {
                var neighborQ = currentQ + DirectionQ(direction);
                var neighborR = currentR + DirectionR(direction);
                if (world.CellCoverageOwner(neighborQ, neighborR) != sourceOwnerId ||
                    CoverageRemovalContains(neighborQ, neighborR, removedQ, removedR, alreadyClaimedCount) ||
                    ScratchContains(_scratchConnectivityQ, _scratchConnectivityR, visitedCount, neighborQ, neighborR))
                {
                    continue;
                }

                if (visitedCount >= _scratchConnectivityQ.Length || queueCount >= _scratchConnectivityQueueQ.Length)
                {
                    throw new global::System.InvalidOperationException();
                }

                _scratchConnectivityQ[visitedCount] = neighborQ;
                _scratchConnectivityR[visitedCount] = neighborR;
                visitedCount++;
                _scratchConnectivityQueueQ[queueCount] = neighborQ;
                _scratchConnectivityQueueR[queueCount] = neighborR;
                queueCount++;
            }
        }

        return visitedCount == remainingCount;
    }

    private bool CoverageRemovalContains(int q, int r, int removedQ, int removedR, int alreadyClaimedCount)
    {
        if (q == removedQ && r == removedR)
        {
            return true;
        }

        return ScratchContains(_scratchClaimedQ, _scratchClaimedR, alreadyClaimedCount, q, r);
    }

    private int ScoreCoverageCell(
        WorldState world,
        int producedSlot,
        int q,
        int r,
        int originQ,
        int originR,
        int boundaryGroupId)
    {
        if (!world.CellIsKnown(q, r))
        {
            return int.MaxValue;
        }

        var score = HexDistance(originQ, originR, q, r) * 10;
        var terrainId = world.CellTerrainId(q, r);
        if (!TerrainInFilter(terrainId, _producedCoveragePreferredTerrainStarts[producedSlot], _producedCoveragePreferredTerrainCounts[producedSlot], emptyPasses: true))
        {
            score += 20;
        }

        if (_producedCoverageRespectTerrainBoundaries[producedSlot] != 0 &&
            boundaryGroupId != 0 &&
            world.TerrainBoundaryGroupId(terrainId) != boundaryGroupId)
        {
            score += 100;
        }

        return score;
    }

    private bool TerrainInFilter(int terrainId, int start, int count, bool emptyPasses)
    {
        if (count == 0)
        {
            return emptyPasses;
        }

        var end = start + count;
        for (var i = start; i < end; i++)
        {
            if (_terrainFilterIds[i] == terrainId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool FindFreePhysicalHex(
        WorldState world,
        int originQ,
        int originR,
        int maxRadius,
        out int q,
        out int r)
    {
        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                var minDr = global::System.Math.Max(-radius, -dq - radius);
                var maxDr = global::System.Math.Min(radius, -dq + radius);
                for (var dr = minDr; dr <= maxDr; dr++)
                {
                    q = originQ + dq;
                    r = originR + dr;
                    if (HexDistance(originQ, originR, q, r) == radius && IsFreePhysical(world, q, r))
                    {
                        return true;
                    }
                }
            }
        }

        q = originQ;
        r = originR;
        return false;
    }

    private static bool FindRandomFreePhysicalHexInCoverage(
        Simulation simulation,
        int coverageOwnerId,
        out int q,
        out int r)
    {
        var world = simulation.World;
        var candidateCount = 0;
        for (var cq = -world.MapRadius; cq <= world.MapRadius; cq++)
        {
            var minR = global::System.Math.Max(-world.MapRadius, -cq - world.MapRadius);
            var maxR = global::System.Math.Min(world.MapRadius, -cq + world.MapRadius);
            for (var cr = minR; cr <= maxR; cr++)
            {
                if (world.CellCoverageOwner(cq, cr) == coverageOwnerId && IsFreePhysical(world, cq, cr))
                {
                    candidateCount++;
                }
            }
        }

        if (candidateCount == 0)
        {
            q = 0;
            r = 0;
            return false;
        }

        var selected = simulation.NextInt(candidateCount);
        var seen = 0;
        for (var cq = -world.MapRadius; cq <= world.MapRadius; cq++)
        {
            var minR = global::System.Math.Max(-world.MapRadius, -cq - world.MapRadius);
            var maxR = global::System.Math.Min(world.MapRadius, -cq + world.MapRadius);
            for (var cr = minR; cr <= maxR; cr++)
            {
                if (world.CellCoverageOwner(cq, cr) != coverageOwnerId || !IsFreePhysical(world, cq, cr))
                {
                    continue;
                }

                if (seen == selected)
                {
                    q = cq;
                    r = cr;
                    return true;
                }

                seen++;
            }
        }

        q = 0;
        r = 0;
        return false;
    }

    private static bool FindFreeSemanticHex(
        WorldState world,
        int originQ,
        int originR,
        int maxRadius,
        out int q,
        out int r)
    {
        for (var radius = 0; radius <= maxRadius; radius++)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                var minDr = global::System.Math.Max(-radius, -dq - radius);
                var maxDr = global::System.Math.Min(radius, -dq + radius);
                for (var dr = minDr; dr <= maxDr; dr++)
                {
                    q = originQ + dq;
                    r = originR + dr;
                    if (HexDistance(originQ, originR, q, r) == radius && !IsSemanticOccupied(world, q, r))
                    {
                        return true;
                    }
                }
            }
        }

        q = originQ;
        r = originR;
        return false;
    }

    private static bool IsFreePhysical(WorldState world, int q, int r)
    {
        return world.CellIsKnown(q, r) && world.CellIsPassable(q, r) && !IsPhysicalOccupied(world, q, r);
    }

    private static bool IsPhysicalOccupied(WorldState world, int q, int r)
    {
        for (var entityId = world.FirstPhysicalEntityAt(q, r);
             entityId > 0;
             entityId = world.NextPhysicalEntityAt(entityId))
        {
            if (world.EntityIsActive(entityId) &&
                world.EntityPhysicalQ(entityId) == q &&
                world.EntityPhysicalR(entityId) == r)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSemanticOccupied(WorldState world, int q, int r)
    {
        for (var entityId = world.FirstSemanticEntityAt(q, r);
             entityId > 0;
             entityId = world.NextSemanticEntityAt(entityId))
        {
            if (world.EntityIsActive(entityId) &&
                world.EntitySemanticQ(entityId) == q &&
                world.EntitySemanticR(entityId) == r)
            {
                return true;
            }
        }

        return false;
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

    private static void SetRef(global::System.Span<int> refCodes, global::System.Span<int> refEntityIds, ref int refCount, int refCode, int entityId)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refCodes[i] == refCode)
            {
                refEntityIds[i] = entityId;
                return;
            }
        }

        if (refCount >= refCodes.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        refCodes[refCount] = refCode;
        refEntityIds[refCount] = entityId;
        refCount++;
    }

    private static int ResolveRef(global::System.Span<int> refCodes, global::System.Span<int> refEntityIds, int refCount, int refCode)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refCodes[i] == refCode)
            {
                return refEntityIds[i];
            }
        }

        return 0;
    }

    private static bool RefAlreadyBound(global::System.Span<int> refEntityIds, int refCount, int entityId)
    {
        for (var i = 0; i < refCount; i++)
        {
            if (refEntityIds[i] == entityId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ScratchContains(global::System.Span<int> qs, global::System.Span<int> rs, int count, int q, int r)
    {
        for (var i = 0; i < count; i++)
        {
            if (qs[i] == q && rs[i] == r)
            {
                return true;
            }
        }

        return false;
    }

    private static void MarkCreatedEntitiesHistorical(WorldState world, global::System.Span<int> createdEntityIds, int createdEntityCount, int tick)
    {
        for (var i = 0; i < createdEntityCount; i++)
        {
            if (createdEntityIds[i] > 0 && world.EntityIsActive(createdEntityIds[i]))
            {
                world.SetEntityStatus(createdEntityIds[i], EntityStatusCodes.Historical, tick);
            }
        }
    }

    private void PublishDueGeneratorStats(Simulation simulation, int tick)
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_pulseStatusCodes[slot] != GeneratorStatusCodes.NotEvaluated)
            {
                WriteGeneratorEvaluated(simulation, tick, slot);
            }
        }
    }

    private static void WriteGeneratorEvaluated(Simulation simulation, int tick, int slot)
    {
        if (!simulation.Events.TryWrite(KernelEvent.From(new GeneratorEvaluatedKernelEvent(
            tick,
            simulation.WorldVersion,
            simulation.Generators._generatorIds[slot],
            simulation.Generators._pulseStatusCodes[slot],
            simulation.Generators._pulseSelectedTargetIds[slot],
            simulation.Generators._pulseCandidateCounts[slot],
            simulation.Generators._pulseFailedFireCounts[slot]))))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static void WriteGeneratorFired(
        Simulation simulation,
        int tick,
        int slot,
        int selectedTargetEntityId,
        int createdEntityCount,
        int createdRelationshipCount)
    {
        if (!simulation.Events.TryWrite(KernelEvent.From(new GeneratorFiredKernelEvent(
            tick,
            simulation.WorldVersion,
            simulation.Generators._generatorIds[slot],
            selectedTargetEntityId,
            createdEntityCount,
            createdRelationshipCount))))
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void AddSubtypeId(int subtypeId)
    {
        ValidatePositive(subtypeId);
        if (_subtypeCount + 1 >= _subtypeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _subtypeCount++;
        _subtypeIds[_subtypeCount] = subtypeId;
    }

    private void AddRelationshipKindFilterId(int relationshipKindId)
    {
        ValidatePositive(relationshipKindId);
        if (_relationshipKindFilterCount + 1 >= _relationshipKindFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _relationshipKindFilterCount++;
        _relationshipKindFilterIds[_relationshipKindFilterCount] = relationshipKindId;
    }

    private void AddTerrainFilterId(int terrainId)
    {
        ValidatePositive(terrainId);
        if (_terrainFilterCount + 1 >= _terrainFilterIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _terrainFilterCount++;
        _terrainFilterIds[_terrainFilterCount] = terrainId;
    }

    private void AddAttributeCondition(int attributeId, int directionCode, float threshold)
    {
        ValidatePositive(attributeId);
        ValidatePositive(directionCode);
        if (_attributeConditionCount + 1 >= _attributeConditionAttributeIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeConditionCount++;
        _attributeConditionAttributeIds[_attributeConditionCount] = attributeId;
        _attributeConditionDirectionCodes[_attributeConditionCount] = directionCode;
        _attributeConditionThresholds[_attributeConditionCount] = threshold;
    }

    private void EnsureOpenGenerator(int generatorSlot)
    {
        EnsureGenerator(generatorSlot);
        if (generatorSlot != _openGeneratorSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenBinding(int bindingSlot)
    {
        if ((uint)bindingSlot == 0u || bindingSlot > _bindingCount || bindingSlot != _openBindingSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureOpenProduced(int producedSlot)
    {
        if ((uint)producedSlot == 0u || producedSlot > _producedCount || producedSlot != _openProducedSlot)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private void EnsureGenerator(int generatorSlot)
    {
        if ((uint)generatorSlot == 0u || generatorSlot > _generatorCount)
        {
            throw new global::System.InvalidOperationException();
        }
    }

    private static int Abs(int value)
    {
        return value < 0 ? -value : value;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }

    private static void ValidateFactCount(int factCount)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(factCount);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(factCount, 4);
    }
}
