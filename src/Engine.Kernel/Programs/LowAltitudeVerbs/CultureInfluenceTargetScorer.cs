namespace Engine.Kernel;

internal readonly record struct CultureInfluenceTargetScoreInput(
    int TargetingCode,
    float OwnShare,
    float OwnRaw,
    float ForeignShare,
    float ForeignRaw,
    float AttachedComponentMass,
    int AdjacentSupportedCount,
    int AttachedComponentCount);

internal static class CultureInfluenceTargetScorer
{
    private const float ClaimedRawInfluence = WorldState.CultureClaimedRawMass;
    private const float ControlledRawInfluence = WorldState.CultureControlledRawMass;
    private const float FullRawInfluence = WorldState.CultureFullRawMass;
    private const float ComponentMassScale = 6f;
    private const float AttachmentScale = 2f;
    private const float BridgeBonusPerComponent = 0.2f;
    private const float BridgeBonusCap = 7f / 20f;
    private const int InteriorHoleAdjacentSupportThreshold = 4;
    private const float InteriorHoleComponentMassScale = 4f;
    private const float InteriorHoleUtilityMultiplier = 2f;
    private const float ForeignTraceToleranceShare = 10f;
    private const float ForeignContestScaleShare = 45f;

    internal static float Score(in CultureInfluenceTargetScoreInput input)
    {
        var utility = Utility(input);
        if (utility <= 0f)
        {
            return 0f;
        }

        var priority = ComponentPriority(input);
        return priority <= 0f ? 0f : priority * utility;
    }

    private static float Utility(in CultureInfluenceTargetScoreInput input) =>
        input.TargetingCode switch
        {
            LowAltitudeInfluenceTargetingCodes.Reinforce => ReinforceUtility(input),
            LowAltitudeInfluenceTargetingCodes.Contest => ContestUtility(input),
            LowAltitudeInfluenceTargetingCodes.Spread => SpreadUtility(input),
            _ => SpreadUtility(input),
        };

    private static float ComponentPriority(in CultureInfluenceTargetScoreInput input)
    {
        var attachedComponentMass = input.AttachedComponentMass;
        var adjacentSupportedCount = input.AdjacentSupportedCount;
        var attachedComponentCount = input.AttachedComponentCount;
        if (attachedComponentMass <= 0f &&
            input.TargetingCode != LowAltitudeInfluenceTargetingCodes.Contest)
        {
            attachedComponentMass = Clamp01(input.OwnRaw / ControlledRawInfluence);
            adjacentSupportedCount = attachedComponentMass > 0f ? 1 : 0;
            attachedComponentCount = adjacentSupportedCount;
        }

        if (attachedComponentMass <= 0f || attachedComponentCount <= 0)
        {
            return 0f;
        }

        var componentSupport = Saturating(attachedComponentMass, ComponentMassScale);
        var attachmentSupport = Saturating(global::System.Math.Max(adjacentSupportedCount, attachedComponentCount), AttachmentScale);
        var bridgeBonus = 1f + global::System.MathF.Min(
            BridgeBonusCap,
            global::System.Math.Max(0, attachedComponentCount - 1) * BridgeBonusPerComponent);
        return componentSupport * componentSupport * attachmentSupport * bridgeBonus;
    }

    private static float SpreadUtility(in CultureInfluenceTargetScoreInput input)
    {
        var headroom = Clamp01((ControlledRawInfluence - input.OwnRaw) / ControlledRawInfluence);
        if (headroom <= 0f)
        {
            return 0f;
        }

        var rawShortfall = Clamp01((ControlledRawInfluence - input.OwnRaw) / ControlledRawInfluence);
        return InteriorHoleBoost(input) *
            global::System.MathF.Pow(headroom, 1.25f) *
            ((7f / 20f) + ((13f / 20f) * rawShortfall)) *
            ExpansionForeignDrag(input);
    }

