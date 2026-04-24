namespace Engine.Generators;

using Engine.Domain.IceRemembers;
using Engine.Kinds;

public static class NameSynthesizer
{
    private static readonly string[] NpcSyllables =
    [
        "bren",
        "fel",
        "sor",
        "ka",
        "mir",
        "dal",
        "tho",
        "ren",
    ];

    private static readonly string[] SettlementSyllables =
    [
        "iron",
        "frost",
        "sun",
        "stone",
        "wave",
        "high",
        "low",
        "south",
        "north",
        "old",
        "new",
        "bright",
    ];

    private static readonly string[] SettlementSuffixes =
    [
        "hold",
        "haven",
        "reach",
        "watch",
        "port",
        "gate",
        "fall",
    ];

    private static readonly string[] FactionAdjectives =
    [
        "Red",
        "Iron",
        "Silent",
        "Hungry",
        "Golden",
        "Forgotten",
        "Ember",
        "Silver",
    ];

    private static readonly string[] FactionNouns =
    [
        "Compact",
        "League",
        "Covenant",
        "Host",
        "Kin",
        "Assembly",
        "Circle",
        "Order",
    ];

    private static readonly string[] ArtifactPrefixes =
    [
        "storm",
        "night",
        "sun",
        "iron",
        "ember",
        "thorn",
        "frost",
        "dawn",
    ];

    private static readonly string[] ArtifactSuffixes =
    [
        "cleaver",
        "breaker",
        "shield",
        "crown",
        "song",
        "tear",
        "rune",
        "chain",
    ];

    private static readonly string[] RegionDescriptors =
    [
        "hush",
        "ember",
        "pale",
        "broken",
        "deep",
        "old",
        "black",
        "white",
        "salt",
        "cold",
    ];

    private static readonly string[] SeaSuffixes =
    [
        "sea",
        "reach",
        "sound",
        "water",
    ];

    private static readonly string[] ShelfSuffixes =
    [
        "shelf",
        "ledge",
        "face",
        "wall",
    ];

    private static readonly string[] ScarSuffixes =
    [
        "scar",
        "rent",
        "breach",
        "wound",
    ];

    private static readonly string[] GenericRegionSuffixes =
    [
        "march",
        "waste",
        "expanse",
        "reach",
    ];

    private static readonly string[] HeroPrefixes = ["Captain", "Sir"];
    private static readonly string[] ScholarPrefixes = ["Elder", "Master"];
    private static readonly string[] WarriorPrefixes = ["Captain", "Warden"];

    public static string SynthesizeName(EntityKind kind, string subtype, string culture, Random rng)
    {
        ArgumentException.ThrowIfNullOrEmpty(subtype);
        ArgumentNullException.ThrowIfNull(rng);

        _ = culture;

        if (kind == IceRemembersKinds.Penguin) return BuildPenguinName(subtype, rng);
        if (kind == IceRemembersKinds.Settlement) return BuildSettlementName(rng);
        if (kind == IceRemembersKinds.Faction) return $"The {Pick(FactionAdjectives, rng)} {Pick(FactionNouns, rng)}";
        if (kind == IceRemembersKinds.Artifact) return BuildArtifactName(rng);
        if (kind == IceRemembersKinds.Region) return BuildRegionName(subtype, rng);
        return "Unnamed";
    }

    private static string BuildPenguinName(string subtype, Random rng)
    {
        var core = Capitalize(Pick(NpcSyllables, rng) + Pick(NpcSyllables, rng));
        var prefix = subtype switch
        {
            "warden" => Pick(HeroPrefixes, rng),
            "scholar" => Pick(ScholarPrefixes, rng),
            "warrior" => Pick(WarriorPrefixes, rng),
            "merchant" => null,
            _ => null,
        };

        return prefix is null ? core : $"{prefix} {core}";
    }

    private static string BuildSettlementName(Random rng)
    {
        var syllableCount = rng.Next(2, 4);
        var parts = new List<string>(capacity: syllableCount);
        for (var i = 0; i < syllableCount - 1; i++)
        {
            parts.Add(Pick(SettlementSyllables, rng));
        }

        parts.Add(Pick(SettlementSuffixes, rng));
        return Capitalize(string.Concat(parts));
    }

    private static string BuildArtifactName(Random rng)
    {
        var prefixCount = rng.Next(1, 3);
        var parts = new List<string>(capacity: prefixCount + 1);
        for (var i = 0; i < prefixCount; i++)
        {
            parts.Add(Pick(ArtifactPrefixes, rng));
        }

        parts.Add(Pick(ArtifactSuffixes, rng));
        return Capitalize(string.Concat(parts));
    }

    private static string BuildRegionName(string subtype, Random rng)
    {
        var descriptor = Capitalize(Pick(RegionDescriptors, rng));
        var suffixes = subtype switch
        {
            "sea" => SeaSuffixes,
            "shelf" => ShelfSuffixes,
            "scar" => ScarSuffixes,
            _ => GenericRegionSuffixes,
        };

        return $"{descriptor} {Capitalize(Pick(suffixes, rng))}";
    }

    private static string Pick(string[] values, Random rng) =>
        values[rng.Next(values.Length)];

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
}
