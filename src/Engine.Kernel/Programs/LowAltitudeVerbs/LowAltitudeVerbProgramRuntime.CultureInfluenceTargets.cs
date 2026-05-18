namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private float OwnCultureExpansionDriveAround(
        WorldState world,
        int entityId,
        int planeCode,
        int centerQ,
        int centerR,
        int radius)
    {
        var cultureId = world.EntityCultureId(entityId);
        var best = 0f;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                best = global::System.MathF.Max(
                    best,
                    OwnCultureExpansionDriveAt(world, entityId, cultureId, planeCode, centerQ + dq, centerR + dr));
            }
        }

        return best;
    }

    private float OwnCultureExpansionDriveAt(
        WorldState world,
        int entityId,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        if (!CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 0f;
        }

        var entityInfluence = world.GetCellInfluence(entityId, planeCode, q, r);
        var ownCultureInfluence = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        if (!OwnCultureExpansionTargetAllowed(world, cultureId, planeCode, q, r, ownCultureInfluence, entityInfluence))
        {
            return 0f;
        }

        var foreignCultureInfluence = StrongestOtherCultureInfluence(world, cultureId, planeCode, q, r, out _);
        return OwnCultureExpansionDrive(ownCultureInfluence, foreignCultureInfluence, entityInfluence);
    }

    private static bool OwnCultureExpansionTargetAllowed(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        float ownCultureInfluence,
        float entityInfluence)
    {
        if (ownCultureInfluence >= 99.5f && entityInfluence >= 99.5f)
        {
            return false;
        }

        return ownCultureInfluence > 0f ||
            entityInfluence > 0f ||
            HasAdjacentCultureInfluence(world, cultureId, planeCode, q, r);
    }

    private static bool HasAdjacentCultureInfluence(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        return CultureInfluenceAt(world, cultureId, planeCode, q + 1, r) > 0f ||
            CultureInfluenceAt(world, cultureId, planeCode, q + 1, r - 1) > 0f ||
            CultureInfluenceAt(world, cultureId, planeCode, q, r - 1) > 0f ||
            CultureInfluenceAt(world, cultureId, planeCode, q - 1, r) > 0f ||
            CultureInfluenceAt(world, cultureId, planeCode, q - 1, r + 1) > 0f ||
            CultureInfluenceAt(world, cultureId, planeCode, q, r + 1) > 0f;
    }

    private static float CultureInfluenceAt(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r) =>
        world.GetCellCultureInfluence(cultureId, planeCode, q, r);

    private float OwnCultureExpansionDrive(
        float ownCultureInfluence,
        float foreignCultureInfluence,
        float entityInfluence)
    {
        var culturalShortfall = global::System.MathF.Max(0f, (100f - ownCultureInfluence) / 100f);
        var entityShortfall = global::System.MathF.Max(0f, (100f - entityInfluence) / 100f);
        var frontierDrive = global::System.MathF.Max(0f, foreignCultureInfluence / 100f);
        var drive =
            0.2f +
            (Tuning.LowAltitudeVerbs.InfluenceShortfallSamplingFactor * culturalShortfall) +
            (0.25f * entityShortfall) +
            (0.75f * frontierDrive);
        if (ownCultureInfluence <= 0f && entityInfluence <= 0f)
        {
            drive *= 0.8f;
        }

        return drive;
    }

    private static float StrongestOtherCultureInfluence(
        WorldState world,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int cultureId)
    {
        cultureId = 0;
        var influence = 0f;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == ownCultureId)
            {
                continue;
            }

            var candidateInfluence = world.GetCellCultureInfluence(otherCultureId, planeCode, q, r);
            if (candidateInfluence <= influence)
            {
                continue;
            }

            influence = candidateInfluence;
            cultureId = otherCultureId;
        }

        return influence;
    }

    private void AddForeignInfluenceCandidate(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int actorKindId,
        int actorCultureId,
        EffectiveVerbScalars scalars,
        int actorQ,
        int actorR,
        int q,
        int r,
        int influencerId,
        float influence,
        int tick,
        ref float totalWeight,
        ref float sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var foreignCultureInfluence = StrongestOtherCultureInfluence(
            simulation.World,
            actorCultureId,
            planeCode,
            q,
            r,
            out _);
        if (foreignCultureInfluence <= 0f)
        {
            return;
        }

        var influenceSignal = global::System.MathF.Max(influence, foreignCultureInfluence);
        var presenceBias = Tuning.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor * (influenceSignal / 100f);
        var distance = DistancePref(rule, TargetSelectorCodes.OtherMapValue, actorQ, actorR, q, r);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackEntityTargetKey(influencerId);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * (0.25f + presenceBias) * distance * saturationDivisor * recency;
        var influencerKindId = simulation.World.EntityKindId(influencerId);
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            influencerKindId,
            AffordancePolarityCodes.Reduce,
            influenceSignal);
        sumAffordanceScore += affordance.Score;
        var candidate = Candidate(rule, planeCode, influencerId, influencerKindId, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
    }
}
