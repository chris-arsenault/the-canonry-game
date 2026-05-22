namespace Engine.Kernel;

using Engine.Constants;

internal sealed partial class MinorActorProgramRuntime
{
    private static CultureInfluenceNeighborhood CultureNeighborhood(
        WorldState world,
        int cultureId,
        int planeCode,
        int q,
        int r)
    {
        var centerRaw = world.GetCellCultureInfluenceRaw(cultureId, planeCode, q, r);
        var adjacentRawTotal = 0f;
        var supportedAdjacentCount = 0;
        var ringTwoRawTotal = 0f;
        for (var i = 0; i < CultureNeighborDQ.Length; i++)
        {
            var raw = world.GetCellCultureInfluenceRaw(
                cultureId,
                planeCode,
                q + CultureNeighborDQ[i],
                r + CultureNeighborDR[i]);
            if (i < CultureAdjacentNeighborCount)
            {
                adjacentRawTotal += raw;
                supportedAdjacentCount += raw >= CultureSupportedRawInfluence ? 1 : 0;
                continue;
            }

            ringTwoRawTotal += raw;
        }

        return new CultureInfluenceNeighborhood(centerRaw, adjacentRawTotal, supportedAdjacentCount, ringTwoRawTotal);
    }

    private static int CultureFieldCellKind(
        float raw,
        float share,
        float foreignShare,
        int supportedAdjacentCount,
        float connectedRaw)
    {
        if (CultureFieldIsEmpty(raw, share, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (CultureFieldIsContested(raw, share, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (CultureFieldIsInterior(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (CultureFieldIsIsolated(raw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return CultureFieldIsBorder(raw, supportedAdjacentCount)
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool CultureFieldIsEmpty(float raw, float share, float connectedRaw) =>
        raw < CultureTraceRawInfluence &&
        share <= 0f &&
        connectedRaw < CultureSupportedRawInfluence;

    private static bool CultureFieldIsContested(float raw, float share, float foreignShare) =>
        raw >= CultureTraceRawInfluence &&
        foreignShare >= CultureFieldForeignContestShare &&
        (share < CultureFieldDominantOwnShare || foreignShare >= CultureFieldMajorForeignContestShare);

    private static bool CultureFieldIsInterior(float raw, int supportedAdjacentCount) =>
        raw >= CultureStrongRawInfluence && supportedAdjacentCount >= 5;

    private static bool CultureFieldIsIsolated(float raw, int supportedAdjacentCount) =>
        raw >= CultureTraceRawInfluence && supportedAdjacentCount == 0;

    private static bool CultureFieldIsBorder(float raw, int supportedAdjacentCount) =>
        raw >= CultureTraceRawInfluence || supportedAdjacentCount > 0;

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

    private static float StrongestOtherCultureRaw(
        WorldState world,
        int ownCultureId,
        int planeCode,
        int q,
        int r,
        out int cultureId)
    {
        cultureId = 0;
        var raw = 0f;
        for (var otherCultureId = 1; otherCultureId <= world.CultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == ownCultureId)
            {
                continue;
            }

            var candidateRaw = world.GetCellCultureInfluenceRaw(otherCultureId, planeCode, q, r);
            if (candidateRaw <= raw)
            {
                continue;
            }

            raw = candidateRaw;
            cultureId = otherCultureId;
        }

        return raw;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        return (global::System.Math.Abs(aq - bq) +
            global::System.Math.Abs(ar - br) +
            global::System.Math.Abs(asCoord - bsCoord)) / 2;
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

    private readonly record struct CultureInfluenceNeighborhood(
        float CenterRaw,
        float AdjacentRawTotal,
        int SupportedAdjacentCount,
        float RingTwoRawTotal)
    {
        public float LocalRaw => CenterRaw + AdjacentRawTotal;

        public float ConnectedRaw => LocalRaw + (RingTwoRawTotal * CultureRingTwoConnectionWeight);
    }

    private readonly record struct MinorActorCellTarget(
        int Q,
        int R,
        int DistanceFromActor,
        int DistanceFromSource,
        float OwnShare,
        float OwnRaw,
        float ForeignShare,
        float ForeignRaw,
        CultureInfluenceNeighborhood Neighborhood,
        CultureInfluenceAttachment Attachment,
        int CellKindCode);

    private readonly record struct MinorActorVerbCandidate(
        int VerbId,
        int ActionCode,
        int TargetingCode,
        int PlaneCode,
        int Q,
        int R,
        int CellKindCode,
        int EffectiveRadius,
        float LocalRawMass,
        float ConnectedRawMass,
        float Score,
        float WorkDeltaMultiplier);
}
