namespace NameForge.Runtime;

using System.Reflection;
using System.Text.Json;

public sealed class NameForgeProject
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public List<NameForgeProjectInclude> Includes { get; init; } = [];
    public Dictionary<string, NameForgeCulture> Cultures { get; init; } = new(StringComparer.Ordinal);

    public static NameForgeProject Load(Stream stream) =>
        Load(stream, openInclude: null);

    public static NameForgeProject Load(Stream stream, Func<string, Stream?>? openInclude)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var project = JsonSerializer.Deserialize<NameForgeProject>(stream, JsonOptions)
            ?? throw new JsonException("Name Forge project JSON did not contain a project object.");

        return ResolveIncludes(project, openInclude);
    }

    public static NameForgeProject LoadJson(string json) =>
        LoadJson(json, openInclude: null);

    public static NameForgeProject LoadJson(string json, Func<string, Stream?>? openInclude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var project = JsonSerializer.Deserialize<NameForgeProject>(json, JsonOptions)
            ?? throw new JsonException("Name Forge project JSON did not contain a project object.");

        return ResolveIncludes(project, openInclude);
    }

    public static NameForgeProject LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var projectPath = Directory.Exists(path)
            ? Path.Combine(path, "ice-remembers.nameforge.json")
            : path;
        var root = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? Environment.CurrentDirectory;

        using var stream = File.OpenRead(projectPath);
        return Load(stream, includePath => OpenRelativeInclude(root, includePath));
    }

    public static NameForgeProject LoadEmbeddedResource(
        Assembly assembly,
        string manifestResourceName,
        string includeResourcePrefix)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestResourceName);
        ArgumentNullException.ThrowIfNull(includeResourcePrefix);

        using var stream = assembly.GetManifestResourceStream(manifestResourceName)
            ?? throw new FileNotFoundException($"Embedded Name Forge project resource '{manifestResourceName}' was not found.");

        return Load(
            stream,
            includePath => OpenEmbeddedInclude(assembly, includeResourcePrefix, includePath));
    }

    private static Stream? OpenRelativeInclude(string root, string includePath)
    {
        if (string.IsNullOrWhiteSpace(includePath))
        {
            return null;
        }

        var normalized = includePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.IsPathFullyQualified(normalized)
            ? normalized
            : Path.GetFullPath(Path.Combine(root, normalized));

        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }

    private static NameForgeProject ResolveIncludes(
        NameForgeProject project,
        Func<string, Stream?>? openInclude)
    {
        if (project.Includes.Count == 0)
        {
            return project;
        }

        if (openInclude is null)
        {
            throw new JsonException("Name Forge project contains includes, but no include resolver was provided.");
        }

        foreach (var include in project.Includes)
        {
            LoadInclude(project, include, openInclude);
        }

        return project;
    }

    private static void LoadInclude(
        NameForgeProject project,
        NameForgeProjectInclude include,
        Func<string, Stream?> openInclude)
    {
        if (string.IsNullOrWhiteSpace(include.Path))
        {
            throw new JsonException("Name Forge include did not specify a path.");
        }

        using var stream = openInclude(include.Path)
            ?? throw new FileNotFoundException($"Name Forge include '{include.Path}' was not found.");

        switch (include.Type)
        {
            case "culture":
                AddCulture(project, DeserializeInclude<NameForgeCulture>(stream, include.Path));
                break;
            case "domain":
                CultureFor(project, include).Domains.Add(DeserializeInclude<NameForgeNamingDomain>(stream, include.Path));
                break;
            case "lexemeList":
                AddLexemeList(CultureFor(project, include), DeserializeInclude<NameForgeLexemeList>(stream, include.Path));
                break;
            case "grammar":
                CultureFor(project, include).Grammars.Add(DeserializeInclude<NameForgeGrammar>(stream, include.Path));
                break;
            case "profile":
                CultureFor(project, include).Profiles.Add(DeserializeInclude<NameForgeProfile>(stream, include.Path));
                break;
            default:
                throw new JsonException($"Name Forge include '{include.Path}' has unknown type '{include.Type}'.");
        }
    }

    private static T DeserializeInclude<T>(Stream stream, string path) =>
        JsonSerializer.Deserialize<T>(stream, JsonOptions)
            ?? throw new JsonException($"Name Forge include '{path}' did not contain a {typeof(T).Name} object.");

    private static void AddCulture(NameForgeProject project, NameForgeCulture culture)
    {
        if (string.IsNullOrWhiteSpace(culture.Id))
        {
            throw new JsonException("Name Forge culture include did not specify an id.");
        }

        project.Cultures[culture.Id] = culture;
    }

    private static void AddLexemeList(NameForgeCulture culture, NameForgeLexemeList lexemeList)
    {
        if (string.IsNullOrWhiteSpace(lexemeList.Id))
        {
            throw new JsonException($"Name Forge culture '{culture.Id}' included a lexeme list without an id.");
        }

        culture.LexemeLists[lexemeList.Id] = lexemeList;
    }

    private static NameForgeCulture CultureFor(NameForgeProject project, NameForgeProjectInclude include)
    {
        if (string.IsNullOrWhiteSpace(include.CultureId))
        {
            throw new JsonException($"Name Forge include '{include.Path}' did not specify a cultureId.");
        }

        if (!project.Cultures.TryGetValue(include.CultureId, out var culture))
        {
            throw new JsonException($"Name Forge include '{include.Path}' references unknown culture '{include.CultureId}'.");
        }

        return culture;
    }

    private static string NormalizeIncludePath(string includePath) =>
        includePath.Replace('\\', '/');

    private static Stream? OpenEmbeddedInclude(Assembly assembly, string includeResourcePrefix, string includePath)
    {
        var expected = includeResourcePrefix + NormalizeIncludePath(includePath);
        var stream = assembly.GetManifestResourceStream(expected);
        if (stream is not null)
        {
            return stream;
        }

        var normalizedExpected = expected.Replace('\\', '/');
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name.Replace('\\', '/'), normalizedExpected, StringComparison.Ordinal));

        return resourceName is null ? null : assembly.GetManifestResourceStream(resourceName);
    }
}

