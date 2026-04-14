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

    /// <summary>Entity-owned attribute instances. Initialized from domain config at creation.</summary>
    public Attributes.AttributeSet Attributes { get; } = new();

    /// <summary>Active goals. Max 3 primary + their sub-goals. Regenerated periodically.</summary>
    public System.Collections.ObjectModel.Collection<EntityGoal> Goals { get; } = [];

    /// <summary>Tick when goals were last generated.</summary>
    public int GoalsGeneratedAt { get; set; }

    /// <summary>Transient: target entity for current process execution. Set by evaluator, cleared after.</summary>
    public EntityId? CurrentTargetId { get; set; }

    /// <summary>Name of the last process or action that executed for this entity.</summary>
    public string? LastActionName { get; set; }

    /// <summary>Tick when the last process or action executed.</summary>
    public int? LastActionTick { get; set; }

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
