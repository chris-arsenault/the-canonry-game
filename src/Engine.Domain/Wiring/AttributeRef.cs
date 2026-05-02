namespace Engine.Wiring;

using Engine.Kinds;

/// <summary>
/// A structured reference to a value in the simulation. Used by wiring contracts
/// to declare what behaviors/gambits read from and write to.
/// Not a string -- a typed path through the simulation state.
/// </summary>
public sealed class AttributeRef : IEquatable<AttributeRef>
{
    /// <summary>The scope of the reference.</summary>
    public RefScope Scope { get; }

    /// <summary>The entity kind this reference applies to (for kind-level validation).</summary>
    public EntityKind? Kind { get; }

    /// <summary>The attribute or property name.</summary>
    public string Name { get; }

    /// <summary>Optional relationship kind for related-entity references.</summary>
    public string? RelationshipKind { get; }

    public AttributeRef(RefScope scope, string name, EntityKind? kind = null, string? relationshipKind = null)
    {
        Scope = scope;
        Name = name;
        Kind = kind;
        RelationshipKind = relationshipKind;
    }

    // --- Factory methods for common patterns ---

    /// <summary>Attribute on the entity being evaluated: self.hp, self.warmth</summary>
    public static AttributeRef Self(string name, EntityKind kind) =>
        new(RefScope.Self, name, kind);

    /// <summary>Attribute on the resolved target/provider entity.</summary>
    public static AttributeRef Target(string name, EntityKind? kind = null) =>
        new(RefScope.Target, name, kind);

    /// <summary>Attribute on an entity at this entity's location.</summary>
    public static AttributeRef Location(string name, EntityKind kind) =>
        new(RefScope.Location, name, kind);

    /// <summary>Attribute on an entity related via a specific relationship kind</summary>
    public static AttributeRef Related(string relationshipKind, string attributeName, EntityKind targetKind) =>
        new(RefScope.Related, attributeName, targetKind, relationshipKind);

    /// <summary>BehaviorProgram fact about the relationship between self and the resolved target.</summary>
    public static AttributeRef TargetRelation(string relationshipKind, EntityKind? targetKind = null, string factName = "matched") =>
        new(RefScope.Relational, factName, targetKind, relationshipKind);

    /// <summary>Map cell property at entity's coordinates</summary>
    public static AttributeRef MapCell(string propertyName) =>
        new(RefScope.MapCell, propertyName);

    /// <summary>Resource value at entity's location</summary>
    public static AttributeRef Resource(string resourceName) =>
        new(RefScope.Resource, resourceName);

    /// <summary>Named structural/behavior program fact.</summary>
    public static AttributeRef StructuralFact(string name, EntityKind? kind = null) =>
        new(RefScope.Structural, name, kind);

    /// <summary>Entity's own coordinates</summary>
    public static AttributeRef Coordinates(EntityKind kind) =>
        StructuralFact("coordinates", kind);

    /// <summary>Entity's subtype</summary>
    public static AttributeRef Subtype(EntityKind kind) =>
        StructuralFact("subtype", kind);

    // --- Equality (for use in HashSet) ---

    public override string ToString() =>
        $"{Scope}.{(Kind.HasValue ? $"{Kind}." : "")}{(RelationshipKind is not null ? $"{RelationshipKind}." : "")}{Name}";

    public bool Equals(AttributeRef? other)
    {
        if (other is null) return false;
        return Scope == other.Scope && Kind == other.Kind &&
               Name == other.Name && RelationshipKind == other.RelationshipKind;
    }

    public override bool Equals(object? obj) => Equals(obj as AttributeRef);

    public override int GetHashCode() =>
        HashCode.Combine(Scope, Kind, Name, RelationshipKind);
}

/// <summary>
/// The scope of an AttributeRef -- where in the simulation the value lives.
/// </summary>
public enum RefScope
{
    /// <summary>Attribute on the entity itself.</summary>
    Self,

    /// <summary>Attribute on the behavior target/provider.</summary>
    Target,

    /// <summary>Attribute on an entity at this entity's location.</summary>
    Location,

    /// <summary>Attribute on a related entity (via relationship traversal).</summary>
    Related,

    /// <summary>BehaviorProgram fact about the relationship between self and the resolved target.</summary>
    Relational,

    /// <summary>Property of the entity's hex cell.</summary>
    MapCell,

    /// <summary>Resource value at entity's location.</summary>
    Resource,

    /// <summary>Structural property (coordinates, subtype, status).</summary>
    Structural,
}
