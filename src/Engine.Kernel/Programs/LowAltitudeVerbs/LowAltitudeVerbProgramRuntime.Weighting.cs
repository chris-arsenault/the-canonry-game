namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private const float AffordanceScoreCap = 3f;
    private const float AffordanceWeightMultiplier = 3f;

    private void AddEntityCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int targetEntityId,
        ref float totalWeight,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var q = CurrentQ(world, targetEntityId, planeCode);
        var r = CurrentR(world, targetEntityId, planeCode);
        var s = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
        var targetKindId = world.EntityKindId(targetEntityId);
        var actorKindId = world.EntityKindId(entityId);
        var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick, targetEntityId);
        baseWeight += RelationshipAffinityWeight(world, entityId, targetEntityId, rule);
        var affordance = EntityPresenceAffordance(actorKindId, targetKindId, planeCode);
        var affordanceWeight = RuleAffordanceWeight(world, entityId, rule);
        var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selected = Candidate(rule, planeCode, targetEntityId, targetKindId, q, r, s, baseWeight, affordance, affordanceWeight);
        }
    }

    private void AddRelationshipCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int tick,
        int targetEntityId,
        int relationshipId,
        bool outgoing,
        ref float totalWeight,
        ref VerbTargetCandidate selected)
    {
        var world = simulation.World;
        var planeCode = Data.PlaneCode(rule);
        var q = CurrentQ(world, targetEntityId, planeCode);
        var r = CurrentR(world, targetEntityId, planeCode);
        var s = planeCode == PlaneCodes.Semantic ? world.EntitySemanticS(targetEntityId) : 0;
        var actorKindId = world.EntityKindId(entityId);
        var targetKindId = world.EntityKindId(targetEntityId);
        var baseWeight = LocationBaseWeight(simulation, entityId, rule, planeCode, q, r, tick, targetEntityId);
        baseWeight += RelationshipAffinityWeight(world, entityId, targetEntityId, rule);
        var relationshipDirectionCode = outgoing
            ? LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            : LowAltitudeVerbRelationshipDirectionCodes.Incoming;
        var affordance = RelationshipTargetAffordance(
            actorKindId,
            targetKindId,
            planeCode,
            Data.RelationshipKindId(rule),
            relationshipDirectionCode,
            world.RelationshipStrength(relationshipId),
            Data.RelationshipStrengthDelta(rule));
        var affordanceWeight = RuleAffordanceWeight(world, entityId, rule);
        var weight = CandidateWeight(baseWeight, affordance, affordanceWeight);
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selected = Candidate(rule, planeCode, targetEntityId, targetKindId, q, r, s, baseWeight, affordance, affordanceWeight);
        }
    }

    private float RelationshipAffinityWeight(WorldState world, int entityId, int targetEntityId, int rule)
    {
        var relationshipKindId = Data.AffinityRelationshipKindId(rule);
        var weight = Data.RelationshipAffinityWeight(rule) + RelationshipAffinityDecisionDelta(world, entityId, rule);
        if (relationshipKindId == 0 || weight == 0f)
        {
            return 0f;
        }

        var strength = KernelQuery.BestRelationshipStrengthBetween(
            world,
            entityId,
            targetEntityId,
            relationshipKindId,
            Data.AffinityRelationshipDirectionCode(rule));
        return strength * weight;
    }

    private float RelationshipAffinityDecisionDelta(WorldState world, int entityId, int rule)
    {
        return DecisionModifierEvaluation.SumLowAltitudeValues(
            world,
            Data.TraitWeightModifierCount,
            Data.ReadDecisionModifier,
            world.EntityKindId(entityId),
            Data.VerbId(rule),
            entityId).RelationshipAffinityWeight;
    }

    private float LocationBaseWeight(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick,
        int targetEntityId = 0)
    {
        // PreviousTick reads are intentional: this program runs before MapActivity and MapSignals
        // signal programs refresh, so the SignalSnapshot is a previous tick view.
        var snapshot = simulation.ReadLowAltitudeSignalSnapshot(planeCode, q, r, tick, targetEntityId);

        var staleness = snapshot.LastVisitedTick <= 0 ? 1f : Clamp01((tick - snapshot.LastVisitedTick) / 120f);
        var heatScore = Clamp01(snapshot.Heat / 100f);
        var attractorScore = Clamp01(snapshot.AttractorMass / 100f);
        var weight = Data.Weight(rule);
        var exploreWeight = Data.ExploreWeight(rule);
        var stalenessWeight = Data.StalenessWeight(rule);
        var hotspotWeight = Data.HotspotWeight(rule);
        var crowdingWeight = Data.CrowdingWeight(rule);
        var affordanceWeight = Data.VerbAffordanceWeight(rule);
        ApplyTraitWeightModifiers(
            simulation.World,
            entityId,
            rule,
            ref weight,
            ref exploreWeight,
            ref stalenessWeight,
            ref hotspotWeight,
            ref crowdingWeight,
            ref affordanceWeight);
        weight += exploreWeight / (1f + snapshot.VisitCount);
        weight += stalenessWeight * staleness;
        weight += hotspotWeight * (heatScore + attractorScore);
        weight -= crowdingWeight * (snapshot.CurrentOccupancy + snapshot.CrowdingLoad);
        return ClampCandidateWeight(weight);
    }

    private void ApplyTraitWeightModifiers(
        WorldState world,
        int entityId,
        int rule,
        ref float weight,
        ref float exploreWeight,
        ref float stalenessWeight,
        ref float hotspotWeight,
        ref float crowdingWeight,
        ref float affordanceWeight)
    {
        var totals = DecisionModifierEvaluation.SumLowAltitudeValues(
            world,
            Data.TraitWeightModifierCount,
            Data.ReadDecisionModifier,
            world.EntityKindId(entityId),
            Data.VerbId(rule),
            entityId);
        weight += totals.Weight;
        exploreWeight += totals.ExploreWeight;
        stalenessWeight += totals.StalenessWeight;
        hotspotWeight += totals.HotspotWeight;
        crowdingWeight += totals.CrowdingWeight;
        affordanceWeight += totals.AffordanceWeight;
    }

    private float RuleAffordanceWeight(WorldState world, int entityId, int rule)
    {
        var affordanceWeight = Data.VerbAffordanceWeight(rule);
        var totals = DecisionModifierEvaluation.SumLowAltitudeValues(
            world,
            Data.TraitWeightModifierCount,
            Data.ReadDecisionModifier,
            world.EntityKindId(entityId),
            Data.VerbId(rule),
            entityId);
        return affordanceWeight + totals.AffordanceWeight;
    }

    private AffordanceContribution EntityPresenceAffordance(int actorKindId, int candidateKindId, int planeCode)
    {
        if (candidateKindId == 0)
        {
            return default;
        }

        var contribution = default(AffordanceContribution);
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
        {
            if (Data.AffordanceShapeCode(affordance) == LowAltitudeAffordanceShapeCodes.PresenceNearEntity &&
                AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) &&
                AffordanceCandidateKindMatches(affordance, candidateKindId))
            {
                contribution = AddAffordanceScore(contribution, affordance, Data.AffordanceWeight(affordance));
            }
        }

        return contribution;
    }

    private AffordanceContribution RelationshipTargetAffordance(
        int actorKindId,
        int targetKindId,
        int planeCode,
        int relationshipKindId,
        int relationshipDirectionCode,
        float currentStrength,
        float strengthDelta)
    {
        var contribution = EntityPresenceAffordance(actorKindId, targetKindId, planeCode);
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
        {
            if (!AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) ||
                !AffordanceRelationshipMatches(affordance, relationshipKindId, relationshipDirectionCode))
            {
                continue;
            }

            if (Data.AffordanceShapeCode(affordance) == LowAltitudeAffordanceShapeCodes.PresenceNearRelationshipTarget)
            {
                contribution = AddAffordanceScore(contribution, affordance, Data.AffordanceWeight(affordance));
                continue;
            }

            if (Data.AffordanceShapeCode(affordance) != LowAltitudeAffordanceShapeCodes.RelationshipStrengthChange ||
                strengthDelta == 0f ||
                !AffordanceOperationMatches(affordance, ProgramOpcodeCodes.AdjustRelationshipStrength) ||
                !AffordancePolarityMatchesStrengthDelta(affordance, strengthDelta))
            {
                continue;
            }

            var opportunity = RelationshipStrengthOpportunity(
                currentStrength,
                Data.AffordanceThreshold(affordance),
                Data.AffordancePolarityCode(affordance));
            contribution = AddAffordanceScore(contribution, affordance, Data.AffordanceWeight(affordance) * opportunity);
        }

        return contribution;
    }

    private AffordanceContribution TerrainAffordance(int actorKindId, int planeCode, int terrainId)
    {
        if (terrainId == 0)
        {
            return default;
        }

        var contribution = default(AffordanceContribution);
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
        {
            if (Data.AffordanceShapeCode(affordance) == LowAltitudeAffordanceShapeCodes.TerrainPresence &&
                AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) &&
                (Data.AffordanceTerrainId(affordance) == 0 || Data.AffordanceTerrainId(affordance) == terrainId))
            {
                contribution = AddAffordanceScore(contribution, affordance, Data.AffordanceWeight(affordance));
            }
        }

        return contribution;
    }

    private AffordanceContribution InfluenceAffordance(
        int actorKindId,
        int planeCode,
        int operationCode,
        int candidateKindId,
        int polarityCode,
        float currentInfluence)
    {
        var contribution = default(AffordanceContribution);
        for (var affordance = 1; affordance <= Data.AffordanceCount; affordance++)
        {
            if (Data.AffordanceShapeCode(affordance) != LowAltitudeAffordanceShapeCodes.InfluenceChange ||
                !AffordanceMatchesActorPlane(affordance, actorKindId, planeCode) ||
                !AffordanceCandidateKindMatches(affordance, candidateKindId) ||
                !AffordanceOperationMatches(affordance, operationCode) ||
                !AffordancePolarityMatches(affordance, polarityCode))
            {
                continue;
            }

            var opportunity = InfluenceOpportunity(
                currentInfluence,
                Data.AffordanceThreshold(affordance),
                Data.AffordancePolarityCode(affordance));
            contribution = AddAffordanceScore(contribution, affordance, Data.AffordanceWeight(affordance) * opportunity);
        }

        return contribution;
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

    private AffordanceContribution AddAffordanceScore(
        AffordanceContribution contribution,
        int affordance,
        float rowScore)
    {
        if (rowScore <= 0f)
        {
            return contribution;
        }

        var rowSourceKindCode = Data.AffordanceSourceKindCode(affordance);
        var rowShapeCode = Data.AffordanceShapeCode(affordance);
        var sourceKindCode = contribution.SourceKindCode;
        var sourceId = contribution.SourceId;
        var shapeCode = contribution.ShapeCode;
        var topScore = contribution.TopScore;
        if (rowScore > topScore)
        {
            sourceKindCode = rowSourceKindCode;
            sourceId = Data.AffordanceSourceId(affordance);
            shapeCode = rowShapeCode;
            topScore = rowScore;
        }

        return new AffordanceContribution(
            global::System.MathF.Min(AffordanceScoreCap, contribution.Score + rowScore),
            sourceKindCode,
            sourceId,
            shapeCode,
            topScore,
            contribution.SourceKindMask | AffordanceSourceKindBit(rowSourceKindCode),
            contribution.ShapeMask | AffordanceShapeBit(rowShapeCode),
            contribution.ContributionCount + 1,
            contribution.GeneratorContributionCount + (rowSourceKindCode == AffordanceSourceKindCodes.Generator ? 1 : 0),
            contribution.GambitContributionCount + (rowSourceKindCode == AffordanceSourceKindCodes.Gambit ? 1 : 0));
    }

    private static float CandidateWeight(float baseWeight, AffordanceContribution affordance, float affordanceWeight) =>
        ClampCandidateWeight(baseWeight + (affordance.Score * affordanceWeight * AffordanceWeightMultiplier));

    private static float ClampCandidateWeight(float weight) => weight <= 0.01f ? 0.01f : weight;

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
}
