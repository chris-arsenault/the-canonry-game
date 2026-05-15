namespace NameForge.Runtime;

internal sealed record NameForgePhonologyResult(
    string Word,
    IReadOnlyList<string> Syllables,
    IReadOnlyList<string> Templates);

internal sealed record NameForgeMorphologyResult(
    string Result,
    string Structure,
    IReadOnlyList<string> Parts,
    IReadOnlyList<string> Syllables);

internal static class NameForgePhonotactics
{
    public static string Generate(NameForgeRandom rng, NameForgeNamingDomain domain)
    {
        var phonology = GenerateWordWithDebug(rng, domain.Phonology);
        var morphed = CanApplyMorphology(domain.Morphology)
            ? ApplyMorphologyBest(rng, phonology.Word, domain.Morphology, phonology.Syllables)
            : new NameForgeMorphologyResult(
                phonology.Word,
                "root",
                [$"root:{phonology.Word}"],
                phonology.Syllables);

        return ApplyStyle(rng, morphed.Result, domain.Style, morphed.Syllables);
    }

    private static NameForgePhonologyResult GenerateWordWithDebug(NameForgeRandom rng, NameForgePhonologyProfile profile)
    {
        var (minLength, maxLength) = LengthRange(profile);
        var syllableCount = rng.NextInt(minLength, maxLength);
        const int maxAttempts = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var syllables = new List<string>(syllableCount);
            var templates = new List<string>(syllableCount);
            var word = string.Empty;
            var valid = true;

            for (var i = 0; i < syllableCount; i++)
            {
                var (syllable, template) = GenerateSyllable(rng, profile);
                if (NameForgeText.ContainsAny(word + syllable, profile.ForbiddenClusters))
                {
                    valid = false;
                    break;
                }

                syllables.Add(syllable);
                templates.Add(template);
                word += syllable;
            }

            if (valid)
            {
                return new NameForgePhonologyResult(word, syllables, templates);
            }
        }

