namespace Engine.Pressure;

using Engine.Domain;

/// <summary>
/// Scope type for pressure computation.
/// </summary>
public enum PressureScope
{
    /// <summary>Computed per geographic region.</summary>
    Regional,

    /// <summary>Computed per entity (typically a nation).</summary>
    Entity,
}

/// <summary>
/// Defines a pressure type with its computation parameters.
/// Pressure definitions are extensible: new types can be added as data without code changes.
/// </summary>
public sealed class PressureDefinition
{
    public string Name { get; }
    public PressureRuntimeId RuntimeId { get; }
    public PressureScope Scope { get; }

    /// <summary>Value the pressure drifts toward when no contributing factors push it.</summary>
    public float Baseline { get; set; }

    /// <summary>Rate at which the pressure drifts toward baseline per tick (0-1). Higher = faster return.</summary>
    public float HomeostasisRate { get; set; }

    /// <summary>Smoothing factor (0-1). How much of the new computed value to blend with the old. Lower = smoother.</summary>
    public float SmoothingFactor { get; set; }

    /// <summary>Contributing factors that push this pressure up or down.</summary>
    public System.Collections.ObjectModel.Collection<IContributingFactor> Factors { get; } = [];

    public PressureDefinition(
        string name,
        PressureScope scope,
        float baseline = 0f,
        float homeostasisRate = 0.1f,
        float smoothingFactor = 0.3f,
        PressureRuntimeId runtimeId = default)
    {
        Name = name;
        RuntimeId = runtimeId;
        Scope = scope;
        Baseline = baseline;
        HomeostasisRate = homeostasisRate;
        SmoothingFactor = smoothingFactor;
    }
}
