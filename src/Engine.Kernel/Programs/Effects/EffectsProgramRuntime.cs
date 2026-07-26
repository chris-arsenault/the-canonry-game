namespace Engine.Kernel;

using Engine.Constants;

internal sealed class EffectsProgramRuntime : IKernelProgramRuntime<EffectsProgramData, EffectsProgramState>
{
    private int[] _attributeDeltaKindIds => Data.AttributeDeltaKindIds;
    private int[] _attributeDeltaAttributeIds => Data.AttributeDeltaAttributeIds;
    private int[] _attributeDeltaConditionCodes => Data.AttributeDeltaConditionCodes;
    private int[] _attributeDeltaRelationshipKindIds => Data.AttributeDeltaRelationshipKindIds;
    private int[] _attributeDeltaRelationshipDirectionCodes => Data.AttributeDeltaRelationshipDirectionCodes;
    private int[] _attributeDeltaPlaneCodes => Data.AttributeDeltaPlaneCodes;
    private int[] _attributeDeltaMaxDistances => Data.AttributeDeltaMaxDistances;
    private FixedScalar[] _attributeDeltaThresholds => Data.AttributeDeltaThresholds;
    private FixedScalar[] _attributeDeltaMinimumRelationshipStrengths => Data.AttributeDeltaMinimumRelationshipStrengths;
    private FixedScalar[] _attributeDeltaDeltas => Data.AttributeDeltaDeltas;
    private int[] _attributeDeltaRequiredTargetTraitIds => Data.AttributeDeltaRequiredTargetTraitIds;
    private int[] _attributeDeltaRequiredRelatedTraitIds => Data.AttributeDeltaRequiredRelatedTraitIds;
    private int[] _attributeDeltaCultureIds => Data.AttributeDeltaCultureIds;
    private int[] _attributeDeltaCultureSlotIds => Data.AttributeDeltaCultureSlotIds;
    private int[] _attributeDeltaCultureSlotPolarityCodes => Data.AttributeDeltaCultureSlotPolarityCodes;
    private ref int _attributeDeltaRuleCount => ref Data.AttributeDeltaRuleCount;
    private FixedScalar[] _attributeDeltaSupportScales => Data.AttributeDeltaSupportScales;
    private int[] _attributeDeltaCoverageOwnerKindIds => Data.AttributeDeltaCoverageOwnerKindIds;
    private int[] _attributeDeltaCoverageOwnerSubtypeIds => Data.AttributeDeltaCoverageOwnerSubtypeIds;
    private int[] _tagRuleCoverageOwnerKindIds => Data.TagRuleCoverageOwnerKindIds;
    private int[] _tagRuleCoverageOwnerSubtypeIds => Data.TagRuleCoverageOwnerSubtypeIds;
    private int[] _tagRuleCoveragePlaneCodes => Data.TagRuleCoveragePlaneCodes;
    private int[] _tagRuleKindIds => Data.TagRuleKindIds;
    private int[] _tagRuleTagIds => Data.TagRuleTagIds;
    private int[] _tagRuleRelationshipKindIds => Data.TagRuleRelationshipKindIds;
    private int[] _tagRuleDirectionCodes => Data.TagRuleDirectionCodes;
    private FixedScalar[] _tagRuleSetThresholds => Data.TagRuleSetThresholds;
    private FixedScalar[] _tagRuleClearThresholds => Data.TagRuleClearThresholds;
    private ref int _tagRuleCount => ref Data.TagRuleCount;
    private readonly int[] _attributeDeltaScratchIds;
    private readonly long[] _attributeDeltaScratchValues;

    public EffectsProgramRuntime(EffectsProgramData data, EffectsProgramState state)
        : this(data, state, new KernelProgramTuningOptions())
    {
    }

    public EffectsProgramRuntime(
        EffectsProgramData data,
        EffectsProgramState state,
        KernelProgramTuningOptions tuning)
    {
        global::System.ArgumentNullException.ThrowIfNull(data);
        global::System.ArgumentNullException.ThrowIfNull(state);
        global::System.ArgumentNullException.ThrowIfNull(tuning);
        Data = data;
        State = state;
        Tuning = tuning;
        _attributeDeltaScratchIds = new int[
            data.AttributeDeltaAttributeIds.Length +
            data.LifecycleAttributeIds.Length];
        _attributeDeltaScratchValues = new long[_attributeDeltaScratchIds.Length];
    }

