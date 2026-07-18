namespace Engine.Domain.Startup;

/// <summary>
/// Stable-name semantic terrain for the complete potential idea-space disk.
/// Cells remain latent until MapFrontier materializes their coordinates.
/// </summary>
public sealed record DomainSemanticTerrainBlueprint(
    int Radius,
    IReadOnlyList<DomainSemanticTerrainCell> Cells)
{
    public static DomainSemanticTerrainBlueprint Empty { get; } = new(0, []);

    public void Validate()
    {
        if (Radius < 0)
        {
            throw new InvalidOperationException("Semantic terrain radius cannot be negative.");
        }
        var expectedCells = checked((3 * Radius * Radius) + (3 * Radius) + 1);
        if (Cells.Count != expectedCells)
        {
            throw new InvalidOperationException("Semantic terrain does not cover its declared hex disk.");
        }

        foreach (var cell in Cells)
        {
            if (string.IsNullOrWhiteSpace(cell.Terrain) ||
                cell.SuperregionId <= 0 ||
                cell.ContinentId <= 0)
            {
                throw new InvalidOperationException("Semantic terrain contains an invalid cell.");
            }
        }
    }
}

public readonly record struct DomainSemanticTerrainCell(
    int Q,
    int R,
    string Terrain,
    int SuperregionId,
    int ContinentId);
