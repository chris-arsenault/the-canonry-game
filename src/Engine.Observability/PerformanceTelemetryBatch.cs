namespace Engine.Observability;

using System.Collections.ObjectModel;

public sealed class PerformanceTelemetryBatch
{
    public Collection<SymbolRow> Symbols { get; } = [];
    public Collection<RunPerformanceRow> RunPerformance { get; } = [];
    public Collection<TickPerformanceRow> TickPerformance { get; } = [];
    public Collection<SystemPerformanceRow> SystemPerformance { get; } = [];
    public Collection<ExportPerformanceRow> ExportPerformance { get; } = [];
}

public sealed record RunPerformanceRow(
    int RunId,
    int? Seed,
    int DomainId,
    long StartedUnixMilliseconds,
    long CompletedUnixMilliseconds,
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
    int RunId,
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
    int RunId,
    int Tick,
    int PhaseId,
    int SystemId,
    double ElapsedMilliseconds);

public sealed record ExportPerformanceRow(
    long ExportId,
    int StepId,
    long StartedUnixMilliseconds,
    long CompletedUnixMilliseconds,
    double ElapsedMilliseconds,
    long RowsWritten);