    public EffectsProgramData Data { get; }

    public EffectsProgramState State { get; }

    public KernelProgramTuningOptions Tuning { get; }

    public int AttributeDeltaRuleCount => _attributeDeltaRuleCount;

    public int LifecycleRuleCount => Data.LifecycleRuleCount;

    public int RegisterAttributeDeltaEffect(
        int kindId,
        int attributeId,
        int conditionCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        int planeCode,
        int maxDistance,
        FixedScalar threshold,
        FixedScalar minimumRelationshipStrength,
        FixedScalar deltaPerTick,
        int requiredTargetTraitId = 0,
        int requiredRelatedTraitId = 0,
        int cultureId = 0,
        int cultureSlotId = 0,
        int cultureSlotPolarityCode = -1,
        FixedScalar supportScale = default,
        int coverageOwnerKindId = 0,
        int coverageOwnerSubtypeId = 0)
    {
        ValidatePositive(kindId);
        ValidatePositive(attributeId);
        ValidatePositive(conditionCode);
        NumericGuard.ThrowIfNegative(relationshipKindId);
        ValidatePositive(relationshipDirectionCode);
        ValidatePositive(planeCode);
        NumericGuard.ThrowIfNegative(maxDistance);
        NumericGuard.ThrowIfNegative(threshold);
        NumericGuard.ThrowIfNegative(minimumRelationshipStrength);
        NumericGuard.ThrowIfNegative(requiredTargetTraitId);
        NumericGuard.ThrowIfNegative(requiredRelatedTraitId);
        NumericGuard.ThrowIfNegative(cultureId);
        NumericGuard.ThrowIfNegative(cultureSlotId);
        NumericGuard.ThrowIfLessThan(cultureSlotPolarityCode, -1);
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
        AppendRuleByKind(
            kindId,
            slot,
            Data.FirstAttributeDeltaRuleByKind,
            Data.LastAttributeDeltaRuleByKind,
            Data.NextAttributeDeltaRuleByKind);
        return slot;
    }

    public int RegisterRelationshipTagRule(
        int kindId,
        int tagId,
        int relationshipKindId,
        int relationshipDirectionCode,
        FixedScalar setAtOrAbove,
        FixedScalar clearBelow,
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
        AppendRuleByKind(
            kindId,
            slot,
            Data.FirstTagRuleByKind,
            Data.LastTagRuleByKind,
            Data.NextTagRuleByKind);
        return slot;
    }

