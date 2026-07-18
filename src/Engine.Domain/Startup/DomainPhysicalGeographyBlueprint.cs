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
        var expectedCells = checked((3 * Radius * Radius) + (3 * Radius) + 1);
        if (Cells.Count != expectedCells ||
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
            if ((uint)microtriangle.MaterialIndex >= (uint)MaterialPalette.Count)
            {
                throw new InvalidOperationException("Physical geography references an unknown material.");
            }
        }
    }
}

public readonly record struct DomainPhysicalGeographyCell(
    int Q,
    int R,
    string BaseTerrain,
    int SuperregionId,
    int ContinentId,
    int MicrotriangleOffset,
    int MeanElevation,
    int MaxElevation,
    int Roughness,
    int WaterCoverage,
    int RiverEdgeMask,
    int CoastEdgeMask);

public readonly record struct DomainPhysicalMicrotriangle(
    int MaterialIndex,
    int Elevation,
    int HydrologyFlags);
