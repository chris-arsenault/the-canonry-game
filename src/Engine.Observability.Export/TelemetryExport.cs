namespace Engine.Observability.Export;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Channels;

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

    public void AppendOwned(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposed();
        Flush();
        if (TelemetryBatchExtensions.CountRows(batch) > 0)
        {
            AppendBatch(batch);
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

    // The cheap tables that answer most scorecard questions (population,
    // density, endings, stage ledger, lifespans) — exported mid-run so
    // analysis can start while the simulation continues. The heavyweight
    // event tables (cell culture influence, attributes, verbs) wait for the
    // end-of-run export.
    private static readonly string[] CoreStreamTables =
    [
        "symbols",
        "runs",
        "tick_snapshots",
        "entity_events",
        "entity_snapshots",
        "gambit_events",
        "generator_events",
        "lifecycle_events",
        "movement_events",
        "coverage_snapshots",
    ];

    public void ExportCoreParquet()
    {
        ThrowIfDisposed();
        Flush();
        var streamDirectory = Path.Combine(_outputDirectory, "stream");
        Directory.CreateDirectory(streamDirectory);
        foreach (var tableName in CoreStreamTables)
        {
            ExportTableTo(tableName, streamDirectory);
        }
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
    }

    private void ExportProcessTables()
    {
        ExportTable("trace_events");
        ExportTable("movement_events");
    }

    private void ExportWorldTables()
    {
        ExportTable("relationship_events");
        ExportTable("gambit_events");
        ExportTable("generator_events");
        ExportTable("generator_outputs");
        ExportTable("lifecycle_events");
        ExportTable("map_generation_events");
        ExportTable("map_cell_events");
        ExportTable("culture_field_cell_snapshots");
        ExportTable("minor_actor_work_events");
        ExportTable("culture_entity_birth_field_events");
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
    }

    private void EnsureProcessSchema()
    {
        EnsureTable("trace_events", typeof(TraceRow));
        EnsureTable("movement_events", typeof(MovementEventRow));
    }

    private void EnsureWorldSchema()
    {
        EnsureTable("relationship_events", typeof(RelationshipEventRow));
        EnsureTable("gambit_events", typeof(GambitEventRow));
        EnsureTable("generator_events", typeof(GeneratorEventRow));
        EnsureTable("generator_outputs", typeof(GeneratorOutputRow));
        EnsureTable("generated_entities", typeof(GeneratedEntityRow));
        EnsureTable("lifecycle_events", typeof(LifecycleEventRow));
        EnsureTable("map_generation_events", typeof(MapGenerationEventRow));
        EnsureTable("map_cell_events", typeof(MapCellEventRow));
        EnsureTable("culture_field_cell_snapshots", typeof(CultureFieldCellSnapshotRow));
        EnsureTable("minor_actor_work_events", typeof(MinorActorWorkEventRow));
        EnsureTable("culture_entity_birth_field_events", typeof(CultureEntityBirthFieldEventRow));
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
        AppendBatch(_pending);
    }

    private void AppendBatch(TelemetryBatch batch)
    {
        AppendIdentityTables(batch);
        AppendProcessTables(batch);
        AppendWorldTables(batch);
    }

    private void AppendIdentityTables(TelemetryBatch batch)
    {
        AppendTable("symbols", batch.Symbols);
        AppendTable("entity_name_symbols", batch.EntityNameSymbols);
        AppendTable("runs", batch.Runs);
        AppendTable("tick_snapshots", batch.TickSnapshots);
        AppendTable("kind_snapshots", batch.KindSnapshots);
        AppendTable("entity_snapshots", batch.EntitySnapshots);
        AppendTable("entity_attribute_snapshots", batch.EntityAttributeSnapshots);
        AppendTable("entity_events", batch.EntityEvents);
    }

    private void AppendProcessTables(TelemetryBatch batch)
    {
        AppendTable("trace_events", batch.TraceEvents);
        AppendTable("movement_events", batch.MovementEvents);
    }

    private void AppendWorldTables(TelemetryBatch batch)
    {
        AppendTable("relationship_events", batch.RelationshipEvents);
        AppendTable("gambit_events", batch.GambitEvents);
        AppendTable("generator_events", batch.GeneratorEvents);
        AppendTable("generator_outputs", batch.GeneratorOutputs);
        AppendTable("generated_entities", batch.GeneratedEntities);
        AppendTable("lifecycle_events", batch.LifecycleEvents);
        AppendTable("map_generation_events", batch.MapGenerationEvents);
        AppendTable("map_cell_events", batch.MapCellEvents);
        AppendTable("culture_field_cell_snapshots", batch.CultureFieldCellSnapshots);
        AppendTable("minor_actor_work_events", batch.MinorActorWorkEvents);
        AppendTable("culture_entity_birth_field_events", batch.CultureEntityBirthFieldEvents);
        AppendTable("cell_road_events", batch.CellRoadEvents);
        AppendTable("map_snapshots", batch.MapSnapshots);
        AppendTable("coverage_snapshots", batch.CoverageSnapshots);
        AppendTable("coverage_terrain_snapshots", batch.CoverageTerrainSnapshots);
        AppendTable("placement_events", batch.PlacementEvents);
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
        ExportTableTo(tableName, _outputDirectory);
    }

    private void ExportTableTo(string tableName, string directory)
    {
        var parquetPath = Path.Combine(directory, $"{tableName}.parquet");
        SqlFileHelpers.Execute(
            _connection,
            $"COPY {SqlFileHelpers.QuoteIdentifier(tableName)} TO {SqlFileHelpers.QuoteString(parquetPath)} (FORMAT PARQUET);");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

internal sealed class BufferedDuckDbTelemetryWriter : IDisposable
{
    private readonly record struct WriterMessage(TelemetryBatch? Batch, bool ExportCore);

    private readonly string _outputDirectory;
    private readonly Channel<WriterMessage> _batches;
    private readonly SemaphoreSlim _inFlight = new(1, 1);
    private readonly Task _writerTask;
    private ExceptionDispatchInfo? _failure;
    private bool _completed;
    private bool _exported;
    private bool _disposed;

    public BufferedDuckDbTelemetryWriter(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        _outputDirectory = outputDirectory;
        _batches = Channel.CreateBounded<WriterMessage>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _writerTask = Task.Run(WriteBatchesAsync);
    }

    public void Append(TelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposedOrCompleted();
        ThrowIfFailed();
        if (TelemetryBatchExtensions.CountRows(batch) == 0)
        {
            return;
        }

        Post(new WriterMessage(batch, ExportCore: false));
    }

    /// <summary>
    /// Queues a mid-run export of the core scorecard tables to the run's
    /// stream/ directory. Runs on the writer thread after all previously
    /// appended batches; the simulation thread only blocks until the
    /// writer accepts the request.
    /// </summary>
    public void StreamCoreTables()
    {
        ThrowIfDisposedOrCompleted();
        ThrowIfFailed();
        Post(new WriterMessage(Batch: null, ExportCore: true));
    }

    private void Post(WriterMessage message)
    {
        _inFlight.Wait();
        try
        {
            ThrowIfFailed();
            _batches.Writer.WriteAsync(message).AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            _inFlight.Release();
            throw;
        }
    }

    public void WaitForIdle()
    {
        ThrowIfDisposedOrCompleted();
        ThrowIfFailed();
        _inFlight.Wait();
        _inFlight.Release();
        ThrowIfFailed();
    }

    public void CompleteAndExport()
    {
        ThrowIfDisposed();
        CompleteWriter();
        if (_exported)
        {
            return;
        }

        using var store = new DuckDbTelemetryStore(_outputDirectory);
        store.ExportParquet();
        _exported = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CompleteWriter();
        _disposed = true;
        _inFlight.Dispose();
    }

    private async Task WriteBatchesAsync()
    {
        try
        {
            using var store = new DuckDbTelemetryStore(_outputDirectory);
            await foreach (var message in _batches.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    if (message.Batch is not null)
                    {
                        store.AppendOwned(message.Batch);
                    }

                    if (message.ExportCore)
                    {
                        store.ExportCoreParquet();
                    }
                }
                finally
                {
                    _inFlight.Release();
                }
            }

            store.Flush();
        }
        catch (Exception ex)
        {
            _failure = ExceptionDispatchInfo.Capture(ex);
            _batches.Writer.TryComplete(ex);
            if (_inFlight.CurrentCount == 0)
            {
                _inFlight.Release();
            }

            throw;
        }
    }

    private void CompleteWriter()
    {
        if (!_completed)
        {
            _completed = true;
            _batches.Writer.TryComplete();
        }

        _writerTask.GetAwaiter().GetResult();
        ThrowIfFailed();
    }

    private void ThrowIfFailed()
    {
        _failure?.Throw();
    }

    private void ThrowIfDisposedOrCompleted()
    {
        ThrowIfDisposed();
        if (_completed)
        {
            throw new InvalidOperationException("Telemetry writer is already complete.");
        }
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
        batch.EntityEvents.Count;

    private static long CountProcessRows(TelemetryBatch batch) =>
        batch.TraceEvents.Count +
        batch.MovementEvents.Count;

    private static long CountWorldRows(TelemetryBatch batch) =>
        batch.RelationshipEvents.Count +
        batch.GambitEvents.Count +
        batch.GeneratorEvents.Count +
        batch.GeneratorOutputs.Count +
        batch.GeneratedEntities.Count +
        batch.LifecycleEvents.Count +
        batch.MapGenerationEvents.Count +
        batch.MapCellEvents.Count +
        batch.CultureFieldCellSnapshots.Count +
        batch.MinorActorWorkEvents.Count +
        batch.CultureEntityBirthFieldEvents.Count +
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
    }

    private static void AppendProcessRows(TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.TraceEvents, source.TraceEvents);
        AppendRows(destination.MovementEvents, source.MovementEvents);
    }

    private static void AppendWorldRows(TelemetryBatch destination, TelemetryBatch source)
    {
        AppendRows(destination.RelationshipEvents, source.RelationshipEvents);
        AppendRows(destination.GambitEvents, source.GambitEvents);
        AppendRows(destination.GeneratorEvents, source.GeneratorEvents);
        AppendRows(destination.GeneratorOutputs, source.GeneratorOutputs);
        AppendRows(destination.GeneratedEntities, source.GeneratedEntities);
        AppendRows(destination.LifecycleEvents, source.LifecycleEvents);
        AppendRows(destination.MapGenerationEvents, source.MapGenerationEvents);
        AppendRows(destination.MapCellEvents, source.MapCellEvents);
        AppendRows(destination.CultureFieldCellSnapshots, source.CultureFieldCellSnapshots);
        AppendRows(destination.MinorActorWorkEvents, source.MinorActorWorkEvents);
        AppendRows(destination.CultureEntityBirthFieldEvents, source.CultureEntityBirthFieldEvents);
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
