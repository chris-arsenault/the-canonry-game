namespace MonteCarloTool;

using System.Collections.ObjectModel;
using System.Reflection;

using DuckDB.NET.Data;

using Engine.Observability;

internal sealed class DuckDbPerformanceTelemetryStore : IDisposable
{
    private readonly string _outputDirectory;
    private readonly string _databasePath;
    private readonly DuckDBConnection _connection;
    private bool _disposed;

    public DuckDbPerformanceTelemetryStore(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        _outputDirectory = outputDirectory;
        _databasePath = Path.Combine(outputDirectory, "performance.duckdb");
        Directory.CreateDirectory(_outputDirectory);

        _connection = new DuckDBConnection($"Data Source={_databasePath}");
        _connection.Open();
        EnsureSchema();
    }

    public void Append(PerformanceTelemetryBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ThrowIfDisposed();

        AppendTable("run_perf", batch.RunPerformance);
        AppendTable("tick_perf", batch.TickPerformance);
        AppendTable("system_perf", batch.SystemPerformance);
        AppendTable("export_perf", batch.ExportPerformance);
    }

    public void AppendExport(ExportPerformanceRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        ThrowIfDisposed();

        var rows = new Collection<ExportPerformanceRow> { row };
        AppendTable("export_perf", rows);
    }

    public void ExportCoreParquet()
    {
        ThrowIfDisposed();

        ExportTable("run_perf");
        ExportTable("tick_perf");
        ExportTable("system_perf");
    }

    public void ExportExportPerformanceParquet()
    {
        ThrowIfDisposed();

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
        _disposed = true;
        _connection.Dispose();
    }

    private void EnsureSchema()
    {
        EnsureTable("run_perf", typeof(RunPerformanceRow));
        EnsureTable("tick_perf", typeof(TickPerformanceRow));
        EnsureTable("system_perf", typeof(SystemPerformanceRow));
        EnsureTable("export_perf", typeof(ExportPerformanceRow));
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
