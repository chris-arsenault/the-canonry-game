namespace Engine.Attributes;

/// <summary>
/// A single attribute on an entity. Owns its base value, tick effects, and static effects.
/// All reads return the computed effective value. All writes go through Modify().
///
/// Composition:
///   Each tick:  base += sum(tick deltas) * product(tick multipliers)
///   On read:    effective = base + sum(static deltas) * product(static multipliers)
///
/// Effects are registered by source ID for cleanup when the source is removed.
/// </summary>
public sealed class AttributeInstance
{
    private readonly List<AttributeEffect> _tickEffects = [];
    private readonly List<AttributeEffect> _staticEffects = [];
    private readonly Queue<float> _recentDeltas = new();
    private const int TrendWindow = 10;

    public string Name { get; }
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

    public AttributeInstance(string name, float min, float max, float defaultValue)
    {
        Name = name;
        Min = min;
        Max = max;
        Base = Math.Clamp(defaultValue, min, max);
    }

    /// <summary>Modify the base value by a delta. Used by mutations (eat, damage, etc.).</summary>
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

    /// <summary>Apply tick effects: base += sum(deltas) * product(multipliers). Called once per tick.</summary>
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

    /// <summary>Register a tick effect (applied each tick to modify base).</summary>
    public void AddTickEffect(AttributeEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _tickEffects.Add(effect);
    }

    /// <summary>Register a static effect (applied on read, doesn't modify base).</summary>
    public void AddStaticEffect(AttributeEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        _staticEffects.Add(effect);
    }

    /// <summary>Remove all effects from a given source (e.g., when relationship culled or artifact dropped).</summary>
    public void RemoveEffectsBySource(string sourceId)
    {
        _tickEffects.RemoveAll(e => e.SourceId == sourceId);
        _staticEffects.RemoveAll(e => e.SourceId == sourceId);
    }

    /// <summary>All active tick effects (for inspection/debugging).</summary>
    public IReadOnlyList<AttributeEffect> TickEffects => _tickEffects;

    /// <summary>All active static effects (for inspection/debugging).</summary>
    public IReadOnlyList<AttributeEffect> StaticEffects => _staticEffects;
}

/// <summary>
/// A single effect modifying an attribute. Tagged with source for cleanup.
/// </summary>
public sealed class AttributeEffect
{
    /// <summary>Identifies the source (e.g., "innate_drift", "artifact_42", "nation_membership_7").</summary>
    public string SourceId { get; }

    /// <summary>Human-readable description of where this effect comes from.</summary>
    public string Description { get; }

    /// <summary>The effect value. For deltas: the amount added. For multipliers: the factor.</summary>
    public float Value { get; }

    /// <summary>If true, this is a multiplicative effect. If false, additive.</summary>
    public bool IsMultiplier { get; }

    public AttributeEffect(string sourceId, string description, float value, bool isMultiplier = false)
    {
        SourceId = sourceId;
        Description = description;
        Value = value;
        IsMultiplier = isMultiplier;
    }
}
