namespace MonteCarloTool;

using System.Globalization;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;

using DuckDB.NET.Data;

using Engine.Observability;

internal sealed class DuckDbTelemetryStore : IDisposable
{
    private readonly string _outputDirectory;
    private readonly string _databasePath;
    private readonly DuckDBConnection _connection;
    private bool _disposed;

    public DuckDbTelemetryStore(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        _outputDirectory = outputDirectory;
        _databasePath = Path.Combine(outputDirectory, "telemetry.duckdb");
        Directory.CreateDirectory(_outputDirectory);

        _connection = new DuckDBConnection($"Data Source={_databasePath}");
        _connection.Open();
        EnsureSchema();
    }

    public void Append(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposed();

        AppendTable("runs", batch.Runs);
        AppendTable("tick_snapshots", batch.TickSnapshots);
        AppendTable("kind_snapshots", batch.KindSnapshots);
        AppendTable("entity_snapshots", batch.EntitySnapshots);
        AppendTable("entity_events", batch.EntityEvents);
        AppendTable("planner_goals", batch.PlannerGoals);
        AppendTable("process_events", batch.ProcessEvents);
        AppendTable("process_candidates", batch.ProcessCandidates);
        AppendTable("attribute_events", batch.AttributeEvents);
        AppendTable("movement_events", batch.MovementEvents);
        AppendTable("resource_events", batch.ResourceEvents);
        AppendTable("resource_snapshots", batch.ResourceSnapshots);
        AppendTable("relationship_events", batch.RelationshipEvents);
        AppendTable("relationship_snapshots", batch.RelationshipSnapshots);
        AppendTable("generator_events", batch.GeneratorEvents);
        AppendTable("generated_entities", batch.GeneratedEntities);
        AppendTable("lifecycle_events", batch.LifecycleEvents);
        AppendTable("map_generation_events", batch.MapGenerationEvents);
        AppendTable("map_snapshots", batch.MapSnapshots);
    }

    public void ExportParquet()
    {
        ThrowIfDisposed();

        ExportTable("runs");
        ExportTable("tick_snapshots");
        ExportTable("kind_snapshots");
        ExportTable("entity_snapshots");
        ExportTable("entity_events");
        ExportTable("planner_goals");
        ExportTable("process_events");
        ExportTable("process_candidates");
        ExportTable("attribute_events");
        ExportTable("movement_events");
        ExportTable("resource_events");
        ExportTable("resource_snapshots");
        ExportTable("relationship_events");
        ExportTable("relationship_snapshots");
        ExportTable("generator_events");
        ExportTable("generated_entities");
        ExportTable("lifecycle_events");
        ExportTable("map_generation_events");
        ExportTable("map_snapshots");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
    }

    private void EnsureSchema()
    {
        EnsureTable("runs", typeof(RunRow));
        EnsureTable("tick_snapshots", typeof(TickSnapshotRow));
        EnsureTable("kind_snapshots", typeof(KindSnapshotRow));
        EnsureTable("entity_snapshots", typeof(EntitySnapshotRow));
        EnsureTable("entity_events", typeof(EntityEventRow));
        EnsureTable("planner_goals", typeof(PlannerGoalRow));
        EnsureTable("process_events", typeof(ProcessEventRow));
        EnsureTable("process_candidates", typeof(ProcessCandidateRow));
        EnsureTable("attribute_events", typeof(AttributeEventRow));
        EnsureTable("movement_events", typeof(MovementEventRow));
        EnsureTable("resource_events", typeof(ResourceEventRow));
        EnsureTable("resource_snapshots", typeof(ResourceSnapshotRow));
        EnsureTable("relationship_events", typeof(RelationshipEventRow));
        EnsureTable("relationship_snapshots", typeof(RelationshipSnapshotRow));
        EnsureTable("generator_events", typeof(GeneratorEventRow));
        EnsureTable("generated_entities", typeof(GeneratedEntityRow));
        EnsureTable("lifecycle_events", typeof(LifecycleEventRow));
        EnsureTable("map_generation_events", typeof(MapGenerationEventRow));
        EnsureTable("map_snapshots", typeof(MapSnapshotRow));
    }

