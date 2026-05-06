namespace MonteCarloTool;

internal sealed class MonteCarloAnalysisReport
{
    public required BatchOverviewRow Overview { get; init; }
    public required IReadOnlyList<AnalysisFlagRow> Flags { get; init; }
    public required IReadOnlyList<GambitSuccessRow> GambitSuccess { get; init; }
    public required IReadOnlyList<GeneratorAnalysisRow> GeneratorBalance { get; init; }
    public required IReadOnlyList<GeneratedSubtypeCoverageRow> GeneratedSubtypeCoverage { get; init; }
    public required IReadOnlyList<LowAltitudeVerbSummaryRow> LowAltitudeVerbUtilization { get; init; }
    public required IReadOnlyList<RoamingSummaryRow> RoamingSummary { get; init; }
    public required IReadOnlyList<InfluenceSummaryRow> InfluenceSummary { get; init; }
    public required IReadOnlyList<ActionUtilizationRow> ActionUtilization { get; init; }
    public required IReadOnlyList<AttributeBandSummaryRow> AttributeBands { get; init; }
    public required IReadOnlyList<MapSpreadSummaryRow> MapSpread { get; init; }
    public required IReadOnlyList<MapGenerationSummaryRow> MapGeneration { get; init; }
    public required IReadOnlyList<TerrainOccupancyRow> TerrainOccupancy { get; init; }
    public required IReadOnlyList<LifecycleSummaryRow> Lifecycle { get; init; }
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

internal sealed record GambitSuccessRow(
    string GambitId,
    int AttemptCount,
    int SuccessCount,
    int FailureCount,
    double SuccessRate,
    int FirstAttemptTick,
    int LastAttemptTick,
    int DistinctActors,
    int DistinctTargets);

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
    int SecondaryTargetRejected,
    int TriggerRejected,
    string PrimaryBlocker,
    int CreatedEntityCount,
    int CreatedRelationshipCount);

internal sealed record GeneratedSubtypeCoverageRow(
    string GeneratorId,
    string Kind,
    string Subtype,
    int ProducedCount);

internal sealed record LowAltitudeVerbSummaryRow(
    string Kind,
    string Verb,
    string Operation,
    string TargetMode,
    string Plane,
    int OfferedCount,
    int SelectedCount,
    int NoTargetCount,
    double SelectionRateWhenOffered,
    double AverageSelectedWeight);

internal sealed record RoamingSummaryRow(
    string Kind,
    string Plane,
    int EntityCount,
    int MoverCount,
    double ZeroMoveShare,
    double AverageMoveCount,
    double AverageUniquePositions,
    double AverageRoamRadius);

internal sealed record InfluenceSummaryRow(
    string Kind,
    string Plane,
    int EntityCount,
    double AverageInfluencedCells,
    double AverageNetInfluenceDelta,
    double AverageMeanAnchorDistance,
    double AverageMaxAnchorDistance);

internal sealed record ActionUtilizationRow(
    string Kind,
    string ActionName,
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
    int ActivityEventCount,
    int ActivityCells,
    double ActivityCellShare,
    double ActivityEntropy,
    double ActivityTopCellShare,
    int ActivityBoundingBoxArea,
    double ActivityCoverageWithinBoundingBox,
    double ActivityAxisAnisotropy,
    int LargestUnoccupiedStrictHexRadius);

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
