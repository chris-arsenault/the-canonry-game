namespace MonteCarloTool;

using System.Diagnostics;
using System.Globalization;

internal sealed record MonteCarloProvenanceSnapshot(
    string? GitHead,
    bool GitDirty,
    string CommandLine,
    long SourceNewestUtcMilliseconds,
    long AssemblyNewestUtcMilliseconds,
    bool BuildFresh);

internal static class MonteCarloProvenance
{
    private static readonly string[] SourceRoots =
    [
        Path.Combine("tools", "MonteCarlo"),
        Path.Combine("src", "Engine.Constants"),
        Path.Combine("src", "Engine.Domain"),
        Path.Combine("src", "Engine.Eventing"),
        Path.Combine("src", "Engine.Gameplay"),
        Path.Combine("src", "Engine.Host"),
        Path.Combine("src", "Engine.Kernel"),
        Path.Combine("src", "Engine.Kernel.Driver"),
        Path.Combine("src", "Engine.Observability"),
        Path.Combine("src", "Engine.Observability.Export"),
        Path.Combine("src", "Engine.WorldGen"),
        Path.Combine("src", "IceRemembers"),
        Path.Combine("src", "IceRemembers.Seed"),
    ];

    private static readonly string[] SourcePatterns = ["*.cs", "*.csproj", "*.props", "*.targets", "*.json", "*.toml"];

    public static MonteCarloProvenanceSnapshot Capture(string[] args)
    {
        var sourceNewest = NewestSourceTimestamp();
        var assemblyNewest = NewestAssemblyTimestamp();
        return new MonteCarloProvenanceSnapshot(
            GitHead: Git("rev-parse", "HEAD"),
            GitDirty: !string.IsNullOrWhiteSpace(Git("status", "--porcelain")),
            CommandLine: string.Join(' ', args.Select(EscapeArgument)),
            SourceNewestUtcMilliseconds: sourceNewest,
            AssemblyNewestUtcMilliseconds: assemblyNewest,
            BuildFresh: assemblyNewest >= sourceNewest);
    }

    public static void ThrowIfBuildIsStale(MonteCarloProvenanceSnapshot provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        if (provenance.BuildFresh)
        {
            return;
        }

        var source = DateTimeOffset
            .FromUnixTimeMilliseconds(provenance.SourceNewestUtcMilliseconds)
            .UtcDateTime
            .ToString("O", CultureInfo.InvariantCulture);
        var assembly = DateTimeOffset
            .FromUnixTimeMilliseconds(provenance.AssemblyNewestUtcMilliseconds)
            .UtcDateTime
            .ToString("O", CultureInfo.InvariantCulture);
        throw new InvalidOperationException(
            "Monte Carlo build appears stale. Run without --no-build before using MC evidence. " +
            $"newest_source_utc={source} newest_assembly_utc={assembly}");
    }

    private static long NewestSourceTimestamp()
    {
        var newest = DateTimeOffset.MinValue;
        foreach (var root in SourceRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var pattern in SourcePatterns)
            {
                foreach (var path in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
                {
                    newest = Max(newest, File.GetLastWriteTimeUtc(path));
                }
            }
        }

        foreach (var path in new[] { "Directory.Build.props", "Directory.Build.targets", "global.json" })
        {
            if (File.Exists(path))
            {
                newest = Max(newest, File.GetLastWriteTimeUtc(path));
            }
        }

        return newest == DateTimeOffset.MinValue
            ? 0
            : new DateTimeOffset(newest.UtcDateTime).ToUnixTimeMilliseconds();
    }

    private static long NewestAssemblyTimestamp()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (!Directory.Exists(baseDirectory))
        {
            return 0;
        }

        var newest = Directory
            .EnumerateFiles(baseDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
        return newest == DateTime.MinValue
            ? 0
            : new DateTimeOffset(DateTime.SpecifyKind(newest, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
    }

    private static DateTimeOffset Max(DateTimeOffset left, DateTime right)
    {
        var offset = new DateTimeOffset(DateTime.SpecifyKind(right, DateTimeKind.Utc));
        return offset > left ? offset : left;
    }

    private static string? Git(params string[] args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static string EscapeArgument(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"" : value;
}
