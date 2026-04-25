namespace Engine.Graph;

using Engine.Processes;
using Engine.Wiring;

/// <summary>
/// A persistent goal an entity is working toward. Entities maintain multiple
/// concurrent goals. Processes serve goals. Sub-goals are generated from
/// blocked preconditions.
/// </summary>
public sealed class EntityGoal
{
    private static int _nextId;

    /// <summary>Unique ID within the entity's goal list.</summary>
    public GoalRuntimeId Id { get; }

    /// <summary>The specific scoped effect this goal wants to produce.</summary>
    public AttributeRef DesiredEffect { get; }

    /// <summary>The entity this plan acts on/toward when evaluating the planned process.</summary>
    public EntityId TargetEntityId { get; }

    /// <summary>How urgent this goal is (from attribute deficit).</summary>
    public float Urgency { get; set; }

    /// <summary>The process this goal is currently planning around. Null for abstract blocker goals.</summary>
    public ProcessRuntimeId? PlannedProcessId { get; }

    /// <summary>Parent goal ID. Non-null for sub-goals.</summary>
    public GoalRuntimeId? ParentGoalId { get; }

    /// <summary>Whether this goal is the current actionable step of a plan.</summary>
    public bool IsActionable { get; set; }

    /// <summary>Planner depth. Primary goals are depth 0.</summary>
    public int Depth { get; }

    public EntityGoal(AttributeRef desiredEffect, EntityId targetEntityId, float urgency,
        ProcessRuntimeId? plannedProcessId = null, GoalRuntimeId? parentGoalId = null,
        bool isActionable = true, int depth = 0)
    {
        Id = new GoalRuntimeId(System.Threading.Interlocked.Increment(ref _nextId));
        DesiredEffect = desiredEffect;
        TargetEntityId = targetEntityId;
        Urgency = urgency;
        PlannedProcessId = plannedProcessId;
        ParentGoalId = parentGoalId;
        IsActionable = isActionable;
        Depth = depth;
    }

    /// <summary>Whether this is a sub-goal (has a parent).</summary>
    public bool IsSubGoal => ParentGoalId is not null;
}
