namespace MonteCarloTool;

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

        try
        {
            var options = MonteCarloOptions.Parse(args);
            var domain = IceRemembersRuntime.CreateDomainConfig();
            var analyzer = new MonteCarloBatchAnalyzer(domain);
            return RunCommand(args, options, analyzer);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static int RunCommand(string[] args, MonteCarloOptions options, MonteCarloBatchAnalyzer analyzer)
    {
        using var registry = new MonteCarloRunRegistry(options.ArtifactRoot);
        switch (options.Command)
        {
            case MonteCarloCommandKind.Run:
                return RunRegisteredBatch(args, options, analyzer, registry);
            case MonteCarloCommandKind.Register:
                RegisterOnly(args, options, registry);
                return 0;
            case MonteCarloCommandKind.Analyze:
                AnalyzeRegistered(options, analyzer, registry);
                return 0;
            case MonteCarloCommandKind.AppendStats:
                registry.AppendManualStats(options.RunId!, options.Stats);
                Console.WriteLine($"updated Monte Carlo stats: {options.RunId}");
                return 0;
            case MonteCarloCommandKind.List:
                PrintRunList(registry);
                return 0;
            case MonteCarloCommandKind.Show:
                Console.WriteLine(registry.ShowManifest(options.RunId!));
                return 0;
            case MonteCarloCommandKind.DeleteRun:
                registry.DeleteRun(options.RunId!);
                Console.WriteLine($"deleted Monte Carlo run: {options.RunId}");
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), $"Unsupported Monte Carlo command {options.Command}.");
        }
    }

    private static int RunRegisteredBatch(
        string[] args,
        MonteCarloOptions options,
        MonteCarloBatchAnalyzer analyzer,
        MonteCarloRunRegistry registry)
    {
        var provenance = MonteCarloProvenance.Capture(args);
        MonteCarloProvenance.ThrowIfBuildIsStale(provenance);
        registry.Register(options, provenance, status: "running");
        try
        {
            var report = RunBatch(options, analyzer);
            registry.MarkCompleted(options.RunId!, StandardStats(report));
            return 0;
        }
        catch
        {
            registry.MarkFailed(options.RunId!);
            throw;
        }
    }

    private static void RegisterOnly(string[] args, MonteCarloOptions options, MonteCarloRunRegistry registry)
    {
        var provenance = MonteCarloProvenance.Capture(args);
        registry.Register(options, provenance, status: "registered");
        Console.WriteLine($"registered Monte Carlo run: {options.RunId}");
        Console.WriteLine($"artifact directory: {Path.GetFullPath(options.OutputDirectory)}");
    }

    private static void AnalyzeRegistered(
        MonteCarloOptions options,
        MonteCarloBatchAnalyzer analyzer,
        MonteCarloRunRegistry registry)
    {
        var report = analyzer.AnalyzeToFiles(options.OutputDirectory);
        registry.MarkAnalyzed(options.RunId!, StandardStats(report));
        Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "analysis"))}");
    }

    private static void PrintRunList(MonteCarloRunRegistry registry)
    {
        foreach (var run in registry.ListRuns())
        {
            Console.WriteLine($"{run.RunId}\t{run.Status}\t{FormatUnixMilliseconds(run.RegisteredUtcMilliseconds)}\t{run.ArtifactPath}");
        }
    }

    private static MonteCarloAnalysisReport? RunBatch(MonteCarloOptions options, MonteCarloBatchAnalyzer analyzer)
    {
        ResetOutputDirectory(options.OutputDirectory);
        using var stateStore = new DuckDbTelemetryStore(options.OutputDirectory);

        ExecuteRuns(options, stateStore);
        stateStore.ExportParquet();
        Console.WriteLine($"wrote Monte Carlo telemetry: {Path.GetFullPath(options.OutputDirectory)}");

        if (!options.ShouldAnalyze)
        {
            return null;
        }

        var report = analyzer.AnalyzeToFiles(options.OutputDirectory);
        Console.WriteLine($"wrote Monte Carlo analysis: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "analysis"))}");
        return report;
    }

    private static void ExecuteRuns(MonteCarloOptions options, DuckDbTelemetryStore stateStore)
    {
        for (var runIndex = 0; runIndex < options.Runs; runIndex++)
        {
            var seed = options.Seed + runIndex;
            var runId = $"seed-{seed}";
            using var run = MonteCarloKernelRunFactory.Create(seed, options.TraceEnabled, options.ConfigFilePath);
            var result = ExecuteKernelRun(run, options, runId, seed, stateStore);
            PrintRunSummary(options, runIndex, seed, result);
        }
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

    private static MonteCarloRunExecutionResult ExecuteKernelRun(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        string runId,
        int seed,
        DuckDbTelemetryStore stateStore)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        using var summaryRecorder = new MonteCarloRunSummaryRecorder(run);
        using var stateRecorder = CreateStateRecorder(run, options, runId, seed);

        var processedTicks = AdvanceTicks(run, options, summaryRecorder, stateRecorder);
        var summary = summaryRecorder.Snapshot();
        var stateRowsWritten = FlushStateTelemetry(stateRecorder, stateStore);

        return new MonteCarloRunExecutionResult(
            stateRowsWritten,
            processedTicks,
            summary);
    }

    private static KernelTelemetryRecorder CreateStateRecorder(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        string runId,
        int seed)
    {
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

    private static int AdvanceTicks(
        MonteCarloKernelRun run,
        MonteCarloOptions options,
        MonteCarloRunSummaryRecorder summaryRecorder,
        KernelTelemetryRecorder stateRecorder)
    {
        var processedTicks = 0;
        for (var tickIndex = 0; tickIndex < options.Ticks; tickIndex++)
        {
            var advanced = run.Host.Time.AdvanceSingleStep(1);
            if (advanced == 0)
            {
                break;
            }

            processedTicks += advanced;
            var currentTick = run.Host.CurrentTick;
            summaryRecorder.DrainAvailable();
            RecordStateSnapshots(stateRecorder, currentTick, options);
        }

        return processedTicks;
    }

    private static long FlushStateTelemetry(KernelTelemetryRecorder recorder, DuckDbTelemetryStore store)
    {
        recorder.DrainAvailable();
        var rowsWritten = CountRows(recorder.Batch);
        store.Append(recorder.Batch);
        return rowsWritten;
    }

    private static void RecordStateSnapshots(
        KernelTelemetryRecorder recorder,
        int tick,
        MonteCarloOptions options)
    {
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
        batch.TraceEvents.Count +
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
        batch.CellCultureInfluenceEvents.Count +
        batch.CultureSlotOccupancyEvents.Count +
        batch.CellRoadEvents.Count +
        batch.MapSnapshots.Count +
        batch.CoverageSnapshots.Count +
        batch.CoverageTerrainSnapshots.Count +
        batch.PlacementEvents.Count;

    private static List<MonteCarloStatInput> StandardStats(MonteCarloAnalysisReport? report)
    {
        if (report is null)
        {
            return [];
        }

        var stats = new List<MonteCarloStatInput>
        {
            Stat("run_count", report.Overview.RunCount),
            Stat("max_tick", report.Overview.MaxTick),
            Stat("generator_fires_total", report.Overview.TotalGeneratorFires),
            Stat("generator_fires_average_per_run", report.Overview.AverageFiresPerRun),
            Stat("final_active_entities_average", report.Overview.AverageFinalActiveEntities),
            Stat("final_historical_entities_average", report.Overview.AverageFinalHistoricalEntities),
            Stat("final_physical_spread_average", report.Overview.AverageFinalPhysicalSpread),
            Stat("final_semantic_spread_average", report.Overview.AverageFinalSemanticSpread),
            Stat("gambit_attempts_total", report.GambitSuccess.Sum(row => row.AttemptCount)),
            Stat("gambit_successes_total", report.GambitSuccess.Sum(row => row.SuccessCount)),
            Stat("low_altitude_offered_total", report.LowAltitudeVerbUtilization.Sum(row => row.OfferedCount)),
            Stat("low_altitude_selected_total", report.LowAltitudeVerbUtilization.Sum(row => row.SelectedCount)),
        };

        foreach (var row in report.GeneratorBalance)
        {
            stats.Add(Stat($"generator.{row.GeneratorId}.fires", row.FiredCount));
        }

        foreach (var row in report.GambitSuccess)
        {
            stats.Add(Stat($"gambit.{row.GambitId}.attempts", row.AttemptCount));
            stats.Add(Stat($"gambit.{row.GambitId}.successes", row.SuccessCount));
        }

        return stats;
    }

    private static MonteCarloStatInput Stat(string name, int value) =>
        new(name, value.ToString(CultureInfo.InvariantCulture));

    private static MonteCarloStatInput Stat(string name, double value) =>
        new(name, value.ToString("G17", CultureInfo.InvariantCulture));

    private static string FormatUnixMilliseconds(long utcMilliseconds) =>
        DateTimeOffset
            .FromUnixTimeMilliseconds(utcMilliseconds)
            .UtcDateTime
            .ToString("O", CultureInfo.InvariantCulture);

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
    }
}
