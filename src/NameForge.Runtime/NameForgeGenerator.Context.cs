namespace NameForge.Runtime;

public sealed partial class NameForgeGenerator
{
    private static Dictionary<string, string> BuildEffectiveContext(IReadOnlyDictionary<string, string> userContext)
    {
        var values = new Dictionary<string, string>(userContext, StringComparer.Ordinal);
        AddContextFallback(values, "primary_entity_reference_name", "primary_entity_name");
        AddContextFallback(values, "primary_entity_inherited_name", "primary_entity_reference_name");
        AddContextFallback(values, "secondary_entity_reference_name", "secondary_entity_name");
        AddContextFallback(values, "secondary_entity_inherited_name", "secondary_entity_reference_name");
        AddContextFallback(values, "selected_name", "selected");
        AddContextFallback(values, "selected_reference_name", "selected_name");
        AddContextFallback(values, "selected_inherited_name", "selected_reference_name");
        AddContextFallback(values, "parent_primary_entity_reference_name", "parent_primary_entity_name");
        AddContextFallback(values, "parent_primary_entity_inherited_name", "parent_primary_entity_reference_name");
        AddContextFallback(values, "parent_secondary_entity_reference_name", "parent_secondary_entity_name");
        AddContextFallback(values, "parent_secondary_entity_inherited_name", "parent_secondary_entity_reference_name");
        AddContextFallback(values, "trigger_relationship_source_entity_reference_name", "trigger_relationship_source_entity_name");
        AddContextFallback(values, "trigger_relationship_source_entity_inherited_name", "trigger_relationship_source_entity_reference_name");
        AddContextFallback(values, "trigger_relationship_target_entity_reference_name", "trigger_relationship_target_entity_name");
        AddContextFallback(values, "trigger_relationship_target_entity_inherited_name", "trigger_relationship_target_entity_reference_name");
        return values;
    }

    private static void AddContextFallback(Dictionary<string, string> values, string targetKey, string fallbackKey)
    {
        if (values.ContainsKey(targetKey) ||
            !values.TryGetValue(fallbackKey, out var fallback) ||
            string.IsNullOrWhiteSpace(fallback))
        {
            return;
        }

        values[targetKey] = fallback;
    }

    private static bool StrategyCanGenerateName(NameForgeStrategy strategy, GenerationContext context)
    {
        if (string.Equals(strategy.Type, "grammar", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(strategy.GrammarId))
        {
            var grammar = context.Culture.Grammars.FirstOrDefault(grammar =>
                string.Equals(grammar.Id, strategy.GrammarId, StringComparison.Ordinal));
            return grammar is not null && GrammarCanGenerateName(grammar, context.UserContext);
        }

        if (string.Equals(strategy.Type, "phonotactic", StringComparison.Ordinal))
        {
            return !string.IsNullOrWhiteSpace(strategy.DomainId) &&
                   (context.Culture.Domains.Any(domain =>
                        string.Equals(domain.Id, strategy.DomainId, StringComparison.Ordinal)) ||
                    context.Culture.Domains.Count > 0);
        }

        return true;
    }

    private static bool GrammarCanGenerateName(
        NameForgeGrammar grammar,
        IReadOnlyDictionary<string, string> userContext)
    {
        var availableContext = new Dictionary<string, string>(userContext, StringComparer.Ordinal);
        foreach (var contextKey in grammar.GeneratedContext.Keys)
        {
            if (!string.IsNullOrWhiteSpace(contextKey) && !availableContext.ContainsKey(contextKey))
            {
                availableContext[contextKey] = contextKey;
            }
        }

        var start = string.IsNullOrWhiteSpace(grammar.Start) ? "name" : grammar.Start;
        return SymbolCanGenerate(start, grammar.Rules, availableContext, 0);
    }

    private static bool SymbolCanGenerate(
        string symbol,
        IReadOnlyDictionary<string, List<List<string>>> rules,
        IReadOnlyDictionary<string, string> userContext,
        int depth)
    {
        if (depth > 10)
        {
            return true;
        }

        if (!rules.TryGetValue(symbol, out var productions) || productions.Count == 0)
        {
            return !TokenNeedsMissingContext(symbol, rules, userContext, depth);
        }

        return productions.Any(production =>
            !ProductionNeedsMissingContext(production, rules, userContext, depth));
    }

    private static bool TryResolveContextValue(
        string key,
        IReadOnlyDictionary<string, string> userContext,
        out string value)
    {
        if (TryGetReferenceContextKey(key, out var referenceKey) &&
            userContext.TryGetValue(referenceKey, out var referenceValue) &&
            referenceValue.Length > 0)
        {
            value = referenceValue;
            return true;
        }

        if (userContext.TryGetValue(key, out var contextValue) && contextValue.Length > 0)
        {
            value = contextValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetReferenceContextKey(string key, out string referenceKey)
    {
        referenceKey = key switch
        {
            "primary_entity_name" => "primary_entity_reference_name",
            "secondary_entity_name" => "secondary_entity_reference_name",
            "selected_name" => "selected_reference_name",
            "parent_primary_entity_name" => "parent_primary_entity_reference_name",
            "parent_secondary_entity_name" => "parent_secondary_entity_reference_name",
            "trigger_relationship_source_entity_name" => "trigger_relationship_source_entity_reference_name",
            "trigger_relationship_target_entity_name" => "trigger_relationship_target_entity_reference_name",
            _ => string.Empty,
        };

        return referenceKey.Length > 0;
    }
}
