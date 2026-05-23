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
    internal CultureFieldCell CultureFieldAt(int cultureId, int planeCode, int q, int r)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        var boundaries = _cultureBoundaries ??
            throw new global::System.InvalidOperationException();
        var boundary = boundaries.CellAt(this, cultureId, planeCode, q, r);
        return new CultureFieldCell(
            cultureId,
            planeCode,
            q,
            r,
            boundary.Share,
            boundary.RawMass,
            boundary.TotalRawMass,
            boundary.ForeignShare,
            boundary.ForeignRawMass,
            boundary.LocalRawMass,
            boundary.ConnectedRawMass,
            boundary.Headroom,
            boundary.SupportedAdjacentCount,
            boundary.SupportedRingTwoCount,
            boundary.CellKindCode);
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

    private float CultureRawTotalAt(int planeCode, int key)
    {
        var total = 0f;
        for (var cultureId = 1; cultureId <= _cultureHighWatermark; cultureId++)
        {
            total += PositiveCultureInfluenceRaw(cultureId, planeCode, key);
        }

        return total;
    }
}
