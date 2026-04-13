namespace Engine.Attributes;

using Engine.Graph;

/// <summary>
/// Stores scalar attribute values for all entities. Keyed by (EntityId, attributeName).
/// Attributes are static unless acted upon by systems. Clamped to kind-defined ranges.
/// </summary>
public sealed class AttributeStore
{
    private readonly Dictionary<(EntityId EntityId, string Name), float> _values = [];
    private readonly Dictionary<(EntityId EntityId, string Name), float> _minValues = [];
    private readonly Dictionary<(EntityId EntityId, string Name), float> _maxValues = [];

    // Rate-of-change ring buffer (last N deltas per attribute for trend display)
    private readonly Dictionary<(EntityId EntityId, string Name), Queue<float>> _deltas = [];
    private const int MaxDeltaHistory = 10;

    /// <summary>
    /// Initialize an attribute for an entity with min/max bounds and a starting value.
    /// </summary>
    public void Initialize(EntityId entityId, string name, float min, float max, float value)
    {
        var key = (entityId, name);
        _minValues[key] = min;
        _maxValues[key] = max;
        _values[key] = Math.Clamp(value, min, max);
        _deltas[key] = new Queue<float>();
    }

    public float Get(EntityId entityId, string name)
    {
        return _values.GetValueOrDefault((entityId, name));
    }

    public bool Has(EntityId entityId, string name)
    {
        return _values.ContainsKey((entityId, name));
    }

    /// <summary>
    /// Set an attribute to an absolute value (clamped to range).
    /// Records the delta for trend tracking.
    /// </summary>
    public void Set(EntityId entityId, string name, float value)
    {
        var key = (entityId, name);
        if (!_values.TryGetValue(key, out var previous))
        {
            return;
        }

        _values[key] = Math.Clamp(value, _minValues[key], _maxValues[key]);
        RecordDelta(key, _values[key] - previous);
    }

    /// <summary>
    /// Apply a delta to an attribute (clamped to range).
    /// </summary>
    public void Modify(EntityId entityId, string name, float delta)
    {
        var key = (entityId, name);
        if (!_values.TryGetValue(key, out var previous))
        {
            return;
        }

        _values[key] = Math.Clamp(previous + delta, _minValues[key], _maxValues[key]);
        RecordDelta(key, _values[key] - previous);
    }

    /// <summary>
    /// Get the recent rate of change (average of last N deltas).
    /// </summary>
    public float GetTrend(EntityId entityId, string name)
    {
        var key = (entityId, name);
        if (!_deltas.TryGetValue(key, out var deltas) || deltas.Count == 0)
        {
            return 0f;
        }

        return deltas.Sum() / deltas.Count;
    }

    /// <summary>
    /// Get all attribute names for an entity.
    /// </summary>
    public IEnumerable<string> GetAttributeNames(EntityId entityId)
    {
        return _values.Keys
            .Where(k => k.EntityId == entityId)
            .Select(k => k.Name);
    }

    /// <summary>
    /// Remove all attributes for an entity (e.g., on entity deletion).
    /// </summary>
    public void RemoveEntity(EntityId entityId)
    {
        var keys = _values.Keys.Where(k => k.EntityId == entityId).ToList();
        foreach (var key in keys)
        {
            _values.Remove(key);
            _minValues.Remove(key);
            _maxValues.Remove(key);
            _deltas.Remove(key);
        }
    }

    private void RecordDelta((EntityId EntityId, string Name) key, float delta)
    {
        if (!_deltas.TryGetValue(key, out var deltas))
        {
            return;
        }

        deltas.Enqueue(delta);
        while (deltas.Count > MaxDeltaHistory)
        {
            deltas.Dequeue();
        }
    }
}
