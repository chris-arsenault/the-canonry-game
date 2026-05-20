namespace Engine.Kernel;

internal readonly record struct CultureInfluenceTargetScoreInput(
    int TargetingCode,
    float OwnShare,
    float OwnRaw,
    float EntityInfluence,
    float ForeignShare,
    float AttachedComponentMass,
    int AdjacentSupportedCount,
    int AttachedComponentCount);

internal static class CultureInfluenceTargetScorer
{
    private const float SupportedRawInfluence = 12f;
    private const float StrongRawInfluence = 30f;
    private const float NexusRawInfluence = 90f;
    private const float ComponentMassScale = 6f;
    private const float AttachmentScale = 2f;
    private const float BridgeBonusPerComponent = 0.2f;
    private const float BridgeBonusCap = 7f / 20f;

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
            attachedComponentMass = Clamp01(global::System.MathF.Max(input.OwnRaw, input.EntityInfluence) / StrongRawInfluence);
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
        var cultureHeadroom = Clamp01((100f - input.OwnShare) / 100f);
        var entityHeadroom = Clamp01((100f - input.EntityInfluence) / 100f);
        var headroom = global::System.MathF.Max(cultureHeadroom, entityHeadroom * 0.5f);
        if (headroom <= 0f)
        {
            return 0f;
        }

        var rawShortfall = Clamp01((StrongRawInfluence - input.OwnRaw) / StrongRawInfluence);
        return global::System.MathF.Pow(headroom, 1.25f) * ((7f / 20f) + ((13f / 20f) * rawShortfall));
    }

    private static float ReinforceUtility(in CultureInfluenceTargetScoreInput input)
    {
        var ownPresence = Saturating(global::System.MathF.Max(input.OwnRaw, input.EntityInfluence), SupportedRawInfluence);
        if (ownPresence <= 0f)
        {
            return 0f;
        }

        var rawShortfall = Clamp01((NexusRawInfluence - input.OwnRaw) / NexusRawInfluence);
        return ownPresence * rawShortfall;
    }

    private static float ContestUtility(in CultureInfluenceTargetScoreInput input)
    {
        var foreignPresence = Clamp01(input.ForeignShare / 100f);
        return foreignPresence <= 0f ? 0f : foreignPresence;
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
