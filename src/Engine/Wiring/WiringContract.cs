namespace Engine.Wiring;

/// <summary>
/// A wiring contract declares both operational and planner-facing dependencies for
/// a process or decision. Used by the WiringValidator to verify feedback loops and
/// emergent plan chains exist.
/// </summary>
public sealed class WiringContract
{
    /// <summary>Name of the process/decision this contract belongs to.</summary>
    public string OwnerName { get; }

    /// <summary>What this process/decision reads from to score and make decisions.</summary>
    public IReadOnlySet<AttributeRef> Reads { get; }

    /// <summary>What this process/decision writes to when it executes.</summary>
    public IReadOnlySet<AttributeRef> Writes { get; }

    /// <summary>Planner effects this process claims it can satisfy.</summary>
    public IReadOnlySet<AttributeRef> GoalEffects { get; }

    /// <summary>Planner selectors used to enumerate external targets/providers.</summary>
    public IReadOnlySet<AttributeRef> ProviderSelectors { get; }

    /// <summary>Planner-managed blockers inferred from the process precondition tree.</summary>
    public IReadOnlySet<AttributeRef> PlannerBlockers { get; }

    /// <summary>True when the precondition tree contains planner logic that static validation cannot fully resolve.</summary>
    public bool HasOpaquePlannerBranches { get; }

    public WiringContract(
        string ownerName,
        IReadOnlySet<AttributeRef> reads,
        IReadOnlySet<AttributeRef> writes,
        IReadOnlySet<AttributeRef>? goalEffects = null,
        IReadOnlySet<AttributeRef>? providerSelectors = null,
        IReadOnlySet<AttributeRef>? plannerBlockers = null,
        bool hasOpaquePlannerBranches = false)
    {
        OwnerName = ownerName;
        Reads = reads;
        Writes = writes;
        GoalEffects = goalEffects ?? new HashSet<AttributeRef>();
        ProviderSelectors = providerSelectors ?? new HashSet<AttributeRef>();
        PlannerBlockers = plannerBlockers ?? new HashSet<AttributeRef>();
        HasOpaquePlannerBranches = hasOpaquePlannerBranches;
    }
}
