namespace Engine.Kernel;

internal readonly record struct CultureFieldCell(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    float Share,
    float RawMass,
    float TotalRawMass,
    float ForeignShare,
    float ForeignRawMass,
    float LocalRawMass,
    float ConnectedRawMass,
    float Headroom,
    int SupportedAdjacentCount,
    int SupportedRingTwoCount,
    int CellKindCode);

internal sealed partial class WorldState
{
    private const float CultureFieldRingTwoConnectionWeight = 7f / 20f;
    private const float CultureFieldContestMarginShare = 35f;

    private readonly record struct CultureFieldNeighborhood(
        float AdjacentRawTotal,
        float RingTwoRawTotal,
        int SupportedAdjacentCount,
        int SupportedRingTwoCount);

    internal CultureFieldCell CultureFieldAt(int cultureId, int planeCode, int q, int r)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        var key = CoordinateKey(q, r);
        var ownRaw = PositiveCultureInfluenceRaw(cultureId, planeCode, key);
        var ownShare = _mapLayers.CultureInfluence.ValueAt(cultureId, planeCode, key);
        var totalRaw = CultureRawTotalAt(planeCode, key);
        var foreignShare = StrongestOtherCultureShare(cultureId, planeCode, key, out var foreignRaw);
        var neighborhood = CultureFieldNeighborhoodAt(cultureId, planeCode, q, r);
        var localRaw = ownRaw + neighborhood.AdjacentRawTotal;
        var connectedRaw = localRaw + (neighborhood.RingTwoRawTotal * CultureFieldRingTwoConnectionWeight);
        var cellKindCode = CultureFieldCellKind(
            ownRaw,
            ownShare,
            foreignRaw,
            foreignShare,
            neighborhood.SupportedAdjacentCount,
            connectedRaw);
        return new CultureFieldCell(
            cultureId,
            planeCode,
            q,
            r,
            ownShare,
            ownRaw,
            totalRaw,
            foreignShare,
            foreignRaw,
            localRaw,
            connectedRaw,
            global::System.MathF.Max(0f, CultureFullRawMass - ownRaw),
            neighborhood.SupportedAdjacentCount,
            neighborhood.SupportedRingTwoCount,
            cellKindCode);
    }

    private CultureFieldNeighborhood CultureFieldNeighborhoodAt(int cultureId, int planeCode, int q, int r)
    {
        var adjacentRawTotal = 0f;
        var ringTwoRawTotal = 0f;
        var supportedAdjacentCount = 0;
        var supportedRingTwoCount = 0;
        for (var dq = -2; dq <= 2; dq++)
        {
            var minDr = global::System.Math.Max(-2, -dq - 2);
            var maxDr = global::System.Math.Min(2, -dq + 2);
            for (var dr = minDr; dr <= maxDr; dr++)
            {
                AddCultureFieldNeighborRaw(
                    cultureId,
                    planeCode,
                    q,
                    r,
                    dq,
                    dr,
                    ref adjacentRawTotal,
                    ref ringTwoRawTotal,
                    ref supportedAdjacentCount,
                    ref supportedRingTwoCount);
            }
        }

        return new CultureFieldNeighborhood(
            adjacentRawTotal,
            ringTwoRawTotal,
            supportedAdjacentCount,
            supportedRingTwoCount);
    }

    private void AddCultureFieldNeighborRaw(
        int cultureId,
        int planeCode,
        int q,
        int r,
        int dq,
        int dr,
        ref float adjacentRawTotal,
        ref float ringTwoRawTotal,
        ref int supportedAdjacentCount,
        ref int supportedRingTwoCount)
    {
        if (dq == 0 && dr == 0)
        {
            return;
        }

        var raw = PositiveCultureInfluenceRaw(cultureId, planeCode, CoordinateKey(q + dq, r + dr));
        if (HexDistance(0, 0, dq, dr) == 1)
        {
            adjacentRawTotal += raw;
            supportedAdjacentCount += CultureFieldSupportedCellCount(raw);
            return;
        }

        ringTwoRawTotal += raw;
        supportedRingTwoCount += CultureFieldSupportedCellCount(raw);
    }

    internal float EntityInfluenceProduction(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Semantic
            ? _entitySemanticInfluenceProduction[entityId]
            : _entityPhysicalInfluenceProduction[entityId];
    }

    internal static int CultureFieldStrengthBand(float rawMass)
    {
        if (rawMass >= CultureNexusRawMass)
        {
            return CultureFieldStrengthBandCodes.Nexus;
        }

        if (rawMass >= CultureEntrenchedRawMass)
        {
            return CultureFieldStrengthBandCodes.Entrenched;
        }

        if (rawMass >= CultureControlledRawMass)
        {
            return CultureFieldStrengthBandCodes.Controlled;
        }

        if (rawMass >= CultureClaimedRawMass)
        {
            return CultureFieldStrengthBandCodes.Claimed;
        }

        return rawMass >= CultureTraceRawMass
            ? CultureFieldStrengthBandCodes.Trace
            : CultureFieldStrengthBandCodes.None;
    }

    private static int CultureFieldCellKind(
        float ownRaw,
        float ownShare,
        float foreignRaw,
        float foreignShare,
        int supportedAdjacentCount,
        float connectedRaw)
    {
        if (CultureFieldIsEmpty(ownRaw, ownShare, connectedRaw))
        {
            return CultureFieldCellKindCodes.None;
        }

        if (CultureFieldIsContested(ownRaw, ownShare, foreignRaw, foreignShare))
        {
            return CultureFieldCellKindCodes.Contested;
        }

        if (CultureFieldIsInterior(ownRaw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Interior;
        }

        if (CultureFieldIsIsolated(ownRaw, supportedAdjacentCount))
        {
            return CultureFieldCellKindCodes.Isolated;
        }

        return CultureFieldIsBorder(ownRaw, supportedAdjacentCount)
            ? CultureFieldCellKindCodes.Border
            : CultureFieldCellKindCodes.None;
    }

    private static bool CultureFieldIsEmpty(float ownRaw, float ownShare, float connectedRaw) =>
        ownRaw < CultureTraceRawMass && ownShare <= 0f && connectedRaw < CultureClaimedRawMass;

    private static bool CultureFieldIsContested(float ownRaw, float ownShare, float foreignRaw, float foreignShare) =>
        ownRaw >= CultureTraceRawMass &&
        foreignRaw >= CultureClaimedRawMass &&
        ownShare - foreignShare < CultureFieldContestMarginShare;

    private static bool CultureFieldIsInterior(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= CultureControlledRawMass && supportedAdjacentCount >= 5;

    private static bool CultureFieldIsIsolated(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= CultureTraceRawMass && supportedAdjacentCount == 0;

    private static bool CultureFieldIsBorder(float ownRaw, int supportedAdjacentCount) =>
        ownRaw >= CultureTraceRawMass || supportedAdjacentCount > 0;

    private static int CultureFieldSupportedCellCount(float raw) =>
        raw >= CultureClaimedRawMass ? 1 : 0;

    private float CultureRawTotalAt(int planeCode, int key)
    {
        var total = 0f;
        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            total += PositiveCultureInfluenceRaw(cultureId, planeCode, key);
        }

        return total;
    }

    private float StrongestOtherCultureShare(int cultureId, int planeCode, int key, out float rawMass)
    {
        var strongestShare = 0f;
        rawMass = 0f;
        for (var otherCultureId = 1; otherCultureId <= _cultureHighWatermark; otherCultureId++)
        {
            if (otherCultureId == cultureId)
            {
                continue;
            }

            var share = _mapLayers.CultureInfluence.ValueAt(otherCultureId, planeCode, key);
            if (share > strongestShare)
            {
                strongestShare = share;
                rawMass = PositiveCultureInfluenceRaw(otherCultureId, planeCode, key);
            }
        }

        return strongestShare;
    }

    private static int HexDistance(int aq, int ar, int bq, int br)
    {
        var asCoord = -aq - ar;
        var bsCoord = -bq - br;
        var dq = aq - bq;
        var dr = ar - br;
        var ds = asCoord - bsCoord;
        return (Abs(dq) + Abs(dr) + Abs(ds)) / 2;
    }

    private static int Abs(int value) => value < 0 ? -value : value;
}
