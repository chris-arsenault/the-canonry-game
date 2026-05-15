namespace NameForge.Runtime;

internal static class NameForgeDerivation
{
    private static readonly Dictionary<string, string> IrregularPast = new(StringComparer.Ordinal)
    {
        ["break"] = "broken",
        ["speak"] = "spoken",
        ["wake"] = "woken",
        ["take"] = "taken",
        ["shake"] = "shaken",
        ["forsake"] = "forsaken",
        ["sing"] = "sung",
        ["ring"] = "rung",
        ["drink"] = "drunk",
        ["sink"] = "sunk",
        ["shrink"] = "shrunk",
        ["stink"] = "stunk",
        ["swim"] = "swum",
        ["begin"] = "begun",
        ["bring"] = "brought",
        ["buy"] = "bought",
        ["catch"] = "caught",
        ["fight"] = "fought",
        ["seek"] = "sought",
        ["teach"] = "taught",
        ["think"] = "thought",
        ["bear"] = "borne",
        ["bite"] = "bitten",
        ["blow"] = "blown",
        ["choose"] = "chosen",
        ["draw"] = "drawn",
        ["drive"] = "driven",
        ["eat"] = "eaten",
        ["fall"] = "fallen",
        ["fly"] = "flown",
        ["freeze"] = "frozen",
        ["give"] = "given",
        ["grow"] = "grown",
        ["hide"] = "hidden",
        ["know"] = "known",
        ["ride"] = "ridden",
        ["rise"] = "risen",
        ["see"] = "seen",
        ["slay"] = "slain",
        ["smite"] = "smitten",
        ["steal"] = "stolen",
        ["strike"] = "stricken",
        ["swear"] = "sworn",
        ["tear"] = "torn",
        ["throw"] = "thrown",
        ["wear"] = "worn",
        ["weave"] = "woven",
        ["write"] = "written",
        ["cut"] = "cut",
        ["hit"] = "hit",
        ["hurt"] = "hurt",
        ["put"] = "put",
        ["set"] = "set",
        ["shed"] = "shed",
        ["shut"] = "shut",
        ["split"] = "split",
        ["spread"] = "spread",
        ["thrust"] = "thrust",
        ["bend"] = "bent",
        ["build"] = "built",
        ["burn"] = "burnt",
        ["deal"] = "dealt",
        ["dream"] = "dreamt",
        ["dwell"] = "dwelt",
        ["feel"] = "felt",
        ["keep"] = "kept",
        ["kneel"] = "knelt",
        ["lean"] = "leant",
        ["leap"] = "leapt",
        ["learn"] = "learnt",
        ["leave"] = "left",
        ["lend"] = "lent",
        ["lose"] = "lost",
        ["mean"] = "meant",
        ["meet"] = "met",
        ["send"] = "sent",
        ["sleep"] = "slept",
        ["smell"] = "smelt",
        ["spell"] = "spelt",
        ["spend"] = "spent",
        ["spill"] = "spilt",
        ["sweep"] = "swept",
        ["weep"] = "wept",
        ["be"] = "been",
        ["do"] = "done",
        ["go"] = "gone",
        ["have"] = "had",
        ["make"] = "made",
        ["say"] = "said",
        ["hold"] = "held",
        ["stand"] = "stood",
        ["find"] = "found",
        ["bind"] = "bound",
        ["grind"] = "ground",
        ["wind"] = "wound",
        ["hang"] = "hung",
        ["dig"] = "dug",
        ["stick"] = "stuck",
        ["spin"] = "spun",
        ["win"] = "won",
        ["cling"] = "clung",
        ["fling"] = "flung",
        ["sling"] = "slung",
        ["sting"] = "stung",
        ["swing"] = "swung",
        ["wring"] = "wrung",
    };

    private static readonly Dictionary<string, string> IrregularAgentive = new(StringComparer.Ordinal)
    {
        ["lie"] = "liar",
        ["die"] = "death-dealer",
        ["sing"] = "singer",
        ["slay"] = "slayer",
    };

    private static readonly Dictionary<string, string> IrregularSuperlative = new(StringComparer.Ordinal)
    {
        ["good"] = "best",
        ["bad"] = "worst",
        ["far"] = "farthest",
        ["little"] = "least",
        ["much"] = "most",
        ["many"] = "most",
    };

    private static readonly Dictionary<string, string> IrregularComparative = new(StringComparer.Ordinal)
    {
        ["good"] = "better",
        ["bad"] = "worse",
        ["far"] = "farther",
        ["little"] = "less",
        ["much"] = "more",
        ["many"] = "more",
    };

