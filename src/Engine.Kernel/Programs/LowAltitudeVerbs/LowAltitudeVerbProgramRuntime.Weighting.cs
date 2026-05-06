namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private const int AffordanceOverlapCap = 6;

    private static readonly float[] AffordanceDiminishingFactors =
    [
        1.0f,
        0.7f,
        0.5f,
        0.4f,
        0.3f,
        0.25f,
    ];

    private float EffectiveDistanceScale(int rule, int targetMode)
    {
        var verbOverride = Data.DistanceScaleOverride(rule);
        if (verbOverride > 0f)
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

    private EffectiveVerbScalars EffectiveScalars(WorldState world, int entityId, int rule)
    {
        var totals = TraitTotals(world, entityId, rule);
        var weight = Data.Weight(rule) + totals.Weight;
        var globalBonus = Tuning.LowAltitudeVerbs.OpportunityBonus;
        var verbOverride = Data.OpportunityBonusOverride(rule);
        var opportunityBonus = (verbOverride > 0f ? verbOverride : globalBonus) + totals.OpportunityBonus;
        var fitnessWeight = Data.FitnessWeight(rule) + totals.FitnessWeight;

        return new EffectiveVerbScalars(
            weight < 0.01f ? 0.01f : weight,
            opportunityBonus < 0f ? 0f : opportunityBonus,
            fitnessWeight < 0f ? 0f : fitnessWeight);
    }

    private LowAltitudeDecisionModifierTotals TraitTotals(WorldState world, int entityId, int rule)
    {
        return DecisionModifierEvaluation.SumLowAltitudeValues(
            world,
            Data.TraitWeightModifierCount,
            Data.ReadDecisionModifier,
            world.EntityKindId(entityId),
            Data.VerbId(rule),
            entityId);
    }

    private float DistancePref(int rule, int targetMode, int actorQ, int actorR, int targetQ, int targetR)
    {
        var distance = HexDistance(actorQ, actorR, targetQ, targetR);
        var scale = EffectiveDistanceScale(rule, targetMode);
        if (scale <= 0f)
        {
            return 1f;
        }

        return 1f / (1f + (distance / scale));
    }

    // MapActivity/MapSignals-derived occupancy reads are intentionally previous tick
    // (PreviousTick) because LowAltitudeVerbs run before those signal programs refresh
    // in the kernel program order.
    private float CellSaturationDivisor(Simulation simulation, int planeCode, int q, int r)
    {
        var occupancyOneRing = simulation.ReadCellOccupancyOneRingSum(planeCode, q, r);
        var factor = Tuning.LowAltitudeVerbs.CellSaturationFactor;
        return 1f / (1f + (occupancyOneRing * factor));
    }

    private float ComputeRecencyMultiplier(int entityId, int tick, long targetKey)
    {
        if (targetKey == 0L)
        {
            return 1f;
        }

        var cooldown = Tuning.LowAltitudeVerbs.RecentTargetCooldownTicks;
        if (cooldown <= 0)
        {
            return 1f;
        }

        var multiplier = 1f;
        var count = State.RecentTargetCount(entityId);
        for (var slot = 0; slot < count; slot++)
        {
            if (State.RecentTargetKey(entityId, slot) != targetKey)
            {
                continue;
            }

            var elapsed = tick - State.RecentTargetTick(entityId, slot);
            var cooled = Clamp01((float)elapsed / cooldown);
            if (cooled < multiplier)
            {
                multiplier = cooled;
            }
        }

        return multiplier;
    }

    private static float ClampCandidateWeight(float weight) => weight <= 0.001f ? 0.001f : weight;

    private float RelationshipFitness(
        WorldState world,
        int entityId,
        int targetEntityId,
        int rule,
        EffectiveVerbScalars scalars)
    {
        var fitnessRelationshipKindId = Data.FitnessRelationshipKindId(rule);
        if (fitnessRelationshipKindId == 0)
        {
            return 1f;
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
        return 1f + (scalars.FitnessWeight * basis);
    }

    private static float DeficitOpportunity(float currentStrength, float threshold)
    {
        if (threshold <= 0f)
        {
            return 0f;
        }

        if (currentStrength >= threshold)
        {
            return 0f;
        }

        return (threshold - currentStrength) / threshold;
    }

    private static float SurplusOpportunity(float currentStrength, float threshold)
    {
        if (currentStrength <= threshold)
        {
            return 0f;
        }

        var denominator = global::System.MathF.Max(0.01f, 1f - threshold);
        return Clamp01((currentStrength - threshold) / denominator);
    }

    private struct AffordanceAccumulator
    {
        public float[] SortedWeights;
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
            SortedWeights = new float[AffordanceOverlapCap],
            SortedSourceKinds = new int[AffordanceOverlapCap],
            SortedSourceIds = new int[AffordanceOverlapCap],
            SortedShapes = new int[AffordanceOverlapCap],
        };
    }

    private void RecordAffordanceContribution(
        ref AffordanceAccumulator accumulator,
        int affordance,
        float effectiveWeight)
    {
        if (effectiveWeight <= 0f)
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

    private AffordanceContribution CombineAffordancesForCellTarget(
        int actorKindId,
        int planeCode,
        int terrainId)
    {
        var accumulator = AffordanceAccumulator.Create();
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
        {
            if (!CellTargetAffordanceMatches(affordance, actorKindId, planeCode, terrainId))
            {
                continue;
            }

            RecordAffordanceContribution(ref accumulator, affordance, Data.AffordanceWeight(affordance));
        }

        return BuildContribution(ref accumulator);
    }

    private bool CellTargetAffordanceMatches(int affordance, int actorKindId, int planeCode, int terrainId)
    {
        if (Data.AffordanceShapeCode(affordance) != LowAltitudeAffordanceShapeCodes.TerrainPresence ||
            !AffordanceMatchesActorPlane(affordance, actorKindId, planeCode))
        {
            return false;
        }

        var requiredTerrainId = Data.AffordanceTerrainId(affordance);
        return requiredTerrainId == 0 || requiredTerrainId == terrainId;
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
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
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
        float currentStrength,
        float strengthDelta)
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

        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
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
        float currentStrength,
        float strengthDelta)
    {
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
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

    private float ResolveRelationshipShapeWeight(int affordance, float currentStrength, float strengthDelta)
    {
        var shapeCode = Data.AffordanceShapeCode(affordance);
        if (shapeCode == LowAltitudeAffordanceShapeCodes.PresenceNearRelationshipTarget)
        {
            return Data.AffordanceWeight(affordance);
        }

        if (shapeCode == LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange &&
            strengthDelta != 0f &&
            AffordanceOperationMatches(affordance, ProgramOpcodeCodes.AdjustRelationshipStrength) &&
            AffordancePolarityMatchesStrengthDelta(affordance, strengthDelta))
        {
            var opportunity = RelationshipStrengthOpportunity(
                currentStrength,
                Data.AffordanceThreshold(affordance),
                Data.AffordancePolarityCode(affordance));
            return Data.AffordanceWeight(affordance) * opportunity;
        }

        return 0f;
    }

    private AffordanceContribution CombineAffordancesForInfluenceTarget(
        int actorKindId,
        int planeCode,
        int operationCode,
        int candidateKindId,
        int polarityCode,
        float currentInfluence)
    {
        var accumulator = AffordanceAccumulator.Create();
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
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
        var combined = 0f;
        for (var i = 0; i < accumulator.InsertedCount; i++)
        {
            combined += accumulator.SortedWeights[i] * AffordanceDiminishingFactors[i];
        }

        var topSourceKind = accumulator.InsertedCount > 0 ? accumulator.SortedSourceKinds[0] : 0;
        var topSourceId = accumulator.InsertedCount > 0 ? accumulator.SortedSourceIds[0] : 0;
        var topShape = accumulator.InsertedCount > 0 ? accumulator.SortedShapes[0] : 0;
        var topScore = accumulator.InsertedCount > 0
            ? accumulator.SortedWeights[0] * AffordanceDiminishingFactors[0]
            : 0f;

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
        float[] sortedWeights,
        int[] sortedSourceKinds,
        int[] sortedSourceIds,
        int[] sortedShapes,
        float weight,
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

    private bool AffordancePolarityMatchesStrengthDelta(int affordance, float strengthDelta)
    {
        var polarityCode = strengthDelta > 0f
            ? AffordancePolarityCodes.Strengthen
            : AffordancePolarityCodes.Weaken;
        return AffordancePolarityMatches(affordance, polarityCode);
    }

    private static bool RelationshipDirectionMatches(int affordanceDirectionCode, int candidateDirectionCode) =>
        affordanceDirectionCode == LowAltitudeVerbRelationshipDirectionCodes.Either ||
        candidateDirectionCode == LowAltitudeVerbRelationshipDirectionCodes.Either ||
        affordanceDirectionCode == candidateDirectionCode;

    private static float RelationshipStrengthOpportunity(float currentStrength, float threshold, int polarityCode) =>
        polarityCode == AffordancePolarityCodes.Weaken
            ? DecreaseOpportunity(currentStrength, threshold, maximum: 1f)
            : IncreaseOpportunity(currentStrength, threshold);

    private static float InfluenceOpportunity(float currentInfluence, float threshold, int polarityCode) =>
        polarityCode == AffordancePolarityCodes.Reduce
            ? DecreaseOpportunity(currentInfluence, threshold, maximum: 100f)
            : IncreaseOpportunity(currentInfluence, threshold);

    private static float IncreaseOpportunity(float currentValue, float threshold)
    {
        if (threshold <= 0f)
        {
            return 1f;
        }

        return currentValue >= threshold ? 0f : Clamp01((threshold - currentValue) / threshold);
    }

    private static float DecreaseOpportunity(float currentValue, float threshold, float maximum)
    {
        if (currentValue <= threshold)
        {
            return 0f;
        }

        var denominator = global::System.MathF.Max(0.01f, maximum - threshold);
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
        LowAltitudeAffordanceShapeCodes.PresenceNearRelationshipTarget => 2,
        LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange => 4,
        LowAltitudeAffordanceShapeCodes.InfluenceChange => 8,
        LowAltitudeAffordanceShapeCodes.TerrainPresence => 16,
        _ => 0,
    };

    private readonly record struct AffordanceContribution(
        float Score,
        int SourceKindCode,
        int SourceId,
        int ShapeCode,
        float TopScore,
        int SourceKindMask,
        int ShapeMask,
        int ContributionCount,
        int GeneratorContributionCount,
        int GambitContributionCount);

    private readonly record struct EffectiveVerbScalars(
        float Weight,
        float OpportunityBonus,
        float FitnessWeight);
}
