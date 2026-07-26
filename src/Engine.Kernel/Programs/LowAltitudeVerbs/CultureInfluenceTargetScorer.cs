namespace Engine.Kernel;

internal readonly record struct CultureInfluenceTargetScoreInput(
    int TargetingCode,
    FixedScalar OwnShare,
    FixedScalar OwnRaw,
    FixedScalar ForeignShare,
    FixedScalar ForeignRaw,
    FixedScalar ConnectedRawMass,
    int AdjacentSupportedCount,
    FixedScalar StrongCenterSupport);

internal static class CultureInfluenceTargetScorer
{
    private static readonly FixedScalar ClaimedRawInfluence = WorldState.CultureClaimedRawMass;
    private static readonly FixedScalar ControlledRawInfluence = WorldState.CultureControlledRawMass;
    private static readonly FixedScalar FullRawInfluence = WorldState.CultureFullRawMass;
    private static readonly FixedScalar ConnectedMassScale = 6;
    private static readonly FixedScalar AdjacentSupportScale = 2;
    private const int InteriorHoleAdjacentSupportThreshold = 4;
    private static readonly FixedScalar InteriorHoleConnectedMassScale = 4;
    private static readonly FixedScalar InteriorHoleUtilityMultiplier = 2;
    private static readonly FixedScalar ForeignTraceToleranceShare = 10;
    private static readonly FixedScalar ForeignContestScaleShare = 45;
    internal static readonly FixedScalar StrongCenterGravity = FixedScalar.FromUnits(750000L);

    internal static FixedScalar Score(in CultureInfluenceTargetScoreInput input)
    {
        var utility = Utility(input);
        if (utility <= 0)
        {
            return 0;
        }

        var priority = LocalityPriority(input);
        return priority <= 0 ? 0 : priority * utility;
    }

    private static FixedScalar Utility(in CultureInfluenceTargetScoreInput input) =>
        input.TargetingCode switch
        {
            LowAltitudeInfluenceTargetingCodes.Reinforce => ReinforceUtility(input),
            LowAltitudeInfluenceTargetingCodes.Contest => ContestUtility(input),
            LowAltitudeInfluenceTargetingCodes.Spread => SpreadUtility(input),
            _ => SpreadUtility(input),
        };

    private static FixedScalar LocalityPriority(in CultureInfluenceTargetScoreInput input)
    {
        var connectedRawMass = input.ConnectedRawMass;
        var localSupportCount = input.AdjacentSupportedCount + (input.OwnRaw > 0 ? 1 : 0);
        if (connectedRawMass <= 0 &&
            input.TargetingCode != LowAltitudeInfluenceTargetingCodes.Contest)
        {
            connectedRawMass = input.OwnRaw;
        }

        if (connectedRawMass <= 0 || localSupportCount <= 0)
        {
            return 0;
        }

        var connectedSupport = Saturating(connectedRawMass, ConnectedMassScale);
        var localAnchorSupport = Saturating(localSupportCount, AdjacentSupportScale);
        return connectedSupport * connectedSupport * localAnchorSupport;
    }

    private static FixedScalar SpreadUtility(in CultureInfluenceTargetScoreInput input)
    {
        var headroom = Clamp01((ControlledRawInfluence - input.OwnRaw) / ControlledRawInfluence);
        if (headroom <= 0)
        {
            return 0;
        }

        var rawShortfall = Clamp01((ControlledRawInfluence - input.OwnRaw) / ControlledRawInfluence);
        return InteriorHoleBoost(input) *
            PowerFiveFourths(headroom) *
            (FixedScalar.FromRatio(7, 20) + (FixedScalar.FromRatio(13, 20) * rawShortfall)) *
            ExpansionForeignDrag(input);
    }

    private static FixedScalar ReinforceUtility(in CultureInfluenceTargetScoreInput input)
    {
        var interiorHoleSupport = InteriorHoleSupport(input);
        var ownPresence = FixedScalar.Max(
            Saturating(input.OwnRaw, ClaimedRawInfluence),
            interiorHoleSupport * FixedScalar.FromUnits(750000L));
        if (ownPresence <= 0)
        {
            return 0;
        }

        // Shortfall keeps a floor so core cells stay valid reinforcement targets
        // near the cap; a strong current center concentrates work without
        // requiring global component topology.
        var rawShortfall = Clamp01((FullRawInfluence - input.OwnRaw) / FullRawInfluence);
        return InteriorHoleBoost(interiorHoleSupport) *
            ownPresence *
            (FixedScalar.FromRatio(3, 10) + (FixedScalar.FromRatio(7, 10) * rawShortfall)) *
            StrongCenterBoost(input.StrongCenterSupport) *
            ExpansionForeignDrag(input);
    }

