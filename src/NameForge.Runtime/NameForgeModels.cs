namespace NameForge.Runtime;

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
    public Dictionary<string, NameForgeCulture> Cultures { get; init; } = new(StringComparer.Ordinal);

    public static NameForgeProject Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.Deserialize<NameForgeProject>(stream, JsonOptions)
            ?? throw new JsonException("Name Forge project JSON did not contain a project object.");
    }

    public static NameForgeProject LoadJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<NameForgeProject>(json, JsonOptions)
            ?? throw new JsonException("Name Forge project JSON did not contain a project object.");
    }
}

public sealed class NameForgeCulture
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public NameForgeNamePostProcessing PostProcessing { get; init; } = new();
    public List<NameForgeNamingDomain> Domains { get; init; } = [];
    public Dictionary<string, NameForgeLexemeList> LexemeLists { get; init; } = new(StringComparer.Ordinal);
    public List<NameForgeGrammar> Grammars { get; init; } = [];
    public List<NameForgeProfile> Profiles { get; init; } = [];
}

public sealed class NameForgeNamePostProcessing
{
    public string HyphenRomanization { get; init; } = string.Empty;
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
    IReadOnlyList<NameForgeNameDebugInfo> DebugInfo);
