namespace Engine.Attributes;

/// <summary>
/// A semantic plane attribute on an entity. Has X and Y components, each with
/// independent effects. References a SemanticPlaneDefinition for axis labels
/// and categorical label reduction.
///
/// All plane-specific logic (distance, labels, components) lives here.
/// </summary>
public sealed class SemanticPlane : IEntityProperty
{
    private float _baseX;
    private float _baseY;
    private readonly List<AttributeEffect> _tickEffectsX = [];
    private readonly List<AttributeEffect> _tickEffectsY = [];
    private readonly List<AttributeEffect> _staticEffectsX = [];
    private readonly List<AttributeEffect> _staticEffectsY = [];

    public string Name { get; }
    public float Min { get; }
    public float Max { get; }
    public SemanticPlaneDefinition Definition { get; }

    /// <summary>Effective X component: base + static effects.</summary>
    public float X => ComputeEffective(_baseX, _staticEffectsX);

    /// <summary>Effective Y component: base + static effects.</summary>
    public float Y => ComputeEffective(_baseY, _staticEffectsY);

    /// <summary>Categorical label from the 3x3 reducer based on current position.</summary>
    public string Label => Definition.Reduce(
        NormalizeToUnit(_baseX), NormalizeToUnit(_baseY));

    public SemanticPlane(
        string name, SemanticPlaneDefinition definition,
        float min, float max, float defaultX, float defaultY)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Name = name;
        Definition = definition;
        Min = min;
        Max = max;
        _baseX = Math.Clamp(defaultX, min, max);
        _baseY = Math.Clamp(defaultY, min, max);
    }

    /// <summary>Modify the X component by a delta.</summary>
    public void ModifyX(float delta)
    {
        _baseX = Math.Clamp(_baseX + delta, Min, Max);
    }

    /// <summary>Modify the Y component by a delta.</summary>
    public void ModifyY(float delta)
    {
        _baseY = Math.Clamp(_baseY + delta, Min, Max);
    }

    /// <summary>Euclidean distance to another plane attribute on the same plane.</summary>
    public float DistanceTo(SemanticPlane other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public void ApplyTickEffects()
    {
        ApplyComponentTickEffects(ref _baseX, _tickEffectsX);
        ApplyComponentTickEffects(ref _baseY, _tickEffectsY);
    }

    /// <summary>Add a tick effect to a component ("x" or "y").</summary>
    public void AddTickEffect(AttributeEffect effect, string component)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (component is "x") _tickEffectsX.Add(effect);
        else if (component is "y") _tickEffectsY.Add(effect);
    }

    /// <summary>Add a static effect to a component ("x" or "y").</summary>
    public void AddStaticEffect(AttributeEffect effect, string component)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (component is "x") _staticEffectsX.Add(effect);
        else if (component is "y") _staticEffectsY.Add(effect);
    }

    public void RemoveEffectsBySource(string sourceId)
    {
        _tickEffectsX.RemoveAll(e => e.SourceId == sourceId);
        _tickEffectsY.RemoveAll(e => e.SourceId == sourceId);
        _staticEffectsX.RemoveAll(e => e.SourceId == sourceId);
        _staticEffectsY.RemoveAll(e => e.SourceId == sourceId);
    }

    /// <summary>All tick effects on X component.</summary>
    public IReadOnlyList<AttributeEffect> TickEffectsX => _tickEffectsX;

    /// <summary>All tick effects on Y component.</summary>
    public IReadOnlyList<AttributeEffect> TickEffectsY => _tickEffectsY;

    /// <summary>All static effects on X component.</summary>
    public IReadOnlyList<AttributeEffect> StaticEffectsX => _staticEffectsX;

    /// <summary>All static effects on Y component.</summary>
    public IReadOnlyList<AttributeEffect> StaticEffectsY => _staticEffectsY;

    private float ComputeEffective(float baseValue, List<AttributeEffect> staticEffects)
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

        return Math.Clamp((baseValue + additive) * multiplicative, Min, Max);
    }

    private void ApplyComponentTickEffects(ref float baseValue, List<AttributeEffect> effects)
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

    /// <summary>Normalize a value from [Min, Max] to [0, 1] for the reducer.</summary>
    private float NormalizeToUnit(float value)
    {
        var range = Max - Min;
        return range > 0 ? (value - Min) / range : 0.5f;
    }
}
