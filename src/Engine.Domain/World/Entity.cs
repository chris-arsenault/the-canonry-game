namespace Engine.Graph;

using Engine.Domain;
using Engine.Domain.Spatial;
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
    public SubtypeRuntimeId SubtypeId { get; }
    public string Name { get; set; }
    public EntityStatus Status { get; set; }
    public string Culture { get; set; }
    public CultureRuntimeId CultureId { get; }
    public int CreatedAt { get; }
    public int UpdatedAt { get; set; }

    // Every entity carries a physical hex coordinate. Stationary kinds pin to a
    // representative hex; mobile kinds move through the grid.
    public (int Q, int R) Coordinates { get; set; }

    /// <summary>
    /// Position on the shared semantic hex grid (neutral q/r idea-space axes).
    /// Every entity carries a semantic position. Physical-mobile entities also carry
    /// physical Coordinates; stationary entities move only on the semantic grid.
    /// </summary>
    public SemanticHexCoord SemanticPosition { get; set; } = SemanticHexCoord.Origin;

    /// <summary>Entity-wide cooldown: tick when entity can act again.</summary>
    public int EntityCooldownUntil { get; set; }

    /// <summary>Health value history for graphing. (tick, value).</summary>
    public Queue<(int Tick, float Value)> HealthHistory { get; } = new();

    private const int MaxHistorySize = 60;

    /// <summary>Record a health value snapshot.</summary>
    public void RecordHealth(int tick, float value)
    {
        HealthHistory.Enqueue((tick, value));
        while (HealthHistory.Count > MaxHistorySize) HealthHistory.Dequeue();
    }

    public Entity(
        EntityId id,
        EntityKind kind,
        string subtype,
        string name,
        string culture,
        int createdAt,
        (int Q, int R)? coordinates = null,
        SubtypeRuntimeId subtypeId = default,
        CultureRuntimeId cultureId = default)
    {
        Id = id;
        Kind = kind;
        Subtype = subtype;
        SubtypeId = subtypeId;
        Name = name;
        Culture = culture;
        CultureId = cultureId;
        Status = EntityStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Coordinates = coordinates ?? (0, 0);
    }
}
