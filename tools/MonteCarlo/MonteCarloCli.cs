namespace MonteCarloTool;

using System.Diagnostics;
using System.Globalization;

using Engine.Domain.IceRemembers;
using Engine.Observability;
using Engine.Observability.Export;

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
                MonteCarloKernelRun? run = null;
                try
                {
                    run = MonteCarloKernelRunFactory.Create(domain, seed);
                    var result = ExecuteKernelRun(run, options, runId, seed, stateStore, performanceStore);
                    stateRowsWritten += result.StateRowsWritten;
                    performanceRowsWritten += result.PerformanceRowsWritten;

                    var degenerateAttempts = result.Summary.MapAttempt?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
                    var droppedSuffix = result.Summary.DroppedSummaryEvents > 0
                        ? FormattableString.Invariant($" summary_events_dropped={result.Summary.DroppedSummaryEvents}")
                        : string.Empty;
                    Console.WriteLine(FormattableString.Invariant(
                        $"run {runIndex + 1}/{options.Runs}: seed={seed} ticks={result.ProcessedTicks} fires={result.Summary.GeneratorFireCount} map_degenerate={degenerateAttempts}{droppedSuffix}"));
                }
                finally
                {
                    run?.Dispose();
                }
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

    private static MonteCarloRunExecutionResult ExecuteKernelRun(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        string runId,
        int seed,
        DuckDbTelemetryStore? stateStore,
        DuckDbPerformanceTelemetryStore? performanceStore)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        var stateRowsWritten = 0L;
        var performanceRowsWritten = 0L;
        var processedTicks = 0;
        using var summaryRecorder = new MonteCarloRunSummaryRecorder(run);
        KernelTelemetryRecorder? stateRecorder = null;
        KernelPerformanceRecorder? performanceRecorder = null;

        try
        {
            if (options.CollectStateTelemetry)
            {
                stateRecorder = new KernelTelemetryRecorder(
                run.Events,
                runId,
                run.Symbols,
                seed);
                RecordMapGenerationAccepted(stateRecorder.Batch, runId, run.WorldSeed);
                RecordStateSnapshots(stateRecorder, run.Host.Kernel.CurrentTick, options);
            }

            if (options.PerformanceMode)
            {
                performanceRecorder = new KernelPerformanceRecorder(
                run.Events,
                runId,
                options.Ticks,
                    seed,
                    tickSampleCadence: options.PerformanceSampleCadence,
                    systemSampleCadence: options.SystemPerformanceSampleCadence,
                    fullTelemetryEnabled: options.CollectStateTelemetry);
            }

            for (var tickIndex = 0; tickIndex < options.Ticks; tickIndex++)
            {
                var stopwatch = Stopwatch.StartNew();
                var advanced = run.Host.Time.AdvanceSingleStep(1);
                stopwatch.Stop();
                if (advanced == 0)
                {
                    break;
                }

                processedTicks += advanced;
                var currentTick = run.Host.Kernel.CurrentTick;
                summaryRecorder.DrainAvailable();
                RecordStateSnapshots(stateRecorder, currentTick, options);
                performanceRecorder?.RecordTick(currentTick, stopwatch.Elapsed);
            }

            var summary = summaryRecorder.Snapshot();
            if (performanceRecorder is not null && performanceStore is not null)
            {
                var performanceBatch = performanceRecorder.Complete(processedTicks);
                performanceRowsWritten = CountRows(performanceBatch);
                performanceStore.Append(performanceBatch);
            }

            if (stateRecorder is not null && stateStore is not null)
            {
                stateRecorder.DrainAvailable();
                stateRowsWritten = CountRows(stateRecorder.Batch);
                stateStore.Append(stateRecorder.Batch);
            }

            return new MonteCarloRunExecutionResult(
                stateRowsWritten,
                performanceRowsWritten,
                processedTicks,
                summary);
        }
        finally
        {
            performanceRecorder?.Dispose();
            stateRecorder?.Dispose();
        }
    }

    private static void RecordStateSnapshots(
        KernelTelemetryRecorder? recorder,
        int tick,
        MonteCarloOptions options)
    {
        if (recorder is null)
        {
            return;
        }

        recorder.RecordSnapshots(tick, IncludeEntitySnapshot(options, tick));
    }

    private static bool IncludeEntitySnapshot(MonteCarloOptions options, int tick) =>
        !options.DisableEntitySnapshots &&
        options.EntitySnapshotCadence > 0 &&
        tick % options.EntitySnapshotCadence == 0;

    private static void RecordMapGenerationAccepted(
        TelemetryBatch batch,
        string runId,
        MonteCarloWorldSeed? worldSeed)
    {
        if (worldSeed is not { } seed)
        {
            return;
        }

        batch.MapGenerationEvents.Add(new MapGenerationEventRow(
            runId,
            Tick: 0,
            seed.BaseSeed,
            seed.Attempt,
            seed.TerrainSeed,
            seed.EntitySeed,
            Phase: "accepted",
            Description: "accepted_world_seed",
            Reasons: string.Empty,
            WillRetry: false));
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
        batch.PlannerTraces.Count +
        batch.PlannerTraceGoals.Count +
        batch.PlannerTraceScores.Count +
        batch.PlannerTraceSteps.Count +
        batch.ProcessEvents.Count +
        batch.ProcessCandidates.Count +
        batch.LocalityOpportunities.Count +
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
