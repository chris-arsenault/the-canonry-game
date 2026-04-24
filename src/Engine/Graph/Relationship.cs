namespace Engine.Graph;

/// <summary>
/// Polarity of a relationship kind. Determines the semantic meaning of strength.
/// Positive: higher strength = stronger positive connection (allied_with).
/// Negative: higher strength = stronger negative connection (rival_of).
/// Neutral: strength = degree of connection, no sentiment (controls, located_in).
/// </summary>
public enum RelationshipPolarity
{
    Positive,
    Negative,
    Neutral,
}

/// <summary>
/// An edge in the entity graph. Represents a typed, directed connection
/// between two entities with strength and temporal metadata.
/// </summary>
public sealed class Relationship
{
    public string Kind { get; }
    public RelationshipPolarity Polarity { get; }
    public EntityId SourceId { get; }
    public EntityId TargetId { get; }

    /// <summary>Strength 0.0 to 1.0. Higher = stronger connection.</summary>
    public float Strength { get; set; }

    public int StartDate { get; }

    /// <summary>Null if active. Set to tick when culled/ended.</summary>
    public int? EndDate { get; set; }

    public bool IsActive => EndDate is null;

    private readonly HashSet<string> _facts;
    public IReadOnlySet<string> Facts => _facts;

    private Dictionary<string, string>? _metadata;
    public IReadOnlyDictionary<string, string>? Metadata => _metadata;

    public bool HasFact(string fact) => _facts.Contains(fact);

    public void SetMetadata(string key, string value)
    {
        _metadata ??= [];
        _metadata[key] = value;
    }

    public Relationship(
        string kind,
        RelationshipPolarity polarity,
        EntityId sourceId,
        EntityId targetId,
        float strength,
        int startDate,
        IEnumerable<string>? facts = null)
    {
        Kind = kind;
        Polarity = polarity;
        SourceId = sourceId;
        TargetId = targetId;
        Strength = Math.Clamp(strength, 0f, 1f);
        StartDate = startDate;
        _facts = facts is null
            ? []
            : new HashSet<string>(facts, StringComparer.Ordinal);
    }
}
