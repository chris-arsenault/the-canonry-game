namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
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
        var affordance = GeneratorAffordanceScore(world.EntityKindId(entityId), targetKindId, targetKindId);
        var weight = LocationWeight(simulation, entityId, rule, planeCode, q, r, tick, affordance, targetEntityId);
        weight += RelationshipAffinityWeight(world, entityId, targetEntityId, rule);
        totalWeight += weight;
        if (simulation.NextFloat() * totalWeight <= weight)
        {
            selected = Candidate(rule, planeCode, targetEntityId, targetKindId, q, r, s, weight);
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

    private float LocationWeight(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r,
        int tick,
        float generatorAffordance,
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
        var generatorAffordanceWeight = Data.GeneratorAffordanceWeight(rule);
        ApplyTraitWeightModifiers(
            simulation.World,
            entityId,
            rule,
            ref weight,
            ref exploreWeight,
            ref stalenessWeight,
            ref hotspotWeight,
            ref crowdingWeight,
            ref generatorAffordanceWeight);
        weight += exploreWeight / (1f + snapshot.VisitCount);
        weight += stalenessWeight * staleness;
        weight += hotspotWeight * (heatScore + attractorScore);
        weight += generatorAffordanceWeight * generatorAffordance;
        weight -= crowdingWeight * (snapshot.CurrentOccupancy + snapshot.CrowdingLoad);
        return weight <= 0.01f ? 0.01f : weight;
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
        ref float generatorAffordanceWeight)
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
        generatorAffordanceWeight += totals.GeneratorAffordanceWeight;
    }

    private float GeneratorAffordanceScore(int actorKindId, int candidateKindId, int bindingKindId)
    {
        if (candidateKindId == 0)
        {
            return 0f;
        }

        var score = 0f;
        for (var pair = 1; pair <= Data.AffordanceKindPairCount; pair++)
        {
            if (Data.AffordanceActorKindId(pair) == actorKindId &&
                Data.AffordanceCandidateKindId(pair) == candidateKindId &&
                Data.AffordanceBindingKindId(pair) == bindingKindId)
            {
                score += Data.AffordanceScore(pair);
            }
        }

        return score > 3f ? 3f : score;
    }
}
