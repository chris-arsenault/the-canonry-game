namespace Engine.Kinds;

/// <summary>
/// Defines the rules, valid subtypes, attributes, and relationship constraints for an entity kind.
/// KindDefinitions are registered at startup and used for validation throughout the simulation.
/// </summary>
public sealed class KindDefinition
{
    public EntityKind Kind { get; }
    public EntityCategory Category { get; }

    /// <summary>Valid subtype strings for this kind. Domain-defined, generic placeholders for Phase 1.</summary>
    public IReadOnlySet<string> ValidSubtypes { get; }

    /// <summary>Attribute definitions for this kind (name, min, max, default).</summary>
    public IReadOnlyList<AttributeDefinition> Attributes { get; }

    /// <summary>The attribute that serves as the health metric for lifecycle drain. Null if kind has no lifecycle.</summary>
    public string? HealthAttribute { get; }

    public KindDefinition(
        EntityKind kind,
        EntityCategory category,
        IReadOnlySet<string> validSubtypes,
        IReadOnlyList<AttributeDefinition> attributes,
        string? healthAttribute = null)
    {
        Kind = kind;
        Category = category;
        ValidSubtypes = validSubtypes;
        Attributes = attributes;
        HealthAttribute = healthAttribute;
    }

    public bool IsValidSubtype(string subtype) => ValidSubtypes.Contains(subtype);
}
