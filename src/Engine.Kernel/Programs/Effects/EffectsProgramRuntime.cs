namespace Engine.Kernel;

internal sealed class EffectsProgramRuntime :
    IKernelProgramRuntime<EffectsProgramData, EffectsProgramState>,
    IKernelParallelJob
{
    private const int BufferedMutationsPerEntity = 4;

    private readonly KernelMutationBuffer _parallelMutations;
    private readonly int[] _parallelRangeStarts;
    private readonly int[] _parallelRangeEnds;
    private Simulation? _parallelSimulation;
    private int[] _attributeDeltaKindIds => Data.AttributeDeltaKindIds;
    private int[] _attributeDeltaAttributeIds => Data.AttributeDeltaAttributeIds;
    private int[] _attributeDeltaConditionCodes => Data.AttributeDeltaConditionCodes;
    private int[] _attributeDeltaRelationshipKindIds => Data.AttributeDeltaRelationshipKindIds;
    private int[] _attributeDeltaRelationshipDirectionCodes => Data.AttributeDeltaRelationshipDirectionCodes;
    private int[] _attributeDeltaPlaneCodes => Data.AttributeDeltaPlaneCodes;
    private int[] _attributeDeltaMaxDistances => Data.AttributeDeltaMaxDistances;
    private float[] _attributeDeltaThresholds => Data.AttributeDeltaThresholds;
    private float[] _attributeDeltaMinimumRelationshipStrengths => Data.AttributeDeltaMinimumRelationshipStrengths;
    private float[] _attributeDeltaDeltas => Data.AttributeDeltaDeltas;
    private int[] _attributeDeltaRequiredTargetTraitIds => Data.AttributeDeltaRequiredTargetTraitIds;
    private int[] _attributeDeltaRequiredRelatedTraitIds => Data.AttributeDeltaRequiredRelatedTraitIds;
    private int[] _attributeDeltaCultureIds => Data.AttributeDeltaCultureIds;
    private int[] _attributeDeltaCultureSlotIds => Data.AttributeDeltaCultureSlotIds;
    private int[] _attributeDeltaCultureSlotPolarityCodes => Data.AttributeDeltaCultureSlotPolarityCodes;
    private ref int _attributeDeltaRuleCount => ref Data.AttributeDeltaRuleCount;
    private float[] _attributeDeltaSupportScales => Data.AttributeDeltaSupportScales;
    private int[] _attributeDeltaCoverageOwnerKindIds => Data.AttributeDeltaCoverageOwnerKindIds;
    private int[] _attributeDeltaCoverageOwnerSubtypeIds => Data.AttributeDeltaCoverageOwnerSubtypeIds;
    private int[] _tagRuleCoverageOwnerKindIds => Data.TagRuleCoverageOwnerKindIds;
    private int[] _tagRuleCoverageOwnerSubtypeIds => Data.TagRuleCoverageOwnerSubtypeIds;
    private int[] _tagRuleCoveragePlaneCodes => Data.TagRuleCoveragePlaneCodes;
    private int[] _tagRuleKindIds => Data.TagRuleKindIds;
    private int[] _tagRuleTagIds => Data.TagRuleTagIds;
    private int[] _tagRuleRelationshipKindIds => Data.TagRuleRelationshipKindIds;
    private int[] _tagRuleDirectionCodes => Data.TagRuleDirectionCodes;
    private float[] _tagRuleSetThresholds => Data.TagRuleSetThresholds;
    private float[] _tagRuleClearThresholds => Data.TagRuleClearThresholds;
    private ref int _tagRuleCount => ref Data.TagRuleCount;

    public EffectsProgramRuntime(EffectsProgramData data, EffectsProgramState state)
        : this(
            data,
            state,
            entityCapacity: 16384,
            parallelWorkerCount: 1)
    {
    }

    internal EffectsProgramRuntime(
        EffectsProgramData data,
        EffectsProgramState state,
        int entityCapacity,
        int parallelWorkerCount)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(entityCapacity, 1);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(parallelWorkerCount, 1);
        global::System.ArgumentOutOfRangeException.ThrowIfGreaterThan(
            parallelWorkerCount,
            SimulationOptions.MaximumParallelWorkerCount);
        Data = data;
        State = state;
        var entitiesPerWorker = (entityCapacity + parallelWorkerCount - 1) / parallelWorkerCount;
        _parallelMutations = new KernelMutationBuffer(
            parallelWorkerCount,
            checked((entitiesPerWorker * BufferedMutationsPerEntity) + 16));
        _parallelRangeStarts = new int[parallelWorkerCount];
        _parallelRangeEnds = new int[parallelWorkerCount];
    }

    public EffectsProgramData Data { get; }

    public EffectsProgramState State { get; }

    public int AttributeDeltaRuleCount => _attributeDeltaRuleCount;

    public int RegisterAttributeDeltaEffect(
        int kindId,
        int attributeId,
        int conditionCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        int planeCode,
        int maxDistance,
        float threshold,
        float minimumRelationshipStrength,
        float deltaPerTick,
        int requiredTargetTraitId = 0,
        int requiredRelatedTraitId = 0,
        int cultureId = 0,
        int cultureSlotId = 0,
        int cultureSlotPolarityCode = -1,
        float supportScale = 0f,
        int coverageOwnerKindId = 0,
        int coverageOwnerSubtypeId = 0)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        ValidatePositive(conditionCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        ValidatePositive(planeCode);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(maxDistance);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(minimumRelationshipStrength);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(requiredTargetTraitId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(requiredRelatedTraitId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cultureId);
        global::System.ArgumentOutOfRangeException.ThrowIfNegative(cultureSlotId);
        global::System.ArgumentOutOfRangeException.ThrowIfLessThan(cultureSlotPolarityCode, -1);
        if (_attributeDeltaRuleCount + 1 >= _attributeDeltaKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeDeltaRuleCount++;
        var slot = _attributeDeltaRuleCount;
        _attributeDeltaKindIds[slot] = kindId;
        _attributeDeltaAttributeIds[slot] = attributeId;
        _attributeDeltaConditionCodes[slot] = conditionCode;
        _attributeDeltaRelationshipKindIds[slot] = relationshipKindId;
        _attributeDeltaRelationshipDirectionCodes[slot] = relationshipDirectionCode;
        _attributeDeltaPlaneCodes[slot] = planeCode;
        _attributeDeltaMaxDistances[slot] = maxDistance;
        _attributeDeltaThresholds[slot] = threshold;
        _attributeDeltaMinimumRelationshipStrengths[slot] = minimumRelationshipStrength;
        _attributeDeltaDeltas[slot] = deltaPerTick;
        _attributeDeltaRequiredTargetTraitIds[slot] = requiredTargetTraitId;
        _attributeDeltaRequiredRelatedTraitIds[slot] = requiredRelatedTraitId;
        _attributeDeltaCultureIds[slot] = cultureId;
        _attributeDeltaCultureSlotIds[slot] = cultureSlotId;
        _attributeDeltaCultureSlotPolarityCodes[slot] = cultureSlotPolarityCode;
        _attributeDeltaSupportScales[slot] = supportScale;
        _attributeDeltaCoverageOwnerKindIds[slot] = coverageOwnerKindId;
        _attributeDeltaCoverageOwnerSubtypeIds[slot] = coverageOwnerSubtypeId;
        return slot;
    }

    public int RegisterRelationshipTagRule(
        int kindId,
        int tagId,
        int relationshipKindId,
        int relationshipDirectionCode,
        float setAtOrAbove,
        float clearBelow,
        int insideCoverageOwnerKindId = 0,
        int insideCoverageOwnerSubtypeId = 0,
        int insideCoveragePlaneCode = PlaneCodes.Semantic)
    {
        ValidatePositive(kindId);
        ValidatePositive(tagId);
        ValidatePositive(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        if (clearBelow > setAtOrAbove || _tagRuleCount + 1 >= _tagRuleKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _tagRuleCount++;
        var slot = _tagRuleCount;
        _tagRuleKindIds[slot] = kindId;
        _tagRuleTagIds[slot] = tagId;
        _tagRuleRelationshipKindIds[slot] = relationshipKindId;
        _tagRuleDirectionCodes[slot] = relationshipDirectionCode;
        _tagRuleSetThresholds[slot] = setAtOrAbove;
        _tagRuleClearThresholds[slot] = clearBelow;
        _tagRuleCoverageOwnerKindIds[slot] = insideCoverageOwnerKindId;
        _tagRuleCoverageOwnerSubtypeIds[slot] = insideCoverageOwnerSubtypeId;
        _tagRuleCoveragePlaneCodes[slot] = insideCoveragePlaneCode;
        return slot;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        _parallelMutations.Reset();
        global::System.Array.Clear(_parallelRangeStarts);
        global::System.Array.Clear(_parallelRangeEnds);
        _parallelSimulation = simulation;
        simulation.WorkScheduler.Run(
            this,
            startInclusive: 1,
            endExclusive: KernelQuery.EntityRangeEndExclusive(simulation.World),
            simulation.Options.ParallelMinimumChunkSize);
        _parallelSimulation = null;
        CommitParallelMutations(simulation, tick);
    }

    void IKernelParallelJob.Execute(int workerIndex, int startInclusive, int endExclusive)
    {
        var simulation = _parallelSimulation ?? throw new global::System.InvalidOperationException();
        _parallelRangeStarts[workerIndex] = startInclusive;
        _parallelRangeEnds[workerIndex] = endExclusive;
        var world = simulation.World;
        for (var entityId = startInclusive; entityId < endExclusive; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            var kindId = world.EntityKindId(entityId);
            EvaluateAttributeDeltaEffectsForKind(simulation, entityId, kindId, workerIndex);
            if (_parallelMutations.Overflowed(workerIndex))
            {
                return;
            }

            EvaluateTagRulesForKind(simulation, entityId, kindId, workerIndex);
            if (_parallelMutations.Overflowed(workerIndex))
            {
                return;
            }
        }
    }

    private void CommitParallelMutations(Simulation simulation, int tick)
    {
        for (var workerIndex = 0; workerIndex < _parallelMutations.WorkerCount; workerIndex++)
        {
            var rangeStart = _parallelRangeStarts[workerIndex];
            var rangeEnd = _parallelRangeEnds[workerIndex];
            if (rangeStart == 0 || rangeEnd <= rangeStart)
            {
                continue;
            }

            if (_parallelMutations.Overflowed(workerIndex))
            {
                ApplyEffectsSequential(simulation, tick, rangeStart, rangeEnd);
                continue;
            }

            for (var index = 0; index < _parallelMutations.Count(workerIndex); index++)
            {
                ApplyMutation(simulation, tick, _parallelMutations.At(workerIndex, index));
            }
        }
    }

    private static void ApplyMutation(Simulation simulation, int tick, in KernelMutation mutation)
    {
        if (mutation.Code == KernelMutationCodes.ModifyAttribute)
        {
            KernelEffectExecutor.ModifyAttribute(
                simulation,
                mutation.SubjectId,
                mutation.ValueId,
                mutation.FloatValue);
            return;
        }

        if (mutation.Code == KernelMutationCodes.SetEntityTag)
        {
            _ = simulation.SetEntityTag(
                mutation.SubjectId,
                mutation.ValueId,
                mutation.IntValue != 0,
                tick);
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private void ApplyEffectsSequential(Simulation simulation, int tick, int startInclusive, int endExclusive)
    {
        var world = simulation.World;
        for (var entityId = startInclusive; entityId < endExclusive; entityId++)
        {
            if (!world.EntityIsActive(entityId))
            {
                continue;
            }

            var kindId = world.EntityKindId(entityId);
            ApplyAttributeDeltaEffectsForKind(simulation, entityId, tick, kindId);
            ApplyTagRulesForKind(simulation, entityId, tick, kindId);
        }
    }

    private void EvaluateTagRulesForKind(
        Simulation simulation,
        int entityId,
        int kindId,
        int workerIndex)
    {
        for (var rule = 1; rule <= _tagRuleCount; rule++)
        {
            if (_tagRuleKindIds[rule] != kindId)
            {
                continue;
            }

            var insideJurisdiction = InsideJurisdiction(
                simulation.World,
                entityId,
                _tagRuleCoverageOwnerKindIds[rule],
                _tagRuleCoverageOwnerSubtypeIds[rule],
                _tagRuleCoveragePlaneCodes[rule]);
            var sum = insideJurisdiction
                ? SummedRelationshipStrength(
                    simulation.World,
                    entityId,
                    _tagRuleRelationshipKindIds[rule],
                    _tagRuleDirectionCodes[rule])
                : 0f;
            var assignment = -1;
            if (sum >= _tagRuleSetThresholds[rule])
            {
                assignment = 1;
            }
            else if (sum < _tagRuleClearThresholds[rule])
            {
                assignment = 0;
            }
            if (assignment < 0)
            {
                continue;
            }

            var mutation = new KernelMutation(
                KernelMutationCodes.SetEntityTag,
                entityId,
                _tagRuleTagIds[rule],
                assignment,
                0f);
            if (!_parallelMutations.TryAppend(workerIndex, mutation))
            {
                return;
            }
        }
    }

    private void ApplyTagRulesForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _tagRuleCount; rule++)
        {
            if (_tagRuleKindIds[rule] != kindId)
            {
                continue;
            }

            var insideJurisdiction = InsideJurisdiction(
                simulation.World,
                entityId,
                _tagRuleCoverageOwnerKindIds[rule],
                _tagRuleCoverageOwnerSubtypeIds[rule],
                _tagRuleCoveragePlaneCodes[rule]);
            var sum = insideJurisdiction
                ? SummedRelationshipStrength(
                    simulation.World,
                    entityId,
                    _tagRuleRelationshipKindIds[rule],
                    _tagRuleDirectionCodes[rule])
                : 0f;
            if (sum >= _tagRuleSetThresholds[rule])
            {
                _ = simulation.SetEntityTag(entityId, _tagRuleTagIds[rule], assigned: true, tick);
            }
            else if (sum < _tagRuleClearThresholds[rule])
            {
                _ = simulation.SetEntityTag(entityId, _tagRuleTagIds[rule], assigned: false, tick);
            }
        }
    }

    private static float SummedRelationshipStrength(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int directionCode)
    {
        var sum = 0f;
        if (directionCode is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            sum += SummedRelationshipStrengthForDirection(
                world,
                entityId,
                relationshipKindId,
                outgoing: true);
        }

        if (directionCode is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            sum += SummedRelationshipStrengthForDirection(
                world,
                entityId,
                relationshipKindId,
                outgoing: false);
        }

        return sum;
    }

    private static float SummedRelationshipStrengthForDirection(
        WorldState world,
        int entityId,
        int relationshipKindId,
        bool outgoing)
    {
        var sum = 0f;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKind(entityId, relationshipKindId)
            : world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
        while (KernelQuery.MoveNextRelatedEndpoint(
            world,
            ref cursor,
            outgoing,
            out var relationshipId,
            out _))
        {
            sum += KernelQuery.RelationshipStrength(world, relationshipId);
        }

        return sum;
    }

    private void EvaluateAttributeDeltaEffectsForKind(
        Simulation simulation,
        int entityId,
        int kindId,
        int workerIndex)
    {
        for (var rule = 1; rule <= _attributeDeltaRuleCount; rule++)
        {
            if (_attributeDeltaKindIds[rule] != kindId ||
                !KernelQuery.TraitFilterPasses(simulation.World, entityId, _attributeDeltaRequiredTargetTraitIds[rule]) ||
                !RuleCultureGatePasses(simulation.World, entityId, rule))
            {
                continue;
            }

            var evidence = EffectEvidence(simulation, entityId, rule);
            if (evidence <= 0f)
            {
                continue;
            }

            var mutation = new KernelMutation(
                KernelMutationCodes.ModifyAttribute,
                entityId,
                _attributeDeltaAttributeIds[rule],
                0,
                _attributeDeltaDeltas[rule] * evidence);
            if (!_parallelMutations.TryAppend(workerIndex, mutation))
            {
                return;
            }
        }
    }

    private void ApplyAttributeDeltaEffectsForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = 1; rule <= _attributeDeltaRuleCount; rule++)
        {
            if (_attributeDeltaKindIds[rule] != kindId)
            {
                continue;
            }

            if (!KernelQuery.TraitFilterPasses(simulation.World, entityId, _attributeDeltaRequiredTargetTraitIds[rule]))
            {
                continue;
            }

            if (!RuleCultureGatePasses(simulation.World, entityId, rule))
            {
                continue;
            }

            var evidence = EffectEvidence(simulation, entityId, rule);
            if (evidence <= 0f)
            {
                continue;
            }

            KernelEffectExecutor.ModifyAttribute(
                simulation,
                entityId,
                _attributeDeltaAttributeIds[rule],
                _attributeDeltaDeltas[rule] * evidence);
        }
    }

    // Rules with an authored culture apply only to entities of that culture
    // (the culture-slot condition manages its own culture semantics).
    private bool RuleCultureGatePasses(WorldState world, int entityId, int rule) =>
        _attributeDeltaCultureIds[rule] <= 0 ||
        _attributeDeltaConditionCodes[rule] == EffectConditionCodes.CultureSlotOccupancy ||
        world.EntityCultureId(entityId) == _attributeDeltaCultureIds[rule];

    private float EffectEvidence(Simulation simulation, int entityId, int rule)
    {
        var conditionCode = _attributeDeltaConditionCodes[rule];
        if (conditionCode == EffectConditionCodes.RelationshipMapValueAtLocation)
        {
            return RelatedInfluenceEvidence(simulation.World, entityId, rule);
        }

        if (conditionCode == EffectConditionCodes.RelatedEntityDistance)
        {
            return RelatedDistanceEvidence(simulation.World, entityId, rule);
        }

        if (conditionCode is EffectConditionCodes.LocalPositiveSignal
            or EffectConditionCodes.LocalNegativeSignal
            or EffectConditionCodes.LocalCrowdingLoadSignal
            or EffectConditionCodes.LocalComfortSignal)
        {
            return SignalEvidence(simulation, entityId, rule, conditionCode);
        }

        if (conditionCode == EffectConditionCodes.LocalCultureMapValue)
        {
            return OwnCultureInfluenceEvidence(simulation.World, entityId, rule);
        }

        return SpatialEvidence(simulation, entityId, rule, conditionCode);
    }

    // Violation as a state: the tag only holds while the entity stands
    // inside coverage owned by a living entity of the declared kind.
    private static bool InsideJurisdiction(
        WorldState world,
        int entityId,
        int ownerKindId,
        int ownerSubtypeId,
        int planeCode)
    {
        if (ownerKindId <= 0)
        {
            return true;
        }

        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var ownerId = world.CoverageOwnerAt(planeCode, q, r);
        return ownerId > 0 &&
            world.EntityIsActive(ownerId) &&
            world.EntityKindId(ownerId) == ownerKindId &&
            (ownerSubtypeId == 0 || world.EntitySubtypeId(ownerId) == ownerSubtypeId);
    }

    private float InsideOwnedCoverageEvidence(WorldState world, int entityId, int rule)
    {
        return InsideJurisdiction(
            world,
            entityId,
            _attributeDeltaCoverageOwnerKindIds[rule],
            _attributeDeltaCoverageOwnerSubtypeIds[rule],
            _attributeDeltaPlaneCodes[rule])
            ? 1f
            : 0f;
    }

    // Local carrying capacity: crowding strain discounted by own-culture
    // mass support. Asymptotic (scale / (scale + mass)), never a hard cap:
    // growing the territory's culture mass is the built-in escape, and later
    // mechanisms may raise support the same way age can be extended.
    private float CrowdingBeyondCultureSupportEvidence(Simulation simulation, int entityId, int rule)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var crowding = KernelQuery.ThresholdEvidence(
            simulation.MapSignals.EntityCrowdingLoad(entityId, planeCode),
            _attributeDeltaThresholds[rule]);
        if (crowding <= 0f)
        {
            return 0f;
        }

        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        var supportScale = _attributeDeltaSupportScales[rule];
        if (cultureId <= 0 || supportScale <= 0f)
        {
            return crowding;
        }

        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var mass = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        return crowding * (supportScale / (supportScale + mass));
    }

    private float SpatialEvidence(Simulation simulation, int entityId, int rule, int conditionCode) =>
        conditionCode switch
        {
            EffectConditionCodes.CrowdingBeyondCultureSupport => CrowdingBeyondCultureSupportEvidence(simulation, entityId, rule),
            EffectConditionCodes.InsideOwnedCoverage => InsideOwnedCoverageEvidence(simulation.World, entityId, rule),
            EffectConditionCodes.CultureSlotOccupancy => CultureSlotOccupancyEvidence(simulation, entityId, rule),
            _ => 0f,
        };

    private float SignalEvidence(Simulation simulation, int entityId, int rule, int conditionCode)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var value = conditionCode switch
        {
            EffectConditionCodes.LocalPositiveSignal => simulation.MapSignals.EntitySupport(entityId, planeCode),
            EffectConditionCodes.LocalNegativeSignal => simulation.MapSignals.EntityThreat(entityId, planeCode),
            EffectConditionCodes.LocalCrowdingLoadSignal => simulation.MapSignals.EntityCrowdingLoad(entityId, planeCode),
            _ => simulation.MapSignals.EntityCrowdingComfort(entityId, planeCode),
        };
        return KernelQuery.ThresholdEvidence(value, _attributeDeltaThresholds[rule]);
    }

    private float OwnCultureInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0f;
        }

        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        return KernelQuery.ThresholdEvidence(
            world.GetCellCultureInfluence(cultureId, planeCode, q, r),
            _attributeDeltaThresholds[rule]);
    }

    private float CultureSlotOccupancyEvidence(Simulation simulation, int entityId, int rule)
    {
        return simulation.CultureSlots.OccupancyEvidence(
            entityId,
            _attributeDeltaCultureIds[rule],
            _attributeDeltaCultureSlotIds[rule],
            _attributeDeltaCultureSlotPolarityCodes[rule],
            _attributeDeltaThresholds[rule]);
    }

    private float RelatedInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = 0f;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1f)
        {
            return 1f;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private float RelatedInfluenceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var total = 0f;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        while (KernelQuery.MoveNextRelatedEndpoint(
            world,
            ref cursor,
            outgoing,
            out var relationshipId,
            out var otherEntityId))
        {
            if (!world.EntityIsActive(otherEntityId) ||
                !KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
            {
                continue;
            }

            var otherCultureId = world.EntityCultureId(otherEntityId);
            var influence = otherCultureId <= 0
                ? 0f
                : world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            if (influence <= 0f)
            {
                continue;
            }

            total += KernelQuery.ThresholdEvidence(influence, _attributeDeltaThresholds[rule]) *
                KernelQuery.RelationshipStrength(world, relationshipId);
            if (total >= 1f)
            {
                break;
            }
        }

        return total;
    }

    private float RelatedDistanceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = 0f;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1f)
        {
            return 1f;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private float RelatedDistanceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var total = 0f;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        while (KernelQuery.MoveNextRelatedEndpoint(
            world,
            ref cursor,
            outgoing,
            out var relationshipId,
            out var otherEntityId))
        {
            if (!world.EntityIsActive(otherEntityId) ||
                !KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
            {
                continue;
            }

            var distance = KernelQuery.PlaneDistance(world, entityId, otherEntityId, _attributeDeltaPlaneCodes[rule]);
            var evidence = KernelQuery.DistanceEvidence(distance, _attributeDeltaMaxDistances[rule]) *
                KernelQuery.RelationshipStrength(world, relationshipId);
            total += evidence;
            if (total >= 1f)
            {
                break;
            }
        }

        return total;
    }

    private static void ValidatePositive(int value)
    {
        global::System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
    }
}
