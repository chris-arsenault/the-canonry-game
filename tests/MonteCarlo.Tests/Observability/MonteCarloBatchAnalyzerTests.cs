namespace MonteCarlo.Tests.Observability;

using Engine.Constants;
using Engine.Domain.Config;
using Engine.Eventing;
using Engine.Observability;
using Engine.Observability.Export;
using Engine.Projection;

using IceRemembers;
using IceRemembers.Seed;

using MonteCarloTool;

public sealed class MonteCarloBatchAnalyzerTests : IDisposable
{
    private readonly List<string> _directoriesToDelete = [];

    [Fact]
    public void AnalyzeToFiles_WritesSummaryArtifactsForSyntheticBatch()
    {
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: true);

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.AnalyzeToFiles(batchDirectory);

        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "penguin_calls_the_unhuddled" &&
            row.FiredCount == 1);
        Assert.Contains(report.ProcessPressure, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.SelectedCount == 1);
        Assert.Contains(report.MapGeneration, row =>
            row.Phase == "quality_validation" &&
            row.EventCount == 1);

        var analysisDirectory = Path.Combine(batchDirectory, "analysis");
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "summary.md")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "generator_balance.csv")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "attribute_bands.csv")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "map_generation.csv")));

        var summary = File.ReadAllText(Path.Combine(analysisDirectory, "summary.md"));
        Assert.Contains("Monte Carlo Tuning Summary", summary, StringComparison.Ordinal);
        Assert.Contains("Generator Balance", summary, StringComparison.Ordinal);
        Assert.Contains("Process Pressure", summary, StringComparison.Ordinal);
        Assert.Contains("Map Generation", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_IncludesConfiguredGeneratorsEvenWhenTheyNeverFire()
    {
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: false);

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.Analyze(batchDirectory);

        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "penguin_calls_the_unhuddled" &&
            row.Pulses == 0 &&
            row.FiredCount == 0);
        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "faction_seals_the_edge" &&
            row.FiredCount == 0);
    }

    [Fact]
    public void Analyze_UsesProcessExecutionEventsForProcessPressure()
    {
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: true);
        batch.ProcessCandidates.Clear();

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.Analyze(batchDirectory);
        var processName = domain.Processes.First(process => process.TargetKind == IceRemembersKinds.Penguin).Name;

        Assert.Contains(report.ProcessPressure, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.ProcessName == processName &&
            row.SelectedCount == 1 &&
            row.CandidateEvaluations == 0 &&
            row.EvidenceSource == "process_events_execution");
        Assert.Contains(report.PlannerPressure, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.Evaluations == 1 &&
            row.TopExecutedProcess == processName);
        Assert.Contains(report.Flags, row =>
            row.Category == "telemetry" &&
            row.Message.Contains("process execution events", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_DoesNotFlagAcceptedMapGenerationEventsAsDegenerate()
    {
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: true);
        var run = batch.Runs.Single();
        var phase = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationPhase, "accepted", "accepted");
        var description = TelemetrySymbolIds.Create(
            TelemetrySymbolKinds.MapGenerationDescription,
            "accepted_world_seed",
            "accepted_world_seed");
        var reason = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationReason, "none", "none");
        batch.Symbols.Add(phase);
        batch.Symbols.Add(description);
        batch.Symbols.Add(reason);
        batch.MapGenerationEvents.Clear();
        batch.MapGenerationEvents.Add(new MapGenerationEventRow(
            run.RunId,
            0,
            run.Seed ?? 1000,
            0,
            111,
            222,
            phase.Id,
            description.Id,
            reason.Id,
            WillRetry: false));

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.Analyze(batchDirectory);

        Assert.Contains(report.MapGeneration, row =>
            row.Phase == "accepted" &&
            row.Description == "accepted_world_seed" &&
            row.EventCount == 1);
        Assert.DoesNotContain(report.Flags, row => row.Category == "map_generation");
    }

    [Fact]
    public void Cli_PerfModeWritesLeanPerformanceTelemetryByDefault()
    {
        var batchDirectory = CreateTempDirectory();

        var exitCode = MonteCarloCli.Run([
            "--runs", "1",
            "--ticks", "3",
            "--seed", "7200",
            "--perf",
            "--out", batchDirectory,
        ]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(batchDirectory, "run_perf.parquet")));
        Assert.True(File.Exists(Path.Combine(batchDirectory, "tick_perf.parquet")));
        Assert.True(File.Exists(Path.Combine(batchDirectory, "system_perf.parquet")));
        Assert.True(File.Exists(Path.Combine(batchDirectory, "export_perf.parquet")));
        Assert.True(File.Exists(Path.Combine(batchDirectory, "performance_symbols.parquet")));
        Assert.False(File.Exists(Path.Combine(batchDirectory, "tick_snapshots.parquet")));
        Assert.False(Directory.Exists(Path.Combine(batchDirectory, "analysis")));
    }

    [Fact]
    public void Options_ParseTraceFlagEnablesTrace()
    {
        var options = MonteCarloOptions.Parse([
            "--trace",
        ]);

        Assert.True(options.TraceEnabled);
    }

    [Fact]
    public void Options_TraceKeepsStateTelemetryEnabledInPerfMode()
    {
        var options = MonteCarloOptions.Parse([
            "--perf",
            "--trace",
        ]);

        Assert.True(options.TraceEnabled);
        Assert.True(options.CollectStateTelemetry);
    }

    public void Dispose()
    {
        foreach (var directory in _directoriesToDelete)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "canonry-monte-carlo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _directoriesToDelete.Add(directory);
        return directory;
    }

    private static TelemetryBatch CreateBatch(DomainConfig domain, bool includeGeneratorFire)
    {
        var seed = CreateBatchSeed(domain);
        var batch = new TelemetryBatch();

        AddSymbols(batch, seed);
        AddRunAndSnapshotRows(batch, seed, includeGeneratorFire);
        AddEntityTelemetry(batch, seed);
        AddProcessTelemetry(batch, seed);
        AddResourceTelemetry(batch, seed);
        AddMapTelemetry(batch, seed);
        AddLifecycleTelemetry(batch, seed);
        AddGeneratorTelemetry(batch, seed, includeGeneratorFire);

        return batch;
    }

    private static SyntheticBatchSeed CreateBatchSeed(DomainConfig domain)
    {
        var penguinConfig = domain.Kinds[IceRemembersKinds.Penguin];
        var settlementConfig = domain.Kinds[IceRemembersKinds.Settlement];
        var penguinProcess = domain.Processes.First(process => process.TargetKind == IceRemembersKinds.Penguin);

        return new SyntheticBatchSeed(
            RunId: 1000,
            DomainId: 1,
            PenguinKindId: 10,
            SettlementKindId: 11,
            FactionKindId: 12,
            ArtifactKindId: 13,
            WardenSubtypeId: 20,
            SanctuarySubtypeId: 21,
            CompactSubtypeId: 22,
            CultureId: 30,
            ProcessId: 40,
            GeneratorId: 50,
            FoodResourceId: 60,
            WealthResourceId: 61,
            ShelfTerrainId: 70,
            OceanTerrainId: 71,
            PenguinProcessId: penguinProcess.Id,
            PenguinProcessName: penguinProcess.Name,
            PenguinAttributeIds: AttributeIds(penguinConfig, firstId: 100),
            SettlementAttributeIds: AttributeIds(settlementConfig, firstId: 200));
    }

    private static void AddSymbols(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Run, seed.RunId, "seed-1000", "seed-1000"));
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Domain, seed.DomainId, "IceRemembers", "IceRemembers"));
        TelemetrySymbolIds.AddFixedCodeSymbols(batch);
        AddSymbol(batch, ProjectionSymbolKind.Kind, seed.PenguinKindId, IceRemembersKinds.Penguin.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, seed.SettlementKindId, IceRemembersKinds.Settlement.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, seed.FactionKindId, IceRemembersKinds.Faction.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, seed.ArtifactKindId, IceRemembersKinds.Artifact.Value);
        AddSymbol(batch, ProjectionSymbolKind.Subtype, seed.WardenSubtypeId, "warden");
        AddSymbol(batch, ProjectionSymbolKind.Subtype, seed.SanctuarySubtypeId, "sanctuary");
        AddSymbol(batch, ProjectionSymbolKind.Subtype, seed.CompactSubtypeId, "compact");
        AddSymbol(batch, ProjectionSymbolKind.Culture, seed.CultureId, IceRemembersCulture.Nightshelf.ToDomainName());
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(
            (int)ProjectionSymbolKind.Process,
            seed.ProcessId,
            seed.PenguinProcessId,
            seed.PenguinProcessName));
        AddSymbol(batch, ProjectionSymbolKind.Generator, seed.GeneratorId, "penguin_calls_the_unhuddled");
        AddSymbol(batch, ProjectionSymbolKind.Generator, 51, "faction_seals_the_edge");
        AddSymbol(batch, ProjectionSymbolKind.Resource, seed.FoodResourceId, "Food");
        AddSymbol(batch, ProjectionSymbolKind.Resource, seed.WealthResourceId, "Wealth");
        AddSymbol(batch, ProjectionSymbolKind.Terrain, seed.ShelfTerrainId, "Shelf");
        AddSymbol(batch, ProjectionSymbolKind.Terrain, seed.OceanTerrainId, "Ocean");
        AddAttributeSymbols(batch, seed.PenguinAttributeIds);
        AddAttributeSymbols(batch, seed.SettlementAttributeIds);
    }

    private static void AddRunAndSnapshotRows(TelemetryBatch batch, SyntheticBatchSeed seed, bool includeGeneratorFire)
    {
        batch.Runs.Add(new RunRow(seed.RunId, 1000, seed.DomainId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        batch.TickSnapshots.Add(new TickSnapshotRow(seed.RunId, 0, 2, 0, 0, 1, 2, 20, 40, 1.5f, 1.0f));
        batch.TickSnapshots.Add(new TickSnapshotRow(seed.RunId, 25, 2, 1, includeGeneratorFire ? 1 : 0, 3, 3, 28, 32, 4.2f, 2.2f));
        batch.KindSnapshots.Add(new KindSnapshotRow(seed.RunId, 25, seed.PenguinKindId, seed.WardenSubtypeId, 1, 0, 92f, 92f, 92f));
        batch.KindSnapshots.Add(new KindSnapshotRow(seed.RunId, 25, seed.SettlementKindId, seed.SanctuarySubtypeId, 1, 0, 45f, 45f, 45f));
    }

    private static void AddEntityTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        AddEntitySnapshot(batch, seed.RunId, 0, 1, seed.PenguinKindId, seed.WardenSubtypeId, seed.CultureId, 0, 0, 0, 0, 0);
        AddEntityAttributes(batch, seed.RunId, 0, 1, seed.PenguinAttributeIds, 92f, 78f, 24f);
        AddEntitySnapshot(batch, seed.RunId, 0, 2, seed.SettlementKindId, seed.SanctuarySubtypeId, seed.CultureId, 2, 0, 1, 0, -1);
        AddEntityAttributes(batch, seed.RunId, 0, 2, seed.SettlementAttributeIds, 45f, 55f, 61f);
        AddEntitySnapshot(batch, seed.RunId, 25, 1, seed.PenguinKindId, seed.WardenSubtypeId, seed.CultureId, 4, 1, 0, 0, 0);
        AddEntityAttributes(batch, seed.RunId, 25, 1, seed.PenguinAttributeIds, 92f, 78f, 24f);
        AddEntitySnapshot(batch, seed.RunId, 25, 2, seed.SettlementKindId, seed.SanctuarySubtypeId, seed.CultureId, 6, 2, 1, 0, -1);
        AddEntityAttributes(batch, seed.RunId, 25, 2, seed.SettlementAttributeIds, 45f, 55f, 61f);
    }

    private static void AddProcessTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        AddProcessEvent(batch, seed, selectedGoalId: 1, candidateCount: 1);
        AddProcessEvent(batch, seed, selectedGoalId: null, candidateCount: 0);
        batch.ProcessCandidates.Add(new ProcessCandidateRow(
            seed.RunId,
            25,
            1,
            seed.PenguinKindId,
            seed.ProcessId,
            1,
            ProviderId: 0,
            1.25f,
            PreconditionPassed: true,
            PreconditionDiagnosticCode: 0,
            OnCooldown: false,
            WasSelected: true));
    }

    private static void AddProcessEvent(
        TelemetryBatch batch,
        SyntheticBatchSeed seed,
        int? selectedGoalId,
        int candidateCount) =>
        batch.ProcessEvents.Add(new ProcessEventRow(
            seed.RunId,
            25,
            1,
            seed.PenguinKindId,
            selectedGoalId,
            seed.ProcessId,
            IsStale: false,
            StateCode: (int)KernelEventState.Succeeded,
            candidateCount,
            VisitId: null,
            LocalityId: null,
            LocalityPlaneCode: null,
            LocalityQ: null,
            LocalityR: null,
            LocalityS: null,
            LocalityBundleScore: null,
            LocalityGoalCount: null,
            LocalityDistinctTargetCount: null,
            LocalityDistinctEffectCount: null,
            LocalityDistance: null,
            LocalityIsCurrent: null,
            LocalityIsCommitted: null,
            LocalityArrived: null,
            LocalityActionsRemaining: null,
            LocalityRealizedActions: null,
            LocalitySelectionReasonCode: null));

    private static void AddResourceTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(seed.RunId, 0, seed.FoodResourceId, 28f, 1.4f, 0.5f, 3.0f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(seed.RunId, 25, seed.FoodResourceId, 12f, 0.6f, 0.2f, 1.5f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(seed.RunId, 0, seed.WealthResourceId, 6f, 0.3f, 0f, 1f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(seed.RunId, 25, seed.WealthResourceId, 5f, 0.25f, 0f, 1f));
    }

    private static void AddMapTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 0, seed.ShelfTerrainId, 20, 8, 2));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 0, seed.OceanTerrainId, 40, 12, 0));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 25, seed.ShelfTerrainId, 20, 10, 1));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 25, seed.OceanTerrainId, 40, 18, 1));
        var phase = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationPhase, "quality_validation", "quality_validation");
        var description = TelemetrySymbolIds.Create(
            TelemetrySymbolKinds.MapGenerationDescription,
            "Rejected procedural seed after validation (1 issues).",
            "Rejected procedural seed after validation (1 issues).");
        var reason = TelemetrySymbolIds.Create(TelemetrySymbolKinds.MapGenerationReason, "coverage too small", "coverage too small");
        batch.Symbols.Add(phase);
        batch.Symbols.Add(description);
        batch.Symbols.Add(reason);
        batch.MapGenerationEvents.Add(new MapGenerationEventRow(
            seed.RunId,
            0,
            1000,
            0,
            111,
            222,
            phase.Id,
            description.Id,
            reason.Id,
            true));
    }

    private static void AddLifecycleTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.EntityNameSymbols.Add(new EntityNameSymbolRow(seed.RunId, 7, seed.ArtifactKindId, 0, 0, 7, "Broken Seal"));
        batch.LifecycleEvents.Add(new LifecycleEventRow(
            seed.RunId,
            18,
            (int)KernelEventType.EntityStatusChanged,
            7,
            seed.ArtifactKindId,
            EntityStatusCodes.Historical));
    }

    private static void AddGeneratorTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed, bool includeGeneratorFire)
    {
        if (includeGeneratorFire)
        {
            AddGeneratorEvents(batch, seed);
            AddGeneratedEntities(batch, seed);
        }
    }

    private static void AddGeneratorEvents(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.GeneratorEvents.Add(new GeneratorEventRow(
            RunId: seed.RunId,
            Tick: 25,
            EventTypeCode: (int)KernelEventType.GeneratorEvaluated,
            GeneratorId: seed.GeneratorId,
            StatusCode: GeneratorStatusCodes.Fired,
            TargetPoolCount: 2,
            InactiveRejectedCount: 0,
            SubtypeRejectedCount: 0,
            SaturationRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            WildernessRejectedCount: 0,
            TriggerRejectedCount: 0,
            CandidateCount: 1,
            FailedFireCount: 0,
            SelectedTargetEntityId: 1,
            CreatedEntityCount: 0,
            CreatedRelationshipCount: 0));
        batch.GeneratorEvents.Add(new GeneratorEventRow(
            RunId: seed.RunId,
            Tick: 25,
            EventTypeCode: (int)KernelEventType.GeneratorFired,
            GeneratorId: seed.GeneratorId,
            StatusCode: GeneratorStatusCodes.Fired,
            TargetPoolCount: 0,
            InactiveRejectedCount: 0,
            SubtypeRejectedCount: 0,
            SaturationRejectedCount: 0,
            SecondaryTargetRejectedCount: 0,
            WildernessRejectedCount: 0,
            TriggerRejectedCount: 0,
            CandidateCount: 0,
            FailedFireCount: 0,
            SelectedTargetEntityId: 1,
            CreatedEntityCount: 2,
            CreatedRelationshipCount: 4));
    }

    private static void AddGeneratedEntities(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.GeneratedEntities.Add(new GeneratedEntityRow(
            seed.RunId,
            25,
            seed.GeneratorId,
            3,
            seed.SettlementKindId,
            seed.SanctuarySubtypeId,
            seed.CultureId,
            6,
            2,
            1,
            0,
            -1));
        batch.GeneratedEntities.Add(new GeneratedEntityRow(
            seed.RunId,
            25,
            seed.GeneratorId,
            4,
            seed.FactionKindId,
            seed.CompactSubtypeId,
            seed.CultureId,
            6,
            2,
            1,
            1,
            -2));
    }

    private sealed record SyntheticBatchSeed(
        int RunId,
        int DomainId,
        int PenguinKindId,
        int SettlementKindId,
        int FactionKindId,
        int ArtifactKindId,
        int WardenSubtypeId,
        int SanctuarySubtypeId,
        int CompactSubtypeId,
        int CultureId,
        int ProcessId,
        int GeneratorId,
        int FoodResourceId,
        int WealthResourceId,
        int ShelfTerrainId,
        int OceanTerrainId,
        string PenguinProcessId,
        string PenguinProcessName,
        Dictionary<string, int> PenguinAttributeIds,
        Dictionary<string, int> SettlementAttributeIds);

    private static Dictionary<string, int> AttributeIds(KindConfig config, int firstId)
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < config.Attributes.Count; i++)
        {
            ids[config.Attributes[i].Name] = firstId + i;
        }

        return ids;
    }

    private static void AddSymbol(TelemetryBatch batch, ProjectionSymbolKind kind, int id, string stableName) =>
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId((int)kind, id, stableName, stableName));

    private static void AddAttributeSymbols(TelemetryBatch batch, IReadOnlyDictionary<string, int> attributeIds)
    {
        foreach (var (name, id) in attributeIds)
        {
            AddSymbol(batch, ProjectionSymbolKind.Attribute, id, name);
        }
    }

    private static void AddEntitySnapshot(
        TelemetryBatch batch,
        int runId,
        int tick,
        int entityId,
        int kindId,
        int subtypeId,
        int cultureId,
        int physicalQ,
        int physicalR,
        int semanticQ,
        int semanticR,
        int semanticS) =>
        batch.EntitySnapshots.Add(new EntitySnapshotRow(
            runId,
            tick,
            entityId,
            kindId,
            subtypeId,
            cultureId,
            EntityStatusCodes.Active,
            physicalQ,
            physicalR,
            semanticQ,
            semanticR,
            semanticS));

    private static void AddEntityAttributes(
        TelemetryBatch batch,
        int runId,
        int tick,
        int entityId,
        IReadOnlyDictionary<string, int> attributeIds,
        params float[] values)
    {
        var index = 0;
        foreach (var id in attributeIds.Values)
        {
            batch.EntityAttributeSnapshots.Add(new EntityAttributeSnapshotRow(
                runId,
                tick,
                entityId,
                id,
                values[index++]));
        }
    }
}
