namespace WorldGeneratorTool;

using System.Diagnostics;
using System.Globalization;

using Engine.Host;

using IceRemembers.Seed;

internal static class WorldGeneratorCli
{
    public static int Run(string[] args)
    {
        if (!TryReadOptions(args, out var baseSeed, out var outputPath))
        {
            PrintUsage();
            return 2;
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        Console.WriteLine($"Generating Ice Remembers world for base seed {baseSeed}...");
        var generationStopwatch = Stopwatch.StartNew();
        var startupSeed = IceRemembersKernelDomainModule.Instance.BuildStartupSeed(
            new DomainStartupSeedRequest(baseSeed));
        generationStopwatch.Stop();
        Console.WriteLine(
            $"Generated accepted seed {startupSeed.Identity.BaseSeed}/{startupSeed.Identity.Attempt} " +
            $"in {generationStopwatch.Elapsed.TotalSeconds:0.00}s. Writing {fullOutputPath}...");

        var writeStopwatch = Stopwatch.StartNew();
        IceRemembersPregeneratedWorldStore.Write(fullOutputPath, startupSeed);
        writeStopwatch.Stop();
        Console.WriteLine(
            $"Wrote {new FileInfo(fullOutputPath).Length / (1024d * 1024d):0.0} MB " +
            $"in {writeStopwatch.Elapsed.TotalSeconds:0.00}s.");
        return 0;
    }

    private static bool TryReadOptions(
        string[] args,
        out int baseSeed,
        out string outputPath)
    {
        const string seedPrefix = "--seed=";
        const string outputPrefix = "--output=";
        baseSeed = 0;
        outputPath = string.Empty;

        foreach (var argument in args)
        {
            if (argument.StartsWith(seedPrefix, StringComparison.Ordinal))
            {
                _ = int.TryParse(
                    argument.AsSpan(seedPrefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out baseSeed);
            }
            else if (argument.StartsWith(outputPrefix, StringComparison.Ordinal))
            {
                outputPath = argument[outputPrefix.Length..];
            }
            else
            {
                return false;
            }
        }

        return args.Length == 2 &&
            baseSeed > 0 &&
            !string.IsNullOrWhiteSpace(outputPath) &&
            string.Equals(Path.GetExtension(outputPath), ".world", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintUsage() =>
        Console.Error.WriteLine(
            "Usage: dotnet run --project tools/WorldGenerator -c Release -- " +
            "--seed=<positive integer> --output=<path.world>");
}
