namespace Engine.Graph;

using Engine.Kinds;

/// <summary>
/// A node in the entity graph. Stores identity and structural data only.
/// Numeric attributes (stability, population, etc.) live in the Attributes system.
/// </summary>
public sealed class Entity
{
    public EntityId Id { get; }
    public EntityKind Kind { get; }
    public string Subtype { get; set; }
    public string Name { get; set; }
    public EntityStatus Status { get; set; }
    public string Culture { get; set; }
    public int CreatedAt { get; }
    public int UpdatedAt { get; set; }

    // Coordinates stored as axial hex (q, r). Nullable for non-spatial entities.
    public (int Q, int R)? Coordinates { get; set; }

    /// <summary>Current process-provider goal. Set by the evaluator, persists across ticks
    /// until reached or preempted. Null when the entity has no active goal.</summary>
    public EntityGoal? CurrentGoal { get; set; }

    public Entity(
        EntityId id,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        int createdAt,
        (int Q, int R)? coordinates = null)
    {
        Id = id;
        Kind = kind;
        Subtype = subtype;
        Name = name;
        Culture = culture;
        Status = EntityStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Coordinates = coordinates;
    }
}