    public int RegisterLifecycle(
        int kindId,
        int healthAttributeId,
        int targetTicks,
        int nominalHealthUnits,
        FixedScalar flatDrainShare,
        FixedScalar? variance = null)
    {
        var compiledVariance = variance ?? FixedScalar.FromUnits(200_000L);
        ValidatePositive(kindId);
        ValidatePositive(healthAttributeId);
        ValidatePositive(targetTicks);
        ValidatePositive(nominalHealthUnits);
        if (flatDrainShare < 0 || flatDrainShare > 1)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(flatDrainShare));
        }

        NumericGuard.ThrowIfNegative(compiledVariance);
        if ((uint)kindId >= (uint)Data.LifecycleRuleByKind.Length ||
            Data.LifecycleRuleByKind[kindId] != 0 ||
            Data.LifecycleRuleCount + 1 >= Data.LifecycleKindIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var slot = ++Data.LifecycleRuleCount;
        Data.LifecycleKindIds[slot] = kindId;
        Data.LifecycleAttributeIds[slot] = healthAttributeId;
        Data.LifecycleTargetTicks[slot] = targetTicks;
        Data.LifecycleNominalHealthUnits[slot] = nominalHealthUnits;
        Data.LifecycleFlatDrainShares[slot] = flatDrainShare;
        Data.LifecycleVariances[slot] = compiledVariance;
        Data.LifecycleRuleByKind[kindId] = slot;
        return slot;
    }

    public int LifecycleAttributeId(int kindId)
    {
        var rule = LifecycleRuleForKind(kindId);
        return rule > 0 ? Data.LifecycleAttributeIds[rule] : 0;
    }

    public bool TryGetLifecycleMaintenanceNeed(
        Simulation simulation,
        int entityId,
        int tick,
        out int attributeId,
        out FixedScalar ageDrainComponent,
        out FixedScalar healthShortfall)
    {
        attributeId = 0;
        ageDrainComponent = 0;
        healthShortfall = 0;
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        if (entityId <= 0 || !world.EntityIsActive(entityId))
        {
            return false;
        }

        var rule = LifecycleRuleForKind(world.EntityKindId(entityId));
        if (rule == 0)
        {
            return false;
        }

        attributeId = Data.LifecycleAttributeIds[rule];
        var age = global::System.Math.Max(0, tick - world.EntityCreatedAt(entityId));
        var multiplier = StageLifespanMultiplier(simulation, entityId, world.EntityKindId(entityId));
        ageDrainComponent = LifecycleDrainComponent(
            rule,
            age,
            multiplier,
            includeFlatDrain: false);
        var maximum = simulation.KernelData.Entities.AttributeMaximum(attributeId);
        if (maximum > 0)
        {
            healthShortfall = KernelQuery.Clamp01(
                (maximum - world.GetAttribute(entityId, attributeId)) / maximum);
        }

        return ageDrainComponent > 0 && healthShortfall > 0;
    }

    public void Tick(Simulation simulation, int tick)
    {
        global::System.ArgumentNullException.ThrowIfNull(simulation);
        var world = simulation.World;
        for (var index = 0; index < simulation.FrozenEntityCount; index++)
        {
            var entityId = simulation.FrozenEntityAt(index);
            ResolveEntity(simulation, world, entityId, tick);
        }

        ApplyDistrictCultureModifiers(simulation, world, tick);
    }

    private void ResolveEntity(Simulation simulation, WorldState world, int entityId, int tick)
    {
        var kindId = world.EntityKindId(entityId);
        var deltaCount = 0;
        ApplyAttributeDeltaEffectsForKind(simulation, entityId, tick, kindId, ref deltaCount);
        ApplyTagRulesForKind(simulation, entityId, tick, kindId);
        ApplyStageAmbientEmission(simulation, world, entityId, kindId, tick);

        var lifecycleRule = LifecycleRuleForKind(kindId);
        if (lifecycleRule > 0)
        {
            var healthAttributeId = Data.LifecycleAttributeIds[lifecycleRule];
            AccumulateAttributeDeltaUnits(
                healthAttributeId,
                world.DistrictHealingMagnitudeUnits(entityId),
                ref deltaCount);
            AccumulateAttributeDelta(
                healthAttributeId,
                -LifecycleDrain(simulation, entityId, kindId, lifecycleRule, tick),
                ref deltaCount);
        }

        ApplyAccumulatedAttributeDeltas(simulation, entityId, deltaCount);
        if (lifecycleRule > 0)
        {
            simulation.QueueLifecycleRetirementWhenAttributeAtMost(
                entityId,
                Data.LifecycleAttributeIds[lifecycleRule],
                threshold: 0);
        }
    }

    // Stage-catalog entities radiate ambient culture at their tile scaled by
    // their stage: interior mass follows civic maturity, replacing the old
    // worker treadmill's holding job (docs/design/entity-stages.md).
    private static void ApplyStageAmbientEmission(Simulation simulation, WorldState world, int entityId, int kindId, int tick)
    {
        var stageRule = world.StageRuleFor(kindId, world.EntityWorkTier(entityId));
        var emission = world.StageAmbientEmission(stageRule);
        if (emission <= 0)
        {
            return;
        }

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return;
        }

        _ = simulation.ModifyCellCultureInfluenceRaw(
            cultureId,
            PlaneCodes.Physical,
            world.EntityPhysicalQ(entityId),
            world.EntityPhysicalR(entityId),
            emission,
            tick);
    }

    private static void ApplyDistrictCultureModifiers(Simulation simulation, WorldState world, int tick)
    {
        for (var modifier = 1; modifier <= world.DistrictCultureModifierCount; modifier++)
        {
            _ = simulation.ModifyCellCultureInfluenceRawUnits(
                world.DistrictCultureModifierCultureId(modifier),
                PlaneCodes.Physical,
                world.DistrictCultureModifierQ(modifier),
                world.DistrictCultureModifierR(modifier),
                0,
                world.DistrictCultureModifierMagnitudeUnits(modifier),
                tick);
        }
    }

    private void ApplyTagRulesForKind(Simulation simulation, int entityId, int tick, int kindId)
    {
        for (var rule = FirstRuleForKind(kindId, Data.FirstTagRuleByKind);
             rule > 0;
             rule = Data.NextTagRuleByKind[rule])
        {
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
                : 0;
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

    private static FixedScalar SummedRelationshipStrength(
        WorldState world,
        int entityId,
        int relationshipKindId,
        int directionCode)
    {
        var sum = FixedScalar.Zero;
        if (directionCode is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            var cursor = world.OutgoingRelationshipsOfKind(entityId, relationshipKindId);
            while (world.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                sum += world.RelationshipStrength(relationshipId);
            }
        }

        if (directionCode is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            var cursor = world.IncomingRelationshipsOfKind(entityId, relationshipKindId);
            while (world.MoveNextRelationship(ref cursor, out var relationshipId))
            {
                sum += world.RelationshipStrength(relationshipId);
            }
        }

        return sum;
    }

    private void ApplyAttributeDeltaEffectsForKind(
        Simulation simulation,
        int entityId,
        int tick,
        int kindId,
        ref int deltaCount)
    {
        _ = tick;
        for (var rule = FirstRuleForKind(kindId, Data.FirstAttributeDeltaRuleByKind);
             rule > 0;
             rule = Data.NextAttributeDeltaRuleByKind[rule])
        {
            if (!KernelQuery.TraitFilterPasses(simulation.World, entityId, _attributeDeltaRequiredTargetTraitIds[rule]))
            {
                continue;
            }

            if (!RuleCultureGatePasses(simulation.World, entityId, rule))
            {
                continue;
            }

            var evidence = EffectEvidence(simulation, entityId, rule);
            if (evidence <= 0)
            {
                continue;
            }

            AccumulateAttributeDelta(
                _attributeDeltaAttributeIds[rule],
                _attributeDeltaDeltas[rule] * evidence,
                ref deltaCount);
        }
    }

    // Rules with an authored culture apply only to entities of that culture
    // (the culture-slot condition manages its own culture semantics).
    private bool RuleCultureGatePasses(WorldState world, int entityId, int rule) =>
        _attributeDeltaCultureIds[rule] <= 0 ||
        _attributeDeltaConditionCodes[rule] == EffectConditionCodes.CultureSlotOccupancy ||
        world.EntityCultureId(entityId) == _attributeDeltaCultureIds[rule];

    private FixedScalar EffectEvidence(Simulation simulation, int entityId, int rule)
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
            or EffectConditionCodes.LocalComfortSignal
            or EffectConditionCodes.LocalSolitudeSignal)
        {
            return SignalEvidence(simulation, entityId, rule, conditionCode);
        }

        if (conditionCode == EffectConditionCodes.LocalCultureMapValue)
        {
            return OwnCultureInfluenceEvidence(simulation.World, entityId, rule);
        }

        if (conditionCode == EffectConditionCodes.BeyondCultureSupport)
        {
            return BeyondCultureSupportEvidence(simulation, entityId, rule);
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

    private FixedScalar InsideOwnedCoverageEvidence(WorldState world, int entityId, int rule)
    {
        return InsideJurisdiction(
            world,
            entityId,
            _attributeDeltaCoverageOwnerKindIds[rule],
            _attributeDeltaCoverageOwnerSubtypeIds[rule],
            _attributeDeltaPlaneCodes[rule])
            ? 1
            : 0;
    }

    // Local carrying capacity: crowding strain discounted by own-culture
    // mass support. Asymptotic (scale / (scale + mass)), never a hard cap:
    // growing the territory's culture mass is the built-in escape, and later
    // mechanisms may raise support the same way age can be extended.
    private FixedScalar CrowdingBeyondCultureSupportEvidence(Simulation simulation, int entityId, int rule)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var crowding = KernelQuery.ThresholdEvidence(
            simulation.Locality.EntityCrowdingLoad(simulation, entityId, planeCode),
            _attributeDeltaThresholds[rule]);
        if (crowding <= 0)
        {
            return 0;
        }

        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        var supportScale = _attributeDeltaSupportScales[rule];
        if (cultureId <= 0 || supportScale <= 0)
        {
            return crowding;
        }

        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var mass = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        return crowding * (supportScale / (supportScale + mass));
    }

    private FixedScalar SpatialEvidence(Simulation simulation, int entityId, int rule, int conditionCode) =>
        conditionCode switch
        {
            EffectConditionCodes.CrowdingBeyondCultureSupport => CrowdingBeyondCultureSupportEvidence(simulation, entityId, rule),
            EffectConditionCodes.InsideOwnedCoverage => InsideOwnedCoverageEvidence(simulation.World, entityId, rule),
            EffectConditionCodes.CultureSlotOccupancy => CultureSlotOccupancyEvidence(simulation, entityId, rule),
            _ => 0,
        };

    private FixedScalar SignalEvidence(Simulation simulation, int entityId, int rule, int conditionCode)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var value = conditionCode switch
        {
            EffectConditionCodes.LocalPositiveSignal => simulation.Locality.EntitySupport(simulation, entityId, planeCode),
            EffectConditionCodes.LocalNegativeSignal => simulation.Locality.EntityThreat(simulation, entityId, planeCode),
            EffectConditionCodes.LocalCrowdingLoadSignal => simulation.Locality.EntityCrowdingLoad(simulation, entityId, planeCode),
            EffectConditionCodes.LocalSolitudeSignal => SolitudeSignal(simulation, entityId, planeCode),
            _ => simulation.Locality.EntityCrowdingComfort(simulation, entityId, planeCode),
        };
        return KernelQuery.ThresholdEvidence(value, _attributeDeltaThresholds[rule]);
    }

    // Solitude is the inverse of crowding comfort: 1 when the entity stands
    // alone, fading toward 0 as local population approaches capacity, and 0
    // when the cell is over capacity (crowding load owns that regime).
    private static FixedScalar SolitudeSignal(Simulation simulation, int entityId, int planeCode)
    {
        var crowding = simulation.Locality.EntityCrowding(simulation, entityId, planeCode);
        if (crowding.Load > 0)
        {
            return 0;
        }

        return crowding.Comfort >= 1 ? 0 : 1 - crowding.Comfort;
    }

    private FixedScalar OwnCultureInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0;
        }

        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        return KernelQuery.ThresholdEvidence(
            world.GetCellCultureInfluence(cultureId, planeCode, q, r),
            _attributeDeltaThresholds[rule]);
    }

    // Inverse of own-culture support: full evidence with no support at all,
    // none at or beyond the threshold. Cultureless entities answer to no
    // hearth and feel nothing. Knowledge range grants lower the threshold,
    // so a far-walking culture's people range farther before the cold bites.
    private FixedScalar BeyondCultureSupportEvidence(Simulation simulation, int entityId, int rule)
    {
        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0;
        }

        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var threshold = _attributeDeltaThresholds[rule] * simulation.Knowledge.Modifier(
            cultureId,
            KnowledgeGrantDomainCodes.FrontierThawThreshold);
        if (threshold <= 0)
        {
            return 0;
        }

        var supported = world.CultureFieldAt(cultureId, planeCode, q, r).TotalRawMass;
        return supported >= threshold ? 0 : 1 - (supported / threshold);
    }

    private FixedScalar CultureSlotOccupancyEvidence(Simulation simulation, int entityId, int rule)
    {
        return simulation.CultureSlots.OccupancyEvidence(
            entityId,
            _attributeDeltaCultureIds[rule],
            _attributeDeltaCultureSlotIds[rule],
            _attributeDeltaCultureSlotPolarityCodes[rule],
            _attributeDeltaThresholds[rule]);
    }

    private FixedScalar RelatedInfluenceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = FixedScalar.Zero;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1)
        {
            return 1;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedInfluenceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private FixedScalar RelatedInfluenceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var planeCode = _attributeDeltaPlaneCodes[rule];
        var q = KernelQuery.CurrentQ(world, entityId, planeCode);
        var r = KernelQuery.CurrentR(world, entityId, planeCode);
        var total = FixedScalar.Zero;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        while (total < 1 && world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var otherEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(otherEntityId) ||
                !KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
            {
                continue;
            }

            var otherCultureId = world.EntityCultureId(otherEntityId);
            var influence = otherCultureId <= 0
                ? 0
                : world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            if (influence > 0)
            {
                total += KernelQuery.ThresholdEvidence(influence, _attributeDeltaThresholds[rule]) *
                    KernelQuery.RelationshipStrength(world, relationshipId);
            }
        }

        return total;
    }

    private FixedScalar RelatedDistanceEvidence(WorldState world, int entityId, int rule)
    {
        var direction = _attributeDeltaRelationshipDirectionCodes[rule];
        var total = FixedScalar.Zero;
        if (direction is RelationshipMatchDirectionCodes.Outgoing or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: true);
        }

        if (total >= 1)
        {
            return 1;
        }

        if (direction is RelationshipMatchDirectionCodes.Incoming or RelationshipMatchDirectionCodes.Either)
        {
            total += RelatedDistanceEvidenceForDirection(world, entityId, rule, outgoing: false);
        }

        return KernelQuery.Clamp01(total);
    }

    private FixedScalar RelatedDistanceEvidenceForDirection(
        WorldState world,
        int entityId,
        int rule,
        bool outgoing)
    {
        var total = FixedScalar.Zero;
        var cursor = outgoing
            ? world.OutgoingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule])
            : world.IncomingRelationshipsOfKindAbove(
                entityId,
                _attributeDeltaRelationshipKindIds[rule],
                _attributeDeltaMinimumRelationshipStrengths[rule]);
        while (total < 1 && world.MoveNextRelationship(ref cursor, out var relationshipId))
        {
            var otherEntityId = outgoing
                ? world.RelationshipTargetId(relationshipId)
                : world.RelationshipSourceId(relationshipId);
            if (!world.EntityIsActive(otherEntityId) ||
                !KernelQuery.TraitFilterPasses(world, otherEntityId, _attributeDeltaRequiredRelatedTraitIds[rule]))
            {
                continue;
            }

            var distance = KernelQuery.PlaneDistance(world, entityId, otherEntityId, _attributeDeltaPlaneCodes[rule]);
            total += KernelQuery.DistanceEvidence(distance, _attributeDeltaMaxDistances[rule]) *
                KernelQuery.RelationshipStrength(world, relationshipId);
        }

        return total;
    }

    private void AccumulateAttributeDelta(int attributeId, FixedScalar delta, ref int count)
    {
        AccumulateAttributeDeltaUnits(
            attributeId,
            KernelFixedPoint.AttributeUnits(delta),
            ref count);
    }

    private void AccumulateAttributeDeltaUnits(int attributeId, int deltaUnits, ref int count)
    {
        if (deltaUnits == 0)
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            if (_attributeDeltaScratchIds[index] == attributeId)
            {
                _attributeDeltaScratchValues[index] = checked(
                    _attributeDeltaScratchValues[index] + deltaUnits);
                return;
            }
        }

        if (count >= _attributeDeltaScratchIds.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        _attributeDeltaScratchIds[count] = attributeId;
        _attributeDeltaScratchValues[count] = deltaUnits;
        count++;
    }

    private void ApplyAccumulatedAttributeDeltas(Simulation simulation, int entityId, int count)
    {
        for (var index = 0; index < count; index++)
        {
            KernelEffectExecutor.ModifyAttributeUnits(
                simulation,
                entityId,
                _attributeDeltaScratchIds[index],
                KernelFixedPoint.ClampToInt(
                    _attributeDeltaScratchValues[index],
                    KernelFixedPoint.AttributeMinimumUnits,
                    KernelFixedPoint.AttributeMaximumUnits));
            _attributeDeltaScratchIds[index] = 0;
            _attributeDeltaScratchValues[index] = 0;
        }
    }

    private FixedScalar LifecycleDrain(
        Simulation simulation,
        int entityId,
        int kindId,
        int rule,
        int tick)
    {
        var age = global::System.Math.Max(0, tick - simulation.World.EntityCreatedAt(entityId));
        var multiplier = StageLifespanMultiplier(simulation, entityId, kindId);
        var drain = LifecycleDrainComponent(
            rule,
            age,
            multiplier,
            includeFlatDrain: true);
        var variance = Data.LifecycleVariances[rule];
        if (variance > 0)
        {
            var factor = 1 + (((simulation.RandomScalar(
                simulation.World.EntityOccurrenceHandle(entityId),
                rule,
                KernelRandomPurposeCodes.LifecycleVariance,
                attempt: tick) * 2) - 1) * variance);
            drain *= FixedScalar.Max(0, factor);
        }

        return drain;
    }

    private static FixedScalar StageLifespanMultiplier(Simulation simulation, int entityId, int kindId)
    {
        var world = simulation.World;
        var stageRule = world.StageRuleFor(kindId, world.EntityWorkTier(entityId));
        var multiplier = world.StageBandMultiplier(stageRule);
        return multiplier <= 0 ? 1 : multiplier;
    }

    private FixedScalar LifecycleDrainComponent(
        int rule,
        int age,
        FixedScalar lifespanMultiplier,
        bool includeFlatDrain)
    {
        var previousAge = global::System.Math.Max(
            0,
            age - Tuning.MainProgramCadenceTicks);
        var current = LifecycleCumulativeDrain(
            rule,
            age,
            lifespanMultiplier,
            includeFlatDrain);
        var previous = LifecycleCumulativeDrain(
            rule,
            previousAge,
            lifespanMultiplier,
            includeFlatDrain);
        return FixedScalar.Max(0, current - previous);
    }

    private FixedScalar LifecycleCumulativeDrain(
        int rule,
        int age,
        FixedScalar lifespanMultiplier,
        bool includeFlatDrain)
    {
        if (age <= 0)
        {
            return 0;
        }

        var effectiveTargetTicks =
            FixedScalar.FromInt(Data.LifecycleTargetTicks[rule]) *
            lifespanMultiplier;
        if (effectiveTargetTicks <= 0)
        {
            throw new global::System.InvalidOperationException();
        }

        var progress = FixedScalar.FromInt(age) / effectiveTargetTicks;
        var flatShare = Data.LifecycleFlatDrainShares[rule];
        FixedScalar curve;
        if (Tuning.Lifecycle.AgingModelCode == LifecycleAgingModelCodes.LateLife)
        {
            var reducedFlatShare = flatShare / 2;
            var ageShare = 1 - reducedFlatShare;
            curve =
                (includeFlatDrain ? reducedFlatShare * progress : 0) +
                (ageShare * progress * progress * progress);
        }
        else
        {
            var ageShare = 1 - flatShare;
            curve =
                (includeFlatDrain ? flatShare * progress : 0) +
                (ageShare * progress * progress);
        }

        return KernelFixedPoint.AttributeScalar(
            Data.LifecycleNominalHealthUnits[rule]) * curve;
    }

    private int LifecycleRuleForKind(int kindId) =>
        (uint)kindId < (uint)Data.LifecycleRuleByKind.Length
            ? Data.LifecycleRuleByKind[kindId]
            : 0;

    private static int FirstRuleForKind(int kindId, int[] firstRuleByKind) =>
        (uint)kindId < (uint)firstRuleByKind.Length ? firstRuleByKind[kindId] : 0;

    private static void AppendRuleByKind(
        int kindId,
        int rule,
        int[] firstRuleByKind,
        int[] lastRuleByKind,
        int[] nextRuleByKind)
    {
        if ((uint)kindId >= (uint)firstRuleByKind.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        var previous = lastRuleByKind[kindId];
        if (previous > 0)
        {
            nextRuleByKind[previous] = rule;
        }
        else
        {
            firstRuleByKind[kindId] = rule;
        }

        lastRuleByKind[kindId] = rule;
    }

    private static void ValidatePositive(int value)
    {
        NumericGuard.ThrowIfNegativeOrZero(value);
    }
}
