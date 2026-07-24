namespace MonteCarloTool;

using System.Globalization;
using System.Text.RegularExpressions;

internal enum MonteCarloCommandKind
{
    Run,
    Register,
    Analyze,
    AppendStats,
    List,
    Show,
    DeleteRun,
}

internal sealed partial record MonteCarloOptions(
    MonteCarloCommandKind Command,
    string? RunId,
    int Runs,
    int Ticks,
    int Seed,
    int EntitySnapshotCadence,
    string ArtifactRoot,
    bool SkipAnalysis,
    bool DisableEntitySnapshots,
    bool SimulationOnly,
    bool TraceEnabled,
    string? ConfigFilePath,
    int ProgressEveryTicks,
    int StreamCoreEveryTicks,
    int SaveStateEveryTicks,
    IReadOnlyList<MonteCarloStatInput> Stats)
{
    public string OutputDirectory => RunId is null
        ? ArtifactRoot
        : Path.Combine(ArtifactRoot, RunId);

    public string RegistryPath => Path.Combine(ArtifactRoot, "registry.duckdb");
    public bool ShouldAnalyze => Command == MonteCarloCommandKind.Run && !SkipAnalysis && !SimulationOnly;

    public static MonteCarloOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var command = ParseCommand(args, out var optionArgs);
        RejectUnsupportedArguments(optionArgs);

        var runId = ReadString(optionArgs, "--run-id", null);
        if (runId is not null)
        {
            ValidateRunId(runId);
        }

        var artifactRoot = ReadString(
            optionArgs,
            "--artifact-root",
            Path.Combine(".artifacts", "monte-carlo"))!;
        var stats = ReadStats(optionArgs, command);
        var simulationOnly = HasFlag(optionArgs, "--simulation-only");
        var traceEnabled = HasFlag(optionArgs, "--trace");
        if (simulationOnly && traceEnabled)
        {
            throw new ArgumentException("--simulation-only cannot be combined with --trace.", nameof(args));
        }

        if (RequiresRunId(command) && runId is null)
        {
            throw new ArgumentException($"{CommandName(command)} requires --run-id.", nameof(args));
        }

        return new MonteCarloOptions(
            Command: command,
            RunId: runId,
            Runs: ReadInt(optionArgs, "--runs", 10),
            Ticks: ReadInt(optionArgs, "--ticks", 500),
            Seed: ReadInt(optionArgs, "--seed", 1000),
            EntitySnapshotCadence: ReadInt(optionArgs, "--entity-snapshot-cadence", 25),
            ArtifactRoot: artifactRoot,
            SkipAnalysis: HasFlag(optionArgs, "--no-analysis"),
            DisableEntitySnapshots: HasFlag(optionArgs, "--disable-entity-snapshots"),
            SimulationOnly: simulationOnly,
            TraceEnabled: traceEnabled,
            ConfigFilePath: ReadString(optionArgs, "--config-file", null),
            ProgressEveryTicks: ReadInt(optionArgs, "--progress-every", 5000),
            StreamCoreEveryTicks: ReadInt(optionArgs, "--stream-core-every", 0),
            SaveStateEveryTicks: ReadInt(optionArgs, "--save-state-every", 0),
            Stats: stats);
    }

    private static MonteCarloCommandKind ParseCommand(string[] args, out string[] optionArgs)
    {
        var aliasCommand = ParseCommandAlias(args);
        if (aliasCommand is not null)
        {
            optionArgs = args;
            return aliasCommand.Value;
        }

        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            optionArgs = args;
            return MonteCarloCommandKind.Run;
        }

        optionArgs = args[1..];
        return ParseCommandName(args[0]);
    }

    private static MonteCarloCommandKind? ParseCommandAlias(string[] args)
    {
        if (HasFlag(args, "--append-stats"))
        {
            return MonteCarloCommandKind.AppendStats;
        }

        if (HasFlag(args, "--no-run"))
        {
            return MonteCarloCommandKind.Register;
        }

        return null;
    }

    private static MonteCarloCommandKind ParseCommandName(string value)
    {
        return value switch
        {
            "run" => MonteCarloCommandKind.Run,
            "register" => MonteCarloCommandKind.Register,
            "analyze" => MonteCarloCommandKind.Analyze,
            "append-stats" => MonteCarloCommandKind.AppendStats,
            "list" => MonteCarloCommandKind.List,
            "show" => MonteCarloCommandKind.Show,
            "delete-run" => MonteCarloCommandKind.DeleteRun,
            _ => throw new ArgumentException($"Unknown Monte Carlo command '{value}'.", nameof(value)),
        };
    }

    private static void RejectUnsupportedArguments(string[] args)
    {
        if (ReadString(args, "--out", null) is not null)
        {
            throw new ArgumentException("Use --run-id and --artifact-root instead of --out.", nameof(args));
        }

        if (ReadString(args, "--analyze", null) is not null)
        {
            throw new ArgumentException("Use 'analyze --run-id <id>' instead of --analyze.", nameof(args));
        }

        if (HasFlag(args, "--perf") ||
            HasFlag(args, "--include-state-telemetry") ||
            ReadString(args, "--perf-sample-cadence", null) is not null ||
            ReadString(args, "--system-perf-sample-cadence", null) is not null)
        {
            throw new ArgumentException(
                "Monte Carlo --perf mode has been removed. Use docs/methodology/performance-testing.md for dotnet-trace profiling.",
                nameof(args));
        }
    }

    private static bool RequiresRunId(MonteCarloCommandKind command) =>
        command is
            MonteCarloCommandKind.Run or
            MonteCarloCommandKind.Register or
            MonteCarloCommandKind.Analyze or
            MonteCarloCommandKind.AppendStats or
            MonteCarloCommandKind.Show or
            MonteCarloCommandKind.DeleteRun;

    private static string CommandName(MonteCarloCommandKind command) =>
        command switch
        {
            MonteCarloCommandKind.Run => "run",
            MonteCarloCommandKind.Register => "register",
            MonteCarloCommandKind.Analyze => "analyze",
            MonteCarloCommandKind.AppendStats => "append-stats",
            MonteCarloCommandKind.List => "list",
            MonteCarloCommandKind.Show => "show",
            MonteCarloCommandKind.DeleteRun => "delete-run",
            _ => command.ToString(),
        };

    private static void ValidateRunId(string runId)
    {
        if (!RunIdRegex().IsMatch(runId))
        {
            throw new ArgumentException(
                "Run id must be 1-80 lowercase letters, digits, or dashes, and must start with a letter or digit.",
                nameof(runId));
        }
    }

    private static List<MonteCarloStatInput> ReadStats(string[] args, MonteCarloCommandKind command)
    {
        var stats = new List<MonteCarloStatInput>();
        var i = 0;
        while (i < args.Length)
        {
            if (string.Equals(args[i], "--stat", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("--stat requires name=value.", nameof(args));
                }

                stats.Add(ParseStat(args[i + 1]));
                i += 2;
                continue;
            }

            if (command == MonteCarloCommandKind.AppendStats &&
                !args[i].StartsWith("--", StringComparison.Ordinal) &&
                args[i].Contains('=', StringComparison.Ordinal))
            {
                stats.Add(ParseStat(args[i]));
            }

            i++;
        }

        if (command == MonteCarloCommandKind.AppendStats && stats.Count == 0)
        {
            throw new ArgumentException("append-stats requires at least one name=value stat.", nameof(args));
        }

        return stats;
    }

    private static MonteCarloStatInput ParseStat(string value)
    {
        var separator = value.IndexOf('=', StringComparison.Ordinal);
        if (separator <= 0)
        {
            throw new ArgumentException($"Invalid stat '{value}'. Use name=value.", nameof(value));
        }

        var name = value[..separator];
        if (!StatNameRegex().IsMatch(name))
        {
            throw new ArgumentException(
                $"Invalid stat name '{name}'. Use lowercase letters, digits, dots, dashes, or underscores.",
                nameof(value));
        }

        return new MonteCarloStatInput(name, value[(separator + 1)..]);
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

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,79}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex RunIdRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9_.-]{0,127}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex StatNameRegex();
}

internal readonly record struct MonteCarloStatInput(string Name, string Value);
