namespace Engine.Pressure;

using Engine.Graph;

/// <summary>
/// Computes and stores pressure values per scope instance (region or entity).
/// The BRIDGE between low-altitude simulation and mid-altitude card evaluation.
/// Pressure is read-only for low-altitude systems; only cards and direct modifications write to it.
/// </summary>
public sealed class PressureSystem
{
    private readonly List<PressureDefinition> _definitions = [];

    // Stored values: (pressureName, scopeId) -> value
    // For regional: scopeId is the region EntityId
    // For entity: scopeId is the entity EntityId
    private readonly Dictionary<(string PressureName, EntityId ScopeId), float> _values = [];

    // Direct modifications from cards (applied before homeostasis on next compute)
    private readonly Dictionary<(string PressureName, EntityId ScopeId), float> _pendingDeltas = [];

    public void RegisterDefinition(PressureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions.Add(definition);
    }

    /// <summary>
    /// Get the current pressure value for a scope instance.
    /// Returns 0 if no value has been computed yet.
    /// </summary>
    public float GetValue(string pressureName, EntityId scopeId)
    {
        return _values.GetValueOrDefault((pressureName, scopeId));
    }

    /// <summary>
    /// Apply a direct delta to a pressure (from card effects).
    /// Applied on next compute before homeostasis/smoothing.
    /// </summary>
    public void ApplyDelta(string pressureName, EntityId scopeId, float delta)
    {
        var key = (pressureName, scopeId);
        _pendingDeltas[key] = _pendingDeltas.GetValueOrDefault(key) + delta;
    }

    /// <summary>
    /// Compute all pressure values for all registered definitions and scope instances.
    /// Called once per tick during the Pressure phase.
    /// </summary>
    public void Compute(IReadOnlyDictionary<EntityId, IPressureContext> scopeContexts)
    {
        ArgumentNullException.ThrowIfNull(scopeContexts);

        foreach (var definition in _definitions)
        {
            foreach (var (scopeId, context) in scopeContexts)
            {
                ComputeForScope(definition, scopeId, context);
            }
        }

        // Clear pending deltas after application
        _pendingDeltas.Clear();
    }

    /// <summary>Get all registered pressure definition names.</summary>
    public IEnumerable<string> DefinitionNames => _definitions.Select(d => d.Name);

    /// <summary>Get all scope IDs that have values for a pressure type.</summary>
    public IEnumerable<EntityId> GetScopeIds(string pressureName)
    {
        return _values.Keys
            .Where(k => k.PressureName == pressureName)
            .Select(k => k.ScopeId);
    }

    private void ComputeForScope(PressureDefinition def, EntityId scopeId, IPressureContext context)
    {
        var key = (def.Name, scopeId);
        var previousValue = _values.GetValueOrDefault(key, def.Baseline);

        // 1. Evaluate all contributing factors
        var rawContribution = 0f;
        foreach (var factor in def.Factors)
        {
            rawContribution += factor.Evaluate(context);
        }

        // 2. Apply pending direct deltas from cards
        rawContribution += _pendingDeltas.GetValueOrDefault(key);

        // 3. Target value = baseline + contributions
        var targetValue = def.Baseline + rawContribution;

        // 4. Apply homeostasis (drift toward baseline)
        targetValue = targetValue + ((def.Baseline - targetValue) * def.HomeostasisRate);

        // 5. Apply smoothing (blend with previous)
        var newValue = previousValue + ((targetValue - previousValue) * def.SmoothingFactor);

        // 6. Clamp to range
        _values[key] = Math.Clamp(newValue, -100f, 100f);
    }
}
