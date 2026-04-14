namespace Engine.Attributes;

/// <summary>
/// Defines a 2D semantic plane with axis labels and a reducer that maps
/// continuous positions to categorical labels (default 3x3 grid = 9 labels).
/// </summary>
public sealed class SemanticPlaneDefinition
{
    public string Name { get; }
    public string XAxisLabel { get; }
    public string YAxisLabel { get; }

    /// <summary>Labels for the 3x3 grid regions. Indexed as [row * 3 + col] where
    /// row 0 = low Y, row 2 = high Y, col 0 = low X, col 2 = high X.</summary>
    public IReadOnlyList<string> Labels { get; }

    public SemanticPlaneDefinition(
        string name,
        string xAxisLabel,
        string yAxisLabel,
        IReadOnlyList<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        if (labels.Count != 9)
        {
            throw new ArgumentException("Semantic plane requires exactly 9 labels for 3x3 grid", nameof(labels));
        }

        Name = name;
        XAxisLabel = xAxisLabel;
        YAxisLabel = yAxisLabel;
        Labels = labels;
    }

    /// <summary>
    /// Reduce a continuous position (0-1 on each axis) to a categorical label.
    /// </summary>
    public string Reduce(float x, float y)
    {
        var col = x switch
        {
            < 0.333f => 0,
            < 0.667f => 1,
            _ => 2,
        };

        var row = y switch
        {
            < 0.333f => 0,
            < 0.667f => 1,
            _ => 2,
        };

        return Labels[(row * 3) + col];
    }
}