public sealed class NameForgeProjectInclude
{
    public string Type { get; init; } = string.Empty;
    public string CultureId { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}

public sealed class NameForgeCulture
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<NameForgeNamingDomain> Domains { get; init; } = [];
    public Dictionary<string, NameForgeLexemeList> LexemeLists { get; init; } = new(StringComparer.Ordinal);
    public List<NameForgeGrammar> Grammars { get; init; } = [];
    public List<NameForgeProfile> Profiles { get; init; } = [];
}

public sealed class NameForgeLexemeList
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Entries { get; init; } = [];
    public string Source { get; init; } = string.Empty;
}

public sealed class NameForgeGrammar
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Start { get; init; } = "name";
    public Dictionary<string, string> GeneratedContext { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, List<List<string>>> Rules { get; init; } = new(StringComparer.Ordinal);
    public string Capitalization { get; init; } = "title";
}

public sealed class NameForgeStrategy
{
    public string Type { get; init; } = string.Empty;
    public double Weight { get; init; } = 1d;
    public string GrammarId { get; init; } = string.Empty;
    public string DomainId { get; init; } = string.Empty;
}

public sealed class NameForgeGroupConditions
{
    public List<string> EntityKinds { get; init; } = [];
    public List<string> Subtypes { get; init; } = [];
    public bool SubtypeMatchAll { get; init; }
    public List<string> Prominence { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public bool TagMatchAll { get; init; }
    public Dictionary<string, List<string>> Context { get; init; } = new(StringComparer.Ordinal);
}

public sealed class NameForgeStrategyGroup
{
    public string Name { get; init; } = string.Empty;
    public int Priority { get; init; }
    public NameForgeGroupConditions? Conditions { get; init; }
    public List<NameForgeStrategy> Strategies { get; init; } = [];
}

public sealed class NameForgeProfile
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> EntityKinds { get; init; } = [];
    public bool IsDefault { get; init; }
    public List<NameForgeStrategyGroup> StrategyGroups { get; init; } = [];
}

