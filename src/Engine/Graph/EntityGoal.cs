namespace Engine.Graph;

/// <summary>
/// A process-provider pair that an entity is working toward.
/// Persists across ticks until the entity arrives at the target
/// and executes the process, or until preempted by a higher-scoring pair.
/// </summary>
public sealed class EntityGoal
{
    /// <summary>The process that will fire when the entity reaches the target.</summary>
    public string ProcessId { get; }

    /// <summary>The provider entity that satisfies the process's needs.</summary>
    public EntityId TargetEntityId { get; }

    /// <summary>The coordinates to move toward.</summary>
    public (int Q, int R) TargetCoordinates { get; }

    /// <summary>The score this goal had when selected (for preemption comparison).</summary>
    public float Score { get; }

    public EntityGoal(string processId, EntityId targetEntityId, (int Q, int R) targetCoordinates, float score)
    {
        ProcessId = processId;
        TargetEntityId = targetEntityId;
        TargetCoordinates = targetCoordinates;
        Score = score;
    }
}
