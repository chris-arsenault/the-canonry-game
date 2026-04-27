namespace MonteCarlo.Tests.Observability;

using Engine.Domain.Config;
using Engine.Constants;
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
        const int runId = 1000;
        const int domainId = 1;
        const int penguinKindId = 10;
        const int settlementKindId = 11;
        const int factionKindId = 12;
        const int artifactKindId = 13;
        const int wardenSubtypeId = 20;
        const int sanctuarySubtypeId = 21;
        const int compactSubtypeId = 22;
        const int cultureId = 30;
        const int processId = 40;
        const int generatorId = 50;
        const int foodResourceId = 60;
        const int wealthResourceId = 61;
        const int shelfTerrainId = 70;
        const int oceanTerrainId = 71;
        var penguinConfig = domain.Kinds[IceRemembersKinds.Penguin];
        var settlementConfig = domain.Kinds[IceRemembersKinds.Settlement];
        var penguinProcess = domain.Processes.First(process => process.TargetKind == IceRemembersKinds.Penguin);
        var penguinAttributeIds = AttributeIds(penguinConfig, firstId: 100);
        var settlementAttributeIds = AttributeIds(settlementConfig, firstId: 200);

        var batch = new TelemetryBatch();
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Run, runId, "seed-1000", "seed-1000"));
        batch.Symbols.Add(TelemetrySymbolIds.CreateWithId(TelemetrySymbolKinds.Domain, domainId, "IceRemembers", "IceRemembers"));
        TelemetrySymbolIds.AddFixedCodeSymbols(batch);
        AddSymbol(batch, ProjectionSymbolKind.Kind, penguinKindId, IceRemembersKinds.Penguin.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, settlementKindId, IceRemembersKinds.Settlement.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, factionKindId, IceRemembersKinds.Faction.Value);
        AddSymbol(batch, ProjectionSymbolKind.Kind, artifactKindId, IceRemembersKinds.Artifact.Value);
        AddSymbol(batch, ProjectionSymbolKind.Subtype, wardenSubtypeId, "warden");
        AddSymbol(batch, ProjectionSymbolKind.Subtype, sanctuarySubtypeId, "sanctuary");
        AddSymbol(batch, ProjectionSymbolKind.Subtype, compactSubtypeId, "compact");
        AddSymbol(batch, ProjectionSymbolKind.Culture, cultureId, IceRemembersCulture.Nightshelf.ToDomainName());
        AddSymbol(batch, ProjectionSymbolKind.Process, processId, penguinProcess.Name);
        AddSymbol(batch, ProjectionSymbolKind.Generator, generatorId, "penguin_calls_the_unhuddled");
        AddSymbol(batch, ProjectionSymbolKind.Generator, 51, "faction_seals_the_edge");
        AddSymbol(batch, ProjectionSymbolKind.Resource, foodResourceId, "Food");
        AddSymbol(batch, ProjectionSymbolKind.Resource, wealthResourceId, "Wealth");
        AddSymbol(batch, ProjectionSymbolKind.Terrain, shelfTerrainId, "Shelf");
        AddSymbol(batch, ProjectionSymbolKind.Terrain, oceanTerrainId, "Ocean");
        AddAttributeSymbols(batch, penguinAttributeIds);
        AddAttributeSymbols(batch, settlementAttributeIds);

        batch.Runs.Add(new RunRow(runId, 1000, domainId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        batch.TickSnapshots.Add(new TickSnapshotRow(runId, 0, 2, 0, 0, 1, 2, 20, 40, 1.5f, 1.0f));
        batch.TickSnapshots.Add(new TickSnapshotRow(runId, 25, 2, 1, includeGeneratorFire ? 1 : 0, 3, 3, 28, 32, 4.2f, 2.2f));

        batch.KindSnapshots.Add(new KindSnapshotRow(runId, 25, penguinKindId, wardenSubtypeId, 1, 0, 92f, 92f, 92f));
        batch.KindSnapshots.Add(new KindSnapshotRow(runId, 25, settlementKindId, sanctuarySubtypeId, 1, 0, 45f, 45f, 45f));

        AddEntitySnapshot(batch, runId, 0, 1, penguinKindId, wardenSubtypeId, cultureId, 0, 0, 0, 0, 0);
        AddEntityAttributes(batch, runId, 0, 1, penguinAttributeIds, 92f, 78f, 24f);
        AddEntitySnapshot(batch, runId, 0, 2, settlementKindId, sanctuarySubtypeId, cultureId, 2, 0, 1, 0, -1);
        AddEntityAttributes(batch, runId, 0, 2, settlementAttributeIds, 45f, 55f, 61f);
        AddEntitySnapshot(batch, runId, 25, 1, penguinKindId, wardenSubtypeId, cultureId, 4, 1, 0, 0, 0);
        AddEntityAttributes(batch, runId, 25, 1, penguinAttributeIds, 92f, 78f, 24f);
        AddEntitySnapshot(batch, runId, 25, 2, settlementKindId, sanctuarySubtypeId, cultureId, 6, 2, 1, 0, -1);
        AddEntityAttributes(batch, runId, 25, 2, settlementAttributeIds, 45f, 55f, 61f);

        batch.ProcessEvents.Add(new ProcessEventRow(
            runId,
            25,
            1,
            penguinKindId,
            1,
            processId,
            IsStale: false,
            StateCode: (int)KernelEventState.Succeeded,
            CandidateCount: 1,
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
        batch.ProcessCandidates.Add(new ProcessCandidateRow(
            runId,
            25,
            1,
            penguinKindId,
            processId,
            1,
            ProviderId: 0,
            1.25f,
            PreconditionPassed: true,
            PreconditionDiagnosticCode: 0,
            OnCooldown: false,
            WasSelected: true));

        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(runId, 0, foodResourceId, 28f, 1.4f, 0.5f, 3.0f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(runId, 25, foodResourceId, 12f, 0.6f, 0.2f, 1.5f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(runId, 0, wealthResourceId, 6f, 0.3f, 0f, 1f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow(runId, 25, wealthResourceId, 5f, 0.25f, 0f, 1f));

        batch.MapSnapshots.Add(new MapSnapshotRow(runId, 0, shelfTerrainId, 20, 8, 2));
        batch.MapSnapshots.Add(new MapSnapshotRow(runId, 0, oceanTerrainId, 40, 12, 0));
        batch.MapSnapshots.Add(new MapSnapshotRow(runId, 25, shelfTerrainId, 20, 10, 1));
        batch.MapSnapshots.Add(new MapSnapshotRow(runId, 25, oceanTerrainId, 40, 18, 1));
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
            runId,
            0,
            1000,
            0,
            111,
            222,
            phase.Id,
            description.Id,
            reason.Id,
            true));

        batch.EntityNameSymbols.Add(new EntityNameSymbolRow(runId, 7, artifactKindId, 0, 0, 7, "Broken Seal"));
        batch.LifecycleEvents.Add(new LifecycleEventRow(
            runId,
            18,
            (int)KernelEventType.EntityStatusChanged,
            7,
            artifactKindId,
            EntityStatusCodes.Historical));

        if (includeGeneratorFire)
        {
            batch.GeneratorEvents.Add(new GeneratorEventRow(
                RunId: runId,
                Tick: 25,
                EventTypeCode: (int)KernelEventType.GeneratorEvaluated,
                GeneratorId: generatorId,
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
                RunId: runId,
                Tick: 25,
                EventTypeCode: (int)KernelEventType.GeneratorFired,
                GeneratorId: generatorId,
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
            batch.GeneratedEntities.Add(new GeneratedEntityRow(
                runId,
                25,
                generatorId,
                3,
                settlementKindId,
                sanctuarySubtypeId,
                cultureId,
                6,
                2,
                1,
                0,
                -1));
            batch.GeneratedEntities.Add(new GeneratedEntityRow(
                runId,
                25,
                generatorId,
                4,
                factionKindId,
                compactSubtypeId,
                cultureId,
                6,
                2,
                1,
                1,
                -2));
        }

        return batch;
    }

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
