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
        var weight = Data.RelationshipAffinityWeight(rule) + TraitRelationshipAffinityWeightDelta(world, entityId, rule);
        if (relationshipKindId == 0 || weight == 0f)
        {
            return 0f;
        }

        var strength = BestRelationshipStrengthBetween(
            world,
            entityId,
            targetEntityId,
            relationshipKindId,
            Data.AffinityRelationshipDirectionCode(rule));
        return strength * weight;
    }

    private float TraitRelationshipAffinityWeightDelta(WorldState world, int entityId, int rule)
    {
        var kindId = world.EntityKindId(entityId);
        var verbId = Data.VerbId(rule);
        var delta = 0f;
        for (var modifier = 1; modifier <= Data.TraitWeightModifierCount; modifier++)
        {
            if (Data.TraitModifierKindId(modifier) == kindId &&
                TraitModifierMatchesVerb(modifier, verbId) &&
                world.HasTrait(entityId, Data.TraitModifierTraitId(modifier)))
            {
                delta += Data.TraitModifierRelationshipAffinityWeightDelta(modifier);
            }
        }

        return delta;
    }

    private static float BestRelationshipStrengthBetween(
        WorldState world,
        int entityId,
        int targetEntityId,
        int relationshipKindId,
        int directionCode)
    {
        var best = 0f;
        if (directionCode is LowAltitudeVerbRelationshipDirectionCodes.Outgoing
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            best = Max(best, RelationshipStrengthBetween(world, entityId, targetEntityId, relationshipKindId));
        }

        if (directionCode is LowAltitudeVerbRelationshipDirectionCodes.Incoming
            or LowAltitudeVerbRelationshipDirectionCodes.Either)
        {
            best = Max(best, RelationshipStrengthBetween(world, targetEntityId, entityId, relationshipKindId));
        }

        return best;
    }

    private static float RelationshipStrengthBetween(WorldState world, int sourceId, int targetId, int relationshipKindId)
    {
        var relationshipId = world.FindRelationship(sourceId, targetId, relationshipKindId);
        return relationshipId == 0 ? 0f : world.RelationshipStrength(relationshipId);
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
        var kindId = world.EntityKindId(entityId);
        var verbId = Data.VerbId(rule);
        for (var modifier = 1; modifier <= Data.TraitWeightModifierCount; modifier++)
        {
            if (Data.TraitModifierKindId(modifier) != kindId ||
                !TraitModifierMatchesVerb(modifier, verbId) ||
                !world.HasTrait(entityId, Data.TraitModifierTraitId(modifier)))
            {
                continue;
            }

            weight += Data.TraitModifierWeightDelta(modifier);
            exploreWeight += Data.TraitModifierExploreWeightDelta(modifier);
            stalenessWeight += Data.TraitModifierStalenessWeightDelta(modifier);
            hotspotWeight += Data.TraitModifierHotspotWeightDelta(modifier);
            crowdingWeight += Data.TraitModifierCrowdingWeightDelta(modifier);
            generatorAffordanceWeight += Data.TraitModifierGeneratorAffordanceWeightDelta(modifier);
        }
    }

    private bool TraitModifierMatchesVerb(int modifier, int verbId)
    {
        var modifierVerbId = Data.TraitModifierVerbId(modifier);
        return modifierVerbId == 0 || modifierVerbId == verbId;
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
