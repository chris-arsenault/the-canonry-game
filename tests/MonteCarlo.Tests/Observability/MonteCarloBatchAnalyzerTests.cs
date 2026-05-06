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
        var domain = WithAnalyzerTestGenerators(IceRemembersRuntime.CreateDomainConfig());
        var penguinVerbId = domain.Gameplay!.LowAltitudeVerbs.First(verb => verb.Kind == IceRemembersKinds.Penguin.Value).Id;
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: true);

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.AnalyzeToFiles(batchDirectory);

        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "analysis_test_generator_alpha" &&
            row.FiredCount == 1);
        Assert.Contains(report.ActionUtilization, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.SelectedCount == 1);
        Assert.Contains(report.LowAltitudeVerbUtilization, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.Verb == penguinVerbId &&
            row.SelectedCount == 1);
        Assert.Contains(report.InfluenceSummary, row =>
            row.Kind == IceRemembersKinds.Settlement.Value &&
            row.AverageInfluencedCells > 0);
        Assert.Contains(report.GambitSuccess, row =>
            row.GambitId == "penguin_wields_artifact" &&
            row.AttemptCount == 2 &&
            row.SuccessCount == 1);
        Assert.Contains(report.MapGeneration, row =>
            row.Phase == "quality_validation" &&
            row.EventCount == 1);

        var analysisDirectory = Path.Combine(batchDirectory, "analysis");
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "summary.md")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "generator_balance.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "low_altitude_verb_utilization.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "low_altitude_affordance_utilization.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "entity_roaming.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "roaming_summary.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "influence_dynamics.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "road_creation.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "road_usage.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "road_reinforcement.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "gambit_success_rate.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "stock_dynamics.parquet")));
        Assert.True(File.Exists(Path.Combine(analysisDirectory, "map_spread.parquet")));
        Assert.False(File.Exists(Path.Combine(analysisDirectory, "generator_balance.csv")));

        var summary = File.ReadAllText(Path.Combine(analysisDirectory, "summary.md"));
        Assert.Contains("Monte Carlo Tuning Summary", summary, StringComparison.Ordinal);
        Assert.Contains("Map Utilization", summary, StringComparison.Ordinal);
        Assert.Contains("Roaming Variance", summary, StringComparison.Ordinal);
        Assert.Contains("Low-Altitude Verb Utilization", summary, StringComparison.Ordinal);
        Assert.Contains("Influence Dynamics", summary, StringComparison.Ordinal);
        Assert.Contains("Gambit Success Rate", summary, StringComparison.Ordinal);
        Assert.Contains("Generator Balance", summary, StringComparison.Ordinal);
        Assert.Contains("Action Utilization", summary, StringComparison.Ordinal);
        Assert.Contains("Map Generation", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_IncludesConfiguredGeneratorsEvenWhenTheyNeverFire()
    {
        var domain = WithAnalyzerTestGenerators(IceRemembersRuntime.CreateDomainConfig());
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: false);

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.Analyze(batchDirectory);

        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "analysis_test_generator_alpha" &&
            row.Pulses == 0 &&
            row.FiredCount == 0);
        Assert.Contains(report.GeneratorBalance, row =>
            row.GeneratorId == "analysis_test_generator_beta" &&
            row.FiredCount == 0);
    }

    [Fact]
    public void Analyze_UsesTraceEventsForLowAltitudeVerbUtilization()
    {
        var domain = IceRemembersRuntime.CreateDomainConfig();
        var batchDirectory = CreateTempDirectory();
        var batch = CreateBatch(domain, includeGeneratorFire: true);

        new DuckDbParquetTelemetryWriter(batchDirectory).Write(batch);

        var analyzer = new MonteCarloBatchAnalyzer(domain);
        var report = analyzer.Analyze(batchDirectory);
        var verbId = domain.Gameplay!.LowAltitudeVerbs.First(verb => verb.Kind == IceRemembersKinds.Penguin.Value).Id;

        Assert.Contains(report.ActionUtilization, row =>
            row.Kind == IceRemembersKinds.Penguin.Value &&
            row.ActionName == verbId &&
            row.SelectedCount == 1 &&
            row.CandidateEvaluations == 1 &&
            row.EvidenceSource == "trace_events");
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
    public void Options_PerfModeDisablesStateTelemetryAndAnalysisByDefault()
    {
        var options = MonteCarloOptions.Parse([
            "run",
            "--run-id", "perf-mode-test",
            "--runs", "1",
            "--ticks", "3",
            "--seed", "7200",
            "--perf",
        ]);

        Assert.Equal(1, options.Runs);
        Assert.Equal(3, options.Ticks);
        Assert.Equal(7200, options.Seed);
        Assert.True(options.PerformanceMode);
        Assert.False(options.CollectStateTelemetry);
        Assert.False(options.ShouldAnalyze);
    }

    [Fact]
    public void PerformanceTelemetryStore_WritesLeanPerformanceParquetSet()
    {
        var batchDirectory = CreateTempDirectory();

        using var store = new DuckDbPerformanceTelemetryStore(batchDirectory);
        var batch = new PerformanceTelemetryBatch();
        batch.RunPerformance.Add(new RunPerformanceRow(
            RunId: 1,
            Seed: 7200,
            DomainId: TelemetrySymbolIds.StableId(TelemetrySymbolKinds.Domain, "ice_remembers"),
            StartedUnixMilliseconds: 10,
            CompletedUnixMilliseconds: 20,
            RequestedTicks: 3,
            ProcessedTicks: 3,
            TotalSimulationMilliseconds: 1,
            AverageSampledTickMilliseconds: 1,
            P95SampledTickMilliseconds: 1,
            MaxSampledTickMilliseconds: 1,
            Gen0Collections: 0,
            Gen1Collections: 0,
            Gen2Collections: 0,
            PeakManagedMemoryBytes: 1024,
            TickSampleCadence: 1,
            SystemSampleCadence: 1,
            FullTelemetryEnabled: false));
        batch.TickPerformance.Add(new TickPerformanceRow(
            RunId: 1,
            Tick: 1,
            ElapsedMilliseconds: 1,
            ActiveEntities: 2,
            ActiveRelationships: 3,
            GeneratorEvents: 0,
            TotalMessages: 0,
            ManagedMemoryBytes: 1024));
        batch.SystemPerformance.Add(new SystemPerformanceRow(
            RunId: 1,
            Tick: 1,
            PhaseId: TelemetrySymbolIds.StableId(TelemetrySymbolKinds.PerformancePhase, "advance"),
            SystemId: TelemetrySymbolIds.StableId(TelemetrySymbolKinds.PerformanceSystem, "test_system"),
            ElapsedMilliseconds: 1));
        store.Append(batch);
        store.ExportCoreParquet();
        store.AppendExport(new ExportPerformanceRow(
            ExportId: 1,
            StepId: TelemetrySymbolIds.StableId(TelemetrySymbolKinds.PerformanceExportStep, "performance_parquet"),
            StartedUnixMilliseconds: 20,
            CompletedUnixMilliseconds: 21,
            ElapsedMilliseconds: 1,
            RowsWritten: 3), "performance_parquet");
        store.ExportExportPerformanceParquet();

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
            "run",
            "--run-id", "trace-test",
            "--trace",
        ]);

        Assert.True(options.TraceEnabled);
    }

    [Fact]
    public void Options_ConfigFileFlagSetsKernelConfigPath()
    {
        var options = MonteCarloOptions.Parse([
            "run",
            "--run-id", "config-test",
            "--config-file",
            "kernel-tuning.toml",
        ]);

        Assert.Equal("kernel-tuning.toml", options.ConfigFilePath);
    }

    [Fact]
    public void Options_TraceKeepsStateTelemetryEnabledInPerfMode()
    {
        var options = MonteCarloOptions.Parse([
            "run",
            "--run-id", "perf-trace-test",
            "--perf",
            "--trace",
        ]);

        Assert.True(options.TraceEnabled);
        Assert.True(options.CollectStateTelemetry);
    }

    [Fact]
    public void Options_RunCommandRequiresRunId()
    {
        var error = Assert.Throws<ArgumentException>(() => MonteCarloOptions.Parse([
            "run",
            "--runs", "1",
        ]));

        Assert.Contains("--run-id", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_RejectsLegacyOutPath()
    {
        var error = Assert.Throws<ArgumentException>(() => MonteCarloOptions.Parse([
            "run",
            "--run-id", "legacy-out-test",
            "--out", "somewhere",
        ]));

        Assert.Contains("--artifact-root", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_RegisterAppendStatsAndDeleteRunAreRegistryOwned()
    {
        var artifactRoot = CreateTempDirectory();
        var runId = "registry-owned-test";

        var registerExit = MonteCarloCli.Run([
            "register",
            "--run-id", runId,
            "--artifact-root", artifactRoot,
            "--runs", "1",
            "--ticks", "2",
            "--seed", "1111",
        ]);
        Assert.Equal(0, registerExit);

        var duplicateExit = MonteCarloCli.Run([
            "register",
            "--run-id", runId,
            "--artifact-root", artifactRoot,
        ]);
        Assert.Equal(2, duplicateExit);

        var appendExit = MonteCarloCli.Run([
            "append-stats",
            "--run-id", runId,
            "--artifact-root", artifactRoot,
            "semantic_road_moves=28",
            "--stat", "placement_coord_diffs=6818",
        ]);
        Assert.Equal(0, appendExit);

        var manifestPath = Path.Combine(artifactRoot, runId, "manifest.json");
        var manifest = File.ReadAllText(manifestPath);
        Assert.Contains("\"runId\": \"registry-owned-test\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"semantic_road_moves\": \"28\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"placement_coord_diffs\": \"6818\"", manifest, StringComparison.Ordinal);

        var deleteExit = MonteCarloCli.Run([
            "delete-run",
            "--run-id", runId,
            "--artifact-root", artifactRoot,
        ]);
        Assert.Equal(0, deleteExit);
        Assert.False(Directory.Exists(Path.Combine(artifactRoot, runId)));
        Assert.True(Directory.Exists(Path.Combine(artifactRoot, ".deleted")));
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
        AddLowAltitudeTelemetry(batch, seed);
        AddGambitTelemetry(batch, seed);
        AddMapTelemetry(batch, seed);
        AddLifecycleTelemetry(batch, seed);
        AddGeneratorTelemetry(batch, seed, includeGeneratorFire);

        return batch;
    }

    private static DomainConfig WithAnalyzerTestGenerators(DomainConfig config)
    {
        var semantics = config.Semantics!;
        return new DomainConfig
        {
            Kinds = config.Kinds,
            Gambits = config.Gambits,
            Relationships = config.Relationships,
            Lifecycles = config.Lifecycles,
            Terrains = config.Terrains,
            Cultures = config.Cultures,
            Gameplay = config.Gameplay,
            Semantics = new SemanticDomainConfig
            {
                Stocks = semantics.Stocks,
                Verbs = semantics.Verbs,
                Flows = semantics.Flows,
                Modifiers = semantics.Modifiers,
                Generators =
                [
                    AnalyzerTestGenerator("analysis_test_generator_alpha"),
                    AnalyzerTestGenerator("analysis_test_generator_beta"),
                ],
            },
        };
    }

    private static SemanticGeneratorConfig AnalyzerTestGenerator(string id) =>
        new()
        {
            Id = id,
            Name = id,
            Target = new SemanticGeneratorTarget { Kind = IceRemembersKinds.Penguin },
            Produces = [],
            Triggers = [],
        };

    private static SyntheticBatchSeed CreateBatchSeed(DomainConfig domain)
    {
        var penguinConfig = domain.Kinds[IceRemembersKinds.Penguin];
        var settlementConfig = domain.Kinds[IceRemembersKinds.Settlement];
        var penguinVerb = domain.Gameplay!.LowAltitudeVerbs.First(verb => verb.Kind == IceRemembersKinds.Penguin.Value);

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
            VerbSymbolId: 40,
            GeneratorId: 50,
            GambitId: 55,
            ShelfTerrainId: 70,
            OceanTerrainId: 71,
            PenguinVerbId: penguinVerb.Id,
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
            (int)ProjectionSymbolKind.Verb,
            seed.VerbSymbolId,
            seed.PenguinVerbId,
            seed.PenguinVerbId));
        AddSymbol(batch, ProjectionSymbolKind.Generator, seed.GeneratorId, "analysis_test_generator_alpha");
        AddSymbol(batch, ProjectionSymbolKind.Generator, 51, "analysis_test_generator_beta");
        AddSymbol(batch, ProjectionSymbolKind.Gambit, seed.GambitId, "penguin_wields_artifact");
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

    private static void AddLowAltitudeTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        AddLowAltitudeTraceTelemetry(batch, seed);
        AddLowAltitudeMovementTelemetry(batch, seed);
    }

    private static void AddLowAltitudeTraceTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        AddOfferedLowAltitudeTraceTelemetry(batch, seed);
        AddSelectedLowAltitudeTraceTelemetry(batch, seed);
    }

    private static void AddOfferedLowAltitudeTraceTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed) =>
        batch.TraceEvents.Add(new TraceRow(
            seed.RunId,
            20,
            IntentId: 2,
            EntityId: 1,
            TraceSubjectCodes.MovementIntent,
            CandidateIndex: 1,
            ParentCandidateIndex: ProgramOpcodeCodes.MoveActor,
            ActionFamilyCodes.LowAltitudeVerb,
            ActionId: seed.VerbSymbolId,
            EffectScopeCode: PlaneCodes.Physical,
            EffectValueId: TargetSelectorCodes.Explore,
            EffectKindId: seed.SettlementKindId,
            TargetEntityId: 0,
            TraceStatusCodes.Offered,
            AffordanceSourceKindCode: AffordanceSourceKindCodes.Generator,
            AffordanceSourceId: 5,
            AffordanceShapeCode: LowAltitudeAffordanceShapeCodes.TerrainPresence,
            AffordanceSourceKindMask: 3,
            AffordanceShapeMask: 24,
            AffordanceContributionCount: 2,
            GeneratorAffordanceCount: 1,
            GambitAffordanceCount: 1,
            HiddenAffordanceCount: 1,
            Urgency: 1f,
            RewardOrBaseScore: 1f,
            RawWeight: 1f,
            AdjustedWeight: 1f,
            AffordanceScore: 0.5f,
            AffordanceBonus: 0.25f));

    private static void AddSelectedLowAltitudeTraceTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed) =>
        batch.TraceEvents.Add(new TraceRow(
            seed.RunId,
            20,
            IntentId: 2,
            EntityId: 1,
            TraceSubjectCodes.MovementIntent,
            CandidateIndex: 1,
            ParentCandidateIndex: ProgramOpcodeCodes.MoveActor,
            ActionFamilyCodes.LowAltitudeVerb,
            ActionId: seed.VerbSymbolId,
            EffectScopeCode: PlaneCodes.Physical,
            EffectValueId: TargetSelectorCodes.Explore,
            EffectKindId: seed.SettlementKindId,
            TargetEntityId: 0,
            TraceStatusCodes.Selected,
            AffordanceSourceKindCode: AffordanceSourceKindCodes.Generator,
            AffordanceSourceId: 5,
            AffordanceShapeCode: LowAltitudeAffordanceShapeCodes.TerrainPresence,
            AffordanceSourceKindMask: 1,
            AffordanceShapeMask: 16,
            AffordanceContributionCount: 1,
            GeneratorAffordanceCount: 1,
            GambitAffordanceCount: 0,
            HiddenAffordanceCount: 0,
            Urgency: 1f,
            RewardOrBaseScore: 1f,
            RawWeight: 1f,
            AdjustedWeight: 1.2f,
            AffordanceScore: 0.5f,
            AffordanceBonus: 0.25f));

    private static void AddLowAltitudeMovementTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.MovementEvents.Add(new MovementEventRow(
            seed.RunId,
            21,
            EntityId: 1,
            PlaneCodes.Physical,
            SourceCodeId: 0,
            FromQ: 0,
            FromR: 0,
            FromS: 0,
            ToQ: 1,
            ToR: 0,
            ToS: -1,
            Distance: 1));
        batch.MovementEvents.Add(new MovementEventRow(
            seed.RunId,
            22,
            EntityId: 1,
            PlaneCodes.Physical,
            SourceCodeId: 0,
            FromQ: 1,
            FromR: 0,
            FromS: -1,
            ToQ: 2,
            ToR: 0,
            ToS: -2,
            Distance: 1));
    }

    private static void AddGambitTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.GambitEvents.Add(new GambitEventRow(
            RunId: seed.RunId,
            Tick: 24,
            EventTypeCode: (int)KernelEventType.GambitExecuted,
            GambitId: seed.GambitId,
            ActorId: 1,
            ActorKindId: seed.PenguinKindId,
            TargetEntityId: 7,
            StateCode: (int)KernelEventState.Succeeded,
            Succeeded: true,
            AffectedEntityCount: 2));
        batch.GambitEvents.Add(new GambitEventRow(
            RunId: seed.RunId,
            Tick: 25,
            EventTypeCode: (int)KernelEventType.GambitExecuted,
            GambitId: seed.GambitId,
            ActorId: 1,
            ActorKindId: seed.PenguinKindId,
            TargetEntityId: 7,
            StateCode: (int)KernelEventState.Failed,
            Succeeded: false,
            AffectedEntityCount: 2));
    }

    private static void AddMapTelemetry(TelemetryBatch batch, SyntheticBatchSeed seed)
    {
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 0, seed.ShelfTerrainId, 20, 8, 2));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 0, seed.OceanTerrainId, 40, 12, 0));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 25, seed.ShelfTerrainId, 20, 10, 1));
        batch.MapSnapshots.Add(new MapSnapshotRow(seed.RunId, 25, seed.OceanTerrainId, 40, 18, 1));
        batch.CellInfluenceEvents.Add(new CellInfluenceEventRow(seed.RunId, 12, 0, 0, 2, PlaneCodes.Physical, 2, 0, 0f, 18f, 18f));
        batch.CellInfluenceEvents.Add(new CellInfluenceEventRow(seed.RunId, 13, 0, 0, 2, PlaneCodes.Physical, 3, 0, 0f, 12f, 12f));
        batch.CellRoadEvents.Add(new CellRoadEventRow(seed.RunId, 20, 0, PlaneCodes.Physical, 1, 0, 1, 0f, 30f, 30f));
        batch.CellRoadEvents.Add(new CellRoadEventRow(seed.RunId, 22, 0, PlaneCodes.Physical, 1, 0, 2, 30f, 48f, 18f));
        batch.CellRoadEvents.Add(new CellRoadEventRow(seed.RunId, 23, 0, PlaneCodes.Semantic, 1, 0, 1, 0f, 28f, 28f));
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
            SecondaryTargetRejectedCount: 0,
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
            SecondaryTargetRejectedCount: 0,
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
        int VerbSymbolId,
        int GeneratorId,
        int GambitId,
        int ShelfTerrainId,
        int OceanTerrainId,
        string PenguinVerbId,
        Dictionary<string, int> PenguinAttributeIds,
        Dictionary<string, int> SettlementAttributeIds);

    private static Dictionary<string, int> AttributeIds(KindConfig config, int firstId)
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        ids[config.Health.Name] = firstId;

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
