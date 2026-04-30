namespace MonteCarloTool;

using System.Globalization;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static List<AnalysisFlagRow> BuildFlags(
        List<MapSpreadSummaryRow> mapRows,
        List<LifecycleSummaryRow> lifecycleRows,
        List<MapGenerationSummaryRow> mapGenerationRows,
        TelemetryCoverage telemetryCoverage)
    {
        var flags = new List<AnalysisFlagRow>();
        AddTelemetryCoverageFlags(flags, telemetryCoverage);
        AddMapGenerationFlags(flags, mapGenerationRows);
        AddMapSpreadFlags(flags, mapRows);
        AddLifecycleFlags(flags, lifecycleRows);
        return flags;
    }

    private static void AddMapGenerationFlags(
        List<AnalysisFlagRow> flags,
        List<MapGenerationSummaryRow> mapGenerationRows)
    {
        var degenerateRows = mapGenerationRows
            .Where(row => !IsAcceptedMapGenerationRow(row))
            .ToList();
        var degenerateCount = degenerateRows.Sum(row => row.EventCount);
        if (degenerateCount > 0)
        {
            var top = degenerateRows
                .OrderByDescending(row => row.EventCount)
                .ThenBy(row => row.Phase, StringComparer.Ordinal)
                .First();
            flags.Add(new AnalysisFlagRow(
                Category: "map_generation",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Procedural map degeneracy occurred {degenerateCount} times. Top cause: {top.Phase} -> {top.Description}.")));
        }
    }

    private static bool IsAcceptedMapGenerationRow(MapGenerationSummaryRow row) =>
        string.Equals(row.Phase, "accepted", StringComparison.Ordinal) && row.RetryingEvents == 0;

    private static void AddTelemetryCoverageFlags(
        List<AnalysisFlagRow> flags,
        TelemetryCoverage telemetryCoverage)
    {
        if (telemetryCoverage.ActionExecutionRows == 0 && telemetryCoverage.PlannerCandidateRows > 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "warning",
                Message: "Planner evaluations were recorded, but action execution rows are unavailable; action pressure cannot report selected action utilization."));
        }
        else if (telemetryCoverage.ActionExecutionRows > 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "info",
                Message: "Action execution telemetry is available as background context; current low-altitude tuning should use low-altitude verb utilization."));
        }

        if (telemetryCoverage.MapSnapshotRows == 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "warning",
                Message: "Map snapshots are unavailable; terrain occupancy and physical occupied-share denominators are incomplete."));
        }

        if (telemetryCoverage.LocalityOpportunityRows == 0 && telemetryCoverage.PlannerTraceRows > 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "info",
                Message: "Locality opportunity rows are unavailable; locality analysis is limited to selected planner traces and action locality rollups."));
        }

        if (telemetryCoverage.LowAltitudeTraceRows == 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "warning",
                Message: "Low-altitude trace rows are unavailable; verb utilization cannot be judged. Run Monte Carlo with --trace for the active tuning pass."));
        }

        if (telemetryCoverage.CellInfluenceRows == 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "telemetry",
                Severity: "warning",
                Message: "Cell influence rows are unavailable; settlement/faction influence shape cannot be judged."));
        }
    }

    private static void AddMapSpreadFlags(List<AnalysisFlagRow> flags, List<MapSpreadSummaryRow> mapRows)
    {
        var averageActivityShare = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.ActivityCellShare);
        var averageAnisotropy = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.ActivityAxisAnisotropy);
        var averageEmptyRadius = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.LargestUnoccupiedStrictHexRadius);
        if (averageActivityShare > 0d && averageActivityShare <= 0.15d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Physical activity cell share averages {averageActivityShare:P1}; expansion is still sparse.")));
        }

        if (averageAnisotropy >= 3.5d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Physical activity anisotropy averages {averageAnisotropy:F2}; the map is still corridor-biased.")));
        }

        if (averageEmptyRadius >= 8d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Largest strict empty physical radius averages {averageEmptyRadius:F1}; large map regions remain unused.")));
        }
    }

    private static void AddLifecycleFlags(List<AnalysisFlagRow> flags, List<LifecycleSummaryRow> lifecycleRows)
    {
        var totalDeaths = lifecycleRows.Sum(row => row.Deaths);
        if (totalDeaths == 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "lifecycle",
                Severity: "warning",
                Message: "No entities died across the analyzed batch."));
        }
        else
        {
            var noDeathKinds = lifecycleRows
                .Where(row => row.Deaths == 0)
                .Select(row => row.Kind)
                .Order(StringComparer.Ordinal)
                .ToList();
            if (noDeathKinds.Count > 0)
            {
                flags.Add(new AnalysisFlagRow(
                    Category: "lifecycle",
                    Severity: "info",
                    Message: $"Kinds with zero recorded deaths: {string.Join(", ", noDeathKinds)}."));
            }
        }
    }

}
