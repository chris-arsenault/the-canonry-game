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

    /// <summary>Whether this is a 2D semantic plane attribute (has X and Y components).</summary>
    public bool Is2D { get; }

    // 2D components (only used when Is2D = true)
    private float _baseX;
    private float _baseY;
    private readonly List<AttributeEffect> _tickEffectsX = [];
    private readonly List<AttributeEffect> _tickEffectsY = [];
    private readonly List<AttributeEffect> _staticEffectsX = [];
    private readonly List<AttributeEffect> _staticEffectsY = [];

    /// <summary>X component of a 2D attribute. Returns 0 for scalar attributes.</summary>
    public float X => !Is2D ? 0f : ComputeComponent(_baseX, _staticEffectsX);

    /// <summary>Y component of a 2D attribute. Returns 0 for scalar attributes.</summary>
    public float Y => !Is2D ? 0f : ComputeComponent(_baseY, _staticEffectsY);

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

    /// <summary>Create a scalar attribute.</summary>
    public AttributeInstance(string name, float min, float max, float defaultValue)
    {
        Name = name;
        Min = min;
        Max = max;
        Base = Math.Clamp(defaultValue, min, max);
    }

    /// <summary>Create a 2D semantic plane attribute.</summary>
    public AttributeInstance(string name, float min, float max, float defaultX, float defaultY)
    {
        Name = name;
        Min = min;
        Max = max;
        Is2D = true;
        _baseX = Math.Clamp(defaultX, min, max);
        _baseY = Math.Clamp(defaultY, min, max);
        Base = 0f; // Not meaningful for 2D — use X and Y
    }

    /// <summary>Modify the base value by a delta. For scalar attributes.</summary>
    public void Modify(float delta)
    {
        var before = Base;
        Base = Math.Clamp(Base + delta, Min, Max);
        TrackDelta(Base - before);
    }

    /// <summary>Modify the X component by a delta. For 2D attributes.</summary>
    public void ModifyX(float delta)
    {
        _baseX = Math.Clamp(_baseX + delta, Min, Max);
    }

    /// <summary>Modify the Y component by a delta. For 2D attributes.</summary>
    public void ModifyY(float delta)
    {
        _baseY = Math.Clamp(_baseY + delta, Min, Max);
    }

    /// <summary>Set the base value directly. Rare — used for initialization or resets.</summary>
    public void SetBase(float value)
    {
        Base = Math.Clamp(value, Min, Max);
    }

    /// <summary>Apply tick effects. For scalar: base += sum * product. For 2D: same per component.</summary>
    public void ApplyTickEffects()
    {
        if (!Is2D)
        {
            ApplyTickEffectsScalar();
        }
        else
        {
            ApplyTickEffectsComponent(ref _baseX, _tickEffectsX);
            ApplyTickEffectsComponent(ref _baseY, _tickEffectsY);
        }
    }

    private void ApplyTickEffectsScalar()
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

    private void ApplyTickEffectsComponent(ref float baseValue, List<AttributeEffect> effects)
    {
        if (effects.Count == 0) return;

        var additive = 0f;
        var multiplicative = 1f;
        foreach (var effect in effects)
        {
            if (effect.IsMultiplier)
                multiplicative *= effect.Value;
            else
                additive += effect.Value;
        }

        baseValue = Math.Clamp(baseValue + (additive * multiplicative), Min, Max);
    }

    private static float ComputeComponent(float baseValue, List<AttributeEffect> staticEffects)
    {
        if (staticEffects.Count == 0) return baseValue;

        var additive = 0f;
        var multiplicative = 1f;
        foreach (var effect in staticEffects)
        {
            if (effect.IsMultiplier)
                multiplicative *= effect.Value;
            else
                additive += effect.Value;
        }

        return (baseValue + additive) * multiplicative;
    }

    /// <summary>Recent rate of change (average of last N deltas).</summary>
    public float Trend => _recentDeltas.Count == 0 ? 0f : _recentDeltas.Sum() / _recentDeltas.Count;

    private void TrackDelta(float delta)
    {
        _recentDeltas.Enqueue(delta);
        while (_recentDeltas.Count > TrendWindow) _recentDeltas.Dequeue();
    }

    /// <summary>Register a tick effect. For scalar attributes, targets the base.
    /// For 2D, use component parameter: null=scalar, "x"=X, "y"=Y.</summary>
    public void AddTickEffect(AttributeEffect effect, string? component = null)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (Is2D && component is "x") _tickEffectsX.Add(effect);
        else if (Is2D && component is "y") _tickEffectsY.Add(effect);
        else _tickEffects.Add(effect);
    }

    /// <summary>Register a static effect. For 2D, specify component.</summary>
    public void AddStaticEffect(AttributeEffect effect, string? component = null)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (Is2D && component is "x") _staticEffectsX.Add(effect);
        else if (Is2D && component is "y") _staticEffectsY.Add(effect);
        else _staticEffects.Add(effect);
    }

    /// <summary>Remove all effects from a given source across all components.</summary>
    public void RemoveEffectsBySource(string sourceId)
    {
        _tickEffects.RemoveAll(e => e.SourceId == sourceId);
        _staticEffects.RemoveAll(e => e.SourceId == sourceId);
        _tickEffectsX.RemoveAll(e => e.SourceId == sourceId);
        _tickEffectsY.RemoveAll(e => e.SourceId == sourceId);
        _staticEffectsX.RemoveAll(e => e.SourceId == sourceId);
        _staticEffectsY.RemoveAll(e => e.SourceId == sourceId);
    }

    /// <summary>All active tick effects on scalar/base (for inspection).</summary>
    public IReadOnlyList<AttributeEffect> TickEffects => _tickEffects;

    /// <summary>All active static effects on scalar/base (for inspection).</summary>
    public IReadOnlyList<AttributeEffect> StaticEffects => _staticEffects;

    /// <summary>Tick effects on X component (2D only).</summary>
    public IReadOnlyList<AttributeEffect> TickEffectsX => _tickEffectsX;

    /// <summary>Tick effects on Y component (2D only).</summary>
    public IReadOnlyList<AttributeEffect> TickEffectsY => _tickEffectsY;
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

    /// <summary>The effect value. For deltas: the amount added. For multipliers: the factor.
    /// Mutable so systems can update values (e.g., age-dependent lifecycle drain).</summary>
    public float Value { get; set; }

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
