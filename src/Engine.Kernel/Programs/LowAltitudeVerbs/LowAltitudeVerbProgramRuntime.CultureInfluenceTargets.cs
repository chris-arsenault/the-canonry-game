namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private const float CultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private const float CultureSupportedRawInfluence = WorldState.CultureClaimedRawMass;
    private const float CultureStrongRawInfluence = WorldState.CultureControlledRawMass;
    private const float CultureNexusRawInfluence = WorldState.CultureNexusRawMass;
    private const float CultureFieldRawInfluence = WorldState.CultureFullRawMass;
    private const float CultureRouteBorderSupportWeight = 7f / 20f;

    private readonly struct CultureInfluenceNeighborhood
    {
        public CultureInfluenceNeighborhood(
            float centerRaw,
            float adjacentRawTotal,
            float strongestAdjacentRaw,
            int supportedAdjacentCount,
            float ringTwoRawTotal,
            float strongestRingTwoRaw,
            int supportedRingTwoCount,
            float connectedRaw)
        {
            CenterRaw = centerRaw;
            AdjacentRawTotal = adjacentRawTotal;
            StrongestAdjacentRaw = strongestAdjacentRaw;
            SupportedAdjacentCount = supportedAdjacentCount;
            RingTwoRawTotal = ringTwoRawTotal;
            StrongestRingTwoRaw = strongestRingTwoRaw;
            SupportedRingTwoCount = supportedRingTwoCount;
            ConnectedRaw = connectedRaw;
        }

        public float CenterRaw { get; }

        public float AdjacentRawTotal { get; }

        public float StrongestAdjacentRaw { get; }

        public int SupportedAdjacentCount { get; }

        public float RingTwoRawTotal { get; }

        public float StrongestRingTwoRaw { get; }

        public int SupportedRingTwoCount { get; }

        public float ConnectedRaw { get; }
    }

    private readonly record struct CultureInfluenceAttachment(
        float ComponentMass,
        int AdjacentSupportedCount,
        int ComponentCount,
        float LargestComponentSupport);

    private static CultureInfluenceNeighborhood CultureNeighborhood(in CultureBoundaryCell boundary)
    {
        return new CultureInfluenceNeighborhood(
            boundary.RawMass,
            boundary.LocalRawMass - boundary.RawMass,
            boundary.StrongestAdjacentRawMass,
            boundary.SupportedAdjacentCount,
            boundary.RingTwoRawMass,
            boundary.StrongestRingTwoRawMass,
            boundary.SupportedRingTwoCount,
            boundary.ConnectedRawMass);
    }

    private float OwnCultureExpansionDriveAround(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int centerQ,
        int centerR,
        int radius)
    {
        var cultureId = simulation.World.EntityCultureId(entityId);
        if (cultureId <= 0)
        {
            return 0f;
        }

        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        var best = 0f;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                best = global::System.MathF.Max(
                    best,
                    OwnCultureExpansionDriveAt(
                        simulation,
                        entityId,
                        cultureId,
                        targetingCode,
                        planeCode,
                        centerQ + dq,
                        centerR + dr));
            }
        }

        return best;
    }

    private float TravelCultureRouteMultiplier(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor ||
            Data.InfluenceDelta(rule) <= 0f)
        {
            return 1f;
        }

        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0 ||
            !CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 1f;
        }

        var boundary = simulation.CultureBoundaries.CellAt(world, cultureId, planeCode, q, r);
        var ownCultureInfluence = boundary.Share;
        var foreignCultureInfluence = boundary.ForeignShare;
        var neighborhood = CultureNeighborhood(boundary);
        var connectedSupport = CultureConnectedSupport(neighborhood);
        var borderSupport = CultureEdgeSupport(neighborhood);
        var culturePresence = Clamp01(ownCultureInfluence / 100f);
        var foreignCulturePresence = Clamp01(foreignCultureInfluence / 100f);
        return 0.55f +
            (0.95f * connectedSupport) +
            (CultureRouteBorderSupportWeight * borderSupport) +
            (0.25f * culturePresence) +
            (0.2f * foreignCulturePresence);
    }

    private float OwnCultureExpansionDriveAt(
        Simulation simulation,
        int entityId,
        int cultureId,
        int targetingCode,
        int planeCode,
        int q,
        int r)
    {
        var world = simulation.World;
        if (!CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 0f;
        }

        var boundary = simulation.CultureBoundaries.CellAt(world, cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(boundary);
        if (!OwnCultureExpansionTargetAllowed(targetingCode, boundary.RawMass, neighborhood))
        {
            return 0f;
        }

        return OwnCultureExpansionDrive(
            targetingCode,
            boundary.Share,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            boundary.RawMass,
            neighborhood,
            CultureAttachment(boundary));
    }

    private static bool OwnCultureExpansionTargetAllowed(
        int targetingCode,
        float ownCultureRaw,
        in CultureInfluenceNeighborhood neighborhood)
    {
        if (ownCultureRaw >= WorldState.CultureFullRawMass)
        {
            return false;
        }

        if (targetingCode == LowAltitudeInfluenceTargetingCodes.Reinforce)
        {
            return ownCultureRaw >= CultureTraceRawInfluence ||
                neighborhood.StrongestAdjacentRaw >= CultureSupportedRawInfluence;
        }

        if (targetingCode == LowAltitudeInfluenceTargetingCodes.Spread)
        {
            return SpreadTargetHasSupport(ownCultureRaw, neighborhood);
        }

        return ownCultureRaw > 0f ||
            neighborhood.AdjacentRawTotal > 0f;
    }

    private static bool SpreadTargetHasSupport(float ownCultureRaw)
    {
        return ownCultureRaw >= CultureTraceRawInfluence;
    }

    private static bool SpreadTargetHasSupport(
        float ownCultureRaw,
        in CultureInfluenceNeighborhood neighborhood)
    {
        return SpreadTargetHasSupport(ownCultureRaw) ||
            neighborhood.StrongestAdjacentRaw >= CultureSupportedRawInfluence ||
            neighborhood.AdjacentRawTotal >= CultureStrongRawInfluence ||
            neighborhood.StrongestRingTwoRaw >= CultureStrongRawInfluence ||
            neighborhood.RingTwoRawTotal >= CultureNexusRawInfluence;
    }

    private static CultureInfluenceAttachment CultureAttachment(in CultureBoundaryCell boundary) =>
        new(
            boundary.AttachedComponentScoreMass,
            boundary.SupportedAdjacentCount,
            boundary.AttachedComponentCount,
            boundary.IsLargestComponent ? 1f : 0f);

    private float OwnCultureExpansionDrive(
        int targetingCode,
        float ownCultureInfluence,
        float foreignCultureInfluence,
        float foreignCultureRaw,
        float ownCultureRaw,
        in CultureInfluenceNeighborhood neighborhood,
        in CultureInfluenceAttachment attachment)
    {
        var input = new CultureInfluenceTargetScoreInput(
            targetingCode,
            ownCultureInfluence,
            ownCultureRaw,
            foreignCultureInfluence,
            foreignCultureRaw,
            attachment.ComponentMass,
            attachment.AdjacentSupportedCount,
            attachment.ComponentCount,
            attachment.LargestComponentSupport);
        return CultureInfluenceTargetScorer.Score(input);
    }

    private static float CultureConnectedSupport(in CultureInfluenceNeighborhood neighborhood) =>
        Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);

    private static float CultureEdgeSupport(in CultureInfluenceNeighborhood neighborhood)
    {
        var adjacentSupport = Clamp01(neighborhood.AdjacentRawTotal / CultureNexusRawInfluence);
        var centerHeadroom = Clamp01((CultureStrongRawInfluence - neighborhood.CenterRaw) / CultureStrongRawInfluence);
        return adjacentSupport * centerHeadroom;
    }

    private static float OwnCultureFieldOpportunity(float culturalExpansionDrive) =>
        Clamp01(culturalExpansionDrive);

    private int EffectiveInfluenceTargetingCode(int rule)
    {
        var targetingCode = Data.InfluenceTargetingCode(rule);
        if (targetingCode != LowAltitudeInfluenceTargetingCodes.None)
        {
            return targetingCode;
        }

        var operationCode = Data.OperationCode(rule);
        if (operationCode == ProgramOpcodeCodes.SubtractOtherCellScalar)
        {
            return LowAltitudeInfluenceTargetingCodes.Contest;
        }

        return OperationBuildsOwnedInfluence(operationCode)
            ? LowAltitudeInfluenceTargetingCodes.Spread
            : LowAltitudeInfluenceTargetingCodes.None;
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

    private static float ForeignCultureInfluenceAt(
        Simulation simulation,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int foreignCultureId)
    {
        if (ownCultureId > 0)
        {
            var boundary = simulation.CultureBoundaries.CellAt(simulation.World, ownCultureId, planeCode, q, r);
            foreignCultureId = boundary.ForeignCultureId;
            return boundary.ForeignShare;
        }

        return StrongestOtherCultureInfluence(simulation.World, ownCultureId, planeCode, q, r, out foreignCultureId);
    }

    /// <summary>
    /// Contest score for the cell plus the standing tide's other hand:
    /// genuinely contested cells (both sides established) draw the fight -
    /// verbs of both cultures weight the front.
    /// </summary>
    private const float FrontierEstablishedRawMass = 4f;
    private const float FrontierWeightMultiplier = 1.75f;

    private (float ContestSupport, float Frontier) ContestSupportAndFrontier(
        Simulation simulation,
        int actorCultureId,
        int planeCode,
        int q,
        int r)
    {
        if (actorCultureId <= 0)
        {
            return (0f, 1f);
        }

        var boundary = simulation.CultureBoundaries.CellAt(simulation.World, actorCultureId, planeCode, q, r);
        var frontier = boundary.RawMass >= FrontierEstablishedRawMass && boundary.ForeignRawMass >= FrontierEstablishedRawMass
            ? FrontierWeightMultiplier
            : 1f;
        var attachment = CultureAttachment(boundary);
        var input = new CultureInfluenceTargetScoreInput(
            LowAltitudeInfluenceTargetingCodes.Contest,
            boundary.Share,
            boundary.RawMass,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            attachment.ComponentMass,
            attachment.AdjacentSupportedCount,
            attachment.ComponentCount,
            attachment.LargestComponentSupport);
        return (CultureInfluenceTargetScorer.Score(input), frontier);
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
        var foreignCultureInfluence = ForeignCultureInfluenceAt(
            simulation,
            actorCultureId,
            planeCode,
            q,
            r,
            out _);
        if (foreignCultureInfluence <= 0f)
        {
            return;
        }

        var (contestSupport, frontier) = ContestSupportAndFrontier(simulation, actorCultureId, planeCode, q, r);

        if (contestSupport <= 0f)
        {
            return;
        }

        var influenceSignal = global::System.MathF.Max(influence, foreignCultureInfluence);
        var presenceBias = Tuning.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor * (influenceSignal / 100f);
        var distance = DistancePref(rule, TargetSelectorCodes.OtherMapValue, actorQ, actorR, q, r);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * (0.25f + presenceBias) * contestSupport * distance * saturationDivisor * recency * frontier;
        var influencerKindId = influencerId > 0 ? simulation.World.EntityKindId(influencerId) : actorKindId;
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            influencerKindId,
            AffordancePolarityCodes.Reduce,
            influenceSignal);
        sumAffordanceScore += affordance.Score + (0.75f * contestSupport);
        var candidate = Candidate(rule, planeCode, 0, influencerKindId, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, candidate, ref totalWeight, ref selected);
    }
}
