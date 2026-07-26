namespace Engine.Kernel;

using Engine.Constants;
using Engine.Eventing;

internal sealed partial class GeneratorProgramRuntime : IKernelProgramRuntime<GeneratorProgramData, GeneratorProgramState>
{
    private const int MaxResolvedReferences = 32;
    private const int MaxScratchCells = 4096;
    private const int SlotFillBaseFireWeightBonus = 8;
    private const int SlotFillAdditionalFireWeightBonus = 2;
    private const int SlotFillMaxFireWeightBonus = 16;
    private readonly GeneratorProgramScratch _scratch;

    // Fire-scoped plan state: produced entities resolved but not yet committed.
    // Valid only between PlanProducedEntities and CommitPlannedEntities within
    // one Fire call; pending refs are encoded as -(planIndex + 1).
    private int _plannedCreateCount;
    private int _plannedCoverageCellCount;
    private int[] _generatorIds => Data.GeneratorIds;
    private int[] _targetKindIds => Data.TargetKindIds;
    private int[] _fireWeights => Data.FireWeights;
    private FixedScalar[] _ageFireWeightMultipliers => Data.AgeFireWeightMultipliers;
    private int[] _cadenceTicks => Data.CadenceTicks;
    private int[] _cadenceJitterTicks => Data.CadenceJitterTicks;
    private int[] _targetRequiredTagBits => Data.TargetRequiredTagBits;
    private int[] _targetWithoutTagBits => Data.TargetWithoutTagBits;
    private int[] _bindingRequiredTagBits => Data.BindingRequiredTagBits;
    private int[] _bindingWithoutTagBits => Data.BindingWithoutTagBits;
    private int[] _bindingCultureIds => Data.BindingCultureIds;
    private int[] _bindingRelatedToTargetKindIds => Data.BindingRelatedToTargetKindIds;
    private int[] _bindingRelatedToTargetIncoming => Data.BindingRelatedToTargetIncoming;
    private FixedScalar[] _bindingRelatedToTargetMinStrengths => Data.BindingRelatedToTargetMinStrengths;
    private int[] _producedQueryRequiredTagBits => Data.ProducedQueryRequiredTagBits;
    private int[] _producedQueryWithoutTagBits => Data.ProducedQueryWithoutTagBits;
    private int[] _producedQueryCultureIds => Data.ProducedQueryCultureIds;
    private int[] _producedQueryRelatedToTargetKindIds => Data.ProducedQueryRelatedToTargetKindIds;
    private int[] _producedQueryRelatedToTargetIncoming => Data.ProducedQueryRelatedToTargetIncoming;
    private FixedScalar[] _producedQueryRelatedToTargetMinStrengths => Data.ProducedQueryRelatedToTargetMinStrengths;
    private int[] _nextCadenceFireTicks => State.NextCadenceFireTicks;
    private int[] _maxRuns => Data.MaxRuns;
    private int[] _runCounts => State.RunCounts;
    private bool[] _enabled => State.Enabled;
    private const int ProvinceBandDormant = 0;
    private const int ProvinceBandFrontier = 1;
    private const int ProvinceBandSettled = 2;
    private const int ProvinceBandHeartland = 3;

