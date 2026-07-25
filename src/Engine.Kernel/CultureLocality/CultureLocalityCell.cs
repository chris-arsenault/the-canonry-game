namespace Engine.Kernel;

internal readonly record struct CultureLocalityCell(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    FixedScalar Share,
    FixedScalar RawMass,
    FixedScalar TotalRawMass,
    int ForeignCultureId,
    FixedScalar ForeignShare,
    FixedScalar ForeignRawMass,
    FixedScalar LocalRawMass,
    FixedScalar ConnectedRawMass,
    FixedScalar Headroom,
    int SupportedAdjacentCount,
    int SupportedRingTwoCount,
    FixedScalar StrongestAdjacentRawMass,
    FixedScalar RingTwoRawMass,
    FixedScalar StrongestRingTwoRawMass,
    int CellKindCode,
    int StrengthBandCode)
{
    public bool IsInteriorHole =>
        RawMass < WorldState.CultureControlledRawMass &&
        SupportedAdjacentCount >= CultureLocality.InteriorHoleAdjacentSupportThreshold &&
        LocalRawMass - RawMass >= WorldState.CultureControlledRawMass;
}

internal readonly record struct CultureLocalityDirtyCell(
    int PlaneCode,
    int Q,
    int R);
