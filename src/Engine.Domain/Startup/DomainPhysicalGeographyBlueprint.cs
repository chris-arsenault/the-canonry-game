namespace Engine.Domain.Startup;

using Engine.Constants;

/// <summary>
/// Stable-name physical geography generated before kernel installation. The
/// radius-wide field exists from startup, while individual hexes are only
/// materialized when the growing map reveals them.
/// </summary>
public sealed record DomainPhysicalGeographyBlueprint(
    int Radius,
    IReadOnlyList<string> MaterialPalette,
    IReadOnlyList<DomainPhysicalGeographyCell> Cells,
    IReadOnlyList<DomainPhysicalMicrotriangle> Microtriangles)
{
    public static DomainPhysicalGeographyBlueprint Empty { get; } =
        new(0, [], [], []);

    public void Validate()
    {
        if (Radius < 0)
        {
            throw new InvalidOperationException("Physical geography radius cannot be negative.");
        }

        if (Radius == 0)
        {
            if (MaterialPalette.Count != 0 || Cells.Count != 0 || Microtriangles.Count != 0)
            {
                throw new InvalidOperationException("Empty physical geography cannot contain data.");
            }

            return;
        }

        var expectedCells = checked((3 * Radius * Radius) + (3 * Radius) + 1);
        if (MaterialPalette.Count == 0 ||
            Cells.Count != expectedCells ||
            Microtriangles.Count != checked(expectedCells * PhysicalGeographyCodes.MicrotrianglesPerHex))
        {
            throw new InvalidOperationException("Physical geography does not cover its declared hex disk.");
        }

        ValidateCells();
        ValidateMicrotriangles();
    }

    private void ValidateCells()
    {
        for (var cellIndex = 0; cellIndex < Cells.Count; cellIndex++)
        {
            var cell = Cells[cellIndex];
            if (cell.MicrotriangleOffset != cellIndex * PhysicalGeographyCodes.MicrotrianglesPerHex ||
                string.IsNullOrWhiteSpace(cell.BaseTerrain) ||
                (uint)cell.DominantMaterialIndex >= (uint)MaterialPalette.Count ||
                cell.SuperregionId <= 0 ||
                cell.ContinentId <= 0)
            {
                throw new InvalidOperationException("Physical geography contains an invalid hex summary.");
            }
        }
    }

    private void ValidateMicrotriangles()
    {
        foreach (var microtriangle in Microtriangles)
        {
            if (!MicrotriangleIsValid(microtriangle))
            {
                throw new InvalidOperationException("Physical geography contains an invalid microtriangle.");
            }
        }
    }

    private bool MicrotriangleIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        (uint)microtriangle.MaterialIndex < (uint)MaterialPalette.Count &&
        SurfaceCodesAreValid(microtriangle) &&
        HydrologyIsValid(microtriangle) &&
        FlowIsValid(microtriangle);

    private static bool SurfaceCodesAreValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.LandformCode >= PhysicalLandformCodes.Min &&
        microtriangle.LandformCode <= PhysicalLandformCodes.Max &&
        (microtriangle.FeatureFlags & ~PhysicalGeographyFeatureCodes.All) == 0;

    private static bool HydrologyIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.WaterKindCode >= PhysicalWaterKindCodes.Min &&
        microtriangle.WaterKindCode <= PhysicalWaterKindCodes.Max &&
        microtriangle.WaterDepth >= 0 &&
        microtriangle.WaterBodyId >= 0 &&
        microtriangle.DrainageBasinId >= 0;

    private static bool FlowIsValid(DomainPhysicalMicrotriangle microtriangle) =>
        microtriangle.FlowEdge >= PhysicalGeographyCodes.NoFlowEdge &&
        microtriangle.FlowEdge <= 2 &&
        microtriangle.FlowAccumulation >= 0 &&
        microtriangle.RiverOrder >= 0;
}

public readonly record struct DomainPhysicalGeographyCell(
    int Q,
    int R,
    string BaseTerrain,
    int SuperregionId,
    int ContinentId,
    int MicrotriangleOffset,
    int DominantMaterialIndex,
    int MeanElevation,
    int MinElevation,
    int MaxElevation,
    int Relief,
    int Roughness,
    int MeanTemperature,
    int MeanMoisture,
    int MeanGeothermal,
    int LandCoverage,
    int WaterCoverage,
    int OceanCoverage,
    int LakeCoverage,
    int PermanentIceCoverage,
    int RiverCoverage,
    int PeakCount,
    int RiverEdgeMask,
    int CoastEdgeMask,
    int RidgeEdgeMask,
    int CliffEdgeMask,
    int PassEdgeMask);

public readonly record struct DomainPhysicalMicrotriangle(
    int MaterialIndex,
    int LandformCode,
    int Elevation,
    int Temperature,
    int Moisture,
    int Geothermal,
    int WaterKindCode,
    int WaterDepth,
    int WaterBodyId,
    int DrainageBasinId,
    int FlowEdge,
    int FlowAccumulation,
    int RiverOrder,
    int FeatureFlags);
