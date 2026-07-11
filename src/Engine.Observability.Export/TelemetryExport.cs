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
        var databasePath = Path.Combine(outputDirectory, "telemetry.duckdb");
        _flushRowThreshold = flushRowThreshold;
        Directory.CreateDirectory(_outputDirectory);

        _connection = new DuckDBConnection($"Data Source={databasePath}");
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
        ExportIdentityTables();
        ExportProcessTables();
        ExportWorldTables();
    }

    private void ExportIdentityTables()
    {
        ExportTable("symbols");
        ExportTable("entity_name_symbols");
        ExportTable("runs");
        ExportTable("tick_snapshots");
        ExportTable("kind_snapshots");
        ExportTable("entity_snapshots");
        ExportTable("entity_attribute_snapshots");
        ExportTable("entity_events");
        ExportTable("experiment_variants");
    }

    private void ExportProcessTables()
    {
        ExportTable("trace_events");
        ExportTable("attribute_events");
        ExportTable("movement_events");
    }

    private void ExportWorldTables()
    {
        ExportTable("relationship_events");
        ExportTable("relationship_snapshots");
        ExportTable("gambit_events");
        ExportTable("generator_events");
        ExportTable("generator_target_evaluations");
        ExportTable("generator_outputs");
        ExportTable("generator_state_snapshots");
        ExportTable("lifecycle_events");
        ExportTable("lifecycle_reason_events");
        ExportTable("map_generation_events");
        ExportTable("map_cell_events");
        ExportTable("cell_influence_events");
        ExportTable("cell_culture_influence_events");
        ExportTable("culture_slot_occupancy_events");
        ExportTable("minor_actor_verb_events");
        ExportTable("culture_field_component_snapshots");
        ExportTable("culture_power_snapshots");
        ExportTable("culture_entity_field_snapshots");
        ExportTable("culture_entity_birth_field_events");
        ExportTable("culture_influence_action_field_events");
        ExportTable("cell_road_events");
        ExportTable("map_snapshots");
        ExportTable("coverage_snapshots");
        ExportTable("coverage_terrain_snapshots");
        ExportTable("placement_events");
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
        EnsureIdentitySchema();
        EnsureProcessSchema();
        EnsureWorldSchema();
    }

    private void EnsureIdentitySchema()
    {
        EnsureTable("symbols", typeof(SymbolRow));
        EnsureTable("entity_name_symbols", typeof(EntityNameSymbolRow));
        EnsureTable("runs", typeof(RunRow));
        EnsureTable("tick_snapshots", typeof(TickSnapshotRow));
        EnsureTable("kind_snapshots", typeof(KindSnapshotRow));
        EnsureTable("entity_snapshots", typeof(EntitySnapshotRow));
        EnsureTable("entity_attribute_snapshots", typeof(EntityAttributeSnapshotRow));
        EnsureTable("entity_events", typeof(EntityEventRow));
        EnsureTable("experiment_variants", typeof(ExperimentVariantRow));
    }

    private void EnsureProcessSchema()
    {
        EnsureTable("trace_events", typeof(TraceRow));
        EnsureTable("attribute_events", typeof(AttributeEventRow));
        EnsureTable("movement_events", typeof(MovementEventRow));
    }

    private void EnsureWorldSchema()
    {
        EnsureTable("relationship_events", typeof(RelationshipEventRow));
        EnsureTable("relationship_snapshots", typeof(RelationshipSnapshotRow));
        EnsureTable("gambit_events", typeof(GambitEventRow));
        EnsureTable("generator_events", typeof(GeneratorEventRow));
        EnsureTable("generator_target_evaluations", typeof(GeneratorTargetEvaluationRow));
        EnsureTable("generator_outputs", typeof(GeneratorOutputRow));
        EnsureTable("generator_state_snapshots", typeof(GeneratorStateSnapshotRow));
        EnsureTable("generated_entities", typeof(GeneratedEntityRow));
        EnsureTable("lifecycle_events", typeof(LifecycleEventRow));
        EnsureTable("lifecycle_reason_events", typeof(LifecycleReasonEventRow));
        EnsureTable("map_generation_events", typeof(MapGenerationEventRow));
        EnsureTable("map_cell_events", typeof(MapCellEventRow));
        EnsureTable("cell_influence_events", typeof(CellInfluenceEventRow));
        EnsureTable("cell_culture_influence_events", typeof(CellCultureInfluenceEventRow));
        EnsureTable("culture_slot_occupancy_events", typeof(CultureSlotOccupancyEventRow));
        EnsureTable("minor_actor_verb_events", typeof(MinorActorVerbEventRow));
        EnsureTable("culture_field_component_snapshots", typeof(CultureFieldComponentSnapshotRow));
        EnsureTable("culture_power_snapshots", typeof(CulturePowerSnapshotRow));
        EnsureTable("culture_entity_field_snapshots", typeof(CultureEntityFieldSnapshotRow));
        EnsureTable("culture_entity_birth_field_events", typeof(CultureEntityBirthFieldEventRow));
        EnsureTable("culture_influence_action_field_events", typeof(CultureInfluenceActionFieldEventRow));
        EnsureTable("cell_road_events", typeof(CellRoadEventRow));
        EnsureTable("map_snapshots", typeof(MapSnapshotRow));
        EnsureTable("coverage_snapshots", typeof(CoverageSnapshotRow));
        EnsureTable("coverage_terrain_snapshots", typeof(CoverageTerrainSnapshotRow));
        EnsureTable("placement_events", typeof(PlacementEventRow));
    }

    private void EnsureTable(string tableName, Type rowType)
    {
        var properties = DuckDbRowSchema.PropertiesFor(rowType);
        SqlFileHelpers.Execute(_connection, SqlFileHelpers.CreateTableSql(tableName, properties, ifNotExists: true));
    }

    private void AppendPending()
    {
        AppendIdentityTables();
        AppendProcessTables();
        AppendWorldTables();
    }

    private void AppendIdentityTables()
    {
        AppendTable("symbols", _pending.Symbols);
        AppendTable("entity_name_symbols", _pending.EntityNameSymbols);
        AppendTable("runs", _pending.Runs);
        AppendTable("tick_snapshots", _pending.TickSnapshots);
        AppendTable("kind_snapshots", _pending.KindSnapshots);
        AppendTable("entity_snapshots", _pending.EntitySnapshots);
        AppendTable("entity_attribute_snapshots", _pending.EntityAttributeSnapshots);
        AppendTable("entity_events", _pending.EntityEvents);
        AppendTable("experiment_variants", _pending.ExperimentVariants);
    }

    private void AppendProcessTables()
    {
        AppendTable("trace_events", _pending.TraceEvents);
        AppendTable("attribute_events", _pending.AttributeEvents);
        AppendTable("movement_events", _pending.MovementEvents);
    }

    private void AppendWorldTables()
    {
        AppendTable("relationship_events", _pending.RelationshipEvents);
        AppendTable("relationship_snapshots", _pending.RelationshipSnapshots);
        AppendTable("gambit_events", _pending.GambitEvents);
        AppendTable("generator_events", _pending.GeneratorEvents);
        AppendTable("generator_target_evaluations", _pending.GeneratorTargetEvaluations);
        AppendTable("generator_outputs", _pending.GeneratorOutputs);
        AppendTable("generator_state_snapshots", _pending.GeneratorStateSnapshots);
        AppendTable("generated_entities", _pending.GeneratedEntities);
        AppendTable("lifecycle_events", _pending.LifecycleEvents);
        AppendTable("lifecycle_reason_events", _pending.LifecycleReasonEvents);
        AppendTable("map_generation_events", _pending.MapGenerationEvents);
        AppendTable("map_cell_events", _pending.MapCellEvents);
        AppendTable("cell_influence_events", _pending.CellInfluenceEvents);
        AppendTable("cell_culture_influence_events", _pending.CellCultureInfluenceEvents);
        AppendTable("culture_slot_occupancy_events", _pending.CultureSlotOccupancyEvents);
        AppendTable("minor_actor_verb_events", _pending.MinorActorVerbEvents);
        AppendTable("culture_field_component_snapshots", _pending.CultureFieldComponentSnapshots);
        AppendTable("culture_power_snapshots", _pending.CulturePowerSnapshots);
        AppendTable("culture_entity_field_snapshots", _pending.CultureEntityFieldSnapshots);
        AppendTable("culture_entity_birth_field_events", _pending.CultureEntityBirthFieldEvents);
        AppendTable("culture_influence_action_field_events", _pending.CultureInfluenceActionFieldEvents);
        AppendTable("cell_road_events", _pending.CellRoadEvents);
        AppendTable("map_snapshots", _pending.MapSnapshots);
        AppendTable("coverage_snapshots", _pending.CoverageSnapshots);
        AppendTable("coverage_terrain_snapshots", _pending.CoverageTerrainSnapshots);
        AppendTable("placement_events", _pending.PlacementEvents);
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
        return CountIdentityRows(batch) +
               CountProcessRows(batch) +
               CountWorldRows(batch);
    }

    private static long CountIdentityRows(TelemetryBatch batch) =>
        batch.Symbols.Count +
        batch.EntityNameSymbols.Count +
        batch.Runs.Count +
        batch.TickSnapshots.Count +
        batch.KindSnapshots.Count +
        batch.EntitySnapshots.Count +
        batch.EntityAttributeSnapshots.Count +
        batch.EntityEvents.Count +
        batch.ExperimentVariants.Count;

    private static long CountProcessRows(TelemetryBatch batch) =>
        batch.TraceEvents.Count +
        batch.AttributeEvents.Count +
        batch.MovementEvents.Count;

    private static long CountWorldRows(TelemetryBatch batch) =>
        batch.RelationshipEvents.Count +
        batch.RelationshipSnapshots.Count +
        batch.GambitEvents.Count +
        batch.GeneratorEvents.Count +
        batch.GeneratorTargetEvaluations.Count +
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
        batch.MinorActorVerbEvents.Count +
        batch.CultureFieldComponentSnapshots.Count +
        batch.CulturePowerSnapshots.Count +
        batch.CultureEntityFieldSnapshots.Count +
        batch.CultureEntityBirthFieldEvents.Count +
        batch.CultureInfluenceActionFieldEvents.Count +
        batch.CellRoadEvents.Count +
        batch.MapSnapshots.Count +
        batch.CoverageSnapshots.Count +
        batch.CoverageTerrainSnapshots.Count +
        batch.PlacementEvents.Count;

    public static void Append(this TelemetryBatch destination, TelemetryBatch source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        AppendIdentityRows(destination, source);
        AppendProcessRows(destination, source);
        AppendWorldRows(destination, source);
    }

    private static void AppendIdentityRows(TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.Symbols, source.Symbols);
        AppendRows(destination.EntityNameSymbols, source.EntityNameSymbols);
        AppendRows(destination.Runs, source.Runs);
        AppendRows(destination.TickSnapshots, source.TickSnapshots);
        AppendRows(destination.KindSnapshots, source.KindSnapshots);
        AppendRows(destination.EntitySnapshots, source.EntitySnapshots);
        AppendRows(destination.EntityAttributeSnapshots, source.EntityAttributeSnapshots);
        AppendRows(destination.EntityEvents, source.EntityEvents);
        AppendRows(destination.ExperimentVariants, source.ExperimentVariants);
    }

    private static void AppendProcessRows(TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.TraceEvents, source.TraceEvents);
        AppendRows(destination.AttributeEvents, source.AttributeEvents);
        AppendRows(destination.MovementEvents, source.MovementEvents);
    }

    private static void AppendWorldRows(TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.RelationshipEvents, source.RelationshipEvents);
        AppendRows(destination.RelationshipSnapshots, source.RelationshipSnapshots);
        AppendRows(destination.GambitEvents, source.GambitEvents);
        AppendRows(destination.GeneratorEvents, source.GeneratorEvents);
        AppendRows(destination.GeneratorTargetEvaluations, source.GeneratorTargetEvaluations);
        AppendRows(destination.GeneratorOutputs, source.GeneratorOutputs);
        AppendRows(destination.GeneratorStateSnapshots, source.GeneratorStateSnapshots);
        AppendRows(destination.GeneratedEntities, source.GeneratedEntities);
        AppendRows(destination.LifecycleEvents, source.LifecycleEvents);
        AppendRows(destination.LifecycleReasonEvents, source.LifecycleReasonEvents);
        AppendRows(destination.MapGenerationEvents, source.MapGenerationEvents);
        AppendRows(destination.MapCellEvents, source.MapCellEvents);
        AppendRows(destination.CellInfluenceEvents, source.CellInfluenceEvents);
        AppendRows(destination.CellCultureInfluenceEvents, source.CellCultureInfluenceEvents);
        AppendRows(destination.CultureSlotOccupancyEvents, source.CultureSlotOccupancyEvents);
        AppendRows(destination.MinorActorVerbEvents, source.MinorActorVerbEvents);
        AppendRows(destination.CultureFieldComponentSnapshots, source.CultureFieldComponentSnapshots);
        AppendRows(destination.CulturePowerSnapshots, source.CulturePowerSnapshots);
        AppendRows(destination.CultureEntityFieldSnapshots, source.CultureEntityFieldSnapshots);
        AppendRows(destination.CultureEntityBirthFieldEvents, source.CultureEntityBirthFieldEvents);
        AppendRows(destination.CultureInfluenceActionFieldEvents, source.CultureInfluenceActionFieldEvents);
        AppendRows(destination.CellRoadEvents, source.CellRoadEvents);
        AppendRows(destination.MapSnapshots, source.MapSnapshots);
        AppendRows(destination.CoverageSnapshots, source.CoverageSnapshots);
        AppendRows(destination.CoverageTerrainSnapshots, source.CoverageTerrainSnapshots);
        AppendRows(destination.PlacementEvents, source.PlacementEvents);
    }

    private static void AppendRows<T>(
        System.Collections.ObjectModel.Collection<T> destination,
        System.Collections.ObjectModel.Collection<T> source)
    {
        if (destination is TelemetryRowCollection<T> rows)
        {
            rows.EnsureAdditionalCapacity(source.Count);
        }

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
