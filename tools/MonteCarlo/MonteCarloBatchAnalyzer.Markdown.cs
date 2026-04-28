namespace MonteCarloTool;

using System.Globalization;
using System.Text;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private string RenderMarkdown(MonteCarloAnalysisReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Monte Carlo Tuning Summary");
        builder.AppendLine();
        AppendBatchSection(builder, report.Overview);
        AppendFlagsSection(builder, report.Flags);
        AppendMapGenerationSection(builder, report.MapGeneration);
        AppendGeneratorSection(builder, report.GeneratorBalance);
        AppendPlannerSection(builder, report.PlannerPressure);
        AppendProcessSection(builder, report.ProcessPressure);
        AppendAttributeSection(builder, report.AttributeBands);
        AppendMapSpreadSection(builder, report.MapSpread);
        AppendLifecycleSection(builder, report.Lifecycle);
        AppendResourceSection(builder, report.ResourcePressure);
        return builder.ToString();
    }

    private static void AppendBatchSection(StringBuilder builder, BatchOverviewRow overview)
    {
        builder.AppendLine("## Batch");
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Runs: {overview.RunCount}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Max tick: {overview.MaxTick}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Total generator fires: {overview.TotalGeneratorFires}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Avg fires/run: {overview.AverageFiresPerRun:F2}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Avg final active entities: {overview.AverageFinalActiveEntities:F2}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Avg final historical entities: {overview.AverageFinalHistoricalEntities:F2}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Avg final physical spread: {overview.AverageFinalPhysicalSpread:F2}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Avg final semantic spread: {overview.AverageFinalSemanticSpread:F2}"));
        builder.AppendLine();
    }

    private static void AppendFlagsSection(StringBuilder builder, IReadOnlyList<AnalysisFlagRow> flags)
    {
        builder.AppendLine("## Flags");
        if (flags.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var flag in flags)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- [{flag.Severity}] {flag.Category}: {flag.Message}");
            }
        }
    }

    private static void AppendMapGenerationSection(StringBuilder builder, IReadOnlyList<MapGenerationSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Map Generation");
        AppendTable(
            builder,
            rows.Take(10),
            ("phase", row => row.Phase),
            ("events", row => row.EventCount.ToString(CultureInfo.InvariantCulture)),
            ("runs", row => row.AffectedRuns.ToString(CultureInfo.InvariantCulture)),
            ("max_attempt", row => row.MaxAttempt.ToString(CultureInfo.InvariantCulture)),
            ("description", row => row.Description),
            ("reasons", row => string.IsNullOrWhiteSpace(row.Reasons) ? "-" : row.Reasons));
    }

    private static void AppendGeneratorSection(StringBuilder builder, IReadOnlyList<GeneratorAnalysisRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Generator Balance");
        AppendTable(
            builder,
            rows.Take(8),
            ("generator", row => row.GeneratorId),
            ("fires", row => row.FiredCount.ToString(CultureInfo.InvariantCulture)),
            ("eligible", row => row.EligiblePulses.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenEligible)),
            ("blocker", row => row.PrimaryBlocker));
    }

    private static void AppendPlannerSection(StringBuilder builder, IReadOnlyList<PlannerPressureRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Planner Pressure");
        AppendTable(
            builder,
            rows,
            ("kind", row => row.Kind),
            ("stale", row => FormatPercent(row.StaleRate)),
            ("no_exec", row => FormatPercent(row.NoExecutionRate)),
            ("avg_candidates", row => row.AverageCandidateCount.ToString("F2", CultureInfo.InvariantCulture)),
            ("top_process", row => row.TopExecutedProcess),
            ("top_share", row => FormatPercent(row.TopProcessShareOfExecutions)));
    }

    private static void AppendProcessSection(StringBuilder builder, IReadOnlyList<ProcessPressureRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Process Pressure");
        AppendTable(
            builder,
            rows.Take(12),
            ("kind", row => row.Kind),
            ("process", row => row.ProcessName),
            ("selected", row => row.SelectedCount.ToString(CultureInfo.InvariantCulture)),
            ("offered", row => row.CandidateEvaluations.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenOffered)),
            ("precond", row => FormatPercent(row.PreconditionPassRate)),
            ("top_failure", row => string.IsNullOrWhiteSpace(row.TopFailureDiagnostic) ? "-" : row.TopFailureDiagnostic),
            ("source", row => row.EvidenceSource));
    }

    private static void AppendAttributeSection(StringBuilder builder, IReadOnlyList<AttributeBandSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Attribute Bands");
        AppendTable(
            builder,
            rows
                .OrderByDescending(row => row.CriticalShare)
                .ThenByDescending(row => row.LowShare)
                .Take(12),
            ("kind", row => row.Kind),
            ("subtype", row => row.Subtype),
            ("attribute", row => row.AttributeName),
            ("critical", row => FormatPercent(row.CriticalShare)),
            ("low", row => FormatPercent(row.LowShare)),
            ("mid", row => FormatPercent(row.MidShare)),
            ("high", row => FormatPercent(row.HighShare)));
    }

    private static void AppendMapSpreadSection(StringBuilder builder, IReadOnlyList<MapSpreadSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Map Spread");
        AppendTable(
            builder,
            rows.Take(10),
            ("run", row => row.RunId),
            ("occupied", row => row.OccupiedCells.ToString(CultureInfo.InvariantCulture)),
            ("share", row => FormatPercent(row.OccupiedCellShare)),
            ("bbox_fill", row => FormatPercent(row.OccupancyWithinBoundingBox)),
            ("mean_seed_dist", row => row.MeanDistanceFromSeed.ToString("F2", CultureInfo.InvariantCulture)),
            ("anisotropy", row => row.Anisotropy.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static void AppendLifecycleSection(StringBuilder builder, IReadOnlyList<LifecycleSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Lifecycle");
        AppendTable(
            builder,
            rows,
            ("kind", row => row.Kind),
            ("deaths", row => row.Deaths.ToString(CultureInfo.InvariantCulture)),
            ("deaths/run", row => row.DeathsPerRun.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_death_tick", row => row.AverageDeathTick.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_active", row => row.AverageFinalActive.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_health", row => row.AverageFinalHealth.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static void AppendResourceSection(StringBuilder builder, IReadOnlyList<ResourcePressureRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Resource Pressure");
        AppendTable(
            builder,
            rows,
            ("resource", row => row.ResourceType),
            ("initial_total", row => row.AverageInitialTotal.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_total", row => row.AverageTotalOverTime.ToString("F2", CultureInfo.InvariantCulture)),
            ("final_total", row => row.AverageFinalTotal.ToString("F2", CultureInfo.InvariantCulture)),
            ("final/initial", row => FormatPercent(row.FinalToInitialRatio)),
            ("lowest_cell", row => row.LowestCellStock.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static void AppendTable<T>(
        StringBuilder builder,
        IEnumerable<T> rows,
        params (string Header, Func<T, string> Cell)[] columns)
    {
        builder.Append('|');
        builder.AppendJoin('|', columns.Select(column => column.Header));
        builder.AppendLine("|");
        builder.Append('|');
        builder.AppendJoin('|', columns.Select(_ => "---"));
        builder.AppendLine("|");

        foreach (var row in rows)
        {
            builder.Append('|');
            builder.AppendJoin('|', columns.Select(column => EscapeMarkdownCell(column.Cell(row))));
            builder.AppendLine("|");
        }
    }

    private static string EscapeMarkdownCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatPercent(double value) =>
        value.ToString("P1", CultureInfo.InvariantCulture);

}
