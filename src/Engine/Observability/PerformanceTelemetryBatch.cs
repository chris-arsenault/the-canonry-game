namespace Engine.Observability;

using System.Collections.ObjectModel;

public sealed class PerformanceTelemetryBatch
{
    public Collection<RunPerformanceRow> RunPerformance { get; } = [];
    public Collection<TickPerformanceRow> TickPerformance { get; } = [];
    public Collection<SystemPerformanceRow> SystemPerformance { get; } = [];
    public Collection<ExportPerformanceRow> ExportPerformance { get; } = [];
}

public sealed record RunPerformanceRow(
    string RunId,
    int? Seed,
    string Domain,
    string StartedUtc,
    string CompletedUtc,
    int RequestedTicks,
    int ProcessedTicks,
    double TotalSimulationMilliseconds,
    double AverageSampledTickMilliseconds,
    double P95SampledTickMilliseconds,
    double MaxSampledTickMilliseconds,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    long PeakManagedMemoryBytes,
    int TickSampleCadence,
    int SystemSampleCadence,
    bool FullTelemetryEnabled);

public sealed record TickPerformanceRow(
    string RunId,
    int Tick,
    double ElapsedMilliseconds,
    int ActiveEntities,
    int ActiveRelationships,
    int ProcessEvents,
    int ProcessCandidates,
    int GeneratorEvents,
    int TotalMessages,
    long ManagedMemoryBytes);

public sealed record SystemPerformanceRow(
    string RunId,
    int Tick,
    string Phase,
    string SystemName,
    double ElapsedMilliseconds);

public sealed record ExportPerformanceRow(
    string ExportId,
    string Step,
    string StartedUtc,
    string CompletedUtc,
    double ElapsedMilliseconds,
    long RowsWritten);
