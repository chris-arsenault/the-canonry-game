namespace Engine.Wiring;

/// <summary>
/// A wiring contract declares both operational and process program-facing dependencies for
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

    /// <summary>ProcessProgram effects this process claims it can satisfy.</summary>
    public IReadOnlySet<AttributeRef> GoalEffects { get; }

    /// <summary>ProcessProgram selectors used to enumerate external targets/providers.</summary>
    public IReadOnlySet<AttributeRef> ProviderSelectors { get; }

    /// <summary>ProcessProgram-managed blockers inferred from the process precondition tree.</summary>
    public IReadOnlySet<AttributeRef> ProcessBlockers { get; }

    /// <summary>
    /// ProcessProgram-managed support effects that may be chosen to improve the utility
    /// of an otherwise viable process.
    /// </summary>
    public IReadOnlySet<AttributeRef> ProcessSupportEffects { get; }

    /// <summary>True when the precondition tree contains process program logic that static validation cannot fully resolve.</summary>
    public bool HasOpaqueProcessBranches { get; }

    public WiringContract(
        string ownerName,
        IReadOnlySet<AttributeRef> reads,
        IReadOnlySet<AttributeRef> writes,
        IReadOnlySet<AttributeRef>? goalEffects = null,
        IReadOnlySet<AttributeRef>? providerSelectors = null,
        IReadOnlySet<AttributeRef>? processBlockers = null,
        IReadOnlySet<AttributeRef>? processSupportEffects = null,
        bool hasOpaqueProcessBranches = false)
    {
        OwnerName = ownerName;
        Reads = reads;
        Writes = writes;
        GoalEffects = goalEffects ?? new HashSet<AttributeRef>();
        ProviderSelectors = providerSelectors ?? new HashSet<AttributeRef>();
        ProcessBlockers = processBlockers ?? new HashSet<AttributeRef>();
        ProcessSupportEffects = processSupportEffects ?? new HashSet<AttributeRef>();
        HasOpaqueProcessBranches = hasOpaqueProcessBranches;
    }
}
