namespace Engine.Projection;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage(
    "Design",
    "CA1027:Mark enums with FlagsAttribute",
    Justification = "Projection symbol kinds are stable identifiers, not bit flags.")]
public enum ProjectionSymbolKind
{
    None = 0,
    Kind = 1,
    Subtype = 2,
    Culture = 3,
    Attribute = 4,
    Behavior = 5,
    StructuralFact = 6,
    RelationshipKind = 7,
    Terrain = 10,
    TravelStructure = 11,
    Generator = 12,
    Gambit = 13,
    EventCode = 14,
    ReasonCode = 15,
    SourceCode = 16,
    Goal = 18,
    Verb = 19,
    Continent = 20,
    Superregion = 21,
    Trait = 22,
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
    private readonly Dictionary<ProjectionSymbolKey, ProjectionSymbol> _symbols = [];
    private readonly Dictionary<int, string> _entityNamesById = [];
    private IProjectionEntityNameSynthesizer? _entityNameSynthesizer;
    private ProvenanceProjection? _provenance;
    private Func<int, ProjectionEntityNameKey?>? _entityNameKeyProvider;
    private readonly HashSet<int> _resolvingEntityNames = [];

    public IReadOnlyDictionary<ProjectionSymbolKey, ProjectionSymbol> Symbols =>
        new ReadOnlyDictionary<ProjectionSymbolKey, ProjectionSymbol>(_symbols);

    public IReadOnlyDictionary<int, string> ExplicitEntityNames =>
        new ReadOnlyDictionary<int, string>(_entityNamesById);

    public ProjectionSymbolCatalog UseEntityNameSynthesizer(IProjectionEntityNameSynthesizer synthesizer)
    {
        ArgumentNullException.ThrowIfNull(synthesizer);
        _entityNameSynthesizer = synthesizer;
        return this;
    }

    public ProjectionSymbolCatalog UseProvenanceProjection(ProvenanceProjection provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        _provenance = provenance;
        return this;
    }

