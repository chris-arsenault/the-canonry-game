namespace Engine.Attributes;

/// <summary>
/// Collection of IEntityProperty instances owned by an entity.
/// Stores both ScalarValue and SemanticPlane.
/// </summary>
public sealed class AttributeSet
{
    private readonly Dictionary<string, IEntityProperty> _attributes = [];

    /// <summary>Get an attribute by name. Returns null if not found.</summary>
    public IEntityProperty? this[string name] =>
        _attributes.GetValueOrDefault(name);

    /// <summary>Get a scalar attribute by name. Returns null if not found or not scalar.</summary>
    public ScalarValue? GetScalar(string name) =>
        _attributes.TryGetValue(name, out var attr) ? attr as ScalarValue : null;

    /// <summary>Get a semantic plane attribute by name. Returns null if not found or not a plane.</summary>
    public SemanticPlane? GetPlane(string name) =>
        _attributes.TryGetValue(name, out var attr) ? attr as SemanticPlane : null;

    /// <summary>Get the effective value of a scalar attribute. Returns 0 if not found or not scalar.</summary>
    public float GetValue(string name) =>
        GetScalar(name)?.Value ?? 0f;

    /// <summary>Modify a scalar attribute's base value by a delta. No-op if not found or not scalar.</summary>
    public void Modify(string name, float delta) =>
        GetScalar(name)?.Modify(delta);

    /// <summary>Add an attribute to the set.</summary>
    public void Add(IEntityProperty attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _attributes[attribute.Name] = attribute;
    }

    /// <summary>Apply tick effects on all attributes.</summary>
    public void ApplyTickEffects()
    {
        foreach (var (_, attr) in _attributes)
        {
            attr.ApplyTickEffects();
        }
    }

    /// <summary>Remove all effects from a source across all attributes.</summary>
    public void RemoveEffectsBySource(string sourceId)
    {
        foreach (var (_, attr) in _attributes)
        {
            attr.RemoveEffectsBySource(sourceId);
        }
    }

    /// <summary>All attribute names on this entity.</summary>
    public IEnumerable<string> Names => _attributes.Keys;

    /// <summary>All attributes on this entity.</summary>
    public IEnumerable<IEntityProperty> All => _attributes.Values;

    /// <summary>Whether this entity has a named attribute.</summary>
    public bool Has(string name) => _attributes.ContainsKey(name);
}
