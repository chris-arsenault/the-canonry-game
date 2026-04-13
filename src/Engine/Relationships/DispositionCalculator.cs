namespace Engine.Relationships;

using Engine.Graph;

/// <summary>
/// Computes disposition between two entities. Disposition is DERIVED (not stored),
/// primarily from Euclidean distance on semantic planes. Used as preconditions
/// for relationship-creating cards.
/// </summary>
public sealed class DispositionCalculator
{
    private readonly IRelationshipContext _context;
    private readonly List<string> _planeNames = [];

    /// <summary>Base disposition when no planes are available (neutral).</summary>
    public float DefaultDisposition { get; set; } = 0.5f;

    /// <summary>Weight of semantic distance in disposition (higher = more sensitive to differences).</summary>
    public float SemanticWeight { get; set; } = 1f;

    public DispositionCalculator(IRelationshipContext context)
    {
        _context = context;
    }

    /// <summary>Register a semantic plane to consider in disposition calculations.</summary>
    public void RegisterPlane(string planeName)
    {
        if (!_planeNames.Contains(planeName))
        {
            _planeNames.Add(planeName);
        }
    }

    /// <summary>
    /// Compute disposition between two entities. Range 0-1.
    /// High = similar (favorable), Low = different (unfavorable).
    /// Returns DefaultDisposition if no planes are registered or entities have no positions.
    /// </summary>
    public float ComputeDisposition(EntityId a, EntityId b)
    {
        if (_planeNames.Count == 0)
        {
            return DefaultDisposition;
        }

        var totalDistance = 0f;
        var planeCount = 0;

        foreach (var plane in _planeNames)
        {
            var dist = _context.GetSemanticDistance(a, b, plane);
            if (dist is not null)
            {
                totalDistance += dist.Value;
                planeCount++;
            }
        }

        if (planeCount == 0)
        {
            return DefaultDisposition;
        }

        // Average distance across all planes. Max possible distance in a 0-1 2D plane is sqrt(2) ≈ 1.414
        var avgDistance = totalDistance / planeCount;
        var maxDistance = MathF.Sqrt(2f);

        // Convert distance to disposition: close = high disposition, far = low
        var normalized = Math.Clamp(avgDistance / maxDistance * SemanticWeight, 0f, 1f);
        return 1f - normalized;
    }
}
