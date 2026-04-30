namespace Engine.Wiring;

/// <summary>
/// A wiring contract declares both operational and behavior program-facing dependencies for
/// a behavior or gambit. Used by the WiringValidator to verify feedback loops and
/// emergent plan chains exist.
/// </summary>
public sealed class WiringContract
{
    /// <summary>Name of the behavior/gambit this contract belongs to.</summary>
    public string OwnerName { get; }

    /// <summary>What this behavior/gambit reads from to score and select behavior.</summary>
    public IReadOnlySet<AttributeRef> Reads { get; }

    /// <summary>What this behavior/gambit writes to when it executes.</summary>
    public IReadOnlySet<AttributeRef> Writes { get; }

    /// <summary>BehaviorProgram effects this behavior claims it can satisfy.</summary>
    public IReadOnlySet<AttributeRef> GoalEffects { get; }

    /// <summary>BehaviorProgram selectors used to enumerate external targets/providers.</summary>
    public IReadOnlySet<AttributeRef> ProviderSelectors { get; }

    /// <summary>BehaviorProgram-managed blockers inferred from the behavior precondition tree.</summary>
    public IReadOnlySet<AttributeRef> BehaviorBlockers { get; }

    /// <summary>
    /// BehaviorProgram-managed support effects that may be chosen to improve the utility
    /// of an otherwise viable behavior.
    /// </summary>
    public IReadOnlySet<AttributeRef> BehaviorSupportEffects { get; }

    /// <summary>True when the precondition tree contains behavior program logic that static validation cannot fully resolve.</summary>
    public bool HasOpaqueBehaviorBranches { get; }

    public WiringContract(
        string ownerName,
        IReadOnlySet<AttributeRef> reads,
        IReadOnlySet<AttributeRef> writes,
        IReadOnlySet<AttributeRef>? goalEffects = null,
        IReadOnlySet<AttributeRef>? providerSelectors = null,
        IReadOnlySet<AttributeRef>? behaviorBlockers = null,
        IReadOnlySet<AttributeRef>? behaviorSupportEffects = null,
        bool hasOpaqueBehaviorBranches = false)
    {
        OwnerName = ownerName;
        Reads = reads;
        Writes = writes;
        GoalEffects = goalEffects ?? new HashSet<AttributeRef>();
        ProviderSelectors = providerSelectors ?? new HashSet<AttributeRef>();
        BehaviorBlockers = behaviorBlockers ?? new HashSet<AttributeRef>();
        BehaviorSupportEffects = behaviorSupportEffects ?? new HashSet<AttributeRef>();
        HasOpaqueBehaviorBranches = hasOpaqueBehaviorBranches;
    }
}
