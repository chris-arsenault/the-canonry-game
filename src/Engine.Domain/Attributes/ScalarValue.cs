namespace Engine.Attributes;

using Engine.Domain;

/// <summary>
/// A scalar attribute on an entity. Owns its base value, tick effects, and static effects.
/// All reads return the computed effective value. All writes go through Modify().
///
/// Composition:
///   Each tick:  base += sum(tick deltas) * product(tick multipliers)
///   On read:    effective = base + sum(static deltas) * product(static multipliers)
///
/// Effects are registered by source ID for cleanup when the source is removed.
/// </summary>
public sealed class ScalarValue : IEntityProperty
{
    private readonly List<AttributeEffect> _tickEffects = [];
    private readonly List<AttributeEffect> _staticEffects = [];
    private readonly Queue<float> _recentDeltas = new();
    private const int TrendWindow = 10;

    public string Name { get; }
    public AttributeRuntimeId RuntimeId { get; }
    public float Min { get; }
    public float Max { get; }
    public float Base { get; private set; }

    /// <summary>Computed effective value: base composed with static effects.</summary>
    public float Value
    {
        get
        {
            if (_staticEffects.Count == 0) return Base;

            var additive = 0f;
            var multiplicative = 1f;
            foreach (var effect in _staticEffects)
            {
                if (effect.IsMultiplier)
                    multiplicative *= effect.Value;
                else
                    additive += effect.Value;
            }

            return Math.Clamp((Base + additive) * multiplicative, Min, Max);
        }
    }

    public ScalarValue(string name, float min, float max, float defaultValue, AttributeRuntimeId runtimeId = default)
    {
        Name = name;
        RuntimeId = runtimeId;
        Min = min;
        Max = max;
        Base = Math.Clamp(defaultValue, min, max);
    }

    /// <summary>Modify the base value by a delta.</summary>
    public void Modify(float delta)
    {
        var before = Base;
        Base = Math.Clamp(Base + delta, Min, Max);
        TrackDelta(Base - before);
    }

    /// <summary>Set the base value directly. Rare — used for initialization or resets.</summary>
    public void SetBase(float value)
    {
        Base = Math.Clamp(value, Min, Max);
    }

    public void ApplyTickEffects()
    {
        if (_tickEffects.Count == 0) return;

        var additive = 0f;
        var multiplicative = 1f;
        foreach (var effect in _tickEffects)
        {
            if (effect.IsMultiplier)
                multiplicative *= effect.Value;
            else
                additive += effect.Value;
        }

        var before = Base;
        Base = Math.Clamp(Base + (additive * multiplicative), Min, Max);
        TrackDelta(Base - before);
    }

    /// <summary>Recent rate of change (average of last N deltas).</summary>
    public float Trend => _recentDeltas.Count == 0 ? 0f : _recentDeltas.Sum() / _recentDeltas.Count;

    private void TrackDelta(float delta)
    {
        _recentDeltas.Enqueue(delta);
        while (_recentDeltas.Count > TrendWindow) _recentDeltas.Dequeue();
    }

    public void AddTickEffect(AttributeEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _tickEffects.Add(effect);
    }

    public void AddStaticEffect(AttributeEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _staticEffects.Add(effect);
    }

    public void RemoveEffectsBySource(string sourceId)
    {
        _tickEffects.RemoveAll(e => e.SourceId == sourceId);
        _staticEffects.RemoveAll(e => e.SourceId == sourceId);
    }

    /// <summary>All active tick effects (for inspection).</summary>
    public IReadOnlyList<AttributeEffect> TickEffects => _tickEffects;

    /// <summary>All active static effects (for inspection).</summary>
    public IReadOnlyList<AttributeEffect> StaticEffects => _staticEffects;
}

/// <summary>
/// A single effect modifying an attribute. Tagged with source for cleanup.
/// </summary>
public sealed class AttributeEffect
{
    /// <summary>Identifies the source (e.g., "innate_drift", "artifact_42").</summary>
    public string SourceId { get; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; }

    /// <summary>The effect value. Mutable so systems can update (e.g., age-dependent drain).</summary>
    public float Value { get; set; }

    /// <summary>If true, multiplicative. If false, additive.</summary>
    public bool IsMultiplier { get; }

    public AttributeEffect(string sourceId, string description, float value, bool isMultiplier = false)
    {
        SourceId = sourceId;
        Description = description;
        Value = value;
        IsMultiplier = isMultiplier;
    }
}
