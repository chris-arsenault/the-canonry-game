namespace Engine.Kernel;

using Engine.Constants;

internal readonly record struct CultureFieldCell(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    FixedScalar Share,
    FixedScalar RawMass,
    FixedScalar TotalRawMass,
    FixedScalar ForeignShare,
    FixedScalar ForeignRawMass,
    FixedScalar LocalRawMass,
    FixedScalar ConnectedRawMass,
    FixedScalar Headroom,
    int SupportedAdjacentCount,
    int SupportedRingTwoCount,
    int CellKindCode);

internal sealed partial class WorldState
{
    internal CultureFieldCell CultureFieldAt(int cultureId, int planeCode, int q, int r)
    {
        ValidatePositive(cultureId);
        ValidateInfluencePlaneCode(planeCode);
        var locality = _cultureLocality ??
            throw new global::System.InvalidOperationException();
        var boundary = locality.CellAt(this, cultureId, planeCode, q, r);
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

    internal FixedScalar EntityInfluenceProduction(int entityId, int planeCode)
    {
        EnsureEntity(entityId);
        ValidateInfluencePlaneCode(planeCode);
        return planeCode == PlaneCodes.Semantic
            ? KernelFixedPoint.CultureMassScalar(_entitySemanticInfluenceProduction[entityId])
            : KernelFixedPoint.CultureMassScalar(_entityPhysicalInfluenceProduction[entityId]);
    }

    internal static int CultureFieldStrengthBand(FixedScalar rawMass)
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

}
