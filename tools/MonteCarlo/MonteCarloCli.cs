namespace MonteCarloTool;

using System.Diagnostics;
using System.Globalization;

using Engine.Constants;
using Engine.Observability;
using Engine.Observability.Export;

using IceRemembers.Seed;

internal static class MonteCarloCli
{
    public static int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = MonteCarloOptions.Parse(args);
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var analyzer = new MonteCarloBatchAnalyzer(domain);

        if (options.AnalyzeOnly)
        {
            return RunAnalysisOnly(options, analyzer);
        }

        RunBatch(options, analyzer);
        return 0;
    }

    private static int RunAnalysisOnly(MonteCarloOptions options, MonteCarloBatchAnalyzer analyzer)
    {
        var batchDirectory = options.AnalyzeDirectory!;
        analyzer.AnalyzeToFiles(batchDirectory);
        Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(batchDirectory, "analysis"))}");
        return 0;
    }

    private static void RunBatch(MonteCarloOptions options, MonteCarloBatchAnalyzer analyzer)
    {
        ResetOutputDirectory(options.OutputDirectory);
        DuckDbTelemetryStore? stateStore = null;
        DuckDbPerformanceTelemetryStore? performanceStore = null;

        try
        {
            stateStore = options.CollectStateTelemetry
                ? new DuckDbTelemetryStore(options.OutputDirectory)
                : null;
            performanceStore = options.PerformanceMode
                ? new DuckDbPerformanceTelemetryStore(options.OutputDirectory)
                : null;

            var totals = ExecuteRuns(options, stateStore, performanceStore);
            ExportStateTelemetry(options, stateStore, performanceStore, totals.StateRowsWritten);
            ExportAnalysis(options, analyzer, performanceStore);
            ExportPerformanceTelemetry(options, performanceStore, totals.PerformanceRowsWritten);
        }
        finally
        {
            performanceStore?.Dispose();
            stateStore?.Dispose();
        }
    }

    private static (long StateRowsWritten, long PerformanceRowsWritten) ExecuteRuns(
        MonteCarloOptions options,
        DuckDbTelemetryStore? stateStore,
        DuckDbPerformanceTelemetryStore? performanceStore)
    {
        var stateRowsWritten = 0L;
        var performanceRowsWritten = 0L;
        for (var runIndex = 0; runIndex < options.Runs; runIndex++)
        {
            var seed = options.Seed + runIndex;
            var runId = $"seed-{seed}";
            using (var run = MonteCarloKernelRunFactory.Create(seed, options.TraceEnabled, options.ConfigFilePath))
            {
                var result = ExecuteKernelRun(run, options, runId, seed, stateStore, performanceStore);
                stateRowsWritten += result.StateRowsWritten;
                performanceRowsWritten += result.PerformanceRowsWritten;
                PrintRunSummary(options, runIndex, seed, result);
            }
        }

        return (stateRowsWritten, performanceRowsWritten);
    }

    private static void PrintRunSummary(
        MonteCarloOptions options,
        int runIndex,
        int seed,
        MonteCarloRunExecutionResult result)
    {
        var degenerateAttempts = result.Summary.MapAttempt?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        var droppedSuffix = result.Summary.DroppedSummaryEvents > 0
            ? string.Create(CultureInfo.InvariantCulture, $" summary_events_dropped={result.Summary.DroppedSummaryEvents}")
            : string.Empty;
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"run {runIndex + 1}/{options.Runs}: seed={seed} ticks={result.ProcessedTicks} fires={result.Summary.GeneratorFireCount} map_degenerate={degenerateAttempts}{droppedSuffix}"));
    }

    private static void ExportStateTelemetry(
        MonteCarloOptions options,
        DuckDbTelemetryStore? stateStore,
        DuckDbPerformanceTelemetryStore? performanceStore,
        long rowsWritten)
    {
        if (stateStore is null)
        {
            return;
        }

        const string step = "state_telemetry_parquet";
        var export = TimedExport(step, rowsWritten, () => stateStore.ExportParquet());
        performanceStore?.AppendExport(export, step);
        Console.WriteLine($"wrote Monte Carlo telemetry: {Path.GetFullPath(options.OutputDirectory)}");
    }

    private static void ExportAnalysis(
        MonteCarloOptions options,
        MonteCarloBatchAnalyzer analyzer,
        DuckDbPerformanceTelemetryStore? performanceStore)
    {
        if (!options.ShouldAnalyze)
        {
            return;
        }

        const string step = "analysis";
        var export = TimedExport(step, rowsWritten: 0, () => analyzer.AnalyzeToFiles(options.OutputDirectory));
        performanceStore?.AppendExport(export, step);
        Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "analysis"))}");
    }

    private static void ExportPerformanceTelemetry(
        MonteCarloOptions options,
        DuckDbPerformanceTelemetryStore? performanceStore,
        long rowsWritten)
    {
        if (performanceStore is null)
        {
            return;
        }

        const string step = "performance_parquet";
        var export = TimedExport(step, rowsWritten, () => performanceStore.ExportCoreParquet());
        performanceStore.AppendExport(export, step);
        performanceStore.ExportExportPerformanceParquet();
        Console.WriteLine($"wrote Monte Carlo performance telemetry: {Path.GetFullPath(options.OutputDirectory)}");
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
        using var summaryRecorder = new MonteCarloRunSummaryRecorder(run);
        var stateRecorder = CreateStateRecorder(run, options, runId, seed);
        var performanceRecorder = CreatePerformanceRecorder(run, options, runId, seed);

        try
        {
            var processedTicks = AdvanceTicks(run, options, summaryRecorder, stateRecorder, performanceRecorder);
            var summary = summaryRecorder.Snapshot();
            performanceRowsWritten = FlushPerformanceTelemetry(performanceRecorder, performanceStore, processedTicks);
            stateRowsWritten = FlushStateTelemetry(stateRecorder, stateStore);

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

    private static KernelTelemetryRecorder? CreateStateRecorder(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        string runId,
        int seed)
    {
        if (!options.CollectStateTelemetry)
        {
            return null;
        }

        var recorder = new KernelTelemetryRecorder(
            run.Events,
            runId,
            run.Symbols,
            seed,
            traceEnabled: options.TraceEnabled,
            entitySnapshotCadence: options.EntitySnapshotCadence,
            mapSnapshotCadence: options.EntitySnapshotCadence,
            generatorSnapshotCadence: options.EntitySnapshotCadence);
        RecordMapGenerationAccepted(recorder.Batch, recorder.RunId, run.WorldSeed);
        RecordStateSnapshots(recorder, run.Host.CurrentTick, options);
        return recorder;
    }

    private static KernelPerformanceRecorder? CreatePerformanceRecorder(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        string runId,
        int seed)
    {
        if (!options.PerformanceMode)
        {
            return null;
        }

        return new KernelPerformanceRecorder(
            run.Events,
            runId,
            options.Ticks,
            seed,
            tickSampleCadence: options.PerformanceSampleCadence,
            systemSampleCadence: options.SystemPerformanceSampleCadence,
            fullTelemetryEnabled: options.CollectStateTelemetry);
    }

    private static int AdvanceTicks(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        MonteCarloRunSummaryRecorder summaryRecorder,
        KernelTelemetryRecorder? stateRecorder,
        KernelPerformanceRecorder? performanceRecorder)
    {
        var processedTicks = 0;
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
            var currentTick = run.Host.CurrentTick;
            summaryRecorder.DrainAvailable();
            RecordStateSnapshots(stateRecorder, currentTick, options);
            performanceRecorder?.RecordTick(currentTick, stopwatch.Elapsed);
        }

        return processedTicks;
    }

    private static long FlushPerformanceTelemetry(
        KernelPerformanceRecorder? recorder,
        DuckDbPerformanceTelemetryStore? store,
        int processedTicks)
    {
        if (recorder is null || store is null)
        {
            return 0;
        }

        var batch = recorder.Complete(processedTicks);
        var rowsWritten = CountRows(batch);
        store.Append(batch);
        return rowsWritten;
    }

    private static long FlushStateTelemetry(KernelTelemetryRecorder? recorder, DuckDbTelemetryStore? store)
    {
        if (recorder is null || store is null)
        {
            return 0;
        }

        recorder.DrainAvailable();
        var rowsWritten = CountRows(recorder.Batch);
        store.Append(recorder.Batch);
        return rowsWritten;
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
        int runId,
        MonteCarloWorldSeed? worldSeed)
    {
        if (worldSeed is not { } seed)
        {
            return;
        }

        var phase = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationPhase, "accepted", "accepted");
        var description = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationDescription, "accepted_world_seed", "accepted_world_seed");
        batch.Symbols.Add(phase);
        batch.Symbols.Add(description);
        batch.MapGenerationEvents.Add(new MapGenerationEventRow(
            runId,
            Tick: 0,
            seed.BaseSeed,
            seed.Attempt,
            seed.TerrainSeed,
            seed.EntitySeed,
            phase.Id,
            description.Id,
            ReasonId: 0,
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
            started.Ticks,
            TelemetrySymbolIds.StableId(TelemetrySymbolKinds.PerformanceExportStep, step),
            new DateTimeOffset(started).ToUnixTimeMilliseconds(),
            new DateTimeOffset(completed).ToUnixTimeMilliseconds(),
            stopwatch.Elapsed.TotalMilliseconds,
            rowsWritten);
    }

    private static long CountRows(TelemetryBatch batch) =>
        batch.Symbols.Count +
        batch.EntityNameSymbols.Count +
        batch.Runs.Count +
        batch.TickSnapshots.Count +
        batch.KindSnapshots.Count +
        batch.EntitySnapshots.Count +
        batch.EntityAttributeSnapshots.Count +
        batch.EntityEvents.Count +
        batch.ExperimentVariants.Count +
        batch.PlannerGoals.Count +
        batch.PlannerTraces.Count +
        batch.PlannerTraceGoals.Count +
        batch.PlannerTraceScores.Count +
        batch.PlannerTraceSteps.Count +
        batch.PlannerCandidates.Count +
        batch.PlannerScores.Count +
        batch.PlannerSteps.Count +
        batch.PlannerLocalities.Count +
        batch.TraceEvents.Count +
        batch.ActionExecutions.Count +
        batch.LocalityOpportunities.Count +
        batch.AttributeEvents.Count +
        batch.MovementEvents.Count +
        batch.RelationshipEvents.Count +
        batch.RelationshipSnapshots.Count +
        batch.GeneratorEvents.Count +
        batch.GeneratorOutputs.Count +
        batch.GeneratorStateSnapshots.Count +
        batch.GeneratedEntities.Count +
        batch.LifecycleEvents.Count +
        batch.LifecycleReasonEvents.Count +
        batch.MapGenerationEvents.Count +
        batch.MapCellEvents.Count +
        batch.CellInfluenceEvents.Count +
        batch.MapSnapshots.Count +
        batch.CoverageSnapshots.Count +
        batch.CoverageTerrainSnapshots.Count +
        batch.PlacementEvents.Count;

    private static long CountRows(PerformanceTelemetryBatch batch) =>
        batch.Symbols.Count +
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
