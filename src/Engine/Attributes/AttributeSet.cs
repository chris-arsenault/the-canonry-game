namespace Engine.Attributes;

/// <summary>
/// Collection of AttributeInstances owned by an entity.
/// Initialized from domain config at entity creation.
/// </summary>
public sealed class AttributeSet
{
    private readonly Dictionary<string, AttributeInstance> _attributes = [];

    /// <summary>Get an attribute by name. Returns null if the attribute doesn't exist on this entity.</summary>
    public AttributeInstance? this[string name] =>
        _attributes.GetValueOrDefault(name);

    /// <summary>Get the effective value of an attribute. Returns 0 if not found.</summary>
    public float GetValue(string name) =>
        _attributes.TryGetValue(name, out var attr) ? attr.Value : 0f;

    /// <summary>Modify an attribute's base value by a delta. No-op if attribute doesn't exist.</summary>
    public void Modify(string name, float delta)
    {
        if (_attributes.TryGetValue(name, out var attr))
        {
            attr.Modify(delta);
        }
    }

    /// <summary>Add an attribute to the collection.</summary>
    public void Add(AttributeInstance attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _attributes[attribute.Name] = attribute;
    }

    /// <summary>Apply tick effects on all attributes. Called once per tick by the maintenance system.</summary>
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

    /// <summary>All attribute instances on this entity.</summary>
    public IEnumerable<AttributeInstance> All => _attributes.Values;

    /// <summary>Whether this entity has a named attribute.</summary>
    public bool Has(string name) => _attributes.ContainsKey(name);
}
