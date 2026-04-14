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

    /// <summary>Engine-produced evaluation report from the last tick. UI reads this.</summary>
    public EvaluationReport? LastEvaluation { get; set; }

    /// <summary>Per-process cooldown tracking: processId → tick when cooldown expires.</summary>
    public Dictionary<string, int> ProcessCooldowns { get; } = [];

    /// <summary>Entity-wide cooldown: tick when entity can act again.</summary>
    public int EntityCooldownUntil { get; set; }

    /// <summary>Name of the last process or action that executed for this entity.</summary>
    public string? LastActionName { get; set; }

    /// <summary>Tick when the last process or action executed.</summary>
    public int? LastActionTick { get; set; }

    /// <summary>Recent action history for frequency display. (tick, processName).</summary>
    public Queue<(int Tick, string Name)> ActionHistory { get; } = new();

    /// <summary>Health value history for graphing. (tick, value).</summary>
    public Queue<(int Tick, float Value)> HealthHistory { get; } = new();

    private const int MaxHistorySize = 60;

    /// <summary>Record an action in the history.</summary>
    public void RecordAction(int tick, string name)
    {
        LastActionName = name;
        LastActionTick = tick;
        ActionHistory.Enqueue((tick, name));
        while (ActionHistory.Count > MaxHistorySize) ActionHistory.Dequeue();
    }

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
