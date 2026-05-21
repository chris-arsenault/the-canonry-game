namespace Engine.Kernel;

internal sealed partial class LowAltitudeVerbProgramRuntime
{
    private const float CultureTraceRawInfluence = WorldState.CultureTraceRawMass;
    private const float CultureSupportedRawInfluence = WorldState.CultureClaimedRawMass;
    private const float CultureStrongRawInfluence = WorldState.CultureControlledRawMass;
    private const float CultureNexusRawInfluence = WorldState.CultureNexusRawMass;
    private const float CultureFieldRawInfluence = WorldState.CultureFullRawMass;
    private const float CultureRingTwoConnectionWeight = 7f / 20f;
    private const float CultureRouteBorderSupportWeight = 7f / 20f;
    private const int CultureAdjacentNeighborCount = 6;

    private static readonly int[] CultureNeighborDq =
    [
        1, 1, 0, -1, -1, 0,
        2, 2, 2, 1, 0, -1, -2, -2, -2, -1, 0, 1,
    ];

    private static readonly int[] CultureNeighborDr =
    [
        0, -1, -1, 0, 1, 1,
        0, -1, -2, -2, -2, -1, 0, 1, 2, 2, 2, 1,
    ];

    private readonly struct CultureInfluenceNeighborhood
    {
        public CultureInfluenceNeighborhood(
            float centerRaw,
            float adjacentRawTotal,
            float strongestAdjacentRaw,
            int supportedAdjacentCount,
            float ringTwoRawTotal,
            float strongestRingTwoRaw,
            int supportedRingTwoCount)
        {
            CenterRaw = centerRaw;
            AdjacentRawTotal = adjacentRawTotal;
            StrongestAdjacentRaw = strongestAdjacentRaw;
            SupportedAdjacentCount = supportedAdjacentCount;
            RingTwoRawTotal = ringTwoRawTotal;
            StrongestRingTwoRaw = strongestRingTwoRaw;
            SupportedRingTwoCount = supportedRingTwoCount;
        }

        public float CenterRaw { get; }

        public float AdjacentRawTotal { get; }

        public float StrongestAdjacentRaw { get; }

        public int SupportedAdjacentCount { get; }

        public float RingTwoRawTotal { get; }

        public float StrongestRingTwoRaw { get; }

        public int SupportedRingTwoCount { get; }

        public float ConnectedRaw => CenterRaw + AdjacentRawTotal + (RingTwoRawTotal * CultureRingTwoConnectionWeight);
    }

    private float OwnCultureExpansionDriveAround(
        WorldState world,
        int entityId,
        int rule,
        int planeCode,
        int centerQ,
        int centerR,
        int radius,
        CultureInfluenceComponentScratch components)
    {
        var cultureId = world.EntityCultureId(entityId);
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
                        world,
                        entityId,
                        cultureId,
                        targetingCode,
                        planeCode,
                        centerQ + dq,
                        centerR + dr,
                        components));
            }
        }

        return best;
    }

    private float TravelCultureRouteMultiplier(
        WorldState world,
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

        var cultureId = world.EntityCultureId(entityId);
        if (cultureId <= 0 ||
            !CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 1f;
        }

        var ownCultureInfluence = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        var foreignCultureInfluence = StrongestOtherCultureInfluence(world, cultureId, planeCode, q, r, out _);
        var neighborhood = CultureNeighborhood(world, cultureId, planeCode, q, r);
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
        WorldState world,
        int entityId,
        int cultureId,
        int targetingCode,
        int planeCode,
        int q,
        int r,
        CultureInfluenceComponentScratch components)
    {
        if (!CellTargetCoordinateAllowed(world, planeCode, q, r))
        {
            return 0f;
        }

        var ownCultureInfluence = world.GetCellCultureInfluence(cultureId, planeCode, q, r);
        var neighborhood = CultureNeighborhood(world, cultureId, planeCode, q, r);
        if (!OwnCultureExpansionTargetAllowed(targetingCode, neighborhood.CenterRaw, neighborhood))
        {
            return 0f;
        }

        var foreignCultureInfluence = StrongestOtherCultureInfluence(world, cultureId, planeCode, q, r, out var foreignCultureId);
        var foreignCultureRaw = foreignCultureId > 0
            ? world.GetCellCultureInfluenceRaw(foreignCultureId, planeCode, q, r)
            : 0f;
        return OwnCultureExpansionDrive(
            targetingCode,
            ownCultureInfluence,
            foreignCultureInfluence,
            foreignCultureRaw,
            neighborhood.CenterRaw,
            neighborhood,
            components.AttachmentAt(q, r));
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

    private static CultureInfluenceNeighborhood CultureNeighborhood(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var centerRaw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var adjacentRawTotal = 0f;
        var strongestAdjacentRaw = 0f;
        var supportedAdjacentCount = 0;
        var ringTwoRawTotal = 0f;
        var strongestRingTwoRaw = 0f;
        var supportedRingTwoCount = 0;
        for (var i = 0; i < CultureNeighborDq.Length; i++)
        {
            if (i < CultureAdjacentNeighborCount)
            {
                AddCultureRaw(
                    world,
                    cultureId,
                    planeCode,
                    q + CultureNeighborDq[i],
                    r + CultureNeighborDr[i],
                    ref adjacentRawTotal,
                    ref strongestAdjacentRaw,
                    ref supportedAdjacentCount);
                continue;
            }

            AddCultureRaw(
                world,
                cultureId,
                planeCode,
                q + CultureNeighborDq[i],
                r + CultureNeighborDr[i],
                ref ringTwoRawTotal,
                ref strongestRingTwoRaw,
                ref supportedRingTwoCount);
        }

        return new CultureInfluenceNeighborhood(
            centerRaw,
            adjacentRawTotal,
            strongestAdjacentRaw,
            supportedAdjacentCount,
            ringTwoRawTotal,
            strongestRingTwoRaw,
            supportedRingTwoCount);
    }

    private static void AddCultureRaw(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r,
        ref float adjacentRawTotal,
        ref float strongestAdjacentRaw,
        ref int supportedAdjacentCount)
    {
        var raw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        adjacentRawTotal += raw;
        strongestAdjacentRaw = global::System.MathF.Max(strongestAdjacentRaw, raw);
        if (raw >= CultureTraceRawInfluence)
        {
            supportedAdjacentCount++;
        }
    }

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
            attachment.ComponentCount);
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
            out var foreignCultureId);
        if (foreignCultureInfluence <= 0f)
        {
            return;
        }

        var contestSupport = 0f;
        if (actorCultureId > 0)
        {
            var ownCultureInfluence = simulation.World.GetCellCultureInfluence(actorCultureId, planeCode, q, r);
            var ownCultureRaw = simulation.World.GetCellCultureInfluenceRaw(actorCultureId, planeCode, q, r);
            var foreignCultureRaw = foreignCultureId > 0
                ? simulation.World.GetCellCultureInfluenceRaw(foreignCultureId, planeCode, q, r)
                : 0f;
            var attachment = _cultureInfluenceComponents.AttachmentAt(q, r);
            var input = new CultureInfluenceTargetScoreInput(
                LowAltitudeInfluenceTargetingCodes.Contest,
                ownCultureInfluence,
                ownCultureRaw,
                foreignCultureInfluence,
                foreignCultureRaw,
                attachment.ComponentMass,
                attachment.AdjacentSupportedCount,
                attachment.ComponentCount);
            contestSupport = CultureInfluenceTargetScorer.Score(input);
        }

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
        var baseWeight = scalars.Weight * (0.25f + presenceBias) * contestSupport * distance * saturationDivisor * recency;
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
