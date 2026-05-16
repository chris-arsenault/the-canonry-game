namespace NameForge.Runtime;

using System.Text.RegularExpressions;

public sealed partial class NameForgeGenerator
{
    private const string CapMarkerStart = "\uE000";
    private const string CapMarkerSep = "\uE001";
    private const string CapMarkerEnd = "\uE002";

    private readonly NameForgeProject _project;
    private readonly INameForgeMarkovModelProvider _markovModels;

    public NameForgeGenerator(NameForgeProject project, INameForgeMarkovModelProvider? markovModels = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _markovModels = markovModels ?? NameForgeEmbeddedMarkovModelProvider.Instance;
    }

    public string GenerateOne(NameForgeGenerateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = Generate(WithCount(request, 1));
        return result.Names.Count > 0 ? result.Names[0] : string.Empty;
    }

    public bool TryGenerateOne(NameForgeGenerateRequest request, out string name)
    {
        ArgumentNullException.ThrowIfNull(request);

        name = string.Empty;
        if (!_project.Cultures.TryGetValue(request.CultureId, out var culture))
        {
            return false;
        }

        var profile = SelectProfile(culture.Profiles, request.ProfileId, request.Kind);
        if (profile is null)
        {
            return false;
        }

        var core = GenerateCore(culture, profile, WithCount(request, 1));
        if (core.Names.Count == 0 || string.IsNullOrWhiteSpace(core.Names[0]))
        {
            return false;
        }

        name = core.Names[0];
        return true;
    }

    public NameForgeGenerateResult Generate(NameForgeGenerateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_project.Cultures.TryGetValue(request.CultureId, out var culture))
        {
            throw new KeyNotFoundException($"Name Forge culture '{request.CultureId}' was not found.");
        }

        var profile = SelectProfile(culture.Profiles, request.ProfileId, request.Kind)
            ?? throw new InvalidOperationException(
                $"No matching Name Forge profile for kind '{request.Kind}' in culture '{request.CultureId}'.");

