namespace Engine.Persistence;

using MessagePack;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Serializable snapshot of the full simulation state.
/// Reconstructs all in-memory structures on load.
/// </summary>
#pragma warning disable CA1002, CA2227 // MessagePack DTOs require mutable List<T> with setters
[MessagePackObject]
public sealed class SaveState
{
    [Key(0)]
    public int Version { get; set; } = 1;

    [Key(1)]
    public int CurrentTick { get; set; }

    [Key(2)]
    public List<EntityData> Entities { get; set; } = [];

    [Key(3)]
    public List<RelationshipData> Relationships { get; set; } = [];

    [Key(4)]
    public List<AttributeData> Attributes { get; set; } = [];

    [Key(5)]
    public float CurrentMana { get; set; }

    [Key(6)]
    public float ManaCap { get; set; }
}

[MessagePackObject]
public sealed class EntityData
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public EntityKind Kind { get; set; }

    [Key(2)]
    public string Subtype { get; set; } = string.Empty;

    [Key(3)]
    public string Name { get; set; } = string.Empty;

    [Key(4)]
    public EntityStatus Status { get; set; }

    [Key(5)]
    public string Culture { get; set; } = string.Empty;

    [Key(6)]
    public int CreatedAt { get; set; }

    [Key(7)]
    public int UpdatedAt { get; set; }

    [Key(8)]
    public int? CoordQ { get; set; }

    [Key(9)]
    public int? CoordR { get; set; }
}

[MessagePackObject]
public sealed class RelationshipData
{
    [Key(0)]
    public string Kind { get; set; } = string.Empty;

    [Key(1)]
    public RelationshipPolarity Polarity { get; set; }

    [Key(2)]
    public int SourceId { get; set; }

    [Key(3)]
    public int TargetId { get; set; }

    [Key(4)]
    public float Strength { get; set; }

    [Key(5)]
    public int StartDate { get; set; }

    [Key(6)]
    public int? EndDate { get; set; }
}

[MessagePackObject]
public sealed class AttributeData
{
    [Key(0)]
    public int EntityId { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [Key(2)]
    public float Value { get; set; }

    [Key(3)]
    public float Min { get; set; }

    [Key(4)]
    public float Max { get; set; }
}
