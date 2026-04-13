namespace Engine.Attributes;

using Engine.Graph;

/// <summary>
/// An active effect that modifies an entity's attribute.
/// Effects are sourced from relationships and follow PF2e stacking rules:
/// same-type bonuses don't stack (take best), cross-type always stack.
/// </summary>
public sealed class ActiveEffect
{
    /// <summary>The entity being affected.</summary>
    public EntityId TargetEntityId { get; }

    /// <summary>The attribute being modified.</summary>
    public string AttributeName { get; }

    /// <summary>Effect type for stacking rules (political, cultural, artifact, environmental, etc.).</summary>
    public string EffectType { get; }

    /// <summary>The modification value. Positive = bonus, negative = penalty.</summary>
    public float Value { get; }

    /// <summary>Source relationship that created this effect. For auto-cleanup on culling.</summary>
    public EntityId SourceEntityId { get; }

    /// <summary>Human-readable description of the effect source for UI display.</summary>
    public string SourceDescription { get; }

    public ActiveEffect(
        EntityId targetEntityId,
        string attributeName,
        string effectType,
        float value,
        EntityId sourceEntityId,
        string sourceDescription)
    {
        TargetEntityId = targetEntityId;
        AttributeName = attributeName;
        EffectType = effectType;
        Value = value;
        SourceEntityId = sourceEntityId;
        SourceDescription = sourceDescription;
    }
}

/// <summary>
/// Manages active effects on entities and computes effective attribute values.
/// Follows PF2e typed bonus pattern:
/// - Same-type bonuses: take best (highest positive OR most negative penalty)
/// - Cross-type: always stack
/// - Every effect carries source metadata
/// </summary>
public sealed class EffectSystem
{
    // Effects indexed by target entity for fast lookup
    private readonly Dictionary<EntityId, List<ActiveEffect>> _effectsByTarget = [];

    // Also indexed by source entity for cleanup when relationships are culled
    private readonly Dictionary<EntityId, List<ActiveEffect>> _effectsBySource = [];

    public void AddEffect(ActiveEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (!_effectsByTarget.TryGetValue(effect.TargetEntityId, out var targetList))
        {
            targetList = [];
            _effectsByTarget[effect.TargetEntityId] = targetList;
        }

        targetList.Add(effect);

        if (!_effectsBySource.TryGetValue(effect.SourceEntityId, out var sourceList))
        {
            sourceList = [];
            _effectsBySource[effect.SourceEntityId] = sourceList;
        }

        sourceList.Add(effect);
    }

    /// <summary>
    /// Remove all effects sourced from a specific entity.
    /// Called when a relationship is culled.
    /// </summary>
    public void RemoveEffectsFromSource(EntityId sourceEntityId)
    {
        if (!_effectsBySource.TryGetValue(sourceEntityId, out var sourceEffects))
        {
            return;
        }

        foreach (var effect in sourceEffects)
        {
            if (_effectsByTarget.TryGetValue(effect.TargetEntityId, out var targetList))
            {
                targetList.Remove(effect);
            }
        }

        _effectsBySource.Remove(sourceEntityId);
    }

    /// <summary>
    /// Remove all effects targeting a specific entity.
    /// Called when an entity is removed.
    /// </summary>
    public void RemoveEffectsOnTarget(EntityId targetEntityId)
    {
        if (!_effectsByTarget.TryGetValue(targetEntityId, out var effects))
        {
            return;
        }

        foreach (var effect in effects)
        {
            if (_effectsBySource.TryGetValue(effect.SourceEntityId, out var sourceList))
            {
                sourceList.Remove(effect);
            }
        }

        _effectsByTarget.Remove(targetEntityId);
    }

    /// <summary>
    /// Compute the total effect modifier for an entity's attribute, applying PF2e stacking rules.
    /// Same-type bonuses: take best (highest positive). Same-type penalties: take worst (most negative).
    /// Cross-type effects always stack.
    /// </summary>
    public float ComputeEffectModifier(EntityId entityId, string attributeName)
    {
        if (!_effectsByTarget.TryGetValue(entityId, out var effects))
        {
            return 0f;
        }

        var relevantEffects = effects.Where(e => e.AttributeName == attributeName).ToList();
        if (relevantEffects.Count == 0)
        {
            return 0f;
        }

        // Group by effect type, apply stacking rules per type
        var byType = relevantEffects.GroupBy(e => e.EffectType);
        var total = 0f;

        foreach (var typeGroup in byType)
        {
            var bonuses = typeGroup.Where(e => e.Value > 0).ToList();
            var penalties = typeGroup.Where(e => e.Value < 0).ToList();

            // Same-type bonuses: take best (highest)
            if (bonuses.Count > 0)
            {
                total += bonuses.Max(e => e.Value);
            }

            // Same-type penalties: take worst (most negative)
            if (penalties.Count > 0)
            {
                total += penalties.Min(e => e.Value);
            }
        }

        return total;
    }

    /// <summary>
    /// Compute the effective attribute value: base + effect modifier.
    /// </summary>
    public float ComputeEffectiveValue(float baseValue, EntityId entityId, string attributeName)
    {
        return baseValue + ComputeEffectModifier(entityId, attributeName);
    }

    /// <summary>
    /// Get all active effects on an entity for a specific attribute.
    /// Used for UI display (showing the effect breakdown).
    /// </summary>
    public IReadOnlyList<ActiveEffect> GetEffects(EntityId entityId, string attributeName)
    {
        if (!_effectsByTarget.TryGetValue(entityId, out var effects))
        {
            return [];
        }

        return effects.Where(e => e.AttributeName == attributeName).ToList();
    }

    /// <summary>
    /// Get all active effects on an entity (all attributes).
    /// </summary>
    public IReadOnlyList<ActiveEffect> GetAllEffects(EntityId entityId)
    {
        return _effectsByTarget.TryGetValue(entityId, out var effects)
            ? effects.AsReadOnly()
            : [];
    }
}