    // Fire-scoped province filter: while a province's window selects and
    // fires, only targets standing inside that superregion are eligible.
    // -1 means unrestricted (cadence generators stay world-wide).
    private int _fireProvinceSrId = -1;
    private int _lastProvinceRefreshTick = -1;
    private readonly int[] _provinceBandCodes;
    private readonly int[] _provinceCoveredCounts;
    private readonly int[] _provinceKnownCounts;
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
    private int[] _fireStatusCodes => _scratch.FireStatusCodes;
    private int[] _fireCandidateCounts => _scratch.FireCandidateCounts;
    private int[] _fireSelectionWeights => _scratch.FireSelectionWeights;
    private int[] _fireSelectedTargetOccurrenceHandles =>
        _scratch.FireSelectedTargetOccurrenceHandles;
    private int[] _fireFailedCounts => _scratch.FireFailedCounts;
    private int[] _subtypeIds => Data.SubtypeIds;
    private int[] _relationshipKindFilterIds => Data.RelationshipKindFilterIds;
    private FixedScalar[] _relationshipKindFilterMinimumStrengths => Data.RelationshipKindFilterMinimumStrengths;
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
    private int[] _bindingRequireTargetCultureRoute => Data.BindingRequireTargetCultureRoute;
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
    private int[] _producedFixedCultureIds => Data.ProducedFixedCultureIds;
    private int[] _producedCulturePlacementCodes => Data.ProducedCulturePlacementCodes;
    private int[] _producedFeatureKinds => Data.ProducedFeatureKinds;
    private FixedScalar[] _producedCreateChances => Data.ProducedCreateChances;
    private int[] _producedSubtypeChoiceStarts => Data.ProducedSubtypeChoiceStarts;
    private int[] _producedSubtypeChoiceCounts => Data.ProducedSubtypeChoiceCounts;
    private int[] _producedQuerySubtypeStarts => Data.ProducedQuerySubtypeStarts;
    private int[] _producedQuerySubtypeCounts => Data.ProducedQuerySubtypeCounts;
    private int[] _producedQueryAdjacentToTarget => Data.ProducedQueryAdjacentToTarget;
    private int[] _producedQueryCultureMatchCodes => Data.ProducedQueryCultureMatchCodes;
    private int[] _producedQueryRequireTargetCultureRoute => Data.ProducedQueryRequireTargetCultureRoute;
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
    private FixedScalar[] _relationshipStrengths => Data.RelationshipStrengths;
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
    private FixedScalar[] _effectValues => Data.EffectValues;
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
    private int[] _plannedProducedSlots => _scratch.PlannedProducedSlots;
    private int[] _plannedSubtypeIds => _scratch.PlannedSubtypeIds;
    private int[] _plannedCultureIds => _scratch.PlannedCultureIds;
    private int[] _plannedSpawnPhysicalQ => _scratch.PlannedSpawnPhysicalQ;
    private int[] _plannedSpawnPhysicalR => _scratch.PlannedSpawnPhysicalR;
    private int[] _plannedFinalPhysicalQ => _scratch.PlannedFinalPhysicalQ;
    private int[] _plannedFinalPhysicalR => _scratch.PlannedFinalPhysicalR;
    private int[] _plannedSemanticQ => _scratch.PlannedSemanticQ;
    private int[] _plannedSemanticR => _scratch.PlannedSemanticR;
    private int[] _plannedCoverageStarts => _scratch.PlannedCoverageStarts;
    private int[] _plannedCoverageCounts => _scratch.PlannedCoverageCounts;
    private int[] _plannedCoverageCellQ => _scratch.PlannedCoverageCellQ;
    private int[] _plannedCoverageCellR => _scratch.PlannedCoverageCellR;
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
        if (tuning.Generators.FireWindowTicks <= 0 || tuning.Generators.FiresPerWindow < 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(tuning));
        }

        Data = data;
        _provinceBandCodes = new int[state.ProvinceSrIds.Length];
        _provinceCoveredCounts = new int[state.ProvinceSrIds.Length];
        _provinceKnownCounts = new int[state.ProvinceSrIds.Length];
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
        int fireWeight,
        int maxRuns,
        int cadenceTicks = 0,
        int cadenceJitterTicks = 0,
        int targetRequiredTagBits = 0,
        int targetWithoutTagBits = 0)
    {
        ValidatePositive(generatorId);
        ValidatePositive(targetKindId);
        NumericGuard.ThrowIfNegative(fireWeight);
        NumericGuard.ThrowIfNegative(maxRuns);
        NumericGuard.ThrowIfNegative(cadenceTicks);
        if (cadenceJitterTicks < 0 || (cadenceTicks > 0 && cadenceJitterTicks >= cadenceTicks))
        {
            throw new global::System.InvalidOperationException();
        }
        if (_generatorCount + 1 >= _generatorIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _generatorCount++;
        var slot = _generatorCount;
        _generatorIds[slot] = generatorId;
        _targetKindIds[slot] = targetKindId;
        _fireWeights[slot] = fireWeight;
        _maxRuns[slot] = maxRuns;
        _cadenceTicks[slot] = Tuning.ScaleMainProgramDuration(cadenceTicks);
        _cadenceJitterTicks[slot] = Tuning.ScaleMainProgramDuration(cadenceJitterTicks);
        _targetRequiredTagBits[slot] = targetRequiredTagBits;
        _targetWithoutTagBits[slot] = targetWithoutTagBits;
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

    public void AddTargetRequiredOutgoingRelationship(int generatorSlot, int relationshipKindId, FixedScalar minimumStrength = default)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetRequiredOutgoingCounts[generatorSlot] == 0)
        {
            _targetRequiredOutgoingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilter(relationshipKindId, minimumStrength);
        _targetRequiredOutgoingCounts[generatorSlot]++;
    }

    public void AddTargetRequiredIncomingRelationship(int generatorSlot, int relationshipKindId, FixedScalar minimumStrength = default)
    {
        EnsureOpenGenerator(generatorSlot);
        if (_targetRequiredIncomingCounts[generatorSlot] == 0)
        {
            _targetRequiredIncomingStarts[generatorSlot] = _relationshipKindFilterCount + 1;
        }

        AddRelationshipKindFilter(relationshipKindId, minimumStrength);
        _targetRequiredIncomingCounts[generatorSlot]++;
    }

    /// <summary>
    /// Remembrance gates: the generator only targets entities the chronicle
    /// system vouches for - currently cast in a chronicle at or above the
    /// heat, or recently resolved out of a legendary one.
    /// </summary>
    public void SetTargetChronicleGates(int generatorSlot, FixedScalar heatMin, int legendaryWithinTicks)
    {
        EnsureGenerator(generatorSlot);
        Data.TargetChronicleHeatMin[generatorSlot] = heatMin;
        Data.TargetChronicleLegendaryWithinTicks[generatorSlot] = legendaryWithinTicks;
    }

    public void AddTargetRequiredRelationship(int generatorSlot, int relationshipKindId, FixedScalar minimumStrength = default)
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
        NumericGuard.ThrowIfNegative(radius);
        NumericGuard.ThrowIfNegativeOrZero(maximumExclusive);
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
        TickCadenceGenerators(simulation, tick);
        if (tick <= 0)
        {
            return;
        }

        RefreshProvincesIfDue(simulation, tick);
        for (var province = 1; province <= State.ProvinceCount; province++)
        {
            TickProvinceWindow(simulation, tick, province);
        }
    }

    // One fire window per settled place: the province's band stretches the
    // base window, so heartlands run at full story tempo, frontiers get
    // sparse events, and dormant ground stays silent. Target selection is
    // scoped to the province, so a crowded heartland can no longer starve
    // frontier events of the fire slot.
    private void TickProvinceWindow(Simulation simulation, int tick, int province)
    {
        var band = _provinceBandCodes[province];
        if (band == ProvinceBandDormant)
        {
            return;
        }

        var width = Tuning.ScaleMainProgramDuration(
            Tuning.Generators.FireWindowTicks * ProvinceWidthMultiplier(band));
        var windowIndex = (tick - 1) / width;
        if (State.ProvinceWindowIndices[province] != windowIndex)
        {
            State.ProvinceWindowIndices[province] = windowIndex;
            State.FiresInCurrentWindow[province] = 0;
        }

        if (State.FiresInCurrentWindow[province] >= Tuning.Generators.FiresPerWindow)
        {
            return;
        }

        _fireProvinceSrId = State.ProvinceSrIds[province];
        if (EvaluateGenerators(simulation, tick))
        {
            FireProvinceWindow(simulation, tick, province);
        }

        _fireProvinceSrId = -1;
    }

    private void FireProvinceWindow(Simulation simulation, int tick, int province)
    {
        while (TryChooseEligibleGenerator(simulation, province, out var selectedGeneratorSlot))
        {
            if (TryFireEligibleGenerator(simulation, selectedGeneratorSlot, tick))
            {
                State.FiresInCurrentWindow[province]++;
                return;
            }
        }

        PublishGeneratorEvaluationStats(simulation, tick);
    }

    private int ProvinceWidthMultiplier(int band) =>
        band switch
        {
            ProvinceBandFrontier => Tuning.Generators.FrontierFireWindowMultiplier,
            ProvinceBandSettled => Tuning.Generators.SettledFireWindowMultiplier,
            _ => 1,
        };

    // Registers every revealed physical superregion as a province and bands
    // it by how settled it is (covered cells within it). A full map sweep is
    // cheap at the refresh cadence, and recomputing bands from world state
    // keeps them out of the snapshot.
    private void RefreshProvincesIfDue(Simulation simulation, int tick)
    {
        var refreshTicks = Tuning.ScaleMainProgramDuration(
            Tuning.Generators.ProvinceBandRefreshTicks);
        if (_lastProvinceRefreshTick >= 0 && tick - _lastProvinceRefreshTick < refreshTicks)
        {
            return;
        }

        _lastProvinceRefreshTick = tick;
        for (var province = 1; province <= State.ProvinceCount; province++)
        {
            _provinceKnownCounts[province] = 0;
            _provinceCoveredCounts[province] = 0;
        }

        var world = simulation.World;
        var radius = world.MapRadius;
        for (var q = -radius; q <= radius; q++)
        {
            var minR = global::System.Math.Max(-radius, -q - radius);
            var maxR = global::System.Math.Min(radius, -q + radius);
            for (var r = minR; r <= maxR; r++)
            {
                if (!world.CellIsKnown(q, r))
                {
                    continue;
                }

                var province = ProvinceSlotFor(world.CellSuperregionId(q, r));
                _provinceKnownCounts[province]++;
                if (world.CoverageOwnerAt(q, r) != 0)
                {
                    _provinceCoveredCounts[province]++;
                }
            }
        }

        for (var province = 1; province <= State.ProvinceCount; province++)
        {
            _provinceBandCodes[province] = ProvinceBand(
                _provinceKnownCounts[province],
                _provinceCoveredCounts[province]);
        }
    }

    private int ProvinceBand(int knownCells, int coveredCells)
    {
        if (knownCells == 0)
        {
            return ProvinceBandDormant;
        }

        if (coveredCells >= Tuning.Generators.HeartlandCoveredCellThreshold)
        {
            return ProvinceBandHeartland;
        }

        return coveredCells >= Tuning.Generators.SettledCoveredCellThreshold
            ? ProvinceBandSettled
            : ProvinceBandFrontier;
    }

    private int ProvinceSlotFor(int superregionId)
    {
        for (var province = 1; province <= State.ProvinceCount; province++)
        {
            if (State.ProvinceSrIds[province] == superregionId)
            {
                return province;
            }
        }

        if (State.ProvinceCount + 1 >= State.ProvinceSrIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        State.ProvinceCount++;
        State.ProvinceSrIds[State.ProvinceCount] = superregionId;
        State.ProvinceWindowIndices[State.ProvinceCount] = -1;
        State.FiresInCurrentWindow[State.ProvinceCount] = 0;
        return State.ProvinceCount;
    }

    private bool TargetInsideActiveProvince(WorldState world, int entityId)
    {
        if (_fireProvinceSrId < 0)
        {
            return true;
        }

        return world.CellSuperregionId(
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId)) == _fireProvinceSrId;
    }

    private bool EvaluateGenerators(Simulation simulation, int tick)
    {
        var anyEvaluated = false;
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            ResetFireScratch(slot);
            if (!_enabled[slot] || tick <= 0 || _cadenceTicks[slot] > 0)
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
            _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
            return false;
        }

        _fireSelectedTargetOccurrenceHandles[selectedGeneratorSlot] =
            simulation.World.EntityOccurrenceHandle(targetId);
        if (Fire(simulation, selectedGeneratorSlot, targetId, tick, out var failureReasonCode))
        {
            _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
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
        _fireFailedCounts[selectedGeneratorSlot]++;
        _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
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
                _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
                return false;
            }

            _fireSelectedTargetOccurrenceHandles[selectedGeneratorSlot] =
                simulation.World.EntityOccurrenceHandle(targetId);
            if (Fire(simulation, selectedGeneratorSlot, targetId, tick, out var failureReasonCode))
            {
                _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.Fired;
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
            _fireFailedCounts[selectedGeneratorSlot]++;
            if (rejectedTargetCount >= _retryRejectedTargetIds.Length)
            {
                _fireStatusCodes[selectedGeneratorSlot] = GeneratorStatusCodes.FireFailed;
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
            if (_fireStatusCodes[slot] == GeneratorStatusCodes.Eligible)
            {
                _fireStatusCodes[slot] = GeneratorStatusCodes.EligibleNotSelected;
            }
        }
    }

    private void EvaluateGenerator(Simulation simulation, int slot)
    {
        if (_maxRuns[slot] > 0 && _runCounts[slot] >= _maxRuns[slot])
        {
            _fireStatusCodes[slot] = GeneratorStatusCodes.MaxRuns;
            return;
        }

        if (_fireWeights[slot] == 0)
        {
            _fireStatusCodes[slot] = GeneratorStatusCodes.ZeroWeight;
            return;
        }

        var (candidateCount, slotFillBonus, knowledgeWeight) = CountEligibleTargetsAndSlotFillBonus(simulation, slot);
        _fireCandidateCounts[slot] = candidateCount;
        var agedWeight = (int)FixedScalar.Max(0, _fireWeights[slot] * _ageFireWeightMultipliers[slot]);
        _fireSelectionWeights[slot] = candidateCount == 0
            ? 0
            : (int)FixedScalar.Max(1, (agedWeight + slotFillBonus) * (knowledgeWeight / candidateCount));
        _fireStatusCodes[slot] = candidateCount == 0
            ? GeneratorStatusCodes.Blocked
            : GeneratorStatusCodes.Eligible;
    }

    /// <summary>Age charges: neutralize, then apply once per world-beat run.</summary>
    public void ResetAgeFireWeightMultipliers()
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            _ageFireWeightMultipliers[slot] = 1;
        }
    }

    public void ApplyAgeFireWeightMultiplier(int generatorId, FixedScalar multiplier)
    {
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (generatorId == 0 || _generatorIds[slot] == generatorId)
            {
                _ageFireWeightMultipliers[slot] *= multiplier;
            }
        }
    }

    private bool TryChooseEligibleGenerator(
        Simulation simulation,
        int province,
        out int selectedGeneratorSlot)
    {
        selectedGeneratorSlot = 0;
        var totalWeight = 0;
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_fireStatusCodes[slot] == GeneratorStatusCodes.Eligible)
            {
                totalWeight += _fireSelectionWeights[slot];
            }
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        var roll = simulation.RandomInt(
            totalWeight,
            simulation.CurrentTick,
            _fireProvinceSrId,
            KernelRandomPurposeCodes.GeneratorFireSelection,
            attempt: State.ProvinceWindowIndices[province]);
        for (var slot = 1; slot <= _generatorCount; slot++)
        {
            if (_fireStatusCodes[slot] != GeneratorStatusCodes.Eligible)
            {
                continue;
            }

            if (roll < _fireSelectionWeights[slot])
            {
                selectedGeneratorSlot = slot;
                return true;
            }

            roll -= _fireSelectionWeights[slot];
        }

        return false;
    }

    private (int CandidateCount, int SlotFillBonus, FixedScalar KnowledgeWeight) CountEligibleTargetsAndSlotFillBonus(
        Simulation simulation,
        int generatorSlot)
    {
        var count = 0;
        var maxSlotFitCount = 0;
        var knowledgeWeight = FixedScalar.Zero;
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
                    knowledgeWeight += simulation.Knowledge.Modifier(
                        world.EntityCultureId(entityId),
                        KnowledgeGrantDomainCodes.GeneratorFireWeight,
                        _generatorIds[generatorSlot]);
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

        return (count, SlotFillFireWeightBonus(maxSlotFitCount), knowledgeWeight);
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
            if (_producedCreateIfMissing[produced] == 0 || _producedCreateChances[produced] <= 0)
            {
                continue;
            }

            var producedCultureId = ResolveProducedCultureId(world, refCodes, refEntityIds, refCount, produced);
            if (producedCultureId <= 0)
            {
                continue;
            }

            count += simulation.CultureSlots.UnoccupiedSlotFitCount(
                producedCultureId,
                _producedKindIds[produced]);
        }

        return count;
    }

    private static int SlotFillFireWeightBonus(int slotFitCount)
    {
        if (slotFitCount <= 0)
        {
            return 0;
        }

        return global::System.Math.Min(
            SlotFillMaxFireWeightBonus,
            SlotFillBaseFireWeightBonus + ((slotFitCount - 1) * SlotFillAdditionalFireWeightBonus));
    }

    private int SelectEligibleTarget(Simulation simulation, int generatorSlot)
    {
        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        var selectedTargetId = 0;
        var selectedPriority = FixedScalar.Zero;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                if (!TargetPasses(simulation, generatorSlot, entityId, refCodes, refEntityIds, out var refCount))
                {
                    return true;
                }

                var targetWeight = TargetSelectionWeight(simulation, generatorSlot, entityId, refCodes, refEntityIds, refCount);
                var priority = simulation.RandomScalar(
                    generatorSlot,
                    _generatorIds[generatorSlot],
                    KernelRandomPurposeCodes.GeneratorTargetSelection,
                    world.EntityOccurrenceHandle(entityId)) * targetWeight;
                if (selectedTargetId == 0 || priority > selectedPriority ||
                    (priority == selectedPriority && entityId < selectedTargetId))
                {
                    selectedPriority = priority;
                    selectedTargetId = entityId;
                }

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

        var world = simulation.World;
        var refCodes = _refCodes;
        var refEntityIds = _refEntityIds;
        var selectedTargetId = 0;
        var selectedPriority = FixedScalar.Zero;
        KernelQuery.ForEachActiveEntityOfKind(
            world,
            _targetKindIds[generatorSlot],
            entityId =>
            {
                if (RejectedTargetContains(entityId, rejectedTargetIds, rejectedTargetCount) ||
                    !TargetPasses(simulation, generatorSlot, entityId, refCodes, refEntityIds, out var refCount))
                {
                    return true;
                }

                var targetWeight = TargetSelectionWeight(simulation, generatorSlot, entityId, refCodes, refEntityIds, refCount);
                var priority = simulation.RandomScalar(
                    generatorSlot,
                    _generatorIds[generatorSlot],
                    KernelRandomPurposeCodes.GeneratorTargetSelection,
                    world.EntityOccurrenceHandle(entityId)) * targetWeight;
                if (selectedTargetId == 0 || priority > selectedPriority ||
                    (priority == selectedPriority && entityId < selectedTargetId))
                {
                    selectedPriority = priority;
                    selectedTargetId = entityId;
                }

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