    public ProjectionSymbolCatalog UseEntityNameKeyProvider(Func<int, ProjectionEntityNameKey?> keyProvider)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        _entityNameKeyProvider = keyProvider;
        return this;
    }

    public ProjectionSymbolCatalog UseEntityProjectionState(UiProjectionState projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return UseEntityNameKeyProvider(entityId =>
            projection.Entities.TryGetValue(entityId, out var entity)
                ? new ProjectionEntityNameKey(
                    entity.EntityId,
                    entity.KindId,
                    entity.SubtypeId,
                    entity.CultureId,
                    entity.NameSeed)
                : null);
    }

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

        if (!_resolvingEntityNames.Add(key.EntityId))
        {
            displayName = string.Empty;
            return false;
        }

        try
        {
            var context = CreateEntityNameContext(key);
            if (_entityNameSynthesizer is not null &&
                _entityNameSynthesizer.TrySynthesizeName(context, out displayName) &&
                !string.IsNullOrWhiteSpace(displayName))
            {
                return true;
            }

            return DefaultProjectionEntityNameSynthesizer.Instance.TrySynthesizeName(context, out displayName);
        }
        finally
        {
            _resolvingEntityNames.Remove(key.EntityId);
        }
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

    private ProjectionEntityNameContext CreateEntityNameContext(ProjectionEntityNameKey key)
    {
        ProjectionSymbol? kind = null;
        ProjectionSymbol? subtype = null;
        ProjectionSymbol? culture = null;
        if (key.KindId > 0 && TryResolve(ProjectionSymbolKind.Kind, key.KindId, out var resolvedKind))
        {
            kind = resolvedKind;
        }

        if (key.SubtypeId > 0 && TryResolve(ProjectionSymbolKind.Subtype, key.SubtypeId, out var resolvedSubtype))
        {
            subtype = resolvedSubtype;
        }

        if (key.CultureId > 0 && TryResolve(ProjectionSymbolKind.Culture, key.CultureId, out var resolvedCulture))
        {
            culture = resolvedCulture;
        }

        return new ProjectionEntityNameContext(key, kind, subtype, culture, CreateEntityNameProvenance(key.EntityId));
    }

    private ProjectionEntityNameProvenance? CreateEntityNameProvenance(int entityId)
    {
        if (_provenance is null ||
            !_provenance.TryGetEntityCreatedByInvocation(entityId, out var invocationId) ||
            !_provenance.TryGetInvocation(invocationId, out var invocation))
        {
            return null;
        }

        var parent = CreateParentInvocationContext(entityId, invocation.InvocationId);
        var trigger = CreateRelationshipTriggerContext(entityId, invocation.InvocationId);

        return new ProjectionEntityNameProvenance
        {
            Invocation = invocation,
            Producer = ResolveProducerSymbolOrNull(invocation.ProducerKindCode, invocation.ProducerId),
            PrimaryEntityName = TryResolveReferencedEntityName(entityId, invocation.PrimaryEntityId),
            SecondaryEntityName = TryResolveReferencedEntityName(entityId, invocation.SecondaryEntityId),
            ParentInvocation = parent.Invocation,
            ParentProducer = parent.Producer,
            ParentPrimaryEntityName = parent.PrimaryEntityName,
            ParentSecondaryEntityName = parent.SecondaryEntityName,
            RelationshipTrigger = trigger.Trigger,
            RelationshipTriggerKind = trigger.RelationshipKind,
            RelationshipTriggerSourceEntityName = trigger.SourceEntityName,
            RelationshipTriggerTargetEntityName = trigger.TargetEntityName,
            Outputs = _provenance.OutputsOfInvocation(invocation.InvocationId),
            ParentChain = _provenance.EnumerateParentChain(invocation.InvocationId).ToArray(),
        };
    }

    private ParentInvocationNameContext CreateParentInvocationContext(int entityId, int invocationId)
    {
        if (_provenance is null ||
            !_provenance.TryGetParentInvocation(invocationId, out var parent))
        {
            return default;
        }

        return new ParentInvocationNameContext(
            parent,
            ResolveProducerSymbolOrNull(parent.ProducerKindCode, parent.ProducerId),
            TryResolveReferencedEntityName(entityId, parent.PrimaryEntityId),
            TryResolveReferencedEntityName(entityId, parent.SecondaryEntityId));
    }

    private RelationshipTriggerNameContext CreateRelationshipTriggerContext(int entityId, int invocationId)
    {
        if (_provenance is null ||
            !_provenance.TryGetRelationshipTrigger(invocationId, out var trigger))
        {
            return default;
        }

        ProjectionSymbol? relationshipKind = null;
        if (trigger.RelationshipKindId > 0 &&
            TryResolve(ProjectionSymbolKind.RelationshipKind, trigger.RelationshipKindId, out var resolvedRelationshipKind))
        {
            relationshipKind = resolvedRelationshipKind;
        }

        return new RelationshipTriggerNameContext(
            trigger,
            relationshipKind,
            TryResolveReferencedEntityName(entityId, trigger.SourceEntityId),
            TryResolveReferencedEntityName(entityId, trigger.TargetEntityId));
    }

    private ProjectionSymbol? ResolveProducerSymbolOrNull(int producerKindCode, int producerId) =>
        TryResolveProducerSymbol(producerKindCode, producerId, out var producer)
            ? producer
            : null;

    private string? TryResolveReferencedEntityName(int currentEntityId, int referencedEntityId)
    {
        if (referencedEntityId <= 0 || referencedEntityId == currentEntityId)
        {
            return null;
        }

        if (_entityNamesById.TryGetValue(referencedEntityId, out var explicitName))
        {
            return explicitName;
        }

        if (_entityNameKeyProvider?.Invoke(referencedEntityId) is not { } key ||
            key.EntityId <= 0 ||
            _resolvingEntityNames.Contains(key.EntityId))
        {
            return null;
        }

        return TryResolveEntityName(key, out var displayName) &&
            !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : null;
    }

    private bool TryResolveProducerSymbol(
        int producerKindCode,
        int producerId,
        out ProjectionSymbol symbol)
    {
        if (producerId > 0 &&
            TryResolveProducerSymbolKind(producerKindCode, out var symbolKind) &&
            TryResolve(symbolKind, producerId, out symbol))
        {
            return true;
        }

        symbol = null!;
        return false;
    }

    private static bool TryResolveProducerSymbolKind(int producerKindCode, out ProjectionSymbolKind symbolKind)
    {
        symbolKind = producerKindCode switch
        {
            ProvenanceProducerKindCodes.Generator => ProjectionSymbolKind.Generator,
            ProvenanceProducerKindCodes.Gambit => ProjectionSymbolKind.Gambit,
            _ => ProjectionSymbolKind.None,
        };

        return symbolKind != ProjectionSymbolKind.None;
    }

    private readonly record struct ParentInvocationNameContext(
        ProvenanceInvocationProjection? Invocation,
        ProjectionSymbol? Producer,
        string? PrimaryEntityName,
        string? SecondaryEntityName);

    private readonly record struct RelationshipTriggerNameContext(
        ProvenanceRelationshipTriggerProjection? Trigger,
        ProjectionSymbol? RelationshipKind,
        string? SourceEntityName,
        string? TargetEntityName);

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
