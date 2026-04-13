namespace Engine.Lifecycle;

using Engine.Graph;
using Engine.Kinds;

/// <summary>
/// Per-kind aging function. Takes time-since-creation and returns
/// the per-tick drain on the entity's health metric. Starts near
/// zero for new entities, increases with age.
/// </summary>
public interface IAgingFunction
{
    /// <summary>
    /// Compute the health drain for this tick based on entity age.
    /// Returns a positive value (amount to subtract from health).
    /// </summary>
    float ComputeDrain(int age);
}

/// <summary>
/// Linear aging: drain increases linearly with age.
/// drain = baseRate + (age * ageCoefficient)
/// Simple, predictable, good starting point for tuning.
/// </summary>
public sealed class LinearAging : IAgingFunction
{
    /// <summary>Base drain per tick (even for brand new entities). Default near zero.</summary>
    public float BaseRate { get; set; } = 0.001f;

    /// <summary>How much additional drain per unit of age.</summary>
    public float AgeCoefficient { get; set; } = 0.0001f;

    public float ComputeDrain(int age)
    {
        return BaseRate + (age * AgeCoefficient);
    }
}

/// <summary>
/// Configuration for the lifecycle system: maps entity kinds to their
/// aging functions and health attribute names.
/// </summary>
public sealed class LifecycleConfig
{
    private readonly Dictionary<EntityKind, (string HealthAttribute, IAgingFunction Aging)> _configs = [];

    public void Register(EntityKind kind, string healthAttribute, IAgingFunction aging)
    {
        ArgumentNullException.ThrowIfNull(aging);
        _configs[kind] = (healthAttribute, aging);
    }

    public (string HealthAttribute, IAgingFunction Aging)? GetConfig(EntityKind kind)
    {
        return _configs.TryGetValue(kind, out var config) ? config : null;
    }

    public bool HasConfig(EntityKind kind) => _configs.ContainsKey(kind);
}
