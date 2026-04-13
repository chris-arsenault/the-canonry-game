namespace Engine.Degeneracy;

/// <summary>
/// Severity levels for degeneracy detection.
/// </summary>
public enum Severity
{
    Normal,
    Warning,
    Critical,
}

/// <summary>
/// A detector that evaluates world state for a specific degeneracy pattern.
/// Detectors are registered in a registry and evaluated periodically.
/// </summary>
public interface IDegeneracyDetector
{
    /// <summary>Human-readable name of the degeneracy pattern.</summary>
    string Name { get; }

    /// <summary>Human-readable warning message when severity >= Warning.</summary>
    string WarningMessage { get; }

    /// <summary>Evaluate the current world state and return severity.</summary>
    Severity Evaluate(IDegeneracyContext context);
}

/// <summary>
/// Context for degeneracy detectors to access world state.
/// </summary>
public interface IDegeneracyContext
{
    int ActiveSettlementCount { get; }
    int DecliningSettlementCount { get; }
    int ActiveNationCount { get; }
    int MaxNationSettlementCount { get; }
    int TotalSettlementCount { get; }
    int ActiveNpcCount { get; }
}
