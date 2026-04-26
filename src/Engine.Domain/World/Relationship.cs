namespace Engine.Graph;

using Engine.Domain;

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
    public RelationshipKindRuntimeId KindId { get; }
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
    public RelationshipFactRuntimeId FactId0 { get; }
    public RelationshipFactRuntimeId FactId1 { get; }
    public RelationshipFactRuntimeId FactId2 { get; }
    public RelationshipFactRuntimeId FactId3 { get; }
    public int FactCount { get; }

    private Dictionary<string, string>? _metadata;
    public IReadOnlyDictionary<string, string>? Metadata => _metadata;

    public bool HasFact(string fact) => _facts.Contains(fact);
    public bool HasFact(RelationshipFactRuntimeId factId) =>
        factId.Value > 0 &&
        ((FactCount > 0 && FactId0 == factId) ||
         (FactCount > 1 && FactId1 == factId) ||
         (FactCount > 2 && FactId2 == factId) ||
         (FactCount > 3 && FactId3 == factId));

    public RelationshipFactRuntimeId GetFactId(int index) =>
        index switch
        {
            0 when FactCount > 0 => FactId0,
            1 when FactCount > 1 => FactId1,
            2 when FactCount > 2 => FactId2,
            3 when FactCount > 3 => FactId3,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

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
        IEnumerable<string>? facts = null,
        RelationshipKindRuntimeId kindId = default,
        RelationshipFactRuntimeId factId0 = default,
        RelationshipFactRuntimeId factId1 = default,
        RelationshipFactRuntimeId factId2 = default,
        RelationshipFactRuntimeId factId3 = default,
        int factCount = 0)
    {
        if (factCount is < 0 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(factCount), "Relationships support up to four primitive facts.");
        }

        Kind = kind;
        KindId = kindId;
        Polarity = polarity;
        SourceId = sourceId;
        TargetId = targetId;
        Strength = Math.Clamp(strength, 0f, 1f);
        StartDate = startDate;
        _facts = facts is null
            ? []
            : new HashSet<string>(facts, StringComparer.Ordinal);
        FactCount = factCount;

        Span<int> sortedFactIds = stackalloc int[4];
        sortedFactIds[0] = factId0.Value;
        sortedFactIds[1] = factId1.Value;
        sortedFactIds[2] = factId2.Value;
        sortedFactIds[3] = factId3.Value;
        sortedFactIds[..factCount].Sort();

        FactId0 = new RelationshipFactRuntimeId(sortedFactIds[0]);
        FactId1 = new RelationshipFactRuntimeId(sortedFactIds[1]);
        FactId2 = new RelationshipFactRuntimeId(sortedFactIds[2]);
        FactId3 = new RelationshipFactRuntimeId(sortedFactIds[3]);
    }
}
