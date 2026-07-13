namespace MonteCarloTool;

using System.Globalization;
using System.Text.Json;

using DuckDB.NET.Data;

using Engine.Observability.Export;

internal sealed class MonteCarloRunRegistry : IDisposable
{
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly DuckDBConnection _connection;

    public MonteCarloRunRegistry(string artifactRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);

        Directory.CreateDirectory(artifactRoot);
        ArtifactRoot = artifactRoot;
        RegistryPath = Path.Combine(artifactRoot, "registry.duckdb");
        _connection = new DuckDBConnection($"Data Source={RegistryPath}");
        _connection.Open();
        EnsureSchema();
    }

    public string ArtifactRoot { get; }
    public string RegistryPath { get; }

    public void Dispose() => _connection.Dispose();

    public void Register(MonteCarloOptions options, MonteCarloProvenanceSnapshot provenance, string status)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        var runId = RequireRunId(options);
        if (TryGetRun(runId) is not null)
        {
            throw new InvalidOperationException($"Monte Carlo run id '{runId}' already exists in the registry.");
        }

        var artifactPath = Path.GetFullPath(options.OutputDirectory);
        if (Directory.Exists(artifactPath))
        {
            throw new InvalidOperationException(
                $"Monte Carlo artifact directory already exists for run id '{runId}': {artifactPath}");
        }

        Directory.CreateDirectory(artifactPath);
        var now = Now();
        var started = string.Equals(status, "running", StringComparison.Ordinal) ? now.ToString(CultureInfo.InvariantCulture) : "NULL";
        var optionsJson = JsonSerializer.Serialize(ToRunOptionsDocument(options), JsonOptions);
        var provenanceJson = JsonSerializer.Serialize(provenance, JsonOptions);
        Execute(
            $"""
            INSERT INTO mc_runs (
                run_id,
                artifact_path,
                status,
                registered_utc_ms,
                started_utc_ms,
                completed_utc_ms,
                deleted_utc_ms,
                options_json,
                provenance_json)
            VALUES (
                {Q(runId)},
                {Q(artifactPath)},
                {Q(status)},
                {now.ToString(CultureInfo.InvariantCulture)},
                {started},
                NULL,
                NULL,
                {Q(optionsJson)},
                {Q(provenanceJson)});
            """);
        WriteManifest(runId);
    }

    public void MarkCompleted(string runId, IEnumerable<MonteCarloStatInput> stats)
    {
        var now = Now();
        Execute(
            $"""
            UPDATE mc_runs
            SET status = 'completed',
                completed_utc_ms = {now.ToString(CultureInfo.InvariantCulture)}
            WHERE run_id = {Q(runId)};
            """);
        AppendStats(runId, stats, source: "analysis");
        WriteManifest(runId);
    }

    public void MarkFailed(string runId)
    {
        var now = Now();
        Execute(
            $"""
            UPDATE mc_runs
            SET status = 'failed',
                completed_utc_ms = {now.ToString(CultureInfo.InvariantCulture)}
            WHERE run_id = {Q(runId)};
            """);
        WriteManifest(runId);
    }

    public void AppendManualStats(string runId, IEnumerable<MonteCarloStatInput> stats)
    {
        RequireExistingRun(runId);
        AppendStats(runId, stats, source: "manual");
        WriteManifest(runId);
    }

    public void MarkAnalyzed(string runId, IEnumerable<MonteCarloStatInput> stats)
    {
        RequireExistingRun(runId);
        AppendStats(runId, stats, source: "analysis");
        WriteManifest(runId);
    }

    public IReadOnlyList<MonteCarloRegisteredRun> ListRuns() =>
        QueryRuns(
            """
            SELECT
                run_id,
                artifact_path,
                status,
                registered_utc_ms,
                started_utc_ms,
                completed_utc_ms,
                deleted_utc_ms,
                options_json,
                provenance_json
            FROM mc_runs
            ORDER BY registered_utc_ms, run_id;
            """);

    public MonteCarloRegisteredRun GetRun(string runId) =>
        TryGetRun(runId) ?? throw new InvalidOperationException($"Monte Carlo run id '{runId}' is not registered.");

    public IReadOnlyList<MonteCarloRegisteredStat> GetStats(string runId)
    {
        RequireExistingRun(runId);
        using var command = _connection.CreateCommand();
#pragma warning disable CA2100
        command.CommandText =
            $"""
            SELECT stat_name, stat_value, source, recorded_utc_ms
            FROM mc_stats
            WHERE run_id = {Q(runId)}
            ORDER BY recorded_utc_ms, stat_name;
            """;
#pragma warning restore CA2100
        using var reader = command.ExecuteReader();
        var rows = new List<MonteCarloRegisteredStat>();
        while (reader.Read())
        {
            rows.Add(new MonteCarloRegisteredStat(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3)));
        }

        return rows;
    }

    public string ShowManifest(string runId)
    {
        var run = GetRun(runId);
        var manifestPath = Path.Combine(run.ArtifactPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            WriteManifest(runId);
        }

        return File.ReadAllText(manifestPath);
    }

    public void DeleteRun(string runId)
    {
        var run = GetRun(runId);
        if (string.Equals(run.Status, "deleted", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Monte Carlo run id '{runId}' is already deleted.");
        }

        var deletedRoot = Path.Combine(ArtifactRoot, ".deleted");
        Directory.CreateDirectory(deletedRoot);
        var deletedPath = Path.GetFullPath(Path.Combine(
            deletedRoot,
            $"{runId}-{DateTime.UtcNow:yyyyMMddTHHmmssfff}"));
        if (Directory.Exists(run.ArtifactPath))
        {
            Directory.Move(run.ArtifactPath, deletedPath);
        }

        var now = Now();
        Execute(
            $"""
            UPDATE mc_runs
            SET status = 'deleted',
                artifact_path = {Q(deletedPath)},
                deleted_utc_ms = {now.ToString(CultureInfo.InvariantCulture)}
            WHERE run_id = {Q(runId)};
            """);
        WriteManifest(runId);
    }

    private void AppendStats(string runId, IEnumerable<MonteCarloStatInput> stats, string source)
    {
        var now = Now();
        foreach (var stat in stats)
        {
            Execute(
                $"""
                INSERT INTO mc_stats (run_id, stat_name, stat_value, source, recorded_utc_ms)
                VALUES (
                    {Q(runId)},
                    {Q(stat.Name)},
                    {Q(stat.Value)},
                    {Q(source)},
                    {now.ToString(CultureInfo.InvariantCulture)});
                """);
        }
    }

    private void WriteManifest(string runId)
    {
        var run = GetRun(runId);
        Directory.CreateDirectory(run.ArtifactPath);
        var latestStats = GetStats(runId)
            .GroupBy(stat => stat.Name, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.Ordinal);
        var manifest = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["schemaVersion"] = SchemaVersion,
            ["runId"] = run.RunId,
            ["status"] = run.Status,
            ["artifactPath"] = run.ArtifactPath,
            ["registeredUtc"] = FormatTime(run.RegisteredUtcMilliseconds),
            ["startedUtc"] = FormatTime(run.StartedUtcMilliseconds),
            ["completedUtc"] = FormatTime(run.CompletedUtcMilliseconds),
            ["deletedUtc"] = FormatTime(run.DeletedUtcMilliseconds),
            ["options"] = JsonSerializer.Deserialize<JsonElement>(run.OptionsJson),
            ["provenance"] = JsonSerializer.Deserialize<JsonElement>(run.ProvenanceJson),
            ["stats"] = latestStats,
        };
        File.WriteAllText(
            Path.Combine(run.ArtifactPath, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonOptions));
    }

    private void RequireExistingRun(string runId)
    {
        _ = GetRun(runId);
    }

    private MonteCarloRegisteredRun? TryGetRun(string runId)
    {
        var rows = QueryRuns(
            $"""
            SELECT
                run_id,
                artifact_path,
                status,
                registered_utc_ms,
                started_utc_ms,
                completed_utc_ms,
                deleted_utc_ms,
                options_json,
                provenance_json
            FROM mc_runs
            WHERE run_id = {Q(runId)}
            LIMIT 1;
            """);
        return rows.Count == 0 ? null : rows[0];
    }

    private List<MonteCarloRegisteredRun> QueryRuns(string sql)
    {
        using var command = _connection.CreateCommand();
#pragma warning disable CA2100
        command.CommandText = sql;
#pragma warning restore CA2100
        using var reader = command.ExecuteReader();
        var rows = new List<MonteCarloRegisteredRun>();
        while (reader.Read())
        {
            rows.Add(new MonteCarloRegisteredRun(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4),
                reader.IsDBNull(5) ? null : reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetInt64(6),
                reader.GetString(7),
                reader.GetString(8)));
        }

        return rows;
    }

    private void EnsureSchema()
    {
        Execute(
            """
            CREATE TABLE IF NOT EXISTS mc_runs (
                run_id VARCHAR PRIMARY KEY,
                artifact_path VARCHAR NOT NULL,
                status VARCHAR NOT NULL,
                registered_utc_ms BIGINT NOT NULL,
                started_utc_ms BIGINT,
                completed_utc_ms BIGINT,
                deleted_utc_ms BIGINT,
                options_json VARCHAR NOT NULL,
                provenance_json VARCHAR NOT NULL
            );
            """);
        Execute(
            """
            CREATE TABLE IF NOT EXISTS mc_stats (
                run_id VARCHAR NOT NULL,
                stat_name VARCHAR NOT NULL,
                stat_value VARCHAR NOT NULL,
                source VARCHAR NOT NULL,
                recorded_utc_ms BIGINT NOT NULL
            );
            """);
    }

    private void Execute(string sql) => SqlFileHelpers.Execute(_connection, sql);

    private static string RequireRunId(MonteCarloOptions options) =>
        options.RunId ?? throw new InvalidOperationException("A registered MC command requires --run-id.");

    private static object ToRunOptionsDocument(MonteCarloOptions options) =>
        new
        {
            runs = options.Runs,
            ticks = options.Ticks,
            seed = options.Seed,
            entitySnapshotCadence = options.EntitySnapshotCadence,
            skipAnalysis = options.SkipAnalysis,
            disableEntitySnapshots = options.DisableEntitySnapshots,
            simulationOnly = options.SimulationOnly,
            traceEnabled = options.TraceEnabled,
            parallelWorkerCount = options.ParallelWorkerCount,
            configFilePath = options.ConfigFilePath,
        };

    private static string? FormatTime(long? utcMilliseconds) =>
        utcMilliseconds is null
            ? null
            : DateTimeOffset.FromUnixTimeMilliseconds(utcMilliseconds.Value).UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string Q(string value) => SqlFileHelpers.QuoteString(value);
}

internal sealed record MonteCarloRegisteredRun(
    string RunId,
    string ArtifactPath,
    string Status,
    long RegisteredUtcMilliseconds,
    long? StartedUtcMilliseconds,
    long? CompletedUtcMilliseconds,
    long? DeletedUtcMilliseconds,
    string OptionsJson,
    string ProvenanceJson);

internal sealed record MonteCarloRegisteredStat(
    string Name,
    string Value,
    string Source,
    long RecordedUtcMilliseconds);
