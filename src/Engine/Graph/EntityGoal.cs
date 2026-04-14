namespace Engine.Graph;

/// <summary>
/// A persistent goal an entity is working toward. Entities maintain multiple
/// concurrent goals. Processes serve goals. Sub-goals are generated from
/// blocked preconditions.
/// </summary>
public sealed class EntityGoal
{
    private static int _nextId;

    /// <summary>Unique ID within the entity's goal list.</summary>
    public string Id { get; }

    /// <summary>The attribute this goal wants to change.</summary>
    public string Attribute { get; }

    /// <summary>The entity to act on/toward (self or another entity).</summary>
    public EntityId Provider { get; }

    /// <summary>How urgent this goal is (from attribute deficit).</summary>
    public float Urgency { get; set; }

    /// <summary>Parent goal ID. Non-null for sub-goals.</summary>
    public string? ParentGoalId { get; }

    /// <summary>For sub-goals: which attribute is being unblocked on the parent's process.</summary>
    public string? BlockingAttribute { get; }

    public EntityGoal(string attribute, EntityId provider, float urgency,
        string? parentGoalId = null, string? blockingAttribute = null)
    {
        Id = $"goal_{System.Threading.Interlocked.Increment(ref _nextId)}";
        Attribute = attribute;
        Provider = provider;
        Urgency = urgency;
        ParentGoalId = parentGoalId;
        BlockingAttribute = blockingAttribute;
    }

    /// <summary>Whether this is a sub-goal (has a parent).</summary>
    public bool IsSubGoal => ParentGoalId is not null;

    /// <summary>The attribute that processes should write to in order to serve this goal.
    /// For primary goals: the goal's attribute. For sub-goals: the blocking attribute.</summary>
    public string TargetAttribute => BlockingAttribute ?? Attribute;
}
