namespace Engine.Projection;

public readonly record struct ProjectionEntityNameContext(
    ProjectionEntityNameKey Key,
    ProjectionSymbol? Kind,
    ProjectionSymbol? Subtype,
    ProjectionSymbol? Culture,
    ProjectionEntityNameProvenance? Provenance = null);

public sealed record ProjectionEntityNames(string DisplayName, string ReferenceName)
{
    public static ProjectionEntityNames FromDisplayName(string displayName) =>
        new(displayName, displayName);
}

public sealed record ProjectionEntityNameProvenance
{
    public required ProvenanceInvocationProjection Invocation { get; init; }

    public ProjectionSymbol? Producer { get; init; }

    public string? PrimaryEntityName { get; init; }

    public string? PrimaryEntityReferenceName { get; init; }

    public string? SecondaryEntityName { get; init; }

    public string? SecondaryEntityReferenceName { get; init; }

    public ProvenanceInvocationProjection? ParentInvocation { get; init; }

    public ProjectionSymbol? ParentProducer { get; init; }

    public string? ParentPrimaryEntityName { get; init; }

    public string? ParentPrimaryEntityReferenceName { get; init; }

    public string? ParentSecondaryEntityName { get; init; }

    public string? ParentSecondaryEntityReferenceName { get; init; }

    public ProvenanceRelationshipTriggerProjection? RelationshipTrigger { get; init; }

    public ProjectionSymbol? RelationshipTriggerKind { get; init; }

    public string? RelationshipTriggerSourceEntityName { get; init; }

    public string? RelationshipTriggerSourceEntityReferenceName { get; init; }

    public string? RelationshipTriggerTargetEntityName { get; init; }

    public string? RelationshipTriggerTargetEntityReferenceName { get; init; }

    public required IReadOnlyList<ProvenanceOutputProjection> Outputs { get; init; }

    public required IReadOnlyList<ProvenanceInvocationProjection> ParentChain { get; init; }
}

public interface IProjectionEntityNameSynthesizer
{
    bool TrySynthesizeName(ProjectionEntityNameContext context, out string displayName);
}

public interface IProjectionEntityNamesSynthesizer : IProjectionEntityNameSynthesizer
{
    bool TrySynthesizeNames(ProjectionEntityNameContext context, out ProjectionEntityNames names);
}

public sealed class DefaultProjectionEntityNameSynthesizer : IProjectionEntityNamesSynthesizer
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
        if (TrySynthesizeNames(context, out var names))
        {
            displayName = names.DisplayName;
            return true;
        }

        displayName = string.Empty;
        return false;
    }

    public bool TrySynthesizeNames(ProjectionEntityNameContext context, out ProjectionEntityNames names)
    {
        if (context.Key.EntityId <= 0)
        {
            names = ProjectionEntityNames.FromDisplayName(string.Empty);
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
        var displayName = $"{baseLabel} {root}{tail}";
        names = ProjectionEntityNames.FromDisplayName(displayName);
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