        var fallback = GenerateSyllable(rng, profile);
        return new NameForgePhonologyResult(
            fallback.Syllable,
            [fallback.Syllable],
            [fallback.Template]);
    }

    private static (string Syllable, string Template) GenerateSyllable(
        NameForgeRandom rng,
        NameForgePhonologyProfile profile)
    {
        var template = rng.PickWeighted(profile.SyllableTemplates, profile.TemplateWeights, "CV");
        var syllable = string.Empty;
        foreach (var symbol in template)
        {
            syllable += symbol switch
            {
                'C' => rng.PickWeighted(profile.Consonants, profile.ConsonantWeights, "n"),
                'V' => rng.PickWeighted(profile.Vowels, profile.VowelWeights, "a"),
                _ => symbol.ToString(),
            };
        }

        return (syllable, template);
    }

    private static NameForgeMorphologyResult ApplyMorphologyBest(
        NameForgeRandom rng,
        string root,
        NameForgeMorphologyProfile profile,
        IReadOnlyList<string> rootSyllables)
    {
        const int candidateCount = 3;
        const int maxLength = 20;

        var candidates = new List<(NameForgeMorphologyResult Result, double Score)>(candidateCount);
        for (var i = 0; i < candidateCount; i++)
        {
            var result = ApplyMorphology(rng, root, profile, rootSyllables);
            var score = 1d;
            if (result.Result.Length > maxLength)
            {
                score *= 0.5d;
            }

            if (result.Result.Length < 3)
            {
                score *= 0.5d;
            }

            candidates.Add((result, score));
        }

        var total = candidates.Sum(candidate => candidate.Score);
        if (total <= 0d)
        {
            return candidates[0].Result;
        }

        var roll = rng.NextDouble() * total;
        foreach (var candidate in candidates)
        {
            roll -= candidate.Score;
            if (roll <= 0d)
            {
                return candidate.Result;
            }
        }

        return candidates[^1].Result;
    }

    private static NameForgeMorphologyResult ApplyMorphology(
        NameForgeRandom rng,
        string root,
        NameForgeMorphologyProfile profile,
        IReadOnlyList<string> rootSyllables)
    {
        var structure = rng.PickWeighted(profile.Structure, profile.StructureWeights, "root");
        var result = string.Empty;
        var parts = new List<string>();
        var syllables = new List<string>();

        foreach (var token in structure.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token)
            {
                case "root":
                    result += root;
                    parts.Add($"root:{root}");
                    syllables.AddRange(rootSyllables.Count > 0 ? rootSyllables : [root]);
                    break;
                case "prefix":
                    if (profile.Prefixes.Count > 0)
                    {
                        var prefix = rng.PickWeighted(profile.Prefixes, profile.PrefixWeights, string.Empty);
                        result += prefix;
                        parts.Add($"prefix:{prefix}");
                        syllables.Add(prefix);
                    }

                    break;
                case "suffix":
                    if (profile.Suffixes.Count > 0)
                    {
                        var suffix = rng.PickWeighted(profile.Suffixes, profile.SuffixWeights, string.Empty);
                        result += suffix;
                        parts.Add($"suffix:{suffix}");
                        syllables.Add(suffix);
                    }

                    break;
                case "infix":
                    if (profile.Infixes.Count > 0 && root.Length > 2)
                    {
                        var infix = rng.Pick(profile.Infixes, string.Empty);
                        var mid = root.Length / 2;
                        result = result[..Math.Max(0, result.Length - root.Length)] + root[..mid] + infix + root[mid..];
                        parts.Add($"infix:{infix}");
                    }

                    break;
                case "wordroot":
                    if (profile.WordRoots.Count > 0)
                    {
                        var wordRoot = rng.Pick(profile.WordRoots, root);
                        result += wordRoot;
                        parts.Add($"wordroot:{wordRoot}");
                        syllables.Add(wordRoot);
                    }
                    else
                    {
                        result += root;
                        parts.Add($"root:{root}");
                        syllables.AddRange(rootSyllables);
                    }

                    break;
                case "honorific":
                    if (profile.Honorifics.Count > 0)
                    {
                        var honorific = rng.Pick(profile.Honorifics, string.Empty);
                        result = string.IsNullOrEmpty(result) ? honorific : $"{honorific} {result}";
                        parts.Add($"honorific:{honorific}");
                    }

                    break;
            }
        }

        return new NameForgeMorphologyResult(result, structure, parts, syllables);
    }

    private static bool CanApplyMorphology(NameForgeMorphologyProfile profile)
    {
        var hasComplexStructure = profile.Structure.Any(static structure =>
            !string.Equals(structure, "root", StringComparison.Ordinal) &&
            structure.Contains('-', StringComparison.Ordinal));
        var hasAffixes =
            profile.Prefixes.Count > 0 ||
            profile.Suffixes.Count > 0 ||
            profile.Infixes.Count > 0 ||
            profile.WordRoots.Count > 0;

        return hasComplexStructure && hasAffixes;
    }

    private static string ApplyStyle(
        NameForgeRandom rng,
        string name,
        NameForgeStyleRules style,
        IReadOnlyList<string> syllables)
    {
        var result = name;
        var wantApostrophe = rng.Chance(style.ApostropheRate);
        var wantHyphen = rng.Chance(style.HyphenRate);

        if ((wantApostrophe || wantHyphen) && syllables.Count > 1)
        {
            var boundaries = NameForgeText.FindSyllableBoundaries(syllables);
            if (boundaries.Count > 0)
            {
                result = InsertStyleMarkers(result, boundaries, wantApostrophe, wantHyphen, rng);
            }
        }

        return NameForgeText.ApplyCapitalization(result, style.Capitalization);
    }

    private static string InsertStyleMarkers(
        string value,
        IReadOnlyList<int> boundaries,
        bool wantApostrophe,
        bool wantHyphen,
        NameForgeRandom rng)
    {
        if (wantApostrophe && wantHyphen)
        {
            if (boundaries.Count >= 2)
            {
                var first = rng.Pick(boundaries, 0);
                var second = rng.Pick(boundaries.Where(boundary => boundary != first).ToArray(), first);
                var higher = Math.Max(first, second);
                var lower = Math.Min(first, second);
                var withHigher = value[..higher] + "-" + value[higher..];
                return withHigher[..lower] + "'" + withHigher[lower..];
            }

            return rng.Chance(0.5d)
                ? NameForgeText.InsertAtBoundary(value, "'", boundaries, rng)
                : NameForgeText.InsertAtBoundary(value, "-", boundaries, rng);
        }

        return wantApostrophe
            ? NameForgeText.InsertAtBoundary(value, "'", boundaries, rng)
            : NameForgeText.InsertAtBoundary(value, "-", boundaries, rng);
    }

    private static (int Min, int Max) LengthRange(NameForgePhonologyProfile profile)
    {
        if (profile.LengthRange.Count >= 2 && profile.LengthRange[0] > 0 && profile.LengthRange[1] >= profile.LengthRange[0])
        {
            return (profile.LengthRange[0], profile.LengthRange[1]);
        }

        return (1, 2);
    }
}
