namespace Engine.Kernel;

internal readonly record struct CultureBoundaryCell(
    int CultureId,
    int PlaneCode,
    int Q,
    int R,
    float Share,
    float RawMass,
    float TotalRawMass,
    int ForeignCultureId,
    float ForeignShare,
    float ForeignRawMass,
    float LocalRawMass,
    float ConnectedRawMass,
    float Headroom,
    int SupportedAdjacentCount,
    int SupportedRingTwoCount,
    float StrongestAdjacentRawMass,
    float RingTwoRawMass,
    float StrongestRingTwoRawMass,
    int CellKindCode,
    int StrengthBandCode,
    int ComponentId,
    int ComponentCellCount,
    float ComponentRawMass,
    float ComponentScoreMass,
    bool IsLargestComponent,
    int AttachedComponentCount,
    int AttachedComponentCellCount,
    float AttachedComponentRawMass,
    float AttachedComponentScoreMass)
{
    public bool IsInteriorHole =>
        RawMass < WorldState.CultureControlledRawMass &&
        AttachedComponentCount == 1 &&
        SupportedAdjacentCount >= CultureBoundaryProgramRuntime.InteriorHoleAdjacentSupportThreshold &&
        AttachedComponentScoreMass > 0f;
}

internal readonly record struct CultureBoundaryComponent(
    int CultureId,
    int PlaneCode,
    int ComponentId,
    int CellCount,
    float RawMass,
    float ScoreMass,
    bool IsLargestComponent);

internal readonly record struct CultureBoundaryAttachment(
    int ComponentCount,
    int CellCount,
    float RawMass,
    float ScoreMass);
