namespace Engine.Wiring;

using Engine.Domain;
using Engine.Kinds;

/// <summary>
/// A structured reference to a value in the simulation. Used by wiring contracts
/// to declare what processes/gambits read from and write to.
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
    public AttributeRuntimeId AttributeId { get; private set; }
    public PressureRuntimeId PressureId { get; private set; }
    public ResourceRuntimeId ResourceId { get; private set; }
    public StructuralFactRuntimeId StructuralFactId { get; private set; }

    /// <summary>Optional relationship kind for related-entity references.</summary>
    public string? RelationshipKind { get; }
    public RelationshipKindRuntimeId RelationshipKindId { get; private set; }

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

    /// <summary>ProcessProgram fact about the relationship between self and the resolved target.</summary>
    public static AttributeRef TargetRelation(string relationshipKind, EntityKind? targetKind = null, string factName = "matched") =>
        new(RefScope.Relational, factName, targetKind, relationshipKind);

    /// <summary>Map cell property at entity's coordinates</summary>
    public static AttributeRef MapCell(string propertyName) =>
        new(RefScope.MapCell, propertyName);

    /// <summary>Resource value at entity's location</summary>
    public static AttributeRef Resource(string resourceName) =>
        new(RefScope.Resource, resourceName);

    /// <summary>Pressure value in entity's region</summary>
    public static AttributeRef Pressure(string pressureName) =>
        new(RefScope.Pressure, pressureName);

    /// <summary>Named structural/process program fact.</summary>
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

    public void BindRuntimeIds(global::Engine.Domain.DomainIdRegistry runtimeIds)
    {
        ArgumentNullException.ThrowIfNull(runtimeIds);

        RelationshipKindId = RelationshipKind is null
            ? RelationshipKindRuntimeId.None
            : runtimeIds.RelationshipKindIdOf(RelationshipKind);

        AttributeId = Scope is RefScope.Self or RefScope.Target or RefScope.Location or RefScope.Related
            ? runtimeIds.AttributeIdOf(Name)
            : AttributeRuntimeId.None;
        PressureId = Scope == RefScope.Pressure
            ? runtimeIds.PressureIdOf(Name)
            : PressureRuntimeId.None;
        ResourceId = Scope == RefScope.Resource
            ? runtimeIds.ResourceIdOf(Name)
            : ResourceRuntimeId.None;
        StructuralFactId = Scope == RefScope.Structural
            ? runtimeIds.StructuralFactIdOf(Name)
            : StructuralFactRuntimeId.None;
    }
}

/// <summary>
/// The scope of an AttributeRef -- where in the simulation the value lives.
/// </summary>
public enum RefScope
{
    /// <summary>Attribute on the entity itself.</summary>
    Self,

    /// <summary>Attribute on the process target/provider.</summary>
    Target,

    /// <summary>Attribute on an entity at this entity's location.</summary>
    Location,

    /// <summary>Attribute on a related entity (via relationship traversal).</summary>
    Related,

    /// <summary>ProcessProgram fact about the relationship between self and the resolved target.</summary>
    Relational,

    /// <summary>Property of the entity's hex cell.</summary>
    MapCell,

    /// <summary>Resource value at entity's location.</summary>
    Resource,

    /// <summary>Pressure value in entity's region.</summary>
    Pressure,

    /// <summary>Structural property (coordinates, subtype, status).</summary>
    Structural,
}