    private static float ReinforceUtility(in CultureInfluenceTargetScoreInput input)
    {
        var interiorHoleSupport = InteriorHoleSupport(input);
        var ownPresence = global::System.MathF.Max(
            Saturating(input.OwnRaw, ClaimedRawInfluence),
            interiorHoleSupport * 0.75f);
        if (ownPresence <= 0f)
        {
            return 0f;
        }

        var rawShortfall = Clamp01((FullRawInfluence - input.OwnRaw) / FullRawInfluence);
        return InteriorHoleBoost(interiorHoleSupport) * ownPresence * rawShortfall * ExpansionForeignDrag(input);
    }

    private static float ContestUtility(in CultureInfluenceTargetScoreInput input)
    {
        var foreignPresence = ForeignCleanupSignal(input.ForeignShare, input.ForeignRaw);
        if (foreignPresence <= 0f)
        {
            return 0f;
        }

        var ownAnchor = global::System.MathF.Max(
            global::System.MathF.Max(
                Clamp01(input.OwnShare / 100f),
                Saturating(input.OwnRaw, ClaimedRawInfluence)),
            InteriorHoleSupport(input));
        return foreignPresence * (0.5f + ownAnchor) * (0.75f + (0.5f * ownAnchor));
    }

    private static float ExpansionForeignDrag(in CultureInfluenceTargetScoreInput input)
    {
        var foreignCompetition = ForeignContestSignal(input.ForeignShare);
        if (foreignCompetition <= 0f)
        {
            return 1f;
        }

        var ownBoundary = global::System.MathF.Max(
            global::System.MathF.Max(
                Clamp01(input.OwnShare / 100f),
                Saturating(input.OwnRaw, ControlledRawInfluence)),
            InteriorHoleSupport(input));
        return 1f - (0.7f * foreignCompetition * (1f - ownBoundary));
    }

    internal static float InteriorHoleSupport(float ownRaw, float attachedComponentMass, int adjacentSupportedCount)
    {
        if (ownRaw >= ControlledRawInfluence ||
            adjacentSupportedCount < InteriorHoleAdjacentSupportThreshold ||
            attachedComponentMass <= 0f)
        {
            return 0f;
        }

        var adjacentSupport = Clamp01(
            (adjacentSupportedCount - (InteriorHoleAdjacentSupportThreshold - 1)) /
            (float)(7 - InteriorHoleAdjacentSupportThreshold));
        var componentSupport = Saturating(attachedComponentMass, InteriorHoleComponentMassScale);
        var rawHeadroom = Clamp01((ControlledRawInfluence - ownRaw) / ControlledRawInfluence);
        return adjacentSupport * componentSupport * rawHeadroom;
    }

    private static float InteriorHoleSupport(in CultureInfluenceTargetScoreInput input) =>
        InteriorHoleSupport(input.OwnRaw, input.AttachedComponentMass, input.AdjacentSupportedCount);

    private static float InteriorHoleBoost(in CultureInfluenceTargetScoreInput input) =>
        InteriorHoleBoost(InteriorHoleSupport(input));

    private static float InteriorHoleBoost(float support) =>
        1f + (InteriorHoleUtilityMultiplier * support);

    private static float ForeignCleanupSignal(float foreignShare, float foreignRaw)
    {
        var shareSignal = ForeignContestSignal(foreignShare);
        var rawSignal = Saturating(foreignRaw, ClaimedRawInfluence) * 0.65f;
        return global::System.MathF.Max(shareSignal, rawSignal);
    }

    private static float ForeignContestSignal(float foreignShare)
    {
        if (foreignShare <= ForeignTraceToleranceShare)
        {
            return 0f;
        }

        return Clamp01((foreignShare - ForeignTraceToleranceShare) / ForeignContestScaleShare);
    }

    private static float Saturating(float value, float scale) =>
        value <= 0f ? 0f : 1f - global::System.MathF.Exp(-value / scale);

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
