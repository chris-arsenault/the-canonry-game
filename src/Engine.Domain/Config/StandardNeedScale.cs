namespace Engine.Domain.Config;

/// <summary>
/// Shared semantics for bounded 0-100 scalar attributes where higher is better and
/// lower is worse. This provides one global concern/critical curve until the domain
/// grows explicit per-attribute tuning.
/// </summary>
public static class StandardNeedScale
{
    /// <summary>At or above this value, the attribute is comfortable and produces no behavior program urgency.</summary>
    public const float Comfortable = 70f;

    /// <summary>Below this value, the attribute becomes meaningfully concerning.</summary>
    public const float Concern = 40f;

    /// <summary>Below this value, the attribute is in the critical band.</summary>
    public const float Critical = 15f;

    public static bool AppliesTo(AttributeConfig attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return attribute.Min == 0f &&
               attribute.Max == 100f;
    }

    public static float ComputeUrgency(float value)
    {
        var clamped = Math.Clamp(value, 0f, 100f);
        if (clamped >= Comfortable)
        {
            return 0f;
        }

        if (clamped >= Concern)
        {
            var t = (Comfortable - clamped) / (Comfortable - Concern);
            return 0.25f * t;
        }

        if (clamped >= Critical)
        {
            var t = (Concern - clamped) / (Concern - Critical);
            return 0.25f + (0.45f * t);
        }

        var criticalT = (Critical - clamped) / Critical;
        return 0.70f + (0.30f * criticalT);
    }

    public static bool IsCritical(float value) =>
        Math.Clamp(value, 0f, 100f) <= Critical;
}
