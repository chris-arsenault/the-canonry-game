namespace KernelProfile;

using System.Diagnostics;
using System.Globalization;

using Engine.Host;

using IceRemembers.Seed;

internal static class Program
{
    private static readonly KernelRuntimeFactory RuntimeFactory = IceRemembersRuntime.CreateRuntimeFactory();

    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        try
        {
            var options = KernelProfileOptions.Parse(args);
            Run(options);
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static void Run(KernelProfileOptions options)
    {
        var elapsed = Stopwatch.StartNew();
        long processedTicks = 0;

        for (var runIndex = 0; runIndex < options.Runs; runIndex++)
        {
            var seed = options.Seed + runIndex;
            using var runtime = StartRuntime(seed, options);
            var runTicks = AdvanceTicks(runtime.Host, options.Ticks);
            processedTicks += runTicks;

            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"run {runIndex + 1}/{options.Runs}: seed={seed} ticks={runTicks}"));
        }

        elapsed.Stop();
        var ticksPerSecond = processedTicks / Math.Max(elapsed.Elapsed.TotalSeconds, double.Epsilon);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"profile complete: runs={options.Runs} ticks={processedTicks} elapsed_seconds={elapsed.Elapsed.TotalSeconds:G17} ticks_per_second={ticksPerSecond:G17}"));
    }

    private static KernelRuntime StartRuntime(int seed, KernelProfileOptions options) =>
        RuntimeFactory.Start(new KernelStartRequest(
            IceRemembersRuntime.DomainName,
            StartupSeedPolicy.Requested(seed),
            KernelFunctionProfile.Batch,
            options.TraceEnabled,
            options.ConfigFilePath));

    private static int AdvanceTicks(KernelHost host, int requestedTicks)
    {
        ArgumentNullException.ThrowIfNull(host);

        var processedTicks = 0;
        for (var tickIndex = 0; tickIndex < requestedTicks; tickIndex++)
        {
            var advanced = host.Time.AdvanceSingleStep(1);
            if (advanced == 0)
            {
                break;
            }

            processedTicks += advanced;
        }

        return processedTicks;
    }
}

internal sealed partial record KernelProfileOptions(
    int Runs,
    int Ticks,
    int Seed,
    bool TraceEnabled,
    string? ConfigFilePath)
{
    public static KernelProfileOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return new KernelProfileOptions(
            Runs: ReadInt(args, "--runs", 50),
            Ticks: ReadInt(args, "--ticks", 500),
            Seed: ReadInt(args, "--seed", 1000),
            TraceEnabled: HasFlag(args, "--trace"),
            ConfigFilePath: ReadString(args, "--config-file", null));
    }

    private static int ReadInt(string[] args, string name, int fallback)
    {
        var value = ReadString(args, name, null);
        if (value is null)
        {
            return fallback;
        }

        var parsed = int.Parse(value, CultureInfo.InvariantCulture);
        if (parsed <= 0)
        {
            throw new ArgumentOutOfRangeException(name, parsed, $"{name} must be greater than zero.");
        }

        return parsed;
    }

    private static string? ReadString(string[] args, string name, string? fallback)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"{name} requires a value.", nameof(args));
            }

            return args[i + 1];
        }

        return fallback;
    }

    private static bool HasFlag(string[] args, string name) =>
        args.Any(arg => string.Equals(arg, name, StringComparison.Ordinal));
}
