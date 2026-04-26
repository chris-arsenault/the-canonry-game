namespace Engine.Kinds;

using Engine.Graph;

/// <summary>
/// Central registry of entity kind definitions and relationship kind definitions.
/// Provides validation for entity creation and relationship creation.
/// Supports behavior dispatch via registered per-kind handlers.
/// </summary>
public sealed class KindRegistry
{
    private readonly Dictionary<EntityKind, KindDefinition> _kindDefs = [];
    private readonly Dictionary<string, RelationshipKindDefinition> _relKindDefs = [];
    private readonly Dictionary<EntityKind, Action<Entity>> _behaviorHandlers = [];

    // --- Kind Registration ---

    public void RegisterKind(KindDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _kindDefs[definition.Kind] = definition;
    }

    public void RegisterRelationshipKind(RelationshipKindDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _relKindDefs[definition.Name] = definition;
    }

    public void RegisterBehaviorHandler(EntityKind kind, Action<Entity> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _behaviorHandlers[kind] = handler;
    }

    // --- Lookups ---

    public KindDefinition? GetKindDefinition(EntityKind kind)
    {
        return _kindDefs.GetValueOrDefault(kind);
    }

    public EntityCategory GetCategory(EntityKind kind)
    {
        if (!_kindDefs.TryGetValue(kind, out var definition))
        {
            throw new InvalidOperationException($"Unknown kind '{kind}'.");
        }

        return definition.Category;
    }

    public RelationshipKindDefinition? GetRelationshipKindDefinition(string name)
    {
        return _relKindDefs.GetValueOrDefault(name);
    }

    public IEnumerable<RelationshipKindDefinition> GetValidRelationshipKinds(
        EntityCategory sourceCategory, EntityCategory targetCategory)
    {
        return _relKindDefs.Values.Where(r =>
            r.ValidSourceCategories.Contains(sourceCategory) &&
            r.ValidTargetCategories.Contains(targetCategory));
    }

    // --- Validation ---

    public bool IsValidSubtype(EntityKind kind, string subtype)
    {
        var def = GetKindDefinition(kind);
        return def is not null && def.IsValidSubtype(subtype);
    }

    /// <summary>
    /// Validates that a relationship kind is valid between the given source and target entities.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    public string? ValidateRelationship(string relationshipKind, EntityKind sourceKind, EntityKind targetKind)
    {
        var relDef = GetRelationshipKindDefinition(relationshipKind);
        if (relDef is null)
        {
            return $"Unknown relationship kind: {relationshipKind}";
        }

        var sourceCategory = GetCategory(sourceKind);
        var targetCategory = GetCategory(targetKind);

        if (!relDef.ValidSourceCategories.Contains(sourceCategory))
        {
            return $"Relationship '{relationshipKind}' does not accept source category {sourceCategory} (kind: {sourceKind})";
        }

        if (!relDef.ValidTargetCategories.Contains(targetCategory))
        {
            return $"Relationship '{relationshipKind}' does not accept target category {targetCategory} (kind: {targetKind})";
        }

        return null;
    }

    /// <summary>
    /// Checks whether a relationship kind is exclusive and already exists from the source.
    /// Used by WorldGraph to enforce exclusivity constraints.
    /// </summary>
    public bool IsExclusiveRelationship(string relationshipKind)
    {
        var relDef = GetRelationshipKindDefinition(relationshipKind);
        return relDef?.IsExclusive ?? false;
    }

    // --- Behavior Dispatch ---

    public void DispatchBehavior(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_behaviorHandlers.TryGetValue(entity.Kind, out var handler))
        {
            handler(entity);
        }
    }

    public bool HasBehaviorHandler(EntityKind kind) => _behaviorHandlers.ContainsKey(kind);
}
