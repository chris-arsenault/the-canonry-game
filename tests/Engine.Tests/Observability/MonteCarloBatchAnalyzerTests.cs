namespace Engine.Tests.Observability;

using Engine.Domain.Config;
using Engine.Domain.IceRemembers;
using Engine.Observability;
using MonteCarloTool;

public sealed class MonteCarloBatchAnalyzerTests : IDisposable
{
    private readonly List<string> _directoriesToDelete = [];

    [Fact]
    public void AnalyzeToFiles_WritesSummaryArtifactsForSyntheticBatch()
    {
        var domain = IceRemembersDefinition.Create();
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
        var domain = IceRemembersDefinition.Create();
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
        var penguinKind = IceRemembersKinds.Penguin.Value;
        var settlementKind = IceRemembersKinds.Settlement.Value;
        var penguinConfig = domain.Kinds[IceRemembersKinds.Penguin];
        var settlementConfig = domain.Kinds[IceRemembersKinds.Settlement];
        var penguinProcess = domain.Processes.First(process => process.TargetKind == IceRemembersKinds.Penguin);

        var penguinAttributes = FormatAttributes(
            penguinConfig.Attributes[0].Name, 92d,
            penguinConfig.Attributes[1].Name, 78d,
            penguinConfig.Attributes[2].Name, 24d);
        var settlementAttributes = FormatAttributes(
            settlementConfig.Attributes[0].Name, 45d,
            settlementConfig.Attributes[1].Name, 55d,
            settlementConfig.Attributes[2].Name, 61d);

        var batch = new TelemetryBatch();
        batch.Runs.Add(new RunRow("seed-1000", 1000, "IceRemembers", DateTime.UtcNow.ToString("o")));
        batch.TickSnapshots.Add(new TickSnapshotRow("seed-1000", 0, 2, 0, 0, 1, 2, 20, 40, 1.5f, 1.0f));
        batch.TickSnapshots.Add(new TickSnapshotRow("seed-1000", 25, 2, 1, includeGeneratorFire ? 1 : 0, 3, 3, 28, 32, 4.2f, 2.2f));

        batch.KindSnapshots.Add(new KindSnapshotRow("seed-1000", 25, penguinKind, "warden", 1, 0, 92f, 92f, 92f));
        batch.KindSnapshots.Add(new KindSnapshotRow("seed-1000", 25, settlementKind, "sanctuary", 1, 0, 45f, 45f, 45f));

        batch.EntitySnapshots.Add(new EntitySnapshotRow("seed-1000", 0, 1, penguinKind, "warden", IceRemembersCulture.Nightshelf.ToDomainName(), "Active", 0, 0, 0, 0, 0, penguinAttributes));
        batch.EntitySnapshots.Add(new EntitySnapshotRow("seed-1000", 0, 2, settlementKind, "sanctuary", IceRemembersCulture.Nightshelf.ToDomainName(), "Active", 2, 0, 1, 0, -1, settlementAttributes));
        batch.EntitySnapshots.Add(new EntitySnapshotRow("seed-1000", 25, 1, penguinKind, "warden", IceRemembersCulture.Nightshelf.ToDomainName(), "Active", 4, 1, 0, 0, 0, penguinAttributes));
        batch.EntitySnapshots.Add(new EntitySnapshotRow("seed-1000", 25, 2, settlementKind, "sanctuary", IceRemembersCulture.Nightshelf.ToDomainName(), "Active", 6, 2, 1, 0, -1, settlementAttributes));

        batch.ProcessEvents.Add(new ProcessEventRow(
            "seed-1000",
            25,
            1,
            penguinKind,
            "goal-1",
            penguinProcess.Name,
            IsStale: false,
            StatusNote: "executed",
            CandidateCount: 1));
        batch.ProcessCandidates.Add(new ProcessCandidateRow(
            "seed-1000",
            25,
            1,
            penguinKind,
            penguinProcess.Name,
            "goal-1",
            "self",
            1.25f,
            PreconditionPassed: true,
            PreconditionDiagnostic: string.Empty,
            OnCooldown: false,
            WasSelected: true));

        batch.ResourceSnapshots.Add(new ResourceSnapshotRow("seed-1000", 0, "Food", 28f, 1.4f, 0.5f, 3.0f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow("seed-1000", 25, "Food", 12f, 0.6f, 0.2f, 1.5f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow("seed-1000", 0, "Wealth", 6f, 0.3f, 0f, 1f));
        batch.ResourceSnapshots.Add(new ResourceSnapshotRow("seed-1000", 25, "Wealth", 5f, 0.25f, 0f, 1f));

        batch.MapSnapshots.Add(new MapSnapshotRow("seed-1000", 0, "Shelf", 20, 8, 2));
        batch.MapSnapshots.Add(new MapSnapshotRow("seed-1000", 0, "Ocean", 40, 12, 0));
        batch.MapSnapshots.Add(new MapSnapshotRow("seed-1000", 25, "Shelf", 20, 10, 1));
        batch.MapSnapshots.Add(new MapSnapshotRow("seed-1000", 25, "Ocean", 40, 18, 1));
        batch.MapGenerationEvents.Add(new MapGenerationEventRow(
            "seed-1000",
            0,
            1000,
            0,
            111,
            222,
            "quality_validation",
            "Rejected procedural seed after validation (1 issues).",
            "coverage too small",
            true));

        batch.LifecycleEvents.Add(new LifecycleEventRow("seed-1000", 18, "death", 7, IceRemembersKinds.Artifact.Value, "Broken Seal"));

        if (includeGeneratorFire)
        {
            batch.GeneratorEvents.Add(new GeneratorEventRow(
                "seed-1000",
                25,
                "pulse",
                "penguin_calls_the_unhuddled",
                "fired",
                2,
                0,
                0,
                0,
                0,
                0,
                0,
                1,
                0,
                1,
                0,
                0));
            batch.GeneratorEvents.Add(new GeneratorEventRow(
                "seed-1000",
                25,
                "fired",
                "penguin_calls_the_unhuddled",
                "fired",
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                1,
                2,
                4));
            batch.GeneratedEntities.Add(new GeneratedEntityRow(
                "seed-1000",
                25,
                "penguin_calls_the_unhuddled",
                3,
                settlementKind,
                "sanctuary",
                IceRemembersCulture.Nightshelf.ToDomainName(),
                6,
                2,
                1,
                0,
                -1));
            batch.GeneratedEntities.Add(new GeneratedEntityRow(
                "seed-1000",
                25,
                "penguin_calls_the_unhuddled",
                4,
                IceRemembersKinds.Faction.Value,
                "compact",
                IceRemembersCulture.Nightshelf.ToDomainName(),
                6,
                2,
                1,
                1,
                -2));
        }

        return batch;
    }

    private static string FormatAttributes(string firstName, double firstValue, string secondName, double secondValue, string thirdName, double thirdValue) =>
        string.Join('|', new[]
        {
            $"{firstName}={firstValue:F3}",
            $"{secondName}={secondValue:F3}",
            $"{thirdName}={thirdValue:F3}",
        });
}
