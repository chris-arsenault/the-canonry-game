namespace MonteCarloTool;

using System.Globalization;

internal sealed record MonteCarloOptions(
    int Runs,
    int Ticks,
    int Seed,
    int EntitySnapshotCadence,
    int PerformanceSampleCadence,
    int SystemPerformanceSampleCadence,
    string OutputDirectory,
    string? AnalyzeDirectory,
    bool SkipAnalysis,
    bool PerformanceMode,
    bool IncludeStateTelemetry,
    bool DisableEntitySnapshots,
    bool TraceEnabled)
{
    public bool AnalyzeOnly => AnalyzeDirectory is not null;
    public bool CollectStateTelemetry => !PerformanceMode || IncludeStateTelemetry || TraceEnabled;
    public bool ShouldAnalyze => !SkipAnalysis && CollectStateTelemetry;

    public static MonteCarloOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var analyzeDirectory = ReadString(args, "--analyze", null);
        var output = ReadString(
            args,
            "--out",
            Path.Combine(
                ".artifacts",
                "monte-carlo",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture)))!;

        return new MonteCarloOptions(
            Runs: ReadInt(args, "--runs", 10),
            Ticks: ReadInt(args, "--ticks", 500),
            Seed: ReadInt(args, "--seed", 1000),
            EntitySnapshotCadence: ReadInt(args, "--entity-snapshot-cadence", 25),
            PerformanceSampleCadence: ReadInt(args, "--perf-sample-cadence", 1),
            SystemPerformanceSampleCadence: ReadInt(args, "--system-perf-sample-cadence", 1),
            OutputDirectory: output,
            AnalyzeDirectory: analyzeDirectory,
            SkipAnalysis: HasFlag(args, "--no-analysis"),
            PerformanceMode: HasFlag(args, "--perf"),
            IncludeStateTelemetry: HasFlag(args, "--include-state-telemetry"),
            DisableEntitySnapshots: HasFlag(args, "--disable-entity-snapshots"),
            TraceEnabled: HasFlag(args, "--trace"));
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

    private static bool HasFlag(string[] args, string name) =>
        args.Any(arg => string.Equals(arg, name, StringComparison.Ordinal));
}