    internal static FixedScalar StrongCenterBoost(FixedScalar strongCenterSupport) =>
        1 + (StrongCenterGravity * Clamp01(strongCenterSupport));

    private static FixedScalar ContestUtility(in CultureInfluenceTargetScoreInput input)
    {
        var foreignPresence = ForeignCleanupSignal(input.ForeignShare, input.ForeignRaw);
        if (foreignPresence <= 0)
        {
            return 0;
        }

        var ownAnchor = FixedScalar.Max(
            FixedScalar.Max(
                Clamp01(input.OwnShare / 100),
                Saturating(input.OwnRaw, ClaimedRawInfluence)),
            InteriorHoleSupport(input));
        return foreignPresence * (FixedScalar.FromUnits(500000L) + ownAnchor) * (FixedScalar.FromUnits(750000L) + (FixedScalar.FromUnits(500000L) * ownAnchor));
    }

    private static FixedScalar ExpansionForeignDrag(in CultureInfluenceTargetScoreInput input)
    {
        var foreignCompetition = ForeignContestSignal(input.ForeignShare);
        if (foreignCompetition <= 0)
        {
            return 1;
        }

        var ownBoundary = FixedScalar.Max(
            FixedScalar.Max(
                Clamp01(input.OwnShare / 100),
                Saturating(input.OwnRaw, ControlledRawInfluence)),
            InteriorHoleSupport(input));
        return 1 - (FixedScalar.FromUnits(700000L) * foreignCompetition * (1 - ownBoundary));
    }

    internal static FixedScalar InteriorHoleSupport(FixedScalar ownRaw, FixedScalar connectedRawMass, int adjacentSupportedCount)
    {
        if (ownRaw >= ControlledRawInfluence ||
            adjacentSupportedCount < InteriorHoleAdjacentSupportThreshold ||
            connectedRawMass <= 0)
        {
            return 0;
        }

        var adjacentSupport = Clamp01(
            (adjacentSupportedCount - (InteriorHoleAdjacentSupportThreshold - 1)) /
            (FixedScalar)(7 - InteriorHoleAdjacentSupportThreshold));
        var connectedSupport = Saturating(connectedRawMass, InteriorHoleConnectedMassScale);
        var rawHeadroom = Clamp01((ControlledRawInfluence - ownRaw) / ControlledRawInfluence);
        return adjacentSupport * connectedSupport * rawHeadroom;
    }

    private static FixedScalar InteriorHoleSupport(in CultureInfluenceTargetScoreInput input) =>
        InteriorHoleSupport(input.OwnRaw, input.ConnectedRawMass, input.AdjacentSupportedCount);

    private static FixedScalar InteriorHoleBoost(in CultureInfluenceTargetScoreInput input) =>
        InteriorHoleBoost(InteriorHoleSupport(input));

    private static FixedScalar InteriorHoleBoost(FixedScalar support) =>
        1 + (InteriorHoleUtilityMultiplier * support);

    private static FixedScalar ForeignCleanupSignal(FixedScalar foreignShare, FixedScalar foreignRaw)
    {
        var shareSignal = ForeignContestSignal(foreignShare);
        var rawSignal = Saturating(foreignRaw, ClaimedRawInfluence) * FixedScalar.FromUnits(650000L);
        return FixedScalar.Max(shareSignal, rawSignal);
    }

    private static FixedScalar ForeignContestSignal(FixedScalar foreignShare)
    {
        if (foreignShare <= ForeignTraceToleranceShare)
        {
            return 0;
        }

        return Clamp01((foreignShare - ForeignTraceToleranceShare) / ForeignContestScaleShare);
    }

    private static FixedScalar Saturating(FixedScalar value, FixedScalar scale) =>
        value <= 0 ? 0 : value / (value + scale);

    private static FixedScalar PowerFiveFourths(FixedScalar value) =>
        value * (3 + value) / 4;

    private static FixedScalar Clamp01(FixedScalar value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1 ? 1 : value;
    }
}
