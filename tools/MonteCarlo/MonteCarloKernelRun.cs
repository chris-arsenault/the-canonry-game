namespace MonteCarloTool;

using Engine.Eventing;
using Engine.Host;
using Engine.Projection;

internal sealed record MonteCarloKernelRun(
    KernelHost Host,
    IProjectionSymbolCatalog? Symbols,
    MonteCarloWorldSeed? WorldSeed)
{
    public KernelEventLog Events => Host.Events;

    public void Dispose() => Host.Dispose();
}

internal readonly record struct MonteCarloWorldSeed(
    int BaseSeed,
    int Attempt,
    int TerrainSeed,
    int EntitySeed);

internal readonly record struct MonteCarloRunExecutionResult(
    long StateRowsWritten,
    long PerformanceRowsWritten,
    int ProcessedTicks,
    MonteCarloRunSummary Summary);

internal readonly record struct MonteCarloRunSummary(
    int GeneratorFireCount,
    int? MapAttempt,
    long DroppedSummaryEvents);
