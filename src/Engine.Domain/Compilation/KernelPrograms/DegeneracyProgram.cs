namespace Engine.Domain.Compilation.KernelPrograms;

public sealed class DegeneracyProgram
{
    public bool EnabledByDefault { get; init; } = true;
    public IReadOnlyList<CompiledDegeneracyDetectorRule> Detectors { get; init; } = [];
}

public sealed record CompiledDegeneracyDetectorRule(
    int DetectorId,
    DegeneracyProgramMetric Metric,
    DegeneracyProgramThresholdDirection ThresholdDirection,
    DegeneracyProgramSeverity NoSubjectsSeverity,
    int ReasonCodeId,
    int PrimaryKindId,
    int SecondaryKindId,
    int AttributeId,
    int RelationshipKindId,
    DegeneracyProgramRelationshipEndpoint DominantRelationshipEndpoint,
    float WarningThreshold,
    float CriticalThreshold);

public enum DegeneracyProgramMetric
{
    AttributeDeclineRatio,
    RelationshipEndpointDominanceRatio,
    ActiveKindCount,
}

public enum DegeneracyProgramThresholdDirection
{
    HighIsBad,
    LowIsBad,
}

public enum DegeneracyProgramRelationshipEndpoint
{
    Source,
    Target,
}

public enum DegeneracyProgramSeverity
{
    Normal,
    Warning,
    Critical,
}
