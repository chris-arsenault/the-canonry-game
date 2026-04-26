namespace Engine.Projection;

using System.Collections.ObjectModel;

public enum ProjectionSymbolKind
{
    None = 0,
    Kind = 1,
    Subtype = 2,
    Culture = 3,
    Attribute = 4,
    Process = 5,
    StructuralFact = 6,
    RelationshipKind = 7,
    RelationshipFact = 8,
    Resource = 9,
    Terrain = 10,
    Pressure = 11,
    Generator = 12,
    Decision = 13,
    Card = 14,
    EventCode = 15,
    ReasonCode = 16,
    SourceCode = 17,
    Detector = 18,
    Goal = 19,
}

public readonly record struct ProjectionSymbolKey(ProjectionSymbolKind Kind, int Id);

public sealed record ProjectionSymbol(
    ProjectionSymbolKind Kind,
    int Id,
    string StableName,
    string DisplayName,
    string? Description = null,
    string? IconKey = null,
    string? ColorKey = null);

public sealed record ProjectionSymbolSeed(
    ProjectionSymbolKind Kind,
    int Id,
    string StableName,
    string? DisplayName = null,
    string? Description = null,
    string? IconKey = null,
    string? ColorKey = null);

public sealed record ProjectionEntityNameSeed(
    int EntityId,
    string DisplayName);

public readonly record struct ProjectionEntityNameKey(
    int EntityId,
    int KindId,
    int SubtypeId,
    int CultureId,
    int NameSeed);

public interface IProjectionSymbolCatalog
{
    IReadOnlyDictionary<ProjectionSymbolKey, ProjectionSymbol> Symbols { get; }

    bool TryResolve(ProjectionSymbolKind kind, int id, out ProjectionSymbol symbol);

    string ResolveDisplayName(ProjectionSymbolKind kind, int id, string? fallback = null);

    bool TryResolveEntityName(ProjectionEntityNameKey key, out string displayName);

    string ResolveEntityName(ProjectionEntityNameKey key, string? fallback = null);
}

public sealed class ProjectionSymbolCatalog : IProjectionSymbolCatalog
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

    private readonly Dictionary<ProjectionSymbolKey, ProjectionSymbol> _symbols = [];
    private readonly Dictionary<int, string> _entityNamesById = [];

    public IReadOnlyDictionary<ProjectionSymbolKey, ProjectionSymbol> Symbols =>
        new ReadOnlyDictionary<ProjectionSymbolKey, ProjectionSymbol>(_symbols);

    public IReadOnlyDictionary<int, string> ExplicitEntityNames =>
        new ReadOnlyDictionary<int, string>(_entityNamesById);

    public ProjectionSymbolCatalog RegisterSymbol(
        ProjectionSymbolKind kind,
        int id,
        string stableName,
        string? displayName = null,
        string? description = null,
        string? iconKey = null,
        string? colorKey = null)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Projection symbol IDs cannot be negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);
        var key = new ProjectionSymbolKey(kind, id);
        var display = string.IsNullOrWhiteSpace(displayName)
            ? ToDisplayName(stableName)
            : displayName;

        if (_symbols.TryGetValue(key, out var existing) &&
            !string.Equals(existing.StableName, stableName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Projection symbol {kind}:{id} is already registered as '{existing.StableName}'.");
        }

        _symbols[key] = new ProjectionSymbol(
            kind,
            id,
            stableName,
            display,
            description,
            iconKey,
            colorKey);
        return this;
    }

    public ProjectionSymbolCatalog RegisterEntityName(int entityId, string displayName)
    {
        if (entityId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), entityId, "Entity IDs must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        _entityNamesById[entityId] = displayName;
        return this;
    }

    public bool TryResolve(ProjectionSymbolKind kind, int id, out ProjectionSymbol symbol)
    {
        if (_symbols.TryGetValue(new ProjectionSymbolKey(kind, id), out var resolved))
        {
            symbol = resolved;
            return true;
        }

        symbol = null!;
        return false;
    }

    public string ResolveDisplayName(ProjectionSymbolKind kind, int id, string? fallback = null)
    {
        if (TryResolve(kind, id, out var symbol))
        {
            return symbol.DisplayName;
        }

        return fallback ?? $"{kind} {id}";
    }

    public bool TryResolveEntityName(ProjectionEntityNameKey key, out string displayName)
    {
        if (key.EntityId <= 0)
        {
            displayName = string.Empty;
            return false;
        }

        if (_entityNamesById.TryGetValue(key.EntityId, out var explicitName))
        {
            displayName = explicitName;
            return true;
        }

        displayName = GenerateEntityName(key);
        return true;
    }

    public string ResolveEntityName(ProjectionEntityNameKey key, string? fallback = null)
    {
        if (TryResolveEntityName(key, out var displayName))
        {
            return displayName;
        }

        return fallback ?? $"E{key.EntityId}";
    }

    public static string ToDisplayName(string stableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableName);

        var chars = stableName.Replace('-', '_').Split(
            '_',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (chars.Length == 0)
        {
            return stableName;
        }

        return string.Join(' ', chars.Select(Capitalize));
    }

    private string GenerateEntityName(ProjectionEntityNameKey key)
    {
        var baseLabel = ResolveEntityBaseLabel(key);
        var seed = key.NameSeed != 0 ? key.NameSeed : key.EntityId;
        var hash = Mix(seed, key.EntityId, key.KindId, key.SubtypeId, key.CultureId);
        var root = GeneratedNameRoots[hash % GeneratedNameRoots.Length];
        var tail = GeneratedNameTails[(hash / GeneratedNameRoots.Length) % GeneratedNameTails.Length];
        return $"{baseLabel} {root}{tail}";
    }

    private string ResolveEntityBaseLabel(ProjectionEntityNameKey key)
    {
        if (key.SubtypeId > 0 &&
            TryResolve(ProjectionSymbolKind.Subtype, key.SubtypeId, out var subtype) &&
            !IsNone(subtype.StableName))
        {
            return subtype.DisplayName;
        }

        if (key.KindId > 0 && TryResolve(ProjectionSymbolKind.Kind, key.KindId, out var kind))
        {
            return kind.DisplayName;
        }

        return "Entity";
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

    private static bool IsNone(string stableName) =>
        string.Equals(stableName, "none", StringComparison.OrdinalIgnoreCase);

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}

public static class ProjectionSymbolCatalogSeeder
{
    public static void Apply(
        ProjectionSymbolCatalog catalog,
        IEnumerable<ProjectionSymbolSeed> symbols,
        IEnumerable<ProjectionEntityNameSeed> entityNames)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(symbols);
        ArgumentNullException.ThrowIfNull(entityNames);

        foreach (var symbol in symbols)
        {
            catalog.RegisterSymbol(
                symbol.Kind,
                symbol.Id,
                symbol.StableName,
                symbol.DisplayName,
                symbol.Description,
                symbol.IconKey,
                symbol.ColorKey);
        }

        foreach (var name in entityNames)
        {
            catalog.RegisterEntityName(name.EntityId, name.DisplayName);
        }
    }
}
