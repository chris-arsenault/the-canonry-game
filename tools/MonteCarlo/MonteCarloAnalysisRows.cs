namespace MonteCarloTool;

internal sealed class MonteCarloAnalysisReport
{
    public required BatchOverviewRow Overview { get; init; }
    public required IReadOnlyList<AnalysisFlagRow> Flags { get; init; }
    public required IReadOnlyList<GeneratorAnalysisRow> GeneratorBalance { get; init; }
    public required IReadOnlyList<GeneratedSubtypeCoverageRow> GeneratedSubtypeCoverage { get; init; }
    public required IReadOnlyList<PlannerPressureRow> PlannerPressure { get; init; }
    public required IReadOnlyList<ProcessPressureRow> ProcessPressure { get; init; }
    public required IReadOnlyList<AttributeBandSummaryRow> AttributeBands { get; init; }
    public required IReadOnlyList<MapSpreadSummaryRow> MapSpread { get; init; }
    public required IReadOnlyList<MapGenerationSummaryRow> MapGeneration { get; init; }
    public required IReadOnlyList<TerrainOccupancyRow> TerrainOccupancy { get; init; }
    public required IReadOnlyList<LifecycleSummaryRow> Lifecycle { get; init; }
    public required IReadOnlyList<ResourcePressureRow> ResourcePressure { get; init; }
}

internal sealed record BatchOverviewRow(
    int RunCount,
    int MaxTick,
    int TotalGeneratorFires,
    double AverageFiresPerRun,
    double AverageFinalActiveEntities,
    double AverageFinalHistoricalEntities,
    double AverageFinalPhysicalSpread,
    double AverageFinalSemanticSpread,
    double AverageFinalUncoveredCells);

internal sealed record AnalysisFlagRow(string Category, string Severity, string Message);

internal sealed record GeneratorAnalysisRow(
    string GeneratorId,
    string GeneratorName,
    string TargetKind,
    int Pulses,
    int EligiblePulses,
    int BlockedPulses,
    int EligibleNotSelectedPulses,
    int FiredCount,
    int FireFailedPulses,
    double FireRatePerRun,
    double SelectionRateWhenEligible,
    double AverageTargetPool,
    double AverageCandidateCount,
    int InactiveRejected,
    int SubtypeRejected,
    int SaturationRejected,
    int SecondaryTargetRejected,
    int WildernessRejected,
    int TriggerRejected,
    string PrimaryBlocker,
    int CreatedEntityCount,
    int CreatedRelationshipCount);

internal sealed record GeneratedSubtypeCoverageRow(
    string GeneratorId,
    string Kind,
    string Subtype,
    int ProducedCount);

internal sealed record PlannerPressureRow(
    string Kind,
    int Evaluations,
    double StaleRate,
    double NoExecutionRate,
    double AverageCandidateCount,
    string TopExecutedProcess,
    double TopProcessShareOfExecutions);

internal sealed record ProcessPressureRow(
    string Kind,
    string ProcessName,
    int CandidateEvaluations,
    int SelectedCount,
    double SelectionRateWhenOffered,
    double AverageScore,
    double PreconditionPassRate,
    double CooldownRate,
    string TopFailureDiagnostic,
    string EvidenceSource);

internal sealed record AttributeBandSummaryRow(
    string Kind,
    string Subtype,
    string AttributeName,
    int SampleCount,
    double AverageValue,
    double AverageNormalized,
    double MinimumObserved,
    double MaximumObserved,
    double CriticalShare,
    double LowShare,
    double MidShare,
    double HighShare);

internal sealed record MapSpreadSummaryRow(
    string RunId,
    int SnapshotTick,
    int ActiveEntities,
    int OccupiedCells,
    double OccupiedCellShare,
    int BoundingBoxArea,
    double OccupancyWithinBoundingBox,
    double MeanDistanceFromSeed,
    double MaxDistanceFromSeed,
    double Anisotropy);

internal sealed record MapGenerationSummaryRow(
    string Phase,
    string Description,
    string Reasons,
    int EventCount,
    int AffectedRuns,
    int RetryingEvents,
    int MaxAttempt);

internal sealed record TerrainOccupancyRow(
    string Terrain,
    double AverageFinalActiveEntities,
    double AverageNamedCoverageShare);

internal sealed record LifecycleSummaryRow(
    string Kind,
    int Deaths,
    double DeathsPerRun,
    double AverageDeathTick,
    double AverageFinalActive,
    double AverageFinalHistorical,
    double AverageFinalHealth);

internal sealed record ResourcePressureRow(
    string ResourceType,
    double AverageTotalOverTime,
    double AverageInitialTotal,
    double AverageFinalTotal,
    double FinalToInitialRatio,
    double LowestCellStock);
