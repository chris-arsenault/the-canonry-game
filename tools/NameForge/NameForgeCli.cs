namespace NameForgeTool;

using NameForge.Runtime;

internal static class NameForgeCli
{
    public static int Run(string[] args)
    {
        var options = NameForgeCliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var project = NameForgeProject.LoadFile(options.ProjectPath);
            var generator = new NameForgeGenerator(project);

            if (options.ListCultures)
            {
                PrintCultures(project);
                return 0;
            }

            if (options.ListProfiles)
            {
                PrintProfiles(project, options.CultureId);
                return 0;
            }

            if (!project.Cultures.ContainsKey(options.CultureId))
            {
                Console.Error.WriteLine($"Unknown culture '{options.CultureId}'. Use --list-cultures to inspect the project.");
                return 2;
            }

            var names = string.IsNullOrWhiteSpace(options.DomainId)
                ? GenerateNames(generator, options)
                : generator.GenerateFromDomain(options.CultureId, options.DomainId, options.Count, options.Seed);

            foreach (var name in names)
            {
                Console.WriteLine(name);
            }

            if (options.Debug && string.IsNullOrWhiteSpace(options.DomainId))
            {
                PrintDebug(generator, options);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static IReadOnlyList<string> GenerateNames(NameForgeGenerator generator, NameForgeCliOptions options)
    {
        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = options.CultureId,
            ProfileId = options.ProfileId,
            Kind = options.Kind,
            Subtype = options.Subtype,
            Prominence = options.Prominence,
            Count = options.Count,
            Seed = options.Seed,
            Tags = options.Tags,
            Context = options.Context,
        });

        return result.Names;
    }

    private static void PrintDebug(NameForgeGenerator generator, NameForgeCliOptions options)
    {
        var result = generator.Generate(new NameForgeGenerateRequest
        {
            CultureId = options.CultureId,
            ProfileId = options.ProfileId,
            Kind = options.Kind,
            Subtype = options.Subtype,
            Prominence = options.Prominence,
            Count = options.Count,
            Seed = options.Seed,
            Tags = options.Tags,
            Context = options.Context,
        });

        Console.WriteLine();
        Console.WriteLine("Strategy usage:");
        foreach (var (strategy, count) in result.StrategyUsage)
        {
            Console.WriteLine($"  {strategy}: {count}");
        }

        if (result.DebugInfo.Count > 0)
        {
            var first = result.DebugInfo[0];
            Console.WriteLine();
            Console.WriteLine($"First name group: {first.GroupUsed}");
            Console.WriteLine($"First name strategy: {first.StrategyUsed}");
        }
    }

    private static void PrintCultures(NameForgeProject project)
    {
        foreach (var culture in project.Cultures.Values.OrderBy(static culture => culture.Id, StringComparer.Ordinal))
        {
            Console.WriteLine($"{culture.Id}\t{culture.Name}");
        }
    }

    private static void PrintProfiles(NameForgeProject project, string cultureId)
    {
        if (!project.Cultures.TryGetValue(cultureId, out var culture))
        {
            Console.Error.WriteLine($"Unknown culture '{cultureId}'.");
            return;
        }

        foreach (var profile in culture.Profiles)
        {
            Console.WriteLine($"{profile.Id}\t{string.Join(",", profile.EntityKinds)}\t{profile.StrategyGroups.Count} groups");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/NameForge -- --culture wake_singers --kind orca --subtype scout --count 10 --seed demo");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --project <path>      Name Forge project JSON or manifest directory. Defaults to src/IceRemembers/Names/ice-remembers.nameforge.json.");
        Console.WriteLine("  --culture <id>        Culture id.");
        Console.WriteLine("  --kind <kind>         Entity kind.");
        Console.WriteLine("  --subtype <subtype>   Entity subtype.");
        Console.WriteLine("  --profile <id>        Optional profile id.");
        Console.WriteLine("  --domain <id>         Generate directly from a phonotactic domain.");
        Console.WriteLine("  --count <n>           Number of names. Default: 10.");
        Console.WriteLine("  --seed <seed>         Deterministic seed.");
        Console.WriteLine("  --tag <tag>           Add a tag. Repeatable.");
        Console.WriteLine("  --context <key=value> Add a context value. Repeatable.");
        Console.WriteLine("  --list-cultures       Print cultures in the project.");
        Console.WriteLine("  --list-profiles       Print profiles for --culture.");
        Console.WriteLine("  --debug               Print strategy summary.");
    }
}

