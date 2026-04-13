namespace Engine.Wiring;

/// <summary>
/// A wiring contract declares what a process or decision reads from
/// and writes to. Used by the WiringValidator to verify feedback loops exist.
/// </summary>
public sealed class WiringContract
{
    /// <summary>Name of the process/decision this contract belongs to.</summary>
    public string OwnerName { get; }

    /// <summary>What this process/decision reads from to score and make decisions.</summary>
    public IReadOnlySet<AttributeRef> Reads { get; }

    /// <summary>What this process/decision writes to when it executes.</summary>
    public IReadOnlySet<AttributeRef> Writes { get; }

    public WiringContract(string ownerName, IReadOnlySet<AttributeRef> reads, IReadOnlySet<AttributeRef> writes)
    {
        OwnerName = ownerName;
        Reads = reads;
        Writes = writes;
    }
}
