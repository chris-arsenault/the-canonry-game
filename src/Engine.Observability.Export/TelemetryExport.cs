namespace Engine.Observability.Export;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;

using DuckDB.NET.Data;

using Engine.Observability;

internal sealed class DuckDbTelemetryStore : IDisposable
{
    private const long DefaultFlushRowThreshold = 250_000;

    private readonly string _outputDirectory;
    private readonly string _databasePath;
    private readonly DuckDBConnection _connection;
    private readonly long _flushRowThreshold;
    private TelemetryBatch _pending = new();
    private long _pendingRows;
    private bool _disposed;

    public DuckDbTelemetryStore(string outputDirectory, long flushRowThreshold = DefaultFlushRowThreshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentOutOfRangeException.ThrowIfNegative(flushRowThreshold);

        _outputDirectory = outputDirectory;
        _databasePath = Path.Combine(outputDirectory, "telemetry.duckdb");
        _flushRowThreshold = flushRowThreshold;
        Directory.CreateDirectory(_outputDirectory);

        _connection = new DuckDBConnection($"Data Source={_databasePath}");
        _connection.Open();
        EnsureSchema();
    }

    public void Append(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposed();

        var rowCount = TelemetryBatchExtensions.CountRows(batch);
        if (rowCount == 0)
        {
            return;
        }

        _pending.Append(batch);
        _pendingRows += rowCount;
        if (_pendingRows >= _flushRowThreshold)
        {
            Flush();
        }
    }

    public void Flush()
    {
        ThrowIfDisposed();
        if (_pendingRows == 0)
        {
            return;
        }

        AppendPending();
        _pending = new TelemetryBatch();
        _pendingRows = 0;
    }

    public void ExportParquet()
    {
        ThrowIfDisposed();
        Flush();

        ExportTable("symbols");
        ExportTable("entity_name_symbols");
        ExportTable("runs");
        ExportTable("tick_snapshots");
        ExportTable("kind_snapshots");
        ExportTable("entity_snapshots");
        ExportTable("entity_attribute_snapshots");
        ExportTable("entity_events");
        ExportTable("planner_goals");
        ExportTable("planner_traces");
        ExportTable("planner_trace_goals");
        ExportTable("planner_trace_scores");
        ExportTable("planner_trace_steps");
        ExportTable("process_events");
        ExportTable("process_candidates");
        ExportTable("locality_opportunities");
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
        Flush();
        _disposed = true;
        _connection.Dispose();
    }

    private void EnsureSchema()
    {
        EnsureTable("symbols", typeof(SymbolRow));
        EnsureTable("entity_name_symbols", typeof(EntityNameSymbolRow));
        EnsureTable("runs", typeof(RunRow));
        EnsureTable("tick_snapshots", typeof(TickSnapshotRow));
        EnsureTable("kind_snapshots", typeof(KindSnapshotRow));
        EnsureTable("entity_snapshots", typeof(EntitySnapshotRow));
        EnsureTable("entity_attribute_snapshots", typeof(EntityAttributeSnapshotRow));
        EnsureTable("entity_events", typeof(EntityEventRow));
        EnsureTable("planner_goals", typeof(PlannerGoalRow));
        EnsureTable("planner_traces", typeof(PlannerTraceRow));
        EnsureTable("planner_trace_goals", typeof(PlannerTraceGoalRow));
        EnsureTable("planner_trace_scores", typeof(PlannerTraceScoreRow));
        EnsureTable("planner_trace_steps", typeof(PlannerTraceStepRow));
        EnsureTable("process_events", typeof(ProcessEventRow));
        EnsureTable("process_candidates", typeof(ProcessCandidateRow));
        EnsureTable("locality_opportunities", typeof(LocalityOpportunityRow));
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
        var properties = DuckDbRowSchema.PropertiesFor(rowType);
        SqlFileHelpers.Execute(_connection, SqlFileHelpers.CreateTableSql(tableName, properties, ifNotExists: true));
    }

    private void AppendPending()
    {
        AppendTable("symbols", _pending.Symbols);
        AppendTable("entity_name_symbols", _pending.EntityNameSymbols);
        AppendTable("runs", _pending.Runs);
        AppendTable("tick_snapshots", _pending.TickSnapshots);
        AppendTable("kind_snapshots", _pending.KindSnapshots);
        AppendTable("entity_snapshots", _pending.EntitySnapshots);
        AppendTable("entity_attribute_snapshots", _pending.EntityAttributeSnapshots);
        AppendTable("entity_events", _pending.EntityEvents);
        AppendTable("planner_goals", _pending.PlannerGoals);
        AppendTable("planner_traces", _pending.PlannerTraces);
        AppendTable("planner_trace_goals", _pending.PlannerTraceGoals);
        AppendTable("planner_trace_scores", _pending.PlannerTraceScores);
        AppendTable("planner_trace_steps", _pending.PlannerTraceSteps);
        AppendTable("process_events", _pending.ProcessEvents);
        AppendTable("process_candidates", _pending.ProcessCandidates);
        AppendTable("locality_opportunities", _pending.LocalityOpportunities);
        AppendTable("attribute_events", _pending.AttributeEvents);
        AppendTable("movement_events", _pending.MovementEvents);
        AppendTable("resource_events", _pending.ResourceEvents);
        AppendTable("resource_snapshots", _pending.ResourceSnapshots);
        AppendTable("relationship_events", _pending.RelationshipEvents);
        AppendTable("relationship_snapshots", _pending.RelationshipSnapshots);
        AppendTable("generator_events", _pending.GeneratorEvents);
        AppendTable("generated_entities", _pending.GeneratedEntities);
        AppendTable("lifecycle_events", _pending.LifecycleEvents);
        AppendTable("map_generation_events", _pending.MapGenerationEvents);
        AppendTable("map_snapshots", _pending.MapSnapshots);
    }

