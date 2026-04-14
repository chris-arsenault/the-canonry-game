namespace Engine.Processes;

/// <summary>
/// Result of precondition evaluation. Includes diagnostic info for debugging
/// and blocking attribute information for sub-goal generation.
/// </summary>
public sealed class PreconditionResult
{
    public bool Passed { get; }
    public string Diagnostic { get; }
    public IReadOnlyList<string> BlockingAttributes { get; }

    private PreconditionResult(bool passed, string diagnostic, IReadOnlyList<string> blockingAttributes)
    {
        Passed = passed;
        Diagnostic = diagnostic;
        BlockingAttributes = blockingAttributes;
    }

    public static PreconditionResult Pass(string diagnostic = "") =>
        new(true, diagnostic, []);

    public static PreconditionResult Fail(string diagnostic, params string[] blockingAttributes) =>
        new(false, diagnostic, blockingAttributes);

    public static PreconditionResult Fail(string diagnostic, IReadOnlyList<string> blockingAttributes) =>
        new(false, diagnostic, blockingAttributes);
}