public sealed class NameForgeNamingDomain
{
    public string Id { get; init; } = string.Empty;
    public NameForgeAppliesTo AppliesTo { get; init; } = new();
    public NameForgePhonologyProfile Phonology { get; init; } = new();
    public NameForgeMorphologyProfile Morphology { get; init; } = new();
    public NameForgeStyleRules Style { get; init; } = new();
}

public sealed class NameForgeAppliesTo
{
    public List<string> Kind { get; init; } = [];
    public List<string> SubKind { get; init; } = [];
    public List<string> Tags { get; init; } = [];
}

public sealed class NameForgePhonologyProfile
{
    public List<string> Consonants { get; init; } = [];
    public List<string> Vowels { get; init; } = [];
    public List<string> SyllableTemplates { get; init; } = [];
    public List<string> ForbiddenClusters { get; init; } = [];
    public List<string> FavoredClusters { get; init; } = [];
    public List<double> ConsonantWeights { get; init; } = [];
    public List<double> VowelWeights { get; init; } = [];
    public List<double> TemplateWeights { get; init; } = [];
    public List<int> LengthRange { get; init; } = [1, 2];
    public double FavoredClusterBoost { get; init; } = 2d;
    public int MaxConsonantCluster { get; init; } = 3;
    public int MinVowelSpacing { get; init; } = 3;
    public Dictionary<string, int> SonorityRanks { get; init; } = new(StringComparer.Ordinal);
}

public sealed class NameForgeMorphologyProfile
{
    public List<string> Prefixes { get; init; } = [];
    public List<string> Suffixes { get; init; } = [];
    public List<string> Infixes { get; init; } = [];
    public List<string> WordRoots { get; init; } = [];
    public List<string> Honorifics { get; init; } = [];
    public List<string> Structure { get; init; } = ["root"];
    public List<double> PrefixWeights { get; init; } = [];
    public List<double> SuffixWeights { get; init; } = [];
    public List<double> StructureWeights { get; init; } = [];
}

public sealed class NameForgeStyleRules
{
    public double ApostropheRate { get; init; }
    public double HyphenRate { get; init; }
    public string Capitalization { get; init; } = "title";
    public List<string> PreferredEndings { get; init; } = [];
    public double PreferredEndingBoost { get; init; } = 2d;
    public string RhythmBias { get; init; } = "neutral";
    public int TargetLength { get; init; }
    public double LengthTolerance { get; init; } = 3d;
}

public sealed class NameForgeGenerateRequest
{
    public string CultureId { get; init; } = string.Empty;
    public string ProfileId { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Subtype { get; init; } = string.Empty;
    public string Prominence { get; init; } = "recognized";
    public List<string> Tags { get; init; } = [];
    public Dictionary<string, string> Context { get; init; } = new(StringComparer.Ordinal);
    public int Count { get; init; } = 1;
    public string Seed { get; init; } = string.Empty;
}

public sealed record NameForgeGroupMatchDebug(
    string GroupName,
    bool Matched,
    string Reason,
    int Priority);

public sealed record NameForgeNameDebugInfo(
    string GroupUsed,
    string StrategyUsed,
    string StrategyType,
    string GrammarId,
    string DomainId,
    IReadOnlyList<NameForgeGroupMatchDebug> GroupMatching);

public sealed record NameForgeGenerateResult(
    IReadOnlyList<string> Names,
    IReadOnlyDictionary<string, int> StrategyUsage,
    IReadOnlyList<NameForgeNameDebugInfo> DebugInfo,
    IReadOnlyList<string> ReferenceNames);
