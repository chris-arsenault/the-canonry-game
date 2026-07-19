using WorldGeneratorTool;

return args.Length > 0 && string.Equals(args[0], "capture", StringComparison.Ordinal)
    ? WorldCaptureCli.Run(args[1..])
    : WorldGeneratorCli.Run(args);
