namespace Engine.Observability.Export;

using System.Collections.ObjectModel;

using DuckDB.NET.Data;

using Engine.Constants;
using Engine.Observability;

internal sealed class DuckDbPerformanceTelemetryStore : IDisposable
{
    private const long DefaultFlushRowThreshold = 250_000;

    private readonly string _outputDirectory;
    private readonly DuckDBConnection _connection;
    private readonly long _flushRowThreshold;
    private PerformanceTelemetryBatch _pending = new();
    private long _pendingRows;
    private bool _disposed;

    public DuckDbPerformanceTelemetryStore(string outputDirectory, long flushRowThreshold = DefaultFlushRowThreshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentOutOfRangeException.ThrowIfNegative(flushRowThreshold);

        _outputDirectory = outputDirectory;
        var databasePath = Path.Combine(outputDirectory, "performance.duckdb");
        _flushRowThreshold = flushRowThreshold;
        Directory.CreateDirectory(_outputDirectory);

        _connection = new DuckDBConnection($"Data Source={databasePath}");
        _connection.Open();
        EnsureSchema();
    }

    public void Append(PerformanceTelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposed();

        var rowCount = PerformanceTelemetryBatchExtensions.CountRows(batch);
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

    public void AppendExport(ExportPerformanceRow row, string stepName)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ThrowIfDisposed();

        _pending.Symbols.Add(TelemetrySymbolIds.CreateWithId(
            TelemetrySymbolKinds.PerformanceExportStep,
            row.StepId,
            stepName,
            stepName));
        _pending.ExportPerformance.Add(row);
        _pendingRows += 2;
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
        _pending = new PerformanceTelemetryBatch();
        _pendingRows = 0;
    }

    public void ExportCoreParquet()
    {
        ThrowIfDisposed();
        Flush();

        ExportTable("performance_symbols");
        ExportTable("run_perf");
        ExportTable("tick_perf");
        ExportTable("system_perf");
    }

    public void ExportExportPerformanceParquet()
    {
        ThrowIfDisposed();
        Flush();

        ExportTable("export_perf");
    }

    public void ExportParquet()
    {
        ExportCoreParquet();
        ExportExportPerformanceParquet();
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
        EnsureTable("performance_symbols", typeof(SymbolRow));
        EnsureTable("run_perf", typeof(RunPerformanceRow));
        EnsureTable("tick_perf", typeof(TickPerformanceRow));
        EnsureTable("system_perf", typeof(SystemPerformanceRow));
        EnsureTable("export_perf", typeof(ExportPerformanceRow));
    }

    private void EnsureTable(string tableName, Type rowType)
    {
        var properties = DuckDbRowSchema.PropertiesFor(rowType);
        SqlFileHelpers.Execute(_connection, SqlFileHelpers.CreateTableSql(tableName, properties, ifNotExists: true));
    }

    private void AppendPending()
    {
        AppendTable("performance_symbols", _pending.Symbols);
        AppendTable("run_perf", _pending.RunPerformance);
        AppendTable("tick_perf", _pending.TickPerformance);
        AppendTable("system_perf", _pending.SystemPerformance);
        AppendTable("export_perf", _pending.ExportPerformance);
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

internal static class PerformanceTelemetryBatchExtensions
{
    public static long CountRows(this PerformanceTelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return batch.Symbols.Count +
               batch.RunPerformance.Count +
               batch.TickPerformance.Count +
               batch.SystemPerformance.Count +
               batch.ExportPerformance.Count;
    }

    public static void Append(this PerformanceTelemetryBatch destination, PerformanceTelemetryBatch source)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);

        AppendRows(destination.Symbols, source.Symbols);
        AppendRows(destination.RunPerformance, source.RunPerformance);
        AppendRows(destination.TickPerformance, source.TickPerformance);
        AppendRows(destination.SystemPerformance, source.SystemPerformance);
        AppendRows(destination.ExportPerformance, source.ExportPerformance);
    }

    private static void AppendRows<T>(Collection<T> destination, Collection<T> source)
    {
        foreach (var row in source)
        {
            destination.Add(row);
        }
    }
}