    public static bool IsDerivation(string modifier) =>
        modifier is "er" or "est" or "ing" or "ed" or "poss" or "comp";

    public static string Apply(string value, string derivation) =>
        derivation switch
        {
            "er" => Agentive(value),
            "est" => Superlative(value),
            "comp" => Comparative(value),
            "ing" => Gerund(value),
            "ed" => Past(value),
            "poss" => Possessive(value),
            _ => value,
        };

    private static string Agentive(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularAgentive.TryGetValue(lower, out var irregular))
        {
            return MatchCase(word, irregular);
        }

        if (lower.EndsWith('e'))
        {
            return word + "r";
        }

        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
        {
            return word[..^1] + "ier";
        }

        return ShouldDoubleConsonant(word)
            ? word + word[^1] + "er"
            : word + "er";
    }

    private static string Superlative(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularSuperlative.TryGetValue(lower, out var irregular))
        {
            return MatchCase(word, irregular);
        }

        if (lower.EndsWith('e'))
        {
            return word + "st";
        }

        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
        {
            return word[..^1] + "iest";
        }

        return ShouldDoubleConsonant(word)
            ? word + word[^1] + "est"
            : word + "est";
    }

    private static string Comparative(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularComparative.TryGetValue(lower, out var irregular))
        {
            return MatchCase(word, irregular);
        }

        if (lower.EndsWith('e'))
        {
            return word + "r";
        }

        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
        {
            return word[..^1] + "ier";
        }

        return ShouldDoubleConsonant(word)
            ? word + word[^1] + "er"
            : word + "er";
    }

    private static string Gerund(string word)
    {
        var lower = word.ToLowerInvariant();
        if (lower.EndsWith("ie", StringComparison.Ordinal))
        {
            return word[..^2] + "ying";
        }

        if (lower.EndsWith('e') && !lower.EndsWith("ee", StringComparison.Ordinal))
        {
            return word[..^1] + "ing";
        }

        return ShouldDoubleConsonant(word)
            ? word + word[^1] + "ing"
            : word + "ing";
    }

    private static string Past(string word)
    {
        var lower = word.ToLowerInvariant();
        if (IrregularPast.TryGetValue(lower, out var irregular))
        {
            return MatchCase(word, irregular);
        }

        if (lower.EndsWith('e'))
        {
            return word + "d";
        }

        if (lower.EndsWith('y') && word.Length > 1 && IsConsonant(lower[^2]))
        {
            return word[..^1] + "ied";
        }

        return ShouldDoubleConsonant(word)
            ? word + word[^1] + "ed"
            : word + "ed";
    }

    private static string Possessive(string word)
    {
        var lower = word.ToLowerInvariant();
        return lower.EndsWith('s') || lower.EndsWith('x') || lower.EndsWith('z')
            ? word + "'"
            : word + "'s";
    }

    private static bool ShouldDoubleConsonant(string word)
    {
        if (word.Length < 3)
        {
            return false;
        }

        var last = char.ToLowerInvariant(word[^1]);
        var secondLast = char.ToLowerInvariant(word[^2]);
        var thirdLast = char.ToLowerInvariant(word[^3]);

        if (last is 'w' or 'x' or 'y')
        {
            return false;
        }

        if (!IsConsonant(last) || !IsVowel(secondLast) || !IsConsonant(thirdLast))
        {
            return false;
        }

        if (word.Length <= 4)
        {
            return true;
        }

        return word.EndsWith("mit", StringComparison.OrdinalIgnoreCase) ||
               word.EndsWith("gin", StringComparison.OrdinalIgnoreCase) ||
               word.EndsWith("fer", StringComparison.OrdinalIgnoreCase) ||
               word.EndsWith("cur", StringComparison.OrdinalIgnoreCase) ||
               word.EndsWith("pel", StringComparison.OrdinalIgnoreCase) ||
               word.EndsWith("quet", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVowel(char ch) =>
        ch is 'a' or 'e' or 'i' or 'o' or 'u';

    private static bool IsConsonant(char ch) =>
        ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z')) && !IsVowel(char.ToLowerInvariant(ch));

    private static string MatchCase(string source, string result)
    {
        if (string.Equals(source, source.ToUpperInvariant(), StringComparison.Ordinal))
        {
            return result.ToUpperInvariant();
        }

        return source.Length > 0 && char.IsUpper(source[0])
            ? NameForgeText.Capitalize(result)
            : result;
    }
}
