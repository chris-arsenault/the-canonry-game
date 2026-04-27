namespace IceRemembers;

using Engine.Projection;

public sealed class IceRemembersEntityNameSynthesizer : IProjectionEntityNameSynthesizer
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

    public static IceRemembersEntityNameSynthesizer Instance { get; } = new();

    private IceRemembersEntityNameSynthesizer()
    {
    }

    public bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName)
    {
        var kind = context.Kind?.StableName;
        if (string.IsNullOrWhiteSpace(kind))
        {
            displayName = string.Empty;
            return false;
        }

        var subtype = context.Subtype?.StableName ?? string.Empty;
        var rng = CreateRandom(context.Key);

        if (IsKind(kind, IceRemembersKinds.Penguin.Value))
        {
            displayName = BuildPenguinName(subtype, rng);
            return true;
        }

        if (IsKind(kind, IceRemembersKinds.Settlement.Value))
        {
            displayName = BuildSettlementName(rng);
            return true;
        }

        if (IsKind(kind, IceRemembersKinds.Faction.Value))
        {
            displayName = $"The {Pick(FactionAdjectives, rng)} {Pick(FactionNouns, rng)}";
            return true;
        }

        if (IsKind(kind, IceRemembersKinds.Artifact.Value))
        {
            displayName = BuildArtifactName(rng);
            return true;
        }

        if (IsKind(kind, IceRemembersKinds.Region.Value))
        {
            displayName = BuildRegionName(subtype, rng);
            return true;
        }

        displayName = string.Empty;
        return false;
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

    private static bool IsKind(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.Ordinal);

    private static Random CreateRandom(ProjectionEntityNameKey key)
    {
        var seed = key.NameSeed != 0 ? key.NameSeed : key.EntityId;
        return new Random(Mix(seed, key.EntityId, key.KindId, key.SubtypeId, key.CultureId));
    }

    private static int Mix(int a, int b, int c, int d, int e)
    {
        unchecked
        {
            var hash = 2166136261u;
            hash = (hash ^ (uint)a) * 16777619u;
            hash = (hash ^ (uint)b) * 16777619u;
            hash = (hash ^ (uint)c) * 16777619u;
            hash = (hash ^ (uint)d) * 16777619u;
            hash = (hash ^ (uint)e) * 16777619u;
            return (int)(hash & 0x7fffffffu);
        }
    }

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];
}