    private void EnsureTable(string tableName, Type rowType)
    {
        var properties = rowType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        SqlFileHelpers.Execute(_connection, SqlFileHelpers.CreateTableSql(tableName, properties, ifNotExists: true));
    }

    private void AppendTable<T>(string tableName, Collection<T> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var tempCsv = Path.Combine(_outputDirectory, ".staging", $"{tableName}-{Guid.NewGuid():N}.csv");
        CsvFileWriter.WriteCsv(tempCsv, properties, rows);
        SqlFileHelpers.Execute(
            _connection,
            $"COPY {SqlFileHelpers.QuoteIdentifier(tableName)} FROM {SqlFileHelpers.QuoteString(tempCsv)} (HEADER, DELIMITER ',', QUOTE '\"', ESCAPE '\"', NULL '');");
        File.Delete(tempCsv);
    }

    private void ExportTable(string tableName)
    {
        var parquetPath = Path.Combine(_outputDirectory, $"{tableName}.parquet");
        SqlFileHelpers.Execute(
            _connection,
            $"COPY {SqlFileHelpers.QuoteIdentifier(tableName)} TO {SqlFileHelpers.QuoteString(parquetPath)} (FORMAT PARQUET);");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed class DuckDbParquetTelemetryWriter(string outputDirectory)
{
    private readonly string _outputDirectory = outputDirectory;

    public void Write(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        using var store = new DuckDbTelemetryStore(_outputDirectory);
        store.Append(batch);
        store.ExportParquet();
    }
}

internal static class TelemetryBatchExtensions
{
    public static void Append(this TelemetryBatch destination, TelemetryBatch source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

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
        AppendRows(destination.MapGenerationEvents, source.MapGenerationEvents);
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

internal static class CsvFileWriter
{
    public static void WriteCsv<T>(string path, IReadOnlyCollection<T> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        WriteCsv(path, typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public), rows);
    }

    public static void WriteCsv<T>(string path, IReadOnlyList<PropertyInfo> properties, IReadOnlyCollection<T> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(rows);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine(string.Join(',', properties.Select(property => EscapeCsv(SqlFileHelpers.ToSnakeCase(property.Name)))));

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(
                ',',
                properties.Select(property => EscapeCsv(FormatValue(property.GetValue(row))))));
        }
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
}

internal static class SqlFileHelpers
{
    public static string CreateTableSql(string tableName, IReadOnlyList<PropertyInfo> properties, bool ifNotExists = false)
    {
        var columns = properties.Select(property =>
            $"{QuoteIdentifier(ToSnakeCase(property.Name))} {DuckDbTypeFor(property.PropertyType)}");
        var create = ifNotExists ? "CREATE TABLE IF NOT EXISTS" : "CREATE TABLE";
        return $"{create} {QuoteIdentifier(tableName)} ({string.Join(", ", columns)});";
    }

    public static void Execute(DuckDBConnection connection, string sql)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var command = connection.CreateCommand();
#pragma warning disable CA2100
        // SQL is generated only from fixed table names, reflected row schemas,
        // static query text, and quoted local file paths. DuckDB COPY and CREATE
        // VIEW cannot parameterize identifiers.
        command.CommandText = sql;
#pragma warning restore CA2100
        command.ExecuteNonQuery();
    }

    public static string QuoteIdentifier(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    public static string QuoteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    public static string ToSnakeCase(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

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

    private static string DuckDbTypeFor(Type type)
    {
        var effective = Nullable.GetUnderlyingType(type) ?? type;
        if (effective == typeof(int) || effective == typeof(long)) return "BIGINT";
        if (effective == typeof(float) || effective == typeof(double) || effective == typeof(decimal)) return "DOUBLE";
        if (effective == typeof(bool)) return "BOOLEAN";
        return "VARCHAR";
    }
}