        return GenerateCore(culture, profile, request);
    }

    public IReadOnlyList<string> GenerateFromDomain(string cultureId, string domainId, int count, string seed)
    {
        if (!_project.Cultures.TryGetValue(cultureId, out var culture))
        {
            throw new KeyNotFoundException($"Name Forge culture '{cultureId}' was not found.");
        }

        var domain = culture.Domains.FirstOrDefault(domain => string.Equals(domain.Id, domainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Name Forge domain '{domainId}' was not found in culture '{cultureId}'.");

        var rng = new NameForgeRandom(string.IsNullOrEmpty(seed) ? $"{cultureId}:{domainId}" : seed);
        var names = new List<string>(Math.Max(1, count));
        for (var i = 0; i < Math.Max(1, count); i++)
        {
            names.Add(NameForgePhonotactics.Generate(rng, domain));
        }

        return names;
    }

    private NameForgeGenerateResult GenerateCore(
        NameForgeCulture culture,
        NameForgeProfile profile,
        NameForgeGenerateRequest request)
    {
        var seed = string.IsNullOrWhiteSpace(request.Seed)
            ? $"name-forge:{culture.Id}:{request.Kind}:{request.Subtype}"
            : request.Seed;
        var userContext = BuildEffectiveContext(request.Context);
        var context = new GenerationContext(
            new NameForgeRandom(seed),
            culture,
            _markovModels,
            userContext);
        var groupMatch = FindMatchingGroup(
            profile.StrategyGroups,
            request.Kind,
            request.Subtype,
            request.Prominence,
            request.Tags,
            userContext);

        var count = Math.Max(1, request.Count);
        var names = new List<string>(count);
        var referenceNames = new List<string>(count);
        var debug = new List<NameForgeNameDebugInfo>(count);
        var strategyUsage = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["grammar"] = 0,
            ["phonotactic"] = 0,
            ["markov"] = 0,
            ["fallback"] = 0,
        };

        for (var i = 0; i < count; i++)
        {
            var single = GenerateSingleName(groupMatch.Group, context, i);
            names.Add(single.Name);
            referenceNames.Add(string.IsNullOrWhiteSpace(single.ReferenceName) ? single.Name : single.ReferenceName);
            strategyUsage[single.StrategyType] = strategyUsage.GetValueOrDefault(single.StrategyType) + 1;
            debug.Add(new NameForgeNameDebugInfo(
                groupMatch.Group?.Name ?? "(fallback)",
                single.StrategyDescription,
                single.StrategyType,
                single.GrammarId,
                single.DomainId,
                groupMatch.Debug));
        }

        return new NameForgeGenerateResult(names, strategyUsage, debug, referenceNames);
    }

    private static NameForgeProfile? SelectProfile(
        IReadOnlyList<NameForgeProfile> profiles,
        string profileId,
        string kind)
    {
        if (profiles.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(profileId))
        {
            return profiles.FirstOrDefault(profile => string.Equals(profile.Id, profileId, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            var kindMatch = profiles.FirstOrDefault(profile => profile.EntityKinds.Contains(kind, StringComparer.Ordinal));
            if (kindMatch is not null)
            {
                return kindMatch;
            }
        }

        var defaultProfile = profiles.FirstOrDefault(static profile => profile.IsDefault);
        if (defaultProfile is not null)
        {
            return defaultProfile;
        }

        return profiles.Count == 1 && profiles[0].EntityKinds.Count == 0 ? profiles[0] : null;
    }

    private static GroupMatchResult FindMatchingGroup(
        IReadOnlyList<NameForgeStrategyGroup> groups,
        string kind,
        string subtype,
        string prominence,
        IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, string> userContext)
    {
        var debug = new List<NameForgeGroupMatchDebug>(groups.Count);
        if (groups.Count == 0)
        {
            return new GroupMatchResult(null, debug);
        }

        var matching = new List<NameForgeStrategyGroup>();
        foreach (var group in groups)
        {
            var (matched, reason) = CheckGroupMatch(group, kind, subtype, prominence, tags, userContext);
            debug.Add(new NameForgeGroupMatchDebug(group.NameOrFallback(), matched, reason, group.Priority));
            if (matched)
            {
                matching.Add(group);
            }
        }

        if (matching.Count == 0)
        {
            debug[0] = debug[0] with { Reason = debug[0].Reason + " [FALLBACK - used because no groups matched]" };
            return new GroupMatchResult(groups[0], debug);
        }

        matching.Sort(static (a, b) =>
        {
            var priorityDiff = b.Priority.CompareTo(a.Priority);
            if (priorityDiff != 0)
            {
                return priorityDiff;
            }

            return HasConditions(b).CompareTo(HasConditions(a));
        });

        var selected = matching[0];
        for (var i = 0; i < debug.Count; i++)
        {
            if (string.Equals(debug[i].GroupName, selected.NameOrFallback(), StringComparison.Ordinal))
            {
                debug[i] = debug[i] with { Reason = debug[i].Reason + " [SELECTED]" };
                break;
            }
        }

        return new GroupMatchResult(selected, debug);
    }

    private static (bool Matched, string Reason) CheckGroupMatch(
        NameForgeStrategyGroup group,
        string kind,
        string subtype,
        string prominence,
        IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, string> userContext)
    {
        var conditions = group.Conditions;
        if (conditions is null)
        {
            return (true, "No conditions (matches all)");
        }

        var kindResult = CheckListCondition(conditions.EntityKinds, kind, "entityKind");
        if (kindResult is not null)
        {
            return kindResult.Value;
        }

        var subtypeResult = CheckListCondition(conditions.Subtypes, subtype, "subtype");
        if (subtypeResult is not null)
        {
            return subtypeResult.Value;
        }

        var prominenceResult = CheckListCondition(conditions.Prominence, prominence, "prominence");
        if (prominenceResult is not null)
        {
            return prominenceResult.Value;
        }

        var tagResult = CheckTagCondition(conditions.Tags, conditions.TagMatchAll, tags);
        if (tagResult is not null)
        {
            return tagResult.Value;
        }

        var contextResult = CheckContextCondition(conditions.Context, userContext);
        return contextResult ?? (true, "All conditions matched");
    }

    private static (bool Matched, string Reason)? CheckListCondition(
        IReadOnlyList<string> conditions,
        string value,
        string fieldName)
    {
        if (conditions.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, $"Requires {fieldName} in [{string.Join(", ", conditions)}] but none provided");
        }

        return conditions.Contains(value, StringComparer.Ordinal)
            ? null
            : (false, $"{fieldName} \"{value}\" not in [{string.Join(", ", conditions)}]");
    }

    private static (bool Matched, string Reason)? CheckTagCondition(
        IReadOnlyList<string> conditionTags,
        bool tagMatchAll,
        IReadOnlyList<string> tags)
    {
        if (conditionTags.Count == 0)
        {
            return null;
        }

        if (tagMatchAll)
        {
            var missingTags = conditionTags.Where(tag => !tags.Contains(tag, StringComparer.Ordinal)).ToArray();
            return missingTags.Length == 0
                ? null
                : (false, $"Missing required tags: [{string.Join(", ", missingTags)}]");
        }

        return conditionTags.Any(tag => tags.Contains(tag, StringComparer.Ordinal))
            ? null
            : (false, $"No matching tags from [{string.Join(", ", conditionTags)}]");
    }

    private static (bool Matched, string Reason)? CheckContextCondition(
        IReadOnlyDictionary<string, List<string>> conditionContext,
        IReadOnlyDictionary<string, string> userContext)
    {
        if (conditionContext.Count == 0)
        {
            return null;
        }

        foreach (var (key, allowedValues) in conditionContext)
        {
            if (!userContext.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return (false, $"Requires context:{key} but none provided");
            }

            if (allowedValues.Count > 0 && !allowedValues.Contains(value, StringComparer.Ordinal))
            {
                return (false, $"context:{key} \"{value}\" not in [{string.Join(", ", allowedValues)}]");
            }
        }

        return null;
    }

    private static SingleNameResult GenerateSingleName(
        NameForgeStrategyGroup? group,
        GenerationContext context,
        int index)
    {
        if (group is null || group.Strategies.Count == 0)
        {
            return SingleNameResult.Fallback(GenerateFallbackName(context.Culture.LexemeLists.Values.ToArray(), context.Rng, index), "No strategies in group");
        }

        var viableStrategies = group.Strategies
            .Where(strategy => StrategyCanGenerateName(strategy, context))
            .ToArray();
        if (viableStrategies.Length == 0)
        {
            return SingleNameResult.Fallback(
                GenerateFallbackName(context.Culture.LexemeLists.Values.ToArray(), context.Rng, index),
                "No viable strategies under context");
        }

        var strategy = PickWeightedStrategy(viableStrategies, context.Rng);
        if (string.Equals(strategy.Type, "grammar", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(strategy.GrammarId))
        {
            var grammar = context.Culture.Grammars.FirstOrDefault(grammar =>
                string.Equals(grammar.Id, strategy.GrammarId, StringComparison.Ordinal));
            if (grammar is not null)
            {
                var result = ExpandGrammar(grammar, context);
                return new SingleNameResult(
                    result.Name,
                    result.ReferenceName,
                    result.UsedMarkov ? "markov" : "grammar",
                    $"grammar:{strategy.GrammarId}",
                    strategy.GrammarId,
                    string.Empty);
            }

            return SingleNameResult.Fallback(
                GenerateFallbackName(context.Culture.LexemeLists.Values.ToArray(), context.Rng, index),
                $"grammar:{strategy.GrammarId} NOT FOUND",
                strategy.GrammarId);
        }

        if (string.Equals(strategy.Type, "phonotactic", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(strategy.DomainId))
        {
            var domain = context.Culture.Domains.FirstOrDefault(domain =>
                string.Equals(domain.Id, strategy.DomainId, StringComparison.Ordinal));
            if (domain is not null)
            {
                return new SingleNameResult(
                    NameForgePhonotactics.Generate(context.Rng, domain),
                    string.Empty,
                    "phonotactic",
                    $"phonotactic:{strategy.DomainId}",
                    string.Empty,
                    strategy.DomainId);
            }

            if (context.Culture.Domains.Count > 0)
            {
                return new SingleNameResult(
                    NameForgePhonotactics.Generate(context.Rng, context.Culture.Domains[0]),
                    string.Empty,
                    "phonotactic",
                    $"phonotactic:{strategy.DomainId} NOT FOUND, used {context.Culture.Domains[0].Id}",
                    string.Empty,
                    context.Culture.Domains[0].Id);
            }
        }

        return SingleNameResult.Fallback(
            GenerateFallbackName(context.Culture.LexemeLists.Values.ToArray(), context.Rng, index),
            $"Unknown strategy type: {strategy.Type}");
    }

    private static NameForgeStrategy PickWeightedStrategy(
        IReadOnlyList<NameForgeStrategy> strategies,
        NameForgeRandom rng)
    {
        if (strategies.Count == 1)
        {
            return strategies[0];
        }

        var total = strategies.Sum(static strategy => Math.Max(0d, strategy.Weight));
        if (total <= 0d)
        {
            return strategies[0];
        }

        var roll = rng.NextDouble() * total;
        foreach (var strategy in strategies)
        {
            roll -= Math.Max(0d, strategy.Weight);
            if (roll <= 0d)
            {
                return strategy;
            }
        }

        return strategies[^1];
    }

    private static GrammarExpansionResult ExpandGrammar(NameForgeGrammar grammar, GenerationContext context)
    {
        var expansion = new GrammarExpansionContext(context.UserContext);
        PopulateGeneratedContext(grammar, context, expansion);
        var name = ExpandSymbol(
            string.IsNullOrWhiteSpace(grammar.Start) ? "name" : grammar.Start,
            grammar.Rules,
            context,
            expansion,
            0);

        if (!string.Equals(grammar.Capitalization, "mixed", StringComparison.Ordinal))
        {
            name = NameForgeText.ApplyCapitalization(name, grammar.Capitalization);
        }

        name = ApplyTokenCapitalizationMarkers(name);
        expansion.GeneratedContext.TryGetValue(NameForgeContextKeys.ReferenceName, out var referenceName);
        if (!string.IsNullOrWhiteSpace(referenceName) &&
            !string.Equals(grammar.Capitalization, "mixed", StringComparison.Ordinal))
        {
            referenceName = NameForgeText.ApplyCapitalization(referenceName, grammar.Capitalization);
        }

        referenceName = ApplyTokenCapitalizationMarkers(referenceName ?? string.Empty);
        return new GrammarExpansionResult(name, expansion.UsedMarkov, referenceName);
    }

    private static void PopulateGeneratedContext(
        NameForgeGrammar grammar,
        GenerationContext context,
        GrammarExpansionContext expansion)
    {
        foreach (var (contextKey, ruleName) in grammar.GeneratedContext)
        {
            if (string.IsNullOrWhiteSpace(contextKey) ||
                string.IsNullOrWhiteSpace(ruleName) ||
                expansion.UserContext.ContainsKey(contextKey))
            {
                continue;
            }

            var value = ExpandSymbol(ruleName, grammar.Rules, context, expansion, 0);
            value = ApplyTokenCapitalizationMarkers(value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                expansion.UserContext[contextKey] = value;
                expansion.GeneratedContext[contextKey] = value;
            }
        }
    }

    private static string ExpandSymbol(
        string symbol,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        GenerationContext context,
        GrammarExpansionContext expansion,
        int depth)
    {
        if (depth > 10)
        {
            return symbol;
        }

        if (!rules.TryGetValue(symbol, out var productions) || productions.Count == 0)
        {
            return ResolveToken(symbol, rules, context, expansion, depth);
        }

        var viable = productions
            .Where(production => !ProductionNeedsMissingContext(production, rules, expansion.UserContext, depth))
            .ToArray();
        if (viable.Length == 0)
        {
            return string.Empty;
        }

        var production = context.Rng.Pick(viable, []);
        var parts = new List<string>(production.Count);

        foreach (var token in production)
        {
            if (token.Contains('-', StringComparison.Ordinal) &&
                ContainsPatternPrefix(token) &&
                ShouldSplitHyphenatedPatternToken(token))
            {
                var subParts = token.Split('-');
                parts.Add(string.Join(
                    "-",
                    subParts.Select(part =>
                    {
                        var trimmed = part.Trim();
                        return rules.ContainsKey(trimmed)
                            ? ExpandSymbol(trimmed, rules, context, expansion, depth + 1)
                            : ResolveToken(trimmed, rules, context, expansion, depth);
                    })));
            }
            else if (rules.ContainsKey(token))
            {
                parts.Add(ExpandSymbol(token, rules, context, expansion, depth + 1));
            }
            else
            {
                parts.Add(ResolveToken(token, rules, context, expansion, depth));
            }
        }

        return string.Join(' ', parts.Where(static part => part.Length > 0)).Trim();
    }

    private static bool ProductionNeedsMissingContext(
        IReadOnlyList<string> production,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        IReadOnlyDictionary<string, string> userContext,
        int depth)
    {
        return production.Any(token => TokenNeedsMissingContext(token, rules, userContext, depth));
    }

    private static bool TokenNeedsMissingContext(
        string token,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        IReadOnlyDictionary<string, string> userContext,
        int depth)
    {
        if (rules.ContainsKey(token))
        {
            return !SymbolCanGenerate(token, rules, userContext, depth + 1);
        }

        if (token.Contains('-', StringComparison.Ordinal) &&
            ContainsPatternPrefix(token) &&
            ShouldSplitHyphenatedPatternToken(token))
        {
            return token.Split('-').Any(part =>
                TokenNeedsMissingContext(part.Trim(), rules, userContext, depth + 1));
        }

        if (TryParseContextGuard(token, out var guardKey, out var expectedValue) &&
            (!userContext.TryGetValue(guardKey, out var guardValue) ||
             string.IsNullOrEmpty(guardValue) ||
             (expectedValue is not null && !string.Equals(guardValue, expectedValue, StringComparison.Ordinal))))
        {
            return true;
        }

        var index = 0;
        while (index < token.Length)
        {
            index = token.IndexOf("context:", index, StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            var start = index + "context:".Length;
            var end = start;
            while (end < token.Length && IsContextKeyCharacter(token[end]))
            {
                end++;
            }

            var key = token[start..end];
            if (!TryResolveContextValue(key, userContext, out _))
            {
                return true;
            }

            index = end;
        }

        return false;
    }

    private static string ResolveToken(
        string token,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        GenerationContext context,
        GrammarExpansionContext expansion,
        int depth)
    {
        if (!token.Contains('^', StringComparison.Ordinal))
        {
            return ResolveSimpleToken(token, context, expansion);
        }

        var segments = token.Split('^', StringSplitOptions.None);
        var parts = new List<string>(segments.Length);
        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                continue;
            }

            parts.Add(ResolveSegmentWithPrefix(segment, rules, context, expansion, depth));
        }

        return string.Concat(parts);
    }

    private static string ResolveSegmentWithPrefix(
        string segment,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        GenerationContext context,
        GrammarExpansionContext expansion,
        int depth)
    {
        var earliestIndex = -1;
        foreach (var prefix in PatternPrefixes)
        {
            var index = segment.IndexOf(prefix, StringComparison.Ordinal);
            if (index >= 0 && (earliestIndex < 0 || index < earliestIndex))
            {
                earliestIndex = index;
            }
        }

        if (earliestIndex < 0)
        {
            return segment;
        }

        var literalPrefix = segment[..earliestIndex];
        var pattern = segment[earliestIndex..];
        return literalPrefix + (rules.ContainsKey(pattern)
            ? ExpandSymbol(pattern, rules, context, expansion, depth + 1)
            : ResolveSimpleToken(pattern, context, expansion));
    }

    private static string ResolveSimpleToken(
        string token,
        GenerationContext context,
        GrammarExpansionContext expansion)
    {
        var modifiers = ParseModifiers(token);
        var result = ResolvePatternRef(modifiers.Pattern, token, context, expansion);
        if (result is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(modifiers.Derivation))
        {
            result = NameForgeDerivation.Apply(result, modifiers.Derivation);
        }

        if (!string.IsNullOrEmpty(modifiers.Truncation))
        {
            result = ApplyTruncation(result, modifiers.Truncation, context.Rng);
        }

        if (!string.IsNullOrEmpty(modifiers.Capitalization))
        {
            result = WrapCapitalizationMarker(result, modifiers.Capitalization);
        }

        return result;
    }

    private static string? ResolvePatternRef(
        string pattern,
        string token,
        GenerationContext context,
        GrammarExpansionContext expansion)
    {
        if (pattern.StartsWith("slot:", StringComparison.Ordinal))
        {
            var listId = pattern["slot:".Length..];
            return context.Culture.LexemeLists.TryGetValue(listId, out var list) && list.Entries.Count > 0
                ? context.Rng.Pick(list.Entries, listId)
                : listId;
        }

        if (pattern.StartsWith("domain:", StringComparison.Ordinal))
        {
            var domainId = pattern["domain:".Length..];
            var domain = context.Culture.Domains.FirstOrDefault(domain =>
                string.Equals(domain.Id, domainId, StringComparison.Ordinal));
            return domain is null ? domainId : NameForgePhonotactics.Generate(context.Rng, domain);
        }

        if (pattern.StartsWith("markov:", StringComparison.Ordinal))
        {
            return ResolveMarkov(pattern["markov:".Length..], context, expansion);
        }

        if (pattern.StartsWith("context:", StringComparison.Ordinal))
        {
            var key = pattern["context:".Length..];
            return TryResolveContextValue(key, expansion.UserContext, out var value) ? value : null;
        }

        if (pattern.StartsWith("when:", StringComparison.Ordinal))
        {
            return TryContextGuardMatches(pattern, expansion.UserContext) ? string.Empty : null;
        }

        return token;
    }

    private static bool TryContextGuardMatches(
        string pattern,
        IReadOnlyDictionary<string, string> userContext)
    {
        return TryParseContextGuard(pattern, out var key, out var expectedValue) &&
               userContext.TryGetValue(key, out var value) &&
               value.Length > 0 &&
               (expectedValue is null || string.Equals(value, expectedValue, StringComparison.Ordinal));
    }

    private static bool TryParseContextGuard(string token, out string key, out string? expectedValue)
    {
        key = string.Empty;
        expectedValue = null;
        if (!token.StartsWith("when:", StringComparison.Ordinal))
        {
            return false;
        }

        var body = token["when:".Length..];
        var separator = body.IndexOf('=', StringComparison.Ordinal);
        if (separator < 0)
        {
            key = body;
            return key.Length > 0;
        }

        key = body[..separator];
        expectedValue = body[(separator + 1)..];
        return key.Length > 0 && expectedValue.Length > 0;
    }

    private static string ResolveMarkov(
        string modelId,
        GenerationContext context,
        GrammarExpansionContext expansion)
    {
        if (context.MarkovModels.TryGetModel(modelId, out var model))
        {
            expansion.UsedMarkov = true;
            return NameForgeMarkov.Generate(model, context.Rng);
        }

        return context.Culture.Domains.Count > 0
            ? NameForgePhonotactics.Generate(context.Rng, context.Culture.Domains[0])
            : modelId;
    }

    private static ParsedModifiers ParseModifiers(string pattern)
    {
        string? derivation = null;
        string? truncation = null;
        string? capitalization = null;
        var remaining = pattern;

        while (true)
        {
            var tildeIndex = remaining.LastIndexOf('~');
            if (tildeIndex < 0)
            {
                break;
            }

            var modifier = remaining[(tildeIndex + 1)..];
            if (TryCapitalizationModifier(modifier, out var cap) && capitalization is null)
            {
                capitalization = cap;
                remaining = remaining[..tildeIndex];
            }
            else if (IsTruncationModifier(modifier) && truncation is null)
            {
                truncation = modifier;
                remaining = remaining[..tildeIndex];
            }
            else if (NameForgeDerivation.IsDerivation(modifier) && derivation is null)
            {
                derivation = modifier;
                remaining = remaining[..tildeIndex];
            }
            else
            {
                break;
            }
        }

        return new ParsedModifiers(remaining, derivation ?? string.Empty, truncation ?? string.Empty, capitalization ?? string.Empty);
    }

    private static string ApplyTruncation(string value, string type, NameForgeRandom rng)
    {
        if (value.Length <= 3)
        {
            return value;
        }

        var maxChop = Math.Min(3, value.Length - 2);
        var amount = rng.NextInt(1, maxChop);
        return type switch
        {
            "chopL" => value[amount..],
            "chopR" => value[..^amount],
            "chop" => rng.Chance(0.5d) ? value[amount..] : value[..^amount],
            _ => value,
        };
    }

    private static string GenerateFallbackName(
        IReadOnlyList<NameForgeLexemeList> lexemeLists,
        NameForgeRandom rng,
        int index)
    {
        var nonEmptyLists = lexemeLists.Where(static list => list.Entries.Count > 0).ToArray();
        if (nonEmptyLists.Length == 0)
        {
            return $"Name-{index + 1}";
        }

        var partCount = rng.NextInt(1, 2);
        var parts = new List<string>(partCount);
        for (var i = 0; i < partCount; i++)
        {
            var list = rng.Pick(nonEmptyLists, nonEmptyLists[0]);
            if (list.Entries.Count > 0)
            {
                parts.Add(rng.Pick(list.Entries, string.Empty));
            }
        }

        return parts.Count == 0
            ? $"Name-{index + 1}"
            : NameForgeText.Capitalize(string.Join('-', parts));
    }

    private static bool HasConditions(NameForgeStrategyGroup group)
    {
        var conditions = group.Conditions;
        return conditions is not null &&
               (conditions.EntityKinds.Count > 0 ||
                conditions.Subtypes.Count > 0 ||
                conditions.Prominence.Count > 0 ||
                conditions.Tags.Count > 0 ||
                conditions.Context.Count > 0);
    }

    private static bool ContainsPatternPrefix(string value) =>
        PatternPrefixes.Any(prefix => value.Contains(prefix, StringComparison.Ordinal));

    private static bool ShouldSplitHyphenatedPatternToken(string token) =>
        !token.StartsWith("context:", StringComparison.Ordinal);

    private static bool IsContextKeyCharacter(char ch) =>
        char.IsAsciiLetterOrDigit(ch) || ch is '_' or '-';

    private static bool TryCapitalizationModifier(string modifier, out string capitalization)
    {
        capitalization = modifier switch
        {
            "cap" or "c" => "cap",
            "lower" or "l" => "lower",
            "upper" or "u" => "upper",
            "title" or "t" => "title",
            _ => string.Empty,
        };

        return capitalization.Length > 0;
    }

    private static bool IsTruncationModifier(string modifier) =>
        modifier is "chopL" or "chopR" or "chop";

    private static string WrapCapitalizationMarker(string value, string modifier) =>
        $"{CapMarkerStart}{modifier}{CapMarkerSep}{value}{CapMarkerEnd}";

    private static string ApplyTokenCapitalizationMarkers(string value) =>
        TokenCapitalizationRegex().Replace(value, match =>
            ApplyTokenCapitalization(match.Groups[2].Value, match.Groups[1].Value));

    private static string ApplyTokenCapitalization(string value, string modifier) =>
        modifier switch
        {
            "cap" => value.Length == 0 ? value : NameForgeText.Capitalize(value.ToLowerInvariant()),
            "lower" => value.ToLowerInvariant(),
            "upper" => value.ToUpperInvariant(),
            "title" => NameForgeText.CapitalizeWords(value),
            _ => value,
        };

    private static NameForgeGenerateRequest WithCount(NameForgeGenerateRequest request, int count) =>
        new()
        {
            CultureId = request.CultureId,
            ProfileId = request.ProfileId,
            Kind = request.Kind,
            Subtype = request.Subtype,
            Prominence = request.Prominence,
            Tags = request.Tags,
            Context = request.Context,
            Count = count,
            Seed = request.Seed,
        };

    [GeneratedRegex(@"\uE000(cap|lower|upper|title)\uE001([^\uE002]*)\uE002")]
    private static partial Regex TokenCapitalizationRegex();

    private static readonly string[] PatternPrefixes = ["slot:", "domain:", "markov:", "context:", "when:"];

    private sealed record GenerationContext(
        NameForgeRandom Rng,
        NameForgeCulture Culture,
        INameForgeMarkovModelProvider MarkovModels,
        IReadOnlyDictionary<string, string> UserContext);

    private sealed class GrammarExpansionContext
    {
        public GrammarExpansionContext(IReadOnlyDictionary<string, string> userContext)
        {
            UserContext = new Dictionary<string, string>(userContext, StringComparer.Ordinal);
        }

        public Dictionary<string, string> UserContext { get; }

        public Dictionary<string, string> GeneratedContext { get; } = new(StringComparer.Ordinal);

        public bool UsedMarkov { get; set; }
    }

    private sealed record GroupMatchResult(
        NameForgeStrategyGroup? Group,
        IReadOnlyList<NameForgeGroupMatchDebug> Debug);

    private sealed record SingleNameResult(
        string Name,
        string ReferenceName,
        string StrategyType,
        string StrategyDescription,
        string GrammarId,
        string DomainId)
    {
        public static SingleNameResult Fallback(string name, string description, string grammarId = "") =>
            new(name, string.Empty, "fallback", description, grammarId, string.Empty);
    }

    private sealed record GrammarExpansionResult(string Name, bool UsedMarkov, string ReferenceName);

    private sealed record ParsedModifiers(
        string Pattern,
        string Derivation,
        string Truncation,
        string Capitalization);
}

internal static class NameForgeStrategyGroupExtensions
{
    public static string NameOrFallback(this NameForgeStrategyGroup group) =>
        string.IsNullOrWhiteSpace(group.Name) ? "(unnamed)" : group.Name;
}

internal static class NameForgeContextKeys
{
    public const string ReferenceName = "reference_name";
}
