namespace Engine.Wiring;

/// <summary>
/// A wiring contract declares operational dependencies for a behavior or gambit.
/// Used by the WiringValidator to verify feedback loops exist.
/// </summary>
public sealed class WiringContract
{
    /// <summary>Name of the behavior/gambit this contract belongs to.</summary>
    public string OwnerName { get; }

    /// <summary>What this behavior/gambit reads from to score and select behavior.</summary>
    public IReadOnlySet<AttributeRef> Reads { get; }

    /// <summary>What this behavior/gambit writes to when it executes.</summary>
    public IReadOnlySet<AttributeRef> Writes { get; }

    /// <summary>BehaviorProgram selectors used to enumerate external targets/providers.</summary>
    public IReadOnlySet<AttributeRef> ProviderSelectors { get; }

    public WiringContract(
        string ownerName,
        IReadOnlySet<AttributeRef> reads,
        IReadOnlySet<AttributeRef> writes,
        IReadOnlySet<AttributeRef>? providerSelectors = null)
    {
        OwnerName = ownerName;
        Reads = reads;
        Writes = writes;
        ProviderSelectors = providerSelectors ?? new HashSet<AttributeRef>();
    }
}
