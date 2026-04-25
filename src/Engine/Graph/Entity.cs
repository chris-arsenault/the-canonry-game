namespace Engine.Graph;

using Engine.Domain;
using Engine.Kinds;
using Engine.Map;
using Engine.Processes;

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
    /// Position on the shared semantic hex grid (autonomy/resonance/tradition).
    /// Every entity carries a semantic position. Physical-mobile entities also carry
    /// physical Coordinates; stationary entities move only on the semantic grid.
    /// </summary>
    public SemanticHexCoord SemanticPosition { get; set; } = SemanticHexCoord.Origin;

    /// <summary>Entity-owned attribute instances. Initialized from domain config at creation.</summary>
    public Attributes.AttributeSet Attributes { get; } = new();

    /// <summary>Active goals. Max 3 primary + their sub-goals. Regenerated periodically.</summary>
    public System.Collections.ObjectModel.Collection<EntityGoal> Goals { get; } = [];

    /// <summary>Tick when goals were last generated.</summary>
    public int GoalsGeneratedAt { get; set; }

    /// <summary>Engine-produced evaluation report from the last tick. UI reads this.</summary>
    public EvaluationReport? LastEvaluation { get; set; }

    /// <summary>
    /// Short-horizon locality commitment used by the planner to make movement choose
    /// destinations that unlock multiple follow-up actions instead of replanning
    /// every tick from scratch.
    /// </summary>
    public LocalityVisitIntent? ActiveVisitIntent { get; set; }

    /// <summary>Per-process cooldown tracking: process runtime id → tick when cooldown expires.</summary>
    public Dictionary<ProcessRuntimeId, int> ProcessCooldowns { get; } = [];

    /// <summary>Per-definition last execution tick keyed by primitive runtime id.</summary>
    public Dictionary<ProcessRuntimeId, int> LastDefinitionExecutionTicks { get; } = [];

    /// <summary>Entity-wide cooldown: tick when entity can act again.</summary>
    public int EntityCooldownUntil { get; set; }

    /// <summary>Primitive id of the last process or action that executed for this entity.</summary>
    public ProcessRuntimeId LastActionProcessId { get; set; }

    /// <summary>Tick when the last process or action executed.</summary>
    public int? LastActionTick { get; set; }

    /// <summary>Recent action history for frequency display. (tick, process id).</summary>
    public Queue<(int Tick, ProcessRuntimeId ProcessId)> ActionHistory { get; } = new();

    /// <summary>Health value history for graphing. (tick, value).</summary>
    public Queue<(int Tick, float Value)> HealthHistory { get; } = new();

    private const int MaxHistorySize = 60;

    /// <summary>Record an action in the history.</summary>
    public void RecordAction(int tick, ProcessRuntimeId processId)
    {
        LastActionProcessId = processId;
        LastActionTick = tick;
        LastDefinitionExecutionTicks[processId] = tick;

        ActionHistory.Enqueue((tick, processId));
        while (ActionHistory.Count > MaxHistorySize) ActionHistory.Dequeue();
    }

    /// <summary>Record a health value snapshot.</summary>
    public void RecordHealth(int tick, float value)
    {
        HealthHistory.Enqueue((tick, value));
        while (HealthHistory.Count > MaxHistorySize) HealthHistory.Dequeue();
    }

    /// <summary>
    /// Count how many times <paramref name="processId"/> fired on this entity at or
    /// after <paramref name="sinceTick"/>. Used by the utility AI to soft-penalize
    /// recent repetition — a process fired N times recently gets its score scaled
    /// down, encouraging the planner to consider alternatives without hard-locking.
    /// </summary>
    public int CountRecentActions(ProcessRuntimeId processId, int sinceTick)
    {
        var count = 0;
        foreach (var (tick, recordedProcessId) in ActionHistory)
        {
            if (tick >= sinceTick && recordedProcessId == processId)
            {
                count++;
            }
        }
        return count;
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
