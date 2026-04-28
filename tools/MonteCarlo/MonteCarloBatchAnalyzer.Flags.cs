namespace MonteCarloTool;

using System.Globalization;

internal sealed partial class MonteCarloBatchAnalyzer
{
    private static List<AnalysisFlagRow> BuildFlags(
        List<GeneratorAnalysisRow> generatorRows,
        List<PlannerPressureRow> plannerRows,
        List<MapSpreadSummaryRow> mapRows,
        List<LifecycleSummaryRow> lifecycleRows,
        List<ResourcePressureRow> resourceRows,
        List<MapGenerationSummaryRow> mapGenerationRows)
    {
        var flags = new List<AnalysisFlagRow>();
        AddMapGenerationFlags(flags, mapGenerationRows);
        AddGeneratorFlags(flags, generatorRows);
        AddPlannerFlags(flags, plannerRows);
        AddMapSpreadFlags(flags, mapRows);
        AddLifecycleFlags(flags, lifecycleRows);
        AddResourceFlags(flags, resourceRows);
        return flags;
    }

    private static void AddMapGenerationFlags(
        List<AnalysisFlagRow> flags,
        List<MapGenerationSummaryRow> mapGenerationRows)
    {
        var degenerateCount = mapGenerationRows.Sum(row => row.EventCount);
        if (degenerateCount > 0)
        {
            var top = mapGenerationRows
                .OrderByDescending(row => row.EventCount)
                .ThenBy(row => row.Phase, StringComparer.Ordinal)
                .First();
            flags.Add(new AnalysisFlagRow(
                Category: "map_generation",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Procedural map degeneracy occurred {degenerateCount} times. Top cause: {top.Phase} -> {top.Description}.")));
        }
    }

    private static void AddGeneratorFlags(List<AnalysisFlagRow> flags, List<GeneratorAnalysisRow> generatorRows)
    {
        var zeroFire = generatorRows
            .Where(row => row.Pulses > 0 && row.FiredCount == 0)
            .OrderByDescending(row => row.Pulses)
            .ThenBy(row => row.GeneratorId, StringComparer.Ordinal)
            .Take(5)
            .ToList();
        if (zeroFire.Count > 0)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "generator_balance",
                Severity: "warning",
                Message: $"Configured generators with pulse activity but zero fires: {string.Join(", ", zeroFire.Select(row => row.GeneratorId))}."));
        }

        var totalFires = generatorRows.Sum(row => row.FiredCount);
        var topGenerator = generatorRows.OrderByDescending(row => row.FiredCount).FirstOrDefault();
        if (totalFires > 0 && topGenerator is not null)
        {
            var topShare = topGenerator.FiredCount / (double)totalFires;
            if (topShare >= 0.35d)
            {
                flags.Add(new AnalysisFlagRow(
                    Category: "generator_balance",
                    Severity: "warning",
                    Message: string.Create(CultureInfo.InvariantCulture, $"{topGenerator.GeneratorId} accounts for {topShare:P1} of generator fires across the batch.")));
            }
        }
    }

    private static void AddPlannerFlags(List<AnalysisFlagRow> flags, List<PlannerPressureRow> plannerRows)
    {
        foreach (var planner in plannerRows.Where(row => row.StaleRate >= 0.25d || row.NoExecutionRate >= 0.4d))
        {
            flags.Add(new AnalysisFlagRow(
                Category: "planner",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"{planner.Kind} planner is sticky: stale={planner.StaleRate:P1}, no_execution={planner.NoExecutionRate:P1}, top_process='{planner.TopExecutedProcess}'.")));
        }
    }

    private static void AddMapSpreadFlags(List<AnalysisFlagRow> flags, List<MapSpreadSummaryRow> mapRows)
    {
        var averageOccupiedShare = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.OccupiedCellShare);
        var averageAnisotropy = mapRows.Count == 0 ? 0d : mapRows.Average(row => row.Anisotropy);
        if (averageOccupiedShare > 0d && averageOccupiedShare <= 0.15d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Final occupied physical map share averages {averageOccupiedShare:P1}; expansion is still sparse.")));
        }

        if (averageAnisotropy >= 3.5d)
        {
            flags.Add(new AnalysisFlagRow(
                Category: "map",
                Severity: "warning",
                Message: string.Create(CultureInfo.InvariantCulture, $"Final physical placement anisotropy averages {averageAnisotropy:F2}; the map is still corridor-biased.")));
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

    private static void AddResourceFlags(List<AnalysisFlagRow> flags, List<ResourcePressureRow> resourceRows)
    {
        if (resourceRows.Count > 0 && resourceRows.TrueForAll(row => row.FinalToInitialRatio >= 0.9d))
        {
            flags.Add(new AnalysisFlagRow(
                Category: "resources",
                Severity: "info",
                Message: "Global resource stocks barely deplete across the batch; carrying-capacity pressure may still be too soft."));
        }
    }
}
