namespace Engine.Projection;

public readonly record struct ProjectionEntityNameContext(
    ProjectionEntityNameKey Key,
    ProjectionSymbol? Kind,
    ProjectionSymbol? Subtype,
    ProjectionSymbol? Culture);

public interface IProjectionEntityNameSynthesizer
{
    bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName);
}

public sealed class DefaultProjectionEntityNameSynthesizer : IProjectionEntityNameSynthesizer
{
    private static readonly string[] GeneratedNameRoots =
    [
        "Aren",
        "Bel",
        "Cair",
        "Dalen",
        "Eld",
        "Fenn",
        "Galen",
        "Hale",
        "Iven",
        "Jora",
        "Kael",
        "Lorn",
        "Maren",
        "Neris",
        "Oren",
        "Pelar",
        "Quen",
        "Saren",
        "Tovin",
        "Ulven",
        "Varen",
        "Yorin",
        "Zalen",
    ];

    private static readonly string[] GeneratedNameTails =
    [
        "a",
        "en",
        "is",
        "or",
        "un",
        "el",
        "ia",
        "ar",
        "in",
        "eth",
        "os",
        "ir",
    ];

    public static DefaultProjectionEntityNameSynthesizer Instance { get; } = new();

    private DefaultProjectionEntityNameSynthesizer()
    {
    }

    public bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName)
    {
        if (context.Key.EntityId <= 0)
        {
            displayName = string.Empty;
            return false;
        }

        var baseLabel = ResolveEntityBaseLabel(context);
        var seed = context.Key.NameSeed != 0 ? context.Key.NameSeed : context.Key.EntityId;
        var hash = Mix(
            seed,
            context.Key.EntityId,
            context.Key.KindId,
            context.Key.SubtypeId,
            context.Key.CultureId);
        var root = GeneratedNameRoots[hash % GeneratedNameRoots.Length];
        var tail = GeneratedNameTails[(hash / GeneratedNameRoots.Length) % GeneratedNameTails.Length];
        displayName = $"{baseLabel} {root}{tail}";
        return true;
    }

    private static string ResolveEntityBaseLabel(ProjectionEntityNameContext context)
    {
        if (context.Subtype is not null && !IsNone(context.Subtype.StableName))
        {
            return context.Subtype.DisplayName;
        }

        if (context.Kind is not null)
        {
            return context.Kind.DisplayName;
        }

        return "Entity";
    }

    private static bool IsNone(string stableName) =>
        string.Equals(stableName, "none", StringComparison.OrdinalIgnoreCase);

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
}
