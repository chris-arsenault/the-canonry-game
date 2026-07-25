namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private const int AffordanceOverlapCap = 6;
    private static readonly FixedScalar CultureSlotAgencyEvidenceThreshold = FixedScalar.FromUnits(2400000L);
    private static readonly FixedScalar CultureSlotForeignReductionPositiveAgencyWeight =
        FixedScalar.FromRatio(7, 20);

    private static readonly FixedScalar[] AffordanceDiminishingFactors =
    [
        FixedScalar.FromUnits(1000000L),
        FixedScalar.FromUnits(700000L),
        FixedScalar.FromUnits(500000L),
        FixedScalar.FromUnits(400000L),
        FixedScalar.FromUnits(300000L),
        FixedScalar.FromUnits(250000L),
    ];

    private FixedScalar EffectiveDistanceScale(int rule, int targetMode)
    {
        var verbOverride = Data.DistanceScaleOverride(rule);
        if (verbOverride > 0)
        {
            return verbOverride;
        }

        var modeOptions = Tuning.LowAltitudeVerbs;
        return targetMode switch
        {
            TargetSelectorCodes.Explore => modeOptions.ExploreDistanceScale,
            TargetSelectorCodes.Terrain => modeOptions.TerrainDistanceScale,
            TargetSelectorCodes.EntityKind => modeOptions.EntityKindDistanceScale,
            TargetSelectorCodes.RelationshipTarget => modeOptions.RelationshipTargetDistanceScale,
            TargetSelectorCodes.SelfMapValue => modeOptions.OwnInfluenceDistanceScale,
            TargetSelectorCodes.OtherMapValue => modeOptions.ForeignInfluenceDistanceScale,
            _ => modeOptions.EntityKindDistanceScale,
        };
    }

    private EffectiveVerbScalars EffectiveScalars(Simulation simulation, int entityId, int rule)
    {
        var totals = TraitTotals(simulation.World, entityId, rule);
        var cultureId = simulation.World.EntityCultureId(entityId);
        var weight = (Data.Weight(rule) + totals.Weight) *
            CultureSlotAgencyMultiplier(simulation, entityId, rule) *
            Data.AgeWeightMultiplier(rule) *
            CrowdedOriginDepartureMultiplier(simulation, entityId, rule) *
            simulation.Knowledge.Modifier(
                cultureId,
                KnowledgeGrantDomainCodes.VerbWeight,
                Data.VerbId(rule));
        var globalBonus = Tuning.LowAltitudeVerbs.OpportunityBonus;
        var verbOverride = Data.OpportunityBonusOverride(rule);
        var opportunityBonus = (verbOverride > 0 ? verbOverride : globalBonus) + totals.OpportunityBonus;
        var fitnessWeight = Data.FitnessWeight(rule) + totals.FitnessWeight;

        return new EffectiveVerbScalars(
            weight < FixedScalar.FromUnits(10000L) ? FixedScalar.FromUnits(10000L) : weight,
            opportunityBonus < 0 ? 0 : opportunityBonus,
            fitnessWeight < 0 ? 0 : fitnessWeight);
    }

    private FixedScalar CultureSlotAgencyMultiplier(Simulation simulation, int entityId, int rule)
    {
        var operationCode = Data.OperationCode(rule);
        var hasTravelInfluence = operationCode == ProgramOpcodeCodes.MoveActor &&
            Data.InfluenceDelta(rule) > 0;
        if (!hasTravelInfluence &&
            !OperationBuildsOwnedInfluence(operationCode) &&
            operationCode != ProgramOpcodeCodes.SubtractOtherCellScalar &&
            operationCode != ProgramOpcodeCodes.AdjustRelationshipStrength)
        {
            return 1;
        }

        var cultureId = simulation.World.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 1;
        }

        simulation.CultureSlots.OccupancyEvidenceByPolarity(
            entityId,
            cultureId,
            CultureSlotAgencyEvidenceThreshold,
            out var positive,
            out var neutral,
            out var negative);

        if (OperationBuildsOwnedInfluence(operationCode) || hasTravelInfluence)
        {
            return 1 + (FixedScalar.FromUnits(850000L) * positive) + (FixedScalar.FromUnits(450000L) * neutral) + (FixedScalar.FromUnits(150000L) * negative);
        }

        if (operationCode == ProgramOpcodeCodes.SubtractOtherCellScalar)
        {
            return 1 + (CultureSlotForeignReductionPositiveAgencyWeight * positive) + (FixedScalar.FromUnits(200000L) * neutral) + (FixedScalar.FromUnits(650000L) * negative);
        }

        var best = FixedScalar.Max(positive, FixedScalar.Max(neutral, negative));
        return 1 + (FixedScalar.FromUnits(250000L) * best);
    }

    private FixedScalar CultureSlotInfluenceEffectivenessMultiplier(
        Simulation simulation,
        int entityId,
        int rule,
        bool reducingForeign)
    {
        var cultureId = simulation.World.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 1;
        }

        simulation.CultureSlots.OccupancyEvidenceByPolarity(
            entityId,
            cultureId,
            CultureSlotAgencyEvidenceThreshold,
            out var positive,
            out var neutral,
            out var negative);

        return reducingForeign
            ? 1 + (FixedScalar.FromUnits(150000L) * positive) + (FixedScalar.FromUnits(100000L) * neutral) + (FixedScalar.FromUnits(250000L) * negative)
            : 1 + (FixedScalar.FromUnits(250000L) * positive) + (FixedScalar.FromUnits(150000L) * neutral) + (FixedScalar.FromUnits(50000L) * negative);
    }

    private LowAltitudeDecisionModifierTotals TraitTotals(WorldState world, int entityId, int rule)
    {
        var totals = new LowAltitudeDecisionModifierTotals();
        var kindId = world.EntityKindId(entityId);
        var verbId = Data.VerbId(rule);
        for (var modifierSlot = Data.FirstTraitModifierForKind(kindId);
             modifierSlot > 0;
             modifierSlot = Data.NextTraitModifierForKind(modifierSlot))
        {
            var modifier = Data.ReadDecisionModifier(modifierSlot);
            if (modifier.AppliesToLowAltitudeVerb(world, kindId, verbId, entityId))
            {
                totals.Add(modifier);
            }
        }

        return totals;
    }

    private FixedScalar DistancePref(int rule, int targetMode, int actorQ, int actorR, int targetQ, int targetR)
    {
        var distance = HexDistance(actorQ, actorR, targetQ, targetR);
        var scale = EffectiveDistanceScale(rule, targetMode);
        if (scale <= 0)
        {
            return 1;
        }

        return 1 / (1 + (distance / scale));
    }

    // The departure half of carrying capacity: an entity standing on
    // overloaded ground weights its movement verbs up in proportion to the
    // local crowding, so crowded hubs shed excess actors toward the open
    // ground the destination saturation divisor already favors. Non-movement
    // verbs are unaffected; comfortable ground changes nothing.
    private FixedScalar CrowdedOriginDepartureMultiplier(Simulation simulation, int entityId, int rule)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor)
        {
            return 1;
        }

        var planeCode = Data.PlaneCode(rule);
        var load = simulation.Locality.EntityCrowdingLoad(simulation, entityId, planeCode);
        return load <= 0 ? 1 : 1 + FixedScalar.Min(load, 2);
    }

    private FixedScalar CellSaturationDivisor(Simulation simulation, int planeCode, int q, int r)
    {
        var occupancyOneRing = simulation.ReadCellOccupancyOneRingSum(planeCode, q, r);
        var factor = Tuning.LowAltitudeVerbs.CellSaturationFactor;
        return 1 / (1 + (occupancyOneRing * factor));
    }

    private FixedScalar InfluenceTargetSaturationDivisor(
        Simulation simulation,
        int targetingCode,
        int planeCode,
        int q,
        int r) =>
        targetingCode == LowAltitudeInfluenceTargetingCodes.Reinforce
            ? 1
            : CellSaturationDivisor(simulation, planeCode, q, r);

    private FixedScalar ComputeRecencyMultiplier(int entityId, int tick, long targetKey)
    {
        if (targetKey == 0L)
        {
            return 1;
        }

        var cooldown = Tuning.LowAltitudeVerbs.RecentTargetCooldownTicks;
        if (cooldown <= 0)
        {
            return 1;
        }

        var multiplier = FixedScalar.One;
        var count = State.RecentTargetCount(entityId);
        for (var slot = 0; slot < count; slot++)
        {
            if (State.RecentTargetKey(entityId, slot) != targetKey)
            {
                continue;
            }

            var elapsed = tick - State.RecentTargetTick(entityId, slot);
            var cooled = Clamp01((FixedScalar)elapsed / cooldown);
            if (cooled < multiplier)
            {
                multiplier = cooled;
            }
        }

        return multiplier;
    }

    private static FixedScalar ClampCandidateWeight(FixedScalar weight) => weight <= FixedScalar.FromUnits(1000L) ? FixedScalar.FromUnits(1000L) : weight;

    private FixedScalar RelationshipFitness(
        WorldState world,
        int entityId,
        int targetEntityId,
        int rule,
        EffectiveVerbScalars scalars)
    {
        var fitnessRelationshipKindId = Data.FitnessRelationshipKindId(rule);
        if (fitnessRelationshipKindId == 0)
        {
            return 1;
        }

        var strength = KernelQuery.BestRelationshipStrengthBetween(
            world,
            entityId,
            targetEntityId,
            fitnessRelationshipKindId,
            Data.FitnessRelationshipDirectionCode(rule));
        var polarity = Data.FitnessPolarityCode(rule);
        var threshold = Data.FitnessThreshold(rule);
        var basis = polarity switch
        {
            AffordancePolarityCodes.Strengthen => DeficitOpportunity(strength, threshold),
            AffordancePolarityCodes.Weaken => SurplusOpportunity(strength, threshold),
            _ => strength,
        };
        return 1 + (scalars.FitnessWeight * basis);
    }

    private static FixedScalar DeficitOpportunity(FixedScalar currentStrength, FixedScalar threshold)
    {
        if (threshold <= 0)
        {
            return 0;
        }

        if (currentStrength >= threshold)
        {
            return 0;
        }

        return (threshold - currentStrength) / threshold;
    }

    private static FixedScalar SurplusOpportunity(FixedScalar currentStrength, FixedScalar threshold)
    {
        if (currentStrength <= threshold)
        {
            return 0;
        }

        var denominator = FixedScalar.Max(FixedScalar.FromUnits(10000L), 1 - threshold);
        return Clamp01((currentStrength - threshold) / denominator);
    }

    private struct AffordanceAccumulator
    {
        public FixedScalar[] SortedWeights;
        public int[] SortedSourceKinds;
        public int[] SortedSourceIds;
        public int[] SortedShapes;
        public int InsertedCount;
        public int SourceKindMask;
        public int ShapeMask;
        public int ContributionCount;
        public int GeneratorCount;
        public int GambitCount;

        public static AffordanceAccumulator Create() => new()
        {
            SortedWeights = new FixedScalar[AffordanceOverlapCap],
            SortedSourceKinds = new int[AffordanceOverlapCap],
            SortedSourceIds = new int[AffordanceOverlapCap],
            SortedShapes = new int[AffordanceOverlapCap],
        };
    }

    private void RecordAffordanceContribution(
        ref AffordanceAccumulator accumulator,
        int affordance,
        FixedScalar effectiveWeight)
    {
        if (effectiveWeight <= 0)
        {
            return;
        }

        var sourceKindCode = Data.AffordanceSourceKindCode(affordance);
        var shapeCode = Data.AffordanceShapeCode(affordance);
        accumulator.SourceKindMask |= AffordanceSourceKindBit(sourceKindCode);
        accumulator.ShapeMask |= AffordanceShapeBit(shapeCode);
        accumulator.ContributionCount++;
        if (sourceKindCode == AffordanceSourceKindCodes.Generator)
        {
            accumulator.GeneratorCount++;
        }
        else if (sourceKindCode == AffordanceSourceKindCodes.Gambit)
        {
            accumulator.GambitCount++;
        }

        InsertSortedDescending(
            ref accumulator.InsertedCount,
            accumulator.SortedWeights,
            accumulator.SortedSourceKinds,
            accumulator.SortedSourceIds,
            accumulator.SortedShapes,
            effectiveWeight,
            sourceKindCode,
            Data.AffordanceSourceId(affordance),
            shapeCode);
    }

    private AffordanceContribution CombineAffordancesForEntityTarget(
        int actorKindId,
        int planeCode,
        int candidateKindId)
    {
        if (candidateKindId == 0)
        {
            return default;
        }

        var accumulator = AffordanceAccumulator.Create();
        for (var affordance = Data.FirstAffordanceForActorKind(actorKindId);
             affordance > 0;
             affordance = Data.NextAffordanceForActorKind(affordance))
        {
            if (!EntityTargetAffordanceMatches(affordance, actorKindId, planeCode, candidateKindId))
            {
                continue;
            }

            RecordAffordanceContribution(ref accumulator, affordance, Data.AffordanceWeight(affordance));
        }

        return BuildContribution(ref accumulator);
    }

    private bool EntityTargetAffordanceMatches(int affordance, int actorKindId, int planeCode, int candidateKindId)
    {
        return Data.AffordanceShapeCode(affordance) == LowAltitudeAffordanceShapeCodes.PresenceNearEntity &&
            AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) &&
            AffordanceCandidateKindMatches(affordance, candidateKindId);
    }

    private AffordanceContribution CombineAffordancesForRelationshipTarget(
        int actorKindId,
        int candidateKindId,
        int planeCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        FixedScalar currentStrength,
        FixedScalar strengthDelta)
    {
        var accumulator = AffordanceAccumulator.Create();
        AccumulateEntityPresenceForRelationship(ref accumulator, actorKindId, planeCode, candidateKindId);
        AccumulateRelationshipShapeAffordances(
            ref accumulator,
            actorKindId,
            planeCode,
            relationshipKindId,
            relationshipDirectionCode,
            currentStrength,
            strengthDelta);
        return BuildContribution(ref accumulator);
    }

    private void AccumulateEntityPresenceForRelationship(
        ref AffordanceAccumulator accumulator,
        int actorKindId,
        int planeCode,
        int candidateKindId)
    {
        if (candidateKindId == 0)
        {
            return;
        }

        for (var affordance = Data.FirstAffordanceForActorKind(actorKindId);
             affordance > 0;
             affordance = Data.NextAffordanceForActorKind(affordance))
        {
            if (!EntityTargetAffordanceMatches(affordance, actorKindId, planeCode, candidateKindId))
            {
                continue;
            }

            RecordAffordanceContribution(ref accumulator, affordance, Data.AffordanceWeight(affordance));
        }
    }

    private void AccumulateRelationshipShapeAffordances(
        ref AffordanceAccumulator accumulator,
        int actorKindId,
        int planeCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        FixedScalar currentStrength,
        FixedScalar strengthDelta)
    {
        for (var affordance = Data.FirstAffordanceForActorKind(actorKindId);
             affordance > 0;
             affordance = Data.NextAffordanceForActorKind(affordance))
        {
            if (!AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) ||
                !AffordanceRelationshipMatches(affordance, relationshipKindId, relationshipDirectionCode))
            {
                continue;
            }

            var effectiveWeight = ResolveRelationshipShapeWeight(affordance, currentStrength, strengthDelta);
            RecordAffordanceContribution(ref accumulator, affordance, effectiveWeight);
        }
    }

    private FixedScalar ResolveRelationshipShapeWeight(int affordance, FixedScalar currentStrength, FixedScalar strengthDelta)
    {
        var shapeCode = Data.AffordanceShapeCode(affordance);
        if (shapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            strengthDelta != 0 &&
            AffordanceOperationMatches(affordance, ProgramOpcodeCodes.AdjustRelationshipStrength) &&
            AffordancePolarityMatchesStrengthDelta(affordance, strengthDelta))
        {
            var opportunity = RelationshipStrengthOpportunity(
                currentStrength,
                Data.AffordanceThreshold(affordance),
                Data.AffordancePolarityCode(affordance));
            return Data.AffordanceWeight(affordance) * opportunity;
        }

        return 0;
    }

    private AffordanceContribution CombineAffordancesForInfluenceTarget(
        int actorKindId,
        int planeCode,
        int operationCode,
        int candidateKindId,
        int polarityCode,
        FixedScalar currentInfluence)
    {
        var accumulator = AffordanceAccumulator.Create();
        for (var affordance = Data.FirstAffordanceForActorKind(actorKindId);
             affordance > 0;
             affordance = Data.NextAffordanceForActorKind(affordance))
        {
            if (!InfluenceAffordanceMatches(affordance, actorKindId, planeCode, operationCode, candidateKindId, polarityCode))
            {
                continue;
            }

            var opportunity = InfluenceOpportunity(
                currentInfluence,
                Data.AffordanceThreshold(affordance),
                Data.AffordancePolarityCode(affordance));
            RecordAffordanceContribution(ref accumulator, affordance, Data.AffordanceWeight(affordance) * opportunity);
        }

        return BuildContribution(ref accumulator);
    }

    private bool InfluenceAffordanceMatches(
        int affordance,
        int actorKindId,
        int planeCode,
        int operationCode,
        int candidateKindId,
        int polarityCode)
    {
        return Data.AffordanceShapeCode(affordance) == LowAltitudeAffordanceShapeCodes.InfluenceChange &&
            AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) &&
            AffordanceCandidateKindMatches(affordance, candidateKindId) &&
            AffordanceOperationMatches(affordance, operationCode) &&
            AffordancePolarityMatches(affordance, polarityCode);
    }

    private static AffordanceContribution BuildContribution(ref AffordanceAccumulator accumulator)
    {
        var combined = FixedScalar.Zero;
        for (var i = 0; i < accumulator.InsertedCount; i++)
        {
            combined += accumulator.SortedWeights[i] * AffordanceDiminishingFactors[i];
        }

        var topSourceKind = accumulator.InsertedCount > 0 ? accumulator.SortedSourceKinds[0] : 0;
        var topSourceId = accumulator.InsertedCount > 0 ? accumulator.SortedSourceIds[0] : 0;
        var topShape = accumulator.InsertedCount > 0 ? accumulator.SortedShapes[0] : 0;
        var topScore = accumulator.InsertedCount > 0
            ? accumulator.SortedWeights[0] * AffordanceDiminishingFactors[0]
            : 0;

        return new AffordanceContribution(
            combined,
            topSourceKind,
            topSourceId,
            topShape,
            topScore,
            accumulator.SourceKindMask,
            accumulator.ShapeMask,
            accumulator.ContributionCount,
            accumulator.GeneratorCount,
            accumulator.GambitCount);
    }

    private static void InsertSortedDescending(
        ref int insertedCount,
        FixedScalar[] sortedWeights,
        int[] sortedSourceKinds,
        int[] sortedSourceIds,
        int[] sortedShapes,
        FixedScalar weight,
        int sourceKindCode,
        int sourceId,
        int shapeCode)
    {
        if (insertedCount == AffordanceOverlapCap && weight <= sortedWeights[insertedCount - 1])
        {
            return;
        }

        var insertAt = insertedCount;
        for (var i = 0; i < insertedCount; i++)
        {
            if (weight > sortedWeights[i])
            {
                insertAt = i;
                break;
            }
        }

        var lastIndex = insertedCount < AffordanceOverlapCap ? insertedCount : AffordanceOverlapCap - 1;
        for (var i = lastIndex; i > insertAt; i--)
        {
            sortedWeights[i] = sortedWeights[i - 1];
            sortedSourceKinds[i] = sortedSourceKinds[i - 1];
            sortedSourceIds[i] = sortedSourceIds[i - 1];
            sortedShapes[i] = sortedShapes[i - 1];
        }

        sortedWeights[insertAt] = weight;
        sortedSourceKinds[insertAt] = sourceKindCode;
        sortedSourceIds[insertAt] = sourceId;
        sortedShapes[insertAt] = shapeCode;
        if (insertedCount < AffordanceOverlapCap)
        {
            insertedCount++;
        }
    }

    private bool AffordanceMatchesActorPlane(int affordance, int actorKindId, int planeCode) =>
        Data.AffordanceActorKindId(affordance) == actorKindId &&
        (Data.AffordancePlaneCode(affordance) == PlaneCodes.None || Data.AffordancePlaneCode(affordance) == planeCode);

    private bool AffordanceCandidateKindMatches(int affordance, int candidateKindId) =>
        Data.AffordanceCandidateKindId(affordance) == 0 ||
        Data.AffordanceCandidateKindId(affordance) == candidateKindId;

    private bool AffordanceRelationshipMatches(int affordance, int relationshipKindId, int relationshipDirectionCode) =>
        Data.AffordanceRelationshipKindId(affordance) == relationshipKindId &&
        RelationshipDirectionMatches(Data.AffordanceRelationshipDirectionCode(affordance), relationshipDirectionCode);

    private bool AffordanceOperationMatches(int affordance, int operationCode)
    {
        var required = Data.AffordanceOperationCode(affordance);
        return required == 0 ||
            required == operationCode ||
            (required == ProgramOpcodeCodes.AddOwnedCellScalar && OperationBuildsOwnedInfluence(operationCode));
    }

    private static bool OperationBuildsOwnedInfluence(int operationCode) =>
        operationCode is ProgramOpcodeCodes.AddOwnedCellScalar
            or ProgramOpcodeCodes.TryClaimCell
            or ProgramOpcodeCodes.TryRelocateEntity;

    private bool AffordancePolarityMatches(int affordance, int polarityCode)
    {
        var required = Data.AffordancePolarityCode(affordance);
        return required == AffordancePolarityCodes.None || required == polarityCode;
    }

    private bool AffordancePolarityMatchesStrengthDelta(int affordance, FixedScalar strengthDelta)
    {
        var polarityCode = strengthDelta > 0
            ? AffordancePolarityCodes.Strengthen
            : AffordancePolarityCodes.Weaken;
        return AffordancePolarityMatches(affordance, polarityCode);
    }

    private static bool RelationshipDirectionMatches(int affordanceDirectionCode, int candidateDirectionCode) =>
        affordanceDirectionCode == LowAltitudeVerbRelationshipDirectionCodes.Either ||
        candidateDirectionCode == LowAltitudeVerbRelationshipDirectionCodes.Either ||
        affordanceDirectionCode == candidateDirectionCode;

    private static FixedScalar RelationshipStrengthOpportunity(FixedScalar currentStrength, FixedScalar threshold, int polarityCode) =>
        polarityCode == AffordancePolarityCodes.Weaken
            ? DecreaseOpportunity(currentStrength, threshold, maximum: 1)
            : IncreaseOpportunity(currentStrength, threshold);

    private static FixedScalar InfluenceOpportunity(FixedScalar currentInfluence, FixedScalar threshold, int polarityCode) =>
        polarityCode == AffordancePolarityCodes.Reduce
            ? DecreaseOpportunity(currentInfluence, threshold, maximum: 100)
            : IncreaseOpportunity(currentInfluence, threshold);

    private static FixedScalar IncreaseOpportunity(FixedScalar currentValue, FixedScalar threshold)
    {
        if (threshold <= 0)
        {
            return 1;
        }

        return currentValue >= threshold ? 0 : Clamp01((threshold - currentValue) / threshold);
    }

    private static FixedScalar DecreaseOpportunity(FixedScalar currentValue, FixedScalar threshold, FixedScalar maximum)
    {
        if (currentValue <= threshold)
        {
            return 0;
        }

        var denominator = FixedScalar.Max(FixedScalar.FromUnits(10000L), maximum - threshold);
        return Clamp01((currentValue - threshold) / denominator);
    }

    private static int AffordanceSourceKindBit(int sourceKindCode) => sourceKindCode switch
    {
        AffordanceSourceKindCodes.Generator => 1,
        AffordanceSourceKindCodes.Gambit => 2,
        _ => 0,
    };

    private static int AffordanceShapeBit(int shapeCode) => shapeCode switch
    {
        LowAltitudeAffordanceShapeCodes.PresenceNearEntity => 1,
        LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange => 2,
        LowAltitudeAffordanceShapeCodes.InfluenceChange => 4,
        _ => 0,
    };

    private readonly record struct AffordanceContribution(
        FixedScalar Score,
        int SourceKindCode,
        int SourceId,
        int ShapeCode,
        FixedScalar TopScore,
        int SourceKindMask,
        int ShapeMask,
        int ContributionCount,
        int GeneratorContributionCount,
        int GambitContributionCount);

    private readonly record struct EffectiveVerbScalars(
        FixedScalar Weight,
        FixedScalar OpportunityBonus,
        FixedScalar FitnessWeight);
}
