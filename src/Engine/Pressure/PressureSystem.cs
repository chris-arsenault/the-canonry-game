namespace Engine.Pressure;

using Engine.Domain;
using Engine.Graph;
using Engine.Kernel;

/// <summary>
/// Computes and stores pressure values per scope instance (region or entity).
/// The BRIDGE between low-altitude simulation and mid-altitude card evaluation.
/// Pressure is read-only for low-altitude systems; only cards and direct modifications write to it.
/// </summary>
public sealed class PressureSystem
{
    private const int DefaultValueCapacity = 65536;
    private readonly List<PressureDefinition> _definitions = [];

    // Stored values: pressure runtime ID + scope entity ID -> value.
    private readonly PrimitiveFloatTable _values;
    private readonly Dictionary<string, PressureRuntimeId> _idsByName = new(StringComparer.Ordinal);

    // Direct modifications from cards (applied before homeostasis on next compute)
    private readonly PrimitiveFloatTable _pendingDeltas;

    public PressureSystem(int valueCapacity = DefaultValueCapacity)
    {
        _values = new PrimitiveFloatTable(valueCapacity);
        _pendingDeltas = new PrimitiveFloatTable(valueCapacity);
    }

    public void RegisterDefinition(PressureDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.RuntimeId.Value <= 0)
        {
            throw new InvalidOperationException($"Pressure '{definition.Name}' must have a registered runtime ID.");
        }

        _definitions.Add(definition);
        _idsByName.Add(definition.Name, definition.RuntimeId);
    }

    /// <summary>
    /// Get the current pressure value for a scope instance.
    /// Returns 0 if no value has been computed yet.
    /// </summary>
    public float GetValue(string pressureName, EntityId scopeId)
    {
        return GetValue(ResolvePressureId(pressureName), scopeId);
    }

    public float GetValue(PressureRuntimeId pressureId, EntityId scopeId) =>
        _values.GetOrDefault(pressureId.Value, scopeId.Value);

    /// <summary>
    /// Apply a direct delta to a pressure (from card effects).
    /// Applied on next compute before homeostasis/smoothing.
    /// </summary>
    public void ApplyDelta(string pressureName, EntityId scopeId, float delta)
    {
        ApplyDelta(ResolvePressureId(pressureName), scopeId, delta);
    }

    public void ApplyDelta(PressureRuntimeId pressureId, EntityId scopeId, float delta)
    {
        _pendingDeltas.Add(pressureId.Value, scopeId.Value, delta);
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
        var pressureId = ResolvePressureId(pressureName);
        var count = _values.CountSecondKeysForFirst(pressureId.Value);
        if (count == 0)
        {
            return [];
        }

        var scopeIds = new int[count];
        var copied = _values.CopySecondKeysForFirst(pressureId.Value, scopeIds);
        if (copied != count)
        {
            Array.Resize(ref scopeIds, copied);
        }

        return scopeIds.Select(static id => new EntityId(id));
    }

    private void ComputeForScope(PressureDefinition def, EntityId scopeId, IPressureContext context)
    {
        var previousValue = _values.GetOrDefault(def.RuntimeId.Value, scopeId.Value, def.Baseline);

        // 1. Evaluate all contributing factors
        var rawContribution = 0f;
        foreach (var factor in def.Factors)
        {
            rawContribution += factor.Evaluate(context);
        }

        // 2. Apply pending direct deltas from cards
        rawContribution += _pendingDeltas.GetOrDefault(def.RuntimeId.Value, scopeId.Value);

        // 3. Target value = baseline + contributions
        var targetValue = def.Baseline + rawContribution;

        // 4. Apply homeostasis (drift toward baseline)
        targetValue = targetValue + ((def.Baseline - targetValue) * def.HomeostasisRate);

        // 5. Apply smoothing (blend with previous)
        var newValue = previousValue + ((targetValue - previousValue) * def.SmoothingFactor);

        // 6. Clamp to range
        _values.Set(def.RuntimeId.Value, scopeId.Value, Math.Clamp(newValue, -100f, 100f));
    }

    private PressureRuntimeId ResolvePressureId(string pressureName)
    {
        if (_idsByName.TryGetValue(pressureName, out var id))
        {
            return id;
        }

        throw new KeyNotFoundException($"Unknown pressure '{pressureName}'.");
    }
}