    private void AppendTable<T>(string tableName, IReadOnlyCollection<T> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        DuckDbAppenderWriter.Append(_connection, tableName, rows);
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
    public static long CountRows(this TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return batch.Symbols.Count +
               batch.EntityNameSymbols.Count +
               batch.Runs.Count +
               batch.TickSnapshots.Count +
               batch.KindSnapshots.Count +
               batch.EntitySnapshots.Count +
               batch.EntityAttributeSnapshots.Count +
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
    }

    public static void Append(this TelemetryBatch destination, TelemetryBatch source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

        AppendRows(destination.Symbols, source.Symbols);
        AppendRows(destination.EntityNameSymbols, source.EntityNameSymbols);
        AppendRows(destination.Runs, source.Runs);
        AppendRows(destination.TickSnapshots, source.TickSnapshots);
        AppendRows(destination.KindSnapshots, source.KindSnapshots);
        AppendRows(destination.EntitySnapshots, source.EntitySnapshots);
        AppendRows(destination.EntityAttributeSnapshots, source.EntityAttributeSnapshots);
        AppendRows(destination.EntityEvents, source.EntityEvents);
        AppendRows(destination.PlannerGoals, source.PlannerGoals);
        AppendRows(destination.PlannerTraces, source.PlannerTraces);
        AppendRows(destination.PlannerTraceGoals, source.PlannerTraceGoals);
        AppendRows(destination.PlannerTraceScores, source.PlannerTraceScores);
        AppendRows(destination.PlannerTraceSteps, source.PlannerTraceSteps);
        AppendRows(destination.ProcessEvents, source.ProcessEvents);
        AppendRows(destination.ProcessCandidates, source.ProcessCandidates);
        AppendRows(destination.LocalityOpportunities, source.LocalityOpportunities);
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

internal static class DuckDbAppenderWriter
{
    public static void Append<T>(DuckDBConnection connection, string tableName, IReadOnlyCollection<T> rows)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return;
        }

        var properties = DuckDbRowSchema.PropertiesFor(typeof(T));
        using var appender = connection.CreateAppender(tableName);
        foreach (var rowValue in rows)
        {
            var appenderRow = appender.CreateRow();
            for (var i = 0; i < properties.Length; i++)
            {
                AppendValue(appenderRow, properties[i].GetValue(rowValue));
            }

            appenderRow.EndRow();
        }

        appender.Close();
    }

    private static void AppendValue(IDuckDBAppenderRow row, object? value)
    {
        if (value is null)
        {
            row.AppendNullValue();
            return;
        }

        switch (value)
        {
            case bool boolValue:
                row.AppendValue((bool?)boolValue);
                break;
            case byte byteValue:
                row.AppendValue((long?)byteValue);
                break;
            case short shortValue:
                row.AppendValue((long?)shortValue);
                break;
            case int intValue:
                row.AppendValue((long?)intValue);
                break;
            case long longValue:
                row.AppendValue((long?)longValue);
                break;
            case float floatValue:
                row.AppendValue((double?)floatValue);
                break;
            case double doubleValue:
                row.AppendValue((double?)doubleValue);
                break;
            case decimal decimalValue:
                row.AppendValue((double?)decimalValue);
                break;
            case string stringValue:
                row.AppendValue(stringValue);
                break;
            case DateTime dateTimeValue:
                row.AppendValue((DateTime?)dateTimeValue);
                break;
            case DateTimeOffset dateTimeOffsetValue:
                row.AppendValue((DateTimeOffset?)dateTimeOffsetValue);
                break;
            case TimeSpan timeSpanValue:
                row.AppendValue((TimeSpan?)timeSpanValue);
                break;
            default:
                row.AppendValue(value.ToString());
                break;
        }
    }
}

internal static class DuckDbRowSchema
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesByType = new();

    public static PropertyInfo[] PropertiesFor(Type rowType)
    {
        ArgumentNullException.ThrowIfNull(rowType);
        return PropertiesByType.GetOrAdd(
            rowType,
            static type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public));
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
