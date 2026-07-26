namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private static readonly FixedScalar CultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private static readonly FixedScalar CultureSupportedRawInfluence = WorldState.CultureClaimedRawMass;
    private static readonly FixedScalar CultureStrongRawInfluence = WorldState.CultureControlledRawMass;
    private static readonly FixedScalar CultureNexusRawInfluence = WorldState.CultureNexusRawMass;
    private static readonly FixedScalar CultureFieldRawInfluence = WorldState.CultureFullRawMass;
    private static readonly FixedScalar CultureRouteBorderSupportWeight = FixedScalar.FromRatio(7, 20);

    private readonly struct CultureInfluenceNeighborhood
    {
        public CultureInfluenceNeighborhood(
            FixedScalar centerRaw,
            FixedScalar adjacentRawTotal,
            FixedScalar strongestAdjacentRaw,
            int supportedAdjacentCount,
            FixedScalar ringTwoRawTotal,
            FixedScalar strongestRingTwoRaw,
            int supportedRingTwoCount,
            FixedScalar connectedRaw)
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

        public FixedScalar CenterRaw { get; }

        public FixedScalar AdjacentRawTotal { get; }

        public FixedScalar StrongestAdjacentRaw { get; }

        public int SupportedAdjacentCount { get; }

        public FixedScalar RingTwoRawTotal { get; }

        public FixedScalar StrongestRingTwoRaw { get; }

        public int SupportedRingTwoCount { get; }

        public FixedScalar ConnectedRaw { get; }
    }

    private readonly record struct CultureInfluenceAttachment(
        FixedScalar ConnectedRawMass,
        int AdjacentSupportedCount,
        FixedScalar StrongCenterSupport);

    private static CultureInfluenceNeighborhood CultureNeighborhood(in CultureLocalityCell boundary)
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

    private FixedScalar OwnCultureExpansionDriveAround(
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
            return 0;
        }

        var targetingCode = EffectiveInfluenceTargetingCode(rule);
        var best = FixedScalar.Zero;
        for (var dq = -radius; dq <= radius; dq++)
        {
            var minDr = global::System.Math.Max(-radius, -dq - radius);
            var maxDr = global::System.Math.Min(radius, -dq + radius);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                best = FixedScalar.Max(
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

    private FixedScalar TravelCultureRouteMultiplier(
        Simulation simulation,
        int entityId,
        int rule,
        int planeCode,
        int q,
        int r)
    {
        if (Data.OperationCode(rule) != ProgramOpcodeCodes.MoveActor ||
            Data.InfluenceDelta(rule) <= 0)
        {
            return 1;
        }

        var world = simulation.World;
        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0 ||
            !CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 1;
        }

        var boundary = simulation.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
        var ownCultureInfluence = boundary.Share;
        var foreignCultureInfluence = boundary.ForeignShare;
        var neighborhood = CultureNeighborhood(boundary);
        var connectedSupport = CultureConnectedSupport(neighborhood);
        var borderSupport = CultureEdgeSupport(neighborhood);
        var culturePresence = Clamp01(ownCultureInfluence / 100);
        var foreignCulturePresence = Clamp01(foreignCultureInfluence / 100);
        return FixedScalar.FromUnits(550000L) +
            (FixedScalar.FromUnits(950000L) * connectedSupport) +
            (CultureRouteBorderSupportWeight * borderSupport) +
            (FixedScalar.FromUnits(250000L) * culturePresence) +
            (FixedScalar.FromUnits(200000L) * foreignCulturePresence);
    }

    private FixedScalar OwnCultureExpansionDriveAt(
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
            return 0;
        }

        var boundary = simulation.CultureLocality.CellAt(world, cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(boundary);
        if (!OwnCultureExpansionTargetAllowed(targetingCode, boundary.RawMass, neighborhood))
        {
            return 0;
        }

        var drive = OwnCultureExpansionDrive(
            targetingCode,
            boundary.Share,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            boundary.RawMass,
            neighborhood,
            CultureAttachment(boundary));

        // Territory grows where the land can hold it and the culture wants
        // it: geography and identity bend the topological drive on the
        // physical plane.
        return planeCode == PlaneCodes.Physical
            ? drive * simulation.CultureGrowthMultiplier(cultureId, q, r)
            : drive;
    }

    private static bool OwnCultureExpansionTargetAllowed(
        int targetingCode,
        FixedScalar ownCultureRaw,
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

        return ownCultureRaw > 0 ||
            neighborhood.AdjacentRawTotal > 0;
    }

    private static bool SpreadTargetHasSupport(FixedScalar ownCultureRaw)
    {
        return ownCultureRaw >= CultureTraceRawInfluence;
    }

    private static bool SpreadTargetHasSupport(
        FixedScalar ownCultureRaw,
        in CultureInfluenceNeighborhood neighborhood)
    {
        return SpreadTargetHasSupport(ownCultureRaw) ||
            neighborhood.StrongestAdjacentRaw >= CultureSupportedRawInfluence ||
            neighborhood.AdjacentRawTotal >= CultureStrongRawInfluence ||
            neighborhood.StrongestRingTwoRaw >= CultureStrongRawInfluence ||
            neighborhood.RingTwoRawTotal >= CultureNexusRawInfluence;
    }

    private static CultureInfluenceAttachment CultureAttachment(in CultureLocalityCell boundary) =>
        new(
            boundary.ConnectedRawMass,
            boundary.SupportedAdjacentCount,
            boundary.RawMass >= WorldState.CultureControlledRawMass ? 1 : 0);

    private FixedScalar OwnCultureExpansionDrive(
        int targetingCode,
        FixedScalar ownCultureInfluence,
        FixedScalar foreignCultureInfluence,
        FixedScalar foreignCultureRaw,
        FixedScalar ownCultureRaw,
        in CultureInfluenceNeighborhood neighborhood,
        in CultureInfluenceAttachment attachment)
    {
        var input = new CultureInfluenceTargetScoreInput(
            targetingCode,
            ownCultureInfluence,
            ownCultureRaw,
            foreignCultureInfluence,
            foreignCultureRaw,
            attachment.ConnectedRawMass,
            attachment.AdjacentSupportedCount,
            attachment.StrongCenterSupport);
        return CultureInfluenceTargetScorer.Score(input);
    }

    private static FixedScalar CultureConnectedSupport(in CultureInfluenceNeighborhood neighborhood) =>
        Clamp01(neighborhood.ConnectedRaw / CultureFieldRawInfluence);

    private static FixedScalar CultureEdgeSupport(in CultureInfluenceNeighborhood neighborhood)
    {
        var adjacentSupport = Clamp01(neighborhood.AdjacentRawTotal / CultureNexusRawInfluence);
        var centerHeadroom = Clamp01((CultureStrongRawInfluence - neighborhood.CenterRaw) / CultureStrongRawInfluence);
        return adjacentSupport * centerHeadroom;
    }

    private static FixedScalar OwnCultureFieldOpportunity(FixedScalar culturalExpansionDrive) =>
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

    private static FixedScalar StrongestOtherCultureInfluence(
        WorldState world,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int cultureId)
    {
        cultureId = 0;
        var influence = FixedScalar.Zero;
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

    private static FixedScalar ForeignCultureInfluenceAt(
        Simulation simulation,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int foreignCultureId)
    {
        if (ownCultureId > 0)
        {
            var boundary = simulation.CultureLocality.CellAt(simulation.World, ownCultureId, planeCode, q, r);
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
    private static readonly FixedScalar FrontierEstablishedRawMass = 4;
    private static readonly FixedScalar FrontierWeightMultiplier = FixedScalar.FromUnits(1750000L);

    private (FixedScalar ContestSupport, FixedScalar Frontier) ContestSupportAndFrontier(
        Simulation simulation,
        int actorCultureId,
        int planeCode,
        int q,
        int r)
    {
        if (actorCultureId <= 0)
        {
            return (0, 1);
        }

        var boundary = simulation.CultureLocality.CellAt(simulation.World, actorCultureId, planeCode, q, r);
        var frontier = boundary.RawMass >= FrontierEstablishedRawMass && boundary.ForeignRawMass >= FrontierEstablishedRawMass
            ? FrontierWeightMultiplier
            : 1;
        var attachment = CultureAttachment(boundary);
        var input = new CultureInfluenceTargetScoreInput(
            LowAltitudeInfluenceTargetingCodes.Contest,
            boundary.Share,
            boundary.RawMass,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            attachment.ConnectedRawMass,
            attachment.AdjacentSupportedCount,
            attachment.StrongCenterSupport);
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
        FixedScalar influence,
        int tick,
        ref FixedScalar sumAffordanceScore,
        ref VerbTargetCandidate selected)
    {
        var foreignCultureInfluence = ForeignCultureInfluenceAt(
            simulation,
            actorCultureId,
            planeCode,
            q,
            r,
            out _);
        if (foreignCultureInfluence <= 0)
        {
            return;
        }

        var (contestSupport, frontier) = ContestSupportAndFrontier(simulation, actorCultureId, planeCode, q, r);

        if (contestSupport <= 0)
        {
            return;
        }

        var influenceSignal = FixedScalar.Max(influence, foreignCultureInfluence);
        var presenceBias = Tuning.LowAltitudeVerbs.ForeignInfluencePresenceSamplingFactor * (influenceSignal / 100);
        var distance = DistancePref(rule, TargetSelectorCodes.OtherMapValue, actorQ, actorR, q, r);
        var saturationDivisor = CellSaturationDivisor(simulation, planeCode, q, r);
        var recencyKey = LowAltitudeVerbProgramState.PackCellTargetKey(planeCode, q, r);
        var recency = ComputeRecencyMultiplier(entityId, tick, recencyKey);
        var baseWeight = scalars.Weight * (FixedScalar.FromUnits(250000L) + presenceBias) * contestSupport * distance * saturationDivisor * recency * frontier;
        var influencerKindId = influencerId > 0 ? simulation.World.EntityKindId(influencerId) : actorKindId;
        var affordance = CombineAffordancesForInfluenceTarget(
            actorKindId,
            planeCode,
            Data.OperationCode(rule),
            influencerKindId,
            AffordancePolarityCodes.Reduce,
            influenceSignal);
        sumAffordanceScore += affordance.Score + (FixedScalar.FromUnits(750000L) * contestSupport);
        var candidate = Candidate(rule, planeCode, 0, influencerKindId, q, r, TargetS(planeCode, q, r), baseWeight, affordance, scalars.OpportunityBonus);
        ConsiderCandidate(simulation, entityId, candidate, ref selected);
    }
}