internal sealed class NameForgeCliOptions
{
    public string ProjectPath { get; private init; } = DefaultProjectPath();
    public string CultureId { get; private init; } = "wake_singers";
    public string Kind { get; private init; } = "orca";
    public string Subtype { get; private init; } = "scout";
    public string ProfileId { get; private init; } = string.Empty;
    public string DomainId { get; private init; } = string.Empty;
    public string Prominence { get; private init; } = "recognized";
    public int Count { get; private init; } = 10;
    public string Seed { get; private init; } = "name-forge-cli";
    public List<string> Tags { get; private init; } = [];
    public Dictionary<string, string> Context { get; private init; } = new(StringComparer.Ordinal);
    public bool ListCultures { get; private init; }
    public bool ListProfiles { get; private init; }
    public bool Debug { get; private init; }
    public bool ShowHelp { get; private init; }

    public static NameForgeCliOptions Parse(IReadOnlyList<string> args)
    {
        var options = new MutableOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;
                case "--project":
                    options.ProjectPath = ReadValue(args, ref i, arg);
                    break;
                case "--culture":
                    options.CultureId = ReadValue(args, ref i, arg);
                    break;
                case "--kind":
                    options.Kind = ReadValue(args, ref i, arg);
                    break;
                case "--subtype":
                    options.Subtype = ReadValue(args, ref i, arg);
                    break;
                case "--profile":
                    options.ProfileId = ReadValue(args, ref i, arg);
                    break;
                case "--domain":
                    options.DomainId = ReadValue(args, ref i, arg);
                    break;
                case "--prominence":
                    options.Prominence = ReadValue(args, ref i, arg);
                    break;
                case "--count":
                    options.Count = int.TryParse(ReadValue(args, ref i, arg), out var count) ? Math.Max(1, count) : 10;
                    break;
                case "--seed":
                    options.Seed = ReadValue(args, ref i, arg);
                    break;
                case "--tag":
                    options.Tags.Add(ReadValue(args, ref i, arg));
                    break;
                case "--context":
                    options.AddContext(ReadValue(args, ref i, arg));
                    break;
                case "--list-cultures":
                    options.ListCultures = true;
                    break;
                case "--list-profiles":
                    options.ListProfiles = true;
                    break;
                case "--debug":
                    options.Debug = true;
                    break;
            }
        }

        return options.ToImmutable();
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }

    private static string DefaultProjectPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "src", "IceRemembers", "Names", "ice-remembers.nameforge.json"),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "src", "IceRemembers", "Names", "ice-remembers.nameforge.json")),
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private sealed class MutableOptions
    {
        public string ProjectPath { get; set; } = DefaultProjectPath();
        public string CultureId { get; set; } = "wake_singers";
        public string Kind { get; set; } = "orca";
        public string Subtype { get; set; } = "scout";
        public string ProfileId { get; set; } = string.Empty;
        public string DomainId { get; set; } = string.Empty;
        public string Prominence { get; set; } = "recognized";
        public int Count { get; set; } = 10;
        public string Seed { get; set; } = "name-forge-cli";
        public List<string> Tags { get; } = [];
        public Dictionary<string, string> Context { get; } = new(StringComparer.Ordinal);
        public bool ListCultures { get; set; }
        public bool ListProfiles { get; set; }
        public bool Debug { get; set; }
        public bool ShowHelp { get; set; }

        public NameForgeCliOptions ToImmutable() =>
            new()
            {
                ProjectPath = ProjectPath,
                CultureId = CultureId,
                Kind = Kind,
                Subtype = Subtype,
                ProfileId = ProfileId,
                DomainId = DomainId,
                Prominence = Prominence,
                Count = Count,
                Seed = Seed,
                Tags = Tags,
                Context = Context,
                ListCultures = ListCultures,
                ListProfiles = ListProfiles,
                Debug = Debug,
                ShowHelp = ShowHelp,
            };

        public void AddContext(string value)
        {
            var parsed = ParseContextValue(value);
            Context[parsed.Key] = parsed.Value;
        }

        private static KeyValuePair<string, string> ParseContextValue(string value)
        {
            var separator = value.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0 || separator == value.Length - 1)
            {
                throw new ArgumentException("Context values must be formatted as key=value.");
            }

            return new KeyValuePair<string, string>(value[..separator], value[(separator + 1)..]);
        }
    }
}
