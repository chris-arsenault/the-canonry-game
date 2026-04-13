namespace Engine.Attributes;

using Engine.Graph;

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

/// <summary>
/// Stores entity positions on semantic planes.
/// </summary>
public sealed class SemanticPlaneStore
{
    private readonly Dictionary<string, SemanticPlaneDefinition> _definitions = [];
    private readonly Dictionary<(EntityId EntityId, string PlaneName), (float X, float Y)> _positions = [];

    public void RegisterPlane(SemanticPlaneDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Name] = definition;
    }

    public SemanticPlaneDefinition? GetDefinition(string planeName)
    {
        return _definitions.GetValueOrDefault(planeName);
    }

    public void SetPosition(EntityId entityId, string planeName, float x, float y)
    {
        _positions[(entityId, planeName)] = (Math.Clamp(x, 0f, 1f), Math.Clamp(y, 0f, 1f));
    }

    public (float X, float Y)? GetPosition(EntityId entityId, string planeName)
    {
        return _positions.TryGetValue((entityId, planeName), out var pos) ? pos : null;
    }

    /// <summary>
    /// Get the reduced label for an entity's position on a semantic plane.
    /// Returns null if the entity has no position on this plane or the plane is undefined.
    /// </summary>
    public string? GetLabel(EntityId entityId, string planeName)
    {
        if (!_definitions.TryGetValue(planeName, out var definition))
        {
            return null;
        }

        if (!_positions.TryGetValue((entityId, planeName), out var pos))
        {
            return null;
        }

        return definition.Reduce(pos.X, pos.Y);
    }

    /// <summary>
    /// Compute Euclidean distance between two entities on a semantic plane.
    /// Used for disposition calculation. Returns null if either entity has no position.
    /// </summary>
    public float? GetDistance(EntityId a, EntityId b, string planeName)
    {
        var posA = GetPosition(a, planeName);
        var posB = GetPosition(b, planeName);

        if (posA is null || posB is null)
        {
            return null;
        }

        var dx = posA.Value.X - posB.Value.X;
        var dy = posA.Value.Y - posB.Value.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public void RemoveEntity(EntityId entityId)
    {
        var keys = _positions.Keys.Where(k => k.EntityId == entityId).ToList();
        foreach (var key in keys)
        {
            _positions.Remove(key);
        }
    }
}
