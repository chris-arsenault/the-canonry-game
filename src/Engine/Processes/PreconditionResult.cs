namespace Engine.Processes;

using Engine.Wiring;

/// <summary>
/// Result of precondition evaluation. Includes diagnostic info for debugging
/// and blocking attribute information for sub-goal generation.
/// </summary>
public sealed class PreconditionResult
{
    public bool Passed { get; }
    public string Diagnostic { get; }
    public IReadOnlyList<AttributeRef> Blockers { get; }

    private PreconditionResult(bool passed, string diagnostic, IReadOnlyList<AttributeRef> blockers)
    {
        Passed = passed;
        Diagnostic = diagnostic;
        Blockers = blockers;
    }

    public static PreconditionResult Pass(string diagnostic = "") =>
        new(true, diagnostic, []);

    public static PreconditionResult Fail(string diagnostic, params AttributeRef[] blockers) =>
        new(false, diagnostic, blockers);

    public static PreconditionResult Fail(string diagnostic, IReadOnlyList<AttributeRef> blockers) =>
        new(false, diagnostic, blockers);
}
