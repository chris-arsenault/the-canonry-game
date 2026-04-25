namespace MonteCarloTool;

using System.Diagnostics;
using System.Globalization;

using Engine.Domain.IceRemembers;
using Engine.Observability;
using Engine.Simulation;

internal static class MonteCarloCli
{
    public static int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = MonteCarloOptions.Parse(args);
        var domain = IceRemembersDefinition.Create();
        var analyzer = new MonteCarloBatchAnalyzer(domain);

        if (options.AnalyzeOnly)
        {
            var batchDirectory = options.AnalyzeDirectory!;
            analyzer.AnalyzeToFiles(batchDirectory);
            Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(batchDirectory, "analysis"))}");
            return 0;
        }

        ResetOutputDirectory(options.OutputDirectory);
        DuckDbTelemetryStore? stateStore = null;
        DuckDbPerformanceTelemetryStore? performanceStore = null;
        long stateRowsWritten = 0;
        long performanceRowsWritten = 0;

        try
        {
            stateStore = options.CollectStateTelemetry
                ? new DuckDbTelemetryStore(options.OutputDirectory)
                : null;
            performanceStore = options.PerformanceMode
                ? new DuckDbPerformanceTelemetryStore(options.OutputDirectory)
                : null;

            for (var runIndex = 0; runIndex < options.Runs; runIndex++)
            {
                var seed = options.Seed + runIndex;
                var runId = $"seed-{seed}";
                var diagnosticsOptions = options.PerformanceMode && !options.CollectStateTelemetry
                    ? SimulationDiagnosticsOptions.LeanPerformance
                    : SimulationDiagnosticsOptions.Detailed;
                var sim = SimulationRunner.CreateFromDomain(domain, new Random(seed), diagnosticsOptions: diagnosticsOptions);

                SimulationTelemetryRecorder? recorder = null;
                SimulationPerformanceRecorder? performanceRecorder = null;
                try
                {
                    if (options.CollectStateTelemetry)
                    {
                        recorder = new SimulationTelemetryRecorder(
                            sim,
                            runId,
                            seed,
                            entitySnapshotCadence: options.DisableEntitySnapshots ? 0 : options.EntitySnapshotCadence);
                    }

                    if (options.PerformanceMode)
                    {
                        performanceRecorder = new SimulationPerformanceRecorder(
                            sim,
                            runId,
                            options.Ticks,
                            seed,
                            tickSampleCadence: options.PerformanceSampleCadence,
                            systemSampleCadence: options.SystemPerformanceSampleCadence,
                            fullTelemetryEnabled: options.CollectStateTelemetry);
                    }

                    for (var tick = 0; tick < options.Ticks; tick++)
                    {
                        sim.Tick();
                        recorder?.OnTickEnded();
                    }

                    if (performanceRecorder is not null && performanceStore is not null)
                    {
                        var performanceBatch = performanceRecorder.Complete();
                        performanceRowsWritten += CountRows(performanceBatch);
                        performanceStore.Append(performanceBatch);
                    }

                    if (recorder is not null && stateStore is not null)
                    {
                        stateRowsWritten += CountRows(recorder.Batch);
                        stateStore.Append(recorder.Batch);
                    }
                }
                finally
                {
                    performanceRecorder?.Dispose();
                    recorder?.Dispose();
                }

                var degenerateAttempts = sim.WorldSeed.Attempt;
                Console.WriteLine(FormattableString.Invariant(
                    $"run {runIndex + 1}/{options.Runs}: seed={seed} ticks={options.Ticks} fires={sim.GeneratorSystem.TotalFired} map_degenerate={degenerateAttempts}"));
            }

            if (stateStore is not null)
            {
                var export = TimedExport("state_telemetry_parquet", stateRowsWritten, () => stateStore.ExportParquet());
                performanceStore?.AppendExport(export);
                Console.WriteLine($"wrote Monte Carlo telemetry: {Path.GetFullPath(options.OutputDirectory)}");
            }

            if (options.ShouldAnalyze)
            {
                var export = TimedExport("analysis", rowsWritten: 0, () =>
                {
                    analyzer.AnalyzeToFiles(options.OutputDirectory);
                });
                performanceStore?.AppendExport(export);
                Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "analysis"))}");
            }

            if (performanceStore is not null)
            {
                var export = TimedExport("performance_parquet", performanceRowsWritten, () => performanceStore.ExportCoreParquet());
                performanceStore.AppendExport(export);
                performanceStore.ExportExportPerformanceParquet();
                Console.WriteLine($"wrote Monte Carlo performance telemetry: {Path.GetFullPath(options.OutputDirectory)}");
            }
        }
        finally
        {
            performanceStore?.Dispose();
            stateStore?.Dispose();
        }

        return 0;
    }

    private static ExportPerformanceRow TimedExport(string step, long rowsWritten, Action action)
    {
        var started = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        var completed = DateTime.UtcNow;
        return new ExportPerformanceRow(
            Guid.NewGuid().ToString("N"),
            step,
            started.ToString("o", CultureInfo.InvariantCulture),
            completed.ToString("o", CultureInfo.InvariantCulture),
            stopwatch.Elapsed.TotalMilliseconds,
            rowsWritten);
    }

    private static long CountRows(TelemetryBatch batch) =>
        batch.Runs.Count +
        batch.TickSnapshots.Count +
        batch.KindSnapshots.Count +
        batch.EntitySnapshots.Count +
        batch.EntityEvents.Count +
        batch.PlannerGoals.Count +
        batch.ProcessEvents.Count +
        batch.ProcessCandidates.Count +
        batch.AttributeEvents.Count +
        batch.MovementEvents.Count +
        batch.ResourceEvents.Count +
        batch.ResourceSnapshots.Count +
        batch.RelationshipEvents.Count +
        batch.RelationshipSnapshots.Count +
        batch.GeneratorEvents.Count +
        batch.GeneratedEntities.Count +
        batch.LifecycleEvents.Count +
        batch.MapGenerationEvents.Count +
        batch.MapSnapshots.Count;

    private static long CountRows(PerformanceTelemetryBatch batch) =>
        batch.RunPerformance.Count +
        batch.TickPerformance.Count +
        batch.SystemPerformance.Count +
        batch.ExportPerformance.Count;

    private static void ResetOutputDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        foreach (var path in Directory.EnumerateFiles(outputDirectory, "*.parquet", SearchOption.TopDirectoryOnly))
        {
            File.Delete(path);
        }

        var analysisDirectory = Path.Combine(outputDirectory, "analysis");
        if (Directory.Exists(analysisDirectory))
        {
            Directory.Delete(analysisDirectory, recursive: true);
        }

        var stagingDirectory = Path.Combine(outputDirectory, ".staging");
        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }

        var databasePath = Path.Combine(outputDirectory, "telemetry.duckdb");
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        var walPath = databasePath + ".wal";
        if (File.Exists(walPath))
        {
            File.Delete(walPath);
        }

        var performanceDatabasePath = Path.Combine(outputDirectory, "performance.duckdb");
        if (File.Exists(performanceDatabasePath))
        {
            File.Delete(performanceDatabasePath);
        }

        var performanceWalPath = performanceDatabasePath + ".wal";
        if (File.Exists(performanceWalPath))
        {
            File.Delete(performanceWalPath);
        }
    }
}
