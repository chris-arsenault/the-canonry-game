namespace Engine.Behaviors;

/// <summary>Shared variance helpers for mutations.</summary>
internal static class Variance
{
    /// <summary>Multiplicative variance for deltas: value * (1 + random(-variance, +variance)).</summary>
    internal static float Scale(float value, float variance, Random random)
    {
        if (variance <= 0f) return value;
        var factor = 1f + ((float)(random.NextDouble() * 2.0 - 1.0) * variance);
        return value * factor;
    }
}
