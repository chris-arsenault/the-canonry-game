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
        AppendMapSpreadSection(builder, report.MapSpread);
        AppendRoamingSection(builder, report.RoamingSummary);
        AppendLowAltitudeVerbSection(builder, report.LowAltitudeVerbUtilization);
        AppendInfluenceSection(builder, report.InfluenceSummary);
        AppendMapGenerationSection(builder, report.MapGeneration);
        AppendGambitSection(builder, report.GambitSuccess);
        AppendGeneratorSection(builder, report.GeneratorBalance);
        AppendActionSection(builder, report.ActionUtilization);
        AppendAttributeSection(builder, report.AttributeBands);
        AppendLifecycleSection(builder, report.Lifecycle);
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
        builder.AppendLine("## Background Generator Balance");
        AppendTable(
            builder,
            rows.Take(8),
            ("generator", row => row.GeneratorId),
            ("fires", row => row.FiredCount.ToString(CultureInfo.InvariantCulture)),
            ("eligible", row => row.EligiblePulses.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenEligible)),
            ("blocker", row => row.PrimaryBlocker));
    }

    private static void AppendGambitSection(StringBuilder builder, IReadOnlyList<GambitSuccessRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Gambit Success Rate");
        AppendTable(
            builder,
            rows.Take(8),
            ("gambit", row => row.GambitId),
            ("attempts", row => row.AttemptCount.ToString(CultureInfo.InvariantCulture)),
            ("successes", row => row.SuccessCount.ToString(CultureInfo.InvariantCulture)),
            ("success_rate", row => FormatPercent(row.SuccessRate)),
            ("ticks", row => string.Create(CultureInfo.InvariantCulture, $"{row.FirstAttemptTick}-{row.LastAttemptTick}")));
    }

    private static void AppendActionSection(StringBuilder builder, IReadOnlyList<ActionUtilizationRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Background Action Utilization");
        AppendTable(
            builder,
            rows.Take(12),
            ("kind", row => row.Kind),
            ("action", row => row.ActionName),
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
        builder.AppendLine("## Map Utilization");
        AppendTable(
            builder,
            rows.Take(10),
            ("run", row => row.RunId),
            ("activity_cells", row => row.ActivityCells.ToString(CultureInfo.InvariantCulture)),
            ("activity_share", row => FormatPercent(row.ActivityCellShare)),
            ("entropy", row => row.ActivityEntropy.ToString("F2", CultureInfo.InvariantCulture)),
            ("top_cell", row => FormatPercent(row.ActivityTopCellShare)),
            ("empty_radius", row => row.LargestUnoccupiedStrictHexRadius.ToString(CultureInfo.InvariantCulture)),
            ("anisotropy", row => row.ActivityAxisAnisotropy.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static void AppendRoamingSection(StringBuilder builder, IReadOnlyList<RoamingSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Roaming Variance");
        AppendTable(
            builder,
            rows,
            ("kind", row => row.Kind),
            ("plane", row => row.Plane),
            ("entities", row => row.EntityCount.ToString(CultureInfo.InvariantCulture)),
            ("movers", row => row.MoverCount.ToString(CultureInfo.InvariantCulture)),
            ("zero_move", row => FormatPercent(row.ZeroMoveShare)),
            ("avg_moves", row => row.AverageMoveCount.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_unique", row => row.AverageUniquePositions.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_radius", row => row.AverageRoamRadius.ToString("F2", CultureInfo.InvariantCulture)));
    }

    private static void AppendLowAltitudeVerbSection(StringBuilder builder, IReadOnlyList<LowAltitudeVerbSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Low-Altitude Verb Utilization");
        AppendTable(
            builder,
            rows.Take(16),
            ("kind", row => row.Kind),
            ("verb", row => row.Verb),
            ("op", row => row.Operation),
            ("target", row => row.TargetMode),
            ("plane", row => row.Plane),
            ("selected", row => row.SelectedCount.ToString(CultureInfo.InvariantCulture)),
            ("offered", row => row.OfferedCount.ToString(CultureInfo.InvariantCulture)),
            ("no_target", row => row.NoTargetCount.ToString(CultureInfo.InvariantCulture)),
            ("select_rate", row => FormatPercent(row.SelectionRateWhenOffered)));
    }

    private static void AppendInfluenceSection(StringBuilder builder, IReadOnlyList<InfluenceSummaryRow> rows)
    {
        builder.AppendLine();
        builder.AppendLine("## Influence Dynamics");
        AppendTable(
            builder,
            rows,
            ("kind", row => row.Kind),
            ("plane", row => row.Plane),
            ("entities", row => row.EntityCount.ToString(CultureInfo.InvariantCulture)),
            ("avg_cells", row => row.AverageInfluencedCells.ToString("F2", CultureInfo.InvariantCulture)),
            ("avg_delta", row => row.AverageNetInfluenceDelta.ToString("F2", CultureInfo.InvariantCulture)),
            ("mean_dist", row => row.AverageMeanAnchorDistance.ToString("F2", CultureInfo.InvariantCulture)),
            ("max_dist", row => row.AverageMaxAnchorDistance.ToString("F2", CultureInfo.InvariantCulture)));
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
