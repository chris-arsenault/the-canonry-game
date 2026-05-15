namespace NameForge.Runtime;

using System.Text;

internal static class NameForgeText
{
    public static string ApplyCapitalization(string value, string style)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return style switch
        {
            "title" => Capitalize(value.ToLowerInvariant()),
            "titleWords" => CapitalizeWords(value),
            "allcaps" => value.ToUpperInvariant(),
            "lowercase" => value.ToLowerInvariant(),
            "mixed" => MixedCase(value),
            _ => value,
        };
    }

    public static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];

    public static string CapitalizeWords(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var start = 0;
        while (start < value.Length)
        {
            var end = start;
            while (end < value.Length && !char.IsWhiteSpace(value[end]))
            {
                end++;
            }

            if (end > start)
            {
                var part = value[start..end].ToLowerInvariant();
                builder.Append(Capitalize(part));
            }

            while (end < value.Length && char.IsWhiteSpace(value[end]))
            {
                builder.Append(value[end]);
                end++;
            }

            start = end;
        }

        return builder.ToString();
    }

    public static bool EndsWithAny(string value, IReadOnlyList<string> suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsAny(string value, IReadOnlyList<string> fragments)
    {
        foreach (var fragment in fragments)
        {
            if (value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static List<int> FindSyllableBoundaries(IReadOnlyList<string> syllables)
    {
        var boundaries = new List<int>(Math.Max(0, syllables.Count - 1));
        var position = 0;
        for (var i = 0; i < syllables.Count - 1; i++)
        {
            position += syllables[i].Length;
            boundaries.Add(position);
        }

        return boundaries;
    }

    public static string InsertAtBoundary(string value, string marker, IReadOnlyList<int> boundaries, NameForgeRandom rng)
    {
        if (boundaries.Count == 0)
        {
            return value;
        }

        var index = rng.Pick(boundaries, 0);
        return value[..index] + marker + value[index..];
    }

    private static string MixedCase(string value)
    {
        var builder = new StringBuilder(value.Length);
        var letterIndex = 0;
        foreach (var ch in value)
        {
            if (IsAsciiLetter(ch))
            {
                builder.Append(letterIndex % 2 == 0 ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
                letterIndex++;
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static bool IsAsciiLetter(char ch) =>
        (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
}
