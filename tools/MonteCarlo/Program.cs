using System.Globalization;
using System.Reflection;
using System.Text;

using DuckDB.NET.Data;

using Engine.Domain.IceRemembers;
using Engine.Observability;
using Engine.Simulation;

var options = MonteCarloOptions.Parse(args);
Directory.CreateDirectory(options.OutputDirectory);

var batch = new TelemetryBatch();
for (var runIndex = 0; runIndex < options.Runs; runIndex++)
{
    var seed = options.Seed + runIndex;
    var runId = $"seed-{seed}";
    var sim = SimulationRunner.CreateFromDomain(IceRemembersDefinition.Create(), new Random(seed));

    using var recorder = new SimulationTelemetryRecorder(
        sim,
        runId,
        seed,
        entitySnapshotCadence: options.EntitySnapshotCadence);

    for (var tick = 0; tick < options.Ticks; tick++)
    {
        sim.Tick();
        recorder.OnTickEnded();
    }

    batch.Append(recorder.Batch);
    Console.WriteLine(FormattableString.Invariant(
        $"run {runIndex + 1}/{options.Runs}: seed={seed} ticks={options.Ticks} fires={sim.GeneratorSystem.TotalFired}"));
}

var writer = new DuckDbParquetTelemetryWriter(options.OutputDirectory);
writer.Write(batch);
Console.WriteLine($"wrote Monte Carlo telemetry: {Path.GetFullPath(options.OutputDirectory)}");

internal sealed record MonteCarloOptions(
    int Runs,
    int Ticks,
    int Seed,
    int EntitySnapshotCadence,
    string OutputDirectory)
{
    public static MonteCarloOptions Parse(string[] args)
    {
        var runs = ReadInt(args, "--runs", 10);
        var ticks = ReadInt(args, "--ticks", 500);
        var seed = ReadInt(args, "--seed", 1000);
        var cadence = ReadInt(args, "--entity-snapshot-cadence", 25);
        var output = ReadString(
            args,
            "--out",
            Path.Combine(
                ".artifacts",
                "monte-carlo",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture)))!;

        return new MonteCarloOptions(runs, ticks, seed, cadence, output);
    }

    private static int ReadInt(string[] args, string name, int fallback)
    {
        var value = ReadString(args, name, null);
        return value is null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string? ReadString(string[] args, string name, string? fallback)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return fallback;
    }
}

internal sealed class DuckDbParquetTelemetryWriter(string outputDirectory)
{
    private readonly string _outputDirectory = outputDirectory;

    public void Write(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        Directory.CreateDirectory(_outputDirectory);

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();

        WriteTable(connection, "runs", batch.Runs);
        WriteTable(connection, "tick_snapshots", batch.TickSnapshots);
        WriteTable(connection, "kind_snapshots", batch.KindSnapshots);
        WriteTable(connection, "entity_snapshots", batch.EntitySnapshots);
        WriteTable(connection, "entity_events", batch.EntityEvents);
        WriteTable(connection, "planner_goals", batch.PlannerGoals);
        WriteTable(connection, "process_events", batch.ProcessEvents);
        WriteTable(connection, "process_candidates", batch.ProcessCandidates);
        WriteTable(connection, "attribute_events", batch.AttributeEvents);
        WriteTable(connection, "movement_events", batch.MovementEvents);
        WriteTable(connection, "resource_events", batch.ResourceEvents);
        WriteTable(connection, "resource_snapshots", batch.ResourceSnapshots);
        WriteTable(connection, "relationship_events", batch.RelationshipEvents);
        WriteTable(connection, "relationship_snapshots", batch.RelationshipSnapshots);
        WriteTable(connection, "generator_events", batch.GeneratorEvents);
        WriteTable(connection, "generated_entities", batch.GeneratedEntities);
        WriteTable(connection, "lifecycle_events", batch.LifecycleEvents);
        WriteTable(connection, "map_snapshots", batch.MapSnapshots);
    }

