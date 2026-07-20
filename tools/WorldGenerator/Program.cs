using WorldGeneratorTool;

if (args.Length > 0 && string.Equals(args[0], "capture", StringComparison.Ordinal))
{
    return WorldCaptureCli.Run(args[1..]);
}

if (args.Length > 0 && string.Equals(args[0], "diagnose", StringComparison.Ordinal))
{
    return WorldDiagnosticsCli.Run(args[1..]);
}

return WorldGeneratorCli.Run(args);