    private void WriteTable<T>(DuckDBConnection connection, string tableName, IEnumerable<T> rows)
    {
        var rowList = rows.ToList();
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Execute(connection, CreateTableSql(tableName, properties));

        if (rowList.Count > 0)
        {
            var csvPath = Path.Combine(_outputDirectory, $"{tableName}.csv");
            WriteCsv(csvPath, properties, rowList);
            Execute(
                connection,
                $"COPY {QuoteIdentifier(tableName)} FROM {QuoteString(csvPath)} (HEADER, DELIMITER ',', QUOTE '\"', ESCAPE '\"', NULL '');");
            File.Delete(csvPath);
        }

        var parquetPath = Path.Combine(_outputDirectory, $"{tableName}.parquet");
        Execute(connection, $"COPY {QuoteIdentifier(tableName)} TO {QuoteString(parquetPath)} (FORMAT PARQUET);");
    }

    private static string CreateTableSql(string tableName, IReadOnlyList<PropertyInfo> properties)
    {
        var columns = properties.Select(property =>
            $"{QuoteIdentifier(ToSnakeCase(property.Name))} {DuckDbTypeFor(property.PropertyType)}");
        return $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", columns)});";
    }

    private static void WriteCsv<T>(string path, IReadOnlyList<PropertyInfo> properties, IReadOnlyList<T> rows)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine(string.Join(',', properties.Select(property => EscapeCsv(ToSnakeCase(property.Name)))));

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(
                ',',
                properties.Select(property => EscapeCsv(FormatValue(property.GetValue(row))))));
        }
    }

    private static void Execute(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100
        // SQL is generated only from fixed table names, reflected row schemas,
        // and quoted local file paths. DuckDB COPY cannot parameterize identifiers.
        command.CommandText = sql;
#pragma warning restore CA2100
        command.ExecuteNonQuery();
    }

    private static string DuckDbTypeFor(Type type)
    {
        var effective = Nullable.GetUnderlyingType(type) ?? type;
        if (effective == typeof(int) || effective == typeof(long)) return "BIGINT";
        if (effective == typeof(float) || effective == typeof(double) || effective == typeof(decimal)) return "DOUBLE";
        if (effective == typeof(bool)) return "BOOLEAN";
        return "VARCHAR";
    }

    private static string FormatValue(object? value) =>
        value switch
        {
            null => string.Empty,
            bool boolValue => boolValue ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    private static string EscapeCsv(string value)
    {
        if (!value.Contains('"', StringComparison.Ordinal) &&
            !value.Contains(',', StringComparison.Ordinal) &&
            !value.Contains('\n', StringComparison.Ordinal) &&
            !value.Contains('\r', StringComparison.Ordinal))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static string QuoteIdentifier(string value) =>
        "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string QuoteString(string value) =>
        "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string ToSnakeCase(string value)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (char.IsUpper(ch) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}

internal static class TelemetryBatchExtensions
{
    public static void Append(this TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.Runs, source.Runs);
        AppendRows(destination.TickSnapshots, source.TickSnapshots);
        AppendRows(destination.KindSnapshots, source.KindSnapshots);
        AppendRows(destination.EntitySnapshots, source.EntitySnapshots);
        AppendRows(destination.EntityEvents, source.EntityEvents);
        AppendRows(destination.PlannerGoals, source.PlannerGoals);
        AppendRows(destination.ProcessEvents, source.ProcessEvents);
        AppendRows(destination.ProcessCandidates, source.ProcessCandidates);
        AppendRows(destination.AttributeEvents, source.AttributeEvents);
        AppendRows(destination.MovementEvents, source.MovementEvents);
        AppendRows(destination.ResourceEvents, source.ResourceEvents);
        AppendRows(destination.ResourceSnapshots, source.ResourceSnapshots);
        AppendRows(destination.RelationshipEvents, source.RelationshipEvents);
        AppendRows(destination.RelationshipSnapshots, source.RelationshipSnapshots);
        AppendRows(destination.GeneratorEvents, source.GeneratorEvents);
        AppendRows(destination.GeneratedEntities, source.GeneratedEntities);
        AppendRows(destination.LifecycleEvents, source.LifecycleEvents);
        AppendRows(destination.MapSnapshots, source.MapSnapshots);
    }

    private static void AppendRows<T>(
        System.Collections.ObjectModel.Collection<T> destination,
        System.Collections.ObjectModel.Collection<T> source)
    {
        foreach (var row in source)
        {
            destination.Add(row);
        }
    }
}
